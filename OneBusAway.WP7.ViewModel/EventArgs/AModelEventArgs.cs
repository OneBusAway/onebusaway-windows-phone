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
        public object state { get; private set; }

        public AModelEventArgs()
            : this(null)
        {

        }

        public AModelEventArgs(object state)
        {
            this.state = state;
        }
    }
}
