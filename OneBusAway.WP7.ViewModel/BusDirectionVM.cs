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
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Threading;

namespace OneBusAway.WP7.ViewModel
{
    public class BusDirectionVM : AViewModel
    {
        private Object routeDirectionsLock;
        public ObservableCollection<RouteStops> RouteDirections { get; private set; }

        private int pendingRouteDirectionsCount;
        private List<RouteStops> pendingRouteDirections;

        #region Constructors

        public BusDirectionVM()
            : base()
        {
            Initialize();
        }

        public BusDirectionVM(IBusServiceModel busServiceModel)
            : base(busServiceModel)
        {
            Initialize();
        }

        private void Initialize()
        {
            routeDirectionsLock = new Object();
            pendingRouteDirections = new List<RouteStops>();
            pendingRouteDirectionsCount = 0;
            RouteDirections = new ObservableCollection<RouteStops>();
        }

        #endregion

        public void LoadRouteDirections(List<Route> routes)
        {
            lock (routeDirectionsLock)
            {
                RouteDirections.Clear();
                pendingRouteDirections.Clear();
            }

            pendingRouteDirectionsCount += routes.Count;
            foreach(Route route in routes)
            {
                operationTracker.WaitForOperation("StopsForRoute_" + route.id, string.Format("Looking up details for bus {0}...", route.shortName));
                busServiceModel.StopsForRoute(LocationTracker.CurrentLocation, route);
            }
        }

        void busServiceModel_StopsForRoute_Completed(object sender, EventArgs.StopsForRouteEventArgs e)
        {
            // If the main page is still loading we might receive callbacks for other
            // routes. This fix isn't perfect, but it should fix us in the vast majority
            // of the cases. It will only cause problems if we get two callsbacks for
            // one of the routes we're searching for.
            if (CurrentViewState.CurrentRoutes.Contains(e.route) == true)
            {
                lock (routeDirectionsLock)
                {
                    e.routeStops.ForEach(routeStop => pendingRouteDirections.Add(routeStop));

                    // Subtract 1 because we haven't decremented the count yet
                    if (pendingRouteDirectionsCount - 1 == 0)
                    {
                        if (LocationTracker.LocationKnown == true)
                        {
                            pendingRouteDirections.Sort(new RouteStopsDistanceComparer(locationTracker.CurrentLocation));
                        }

                        pendingRouteDirections.ForEach(route => UIAction(() => RouteDirections.Add(route)));
                    }
                }

                pendingRouteDirectionsCount--;
                operationTracker.DoneWithOperation("StopsForRoute_" + e.route.id);
            }
        }

        public override void RegisterEventHandlers(Dispatcher dispatcher)
        {
            base.RegisterEventHandlers(dispatcher);

            this.busServiceModel.StopsForRoute_Completed += new EventHandler<EventArgs.StopsForRouteEventArgs>(busServiceModel_StopsForRoute_Completed);
        }

        public override void UnregisterEventHandlers()
        {
            base.UnregisterEventHandlers();

            this.busServiceModel.StopsForRoute_Completed -= new EventHandler<EventArgs.StopsForRouteEventArgs>(busServiceModel_StopsForRoute_Completed);
            this.operationTracker.ClearOperations();
        }
    }
}
