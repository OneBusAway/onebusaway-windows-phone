using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace OneBusAway.WP7.ViewModel.BusServiceDataStructures
{
    [DataContract()]
    public class DirectionSchedule
    {
        [DataMember()]
        public string tripHeadsign { get; set; }
        [DataMember()]
        public List<ScheduleStopTime> trips { get; set; }
    }
}
