using System;
using System.Net;
using System.Collections.Generic;
using OneBusAway.WP7.ViewModel.DataStructures;

namespace OneBusAway.WP7.ViewModel.EventArgs
{
    public class ScheduleForStopEventArgs : System.EventArgs
    {
        public Exception error { get; private set; }
        public List<RouteSchedule> schedules { get; private set; }

        public ScheduleForStopEventArgs(List<RouteSchedule> schedules, Exception error)
        {
            this.error = error;
            this.schedules = schedules;
        }
    }
}
