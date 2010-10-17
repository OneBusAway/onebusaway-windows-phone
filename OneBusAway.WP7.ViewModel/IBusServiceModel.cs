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
        event EventHandler<StopsForLocationEventArgs> StopsForLocation_Completed;
        void StopsForLocation(GeoCoordinate location, int radiusInMeters);
        void StopsForLocation(GeoCoordinate location, int radiusInMeters, int maxCount);
        void StopsForLocation(GeoCoordinate location, int radiusInMeters, int maxCount, bool invalidateCache);

        event EventHandler<RoutesForLocationEventArgs> RoutesForLocation_Completed;
        void RoutesForLocation(GeoCoordinate location, int radiusInMeters);
        void RoutesForLocation(GeoCoordinate location, int radiusInMeters, int maxCount);
        void RoutesForLocation(GeoCoordinate location, int radiusInMeters, int maxCount, bool invalidateCache);

        event EventHandler<StopsForRouteEventArgs> StopsForRoute_Completed;
        void StopsForRoute(Route route);

        event EventHandler<ArrivalsForStopEventArgs> ArrivalsForStop_Completed;
        void ArrivalsForStop(Stop stop);

        event EventHandler<ScheduleForStopEventArgs> ScheduleForStop_Completed;
        void ScheduleForStop(Stop stop);

        event EventHandler<TripDetailsForArrivalEventArgs> TripDetailsForArrival_Completed;
        void TripDetailsForArrivals(List<ArrivalAndDeparture> arrivals);

        event EventHandler<SearchForRoutesEventArgs> SearchForRoutes_Completed;
        void SearchForRoutes(GeoCoordinate location, string query);
        void SearchForRoutes(GeoCoordinate location, string query, int radiusInMeters, int maxCount);

        event EventHandler<SearchForStopsEventArgs> SearchForStops_Completed;
        void SearchForStops(GeoCoordinate location, string query);

        event EventHandler<LocationForAddressEventArgs> LocationForAddress_Completed;
        void LocationForAddress(string addressString);

        void ClearCache();
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

    public class WebserviceResponseException : Exception
    {
        private string requestUrl;
        private string serverResponse;
        private HttpStatusCode serverStatusCode;

        public WebserviceResponseException(HttpStatusCode serverStatusCode, string requestUrl, string serverResponse, Exception innerException)
            : base("We were able to contact the webservice but the service returned an error", innerException)
        {
            this.requestUrl = requestUrl;
            this.serverResponse = serverResponse;
            this.serverStatusCode = serverStatusCode;
        }

        public override string ToString()
        {
            return string.Format(
                "{0}\r\nHttpErrorCode: '{1}'\r\nRequestURL: '{2}'\r\nResponse:\r\n{3}",
                base.ToString(),
                serverStatusCode,
                requestUrl,
                serverResponse
                );
        }
    }
}
