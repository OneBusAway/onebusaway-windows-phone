using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Device.Location;
using System.Runtime.Serialization;
using System.ComponentModel;

namespace OneBusAway.WP7.ViewModel.BusServiceDataStructures
{
    [DataContract()]
    public class TripDetails : INotifyPropertyChanged
    {
        [DataMember()]
        public string tripId { get; set; }
        [DataMember()]
        public DateTime serviceDate { get; set; }
        [DataMember()]
        public int? scheduleDeviationInSec { get; set; }
        [DataMember()]
        public string closestStopId { get; set; }
        [DataMember()]
        public int? closestStopTimeOffset { get; set; }

        [DataMember]
        private Coordinate coordinatePrivate;

        public Coordinate coordinate 
        {
            get
            {
                return coordinatePrivate;
            }

            set
            {
                coordinatePrivate = value;

                OnPropertyChanged("coordinate");
                OnPropertyChanged("locationKnown");
                OnPropertyChanged("location");
            }
        }

        public bool locationKnown
        {
            get
            {
                return coordinate != null;
            }
        }

        public GeoCoordinate location
        {
            get
            {
                if (coordinate == null) return null;

                return new GeoCoordinate
                {
                    Latitude = coordinate.Latitude,
                    Longitude = coordinate.Longitude
                };
            }

            set
            {
                if (value != null)
                {
                    coordinate = new Coordinate
                    {
                        Latitude = value.Latitude,
                        Longitude = value.Longitude
                    };
                }
                else
                {
                    coordinate = null;
                }
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
    }
}
