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
using OneBusAway.WP7.ViewModel;
using OneBusAway.WP7.ViewModel.EventArgs;
using System.Collections.Generic;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;

namespace OneBusAway.WP7.Test
{
    [TestClass]
    public class MainPageVmTests : SilverlightTest
    {
        private FakeData fakeData;
        private MainPageVM viewModel;

        public MainPageVmTests()
        {
            fakeData = FakeData.Singleton;
            viewModel = new MainPageVM();
        }

        [TestInitialize]
        public void TestInitialize()
        {

        }

        [TestMethod]
        [Asynchronous]
        public void SearchByRoute()
        {
            Assert.Equals(viewModel.Loading, false);

            viewModel.SearchByRoute(
                "48",
                delegate(List<Route> routes, Exception error)
                {
                    Assert.Equals(error, null);
                    Assert.Equals(routes.Count, 1);
                    Assert.Equals(viewModel.Loading, false);

                    EnqueueTestComplete();
                }
            );

            Assert.Equals(viewModel.Loading, true);
        }

        [TestMethod]
        [Asynchronous]
        public void SearchByRoute_NoResult()
        {
            Assert.Equals(viewModel.Loading, false);

            viewModel.SearchByRoute(
                "BusDoesNotExist",
                delegate(List<Route> routes, Exception error)
                {
                    Assert.Equals(error, null);
                    Assert.Equals(routes.Count, 0);
                    Assert.Equals(viewModel.Loading, false);

                    EnqueueTestComplete();
                }
            );

            Assert.Equals(viewModel.Loading, true);
        }

        private class Callback
        {
            public bool finished { get; private set; }

            public Callback()
            {
                ResetFinished();
            }

            public void ResetFinished()
            {
                finished = false;
            }

            public void callback_Completed(object sender, AModelEventArgs e)
            {
                Assert.AreEqual(e.error, null);
                finished = true;
            }
        }

    }
}
