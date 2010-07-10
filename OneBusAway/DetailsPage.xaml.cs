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

            viewModel.LoadArrivalsForStop(ViewState.CurrentStop);
            viewModel.LoadStopsForRoute(ViewState.CurrentRoute);
            //

            viewModel.ArrivalsForStop.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(ArrivalsForStop_CollectionChanged);
            viewModel.StopsForRoute.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(StopsForRoute_CollectionChanged);

            RouteName.Text = ViewState.CurrentRoute.shortName;
            RouteInfo.Text = ViewState.CurrentRoute.description;
        }

        void StopsForRoute_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            DetailsMap.Children.Clear();
            if (viewModel.StopsForRoute.Count > 0)
            {
                //PolyLine pl = new PolyLine();
                //viewModel.StopsForRoute[0].encodedPolylines.ForEach(poly => pl.coordinates.AddRange(poly.coordinates)); ;

                LocationCollection points = new LocationCollection();
                foreach (PolyLine pl in viewModel.StopsForRoute[0].encodedPolylines)
                {
                    points = new LocationCollection();
                    pl.coordinates.ForEach(delegate(Coordinate c) { points.Add(LocationExtensions.Location(c.Latitude, c.Longitude)); });

                    MapPolyline shape = new MapPolyline();
                    shape.Locations = points;
                    shape.StrokeThickness = 5;
                    shape.Stroke = new SolidColorBrush((Color)App.Current.Resources["PhoneAccentColor"]);
                    DetailsMap.Children.Add(shape);

                }

            }
        }

        void ArrivalsForStop_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //throw new NotImplementedException();
            viewModel.LoadTripsForArrivals(viewModel.ArrivalsForStop.ToList());

        }
    }
 
    public static class LocationExtensions
    {
        public static Location Location(double lat, double lon)
        {
            Location loc = new Location();
            loc.Longitude = lon;
            loc.Latitude = lat;
            return loc;
        }
    } 
}