using System;
using System.Net;
using System.Collections.ObjectModel;
using OneBusAway.WP7.ViewModel.DataStructures;
using System.Device.Location;
using System.Reflection;
using System.Diagnostics;

namespace OneBusAway.WP7.ViewModel
{
    public class MainPageVM
    {

        #region Private Variables

        private IBusServiceModel busServiceModel;

        #endregion

        #region Constructors

        public MainPageVM()
            : this((IBusServiceModel)Assembly.Load("OneBusAway.WP7.Model")
                .GetType("OneBusAway.WP7.Model.BusServiceModel")    
                .GetField("Singleton")
                .GetValue(null))
        {

        }

        public MainPageVM(IBusServiceModel busServiceModel)
        {
            this.busServiceModel = busServiceModel;

            StopsForLocation = new ObservableCollection<Stop>();
            RoutesForLocation = new ObservableCollection<Route>();

            this.busServiceModel.RoutesForLocation_Completed += new EventHandler<EventArgs.RoutesForLocationEventArgs>(busServiceModel_RoutesForLocation_Completed);
            this.busServiceModel.StopsForLocation_Completed += new EventHandler<EventArgs.StopsForLocationEventArgs>(busServiceModel_StopsForLocation_Completed);
            this.busServiceModel.ArrivalsForStop_Completed += new EventHandler<EventArgs.ArrivalsForStopEventArgs>(busServiceModel_ArrivalsForStop_Completed);
        }

        #endregion

        #region Public Properties

        public ObservableCollection<Stop> StopsForLocation { get; private set; }
        public ObservableCollection<Route> RoutesForLocation { get; private set; }

        #endregion

        #region Public Methods

        public void LoadInfoForLocation(GeoCoordinate location, int radiusInMeters)
        {
            busServiceModel.StopsForLocation(location, radiusInMeters);
            busServiceModel.RoutesForLocation(location, radiusInMeters);
        }

        #endregion

        #region Event Handlers

        void busServiceModel_StopsForLocation_Completed(object sender, EventArgs.StopsForLocationEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                e.stops.Sort(new StopDistanceComparer(e.location));
                StopsForLocation.Clear();
                e.stops.ForEach(stop => StopsForLocation.Add(stop));
            }
        }

        void busServiceModel_RoutesForLocation_Completed(object sender, EventArgs.RoutesForLocationEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                RoutesForLocation.Clear();
                e.routes.ForEach(route => RoutesForLocation.Add(route));
            }
        }

        void busServiceModel_ArrivalsForStop_Completed(object sender, EventArgs.ArrivalsForStopEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                // This should ensure the first arrival for a bus in the list is the first time-wise
                e.arrivals.Sort(new ArrivalTimeComparer());

                foreach (Route route in RoutesForLocation)
                {
                    // Find the route with the closest stop that these arrivals are for
                    if (route.closestStop.Equals(e.stop))
                    {
                        foreach (ArrivalAndDeparture arrival in e.arrivals)
                        {
                            // Now that we have the correct route/closest stop pair, find the arrival
                            // for the right bus
                            if (arrival.routeId == route.id)
                            {
                                // Since there could be multiple arrivals for the bus at this stop only assign
                                // the first one to nextArrival
                                if (route.nextArrival == null)
                                {
                                    route.nextArrival = arrival.nextKnownArrival;
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

    }
}
