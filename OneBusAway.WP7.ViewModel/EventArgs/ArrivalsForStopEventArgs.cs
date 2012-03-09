using System;
using System.Net;
using System.Collections.Generic;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;

namespace OneBusAway.WP7.ViewModel.EventArgs
{
    public class ArrivalsForStopEventArgs : AModelEventArgs
    {
        public List<ArrivalAndDeparture> arrivals;
        public Stop stop { get; private set; }

        public ArrivalsForStopEventArgs(Stop stop, List<ArrivalAndDeparture> arrivals)
            : base()
        {
            this.arrivals = arrivals;
            this.stop = stop;
        }
    }
}
