using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Device.Location;
using System.Threading;
using Microsoft.Devices;
using OneBusAway.WP7.ViewModel.EventArgs;
using System.Diagnostics;


namespace OneBusAway.WP7.ViewModel 
{
    public class LocationTracker : INotifyPropertyChanged
    {
        #region Static Location Code

        private static GeoCoordinateWatcher locationWatcher;
        private static GeoCoordinate lastKnownLocation;

        static LocationTracker()
        {
            lastKnownLocation = null;

            locationWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            locationWatcher.MovementThreshold = 5; // 5 meters
            locationWatcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(LocationWatcher_PositionChanged);

            locationWatcher.Start();
        }

        private static void LocationWatcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            // The location service will return the last known location of the phone when it first starts up.  Since
            // we can't refresh the home screen wait until a recent location value is found before using it.  The
            // location must be less than 1 minute old.
            if (e.Position.Location.IsUnknown == false)
            {
                if ((DateTime.Now - e.Position.Timestamp.DateTime) < new TimeSpan(0, 1, 0))
                {
                    lastKnownLocation = e.Position.Location;
                }
            }
        }

        #endregion

        #region Private Variables

        private bool locationLoading;
        private Timer methodsRequiringLocationTimer;
        private const int timerIntervalMs = 500;
        private Object methodsRequiringLocationLock;
        private List<RequiresKnownLocation> methodsRequiringLocation;
        private AsyncOperationTracker operationTracker;

        #endregion

        #region Constructors

        public LocationTracker(AsyncOperationTracker operationTracker)
        {
            this.operationTracker = operationTracker;
            locationLoading = false;

            methodsRequiringLocation = new List<RequiresKnownLocation>();
            methodsRequiringLocationLock = new Object();
            // Create the timer but don't run it until methods are added to the queue
            methodsRequiringLocationTimer = new Timer(new TimerCallback(RunMethodsRequiringLocation), null, Timeout.Infinite, Timeout.Infinite);

            if (LocationKnown == false)
            {
                operationTracker.WaitForOperation("LoadLocation");
                locationLoading = true;
                locationWatcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(LocationWatcher_LocationKnown);
                locationWatcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(LocationWatcher_StatusChanged);
            }

            locationWatcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(locationWatcher_PositionChanged_NotifyPropertyChanged);
        }

        #endregion

        #region Public Properties

        public event EventHandler<ErrorHandlerEventArgs> ErrorHandler;

        public GeoCoordinate CurrentLocation
        {
            get
            {
                if (Microsoft.Devices.Environment.DeviceType == DeviceType.Emulator)
                {
                    return new GeoCoordinate(47.676, -122.32); // Greenlake P&R
                    //return new GeoCoordinate(30.266, -97.742); // Austin, TX
                }

                if (lastKnownLocation != null)
                {
                    return lastKnownLocation;
                }

                throw new LocationUnavailableException("The location is currently unavailable: " + locationWatcher.Status, locationWatcher.Status);
            }
        }

        public bool LocationKnown
        {
            get
            {
                if (Microsoft.Devices.Environment.DeviceType == DeviceType.Emulator)
                {
                    return true;
                }

                return lastKnownLocation != null;
            }
        }

        /// <summary>
        /// Returns a default location to use when our current location is
        /// unavailable.  This is downtown Seattle.
        /// </summary>
        public GeoCoordinate DefaultLocationStatic
        {
            get
            {
                return new GeoCoordinate(47.60621, -122.332071);
            }
        }

        public GeoCoordinate CurrentLocationSafe
        {
            get
            {
                if (LocationKnown == true)
                {
                    return CurrentLocation;
                }
                else
                {
                    return DefaultLocationStatic;
                }
            }
        }

        #endregion

        #region Private Methods

        private void LocationWatcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            if (e.Status == GeoPositionStatus.Disabled)
            {
                // Status disabled means the user has disabled the location service on their phone
                // and we won't be getting a location.  Go ahead and stop loading the location and
                // set it to the default
                if (locationLoading == true)
                {
                    locationLoading = false;
                    operationTracker.DoneWithOperation("LoadLocation");

                    lastKnownLocation = DefaultLocationStatic;
                    OnPropertyChanged("CurrentLocation");
                    OnPropertyChanged("CurrentLocationSafe");
                    OnPropertyChanged("LocationKnown");
                }

                // Let them know OneBusAway is pretty useless without location
                ErrorOccured(this, new LocationUnavailableException("The location is currently unavailable: " + locationWatcher.Status, locationWatcher.Status));
            }
        }

        private void LocationWatcher_LocationKnown(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            if (e.Position.Location.IsUnknown == false)
            {
                if (locationLoading == true)
                {
                    // We know where we are now, decrease the pending count
                    locationLoading = false;
                    operationTracker.DoneWithOperation("LoadLocation");

                    // Remove this handler now that the location is known
                    locationWatcher.PositionChanged -= new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(LocationWatcher_LocationKnown);
                }
            }
        }

        private void locationWatcher_PositionChanged_NotifyPropertyChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            if (e.Position.Location.IsUnknown == false)
            {
                OnPropertyChanged("CurrentLocation");
                OnPropertyChanged("CurrentLocationSafe");
                OnPropertyChanged("LocationKnown");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected void ErrorOccured(object sender, Exception e)
        {
            Debug.Assert(false);

            // The VM should always be subscribed to the ErrorHandler event
            Debug.Assert(ErrorHandler != null);

            if (ErrorHandler != null)
            {
                ErrorHandler(sender, new ErrorHandlerEventArgs(e));
            }
        }

        #endregion

        #region Run Requiring Location methods

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

        public delegate void RequiresKnownLocation(GeoCoordinate location);
        public void RunWhenLocationKnown(RequiresKnownLocation method)
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
