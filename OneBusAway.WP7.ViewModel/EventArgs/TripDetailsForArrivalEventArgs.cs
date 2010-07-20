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
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using System.Collections.Generic;

namespace OneBusAway.WP7.ViewModel.EventArgs
{
    public class TripDetailsForArrivalEventArgs : AModelEventArgs
    {
        public List<TripDetails> tripDetails { get; private set; }
        public ArrivalAndDeparture arrival { get; private set; }

        public TripDetailsForArrivalEventArgs(List<ArrivalAndDeparture> arrivals, List<TripDetails> tripDetails, Exception error)
        {
            this.tripDetails = tripDetails;
            this.error = error;
            this.arrival = arrival;
        }
    }
}
