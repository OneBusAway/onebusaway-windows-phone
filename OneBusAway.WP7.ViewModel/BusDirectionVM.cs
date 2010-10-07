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
    public class BusDirectionVM : AViewModel
    {
        public ObservableCollection<RouteStops> RouteDirections { get; private set; }

        #region Constructors

        public BusDirectionVM()
            : base()
        {
            Initialize();
        }

        public BusDirectionVM(IBusServiceModel busServiceModel)
            : base(busServiceModel)
        {
            Initialize();
        }

        private void Initialize()
        {
            RouteDirections = new ObservableCollection<RouteStops>();
        }

        #endregion

        public void LoadRouteDirections(Route route)
        {
            RouteDirections.Clear();
            pendingOperations++;
            busServiceModel.StopsForRoute(route);
        }

        void busServiceModel_StopsForRoute_Completed(object sender, EventArgs.StopsForRouteEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                e.routeStops.ForEach(routeStop => RouteDirections.Add(routeStop));
            }

            pendingOperations--;
        }

        public override void RegisterEventHandlers()
        {
            this.busServiceModel.StopsForRoute_Completed += new EventHandler<EventArgs.StopsForRouteEventArgs>(busServiceModel_StopsForRoute_Completed);
        }

        public override void UnregisterEventHandlers()
        {
            this.busServiceModel.StopsForRoute_Completed -= new EventHandler<EventArgs.StopsForRouteEventArgs>(busServiceModel_StopsForRoute_Completed);

            pendingOperations = 0;
        }
    }
}
