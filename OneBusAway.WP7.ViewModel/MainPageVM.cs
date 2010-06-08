using System;
using System.Net;
using System.Collections.ObjectModel;
using OneBusAway.WP7.ViewModel.DataStructures;
using System.Device.Location;
using System.Reflection;

namespace OneBusAway.WP7.ViewModel
{
    public class MainPageVM
    {

        #region Private Variables

        private IBusServiceModel busServiceModel;

        #endregion

        #region Constructors

        public MainPageVM()
            : this((IBusServiceModel)Assembly.Load("OneBusAway.WP7.Model").CreateInstance("OneBusAway.WP7.Model.BusServiceModel"))
        {
            
        }

        public MainPageVM(IBusServiceModel busServiceModel)
        {
            this.busServiceModel = busServiceModel;

            StopsForLocation = new ObservableCollection<Stop>();
            StopsForRoute = new ObservableCollection<RouteStops>();
            ScheduleForStop = new ObservableCollection<RouteSchedule>();
            RoutesForLocation = new ObservableCollection<Route>();
            ArrivalsForStop = new ObservableCollection<ArrivalAndDeparture>();

            this.busServiceModel.ArrivalsForStop_Completed += new EventHandler<EventArgs.ArrivalsForStopEventArgs>(busServiceModel_ArrivalsForStop_Completed);
            this.busServiceModel.RoutesForLocation_Completed += new EventHandler<EventArgs.RoutesForLocationEventArgs>(busServiceModel_RoutesForLocation_Completed);
            this.busServiceModel.ScheduleForStop_Completed += new EventHandler<EventArgs.ScheduleForStopEventArgs>(busServiceModel_ScheduleForStop_Completed);
            this.busServiceModel.StopsForLocation_Completed += new EventHandler<EventArgs.StopsForLocationEventArgs>(busServiceModel_StopsForLocation_Completed);
            this.busServiceModel.StopsForRoute_Completed += new EventHandler<EventArgs.StopsForRouteEventArgs>(busServiceModel_StopsForRoute_Completed);
        }

        #endregion

        #region Public Properties

        public ObservableCollection<Stop> StopsForLocation { get; private set; }
        public ObservableCollection<RouteStops> StopsForRoute { get; private set; }
        public ObservableCollection<RouteSchedule> ScheduleForStop { get; private set; }
        public ObservableCollection<Route> RoutesForLocation { get; private set; }
        public ObservableCollection<ArrivalAndDeparture> ArrivalsForStop { get; private set; }

        #endregion

        #region Public Methods

        public void LoadInfoForLocation(GeoCoordinate location, int radiusInMeters)
        {
            busServiceModel.StopsForLocation(location, radiusInMeters);
            busServiceModel.RoutesForLocation(location, radiusInMeters);
        }

        #endregion

        #region Event Handlers

        void busServiceModel_StopsForRoute_Completed(object sender, EventArgs.StopsForRouteEventArgs e)
        {
            if (e.error == null)
            {
                StopsForRoute.Clear();
                e.routeStops.ForEach(routeStop => StopsForRoute.Add(routeStop));
            }
        }

        void busServiceModel_StopsForLocation_Completed(object sender, EventArgs.StopsForLocationEventArgs e)
        {
            if (e.error == null)
            {
                e.stops.Sort(new StopDistanceComparer(e.searchLocation));
                StopsForLocation.Clear();
                e.stops.ForEach(stop => StopsForLocation.Add(stop));
            }
        }

        void busServiceModel_ScheduleForStop_Completed(object sender, EventArgs.ScheduleForStopEventArgs e)
        {
            if (e.error == null)
            {
                ScheduleForStop.Clear();
                e.schedules.ForEach(schedule => ScheduleForStop.Add(schedule));
            }
        }

        void busServiceModel_RoutesForLocation_Completed(object sender, EventArgs.RoutesForLocationEventArgs e)
        {
            if (e.error == null)
            {
                RoutesForLocation.Clear();
                e.routes.ForEach(route => RoutesForLocation.Add(route));
            }
        }

        void busServiceModel_ArrivalsForStop_Completed(object sender, EventArgs.ArrivalsForStopEventArgs e)
        {
            if (e.error == null)
            {
                ArrivalsForStop.Clear();
                e.arrivals.ForEach(arrival => ArrivalsForStop.Add(arrival));
            }
        }

        #endregion

    }
}
