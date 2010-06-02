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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Data;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Device.Location;

namespace OBA
{
    public class NearbyStopsMV : ObservableCollection<Stop>
    {
        private GeoCoordinate Center;
        private IBusService BusService;

        public NearbyStopsMV()
            : base()
        {
            Center = new GeoCoordinate(47.6597381728812, -122.342648553848);
            BusService = OneBusAwayService.Singleton;

            BusService.StopsForLocation(Center, 1000, routesForLocation_Completed);
        }

        public void routesForLocation_Completed(List<Stop> stops, Exception e)
        {
            if (e == null)
            {
                //clear and add stops
                ClearItems();

                stops.ForEach(stop => { Add(stop); });
            }
        }
    }

    public class NearbyRoutesMV : ObservableCollection<Route>
    {
        private GeoCoordinate Center;
        private IBusService BusService;

        public NearbyRoutesMV()
            : base()
        {
            Center = new GeoCoordinate(47.6597381728812, -122.342648553848);
            BusService = OneBusAwayService.Singleton;

            BusService.RoutesForLocation(Center, 1000, downloader_OpenReadCompleted);
        }

        public void downloader_OpenReadCompleted(List<Route> routes, Exception e)
        {
            if (e == null)
            {
                //clear and add
                ClearItems();

                routes.ForEach(route => { Add(route); });
            }
        }
    }

    public class RouteDirectionsMV : ObservableCollection<Route>
    {
        Location Center;

        public RouteDirectionsMV()
            : base()
        {
            //BoundingRect = new LocationRect(LocationExtensions.Location(47.6197381728812, -122.342648553848), 0.1, 0.1);
            Center = LocationExtensions.Location(47.6597381728812, -122.342648553848);
        }

        public void downloader_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                //Stream responseStream = e.Result;
                //XmlSerializer serializer = new XmlSerializer(typeof(Response));
                //Response root = (Response)serializer.Deserialize(responseStream);
                //string responseClassString = (string)root.data.Attribute("class");

                //XmlSerializer s = new XmlSerializer(typeof(RoutesForLocationResponse));
                //RoutesForLocationResponse routes = (RoutesForLocationResponse)s.Deserialize(root.data.CreateReader());

                ////sort
                //routes.routes.Sort(delegate(Route p1, Route p2) { return p1.shortName.CompareTo(p2.shortName); });
                    
                ////add to the observalble collection
                //routes.routes.ForEach(route => { Add(new RouteStop(route)); });
                
            }
        }
    }


    public class StopRoutesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Stop stop = (Stop)value;

            return stop.name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DistanceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Stop stop = (Stop)value;
            return "Distance mi ";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class Location
    {
        public double Longitude;
        public double Latitude;
    }

    //Location extension method
    public static class LocationExtensions
    {
        public static Location Location(double lat, double lon)
        {
            Location loc = new Location();
            loc.Longitude = lon;
            loc.Latitude = lat;
            return loc;
        }

        //public static Location Location(GeoCoordinate coord)
        //{
        //    return Location(coord.Latitude, coord.Longitude);
        //}

        public static double Distance(Location pos1, double lat, double lon)
        {
            double R = 3950; //mile conversion, 6371 for km

            double dLat = toRadian((double)lat - (double)pos1.Latitude);
            double dLon = toRadian((double)lon - (double)pos1.Longitude);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(toRadian(pos1.Latitude)) * Math.Cos(toRadian(lat)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
            double d = R * c;
            return d;
        }

        private static double toRadian(double val)
        {
            return (Math.PI / 180) * val;
        }

    } 
}
