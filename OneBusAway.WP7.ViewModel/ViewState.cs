using System;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;

namespace OneBusAway.WP7.ViewModel
{
    public class ViewState
    {
        private ViewState() { }
        public static readonly ViewState Instance = new ViewState();
        public Stop CurrentStop;
        public Route CurrentRoute;
        public RouteStops CurrentRouteDirection;
        public DateTime CurrentTime;
    }
}
