using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;

namespace OneBusAway.WP7.ViewModel
{
    public class BusDirectionVM : IViewModel
    {
        private IBusServiceModel busServiceModel;

        public ObservableCollection<RouteStops> RouteDirections { get; private set; }
        public Route CurrentRoute { get; private set; }

        public BusDirectionVM()
            : this((IBusServiceModel)Assembly.Load("OneBusAway.WP7.Model")
                .GetType("OneBusAway.WP7.Model.BusServiceModel")
                .GetField("Singleton")
                .GetValue(null))
        {

        }

        public BusDirectionVM(IBusServiceModel busServiceModel)
        {
            RouteDirections = new ObservableCollection<RouteStops>();
            this.busServiceModel = busServiceModel;
        }

        public void LoadRouteDirections(Route route)
        {
            RouteDirections.Clear();
            busServiceModel.StopsForRoute(route);
            CurrentRoute = route;
        }

        void busServiceModel_StopsForRoute_Completed(object sender, EventArgs.StopsForRouteEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                e.routeStops.ForEach(routeStop => RouteDirections.Add(routeStop));
            }
        }

        public void RegisterEventHandlers()
        {
            this.busServiceModel.StopsForRoute_Completed += new EventHandler<EventArgs.StopsForRouteEventArgs>(busServiceModel_StopsForRoute_Completed);
        }

        public void UnregisterEventHandlers()
        {
            this.busServiceModel.StopsForRoute_Completed -= new EventHandler<EventArgs.StopsForRouteEventArgs>(busServiceModel_StopsForRoute_Completed);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
