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
using System.Reflection;
using System.Threading;
using System.IO.IsolatedStorage;

namespace OneBusAway.WP7.Model
{
    internal class OneBusAwayWebservice
    {

        #region Private Variables

        private const string REGIONS_XML_FILE = "Regions.xml";
        private static readonly object regionsLock = new object();
        private static Region[] discoveredRegions;
        
        /// <summary>
        /// This is the URL of the regions web service.
        /// </summary>
        private const string REGIONS_SERVICE_URI = "http://regions.onebusaway.org/regions.xml";

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

        #region Properties

        /// <summary>
        /// An array of regions supported by OneBusAway.org.
        /// </summary>
        internal static Region[] Regions
        {
            get
            {
                if (discoveredRegions == null)
                {
                    lock (regionsLock)
                    {
                        if (discoveredRegions == null)
                        {
                            XDocument regionsDoc = null;
                            AutoResetEvent resetEvent = new AutoResetEvent(false);
                            try
                            {
                                // First try and read the regions.xml file from isolated storage:
                                using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
                                {
                                    if (appStorage.FileExists(REGIONS_XML_FILE) == true)
                                    {
                                        var creationTime = appStorage.GetCreationTime(REGIONS_XML_FILE);
                                        if ((DateTime.Now - creationTime).TotalDays <= 7)
                                        {
                                            using (var streamReader = new StreamReader(appStorage.OpenFile(REGIONS_XML_FILE, FileMode.Open)))
                                            {
                                                string xml = streamReader.ReadToEnd();
                                                regionsDoc = XDocument.Parse(xml);
                                            }
                                        }
                                    }
                                }

                                if (regionsDoc == null)
                                {
                                    var webRequest = WebRequest.CreateHttp(REGIONS_SERVICE_URI);
                                    var asyncResult = webRequest.BeginGetResponse(result => resetEvent.Set(), webRequest);

                                    // Not the best wy to handle this...but we shouldn't block for long.
                                    resetEvent.WaitOne(5000);
                                    var response = (HttpWebResponse)webRequest.EndGetResponse(asyncResult);

                                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                                    {
                                        string xml = streamReader.ReadToEnd();
                                        regionsDoc = XDocument.Parse(xml);
                                    }

                                    // Save the regions.xml file to isolated storage:
                                    using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
                                    {
                                        using (var streamWriter = new StreamWriter(appStorage.OpenFile(REGIONS_XML_FILE, FileMode.OpenOrCreate)))
                                        {
                                            regionsDoc.Save(streamWriter);
                                        }
                                    }
                                }
                            }
                            catch
                            {
                            }
                            finally
                            {
                                resetEvent.Dispose();
                            }

                            // If we make it here, use the backup regions.xml file:
                            if (regionsDoc == null)
                            {
                                Assembly assembly = typeof(OneBusAwayWebservice).Assembly;
                                using (var streamReader = new StreamReader(assembly.GetManifestResourceStream("OneBusAway.WP7.Model.Regions.xml")))
                                {
                                    string xml = streamReader.ReadToEnd();
                                    regionsDoc = XDocument.Parse(xml);
                                }
                            }

                            discoveredRegions = (from regionElement in regionsDoc.Descendants("region")
                                                 let region = new Region(regionElement)
                                                 where region.IsActive && region.SupportsObaRealtimeApis && region.SupportsObaDiscoveryApis
                                                 select region).ToArray();
                        }
                    }
                }

                return discoveredRegions;
            }
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
                        // Reading the server response will probably fail since the request was unsuccessful
                        // so just return null as the server response. Also, from my personal testing this
                        // code is unreachable: if the server returns a 404 request.EndGetResponse() will
                        // throw an exception.
                        throw new WebserviceResponseException(response.StatusCode, request.RequestUri.ToString(), null, null);
                    }
                    else
                    {
                        XDocument xmlResponse = CheckResponseCode(new StreamReader(response.GetResponseStream()), request.RequestUri.ToString());
                        ParseResults(xmlResponse, null);
                    }
                }
                catch (Exception e)
                {
                    ParseResults(null, e);
                }
            }

            /// <summary>
            /// Callback entry point for calls based on HttpCache
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            public void HttpCache_Completed(object sender, HttpCache.CacheDownloadStringCompletedEventArgs e)
            {
                Exception error = e.Error;
                XDocument xmlDoc = null;
                
                if (error == null)
                {
                    try
                    {
                        string requestUrl = string.Empty;
                        if (e.UserState is Uri)
                        {
                            requestUrl = ((Uri)e.UserState).ToString();
                        }

                        xmlDoc = CheckResponseCode(e.Result, requestUrl);
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                }

                ParseResults(xmlDoc, error);
            }

            private static XDocument CheckResponseCode(TextReader xmlResponse, string requestUrl)
            {
                XDocument xmlDoc = null;
                HttpStatusCode code = HttpStatusCode.Unused;

                try
                {
                    xmlDoc = XDocument.Load(xmlResponse);
                    code = (HttpStatusCode)int.Parse(xmlDoc.Element("response").Element("code").Value);
                }
                catch (Exception e)
                {
                    // Any exception thrown in this code means either A) the server response wasn't XML so XDocument.Load() failed or
                    // B) the code element doesn't exist so the server response is invalid. The known cause for these things to
                    // fail (besides a server malfunction) is the phone being connected to a WIFI access point which requires
                    // a login page, so we get the hotspot login page back instead of our web request.
                    Debug.Assert(false);

                    throw new WebserviceResponseException(HttpStatusCode.Unused, requestUrl, xmlResponse.ReadToEnd(), e);
                }
                finally
                {
                    xmlResponse.Close();
                }

                if (code != HttpStatusCode.OK)
                {
                    Debug.Assert(false);

                    throw new WebserviceResponseException(code, requestUrl, xmlDoc.ToString(), null);
                }

                return xmlDoc;
            }

            /// <summary>
            /// Subclasses should implement this by pulling data out of the specified reader, parsing it, and invoking the desired callback.
            /// </summary>
            /// <param name="result"></param>
            /// <param name="error"></param>
            public abstract void ParseResults(XDocument result, Exception error);
        }

        public void RoutesForLocation(GeoCoordinate location, string query, int radiusInMeters, int maxCount, RoutesForLocation_Callback callback)
        {
            string requestUrl = string.Format(
                "{0}/{1}.xml?key={2}&lat={3}&lon={4}&radius={5}&Version={6}",
                WebServiceUrlForLocation(location),
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

            public override void ParseResults(XDocument xmlDoc, Exception error)
            {
                List<Route> routes = new List<Route>();
                if (xmlDoc == null || error != null)
                {
                    callback(routes, error);
                }
                else
                {
                    try
                    {
                        routes.AddRange(from route in xmlDoc.Descendants("route")
                                        select ParseRoute(route, xmlDoc.Descendants("agency")));
                    }
                    catch (Exception ex)
                    {
                        error = new WebserviceParsingException(requestUrl, xmlDoc.ToString(), ex);
                    }

                    Debug.Assert(error == null);

                    callback(routes, error);
                }
            }
        }


        public void StopsForLocation(GeoCoordinate location, string query, int radiusInMeters, int maxCount, bool invalidateCache, StopsForLocation_Callback callback)
        {
            GeoCoordinate roundedLocation = GetRoundedLocation(location);

            // ditto for the search radius -- nearest 50 meters for caching
            int roundedRadius = (int)(Math.Round(radiusInMeters / 50.0) * 50);

            string requestString = string.Format(
                "{0}/{1}.xml?key={2}&lat={3}&lon={4}&radius={5}&Version={6}",
                WebServiceUrlForLocation(location),
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

            public override void ParseResults(XDocument xmlDoc, Exception error)
            {
                List<Stop> stops = new List<Stop>(); ;
                bool limitExceeded = false;

                if (xmlDoc == null || error != null)
                {
                    callback(stops, limitExceeded, error);
                }
                else
                {
                    try
                    {
                        IDictionary<string, Route> routesMap = ParseAllRoutes(xmlDoc);

                        stops.AddRange(from stop in xmlDoc.Descendants("stop")
                                       select ParseStop(
                                       stop,
                                       (from routeId in stop.Element("routeIds").Descendants("string")
                                        select routesMap[SafeGetValue(routeId)]).ToList<Route>()));                                        

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

        public void StopsForRoute(GeoCoordinate location, Route route, StopsForRoute_Callback callback)
        {
            string requestUrl = string.Format(
                "{0}/{1}/{2}.xml?key={3}&Version={4}",
                WebServiceUrlForLocation(location),
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

            public override void ParseResults(XDocument xmlDoc, Exception error)
            {
                List<RouteStops> routeStops = new List<RouteStops>();

                if (xmlDoc == null || error != null)
                {
                    callback(routeStops, error);
                }
                else
                {
                    try
                    {
                        IDictionary<string, Route> routesMap = ParseAllRoutes(xmlDoc);

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
                        routeStops.AddRange
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

                             });
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

        public void ArrivalsForStop(GeoCoordinate location, Stop stop, ArrivalsForStop_Callback callback)
        {
            string requestUrl = string.Format(
                "{0}/{1}/{2}.xml?minutesAfter={3}&key={4}&Version={5}",
                WebServiceUrlForLocation(location),
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

            public override void ParseResults(XDocument xmlDoc, Exception error)
            {
                List<ArrivalAndDeparture> arrivals = new List<ArrivalAndDeparture>();

                if (xmlDoc == null || error != null)
                {
                    callback(arrivals, error);
                }
                else
                {
                    try
                    {
                        arrivals.AddRange(from arrival in xmlDoc.Descendants("arrivalAndDeparture")
                                          select ParseArrivalAndDeparture(arrival));
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

        public void ScheduleForStop(GeoCoordinate location, Stop stop, ScheduleForStop_Callback callback)
        {
            string requestUrl = string.Format(
                "{0}/{1}/{2}.xml?key={3}&Version={4}",
                WebServiceUrlForLocation(location),
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

            public override void ParseResults(XDocument xmlDoc, Exception error)
            {
                List<RouteSchedule> schedules = new List<RouteSchedule>();

                if (xmlDoc == null || error != null)
                {
                    callback(schedules, error);
                }
                else
                {
                    try
                    {
                        IDictionary<string, Route> routesMap = ParseAllRoutes(xmlDoc);

                        schedules.AddRange
                            (from schedule in xmlDoc.Descendants("stopRouteSchedule")
                             select new RouteSchedule
                             {
                                 route = routesMap[SafeGetValue(schedule.Element("routeId"))],

                                 directions =
                                     (from direction in schedule.Descendants("stopRouteDirectionSchedule")
                                      select new DirectionSchedule
                                      {
                                          tripHeadsign = SafeGetValue(direction.Element("tripHeadsign")),

                                          trips =
                                          (from trip in direction.Descendants("scheduleStopTime")
                                           select ParseScheduleStopTime(trip)).ToList<ScheduleStopTime>()
                                      }).ToList<DirectionSchedule>()

                             });
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

        public void TripDetailsForArrival(GeoCoordinate location, ArrivalAndDeparture arrival, TripDetailsForArrival_Callback callback)
        {
            string requestUrl = string.Format(
                "{0}/{1}/{2}.xml?key={3}&includeSchedule={4}",
                WebServiceUrlForLocation(location),
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

            public override void ParseResults(XDocument xmlDoc, Exception error)
            {
                TripDetails tripDetail = new TripDetails();

                if (xmlDoc == null || error != null)
                {
                    callback(tripDetail, error);
                }
                else
                {
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

            // TODO: Log a warning for when the serviceDate is invalid. This might be a OBA bug, but I don't
            // have the debugging info to prove it
            string serviceDate = SafeGetValue(statusElement.Element("serviceDate"));
            if (string.IsNullOrEmpty(serviceDate) == false)
            {
                long serviceDateLong;
                bool success = long.TryParse(serviceDate, out serviceDateLong);
                if (success)
                {
                    tripDetails.serviceDate = UnixTimeToDateTime(serviceDateLong);
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

        /// <summary>
        /// Parses all the routes in the document.
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <returns>A map of route id to route object</returns>
        private static IDictionary<string, Route> ParseAllRoutes(XDocument xmlDoc)
        {
            IList<Route> routes =
                (from route in xmlDoc.Descendants("route")
                    select ParseRoute(route, xmlDoc.Descendants("agency"))).ToList<Route>();
            IDictionary<string, Route> routesMap = new Dictionary<string, Route>();
            foreach (Route r in routes)
            {
                routesMap.Add(r.id, r);
            }
            return routesMap;
        }

        private static ScheduleStopTime ParseScheduleStopTime(XElement trip)
        {
            return new ScheduleStopTime()
            {
                arrivalTime = UnixTimeToDateTime(long.Parse(SafeGetValue(trip.Element("arrivalTime"), "0"))),
                departureTime = UnixTimeToDateTime(long.Parse(SafeGetValue(trip.Element("departureTime"), "0"))),
                serviceId = SafeGetValue(trip.Element("serviceId")),
                tripId = SafeGetValue(trip.Element("tripId"))
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

        private static DateTime UnixTimeToDateTime(long unixTime)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(unixTime);
        }

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

        #endregion

        # region Public static methods

        /// <summary>
        /// Connects to the regions webservice to find the URL of the closets server to us so
        /// that we can support multiple regions.
        /// </summary>
        public static string WebServiceUrlForLocation(GeoCoordinate location)
        {
            // Find the region closets to us and return it's URL:
            return ClosestRegion(location).RegionUrl;
        }

        /// <summary>
        /// Finds the closest region to the current location.
        /// </summary>
        public static Region ClosestRegion(GeoCoordinate location)
        {
            return (from region in Regions
                    let distance = region.DistanceFrom(location.Latitude, location.Longitude)
                    orderby distance ascending
                    select region).First();
        }

        public static GeoCoordinate GetRoundedLocation(GeoCoordinate location)
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

        #endregion
    }
}
