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

namespace OneBusAway.WP7.ViewModel.DataStructures
{
    public class Stop
    {
        public string id { get; set; }
        public GeoCoordinate location { get; set; }
        public string direction { get; set; }
        public string name { get; set; }
        public List<Route> routes { get; set; }

        public double CalculateDistanceInMiles(GeoCoordinate location2)
        {
            double R = 3950; //mile conversion, 6371 for km

            double dLat = toRadian(location.Latitude - location2.Latitude);
            double dLon = toRadian(location.Longitude - location2.Longitude);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(toRadian(location2.Latitude)) * Math.Cos(toRadian(location.Latitude)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
            double d = R * c;

            return d;
        }

        private static double toRadian(double val)
        {
            return (Math.PI / 180) * val;
        }

        public override bool Equals(object obj)
        {
            if (obj is Stop == false)
            {
                return false;
            }

            return ((Stop)obj).id == this.id;
        }
    }

    public class StopDistanceComparer : IComparer<Stop>
    {
        private GeoCoordinate center;

        public StopDistanceComparer(GeoCoordinate center)
        {
            this.center = center;
        }

        public int Compare(Stop x, Stop y)
        {
            return x.CalculateDistanceInMiles(center).CompareTo(y.CalculateDistanceInMiles(center));
        }
        
    }
}
