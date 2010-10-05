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

            if (stop != null && AViewModel.LocationKnown == true)
            {
                double distance = stop.CalculateDistanceInMiles(AViewModel.CurrentLocation);
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
                    return new SolidColorBrush(Colors.Blue);
                }
                else if (delay.Minutes == 0)
                {
                    // Bus is on time
                    return new SolidColorBrush(Colors.Green);
                }
                else
                {
                    // Bus is running early
                    return new SolidColorBrush(Colors.Red);
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
}
