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

        private FavoriteRouteAndStop fakeFavorite;

        public AppDataModelTests()
        {
            appDataModel = AppDataModel.Singleton;
            fakeData = FakeData.Singleton;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            // Runs before every test to ensure all old favorites are deleted
            appDataModel.DeleteAllFavorites();
        }

        [TestMethod]
        public void AddAndGetFavorite()
        {
            List<FavoriteRouteAndStop> favorites = appDataModel.GetFavorites();
            Assert.Equals(favorites.Count, 0);

            appDataModel.AddFavorite(fakeData.FAVORITE);

            IAppDataModel appDataModel2 = new AppDataModel();
            favorites = appDataModel2.GetFavorites();

            Assert.Equals(favorites.Count, 1);

            Assert.Equals(favorites[0], fakeData.FAVORITE);
        }

        [TestMethod]
        public void DeleteFavorite()
        {
            AddAndGetFavorite();

            appDataModel.DeleteFavorite(fakeData.FAVORITE);

            List<FavoriteRouteAndStop> favorites = appDataModel.GetFavorites();
            Assert.Equals(favorites.Count, 0);

            IAppDataModel appDataModel2 = new AppDataModel();
            favorites = appDataModel2.GetFavorites();

            Assert.Equals(favorites.Count, 0);
        }

        [TestMethod]
        public void InvalidFavoritesFile()
        {
            IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication();
            using (IsolatedStorageFileStream favoritesFile = appStorage.OpenFile("favorites.xml", System.IO.FileMode.Create))
            {
                byte[] randomData = Guid.NewGuid().ToByteArray();
                favoritesFile.Write(randomData, 0, randomData.Length);
            }

            IAppDataModel appDataModel2 = new AppDataModel();
            List<FavoriteRouteAndStop> favorites = appDataModel2.GetFavorites();

            Assert.Equals(favorites.Count, 0);
        }
    }
}
