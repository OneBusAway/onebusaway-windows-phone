using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OneBusAway.WP7.ViewModel.DataStructures
{
    public class ArrivalAndDeparture
    {
        public string routeId { get; set; }
        public string routeShortName { get; set; }
        public string tripId { get; set; }
        public string tripHeadsign { get; set; }
        public string stopId { get; set; }
        public DateTime? predictedArrivalTime { get; set; }
        public DateTime scheduledArrivalTime { get; set; }
        public DateTime? predictedDepartureTime { get; set; }
        public DateTime scheduledDepartureTime { get; set; }
        public string status { get; set; }
    }
}
