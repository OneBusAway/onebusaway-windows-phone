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
            if (e.error == null)
            {
                e.stops.Sort(new StopDistanceComparer(e.searchLocation));
                StopsForLocation.Clear();
                e.stops.ForEach(stop => StopsForLocation.Add(stop));
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

        #endregion

    }
}
