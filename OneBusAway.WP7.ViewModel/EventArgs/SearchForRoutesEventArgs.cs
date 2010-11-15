using System;
using System.Net;
using System.Collections.Generic;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using System.Device.Location;

namespace OneBusAway.WP7.ViewModel.EventArgs
{
    public class SearchForRoutesEventArgs : AModelEventArgs
    {
        public List<Route> routes { get; private set; }
        public GeoCoordinate location { get; private set; }
        public string query { get; private set; }

        public SearchForRoutesEventArgs(List<Route> routes, GeoCoordinate location, string query, Exception error)
            : base(error)
        {
            this.routes = routes;
            this.query = query;
            this.location = location;
        }
    }
}
