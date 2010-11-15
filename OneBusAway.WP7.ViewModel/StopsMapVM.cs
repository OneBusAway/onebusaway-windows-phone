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
        private Object stopsForLocationCompletedLock;
        private Object stopsForLocationLock;
        private GeoCoordinate previousQuery;

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
            stopsForLocationCompletedLock = new Object();
            stopsForLocationLock = new Object();
            StopsForLocation = new ObservableCollection<Stop>();
            previousQuery = new GeoCoordinate();
        }

        #endregion

        #region Public Methods/Properties

        public ObservableCollection<Stop> StopsForLocation { get; private set; }

        public void LoadStopsForLocation(GeoCoordinate center)
        {
            // If the two queries are being rounded to the same coordinate, no 
            // reason to re-parse the data out of the cache
            if (busServiceModel.AreLocationsEquivalent(previousQuery, center) == true)
            {
                return;
            }

            operationTracker.WaitForOperation("StopsForLocation", "Loading stops...");

            previousQuery = center;
            busServiceModel.StopsForLocation(center, defaultSearchRadius);
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
                lock (stopsForLocationCompletedLock)
                {
                    // TODO: This algorithm is pretty slow, around ~1/4 second under debugger
                    // We should try to find a more efficient way to do this
                    List<Stop> stopsToRemove = new List<Stop>();
                    lock (stopsForLocationLock)
                    {
                        foreach (Stop stop in StopsForLocation)
                        {
                            if (e.stops.Contains(stop) == false)
                            {
                                stopsToRemove.Add(stop);
                            }
                        }
                    }

                    stopsToRemove.ForEach(stop => 
                        UIAction(() => 
                    {
                        lock (stopsForLocationLock) 
                        { 
                            StopsForLocation.Remove(stop);
                        }
                    }));

                    foreach (Stop stop in e.stops)
                    {
                        lock (stopsForLocationLock)
                        {
                            if (StopsForLocation.Contains(stop) == false)
                            {
                                Stop currentStop = stop;
                                UIAction(() =>
                                    {
                                        lock (stopsForLocationLock)
                                        {
                                            StopsForLocation.Add(currentStop);
                                        }
                                    });
                            }
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
