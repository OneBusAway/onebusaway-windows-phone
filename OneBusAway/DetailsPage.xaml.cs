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

namespace OneBusAway.WP7.View
{
    public partial class DetailsPage : PhoneApplicationPage
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

        public DetailsPage()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(DetailsPage_Loaded);

            viewModel = Resources["ViewModel"] as RouteDetailsVM;

            viewModel.ArrivalsForStop.CollectionChanged += new NotifyCollectionChangedEventHandler(ArrivalsForStop_CollectionChanged);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            viewModel.RegisterEventHandlers();


            appbar_allroutes = ((ApplicationBarIconButton)ApplicationBar.Buttons[1]);
            appbar_favorite = ((ApplicationBarIconButton)ApplicationBar.Buttons[0]);

            viewModel.LoadArrivalsForStop(ViewState.CurrentStop, ViewState.CurrentRoute);

            DetailsMap.Children.Clear();
            DetailsMap.Center = MainPage.CurrentLocation;
            DetailsMap.ZoomLevel = 15;

            

            if (ViewState.CurrentRouteDirection != null)
            {
                // CurrentRouteDirection isn't null so we've been called for a specific route
                // Load all of the route details
                RouteNumber.Text = ViewState.CurrentRoute.shortName;
                RouteName.Text = ViewState.CurrentRouteDirection.name;
                RouteInfo.Text = ViewState.CurrentStop.name;

                isFiltered = true;

                LocationCollection points = new LocationCollection();
                foreach (PolyLine pl in ViewState.CurrentRouteDirection.encodedPolylines)
                {
                    points = new LocationCollection();
                    pl.coordinates.ForEach(delegate(Coordinate c) { points.Add(new GeoCoordinate(c.Latitude, c.Longitude)); });

                    MapPolyline shape = new MapPolyline();
                    shape.Locations = points;
                    shape.StrokeThickness = 5;
                    shape.Stroke = new SolidColorBrush((Color)Resources["PhoneAccentColor"]);
                    DetailsMap.Children.Add(shape);
                }
            }
            else
            {
                // There isn't a specific route, just load up info on this bus stop
                isFiltered = false;

                RouteNumber.Text = string.Empty;
                RouteName.Text = ViewState.CurrentStop.name;
                RouteInfo.Text = string.Format("Direction: '{0}'", ViewState.CurrentStop.direction);
            }

            //Add current location and nearest stop
            MapLayer mapLayer = new MapLayer();
            DetailsMap.Children.Add(mapLayer);
            mapLayer.AddChild(new BusStopControl(), ViewState.CurrentStop.location);
            mapLayer.AddChild(new CenterControl(), MainPage.CurrentLocation);

            SetFilterRoutesIcon();

            FavoriteRouteAndStop currentInfo = new FavoriteRouteAndStop();
            currentInfo.route = ViewState.CurrentRoute;
            currentInfo.routeStops = ViewState.CurrentRouteDirection;
            currentInfo.stop = ViewState.CurrentStop;

            isFavorite = viewModel.IsFavorite(currentInfo);
            SetFavoriteIcon();
        }

        void DetailsPage_Loaded(object sender, RoutedEventArgs e)
        {
            RecentRouteAndStop recent = new RecentRouteAndStop();
            recent.route = ViewState.CurrentRoute;
            recent.routeStops = ViewState.CurrentRouteDirection;
            recent.stop = ViewState.CurrentStop;

            viewModel.AddRecent(recent);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            viewModel.UnregisterEventHandlers();
        }

        void ArrivalsForStop_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // This call is disabled because we don't use this data currently
            //viewModel.LoadTripsForArrivals(viewModel.ArrivalsForStop.ToList());
        }

        private void appbar_favorite_Click(object sender, EventArgs e)
        {
            FavoriteRouteAndStop favorite = new FavoriteRouteAndStop();
            favorite.route = ViewState.CurrentRoute;
            favorite.stop = ViewState.CurrentStop;
            favorite.routeStops = ViewState.CurrentRouteDirection;

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
                viewModel.ChangeFilterForArrivals(ViewState.CurrentRoute);
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
    }
}
