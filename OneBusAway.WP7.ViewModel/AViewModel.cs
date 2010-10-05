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
using System.ComponentModel;
using System.Device.Location;
using Microsoft.Devices;
using System.Collections.Generic;
using System.Threading;

namespace OneBusAway.WP7.ViewModel
{
    public abstract class AViewModel : INotifyPropertyChanged
    {

        #region Static Location Code

        private static GeoCoordinate lastKnownLocation;
        protected static GeoCoordinateWatcher LocationWatcher { get; private set; }
        
        static AViewModel()
        {
            lastKnownLocation = null;

            LocationWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            LocationWatcher.MovementThreshold = 5; // 5 meters
            LocationWatcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(LocationWatcher_PositionChanged);
            LocationWatcher.Start();
        }

        static void LocationWatcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            if (e.Position.Location.IsUnknown == false)
            {
                lastKnownLocation = e.Position.Location;
            }
        }

        public static GeoCoordinate CurrentLocation
        {
            get
            {
#if DEBUG
                if (Microsoft.Devices.Environment.DeviceType == DeviceType.Emulator)
                {
                    return new GeoCoordinate(47.676, -122.32);
                }
#endif

                if (lastKnownLocation != null)
                {
                    return lastKnownLocation;
                }

                throw new LocationUnavailableException("The location is currently unavailable: " + LocationWatcher.Status, LocationWatcher.Status);
            }
        }

        public static bool LocationKnown
        {
            get
            {
#if DEBUG
                if (Microsoft.Devices.Environment.DeviceType == DeviceType.Emulator)
                {
                    return true;
                }
#endif

                return lastKnownLocation != null;
            }
        }

        /// <summary>
        /// Returns a default location to use when our current location is
        /// unavailable.  This is downtown Seattle.
        /// </summary>
        public static GeoCoordinate DefaultLocation
        {
            get
            {
                return new GeoCoordinate(47.60621, -122.332071);
            }
        }

        #endregion

        public AViewModel()
        {
            locationLoading = false;
            Loading = false;
            pendingOperationsCount = 0;
            pendingOperationsLock = new Object();

            methodsRequiringLocation = new List<RequiresKnownLocation>();
            methodsRequiringLocationLock = new Object();
            // Create the timer but don't run it until methods are added to the queue
            methodsRequiringLocationTimer = new Timer(new TimerCallback(RunMethodsRequiringLocation), null, Timeout.Infinite, Timeout.Infinite);

            if (LocationKnown == false)
            {
                pendingOperations++;
                locationLoading = true;
                LocationWatcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(LocationWatcher_LocationKnown);
            }

        }

        #region Private/Protected Properties

        private bool locationLoading;
        private bool loading;
        private int pendingOperationsCount;
        private Timer methodsRequiringLocationTimer;
        private const int timerIntervalMs = 500;
        private Object methodsRequiringLocationLock;
        private List<RequiresKnownLocation> methodsRequiringLocation;

        private Object pendingOperationsLock;
        protected int pendingOperations
        {
            get
            {
                return pendingOperationsCount;
            }

            set
            {
                // Make sure we never set pendingOperations to a negative number
                if (pendingOperationsCount >= 0)
                {
                    pendingOperationsCount = value;
                }

                if (pendingOperationsCount == 0)
                {
                    Loading = false;
                }
                else
                {
                    Loading = true;
                }
            }
        }

        #endregion

        #region Private/Protected Methods

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        void LocationWatcher_LocationKnown(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            if (e.Position.Location.IsUnknown == false)
            {
                lock (pendingOperationsLock)
                {
                    if (locationLoading == true)
                    {
                        // We know where we are now, decrease the pending count
                        locationLoading = false;
                        pendingOperations--;
                    }
                }
            }
        }

        private void RunMethodsRequiringLocation(object param)
        {
            if (LocationKnown == true)
            {
                lock (methodsRequiringLocationLock)
                {
                    methodsRequiringLocation.ForEach(method => method(CurrentLocation));
                    methodsRequiringLocation.Clear();
                    // Disable the timer now that no methods are in the queue
                    methodsRequiringLocationTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }

        protected delegate void RequiresKnownLocation(GeoCoordinate location);
        protected void RunWhenLocationKnown(RequiresKnownLocation method)
        {
            if (LocationKnown == true)
            {
                method(CurrentLocation);
            }
            else
            {
                lock (methodsRequiringLocationLock)
                {
                    methodsRequiringLocation.Add(method);
                    methodsRequiringLocationTimer.Change(timerIntervalMs, timerIntervalMs);
                }
            }
        }

        #endregion

        #region Public Members

        public ViewState CurrentViewState
        {
            get
            {
                return ViewState.Instance;
            }
        }

        public bool Loading
        {
            get
            {
                return loading;
            }

            protected set
            {
                if (loading != value)
                {
                    loading = value;
                    OnPropertyChanged("Loading");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        /// <summary>
        /// Registers all event handlers with the model.  Call this when 
        /// the page is first loaded.
        /// </summary>
        public abstract void RegisterEventHandlers();

        /// <summary>
        /// Unregisters all event handlers with the model. Call this when
        /// the page is navigated away from.
        /// </summary>
        public abstract void UnregisterEventHandlers();

        #endregion

    }

    public class LocationUnavailableException : Exception
    {
        public GeoPositionStatus Status { get; private set; }

        public LocationUnavailableException(string message, GeoPositionStatus status)
            : base(message)
        {
            Status = status;
        }

        public override string ToString()
        {
            return string.Format(
                "{0} \r\n" +
                "LocationStatus: {1}",
                base.ToString(),
                Status
                );
        }
    }
}
