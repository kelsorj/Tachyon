using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using BioNex.Shared.DeviceInterfaces;
using log4net;

namespace BioNex.KeyenceSZ01SPlugin
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(DeviceInterface))]
    [Export(typeof(SystemStartupCheckInterface))]
    [Export(typeof(SafetyInterface))]
    public class SZ01S : SystemStartupCheckInterface, DeviceInterface, SafetyInterface
    {
        private Dictionary<string,string> DeviceProperties { get; set; }
        private static readonly ILog Log = LogManager.GetLogger( typeof( SZ01S));
        private IOInterface IO { get; set; }
        private System.Windows.Window _diagnostics_panel { get; set; }

        [Import]
        public Lazy<ExternalDataRequesterInterface> DataRequestInterface { get; set; }

        // device manager configuration properties
        private const string Simulate = "simulate";
        private const string IODeviceName = "i/o device name";
        private const string Aux1 = "aux1 input bit(s)";
        private const string Aux2 = "aux2 input bit(s)";
        private const string ResetConfigString = "all SZ01S reset bit";
        private const string FrontOverride = "front override bit";
        private const string RearOverride = "rear override bit";
        private const string InterlockTrippedConfigString = "interlock tripped bit";

        // cached values from the device manager after connecting to I/O module
        private List<int> Aux1InputBits { get; set; }
        private List<int> Aux2InputBits { get; set; }
        private int ResetOutputBit { get; set; }
        private bool Simulating { get; set; }
        private int FrontOverrideBit { get; set; }
        private int RearOverrideBit { get; set; }
        private int InterlockTrippedBit { get; set; }

        private Thread InterlockMonitorThread { get; set; }
        private AutoResetEvent StopEvent { get; set; }
        public event EventHandler SafetyEventTriggered;
        public event EventHandler SafetyEventReset;
        public event EventHandler<SafetyEventArgs> SafetyOverrideEvent;
        private bool LastInterlockTrippedState { get; set; }
        private bool LastFrontOverriddenState { get; set; }
        private bool LastRearOverriddenState { get; set; }

        public SZ01S()
        {   
            StopEvent = new AutoResetEvent( false);
        }

        private void CheckInterlockState()
        {
            // do this to ensure that the safety override check gets handled once
            bool first_time_through = true;
            while( !StopEvent.WaitOne( 50)) {
                try {
                    if( IsInterlockTripped() && !LastInterlockTrippedState) {
                        LastInterlockTrippedState = true;
                        Log.Info( "Laser curtain tripped");
                        if( SafetyEventTriggered != null)
                            SafetyEventTriggered( this, new EventArgs());
                    } else if( !IsInterlockTripped() && LastInterlockTrippedState) {
                        Log.Info( "Laser curtain reset");
                        LastInterlockTrippedState = false;
                    }

                    // anytime a front or rear interlock override state changes, we
                    // will fire an event that contains the current state of each input
                    bool front_overridden = FrontInterlockOverridden;
                    bool rear_overridden = RearInterlockOverridden;
                    // create the screen message ahead of time
                    string message = "";
                    if( front_overridden && rear_overridden)
                        message = "FRONT AND REAR LASERS OVERRIDDEN";
                    else if( front_overridden)
                        message = "FRONT LASER OVERRIDDEN";
                    else if( rear_overridden)
                        message = "REAR LASER OVERRIDDEN";
                    // now we only want to fire the event if one of the override states have changed
                    // check for front state updates
                    if( (front_overridden && !LastFrontOverriddenState) || (!front_overridden && LastFrontOverriddenState) ||
                        (rear_overridden && !LastRearOverriddenState) || (!rear_overridden && LastRearOverriddenState) ||
                        first_time_through) {

                        // clear flag so we don't come in here again unless a transition has occurred
                        first_time_through = false;
                        // set the last state values so we can find a transition properly the next time through
                        if( front_overridden && !LastFrontOverriddenState) {
                            Log.Info( "Front laser overriden");
                            LastFrontOverriddenState = true;
                        } else if( !front_overridden && LastFrontOverriddenState) {
                            Log.Info( "Front laser enabled");
                            LastFrontOverriddenState = false;
                        }
                        
                        if( rear_overridden && !LastRearOverriddenState) {
                            Log.Info( "Rear laser overriden");
                            LastRearOverriddenState = true;
                        } else if( !rear_overridden && LastRearOverriddenState) {
                            Log.Info( "Rear laser enabled");
                            LastRearOverriddenState = false;
                        }
    
                        SafetyOverrideEvent( this, new SafetyEventArgs( front_overridden || rear_overridden, message));
                    }
                } catch( Exception) {

                }
            }
        }
        
        #region DeviceInterface Members

        public string Manufacturer
        {
            get
            {
                return "Keyence";
            }
        }

        public string ProductName
        {
            get
            {
                return "SZ-01S";
            }
        }

        public string Name { get; private set; }

        public string Description
        {
            get
            {
                return "SZ-01S safety laser scanner";
            }
        }

        public UserControl GetDiagnosticsPanel()
        {
            DiagnosticsPanel panel = new DiagnosticsPanel( this);
            return panel;
        }

        public void SetProperties(DeviceManagerDatabase.DeviceInfo device_info)
        {
            Name = device_info.InstanceName;
            DeviceProperties = new Dictionary<string,string>( device_info.Properties);
        }

        public void ShowDiagnostics()
        {
            if( _diagnostics_panel == null) {
                _diagnostics_panel = new System.Windows.Window();
                _diagnostics_panel.Width = 200;
                _diagnostics_panel.Height = 350;
                _diagnostics_panel.Content = new DiagnosticsPanel( this);
                _diagnostics_panel.Closed += new EventHandler(_diagnostics_panel_Closed);
                _diagnostics_panel.Title =  Name + "- Diagnostics";
            }

            _diagnostics_panel.Show();
            _diagnostics_panel.Activate();
        }

        void _diagnostics_panel_Closed(object sender, EventArgs e)
        {
            _diagnostics_panel = null;
        }

        public void Connect()
        {
            IEnumerable< IOInterface> io_interfaces = DataRequestInterface.Value.GetIOInterfaces();
            IO = ( from i in io_interfaces where ( i as DeviceInterface).Name == DeviceProperties[ IODeviceName] select i).FirstOrDefault();
            if( IO == null){
                Log.Info( String.Format( "Could not find IO provider '{0}'.", DeviceProperties[ IODeviceName]));
                return;
            }
            // get the input and output bits
            Aux1InputBits = (from b in DeviceProperties[Aux1].Split(',') select int.Parse(b)).ToList();
            Aux2InputBits = (from b in DeviceProperties[Aux2].Split(',') select int.Parse(b)).ToList();
            ResetOutputBit = int.Parse( DeviceProperties[ResetConfigString]);
            FrontOverrideBit = int.Parse( DeviceProperties[FrontOverride]);
            RearOverrideBit = int.Parse( DeviceProperties[RearOverride]);
            InterlockTrippedBit = int.Parse( DeviceProperties[InterlockTrippedConfigString]);
            Simulating = DeviceProperties[Simulate] != "0";
            Connected = true;
        }

        public bool Connected { get; private set; }

        public void Home() {}
        public bool IsHomed { get { return true; } }

        public void Close()
        {
            StartMonitoring( false);
            Connected = false;
        }

        public bool ExecuteCommand(string command, IDictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetCommands() { return null; }
        public void Abort() {}
        public void Pause() {}
        public void Resume() {}
        public void Reset() {}

        #endregion

        public bool WarningCondition
        {
            get {
                try {
                    if( Simulating)
                        return true;

                    int num_modules = Aux1InputBits.Count();
                    for( int i=0; i<num_modules; i++) {
                        // get Aux1 and Aux2
                        bool aux2 = IO.GetInput( Aux2InputBits[i]);
                        if( !aux2)
                            return true;
                    }
                    return false;
                } catch( Exception) {
                    return false;
                }
            }
        }

        public bool InterlockResetReadyCondition
        {
            get {
                try {
                    if( Simulating)
                        return false;

                    int num_modules = Aux1InputBits.Count();
                    // 2012-04-18 refs #567 reset ready state is incorrect due to improper checking of multiple SZ-01S modules
                    bool reset_ready = true;
                    for( int i=0; i<num_modules; i++) {
                        // get Aux1 and Aux2
                        reset_ready &= IO.GetInput( Aux1InputBits[i]);
                    }
                    return reset_ready;
                } catch( Exception) {
                    return false;
                }
            }
        }

        public bool InterlockCondition
        {
            get {
                try {
                    if( Simulating)
                        return false;

                    return IsInterlockTripped();
                } catch( Exception) {
                    return true;
                }
            }
        }

        private bool IsInterlockTripped()
        {
            return !IO.GetInput( InterlockTrippedBit);
        }

        public bool InterlocksOverridden
        {
            get { return FrontInterlockOverridden || RearInterlockOverridden; }
        }

        public bool FrontInterlockOverridden
        {
            get { return IO.GetInput( FrontOverrideBit); }
        }

        public bool RearInterlockOverridden
        {
            get { return IO.GetInput( RearOverrideBit); }
        }

        public void ResetSafety()
        {
            try {
                if( Simulating)
                    return;
                IO.SetOutputState( ResetOutputBit, true);
                Thread.Sleep( 100);
                IO.SetOutputState( ResetOutputBit, false);
                if( SafetyEventReset != null)
                    SafetyEventReset( this, new EventArgs());
            } catch( Exception) {

            }
        }

        public void StartMonitoring(bool start)
        {
            if (start) {
                // start the interlock monitoring thread
                InterlockMonitorThread = new Thread( CheckInterlockState);
                //InterlockMonitorThread.IsBackground = true; // Should NOT be background since we're joining this thread --> joining a background thread is a race condition
                InterlockMonitorThread.Start();
            } else {
                StopEvent.Set();
                // check for null since we could have loaded the plugin, but not actually started the monitoring
                if( InterlockMonitorThread != null)
                    InterlockMonitorThread.Join();
            }
        }

        public override bool IsReady(out string reason_not_ready)
        {
            try {
                if( Simulating) {
                    reason_not_ready = "";
                    return true;
                }

                StringBuilder reason = new StringBuilder();
                // look at the Aux1 and Aux2 input values for all SZ01S modules
                int num_modules = Aux1InputBits.Count();
                for( int i=0; i<num_modules; i++) {
                    // get aux1 and override
                    bool aux1 = IO.GetInput( Aux1InputBits[i]);
                    // we assume that the first index is front, and second is rear, which isn't a great assumption
                    bool overridden = false;
                    if( i == 0)
                        overridden = FrontInterlockOverridden;
                    else
                        overridden = RearInterlockOverridden;
                    
                    if( aux1 && !overridden)
                        reason.AppendLine( String.Format( "Laser curtain #{0} is currently tripped", i + 1));
                }

                // this is a temporary location -- the interlock is connected to the IO module, not to the SZ01S,
                // but for now, we'll check it in the SZ01S plugin
                if( InterlockCondition)
                    reason.AppendLine( "Interlock was activated -- please reset before homing");

                if( reason.Length != 0) {
                    reason_not_ready = reason.ToString();
                    return false;
                }
                reason_not_ready = "";
                return true;
            } catch( Exception) {
                reason_not_ready = "SZ-01S communication error";
                return false;
            }
        }

        public override UserControl GetSystemPanel()
        {
            DiagnosticsPanel panel = new DiagnosticsPanel( this);
            return panel;
        }
    }
}
