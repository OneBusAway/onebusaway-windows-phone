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

        private ApplicationBarIconButton appbar_allroutes;

        private DispatcherTimer busLocationUpdateTimer;

        public DetailsPage()
            : base()
        {
            InitializeComponent();
            base.Initialize();

            this.Loaded += new RoutedEventHandler(DetailsPage_Loaded);

            viewModel = Resources["ViewModel"] as RouteDetailsVM;

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            appbar_favorite = ((ApplicationBarIconButton)ApplicationBar.Buttons[0]);

            if (viewModel.CurrentViewState.CurrentRouteDirection != null)
            {
                // CurrentRouteDirection isn't null so we've been called for a specific route
                // Load all of the route details
                RouteNumber.Text = viewModel.CurrentViewState.CurrentRoute.shortName;
                RouteName.Text = viewModel.CurrentViewState.CurrentRouteDirection.name;
                RouteInfo.Text = viewModel.CurrentViewState.CurrentStop.name;

                appbar_allroutes = new ApplicationBarIconButton(unfilterRoutesIcon);
                appbar_allroutes.Text = unfilterRoutesText;
                appbar_allroutes.Click += new EventHandler(appbar_allroutes_Click);
                ApplicationBar.Buttons.Add(appbar_allroutes);

                isFiltered = true;
            }
            else
            {
                // There isn't a specific route, just load up info on this bus stop
                isFiltered = false;

                RouteNumber.Text = string.Empty;
                RouteName.Text = viewModel.CurrentViewState.CurrentStop.name;
                RouteInfo.Text = string.Format("Direction: '{0}'", viewModel.CurrentViewState.CurrentStop.direction);
            }

            FavoriteRouteAndStop currentInfo = new FavoriteRouteAndStop();
            currentInfo.route = viewModel.CurrentViewState.CurrentRoute;
            currentInfo.routeStops = viewModel.CurrentViewState.CurrentRouteDirection;
            currentInfo.stop = viewModel.CurrentViewState.CurrentStop;

            isFavorite = viewModel.IsFavorite(currentInfo);
            SetFavoriteIcon();

            busLocationUpdateTimer = new DispatcherTimer();
            busLocationUpdateTimer.Interval = new TimeSpan(0, 0, 1, 0, 0); // 1 min 
            busLocationUpdateTimer.Tick += new EventHandler(busLocationUpdateTimer_Tick);
            busLocationUpdateTimer.Start();
        }

        void busLocationUpdateTimer_Tick(object sender, EventArgs e)
        {
            viewModel.LoadTripsForArrivals(viewModel.ArrivalsForStop.ToList());
        }

        void DetailsPage_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel.RegisterEventHandlers();
            viewModel.LoadArrivalsForStop(viewModel.CurrentViewState.CurrentStop, viewModel.CurrentViewState.CurrentRoute);

            if (viewModel.CurrentViewState.CurrentRouteDirection != null)
            {
                LocationCollection points = new LocationCollection();
                foreach (PolyLine pl in viewModel.CurrentViewState.CurrentRouteDirection.encodedPolylines)
                {
                    points = new LocationCollection();
                    pl.Coordinates.ForEach(delegate(Coordinate c) { points.Add(new GeoCoordinate(c.Latitude, c.Longitude)); });

                    MapPolyline shape = new MapPolyline();
                    shape.Locations = points;
                    shape.StrokeThickness = 5;
                    shape.Stroke = new SolidColorBrush((Color)Resources["PhoneAccentColor"]);


                    DetailsMap.Children.Insert(0, shape);
                }

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
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            busLocationUpdateTimer.Stop();

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
                appbar_favorite.IconUri = deleteFavoriteIcon;
                appbar_favorite.Text = deleteFavoriteText;
            }
            else
            {
                appbar_favorite.IconUri = addFavoriteIcon;
                appbar_favorite.Text = addFavoriteText;
            }
        }

        private void ArrivalsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private void DetailsMap_MapZoom(object sender, MapZoomEventArgs e)
        {
            if (DetailsMap.ZoomLevel < 15)
                BusStopsLayer.Visibility = System.Windows.Visibility.Collapsed;
            else
                BusStopsLayer.Visibility = System.Windows.Visibility.Visible;
        }
    }
}
