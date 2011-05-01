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
using OneBusAway.WP7.ViewModel;
using System.Device.Location;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using System.Diagnostics;
using OneBusAway.WP7.ViewModel.EventArgs;
using Microsoft.Phone.Controls.Maps;
using OneBusAway.WP7.ViewModel.LocationServiceDataStructures;

namespace OneBusAway.WP7.Model
{
    public class BusServiceModel : IBusServiceModel
    {
        private OneBusAwayWebservice webservice;

        #region Events

        public event EventHandler<CombinedInfoForLocationEventArgs> CombinedInfoForLocation_Completed;
        public event EventHandler<StopsForLocationEventArgs> StopsForLocation_Completed;
        public event EventHandler<RoutesForLocationEventArgs> RoutesForLocation_Completed;
        public event EventHandler<StopsForRouteEventArgs> StopsForRoute_Completed;
        public event EventHandler<ArrivalsForStopEventArgs> ArrivalsForStop_Completed;
        public event EventHandler<ScheduleForStopEventArgs> ScheduleForStop_Completed;
        public event EventHandler<TripDetailsForArrivalEventArgs> TripDetailsForArrival_Completed;
        public event EventHandler<SearchForRoutesEventArgs> SearchForRoutes_Completed;
        public event EventHandler<SearchForStopsEventArgs> SearchForStops_Completed;
        public event EventHandler<LocationForAddressEventArgs> LocationForAddress_Completed;


        #endregion

        #region Constructor/Singleton

        // TODO we really need to get rid of this singleton and move to a better dependency injection model at some point.

        public static BusServiceModel Singleton = new BusServiceModel();

        private BusServiceModel()
        {
        }

        #endregion

        /// <summary>
        /// Scan the list of stops to find all associated routes.
        /// </summary>
        /// <param name="stops"></param>
        /// <param name="location">Center location used to find closestStop on each route.</param>
        /// <returns></returns>
        private List<Route> GetRoutesFromStops(List<Stop> stops, GeoCoordinate location)
        {
            IDictionary<string, Route> routesMap = new Dictionary<string, Route>();
            stops.Sort(new StopDistanceComparer(location));

            foreach (Stop stop in stops)
            {
                foreach (Route route in stop.routes)
                {
                    if (!routesMap.ContainsKey(route.id))
                    {
                        // the stops are sorted in distance order.
                        // so if we haven't already seen this route, then this is the closest stop.
                        route.closestStop = stop;
                        routesMap.Add(route.id, route);
                    }
                }
            }
            return routesMap.Values.ToList<Route>();
        }

        #region Public Methods

        public void Initialize()
        {
            webservice = new OneBusAwayWebservice();
        }

        public bool AreLocationsEquivalent(GeoCoordinate location1, GeoCoordinate location2)
        {
            return OneBusAwayWebservice.GetRoundedLocation(location1) == OneBusAwayWebservice.GetRoundedLocation(location2);
        }

        public void CombinedInfoForLocation(GeoCoordinate location, int radiusInMeters)
        {
            CombinedInfoForLocation(location, radiusInMeters, -1);
        }

        public void CombinedInfoForLocation(GeoCoordinate location, int radiusInMeters, int maxCount)
        {
            CombinedInfoForLocation(location, radiusInMeters, maxCount, false);
        }

        public void CombinedInfoForLocation(GeoCoordinate location, int radiusInMeters, int maxCount, bool invalidateCache)
        {
            webservice.StopsForLocation(
                location,
                null,
                radiusInMeters,
                maxCount,
                invalidateCache,
                delegate(List<Stop> stops, bool limitExceeded, Exception e)
                {
                    Exception error = e;
                    List<Route> routes = new List<Route>();

                    try
                    {
                        if (error == null)
                        {
                            routes = GetRoutesFromStops(stops, location);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Assert(false);
                        error = ex;
                    }

                    if (CombinedInfoForLocation_Completed != null)
                    {
                        CombinedInfoForLocation_Completed(this, new ViewModel.EventArgs.CombinedInfoForLocationEventArgs(stops, routes, location, error));
                    }
                }
            );
        }

        public void StopsForLocation(GeoCoordinate location, int radiusInMeters)
        {
            StopsForLocation(location, radiusInMeters, -1);
        }

        public void StopsForLocation(GeoCoordinate location, int radiusInMeters, int maxCount)
        {
            StopsForLocation(location, radiusInMeters, maxCount, false);
        }

        public void StopsForLocation(GeoCoordinate location, int radiusInMeters, int maxCount, bool invalidateCache)
        {
            webservice.StopsForLocation(
                location,
                null,
                radiusInMeters,
                maxCount,
                invalidateCache,
                delegate(List<Stop> stops, bool limitExceeded, Exception error)
                {
                    if (StopsForLocation_Completed != null)
                    {
                        StopsForLocation_Completed(this, new ViewModel.EventArgs.StopsForLocationEventArgs(stops, location, limitExceeded, error));
                    }
                }
            );
        }

        public void RoutesForLocation(GeoCoordinate location, int radiusInMeters)
        {
            RoutesForLocation(location, radiusInMeters, -1);
        }

        public void RoutesForLocation(GeoCoordinate location, int radiusInMeters, int maxCount)
        {
            RoutesForLocation(location, radiusInMeters, maxCount, false);
        }

        public void RoutesForLocation(GeoCoordinate location, int radiusInMeters, int maxCount, bool invalidateCache)
        {
            webservice.StopsForLocation(
                location,
                null,
                radiusInMeters,
                maxCount,
                invalidateCache,
                delegate(List<Stop> stops, bool limitExceeded, Exception e)
                {
                    Exception error = e;
                    List<Route> routes = new List<Route>();

                    try
                    {
                        if (error == null)
                        {
                            routes = GetRoutesFromStops(stops, location);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Assert(false);
                        error = ex;
                    }

                    if (RoutesForLocation_Completed != null)
                    {
                        RoutesForLocation_Completed(this, new ViewModel.EventArgs.RoutesForLocationEventArgs(routes, location, error));
                    }
                }
            );
        }

        public void StopsForRoute(Route route)
        {
            webservice.StopsForRoute(
                route,
                delegate(List<RouteStops> routeStops, Exception error)
                {
                    if (StopsForRoute_Completed != null)
                    {
                        StopsForRoute_Completed(this, new ViewModel.EventArgs.StopsForRouteEventArgs(route, routeStops, error));
                    }
                }
            );
        }

        public void ArrivalsForStop(Stop stop)
        {
            webservice.ArrivalsForStop(
                stop,
                delegate(List<ArrivalAndDeparture> arrivals, Exception error)
                {
                    if (ArrivalsForStop_Completed != null)
                    {
                        ArrivalsForStop_Completed(this, new ViewModel.EventArgs.ArrivalsForStopEventArgs(stop, arrivals, error));
                    }
                }
            );
        }

        public void ScheduleForStop(Stop stop)
        {
            webservice.ScheduleForStop(
                stop,
                delegate(List<RouteSchedule> schedule, Exception error)
                {
                    if (ScheduleForStop_Completed != null)
                    {
                        ScheduleForStop_Completed(this, new ViewModel.EventArgs.ScheduleForStopEventArgs(stop, schedule, error));
                    }
                }
            );
        }

        public void TripDetailsForArrivals(List<ArrivalAndDeparture> arrivals)
        {
            int count = 0;
            List<TripDetails> tripDetails = new List<TripDetails>(arrivals.Count);
            Exception overallError = null;

            if (arrivals.Count == 0)
            {
                if (TripDetailsForArrival_Completed != null)
                {
                    TripDetailsForArrival_Completed(
                        this,
                        new ViewModel.EventArgs.TripDetailsForArrivalEventArgs(arrivals, tripDetails, overallError)
                        );
                }
            }
            else
            {
                arrivals.ForEach(arrival =>
                    {
                        webservice.TripDetailsForArrival(
                            arrival,
                            delegate(TripDetails tripDetail, Exception error)
                            {
                                if (error != null)
                                {
                                    overallError = error;
                                }
                                else
                                {
                                    tripDetails.Add(tripDetail);
                                }

                                // Is this code thread-safe?
                                count++;
                                if (count == arrivals.Count && TripDetailsForArrival_Completed != null)
                                {
                                    TripDetailsForArrival_Completed(this, new ViewModel.EventArgs.TripDetailsForArrivalEventArgs(arrivals, tripDetails, error));
                                }
                            }
                        );
                    }
                );
            }
        }

        public void SearchForRoutes(GeoCoordinate location, string query)
        {
            SearchForRoutes(location, query, 1000000, -1);
        }

        public void SearchForRoutes(GeoCoordinate location, string query, int radiusInMeters, int maxCount)
        {
            webservice.RoutesForLocation(
                location,
                query,
                radiusInMeters,
                maxCount,
                delegate(List<Route> routes, Exception error)
                {
                    if (SearchForRoutes_Completed != null)
                    {
                        SearchForRoutes_Completed(this, new ViewModel.EventArgs.SearchForRoutesEventArgs(routes, location, query, error));
                    }
                }
            );
        }

        public void SearchForStops(GeoCoordinate location, string query)
        {
            SearchForStops(location, query, 1000000, -1);
        }

        public void SearchForStops(GeoCoordinate location, string query, int radiusInMeters, int maxCount)
        {
            webservice.StopsForLocation(
                location,
                query,
                radiusInMeters,
                maxCount,
                false,
                delegate(List<Stop> stops, bool limitExceeded, Exception error)
                {
                    if (SearchForStops_Completed != null)
                    {
                        SearchForStops_Completed(this, new ViewModel.EventArgs.SearchForStopsEventArgs(stops, location, query, error));
                    }
                }
            );
        }

        public void LocationForAddress(string query, GeoCoordinate searchNearLocation)
        {
            string bingMapAPIURL = "http://dev.virtualearth.net/REST/v1/Locations";
            string requestUrl = string.Format(
                "{0}?query={1}&key={2}&o=xml&userLocation={3}",
                bingMapAPIURL,
                query.Replace('&', ' '),
                "ApSTUUj6aWA3MIgccEpN30BT7T84k1Npvnx5bDOLkFA_OLMxvirZeGLWODPZlqXm",
                string.Format("{0},{1}", searchNearLocation.Latitude, searchNearLocation.Longitude)
            );

            WebClient client = new WebClient();
            client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(
                new GetLocationForAddressCompleted(requestUrl,
                        delegate(List<LocationForQuery> locations, Exception error)
                        {
                            if (LocationForAddress_Completed != null)
                            {
                                LocationForAddress_Completed(this, new ViewModel.EventArgs.LocationForAddressEventArgs(
                                        locations,
                                        query,
                                        searchNearLocation,
                                        error
                                        ));
                            }
                        }
                    ).LocationForAddress_Completed);
            client.DownloadStringAsync(new Uri(requestUrl));
        }

        public delegate void LocationForAddress_Callback(List<LocationForQuery> locations, Exception error);
        private class GetLocationForAddressCompleted
        {
            private LocationForAddress_Callback callback;
            private string requestUrl;

            public GetLocationForAddressCompleted(string requestUrl, LocationForAddress_Callback callback)
            {
                this.callback = callback;
                this.requestUrl = requestUrl;
            }

            public void LocationForAddress_Completed(object sender, DownloadStringCompletedEventArgs e)
            {
                Exception error = e.Error;
                List<LocationForQuery> locations = null;
                
                try
                {
                    if (error == null)
                    {
                        XDocument xmlDoc = XDocument.Load(new StringReader(e.Result));

                        XNamespace ns = "http://schemas.microsoft.com/search/local/ws/rest/v1";

                        locations = (from location in xmlDoc.Descendants(ns + "Location")
                               select new LocationForQuery
                               {
                                   location = new GeoCoordinate(
                                       Convert.ToDouble(location.Element(ns + "Point").Element(ns + "Latitude").Value),
                                       Convert.ToDouble(location.Element(ns + "Point").Element(ns + "Longitude").Value)
                                       ),
                                    name = location.Element(ns + "Name").Value,
                                    confidence = (Confidence)Enum.Parse(
                                        typeof(Confidence),
                                        location.Element(ns + "Confidence").Value,
                                        true
                                        ),
                                   boundingBox = new LocationRect(
                                        Convert.ToDouble(location.Element(ns + "BoundingBox").Element(ns + "NorthLatitude").Value),
                                        Convert.ToDouble(location.Element(ns + "BoundingBox").Element(ns + "WestLongitude").Value),
                                        Convert.ToDouble(location.Element(ns + "BoundingBox").Element(ns + "SouthLatitude").Value),
                                        Convert.ToDouble(location.Element(ns + "BoundingBox").Element(ns + "EastLongitude").Value)
                                        )
                               }).ToList();

                    }
                }
                catch (Exception ex)
                {
                    error = new WebserviceParsingException(requestUrl, e.Result, ex);
                }

                Debug.Assert(error == null);

                callback(locations, error);
            }
        }

        #endregion

        public void ClearCache()
        {
            webservice.ClearCache();
        }

        public void SaveCache()
        {
            webservice.SaveCache();
        }
    }
}
