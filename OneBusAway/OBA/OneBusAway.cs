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
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Xml.Linq;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Device.Location;

namespace OBA
{

    public class OneBusAwayService : IBusService
    {
        private const string WEBSERViCE = "http://api.onebusaway.org/api/where";
        private const string KEY = "v1_C5%2Baiesgg8DxpmG1yS2F%2Fpj2zHk%3Dc3BoZW5yeUBnbWFpbC5jb20%3D=";
        private const int APIVERSION = 2;

        public static OneBusAwayService Singleton = new OneBusAwayService();

        private OneBusAwayService()
        {

        }

        public void StopsForLocation(GeoCoordinate location, int radiusInMeters, StopsForLocation_Callback callback)
        {
            string requestUrl = string.Format(
                "{0}/{1}.xml?key={2}&lat={3}&lon={4}&radius={5}&version={6}",
                WEBSERViCE,
                "stops-for-location",
                KEY,
                location.Latitude,
                location.Longitude,
                radiusInMeters,
                APIVERSION
                );
            WebClient client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(new GetStopsForLocationCompleted(callback).StopsForLocation_Completed);
            client.DownloadStringAsync(new Uri(requestUrl));
        }

        private class GetStopsForLocationCompleted
        {
            private StopsForLocation_Callback callback;

            public GetStopsForLocationCompleted(StopsForLocation_Callback callback)
            {
                this.callback = callback;
            }

            public void StopsForLocation_Completed(object sender, DownloadStringCompletedEventArgs e)
            {
                Exception error = e.Error;
                List<Stop> stops = null;

                try
                {
                    if (error == null)
                    {
                        XDocument xmlDoc = XDocument.Load(new StringReader(e.Result));

                        stops =
                            (from stop in xmlDoc.Descendants("stop")
                             select new Stop
                             {
                                 id = stop.Element("id").Value,
                                 latitude = double.Parse(stop.Element("lat").Value),
                                 longitude = double.Parse(stop.Element("lon").Value),
                                 direction = stop.Element("direction").Value,
                                 name = stop.Element("name").Value,

                                 routes =
                                 (from routeId in stop.Element("routeIds").Descendants("string")
                                  from route in xmlDoc.Descendants("route")
                                  where route.Element("id").Value == routeId.Value
                                  select new Route
                                  {
                                      id = route.Element("id").Value,
                                      description = route.Element("description").Value,
                                      shortName = route.Element("shortName").Value,
                                      url = route.Element("url").Value,

                                      agency =
                                      (from agency in xmlDoc.Descendants("agency")
                                       where route.Element("agencyId").Value == agency.Element("id").Value
                                       select new Agency
                                       {
                                           id = agency.Element("id").Value,
                                           name = agency.Element("name").Value
                                       }).First()

                                  }).ToList<Route>()

                             }).ToList<Stop>();
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                }

                callback(stops, error);
            }
        }

        public void RoutesForLocation(GeoCoordinate location, int radiusInMeters, RoutesForLocation_Callback callback)
        {
            StopsForLocation(location, radiusInMeters,
                delegate(List<Stop> stops, Exception e)
                {
                    Exception error = e;
                    List<Route> routes = new List<Route>();

                    try
                    {
                        if (error == null)
                        {
                            stops.Sort(new StopDistanceComparer(location));

                            foreach (Stop stop in stops)
                            {
                                foreach (Route route in stop.routes)
                                {
                                    if (routes.Contains(route) == false)
                                    {
                                        routes.Add(route);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }

                    callback(routes, error);
                }
            );
        }

        public void DirectionsForRoute(Route route, DirectionsForRoute_Callback callback)
        {
            string requestUrl = string.Format(
                "{0}/{1}/{2}.xml?key={3}&version={4}",
                WEBSERViCE,
                "stops-for-route",
                route.id,
                KEY,
                APIVERSION
                );
            WebClient client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(new GetDirectionsForRouteCompleted(callback).DirectionsForRoute_Completed);
            client.DownloadStringAsync(new Uri(requestUrl));
        }

        private class GetDirectionsForRouteCompleted
        {
            private DirectionsForRoute_Callback callback;

            public GetDirectionsForRouteCompleted(DirectionsForRoute_Callback callback)
            {
                this.callback = callback;
            }

            public void DirectionsForRoute_Completed(object sender, DownloadStringCompletedEventArgs e)
            {
                Exception error = e.Error;
                List<RouteDirection> routeDirections = null;

                try
                {
                    if (error == null)
                    {
                        XDocument xmlDoc = XDocument.Load(new StringReader(e.Result));

                        routeDirections =
                            (from stopGrouping in xmlDoc.Descendants("stopGroupings")
                             from direction in stopGrouping.Descendants("stopGroup")
                             where stopGrouping.Element("type").Value == "direction"
                                select new RouteDirection
                                {
                                    name = direction.Descendants("names").First().Element("string").Value,

                                    stops =
                                        (from stopId in xmlDoc.Descendants("stopIds")
                                            from stop in xmlDoc.Descendants("stops")
                                            where stopId.Value == stop.Element("id").Value
                                            select new Stop
                                            {
                                                id = stop.Element("id").Value,
                                                direction = stop.Element("direction").Value,
                                                latitude = double.Parse(stop.Element("lat").Value),
                                                longitude = double.Parse(stop.Element("lon").Value),
                                                name = stop.Element("name").Value,

                                                routes =
                                                    (from routeId in stop.Element("routeIds").Descendants("string")
                                                    from route in xmlDoc.Descendants("route")
                                                    where route.Element("id").Value == routeId.Value
                                                    select new Route
                                                    {
                                                        id = route.Element("id").Value,
                                                        description = route.Element("description").Value,
                                                        shortName = route.Element("shortName").Value,
                                                        url = route.Element("url").Value,

                                                        agency =
                                                        (from agency in xmlDoc.Descendants("agency")
                                                        where route.Element("agencyId").Value == agency.Element("id").Value
                                                        select new Agency
                                                        {
                                                            id = agency.Element("id").Value,
                                                            name = agency.Element("name").Value
                                                        }).First()
                                                    }).ToList<Route>()

                                            }).ToList<Stop>()

                            }).ToList<RouteDirection>();
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                }

                callback(routeDirections, error);
            }
        }

        public void ArrivalsForStop(Stop stop, ArrivalsForStop_Callback callback)
        {
            string requestUrl = string.Format(
                "{0}/{1}.xml?key={2}&stopId={3}&version={4}",
                WEBSERViCE,
                "arrivals-and-departures",
                KEY,
                stop.id,
                APIVERSION
                );
            WebClient client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(new GetArrivalsForStopCompleted(callback).ArrivalsForStop_Completed);
            client.DownloadStringAsync(new Uri(requestUrl));
        }

        private class GetArrivalsForStopCompleted
        {
            private ArrivalsForStop_Callback callback;

            public GetArrivalsForStopCompleted(ArrivalsForStop_Callback callback)
            {
                this.callback = callback;
            }

            public void ArrivalsForStop_Completed(object sender, DownloadStringCompletedEventArgs e)
            {
                Exception error = e.Error;
                List<ArrivalAndDeparture> arrivals = null;

                try
                {
                    if (error == null)
                    {
                        XDocument xmlDoc = XDocument.Load(new StringReader(e.Result));

                        arrivals =
                            (from arrival in xmlDoc.Descendants("arrivalAndDeparture")
                             select new ArrivalAndDeparture
                             {

                             }).ToList<ArrivalAndDeparture>();
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                }

                callback(arrivals, error);
            }
        }
    }   

    //[XmlRoot("encodedPolyline")]
    //public class PolyLine
    //{
    //    public List<Coordinate> coordinates = new List<Coordinate>();

    //    private string pointsString;
    //    [XmlElement]
    //    public string points
    //    {
    //        get { return pointsString; }
    //        set
    //        {
    //            pointsString = value;
    //            coordinates = DecodeLatLongList(value);
    //        }
    //    }


    //    [XmlElement]
    //    public string length { get; set; }

    //    [XmlElement]
    //    public string levels { get; set; }

    //    public static List<Coordinate> DecodeLatLongList(string encoded)
    //    {

    //        int index = 0;
    //        int lat = 0;
    //        int lng = 0;

    //        int len = encoded.Length;
    //        List<Coordinate> locs = new List<Coordinate>();

    //        while (index < len)
    //        {
    //            lat += decodePoint(encoded, index, out index);
    //            lng += decodePoint(encoded, index, out index);

    //            Coordinate loc = new Coordinate();
    //            loc.Latitude = (lat * 1e-5);
    //            loc.Longitude = (lng * 1e-5);

    //            locs.Add(loc);
    //        }

    //        return locs;
    //    }


    //    private static int decodePoint(string encoded, int startindex, out int finishindex)
    //    {
    //        int b;
    //        int shift = 0;
    //        int result = 0;

    //        //magic google algorithm, see http://code.google.com/apis/maps/documentation/polylinealgorithm.html
    //        do
    //        {
    //            b = Convert.ToInt32(encoded[startindex++]) - 63;
    //            result |= (b & 0x1f) << shift;
    //            shift += 5;
    //        } while (b >= 0x20);
    //        //if negative flip
    //        int dlat = (((result & 1) > 0) ? ~(result >> 1) : (result >> 1));

    //        //set output index
    //        finishindex = startindex;

    //        return dlat;
    //    }
    //}
}
