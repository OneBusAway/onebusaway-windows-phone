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
