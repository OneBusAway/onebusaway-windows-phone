using System;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Threading;

namespace OneBusAway.WP7.ViewModel
{
    public class ViewState : INotifyPropertyChanged
    {
        public static readonly ViewState Instance = new ViewState();

        private ViewState() 
        {
            // Set up the default action, just execute in the same thread
            UIAction = (uiAction => uiAction());
        }

        public Action<Action> UIAction { get; set; }

        private Stop currentStop;
        public Stop CurrentStop 
        {
            get { return currentStop; }

            set
            {
                currentStop = value;
                OnPropertyChanged("CurrentStop");
            }
        }

        private Route currentRoute;
        public Route CurrentRoute
        {
            get { return currentRoute; }

            set
            {
                currentRoute = value;
                OnPropertyChanged("CurrentRoute");
            }
        }

        private List<Route> currentRoutes;
        public List<Route> CurrentRoutes
        {
            get { return currentRoutes; }

            set
            {
                currentRoutes = value;
                OnPropertyChanged("CurrentRoutes");
            }
        }

        RouteStops currentRouteDirection;
        public RouteStops CurrentRouteDirection
        {
            get { return currentRouteDirection; }

            set
            {
                currentRouteDirection = value;
                OnPropertyChanged("CurrentRouteDirection");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            UIAction(() => 
                {
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                });
        }
    }
}
