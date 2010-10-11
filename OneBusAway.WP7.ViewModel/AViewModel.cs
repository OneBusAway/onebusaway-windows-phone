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
using System.Reflection;
using System.Diagnostics;

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
            LocationWatcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(LocationWatcher_PositionChangedStatic);
            LocationWatcher.Start();
        }

        static void LocationWatcher_PositionChangedStatic(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
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

        public static GeoCoordinate CurrentLocationStatic
        {
            get
            {
                if (Microsoft.Devices.Environment.DeviceType == DeviceType.Emulator)
                {
                    return new GeoCoordinate(47.676, -122.32);
                }

                if (lastKnownLocation != null)
                {
                    return lastKnownLocation;
                }

                throw new LocationUnavailableException("The location is currently unavailable: " + LocationWatcher.Status, LocationWatcher.Status);
            }
        }

        public static bool LocationKnownStatic
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
        public static GeoCoordinate DefaultLocationStatic
        {
            get
            {
                return new GeoCoordinate(47.60621, -122.332071);
            }
        }

        #endregion

        #region Constructors

        public AViewModel()
            :   this((IBusServiceModel)Assembly.Load("OneBusAway.WP7.Model")
                    .GetType("OneBusAway.WP7.Model.BusServiceModel")
                    .GetField("Singleton")
                    .GetValue(null),
                (IAppDataModel)Assembly.Load("OneBusAway.WP7.Model")
                    .GetType("OneBusAway.WP7.Model.AppDataModel")
                    .GetField("Singleton")
                    .GetValue(null))
        {

        }

        public AViewModel(IBusServiceModel busSerivceModel)
            : this(busSerivceModel,
                (IAppDataModel)Assembly.Load("OneBusAway.WP7.Model")
                    .GetType("OneBusAway.WP7.Model.AppDataModel")
                    .GetField("Singleton")
                    .GetValue(null))
        {

        }

        public AViewModel(IBusServiceModel busServiceModel, IAppDataModel appDataModel)
        {
            this.busServiceModel = busServiceModel;
            this.appDataModel = appDataModel;

            locationLoading = false;
            Loading = false;
            pendingOperationsCount = 0;
            pendingOperationsLock = new Object();

            methodsRequiringLocation = new List<RequiresKnownLocation>();
            methodsRequiringLocationLock = new Object();
            // Create the timer but don't run it until methods are added to the queue
            methodsRequiringLocationTimer = new Timer(new TimerCallback(RunMethodsRequiringLocation), null, Timeout.Infinite, Timeout.Infinite);

            if (LocationKnownStatic == false)
            {
                pendingOperations++;
                locationLoading = true;
                LocationWatcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(LocationWatcher_LocationKnown);
                LocationWatcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(LocationWatcher_StatusChanged);
            }
        }

        #endregion

        #region Private/Protected Properties

        protected IBusServiceModel busServiceModel { get; private set; }
        protected IAppDataModel appDataModel { get; private set; }

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
                if (value >= 0)
                {
                    pendingOperationsCount = value;
                }
                else
                {
                    Debug.Assert(value >= 0);
                    pendingOperations = 0;
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

        void LocationWatcher_StatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            if (e.Status == GeoPositionStatus.Disabled)
            {
                // Status disabled means the user has disabled the location service on their phone
                // and we won't be getting a location.  Go ahead and stop loading the location and
                // set it to the default
                lock (pendingOperationsLock)
                {
                    if (locationLoading == true)
                    {
                        lastKnownLocation = DefaultLocationStatic;

                        locationLoading = false;
                        pendingOperations--;
                    }
                }
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

                        // Remove this handler now that the location is known
                        LocationWatcher.PositionChanged -= new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(LocationWatcher_LocationKnown);
                    }
                }
            }
        }

        void LocationWatcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            OnPropertyChanged("CurrentLocation");
            OnPropertyChanged("CurrentLocationSafe");
        }

        private void RunMethodsRequiringLocation(object param)
        {
            if (LocationKnownStatic == true)
            {
                lock (methodsRequiringLocationLock)
                {
                    methodsRequiringLocation.ForEach(method => method(CurrentLocationStatic));
                    methodsRequiringLocation.Clear();
                    // Disable the timer now that no methods are in the queue
                    methodsRequiringLocationTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
            }
        }

        protected delegate void RequiresKnownLocation(GeoCoordinate location);
        protected void RunWhenLocationKnown(RequiresKnownLocation method)
        {
            if (LocationKnownStatic == true)
            {
                method(CurrentLocationStatic);
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

        public GeoCoordinate CurrentLocation
        {
            get
            {
                return AViewModel.CurrentLocationStatic;
            }
        }

        public GeoCoordinate CurrentLocationSafe
        {
            get
            {
                if (AViewModel.LocationKnownStatic == true)
                {
                    return AViewModel.CurrentLocationStatic;
                }
                else
                {
                    return AViewModel.DefaultLocationStatic;
                }
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
        public virtual void RegisterEventHandlers()
        {
            LocationWatcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(LocationWatcher_PositionChanged);
        }

        /// <summary>
        /// Unregisters all event handlers with the model. Call this when
        /// the page is navigated away from.
        /// </summary>
        public virtual void UnregisterEventHandlers()
        {
            LocationWatcher.PositionChanged -= new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(LocationWatcher_PositionChanged);
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
