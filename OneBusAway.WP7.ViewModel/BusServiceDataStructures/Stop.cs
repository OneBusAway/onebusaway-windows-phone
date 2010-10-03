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
using System.Runtime.Serialization;

namespace OneBusAway.WP7.ViewModel.BusServiceDataStructures
{
    [DataContract()]
    public class Stop
    {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public GeoCoordinate location { get; set; }
        [DataMember]
        public string direction { get; set; }
        [DataMember]
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

        public override string ToString()
        {
            return string.Format("Stop: name='{0}'", name);
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
            int result = x.CalculateDistanceInMiles(center).CompareTo(y.CalculateDistanceInMiles(center));

            // If stops are the same distance sort alphabetically
            if (result == 0)
            {
                result = x.name.CompareTo(y.name);
            }

            return result;
        }
        
    }
}
