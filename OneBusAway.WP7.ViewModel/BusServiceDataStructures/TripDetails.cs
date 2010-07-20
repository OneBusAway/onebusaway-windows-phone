using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Device.Location;
using System.Runtime.Serialization;

namespace OneBusAway.WP7.ViewModel.BusServiceDataStructures
{
    [DataContract()]
    public class TripDetails
    {
        [DataMember()]
        public string tripId { get; set; }
        [DataMember()]
        public DateTime serviceDate { get; set; }
        [DataMember()]
        public GeoCoordinate position { get; set; }
        [DataMember()]
        public int? scheduleDeviationInSec { get; set; }
        [DataMember()]
        public string closestStopId { get; set; }
        [DataMember()]
        public int? closestStopTimeOffset { get; set; }
    }
}
