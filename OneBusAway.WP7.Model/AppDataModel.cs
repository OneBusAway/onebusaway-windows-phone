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

        private Dictionary<FavoriteType, string> fileNames;
        private Dictionary<FavoriteType, List<FavoriteRouteAndStop>> favorites;
        private bool initialized;
        private Object initializeLock;

        #endregion

        #region Events

        public event EventHandler<FavoritesChangedEventArgs> Favorites_Changed;
        public event EventHandler<FavoritesChangedEventArgs> Recents_Changed;

        #endregion

        #region Constructor/Initialize/Singleton

        public static AppDataModel Singleton = new AppDataModel();

        // Constructor is public for testing purposes
        public AppDataModel()
        {
            initialized = false;
            initializeLock = new Object();

            fileNames = new Dictionary<FavoriteType, string>(2);
            fileNames.Add(FavoriteType.Favorite, "favorites.xml");
            fileNames.Add(FavoriteType.Recent, "recent.xml");

            favorites = new Dictionary<FavoriteType, List<FavoriteRouteAndStop>>(2);
        }

        private void Initialize()
        {
            lock (initializeLock)
            {
                if (initialized == false)
                {
                    favorites[FavoriteType.Favorite] = ReadFavoritesFromDisk(fileNames[FavoriteType.Favorite]);
                    favorites[FavoriteType.Recent] = ReadFavoritesFromDisk(fileNames[FavoriteType.Recent]);

                    initialized = true;
                }
            }
        }

        #endregion

        #region IAppDataModel Methods

        // Favorites Methods

        public void AddFavorite(FavoriteRouteAndStop favorite, FavoriteType type)
        {
            Exception error = null;

            try
            {
                Initialize();

                // If the recent already exists delete the old instance.
                // This way the new one will be added with the new LastAccessed time.
                if (type == FavoriteType.Recent && IsFavorite(favorite, type))
                {
                    // The comparison doesn't compare the LastAccessed times so
                    // it will remove the other copy
                    favorites[type].Remove(favorite);
                }

                // Remove the oldest favorite if 15 entires already exist
                if (type == FavoriteType.Recent && favorites[type].Count >= 15)
                {
                    favorites[type].Sort(new RecentLastAccessComparer());
                    favorites[type].RemoveAt(favorites[type].Count - 1);
                }

                favorites[type].Add(favorite);
                WriteFavoritesToDisk(favorites[type], fileNames[type]);
            }
            catch (Exception e)
            {
                Debug.Assert(false);
                error = e;
            }

            if (Favorites_Changed != null)
            {
                Favorites_Changed(this, new FavoritesChangedEventArgs(favorites[type], error));
            }
        }

        public List<FavoriteRouteAndStop> GetFavorites(FavoriteType type)
        {
            Initialize();

            return favorites[type];
        }

        public void DeleteFavorite(FavoriteRouteAndStop favorite, FavoriteType type)
        {
            Exception error = null;

            try
            {
                Initialize();

                favorites[type].Remove(favorite);
                WriteFavoritesToDisk(favorites[type], fileNames[type]);
            }
            catch (Exception e)
            {
                Debug.Assert(false);
                error = e;
            }

            if (Favorites_Changed != null)
            {
                Favorites_Changed(this, new FavoritesChangedEventArgs(favorites[type], error));
            }
        }

        public void DeleteAllFavorites(FavoriteType type)
        {
            Exception error = null;

            try
            {
                Initialize();

                favorites[type].Clear();
                WriteFavoritesToDisk(favorites[type], fileNames[type]);
            }
            catch (Exception e)
            {
                Debug.Assert(false);
                error = e;
            }

            if (Favorites_Changed != null)
            {
                Favorites_Changed(this, new FavoritesChangedEventArgs(favorites[type], error));
            }
        }

        public bool IsFavorite(FavoriteRouteAndStop favorite, FavoriteType type)
        {
            Initialize();

            return favorites[type].Contains(favorite);
        }

        #endregion

        #region Private Methods

        // Added for analytics
        private Dictionary<string, string> NumberOfFavorites
        {
            get
            {
                Dictionary<string, string> data = new Dictionary<string, string>();
                data.Add("Favorites-Count", (favorites[FavoriteType.Favorite].Count).ToString());
                data.Add("Recents-Count", (favorites[FavoriteType.Recent].Count).ToString());
                return data;
            }
        }

        private static void WriteFavoritesToDisk(List<FavoriteRouteAndStop> favoritesToWrite, string fileName)
        {
            using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream favoritesFile = appStorage.OpenFile(fileName, FileMode.Create))
                {
                    List<Type> knownTypes = new List<Type>(2);
                    knownTypes.Add(typeof(FavoriteRouteAndStop));
                    knownTypes.Add(typeof(RecentRouteAndStop));

                    DataContractSerializer serializer = new DataContractSerializer(favoritesToWrite.GetType(), knownTypes);
                    serializer.WriteObject(favoritesFile, favoritesToWrite);
                }
            }
        }

        private static List<FavoriteRouteAndStop> ReadFavoritesFromDisk(string fileName)
        {
            List<FavoriteRouteAndStop> favoritesFromFile = new List<FavoriteRouteAndStop>();

            try
            {
                using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (appStorage.FileExists(fileName) == true)
                    {
                        using (IsolatedStorageFileStream favoritesFile = appStorage.OpenFile(fileName, FileMode.Open))
                        {
                            List<Type> knownTypes = new List<Type>(2);
                            knownTypes.Add(typeof(FavoriteRouteAndStop));
                            knownTypes.Add(typeof(RecentRouteAndStop));

                            DataContractSerializer serializer = new DataContractSerializer(favoritesFromFile.GetType(), knownTypes);
                            favoritesFromFile = serializer.ReadObject(favoritesFile) as List<FavoriteRouteAndStop>;
                        }

                        // This is required because we changed the data format between versions 
                        if (favoritesFromFile.Count > 0 && favoritesFromFile[0].version != FavoriteRouteAndStop.CurrentVersion)
                        {
                            // Currently we don't support backwards compatability, just delete all their favorites/recents
                            appStorage.DeleteFile(fileName);
                            favoritesFromFile = new List<FavoriteRouteAndStop>();
                        }
                    }
                    else
                    {
                        favoritesFromFile = new List<FavoriteRouteAndStop>();
                    }
                }
            }
            catch (Exception)
            {
                Debug.Assert(false);

                // We hit an error deserializing the file so delete it if it exists
                using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (appStorage.FileExists(fileName) == true)
                    {
                        appStorage.DeleteFile(fileName);
                    }
                }

                throw;
            }

            return favoritesFromFile;
        }

        #endregion
        
    }
}
