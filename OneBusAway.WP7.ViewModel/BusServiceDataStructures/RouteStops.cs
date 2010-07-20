using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace OneBusAway.WP7.ViewModel.BusServiceDataStructures
{
    [DataContract()]
    public class RouteStops
    {
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
}
