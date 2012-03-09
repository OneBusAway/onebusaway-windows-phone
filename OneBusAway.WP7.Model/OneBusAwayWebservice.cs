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

        #endregion

        #region Delegates

        public delegate void StopsForLocation_Callback(List<Stop> stops, bool limitExceeded);
        public delegate void RoutesForLocation_Callback(List<Route> routes);
        public delegate void StopsForRoute_Callback(List<RouteStops> routeStops);
        public delegate void ArrivalsForStop_Callback(List<ArrivalAndDeparture> arrivals);
        public delegate void ScheduleForStop_Callback(List<RouteSchedule> schedules);
        public delegate void TripDetailsForArrival_Callback(TripDetails tripDetail);

        #endregion

        #region Constructor

        public OneBusAwayWebservice()
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
                delegate(IAsyncResult asyncResult)
                {
                    XDocument xmlResponse = ValidateWebCallback(asyncResult);;
                    List<Route> routes = null;

                    try
                    {
                        routes =
                            (from route in xmlResponse.Descendants("route")
                             select ParseRoute(route, xmlResponse.Descendants("agency"))).ToList<Route>();
                    }
                    catch (Exception ex)
                    {
                        Exception error = new WebserviceParsingException(requestUrl, xmlResponse.ToString(), ex);
                        throw error;
                    }

                    callback(routes);
                },
                requestGetter);
        }

        public void StopsForLocation(GeoCoordinate location, string query, int radiusInMeters, int maxCount, bool invalidateCache, StopsForLocation_Callback callback)
        {
            GeoCoordinate roundedLocation = GetRoundedLocation(location);

            // ditto for the search radius -- nearest 50 meters for caching
            int roundedRadius = (int)(Math.Round(radiusInMeters / 50.0) * 50);

            string requestUrl = string.Format(
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
                requestUrl += string.Format("&query={0}", query);
            }

            if (maxCount > 0)
            {
                requestUrl += string.Format("&maxCount={0}", maxCount);
            }

            Uri requestUri = new Uri(requestUrl);
            HttpWebRequest requestGetter = (HttpWebRequest)HttpWebRequest.Create(requestUri);
            requestGetter.BeginGetResponse(
                delegate(IAsyncResult asyncResult)
                {
                    XDocument xmlResponse = ValidateWebCallback(asyncResult);
                    List<Stop> stops = null;
                    bool limitExceeded = false;

                    try
                    {
                        IDictionary<string, Route> routesMap = ParseAllRoutes(xmlResponse);

                        stops =
                            (from stop in xmlResponse.Descendants("stop")
                                select ParseStop
                                (
                                    stop,
                                    (from routeId in stop.Element("routeIds").Descendants("string")
                                        select routesMap[SafeGetValue(routeId)]).ToList<Route>()
                                )).ToList<Stop>();

                        IEnumerable<XElement> descendants = xmlResponse.Descendants("data");
                        if (descendants.Count() != 0)
                        {
                            limitExceeded = bool.Parse(SafeGetValue(descendants.First().Element("limitExceeded")));
                        }

                        Debug.Assert(limitExceeded == false);
                    }
                    catch (Exception ex)
                    {
                        Exception error = new WebserviceParsingException(requestUrl, xmlResponse.ToString(), ex);
                        throw error;
                    }

                    callback(stops, limitExceeded);
                },
                requestGetter);
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

            HttpWebRequest requestGetter = (HttpWebRequest)HttpWebRequest.Create(requestUrl);
            requestGetter.BeginGetResponse(delegate(IAsyncResult asyncResult)
                {
                    XDocument xmlResponse = ValidateWebCallback(asyncResult);
                    List<RouteStops> routeStops = null;

                    try
                    {
                        IDictionary<string, Route> routesMap = ParseAllRoutes(xmlResponse);

                        // parse all the stops, using previously parsed Route objects
                        IList<Stop> stops =
                            (from stop in xmlResponse.Descendants("stop")
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
                            (from stopGroup in xmlResponse.Descendants("stopGroup")
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

                                 route = routesMap[route.id]

                             }).ToList<RouteStops>();
                    }
                    catch (Exception ex)
                    {
                        Exception error = new WebserviceParsingException(requestUrl, xmlResponse.ToString(), ex);
                        throw error;
                    }

                    callback(routeStops);
                },
                requestGetter);
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
            requestGetter.BeginGetResponse(delegate(IAsyncResult asyncResult)
                {
                    XDocument xmlResponse = ValidateWebCallback(asyncResult);
                    List<ArrivalAndDeparture> arrivals = null;

                    try
                    {
                        arrivals =
                            (from arrival in xmlResponse.Descendants("arrivalAndDeparture")
                                select ParseArrivalAndDeparture(arrival)).ToList<ArrivalAndDeparture>();
                    }
                    catch (Exception ex)
                    {
                        Exception error = new WebserviceParsingException(requestUrl, xmlResponse.ToString(), ex);
                        throw error;
                    }

                    callback(arrivals);
                },
                requestGetter);
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
            requestGetter.BeginGetResponse(delegate(IAsyncResult asyncResult)
                {
                    XDocument xmlResponse = ValidateWebCallback(asyncResult);
                    List<RouteSchedule> schedules = null;

                    try
                    {
                        IDictionary<string, Route> routesMap = ParseAllRoutes(xmlResponse);

                        schedules =
                            (from schedule in xmlResponse.Descendants("stopRouteSchedule")
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

                             }).ToList<RouteSchedule>();
                    }
                    catch (Exception ex)
                    {
                        Exception error = new WebserviceParsingException(requestUrl, xmlResponse.ToString(), ex);
                        throw error;
                    }

                    callback(schedules);
                },
                requestGetter);
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
            requestGetter.BeginGetResponse(delegate(IAsyncResult asyncResult)
                {
                    XDocument xmlResponse = ValidateWebCallback(asyncResult);
                    TripDetails tripDetail = null;

                    try
                    {
                        tripDetail =
                            (from trip in xmlResponse.Descendants("entry")
                             select ParseTripDetails(trip)).First();
                    }
                    catch (Exception ex)
                    {
                        Exception error = new WebserviceParsingException(requestUrl, xmlResponse.ToString(), ex);
                        throw error;
                    }

                    callback(tripDetail);
                },
                requestGetter);
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

        private XDocument ValidateWebCallback(IAsyncResult asyncResult)
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
                TextReader xmlResponse = new StreamReader(response.GetResponseStream());
                string requestUrl = request.RequestUri.ToString();
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
        }

        #endregion

        # region Public static methods

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
