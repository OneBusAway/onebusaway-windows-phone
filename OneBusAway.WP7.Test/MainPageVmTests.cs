/* Copyright 2013 Shawn Henry, Rob Smith, and Michael Friedman
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
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
using System.Windows.Threading;

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
            viewModel.RegisterEventHandlers(this.TestPanel.Dispatcher);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            viewModel.UnregisterEventHandlers();
        }

        [TestMethod]
        [Asynchronous]
        public void SearchByRoute()
        {
            Assert.Equals(viewModel.operationTracker.Loading, false);

            viewModel.SearchByRoute(
                "48",
                delegate(List<Route> routes)
                {
                    Assert.Equals(routes.Count, 1);
                    Assert.Equals(viewModel.operationTracker.Loading, false);

                    EnqueueTestComplete();
                }
            );

            Assert.Equals(viewModel.operationTracker.Loading, true);
        }

        [TestMethod]
        [Asynchronous]
        public void SearchByRoute_NoResult()
        {
            Assert.Equals(viewModel.operationTracker.Loading, false);

            viewModel.SearchByRoute(
                "BusDoesNotExist",
                delegate(List<Route> routes)
                {
                    Assert.Equals(routes.Count, 0);
                    Assert.Equals(viewModel.operationTracker.Loading, false);

                    EnqueueTestComplete();
                }
            );

            Assert.Equals(viewModel.operationTracker.Loading, true);
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
                finished = true;
            }
        }

    }
}
