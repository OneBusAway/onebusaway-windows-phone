using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using System.Device.Location;

namespace OneBusAway.WP7.ViewModel.EventArgs
{
    public class StopsForLocationEventArgs : AModelEventArgs
    {
        public List<Stop> stops { get; private set; }
        public GeoCoordinate location { get; private set; }
        public bool limitExceeded { get; private set; }

        public StopsForLocationEventArgs(List<Stop> stops, GeoCoordinate searchLocation, bool limitExceeded, Exception error)
            : base(error)
        {
            this.stops = stops;
            this.location = searchLocation;
            this.limitExceeded = limitExceeded;
        }
    }
}
