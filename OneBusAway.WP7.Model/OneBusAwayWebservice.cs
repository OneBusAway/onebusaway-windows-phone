using System;
using System.Net;
using System.Device.Location;
using System.Collections.Generic;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using System.Xml.Linq;
using System.IO;
using System.Linq;
using System.Diagnostics;

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

        public void StopsForLocation(GeoCoordinate location, int radiusInMeters, int maxCount, StopsForLocation_Callback callback)
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

            if (maxCount > 0)
            {
                requestUrl += string.Format("&maxCount={0}", maxCount);
            }

            WebClient client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(
                new GetStopsForLocationCompleted(requestUrl, callback).StopsForLocation_Completed);
            client.DownloadStringAsync(new Uri(requestUrl));
        }

        private class GetStopsForLocationCompleted
        {
            private StopsForLocation_Callback callback;
            private string requestUrl;

            public GetStopsForLocationCompleted(string requestUrl, StopsForLocation_Callback callback)
            {
                this.callback = callback;
                this.requestUrl = requestUrl;
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
                    error = new WebserviceParsingException(requestUrl, e.Result, ex);
                }

                Debug.Assert(error == null);

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
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(new GetDirectionsForRouteCompleted(requestUrl, callback).DirectionsForRoute_Completed);
            client.DownloadStringAsync(new Uri(requestUrl));
        }

        private class GetDirectionsForRouteCompleted
        {
            private StopsForRoute_Callback callback;
            private string requestUrl;

            public GetDirectionsForRouteCompleted(string requestUrl, StopsForRoute_Callback callback)
            {
                this.callback = callback;
                this.requestUrl = requestUrl;
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
                            (from stopGroup in xmlDoc.Descendants("stopGroup")
                             where stopGroup.Element("name").Element("type").Value == "destination"
                             select new RouteStops
                             {
                                 name = stopGroup.Descendants("names").First().Element("string").Value,
                                 encodedPolylines = (from poly in stopGroup.Descendants("encodedPolyline")
                                                     select new PolyLine
                                                     {
                                                         points = poly.Element("points").Value,
                                                         length = poly.Element("length").Value
                                                     }).ToList<PolyLine>(),
                                 stops =
                                     (from stopId in stopGroup.Descendants("stopIds").First().Descendants("string")
                                      from stop in xmlDoc.Descendants("stop")
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
                                                   description = route.Element("description") != null ? route.Element("description").Value : String.Empty,
                                                   shortName = route.Element("shortName").Value,
                                                   url = route.Element("url") != null ? route.Element("url").Value : String.Empty,


                                                   agency =
                                                   (from agency in xmlDoc.Descendants("agency")
                                                    where route.Element("agencyId").Value == agency.Element("id").Value
                                                    select new Agency
                                                    {
                                                        id = agency.Element("id").Value,
                                                    }).First()

                                               }).ToList<Route>()

                                      }).ToList<Stop>()

                             }).ToList<RouteStops>();
                    }
                }
                catch (Exception ex)
                {
                    error = new WebserviceParsingException(requestUrl, e.Result, ex);
                }

                Debug.Assert(error == null);

                callback(routeStops, error);
            }
        }

        public void ArrivalsForStop(Stop stop, ArrivalsForStop_Callback callback)
        {
            string requestUrl = string.Format(
                "{0}/{1}/{2}.xml?minutesAfter={3}&key={4}&version={5}",
                WEBSERVICE,
                "arrivals-and-departures-for-stop",
                stop.id,
                60,
                KEY,
                APIVERSION
                );
            WebClient client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(new GetArrivalsForStopCompleted(requestUrl, callback).ArrivalsForStop_Completed);
            client.DownloadStringAsync(new Uri(requestUrl));
        }

        private class GetArrivalsForStopCompleted
        {
            private ArrivalsForStop_Callback callback;
            private string requestUrl;

            public GetArrivalsForStopCompleted(string requestUrl, ArrivalsForStop_Callback callback)
            {
                this.callback = callback;
                this.requestUrl = requestUrl;
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
                    error = new WebserviceParsingException(requestUrl, e.Result, ex);
                }

                Debug.Assert(error == null);

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
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(new GetScheduleForStopCompleted(requestUrl, callback).ScheduleForStop_Completed);
            client.DownloadStringAsync(new Uri(requestUrl));
        }

        private class GetScheduleForStopCompleted
        {
            private ScheduleForStop_Callback callback;
            private string requestUrl;

            public GetScheduleForStopCompleted(string requestUrl, ScheduleForStop_Callback callback)
            {
                this.callback = callback;
                this.requestUrl = requestUrl;
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
                    error = new WebserviceParsingException(requestUrl, e.Result, ex);
                }

                Debug.Assert(error == null);

                callback(schedules, error);
            }
        }

        public void TripDetailsForArrival(ArrivalAndDeparture arrival, TripDetailsForArrival_Callback callback)
        {
            string requestUrl = string.Format(
                "{0}/{1}/{2}.xml?key={3}&includeSchedule={4}",
                WEBSERVICE,
                "trip-details",
                arrival.tripId,
                KEY,
                "false"
                );
            WebClient client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(new TripDetailsForArrivalCompleted(requestUrl, callback).TripDetailsForArrival_Completed);
            client.DownloadStringAsync(new Uri(requestUrl));
        }

        private class TripDetailsForArrivalCompleted
        {
            private TripDetailsForArrival_Callback callback;
            private string requestUrl;

            public TripDetailsForArrivalCompleted(string requestUrl, TripDetailsForArrival_Callback callback)
            {
                this.callback = callback;
                this.requestUrl = requestUrl;
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
                                 tripId = trip.Element("tripId").Value,
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
                    error = new WebserviceParsingException(requestUrl, e.Result, ex);
                }

                Debug.Assert(error == null);

                callback(tripDetail, error);
            }
        }

        private static DateTime UnixTimeToDateTime(long unixTime)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(unixTime);
        }
    }

    public class WebserviceParsingException : Exception
    {
        private string requestUrl;
        private string serverResponse;

        public WebserviceParsingException(string requestUrl, string serverResponse, Exception innerException)
            : base("There was an error parsing the server response", innerException)
        {
            this.requestUrl = requestUrl;
            this.serverResponse = serverResponse;
        }

        public override string ToString()
        {
            return string.Format(
                "{0}\r\nRequestURL: '{1}'\r\nResponse:\r\n{2}",
                base.ToString(),
                requestUrl,
                serverResponse
                );
        }
    }
}
