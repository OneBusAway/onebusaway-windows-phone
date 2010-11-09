using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OneBusAway.WP7.ViewModel.Data
{
    /// <summary>
    /// Interface representing a cache of data.
    /// </summary>
    /// <remarks>
    /// Note that this is more of a general purpose structure than the specialized HttpCache,
    /// which exists to be a content cache at the network layer.
    /// </remarks>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    public interface ICache<K , V> where K : IComparable
    {
        bool ContainsKey(K key);
        V Get(K obj);
        void Put(K key, V obj);
        void Put(K key, V obj, TimeSpan validTime);
        void Remove(K key);
        Pair<K,V>[] GetAll();
        int Count { get; }
    }
}