using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Device.Location;
using OneBusAway.WP7.Model;
using OneBusAway.WP7.ViewModel.EventArgs;
using System.Threading;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using Microsoft.Silverlight.Testing;

namespace OneBusAway.WP7.Test
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            Content = UnitTestSystem.CreateTestPage();
            IMobileTestPage imtp = Content as IMobileTestPage;
            if (imtp != null)
            {
                BackKeyPress += (x, xe) => xe.Cancel = imtp.NavigateBack();
            }
        }
            

        private void button1_Click(object sender, RoutedEventArgs e)
        {
        //    try
        //    {
        //        textBlock1.Text = "";

        //        int testNumber = int.Parse(tbTestNumber.Text);
        //        switch (testNumber)
        //        {
        //            case 1:
        //                model.StopsForLocation(HOME, 1000);
        //                break;

        //            case 2:
        //                model.ArrivalsForStop(STOP);
        //                break;

        //            case 3:
        //                model.RoutesForLocation(HOME, 1000);
        //                break;

        //            case 4:
        //                model.ScheduleForStop(STOP);
        //                break;

        //            case 5:
        //                model.StopsForRoute(ROUTE);
        //                break;

        //            default:
        //                throw new NotImplementedException();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        textBlock1.Text = ex.ToString();
        //    }
        }

        //void modelRequest_Completed(object sender, ABusServiceEventArgs e)
        //{
        //    try
        //    {
        //        if (e.error != null)
        //        {
        //            throw e.error;
        //        }

        //        Dispatcher.BeginInvoke(() => { textBlock1.Text = "PASSED"; });
        //    }
        //    catch (Exception ex)
        //    {
        //        Dispatcher.BeginInvoke(() => { textBlock1.Text = ex.ToString(); });
        //    }
        //}
    }
}
