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
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace OneBusAway.WP7.ViewModel.BusServiceDataStructures
{
    /// <summary>
    /// Represents a regions bounds.
    /// </summary>
    public class RegionBounds : BindableBase
    {
        private double regionLatitude;
        private double regionLatitudeSpan;
        private double regionLongitude;
        private double regionLongitudeSpan;

        /// <summary>
        /// Creates the region bounds.
        /// </summary>
        /// <param name="boundsElement">The bounds element</param>
        public RegionBounds(XElement boundsElement)
        {
            this.regionLatitude = boundsElement.GetFirstElementValue<double>("lat");
            this.regionLatitudeSpan = boundsElement.GetFirstElementValue<double>("latSpan");
            this.regionLongitude = boundsElement.GetFirstElementValue<double>("lon");
            this.regionLongitudeSpan = boundsElement.GetFirstElementValue<double>("lonSpan");
        }

        public double Latitude
        {
            get
            {
                return this.regionLatitude;
            }
        }

        public double Longitude
        {
            get
            {
                return this.regionLongitude;
            }
        }
    }
}
