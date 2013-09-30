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
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Threading;
using OneBusAway.WP7.ViewModel.LocationServiceDataStructures;

namespace OneBusAway.WP7.ViewModel
{
    public class ViewState : INotifyPropertyChanged
    {
        public static readonly ViewState Instance = new ViewState();

        private ViewState() 
        {
            // Set up the default action, just execute in the same thread
            UIAction = (uiAction => uiAction());
        }

        public Action<Action> UIAction { get; set; }

        private Stop currentStop;
        public Stop CurrentStop 
        {
            get { return currentStop; }

            set
            {
                currentStop = value;
                OnPropertyChanged("CurrentStop");
            }
        }

        private Route currentRoute;
        public Route CurrentRoute
        {
            get { return currentRoute; }

            set
            {
                currentRoute = value;
                OnPropertyChanged("CurrentRoute");
            }
        }

        private List<Route> currentRoutes;
        public List<Route> CurrentRoutes
        {
            get { return currentRoutes; }

            set
            {
                currentRoutes = value;
                OnPropertyChanged("CurrentRoutes");
            }
        }

        RouteStops currentRouteDirection;
        public RouteStops CurrentRouteDirection
        {
            get { return currentRouteDirection; }

            set
            {
                currentRouteDirection = value;
                OnPropertyChanged("CurrentRouteDirection");
            }
        }

        LocationForQuery currentSearchLocation;
        public LocationForQuery CurrentSearchLocation
        {
            get { return currentSearchLocation; }

            set
            {
                currentSearchLocation = value;
                OnPropertyChanged("CurrentSearchLocation");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            UIAction(() => 
                {
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                });
        }
    }
}
