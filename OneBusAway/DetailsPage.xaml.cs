using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using OneBusAway.WP7.ViewModel;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using Microsoft.Phone.Controls.Maps;
using System.Windows.Data;
using System.Device.Location;
using OneBusAway.WP7.ViewModel.AppDataDataStructures;
using System.Collections.Specialized;
using Microsoft.Phone.Shell;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Diagnostics;

namespace OneBusAway.WP7.View
{
    public partial class DetailsPage : AViewPage
    {
        private RouteDetailsVM viewModel;

        private Uri unfilterRoutesIcon = new Uri("/Images/appbar.add.rest.png", UriKind.Relative);
        private Uri filterRoutesIcon = new Uri("/Images/appbar.minus.rest.png", UriKind.Relative);
        private Uri addFavoriteIcon = new Uri("/Images/appbar.favs.addto.rest.png", UriKind.Relative);
        private Uri deleteFavoriteIcon = new Uri("/Images/appbar.favs.del.rest.png", UriKind.Relative);

        private string unfilterRoutesText = "all routes";
        private string filterRoutesText = "filter routes";
        private string addFavoriteText = "add";
        private string deleteFavoriteText = "delete";

        private bool isFavorite;
        private bool isFiltered;

        private string isFilteredStateId
        {
            get
            {
                string s = string.Format("DetailsPage-IsFiltered-{0}", viewModel.CurrentViewState.CurrentStop.id);
                if (viewModel.CurrentViewState.CurrentRouteDirection != null && viewModel.CurrentViewState.CurrentRoute != null)
                {
                    s += string.Format("-{0}-{1}", viewModel.CurrentViewState.CurrentRoute.id, viewModel.CurrentViewState.CurrentRouteDirection.name);
                }

                return s;
            }
        }

        private ApplicationBarIconButton appbar_allroutes;

        private DispatcherTimer busArrivalUpdateTimer;

        public DetailsPage()
            : base()
        {
            InitializeComponent();
            base.Initialize();

            this.Loaded += new RoutedEventHandler(DetailsPage_Loaded);
            appbar_favorite = ((ApplicationBarIconButton)ApplicationBar.Buttons[0]);

            viewModel = Resources["ViewModel"] as RouteDetailsVM;
            busArrivalUpdateTimer = new DispatcherTimer();
            busArrivalUpdateTimer.Interval = new TimeSpan(0, 0, 0, 30, 0); // 30 secs 
            busArrivalUpdateTimer.Tick += new EventHandler(busArrivalUpdateTimer_Tick);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            DetailsMap.MapResolved += new EventHandler(DetailsMap_MapResolved);

            UpdateAppBar(true);
        }

        void DetailsMap_MapResolved(object sender, EventArgs e)
        {
            DetailsMap_MapZoom(this, null);
        }

        // Only want to use the state variable on the initial call
        void UpdateAppBar(bool useStateVariable)
        {
            bool addFilterButton = false;
            if (useStateVariable == true &&
                PhoneApplicationService.Current.State.ContainsKey(isFilteredStateId) == true 
                && viewModel.CurrentViewState.CurrentRouteDirection != null)
            {
                // This page was tombstoned and is now reloading, use the previous filter status.
                isFiltered = (bool)PhoneApplicationService.Current.State[isFilteredStateId];
                addFilterButton = true;
            }
            else
            {
                // No filter override, this is the first load of this details page. If
                // there is a specific route direction filter based on it and add the 
                // filter button, otherwise we are just displaying a stop, don't show
                // the filter button.
                isFiltered = viewModel.CurrentViewState.CurrentRouteDirection != null;
                addFilterButton = isFiltered;
            }

            if (addFilterButton == true)
            {
                if (appbar_allroutes == null)
                {
                    appbar_allroutes = new ApplicationBarIconButton();
                    appbar_allroutes.Click += new EventHandler(appbar_allroutes_Click);
                }

                bool localIsFiltered = isFiltered;
                Dispatcher.BeginInvoke(() =>
                    {
                        if (localIsFiltered == true)
                        {
                            appbar_allroutes.IconUri = unfilterRoutesIcon;
                            appbar_allroutes.Text = unfilterRoutesText;
                        }
                        else
                        {
                            appbar_allroutes.IconUri = filterRoutesIcon;
                            appbar_allroutes.Text = filterRoutesText;
                        }

                        if (!ApplicationBar.Buttons.Contains(appbar_allroutes))
                        {
                            // this has to be done after setting the icon
                            ApplicationBar.Buttons.Add(appbar_allroutes);
                        }
                    });
            }

            FavoriteRouteAndStop currentInfo = new FavoriteRouteAndStop();
            currentInfo.route = viewModel.CurrentViewState.CurrentRoute;
            currentInfo.routeStops = viewModel.CurrentViewState.CurrentRouteDirection;
            currentInfo.stop = viewModel.CurrentViewState.CurrentStop;

            isFavorite = viewModel.IsFavorite(currentInfo);
            SetFavoriteIcon();
        }

        void busArrivalUpdateTimer_Tick(object sender, EventArgs e)
        {
            viewModel.RefreshArrivalsForStop(viewModel.CurrentViewState.CurrentStop);
        }

        void DetailsPage_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel.RegisterEventHandlers(Dispatcher);

            if (isFiltered == true)
            {
                viewModel.LoadArrivalsForStop(viewModel.CurrentViewState.CurrentStop, viewModel.CurrentViewState.CurrentRoute);
            }
            else
            {
                viewModel.LoadArrivalsForStop(viewModel.CurrentViewState.CurrentStop, null);
            }

            // When we enter this page after tombstoning often the location won't be available when the map
            // data binding queries CurrentLocationSafe.  The center doesn't update when the property changes
            // so we need to explicitly set the center once the location is known.
            viewModel.LocationTracker.RunWhenLocationKnown(delegate(GeoCoordinate location)
                {
                    Dispatcher.BeginInvoke(() => { 
                        DetailsMap.Center = location;
 
                        //calculate distance to current stop and zoom map
                        if (viewModel.CurrentViewState.CurrentStop != null)
                        {
                            GeoCoordinate stoplocation = new GeoCoordinate(viewModel.CurrentViewState.CurrentStop.coordinate.Latitude,
                                viewModel.CurrentViewState.CurrentStop.coordinate.Longitude);
                            double radius = 2 * location.GetDistanceTo(stoplocation) * 0.009 * 0.001; // convert metres to degress and double

                            DetailsMap.SetView(new LocationRect(location, radius, radius));
                        }

                    });
                }
            );

            RecentRouteAndStop recent = new RecentRouteAndStop();
            recent.route = viewModel.CurrentViewState.CurrentRoute;
            recent.routeStops = viewModel.CurrentViewState.CurrentRouteDirection;
            recent.stop = viewModel.CurrentViewState.CurrentStop;

            viewModel.AddRecent(recent);

            busArrivalUpdateTimer.Start();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            busArrivalUpdateTimer.Stop();
            PhoneApplicationService.Current.State[isFilteredStateId] = isFiltered;

            viewModel.UnregisterEventHandlers();
        }

        private void appbar_favorite_Click(object sender, EventArgs e)
        {
            FavoriteRouteAndStop favorite = new FavoriteRouteAndStop();
            favorite.route = viewModel.CurrentViewState.CurrentRoute;
            favorite.stop = viewModel.CurrentViewState.CurrentStop;
            favorite.routeStops = viewModel.CurrentViewState.CurrentRouteDirection;

            if (isFavorite == false)
            {
                viewModel.AddFavorite(favorite);
                isFavorite = true;
            }
            else
            {
                viewModel.DeleteFavorite(favorite);
                isFavorite = false;
            }

            SetFavoriteIcon();
        }

        private void appbar_allroutes_Click(object sender, EventArgs e)
        {
            if (isFiltered == true)
            {
                viewModel.ChangeFilterForArrivals(null);
                isFiltered = false;
            }
            else
            {
                viewModel.ChangeFilterForArrivals(viewModel.CurrentViewState.CurrentRoute);
                isFiltered = true;
            }

            SetFilterRoutesIcon();
        }

        private void SetFilterRoutesIcon()
        {
            if (isFiltered == false)
            {
                appbar_allroutes.IconUri = filterRoutesIcon;
                appbar_allroutes.Text = filterRoutesText;
            }
            else
            {
                appbar_allroutes.IconUri = unfilterRoutesIcon;
                appbar_allroutes.Text = unfilterRoutesText;
            }
        }

        private void SetFavoriteIcon()
        {
            if (isFavorite == true)
            {
                Dispatcher.BeginInvoke(() =>
                    {
                        appbar_favorite.IconUri = deleteFavoriteIcon;
                        appbar_favorite.Text = deleteFavoriteText;
                    });
            }
            else
            {
                Dispatcher.BeginInvoke(() =>
                    {
                        appbar_favorite.IconUri = addFavoriteIcon;
                        appbar_favorite.Text = addFavoriteText;
                    });
            }
        }

        private void ArrivalsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 0)
            {
                ArrivalAndDeparture arrival = (ArrivalAndDeparture)e.AddedItems[0];
                viewModel.SwitchToRouteByArrival(arrival, () => UpdateAppBar(false));
            }
        }

        private void DetailsMap_MapZoom(object sender, MapZoomEventArgs e)
        {
            if (DetailsMap.ZoomLevel < 15)
                BusStopsLayer.Visibility = System.Windows.Visibility.Collapsed;
            else
                BusStopsLayer.Visibility = System.Windows.Visibility.Visible;
        }

        private void BusStopPushpin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button)
            {
                string selectedStopId = (string)((Button)sender).Tag;

                Stop selectedStop = null;
                foreach (object item in BusStopItemsControl.Items)
                {
                    Stop stop = item as Stop;
                    if (stop != null && stop.id == selectedStopId)
                    {
                        selectedStop = stop;
                        viewModel.SwitchToStop(selectedStop);

                        break;
                    }
                }

                Debug.Assert(selectedStop != null);

            }
        }

        private void appbar_refresh_Click(object sender, EventArgs e)
        {
            viewModel.LoadArrivalsForStop(viewModel.CurrentViewState.CurrentStop);
        }
    }
}
