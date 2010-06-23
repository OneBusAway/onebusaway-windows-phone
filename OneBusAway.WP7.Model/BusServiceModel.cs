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
using OneBusAway.WP7.ViewModel;
using System.Device.Location;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using OneBusAway.WP7.ViewModel.DataStructures;

namespace OneBusAway.WP7.Model
{
    public class BusServiceModel : IBusServiceModel
    {
        private OneBusAwayWebservice webservice;

        #region Events

        public event EventHandler<ViewModel.EventArgs.StopsForLocationEventArgs> StopsForLocation_Completed;
        public event EventHandler<ViewModel.EventArgs.RoutesForLocationEventArgs> RoutesForLocation_Completed;
        public event EventHandler<ViewModel.EventArgs.StopsForRouteEventArgs> StopsForRoute_Completed;
        public event EventHandler<ViewModel.EventArgs.ArrivalsForStopEventArgs> ArrivalsForStop_Completed;
        public event EventHandler<ViewModel.EventArgs.ScheduleForStopEventArgs> ScheduleForStop_Completed;
        public event EventHandler<ViewModel.EventArgs.TripDetailsForArrivalEventArgs> TripDetailsForArrival_Completed;

        #endregion

        #region Constructor/Singleton

        public static BusServiceModel Singleton = new BusServiceModel();

        private BusServiceModel()
        {
            webservice = OneBusAwayWebservice.Singleton;
        }

        #endregion

        #region Public Methods

        public void StopsForLocation(GeoCoordinate location, int radiusInMeters)
        {
            webservice.StopsForLocation(
                location,
                radiusInMeters,
                delegate(List<Stop> stops, Exception error)
                {
                    if (StopsForLocation_Completed != null)
                    {
                        StopsForLocation_Completed(this, new ViewModel.EventArgs.StopsForLocationEventArgs(stops, location, error));
                    }
                }
            );
        }

        public void RoutesForLocation(GeoCoordinate location, int radiusInMeters)
        {
            webservice.StopsForLocation(
                location,
                radiusInMeters,
                delegate(List<Stop> stops, Exception e)
                {
                    Exception error = e;
                    List<Route> routes = new List<Route>();

                    try
                    {
                        if (error == null)
                        {
                            stops.Sort(new StopDistanceComparer(location));

                            foreach (Stop stop in stops)
                            {
                                foreach (Route route in stop.routes)
                                {
                                    if (routes.Contains(route) == false)
                                    {
                                        route.closestStop = stop;
                                        routes.Add(route);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }

                    if (RoutesForLocation_Completed != null)
                    {
                        RoutesForLocation_Completed(this, new ViewModel.EventArgs.RoutesForLocationEventArgs(location, routes, error));
                    }
                }
            );
        }

        public void StopsForRoute(Route route)
        {
            webservice.StopsForRoute(
                route,
                delegate(List<RouteStops> routeStops, Exception error)
                {
                    if (StopsForRoute_Completed != null)
                    {
                        StopsForRoute_Completed(this, new ViewModel.EventArgs.StopsForRouteEventArgs(route, routeStops, error));
                    }
                }
            );
        }

        public void ArrivalsForStop(Stop stop)
        {
            webservice.ArrivalsForStop(
                stop,
                delegate(List<ArrivalAndDeparture> arrivals, Exception error)
                {
                    if (ArrivalsForStop_Completed != null)
                    {
                        ArrivalsForStop_Completed(this, new ViewModel.EventArgs.ArrivalsForStopEventArgs(stop, arrivals, error));
                    }
                }
            );
        }

        public void ScheduleForStop(Stop stop)
        {
            webservice.ScheduleForStop(
                stop,
                delegate(List<RouteSchedule> schedule, Exception error)
                {
                    if (ScheduleForStop_Completed != null)
                    {
                        ScheduleForStop_Completed(this, new ViewModel.EventArgs.ScheduleForStopEventArgs(stop, schedule, error));
                    }
                }
            );
        }

        public void TripDetailsForArrivals(List<ArrivalAndDeparture> arrivals)
        {
            int count = 0;
            List<TripDetails> tripDetails = new List<TripDetails>(arrivals.Count);
            Exception overallError = null;

            if (arrivals.Count == 0)
            {
                if (TripDetailsForArrival_Completed != null)
                {
                    TripDetailsForArrival_Completed(
                        this,
                        new ViewModel.EventArgs.TripDetailsForArrivalEventArgs(arrivals, tripDetails, overallError)
                        );
                }
            }
            else
            {
                arrivals.ForEach(arrival =>
                    {
                        webservice.TripDetailsForArrival(
                            arrival,
                            delegate(TripDetails tripDetail, Exception error)
                            {
                                if (error != null)
                                {
                                    overallError = error;
                                }
                                else
                                {
                                    tripDetails.Add(tripDetail);
                                }

                                // Is this code thread-safe?
                                count++;
                                if (count == arrivals.Count && TripDetailsForArrival_Completed != null)
                                {
                                    TripDetailsForArrival_Completed(this, new ViewModel.EventArgs.TripDetailsForArrivalEventArgs(arrivals, tripDetails, error));
                                }
                            }
                        );
                    }
                );
            }


        }

        #endregion

    }
}
