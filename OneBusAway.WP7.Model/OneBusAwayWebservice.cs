using System;
using System.Net;
using System.Device.Location;
using System.Collections.Generic;
using OneBusAway.WP7.ViewModel.DataStructures;
using System.Xml.Linq;
using System.IO;
using System.Linq;

namespace OneBusAway.WP7.Model
{
    internal class OneBusAwayWebservice
    {

        #region Private Variables

        private const string WEBSERVICE = "http://api.onebusaway.org/api/where";
        private const string KEY = "v1_C5%2Baiesgg8DxpmG1yS2F%2Fpj2zHk%3Dc3BoZW5yeUBnbWFpbC5jb20%3D=";
        private const int APIVERSION = 2;

        #endregion

        #region Delegates

        public delegate void StopsForLocation_Callback(List<Stop> stops, Exception error);
        public delegate void RoutesForLocation_Callback(List<Route> routes, Exception error);
        public delegate void StopsForRoute_Callback(List<RouteStops> routeStops, Exception error);
        public delegate void ArrivalsForStop_Callback(List<ArrivalAndDeparture> arrivals, Exception error);
        public delegate void ScheduleForStop_Callback(List<RouteSchedule> schedules, Exception error);
        public delegate void TripDetailsForArrival_Callback(TripDetails tripDetail, Exception error);

        #endregion

        #region Constructor/Singleton

        public static OneBusAwayWebservice Singleton = new OneBusAwayWebservice();

        private OneBusAwayWebservice()
        {

        }

        #endregion

        public void StopsForLocation(GeoCoordinate location, int radiusInMeters, StopsForLocation_Callback callback)
        {
            string requestUrl = string.Format(
                "{0}/{1}.xml?key={2}&lat={3}&lon={4}&radius={5}&version={6}",
                WEBSERVICE,
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
                                 location = new GeoCoordinate(
                                     double.Parse(stop.Element("lat").Value),
                                     double.Parse(stop.Element("lon").Value)
                                     ),
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

        public void StopsForRoute(Route route, StopsForRoute_Callback callback)
        {
            string requestUrl = string.Format(
                "{0}/{1}/{2}.xml?key={3}&version={4}",
                WEBSERVICE,
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
            private StopsForRoute_Callback callback;

            public GetDirectionsForRouteCompleted(StopsForRoute_Callback callback)
            {
                this.callback = callback;
            }

            public void DirectionsForRoute_Completed(object sender, DownloadStringCompletedEventArgs e)
            {
                Exception error = e.Error;
                List<RouteStops> routeStops = null;

                try
                {
                    if (error == null)
                    {
                        XDocument xmlDoc = XDocument.Load(new StringReader(e.Result));

                        routeStops =
                            (from stopGrouping in xmlDoc.Descendants("stopGroupings")
                             from direction in stopGrouping.Descendants("stopGroup")
                             where stopGrouping.Element("type").Value == "direction"
                             select new RouteStops
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
                                          location = new GeoCoordinate(
                                              double.Parse(stop.Element("lat").Value),
                                              double.Parse(stop.Element("lon").Value)
                                              ),
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

                             }).ToList<RouteStops>();
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                }

                callback(routeStops, error);
            }
        }

        public void ArrivalsForStop(Stop stop, ArrivalsForStop_Callback callback)
        {
            string requestUrl = string.Format(
                "{0}/{1}/{2}.xml?key={2}&version={4}",
                WEBSERVICE,
                "arrivals-and-departures",
                stop.id,
                KEY,
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
                                 routeId = arrival.Element("routeId").Value,
                                 tripId = arrival.Element("tripId").Value,
                                 stopId = arrival.Element("stopId").Value,
                                 routeShortName = arrival.Element("routeShortName").Value,
                                 tripHeadsign = arrival.Element("tripHeadsign").Value,
                                 predictedArrivalTime = arrival.Element("predictedArrivalTime").Value == "0" ?
                                    null : (DateTime?)UnixTimeToDateTime(long.Parse(arrival.Element("predictedArrivalTime").Value)),
                                 scheduledArrivalTime = UnixTimeToDateTime(long.Parse(arrival.Element("scheduledArrivalTime").Value)),
                                 predictedDepartureTime = arrival.Element("predictedDepartureTime").Value == "0" ?
                                    null : (DateTime?)UnixTimeToDateTime(long.Parse(arrival.Element("predictedDepartureTime").Value)),
                                 scheduledDepartureTime = UnixTimeToDateTime(long.Parse(arrival.Element("scheduledDepartureTime").Value)),
                                 status = arrival.Element("status").Value
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

        public void ScheduleForStop(Stop stop, ScheduleForStop_Callback callback)
        {
            string requestUrl = string.Format(
                "{0}/{1}/{2}.xml?key={3}&version={4}",
                WEBSERVICE,
                "schedule-for-stop",
                stop.id,
                KEY,
                APIVERSION
                );
            WebClient client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(new GetScheduleForStopCompleted(callback).ScheduleForStop_Completed);
            client.DownloadStringAsync(new Uri(requestUrl));
        }

        private class GetScheduleForStopCompleted
        {
            private ScheduleForStop_Callback callback;

            public GetScheduleForStopCompleted(ScheduleForStop_Callback callback)
            {
                this.callback = callback;
            }

            public void ScheduleForStop_Completed(object sender, DownloadStringCompletedEventArgs e)
            {
                Exception error = e.Error;
                List<RouteSchedule> schedules = null;

                try
                {
                    if (error == null)
                    {
                        XDocument xmlDoc = XDocument.Load(new StringReader(e.Result));

                        schedules =
                            (from schedule in xmlDoc.Descendants("stopRouteSchedule")
                             select new RouteSchedule
                             {
                                 route =
                                    (from route in xmlDoc.Descendants("route")
                                     where route.Element("id").Value == schedule.Element("routeId").Value
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
                                     }).First(),

                                 directions =
                                     (from direction in schedule.Descendants("stopRouteDirectionSchedule")
                                      select new DirectionSchedule
                                      {
                                          tripHeadsign = direction.Element("tripHeadsign").Value
                                      }).ToList<DirectionSchedule>()

                             }).ToList<RouteSchedule>();
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                }

                callback(schedules, error);
            }
        }

        public void TripDetailsForArrival(ArrivalAndDeparture arrival, TripDetailsForArrival_Callback callback)
        {
            string requestUrl = string.Format(
                "{0}/{1}/{2}.xml?key={3}&includeSchedule={4}",
                WEBSERVICE,
                "trip",
                arrival.tripId,
                KEY,
                "false"
                );
            WebClient client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(new TripDetailsForArrivalCompleted(callback).TripDetailsForArrival_Completed);
            client.DownloadStringAsync(new Uri(requestUrl));
        }

        private class TripDetailsForArrivalCompleted
        {
            private TripDetailsForArrival_Callback callback;

            public TripDetailsForArrivalCompleted(TripDetailsForArrival_Callback callback)
            {
                this.callback = callback;
            }

            public void TripDetailsForArrival_Completed(object sender, DownloadStringCompletedEventArgs e)
            {
                Exception error = e.Error;
                TripDetails tripDetail = null;

                try
                {
                    if (error == null)
                    {
                        XDocument xmlDoc = XDocument.Load(new StringReader(e.Result));

                        tripDetail =
                            (from trip in xmlDoc.Descendants("entry")
                             select new TripDetails
                             {
                                 tripId = trip.Element("id").Value,
                                 serviceDate = UnixTimeToDateTime(long.Parse(trip.Element("status").Element("serviceDate").Value)),
                                 scheduleDeviationInSec = bool.Parse(trip.Element("status").Element("predicted").Value) == true ?
                                    int.Parse(trip.Element("status").Element("scheduleDeviation").Value) : (int?)null,
                                 closestStopId = bool.Parse(trip.Element("status").Element("predicted").Value) == true ?
                                    trip.Element("status").Element("closestStop").Value : null,
                                 closestStopTimeOffset = bool.Parse(trip.Element("status").Element("predicted").Value) == true ?
                                    int.Parse(trip.Element("status").Element("closestStopTimeOffset").Value) : (int?)null,
                                 position = bool.Parse(trip.Element("status").Element("predicted").Value) == true ?
                                    new GeoCoordinate(
                                        double.Parse(trip.Element("status").Element("position").Element("lat").Value),
                                        double.Parse(trip.Element("status").Element("position").Element("lon").Value)
                                        )
                                    :
                                    null

                             }).First();
                    }
                }
                catch (Exception ex)
                {
                    error = ex;
                }

                callback(tripDetail, error);
            }
        }

        private static DateTime UnixTimeToDateTime(long unixTime)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(unixTime);
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
}
