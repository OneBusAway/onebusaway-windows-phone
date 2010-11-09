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
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;

namespace OneBusAway.WP7.ViewModel.Data
{
    public class CacheFactory
    {
        private CacheFactory() 
        {
            // TODO make the cache size configurable
            StopsCache = new LRUCache<string, Stop>(200);
            RoutesCache = new LRUCache<string, Route>(90);
        }
        public static readonly CacheFactory Singleton = new CacheFactory();

        public ICache<string, Stop> StopsCache { get; private set;}
        public ICache<string, Route> RoutesCache { get; private set;}

    }
}
