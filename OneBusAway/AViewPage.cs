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
using Microsoft.Phone.Controls;
using OneBusAway.WP7.ViewModel;
using Microsoft.Phone.Tasks;
using System.Windows.Threading;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;

namespace OneBusAway.WP7.View
{
    public class AViewPage : PhoneApplicationPage
    {
        private static Object errorProcessingLock;
        private static Object reportingErrorLock;
        private static bool reportingError;
        protected AViewModel aViewModel;
        private static Dispatcher dispatcher;

        static AViewPage()
        {
            reportingError = false;
            errorProcessingLock = new Object();
            reportingErrorLock = new Object();
            dispatcher = null;
        }

        public AViewPage()
        {
            dispatcher = Dispatcher;
        }

        // Have to have a seperate Initialize() method because ViewModel hasn't been instanciated when 
        // the constructor is called
        protected void Initialize()
        {
            if (Resources.Contains("ViewModel") == true)
            {
                aViewModel = Resources["ViewModel"] as AViewModel;
            }
            else
            {
                aViewModel = null;
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (aViewModel != null)
            {
                aViewModel.ErrorHandler += new EventHandler<ViewModel.EventArgs.ErrorHandlerEventArgs>(viewModel_ErrorHandler);
            }
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (aViewModel != null)
            {
                aViewModel.ErrorHandler -= new EventHandler<ViewModel.EventArgs.ErrorHandlerEventArgs>(viewModel_ErrorHandler);
            }
        }

        internal static void unhandledException_ErrorHandler(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            Debug.Assert(false);

            viewModel_ErrorHandler(sender, new ViewModel.EventArgs.ErrorHandlerEventArgs(e.ExceptionObject));
            e.Handled = true;
        }

        internal static void viewModel_ErrorHandler(object sender, ViewModel.EventArgs.ErrorHandlerEventArgs e)
        {
            // If we're already showing an error to the user don't bother and queue up some more.  They are probably
            // the same issue since connectivity problems will cause 4+ error messages in a row
            lock (reportingErrorLock)
            {
                if (reportingError == true)
                {
                    return;
                }
                else
                {
                    // Need to ensure we are calling MessageBox from the UI thread
                    if (dispatcher != null)
                    {
                        reportingError = true;

                        IDictionary<string, string> exceptionReport = new Dictionary<string, string>();
                        exceptionReport.Add(e.error.GetType().ToString(), e.error.ToString());
                        dispatcher.BeginInvoke(() => viewModel_ErrorHandlerThread(sender, e, exceptionReport));
                    }
                }
            }
        }

        private static void viewModel_ErrorHandlerThread(object sender, ViewModel.EventArgs.ErrorHandlerEventArgs e, IDictionary<string, string> exceptionReport)
        {
            // Ensure that we never process more than one error at a time
            lock (errorProcessingLock)
            {
                string errorTitle = "Uh oh...";
                string errorMessage;
                MessageBoxButton messageBoxType = MessageBoxButton.OK;

                if (e.error is WebException)
                {
                    errorTitle = "Internet Unavailable";
                    errorMessage =
                        "We couldn't reach the OneBusAway service:  " +
                        "please make sure your phone is correctly connected to the internet, " +
                        "or the OneBusAway service might be unavailable right now.";
                    messageBoxType = MessageBoxButton.OK;
                } 
                else if (e.error is LocationUnavailableException)
                {
                    errorTitle = "Location Unavailable";
                    errorMessage = e.error.Message;                        
                    messageBoxType = MessageBoxButton.OK;
                }
                else if (e.error is WebserviceParsingException)
                {
                    errorMessage =
                        "Something went wrong decyphering the bus status, " +
                        "would you like to report this error to us so we can try and fix it?";
                    messageBoxType = MessageBoxButton.OKCancel;
                }
                else if (e.error is WebserviceResponseException)
                {
                    // If the error code is set to "unused" then we couldn't even parse the return code
                    // from the OBA resopnse
                    if (((WebserviceResponseException)e.error).ServerStatusCode == HttpStatusCode.Unused)
                    {
                        errorTitle = "Internet Unavailable";
                        errorMessage =
                            "Check if you are connected to a WIFI network which requires a login " +
                            "or try to open a web page in Internet Explorer.\r\n\r\n" +
                            "We were able to reach the internet but the response we received wasn't from OneBusAway. " + 
                            "This normally means you are connected to a WIFI network which is returning a login page instead.";
                        messageBoxType = MessageBoxButton.OK;
                    }
                    else
                    {
                        errorMessage =
                            "We were able to contact OneBusAway but the service returned an error. " +
                            "We don't think this is our fault, but would you like to report this error so we can make sure?";
                        messageBoxType = MessageBoxButton.OKCancel;
                    }
                }
                else
                {
                    errorMessage =
                        "Unfortunately I don't have a clue why this happened, " +
                        "but would you like to report this error to us so we can try and fix it?";
                    messageBoxType = MessageBoxButton.OKCancel;
                }

                MessageBoxResult sendReport = MessageBox.Show(errorMessage, errorTitle, messageBoxType);
                if (messageBoxType == MessageBoxButton.OKCancel && sendReport == MessageBoxResult.OK)
                {
                     //Sending the email will take OBA out of the foreground, so leave reportingErorr set to true 
                     //to make sure we don't try to send any more error reports from the background which will hit an exception.
                     //When the app is un-tombstoned the constructor will set reportingError back to false.
                    
                    Version version = new AssemblyName(Assembly.GetExecutingAssembly().FullName).Version;

                    EmailComposeTask emailComposeTask = new EmailComposeTask();
                    emailComposeTask.To = AViewModel.FeedbackEmailAddress;
                    emailComposeTask.Body = string.Format(
                        "Please tell us a few details about what you were doing when the error occurred: \r\n\r\n\r\n" +
                        "Debugging info for us: \r\n" +
                        "OneBusAway Version: {0} \r\n" +
                        "{1}",
                        version,
                        e.error
                        );

                    // The email task will crash if the message is longer than 32k characters
                    if (emailComposeTask.Body.Length > 30000)
                    {
                        emailComposeTask.Body = emailComposeTask.Body.Remove(30000);
                    }

                    emailComposeTask.Subject = string.Format("OneBusAway Error: {0}", e.error.GetType());
                    emailComposeTask.Show();
                }
                else
                {
                    lock (reportingErrorLock)
                    {
                        reportingError = false;
                    }
                }
            }
        }
    }
}
