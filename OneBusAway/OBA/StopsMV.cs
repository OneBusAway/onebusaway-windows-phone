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

namespace OBA
{
    public class StopsMV : ObservableCollection<Stop>
    {
        public static string OBAKey = "v1_C5%2Baiesgg8DxpmG1yS2F%2Fpj2zHk%3Dc3BoZW5yeUBnbWFpbC5jb20%3D=";
        Location Center;

        public StopsMV()
            : base()
        {
            
            Center = LocationExtensions.Location(47.6597381728812, -122.342648553848);

            string uriString = "http://api.onebusaway.org/api/where/stops-for-location.xml?"
            + "key=" + OBAKey
            + "&lat=" + Center.Latitude.ToString()
            + "&lon=" + Center.Longitude.ToString()
            + "&radius=" + "500";

            WebClient downloader = new WebClient();
            downloader.OpenReadCompleted += new OpenReadCompletedEventHandler(downloader_OpenReadCompleted);
            downloader.OpenReadAsync(new Uri(uriString));
        }

        public void downloader_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                Stream responseStream = e.Result;
                XmlSerializer serializer = new XmlSerializer(typeof(Response));
                Response root = (Response)serializer.Deserialize(responseStream);
                string responseClassString = (string)root.data.Attribute("class");

                if (responseClassString.Equals("org.onebusaway.transit_data.model.StopsBean"))
                {
                    XmlSerializer s = new XmlSerializer(typeof(StopsForLocation));
                    StopsForLocation stops = (StopsForLocation)s.Deserialize(root.data.CreateReader());

                    //calc distance
                    stops.stops.ForEach(delegate(Stop p) { p.distance = LocationExtensions.Distance(Center, p.latitude, p.longitude); });
                    //sort
                    stops.stops.Sort(delegate(Stop p1, Stop p2) { return p1.distance.CompareTo(p2.distance); });

                    //limit to 20 results
                    if (stops.stops.Count > 20)
                        stops.stops.RemoveRange(20, stops.stops.Count - 20);

                    //clear and add with index
                    ClearItems();
                    int i = 0;
                    stops.stops.ForEach(stop => { Add(stop); stop.stopIndex = (++i).ToString(); });
                }
            }
        }
    }

    public class RoutesMV : ObservableCollection<Route>
    {
        //private LocationRect boundingRect;
        public static string OBAKey = "v1_C5%2Baiesgg8DxpmG1yS2F%2Fpj2zHk%3Dc3BoZW5yeUBnbWFpbC5jb20%3D=";
        Location Center;

        public RoutesMV()
            : base()
        {
            //BoundingRect = new LocationRect(LocationExtensions.Location(47.6197381728812, -122.342648553848), 0.1, 0.1);
            Center = LocationExtensions.Location(47.6597381728812, -122.342648553848);


            string uriString = "http://api.onebusaway.org/api/where/routes-for-location.xml?"
            + "key=" + OBAKey
            + "&lat=" + Center.Latitude.ToString()
            + "&lon=" + Center.Longitude.ToString()
            + "&radius=" + "500";

            WebClient downloader = new WebClient();
            downloader.OpenReadCompleted += new OpenReadCompletedEventHandler(downloader_OpenReadCompleted);
            downloader.OpenReadAsync(new Uri(uriString));
        }

        public void downloader_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                Stream responseStream = e.Result;
                XmlSerializer serializer = new XmlSerializer(typeof(Response));
                Response root = (Response)serializer.Deserialize(responseStream);
                string responseClassString = (string)root.data.Attribute("class");

                if (responseClassString.Equals("org.onebusaway.transit_data.model.RoutesBean"))
                {
                    XmlSerializer s = new XmlSerializer(typeof(RoutesForLocation));
                    RoutesForLocation routes = (RoutesForLocation)s.Deserialize(root.data.CreateReader());

                    ////calc distance
                    //stops.stops.ForEach(delegate(Stop p) { p.distance = LocationExtensions.Distance(Center, p.latitude, p.longitude); });
                    ////sort
                    routes.routes.Sort(delegate(Route p1, Route p2) { return p1.shortName.CompareTo(p2.shortName); });

                    ////clear and add
                    ClearItems();
                    routes.routes.ForEach(route => { Add(route); });
                }
            }
        }
    }
    public class RoutesStopsMV : ObservableCollection<RouteStop>
    {
        //private LocationRect boundingRect;
        public static string OBAKey = "v1_C5%2Baiesgg8DxpmG1yS2F%2Fpj2zHk%3Dc3BoZW5yeUBnbWFpbC5jb20%3D=";
        Location Center;

        public RoutesStopsMV()
            : base()
        {
            if (DesignerProperties.IsInDesignTool)
            {
                Add(new RouteStop(new Route()));
                Add(new RouteStop(new Route()));
                Add(new RouteStop(new Route()));
                Add(new RouteStop(new Route()));
                Add(new RouteStop(new Route()));

               Add(new RouteStop(new Route()));

            }
            else
            {
                //BoundingRect = new LocationRect(LocationExtensions.Location(47.6197381728812, -122.342648553848), 0.1, 0.1);
                Center = LocationExtensions.Location(47.6597381728812, -122.342648553848);

                string routeUriString = "http://api.onebusaway.org/api/where/routes-for-location.xml?"
              + "key=" + OBAKey
              + "&lat=" + Center.Latitude.ToString()
              + "&lon=" + Center.Longitude.ToString()
              + "&radius=" + "500";

                string stopsUriString = "http://api.onebusaway.org/api/where/stops-for-location.xml?"
              + "key=" + OBAKey
              + "&lat=" + Center.Latitude.ToString()
              + "&lon=" + Center.Longitude.ToString()
              + "&radius=" + "500";

                WebClient routeDownloader = new WebClient();
                routeDownloader.OpenReadCompleted += new OpenReadCompletedEventHandler(downloader_OpenReadCompleted);
                routeDownloader.OpenReadAsync(new Uri(routeUriString));

                WebClient stopsDownloader = new WebClient();
                stopsDownloader.OpenReadCompleted += new OpenReadCompletedEventHandler(downloader_OpenReadCompleted);
                stopsDownloader.OpenReadAsync(new Uri(stopsUriString));
            }
        }

        public void downloader_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                Stream responseStream = e.Result;
                XmlSerializer serializer = new XmlSerializer(typeof(Response));
                Response root = (Response)serializer.Deserialize(responseStream);
                string responseClassString = (string)root.data.Attribute("class");

                if (responseClassString.Equals("org.onebusaway.transit_data.model.RoutesBean"))
                {
                    XmlSerializer s = new XmlSerializer(typeof(RoutesForLocation));
                    RoutesForLocation routes = (RoutesForLocation)s.Deserialize(root.data.CreateReader());

                    //sort
                    routes.routes.Sort(delegate(Route p1, Route p2) { return p1.shortName.CompareTo(p2.shortName); });
                    
                    //add to the observalble collection
                    routes.routes.ForEach(route => { Add(new RouteStop(route)); });
                }
            }
        }
    }


    public class StopRoutesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Stop stop = (Stop)value;

            return stop.RouteString();
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
            return "Distance: " + String.Format("{0:0.00}", stop.distance) + " mi ";
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
