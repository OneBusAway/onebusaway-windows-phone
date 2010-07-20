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
using OneBusAway.WP7.ViewModel;
using System.Runtime.Serialization;
using System.Collections.Generic;
using OneBusAway.WP7.ViewModel.AppDataDataStructures;
using System.IO.IsolatedStorage;
using System.IO;
using System.Diagnostics;
using OneBusAway.WP7.ViewModel.EventArgs;

namespace OneBusAway.WP7.Model
{
    public class AppDataModel : IAppDataModel
    {

        #region Private Variables

        private List<FavoriteRouteAndStop> favorites;
        private const string favoritesFileName = "favorites.xml";

        #endregion

        #region Events

        public event EventHandler<FavoritesChangedEventArgs> Favorites_Changed;

        #endregion

        #region Constructor/Singleton

        public static AppDataModel Singleton = new AppDataModel();

        // Constructor is public for testing purposes
        public AppDataModel()
        {
            // TODO: Delete this before check-in
            //DeleteAllFavorites();

            favorites = ReadFavoritesFromDisk();
        }

        #endregion

        #region IAppDataModel Methods

        public void AddFavorite(FavoriteRouteAndStop favorite)
        {
            Exception error = null;

            try
            {
                favorites.Add(favorite);
                WriteFavoritesToDisk(favorites);
            }
            catch (Exception e)
            {
                Debug.Assert(false);
                error = e;
            }

            if (Favorites_Changed != null)
            {
                Favorites_Changed(this, new FavoritesChangedEventArgs(favorites, error));
            }
        }

        public List<FavoriteRouteAndStop> GetFavorites()
        {
            return favorites;
        }

        public void DeleteFavorite(FavoriteRouteAndStop favorite)
        {
            Exception error = null;

            try
            {
                favorites.Remove(favorite);
                WriteFavoritesToDisk(favorites);
            }
            catch (Exception e)
            {
                Debug.Assert(false);
                error = e;
            }

            if (Favorites_Changed != null)
            {
                Favorites_Changed(this, new FavoritesChangedEventArgs(favorites, error));
            }
        }

        public void DeleteAllFavorites()
        {
            Exception error = null;

            try
            {
                favorites.Clear();
                WriteFavoritesToDisk(favorites);
            }
            catch (Exception e)
            {
                Debug.Assert(false);
                error = e;
            }

            if (Favorites_Changed != null)
            {
                Favorites_Changed(this, new FavoritesChangedEventArgs(favorites, error));
            }
        }

        public bool IsFavorite(FavoriteRouteAndStop favorite)
        {
            return favorites.Contains(favorite);
        }

        #endregion

        #region Private Methods

        private static void WriteFavoritesToDisk(List<FavoriteRouteAndStop> favoritesToWrite)
        {
            IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication();
            using (IsolatedStorageFileStream favoritesFile = appStorage.OpenFile(favoritesFileName, FileMode.Create))
            {
                DataContractSerializer serializer = new DataContractSerializer(favoritesToWrite.GetType());
                serializer.WriteObject(favoritesFile, favoritesToWrite);
            }
        }

        private static List<FavoriteRouteAndStop> ReadFavoritesFromDisk()
        {
            List<FavoriteRouteAndStop> favoritesFromFile = new List<FavoriteRouteAndStop>();

            try
            {
                IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication();
                if (appStorage.FileExists(favoritesFileName) == true)
                {
                    using (IsolatedStorageFileStream favoritesFile = appStorage.OpenFile(favoritesFileName, FileMode.Open))
                    {
                        DataContractSerializer serializer = new DataContractSerializer(favoritesFromFile.GetType());
                        favoritesFromFile = serializer.ReadObject(favoritesFile) as List<FavoriteRouteAndStop>;
                    }
                }
                else
                {
                    favoritesFromFile = new List<FavoriteRouteAndStop>();
                }
            }
            catch (Exception e)
            {
                Debug.Assert(false);

                // We hit an error deserializing the file so delete it if it exists
                IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication();
                if (appStorage.FileExists(favoritesFileName) == true)
                {
                    appStorage.DeleteFile(favoritesFileName);
                }
            }

            return favoritesFromFile;
        }

        #endregion

    }
}
