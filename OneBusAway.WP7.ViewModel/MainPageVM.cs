using System;
using System.Net;
using System.Collections.ObjectModel;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using System.Device.Location;
using System.Reflection;
using System.Diagnostics;
using OneBusAway.WP7.ViewModel.AppDataDataStructures;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using OneBusAway.WP7.ViewModel.EventArgs;

namespace OneBusAway.WP7.ViewModel
{
    public class MainPageVM : AViewModel
    {

        #region Private Variables

        private int maxRoutes = 30;
        private int maxStops = 30;

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
            StopsForLocation.Clear();
            pendingOperations++;
            RunWhenLocationKnown(delegate(GeoCoordinate location)
                {
                    busServiceModel.StopsForLocation(location, radiusInMeters, -1, invalidateCache);
                });

            RoutesForLocation.Clear();
            pendingOperations++;
            RunWhenLocationKnown(delegate(GeoCoordinate location)
                {
                    busServiceModel.RoutesForLocation(location, radiusInMeters, -1, invalidateCache);
                });
        }

        public void LoadFavorites()
        {
            Favorites.Clear();
            List<FavoriteRouteAndStop> favorites = appDataModel.GetFavorites(FavoriteType.Favorite);
            // TODO: Add a way for calls to wait ~5 seconds for location to become available
            // but fallback to running without location if it times out
            if (LocationKnownStatic == true)
            {
                favorites.Sort(new FavoriteDistanceComparer(CurrentLocationStatic));
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
            pendingOperations++;

            busServiceModel.SearchForRoutes_Completed += new SearchByRouteCompleted(callback, busServiceModel, this).SearchByRoute_Completed;
            RunWhenLocationKnown(delegate(GeoCoordinate location)
                {
                    busServiceModel.SearchForRoutes(location, routeNumber);
                });
        }

        public delegate void SearchByAddress_Callback(GeoCoordinate routes, Exception error);
        public void SearchByAddress(string addressString, SearchByAddress_Callback callback)
        {
            pendingOperations++;
            busServiceModel.LocationForAddress(addressString);
        }

        public delegate void CheckForLocalTransitData_Callback(bool hasData);
        public void CheckForLocalTransitData(CheckForLocalTransitData_Callback callback)
        {
            RunWhenLocationKnown(delegate(GeoCoordinate location)
            {
                bool hasData;
                // Ensure that their current location is within ~150 miles of Seattle
                if (location.GetDistanceTo(AViewModel.DefaultLocationStatic) > 250000)
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

        public override void RegisterEventHandlers()
        {
            base.RegisterEventHandlers();

            this.busServiceModel.RoutesForLocation_Completed += new EventHandler<EventArgs.RoutesForLocationEventArgs>(busServiceModel_RoutesForLocation_Completed);
            this.busServiceModel.StopsForLocation_Completed += new EventHandler<EventArgs.StopsForLocationEventArgs>(busServiceModel_StopsForLocation_Completed);
            this.busServiceModel.LocationForAddress_Completed += new EventHandler<EventArgs.LocationForAddressEventArgs>(busServiceModel_LocationForAddress_Completed);


            this.appDataModel.Favorites_Changed += new EventHandler<EventArgs.FavoritesChangedEventArgs>(appDataModel_Favorites_Changed);
            this.appDataModel.Recents_Changed += new EventHandler<EventArgs.FavoritesChangedEventArgs>(appDataModel_Recents_Changed);
        }

        public override void UnregisterEventHandlers()
        {
            base.UnregisterEventHandlers();

            this.busServiceModel.RoutesForLocation_Completed -= new EventHandler<EventArgs.RoutesForLocationEventArgs>(busServiceModel_RoutesForLocation_Completed);
            this.busServiceModel.StopsForLocation_Completed -= new EventHandler<EventArgs.StopsForLocationEventArgs>(busServiceModel_StopsForLocation_Completed);
            this.busServiceModel.LocationForAddress_Completed -= new EventHandler<EventArgs.LocationForAddressEventArgs>(busServiceModel_LocationForAddress_Completed);


            this.appDataModel.Favorites_Changed -= new EventHandler<EventArgs.FavoritesChangedEventArgs>(appDataModel_Favorites_Changed);
            this.appDataModel.Recents_Changed -= new EventHandler<EventArgs.FavoritesChangedEventArgs>(appDataModel_Recents_Changed);

            // Reset loading to 0 since event handlers have been unregistered
            pendingOperations = 0;
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
                    viewModel.RoutesForLocation.Clear();

                    int count = 0;
                    foreach (Route route in e.routes)
                    {
                        if (count > viewModel.maxRoutes)
                        {
                            break;
                        }

                        viewModel.RoutesForLocation.Add(route);
                        count++;
                    }
                }
                
                callback(e.routes, e.error);
                busServiceModel.SearchForRoutes_Completed -= this.SearchByRoute_Completed;
                
                viewModel.pendingOperations--;
            }
        }

        void busServiceModel_StopsForLocation_Completed(object sender, EventArgs.StopsForLocationEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                e.stops.Sort(new StopDistanceComparer(e.location));
                StopsForLocation.Clear();

                int count = 0;
                foreach(Stop stop in e.stops)
                {
                    if (count > maxStops)
                    {
                        break;
                    }

                    StopsForLocation.Add(stop);
                    count++;
                }
            }

            pendingOperations--;
        }

        void busServiceModel_RoutesForLocation_Completed(object sender, EventArgs.RoutesForLocationEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                e.routes.Sort(new RouteDistanceComparer(e.location));
                RoutesForLocation.Clear();

                int count = 0;
                foreach (Route route in e.routes)
                {
                    if (count > maxRoutes)
                    {
                        break;
                    }

                    RoutesForLocation.Add(route);
                    count++;
                }
            }

            pendingOperations--;
        }

        void busServiceModel_LocationForAddress_Completed(object sender, EventArgs.LocationForAddressEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                LoadInfoForLocation(1000);
            }

            pendingOperations--;
        }

        void appDataModel_Favorites_Changed(object sender, EventArgs.FavoritesChangedEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                // Can't sort here right now because we don't have access to the current location
                //e.newFavorites.Sort(new FavoriteDistanceComparer());
                Favorites.Clear();
                e.newFavorites.Sort(new RecentLastAccessComparer());
                e.newFavorites.ForEach(favorite => Favorites.Add(favorite));
            }
        }

        void appDataModel_Recents_Changed(object sender, EventArgs.FavoritesChangedEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                Recents.Clear();
                e.newFavorites.ForEach(recent => Recents.Add(recent));
            }
        }

        #endregion

    }
}
