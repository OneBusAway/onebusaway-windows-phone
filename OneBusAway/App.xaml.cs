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
using Microsoft.Phone.Controls;
using System.Windows.Navigation;
using System.Windows.Controls.Primitives;
using System.Threading;
using System.IO.IsolatedStorage;
using OneBusAway.WP7.ViewModel.LocationServiceDataStructures;
using OneBusAway.WP7.Model;

namespace OneBusAway.WP7.View
{
    public partial class App : Application
    {
        private ViewState viewState = ViewState.Instance;
        public PhoneApplicationFrame RootFrame { get; private set; }

        private bool FeedbackEnabled
        {
            get
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains("FeedbackEnabled") == true)
                {
                    return bool.Parse(IsolatedStorageSettings.ApplicationSettings["FeedbackEnabled"].ToString());
                }
                else
                {
                    // We default to enabled if there is no user setting
                    return true;
                }
            }
        }

        public App()
        {
            UnhandledException += new EventHandler<ApplicationUnhandledExceptionEventArgs>(AViewPage.unhandledException_ErrorHandler);

            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        void Application_Activated(object sender, ActivatedEventArgs e)
        {
            if (e.IsApplicationInstancePreserved == false)
            {
                viewState.CurrentRoute = (Route)GetStateHelper("CurrentRoute", typeof(Route));
                viewState.CurrentRoutes = (List<Route>)GetStateHelper("CurrentRoutes", typeof(List<Route>));
                viewState.CurrentRouteDirection = (RouteStops)GetStateHelper("CurrentRouteDirection", typeof(RouteStops));
                viewState.CurrentStop = (Stop)GetStateHelper("CurrentStop", typeof(Stop));
                viewState.CurrentSearchLocation = (LocationForQuery)GetStateHelper("CurrentSearchLocation", typeof(LocationForQuery));
            }
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

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            PhoneApplicationService.Current.State["CurrentRoute"] = Serialize(viewState.CurrentRoute);
            PhoneApplicationService.Current.State["CurrentRoutes"] = Serialize(viewState.CurrentRoutes);
            PhoneApplicationService.Current.State["CurrentRouteDirection"] = Serialize(viewState.CurrentRouteDirection);
            PhoneApplicationService.Current.State["CurrentStop"] = Serialize(viewState.CurrentStop);
            PhoneApplicationService.Current.State["CurrentSearchLocation"] = Serialize(viewState.CurrentSearchLocation);

            IsolatedStorageSettings.ApplicationSettings.Save();
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

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;

        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion
        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
        }



        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }
    }
}
