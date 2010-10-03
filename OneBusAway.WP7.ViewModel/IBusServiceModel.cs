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

        event EventHandler<RoutesForLocationEventArgs> RoutesForLocation_Completed;
        void RoutesForLocation(GeoCoordinate location, int radiusInMeters);
        void RoutesForLocation(GeoCoordinate location, int radiusInMeters, int maxCount);

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

        event EventHandler<LocationForAddressEventArgs> LocationForAddress_Completed;
        void LocationForAddress(string addressString);
    }
}
