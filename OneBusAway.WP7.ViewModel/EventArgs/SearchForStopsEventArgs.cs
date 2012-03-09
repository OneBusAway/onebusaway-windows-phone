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
using System.Collections.Generic;
using System.Device.Location;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;

namespace OneBusAway.WP7.ViewModel.EventArgs
{
    public class SearchForStopsEventArgs : AModelEventArgs
    {
        public List<Stop> stops { get; private set; }
        public GeoCoordinate location { get; private set; }
        public string query { get; private set; }

        public SearchForStopsEventArgs(List<Stop> stops, GeoCoordinate location, string query)
            : base()
        {
            this.stops = stops;
            this.query = query;
            this.location = location;
        }
    }
}
