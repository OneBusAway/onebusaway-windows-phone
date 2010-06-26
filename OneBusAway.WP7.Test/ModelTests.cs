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
using OneBusAway.WP7.ViewModel.DataStructures;
using OneBusAway.WP7.Model;
using System.Device.Location;
using OneBusAway.WP7.ViewModel.EventArgs;
using Microsoft.Silverlight.Testing;

namespace OneBusAway.WP7.Test
{

    [TestClass]
    public class ModelTests : SilverlightTest
    {
        #region Fake Test Data

        private GeoCoordinate OTC = new GeoCoordinate(47.644385, -122.135353);
        private GeoCoordinate HOME = new GeoCoordinate(47.67652682262796, -122.3183012008667);

        private Stop STOP_RAVENNA = new Stop();
        private Stop STOP_UDIST = new Stop();
        private Route ROUTE = new Route();

        #endregion

        #region Private Variables

        private BusServiceModel model = null;
        private Callback callback = null;

        #endregion

        public ModelTests()
        {
            STOP_RAVENNA.direction = "W";
            STOP_RAVENNA.id = "1_10100";
            STOP_RAVENNA.location = new GeoCoordinate(47.6695671, -122.305412);
            STOP_RAVENNA.name = "NE Ravenna Blvd & Park Rd NE";

            STOP_UDIST.direction = "S";
            STOP_UDIST.id = "1_10914";
            STOP_UDIST.location = new GeoCoordinate(47.6564255, -122.312164);
            STOP_UDIST.name = "15th Ave NE & NE Campus Pkwy";

            ROUTE.agency = null;
            ROUTE.closestStop = null;
            ROUTE.description = "Sandpoint/U-Dist/Seattle Center";
            ROUTE.id = "1_30";
            ROUTE.nextArrival = null;
            ROUTE.shortName = "30";
            ROUTE.url = "http://metro.kingcounty.gov/tops/bus/schedules/s030_0_.html";

            model = BusServiceModel.Singleton;
            callback = new Callback();

            model.StopsForLocation_Completed += new EventHandler<StopsForLocationEventArgs>(callback.callback_Completed);
            model.ArrivalsForStop_Completed += new EventHandler<ArrivalsForStopEventArgs>(callback.callback_Completed);
            model.RoutesForLocation_Completed += new EventHandler<RoutesForLocationEventArgs>(callback.callback_Completed);
            model.ScheduleForStop_Completed += new EventHandler<ScheduleForStopEventArgs>(callback.callback_Completed);
            model.StopsForRoute_Completed += new EventHandler<StopsForRouteEventArgs>(callback.callback_Completed);
            model.TripDetailsForArrival_Completed += new EventHandler<TripDetailsForArrivalEventArgs>(callback.callback_Completed);
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
            ModelTest(() => model.ArrivalsForStop(STOP_RAVENNA));
        }

        [TestMethod]
        [Asynchronous]
        public void RoutesForLocation()
        {
            ModelTest(() => model.RoutesForLocation(HOME, 1000));
        }

        [TestMethod]
        [Asynchronous]
        public void ScheduleForStop()
        {
            ModelTest(() => model.ScheduleForStop(STOP_RAVENNA));
        }

        [TestMethod]
        [Asynchronous]
        public void StopsForLocation()
        {
            ModelTest(() => model.StopsForLocation(HOME, 1000));
        }

        [TestMethod]
        [Asynchronous]
        public void StopsForRoute()
        {
            ModelTest(() => model.StopsForRoute(ROUTE));
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

                    model.ArrivalsForStop(STOP_UDIST);

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

            public void callback_Completed(object sender, ABusServiceEventArgs e)
            {
                Assert.AreEqual(e.error, null);
                finished = true;
            }
        }

        #endregion

    }
}
