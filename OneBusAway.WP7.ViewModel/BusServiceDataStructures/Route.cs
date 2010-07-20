using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Collections;
using System.Device.Location;

namespace OneBusAway.WP7.ViewModel.BusServiceDataStructures
{
    [DataContract()]
    public class Route
    {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string shortName { get; set; }
        [DataMember]
        public string description { get; set; }
        [DataMember]
        public string url { get; set; }
        [DataMember]
        public Agency agency { get; set; }
        [DataMember]
        public Stop closestStop { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is Route)
            {
                Route otherRoute = (Route)obj;
                if (otherRoute.id == this.id)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            return string.Format("Route: ID='{0}', description='{1}'", shortName, description);
        }
    }

    public class RouteDistanceComparer : IComparer<Route>
    {
        private GeoCoordinate center;

        public RouteDistanceComparer(GeoCoordinate center)
        {
            this.center = center;
        }

        public int Compare(Route x, Route y)
        {
            int result = x.closestStop.location.GetDistanceTo(center).CompareTo(y.closestStop.location.GetDistanceTo(center));

            // If the bus routes have the same closest stop sort by route number
            if (result == 0)
            {
                result = x.shortName.CompareTo(y.shortName);
            }

            return result;
        }
    }
}
