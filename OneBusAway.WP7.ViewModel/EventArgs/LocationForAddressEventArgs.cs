using System;
using System.Net;
using System.Collections.Generic;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using System.Device.Location;

namespace OneBusAway.WP7.ViewModel.EventArgs
{
    public class LocationForAddressEventArgs : AModelEventArgs
    {
        public List<LocationForQuery> locations { get; private set; }
        public string query { get; private set; }
        public GeoCoordinate searchNearLocation { get; private set; }

        public LocationForAddressEventArgs(List<LocationForQuery> locations, string query, GeoCoordinate searchNearLocation, Exception error)
        {
            this.error = error;
            this.query = query;
            this.locations = locations;
            this.searchNearLocation = searchNearLocation;
        }
    }
}

