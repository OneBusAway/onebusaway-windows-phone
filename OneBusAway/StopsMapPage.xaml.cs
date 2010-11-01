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
        private LocationRect previousMapView;
        private Object mapHasMovedLock;
        private bool mapHasMoved;

        internal static int minZoomLevel = 15; //below this level we don't even bother querying

        public StopsMapPage()
            : base()
        {
            InitializeComponent();
            base.Initialize();

            viewModel = aViewModel as StopsMapVM;
            previousMapView = DetailsMap.BoundingRectangle;
            mapHasMoved = false;
            mapHasMovedLock = new Object();
            this.Loaded += new RoutedEventHandler(FullScreenMapPage_Loaded);

            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
            this.DetailsMap.MapZoom += new EventHandler<MapZoomEventArgs>(DetailsMap_MapZoom);
            this.DetailsMap.MapPan += new EventHandler<MapDragEventArgs>(DetailsMap_MapPan);
            this.DetailsMap.ViewChangeEnd += new EventHandler<MapEventArgs>(DetailsMap_ViewChangeEnd);

            SupportedOrientations = SupportedPageOrientation.Portrait;
        }

        // This method will kick off the initial load of bus stops, and
        // then unregister itself
        void DetailsMap_MapResolved(object sender, EventArgs e)
        {
            this.DetailsMap.MapResolved -= new EventHandler(DetailsMap_MapResolved);
            DetailsMap_ViewChangeEnd(this, null);
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
                                mapHasMoved = true;
                                this.DetailsMap.MapResolved += new EventHandler(DetailsMap_MapResolved);
                                DetailsMap.Center = location;
                            }
                        }
                        );
                }
            );
        }

        void DetailsMap_ViewChangeEnd(object sender, MapEventArgs e)
        {
                lock (mapHasMovedLock)
                {
                    if (mapHasMoved == false)
                    {
                        return;
                    }
                    else
                    {
                        mapHasMoved = false;
                    }
                }

                if (DetailsMap.ZoomLevel > minZoomLevel )
                {
                    // TODO: Fix this logic, I think it has threading issues that can cause it to break
                    // If the layer isn't visible, that means there were too many stops found by our previous query
                    // so we should always fire a new query, even if they are inside of the previous rectangle
                    //if (BusStopsLayer.Visibility == Visibility.Collapsed || 
                    //    LocationRectContainedBy(previousMapView, DetailsMap.BoundingRectangle) == false)   
                    {
                        viewModel.LoadStopsForLocation(
                            new GeoCoordinate() { Latitude = DetailsMap.BoundingRectangle.North, Longitude = DetailsMap.BoundingRectangle.West },
                            new GeoCoordinate() { Latitude = DetailsMap.BoundingRectangle.South, Longitude = DetailsMap.BoundingRectangle.East }
                            );

                        previousMapView = DetailsMap.BoundingRectangle;
                    }
                }
            
        }

        void DetailsMap_MapPan(object sender, MapDragEventArgs e)
        {
            lock (mapHasMovedLock)
            {
                mapHasMoved = true;
            }
        }

        void DetailsMap_MapZoom(object sender, MapZoomEventArgs e)
        {
            if (DetailsMap.ZoomLevel < minZoomLevel)
            {
                BusStopsLayer.Visibility = Visibility.Collapsed;
            }
            else
            {
                BusStopsLayer.Visibility = Visibility.Visible;
                lock (mapHasMovedLock)
                {
                    mapHasMoved = true;
                }
            }
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
                        viewModel.CurrentViewState.CurrentStop = stop;
                        viewModel.CurrentViewState.CurrentRoute = null;
                        viewModel.CurrentViewState.CurrentRouteDirection = null;
                        NavigationService.Navigate(new Uri("/DetailsPage.xaml", UriKind.Relative));
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

        private void btnClose_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            StopInfoBox.Visibility = Visibility.Collapsed;
            StopInfoBox.Tag = null;
        }
    }

    public class MaxStopsConverter : IValueConverter
    {
        private const int maxNumberOfStop = 80; //maximum number of stops we show at a time

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int count = (int)value;
            bool visibility = (count < maxNumberOfStop) ;

            if (parameter != null) 
                visibility = !visibility;

            return visibility ? Visibility.Visible : Visibility.Collapsed;

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MaxZoomConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double zoom = (double)value;
            bool visibility = (zoom <= StopsMapPage.minZoomLevel);

            return visibility ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    
}