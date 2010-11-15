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
using OneBusAway.WP7.ViewModel.EventArgs;
using System.Windows.Threading;

namespace OneBusAway.WP7.ViewModel
{
    public abstract class AViewModel : INotifyPropertyChanged
    {
        #region Constructors

        public AViewModel()
            :   this(null,null,null)
        {

        }

        public AViewModel(IBusServiceModel busServiceModel)
            : this(busServiceModel, null)
        {

        }

        public AViewModel(IBusServiceModel busServiceModel, IAppDataModel appDataModel)
            : this(busServiceModel, appDataModel, null)
        {

        }

        public AViewModel(IBusServiceModel busServiceModel, IAppDataModel appDataModel, ILocationModel locationModel)
        {
            this.lazyBusServiceModel = busServiceModel;
            this.lazyAppDataModel = appDataModel;
            Loading = false;
            locationTracker = new LocationTracker();

            // Set up the default action, just execute in the same thread
            UIAction = (uiAction => uiAction());
            
            operationTracker = new AsyncOperationTracker(
                () => { UIAction(() => Loading = false); },
                () => { UIAction(() => Loading = true); }
                );

            LoadingText = "Finding your location...";
            eventsRegistered = false;
        }

        #endregion

        #region Private/Protected Properties

        // Always use the same search radius so the cache will be much
        // more efficient.  500 m is small enough that we almost never
        // exceed the 100 stop limit, even downtown.
        protected int defaultSearchRadius = 500;

        private IBusServiceModel lazyBusServiceModel;
        protected IBusServiceModel busServiceModel
        {
            get
            {
                if (lazyBusServiceModel == null)
                {
                    lazyBusServiceModel = (IBusServiceModel)Assembly.Load("OneBusAway.WP7.Model")
                        .GetType("OneBusAway.WP7.Model.BusServiceModel")
                        .GetField("Singleton")
                        .GetValue(null);
                }
                return lazyBusServiceModel;
            }
        }
 
 	 	private IAppDataModel lazyAppDataModel;
 	 	protected IAppDataModel appDataModel
        {
            get
            {
                if (lazyAppDataModel == null)
                {
                    lazyAppDataModel = (IAppDataModel)Assembly.Load("OneBusAway.WP7.Model")
                        .GetType("OneBusAway.WP7.Model.AppDataModel")
                        .GetField("Singleton")
                        .GetValue(null);
                }
                return lazyAppDataModel;
            }
        }

        private ILocationModel lazyLocationModel;
        protected ILocationModel locationModel
        {
            get
            {
                if (lazyLocationModel == null)
                {
                    lazyLocationModel = (ILocationModel)Assembly.Load("OneBusAway.WP7.Model")
                        .GetType("OneBusAway.WP7.Model.LocationModel")
                        .GetField("Singleton")
                        .GetValue(null);
                }
                return lazyLocationModel;
            }
        }
  
        /// <summary>
        /// Subclasses should queue and dequeue their async calls onto this object to tie into the Loading property.
        /// </summary>
        protected AsyncOperationTracker operationTracker;

        protected LocationTracker locationTracker;

        private bool eventsRegistered;
        private bool loading;
        private string loadingText;

        #endregion

        #region Private/Protected Methods

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

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        void locationTracker_ErrorHandler(object sender, ErrorHandlerEventArgs e)
        {
            ErrorOccured(this, e.error);
        }

        #endregion

        #region Public Members

        private Action<Action> uiAction;
        public Action<Action> UIAction
        {
            get { return uiAction; }

            set
            {
                uiAction = value;

                // Set the ViewState's UIAction
                CurrentViewState.UIAction = uiAction;
                locationTracker.UIAction = uiAction;
            }
        }

        public event EventHandler<ErrorHandlerEventArgs> ErrorHandler;

        public const string FeedbackEmailAddress = "wp7@onebusaway.org";

        public ViewState CurrentViewState
        {
            get
            {
                return ViewState.Instance;
            }
        }

        public LocationTracker LocationTracker 
        { 
            get 
            { 
                return locationTracker; 
            } 
        }

        public string LoadingText { 
            get 
            { 
                return loadingText; 
            }
            protected set
            {
                if (loadingText != value)
                {
                    loadingText = value;
                    OnPropertyChanged("LoadingText");
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
        public virtual void RegisterEventHandlers(Dispatcher dispatcher)
        {
            // Set the UI Actions to occur on the UI thread
            UIAction = (uiAction => dispatcher.BeginInvoke(() => uiAction()));

            locationTracker.Initialize(operationTracker);
            locationTracker.ErrorHandler += new EventHandler<ErrorHandlerEventArgs>(locationTracker_ErrorHandler);

            Debug.Assert(eventsRegistered == false);
            eventsRegistered = true;
        }

        /// <summary>
        /// Unregisters all event handlers with the model. Call this when
        /// the page is navigated away from.
        /// </summary>
        public virtual void UnregisterEventHandlers()
        {
            locationTracker.ErrorHandler -= new EventHandler<ErrorHandlerEventArgs>(locationTracker_ErrorHandler);
            locationTracker.Uninitialize();

            Debug.Assert(eventsRegistered == true);
            eventsRegistered = false;
        }

        #endregion

    }
}
