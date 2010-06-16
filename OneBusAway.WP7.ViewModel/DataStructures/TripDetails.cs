using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Device.Location;

namespace OneBusAway.WP7.ViewModel.DataStructures
{
    public class TripDetails
    {
        public string tripId { get; set; }
        public DateTime serviceDate { get; set; }
        public GeoCoordinate position { get; set; }
        public int? scheduleDeviationInSec { get; set; }
        public string closestStopId { get; set; }
        public int? closestStopTimeOffset { get; set; }
    }
}
