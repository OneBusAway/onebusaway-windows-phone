/* Copyright 2013 Shawn Henry, Rob Smith, and Michael Friedman
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
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
