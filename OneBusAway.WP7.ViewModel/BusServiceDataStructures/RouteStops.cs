/* Copyright 2013 Shawn Henry, Rob Smith, and Michael Friedman
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
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

        public override bool Equals(object obj)
        {
            if (obj is RouteStops == false)
            {
                return false;
            }

            return ((RouteStops)obj).name == name;
        }

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
