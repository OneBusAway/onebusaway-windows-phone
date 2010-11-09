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
using OneBusAway.WP7.ViewModel;

namespace OneBusAway.WP7.View
{
    public partial class SettingsPage : AViewPage
    {
        private SettingsVM viewModel;

        public SettingsPage()
            : base()
        {
            InitializeComponent();
            base.Initialize();

            viewModel = Resources["ViewModel"] as SettingsVM;
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

        private void appbar_clear_history_Click(object sender, EventArgs e)
        {
            viewModel.Clear();
        }

        // Created for analytics
        private void ListPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        // Created for analytics
        private void ReportUsageTs_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}