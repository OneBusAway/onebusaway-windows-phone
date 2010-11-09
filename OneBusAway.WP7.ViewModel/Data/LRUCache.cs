using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace OneBusAway.WP7.ViewModel.Data
{
    /// <summary>
    /// A threadsafe, in-memory, LRU cache.
    /// </summary>
    /// <typeparam name="K"></typeparam>
    /// <typeparam name="V"></typeparam>
    public class LRUCache<K, V> : ICache<K, V> where K : IComparable
    {
        IDictionary<K, Item> m_map = new Dictionary<K, Item>();
        Item m_start = new Item();
        Item m_end = new Item();
        int m_maxSize;
        object m_listLock = new object();
        object m_mapLock = new object();
        private class Item
        {
            public Item(K k, V v, DateTime e)
            {
                key = k;
                value = v;
                expires = e;
            }
            public Item() { }
            public K key;
            public V value;
            public DateTime expires;
            public Item previous;
            public Item next;
        }
        void removeItem(Item item)
        {
            lock (m_listLock)
            {
                item.previous.next = item.next;
                item.next.previous = item.previous;
            }
        }
        void insertHead(Item item)
        {
            lock (m_listLock)
            {
                item.previous = m_start;
                item.next = m_start.next;
                m_start.next.previous = item;
                m_start.next = item;
            }
        }
        void moveToHead(Item item)
        {
            lock (m_listLock)
            {
                item.previous.next = item.next;
                item.next.previous = item.previous;
                item.previous = m_start;
                item.next = m_start.next;
                m_start.next.previous = item;
                m_start.next = item;
            }
        }
        public LRUCache(int maxObjects)
        {
            m_maxSize = maxObjects;
            m_start.next = m_end;
            m_end.previous = m_start;
        }

        public Pair<K, V>[] GetAll()
        {
            Pair<K, V>[] p = new Pair<K, V>[m_maxSize];
            int count = 0;
            lock (m_listLock)
            {
                Item cur = m_start.next;
                while (cur != m_end)
                {
                    p[count] = new Pair<K, V>(cur.key, cur.value);
                    ++count;
                    cur = cur.next;
                }
            }
            Pair<K, V>[] np = new Pair<K, V>[count];
            Array.Copy(p, 0, np, 0, count);
            return np;
        }

        public bool ContainsKey(K key)
        {
            Item cur = null;
            lock (m_mapLock)
            {
                if (!m_map.ContainsKey(key))
                {
                    return false;
                }
                cur = m_map[key];
                if (DateTime.Now > cur.expires)
                {
                    m_map.Remove(cur.key);
                    removeItem(cur);
                    return false;
                }
            }
            if (cur != m_start.next)
            {
                moveToHead(cur);
            }
            return true;
        }


        public V Get(K key)
        {
            Item cur = null;
            lock (m_mapLock)
            {
                if (!m_map.ContainsKey(key))
                {
                    throw new KeyNotFoundException("could not find key " + key);
                }
                cur = m_map[key];
                if (DateTime.Now > cur.expires)
                {
                    m_map.Remove(cur.key);
                    removeItem(cur);
                    throw new KeyNotFoundException("could not find key " + key);
                }
            }
            if (cur != m_start.next)
            {
                moveToHead(cur);
            }
            return (V)cur.value;
        }

        public void Put(K key, V obj)
        {
            Put(key, obj, TimeSpan.Zero);
        }

        public void Put(K key, V value, TimeSpan validTime)
        {
            Item cur = null;
            lock (m_mapLock)
            {
                if (m_map.ContainsKey(key))
                {
                    cur = m_map[key];
                    cur.value = value;
                    if (validTime > TimeSpan.Zero)
                    {
                        cur.expires = DateTime.Now.Add(validTime);
                    }
                    else
                    {
                        cur.expires = DateTime.MaxValue;
                    }
                    moveToHead(cur);
                    return;
                }
                if (m_map.Count >= m_maxSize)
                {
                    cur = m_end.previous;
                    m_map.Remove(cur.key);
                    removeItem(cur);
                }
                DateTime expires;
                if (validTime > TimeSpan.Zero)
                {
                    expires = DateTime.Now.Add(validTime);
                }
                else
                {
                    expires = DateTime.MaxValue;
                }
                Item item = new Item(key, value, expires);
                insertHead(item);
                m_map.Add(key, item);
            }
        }
        public void Remove(K key)
        {
            lock (m_mapLock)
            {
                if (m_map.ContainsKey(key))
                {
                    Item cur = m_map[key];
                    m_map.Remove(key);
                    removeItem(cur);
                }
            }
        }
        public int Count
        {
            get
            {
                lock (m_mapLock)
                {
                    return m_map.Count;
                }
            }
        }
    }
}
