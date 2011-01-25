using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Threading;
//using System.Xml.Serialization;
using OneBusAway.WP7.ViewModel;
using System.Runtime.Serialization;

namespace OneBusAway.WP7.Model
{
    /// <summary>
    /// A cache for HTTP GET requests, backed by IsolatedStorage.
    /// The full version of .NET includes support for this already, but Silverlight does not.
    /// </summary>
    public class HttpCache
    {
        /// <summary>
        /// Acquire a lock on this object prior to file IO.
        /// </summary>
        private object fileAccessSync = new object();

        private CacheMetadata metadata;
        private Timer reportingTimer;

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
            this.metadata = new CacheMetadata(this);
            CacheCalls = 0;
            CacheHits = 0;
            CacheMisses = 0;
            CacheExpirations = 0;
            CacheEvictions = 0;

            reportingTimer = new Timer(new TimerCallback(CacheReportTrigger), null, new TimeSpan(0, 1, 0), new TimeSpan(0, 1, 0));
        }

        #region public methods

        public delegate void DownloadStringAsync_Completed(object sender, CacheDownloadStringCompletedEventArgs e);
        public void DownloadStringAsync(Uri address, DownloadStringAsync_Completed callback)
        {
            CacheCalls++;
            // lookup address in cache
            Stream cachedResult = CacheLookup(address);
            if (cachedResult != null)
            {
                CacheHits++;
                CacheDownloadStringCompletedEventArgs eventArgs = new CacheDownloadStringCompletedEventArgs(new StreamReader(cachedResult));
                // Invoke on a different thread.  Otherwise we make the callback from the same thread as the
                // original call and wierd things could happen.
                Thread thread = new Thread(() => callback(this, eventArgs));
                thread.Start();
            }
            else
            {
                CacheMisses++;
                // not found, request data
                HttpWebRequest requestGetter = (HttpWebRequest)HttpWebRequest.Create(address);
                requestGetter.BeginGetResponse(
                    new AsyncCallback(new CacheCallback(this, callback, address).Callback),
                    requestGetter);
            }
        }

        internal void Save()
        {
            metadata.WriteSettingsFile();
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
                    lock (fileAccessSync)
                    {
                        // IsolatedStorage requires you to delete all the files before removing the directory
                        string[] files = iso.GetFileNames(this.Name + "\\*");
                        foreach (string file in files)
                        {
                            iso.DeleteFile(Path.Combine(this.Name, file));
                        }
                        iso.DeleteDirectory(this.Name);
                    }
                }
            }
            metadata.Clear();
        }

        /// <summary>
        /// Ensures that there is no entry for the given address in the cache.
        /// </summary>
        public void Invalidate(Uri address)
        {
            string fileName = MapAddressToFile(address);
            using (IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication())
            {
                lock (fileAccessSync)
                {
                    try
                    {
                        iso.DeleteFile(fileName);
                    }
                    catch (IsolatedStorageException)
                    {
                        // ignore
                    }
                }
            }
            metadata.RemoveUpdateTime(fileName);
        }

        #endregion

        #region diagnostic properties
        // Mainly useful for statistics of cache performance.

        public int CacheCalls { get; private set; }
        public int CacheHits { get; private set; }
        public int CacheMisses { get; private set; }
        public int CacheExpirations { get; private set; }
        public int CacheEvictions { get; private set; }

        private IDictionary<string, string> ReportCacheStats()
        {
            IDictionary<string, string> cacheStats = new Dictionary<string, string>();
            cacheStats.Add(string.Format("{0}-calls", Name), CacheCalls.ToString());
            cacheStats.Add(string.Format("{0}-hits", Name), CacheHits.ToString());
            cacheStats.Add(string.Format("{0}-misses", Name), CacheMisses.ToString());
            cacheStats.Add(string.Format("{0}-expirations", Name), CacheExpirations.ToString());
            cacheStats.Add(string.Format("{0}-evictions", Name), CacheEvictions.ToString());
            cacheStats.Add(string.Format("{0}-numberEntires", Name), metadata.GetNumberEntries().ToString());

            return cacheStats;
        }

        // This method will be called by the timer, and have an attribute attached
        // which will cause the analytics to call ReportCacheStats() and gather
        // the analytics
        private void CacheReportTrigger(object param)
        {
            
        }

        #endregion

        #region private helper methods

        /// <summary>
        /// Checks to see if we have a cached result for a given request
        /// </summary>
        /// <param name="address"></param>
        /// <returns>A stream with the results.  Close it when you're done!</returns>
        private Stream CacheLookup(Uri address)
        {
            using (IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication())
            {
                // get result file for this address
                string fileName = MapAddressToFile(address);
                lock (fileAccessSync)
                {
                    if (CheckForExpiration(fileName))
                    {
                        return null;
                    }
                    try
                    {
                        // all good! return the content
                        IsolatedStorageFileStream stream = iso.OpenFile(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                        return stream;
                    }
                    catch (IsolatedStorageException)
                    {
                        // hrm.  something went wrong.
                        // just treat it as a cache miss.
                        // we'll reload the file later if needed
                        return null;
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
        private TextReader CacheAddResult(Uri address, Stream result)
        {
            // TODO there doesn't seem to be a .NET equivalent of java.io.PipedOutputStream, which is what we really want here.
            // I.e. pipe from result directly into a file stream, rather than buffering in a string.
            string data;
            using (StreamReader reader = new StreamReader(result))
            {
                data = reader.ReadToEnd();
            }
            result.Close();

            using (IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication())
            {
                lock (fileAccessSync)
                {
                    // get isolatedstorage for this cache
                    if (!iso.DirectoryExists(this.Name))
                    {
                        iso.CreateDirectory(this.Name);
                    }

                    string fileName = MapAddressToFile(address);
                    UpdateExpiration(fileName);
                    EvictIfNecessary(iso);

                    Stream stream;
                    // If file already exists don't rewrite it.  The other request which caused the
                    // file to be cached might still be using the thread and will crash if we 
                    // try to delete the file
                    if (iso.FileExists(fileName) == false)
                    {
                        stream = iso.OpenFile(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                        StreamWriter writer = new StreamWriter(stream);
                        writer.Write(data);
                        writer.Flush();
                        stream.Seek(0, SeekOrigin.Begin);
                    }
                    else
                    {
                        stream = iso.OpenFile(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                    }

                    return new StreamReader(stream);
                }
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
        /// 
        /// This function assumes the caller has already locked fileAccessSync
        /// </remarks>
        /// <param name="iso"></param>
        private void EvictIfNecessary(IsolatedStorageFile iso)
        {
            if (metadata.GetNumberEntries() >= this.Capacity)
            {
                string[] filesInCache = iso.GetFileNames(this.Name + "\\*");

                // This should never happen, but if the file count doesn't match
                // go and clean up the cache
                if (filesInCache.Length > metadata.GetNumberEntries())
                {
                    Debug.Assert(false);

                    foreach (string filename in filesInCache)
                    {
                        // the GetFileNames call above does not return qualified paths, but we expect those for the rest of the calls
                        string qualifiedFilename = Path.Combine(this.Name, filename);
                        DateTime? updateTime = metadata.GetUpdateTime(qualifiedFilename);
                        if (null == updateTime)
                        {
                            // Then we have a file in the cache, but no record of it being put there... clean it up
                            // Most common way to hit this would be that I changed the internal naming format between versions.
                            iso.DeleteFile(qualifiedFilename);
                        }
                    }
                }

                KeyValuePair<string, DateTime> oldestFile = metadata.GetOldestFile();

                // If we are over capacity, there should always be at least one over-capacity file
                Debug.Assert(string.IsNullOrEmpty(oldestFile.Key) == false);

                if (string.IsNullOrEmpty(oldestFile.Key) == false)
                {
                    iso.DeleteFile(oldestFile.Key);

                    metadata.RemoveUpdateTime(oldestFile.Key);
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
        /// <remarks>
        /// Assumes that the caller has already locked fileAccessSync
        /// </remarks>
        /// <param name="fileName"></param>
        /// <returns>True if the cached file is expired</returns>
        private bool CheckForExpiration(string fileName)
        {
            if (metadata.IsExpired(fileName))
            {
                // purge the entry from the cache
                using (IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    try
                    {
                        iso.DeleteFile(fileName);
                    }
                    catch (IsolatedStorageException)
                    {
                        // ignore
                    }
                }

                // and purge the metadata entry
                metadata.RemoveUpdateTime(fileName);

                CacheExpirations++;
                return true;
            }
            return false;
        }

        private void UpdateExpiration(string fileName)
        {
            metadata.AddUpdateTime(fileName, DateTime.Now);
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
                    // upgrade scenario.  user has an existing cache.  
                    // migrate it out of AppSettings to file
                    fileUpdateTimes = cacheSettings[owner.Name] as Dictionary<string, DateTime>;

                    WriteSettingsFile();

                    // and clean up AppSettings
                    cacheSettings.Remove(owner.Name);
                    cacheSettings.Save();
                }
                else
                {
                    fileUpdateTimes = ReadSettingsFile();
                }
            }

            private Dictionary<string, DateTime> ReadSettingsFile()
            {
                IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication();
                if (!iso.DirectoryExists("CacheSettings"))
                {
                    return new Dictionary<string, DateTime>();
                }
                string settingsFile = "CacheSettings\\" + owner.Name + ".xml";
                if (!iso.FileExists(settingsFile))
                {
                    return new Dictionary<string, DateTime>();
                }
                DataContractSerializer d = new DataContractSerializer(typeof(Dictionary<string, DateTime>));
                
                lock (owner.fileAccessSync)
                {
                    using (IsolatedStorageFileStream stream = iso.OpenFile(settingsFile, FileMode.Open, FileAccess.Read))
                    {
                        return (Dictionary<string, DateTime>)d.ReadObject(stream);
                    }
                }
            }

            internal void WriteSettingsFile()
            {
                DataContractSerializer d = new DataContractSerializer(typeof(Dictionary<string, DateTime>));
                IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication();
                lock (owner.fileAccessSync)
                {
                    if (!iso.DirectoryExists("CacheSettings"))
                    {
                        iso.CreateDirectory("CacheSettings");
                    }
                    string settingsFile = "CacheSettings\\" + owner.Name + ".xml";
                    using (IsolatedStorageFileStream stream = iso.OpenFile(settingsFile, FileMode.Create, FileAccess.Write))
                    {
                        d.WriteObject(stream, fileUpdateTimes);
                    }
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
                // note: writing out to file is deferred to app  exit
            }

            public void RemoveUpdateTime(string filename)
            {
                fileUpdateTimes.Remove(filename);
                // note: writing out to file is deferred to app  exit
            }

            public int GetNumberEntries()
            {
                return fileUpdateTimes.Count;
            }

            public KeyValuePair<string, DateTime> GetOldestFile()
            {
                KeyValuePair<string, DateTime> oldestFile = new KeyValuePair<string, DateTime>(string.Empty, DateTime.MaxValue);

                foreach (KeyValuePair<string, DateTime> fileUpdateTime in fileUpdateTimes)
                {
                    if (fileUpdateTime.Value < oldestFile.Value)
                    {
                        oldestFile = fileUpdateTime;
                    }
                }

                return oldestFile;
            }

            public void Clear()
            {
                fileUpdateTimes.Clear();
                IsolatedStorageSettings cacheSettings = IsolatedStorageSettings.ApplicationSettings;
                cacheSettings.Remove(owner.Name);
                IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication();
                if(iso.DirectoryExists("CacheSettings"))
                {
                    string settingsFile = "CacheSettings\\" + owner.Name + ".xml";
                    if(iso.FileExists(settingsFile))
                    {
                        lock(owner.fileAccessSync)
                        {
                            iso.DeleteFile(settingsFile);
                        }
                    }
                }
            }
            
        }

        #endregion

        #region Callback support (event, event handler, etc)

        /// <summary>
        /// Exists solely to hold a reference to the originally requested URI
        /// </summary>
        private class CacheCallback
        {
            private Uri requestedAddress;
            private HttpCache owner;
            private DownloadStringAsync_Completed callback;

            public CacheCallback(HttpCache owner, DownloadStringAsync_Completed callback, Uri requestedAddress)
            {
                this.owner = owner;
                this.callback = callback;
                this.requestedAddress = requestedAddress;
            }

            public void Callback(IAsyncResult asyncResult)
            {
                CacheDownloadStringCompletedEventArgs newArgs;

                try
                {
                    HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;
                    HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asyncResult);

                    string statusDescr = response.StatusDescription;
                    long totalBytes = response.ContentLength;

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new WebserviceResponseException(response.StatusCode, request.RequestUri.ToString(), response.ToString(), null);
                    }

                    Stream s = response.GetResponseStream();
                    // no errors -- add data to the cache
                    TextReader result = owner.CacheAddResult(requestedAddress, s);

                    newArgs = new CacheDownloadStringCompletedEventArgs(result);
                }
                catch (Exception e)
                {
                    // TODO: Web exceptions will be caught here, and we just pass up
                    // that exception instead of recasting it to a WebserviceResponseException().
                    // This will result in the loss of the RequestUrl.
                    Debug.Assert(false);
                    newArgs = new CacheDownloadStringCompletedEventArgs(e);
                }

                callback(this, newArgs);
            }
        }


        // Yes, these mirror the ones defined in System.Net.
        // Those don't have public constructors, so they're not reusable.
        public class CacheDownloadStringCompletedEventArgs : AsyncCompletedEventArgs 
        {
            private CacheDownloadStringCompletedEventArgs() { }

            /// <summary>
            /// Indicates successful completion
            /// </summary>
            /// <param name="result"></param>
            public CacheDownloadStringCompletedEventArgs(TextReader result) 
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
                CacheDownloadStringCompletedEventArgs rval = new CacheDownloadStringCompletedEventArgs();
                rval.Result = null;
                rval.Cancelled = true;
                return rval;
            }
            public TextReader Result { get; private set; }
            public new bool Cancelled { get; private set; }
            public new Exception Error { get; private set; }
        }

        #endregion
    }
}
