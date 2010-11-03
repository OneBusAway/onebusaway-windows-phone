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
using OneBusAway.WP7.ViewModel;

namespace OneBusAway.WP7.Test
{
    public class FakeData
    {

        internal GeoCoordinate OTC = new GeoCoordinate(47.644385, -122.135353);
        internal GeoCoordinate GREENLAKE_PR = new GeoCoordinate(47.676, -122.32);

        internal Stop STOP_RAVENNA = new Stop();
        internal Stop STOP_UDIST = new Stop();
        internal Route ROUTE_30 = new Route();
        internal Route ROUTE_70 = new Route();
        internal RouteStops ROUTE_STOPS = new RouteStops();

        internal Dictionary<FavoriteType, FavoriteRouteAndStop> FAVORITE = new Dictionary<FavoriteType, FavoriteRouteAndStop>();

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

            ROUTE_30.agency = null;
            ROUTE_30.closestStop = null;
            ROUTE_30.description = "Sandpoint/U-Dist/Seattle Center";
            ROUTE_30.id = "1_30";
            ROUTE_30.shortName = "30";
            ROUTE_30.url = "http://metro.kingcounty.gov/tops/bus/schedules/s030_0_.html";

            ROUTE_70.agency = null;
            ROUTE_70.closestStop = null;
            ROUTE_70.description = "Seattle Downtown";
            ROUTE_70.id = "1_70";
            ROUTE_70.shortName = "70";
            ROUTE_70.url = "http://metro.kingcounty.gov/tops/bus/schedules/s030_0_.html";

            ROUTE_STOPS.name = "Downtown";
            ROUTE_STOPS.stops = new List<Stop>() { STOP_RAVENNA, STOP_UDIST };

            FAVORITE = new Dictionary<FavoriteType, FavoriteRouteAndStop>(2);

            FAVORITE[FavoriteType.Favorite] = new FavoriteRouteAndStop();
            FAVORITE[FavoriteType.Favorite].route = ROUTE_30;
            FAVORITE[FavoriteType.Favorite].stop = STOP_UDIST;
            FAVORITE[FavoriteType.Favorite].routeStops = ROUTE_STOPS;

            FAVORITE[FavoriteType.Recent] = new RecentRouteAndStop();
            FAVORITE[FavoriteType.Recent].route = ROUTE_70;
            FAVORITE[FavoriteType.Recent].stop = STOP_RAVENNA;
            FAVORITE[FavoriteType.Recent].routeStops = ROUTE_STOPS;
        }
    }
}
