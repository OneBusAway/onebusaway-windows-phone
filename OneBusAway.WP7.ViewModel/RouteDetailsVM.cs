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
using System.Linq;
using System.Windows.Threading;
using System.Threading;


namespace OneBusAway.WP7.ViewModel
{
    public class RouteDetailsVM : AViewModel
    {

        #region Private Variables

        private Route routeFilter;
        private List<ArrivalAndDeparture> unfilteredArrivals;
        private Object arrivalsLock;

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
            ArrivalsForStop = new ObservableCollection<ArrivalAndDeparture>();
            TripDetailsForArrivals = new ObservableCollection<TripDetails>();
            unfilteredArrivals = new List<ArrivalAndDeparture>();
            routeFilter = null;
            arrivalsLock = new Object();
        }

        #endregion

        #region Public Properties

        public ObservableCollection<ArrivalAndDeparture> ArrivalsForStop { get; private set; }
        public ObservableCollection<TripDetails> TripDetailsForArrivals { get; private set; }
 
        #endregion

        #region Public Methods

        public void SwitchToRouteByArrival(ArrivalAndDeparture arrival)
        {
            operationTracker.WaitForOperation("StopsForRoute");

            StopsForRouteCompleted callback = new StopsForRouteCompleted(this, arrival);
            busServiceModel.StopsForRoute_Completed += new EventHandler<EventArgs.StopsForRouteEventArgs>(callback.busServiceModel_StopsForRoute_Completed);

            busServiceModel.StopsForRoute(new Route() { id = arrival.routeId });

            LoadTripsForArrivals(ArrivalsForStop.ToList(), new Route() { id = arrival.routeId });
        }

        public void SwitchToStop(Stop stop)
        {
            CurrentViewState.CurrentStop = stop;
            LoadArrivalsForStop(stop);
        }

        public void LoadArrivalsForStop(Stop stop)
        {
            LoadArrivalsForStop(stop, routeFilter);
        }

        public void LoadArrivalsForStop(Stop stop, Route routeFilter)
        {
            lock (arrivalsLock)
            {
                unfilteredArrivals.Clear();
                ArrivalsForStop.Clear();
            }

            this.routeFilter = routeFilter;
            RefreshArrivalsForStop(stop);
        }

        public void RefreshArrivalsForStop(Stop stop)
        {
            operationTracker.WaitForOperation("ArrivalsForStop");
            busServiceModel.ArrivalsForStop(stop);
        }

        public void ChangeFilterForArrivals(Route routeFilter)
        {
            this.routeFilter = routeFilter;
            FilterArrivals();
        }

        public void LoadTripsForArrivals(List<ArrivalAndDeparture> arrivals, Route selectedRoute)
        {
            List<ArrivalAndDeparture> arrivalsForSelectedRoute = new List<ArrivalAndDeparture>();

            arrivals.ForEach(arrival =>
            {
                if (arrival.routeId == selectedRoute.id)
                {
                    arrivalsForSelectedRoute.Add(arrival);
                }
            }
            );
       
            LoadTripsForArrivals(arrivalsForSelectedRoute);
        }

        public void LoadTripsForArrivals(List<ArrivalAndDeparture> arrivals)
        {
            operationTracker.WaitForOperation("TripsForArrivals");
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

        private class StopsForRouteCompleted
        {
            ArrivalAndDeparture arrival;
            RouteDetailsVM viewModel;

            public StopsForRouteCompleted(RouteDetailsVM viewModel, ArrivalAndDeparture arrival)
            {
                this.viewModel = viewModel;
                this.arrival = arrival;
            }

            public void busServiceModel_StopsForRoute_Completed(object sender, EventArgs.StopsForRouteEventArgs e)
            {
                Debug.Assert(e.error == null);

                if (e.error == null)
                {
                    viewModel.UIAction(() =>
                        {
                            viewModel.CurrentViewState.CurrentRouteDirection = null;
                            e.routeStops.ForEach(routeStop =>
                                {
                                    // These aren't always the same, hopefully this comparison will work
                                    if (routeStop.name.Contains(arrival.tripHeadsign) || arrival.tripHeadsign.Contains(routeStop.name))
                                    {
                                        viewModel.CurrentViewState.CurrentRouteDirection = routeStop;
                                        viewModel.CurrentViewState.CurrentRoute = routeStop.route;
                                    }
                                }
                             );

                            Debug.Assert(viewModel.CurrentViewState.CurrentRouteDirection != null);
                        });
                }
                else
                {
                    viewModel.ErrorOccured(this, e.error);
                }

                viewModel.busServiceModel.StopsForRoute_Completed -= new EventHandler<EventArgs.StopsForRouteEventArgs>(this.busServiceModel_StopsForRoute_Completed);
                viewModel.operationTracker.DoneWithOperation("StopsForRoute");
            }
        }


        void busServiceModel_ArrivalsForStop_Completed(object sender, EventArgs.ArrivalsForStopEventArgs e)
        {
            Debug.Assert(e.error == null); 
        
            if (e.error == null)
            {
                // We are loading arrivals fresh, add all of them
                if (unfilteredArrivals.Count == 0)
                {
                    lock (arrivalsLock)
                    {
                        unfilteredArrivals = e.arrivals;
                    }
                    FilterArrivals();
                }
                else
                {
                    // We already have arrivals in the list, so just refresh them
                    UIAction(() =>
                        {
                            lock (arrivalsLock)
                            {
                                // Start by updating all the times for all of the arrivals currently in the list,
                                // and find any arrivals that have timed out for this stop
                                List<ArrivalAndDeparture> arrivalsToRemove = new List<ArrivalAndDeparture>();
                                foreach (ArrivalAndDeparture arrival in unfilteredArrivals)
                                {
                                    int index = e.arrivals.IndexOf(arrival);
                                    if (index >= 0)
                                    {
                                        arrival.predictedArrivalTime = e.arrivals[index].predictedArrivalTime;
                                        arrival.predictedDepartureTime = e.arrivals[index].predictedDepartureTime;
                                    }
                                    else
                                    {
                                        // The latest collection no longer has this arrival, delete it from the 
                                        // list.  Otherwise we will keep it around forever, no longer updating
                                        // its time
                                        arrivalsToRemove.Add(arrival);
                                    }
                                }

                                arrivalsToRemove.ForEach(arrival =>
                                    {
                                        ArrivalsForStop.Remove(arrival);
                                        unfilteredArrivals.Remove(arrival);
                                    }
                                    );

                                // Now add any new arrivals that just starting showing up for this stop
                                foreach (ArrivalAndDeparture arrival in e.arrivals)
                                {
                                    // Ensure that we aren't adding routes that are filtered out
                                    if ((routeFilter == null || routeFilter.id == arrival.routeId) 
                                        && ArrivalsForStop.Contains(arrival) == false)
                                    {
                                        ArrivalsForStop.Add(arrival);
                                        unfilteredArrivals.Add(arrival);
                                    }
                                }
                            }
                        });
                }
            }
            else
            {
                ErrorOccured(this, e.error);
            }

            // We have a selected route, so load trips for that route
            if (CurrentViewState.CurrentRoute != null)
            {
                lock (arrivalsLock)
                {
                    // Kick off the new request from a different thread since we are
                    // on the HttpWebRequest thread currently
                    new Thread(() =>
                        LoadTripsForArrivals(ArrivalsForStop.ToList(), CurrentViewState.CurrentRoute)).Start();
                }
            }

            operationTracker.DoneWithOperation("ArrivalsForStop");
        }

        void busServiceModel_TripDetailsForArrival_Completed(object sender, EventArgs.TripDetailsForArrivalEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                UIAction(() =>
                    {
                        TripDetailsForArrivals.Clear();

                        foreach (TripDetails tripDetail in e.tripDetails)
                        {
                            if (tripDetail.location != null)
                            {
                                TripDetailsForArrivals.Add(tripDetail);
                            }
                        }
                    });
            }
            else
            {
                ErrorOccured(this, e.error);
            }

            operationTracker.DoneWithOperation("TripsForArrivals");
        }

        #endregion

        private void FilterArrivals()
        {
            UIAction(() =>
                {
                    lock (arrivalsLock)
                    {
                        ArrivalsForStop.Clear();

                        unfilteredArrivals.Sort(new ArrivalTimeComparer());
                        foreach (ArrivalAndDeparture arrival in unfilteredArrivals)
                        {
                            if (routeFilter != null && routeFilter.id != arrival.routeId)
                            {
                                continue;
                            }

                            ArrivalsForStop.Add(arrival);
                        }
                    }
                });
        }

        public override void RegisterEventHandlers(Dispatcher dispatcher)
        {
            base.RegisterEventHandlers(dispatcher);

            this.busServiceModel.TripDetailsForArrival_Completed += new EventHandler<EventArgs.TripDetailsForArrivalEventArgs>(busServiceModel_TripDetailsForArrival_Completed);
            this.busServiceModel.ArrivalsForStop_Completed += new EventHandler<EventArgs.ArrivalsForStopEventArgs>(busServiceModel_ArrivalsForStop_Completed);
        }

        public override void UnregisterEventHandlers()
        {
            base.UnregisterEventHandlers();

            this.busServiceModel.TripDetailsForArrival_Completed -= new EventHandler<EventArgs.TripDetailsForArrivalEventArgs>(busServiceModel_TripDetailsForArrival_Completed);
            this.busServiceModel.ArrivalsForStop_Completed -= new EventHandler<EventArgs.ArrivalsForStopEventArgs>(busServiceModel_ArrivalsForStop_Completed);

            this.operationTracker.ClearOperations();
        }
    }
}
