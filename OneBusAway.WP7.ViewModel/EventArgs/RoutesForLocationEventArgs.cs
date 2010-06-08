using System;
using System.Net;
using System.Collections.Generic;
using OneBusAway.WP7.ViewModel.DataStructures;

namespace OneBusAway.WP7.ViewModel.EventArgs
{
    public class RoutesForLocationEventArgs : System.EventArgs
    {
        public Exception error { get; private set; }
        public List<Route> routes { get; private set; }

        public RoutesForLocationEventArgs(List<Route> routes, Exception error)
        {
            this.error = error;
            this.routes = routes;
        }
    }
}
