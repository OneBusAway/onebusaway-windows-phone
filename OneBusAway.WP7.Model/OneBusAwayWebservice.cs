using System;
using System.Net;
using System.Device.Location;
using System.Collections.Generic;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using System.Xml.Linq;
using System.IO;
using System.Linq;
using System.Diagnostics;
using OneBusAway.WP7.ViewModel;

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

        #region OneBusAway service calls

        public void RoutesForLocation(GeoCoordinate location, string query, int radiusInMeters, int maxCount, RoutesForLocation_Callback callback)
        {
            string requestUrl = string.Format(
                "{0}/{1}.xml?key={2}&lat={3}&lon={4}&radius={5}&version={6}",
                WEBSERVICE,
                "routes-for-location",
                KEY,
                location.Latitude,
                location.Longitude,
                radiusInMeters,
                APIVERSION
                );

            if (string.IsNullOrEmpty(query) == false)
            {
                requestUrl += string.Format("&query={0}", query);
            }

            if (maxCount > 0)
            {
                requestUrl += string.Format("&maxCount={0}", maxCount);
            }

            WebClient client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(
                new GetRoutesForLocationCompleted(requestUrl, callback).RoutesForLocation_Completed);
            client.DownloadStringAsync(new Uri(requestUrl));
        }

        private class GetRoutesForLocationCompleted
        {
            private RoutesForLocation_Callback callback;
            private string requestUrl;

            public GetRoutesForLocationCompleted(string requestUrl, RoutesForLocation_Callback callback)
            {
                this.callback = callback;
                this.requestUrl = requestUrl;
            }

            public void RoutesForLocation_Completed(object sender, DownloadStringCompletedEventArgs e)
            {
                Exception error = e.Error;
                List<Route> routes = null;

                try
                {
                    if (error == null)
                    {
                        CheckResponseCode(e.Result, requestUrl);

                        XDocument xmlDoc = XDocument.Load(new StringReader(e.Result));
                        routes =
                            (from route in xmlDoc.Descendants("route")
                             select ParseRoute(route, xmlDoc.Descendants("agency"))).ToList<Route>();
                    }
                }
                catch (WebserviceResponseException ex)
                {
                    error = ex;
                }
                catch (Exception ex)
                {
                    error = new WebserviceParsingException(requestUrl, e.Result, ex);
                }

                Debug.Assert(error == null);

                callback(routes, error);
            }
        }


        public void StopsForLocation(GeoCoordinate location, string query, int radiusInMeters, int maxCount, bool invalidateCache, StopsForLocation_Callback callback)
        {
            // Round off coordinates so that we can exploit caching.
            // At Seattle's latitude, rounding to 3 decimal places moves the location by at most 50 or so meters.
            GeoCoordinate roundedLocation = new GeoCoordinate(
                Math.Round(location.Latitude, 3),
                Math.Round(location.Longitude, 3)
            );

            string requestString = string.Format(
                "{0}/{1}.xml?key={2}&lat={3}&lon={4}&radius={5}&version={6}",
                WEBSERVICE,
                "stops-for-location",
                KEY,
                roundedLocation.Latitude,
                roundedLocation.Longitude,
                radiusInMeters,
                APIVERSION
                );

            if (string.IsNullOrEmpty(query) == false)
            {
                requestString += string.Format("&query={0}", query);
            }

            if (maxCount > 0)
            {
                requestString += string.Format("&maxCount={0}", maxCount);
            }

            HttpCache stopsCache = new HttpCache("StopsForLocation", (int)TimeSpan.FromDays(7).TotalSeconds, 300);
            Uri requestUri = new Uri(requestString);
            if (invalidateCache)
            {
                stopsCache.Invalidate(requestUri);
            }

            stopsCache.DownloadStringAsync(requestUri, new GetStopsForLocationCompleted(requestString, callback).StopsForLocation_Completed);
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

            public void StopsForLocation_Completed(object sender, HttpCache.CacheDownloadStringCompletedEventArgs e)
            {
                Exception error = e.Error;
                List<Stop> stops = null;

                try
                {
                    if (error == null)
                    {
                        CheckResponseCode(e.Result, requestUrl);

                        XDocument xmlDoc = XDocument.Load(new StringReader(e.Result));

                        stops =
                            (from stop in xmlDoc.Descendants("stop")
                             select ParseStop
                                (
                                    stop,
                                    (from routeId in stop.Element("routeIds").Descendants("string")
                                     from route in xmlDoc.Descendants("route")
                                     where SafeGetValue(route.Element("id")) == SafeGetValue(routeId)
                                     select ParseRoute(route, xmlDoc.Descendants("agency"))).ToList<Route>()
                                )).ToList<Stop>();
                    }
                }
                catch (WebserviceResponseException ex)
                {
                    error = ex;
                }
                catch (Exception ex)
                {
                    error = new WebserviceParsingException(requestUrl, e.Result, ex);
                }

                Debug.Assert(error == null);

                // Remove a page from the cache if we hit a parsing error.  This way we won't keep
                // invalid server data in the cache
                if (error != null)
                {
                    HttpCache stopsCache = new HttpCache("StopsForLocation", (int)TimeSpan.FromDays(7).TotalSeconds, 300);
                    stopsCache.Invalidate(new Uri(requestUrl));
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
            HttpCache directionCache = new HttpCache("StopsForRoute", (int)TimeSpan.FromDays(7).TotalSeconds, 100);
            directionCache.DownloadStringAsync(new Uri(requestUrl), new GetDirectionsForRouteCompleted(requestUrl, route, callback).DirectionsForRoute_Completed);
        }

        private class GetDirectionsForRouteCompleted
        {
            private StopsForRoute_Callback callback;
            private string requestUrl;
            private Route route;

            public GetDirectionsForRouteCompleted(string requestUrl, Route route, StopsForRoute_Callback callback)
            {
                this.callback = callback;
                this.requestUrl = requestUrl;
                this.route = route;
            }

            public void DirectionsForRoute_Completed(object sender, HttpCache.CacheDownloadStringCompletedEventArgs e)
            {
                Exception error = e.Error;
                List<RouteStops> routeStops = null;

                try
                {
                    if (error == null)
                    {
                        CheckResponseCode(e.Result, requestUrl);

                        XDocument xmlDoc = XDocument.Load(new StringReader(e.Result));

                        routeStops =
                            (from stopGroup in xmlDoc.Descendants("stopGroup")
                             where SafeGetValue(stopGroup.Element("name").Element("type")) == "destination"
                             select new RouteStops
                             {
                                 name = SafeGetValue(stopGroup.Descendants("names").First().Element("string")),
                                 encodedPolylines = (from poly in stopGroup.Descendants("encodedPolyline")
                                                     select new PolyLine
                                                     {
                                                         pointsString = SafeGetValue(poly.Element("points")),
                                                         length = SafeGetValue(poly.Element("length"))
                                                     }).ToList<PolyLine>(),
                                 stops =
                                     (from stopId in stopGroup.Descendants("stopIds").First().Descendants("string")
                                      from stop in xmlDoc.Descendants("stop")
                                      where SafeGetValue(stopId) == SafeGetValue(stop.Element("id"))
                                      select ParseStop(
                                            stop, 
                                            (from routeId in stop.Element("routeIds").Descendants("string")
                                            from route in xmlDoc.Descendants("route")
                                            where SafeGetValue(route.Element("id")) == SafeGetValue(routeId)
                                            select ParseRoute(route, xmlDoc.Descendants("agency"))).ToList<Route>()
                                            )).ToList<Stop>(),

                                 route = this.route

                             }).ToList<RouteStops>();
                    }
                }
                catch (WebserviceResponseException ex)
                {
                    error = ex;
                }
                catch (Exception ex)
                {
                    error = new WebserviceParsingException(requestUrl, e.Result, ex);
                }

                Debug.Assert(error == null);

                // Remove a page from the cache if we hit a parsing error.  This way we won't keep
                // invalid server data in the cache
                if (error != null)
                {
                    HttpCache directionCache = new HttpCache("StopsForRoute", (int)TimeSpan.FromDays(7).TotalSeconds, 100);
                    directionCache.Invalidate(new Uri(requestUrl));
                }

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
                        CheckResponseCode(e.Result, requestUrl);

                        XDocument xmlDoc = XDocument.Load(new StringReader(e.Result));

                        arrivals =
                            (from arrival in xmlDoc.Descendants("arrivalAndDeparture")
                             select ParseArrivalAndDeparture(arrival)).ToList<ArrivalAndDeparture>();
                    }
                }
                catch (WebserviceResponseException ex)
                {
                    error = ex;
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
                        CheckResponseCode(e.Result, requestUrl);

                        XDocument xmlDoc = XDocument.Load(new StringReader(e.Result));

                        schedules =
                            (from schedule in xmlDoc.Descendants("stopRouteSchedule")
                             select new RouteSchedule
                             {
                                 route =
                                    (from route in xmlDoc.Descendants("route")
                                     where SafeGetValue(route.Element("id")) == SafeGetValue(schedule.Element("routeId"))
                                     select ParseRoute(route, xmlDoc.Descendants("agency"))).First(),

                                 directions =
                                     (from direction in schedule.Descendants("stopRouteDirectionSchedule")
                                      select new DirectionSchedule
                                      {
                                          tripHeadsign = SafeGetValue(direction.Element("tripHeadsign"))
                                      }).ToList<DirectionSchedule>()

                             }).ToList<RouteSchedule>();
                    }
                }
                catch (WebserviceResponseException ex)
                {
                    error = ex;
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
                        CheckResponseCode(e.Result, requestUrl);

                        XDocument xmlDoc = XDocument.Load(new StringReader(e.Result));

                        tripDetail =
                            (from trip in xmlDoc.Descendants("entry")
                             select ParseTripDetails(trip)).First();
                    }
                }
                catch (WebserviceResponseException ex)
                {
                    error = ex;
                }
                catch (Exception ex)
                {
                    error = new WebserviceParsingException(requestUrl, e.Result, ex);
                }

                Debug.Assert(error == null);

                callback(tripDetail, error);
            }
        }

        #endregion

        #region Structure parsing code

        private static TripDetails ParseTripDetails(XElement trip)
        {
            return new TripDetails()
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
            };
        }

        private static ArrivalAndDeparture ParseArrivalAndDeparture(XElement arrival)
        {
            return new ArrivalAndDeparture
            {
                routeId = SafeGetValue(arrival.Element("routeId")),
                tripId = SafeGetValue(arrival.Element("tripId")),
                stopId = SafeGetValue(arrival.Element("stopId")),
                routeShortName = SafeGetValue(arrival.Element("routeShortName")),
                tripHeadsign = SafeGetValue(arrival.Element("tripHeadsign")),
                predictedArrivalTime = arrival.Element("predictedArrivalTime").Value == "0" ?
                null : (DateTime?)UnixTimeToDateTime(long.Parse(arrival.Element("predictedArrivalTime").Value)),
                scheduledArrivalTime = UnixTimeToDateTime(long.Parse(arrival.Element("scheduledArrivalTime").Value)),
                predictedDepartureTime = arrival.Element("predictedDepartureTime").Value == "0" ?
                null : (DateTime?)UnixTimeToDateTime(long.Parse(arrival.Element("predictedDepartureTime").Value)),
                scheduledDepartureTime = UnixTimeToDateTime(long.Parse(arrival.Element("scheduledDepartureTime").Value)),
                status = SafeGetValue(arrival.Element("status"))
            };
        }

        private static Route ParseRoute(XElement route, IEnumerable<XElement> agencies)
        {
            return new Route()
            {
                id = SafeGetValue(route.Element("id")),
                shortName = SafeGetValue(route.Element("shortName")),
                url = SafeGetValue(route.Element("url")),
                description = route.Element("description") != null ?
                    route.Element("description").Value :
                        (route.Element("longName") != null ?
                            route.Element("longName").Value : string.Empty),

                agency =
                    (from agency in agencies
                     where route.Element("agencyId").Value == agency.Element("id").Value
                     select new Agency
                     {
                         id = SafeGetValue(agency.Element("id")),
                         name = SafeGetValue(agency.Element("name"))
                     }).First()
            };
        }

        private static Stop ParseStop(XElement stop, List<Route> routes)
        {
            return new Stop
            {
                id = SafeGetValue(stop.Element("id")),
                direction = SafeGetValue(stop.Element("direction")),
                location = new GeoCoordinate(
                    double.Parse(SafeGetValue(stop.Element("lat"))),
                    double.Parse(SafeGetValue(stop.Element("lon")))
                    ),
                name = SafeGetValue(stop.Element("name")),
                routes = routes
            };
        }

        private static void CheckResponseCode(string xmlResponse, string requestUrl)
        {
            XDocument xmlDoc = XDocument.Load(new StringReader(xmlResponse));
            HttpStatusCode code = (HttpStatusCode)int.Parse(xmlDoc.Element("response").Element("code").Value);

            if (code != HttpStatusCode.OK)
            {
                Debug.Assert(false);
                throw new WebserviceResponseException(code, requestUrl, xmlResponse, null);
            }
        }

        private static string SafeGetValue(XElement element)
        {
            return SafeGetValue(element, string.Empty);
        }

        private static string SafeGetValue(XElement element, string debuggingString)
        {
            if (element != null)
            {
                return element.Value;
            }
            else
            {
                return string.Empty;
            }
        }

        #endregion

        #region Private Methods

        private static DateTime UnixTimeToDateTime(long unixTime)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(unixTime);
        }

        #endregion

        internal void ClearCache()
        {
            new HttpCache("StopsForRoute", 0, 0).Clear();
            new HttpCache("StopsForLocation", 0, 0).Clear();
        }
    }
}
