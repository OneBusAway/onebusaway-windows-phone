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
using System.Collections.Generic;

namespace OBA
{
    public delegate void StopsForLocation_Callback(List<Stop> stops, Exception error);
    public delegate void RoutesForLocation_Callback(List<Route> routes, Exception error);
    public delegate void DirectionsForRoute_Callback(List<RouteDirection> routeDirections, Exception error);
    public delegate void ArrivalsForStop_Callback(List<ArrivalAndDeparture> arrivals, Exception error);

    public interface IBusService
    {
        void StopsForLocation(GeoCoordinate location, int radiusInMeters, StopsForLocation_Callback callback);

        void RoutesForLocation(GeoCoordinate location, int radiusInMeters, RoutesForLocation_Callback callback);

        void DirectionsForRoute(Route route, DirectionsForRoute_Callback callback);

        void ArrivalsForStop(Stop stop, ArrivalsForStop_Callback callback);
    }

    public class Stop
    {
        public string id { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string direction { get; set; }
        public string name { get; set; }
        public List<Route> routes { get; set; }
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
            return CalculateDistance(x).CompareTo(CalculateDistance(y));
        }

        private double CalculateDistance(Stop stop)
        {
            return Math.Sqrt(Math.Pow(stop.latitude - center.Latitude, 2) + Math.Pow(stop.longitude - center.Longitude, 2));
        }
    }

    public class Route
    {
        public string id { get; set; }
        public string shortName { get; set; }
        public string description { get; set; }
        public string url { get; set; }
        public Agency agency { get; set; }
        
        public override bool Equals(object obj)
        {
            if (obj is Route)
            {
                Route otherRoute = (Route)obj;
                if (otherRoute.id == this.id)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }

    public class RouteDirection
    {
        public string name { get; set; }
        public List<Stop> stops { get; set; }
    }

    public class Agency
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class ArrivalAndDeparture
    {
        private static DateTime BEGIN_UTC = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public string routeId { get; set; }
        public string routeShortName { get; set; }
        public string tripId { get; set; }
        public string tripHeadsign { get; set; }
        public string stopId { get; set; }
        public DateTime? predictedArrivalTime { get; set; }
        public DateTime scheduledArrivalTime { get; set; }
        public DateTime? predictedDepartureTime { get; set; }
        public DateTime scheduledDepartureTime { get; set; }
        public string status { get; set; }

    }
}
