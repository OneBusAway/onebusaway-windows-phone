using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Device.Location;
using Microsoft.Devices;

namespace OneBusAway.WP7.ViewModel
{
    public class Location
    {
        public GeoCoordinateWatcher LocationWatcher { get; private set; }

        public Location Singleton = new Location();

        private Location()
        {
            LocationWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            LocationWatcher.MovementThreshold = 10;
            LocationWatcher.Start();
        }

        ~Location()
        {
            LocationWatcher.Stop();
        }

        
    }
}
