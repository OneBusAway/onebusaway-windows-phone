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

namespace OneBusAway.WP7.ViewModel
{
    public abstract class AViewModel : INotifyPropertyChanged
    {
        #region Constructors

        public AViewModel()
            :   this(null,null)
        {

        }

        public AViewModel(IBusServiceModel busServiceModel)
            : this(busServiceModel,
                (IAppDataModel)Assembly.Load("OneBusAway.WP7.Model")
                    .GetType("OneBusAway.WP7.Model.AppDataModel")
                    .GetField("Singleton")
                    .GetValue(null))
        {

        }

        public AViewModel(IBusServiceModel busServiceModel, IAppDataModel appDataModel)
        {
            this.lazyBusServiceModel = busServiceModel;
            this.lazyAppDataModel = appDataModel;

            operationTracker = new AsyncOperationTracker(
                () => { Loading = false; }, 
                () => { Loading = true; }
                );
            locationTracker = new LocationTracker(operationTracker);
            
            Loading = false;
            eventsRegistered = false;
        }

        #endregion

        #region Private/Protected Properties

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
  
        /// <summary>
        /// Subclasses should queue and dequeue their async calls onto this object to tie into the Loading property.
        /// </summary>
        protected AsyncOperationTracker operationTracker;

        protected LocationTracker locationTracker;

        private bool eventsRegistered;
        private bool loading;

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

        #endregion

        #region Public Members

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
            Debug.Assert(eventsRegistered == false);

            if (eventsRegistered == false)
            {
                locationTracker.RegisterEventHandlers();
                eventsRegistered = true;
            }
        }

        /// <summary>
        /// Unregisters all event handlers with the model. Call this when
        /// the page is navigated away from.
        /// </summary>
        public virtual void UnregisterEventHandlers()
        {
            Debug.Assert(eventsRegistered == true);

            if (eventsRegistered == true)
            {
                locationTracker.UnregisterEventHandlers();
                eventsRegistered = false;
            }
        }

        #endregion

    }
}
