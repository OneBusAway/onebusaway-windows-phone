using System;
using System.Net;
using System.Collections.ObjectModel;
using System.Reflection;
using OneBusAway.WP7.ViewModel.DataStructures;
using System.Device.Location;
using System.Collections.Generic;

namespace OneBusAway.WP7.ViewModel
{
    public class RouteDetailsVM
    {

        #region Private Variables

        private IBusServiceModel busServiceModel;

        #endregion

        #region Constructors

        public RouteDetailsVM()
            : this((IBusServiceModel)Assembly.Load("OneBusAway.WP7.Model")
                .GetType("OneBusAway.WP7.Model.BusServiceModel")
                .GetField("Singleton")
                .GetValue(null))
        {
            
        }

        public RouteDetailsVM(IBusServiceModel busServiceModel)
        {
            this.busServiceModel = busServiceModel;

            this.busServiceModel.StopsForRoute_Completed += new EventHandler<EventArgs.StopsForRouteEventArgs>(busServiceModel_StopsForRoute_Completed);
            this.busServiceModel.ArrivalsForStop_Completed += new EventHandler<EventArgs.ArrivalsForStopEventArgs>(busServiceModel_ArrivalsForStop_Completed);
            this.busServiceModel.TripDetailsForArrival_Completed += new EventHandler<EventArgs.TripDetailsForArrivalEventArgs>(busServiceModel_TripDetailsForArrival_Completed);

            StopsForRoute = new ObservableCollection<RouteStops>();
            ArrivalsForStop = new ObservableCollection<ArrivalAndDeparture>();
        }

        #endregion

        #region Public Properties

        public ObservableCollection<RouteStops> StopsForRoute { get; private set; }
        public ObservableCollection<ArrivalAndDeparture> ArrivalsForStop { get; private set; }
        public ObservableCollection<TripDetails> TripDetailsForArrivals { get; private set; }

        #endregion

        #region Public Methods

        public void LoadStopsForRoute(Route route)
        {
            busServiceModel.StopsForRoute(route);
        }

        public void LoadArrivalsForStop(Stop stop)
        {
            busServiceModel.ArrivalsForStop(stop);
        }

        public void LoadTripsForArrivals(List<ArrivalAndDeparture> arrivals)
        {
            arrivals.ForEach(arrival => busServiceModel.TripDetailsForArrivals(arrivals));
        }

        #endregion

        #region Event Handlers

        void busServiceModel_StopsForRoute_Completed(object sender, EventArgs.StopsForRouteEventArgs e)
        {
            if (e.error == null)
            {
                StopsForRoute.Clear();
                e.routeStops.ForEach(routeStop => StopsForRoute.Add(routeStop));
            }
        }

        void busServiceModel_ArrivalsForStop_Completed(object sender, EventArgs.ArrivalsForStopEventArgs e)
        {
            if (e.error == null)
            {
                ArrivalsForStop.Clear();
                e.arrivals.ForEach(arrival => ArrivalsForStop.Add(arrival));
            }
        }

        void busServiceModel_TripDetailsForArrival_Completed(object sender, EventArgs.TripDetailsForArrivalEventArgs e)
        {
            if (e.error == null)
            {
                TripDetailsForArrivals.Clear();
                e.tripDetails.ForEach(tripDetail => TripDetailsForArrivals.Add(tripDetail));
            }
        }

        #endregion

    }
}
