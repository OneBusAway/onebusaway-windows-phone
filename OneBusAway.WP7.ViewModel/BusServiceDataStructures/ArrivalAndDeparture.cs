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
using System.Runtime.Serialization;
using System.ComponentModel;

namespace OneBusAway.WP7.ViewModel.BusServiceDataStructures
{
    [DataContract()]
    public class ArrivalAndDeparture : INotifyPropertyChanged
    {
        [DataMember()]
        public string routeId { get; set; }
        [DataMember()]
        public string routeShortName { get; set; }
        [DataMember()]
        public string tripId { get; set; }
        [DataMember()]
        public string tripHeadsign { get; set; }
        [DataMember()]
        public string stopId { get; set; }

        [DataMember()]
        public DateTime? predictedArrivalTime 
        {
            get { return privatePredictedArrivalTime; }
            set 
            { 
                privatePredictedArrivalTime = value;
                OnPropertyChanged("predictedArrivalTime");
            }
        }
        private DateTime? privatePredictedArrivalTime;
        
        [DataMember()]
        public DateTime scheduledArrivalTime { get; set; }
        
        [DataMember()]
        public DateTime? predictedDepartureTime 
        {
            get { return privatePredictedDepartureTime; }
            set
            {
                privatePredictedDepartureTime = value;
                OnPropertyChanged("predictedDepartureTime");
                OnPropertyChanged("nextKnownDeparture");
                OnPropertyChanged("busDelay");
            }
        }
        private DateTime? privatePredictedDepartureTime;

        public TimeSpan? busDelay
        {
            get
            {
                if (predictedDepartureTime != null)
                {
                    return (DateTime)predictedDepartureTime - scheduledDepartureTime;
                }
                else
                {
                    return null;
                }
            }
        }

        [DataMember()]
        public DateTime scheduledDepartureTime { get; set; }
        [DataMember()]
        public string status { get; set; }
        [DataMember()]
        public TripDetails tripDetails { get; set; }

        public DateTime nextKnownDeparture
        {
            get
            {
                return predictedDepartureTime != null ? (DateTime)predictedDepartureTime : scheduledDepartureTime;
            }
        }

        public override string ToString()
        {
            return string.Format(
                "Arrival: Route='{0}', Destination='{1}', NextArrival='{2}'",
                routeShortName,
                tripHeadsign,
                nextKnownDeparture.ToString("HH:mm")
                );
        }

        public override bool Equals(object obj)
        {
            if (obj is ArrivalAndDeparture == false)
            {
                return false;
            }

            return ((ArrivalAndDeparture)obj).tripId == this.tripId 
                && ((ArrivalAndDeparture)obj).routeId == this.routeId;
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

    public class DepartureTimeComparer : IComparer<ArrivalAndDeparture>
    {
        public int Compare(ArrivalAndDeparture x, ArrivalAndDeparture y)
        {
            return x.nextKnownDeparture.CompareTo(y.nextKnownDeparture);
        }
    }
}
