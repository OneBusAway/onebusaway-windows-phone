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

namespace OneBusAway.WP7.ViewModel.DataStructures
{
    public class Coordinate
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class PolyLine
    {
        public List<Coordinate> coordinates = new List<Coordinate>();

        private string pointsString;
        public string points
        {
            get { return pointsString; }
            set
            {
                pointsString = value;
                coordinates = DecodeLatLongList(value);
            }
        }


        public string length { get; set; }

        public string levels { get; set; }

        public static List<Coordinate> DecodeLatLongList(string encoded)
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
