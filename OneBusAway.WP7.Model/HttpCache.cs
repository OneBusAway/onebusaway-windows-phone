using System;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Collections.Generic;

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

        public HttpCache(string name, int expirationPeriod)
        {
            this.Name = name;
            this.ExpirationPeriod = expirationPeriod;
            CacheCalls = 0;
            CacheHits = 0;
            CacheMisses = 0;
            CacheExpirations = 0;
        }

        public void DownloadStringAsync(Uri address)
        {
            CacheCalls++;
            // lookup address in cache
            string cachedResult = CacheLookup(address);
            if (cachedResult != null)
            {
                CacheHits++;
                CacheDownloadStringCompletedEventArgs eventArgs = new CacheDownloadStringCompletedEventArgs(cachedResult);
                CacheDownloadStringCompleted(this, eventArgs);
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

        #region diagnostic properties
        // Mainly useful for statistics of cache performance.
        // You'd need to maintain a common instance for these to be useful.
        // That requires some more thought about how to register and clean up EventHandlers

        public int CacheCalls { get; private set; }
        public int CacheHits { get; private set; }
        public int CacheMisses { get; private set; }
        public int CacheExpirations { get; private set; }

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
                    // does file exist?
                    if (iso.FileExists(fileName))
                    {
                        // we have a copy, check expiration
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
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    // this cache does not exist, so we can't have any results
                    return null;
                }
            }

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

                // TODO cache eviction
                // "A cache without an eviction policy is a memory leak"
                // We might run into quota limits on IsolatedStorage.  We need to check those, and evict files to make room. 

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
        /// Maps a request URI to the file name where we will store it in the cache
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private string MapAddressToFile(Uri address)
        {
            string escaped = address.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped);
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
