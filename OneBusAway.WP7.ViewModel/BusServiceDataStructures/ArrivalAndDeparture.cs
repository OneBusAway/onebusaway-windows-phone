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
                OnPropertyChanged("nextKnownArrival");
                OnPropertyChanged("busDelay");
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
            }
        }
        private DateTime? privatePredictedDepartureTime;

        public TimeSpan? busDelay
        {
            get
            {
                if (predictedArrivalTime != null)
                {
                    return (DateTime)predictedArrivalTime - scheduledArrivalTime;
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

        public DateTime nextKnownArrival
        {
            get
            {
                return predictedArrivalTime != null ? (DateTime)predictedArrivalTime : scheduledArrivalTime;
            }
        }

        public override string ToString()
        {
            return string.Format(
                "Arrival: Route='{0}', Destination='{1}', NextArrival='{2}'",
                routeShortName,
                tripHeadsign,
                nextKnownArrival.ToString("HH:mm")
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

    public class ArrivalTimeComparer : IComparer<ArrivalAndDeparture>
    {
        public int Compare(ArrivalAndDeparture x, ArrivalAndDeparture y)
        {
            return x.nextKnownArrival.CompareTo(y.nextKnownArrival);
        }
    }
}
