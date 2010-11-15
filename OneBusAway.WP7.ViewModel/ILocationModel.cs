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
using OneBusAway.WP7.ViewModel.EventArgs;
using System.Device.Location;

namespace OneBusAway.WP7.ViewModel
{
    public interface ILocationModel
    {
        event EventHandler<LocationForAddressEventArgs> LocationForAddress_Completed;
        void LocationForAddress(string addressString, GeoCoordinate searchNearLocation);
        void LocationForAddress(string addressString, GeoCoordinate searchNearLocation, object state);
    }

}
