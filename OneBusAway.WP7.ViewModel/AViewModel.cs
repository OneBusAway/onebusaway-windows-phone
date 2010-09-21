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

namespace OneBusAway.WP7.ViewModel
{
    public abstract class AViewModel : INotifyPropertyChanged
    {

        public AViewModel()
        {
            Loading = false;
        }

        #region Private/Protected Members

        private bool loading;

        private int pendingOperationsCount;
        protected int pendingOperations
        {
            get
            {
                return pendingOperationsCount;
            }

            set
            {
                pendingOperationsCount = value;

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

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Public Members

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
}
