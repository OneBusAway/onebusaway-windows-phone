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

namespace OneBusAway.WP7.ViewModel
{
    /// <summary>
    /// Exists to make sure that we reuse the TripService instance
    /// </summary>
    public class TripServiceFactory
    {
        public static readonly TripServiceFactory Singleton = new TripServiceFactory();
        private TripServiceFactory() { }

        private TripService _tripService = null;

        public TripService TripService {
            get
            {
                if (_tripService == null)
                {
                    _tripService = new TripService(NotificationFlagType.Toast);
                }
                return _tripService;
            }
            private set
            {
                this._tripService = value;
            }
        }
    }
}
