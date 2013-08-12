using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace OneBusAway.WP7.ViewModel.BusServiceDataStructures
{
    /// <summary>
    /// Base class for bindable objects.
    /// </summary>
    public class BindableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Sets a property.
        /// </summary>
        protected bool SetProperty<T>(ref T prop, T newValue, string propName)
        {
            if (!object.Equals(prop, newValue))
            {
                prop = newValue;
                this.FirePropertyChanged(propName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Fires a property changed event.
        /// </summary>
        protected virtual void FirePropertyChanged(string propName)
        {
            var propChanged = this.PropertyChanged;
            if (propChanged != null)
            {
                propChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
    }
}
