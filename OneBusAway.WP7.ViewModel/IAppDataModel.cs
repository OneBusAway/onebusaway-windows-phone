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
using OneBusAway.WP7.ViewModel.AppDataDataStructures;
using System.Collections.Generic;
using OneBusAway.WP7.ViewModel.EventArgs;

namespace OneBusAway.WP7.ViewModel
{
    public enum FavoriteType
    {
        Favorite,
        Recent
    };

    public interface IAppDataModel
    {

        event EventHandler<FavoritesChangedEventArgs> Favorites_Changed;

        event EventHandler<FavoritesChangedEventArgs> Recents_Changed;

        void AddFavorite(FavoriteRouteAndStop favorite, FavoriteType type);

        List<FavoriteRouteAndStop> GetFavorites(FavoriteType type);

        void DeleteFavorite(FavoriteRouteAndStop favorite, FavoriteType type);

        void DeleteAllFavorites(FavoriteType type);

        bool IsFavorite(FavoriteRouteAndStop favorite, FavoriteType type);

    }
}
