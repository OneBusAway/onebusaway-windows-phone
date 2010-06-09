using System;
using System.Net;
using System.Device.Location;
using OneBusAway.WP7.ViewModel.DataStructures;
using OneBusAway.WP7.ViewModel.EventArgs;

namespace OneBusAway.WP7.ViewModel
{
    public interface IBusServiceModel
    {
        event EventHandler<StopsForLocationEventArgs> StopsForLocation_Completed;
        void StopsForLocation(GeoCoordinate location, int radiusInMeters);

        event EventHandler<RoutesForLocationEventArgs> RoutesForLocation_Completed;
        void RoutesForLocation(GeoCoordinate location, int radiusInMeters);

        event EventHandler<StopsForRouteEventArgs> StopsForRoute_Completed;
        void StopsForRoute(Route route);

        event EventHandler<ArrivalsForStopEventArgs> ArrivalsForStop_Completed;
        void ArrivalsForStop(Stop stop);

        event EventHandler<ScheduleForStopEventArgs> ScheduleForStop_Completed;
        void ScheduleForStop(Stop stop);
    }
}
