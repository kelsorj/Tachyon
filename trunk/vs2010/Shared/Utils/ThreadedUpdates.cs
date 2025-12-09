using System;
using System.Threading;

namespace BioNex.Shared.Utils
{
    /// <summary>
    /// Should be used by plugins to do their updating in a thread.  Provides simple Start/Stop functionality,
    /// and uses a callback to let the client deal with its controller-specific operations.
    /// </summary>
    public class ThreadedUpdates
    {
        private ManualResetEvent StopUpdatingEvent { get; set; }
        private Thread UpdateThread { get; set; }
        private Action Callback { get; set; }
        private Action FailureCallback { get; set; }
        private int UpdateFrequency { get; set; }
        private string ThreadName { get; set; }
        public bool Running { get { return UpdateThread != null && UpdateThread.IsAlive; } }

        public ThreadedUpdates( string thread_name, Action callback, int update_frequency_ms=30, Action failure_callback=null)
        {
            ThreadName = thread_name;
            Callback = callback;
            FailureCallback = failure_callback;
            UpdateFrequency = update_frequency_ms;
            StopUpdatingEvent = new ManualResetEvent( false);
        }

        public void Start()
        {
            UpdateThread = new Thread( UpdateThreadFunc);
            UpdateThread.IsBackground = false; // Should not be Background thread, since we're joining on exit, which is a potential race-condition
            UpdateThread.Name = ThreadName;
            StopUpdatingEvent.Reset();
            UpdateThread.Start();
        }

        public void Stop()
        {
            if (!Running)
                return;
            StopUpdatingEvent.Set();
            UpdateThread.Join();
        }

        public void UpdateThreadFunc()
        {
            const int max_failures = 5;
            int num_failures = 0;
            while( !StopUpdatingEvent.WaitOne( UpdateFrequency)) {
                try {
                    Callback();
                    num_failures = 0; // reset counter if we succeed
                } catch( Exception) {
                    // only bail out if the caller provides a failure callback method (could be used to disconnect, like in the HiG case)
                    if( num_failures++ >= max_failures && FailureCallback != null)
                        break;
                }
            }

            if( FailureCallback != null && !StopUpdatingEvent.WaitOne( 0))
                FailureCallback();
        }
    }
}
