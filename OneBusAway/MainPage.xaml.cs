using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Phone.Controls;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using OneBusAway.WP7.ViewModel;
using OneBusAway.WP7.ViewModel.AppDataDataStructures;
using Microsoft.Phone.Shell;
using System.Windows.Controls.Primitives;
using System.Threading;
using Microsoft.Phone.Controls.Maps;

namespace OneBusAway.WP7.View
{
    public partial class MainPage : PhoneApplicationPage
    {
        private MainPageVM viewModel;
        private int selectedPivotIndex = 0;
        private Popup popup;

        public MainPage()
        {
            InitializeComponent();
            //ShowLoadingSplash();

            viewModel = Resources["ViewModel"] as MainPageVM;
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

            viewModel.LoadFavorites();
            viewModel.LoadInfoForLocation(1000);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);



            viewModel.RegisterEventHandlers();

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
            viewModel.LoadInfoForLocation(1000);
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
            viewModel.LoadInfoForLocation(1000);            
        }

        private void SearchByRouteCallback(List<Route> routes, Exception error)
        {
            SearchStoryboard.Seek(TimeSpan.Zero);
            SearchStoryboard.Stop();
            this.Focus();

            if (routes.Count > 0)
            {
                viewModel.CurrentViewState.CurrentRoute = routes[0];
                viewModel.CurrentViewState.CurrentStop = viewModel.CurrentViewState.CurrentRoute.closestStop;

                NavigationService.Navigate(new Uri("/BusDirectionPage.xaml", UriKind.Relative));
            }
        }

        private void SearchInputBox_KeyUp(object sender, KeyEventArgs e)
        {
            string searchString = SearchInputBox.Text;

            if (e.Key == Key.Enter)
            {
                int routeNumber = 0;
                bool canConvert = int.TryParse(searchString, out routeNumber); //check if it's a number
                if (canConvert == true) //it's a route or stop number
                {
                    int number = int.Parse(searchString);
                    if (number < 10000)
                        viewModel.SearchByRoute(searchString, SearchByRouteCallback);
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
            if (AViewModel.LocationKnown == true)
            {
                StopsMap.Center = AViewModel.CurrentLocation;
            }
            else
            {
                StopsMap.Center = AViewModel.DefaultLocation;
            }

            StopsMap.ZoomLevel = 17;

            //Add current location and nearest stop
            MapLayer mapLayer = new MapLayer();
            StopsMap.Children.Add(mapLayer);
            
            //mapLayer.AddChild(new BusStopControl(), viewModel.CurrentViewState.CurrentStop.location);
            if (AViewModel.LocationKnown == true)
            {
                mapLayer.AddChild(new CenterControl(), AViewModel.CurrentLocation, PositionOrigin.Center);
            }
        }

        private void appbar_about_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

    }
}
