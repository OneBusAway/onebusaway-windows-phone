using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Device.Location;
using System.Diagnostics;
using System.Windows.Threading;
using OneBusAway.WP7.ViewModel.AppDataDataStructures;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using OneBusAway.WP7.ViewModel.EventArgs;
using OneBusAway.WP7.ViewModel.Data;

namespace OneBusAway.WP7.ViewModel
{
    public class MainPageVM : AViewModel
    {

        #region Private Variables

        private int maxRoutes = 30;
        private int maxStops = 30;
        private ICache<string, Route> routesCache;
        private ICache<string, Stop> stopsCache;
        private object routesForLocationLock = new object();
        private object stopsForLocationLock = new object();

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
            StopsForLocation = new ObservableCollection<Stop>();
            RoutesForLocation = new ObservableCollection<Route>();
            Favorites = new ObservableCollection<FavoriteRouteAndStop>();
            Recents = new ObservableCollection<FavoriteRouteAndStop>();

            stopsCache = CacheFactory.Singleton.StopsCache;
            routesCache = CacheFactory.Singleton.RoutesCache;
        }

        #endregion

        #region Public Properties

        public ObservableCollection<Stop> StopsForLocation { get; private set; }
        public ObservableCollection<Route> RoutesForLocation { get; private set; }
        public ObservableCollection<FavoriteRouteAndStop> Favorites { get; private set; }
        public ObservableCollection<FavoriteRouteAndStop> Recents { get; private set; }

        #endregion

        #region Public Methods

        public void LoadInfoForLocation(int radiusInMeters)
        {
            LoadInfoForLocation(radiusInMeters, false);
        }

        /// <summary>
        /// Call the OBA webservice to load stops and routes for the current location.
        /// </summary>
        /// <param name="radiusInMeters"></param>
        /// <param name="invalidateCache">If true, will discard any cached result and requery the server</param>
        public void LoadInfoForLocation(int radiusInMeters, bool invalidateCache)
        {
            lock (stopsForLocationLock)
            {
                StopsForLocation.Clear();
            }
            lock (routesForLocationLock)
            {
                RoutesForLocation.Clear();
            }

            locationTracker.RunWhenLocationKnown(delegate(GeoCoordinate location)
            {
                UIAction(() => this.LoadingText = "Searching for buses...");
                
                List<Stop> tempStops = new List<Stop>();
                foreach (Pair<string, Stop> stopPair in stopsCache.GetAll())
                {
                    if (stopPair.value.location.GetDistanceTo(location) <= radiusInMeters)
                    {
                        tempStops.Add(stopPair.value);
                    }
                }
                tempStops.Sort(new StopDistanceComparer(location));

                // yes, this is a loop on the UI thread.
                // if you put just the body of the loop on the UI thread, 
                // then you end up with a list that shows the same stop over and over again.
                UIAction(() =>
                {
                    lock (stopsForLocationLock)
                    {
                        foreach (Stop s in tempStops)
                        {
                            StopsForLocation.Add(s);
                        }
                    }
                });

                List<Route> tempRoutes = new List<Route>();
                foreach (Pair<string, Route> routePair in routesCache.GetAll())
                {
                    if (routePair.value.closestStop.location.GetDistanceTo(location) <= radiusInMeters)
                    {
                        tempRoutes.Add(routePair.value);
                    }
                }
                tempRoutes.Sort(new RouteDistanceComparer(location));

                UIAction(() =>
                {
                    lock (routesForLocationLock)
                    {
                        foreach (Route r in tempRoutes)
                        {
                            RoutesForLocation.Add(r);
                        }
                    }
                });

                operationTracker.WaitForOperation("CombinedInfoForLocation");
                busServiceModel.CombinedInfoForLocation(location, radiusInMeters, -1, invalidateCache);
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
            operationTracker.WaitForOperation("SearchByRoute");

            busServiceModel.SearchForRoutes_Completed += new SearchByRouteCompleted(callback, busServiceModel, this).SearchByRoute_Completed;
            locationTracker.RunWhenLocationKnown(delegate(GeoCoordinate location)
                {
                    busServiceModel.SearchForRoutes(location, routeNumber);
                });
        }

        public delegate void SearchByStop_Callback(List<Stop> stops, Exception error);
        public void SearchByStop(string stopNumber, SearchByStop_Callback callback)
        {
            operationTracker.WaitForOperation("SearchByStop");

            busServiceModel.SearchForStops_Completed += new SearchByStopCompleted(callback, busServiceModel, this).SearchByStop_Completed;
            locationTracker.RunWhenLocationKnown(delegate(GeoCoordinate location)
            {
                busServiceModel.SearchForStops(location, stopNumber);
            });
        }

        public delegate void SearchByAddress_Callback(GeoCoordinate routes, Exception error);
        public void SearchByAddress(string addressString, SearchByAddress_Callback callback)
        {
            operationTracker.WaitForOperation("SearchByAddress");
            busServiceModel.LocationForAddress(addressString);
        }

        public delegate void CheckForLocalTransitData_Callback(bool hasData);
        public void CheckForLocalTransitData(CheckForLocalTransitData_Callback callback)
        {
            locationTracker.RunWhenLocationKnown(delegate(GeoCoordinate location)
            {
                bool hasData;
                // Ensure that their current location is within ~150 miles of Seattle
                if (location.GetDistanceTo(LocationTracker.DefaultLocationStatic) > 250000)
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
            this.busServiceModel.LocationForAddress_Completed += new EventHandler<EventArgs.LocationForAddressEventArgs>(busServiceModel_LocationForAddress_Completed);

            this.appDataModel.Favorites_Changed += new EventHandler<EventArgs.FavoritesChangedEventArgs>(appDataModel_Favorites_Changed);
            this.appDataModel.Recents_Changed += new EventHandler<EventArgs.FavoritesChangedEventArgs>(appDataModel_Recents_Changed);
        }

        public override void UnregisterEventHandlers()
        {
            base.UnregisterEventHandlers();

            this.busServiceModel.CombinedInfoForLocation_Completed -= new EventHandler<EventArgs.CombinedInfoForLocationEventArgs>(busServiceModel_CombinedInfoForLocation_Completed);
            this.busServiceModel.LocationForAddress_Completed -= new EventHandler<EventArgs.LocationForAddressEventArgs>(busServiceModel_LocationForAddress_Completed);

            this.appDataModel.Favorites_Changed -= new EventHandler<EventArgs.FavoritesChangedEventArgs>(appDataModel_Favorites_Changed);
            this.appDataModel.Recents_Changed -= new EventHandler<EventArgs.FavoritesChangedEventArgs>(appDataModel_Recents_Changed);

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

                    viewModel.UIAction(() => viewModel.RoutesForLocation.Clear());
                    
                    int count = 0;
                    foreach (Route route in e.routes)
                    {
                        if (count > viewModel.maxRoutes)
                        {
                            break;
                        }

                        Route currentRoute = route;
                        viewModel.UIAction(() => viewModel.RoutesForLocation.Add(currentRoute));
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

        void busServiceModel_CombinedInfoForLocation_Completed(object sender, EventArgs.CombinedInfoForLocationEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                UIAction(() => 
                {
                    lock (stopsForLocationLock)
                    {
                        StopsForLocation.Clear();
                    }
                });
                UIAction(() =>
                {
                    lock (routesForLocationLock)
                    {
                        RoutesForLocation.Clear();
                    }
                });

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
                    stopsCache.Put(currentStop.id, currentStop);
                    UIAction(() => 
                    {
                        lock (stopsForLocationLock)
                        {
                            StopsForLocation.Add(currentStop);
                        }
                    });
                    stopCount++;
                }

                int routeCount = 0;
                foreach (Route route in e.routes)
                {
                    if (routeCount > maxRoutes)
                    {
                        break;
                    }

                    Route currentRoute = route;
                    routesCache.Put(currentRoute.id, currentRoute);
                    UIAction(() =>
                    {
                        lock (routesForLocationLock)
                        {
                            RoutesForLocation.Add(currentRoute);
                        }
                    });
                    routeCount++;
                }
            }
            else
            {
                ErrorOccured(this, e.error);
            }

            operationTracker.DoneWithOperation("CombinedInfoForLocation");
        }

        void busServiceModel_LocationForAddress_Completed(object sender, EventArgs.LocationForAddressEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                LoadInfoForLocation(1000);
            }
            else
            {
                ErrorOccured(this, e.error);
            }

            operationTracker.DoneWithOperation("SearchByAddress");
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
}
