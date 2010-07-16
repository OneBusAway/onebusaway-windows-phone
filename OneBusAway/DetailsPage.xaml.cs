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
using OneBusAway.WP7.ViewModel.DataStructures;
using Microsoft.Phone.Controls.Maps;
using System.Windows.Data;
using System.Device.Location;

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

            ViewState.CurrentStop = ViewState.CurrentRouteDirection.stops[0];
            foreach (Stop stop in ViewState.CurrentRouteDirection.stops)
            {
                if (ViewState.CurrentStop.CalculateDistanceInMiles(MainPage.CurrentLocation) > stop.CalculateDistanceInMiles(MainPage.CurrentLocation))
                {
                    ViewState.CurrentStop = stop;
                }
            }

            viewModel.ArrivalsForStop.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(ArrivalsForStop_CollectionChanged);

            viewModel.LoadArrivalsForStop(ViewState.CurrentStop);

            RouteName.Text = string.Format("{0}: {1}", ViewState.CurrentRoute.shortName, ViewState.CurrentRouteDirection.name);
            RouteInfo.Text = ViewState.CurrentStop.name;

            DetailsMap.Children.Clear();
            //PolyLine pl = new PolyLine();
            //viewModel.StopsForRoute[0].encodedPolylines.ForEach(poly => pl.coordinates.AddRange(poly.coordinates)); ;

            LocationCollection points = new LocationCollection();
            foreach (PolyLine pl in ViewState.CurrentRouteDirection.encodedPolylines)
            {
                points = new LocationCollection();
                pl.coordinates.ForEach(delegate(Coordinate c) { points.Add(new GeoCoordinate(c.Latitude, c.Longitude)); });

                MapPolyline shape = new MapPolyline();
                shape.Locations = points;
                shape.StrokeThickness = 5;
                shape.Stroke = new SolidColorBrush((Color)App.Current.Resources["PhoneAccentColor"]);
                DetailsMap.Children.Add(shape);
            }
        }

        void ArrivalsForStop_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            viewModel.LoadTripsForArrivals(viewModel.ArrivalsForStop.ToList());
        }
    }
}