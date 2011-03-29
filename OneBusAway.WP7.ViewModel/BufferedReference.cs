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
using System.ComponentModel;

namespace OneBusAway.WP7.ViewModel
{
    /// <summary>
    /// A "double buffer" container for a data object.
    /// </summary>
    /// <typeparam name="T">Must be a reference type</typeparam>
    public class BufferedReference<T> : INotifyPropertyChanged
    {
        public object CurrentSyncRoot { get; private set; }
        /// <summary>
        /// Only the UI thread is allowed to access this value.
        /// </summary>
        public T Current { get; private set; }

        /// <summary>
        /// Other threads access this value.
        /// </summary>
        public T Working { get; private set; }

        public BufferedReference(T current, T working)
        {
            CurrentSyncRoot = new object();
            Current = current;
            Working = working;
        }
        public void Toggle()
        {
            lock (CurrentSyncRoot)
            {
                T temp = Current;
                Current = Working;
                Working = temp;
            }
            OnPropertyChanged("Current");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
