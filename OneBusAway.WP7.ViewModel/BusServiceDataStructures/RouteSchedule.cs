using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace OneBusAway.WP7.ViewModel.BusServiceDataStructures
{
    [DataContract()]
    public class RouteSchedule
    {
        [DataMember()]
        public Route route { get; set; }
        [DataMember()]
        public List<DirectionSchedule> directions { get; set; }

        public override string ToString()
        {
            string s = string.Format("RouteSchedule: route='{0}'", route);
            foreach (DirectionSchedule direction in directions)
            {
                s += string.Format(", direction='{0}'", direction);
            }

            return s;
        }
    }
}
