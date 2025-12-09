using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Windows;
using BioNex.Shared.DeviceInterfaces;
using log4net;

namespace BioNex.ChristmasTreePlugin
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(DeviceInterface))]
    [Export(typeof(SystemStartupCheckInterface))]
    public class ChristmasTree : DeviceInterface, SystemStatusInterface
    {
        // device manager configuration properties
        //private static readonly string Simulate = "simulate"; -- unused
        private const string IODeviceName = "i/o device name";
        private const string GreenLightConfigString = "green light bit";
        private const string YellowLightConfigString = "yellow light bit";
        private const string RedLightConfigString = "red light bit";

        // cached values from the device manager after connecting to I/O module
        private int GreenLightBit { get; set; }
        private int YellowLightBit { get; set; }
        private int RedLightBit { get; set; }

        private Dictionary<string,string> DeviceProperties { get; set; }
        private static readonly ILog Log = LogManager.GetLogger( typeof( ChristmasTree));
        private IOInterface IO { get; set; }
        private readonly ManualResetEvent StopLightDanceEvent = new ManualResetEvent( false);

        [Import]
        public Lazy<ExternalDataRequesterInterface> DataRequestInterface { get; set; }

        #region DeviceInterface Members

        public string Manufacturer
        {
            get
            {
                return "BioNex";
            }
        }

        public string ProductName
        {
            get
            {
                return "Christmas tree";
            }
        }

        public string Name { get; private set; }

        public string Description
        {
            get
            {
                return "Christmas tree status lights";
            }
        }

        //! \todo do we need to have a HasDiagnosticsPanel() method so that the device isn't
        //!       even shown in the Synapsis menu?
        public System.Windows.Controls.UserControl GetDiagnosticsPanel()
        {
            MessageBox.Show( "The system status lights do not have a diagnostics panel available");
            return null;
        }

        public void SetProperties(DeviceManagerDatabase.DeviceInfo device_info)
        {
            Name = device_info.InstanceName;
            DeviceProperties = new Dictionary<string,string>( device_info.Properties);
        }

        public void ShowDiagnostics()
        {
            MessageBox.Show( "The system status lights do not have a diagnostics panel available");
        }

        public void Connect()
        {
            IEnumerable< IOInterface> io_interfaces = DataRequestInterface.Value.GetIOInterfaces();
            IO = ( from i in io_interfaces where ( i as DeviceInterface).Name == DeviceProperties[ IODeviceName] select i).FirstOrDefault();
            if( IO == null){
                Log.InfoFormat( "Could not find IO provider '{0}'.", DeviceProperties[ IODeviceName]);
                return;
            }
            // get the input and output bits
            GreenLightBit = int.Parse( DeviceProperties[GreenLightConfigString]);
            YellowLightBit = int.Parse( DeviceProperties[YellowLightConfigString]);
            RedLightBit = int.Parse( DeviceProperties[RedLightConfigString]);

            // clear the christmas tree lights
            IO.SetOutputState( GreenLightBit, false);
            IO.SetOutputState( YellowLightBit, false);
            IO.SetOutputState( RedLightBit, false);
            Connected = true;
        }

        public bool Connected { get; private set; }

        public void Home()
        {
            return;
        }

        public bool IsHomed
        {
            get
            {
                return true;
            }
        }

        public void Close()
        {
            return;
        }

        public bool ExecuteCommand(string command, IDictionary<string, object> parameters)
        {
            return true;
        }

        public IEnumerable<string> GetCommands()
        {
            return new List<string>();
        }

        public void Abort()
        {
            Paused( false);
            Running( false);
        }

        public void Pause()
        {
            Paused( true);
        }

        public void Resume()
        {
            Paused( false);
            Running( true);
        }

        public void Reset() {}

        #endregion

        #region SystemStatusInterface Members

        public void Running(bool is_running)
        {
            StopLightDanceEvent.Set();
            IO.SetOutputState( GreenLightBit, is_running);
        }

        public void Error(bool has_error)
        {
            IO.SetOutputState( RedLightBit, has_error);
        }

        public void Paused(bool is_paused)
        {
            IO.SetOutputState( YellowLightBit, is_paused);
        }

        public void ProtocolComplete(bool is_complete)
        {
            if( is_complete) {
                StopLightDanceEvent.Reset();
                Thread thread = new Thread( LightDanceThread);
                thread.Name = "Christmas tree light dance";
                thread.IsBackground = true;
                thread.Start();
            } else {
                StopLightDanceEvent.Set();
            }            
        }

        private void LightDanceThread()
        {
            const int delay_ms = 250;
            while( !StopLightDanceEvent.WaitOne( delay_ms)) {
                // seems wasteful, but I just wanted to ensure that the thread and event synching doesn't get messed up
                try {
                    IO.SetOutputState( RedLightBit, true);
                    Thread.Sleep( delay_ms);
                    IO.SetOutputState( YellowLightBit, true);
                    Thread.Sleep( delay_ms);
                    IO.SetOutputState( RedLightBit, false);
                    IO.SetOutputState( GreenLightBit, true);
                    Thread.Sleep( delay_ms);
                    IO.SetOutputState( YellowLightBit, false);
                    Thread.Sleep( delay_ms);
                    IO.SetOutputState( GreenLightBit, false);
                } catch( Exception) {
                    // do nothing... this isn't really that critical
                }
            }
            
            IO.SetOutputState( RedLightBit, false);
            IO.SetOutputState( YellowLightBit, false);
            IO.SetOutputState( GreenLightBit, false);
        }

        #endregion
    }
}
