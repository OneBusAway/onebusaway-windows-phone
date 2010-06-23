using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OneBusAway.WP7.ViewModel.DataStructures;
using System.Device.Location;

namespace OneBusAway.WP7.ViewModel.EventArgs
{
    public class StopsForLocationEventArgs : ABusServiceEventArgs
    {
        public List<Stop> stops { get; private set; }
        public GeoCoordinate location { get; private set; }

        public StopsForLocationEventArgs(List<Stop> stops, GeoCoordinate searchLocation, Exception error)
        {
            this.error = error;
            this.stops = stops;
            this.location = searchLocation;
        }
    }
}
