using System;
using System.Net;
using System.Collections.Generic;
using OneBusAway.WP7.ViewModel.DataStructures;
using System.Device.Location;

namespace OneBusAway.WP7.ViewModel.EventArgs
{
    public class RoutesForLocationEventArgs : ABusServiceEventArgs
    {
        public List<Route> routes { get; private set; }
        public GeoCoordinate location { get; private set; }

        public RoutesForLocationEventArgs(GeoCoordinate location, List<Route> routes, Exception error)
        {
            this.error = error;
            this.routes = routes;
            this.location = location;
        }
    }
}
