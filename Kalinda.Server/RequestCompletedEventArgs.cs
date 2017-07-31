using System;

namespace Kalinda.Server
{
    public class RequestCompletedEventArgs : EventArgs
    {
        public long RequestDurationTickCount { get; set; }
        public Exception Exception { get; set; }
        public bool Success
        {
            get { return Exception == null; }
        }
    }
}