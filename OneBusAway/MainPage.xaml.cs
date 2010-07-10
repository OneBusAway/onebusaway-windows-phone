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
using OneBusAway.WP7.ViewModel.DataStructures;
using OneBusAway.WP7.ViewModel;

namespace OneBusAway.WP7.View
{
    public partial class MainPage : PhoneApplicationPage
    {
        public static GeoCoordinateWatcher locationWatcher = new GeoCoordinateWatcher();

        private MainPageVM viewModel;

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

        public static GeoPositionStatus LocationStatus
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

            viewModel = Resources["ViewModel"] as MainPageVM;

            locationWatcher.Start();
            SupportedOrientations = SupportedPageOrientation.Portrait;

            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel.LoadInfoForLocation(CurrentLocation, 1000);

            //if (Microsoft.Devices.Environment.DeviceType == DeviceType.Emulator)
            //{
            //    locationWatcher_StatusChanged(this, new GeoPositionStatusChangedEventArgs(GeoPositionStatus.Ready));
            //}
            //else
            //{
            //    locationWatcher_StatusChanged(this, new GeoPositionStatusChangedEventArgs(locationWatcher.Status));
            //}
            
        }

        private void RoutesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                ViewState.CurrentRoute = (Route)e.AddedItems[0];
                ViewState.CurrentStop = ViewState.CurrentRoute.closestStop;


                NavigationService.Navigate(new Uri("/DetailsPage.xaml", UriKind.Relative));
            }
        }

        private void appbar_center_Click(object sender, EventArgs e)
        {
            viewModel.LoadInfoForLocation(CurrentLocation, 1000);
        }

        //void locationWatcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        //{
        //    if (e.Status == GeoPositionStatus.Ready)
        //    {
        //        NearbyStopsMV
        //    }
        //}

    }
}