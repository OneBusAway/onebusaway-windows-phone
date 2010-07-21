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

namespace OneBusAway.WP7.View
{
    public partial class BusDirectionPage : PhoneApplicationPage
    {
        private BusDirectionVM viewModel;

        public BusDirectionPage()
        {
            InitializeComponent();

            viewModel = Resources["ViewModel"] as BusDirectionVM;

            this.Loaded += new RoutedEventHandler(BusDirectionPage_Loaded);
        }

        void BusDirectionPage_Loaded(object sender, RoutedEventArgs e)
        {
            ProgressBar.Visibility = Visibility.Visible;

            viewModel.LoadRouteDirections(ViewState.CurrentRoute);
            viewModel.RouteDirections.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(CollectionChanged);

        }
        void CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                ProgressBar.Visibility = Visibility.Collapsed;
            }
        }


        private void BusDirectionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                ViewState.CurrentRouteDirection = (RouteStops)e.AddedItems[0];

                ViewState.CurrentStop = ViewState.CurrentRouteDirection.stops[0];
                foreach (Stop stop in ViewState.CurrentRouteDirection.stops)
                {
                    if (ViewState.CurrentStop.CalculateDistanceInMiles(MainPage.CurrentLocation) > stop.CalculateDistanceInMiles(MainPage.CurrentLocation))
                    {
                        ViewState.CurrentStop = stop;
                    }
                }

                NavigationService.Navigate(new Uri("/DetailsPage.xaml", UriKind.Relative));
            }
        }

    }
}