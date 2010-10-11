using System;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Collections.Generic;
using System.Windows;

namespace OneBusAway.WP7.Model
{
    /// <summary>
    /// A cache for HTTP GET requests, backed by IsolatedStorage.
    /// The full version of .NET includes support for this already, but Silverlight does not.
    /// </summary>
    public class HttpCache
    {
        /// <summary>
        /// Allows multiple caches to coexist in storage.
        /// I.e. caches with different names are independent.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The time in seconds for which a cached entry is good.
        /// </summary>
        public int ExpirationPeriod { get; private set; }

        /// <summary>
        /// The maximum number of entries in the cache.
        /// If the cache is full, old entries will be evicted to make room for new ones.
        /// </summary>
        public int Capacity { get; private set; }

        /// <summary>
        /// </summary>
        /// <param name="name">Identifier for the desired cache.  Caches with the same name target the same underlying storage.</param>
        /// <param name="expirationPeriod">Time in seconds for which a cached entry is good</param>
        /// <param name="capacity">Maximum number of entries in the cache</param>
        public HttpCache(string name, int expirationPeriod, int capacity)
        {
            this.Name = name;
            this.ExpirationPeriod = expirationPeriod;
            this.Capacity = capacity;
            CacheCalls = 0;
            CacheHits = 0;
            CacheMisses = 0;
            CacheExpirations = 0;
            CacheEvictions = 0;
        }

        #region public methods

        public void DownloadStringAsync(Uri address)
        {
            CacheCalls++;
            // lookup address in cache
            string cachedResult = CacheLookup(address);
            if (cachedResult != null)
            {
                CacheHits++;
                CacheDownloadStringCompletedEventArgs eventArgs = new CacheDownloadStringCompletedEventArgs(cachedResult);
                // Invoke on a different thread.  Otherwise we make the callback from the same thread as the
                // original call and wierd things could happen.
                Deployment.Current.Dispatcher.BeginInvoke(() => CacheDownloadStringCompleted(this, eventArgs));
            }
            else
            {
                CacheMisses++;
                // not found, request data
                WebClient client = new WebClient();
                client.DownloadStringCompleted += new CacheCallback(this, address).Callback;
                client.DownloadStringAsync(address);
            }
        }

        /// <summary>
        /// Delete all data in the cache.
        /// </summary>
        public void Clear()
        {
            using (IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (iso.DirectoryExists(this.Name))
                {
                    // IsolatedStorage requires you to delete all the files before removing the directory
                    string[] files = iso.GetFileNames(this.Name + "\\*");
                    foreach (string file in files)
                    {
                        iso.DeleteFile(Path.Combine(this.Name,file));
                    }
                    iso.DeleteDirectory(this.Name);
                }
            }
            CacheMetadata m = new CacheMetadata(this);
            m.Clear();
        }

        /// <summary>
        /// Ensures that there is no entry for the given address in the cache.
        /// </summary>
        public void Invalidate(Uri address)
        {
            string fileName = MapAddressToFile(address);
            using (IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (iso.FileExists(fileName))
                {
                    iso.DeleteFile(fileName);
                }
            }
            CacheMetadata m = new CacheMetadata(this);
            m.RemoveUpdateTime(fileName);
        }

        #endregion

        #region diagnostic properties
        // Mainly useful for statistics of cache performance.
        // TODO We'd need to maintain a common instance for these to be useful.
        // That requires some more thought about how to register and clean up EventHandlers

        public int CacheCalls { get; private set; }
        public int CacheHits { get; private set; }
        public int CacheMisses { get; private set; }
        public int CacheExpirations { get; private set; }
        public int CacheEvictions { get; private set; }

        #endregion

        #region private helper methods

        /// <summary>
        /// Checks to see if we have a cached result for a given request
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private string CacheLookup(Uri address)
        {
            using (IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication())
            {
                // get isolatedstorage for this cache
                if (iso.DirectoryExists(this.Name))
                {
                    // get result file for this address
                    string fileName = MapAddressToFile(address);
                    if (iso.FileExists(fileName))
                    {
                        if (CheckForExpiration(fileName))
                        {
                            return null;
                        }
                        // all good! return the content
                        using (IsolatedStorageFileStream stream = iso.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                        {
                            using (StreamReader r = new StreamReader(stream))
                            {
                                return r.ReadToEnd();
                            }
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Adds a new result for given request
        /// </summary>
        /// <param name="address"></param>
        /// <param name="result"></param>
        private void CacheAddResult(Uri address, string result)
        {
            using (IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication())
            {
                // get isolatedstorage for this cache
                if (!iso.DirectoryExists(this.Name))
                {
                    iso.CreateDirectory(this.Name);
                }

                EvictIfNecessary(iso);

                string fileName = MapAddressToFile(address);
                using (IsolatedStorageFileStream stream = iso.OpenFile(fileName, FileMode.Create, FileAccess.Write))
                {
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        writer.Write(result);
                    }
                }
                UpdateExpiration(fileName);
            }
        }

        /// <summary>
        /// Evict an entry if we need to make room for a new one.
        /// </summary>
        /// <remarks>
        /// "A cache without an eviction policy is a memory leak"
        /// We might run into quota limits on IsolatedStorage.  We need to check those, and evict files to make room. 
        /// 
        /// Cache eviction policy is "least recently updated"
        /// Note this is not the same as least recently used.
        /// Rather, we're evicting the entry that will expire soonest.
        /// </remarks>
        /// <param name="iso"></param>
        private void EvictIfNecessary(IsolatedStorageFile iso)
        {
            string[] filesInCache = iso.GetFileNames(this.Name + "\\*");
            if (filesInCache.Length >= this.Capacity)
            {
                CacheMetadata m = new CacheMetadata(this);

                DateTime oldestFileTime = DateTime.MaxValue;
                string oldestFileName = null;
                foreach (string filename in filesInCache)
                {
                    // the GetFileNames call above does not return qualified paths, but we expect those for the rest of the calls
                    string qualifiedFilename = Path.Combine(this.Name, filename);
                    DateTime? updateTime = m.GetUpdateTime(qualifiedFilename);
                    if (null == updateTime)
                    {
                        // Then we have a file in the cache, but no record of it being put there... clean it up
                        // Most common way to hit this would be that I changed the internal naming format between versions.
                        iso.DeleteFile(qualifiedFilename);
                    }
                    else
                    {
                        if (updateTime.Value < oldestFileTime)
                        {
                            oldestFileTime = updateTime.Value;
                            oldestFileName = qualifiedFilename;
                        }
                    }
                }
                if (oldestFileName != null)
                {
                    iso.DeleteFile(oldestFileName);
                    m.RemoveUpdateTime(oldestFileName);
                    CacheEvictions++;
                }
            }
        }

        /// <summary>
        /// Maps a request URI to the file name where we will store it in the cache
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private string MapAddressToFile(Uri address)
        {
            // HACK - This is specific to the OneBusAway calls.  Ideally, figure out a way to inject this logic so that the cache stays general purpose.
            // Remove the application key, because
            // 1. it's long, and will push us over the max path length
            // 2. it's constant, so no sense storing the information
            // 3. it's somewhat private
            string queryString = address.Query.Substring(1); // remove the leading ?
            string[] parameters = queryString.Split('&');

            string newQueryString = "?";
            bool first = true;
            foreach(string parameter in parameters)
            {
                if (!parameter.StartsWith("key="))
                {
                    if (!first)
                    {
                        newQueryString += "&";
                    }
                    else
                    {
                        first = false;
                    }
                    newQueryString += parameter;
                }
            }
            UriBuilder builder = new UriBuilder(address);
            builder.Query = newQueryString;
          
            string escaped = builder.Uri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped);
            // tried Path.GetInvalidPathChars(). it doesn't seem to work here.
            escaped = escaped.Replace('/', '_');
            escaped = escaped.Replace(':', '_');
            escaped = escaped.Replace('?', '_');
            return Path.Combine(this.Name, escaped);
        }

        /// <summary>
        /// Checks if the specified file is expired.  If so, updates the cache accordingly.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>True if the cached file is expired</returns>
        private bool CheckForExpiration(string fileName)
        {
            CacheMetadata m = new CacheMetadata(this);
            if (m.IsExpired(fileName))
            {
                // purge the entry from the cache
                using (IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    iso.DeleteFile(fileName);
                }

                // and purge the metadata entry
                m.RemoveUpdateTime(fileName);

                CacheExpirations++;
                return true;
            }
            return false;
        }

        private void UpdateExpiration(string fileName)
        {
            CacheMetadata m = new CacheMetadata(this);
            m.AddUpdateTime(fileName, DateTime.Now);
        }

        /// <summary>
        /// Tracks metadata about a given file in the cache
        /// </summary>
        private class CacheMetadata
        {
            private HttpCache owner;

            // Silverlight doesn't track file update / creation time.
            // This approach is based on http://msdn.microsoft.com/en-us/magazine/dd434650.aspx
            private Dictionary<string, DateTime> fileUpdateTimes;

            public CacheMetadata(HttpCache owner)
            {
                this.owner = owner;
                IsolatedStorageSettings cacheSettings = IsolatedStorageSettings.ApplicationSettings;
                if (cacheSettings.Contains(owner.Name))
                {
                    // load existing settings store for this cache
                    fileUpdateTimes = cacheSettings[owner.Name] as Dictionary<string, DateTime>;
                }
                else
                {
                    // create new settings store for this cache
                    fileUpdateTimes = new Dictionary<string, DateTime>();
                    cacheSettings[owner.Name] = fileUpdateTimes;
                    cacheSettings.Save();
                }
            }

            public bool IsExpired(string filename)
            {
                if (fileUpdateTimes.ContainsKey(filename))
                {
                    DateTime lastGoodTime = fileUpdateTimes[filename].AddSeconds(owner.ExpirationPeriod);
                    return (lastGoodTime < DateTime.Now);
                }
                return true;
            }

            public DateTime? GetUpdateTime(string filename)
            {
                if (fileUpdateTimes.ContainsKey(filename))
                {
                    return fileUpdateTimes[filename];
                }
                return null;
            }

            public void AddUpdateTime(string filename, DateTime when)
            {
                fileUpdateTimes[filename] = when;
                // note this relies on referential integrity.
                // i.e. fileUpdateTimes is a reference to an object in the application settings
                IsolatedStorageSettings.ApplicationSettings.Save();
            }

            public void RemoveUpdateTime(string filename)
            {
                fileUpdateTimes.Remove(filename);
                // note this relies on referential integrity.
                // i.e. fileUpdateTimes is a reference to an object in the application settings
                IsolatedStorageSettings.ApplicationSettings.Save();
            }

            public void Clear()
            {
                fileUpdateTimes.Clear();
                IsolatedStorageSettings cacheSettings = IsolatedStorageSettings.ApplicationSettings;
                cacheSettings.Remove(owner.Name);
                cacheSettings.Save();
            }
            
        }

        #endregion

        #region Callback support (event, event handler, etc)

        /// <summary>
        /// Exists solely to hold a reference to the originally requested URI
        /// </summary>
        private class CacheCallback
        {
            private HttpCache owner;
            private Uri requestedAddress;

            public CacheCallback(HttpCache owner, Uri requestedAddress)
            {
                this.owner = owner;
                this.requestedAddress = requestedAddress;
            }

            public void Callback(object sender, DownloadStringCompletedEventArgs eventArgs)
            {
                // check for errors
                if (eventArgs.Cancelled)
                {
                    CacheDownloadStringCompletedEventArgs newArgs = CacheDownloadStringCompletedEventArgs.MakeCancelled();
                    owner.CacheDownloadStringCompleted(this, newArgs);
                }
                else if (eventArgs.Error != null)
                {
                    CacheDownloadStringCompletedEventArgs newArgs = new CacheDownloadStringCompletedEventArgs(eventArgs.Error);
                    owner.CacheDownloadStringCompleted(this, newArgs);
                }
                else
                {
                    // no errors -- add data to the cache
                    owner.CacheAddResult(requestedAddress, eventArgs.Result);
                    // and fire our event
                    CacheDownloadStringCompletedEventArgs newArgs = new CacheDownloadStringCompletedEventArgs(eventArgs.Result);
                    owner.CacheDownloadStringCompleted(this, newArgs);
                }
            }
        }


        // Yes, these mirror the ones defined in System.Net.
        // Those don't have public constructors, so they're not reusable.

        public event CacheDownloadStringCompletedEventHandler CacheDownloadStringCompleted;
        public delegate void CacheDownloadStringCompletedEventHandler(object sender, CacheDownloadStringCompletedEventArgs e);
        public class CacheDownloadStringCompletedEventArgs : AsyncCompletedEventArgs 
        {
            /// <summary>
            /// Indicates successful completion
            /// </summary>
            /// <param name="result"></param>
            public CacheDownloadStringCompletedEventArgs(string result) 
            {
                this.Result = result;
                this.Cancelled = false;
                this.Error = null;
            }
            /// <summary>
            /// Indicates an error was encountered
            /// </summary>
            /// <param name="error"></param>
            public CacheDownloadStringCompletedEventArgs(Exception error)
            {
                this.Result = null;
                this.Cancelled = false;
                this.Error = error;
            }
            /// <summary>
            /// Indicates the operation was cancelled
            /// </summary>
            /// <returns></returns>
            public static CacheDownloadStringCompletedEventArgs MakeCancelled()
            {
                CacheDownloadStringCompletedEventArgs rval = new CacheDownloadStringCompletedEventArgs("");
                rval.Result = null;
                rval.Cancelled = true;
                return rval;
            }
            public string Result { get; private set; }
            public new bool Cancelled { get; private set; }
            public new Exception Error { get; private set; }
        }

        #endregion
    }
}
