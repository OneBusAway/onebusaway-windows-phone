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

            //public void callback_Completed(object sender, AModelEventArgs e)
            //{
            //    Assert.AreEqual(e.error, null);
            //    finished = true;
            //}
        }

    }
}
