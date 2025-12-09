using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.IWorksDriverExecutionStateMachine;
using BioNex.Shared.Location;
using BioNex.Shared.PlateWork;
using log4net;

namespace PlatePiercePlugin
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(DeviceInterface))]
    public class PlatePiercePlugin : AccessibleDeviceInterface, PlateSchedulerDeviceInterface
    {
        [Import]
        private IWorksDriverExecutor _executor { get; set; }
        private bool _connected;
        private static readonly ILog _log = LogManager.GetLogger( typeof( PlatePiercePlugin));
        private string _name = "unknown instance name";
        private string _profile;
        private bool _simulate = false;
        private readonly PlateLocation _location = new PlateLocation( "PlatePierce stage");

        public enum Commands { Pierce };
        public class CommandParameterNames
        {
            public const string pressure = "pressure_psi";
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
                _executor.CreatePlatePierceDriver( _profile);
                _connected = _executor.Initialize( _profile, init_string);
            } catch(Exception e) {
                _log.Error("PlatePierce exception during initialization", e);
                return;
            }
             

            if (!_connected)
            {
                try {
                    var error = _executor.GetErrorInfo( _profile);
                    _log.ErrorFormat( "PlatePierce error during initialization: {0}", error);
                } catch (Exception e) {
                    _log.Error("PlatePierce exception when requesting error info", e);
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

        bool DeviceInterface.ExecuteCommand( string command, IDictionary<string, object> parameters)
        {
            // the only command allowed is ApplySeal
            if( command != Commands.Pierce.ToString())
                return false;

            int pressure = (int)parameters[CommandParameterNames.pressure];

            var command_xml = String.Format( 
          @"<?xml version='1.0' ?>
            <Velocity11 file='MetaData' version='1.0' >
                <Command Name='Pierce Plate'>
                    <Parameters >
                        <Parameter Name='Piercing pressure' Units='psi' Value='{0}' >
                            <Ranges >
                                <Range Value='20' />
                                <Range Value='120' />
                            </Ranges>
                        </Parameter>
                    </Parameters>
                </Command>
            </Velocity11>", pressure);

            bool success = false;
            try {
                if( !_simulate){
                    _executor.ExecuteCommand( _profile, command_xml);
                }
                success = true;
            } catch (Exception e) {
                _log.Error("PlatePierce exception while executing command", e);
                return false;
            }
            if (!success)
            {
                try {
                    var error = _executor.GetErrorInfo( _profile);
                    _log.ErrorFormat( "PlatePierce error while executing command: {0}", error);
                } catch (Exception e) {
                    _log.Error("PlatePierce exception when requesting error info", e);
                    throw;
                }
            }
            return success;
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
            return new List<string> { Commands.Pierce.ToString() };
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
            get { return "PlatePierce"; }
        }

        string IPluginIdentity.Manufacturer
        {
            get { return "Velocity11"; }
        }

        string IPluginIdentity.Description
        {
            get { return "Velocity11 PlatePierce"; }
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
            try {
                _executor.ShowDiagsDialog( _profile, IWorksDriverLib.SecurityLevel.SECURITY_LEVEL_ADMINISTRATOR);
            } catch( Exception ex) {
                _log.ErrorFormat( "PlatePierce ShowDiagsDialog: {0}", ex.Message);
                throw;
            }
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

        #region PlateSchedulerDeviceInterface Members
        public event JobCompleteEventHandler JobComplete { add {} remove {} }
        public PlateLocation GetAvailableLocation( ActivePlate active_plate)
        {
            return _location.Available ? _location : null;
        }

        public bool ReserveLocation(PlateLocation location, ActivePlate active_plate)
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

        protected void JobThread( ActivePlate active_plate)
        {
            active_plate.WaitForPlate();
            try{
                IDictionary< string, object> parameters = new Dictionary< string, object>();
                parameters[ CommandParameterNames.pressure] = int.Parse( active_plate.GetCurrentToDo().ParametersAndVariables.Where( x => x.Name == CommandParameterNames.pressure).First().Value);
                DeviceInterface d = ( this as DeviceInterface);
                d.ExecuteCommand( active_plate.GetCurrentToDo().Command, parameters);
            } catch( Exception){
                // Log.Debug( "Exception occurred");
            }
            active_plate.MarkJobCompleted();
        }

        public void EnqueueWorklist( Worklist worklist)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
