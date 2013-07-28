/* Copyright 2013 Shawn Henry, Rob Smith, and Michael Friedman
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
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
using System.Windows.Navigation;
using System.Collections.Specialized;
using System.Device.Location;
using Microsoft.Phone.Shell;

namespace OneBusAway.WP7.View
{
    public partial class BusDirectionPage : AViewPage
    {
        private BusDirectionVM viewModel;
        private bool informationLoaded;

        public BusDirectionPage()
            : base()
        {
            InitializeComponent();
            base.Initialize();

            viewModel = Resources["ViewModel"] as BusDirectionVM;

            informationLoaded = false;

#if SCREENSHOT
            SystemTray.IsVisible = false;
#endif
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            viewModel.RegisterEventHandlers(Dispatcher);

            // This prevents us from refreshing the data when someone comes back to this page
            // since the bus directions aren't going to change
            if (informationLoaded == false)
            {
                viewModel.LoadRouteDirections(viewModel.CurrentViewState.CurrentRoutes);
                informationLoaded = true;
            }
            else
            {
                // If the information was already loaded clear the selection
                // This way if they navigated back to this page one entry
                // won't already be selected
                BusDirectionListBox.SelectedIndex = -1;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            viewModel.UnregisterEventHandlers();
        }

        private void BusDirectionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                viewModel.CurrentViewState.CurrentRoute = ((RouteStops)e.AddedItems[0]).route;
                viewModel.CurrentViewState.CurrentRouteDirection = (RouteStops)e.AddedItems[0];

                viewModel.CurrentViewState.CurrentStop = viewModel.CurrentViewState.CurrentRouteDirection.stops[0];
                foreach (Stop stop in viewModel.CurrentViewState.CurrentRouteDirection.stops)
                {
                    // TODO: Make this call location-unknown safe.  The CurrentLocation could be unknown
                    // at this point during a tombstoning scenario
                    GeoCoordinate location = viewModel.LocationTracker.CurrentLocation;

                    if (viewModel.CurrentViewState.CurrentStop.CalculateDistanceInMiles(location) > stop.CalculateDistanceInMiles(location))
                    {
                        viewModel.CurrentViewState.CurrentStop = stop;
                    }
                }

                NavigationService.Navigate(new Uri("/DetailsPage.xaml", UriKind.Relative));
            }
        }

    }
}