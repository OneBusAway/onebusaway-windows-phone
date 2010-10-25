using System;
using System.Collections.Generic;
using System.Device.Location;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;

namespace OneBusAway.WP7.ViewModel.EventArgs
{
    public class CombinedInfoForLocationEventArgs : AModelEventArgs
    {
        public List<Stop> stops { get; private set; }
        public List<Route> routes { get; private set; }
        public GeoCoordinate location { get; private set; }

        public CombinedInfoForLocationEventArgs(List<Stop> stops, List<Route> routes, GeoCoordinate location, Exception error)
        {
            this.error = error;
            this.stops = stops;
            this.routes = routes;
            this.location = location;
        }
    }
}
