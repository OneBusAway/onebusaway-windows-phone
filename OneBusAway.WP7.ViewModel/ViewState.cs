using System;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;

namespace OneBusAway.WP7.ViewModel
{
    public class ViewState
    {
        public static readonly ViewState Instance = new ViewState();

        private ViewState() 
        { 
        
        }

        public Stop CurrentStop { get; set; }

        public Route CurrentRoute { get; set; }

        public RouteStops CurrentRouteDirection { get; set; }

    }
}
