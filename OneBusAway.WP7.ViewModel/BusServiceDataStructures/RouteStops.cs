using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Device.Location;

namespace OneBusAway.WP7.ViewModel.BusServiceDataStructures
{
    [DataContract()]
    public class RouteStops
    {
        [DataMember()]
        public Route route { get; set; }
        [DataMember()]
        public string name { get; set; }
        [DataMember()]
        public List<Stop> stops { get; set; }
        [DataMember()]
        public List<PolyLine> encodedPolylines { get; set; }

        public override string ToString()
        {
            return string.Format("RouteStops: name='{0}'", name);
        }
    }

    public class RouteStopsDistanceComparer : IComparer<RouteStops>
    {
        private GeoCoordinate center;

        public RouteStopsDistanceComparer(GeoCoordinate center)
        {
            this.center = center;
        }

        public int Compare(RouteStops x, RouteStops y)
        {
            if (x.route == null && y.route == null)
            {
                return 0;
            }

            if (x.route == null)
            {
                return -1;
            }

            if (y.route == null)
            {
                return 1;
            }

            if (x.route.closestStop == null && y.route.closestStop == null)
            {
                return 0;
            }

            if (x.route.closestStop == null)
            {
                return -1;
            }

            if (y.route.closestStop == null)
            {
                return 1;
            }

            int result = x.route.closestStop.location.GetDistanceTo(center).CompareTo(y.route.closestStop.location.GetDistanceTo(center));

            // If the bus routes have the same closest stop sort by route number
            if (result == 0)
            {
                result = x.route.shortName.CompareTo(y.route.shortName);
            }

            // If the bus routes have the same stop and number (this will happen for the two different
            // directions of the same bus route) then sort alphabetically by description
            if (result == 0)
            {
                result = x.name.CompareTo(y.name);
            }

            return result;
        }
    }
}
