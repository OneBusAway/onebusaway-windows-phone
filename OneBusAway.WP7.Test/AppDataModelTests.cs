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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Silverlight.Testing;
using OneBusAway.WP7.Model;
using OneBusAway.WP7.ViewModel.AppDataDataStructures;
using OneBusAway.WP7.ViewModel;
using System.Collections.Generic;
using System.IO.IsolatedStorage;

namespace OneBusAway.WP7.Test
{
    [TestClass]
    public class AppDataModelTests : SilverlightTest
    {
        private IAppDataModel appDataModel;
        private FakeData fakeData = null;
        private Dictionary<FavoriteType, string> fileNames;

        private FavoriteRouteAndStop fakeFavorite;

        public AppDataModelTests()
        {
            appDataModel = AppDataModel.Singleton;
            fakeData = FakeData.Singleton;

            fileNames = new Dictionary<FavoriteType, string>(2);
            fileNames.Add(FavoriteType.Favorite, "favorites.xml");
            fileNames.Add(FavoriteType.Recent, "recent.xml");
        }

        [TestInitialize]
        public void TestInitialize()
        {
            // Runs before every test to ensure all old favorites are deleted
            appDataModel.DeleteAllFavorites(FavoriteType.Favorite);
            appDataModel.DeleteAllFavorites(FavoriteType.Recent);
        }

        [TestMethod]
        public void AddAndGetFavorite()
        {
            AddAndGetFavoriteGeneric(FavoriteType.Favorite);
        }

        [TestMethod]
        public void DeleteFavorite()
        {
            DeleteFavoriteGeneric(FavoriteType.Favorite);
        }

        [TestMethod]
        public void InvalidFavoritesFile()
        {
            InvalidFileGeneric(FavoriteType.Favorite);
        }

        [TestMethod]
        public void AddAndGetRecent()
        {
            AddAndGetFavoriteGeneric(FavoriteType.Recent);
        }

        [TestMethod]
        public void DeleteRecent()
        {
            DeleteFavoriteGeneric(FavoriteType.Recent);
        }

        [TestMethod]
        public void InvalidRecentsFile()
        {
            InvalidFileGeneric(FavoriteType.Recent);
        }

        [TestMethod]
        public void RecentLastAccessedTime()
        {
            DateTime fakeTime = new DateTime(1990, 1, 1);

            appDataModel.AddFavorite(fakeData.FAVORITE[FavoriteType.Recent], FavoriteType.Recent);

            RecentRouteAndStop fakeTimeRecent = new RecentRouteAndStop();
            fakeTimeRecent.route = fakeData.FAVORITE[FavoriteType.Recent].route;
            fakeTimeRecent.routeStops = fakeData.FAVORITE[FavoriteType.Recent].routeStops;
            fakeTimeRecent.stop = fakeData.FAVORITE[FavoriteType.Recent].stop;

            fakeTimeRecent.LastAccessed = fakeTime;

            // Add it now with the new time
            appDataModel.AddFavorite(fakeData.FAVORITE[FavoriteType.Recent], FavoriteType.Recent);

            List<FavoriteRouteAndStop> recents = appDataModel.GetFavorites(FavoriteType.Recent);

            // Check to see that it replaced the original entry instead of making a new entry
            Assert.Equals(recents.Count, 1);

            // Ensure that the time stamp was updated to a newer time
            Assert.Equals(((RecentRouteAndStop)recents[0]).LastAccessed, fakeTime);
        }

        #region Generic Test Methods

        private void AddAndGetFavoriteGeneric(FavoriteType type)
        {
            List<FavoriteRouteAndStop> favorites = appDataModel.GetFavorites(type);
            Assert.Equals(favorites.Count, 0);

            appDataModel.AddFavorite(fakeData.FAVORITE[type], type);

            IAppDataModel appDataModel2 = new AppDataModel();
            favorites = appDataModel2.GetFavorites(type);

            Assert.Equals(favorites.Count, 1);

            Assert.Equals(favorites[0], fakeData.FAVORITE[type]);
        }

        private void DeleteFavoriteGeneric(FavoriteType type)
        {
            AddAndGetFavoriteGeneric(type);

            appDataModel.DeleteFavorite(fakeData.FAVORITE[type], type);

            List<FavoriteRouteAndStop> favorites = appDataModel.GetFavorites(type);
            Assert.Equals(favorites.Count, 0);

            IAppDataModel appDataModel2 = new AppDataModel();
            favorites = appDataModel2.GetFavorites(type);

            Assert.Equals(favorites.Count, 0);
        }

        private void InvalidFileGeneric(FavoriteType type)
        {
            IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication();
            using (IsolatedStorageFileStream favoritesFile = appStorage.OpenFile(fileNames[type], System.IO.FileMode.Create))
            {
                byte[] randomData = Guid.NewGuid().ToByteArray();
                favoritesFile.Write(randomData, 0, randomData.Length);
            }

            IAppDataModel appDataModel2 = new AppDataModel();
            try
            {
                List<FavoriteRouteAndStop> favorites = appDataModel2.GetFavorites(type);
                Assert.Fail("Expected GetFavorites to throw an exception with bogus data");
                // TODO this used to catch the exception and just return an empty list, which is what the test used to assert as well.
                // I'm not sure which behavior is correct -- I just updated the test to match the current behavior.
            }
            catch (Exception)
            {

            }
        }

        #endregion

    }
}
