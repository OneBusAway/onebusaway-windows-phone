using System;
using System.Net;
using System.Collections.Generic;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using System.Device.Location;

namespace OneBusAway.WP7.ViewModel.EventArgs
{
    public class RoutesForLocationEventArgs : AModelEventArgs
    {
        public List<Route> routes { get; private set; }
        public GeoCoordinate location { get; private set; }

        public RoutesForLocationEventArgs(List<Route> routes, GeoCoordinate location, Exception error)
            : base(error)
        {
            this.routes = routes;
            this.location = location;
        }
    }
}
