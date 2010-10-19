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
            this.DetailsMap.MouseLeftButtonUp += new MouseButtonEventHandler(DetailsMap_MouseLeftButtonUp);

            SupportedOrientations = SupportedPageOrientation.Portrait;
        }

        // This method will kick off the initial load of bus stops, and
        // then unregister itself
        void DetailsMap_MapResolved(object sender, EventArgs e)
        {
            this.DetailsMap.MapResolved -= new EventHandler(DetailsMap_MapResolved);
            DetailsMap_MouseLeftButtonUp(this, null);
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

        void DetailsMap_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
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

                if (BusStopsLayer.Visibility == Visibility.Visible)
                {
                    if (LocationRectContainedBy(previousMapView, DetailsMap.BoundingRectangle) == false)
                    {
                        viewModel.LoadStopsForLocation(
                            new GeoCoordinate() { Latitude = DetailsMap.BoundingRectangle.North, Longitude = DetailsMap.BoundingRectangle.West },
                            new GeoCoordinate() { Latitude = DetailsMap.BoundingRectangle.South, Longitude = DetailsMap.BoundingRectangle.East }
                            );
                    }

                    previousMapView = DetailsMap.BoundingRectangle;
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
            if (DetailsMap.ZoomLevel < 16)
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
            viewModel.RegisterEventHandlers();
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
                    viewModel.CurrentViewState.CurrentStop = stop;
                    viewModel.CurrentViewState.CurrentRoute = null;
                    viewModel.CurrentViewState.CurrentRouteDirection = null;

                    NavigationService.Navigate(new Uri("/DetailsPage.xaml", UriKind.Relative));

                    break;
                }
            }
        }
    }
}