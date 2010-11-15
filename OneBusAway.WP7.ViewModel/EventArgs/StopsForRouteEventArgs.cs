using System;
using System.Net;
using System.Collections.Generic;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;

namespace OneBusAway.WP7.ViewModel.EventArgs
{
    public class StopsForRouteEventArgs : AModelEventArgs
    {
        public List<RouteStops> routeStops { get; private set; }
        public Route route { get; private set; }

        public StopsForRouteEventArgs(Route route, List<RouteStops> routeStops, Exception error)
            : base(error)
        {
            this.routeStops = routeStops;
            this.route = route;
        }
    }
}
