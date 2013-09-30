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
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace OneBusAway.WP7.ViewModel.BusServiceDataStructures
{
    [DataContract()]
    public class Coordinate
    {
        [DataMember()]
        public double Latitude { get; set; }
        [DataMember()]
        public double Longitude { get; set; }
    }

    [DataContract()]
    public class PolyLine
    {
        private List<Coordinate> coordinates;
        public List<Coordinate> Coordinates
        {
            get
            {
                if (coordinates == null)
                {
                    coordinates = DecodeLatLongList(pointsString);
                }

                return coordinates;
            }
        }

        [DataMember()]
        public string pointsString { get; set; }
        [DataMember()]
        public string length { get; set; }
        [DataMember()]
        public string levels { get; set; }

        private static List<Coordinate> DecodeLatLongList(string encoded)
        {

            int index = 0;
            int lat = 0;
            int lng = 0;

            int len = encoded.Length;
            List<Coordinate> locs = new List<Coordinate>();

            while (index < len)
            {
                lat += decodePoint(encoded, index, out index);
                lng += decodePoint(encoded, index, out index);

                Coordinate loc = new Coordinate();
                loc.Latitude = (lat * 1e-5);
                loc.Longitude = (lng * 1e-5);

                locs.Add(loc);
            }

            return locs;
        }

        private static int decodePoint(string encoded, int startindex, out int finishindex)
        {
            int b;
            int shift = 0;
            int result = 0;

            //magic google algorithm, see http://code.google.com/apis/maps/documentation/polylinealgorithm.html
            do
            {
                b = Convert.ToInt32(encoded[startindex++]) - 63;
                result |= (b & 0x1f) << shift;
                shift += 5;
            } while (b >= 0x20);
            //if negative flip
            int dlat = (((result & 1) > 0) ? ~(result >> 1) : (result >> 1));

            //set output index
            finishindex = startindex;

            return dlat;
        }
    }
}
