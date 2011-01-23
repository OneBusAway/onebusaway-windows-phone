using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using OneBusAway.WP7.ViewModel;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;

namespace OneBusAway.WP7.Model
{
    internal class OneBusAwayWebservice
    {

        #region Private Variables

        private const string WEBSERVICE = "http://api.onebusaway.org/api/where";
        private const string KEY = "v1_C5%2Baiesgg8DxpmG1yS2F%2Fpj2zHk%3Dc3BoZW5yeUBnbWFpbC5jb20%3D=";
        private const int APIVERSION = 2;

        // This decides which decimal place we round
        // Ex: roundingLevel = 2, -122.123 -> -122.12
        private const int roundingLevel = 2;
        // This decides what fraction of a whole number we round to
        // Ex: multiplier = 2, we round to the nearest 0.5
        // Ex: multipler = 3, we round to the nearest 0.33
        private const int multiplier = 3;

        private HttpCache stopsCache;
        private HttpCache directionCache;

        #endregion

        #region Delegates

        public delegate void StopsForLocation_Callback(List<Stop> stops, bool limitExceeded, Exception error);
        public delegate void RoutesForLocation_Callback(List<Route> routes, Exception error);
        public delegate void StopsForRoute_Callback(List<RouteStops> routeStops, Exception error);
        public delegate void ArrivalsForStop_Callback(List<ArrivalAndDeparture> arrivals, Exception error);
        public delegate void ScheduleForStop_Callback(List<RouteSchedule> schedules, Exception error);
        public delegate void TripDetailsForArrival_Callback(TripDetails tripDetail, Exception error);

        #endregion

        #region Constructor

        public OneBusAwayWebservice()
        {
            stopsCache =  new HttpCache("StopsForLocation", (int)TimeSpan.FromDays(15).TotalSeconds, 300);
            directionCache = new HttpCache("StopsForRoute", (int)TimeSpan.FromDays(15).TotalSeconds, 100);
        }

        #endregion

        #region OneBusAway service calls

        /// <summary>
        /// Base class for callbacks on service call completion.
        /// </summary>
        private abstract class ACallCompleted
        {
            protected string requestUrl;

            public ACallCompleted(string requestUrl)
            {
                this.requestUrl = requestUrl;
            }

            /// <summary>
            /// Callback entry point for calls based on HttpWebRequest
            /// </summary>
            /// <param name="asyncResult"></param>
            public void HttpWebRequest_Completed(IAsyncResult asyncResult)
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)asyncResult.AsyncState;
                    HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asyncResult);

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        string results = (new StreamReader(response.GetResponseStream())).ReadToEnd();
                        throw new WebserviceResponseException(response.StatusCode, request.RequestUri.ToString(), results, null);
                    }
                    else
                    {
                        ParseResults(new StreamReader(response.GetResponseStream()), null);
                    }
                }
                catch (Exception e)
                {
                    ParseResults(new StringReader(""), e);
                }
            }

            /// <summary>
            /// Callback entry point for calls based on HttpCache
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            public void HttpCache_Completed(object sender, HttpCache.CacheDownloadStringCompletedEventArgs e)
            {
                ParseResults(e.Result, e.Error);
            }

            /// <summary>
            /// Subclasses should implement this by pulling data out of the specified reader, parsing it, and invoking the desired callback.
            /// </summary>
            /// <param name="result"></param>
            /// <param name="error"></param>
            public abstract void ParseResults(TextReader result, Exception error);
        }

        public void RoutesForLocation(GeoCoordinate location, string query, int radiusInMeters, int maxCount, RoutesForLocation_Callback callback)
        {
            string requestUrl = string.Format(
                "{0}/{1}.xml?key={2}&lat={3}&lon={4}&radius={5}&version={6}",
                WEBSERVICE,
                "routes-for-location",
                KEY,
                location.Latitude.ToString(NumberFormatInfo.InvariantInfo),
                location.Longitude.ToString(NumberFormatInfo.InvariantInfo),
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

            HttpWebRequest requestGetter = (HttpWebRequest)HttpWebRequest.Create(requestUrl);
            requestGetter.BeginGetResponse(
                new AsyncCallback(new GetRoutesForLocationCompleted(requestUrl, callback).HttpWebRequest_Completed),
                requestGetter);
        }

        private class GetRoutesForLocationCompleted : ACallCompleted
        {
            private RoutesForLocation_Callback callback;

            public GetRoutesForLocationCompleted(string requestUrl, RoutesForLocation_Callback callback) : base(requestUrl)
            {
                this.callback = callback;
            }

            public override void ParseResults(TextReader result, Exception error)
            {
                List<Route> routes = null;

                XDocument xmlDoc = CheckResponseCode(result, requestUrl);
                try
                {
                    routes =
                        (from route in xmlDoc.Descendants("route")
                         select ParseRoute(route, xmlDoc.Descendants("agency"))).ToList<Route>();
                }
                catch (Exception ex)
                {
                    error = new WebserviceParsingException(requestUrl, xmlDoc.ToString(), ex);
                }

                Debug.Assert(error == null);

                callback(routes, error);
            }
        }


        public void StopsForLocation(GeoCoordinate location, string query, int radiusInMeters, int maxCount, bool invalidateCache, StopsForLocation_Callback callback)
        {
            GeoCoordinate roundedLocation = GetRoundedLocation(location);

            // ditto for the search radius -- nearest 50 meters for caching
            int roundedRadius = (int)(Math.Round(radiusInMeters / 50.0) * 50);

            string requestString = string.Format(
                "{0}/{1}.xml?key={2}&lat={3}&lon={4}&radius={5}&version={6}",
                WEBSERVICE,
                "stops-for-location",
                KEY,
                roundedLocation.Latitude.ToString(NumberFormatInfo.InvariantInfo),
                roundedLocation.Longitude.ToString(NumberFormatInfo.InvariantInfo),
                roundedRadius,
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

            Uri requestUri = new Uri(requestString);
            if (invalidateCache)
            {
                stopsCache.Invalidate(requestUri);
            }

            stopsCache.DownloadStringAsync(requestUri, new GetStopsForLocationCompleted(requestString, stopsCache, callback).HttpCache_Completed);
        }

        private class GetStopsForLocationCompleted : ACallCompleted
        {
            private StopsForLocation_Callback callback;
            private HttpCache stopsCache;

            public GetStopsForLocationCompleted(string requestUrl, HttpCache stopsCache, StopsForLocation_Callback callback) : base(requestUrl)
            {
                this.callback = callback;
                this.stopsCache = stopsCache;
            }

            public override void ParseResults(TextReader results, Exception error)
            {
                List<Stop> stops = null;
                bool limitExceeded = false;

                if (error == null)
                {
                    XDocument xmlDoc = CheckResponseCode(results, requestUrl);
                    try
                    {

                        // parse all the routes
                        IList<Route> routes =
                            (from route in xmlDoc.Descendants("route")
                             select ParseRoute(route, xmlDoc.Descendants("agency"))).ToList<Route>();
                        IDictionary<string, Route> routesMap = new Dictionary<string, Route>();
                        foreach (Route r in routes)
                        {
                            routesMap.Add(r.id, r);
                        }

                        stops =
                            (from stop in xmlDoc.Descendants("stop")
                             select ParseStop
                                (
                                    stop,
                                    (from routeId in stop.Element("routeIds").Descendants("string")
                                     select routesMap[SafeGetValue(routeId)]).ToList<Route>()
                                )).ToList<Stop>();

                        IEnumerable<XElement> descendants = xmlDoc.Descendants("data");
                        if (descendants.Count() != 0)
                        {
                            limitExceeded = bool.Parse(SafeGetValue(descendants.First().Element("limitExceeded")));
                        }

                        Debug.Assert(limitExceeded == false);
                    }
                    catch (WebserviceResponseException ex)
                    {
                        error = ex;
                    }
                    catch (Exception ex)
                    {
                        error = new WebserviceParsingException(requestUrl, xmlDoc.ToString(), ex);
                    }
                }

                Debug.Assert(error == null);

                // Remove a page from the cache if we hit a parsing error.  This way we won't keep
                // invalid server data in the cache
                if (error != null)
                {
                    stopsCache.Invalidate(new Uri(requestUrl));
                }

                callback(stops, limitExceeded, error);
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
            directionCache.DownloadStringAsync(new Uri(requestUrl), new GetDirectionsForRouteCompleted(requestUrl, route.id, directionCache, callback).HttpCache_Completed);
        }

        private class GetDirectionsForRouteCompleted : ACallCompleted
        {
            private StopsForRoute_Callback callback;
            private string routeId;
            private HttpCache directionCache;

            public GetDirectionsForRouteCompleted(string requestUrl, string routeId, HttpCache directionCache, StopsForRoute_Callback callback) : base(requestUrl)
            {
                this.callback = callback;
                this.routeId = routeId;
                this.directionCache = directionCache;
            }

            public override void ParseResults(TextReader results, Exception error)
            {
                List<RouteStops> routeStops = null;

                if (error == null)
                {
                    XDocument xmlDoc = CheckResponseCode(results, requestUrl);
                    try
                    {

                        // parse all the routes
                        IList<Route> routes =
                            (from route in xmlDoc.Descendants("route")
                             select ParseRoute(route, xmlDoc.Descendants("agency"))).ToList<Route>();
                        IDictionary<string, Route> routesMap = new Dictionary<string, Route>();
                        foreach (Route r in routes)
                        {
                            routesMap.Add(r.id, r);
                        }

                        // parse all the stops, using previously parsed Route objects
                        IList<Stop> stops =
                            (from stop in xmlDoc.Descendants("stop")
                             select ParseStop(stop,
                                 (from routeId in stop.Element("routeIds").Descendants("string")
                                  select routesMap[SafeGetValue(routeId)]
                                      ).ToList<Route>()
                             )).ToList<Stop>();

                        IDictionary<string, Stop> stopsMap = new Dictionary<string, Stop>();
                        foreach (Stop s in stops)
                        {
                            stopsMap.Add(s.id, s);
                        }

                        // and put it all together
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
                                      select stopsMap[SafeGetValue(stopId)]).ToList<Stop>(),

                                 route = routesMap[routeId]

                             }).ToList<RouteStops>();
                    }
                    catch (WebserviceResponseException ex)
                    {
                        error = ex;
                    }
                    catch (Exception ex)
                    {
                        error = new WebserviceParsingException(requestUrl, xmlDoc.ToString(), ex);
                    }
                }
                Debug.Assert(error == null);

                // Remove a page from the cache if we hit a parsing error.  This way we won't keep
                // invalid server data in the cache
                if (error != null)
                {
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

            HttpWebRequest requestGetter = (HttpWebRequest)HttpWebRequest.Create(requestUrl);
            requestGetter.BeginGetResponse(
                new AsyncCallback(new GetArrivalsForStopCompleted(requestUrl, callback).HttpWebRequest_Completed),
                requestGetter);
        }

        private class GetArrivalsForStopCompleted : ACallCompleted
        {
            private ArrivalsForStop_Callback callback;

            public GetArrivalsForStopCompleted(string requestUrl, ArrivalsForStop_Callback callback) : base(requestUrl)
            {
                this.callback = callback;
            }

            public override void ParseResults(TextReader result, Exception error)
            {
                List<ArrivalAndDeparture> arrivals = null;

                if (error == null)
                {
                    XDocument xmlDoc = CheckResponseCode(result, requestUrl);
                    try
                    {
                        arrivals =
                            (from arrival in xmlDoc.Descendants("arrivalAndDeparture")
                             select ParseArrivalAndDeparture(arrival)).ToList<ArrivalAndDeparture>();
                    }
                    catch (Exception ex)
                    {
                        error = new WebserviceParsingException(requestUrl, xmlDoc.ToString(), ex);
                    }
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

            HttpWebRequest requestGetter = (HttpWebRequest)HttpWebRequest.Create(requestUrl);
            requestGetter.BeginGetResponse(
                new AsyncCallback(new GetScheduleForStopCompleted(requestUrl, callback).HttpWebRequest_Completed),
                requestGetter);
        }

        private class GetScheduleForStopCompleted : ACallCompleted
        {
            private ScheduleForStop_Callback callback;

            public GetScheduleForStopCompleted(string requestUrl, ScheduleForStop_Callback callback) : base(requestUrl)
            {
                this.callback = callback;
            }

            public override void ParseResults(TextReader result, Exception error)
            {
                List<RouteSchedule> schedules = null;

                if (error == null)
                {
                    XDocument xmlDoc = CheckResponseCode(result, requestUrl);
                    try
                    {
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
                    catch (Exception ex)
                    {
                        error = new WebserviceParsingException(requestUrl, xmlDoc.ToString(), ex);
                    }
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

            HttpWebRequest requestGetter = (HttpWebRequest)HttpWebRequest.Create(requestUrl);
            requestGetter.BeginGetResponse(
                new AsyncCallback(new TripDetailsForArrivalCompleted(requestUrl, callback).HttpWebRequest_Completed),
                requestGetter);
        }

        private class TripDetailsForArrivalCompleted : ACallCompleted
        {
            private TripDetailsForArrival_Callback callback;

            public TripDetailsForArrivalCompleted(string requestUrl, TripDetailsForArrival_Callback callback) : base(requestUrl)
            {
                this.callback = callback;
            }

            public override void ParseResults(TextReader result, Exception error)
            {
                TripDetails tripDetail = null;

                if (error == null)
                {
                    XDocument xmlDoc = CheckResponseCode(result, requestUrl);
                    try
                    {

                        tripDetail =
                            (from trip in xmlDoc.Descendants("entry")
                             select ParseTripDetails(trip)).First();
                    }
                    catch (WebserviceResponseException ex)
                    {
                        error = ex;
                    }
                    catch (Exception ex)
                    {
                        error = new WebserviceParsingException(requestUrl, xmlDoc.ToString(), ex);
                    }
                }

                Debug.Assert(error == null);

                callback(tripDetail, error);
            }
        }

        #endregion

        #region Structure parsing code

        private static TripDetails ParseTripDetails(XElement trip)
        {
            TripDetails tripDetails = new TripDetails();

            tripDetails.tripId = SafeGetValue(trip.Element("tripId"));

            XElement statusElement;
            if (trip.Element("tripStatus") != null)
            {
                // ArrivalsForStop returns the status element as 'tripStatus'
                statusElement = trip.Element("tripStatus");
            }
            else if (trip.Element("status") != null)
            {
                // The TripDetails query returns 'status'
                statusElement = trip.Element("status");
            }
            else
            {
                // No status available, stop parsing here
                return tripDetails;
            }

            tripDetails.serviceDate = UnixTimeToDateTime(long.Parse(SafeGetValue(statusElement.Element("serviceDate"))));
            if (string.IsNullOrEmpty(SafeGetValue(statusElement.Element("predicted"))) == false 
                && bool.Parse(SafeGetValue(statusElement.Element("predicted"))) == true)
            {
                tripDetails.scheduleDeviationInSec = int.Parse(SafeGetValue(statusElement.Element("scheduleDeviation")));
                tripDetails.closestStopId = SafeGetValue(statusElement.Element("closestStop"));
                tripDetails.closestStopTimeOffset = int.Parse(SafeGetValue(statusElement.Element("closestStopTimeOffset")));

                if (statusElement.Element("position") != null)
                {
                    tripDetails.location = new GeoCoordinate(
                        double.Parse(SafeGetValue(statusElement.Element("position").Element("lat")), NumberFormatInfo.InvariantInfo),
                        double.Parse(SafeGetValue(statusElement.Element("position").Element("lon")), NumberFormatInfo.InvariantInfo)
                        );
                }
            }

            return tripDetails;
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
                status = SafeGetValue(arrival.Element("status")),
                tripDetails = ParseTripDetails(arrival)
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
                    double.Parse(SafeGetValue(stop.Element("lat")), NumberFormatInfo.InvariantInfo),
                    double.Parse(SafeGetValue(stop.Element("lon")), NumberFormatInfo.InvariantInfo)
                    ),
                name = SafeGetValue(stop.Element("name")),
                routes = routes
            };
        }

        private static XDocument CheckResponseCode(TextReader xmlResponse, string requestUrl)
        {
            try
            {
                XDocument xmlDoc = XDocument.Load(xmlResponse);
                HttpStatusCode code = (HttpStatusCode)int.Parse(xmlDoc.Element("response").Element("code").Value);

                if (code != HttpStatusCode.OK)
                {
                    Debug.Assert(false);
                    throw new WebserviceResponseException(code, requestUrl, xmlDoc.ToString(), null);
                }
                return xmlDoc;
            }
            finally
            {
                xmlResponse.Close();
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

        #region Internal/Private Methods

        internal GeoCoordinate GetRoundedLocation(GeoCoordinate location)
        {
            //// Round off coordinates so that we can exploit caching
            double lat = Math.Round(location.Latitude * multiplier, roundingLevel) / multiplier;
            double lon = Math.Round(location.Longitude * multiplier, roundingLevel) / multiplier;

            // Round off the extra decimal places to prevent double precision issues
            // from causing multiple cache entires
            GeoCoordinate roundedLocation = new GeoCoordinate(
                Math.Round(lat, roundingLevel + 1),
                Math.Round(lon, roundingLevel + 1)
            );

            return roundedLocation;
        }

        private static DateTime UnixTimeToDateTime(long unixTime)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(unixTime);
        }

        #endregion

        internal void ClearCache()
        {
            stopsCache.Clear();
            directionCache.Clear();
        }

        internal void SaveCache()
        {
            stopsCache.Save();
            directionCache.Save();
        }
    }
}
