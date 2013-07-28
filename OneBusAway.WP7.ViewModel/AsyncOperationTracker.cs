/* Copyright 2013 Shawn Henry, Rob Smith, and Michael Friedman
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
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
