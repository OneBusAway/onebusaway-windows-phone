using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace OneBusAway.WP7.ViewModel
{
    /// <summary>
    /// Keeps track of multiple pending asynchronous operations.
    /// Client is responsible for notifying us of operation begin / end, and for providing unique names.
    /// </summary>
    public class AsyncOperationTracker : INotifyPropertyChanged
    {
        private Object pendingOperationsLock;
        private ObservableCollection<KeyValuePair<string, string>> pendingOperationsList;

        public AsyncOperationTracker()
        {
            // Set up the default action, just execute in the same thread
            UIAction = (uiAction => uiAction());

            pendingOperationsLock = new Object();
            pendingOperationsList = new ObservableCollection<KeyValuePair<string, string>>();
            pendingOperationsList.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(pendingOperationsList_CollectionChanged);
        }

        public Action<Action> UIAction { get; set; }

        public bool Loading
        {
            get 
            {
                return pendingOperationsList.Count != 0;
            }
        }

        public string LoadingMessage
        {
            get 
            {
                if (pendingOperationsList.Count > 0)
                {
                    return pendingOperationsList[0].Value;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public void ClearOperations()
        {
            lock (pendingOperationsLock)
            {
                pendingOperationsList.Clear();
            }
        }

        public void WaitForOperation(string operationName, string loadingMessage)
        {
            lock (pendingOperationsLock)
            {
                bool wasEmpty = pendingOperationsList.Count == 0;
                pendingOperationsList.Add(new KeyValuePair<string, string>(operationName, loadingMessage));
            }
        }

        public void DoneWithOperation(string operationName)
        {
            lock (pendingOperationsLock)
            {
                KeyValuePair<string, string> itemToRemove = new KeyValuePair<string,string>();
                bool found = false;
                foreach(KeyValuePair<string, string> operation in pendingOperationsList)
                {
                    if (operation.Key == operationName)
                    {
                        found = true;
                        itemToRemove = operation;
                        break;
                    }
                }

                if (found == true)
                {
                    pendingOperationsList.Remove(itemToRemove);
                }
                else
                {
                    Debug.Assert(found, "Someone has told us we're done with operation " + operationName + ", but we weren't waiting for it");
                }
            }
        }

        void pendingOperationsList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("Loading");
            OnPropertyChanged("LoadingMessage");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                UIAction(() =>
                    {
                        // Check again in case it has changed while we waited to execute on the UI thread
                        if (PropertyChanged != null)
                        {
                            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                        }
                    }
                    );
            }
        }
    }
}
