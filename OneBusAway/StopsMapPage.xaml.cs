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
using System.Device.Location;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using Microsoft.Phone.Controls.Maps;
using System.Windows.Data;
using System.Collections;
using Microsoft.Phone.Shell;

namespace OneBusAway.WP7.View
{
    /// <summary>
    /// A full screen map of stops.
    /// </summary>
    /// <remarks>
    /// Supports user interaction.  Will reload stops when moved.  Touch a stop to bring up its detail page.
    /// </remarks>
    public partial class StopsMapPage : AViewPage
    {
        private StopsMapVM viewModel;
        private bool mapHasMoved;

        internal static int minZoomLevel = 16; //below this level we don't even bother querying

#if DEBUG
        private MapLayer cacheRectLayer;
#endif

        public StopsMapPage()
            : base()
        {
            InitializeComponent();
            base.Initialize();

            viewModel = aViewModel as StopsMapVM;
            mapHasMoved = false;
            this.Loaded += new RoutedEventHandler(FullScreenMapPage_Loaded);

            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
            this.DetailsMap.TargetViewChanged += new EventHandler<MapEventArgs>(DetailsMap_TargetViewChanged);

            SupportedOrientations = SupportedPageOrientation.Portrait;

#if SCREENSHOT
            SystemTray.IsVisible = false;
#endif

#if DEBUG
            cacheRectLayer = new MapLayer();
            cacheRectLayer.SetValue(Canvas.ZIndexProperty, 20);
            this.DetailsMap.Children.Add(cacheRectLayer);
#endif

#if DEBUG
            if (Microsoft.Devices.Environment.DeviceType == Microsoft.Devices.DeviceType.Emulator)
            {
                Button zoomOutBtn = new Button();
                zoomOutBtn.Content = "Zoom Out";
                zoomOutBtn.Background = new SolidColorBrush(Colors.Transparent);
                zoomOutBtn.Foreground = new SolidColorBrush(Colors.Black);
                zoomOutBtn.BorderBrush = new SolidColorBrush(Colors.Black);
                zoomOutBtn.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                zoomOutBtn.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                zoomOutBtn.Click += new RoutedEventHandler(zoomOutBtn_Click);
                zoomOutBtn.SetValue(Canvas.ZIndexProperty, 30);
                zoomOutBtn.SetValue(Grid.RowProperty, 2);
                LayoutRoot.Children.Add(zoomOutBtn);
            }
#endif
        }

#if DEBUG
        void zoomOutBtn_Click(object sender, RoutedEventArgs e)
        {
            DetailsMap.ZoomLevel--;
        }
#endif

        void FullScreenMapPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (viewModel.CurrentViewState.CurrentSearchLocation != null)
            {
                // Using mapHasMoved prevents us from relocating the map if the user reloads this
                // page from the back stack
                if (mapHasMoved == false)
                {
                    Dispatcher.BeginInvoke(() =>
                        {
                            //DetailsMap.Center = viewModel.CurrentViewState.CurrentSearchLocation.location;
                            DetailsMap.SetView(viewModel.CurrentViewState.CurrentSearchLocation.boundingBox);
                            viewModel.LoadStopsForLocation(viewModel.CurrentViewState.CurrentSearchLocation.location);
                        }
                    );
                }
            }
            else
            {
                viewModel.LocationTracker.RunWhenLocationKnown(delegate(GeoCoordinate location)
                    {
                        Dispatcher.BeginInvoke(() =>
                            {
                                // If the user has already moved the map, don't relocate it
                                if (mapHasMoved == false)
                                {
                                    DetailsMap.Center = location;
                                }
                                    
                                viewModel.LoadStopsForLocation(location);
                            }
                            );
                    }
                );
            }
        }

        void DetailsMap_TargetViewChanged(object sender, MapEventArgs e)
        {
            GeoCoordinate center = DetailsMap.TargetCenter;
            mapHasMoved = true;

            if (DetailsMap.TargetZoomLevel >= minZoomLevel)
            {
                viewModel.LoadStopsForLocation(center);
            }

#if DEBUG
            cacheRectLayer.Children.Clear();

            int roundingLevel = 2;
            int multiplier = 3;
            double positiveOffset = (Math.Pow(.1, roundingLevel) * 0.5) / multiplier;
            double negativeOffset = (Math.Pow(.1, roundingLevel) * 0.5) / multiplier;

            double lat = Math.Round(center.Latitude * multiplier, roundingLevel) / multiplier;
            double lon = Math.Round(center.Longitude * multiplier, roundingLevel) / multiplier;

            // Round off the extra decimal places to prevent double precision issues
            // from causing multiple cache entires
            GeoCoordinate roundedLocation = new GeoCoordinate(
                Math.Round(lat, roundingLevel + 1),
                Math.Round(lon, roundingLevel + 1)
            );

            MapPolygon cacheSquare = new MapPolygon();
            cacheSquare.Locations = new LocationCollection();
            cacheSquare.Locations.Add(new GeoCoordinate(roundedLocation.Latitude + positiveOffset, roundedLocation.Longitude + positiveOffset));
            cacheSquare.Locations.Add(new GeoCoordinate(roundedLocation.Latitude - negativeOffset, roundedLocation.Longitude + positiveOffset));
            cacheSquare.Locations.Add(new GeoCoordinate(roundedLocation.Latitude - negativeOffset, roundedLocation.Longitude - negativeOffset));
            cacheSquare.Locations.Add(new GeoCoordinate(roundedLocation.Latitude + positiveOffset, roundedLocation.Longitude - negativeOffset));
            
            cacheSquare.Stroke = new SolidColorBrush(Colors.Black);
            cacheSquare.StrokeThickness = 5;

            cacheRectLayer.Children.Add(cacheSquare);

            Pushpin requestCenterPushpin = new Pushpin();
            requestCenterPushpin.Location = roundedLocation;

            cacheRectLayer.Children.Add(requestCenterPushpin);

            CenterControl deadCenter = new CenterControl();
            cacheRectLayer.AddChild(deadCenter, center, PositionOrigin.Center);
#endif
        }

        private bool LocationRectContainedBy(LocationRect outer, LocationRect inner)
        {
            // TODO: This algorithm will almost certainly fail around the equator
            if (Math.Abs(inner.North) < Math.Abs(outer.North) && 
                Math.Abs(inner.South) > Math.Abs(outer.South) &&
                Math.Abs(inner.West) < Math.Abs(outer.West) && 
                Math.Abs(inner.East) > Math.Abs(outer.East))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            viewModel.RegisterEventHandlers(Dispatcher);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            viewModel.UnregisterEventHandlers();
        }

        private void BusStopPushpin_Click(object sender, RoutedEventArgs e)
        {
            string selectedStopId = ((Button)sender).Tag as string;

            foreach (object item in StopsMapItemsControl.Items)
            {
                Stop stop = item as Stop;
                if (stop != null && stop.id == selectedStopId)
                {
                    if (selectedStopId.Equals(StopInfoBox.Tag))
                    {
                        // This is the currently selected stop, hide the popup
                        StopInfoBox.Visibility = Visibility.Collapsed;
                        StopInfoBox.Tag = null;
                    }
                    else
                    {
                        // open the popup with details about the stop
                        StopName.Text = stop.name;
                        StopRoutes.Text = (string)new StopRoutesConverter().Convert(stop, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture);
                        StopDirection.Text = (string)new StopDirectionConverter().Convert(stop, typeof(string), null, System.Globalization.CultureInfo.InvariantCulture);
                        StopInfoBox.Visibility = Visibility.Visible;
                        StopInfoBox.Location = stop.location;
                        StopInfoBox.PositionOrigin = PositionOrigin.BottomLeft;
                        StopInfoBox.Tag = stop.id;
                    }     
               
                    break;
                }
            }
        }

        private void NavigateToDetailsPage(Stop stop)
        {
            viewModel.CurrentViewState.CurrentStop = stop;
            viewModel.CurrentViewState.CurrentRoute = null;
            viewModel.CurrentViewState.CurrentRouteDirection = null;

            NavigationService.Navigate(new Uri("/DetailsPage.xaml", UriKind.Relative));
        }

        private void PopupBtn_Click(object sender, RoutedEventArgs e)
        {
            string selectedStopId = StopInfoBox.Tag as string;

            foreach (object item in StopsMapItemsControl.Items)
            {
                Stop stop = item as Stop;
                if (stop != null && stop.id == selectedStopId)
                {
                    // Hide the pop-up for when they return
                    StopInfoBox.Visibility = Visibility.Collapsed;
                    StopInfoBox.Tag = null;

                    NavigateToDetailsPage(stop);

                    break;
                }
            }
        }
    }

    public class MaxZoomConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double zoom = (double)value;
            bool visibility = (zoom < StopsMapPage.minZoomLevel);

            if (parameter != null && bool.Parse(parameter.ToString()) == false)
            {
                visibility = !visibility;
            }

            return visibility ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    
}
