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

namespace OneBusAway.WP7.View
{
    public partial class NotifyPopup : UserControl
    {
        #region Events

        public event EventHandler<NotifyEventArgs> Notify_Completed;
        #endregion

        public NotifyPopup()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(NotifyPopup_Loaded);
        }

        void NotifyPopup_Loaded(object sender, RoutedEventArgs e)
        {
            SwivelForwardIn.Begin();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Dismiss();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {

            if (Notify_Completed != null)
            {
                int numMinutes = TimePicker.SelectedIndex * 5 + 5;
                Notify_Completed(this, new NotifyEventArgs(null, numMinutes));
            }

            Dismiss();
        }

        private void Dismiss()
        {
            SwivelForwardIn.Completed += new EventHandler(SwivelForwardIn_Completed);
            SwivelForwardOut.Begin();
        }

        void SwivelForwardIn_Completed(object sender, EventArgs e)
        {
        }

    }

    public class NotifyEventArgs : System.EventArgs
    {
        public int minutes { get; private set; }

        public NotifyEventArgs(Exception error, int minutes)
        {
            this.minutes = minutes;
        }
    }
}
