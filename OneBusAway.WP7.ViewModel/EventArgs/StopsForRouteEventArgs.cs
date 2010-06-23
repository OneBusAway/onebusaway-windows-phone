using System;
using System.Net;
using System.Collections.Generic;
using OneBusAway.WP7.ViewModel.DataStructures;

namespace OneBusAway.WP7.ViewModel.EventArgs
{
    public class StopsForRouteEventArgs : ABusServiceEventArgs
    {
        public List<RouteStops> routeStops { get; private set; }
        public Route route { get; private set; }

        public StopsForRouteEventArgs(Route route, List<RouteStops> routeStops, Exception error)
        {
            this.routeStops = routeStops;
            this.error = error;
            this.route = route;
        }
    }
}
