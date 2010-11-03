using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace OneBusAway.WP7.ViewModel
{
    /// <summary>
    /// Keeps track of multiple pending asynchronous operations.
    /// Provides callbacks for a client to be notified when:
    /// 1.  There are no more pending operations.
    /// 2.  There are new pending operations.
    /// Client is responsible for notifying us of operation begin / end, and for providing unique names.
    /// </summary>
    public class AsyncOperationTracker
    {
        private Object pendingOperationsLock;
        private List<string> pendingOperationsList;
        private AllOperationsDone doneCallback;
        private WaitingForOperations waitingCallback;

        public AsyncOperationTracker() : this(null, null)
        {
        }

        public AsyncOperationTracker(AllOperationsDone doneCallback, WaitingForOperations waitingCallback)
        {
            pendingOperationsLock = new Object();
            pendingOperationsList = new List<string>();
            this.doneCallback = doneCallback;
            this.waitingCallback = waitingCallback;
        }

        public void ClearOperations()
        {
            lock (pendingOperationsLock)
            {
                pendingOperationsList.Clear();
                if (doneCallback != null)
                {
                    doneCallback();
                }
            }
        }

        public void WaitForOperation(string operationName)
        {
            lock (pendingOperationsLock)
            {
                bool wasEmpty = pendingOperationsList.Count == 0;
                pendingOperationsList.Add(operationName);
                if (wasEmpty && waitingCallback != null)
                {
                    waitingCallback();
                }
            }
        }

        public void DoneWithOperation(string operationName)
        {
            lock (pendingOperationsLock)
            {
                Debug.Assert(pendingOperationsList.Contains(operationName), "Someone has told us we're done with operation " + operationName + ", but we weren't waiting for it");
                pendingOperationsList.Remove(operationName);
                if (pendingOperationsList.Count == 0 && doneCallback != null)
                {
                    doneCallback();
                }
            }
        }

        public delegate void AllOperationsDone();
        public delegate void WaitingForOperations();
    }
}
