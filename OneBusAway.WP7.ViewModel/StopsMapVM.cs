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
using System.Collections.ObjectModel;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using System.Device.Location;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Threading;

namespace OneBusAway.WP7.ViewModel
{
    public class StopsMapVM : AViewModel
    {
        private Object stopsForLocationLock;

        #region Constructors

        public StopsMapVM()
            : base()
        {
            Initialize();
        }

        public StopsMapVM(IBusServiceModel busServiceModel, IAppDataModel appDataModel)
            : base(busServiceModel, appDataModel)
        {
            Initialize();
        }

        private void Initialize()
        {
            stopsForLocationLock = new Object();
            StopsForLocation = new ObservableCollection<Stop>();
        }

        #endregion

        #region Public Methods/Properties

        public ObservableCollection<Stop> StopsForLocation { get; private set; }

        public void LoadStopsForLocation(GeoCoordinate topLeft, GeoCoordinate bottomRight)
        {
            GeoCoordinate center = new GeoCoordinate()
            {
                Latitude = (topLeft.Latitude + bottomRight.Latitude) / 2,
                Longitude = (topLeft.Longitude + bottomRight.Longitude) / 2
            };

            int radiusInMeters = ((int)topLeft.GetDistanceTo(bottomRight)) / 2;
            // Query for at least a 250m radius and less than a 1km radius
            radiusInMeters = Math.Max(radiusInMeters, 250);
            radiusInMeters = Math.Min(radiusInMeters, 3000);

            this.LoadingText = "Loading stops";
            operationTracker.WaitForOperation("StopsForLocation");
            busServiceModel.StopsForLocation(center, radiusInMeters);
        }

        public override void RegisterEventHandlers(Dispatcher dispatcher)
        {
            base.RegisterEventHandlers(dispatcher);

            this.busServiceModel.StopsForLocation_Completed += new EventHandler<EventArgs.StopsForLocationEventArgs>(busServiceModel_StopsForLocation_Completed);
        }

        public override void UnregisterEventHandlers()
        {
            base.UnregisterEventHandlers();

            this.busServiceModel.StopsForLocation_Completed -= new EventHandler<EventArgs.StopsForLocationEventArgs>(busServiceModel_StopsForLocation_Completed);

            // Reset loading to 0 since event handlers have been unregistered
            this.operationTracker.ClearOperations();
        }

        #endregion

        #region Event Handlers

        void busServiceModel_StopsForLocation_Completed(object sender, EventArgs.StopsForLocationEventArgs e)
        {
            Debug.Assert(e.error == null);

            if (e.error == null)
            {
                lock (stopsForLocationLock)
                {
                    // TODO: This algorithm is pretty slow, around ~1/4 second under debugger
                    // We should try to find a more efficient way to do this
                    List<Stop> stopsToRemove = new List<Stop>();
                    foreach (Stop stop in StopsForLocation)
                    {
                        if (e.stops.Contains(stop) == false)
                        {
                            stopsToRemove.Add(stop);
                        }
                    }

                    stopsToRemove.ForEach(stop => UIAction(() => StopsForLocation.Remove(stop)));

                    foreach (Stop stop in e.stops)
                    {
                        if (StopsForLocation.Contains(stop) == false)
                        {
                            Stop currentStop = stop;
                            UIAction(() => StopsForLocation.Add(currentStop));
                        }
                    }
                }
            }
            else
            {
                ErrorOccured(this, e.error);
            }

            operationTracker.DoneWithOperation("StopsForLocation");
        }

        #endregion

    }
}
