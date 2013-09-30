/* Copyright 2013 Shawn Henry, Rob Smith, and Michael Friedman
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Net;
using System.Device.Location;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using OneBusAway.WP7.ViewModel.EventArgs;
using System.Collections.Generic;

namespace OneBusAway.WP7.ViewModel
{
    public interface IBusServiceModel
    {
        double DistanceFromClosestSupportedRegion(GeoCoordinate location);
        bool AreLocationsEquivalent(GeoCoordinate location1, GeoCoordinate location2);

        event EventHandler<CombinedInfoForLocationEventArgs> CombinedInfoForLocation_Completed;
        void CombinedInfoForLocation(GeoCoordinate location, int radiusInMeters);
        void CombinedInfoForLocation(GeoCoordinate location, int radiusInMeters, int maxCount);
        void CombinedInfoForLocation(GeoCoordinate location, int radiusInMeters, int maxCount, bool invalidateCache);

        event EventHandler<StopsForLocationEventArgs> StopsForLocation_Completed;
        void StopsForLocation(GeoCoordinate location, int radiusInMeters);
        void StopsForLocation(GeoCoordinate location, int radiusInMeters, int maxCount);
        void StopsForLocation(GeoCoordinate location, int radiusInMeters, int maxCount, bool invalidateCache);

        event EventHandler<RoutesForLocationEventArgs> RoutesForLocation_Completed;
        void RoutesForLocation(GeoCoordinate location, int radiusInMeters);
        void RoutesForLocation(GeoCoordinate location, int radiusInMeters, int maxCount);
        void RoutesForLocation(GeoCoordinate location, int radiusInMeters, int maxCount, bool invalidateCache);

        event EventHandler<StopsForRouteEventArgs> StopsForRoute_Completed;
        void StopsForRoute(GeoCoordinate location, Route route);

        event EventHandler<ArrivalsForStopEventArgs> ArrivalsForStop_Completed;
        void ArrivalsForStop(GeoCoordinate location, Stop stop);

        event EventHandler<ScheduleForStopEventArgs> ScheduleForStop_Completed;
        void ScheduleForStop(GeoCoordinate location, Stop stop);

        event EventHandler<TripDetailsForArrivalEventArgs> TripDetailsForArrival_Completed;
        void TripDetailsForArrivals(GeoCoordinate location, List<ArrivalAndDeparture> arrivals);

        event EventHandler<SearchForRoutesEventArgs> SearchForRoutes_Completed;
        void SearchForRoutes(GeoCoordinate location, string query);
        void SearchForRoutes(GeoCoordinate location, string query, int radiusInMeters, int maxCount);

        event EventHandler<SearchForStopsEventArgs> SearchForStops_Completed;
        void SearchForStops(GeoCoordinate location, string query);

        void Initialize();
        void ClearCache();
    }

    public class WebserviceParsingException : Exception
    {
        public string RequestUrl { get; private set; }
        public string ServerResponse { get; private set; }

        public WebserviceParsingException(string requestUrl, string serverResponse, Exception innerException)
            : base("There was an error parsing the server response", innerException)
        {
            this.RequestUrl = requestUrl;
            this.ServerResponse = serverResponse;
        }

        public override string ToString()
        {
            return string.Format(
                "{0}\r\nRequestURL: '{1}'\r\nResponse:\r\n{2}",
                base.ToString(),
                RequestUrl,
                ServerResponse
                );
        }
    }

    public class WebserviceResponseException : Exception
    {
        public string RequestUrl { get; private set; }
        public string ServerResponse { get; private set; }
        public HttpStatusCode ServerStatusCode { get; private set; }

        public WebserviceResponseException(HttpStatusCode serverStatusCode, string requestUrl, string serverResponse, Exception innerException)
            : base("We were able to contact the webservice but the service returned an error", innerException)
        {
            this.RequestUrl = requestUrl;
            this.ServerResponse = serverResponse;
            this.ServerStatusCode = serverStatusCode;
        }

        public override string ToString()
        {
            return string.Format(
                "{0}\r\nHttpErrorCode: '{1}'\r\nRequestURL: '{2}'\r\nResponse:\r\n{3}",
                base.ToString(),
                ServerStatusCode,
                RequestUrl,
                ServerResponse
                );
        }
    }
}
