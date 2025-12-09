using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.HiGIntegration
{
    public class SpinTimeUpdatedEventArgs : EventArgs
    {
        public double SecondsRemaining { get; private set; }

        public SpinTimeUpdatedEventArgs( double time_remaining)
        {
            SecondsRemaining = time_remaining;
        }
    }

    /// <summary>
    /// Integrators should only pay attention to the details contained within this interface.  It is intended to make
    /// usage of the HiG integration driver simple and straightforward, as it eliminates all of unnecessary details
    /// in the HiG class.
    /// </summary>
    public interface HiGInterface
    {
        // Events
        /// <summary>
        /// Fired when initialization is complete
        /// </summary>
        event EventHandler InitializeComplete;
        /// <summary>
        /// Fired when there is an initialization error
        /// </summary>
        /// <remarks>
        /// You can get the reason for the error by casting the EventArgs parameter to ErrorEventArgs
        /// and looking at the Reason property.
        /// </remarks>
        event EventHandler InitializeError;
        /// <summary>
        /// Fired when homing is complete
        /// </summary>
        event EventHandler HomeComplete;
        /// <summary>
        /// Fired when there is a homing error
        /// </summary>
        /// <remarks>
        /// You can get the reason for the error by casting the EventArgs parameter to ErrorEventArgs
        /// and looking at the Reason property.
        /// </remarks>
        event EventHandler HomeError;
        /// <summary>
        /// Fired when the shield is opened
        /// </summary>
        event EventHandler OpenShieldComplete;
        /// <summary>
        /// Fired when there is an error opening the shield
        /// </summary>
        /// <remarks>
        /// You can get the reason for the error by casting the EventArgs parameter to ErrorEventArgs
        /// and looking at the Reason property.
        /// </remarks>
        event EventHandler OpenShieldError;
        /// <summary>
        /// Fired when the spin cycle is complete
        /// </summary>
        event EventHandler SpinComplete;
        /// <summary>
        /// Fired when there is a spin error
        /// </summary>
        /// <remarks>
        /// You can get the reason for the error by casting the EventArgs parameter to ErrorEventArgs
        /// and looking at the Reason property.
        /// </remarks>
        event EventHandler SpinError;
        /// <summary>
        /// Fired whenever the remaining time in the spin cycle is recalculated.  This can be used
        /// to update a GUI for progress updates.
        /// </summary>
        event EventHandler SpinTimeRemainingUpdated;
        /// <summary>
        /// Fired whenever the diagnostics dialog is closed.
        /// </summary>
        event EventHandler DiagnosticsClosed;
        /// <summary>
        /// Fired if the HiG is powered down at any point during operation.  You cannot interact with GUI elements
        /// via this event because it is fired from a worker thread.  Instead, try to modify a member variable
        /// and have the main thread act on changes to that variable.  Alternatively, you can cache your main thread's
        /// dispatcher and modify the GUI via a call to Invoke().
        /// </summary>
        /// <remarks>
        /// Do NOT make other calls into the HiG interface from your ForcedDisconnection event handler.  It is okay
        /// to log to a file here.
        /// </remarks>
        event EventHandler ForcedDisconnection;

        /// <summary>
        /// A rough estimate of how much time is left in the current spin cycle
        /// </summary>
        Double TimeRemainingSec { get; }
        /// <summary>
        /// Allows integrators to give the user a rough estimate of how long a spin cycle will take, given the specified parameters
        /// </summary>
        /// <param name="gs">desired Gs, 250-5000</param>
        /// <param name="accel_percent">1-100%</param>
        /// <param name="decel_percent">1-100%</param>
        /// <param name="time_seconds">spin time at specified Gs, in seconds</param>
        /// <returns></returns>
        Double GetEstimatedCycleTime( Double gs, Double accel_percent, Double decel_percent, Double time_seconds);

        // DKM 2011-10-03 added new properties so we would know what state the HiG is in
        bool Homing { get; }
        bool InErrorState { get; }
        bool Spinning { get; }
        bool Idle { get; }
        /// <summary>
        /// Directly queries the HiG controllers to see if they have already been homed.
        /// </summary>
        /// <remarks>
        /// Do NOT use this property to determine when the device is ready to accept commands after
        /// homing.  You should either use the Blocking property to ensure that the device is homed
        /// after calling Home(), or register a handler for the InitializeComplete event.
        /// </remarks>
        bool IsHomed { get; }
        /// <summary>
        /// Returns whether or not a connection has been made to the HiG.  The HiG must
        /// be connected before any commands are accepted.
        /// </summary>
        bool IsConnected { get; }
        /// <summary>
        /// Initializes the device
        /// </summary>
        /// <param name="device_name">Specify the name you want to give this device.  Added to error messages to aid in debugging.</param>
        /// <param name="adapter_device_id">The CAN adapter ID assigned by the USB-CANmodul Utilities.  Valid range is [0,254]</param>
        /// <param name="simulate">Whether or not the device should be simulated.  Set to true if you do not have a device attached.</param>
        void Initialize( String device_name, String adapter_device_id, Boolean simulate);
        /// <summary>
        /// Closes the device connection
        /// </summary>
        void Close();
        /// <summary>
        /// Retrieve the firmware version from the device.  Contains the version numbers for both the "shield" axis
        /// and "spindle axis.
        /// </summary>
        String FirmwareVersion { get; }
        /// <summary>
        /// Retrieve the serial number from the device
        /// </summary>
        String SerialNumber { get; }
        /// <summary>
        /// Displays the diagnostics screen
        /// </summary>
        /// <param name="modal">true = modal, false = modeless</param>
        void ShowDiagnostics( Boolean modal);
        /// <summary>
        /// Returns the last error reported by the device
        /// </summary>
        String LastError { get; }
        /// <summary>
        /// Allows the caller to execute commands in blocking fashion, but calls must be wrapped with a try/catch block
        /// </summary>
        Boolean Blocking { get; set; }
        /// <summary>
        /// Which bucket the HiG is currently presenting.  0 = unknown position, 1 = bucket 1, 2 = bucket 2
        /// </summary>
        Int16 CurrentBucket { get; set; }
        /// <summary>
        /// Homes the device
        /// </summary>
        void Home();
        /// <summary>
        /// Opens the shield and presents the specified bucket
        /// </summary>
        /// <param name="bucket_index">0 = Bucket #1, 1 = Bucket #2</param>
        void OpenShield( Int32 bucket_index);
        /// <summary>
        /// Spins the plates at the specified Gs
        /// </summary>
        /// <param name="gs">desired Gs, 250-5000</param>
        /// <param name="accel_percent">1-100%</param>
        /// <param name="decel_percent">1-100%</param>
        /// <param name="time_seconds">spin time at specified Gs, in seconds</param>
        void Spin( Double gs, Double accel_percent, Double decel_percent, Double time_seconds);
        /// <summary>
        /// Aborts the spin
        /// </summary>
        void AbortSpin();
        /// <summary>
        /// Homes the shield only.  This is a blocking call, and only to be used for unpacking the HiG and performing the first-time setup.
        /// </summary>
        void HomeShield( bool open_shield_after_home_complete);
    }
}
