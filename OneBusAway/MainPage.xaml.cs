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
using System.Device.Location;
using Microsoft.Devices;
using System.Windows.Data;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using OneBusAway.WP7.ViewModel;
using OneBusAway.WP7.ViewModel.AppDataDataStructures;
using Microsoft.Phone.Shell;
using System.Windows.Controls.Primitives;
using System.Threading;
using System.ComponentModel;
using Microsoft.Phone.Controls.Maps;

namespace OneBusAway.WP7.View
{
    public partial class MainPage : PhoneApplicationPage
    {
        private static GeoCoordinateWatcher locationWatcher = new GeoCoordinateWatcher();

        private MainPageVM viewModel;
        private bool informationLoaded;
        private int selectedPivotIndex = 0;
        private Popup popup;

        public static GeoCoordinate CurrentLocation
        {
            get
            {
                if (Microsoft.Devices.Environment.DeviceType == DeviceType.Emulator)
                {
                    return new GeoCoordinate(47.67652682262796, -122.3183012008667); // Home
                }

                if (locationWatcher.Status == GeoPositionStatus.Ready)
                {
                    return locationWatcher.Position.Location;
                }

                // default to downtown Seattle
                return new GeoCoordinate(47.644385, -122.135353);
            }
        }

        private static GeoPositionStatus LocationStatus
        {
            get
            {
                if (Microsoft.Devices.Environment.DeviceType == DeviceType.Emulator)
                {
                    return GeoPositionStatus.Ready;
                }

                return locationWatcher.Status;
            }
        }

        public MainPage()
        {
            InitializeComponent();
            ShowLoadingSplash();


            viewModel = Resources["ViewModel"] as MainPageVM;
            informationLoaded = false;

            locationWatcher.Start();
            locationWatcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(locationWatcher_StatusChanged);

            this.Loaded += new RoutedEventHandler(MainPage_Loaded);

            SupportedOrientations = SupportedPageOrientation.Portrait;
        }

        private void ShowLoadingSplash() 
        { 
            this.popup = new Popup(); 
            this.popup.Child = new PopupSplash(); 
            this.popup.IsOpen = true;

            Timer timer = new Timer(HideLoadingSplash, null, 500, Timeout.Infinite);
        }

        private void HideLoadingSplash(Object stateInfo)
        {
            this.Dispatcher.BeginInvoke(() => { HideLoadingSplash(); });
        }

        private void HideLoadingSplash()
        {
            this.popup.IsOpen = false;
            ApplicationBar.IsVisible = true;
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            PC.SelectedIndex = selectedPivotIndex;
        }

        void locationWatcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            if (LocationStatus == GeoPositionStatus.Ready && informationLoaded == false)
            {
                viewModel.LoadInfoForLocation(CurrentLocation, 1000);
                informationLoaded = true;
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            viewModel.RegisterEventHandlers();

            // Ensure the location changed method is called each time this page is loaded
            informationLoaded = false;
            locationWatcher_StatusChanged(this, new GeoPositionStatusChangedEventArgs(LocationStatus));
            viewModel.LoadFavorites(CurrentLocation);

            if (PhoneApplicationService.Current.State.ContainsKey("MainPageSelectedPivot") == true)
            {
                selectedPivotIndex = Convert.ToInt32(PhoneApplicationService.Current.State["MainPageSelectedPivot"]);
            }
            else
            {
                selectedPivotIndex = 0;
            }

            ZoomMap();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            viewModel.UnregisterEventHandlers();
        }

        private void RoutesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                viewModel.CurrentViewState.CurrentRoute = (Route)e.AddedItems[0];
                viewModel.CurrentViewState.CurrentStop = viewModel.CurrentViewState.CurrentRoute.closestStop;

                NavigationService.Navigate(new Uri("/BusDirectionPage.xaml", UriKind.Relative));
            }
        }

        private void appbar_refresh_Click(object sender, EventArgs e)
        {
            viewModel.LoadInfoForLocation(CurrentLocation, 1000);
        }

        private void FavoritesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                FavoriteRouteAndStop favorite = (FavoriteRouteAndStop)e.AddedItems[0];
                viewModel.CurrentViewState.CurrentRoute = favorite.route;
                viewModel.CurrentViewState.CurrentStop = favorite.stop;
                viewModel.CurrentViewState.CurrentRouteDirection = favorite.routeStops;

                NavigationService.Navigate(new Uri("/DetailsPage.xaml", UriKind.Relative));
            }
        }

        private void RecentsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FavoritesListBox_SelectionChanged(sender, e);
        }

        private void StopsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                viewModel.CurrentViewState.CurrentRoute = null;
                viewModel.CurrentViewState.CurrentRouteDirection = null;
                viewModel.CurrentViewState.CurrentStop = (Stop)e.AddedItems[0];

                NavigationService.Navigate(new Uri("/DetailsPage.xaml", UriKind.Relative));
            }
        }

        private void PC_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Pivot pivot = sender as Pivot;
            PhoneApplicationService.Current.State["MainPageSelectedPivot"] = pivot.SelectedIndex.ToString();
        }

        private void appbar_search_Click(object sender, EventArgs e)
        {
            if (SearchPanel.Opacity == 0)
            {
                SearchStoryboard.Begin();
                SearchInputBox.Focus();
            }
            else
            {
                SearchStoryboard.Seek(TimeSpan.Zero);
                SearchStoryboard.Stop();
                this.Focus();
            }
        }

        private void SearchInputBox_LostFocus(object sender, RoutedEventArgs e)
        {
            viewModel.LoadInfoForLocation(CurrentLocation, 1000);            
        }

        private void SearchByRouteCallback(List<Route> routes, Exception error)
        {

        }

        private void SearchInputBox_KeyUp(object sender, KeyEventArgs e)
        {
            string searchString = SearchInputBox.Text;

            if (e.Key == Key.Enter)
            {
                int routeNumber = 0;
                bool canConvert = int.TryParse(searchString, out routeNumber); //check if it's a number
                if (canConvert == true) //it's a route number
                {
                    viewModel.SearchByRoute(searchString, CurrentLocation, SearchByRouteCallback);
                }
                else
                {
                    //try geocoding
                    viewModel.SearchByAddress(searchString, null);
                }

            }
        }

        private void ZoomMap()
        {
            StopsMap.Center = MainPage.CurrentLocation;
            StopsMap.ZoomLevel = 17;

            //Add current location and nearest stop
            MapLayer mapLayer = new MapLayer();
            StopsMap.Children.Add(mapLayer);
            
            //mapLayer.AddChild(new BusStopControl(), viewModel.CurrentViewState.CurrentStop.location);
            mapLayer.AddChild(new CenterControl(), MainPage.CurrentLocation);
        }

    }
}
