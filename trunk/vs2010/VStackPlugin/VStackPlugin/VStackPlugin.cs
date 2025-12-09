using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.IWorksDriverExecutionStateMachine;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Location;
using BioNex.Shared.PlateWork;
using IWorksDriverLib;
using log4net;

namespace VStackPlugin
{
    public interface IWorksStacker : IWorksDriverLib.IWorksDriver, IWorksDriverLib.IStackerDriver
    {
    }

    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(DeviceInterface))]
    public class VStackPlugin : AccessibleDeviceInterface, StackerInterface, PlateSchedulerDeviceInterface
    {
        [Import]
        private IWorksDriverExecutor _executor { get; set; }
        [Import(typeof(ILabwareDatabase))]
        public ILabwareDatabase _labware_database;

        private bool _connected;
        private static readonly ILog _log = LogManager.GetLogger( typeof( VStackPlugin));
        private string _name = "unknown instance name";
        private string _profile;
        private bool _simulate = false;
        private readonly PlateLocation _location = new PlateLocation( "VStack stage");

        // private Diagnostics _diagnostics_window;

        private readonly fake_controller _controller = new fake_controller();

        public enum Commands { Upstack, Downstack, LoadStack, ReleaseStack};
        public class CommandParameterNames
        {
            public const string LabwareName = "labware_name";
            public const string PlateFlags = "plate_flags";
        }

        public class DevicePropertyNames
        {
            public const string Profile = "profile";
            public const string Simulate = "simulate";
        }
        
        void DeviceInterface.Connect()
        {
            if( _simulate){
                _connected = true;
                return;
            }

            _connected = false;
            var init_string = string.Format(
              @"<?xml version='1.0' ?>
                <Velocity11 file='MetaData' version='1.0' >
                    <Command Name='Initialize'>
                        <Parameters >
                            <Parameter Name='Profile' Value='{0}' />
                        </Parameters>
                    </Command>
                </Velocity11>",  _profile);

            try {
                /*
                _vstack = (IWorksDriverLib.IWorksDriver)new VSTACKBIONETLib.VStackBioNetPlugin();
                _stacker = (IWorksDriverLib.IStackerDriver)_vstack;
                var controller_client = (IWorksDriverLib.IControllerClient)_vstack;
                controller_client.SetController(_controller);
                 */
                _executor.CreateVStackDriver( _profile, _controller);
                _connected = _executor.Initialize( _profile, init_string);
            } catch(Exception e) {
                _log.Error("VStack exception during initialization", e);
                throw;
            }
             

            if (!_connected)
            {
                try {
                    var error = _executor.GetErrorInfo( _profile);
                    _log.ErrorFormat( "VStack error during initialization: {0}", error);
                } catch (Exception e) {
                    _log.Error("VStack exception when requesting error info", e);
                }
            }
        }

        bool DeviceInterface.Connected
        {
            get { return _connected; }
        }

        void DeviceInterface.Home()
        {
            // do nothing
        }

        bool DeviceInterface.IsHomed
        {
            get { return _connected; }
        }

        void DeviceInterface.Close()
        {
            _connected = false;

            if( _simulate){
                return;
            }

            _executor.Close( _profile);
        }

        bool DeviceInterface.ExecuteCommand(string command, IDictionary<string, object> parameters)
        {
            // TODO: put these calls into a state machine so we can support abort/retry/ignore built into V11 plugins
            if( _simulate){
                return true;
            }

            string cmd = command.ToLower();
            string labware_name = (string)parameters[CommandParameterNames.LabwareName];
            // DKM 2011-10-10 using the "plate_flags" parameter passed in didn't cast properly
            const PlateFlagsType plate_type = IWorksDriverLib.PlateFlagsType.STACK_NORMAL_PLATES;
            const bool ret = false;

            if( cmd == Commands.Upstack.ToString().ToLower()) {
                // DKM 2011-10-13 since LoadStack doesn't do anything if the stack is already loaded, and since IStackerDriver
                //                doesn't have a method to check if a stack is loaded, just call LoadStack anytime we upstack a plate
                // parse parameters
                _executor.LoadStack( _profile, labware_name, plate_type, _location.Name);
                _executor.SinkPlate( _profile, labware_name, plate_type, _location.Name);
            } else if( cmd == Commands.Downstack.ToString().ToLower()) {
                // DKM 2011-10-13 since LoadStack doesn't do anything if the stack is already loaded, and since IStackerDriver
                //                doesn't have a method to check if a stack is loaded, just call LoadStack anytime we downstack a plate
                // parse parameters
                _executor.LoadStack( _profile, labware_name, plate_type, _location.Name);
                _executor.SourcePlate( _profile, labware_name, plate_type, _location.Name);                
            } else if( cmd == Commands.LoadStack.ToString().ToLower()) {
                _executor.LoadStack( _profile, labware_name, plate_type, _location.Name);
            } else if( cmd == Commands.ReleaseStack.ToString().ToLower()) {
                _executor.UnloadStack( _profile, labware_name, plate_type, _location.Name);
            }

            return ret;
        }

        /// <summary>
        /// This is required if we want the accessible device to appear correctly (by device instance name) in the robot teaching tab
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _name;
        }

        IEnumerable<string> DeviceInterface.GetCommands()
        {
            return new List<string>();
        }

        void DeviceInterface.Abort()
        {

        }

        void DeviceInterface.Pause()
        {
        
        }

        void DeviceInterface.Resume()
        {
        
        }

        void DeviceInterface.Reset()
        {
            
        }

        string IPluginIdentity.Name
        {
            get { return _name; }
        }

        string IPluginIdentity.ProductName
        {
            get { return "VStack"; }
        }

        string IPluginIdentity.Manufacturer
        {
            get { return "Velocity11"; }
        }

        string IPluginIdentity.Description
        {
            get { return "Velocity11 VStack"; }
        }

        void IPluginIdentity.SetProperties(DeviceManagerDatabase.DeviceInfo device_info)
        {
            _name = device_info.InstanceName;
            if (device_info.Properties.ContainsKey( DevicePropertyNames.Profile))
                _profile = device_info.Properties[DevicePropertyNames.Profile];
            if( device_info.Properties.ContainsKey( DevicePropertyNames.Simulate))
                _simulate = device_info.Properties[ DevicePropertyNames.Simulate] != "0";
        }

        System.Windows.Controls.UserControl IHasDiagnosticPanel.GetDiagnosticsPanel()
        {
            throw new NotImplementedException();
        }

        void IHasDiagnosticPanel.ShowDiagnostics()
        {
            _executor.ShowDiagsDialog( _profile, IWorksDriverLib.SecurityLevel.SECURITY_LEVEL_ADMINISTRATOR);
        }

        void _diagnostics_window_Closed(object sender, EventArgs e)
        {
            // _diagnostics_window = null;
        }

        public IEnumerable<PlateLocation> PlateLocationInfo
        {
            get
            {
                // I didn't just call it "Stage" because we have too many devices with the same location name,
                // and I thought that someday debugging would be a lot nicer if we just added the product name.
                return new List<PlateLocation> { _location };
            }
        }

        public PlateLocation GetLidLocationInfo(string location_name)
        {
            return null;
        }

        public string TeachpointFilenamePrefix
        {
            get
            {
                return _name;
            }
        }

        public int GetBarcodeReaderConfigurationIndex( string location_name)
        {
            return 0;
        }

        #region StackerInterface Members

        public void Upstack(BioNex.Shared.PlateDefs.Plate plate)
        {
            _log.Warn( "Calling Upstack via StackerInterface -- no plate flags are being passed to the IWorks driver!");
            Dictionary<string,object> parameters = new Dictionary<string,object>();
            parameters[CommandParameterNames.LabwareName] = plate.LabwareName;
            parameters[CommandParameterNames.PlateFlags] = "0";
            ((DeviceInterface)this).ExecuteCommand( Commands.Upstack.ToString(), parameters);
        }

        public void Downstack(BioNex.Shared.PlateDefs.Plate plate)
        {
            _log.Warn( "Calling Downstack via StackerInterface -- no plate flags are being passed to the IWorks driver!");
            Dictionary<string,object> parameters = new Dictionary<string,object>();
            parameters[CommandParameterNames.LabwareName] = plate.LabwareName;
            parameters[CommandParameterNames.PlateFlags] = "0";
            ((DeviceInterface)this).ExecuteCommand( Commands.Downstack.ToString(), parameters);
        }

        public void LoadStack(BioNex.Shared.PlateDefs.Plate plate)
        {
            _log.Warn( "Calling LoadStack via StackerInterface -- no plate flags are being passed to the IWorks driver!");
            Dictionary<string,object> parameters = new Dictionary<string,object>();
            parameters[CommandParameterNames.LabwareName] = plate == null ? "TODO" : plate.LabwareName;
            parameters[CommandParameterNames.PlateFlags] = "0";
            ((DeviceInterface)this).ExecuteCommand( Commands.LoadStack.ToString(), parameters);
        }

        public void ReleaseStack(BioNex.Shared.PlateDefs.Plate plate)
        {
            _log.Warn( "Calling ReleaseStack via StackerInterface -- no plate flags are being passed to the IWorks driver!");
            Dictionary<string,object> parameters = new Dictionary<string,object>();
            parameters[CommandParameterNames.LabwareName] = plate == null ? "TODO" : plate.LabwareName;
            parameters[CommandParameterNames.PlateFlags] = "0";
            ((DeviceInterface)this).ExecuteCommand( Commands.ReleaseStack.ToString(), parameters);
        }

        #endregion

        #region PlateSchedulerDeviceInterface Members
        public event JobCompleteEventHandler JobComplete { add {} remove {} }
        public PlateLocation GetAvailableLocation( ActivePlate active_plate)
        {
            if( !_simulate){
                bool reinitialized_ok = _executor.Reinitialize( _profile);
                System.Diagnostics.Debug.Assert( reinitialized_ok, "Could not reinitialize VStack");
                bool stack_is_empty = _executor.IsStackEmpty( _profile, _location.Name);
                if( stack_is_empty ){
                    return null;
                }
            }
            return _location.Available ? _location : null;
        }

        public bool ReserveLocation( PlateLocation location, ActivePlate active_plate)
        {
            // not my location to reserve.
            if( location != _location){
                return false;
            }
            // reserve location.
            location.Reserved.Set();
            return true;
        }

        public void LockPlace( PlatePlace place)
        {
        }

        public void AddJob( ActivePlate active_plate)
        {
            new Thread( () => JobThread( active_plate)){ Name = GetType().ToString() + " Job Thread", IsBackground = true}.Start();
        }

        /// <summary>
        /// Look in the /config/methods folder to see what the pre/post tasks are
        /// </summary>
        /// <param name="active_plate"></param>
        public void JobThread( ActivePlate active_plate)
        {
            // DKM 2011-10-07 possibly replace the command name check with something like:
            // if( active_plate.GetCurrentToDo().Command.ToLower() == VStackPlugin.Commands.Downstack.ToString().ToLower()){
            if( active_plate.GetCurrentToDo().Command == "Downstack"){
                try{
                    IDictionary< string, object> parameters = new Dictionary< string, object>();
                    parameters[ CommandParameterNames.LabwareName] = active_plate.GetCurrentToDo().ParametersAndVariables.Where( x => x.Name == CommandParameterNames.LabwareName).First().Value;
                    parameters[ CommandParameterNames.PlateFlags] = ( IWorksDriverLib.PlateFlagsType)int.Parse( active_plate.GetCurrentToDo().ParametersAndVariables.Where( x => x.Name == CommandParameterNames.PlateFlags).First().Value);
                    DeviceInterface d = ( this as DeviceInterface);
                    d.ExecuteCommand( active_plate.GetCurrentToDo().Command, parameters);
                } catch( Exception){
                    // Log.Debug( "Exception occurred");
                }
                _location.Occupied.Set();
                active_plate.MarkJobCompleted();
            } else{
                throw new NotImplementedException();
            }
        }

        public void EnqueueWorklist( Worklist worklist)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
