using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OneBusAway.WP7.ViewModel.DataStructures
{
    public class RouteSchedule
    {
        public Route route { get; set; }
        public List<DirectionSchedule> directions { get; set; }
    }
}
