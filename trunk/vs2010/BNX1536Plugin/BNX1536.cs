using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.DeviceInterfaces;
using System.ComponentModel.Composition;
using System.Windows.Threading;
using BioNex.Shared.IError;
using BioNex.Shared.Location;
using BioNex.Shared.PlateWork;
using BioNex.Shared.TaskListXMLParser;
using System.Diagnostics;
using System.Threading;
//using log4net;

namespace BioNex.BNX1536Plugin
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(DeviceInterface))]
    public class BNX1536Device : AccessibleDeviceInterface, PlateSchedulerDeviceInterface
    {
        private ViewModel _vm;
        private IController _controller;
        private int Port { get; set; }
        private bool Simulating { get; set; }
        private Dictionary<string,string> DeviceProperties { get; set; }

        private static readonly string Simulate = "simulate";
        private static readonly string PortName = "port";

        private PlateLocation _location = new PlateLocation( "Stage");

        //[Import]
        //public ILog Log = LogManager.GetLogger( typeof(BNX1536Device));

        public class Commands
        {
            public const string RunProgram = "RunProgram";
            public const string RunServiceProgram = "RunServiceProgram";
        }

        public class CommandParameters
        {
            public const string ProgramNumber = "program_number";
        }
                
        #region Model responsibilities
        // Model responsibilities
        public void Connect( string port_name)
        {
            _controller.Connect( port_name);
        }

        public string StartProgram( int program_number)
        {
            try {
                return _controller.StartProgram( program_number);
            } catch( Exception) {
            }
            return "";
        }

        public string StartServiceProgram( int program_number)
        {
            return _controller.StartServiceProgram( program_number);
        }

        public string QueryStatus()
        {
            return _controller.QueryStatus();
        }

        #endregion

        public BNX1536Device()
        {
            // creating the model here is temporary -- we need a configuration / device manager of some kind
            _controller = new Controller();
            _vm = new ViewModel( this);
        }

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
                return "BNX1536";
            }
        }

        public string Name {get; private set;}

        /// <summary>
        /// This is required if we want the accessible device to appear correctly (by device instance name) in the robot teaching tab
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }

        public string Description
        {
            get
            {
                return "BNX1536 dispenser";
            }
        }

        public System.Windows.Controls.UserControl GetDiagnosticsPanel()
        {
            return new DiagnosticsPanel( _vm);
        }

        public void ShowDiagnostics()
        {
            System.Windows.Window window = new System.Windows.Window();
            window.Content = new DiagnosticsPanel( _vm);
            window.Title =  Name + "- Diagnostics" + (Simulating ? " (Simulating)" : "");
            window.Show();

        }

        public void SetErrorInterface( IError error_interface)
        {
        }

        public void SetProperties(DeviceManagerDatabase.DeviceInfo device_info)
        {
            Name = device_info.InstanceName;
            DeviceProperties = new Dictionary<string,string>( device_info.Properties);
        }

        public void Connect()
        {
            bool simulate = DeviceProperties[Simulate] != "0";
            string port = DeviceProperties[PortName];

            if (simulate) {
                _controller = new SimulationController();
            } else {
                _controller = new Controller();
            }

            _controller.Connect( port);
        }

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
            
        }

        public bool ExecuteCommand(string command, IDictionary<string, object> parameters)
        {
            try {
                switch (command)
                {
                    case Commands.RunProgram:
                        {
                            PlateTask.Parameter param = (PlateTask.Parameter)parameters[CommandParameters.ProgramNumber];
                            int program_number = int.Parse(param.Value.ToString());
                            _controller.StartProgram(program_number);
                            break;
                        }
                    case Commands.RunServiceProgram:
                        { 
                            PlateTask.Parameter param = (PlateTask.Parameter)parameters[CommandParameters.ProgramNumber];
                            int program_number = int.Parse(param.Value.ToString());
                            _controller.StartServiceProgram(program_number);
                            break;
                        }
                }
            } catch( Exception) {
                //Log.Debug( String.Format( "Could not execute command '{0}': {1}", command, ex.Message));
                return false;
            }

            return true;
        }

        public IEnumerable<string> GetCommands()
        {
            return new List<string> { Commands.RunProgram, Commands.RunServiceProgram };
        }

        public void Abort()
        {
            
        }

        public IEnumerable<PlateLocation> PlateLocationInfo
        {
            get
            {
                return new List<PlateLocation> { _location };
            }
        }

        public PlateLocation GetLidLocationInfo( string location_name)
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

        public bool Connected
        {
            get { return _controller.Connected; }
        }

        public void Pause()
        {
            return;
        }

        public void Resume()
        {
            return;
        }

        public int GetBarcodeReaderConfigurationIndex(string location_name)
        {
            return 0;
        }
        #endregion



        public void Reset()
        {
            throw new NotImplementedException();
        }

        #region PlateSchedulerDeviceInterface Members

        public event JobCompleteEventHandler JobComplete;

        public PlateLocation GetAvailableLocation(BioNex.Shared.PlateWork.ActivePlate active_plate)
        {
            return _location.Available ? _location : null;
        }

        public bool ReserveLocation(PlateLocation location, BioNex.Shared.PlateWork.ActivePlate active_plate)
        {
            // not my location to reserve.
            if( location != _location){
                return false;
            }
            // reserve location.
            location.Reserved.Set();
            return true;
        }

        public void LockPlace(PlatePlace place)
        {
            
        }

        public void AddJob(BioNex.Shared.PlateWork.ActivePlate active_plate)
        {
            new Thread( () => JobThread( active_plate)){ Name = GetType().ToString() + " Job Thread", IsBackground = true}.Start();
        }

        public void EnqueueWorklist(BioNex.Shared.PlateWork.Worklist worklist)
        {
            throw new NotImplementedException();
        }

        protected void JobThread(ActivePlate active_plate)
        {
            active_plate.WaitForPlate();
            try
            {
                IDictionary<string, object> parameters = new Dictionary<string, object>();
                PlateTask current_task = active_plate.GetCurrentToDo();
                parameters[CommandParameters.ProgramNumber] = current_task.ParametersAndVariables.First( x => x.Name == CommandParameters.ProgramNumber);
                Debug.Assert( parameters[CommandParameters.ProgramNumber] != null, "Missing program number from hitpick XML file for BNX1536");
                DeviceInterface d = (this as DeviceInterface);
                d.ExecuteCommand(active_plate.GetCurrentToDo().Command, parameters);
            }
            catch (Exception)
            {
                // Log.Debug( "Exception occurred");
            }
            active_plate.MarkJobCompleted();
        }

        #endregion
    }
}
