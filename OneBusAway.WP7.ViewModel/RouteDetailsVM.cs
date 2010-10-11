using System;
using System.Net;
using System.Collections.ObjectModel;
using System.Reflection;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using System.Device.Location;
using System.Collections.Generic;
using System.Diagnostics;
using OneBusAway.WP7.ViewModel.AppDataDataStructures;
using System.ComponentModel;

namespace OneBusAway.WP7.ViewModel
{
    public class RouteDetailsVM : AViewModel
    {

        #region Private Variables

        private Route routeFilter;
        private List<ArrivalAndDeparture> unfilteredArrivals;

        #endregion

        #region Constructors

        // TODO: We need to convert the VM's to a Singleton, or add a Dispose method
        // currently a new VM is created every time a new route details page is opened
        // and the old event hanlders keep getting called, wasting perf
        public static RouteDetailsVM Singleton = new RouteDetailsVM();

        public RouteDetailsVM()
            : base()
        {
            Initialize();
        }

        public RouteDetailsVM(IBusServiceModel busServiceModel, IAppDataModel appDataModel)
            : base(busServiceModel, appDataModel)
        {
            Initialize();
        }

        private void Initialize()
        {
            StopsForRoute = new ObservableCollection<RouteStops>();
            ArrivalsForStop = new ObservableCollection<ArrivalAndDeparture>();
            unfilteredArrivals = new List<ArrivalAndDeparture>();
            routeFilter = null;
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
            pendingOperations++;
            busServiceModel.StopsForRoute(route);
        }

        public void LoadArrivalsForStop(Stop stop)
        {
            LoadArrivalsForStop(stop, null);
        }

        public void LoadArrivalsForStop(Stop stop, Route routeFilter)
        {
            pendingOperations++;

            unfilteredArrivals.Clear();
            ArrivalsForStop.Clear();
            ChangeFilterForArrivals(routeFilter);

            busServiceModel.ArrivalsForStop(stop);
        }

        public void ChangeFilterForArrivals(Route routeFilter)
        {
            this.routeFilter = routeFilter;
            FilterArrivals();
        }

        public void LoadTripsForArrivals(List<ArrivalAndDeparture> arrivals)
        {
            pendingOperations++;
            busServiceModel.TripDetailsForArrivals(arrivals);
        }

        public void AddFavorite(FavoriteRouteAndStop favorite)
        {
            appDataModel.AddFavorite(favorite, FavoriteType.Favorite);
        }

        public bool IsFavorite(FavoriteRouteAndStop favorite)
        {
            return appDataModel.IsFavorite(favorite, FavoriteType.Favorite);
        }

        public void DeleteFavorite(FavoriteRouteAndStop favorite)
        {
            appDataModel.DeleteFavorite(favorite, FavoriteType.Favorite);
        }

        public void AddRecent(FavoriteRouteAndStop recent)
        {
            appDataModel.AddFavorite(recent, FavoriteType.Recent);
        }

        #endregion

        #region Event Handlers

        void busServiceModel_StopsForRoute_Completed(object sender, EventArgs.StopsForRouteEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                StopsForRoute.Clear();
                e.routeStops.ForEach(routeStop => StopsForRoute.Add(routeStop));
            }
            else
            {
                ErrorOccured(this, e.error);
            }

            pendingOperations--;
        }


        void busServiceModel_ArrivalsForStop_Completed(object sender, EventArgs.ArrivalsForStopEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                unfilteredArrivals = e.arrivals;
                FilterArrivals();
            }
            else
            {
                ErrorOccured(this, e.error);
            }

            pendingOperations--;
        }

        void busServiceModel_TripDetailsForArrival_Completed(object sender, EventArgs.TripDetailsForArrivalEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                TripDetailsForArrivals.Clear();
                e.tripDetails.ForEach(tripDetail => TripDetailsForArrivals.Add(tripDetail));
            }
            else
            {
                ErrorOccured(this, e.error);
            }

            pendingOperations--;
        }

        #endregion

        private void FilterArrivals()
        {
            ArrivalsForStop.Clear();

            foreach (ArrivalAndDeparture arrival in unfilteredArrivals)
            {
                if (routeFilter != null && routeFilter.id != arrival.routeId)
                {
                    continue;
                }

                ArrivalsForStop.Add(arrival);
            }
        }

        public override void RegisterEventHandlers()
        {
            base.RegisterEventHandlers();

            this.busServiceModel.TripDetailsForArrival_Completed += new EventHandler<EventArgs.TripDetailsForArrivalEventArgs>(busServiceModel_TripDetailsForArrival_Completed);
            this.busServiceModel.StopsForRoute_Completed += new EventHandler<EventArgs.StopsForRouteEventArgs>(busServiceModel_StopsForRoute_Completed);
            this.busServiceModel.ArrivalsForStop_Completed += new EventHandler<EventArgs.ArrivalsForStopEventArgs>(busServiceModel_ArrivalsForStop_Completed);
        }

        public override void UnregisterEventHandlers()
        {
            base.UnregisterEventHandlers();

            this.busServiceModel.TripDetailsForArrival_Completed -= new EventHandler<EventArgs.TripDetailsForArrivalEventArgs>(busServiceModel_TripDetailsForArrival_Completed);
            this.busServiceModel.StopsForRoute_Completed -= new EventHandler<EventArgs.StopsForRouteEventArgs>(busServiceModel_StopsForRoute_Completed);
            this.busServiceModel.ArrivalsForStop_Completed -= new EventHandler<EventArgs.ArrivalsForStopEventArgs>(busServiceModel_ArrivalsForStop_Completed);

            pendingOperations = 0;
        }
    }
}
