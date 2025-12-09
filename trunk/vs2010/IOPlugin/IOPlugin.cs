using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.Utils;
using log4net;
using Systec_IO;

namespace BioNex.IOPlugin
{
    [Export(typeof(DeviceInterface))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class IO : SystemStartupCheckInterface, DeviceInterface, IOInterface, INotifyPropertyChanged
    {
        private Dictionary<string, string> DeviceProperties { get; set; }
        // DeviceProperties keys -- these come from the database_properties table
        public static readonly string Simulate = "simulate";
        public static readonly string CANDeviceID = "CAN device ID";
        public static readonly string CANDeviceChannel = "CAN device channel";
        public static readonly string ConfigFolder = "IO config folder";
        public static readonly string CANOpenNodeID = "CANOpen Node ID";
        public static readonly string InputByteCount = "Input bytes";
        public static readonly string OutputByteCount = "Output bytes";

        private InternalIOInterface Controller { get; set; }
        private System.Windows.Window _diagnostics_window { get; set; }

        private static readonly ILog _log = LogManager.GetLogger(typeof(IO));
        internal IOConfiguration _config { get; set; }
        private bool _simulating;

        // stuff for databinding to alert panel
        private string _header_name;
        public string HeaderName
        {
            get { return _header_name; }
            set
            {
                _header_name = value;
                OnPropertyChanged("HeaderName");
            }
        }

        #region DeviceInterface Members

        public string Name { get; private set; }
        public string Manufacturer { get { return "BioNex"; } }
        public string ProductName { get { return BioNexDeviceNames.IODevice; } }
        public string Description { get { return "I/O Device"; } }

        public System.Windows.Controls.UserControl GetDiagnosticsPanel()
        {
            return new DiagnosticsPanel(this);
        }

        public void SetProperties(DeviceManagerDatabase.DeviceInfo device_info)
        {
            Name = device_info.InstanceName;
            DeviceProperties = new Dictionary<string, string>(device_info.Properties);
        }

        public void ShowDiagnostics()
        {
            if (_diagnostics_window == null)
            {
                _diagnostics_window = new System.Windows.Window();
                _diagnostics_window.Content = new BioNex.IOPlugin.DiagnosticsPanel(this);
                _diagnostics_window.Closed += new EventHandler(_diagnostics_window_Closed);
                _diagnostics_window.Title = Name + "- Diagnostics" + (_simulating ? " (Simulating)" : "");
            }
            _diagnostics_window.Show();
            _diagnostics_window.Activate();
        }

        void _diagnostics_window_Closed(object sender, EventArgs e)
        {
            _diagnostics_window = null;
        }

        public void Connect()
        {
            // load config file first since we need the I/O names
            try
            {
                /*
                // temporary code to see what serialized config looks like
                _config = new IOConfiguration();
                _config.HazardousBits.Add( new IOConfiguration.HazardousBit { BitName = "test bit", BitNumber = 1, HazardousLogicLevel = 1 } );
                FileSystem.SaveXmlConfiguration<IOConfiguration>( _config, DeviceProperties[ConfigFolder] + "\\sample_io_config.xml");
                    */

                _config = FileSystem.LoadXmlConfiguration<IOConfiguration>(DeviceProperties[ConfigFolder] + "\\config.xml");
            }
            catch (KeyNotFoundException ex)
            {
                _log.DebugFormat( "Could not load configuration for {0}: {1}.  I/O-based system safeties were not enabled.", Name, ex.Message);
                _config = new IOConfiguration();
            }


            _simulating = DeviceProperties[Simulate].ToString() != "0";
            if (_simulating)
            {
                Controller = new SimulationIO();
            }
            else
            {
                Controller = new IOX1();
            }
            Controller.IOXInputChanged += new IOXInputChangedEvent(Controller_IOXInputChanged);

            int device_id = int.Parse(DeviceProperties[CANDeviceID]);
            int device_channel = int.Parse(DeviceProperties[CANDeviceChannel]);
            int node_id = DeviceProperties.ContainsKey(CANOpenNodeID) ? int.Parse(DeviceProperties[CANOpenNodeID]) : 0x40;
            int input_bytes = DeviceProperties.ContainsKey(InputByteCount) ? int.Parse(DeviceProperties[InputByteCount]) : 2;
            int output_bytes = DeviceProperties.ContainsKey(OutputByteCount) ? int.Parse(DeviceProperties[OutputByteCount]) : 1;

            if (0 != Controller.Initialize(node_id, device_id, device_channel, input_bytes, output_bytes))
            {
                throw new DeviceInitializationException("Could not initialize " + Name);
            }

            // set all of the input and output names
            // not watching out for going out of bounds on the array right now since we only have one I/O device
            foreach (var bit in GetInputNames())
            {
                Controller.SetInputName(bit.BitNumber - 1, bit.BitName);
            }
            foreach (var bit in GetOutputNames())
            {
                Controller.SetOutputName(bit.BitNumber - 1, bit.BitName);
            }

            Connected = true;
        }

        void Controller_IOXInputChanged(object sender, IOXEventArgs e)
        {
            if (InputChanged != null)
            {
                // not sure if this is the right way to do this, but I think it will work out
                var args = e.BitIndexes.Zip(e.BitValues, (a, b) => new { Index = a, Value = b });
                foreach (var arg in args)
                {
                    InputChanged(this, new InputChangedEventArgs(arg.Index, arg.Value));
                }
            }
        }

        public bool Connected { get; private set; }

        public void Home()
        {
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
            if (Controller != null)
                Controller.Close();
            Connected = false;
        }

        public bool ExecuteCommand(string command, IDictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetCommands()
        {
            return null;
        }

        public void Abort() { }
        public void Pause() { }
        public void Resume() { }
        public void Reset() { }

        #endregion

        #region IOInterface Members

        public event InputChangedEventHandler InputChanged;

        public int NumberOfInputs
        {
            get { return Controller.NumberOfInputs; }
        }

        public int NumberOfOutputs
        {
            get { return Controller.NumberOfOutputs; }
        }

        public void SetOutputState(int bit_index_0based, bool state)
        {
            Controller.WriteOutput(bit_index_0based, state ? IOX1.bit_state.set : IOX1.bit_state.clear);
        }

        public void SetOutputs(byte[] bitmask)
        {
            Controller.SetOutputs(bitmask);
        }

        public void ClearOutputs(byte[] bitmask)
        {
            Controller.ClearOutputs(bitmask);
        }

        public bool GetInput(int bit_0based)
        {
            return Controller.ReadInput(bit_0based) == IOX1.bit_state.set;
        }

        public byte[] GetInputs()
        {
            return Controller.ReadInputs();
        }

        public bool GetOutput(int bit_0based)
        {
            return Controller.ReadOutput(bit_0based) == IOX1.bit_state.set;
        }

        public byte[] GetOutputs()
        {
            return Controller.GetOutputState();
        }

        public List<BitNameMapping> GetInputNames()
        {
            return _config.InputNames;
        }

        public List<BitNameMapping> GetOutputNames()
        {
            return _config.OutputNames;
        }

        #endregion

        #region SystemStartupCheckInterface Members

        public override bool IsReady(out string reason_not_ready)
        {
            StringBuilder sb = new StringBuilder();
            if (!Connected)
                sb.AppendLine(Name + " not connected");
            else
            { // don't want to talk to device if we're not connected
                // now look at the config file to see which bits indicate a hazardous condition
                foreach (var bit in _config.HazardousBits)
                {
                    bool input_state = GetInput(bit.BitNumber - 1);
                    bool hazard_is_logic_high = bit.HazardousLogicLevel != 0;
                    if (input_state && hazard_is_logic_high)
                        sb.AppendLine(String.Format("{0} input '{1}' indicates a potential hazardous condition: {2}", Name, bit.BitName, bit.NotificationMessage));
                }
            }

            reason_not_ready = sb.ToString();
            return reason_not_ready == "";
        }

        public override System.Windows.Controls.UserControl GetSystemPanel()
        {
            if (_config == null || _config.HazardousBits.Count() == 0)
                return null;

            AlertPanel panel = new AlertPanel(this);
            return panel;
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string property_name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property_name));
        }

        #endregion
    }
}
