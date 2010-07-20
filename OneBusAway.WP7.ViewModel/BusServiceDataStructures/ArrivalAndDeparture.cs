using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace OneBusAway.WP7.ViewModel.BusServiceDataStructures
{
    [DataContract()]
    public class ArrivalAndDeparture
    {
        [DataMember()]
        public string routeId { get; set; }
        [DataMember()]
        public string routeShortName { get; set; }
        [DataMember()]
        public string tripId { get; set; }
        [DataMember()]
        public string tripHeadsign { get; set; }
        [DataMember()]
        public string stopId { get; set; }
        [DataMember()]
        public DateTime? predictedArrivalTime { get; set; }
        [DataMember()]
        public DateTime scheduledArrivalTime { get; set; }
        [DataMember()]
        public DateTime? predictedDepartureTime { get; set; }
        [DataMember()]
        public DateTime scheduledDepartureTime { get; set; }
        [DataMember()]
        public string status { get; set; }

        public DateTime nextKnownArrival
        {
            get
            {
                return predictedArrivalTime != null ? (DateTime)predictedArrivalTime : scheduledArrivalTime;
            }
        }

        public override string ToString()
        {
            return string.Format(
                "Arrival: Route='{0}', Destination='{1}', NextArrival='{2}'",
                routeShortName,
                tripHeadsign,
                nextKnownArrival.ToString("HH:mm")
                );
        }
    }

    public class ArrivalTimeComparer : IComparer<ArrivalAndDeparture>
    {
        public int Compare(ArrivalAndDeparture x, ArrivalAndDeparture y)
        {
            return x.nextKnownArrival.CompareTo(y.nextKnownArrival);
        }
    }

    public class RouteArrivalComparer : IComparer<Route>
    {
        public int Compare(Route x, Route y)
        {
            if (y.nextArrival == null && x.nextArrival == null)
            {
                return 0;
            }

            if (x.nextArrival == null)
            {
                return 1; // is 1 correct?
            }

            if (y.nextArrival == null)
            {
                return -1; // is -1 correct?
            }

            return ((DateTime)x.nextArrival).CompareTo((DateTime)y.nextArrival);
        }
    }
}
