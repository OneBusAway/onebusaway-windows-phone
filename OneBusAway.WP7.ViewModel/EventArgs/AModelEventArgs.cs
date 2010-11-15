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

namespace OneBusAway.WP7.ViewModel.EventArgs
{
    public class AModelEventArgs : System.EventArgs
    {
        public Exception error { get; private set; }
        public object state { get; private set; }

        public AModelEventArgs(Exception error)
            : this(error, null)
        {

        }

        public AModelEventArgs(Exception error, object state)
        {
            this.error = error;
            this.state = state;
        }
    }
}
