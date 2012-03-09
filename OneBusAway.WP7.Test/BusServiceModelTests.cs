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
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using OneBusAway.WP7.Model;
using System.Device.Location;
using OneBusAway.WP7.ViewModel.EventArgs;
using Microsoft.Silverlight.Testing;

namespace OneBusAway.WP7.Test
{

    [TestClass]
    public class ModelTests : SilverlightTest
    {

        #region Private Variables

        private BusServiceModel model = null;
        private Callback callback = null;
        private FakeData fakeData = null;

        #endregion

        public ModelTests()
        {
            model = BusServiceModel.Singleton;
            model.Initialize();
            fakeData = FakeData.Singleton;
            callback = new Callback();

            model.StopsForLocation_Completed += new EventHandler<StopsForLocationEventArgs>(callback.callback_Completed);
            model.ArrivalsForStop_Completed += new EventHandler<ArrivalsForStopEventArgs>(callback.callback_Completed);
            model.RoutesForLocation_Completed += new EventHandler<RoutesForLocationEventArgs>(callback.callback_Completed);
            model.ScheduleForStop_Completed += new EventHandler<ScheduleForStopEventArgs>(callback.callback_Completed);
            model.StopsForRoute_Completed += new EventHandler<StopsForRouteEventArgs>(callback.callback_Completed);
            model.TripDetailsForArrival_Completed += new EventHandler<TripDetailsForArrivalEventArgs>(callback.callback_Completed);
            model.SearchForRoutes_Completed += new EventHandler<SearchForRoutesEventArgs>(callback.callback_Completed);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            // Runs before every test to ensure the callback is reset
            callback.ResetFinished();
        }

        [TestMethod]
        [Asynchronous]
        public void ArrivalsForStop()
        {
            ModelTest(() => model.ArrivalsForStop(fakeData.STOP_RAVENNA));
        }

        [TestMethod]
        [Asynchronous]
        public void RoutesForLocation()
        {
            ModelTest(() => model.RoutesForLocation(fakeData.GREENLAKE_PR, 1000));
        }

        [TestMethod]
        [Asynchronous]
        public void ScheduleForStop()
        {
            ModelTest(() => model.ScheduleForStop(fakeData.STOP_RAVENNA));
        }

        [TestMethod]
        [Asynchronous]
        public void StopsForLocation()
        {
            ModelTest(() => model.StopsForLocation(fakeData.GREENLAKE_PR, 1000));
        }

        [TestMethod]
        [Asynchronous]
        public void StopsForRoute()
        {
            ModelTest(() => model.StopsForRoute(fakeData.ROUTE_30));
        }

        [TestMethod]
        [Asynchronous]
        public void SearchForRoutes()
        {
            ModelTest(() => model.SearchForRoutes(fakeData.GREENLAKE_PR, "48"));
        }

        [TestMethod]
        [Asynchronous]
        public void SearchForRoutes_NoRouteFound()
        {
            ModelTest(() => model.SearchForRoutes(fakeData.GREENLAKE_PR, "RouteDoesNotExist"));
        }

        [TestMethod]
        [Asynchronous]
        public void TripDetailsForArrivals()
        {
            ModelTest(() =>
                {
                    // Calling ArrivalsForStop to get current arrival data
                    ArrivalsForStopEventArgs arrivalsArgs = null;
                    model.ArrivalsForStop_Completed +=
                        (caller, args) => { arrivalsArgs = args; };

                    model.ArrivalsForStop(fakeData.STOP_UDIST);

                    // Wait for ArrivalsForStop to finish both callbacks
                    EnqueueConditional(() => arrivalsArgs != null && callback.finished);

                    // Reset the callback finished flag before making the next call
                    EnqueueCallback(() =>
                        callback.ResetFinished());

                    // Now kick off test for TripDetailsForArrivals
                    EnqueueCallback(() =>
                        model.TripDetailsForArrivals(arrivalsArgs.arrivals));

                    // Cleanup callback
                    EnqueueCallback(() =>
                        model.ArrivalsForStop_Completed -=
                            (caller, args) => { arrivalsArgs = args; }
                    );
                }
            );
        }

        #region Private Methods/Classes

        private delegate void CallToModel();
        private void ModelTest(CallToModel callToModel)
        {
            // The delegate will make the sync call into the model
            callToModel();

            // Wait for the execution to finish
            // The async callback will assert if there is a failure
            EnqueueConditional(() => callback.finished);

            // We made it this far in the queue without an error, pass!
            EnqueueTestComplete();
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

        #endregion

    }
}
