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
