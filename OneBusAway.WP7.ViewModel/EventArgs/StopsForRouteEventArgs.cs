using System;
using System.Net;
using System.Collections.Generic;
using OneBusAway.WP7.ViewModel.DataStructures;

namespace OneBusAway.WP7.ViewModel.EventArgs
{
    public class StopsForRouteEventArgs : System.EventArgs
    {
        public Exception error { get; private set; }
        public List<RouteStops> routeStops { get; private set; }

        public StopsForRouteEventArgs(List<RouteStops> routeStops, Exception error)
        {
            this.routeStops = routeStops;
            this.error = error;
        }
    }
}
