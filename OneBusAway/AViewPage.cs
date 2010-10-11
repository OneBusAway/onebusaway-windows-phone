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

namespace OneBusAway.WP7.View
{
    public class AViewPage : PhoneApplicationPage
    {
        private Object reportingErrorLock;
        private bool reportingError;
        protected AViewModel aViewModel;

        public AViewPage()
        {
            reportingError = false;
            reportingErrorLock = new Object();
        }

        // Have to have a seperate Initialize() method because ViewModel hasn't been instanciated when 
        // the constructor is called
        protected void Initialize()
        {
            aViewModel = Resources["ViewModel"] as AViewModel;
            if (aViewModel != null)
            {
                aViewModel.ErrorHandler += new EventHandler<ViewModel.EventArgs.ErrorHandlerEventArgs>(viewModel_ErrorHandler);
            }
        }

        void viewModel_ErrorHandler(object sender, ViewModel.EventArgs.ErrorHandlerEventArgs e)
        {
            // If we're already showing an error to the user don't bother and queue up some more.  They are probably
            // the same issue since connectivity problems will cause 4+ error messages in a row
            lock (reportingErrorLock)
            {
                if (reportingError == false)
                {
                    string errorMessage = "Dang! We've hit an error so you might have to find your bus the old-fashioned way :(\r\n\r\n";

                    if (e.error is WebException)
                    {
                        errorMessage +=
                            "We couldn't reach the OneBusAway service:  " +
                            "please make sure your phone is correctly connected to the internet, " +
                            "or the OneBusAway service might be unavailable right now.";
                        MessageBox.Show(errorMessage, "Uh oh...", MessageBoxButton.OK);

                        return;
                    }

                    if (e.error is WebserviceParsingException)
                    {
                        errorMessage +=
                            "Something went wrong decyphering the bus status, " +
                            "would you like to report this error to us so we can try and fix it?";
                    }
                    else
                    {
                        errorMessage +=
                            "Unfortunately I don't have a clue why this happened, " +
                            "but would you like to report this error to us so we can try and fix it?";
                    }

                    MessageBoxResult sendReport = MessageBox.Show(errorMessage, "Uh oh...", MessageBoxButton.OKCancel);
                    if (sendReport == MessageBoxResult.OK)
                    {
                        // Sending the email will take OBA out of the foreground, so make sure we don't
                        // try to send any more error reports from the background or we'll hit an exception.
                        // When the app is un-tombstoned the constructor will set reportingError back to false.
                        reportingError = true;

                        EmailComposeTask emailComposeTask = new EmailComposeTask();
                        emailComposeTask.To = AViewModel.FeedbackEmailAddress;
                        emailComposeTask.Body = string.Format(
                            "Please tell us a few details about what you were doing when the error occured: \r\n\r\n\r\n" +
                            "Debugging info for us: \r\n{0}",
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
                }
            }
        }
    }
}
