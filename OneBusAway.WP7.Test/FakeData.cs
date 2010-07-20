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
using System.Device.Location;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using OneBusAway.WP7.ViewModel.AppDataDataStructures;
using System.Collections.Generic;

namespace OneBusAway.WP7.Test
{
    public class FakeData
    {

        internal GeoCoordinate OTC = new GeoCoordinate(47.644385, -122.135353);
        internal GeoCoordinate HOME = new GeoCoordinate(47.67652682262796, -122.3183012008667);

        internal Stop STOP_RAVENNA = new Stop();
        internal Stop STOP_UDIST = new Stop();
        internal Route ROUTE = new Route();
        internal RouteStops ROUTE_STOPS = new RouteStops();

        internal FavoriteRouteAndStop FAVORITE = new FavoriteRouteAndStop();

        public static FakeData Singleton = new FakeData();

        public FakeData()
        {
            STOP_RAVENNA.direction = "W";
            STOP_RAVENNA.id = "1_10100";
            STOP_RAVENNA.location = new GeoCoordinate(47.6695671, -122.305412);
            STOP_RAVENNA.name = "NE Ravenna Blvd & Park Rd NE";

            STOP_UDIST.direction = "S";
            STOP_UDIST.id = "1_10914";
            STOP_UDIST.location = new GeoCoordinate(47.6564255, -122.312164);
            STOP_UDIST.name = "15th Ave NE & NE Campus Pkwy";

            ROUTE.agency = null;
            ROUTE.closestStop = null;
            ROUTE.description = "Sandpoint/U-Dist/Seattle Center";
            ROUTE.id = "1_30";
            ROUTE.shortName = "30";
            ROUTE.url = "http://metro.kingcounty.gov/tops/bus/schedules/s030_0_.html";

            ROUTE_STOPS.name = "Downtown";
            ROUTE_STOPS.stops = new List<Stop>() { STOP_RAVENNA, STOP_UDIST };

            FAVORITE.route = ROUTE;
            FAVORITE.stop = STOP_UDIST;
            FAVORITE.routeStops = ROUTE_STOPS;
        }
    }
}
