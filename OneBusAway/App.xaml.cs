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
using Microsoft.Phone.Shell;
using OneBusAway.WP7.ViewModel.BusServiceDataStructures;
using OneBusAway.WP7.ViewModel;

namespace OneBusAway.WP7.View
{
    public partial class App : Application
    {
        private ViewState viewState = ViewState.Instance;

        public App()
        {
            UnhandledException += new EventHandler<ApplicationUnhandledExceptionEventArgs>(Application_UnhandledException);

            PhoneApplicationService current = new PhoneApplicationService();
            this.ApplicationLifetimeObjects.Add(current);

            PhoneApplicationService.Current.Deactivated += new EventHandler<DeactivatedEventArgs>(Current_Deactivated);
            PhoneApplicationService.Current.Activated += new EventHandler<ActivatedEventArgs>(Current_Activated);

            InitializeComponent();
        }

        void Current_Activated(object sender, ActivatedEventArgs e)
        {
            viewState.CurrentRoute = (Route)GetStateHelper("CurrentRoute");
            viewState.CurrentRouteDirection = (RouteStops)GetStateHelper("CurrentRouteDirection");
            viewState.CurrentStop = (Stop)GetStateHelper("CurrentStop");
        }

        private object GetStateHelper(string key)
        {
            if (PhoneApplicationService.Current.State.ContainsKey(key) == true)
            {
                return PhoneApplicationService.Current.State[key];
            }
            else
            {
                return null;
            }
        }

        void Current_Deactivated(object sender, DeactivatedEventArgs e)
        {
            PhoneApplicationService.Current.State["CurrentRoute"] = viewState.CurrentRoute;
            PhoneApplicationService.Current.State["CurrentRouteDirection"] = viewState.CurrentRouteDirection;
            PhoneApplicationService.Current.State["CurrentStop"] = viewState.CurrentStop;
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred, break in the debugger
                System.Diagnostics.Debugger.Break();
            }
            else
            {
                // By default show the error
                e.Handled = true;
                MessageBox.Show(e.ExceptionObject.Message + Environment.NewLine + e.ExceptionObject.StackTrace,
                    "Error", MessageBoxButton.OK);
            }
        }
    }
}