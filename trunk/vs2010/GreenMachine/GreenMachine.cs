using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.DeviceInterfaces;
using System.Windows.Controls;
using System.ComponentModel.Composition;
using System.ComponentModel;
using System.Windows.Media;
using log4net;
using BioNex.GreenMachine.HardwareInterfaces;
using BioNex.Shared.IError;
using BioNex.GreenMachine.StateMachines;

namespace BioNex.GreenMachine
{
    [Export(typeof(DeviceInterface))]
    public class GreenMachine : SystemStartupCheckInterface, AccessibleDeviceInterface, INotifyPropertyChanged
    {
        private UserControl _system_panel;
        private System.Windows.Window _diagnostics_window;
        private ILog _log = LogManager.GetLogger( typeof( GreenMachine));
        internal IGreenMachineController _controller;
        private IError _error_interface;

        // properties from device manager database
        private bool _simulating;
        private string _device_instance_name;
        private int _xyz_port;
        private string _config_folder;
        private string _teachpoint_filename;
        private int[] _pump_ports = new int[DevicePropertyNames.PumpPorts.Count()];

        private class DevicePropertyNames
        {
            public static readonly string Simulating = "simulating";
            public static readonly string XYZStagePort = "XYZ stage port";
            // the number of pumps in the system are defined by adding to the following array
            public static readonly string[] PumpPorts = new string[] { "Pump 1 port", "Pump 2 port", "Pump 3 port" };
            public static readonly string ConfigFolder = "configuration folder";
            public static readonly string TeachpointFilename = "teachpoint filename";
        }

        // status indicator colors
        private Brush _robot_status_color;
        public Brush RobotStatusColor
        {
            get { return _robot_status_color; }
            set {
                _robot_status_color = value;
                OnPropertyChanged( "RobotStatusColor");
            }
        }

        private Brush _pump1_status_color;
        public Brush Pump1StatusColor
        {
            get { return _pump1_status_color; }
            set {
                _pump1_status_color = value;
                OnPropertyChanged( "Pump1StatusColor");
            }
        }

        private Brush _pump2_status_color;
        public Brush Pump2StatusColor
        {
            get { return _pump2_status_color; }
            set {
                _pump2_status_color = value;
                OnPropertyChanged( "Pump2StatusColor");
            }
        }

        private Brush _pump3_status_color;
        public Brush Pump3StatusColor
        {
            get { return _pump3_status_color; }
            set {
                _pump3_status_color = value;
                OnPropertyChanged( "Pump3StatusColor");
            }
        }

        [ImportingConstructor]
        public GreenMachine( [Import] IError error_interface)
        {
            _system_panel = new GreenMachineSystemPanel();
            _system_panel.DataContext = this;
            _error_interface = error_interface;
            RobotStatusColor = Brushes.DarkGray;
        }

        #region DeviceInterface Members

        public void Connect()
        {
            if (_simulating)
                _controller = new GreenMachineSimulationController( _device_instance_name, _xyz_port, _pump_ports, _error_interface);
            else {
                _controller = new GreenMachineController( _device_instance_name, _xyz_port, _pump_ports, _error_interface);
            }
            _controller.Connect();
        }

        public bool Connected
        {
            get { return _controller.Connected; }
        }

        public void Home()
        {
            HomeStateMachine sm = new HomeStateMachine( _controller, _error_interface);
            sm.Start();
            IsHomed = true;
        }

        public bool IsHomed { get; private set; }

        public void Close()
        {
            _controller.Close();
        }

        public bool ExecuteCommand(string command, Dictionary<string, object> parameters)
        {
            BioNex.AmgenProtocolXmlParser.AmgenParser.ProtocolCommands command_id =
                (BioNex.AmgenProtocolXmlParser.AmgenParser.ProtocolCommands)Enum.Parse( typeof(BioNex.AmgenProtocolXmlParser.AmgenParser.ProtocolCommands), command, true);
            switch( command_id) {
                case BioNex.AmgenProtocolXmlParser.AmgenParser.ProtocolCommands.Aspirate:
                    ExecuteAspirate( parameters);
                    break;
                case BioNex.AmgenProtocolXmlParser.AmgenParser.ProtocolCommands.Dispense:
                    ExecuteDispense( parameters);
                    break;
                case BioNex.AmgenProtocolXmlParser.AmgenParser.ProtocolCommands.LoadSyringe:
                    ExecuteLoadSyringe( parameters);
                    break;
                case BioNex.AmgenProtocolXmlParser.AmgenParser.ProtocolCommands.Mix:
                    ExecuteMix( parameters);
                    break;
                case BioNex.AmgenProtocolXmlParser.AmgenParser.ProtocolCommands.Prime:
                    ExecutePrime( parameters);
                    break;
                case BioNex.AmgenProtocolXmlParser.AmgenParser.ProtocolCommands.Transfer:
                    ExecuteTransfer( parameters);
                    break;
            }

            return true;
        }

        /// <summary>
        /// Aspirates liquid into syringes
        /// </summary>
        /// <remarks>
        /// Aspirate takes a column, speed, volume, airgap, and height
        /// </remarks>
        /// <param name="parameters"></param>
        private void ExecuteAspirate( Dictionary<string, object> parameters)
        {
            AspirateStateMachine sm = new AspirateStateMachine( _controller, _error_interface);
            sm.Start();
        }

        /// <summary>
        /// Dispenses liquid from syringes
        /// </summary>
        /// <remarks>
        /// Dispense takes a column, inner speed, inner volume, outer speed, outer volume, and height
        /// </remarks>
        /// <param name="parameters"></param>
        private void ExecuteDispense( Dictionary<string, object> parameters)
        {
            DispenseStateMachine sm = new DispenseStateMachine( _controller, _error_interface);
            sm.Start();
        }

        /// <summary>
        /// Pulls liquid into the syringes
        /// </summary>
        /// <param name="parameters"></param>
        private void ExecuteLoadSyringe( Dictionary<string, object> parameters)
        {
            LoadSyringeStateMachine sm = new LoadSyringeStateMachine( _controller, _error_interface);
            sm.Start();
        }

        private void ExecuteMix( Dictionary<string, object> parameters)
        {
            MixStateMachine sm = new MixStateMachine( _controller, _error_interface);
            sm.Start();
        }

        private void ExecutePrime( Dictionary<string, object> parameters)
        {
            PrimeStateMachine sm = new PrimeStateMachine( _controller, _error_interface);
            sm.Start();
        }

        private void ExecuteTransfer( Dictionary<string, object> parameters)
        {
            TransferStateMachine sm = new TransferStateMachine( _controller, _error_interface);
            sm.Start();
        }

        public IEnumerable<string> GetCommands()
        {
            return (from x in Enum.GetNames( typeof(BioNex.AmgenProtocolXmlParser.AmgenParser.ProtocolCommands)) select x.ToString());
        }

        public void Abort()
        {
            _controller.Abort();
        }

        public void Pause()
        {
            _controller.Pause();
        }

        public void Resume()
        {
            _controller.Resume();
        }

        public void Reset()
        {
            
        }

        #endregion

        #region IPluginIdentity Members

        public string Name
        {
            get { return _device_instance_name; }
        }

        public string ProductName
        {
            get { return "Green Machine"; }
        }

        public string Manufacturer
        {
            get { return "Amgen"; }
        }

        public string Description
        {
            get { return "Gradient diluter"; }
        }

        public void SetProperties(DeviceManagerDatabase.DeviceInfo device_info)
        {
            _device_instance_name = device_info.InstanceName;
            // simulation
            string value;
            if( device_info.Properties.TryGetValue( DevicePropertyNames.Simulating, out value))
                _simulating = value != "0";
            _log.Info( String.Format( "'{0}' is {1}", _device_instance_name, _simulating ? "simulating": "not simulating"));
            // connections
            if( !device_info.Properties.TryGetValue( DevicePropertyNames.XYZStagePort, out value))
                _log.Info( String.Format( "Property '{0}' for device '{1}' was not present in the device manager database", DevicePropertyNames.XYZStagePort, _device_instance_name));
            else
                _xyz_port = int.Parse( value);
            int num_pumps = DevicePropertyNames.PumpPorts.Count();
            for( int i=0; i<num_pumps; i++) {
                if( !device_info.Properties.TryGetValue( DevicePropertyNames.PumpPorts[i], out value))
                    _log.Info( String.Format( "Property '{0}' for device '{1}' was not present in the device manager database", DevicePropertyNames.PumpPorts[i], _device_instance_name));
                else
                    _pump_ports[i] = int.Parse( value);
            }
            // config folder
            _config_folder = device_info.Properties[DevicePropertyNames.ConfigFolder];
            // teachpoint file
            //_teachpoint_path = _config_folder + "\\" + device_info.Properties[DevicePropertyNames.TeachpointFilename];
        }

        #endregion

        #region IHasDiagnosticPanel Members

        public System.Windows.Controls.UserControl GetDiagnosticsPanel()
        {
            return CreateDiagnostics();
        }

        private GreenMachineDiagnostics CreateDiagnostics()
        {
            GreenMachineDiagnostics diags = new GreenMachineDiagnostics();
            diags.DataContext = this;

            TabControl tabcontrol = new TabControl();
            // create all of the tab item content
            TabItem xyz_tabitem = new TabItem();
            xyz_tabitem.Header = "Syringe XYZ Stage";
            Grid grid = new Grid();
            xyz_tabitem.Content = grid;
            grid.Children.Add( new TTDiagnostics( this));
            tabcontrol.Items.Add( xyz_tabitem);

            int num_pumps = DevicePropertyNames.PumpPorts.Count();
            for( int i=0; i<num_pumps; i++) {
                TabItem pump_tabitem = new TabItem();
                pump_tabitem.Header = String.Format( "Pump {0}", i + 1);
                pump_tabitem.Content = new TecanPumpDiagnostics( i, _controller);
                tabcontrol.Items.Add( pump_tabitem);
            }
            diags.Content = tabcontrol;
            return diags;
        }

        public void ShowDiagnostics()
        {
            if( !_controller.Connected) {
                try {
                    Connect();
                } catch( Exception ex) {
                    System.Windows.MessageBox.Show( ex.Message, "Error when attempting to connect to the device");
                }
            }

            if( _diagnostics_window == null) {
                _diagnostics_window = new System.Windows.Window();
                _diagnostics_window.Content = CreateDiagnostics();
                _diagnostics_window.Title =  Name + " - Diagnostics" + (_simulating ? " (Simulating)" : "");
                _diagnostics_window.Closed += new EventHandler(_diagnostics_window_Closed);
                _diagnostics_window.Height = 400;
                _diagnostics_window.Width = 800;
            }
            _diagnostics_window.Show();
            _diagnostics_window.Activate();
            
        }

        private void _diagnostics_window_Closed(object sender, EventArgs e)
        {
            _diagnostics_window.Content = null;
            _diagnostics_window = null;
        }

        #endregion

        #region RobotAccessibleInterface Members

        public IEnumerable<PlateLocationInfo> PlateLocationInfo
        {
            get
            {
                return new List<PlateLocationInfo>();
            }
        }

        public PlateLocationInfo GetLidLocationInfo(string location_name)
        {
            return null;
        }

        public string TeachpointFilenamePrefix
        {
            get
            {
                return Name;
            }
        }

        public int GetBarcodeReaderConfigurationIndex(string location_name)
        {
            return 0;
        }

        #endregion

        #region IHasSystemPanel Members

        public override System.Windows.Controls.UserControl GetSystemPanel()
        {
            return _system_panel;
        }

        #endregion

        public override bool IsReady(out string reason_not_ready)
        {
            reason_not_ready = "";
            return true;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion
    }
}
