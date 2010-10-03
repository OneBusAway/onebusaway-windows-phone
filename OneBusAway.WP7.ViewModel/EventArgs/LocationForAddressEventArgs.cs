using System;
using System.Net;
using System.Collections.Generic;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using System.Device.Location;

namespace OneBusAway.WP7.ViewModel.EventArgs
{
    public class LocationForAddressEventArgs : AModelEventArgs
    {
        public GeoCoordinate location { get; private set; }
        public string query { get; private set; }

        public LocationForAddressEventArgs(GeoCoordinate location, string query, Exception error)
        {
            this.error = error;
            this.query = query;
            this.location = location;
        }
    }
}

