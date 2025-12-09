using System;
using System.Threading;

namespace BioNex.HivePrototypePlugin
{
    /// <summary>
    /// Keeps track of status information used by diagnostics.  This should prevent the
    /// GUI from freezing when we have communication errors.
    /// </summary>
    /// <remarks>
    /// This is only intended to be run while diagnostics is open.  Don't rely on this
    /// for runtime status!  At runtime, we want to query the controller directly to
    /// get the most up-to-date data.
    /// </remarks>
    public class ControllerStatusCache
    {
        private HivePlugin Controller { get; set; }
        private AutoResetEvent StopUpdateEvent { get; set; }
        private Thread UpdateThread { get; set; }

        public ControllerStatusCache(HivePlugin controller)
        {
            Controller = controller;
            StopUpdateEvent = new AutoResetEvent( false);
        }

        public void Start()
        {
            UpdateThread = new Thread( UpdateStatus);
            UpdateThread.Name = "Hive controller status caching";
            UpdateThread.IsBackground = true;  // Should NOT be background, since there's a close event on the thread w/ a join --> joining a background thread is a race condition
            UpdateThread.Start();
        }

        public void Stop()
        {
            StopUpdateEvent.Set();
            if( UpdateThread != null)
                UpdateThread.Join();
        }

        // public bool IsThetaTucked { get; private set; }
        // public bool IsOn { get; private set; }

        public void UpdateStatus()
        {
            while( !StopUpdateEvent.WaitOne( 0)) {
                Thread.Sleep( 100);
                if( !Controller.Initialized || !Controller.TelemetryEnabled)
                    continue;
                try {
                    Controller.Hardware.UpdateCurrentPosition();
                    Controller.UpdateHomingStatus();
                    Controller.UpdateServoOnStatus();
                    // Controller.UpdateAxisDisabledStatus();
                    // IsThetaTucked = Controller.Hardware.IsThetaTucked();
                    // IsOn = Controller.Hardware.IsRobotOn();
                    Controller.Hardware.TMLLibCheckUnrequestedMessages();
                    Controller.UpdateGUI();
                } catch (BioNex.Shared.TechnosoftLibrary.AxisException ) {
                } catch( NullReferenceException ) {
                    // here, we are probably disconnecting via diagnostics
                }
            }
        }
    }
}
