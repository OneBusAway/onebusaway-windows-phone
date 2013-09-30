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
using OneBusAway.WP7.ViewModel;
using dev.virtualearth.net.webservices.v1.geocode;
using dev.virtualearth.net.webservices.v1.common;
using System.Device.Location;
using OneBusAway.WP7.ViewModel.EventArgs;
using System.Diagnostics;
using Microsoft.Phone.Controls.Maps;
using System.Collections.Generic;
using OneBusAway.WP7.ViewModel.LocationServiceDataStructures;
using System.ServiceModel.Channels;
using System.ServiceModel;

namespace OneBusAway.WP7.Model
{
    public class LocationModel : ILocationModel
    {
        #region Private Variables

        private GeocodeServiceClient geocodeService;
        private string bingMapsApiKey = "AtAv-npPzjiTyL6ij1J5cgR7Cxmt6h8e3fHlsTSlfWshc8GQ1jfQB1PnB1VfvBGz";

        #endregion

        #region Constructor/Singleton

        public static LocationModel Singleton = new LocationModel();

        private LocationModel()
        {
            geocodeService = new GeocodeServiceClient(
                new BasicHttpBinding(), 
                new System.ServiceModel.EndpointAddress("http://dev.virtualearth.net/webservices/v1/geocodeservice/GeocodeService.svc")
                );
            geocodeService.GeocodeCompleted += new EventHandler<GeocodeCompletedEventArgs>(geocodeService_GeocodeCompleted);
        }

        #endregion

        #region Public Members

        public event EventHandler<LocationForAddressEventArgs> LocationForAddress_Completed;

        public void LocationForAddress(string addressString, GeoCoordinate searchNearLocation)
        {
            LocationForAddress(addressString, searchNearLocation, null);
        }

        public void LocationForAddress(string addressString, GeoCoordinate searchNearLocation, object callerState)
        {
            GeocodeRequest request = new GeocodeRequest();
            request.Credentials = new dev.virtualearth.net.webservices.v1.common.Credentials()
            {
                ApplicationId = bingMapsApiKey
            };
            request.Query = addressString;
            request.UserProfile = new UserProfile()
            {
                CurrentLocation = new UserLocation()
                {
                    Latitude = searchNearLocation.Latitude,
                    Longitude = searchNearLocation.Longitude
                },
                DistanceUnit = DistanceUnit.Mile,
                DeviceType = DeviceType.Mobile,
                ScreenSize = new SizeOfint()
                {
                    Width = 480,
                    Height = 700
                }
            };

            GeocodeState state = new GeocodeState()
            {
                Query = addressString,
                SearchNearLocation = searchNearLocation,
                CallerState = callerState
            };

            geocodeService.GeocodeAsync(request, state);
        }

        #endregion

        #region Private Members

        private struct GeocodeState
        {
            public string Query { get; set; }
            public GeoCoordinate SearchNearLocation { get; set; }
            public object CallerState { get; set; }
        }

        void geocodeService_GeocodeCompleted(object sender, GeocodeCompletedEventArgs e)
        {
            List<LocationForQuery> locations = new List<LocationForQuery>();

            if (e.Error != null)
            {
                throw e.Error;
            }

            GeocodeResult[] results = e.Result.Results;

            foreach (GeocodeResult result in results)
            {
                LocationForQuery location = new LocationForQuery()
                {
                    name = result.DisplayName,
                    boundingBox = new LocationRect()
                    {
                        Northeast = new GeoCoordinate(result.BestView.Northeast.Latitude, result.BestView.Northeast.Longitude),
                        Southwest = new GeoCoordinate(result.BestView.Southwest.Latitude, result.BestView.Southwest.Longitude)
                    },
                    confidence = (ViewModel.LocationServiceDataStructures.Confidence)(int)result.Confidence
                };

                location.location = new GeoCoordinate()
                {
                    Latitude = result.Locations[0].Latitude,
                    Longitude = result.Locations[0].Longitude,
                    Altitude = result.Locations[0].Altitude
                };

                locations.Add(location);
            }

            if (LocationForAddress_Completed != null)
            {
                GeocodeState state = (GeocodeState)e.UserState;
                LocationForAddress_Completed(this,
                    new LocationForAddressEventArgs(
                        locations,
                        state.Query,
                        state.SearchNearLocation,
                        state.CallerState
                        ));
            }
        }

        #endregion

    }
}
