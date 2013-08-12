using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Device.Location;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using OneBusAway.WP7.ViewModel.AppDataDataStructures;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using OneBusAway.WP7.ViewModel.EventArgs;
using OneBusAway.WP7.ViewModel.LocationServiceDataStructures;

namespace OneBusAway.WP7.ViewModel
{
    public class MainPageVM : AViewModel
    {

        #region Private Variables

        private int maxRoutes = 30;
        private int maxStops = 30;
        private Object displayRouteLock;

        #endregion

        #region Constructors

        public MainPageVM()
            : base()
        {
            Initialize();
        }

        public MainPageVM(IBusServiceModel busServiceModel, IAppDataModel appDataModel)
            : base(busServiceModel, appDataModel)
        {
            Initialize();
        }

        private void Initialize()
        {
            displayRouteLock = new Object();
            StopsForLocation = new ObservableCollection<Stop>();
            DisplayRouteForLocation = new BufferedReference<ObservableCollection<DisplayRoute>>(
                new ObservableCollection<DisplayRoute>(), 
                new ObservableCollection<DisplayRoute>());
            directionHelper = new Dictionary<string, ObservableCollection<RouteStops>>();
            Favorites = new ObservableCollection<FavoriteRouteAndStop>();
            Recents = new ObservableCollection<FavoriteRouteAndStop>();
        }

        #endregion

        #region Public Properties

        private ObservableCollection<Stop> stopsForLocation;
        public ObservableCollection<Stop> StopsForLocation 
        {
            get { return stopsForLocation; }

            private set
            {
                stopsForLocation = value;
                OnPropertyChanged("StopsForLocation");
            }
        }

        private IDictionary<string, ObservableCollection<RouteStops>> directionHelper;

        public BufferedReference<ObservableCollection<DisplayRoute>> DisplayRouteForLocation { get; private set; }

        private ObservableCollection<FavoriteRouteAndStop> favorites;
        public ObservableCollection<FavoriteRouteAndStop> Favorites 
        {
            get { return favorites; }

            private set
            {
                favorites = value;
                OnPropertyChanged("Favorites");
            }
        }

        private ObservableCollection<FavoriteRouteAndStop> recents;
        public ObservableCollection<FavoriteRouteAndStop> Recents 
        {
            get { return recents; }

            private set
            {
                recents = value;
                OnPropertyChanged("Recents");
            }
        }

        #endregion

        #region Public Methods

        public void LoadInfoForLocation()
        {
            LoadInfoForLocation(false);
        }

        /// <summary>
        /// Call the OBA webservice to load stops and routes for the current location.
        /// </summary>
        /// <param name="radiusInMeters"></param>
        /// <param name="invalidateCache">If true, will discard any cached result and requery the server</param>
        public void LoadInfoForLocation(bool invalidateCache)
        {
            StopsForLocation.Clear();
            
            DisplayRouteForLocation.Working.Clear();

            operationTracker.WaitForOperation("CombinedInfoForLocation", "Searching for buses...");
            locationTracker.RunWhenLocationKnown(delegate(GeoCoordinate location)
            {
                busServiceModel.CombinedInfoForLocation(location, defaultSearchRadius, -1, invalidateCache);
            });
        }

        public void LoadFavorites()
        {
            Favorites.Clear();
            List<FavoriteRouteAndStop> favorites = appDataModel.GetFavorites(FavoriteType.Favorite);
            // TODO: Add a way for calls to wait ~5 seconds for location to become available
            // but fallback to running without location if it times out
            if (LocationTracker.LocationKnown == true)
            {
                favorites.Sort(new FavoriteDistanceComparer(locationTracker.CurrentLocation));
            }
            favorites.ForEach(favorite => Favorites.Add(favorite));

            Recents.Clear();
            List<FavoriteRouteAndStop> recents = appDataModel.GetFavorites(FavoriteType.Recent);
            recents.Sort(new RecentLastAccessComparer());
            recents.ForEach(recent => Recents.Add(recent));
        }

        public delegate void SearchByRoute_Callback(List<Route> routes, Exception error);
        public void SearchByRoute(string routeNumber, SearchByRoute_Callback callback)
        {
            operationTracker.WaitForOperation("SearchByRoute", string.Format("Searching for route {0}...", routeNumber));

            busServiceModel.SearchForRoutes_Completed += new SearchByRouteCompleted(callback, busServiceModel, this).SearchByRoute_Completed;
            locationTracker.RunWhenLocationKnown(delegate(GeoCoordinate location)
                {
                    busServiceModel.SearchForRoutes(location, routeNumber);
                });
        }

        public delegate void SearchByStop_Callback(List<Stop> stops, Exception error);
        public void SearchByStop(string stopNumber, SearchByStop_Callback callback)
        {
            operationTracker.WaitForOperation("SearchByStop", string.Format("Searching for stop {0}...", stopNumber));

            busServiceModel.SearchForStops_Completed += new SearchByStopCompleted(callback, busServiceModel, this).SearchByStop_Completed;
            locationTracker.RunWhenLocationKnown(delegate(GeoCoordinate location)
            {
                busServiceModel.SearchForStops(location, stopNumber);
            });
        }

        public delegate void SearchByAddress_Callback(LocationForQuery location, Exception error);
        public void SearchByAddress(string addressString, SearchByAddress_Callback callback)
        {
            operationTracker.WaitForOperation("SearchByAddress", string.Format("Searching for location '{0}'...", addressString));

            locationModel.LocationForAddress(addressString, locationTracker.CurrentLocationSafe, callback);
        }

        public delegate void CheckForLocalTransitData_Callback(bool hasData);
        public void CheckForLocalTransitData(CheckForLocalTransitData_Callback callback)
        {
            locationTracker.RunWhenLocationKnown(delegate(GeoCoordinate location)
            {
                bool hasData;
                // Ensure that their current location is within ~150km of a supported region
                if (busServiceModel.DistanceFromClosestSupportedRegion(LocationTracker.CurrentLocation) > 150000)
                {
                    hasData = false;
                }
                else
                {
                    hasData = true;
                }

                callback(hasData);
            });
        }

        public override void RegisterEventHandlers(Dispatcher dispatcher)
        {
            base.RegisterEventHandlers(dispatcher);

            this.busServiceModel.CombinedInfoForLocation_Completed += new EventHandler<EventArgs.CombinedInfoForLocationEventArgs>(busServiceModel_CombinedInfoForLocation_Completed);
            this.busServiceModel.StopsForRoute_Completed += new EventHandler<EventArgs.StopsForRouteEventArgs>(busServiceModel_StopsForRoute_Completed);
            
            this.appDataModel.Favorites_Changed += new EventHandler<EventArgs.FavoritesChangedEventArgs>(appDataModel_Favorites_Changed);
            this.appDataModel.Recents_Changed += new EventHandler<EventArgs.FavoritesChangedEventArgs>(appDataModel_Recents_Changed);

            this.locationModel.LocationForAddress_Completed += busServiceModel_LocationForAddress_Completed;
        }

        public override void UnregisterEventHandlers()
        {
            base.UnregisterEventHandlers();

            this.busServiceModel.CombinedInfoForLocation_Completed -= new EventHandler<EventArgs.CombinedInfoForLocationEventArgs>(busServiceModel_CombinedInfoForLocation_Completed);
            this.busServiceModel.StopsForRoute_Completed -= new EventHandler<EventArgs.StopsForRouteEventArgs>(busServiceModel_StopsForRoute_Completed);

            this.appDataModel.Favorites_Changed -= new EventHandler<EventArgs.FavoritesChangedEventArgs>(appDataModel_Favorites_Changed);
            this.appDataModel.Recents_Changed -= new EventHandler<EventArgs.FavoritesChangedEventArgs>(appDataModel_Recents_Changed);

            this.locationModel.LocationForAddress_Completed -= busServiceModel_LocationForAddress_Completed;

            // Reset loading to 0 since event handlers have been unregistered
            this.operationTracker.ClearOperations();
        }

        #endregion

        #region Event Handlers

        private class SearchByRouteCompleted
        {
            private SearchByRoute_Callback callback;
            private IBusServiceModel busServiceModel;
            private MainPageVM viewModel;

            public SearchByRouteCompleted(SearchByRoute_Callback callback, IBusServiceModel busServiceModel, MainPageVM viewModel)
            {
                this.callback = callback;
                this.busServiceModel = busServiceModel;
                this.viewModel = viewModel;
            }

            public void SearchByRoute_Completed(object sender, SearchForRoutesEventArgs e)
            {
                Debug.Assert(e.error == null);

                if (e.error == null)
                {
                e.routes.Sort(new RouteDistanceComparer(e.location));

                int count = 0;
                foreach (Route route in e.routes)
                {
                    if (count > viewModel.maxRoutes)
                    {
                        break;
                    }

                    Route currentRoute = route;
                    count++;
                }

                }
                else
                {
                    viewModel.ErrorOccured(this, e.error);
                }
                
                callback(e.routes, e.error);
                busServiceModel.SearchForRoutes_Completed -= new EventHandler<EventArgs.SearchForRoutesEventArgs>(this.SearchByRoute_Completed);

                viewModel.operationTracker.DoneWithOperation("SearchByRoute");
            }
        }

        private class SearchByStopCompleted
        {
            private SearchByStop_Callback callback;
            private IBusServiceModel busServiceModel;
            private MainPageVM viewModel;

            public SearchByStopCompleted(SearchByStop_Callback callback, IBusServiceModel busServiceModel, MainPageVM viewModel)
            {
                this.callback = callback;
                this.busServiceModel = busServiceModel;
                this.viewModel = viewModel;
            }

            public void SearchByStop_Completed(object sender, SearchForStopsEventArgs e)
            {
                Debug.Assert(e.error == null);

                if (e.error == null)
                {
                e.stops.Sort(new StopDistanceComparer(e.location));

                viewModel.UIAction(() => viewModel.StopsForLocation.Clear());

                int count = 0;
                foreach (Stop stop in e.stops)
                {
                    Stop currentStop = stop;
                    viewModel.UIAction(() => viewModel.StopsForLocation.Add(currentStop));
                    count++;
                }
                }
                else
                {
                    viewModel.ErrorOccured(this, e.error);
                }

                callback(e.stops, e.error);
                busServiceModel.SearchForStops_Completed -= this.SearchByStop_Completed;

                viewModel.operationTracker.DoneWithOperation("SearchByStop");
            }
        }

        public void busServiceModel_LocationForAddress_Completed(object sender, EventArgs.LocationForAddressEventArgs e)
        {
            Debug.Assert(e.error == null);

            LocationForQuery location;
            if (e.locations.Count > 1)
            {
                location = e.locations[0];
                foreach (LocationForQuery l in e.locations)
                {
                    // If the candidate is a higher confidence than the
                    // current selected location, pick it instead
                    if (l.confidence < location.confidence)
                    {
                        location = l;
                    }
                    // The candidate doesn't have a higher confidence, so confirm that it
                    // is the same confidence, and then select whichever one is closest to Seattle
                    else if (l.confidence == location.confidence &&
                        l.location.GetDistanceTo(e.searchNearLocation) <
                        location.location.GetDistanceTo(e.searchNearLocation))
                    {
                        location = l;
                    }
                }
            }
            else if (e.locations.Count == 1)
            {
                location = e.locations[0];
            }
            else
            {
                location = null;
            }

            SearchByAddress_Callback callback = (SearchByAddress_Callback)e.state;
            callback(location, e.error);

            operationTracker.DoneWithOperation("SearchByAddress");
        }

        void busServiceModel_CombinedInfoForLocation_Completed(object sender, EventArgs.CombinedInfoForLocationEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
            e.stops.Sort(new StopDistanceComparer(e.location));
            e.routes.Sort(new RouteDistanceComparer(e.location));

            int stopCount = 0;
            foreach (Stop stop in e.stops)
            {
                if (stopCount > maxStops)
                {
                    break;
                }

                Stop currentStop = stop;
                UIAction(() => StopsForLocation.Add(currentStop));
                stopCount++;
            }

            int routeCount = 0;
            foreach (Route route in e.routes)
            {
                if (routeCount > maxRoutes)
                {
                    break;
                }

                DisplayRoute currentDisplayRoute = new DisplayRoute() { Route = route };
                DisplayRouteForLocation.Working.Add(currentDisplayRoute);
                routeCount++;
            }

            // Done with work in the background.  Flush the results out to the UI.  This is quick.
            object testref = null;
            UIAction(() => 
                {
                    DisplayRouteForLocation.Toggle();
                    testref = new object();
                }
            );

            // hack to wait for the UI action to complete
            // note this executes in the background, so it's fine to be slow.
            int execcount = 0;
            while (testref == null)
            {
                execcount++;
                Thread.Sleep(100);
            }

            // finally, queue up more work
            lock (DisplayRouteForLocation.CurrentSyncRoot)
            {
                foreach (DisplayRoute r in DisplayRouteForLocation.Current)
                {
                    directionHelper[r.Route.id] = r.RouteStops;

                    operationTracker.WaitForOperation(string.Format("StopsForRoute_{0}", r.Route.id), "Loading route details...");
                    busServiceModel.StopsForRoute(LocationTracker.CurrentLocation, r.Route);
                }
            }
            }
            else
            {
                ErrorOccured(this, e.error);
            }

            operationTracker.DoneWithOperation("CombinedInfoForLocation");
        }

        void busServiceModel_StopsForRoute_Completed(object sender, EventArgs.StopsForRouteEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
            e.routeStops.ForEach(r => UIAction(() =>
                { 
                    if(directionHelper.ContainsKey(e.route.id))
                    {
                        directionHelper[e.route.id].Add(r); 
                    }
                }));
            }
            else
            {
                ErrorOccured(this, e.error);
            }

            operationTracker.DoneWithOperation(string.Format("StopsForRoute_{0}", e.route.id));
        }

        void appDataModel_Favorites_Changed(object sender, EventArgs.FavoritesChangedEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
            if (LocationTracker.LocationKnown == true)
            {
                e.newFavorites.Sort(new FavoriteDistanceComparer(locationTracker.CurrentLocation));
            }

            UIAction(() => Favorites.Clear());
            e.newFavorites.ForEach(favorite => UIAction(() => Favorites.Add(favorite)));
        }
            else
            {
                ErrorOccured(this, e.error);
            }
        }

        void appDataModel_Recents_Changed(object sender, EventArgs.FavoritesChangedEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
            e.newFavorites.Sort(new RecentLastAccessComparer());

            UIAction(() => Recents.Clear());
            e.newFavorites.ForEach(recent => UIAction(() => Recents.Add(recent)));
        }
            else
            {
                ErrorOccured(this, e.error);
            }
        }

        #endregion

    }

    public class DisplayRoute : INotifyPropertyChanged
    {
        public ObservableCollection<RouteStops> RouteStops { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;

        public DisplayRoute()
        {
            RouteStops = new ObservableCollection<RouteStops>();
            route = null;
        }

        private Route route;
        public Route Route
        {
            get
            {
                return this.route;
            }

            set
            {
                if (value != this.route)
                {
                    this.route = value;
                    NotifyPropertyChanged("Route");
                }
            }
        }

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
