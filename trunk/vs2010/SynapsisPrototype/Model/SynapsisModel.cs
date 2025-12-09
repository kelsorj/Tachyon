using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using BioNex.SynapsisPrototype.ViewModel;
using GalaSoft.MvvmLight.Messaging;
using log4net;

namespace BioNex.SynapsisPrototype.Model
{
    [Export("SynapsisModel")]
    [Export(typeof(ICustomSynapsisQuery))]
    public class SynapsisModel : ICustomSynapsisQuery
    {
        private static readonly ILog _log = LogManager.GetLogger( typeof( SynapsisModel));
        private readonly DeviceManager _device_manager;
        public DeviceManager deviceManager { get { return _device_manager; } }
        
        [Import(typeof(PreferencesDialog))]
        public PreferencesDialog Preferences { get; private set; }
        [Import]
        public BioNex.Shared.LibraryInterfaces.ILabwareDatabase LabwareDatabase { get; set; }
        [Import]
        public BioNex.Shared.LibraryInterfaces.ILiquidProfileLibrary LiquidProfileLibrary { get; set; }
        [Import]
        private AbortPauseResumeStateMachine AbortPauseResumeSM { get; set; }

        public bool Aborting { get; private set; }

        public event EventHandler ModelSafetyEventTriggered;
        public event EventHandler ModelSafetyEventReset;
        public event EventHandler<SafetyEventArgs> ModelSafetyOverriddenEvent;
        public bool InterlocksOverridden { 
            get {
                foreach( SafetyInterface si in _device_manager.GetSafetyInterfaces())
                    if( si.InterlocksOverridden)
                        return true;

                return false;
            }

            private set {}
        }

        /// <summary>
        /// used for labware database, liquid profiles, device manager, etc.
        /// </summary>
        [ImportMany]
        public IEnumerable<ISystemSetupEditor> SystemSetupEditors { get; private set; }
        internal ICustomerGUI CustomerGUI { get; set; }

        [ImportingConstructor]
        public SynapsisModel( [Import] DeviceManager device_manager)
        {
            string exe_path = BioNex.Shared.Utils.FileSystem.GetAppPath();

            _device_manager = device_manager;
            _device_manager.SafeToMove = CheckSafeToMove;

            if( _device_manager != null) {
                try {
                    _device_manager.LoadDeviceFile();

                    // connect to each device
                    foreach( DeviceInterface di in _device_manager.GetAllDevices()) {
                        try {
                            di.Connect();
                            var accessible_device = di as AccessibleDeviceInterface;
                            /*
                            if( accessible_device != null)
                                _device_manager.AddPlateLocations( accessible_device);
                            */
                        } catch( Exception ex) {
                            _log.WarnFormat( "Could not connect to device '{0}': {1}", di.Name, ex.Message);
                        }
                    }

                    // register safety handlers
                    foreach( SafetyInterface si in _device_manager.GetSafetyInterfaces()) {
                        si.SafetyEventTriggered += new EventHandler(si_SafetyEventTriggered);
                        // #276 TEMPORARY FIX, but might be approach to adopt -- register event
                        // handlers in the plugins to allow them to respond to system events, rather
                        // than trapping them in the model and then calling methods in the interface
                        foreach( RobotInterface ri in _device_manager.GetRobotInterfaces())
                            si.SafetyEventTriggered += new EventHandler( ri.SafetyEventTriggeredHandler);
                        si.SafetyEventReset += new EventHandler(si_SafetyEventReset);
                        si.SafetyOverrideEvent += new EventHandler<SafetyEventArgs>(si_SafetyOverrideEvent);
                        si.StartMonitoring( true);
                    }

                } catch( Exception ex) {
                    _log.Fatal( "Could not load device file: " + ex.Message);
                }
            }

            Messenger.Default.Register< ResetInterlocksMessage>( this, ResetInterlocks);
            Messenger.Default.Register< SoftwareInterlockCommand>( this, TriggerSoftwareInterlock);
            Messenger.Default.Register< SMAbortCommand>( this, SMAbortCalled);
            Messenger.Default.Register<UnhandledErrorCountMessage>( this, UnhandledErrorCountMessageHandler);
        }

        void si_SafetyOverrideEvent(object sender, SafetyEventArgs e)
        {
            if( ModelSafetyOverriddenEvent != null)
                ModelSafetyOverriddenEvent( this, e);
        }

        void si_SafetyEventReset(object sender, EventArgs e)
        {
            if( ModelSafetyEventReset != null)
                ModelSafetyEventReset( this, e);
        }

        void si_SafetyEventTriggered(object sender, EventArgs e)
        {
            if( ModelSafetyEventTriggered != null)
                ModelSafetyEventTriggered( this, e);
        }

        private void ResetInterlocks( ResetInterlocksMessage message)
        {
            foreach( SafetyInterface si in _device_manager.GetSafetyInterfaces())
                si.ResetSafety();
            // need to reset the bit on the IO module or we'll always be triggered
            IOInterface io = _device_manager.GetIOInterfaces().FirstOrDefault();
            io.SetOutputState( Preferences.RobotDisableResetBit, false);
        }

        private void TriggerSoftwareInterlock( SoftwareInterlockCommand message)
        {
            IOInterface io = _device_manager.GetIOInterfaces().FirstOrDefault();
            io.SetOutputState( Preferences.RobotDisableResetBit, true);
        }

        /// <summary>
        /// Called by Abort button click in diagnostics error panel, or when starting a new protocol
        /// and the user wants to clear any existing errors.
        /// </summary>
        /// <param name="message"></param>
        private void SMAbortCalled( SMAbortCommand message)
        {
            Abort();
        }

        private bool CheckSafeToMove( DeviceInterface requester)
        {
            //\!todo TODO -- This should be handled by an interface function rather than checking product name

            // BPS140 can't be unlocked 
            if( requester.ProductName.ToUpper() == "BPS140") {
                StringBuilder sb = new StringBuilder();
                if( AbortPauseResumeSM.Running || AbortPauseResumeSM.Paused)
                    sb.AppendLine( String.Format( "It is not safe to unlock '{0}' at this time because a protocol is running", requester.Name));
                if( CustomerGUI != null && CustomerGUI.Busy) {
                    sb.AppendLine( String.Format( "It is not safe to unlock '{0}' at this time: {1}", requester.Name, CustomerGUI.BusyReason));
                }

                if( sb.Length == 0) {
                    _log.InfoFormat( "User manually unlocked '{0}'", requester.Name);
                    return true;
                } else {
                    _log.Info( sb.ToString());
                    return false;
                }
            }
            // Hive shouldn't be allowed to move if the BPS140 is unlocked
            // Note that there is a problem here if we add another Hive to a Bumblebee -- a different Hive could
            // be completely safe for moving if it's on the opposite side, so we'll have to deal with this
            // problem later
            if( requester.ProductName.ToLower() == "hive") {
                // check to see if any BPS140 devices are unlocked or if the interlock was enabled
                var devices = from d in _device_manager.GetSystemStartupCheckInterfaces()
                              //where (d as DeviceInterface).GetProductName() == "BPS140"
                              select d;
                StringBuilder sb = new StringBuilder();
                foreach( var device in devices) {
                    string reason;
                    if( !device.IsReady( out reason)) {
                        sb.AppendLine( reason);
                    }
                }
                if( sb.Length != 0) {
                    string message = String.Format( "Cannot move {0} because some devices are in an unsafe position: {1}", requester.Name, sb.ToString());
                    // cannot log in here because it gets called by the DC2 diags a LOT
                    //_log.Info( message);
                    return false;
                }
            }
            return true;
        }

        public ObservableCollection<UserControl> GetSystemCheckPanels()
        {
            IEnumerable<SystemStartupCheckInterface> devices = _device_manager.GetSystemStartupCheckInterfaces();
            ObservableCollection<UserControl> panels = new ObservableCollection<UserControl>();
            foreach( var d in devices) {
                UserControl panel = d.GetSystemPanel();
                if( panel != null)
                    panels.Add( panel);
            }

            if (CustomerGUI as IHasSystemPanel != null) {
                UserControl panel = ((IHasSystemPanel)CustomerGUI).GetSystemPanel();
                if( panel != null)
                    panels.Add( panel);
            }
            return panels;
        }

        /// <summary>
        /// Returns all of the plugins that were successfully loaded and present in the device manager
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DeviceInterface> GetAllDevices()
        {
            return _device_manager.GetAllDevices();
        }

        public DeviceInterface GetDevice( string device_name)
        {
            return _device_manager.DevicePluginsAvailable[device_name];
        }

        internal void Reset()
        {
            Aborting = false;
            Messenger.Default.Send<ResetCommand>( new ResetCommand());
            foreach( DeviceInterface di in _device_manager.GetDeviceInterfaces())
                di.Reset();
        }

        public void Abort()
        {
            // DKM 2011-03-23 this message needs to get broadcasted immediately, or
            // the plate transfer service can start the next robot move.
            Messenger.Default.Send<AbortCommand>( new AbortCommand());

            // turn off the paused and running lights
            // after the new interface, this is probably redundant because all plugins should
            // be derived from DeviceInterface, which has an Abort() method and the Christmas tree
            // lights will behave appropriately when this method is called...
            var status_devices = _device_manager.GetSystemStatusInterfaces();
            foreach( SystemStatusInterface si in status_devices)
                si.Running( false);
            
            IEnumerable<DeviceInterface> devices = GetAllDevices();
            foreach( DeviceInterface di in devices)
                di.Abort();
            Aborting = true;

            var pause_listener = CustomerGUI as ICustomerGUIPauseListener;
            if (pause_listener != null)
                pause_listener.Abort();
        }

        public void Pause()
        {
            var status_devices = _device_manager.GetSystemStatusInterfaces();
            foreach( SystemStatusInterface si in status_devices)
                si.Paused( true);
            
            IEnumerable<DeviceInterface> devices = GetAllDevices();
            foreach( DeviceInterface di in devices)
                di.Pause();

            var pause_listener = CustomerGUI as ICustomerGUIPauseListener;
            if (pause_listener != null)
                pause_listener.Pause();
        }

        public void Resume()
        {
            var status_devices = _device_manager.GetSystemStatusInterfaces();
            foreach( SystemStatusInterface si in status_devices) {
                si.Paused( false);
                si.Running( true);
            }
            
            IEnumerable<DeviceInterface> devices = GetAllDevices();
            foreach( DeviceInterface di in devices)
                di.Resume();

            var pause_listener = CustomerGUI as ICustomerGUIPauseListener;
            if (pause_listener != null)
                pause_listener.Resume();
        }

        internal void ProcessStarting()
        {
            foreach( ProtocolHooksInterface i in _device_manager.GetProtocolHooksInterfaces())
                i.ProtocolStarting();
        }

        internal void ProcessStarted()
        {
            foreach( ProtocolHooksInterface i in _device_manager.GetProtocolHooksInterfaces()){
                i.ProtocolStarted();
            }

            var status_devices = _device_manager.GetSystemStatusInterfaces();
            foreach( SystemStatusInterface si in status_devices)
                si.Running( true);
        }

        public bool SystemCheckOK( out string reason_not_ready)
        {
            IEnumerable<SystemStartupCheckInterface> startup_devices = _device_manager.GetSystemStartupCheckInterfaces();
            reason_not_ready = "";
            if( startup_devices.Count() == 0)
                return true;

            StringBuilder sb = new StringBuilder();
            bool ok = true;
            foreach( SystemStartupCheckInterface d in startup_devices) {
                string reason;
                if( !d.IsReady( out reason)) {
                    sb.AppendLine( reason);
                    ok = false;
                }
            }
            reason_not_ready = sb.ToString();
            return ok;
        }

        private void UnhandledErrorCountMessageHandler( UnhandledErrorCountMessage msg)
        {
            var status_devices = _device_manager.GetSystemStatusInterfaces();

            if( msg.NumberOfUnhandledErrors > 0) {
                foreach( SystemStatusInterface si in status_devices)
                    si.Error( true);
            } else {
                foreach( SystemStatusInterface si in status_devices)
                    si.Error( false);
            }
        }

        #region ICustomSynapsisQuery Members

        public bool AllDevicesHomed
        {
            get {
                foreach (DeviceInterface di in _device_manager.GetAllDevices()) {
                    // DKM 2012-01-16 used this temporarily to confirm that BB's IsHomed property is switching back and forth during homing
                    //_log.DebugFormat( "Device '{0}' is {1}", di.Name, di.IsHomed ? "homed" : "not homed");
                    if (!di.IsHomed && di.Connected) { 
                        return false;
                    }
                }
                return true;
            }
        }

        public bool ClearToHome( out string tooltip_text)
        {
            StringBuilder sb = new StringBuilder();
            string reason;
            if( !SystemCheckOK( out reason)) { sb.AppendLine( reason); }
            if( AbortPauseResumeSM.Running || AbortPauseResumeSM.Paused) { sb.AppendLine( "Protocol is running"); }
            if( CustomerGUI != null && CustomerGUI.Busy) { sb.AppendLine( CustomerGUI.BusyReason); }

            if( sb.Length != 0) {
                tooltip_text = sb.ToString();
                return false;
            }

            tooltip_text = "Home all devices";
            return true;
        }

        public bool Idle
        {
            get { return AbortPauseResumeSM.Idle; }
        }

        public bool Running
        {
            get { return AbortPauseResumeSM.Running; }
        }

        public bool Paused
        {
            get { return AbortPauseResumeSM.Paused; }
        }

        public bool HomeAllDevices( bool show_prompt=true)
        {
            IEnumerable<DeviceInterface> devices = _device_manager.GetAllDevices();

            StringBuilder message = new StringBuilder();
            message.AppendLine( "This will home the following devices:");
            foreach( DeviceInterface di in devices) {
                message.AppendLine( di.Name);
            }
            message.AppendLine( "\r\nAre you sure that all devices are safe to home?");
            
            // DKM 2012-05-16 added this to allow remote operation of Synapsis via RPC
            if( show_prompt) {
                MessageBoxResult answer = MessageBox.Show( message.ToString(), "Confirm homing", MessageBoxButton.YesNo);
                if( answer == MessageBoxResult.No) {
                    _log.Info( "user aborted homing of all devices");
                    return false;
                }
            }

            _log.Info( "user requested to home all devices");
            // loop over all devices and make sure they are connected
            foreach( DeviceInterface di in devices) {
                if( !di.Connected) {
                    // DKM 2012-05-16 for remote operation, all devices on the Synapsis end must connect properly or we will report a failure
                    if( show_prompt) {
                        return false;
                    } else {
                        MessageBoxResult result = MessageBox.Show( "Not all devices are connected.  Do you want to home the other devices anyway?  Your protocol may not execute properly until all devices are connected and homed.", "Not all devices connected", MessageBoxButton.YesNo);
                        if( result == MessageBoxResult.No)
                            return false;
                    }
                }
            }
            // loop over all of the devices and call Initialize on them
            try
            {
                foreach (DeviceInterface di in devices)
                {
                    if (di.Connected)
                        di.Home();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion
    }
}