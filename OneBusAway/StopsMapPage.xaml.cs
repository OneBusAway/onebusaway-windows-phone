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
            this.DetailsMap.MapPan += new EventHandler<MapDragEventArgs>(DetailsMap_MapPan);

            SupportedOrientations = SupportedPageOrientation.Portrait;

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
            double newZoomLevel = DetailsMap.ZoomLevel - 1;
            DetailsMap.ZoomLevel = newZoomLevel;
        }
#endif

        // This method will kick off the initial load of bus stops, and
        // then unregister itself
        void DetailsMap_MapResolved(object sender, EventArgs e)
        {
            this.DetailsMap.MapResolved -= new EventHandler(DetailsMap_MapResolved);
            DetailsMap_MapPan(this, null);
        }

        void FullScreenMapPage_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel.LocationTracker.RunWhenLocationKnown(delegate(GeoCoordinate location)
                {
                    Dispatcher.BeginInvoke(() =>
                        {
                            // If the user has already moved the map, don't relocate it
                            if (mapHasMoved == false)
                            {
                                // Now that we know where we are register the MapResolved event to load
                                // the initial stops
                                this.DetailsMap.MapResolved += new EventHandler(DetailsMap_MapResolved);
                                DetailsMap.Center = location;
                            }
                        }
                        );
                }
            );
        }

        void DetailsMap_MapPan(object sender, MapDragEventArgs e)
        {
            GeoCoordinate center = DetailsMap.Center;
            mapHasMoved = true;

            if (DetailsMap.ZoomLevel >= minZoomLevel)
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
            viewModel.RegisterEventHandlers(Dispatcher);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
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
                        // popup is already open for this stop.  navigate to the details page
                        StopInfoBox.Visibility = Visibility.Collapsed;
                        StopInfoBox.Tag = null;

                        NavigateToDetailsPage(stop);
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
