using System;

namespace BioNex.Shared.DeviceInterfaces
{
    public class SafetyEventArgs : EventArgs
    {
        public bool Overridden { get; private set; }
        public string Message { get; private set; }
        public SafetyEventArgs( bool overridden, string message)
        {
            Overridden = overridden;
            Message = message;
        }
    }

    public interface SafetyInterface
    {
        event EventHandler SafetyEventTriggered;
        event EventHandler SafetyEventReset;
        event EventHandler<SafetyEventArgs> SafetyOverrideEvent;
        void ResetSafety();
        bool InterlocksOverridden { get; }
        /// <summary>
        /// This allows the application to control the startup of the device.  I
        /// needed this because I fixed the Keyence plugin to properly fire events -- before,
        /// it was firing every time through the loop in its update thread!  Now, it just
        /// fires events when the states CHANGE.  But if the thread starts as soon as the
        /// plugin is initialized, it could be before the application has even registered
        /// the safety event handlers!
        /// </summary>
        void StartMonitoring( bool start);
    }
}
