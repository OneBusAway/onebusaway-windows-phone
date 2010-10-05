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
using System.IO;
using System.Text;
using System.Runtime.Serialization;

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
            viewState.CurrentRoute = (Route)GetStateHelper("CurrentRoute", typeof(Route));
            viewState.CurrentRouteDirection = (RouteStops)GetStateHelper("CurrentRouteDirection", typeof(RouteStops));
            viewState.CurrentStop = (Stop)GetStateHelper("CurrentStop", typeof(Stop));
        }

        private object GetStateHelper(string key, Type type)
        {
            if (PhoneApplicationService.Current.State.ContainsKey(key) == true)
            {
                return Deserialize((string)PhoneApplicationService.Current.State[key], type);
            }
            else
            {
                return null;
            }
        }

        void Current_Deactivated(object sender, DeactivatedEventArgs e)
        {
            PhoneApplicationService.Current.State["CurrentRoute"] = Serialize(viewState.CurrentRoute);
            PhoneApplicationService.Current.State["CurrentRouteDirection"] = Serialize(viewState.CurrentRouteDirection);
            PhoneApplicationService.Current.State["CurrentStop"] = Serialize(viewState.CurrentStop);
        }

        private string Serialize(Object obj)
        {
            if (obj != null)
            {
                Stream stream = new MemoryStream();
                DataContractSerializer serializer = new DataContractSerializer(obj.GetType());
                serializer.WriteObject(stream, obj);

                // Reset the stream to the begining
                stream.Position = 0;
                return new StreamReader(stream, Encoding.UTF8).ReadToEnd();
            }
            else
            {
                return null;
            }
        }

        private Object Deserialize(string data, Type type)
        {
            if (string.IsNullOrEmpty(data) == false)
            {
                byte[] byteArray = Encoding.UTF8.GetBytes(data);
                Stream stream = new MemoryStream(byteArray);
                DataContractSerializer serializer = new DataContractSerializer(type);

                // Reset the stream to teh begining
                stream.Position = 0;
                return serializer.ReadObject(stream);
            }
            else
            {
                return null;
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred, break in the debugger
                System.Diagnostics.Debugger.Break();
            }

            // By default show the error
            e.Handled = true;
            MessageBox.Show(
                e.ExceptionObject.Message,
                "Error", 
                MessageBoxButton.OK
                );
        }
    }
}
