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
        private TripService tripService;
        private bool resultsLoaded;

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
            unfilteredArrivals = new List<ArrivalAndDeparture>();
            routeFilter = null;
            arrivalsLock = new Object();
            tripService = TripServiceFactory.Singleton.TripService;
            resultsLoaded = false;
        }

        #endregion

        #region Public Properties

        public ObservableCollection<ArrivalAndDeparture> ArrivalsForStop { get; private set; }

        private bool noResultsAvailable;
        public bool NoResultsAvailable
        {
            get
            {
                if (operationTracker.Loading == true || resultsLoaded == false)
                {
                    return false;
                }
                else
                {
                    return ArrivalsForStop.Count == 0;
                }
            }
        }
 
        #endregion

        #region Public Methods

        public void SubscribeToToastNotification(string stopId, string tripId, int minutes)
        {
            tripService.StartSubscription(stopId, tripId, minutes);
        }

        public void SwitchToRouteByArrival(ArrivalAndDeparture arrival, Action uiCallback)
        {
            operationTracker.WaitForOperation("StopsForRoute", string.Format("Loading details for route {0}...", arrival.routeShortName));

            StopsForRouteCompleted callback = new StopsForRouteCompleted(this, arrival, uiCallback);
            busServiceModel.StopsForRoute_Completed += new EventHandler<EventArgs.StopsForRouteEventArgs>(callback.busServiceModel_StopsForRoute_Completed);

            Route placeholder = new Route() { id = arrival.routeId, shortName = arrival.routeShortName};
            // This will at least cause the route number to immediately update
            CurrentViewState.CurrentRoute = placeholder;
            CurrentViewState.CurrentRouteDirection = new RouteStops();

            busServiceModel.StopsForRoute(LocationTracker.CurrentLocation, placeholder);

            ChangeFilterForArrivals(placeholder);
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

            // We've sent our first call off, set resultsLoaded to true
            resultsLoaded = true;
        }

        public void RefreshArrivalsForStop(Stop stop)
        {
            if (stop != null)
            {
                operationTracker.WaitForOperation("ArrivalsForStop", string.Empty);
                busServiceModel.ArrivalsForStop(LocationTracker.CurrentLocation, stop);
            }
        }

        public void ChangeFilterForArrivals(Route routeFilter)
        {
            this.routeFilter = routeFilter;
            FilterArrivals();
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
            Action uiCallback;

            public StopsForRouteCompleted(RouteDetailsVM viewModel, ArrivalAndDeparture arrival, Action uiCallback)
            {
                this.viewModel = viewModel;
                this.arrival = arrival;
                this.uiCallback = uiCallback;
            }

            public void busServiceModel_StopsForRoute_Completed(object sender, EventArgs.StopsForRouteEventArgs e)
            {
                Debug.Assert(e.error == null);

                if (e.error == null)
                {
                    viewModel.UIAction(() => viewModel.CurrentViewState.CurrentRouteDirection = null);

                    e.routeStops.ForEach(routeStop =>
                        {
                            // These aren't always the same, hopefully this comparison will work
                            if (routeStop.name.Contains(arrival.tripHeadsign) || arrival.tripHeadsign.Contains(routeStop.name))
                            {
                                viewModel.UIAction(() =>
                                    {
                                        viewModel.CurrentViewState.CurrentRouteDirection = routeStop;
                                        viewModel.CurrentViewState.CurrentRoute = routeStop.route;
                                    });
                            }
                        }
                        );

                    viewModel.UIAction(() => 
                        {
                            // This will happen if we don't find a match between the headsign and route direction
                            // The 48 route in particular has this problem, if we leave this as null the
                            // filtered appbar icon won't update correctly
                            if (viewModel.CurrentViewState.CurrentRouteDirection == null)
                            {
                                Debug.Assert(false);
                                viewModel.CurrentViewState.CurrentRouteDirection = new RouteStops();
                            }
                        }
                        );

                    if (uiCallback != null)
                    {
                        // Make this callback from the UI thread, because that is the easiest way
                        // to ensure it happens after the above UI thread operations.  If this callback
                        // occurs before the ViewState is updated it will cause a bug
                        viewModel.UIAction(() => uiCallback.Invoke());
                    }
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
                lock (arrivalsLock)
                {
                    // We are loading arrivals fresh, add all of them
                    if (unfilteredArrivals.Count == 0)
                    {
                        unfilteredArrivals = e.arrivals;
                        FilterArrivals();
                    }
                    else
                    {
                        // We already have arrivals in the list, so just refresh them
                        // Start by updating all the times for all of the arrivals currently in the list,
                        // and find any arrivals that have timed out for this stop
                        List<ArrivalAndDeparture> arrivalsToRemove = new List<ArrivalAndDeparture>();
                        foreach (ArrivalAndDeparture arrival in unfilteredArrivals)
                        {
                            int index = e.arrivals.IndexOf(arrival);
                            if (index >= 0)
                            {
                                ArrivalAndDeparture newArrivalTime = e.arrivals[index];
                                ArrivalAndDeparture currentArrival = arrival;
                                UIAction(() =>
                                    {
                                        currentArrival.predictedArrivalTime = newArrivalTime.predictedArrivalTime;
                                        currentArrival.predictedDepartureTime = newArrivalTime.predictedDepartureTime;

                                        currentArrival.tripDetails.scheduleDeviationInSec = newArrivalTime.tripDetails.scheduleDeviationInSec;
                                        currentArrival.tripDetails.closestStopId = newArrivalTime.tripDetails.closestStopId;
                                        currentArrival.tripDetails.closestStopTimeOffset = newArrivalTime.tripDetails.closestStopTimeOffset;
                                        currentArrival.tripDetails.coordinate = newArrivalTime.tripDetails.coordinate;
                                    });
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
                                UIAction(() => ArrivalsForStop.Remove(arrival));
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
                                ArrivalAndDeparture currentArrival = arrival;
                                UIAction(() => ArrivalsForStop.Add(currentArrival));
                                unfilteredArrivals.Add(currentArrival);
                            }
                        }
                    }
                }
            }
            else
            {
                ErrorOccured(this, e.error);
            }

            operationTracker.DoneWithOperation("ArrivalsForStop");

            // Refresh NoResultsAvailable status
            OnPropertyChanged("NoResultsAvailable");
        }

        #endregion

        private void FilterArrivals()
        {
            lock (arrivalsLock)
            {
                UIAction(() => ArrivalsForStop.Clear());

                unfilteredArrivals.Sort(new DepartureTimeComparer());
                foreach (ArrivalAndDeparture arrival in unfilteredArrivals)
                {
                    if (routeFilter != null && routeFilter.id != arrival.routeId)
                    {
                        continue;
                    }

                    ArrivalAndDeparture currentArrival = arrival;
                    UIAction(() => ArrivalsForStop.Add(currentArrival));
                }
            }

            // Refresh NoResultsAvailable status
            OnPropertyChanged("NoResultsAvailable");
        }

        public override void RegisterEventHandlers(Dispatcher dispatcher)
        {
            base.RegisterEventHandlers(dispatcher);

            this.busServiceModel.ArrivalsForStop_Completed += new EventHandler<EventArgs.ArrivalsForStopEventArgs>(busServiceModel_ArrivalsForStop_Completed);
        }

        public override void UnregisterEventHandlers()
        {
            base.UnregisterEventHandlers();

            this.busServiceModel.ArrivalsForStop_Completed -= new EventHandler<EventArgs.ArrivalsForStopEventArgs>(busServiceModel_ArrivalsForStop_Completed);

            this.operationTracker.ClearOperations();

            // Reset resultsLoaded to false when they navigate away from the page
            resultsLoaded = false;
        }
    }

    public class LoadArrivalsForStopEventArgs : System.EventArgs
    {

    }
}
