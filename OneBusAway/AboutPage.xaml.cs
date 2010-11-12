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
using Microsoft.Phone.Tasks;
using System.Reflection;
using OneBusAway.WP7.ViewModel;
using Microsoft.Phone.Shell;

namespace OneBusAway.WP7.View
{
    public partial class AboutPage : PhoneApplicationPage
    {
        public AboutPage()
        {
            InitializeComponent();

            VersionTextBlock.Text = "Version " + "2.1.0.1";

#if SCREENSHOT
            SystemTray.IsVisible = false;
#endif
        }

        // Methods overridden for analytics purposes
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        // Methods overridden for analytics purposes
        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        private void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            EmailComposeTask emailComposeTask = new EmailComposeTask();
            emailComposeTask.To = AViewModel.FeedbackEmailAddress;
            emailComposeTask.Body = string.Empty;
            emailComposeTask.Subject = "OneBusAway for Windows Phone";
            emailComposeTask.Show();
        }

        private void AddressBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            WebBrowserTask webBrowserTask = new WebBrowserTask();
            webBrowserTask.URL = "http://onebusaway.org";
            webBrowserTask.Show();
        }

        private void TextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            WebBrowserTask webBrowserTask = new WebBrowserTask();
            webBrowserTask.URL = "http://onebusawaywp7.codeplex.com";
            webBrowserTask.Show();
        }
    }
}
