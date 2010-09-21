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

namespace OneBusAway.WP7.ViewModel.AppDataDataStructures
{
    [DataContract()]
    public class RecentRouteAndStop : FavoriteRouteAndStop
    {
        [DataMember]
        public DateTime LastAccessed { get; set; }

        public RecentRouteAndStop()
        {
            LastAccessed = DateTime.Now;
        }
    }

    public class RecentLastAccessComparer : IComparer<FavoriteRouteAndStop>
    {

        public int Compare(FavoriteRouteAndStop x, FavoriteRouteAndStop y)
        {
            if (x == null && y == null)
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            if (!(x is RecentRouteAndStop) || !(y is RecentRouteAndStop))
            {
                throw new NotSupportedException();
            }

            return ((RecentRouteAndStop)y).LastAccessed.CompareTo(((RecentRouteAndStop)x).LastAccessed);
        }
    }
}
