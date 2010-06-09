using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OneBusAway.WP7.ViewModel.DataStructures
{
    public class DirectionSchedule
    {
        public string tripHeadsign { get; set; }
        public List<Trip> trips { get; set; }
    }
}
