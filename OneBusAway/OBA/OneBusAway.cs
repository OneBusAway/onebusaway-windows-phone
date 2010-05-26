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
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Xml.Linq;

namespace OBA
{
    public class Coordinate
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    [XmlRoot("response")]
    public class Response
    {
        public string version { get; set; }
        public string text { get; set; }
        public string code { get; set; }

        [XmlAnyElement("data")]
        public XElement data { get; set; }
    }

    [XmlRoot("data")]
    public class StopWithArrivalsAndDepartures
    {
        [XmlElement]
        public Stop stop { get; set; }

        [XmlArray("nearbyStops")]
        [XmlArrayItem("stop", Type = typeof(Stop))]
        public List<Stop> nearbyStops { get; set; }

        [XmlArray("arrivalsAndDepartures")]
        [XmlArrayItem("arrivalAndDeparture", Type = typeof(ArrivalAndDeparture))]
        public List<ArrivalAndDeparture> arrivalsAndDepartures { get; set; }
    }

    [XmlRoot("data")]
    public class StopsForRoute
    {
        [XmlArray("stops")]
        [XmlArrayItem("stop", Type = typeof(Stop))]
        public List<Stop> stops { get; set; }

        [XmlArray("stopGroupings")]
        [XmlArrayItem("stopGrouping", Type = typeof(StopGrouping))]
        public List<StopGrouping> stopGroupings { get; set; }

        [XmlArray("polylines")]
        [XmlArrayItem("encodedPolyline", Type = typeof(PolyLine))]
        public List<PolyLine> encodedPolylines { get; set; }
    }

    [XmlRoot("data")]
    public class Routes
    {
        [XmlArray("routes")]
        [XmlArrayItem("route", Type = typeof(Route))]
        public List<Route> routes { get; set; }
    }

    [XmlRoot("name")]
    public class Name
    {
        public string type { get; set; }
        [XmlArray("names")]
        [XmlArrayItem("string", Type = typeof(string))]
        public List<string> names { get; set; }
    }

    [XmlRoot("encodedPolyline")]
    public class PolyLineString
    {
        public string points { get; set; }
        public string length { get; set; }
    }

    [XmlRoot("stopGrouping")]
    public class StopGrouping
    {

        public string type { get; set; }
        public string ordered { get; set; }

        [XmlArray("stopGroups")]
        [XmlArrayItem("stopGroup", Type = typeof(StopGroup))]
        public List<StopGroup> stopGroups { get; set; }
    }

    [XmlRoot("stopGroup")]
    public class StopGroup
    {
        public Name name { get; set; }

        [XmlArray("stopIds")]
        [XmlArrayItem("string", Type = typeof(string))]
        public List<string> stopIds { get; set; }

        [XmlArray("polylines")]
        [XmlArrayItem("encodedPolyline", Type = typeof(PolyLine))]
        public List<PolyLine> polys { get; set; }
    }


    [XmlRoot("data")]
    public class Stops
    {
        [XmlElement]
        public string limitExceeded { get; set; }

        [XmlElement]
        public Stop stop { get; set; }

        [XmlArray("arrivalsAndDepartures")]
        [XmlArrayItem("arrivalAndDeparture", Type = typeof(ArrivalAndDeparture))]
        public List<ArrivalAndDeparture> arrivalsAndDepartures { get; set; }

        [XmlArray("routes")]
        [XmlArrayItem("route", Type = typeof(Route))]
        public List<Route> routes { get; set; }

        [XmlArray("stops")]
        [XmlArrayItem("stop", Type = typeof(Stop))]
        public List<Stop> stops { get; set; }

    }

    public class Trip
    {
        public Stop from;
        public Stop to;
        public Route route;
        public string routeShortName
        {
            get { return route.shortName; }
        }
        public string tripHeadsign;
        public string ScheduledArrivalDateTime;
        public string PredictedDeltaMinutes;
    }

    [XmlRoot("data")]
    public class BusData
    {
        [XmlElement]
        public string limitExceeded { get; set; }

        [XmlElement]
        public Stop stop { get; set; }

        [XmlArray("nearbyStops")]
        [XmlArrayItem("stop", Type = typeof(Stop))]
        public List<Stop> nearbyStops { get; set; }

        [XmlArray("arrivalsAndDepartures")]
        [XmlArrayItem("arrivalAndDeparture", Type = typeof(ArrivalAndDeparture))]
        public List<ArrivalAndDeparture> arrivalsAndDepartures { get; set; }

        [XmlArray("routes")]
        [XmlArrayItem("route", Type = typeof(Route))]
        public List<Route> routes { get; set; }

        [XmlArray("stops")]
        [XmlArrayItem("stop", Type = typeof(Stop))]
        public List<Stop> stops { get; set; }
    }
    [XmlRoot("data")]
    public class StopsForLocation
    {
        public string limitExceeded { get; set; }
        public Stop stop { get; set; }

        [XmlArray("arrivalsAndDepartures")]
        [XmlArrayItem("arrivalAndDeparture", Type = typeof(ArrivalAndDeparture))]
        public List<ArrivalAndDeparture> arrivalsAndDepartures { get; set; }

        [XmlArray("routes")]
        [XmlArrayItem("route", Type = typeof(Route))]
        public List<Route> routes { get; set; }

        [XmlArray("stops")]
        [XmlArrayItem("stop", Type = typeof(Stop))]
        public List<Stop> stops { get; set; }
    }

    public class RouteStop
    {
        public Stop stop { get; set; }
        public Route route { get; set; }

        public RouteStop(Route r)
        {
            this.route = r;
        }
    }

    [XmlRoot("data")]
    public class RoutesForLocation
    {
        public string limitExceeded { get; set; }

        [XmlArray("routes")]
        [XmlArrayItem("route", Type = typeof(Route))]
        public List<Route> routes { get; set; }
    }

    [XmlRoot("arrivalAndDeparture")]
    public class ArrivalAndDeparture
    {
        private static DateTime BEGIN_UTC = new DateTime(1970, 1, 1,0, 0, 0, DateTimeKind.Utc);
        public DateTime PredictedArrivalDateTime
        {
            get { return BEGIN_UTC.AddMilliseconds(predictedArrivalTime); }
        }
        public string ScheduledArrivalDateTime
        {
            get { return "schedule arrival " + BEGIN_UTC.AddMilliseconds(scheduledArrivalTime).ToLocalTime().ToShortTimeString(); }
        }
        public string PredictedDeltaMinutes
        {
            get { return (BEGIN_UTC.AddMilliseconds(scheduledArrivalTime) - DateTime.UtcNow).Minutes.ToString() + "min"; }
            //get { return 4; }

        }
        public DateTime PredictedDepartureDateTime
        {
            get { return BEGIN_UTC.AddMilliseconds(predictedDepartureTime); }
        }
        public DateTime ScheduledDepartureDateTime
        {
            get { return BEGIN_UTC.AddMilliseconds(scheduledDepartureTime); }
        }
        
        public string routeId { get; set; }
        public string routeShortName { get; set; }
        public string tripId { get; set; }
        public string tripHeadsign { get; set; }
        public string stopId { get; set; }
        public long predictedArrivalTime { get; set; }
        public long scheduledArrivalTime { get; set; }
        public long predictedDepartureTime { get; set; }
        public long scheduledDepartureTime { get; set; }
        public string status { get; set; }

    }

    [XmlRoot("route")]
    public class Route
    {
        [XmlElement("id")]
        public string ID { get; set; }

        public string shortName { get; set; }
        public string description { get; set; }
        public Agency agency { get; set; }
    }

    [XmlRoot("agency")]
    public class Agency
    {
        public string name { get; set; }
        public string url { get; set; }
        public string timezone { get; set; }
    }

    [XmlRoot("stop")]
    public class Stop
    {
        public string id { get; set; }
        public string name { get; set; }
        public string code { get; set; }

        [XmlElement("lat")]
        public double latitude { get; set; }

        [XmlElement("lon")]
        public double longitude { get; set; }

        [XmlArray("routes")]
        [XmlArrayItem("route", Type = typeof(Route))]
        public List<Route> routes { get; set; }

        public string stopIndex { get; set; }
        public double distance { get; set; }

        //public Microsoft.Devices.Controls.Maps.Location Location
        //{
        //    get { return LocationExtensions.Location(latitude, longitude); }
        //}

        public string RouteString()
        {
            string stopString = "Stops: ";
            foreach (Route route in this.routes)
            {
                stopString += route.shortName + ", ";
            }

            stopString = stopString.Remove(stopString.Length - 2);

            return stopString;
        }

        public string RoutesString
        {
            get
            {
                string stopString = "Stops: ";
                foreach (Route route in this.routes)
                {
                    stopString += route.shortName + ", ";
                }

                stopString = stopString.Remove(stopString.Length - 2);

                return stopString;
            }
        }
    }

    [XmlRoot("encodedPolyline")]
    public class PolyLine
    {
        public List<Coordinate> coordinates = new List<Coordinate>();

        private string pointsString;
        [XmlElement]
        public string points
        {
            get { return pointsString; }
            set
            {
                pointsString = value;
                coordinates = DecodeLatLongList(value);
            }
        }


        [XmlElement]
        public string length { get; set; }

        [XmlElement]
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
