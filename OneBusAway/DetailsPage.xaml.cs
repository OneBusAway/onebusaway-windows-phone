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

        public DetailsPage()
        {
            InitializeComponent();

            viewModel = Resources["ViewModel"] as RouteDetailsVM;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            viewModel.ArrivalsForStop.CollectionChanged += new NotifyCollectionChangedEventHandler(ArrivalsForStop_CollectionChanged);

            viewModel.LoadArrivalsForStop(ViewState.CurrentStop);

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

                RouteName.Text = ViewState.CurrentStop.name;
                RouteInfo.Text = string.Format("Direction: '{0}'", ViewState.CurrentStop.direction);
            }
        }

        void ArrivalsForStop_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            viewModel.LoadTripsForArrivals(viewModel.ArrivalsForStop.ToList());
        }

        private void appbar_center_Click(object sender, EventArgs e)
        {
            FavoriteRouteAndStop favorite = new FavoriteRouteAndStop();
            favorite.route = ViewState.CurrentRoute;
            favorite.stop = ViewState.CurrentStop;
            favorite.routeStops = ViewState.CurrentRouteDirection;

            viewModel.AddFavorite(favorite);
        }
    }
}
