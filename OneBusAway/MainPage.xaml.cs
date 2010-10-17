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
using System.Windows.Data;
using System.Windows.Media;
using System.Net;
using System.Device.Location;

namespace OneBusAway.WP7.View
{
    public partial class MainPage : AViewPage
    {
        private int selectedPivotIndex = 0;
        private MainPageVM viewModel;
        private bool firstLoad;

        public MainPage()
            : base()
        {
            InitializeComponent();
            base.Initialize();

            viewModel = aViewModel as MainPageVM;
            firstLoad = true;

            this.Loaded += new RoutedEventHandler(MainPage_Loaded);

            SupportedOrientations = SupportedPageOrientation.Portrait;
        }

        private Popup popup;

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
            //ApplicationBar.IsVisible = true;
        }


        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel.RegisterEventHandlers();
            viewModel.LoadFavorites();
            viewModel.LoadInfoForLocation(1000);

            viewModel.CheckForLocalTransitData(delegate(bool hasData)
            {
                if (hasData == false)
                {
                    MessageBox.Show(
                        "Currently the OneBusAway service only supports Seattle and the surrounding counties. " +
                        "Many functions of this app will not work in your current location."
                        );
                }
            });

            viewModel.LocationTracker.RunWhenLocationKnown(delegate(GeoCoordinate location)
                {
                    Dispatcher.BeginInvoke(() => StopsMap.Center = location);
                }
            );
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (firstLoad == true && PhoneApplicationService.Current.State.ContainsKey("MainPageSelectedPivot") == true)
            {
                PC.SelectedIndex = Convert.ToInt32(PhoneApplicationService.Current.State["MainPageSelectedPivot"]);
                firstLoad = false;
            }

            // We refresh this info every load so clear the lists now
            // to avoid a flicker as the page comes back
            viewModel.RoutesForLocation.Clear();
            viewModel.StopsForLocation.Clear();
            viewModel.Favorites.Clear();
            viewModel.Recents.Clear();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            PhoneApplicationService.Current.State["MainPageSelectedPivot"] = PC.SelectedIndex;

            viewModel.UnregisterEventHandlers();
        }

        private void RoutesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                viewModel.CurrentViewState.CurrentRoutes = new List<Route>() { (Route)e.AddedItems[0] };

                NavigationService.Navigate(new Uri("/BusDirectionPage.xaml", UriKind.Relative));
            }
        }

        private void appbar_refresh_Click(object sender, EventArgs e)
        {
            viewModel.LoadInfoForLocation(1000, true);
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
                SearchInputBox.SelectAll();
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

            SearchStoryboard.Seek(TimeSpan.Zero);
            SearchStoryboard.Stop();
            this.Focus();
        }

        private void SearchByRouteCallback(List<Route> routes, Exception error)
        {
            SearchStoryboard.Seek(TimeSpan.Zero);
            SearchStoryboard.Stop();
            this.Focus();

            if (error != null)
            {
                // TODO: Show error
            }
            else if (routes.Count == 0)
            {
                MessageBox.Show("No results found");
            }
            else
            {
                viewModel.CurrentViewState.CurrentRoutes = routes;

                NavigationService.Navigate(new Uri("/BusDirectionPage.xaml", UriKind.Relative));
            }
        }

        private void SearchByStopCallback(List<Stop> stops, Exception error)
        {
            SearchStoryboard.Seek(TimeSpan.Zero);
            SearchStoryboard.Stop();
            this.Focus();

            if (error != null)
            {
                // TODO: Show error
            }
            else if (stops.Count == 0)
            {
                MessageBox.Show("No results found");
            }
            else
            {
                viewModel.CurrentViewState.CurrentRoute = null;
                viewModel.CurrentViewState.CurrentRouteDirection = null;
                viewModel.CurrentViewState.CurrentStop = stops[0];

                NavigationService.Navigate(new Uri("/DetailsPage.xaml", UriKind.Relative));
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
                    if (number < 1000) //route number
                    {
                        viewModel.SearchByRoute(searchString, SearchByRouteCallback);
                    }
                    else //stop number
                    {
                        viewModel.SearchByStop(searchString, SearchByStopCallback);
                    }
                }
                else
                {
                    MessageBox.Show("Try typing a route or stop number, for example '48' or '11132'." );
                }

                //TODO: add this back when we implement search by address 
                //else
                //{
                    //try geocoding
                    //viewModel.SearchByAddress(searchString, null);
                //}

            }
        }

        private void appbar_settings_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        private void appbar_about_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative));
        }

    }
}
