using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OneBusAway.WP7.ViewModel.DataStructures
{
    public class Trip
    {
        public DateTime scheduledArrivalTime { get; set; }
        public DateTime scheduledDepartureTime { get; set; }
        public string serviceId { get; set; }
        public string tripId { get; set; }
    }
}
