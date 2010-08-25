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
    public class RouteDetailsVM : IViewModel
    {

        #region Private Variables

        private IBusServiceModel busServiceModel;
        private IAppDataModel appDataModel;
        private ArrivalsForStopHandler arrivalsForStopHandler;
        private int _loadingCount;
        private bool _loading;
        public int loadingCount
        {
            get
            {
                return _loadingCount;
            }
            set
            {
                _loadingCount = value;
                Loading = loadingCount != 0;
            }
        } 


        #endregion

        #region Constructors

        // TODO: We need to convert the VM's to a Singleton, or add a Dispose method
        // currently a new VM is created every time a new route details page is opened
        // and the old event hanlders keep getting called, wasting perf
        public static RouteDetailsVM Singleton = new RouteDetailsVM();

        public RouteDetailsVM()
            : this((IBusServiceModel)Assembly.Load("OneBusAway.WP7.Model")
                .GetType("OneBusAway.WP7.Model.BusServiceModel")    
                .GetField("Singleton")
                .GetValue(null),
                (IAppDataModel)Assembly.Load("OneBusAway.WP7.Model")
                .GetType("OneBusAway.WP7.Model.AppDataModel")
                .GetField("Singleton")
                .GetValue(null)
            )
        {
            
        }

        public RouteDetailsVM(IBusServiceModel busServiceModel, IAppDataModel appDataModel)
        {
            this.busServiceModel = busServiceModel;
            this.appDataModel = appDataModel;

            StopsForRoute = new ObservableCollection<RouteStops>();
            ArrivalsForStop = new ObservableCollection<ArrivalAndDeparture>();
        }

        #endregion

        #region Public Properties

        public ObservableCollection<RouteStops> StopsForRoute { get; private set; }
        public ObservableCollection<ArrivalAndDeparture> ArrivalsForStop { get; private set; }
        public ObservableCollection<TripDetails> TripDetailsForArrivals { get; private set; }
        public bool Loading { 
            get { return loadingCount != 0; }
            set        
            {
                if (_loading == value)
                    return;

                _loading = value;
                OnPropertyChanged("Loading");        
            }
        } 
 
        #endregion

        #region Public Methods

        public void LoadStopsForRoute(Route route)
        {
            ++loadingCount;
            busServiceModel.StopsForRoute(route);
        }

        public void LoadArrivalsForStop(Stop stop)
        {
            LoadArrivalsForStop(stop, null);
        }

        public void LoadArrivalsForStop(Stop stop, Route routeFilter)
        {
            arrivalsForStopHandler.routeFilter = routeFilter;

            ++loadingCount;
            busServiceModel.ArrivalsForStop(stop);
        }

        public void ChangeFilterForArrivals(Route routeFilter)
        {
            arrivalsForStopHandler.UpdateFilter(routeFilter);
        }

        public void LoadTripsForArrivals(List<ArrivalAndDeparture> arrivals)
        {
            ++loadingCount;
            arrivals.ForEach(arrival => busServiceModel.TripDetailsForArrivals(arrivals));
        }

        public void AddFavorite(FavoriteRouteAndStop favorite)
        {
            appDataModel.AddFavorite(favorite);
        }

        public bool IsFavorite(FavoriteRouteAndStop favorite)
        {
            return appDataModel.IsFavorite(favorite);
        }

        public void DeleteFavorite(FavoriteRouteAndStop favorite)
        {
            appDataModel.DeleteFavorite(favorite);
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
        }


        void busServiceModel_ArrivalsForStop_Completed(object sender, EventArgs.ArrivalsForStopEventArgs e)
        {
            --loadingCount;

            Debug.Assert(e.error == null);
        }

        private class ArrivalsForStopHandler
        {
            public Route routeFilter;

            private ObservableCollection<ArrivalAndDeparture> arrivalsForStop;
            private List<ArrivalAndDeparture> unfilteredArrivals;

            public ArrivalsForStopHandler(ObservableCollection<ArrivalAndDeparture> arrivalsForStop)
            {
                routeFilter = null;
                this.arrivalsForStop = arrivalsForStop;
                unfilteredArrivals = new List<ArrivalAndDeparture>();
            }

            public void busServiceModel_ArrivalsForStop_Completed(object sender, EventArgs.ArrivalsForStopEventArgs e)
            {

                Debug.Assert(e.error == null);

                if (e.error == null)
                {
                    unfilteredArrivals = e.arrivals;
                    FilterArrivals();
                }
            }

            public void UpdateFilter(Route routeFilter)
            {
                this.routeFilter = routeFilter;

                FilterArrivals();
            }

            private void FilterArrivals()
            {
                arrivalsForStop.Clear();

                foreach (ArrivalAndDeparture arrival in unfilteredArrivals)
                {
                    if (routeFilter != null && routeFilter.id != arrival.routeId)
                    {
                        continue;
                    }

                    arrivalsForStop.Add(arrival);
                }
            }
        }

        void busServiceModel_TripDetailsForArrival_Completed(object sender, EventArgs.TripDetailsForArrivalEventArgs e)
        {
            --loadingCount;

            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                TripDetailsForArrivals.Clear();
                e.tripDetails.ForEach(tripDetail => TripDetailsForArrivals.Add(tripDetail));
            }
        }

        #endregion

        public void RegisterEventHandlers()
        {
            this.busServiceModel.TripDetailsForArrival_Completed += new EventHandler<EventArgs.TripDetailsForArrivalEventArgs>(busServiceModel_TripDetailsForArrival_Completed);
            this.busServiceModel.StopsForRoute_Completed += new EventHandler<EventArgs.StopsForRouteEventArgs>(busServiceModel_StopsForRoute_Completed);

            arrivalsForStopHandler = new ArrivalsForStopHandler(ArrivalsForStop);
            this.busServiceModel.ArrivalsForStop_Completed += new EventHandler<EventArgs.ArrivalsForStopEventArgs>(arrivalsForStopHandler.busServiceModel_ArrivalsForStop_Completed);
            this.busServiceModel.ArrivalsForStop_Completed += new EventHandler<EventArgs.ArrivalsForStopEventArgs>(busServiceModel_ArrivalsForStop_Completed);

        }


        public void UnregisterEventHandlers()
        {
            this.busServiceModel.TripDetailsForArrival_Completed -= new EventHandler<EventArgs.TripDetailsForArrivalEventArgs>(busServiceModel_TripDetailsForArrival_Completed);
            this.busServiceModel.StopsForRoute_Completed -= new EventHandler<EventArgs.StopsForRouteEventArgs>(busServiceModel_StopsForRoute_Completed);

            this.busServiceModel.ArrivalsForStop_Completed -= new EventHandler<EventArgs.ArrivalsForStopEventArgs>(arrivalsForStopHandler.busServiceModel_ArrivalsForStop_Completed);
            arrivalsForStopHandler = null;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
