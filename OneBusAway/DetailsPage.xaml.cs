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

namespace OneBusAway.WP7.View
{
    public partial class DetailsPage : PhoneApplicationPage
    {
        private RouteDetailsVM viewModel;

        private Uri unfilterRoutesIcon = new Uri("/Images/appbar.add.rest.png", UriKind.Relative);
        private Uri filterRoutesIcon = new Uri("/Images/appbar.minus.rest.png", UriKind.Relative);
        private bool isFavorite;

        public DetailsPage()
        {
            InitializeComponent();

            viewModel = Resources["ViewModel"] as RouteDetailsVM;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            viewModel.ArrivalsForStop.CollectionChanged += new NotifyCollectionChangedEventHandler(ArrivalsForStop_CollectionChanged);

            viewModel.LoadArrivalsForStop(ViewState.CurrentStop, ViewState.CurrentRoute, ViewState.CurrentRouteDirection);

            DetailsMap.Children.Clear();
            DetailsMap.Center = MainPage.CurrentLocation;
            DetailsMap.ZoomLevel = 15;

            // TODO: Fix the my location icon
            Ellipse yellow = new Ellipse();
            yellow.Height = 7;
            yellow.Width = 7;
            SolidColorBrush y = new SolidColorBrush();
            y.Color = Colors.Yellow;
            yellow.Fill = y;

            Pushpin myLocationPin = new Pushpin();
            myLocationPin.Height = 18;
            myLocationPin.Width = 18;
            RotateTransform rotate = new RotateTransform();
            rotate.Angle = 45;
            myLocationPin.RenderTransform = rotate;
            myLocationPin.Location = MainPage.CurrentLocation;

            DetailsMap.Children.Add(myLocationPin);

            if (ViewState.CurrentRouteDirection != null)
            {
                // CurrentRouteDirection isn't null so we've been called for a specific route
                // Load all of the route details
                RouteNumber.Text = ViewState.CurrentRoute.shortName;
                RouteName.Text = ViewState.CurrentRouteDirection.name;
                RouteInfo.Text = ViewState.CurrentStop.name;

                //appbar_allroutes.IconUri = unfilterRoutesIcon;

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

                //appbar_allroutes.IconUri = filterRoutesIcon;

                RouteName.Text = ViewState.CurrentStop.name;
                RouteInfo.Text = string.Format("Direction: '{0}'", ViewState.CurrentStop.direction);
            }

            FavoriteRouteAndStop currentInfo = new FavoriteRouteAndStop();
            currentInfo.route = ViewState.CurrentRoute;
            currentInfo.routeStops = ViewState.CurrentRouteDirection;
            currentInfo.stop = ViewState.CurrentStop;

            isFavorite = viewModel.IsFavorite(currentInfo);
        }

        void ArrivalsForStop_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            viewModel.LoadTripsForArrivals(viewModel.ArrivalsForStop.ToList());
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
        }

        private void appbar_allroutes_Click(object sender, EventArgs e)
        {
            // TODO: Figure out why appbar_allroutes is always null
            // so I can re-enable this code

            //if (appbar_allroutes.IconUri == unfilterRoutesIcon)
            //{
                viewModel.ChangeFilterForArrivals(null, null);
            //    appbar_allroutes.IconUri = filterRoutesIcon;
            //}
            //else if (appbar_allroutes.IconUri == filterRoutesIcon)
            //{
            //    viewModel.ChangeFilterForArrivals(ViewState.CurrentRoute, ViewState.CurrentRouteDirection);
            //    appbar_allroutes.IconUri = unfilterRoutesIcon;
            //}
            //else
            //{
            //    throw new NotImplementedException();
            //}
        }
    }
}
