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
using OneBusAway.WP7.ViewModel.AppDataDataStructures;

namespace OneBusAway.WP7.ViewModel.EventArgs
{
    public class FavoritesChangedEventArgs : AModelEventArgs
    {
        public List<FavoriteRouteAndStop> newFavorites { get; private set; }

        public FavoritesChangedEventArgs(List<FavoriteRouteAndStop> newFavorites, Exception error)
            : base(error)
        {
            this.newFavorites = newFavorites;
        }
    }
}
