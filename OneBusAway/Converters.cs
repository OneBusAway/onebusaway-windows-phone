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
using System.Windows.Data;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using System.Device.Location;
using OneBusAway.WP7.ViewModel;
using Microsoft.Phone.Controls.Maps;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OneBusAway.WP7.View
{
    public class StopRoutesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Stop stop = (Stop)value;

            string routes = "Routes: ";
            stop.routes.ForEach(route => routes += route.shortName + ", ");

            return routes.Substring(0, routes.Length - 2); // remove the trailing ", "
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DistanceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return String.Empty;
            }

            Stop stop;
            if (value is Stop)
            {
                stop = (Stop)value;
            }
            else
            {
                stop = ((Route)value).closestStop;
            }

            AViewModel viewModel = parameter as AViewModel;
            if (stop != null && viewModel.LocationTracker.LocationKnown == true)
            {
                double distance = stop.CalculateDistanceInMiles(viewModel.LocationTracker.CurrentLocation);
                return string.Format("Distance: {0:0.00} mi", distance);
            }
            else
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DateTimeDeltaConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                DateTime dateTimeToConvert;

                if (value is DateTime)
                {
                    dateTimeToConvert = (DateTime)value;
                }
                else
                {
                    return string.Empty;
                }

                return (int)((dateTimeToConvert - DateTime.UtcNow).TotalMinutes) + " mins";
            }
            else
            {
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
            {
                DateTime date = (DateTime)value;

                return date.ToLocalTime().ToShortTimeString();
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StopDirectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Stop)
            {
                Stop stop = (Stop)value;

                string direction = string.Empty;
                switch (stop.direction)
                {
                    case "S":
                        direction = "south";
                        break;
                    case "SW":
                        direction = "southwest";
                        break;
                    case "W":
                        direction = "west";
                        break;
                    case "NW":
                        direction = "northwest";
                        break;
                    case "N":
                        direction = "north";
                        break;
                    case "NE":
                        direction = "northeast";
                        break;
                    case "E":
                        direction = "east";
                        break;
                    case "SE":
                        direction = "southeast";
                        break;
                    default:
                        direction = stop.direction;
                        break;
                }

                return string.Format("Direction: {0}", direction);
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    /// <summary>   
    /// A type converter for visibility and boolean values.   
    /// </summary>   
    public class VisibilityConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is bool)
            {
                bool visibility = (bool)value;
                return visibility ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value is Visibility)
            {
                Visibility visibility = (Visibility)value;
                return (visibility == Visibility.Visible);
            }
            else
            {
                return null;
            }
        }
    }

    public class DelayColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is ArrivalAndDeparture)
            {
                ArrivalAndDeparture arrival = (ArrivalAndDeparture)value;

                if (arrival.predictedArrivalTime == null)
                {
                    // There is no predicted arrival time
                    return new SolidColorBrush(Colors.Gray);
                }

                TimeSpan delay = arrival.scheduledArrivalTime - (DateTime)arrival.predictedArrivalTime;

                // Intentionally use Minutes instead of TotalMinutes so that we round
                // to the nearest minute
                if (delay.Minutes < 0)
                {
                    // Bus is running late
                    return Application.Current.Resources["OBABlueBrush"];
                }
                else if (delay.Minutes == 0)
                {
                    // Bus is on time
                    return Application.Current.Resources["OBADarkBrush"];
                }
                else
                {
                    // Bus is running early
                    return Application.Current.Resources["OBARedBrush"];
                }
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

    }

    public class PolylineConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is RouteStops)
            {
                RouteStops routeStops = (RouteStops)value;

                List<LocationCollection> polylines = new List<LocationCollection>();
                foreach (PolyLine pl in routeStops.encodedPolylines)
                {
                    LocationCollection polyline = new LocationCollection();
                    pl.Coordinates.ForEach(coordinate => polyline.Add(new GeoCoordinate(coordinate.Latitude, coordinate.Longitude)));
                    polylines.Add(polyline);
                }

                return polylines;
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    /// <summary>
    /// Convert a ZoomLevel into to ScaleTransform
    /// </summary>
    public class PushpinScaleConverter : IValueConverter
    {
        #region IValueConverter Members

        /// <summary>
        /// Convert a ZoomLevel into to Scale
        /// </summary>
        //
        // The published Bing Maps Zoom scaling factors, in meters/pixel
        //     http://msdn.microsoft.com/en-us/library/aa940990.aspx
        //
        // Bing maps resolution in meters/pixel is defined are by the equation:
        //     (78271.52 / (2^(ZoomLevel-1))) * Cos(Latitude)
        //

        static private double[] BingMapsScaleFactor = { 78271.52, 78271.52, 39135.76, 19567.88,
                                                         9783.94,  4891.97,  2445.98,  1222.99,
                                                          611.50,   305.75,   152.87,    76.44,
                                                           38.22,    19.11,     9.55,     4.78,
                                                            2.39,     1.19,     0.60,     0.30 };

        /// <summary>
        /// Convert a Bing Maps ZoomLevel into meters per pixel, assumes at the equator.
        /// </summary>
        /// <param name="zoomLevel">The current ZoomLevel</param>
        /// <returns>The scale in meters per pixel</returns>
        static public double BingMapsScaleMetersPerPixel(double zoomLevel)
        {
            if (zoomLevel < 0 || zoomLevel > (BingMapsScaleFactor.Length - 1))
                return BingMapsScaleFactor[0];

            return BingMapsScaleFactor[(int)Math.Round(zoomLevel)];
        }

        /// <summary>
        /// Convert a Bing Maps ZoomLevel into meters per pixel, scaled with latitude
        /// </summary>
        /// <param name="zoomLevel">The current ZoomLevel</param>
        /// <param name="latitude">The current Latitude</param>
        /// <returns>The scale in meters per pixel</returns>
        static public double BingMapsScaleMetersPerPixel(double zoomLevel, double latitude)
        {
            if (zoomLevel < 0 || zoomLevel > BingMapsScaleFactor.Length)
                return BingMapsScaleFactor[0];

            return BingMapsScaleFactor[(int)Math.Round(zoomLevel)] * Math.Cos(Math.PI * latitude / 180);
        }

        /// <summary>
        /// Convert a ZoomLevel into to ScaleTransform
        /// 
        /// Copyright 2010 David Robinson  All Rights Reserved
        ///
        /// Redistribution and use in source and binary forms, with or
        /// without modification, are permitted provided that any
        /// reedistributions of source code must retain the above
        /// copyright notice and this condition.
        ///
        /// </summary>
        /// <param name="value">The current ZoomLevel</param>
        /// <param name="targetType">The type of the target (unused)</param>
        /// <param name="parameter">The parameter (unused)</param>
        /// <param name="culture">The current culture (unused)</param>
        /// <returns>A ScaleTransform</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double currentZoomLevel = (double)value;

            //
            // A PushPin is normally 35x35 pixels, at zoom level 15 we wouldlike that to be 1:1
            //

            var ScaleVal = BingMapsScaleFactor[15] / BingMapsScaleMetersPerPixel(currentZoomLevel);

            var transform = new ScaleTransform();
            transform.ScaleX = ScaleVal;
            transform.ScaleY = ScaleVal;

            // Don't change the center point since our Pushpins are center-bound

            return transform;
        }

        /// <summary>
        /// Convert a ScaleTransform into to ZoomeLevel
        /// </summary>
        /// <param name="value">The current ScaleTransform</param>
        /// <param name="targetType">The type of the target (unused)</param>
        /// <param name="parameter">The parameter (unused)</param>
        /// <param name="culture">The current culture (unused)</param>
        /// <returns>A ZoomeLevel</returns>
        /// <exception cref="NotImplementedException">Always thrown</exception>
        /// <remarks>Unimplemented</remarks>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class PivotNameConverter : IValueConverter
    {
        private const string favoritesPivot = "Favorites Pivot";
        private const string recentsPivot = "Recent Pivot";
        private const string stopsPivot = "Stops Pivot";
        private const string routesPivot = "Routes Pivot";
        private const string lastUsedPivot = "Previously Used Pivot";

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is ObservableCollection<MainPagePivots>)
            {
                List<string> list = new List<string>();
                foreach (MainPagePivots pivot in (ObservableCollection<MainPagePivots>)value)
                {
                    list.Add(ConvertPivot(pivot));
                }

                return list;
            }
            else if (value is MainPagePivots)
            {
                return ConvertPivot((MainPagePivots)value);
            }
            else
            {
                return string.Empty;
            }
        }

        private string ConvertPivot(MainPagePivots pivot)
        {
            switch (pivot)
            {
                case MainPagePivots.Favorites:
                    return favoritesPivot;

                case MainPagePivots.LastUsed:
                    return lastUsedPivot;

                case MainPagePivots.Recents:
                    return recentsPivot;

                case MainPagePivots.Routes:
                    return routesPivot;

                case MainPagePivots.Stops:
                    return stopsPivot;

                default:
                    throw new NotImplementedException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string selectedName = value.ToString();

            switch (selectedName)
            {
                case favoritesPivot:
                    return MainPagePivots.Favorites;

                case lastUsedPivot:
                    return MainPagePivots.LastUsed;

                case recentsPivot:
                    return MainPagePivots.Recents;

                case routesPivot:
                    return MainPagePivots.Routes;

                case stopsPivot:
                    return MainPagePivots.Stops;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
