using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using BioNex.BumblebeePlugin.Dispatcher;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.Location;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.PlateWork;
using DeviceManagerDatabase;
using log4net;

namespace BioNex.BumblebeePlugin
{
    [ PartCreationPolicy( CreationPolicy.NonShared)]
    [ Export( typeof( DeviceInterface))]
    public class Bumblebee : AccessibleDeviceInterface, ProtocolHooksInterface, ISensorQueryable, PlateSchedulerDeviceInterface, ServicesDevice
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        [ Import( typeof( Model.MainModel))]
        public Model.MainModel Model { get; set; }
        private bool HomeOnProtocolComplete { get; set; }
        private Window DiagnosticsWindow { get; set; }

        // ----------------------------------------------------------------------
        // class members.
        // ----------------------------------------------------------------------
        private static readonly List< string> Commands = new List< string>{ CommandNames.ExecuteHitpickFile, CommandNames.MoveStageToRobotTeachpoint, CommandNames.MoveStageToYR};
        private static readonly ILog Log = LogManager.GetLogger( typeof( Bumblebee));

        // ----------------------------------------------------------------------
        // constants.
        // ----------------------------------------------------------------------
        internal static class CommandNames
        {
            internal const string ExecuteHitpickFile = "ExecuteHitpickFile";
            internal const string MoveStageToRobotTeachpoint = "MoveStageToRobotTeachpoint";
            internal const string MoveStageToYR = "MoveStageToYR";
        }
        internal static class MoveStageToRobotTeachpointParameterNames
        {
            internal const string Stage = "Stage";
            internal const string Orientation = "Orientation";
        }
        internal static class MoveStageToYRParameterNames
        {
            internal const string Stage = "Stage";
            internal const string Y = "Y";
            internal const string R = "R";
        }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public Bumblebee()
        {
            HomeOnProtocolComplete = true;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        /// <summary>
        /// This is required if we want the accessible device to appear correctly (by device instance name) in the robot teaching tab
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }
        // ----------------------------------------------------------------------
        void _diagnostics_window_Closed( object sender, EventArgs e)
        {
            DiagnosticsWindow = null;
        }
        // ----------------------------------------------------------------------
        #region AccessibleDeviceInterface Members
        // ----------------------------------------------------------------------
        // IPluginIdentity:
        // ----------------------------------------------------------------------
        public string Name { get; private set; }
        public string ProductName { get { return BioNexDeviceNames.Bumblebee; }}
        public string Manufacturer { get { return BioNexAttributes.CompanyName; }}
        public string Description { get { return BioNexDeviceNames.Bumblebee; }}
        // ----------------------------------------------------------------------
        public void SetProperties( DeviceInfo device_info)
        {
            Name = device_info.InstanceName;
            Model.DeviceProperties = new Dictionary< string, string>( device_info.Properties);
        }
        // ----------------------------------------------------------------------
        // IHasDiagnosticPanel:
        // ----------------------------------------------------------------------
        public System.Windows.Controls.UserControl GetDiagnosticsPanel()
        {
            return new GUI( Model);
        }
        // ----------------------------------------------------------------------
        public void ShowDiagnostics()
        {
            if( DiagnosticsWindow == null) {
                DiagnosticsWindow = new Window();
                DiagnosticsWindow.Content = new GUI( Model);
                DiagnosticsWindow.Title =  Name + "- Diagnostics" + (Model.TechnosoftConnection.Simulating ? " (Simulating)" : "");
                DiagnosticsWindow.Closed += new EventHandler(_diagnostics_window_Closed);
            }
            DiagnosticsWindow.Show();
            DiagnosticsWindow.Activate();
        }
        // ----------------------------------------------------------------------
        // DeviceInterface:
        // ----------------------------------------------------------------------
        public bool Connected { get { return Model.Connected; }}
        public bool IsHomed { get { return Model.Homed; }}
        // ----------------------------------------------------------------------
        public void Connect()
        {
            Model.Connect(true);
        }
        // ----------------------------------------------------------------------
        public void Home()
        {
            Model.Home();
        }
        // ----------------------------------------------------------------------
        public void Close()
        {
            Model.Close();
        }
        // ----------------------------------------------------------------------
        public IEnumerable< string> GetCommands()
        {
            return Commands;
        }
        // ----------------------------------------------------------------------
        public bool ExecuteCommand( string command, IDictionary< string, object> parameters)
        {
            // bail if the command wasn't found in the list of available commands
            if( !Commands.Contains( command))
                throw new CommandNotFoundException( command);

            switch( command){
                case CommandNames.ExecuteHitpickFile:
                    throw new NotImplementedException( "this command has been deprecated");
                case CommandNames.MoveStageToRobotTeachpoint:
                    Model.MoveToRobotTeachpoint(( byte)parameters[ MoveStageToRobotTeachpointParameterNames.Stage], ( int)parameters[ MoveStageToRobotTeachpointParameterNames.Orientation]);
                    return true;
                case CommandNames.MoveStageToYR:
                    Model.Hardware.GetStage(( byte)parameters[ MoveStageToYRParameterNames.Stage]).MoveAbsolute(( double)parameters[ MoveStageToYRParameterNames.Y], ( double)parameters[ MoveStageToYRParameterNames.R]);
                    return true;
                default:
                    return false;
            }
        }
        // ----------------------------------------------------------------------
        public void Abort()
        {
            try{
                Log.Info( "Bumblebee plugin received Abort() call");
                Model.Scheduler.Abort();
                Model.TechnosoftConnection.Abort();
            } catch( Exception ex){
                Log.Debug( ex.Message, ex);
            }
        }
        // ----------------------------------------------------------------------
        public void Pause()
        {
            try{
                Log.Info( "Bumblebee plugin received Pause() call");
                Model.Pause();
            } catch( Exception ex){
                Log.Debug( ex.Message, ex);
            }
        }
        // ----------------------------------------------------------------------
        public void Resume()
        {
            try{
                Log.Info( "Bumblebee plugin received Resume() call");
                Model.Resume();
            } catch( Exception ex){
                Log.Debug( ex.Message, ex);
            }
        }
        // ----------------------------------------------------------------------
        public void Reset()
        {
            Model.Reset();
        }
        // ----------------------------------------------------------------------
        // RobotAccessibleInterface:
        // ----------------------------------------------------------------------
        public IEnumerable<PlateLocation> PlateLocationInfo
        {
            get
            {
                return Model.GetPlateLocationNames();
            }
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// for the Bumblebee, the lid location name is just the name of the
        /// stage teachpoint + " lid location".
        /// </summary>
        /// <param name="location_name"></param>
        /// <returns></returns>
        public PlateLocation GetLidLocationInfo( string location_name)
        {
            if( !Model.CanDelidPlates)
                return null;

            //! \todo this is flawed -- need to have a way to configure each location with a lid location, and
            // save the reference so we're not creating a new PlateLocationInfo each time.  Also, where are we
            // going to store the information about I/O bits?  We have to be able to associate each lid location
            // with an IODevice and input bit index.  Should probably belong to the BB configuration.
            return new PlateLocation( location_name + " lid location");
        }
        // ----------------------------------------------------------------------
        public string TeachpointFilenamePrefix
        {
            get
            {
                return Name;
            }
        }
        // ----------------------------------------------------------------------
        public int GetBarcodeReaderConfigurationIndex( string location_name)
        {
            return 0;
        }
        // ----------------------------------------------------------------------
        #endregion
        // ----------------------------------------------------------------------
        #region ProtocolHooksInterface Members
        // ----------------------------------------------------------------------
        public void ProtocolStarting()
        {
            try{
                Model.WakeUp();
            } catch( Exception){
            }
        }
        // ----------------------------------------------------------------------
        public void ProtocolStarted()
        {
        }
        // ----------------------------------------------------------------------
        public void ProtocolComplete()
        {
            if( !HomeOnProtocolComplete)
                return;
            try{
                Model.ReturnAllChannelsHome( false);
            } catch( Exception){
            }
        }
        // ----------------------------------------------------------------------
        public void ProtocolAborted()
        {
        }
        // ----------------------------------------------------------------------
        #endregion
        // ----------------------------------------------------------------------
        #region ISensorQueryable Members
        // ----------------------------------------------------------------------
        public Func<bool> GetSensorCallback(string location_name)
        {
            return Model.GetSensorCallback( location_name);
        }
        // ----------------------------------------------------------------------
        #endregion
        // ----------------------------------------------------------------------
        #region PlateSchedulerDeviceInterface Members
        // ----------------------------------------------------------------------
        public event JobCompleteEventHandler JobComplete { add {} remove {} }
        // ----------------------------------------------------------------------
        public PlateLocation GetAvailableLocation( ActivePlate active_plate)
        {
            // TODO. move this somewhere else:
            // partition the plate-locations differently/dynamically?
            IEnumerable< PlateLocation> src_locations = Model.PlateLocations.Values.Where( location => location.Name.Contains( '1') || location.Name.Contains( '2'));
            IEnumerable< PlateLocation> dst_locations = Model.PlateLocations.Values.Where( location => location.Name.Contains( '3'));
            // IEnumerable< PlateLocation> tip_locations = Model._plate_locations.Values.Where( location => location.Name.Contains( '4'));
            // determine plate type.
            ActiveSourcePlate active_src_plate = active_plate as ActiveSourcePlate;
            ActiveDestinationPlate active_dst_plate = active_plate as ActiveDestinationPlate;
            // return available location based on plate type.
            if( active_src_plate != null){
                Plate next_plate;
                // bail if there isn't another workset plate in the queue.
                if( !PlateQueue.TryPeek( out next_plate)){
                    return null;
                }
                // bail if the barcode of the plate trying to acquire a location is not the next workset plate.
                if( active_src_plate.Barcode != next_plate.Barcode){
                    return null;
                }
                // return a location if there is one available.
                foreach( PlateLocation location in src_locations){
                    if( location.Available){
                        return location;
                    }
                }
            } else if( active_dst_plate != null){
                foreach( PlateLocation location in dst_locations){
                    if( location.Available){
                        return location;
                    }
                }
            }
            return null;
        }
        // ----------------------------------------------------------------------
        public bool ReserveLocation( PlateLocation location, ActivePlate active_plate)
        {
            // not my location to reserve.
            if( !Model.PlateLocations.Values.Contains( location)){
                return false;
            }
            // reserve location.
            // determine plate type.
            ActiveSourcePlate active_src_plate = active_plate as ActiveSourcePlate;
            if( active_src_plate != null){
                Plate next_plate;
                PlateQueue.TryDequeue( out next_plate);
                Debug.Assert( active_src_plate.Barcode == next_plate.Barcode);
            }
            LocationAllocation[ active_plate] = location;
            location.Reserved.Set();
            return true;
        }
        // ----------------------------------------------------------------------
        public void LockPlace( PlatePlace place)
        {
            var stage = ( from slp in Model.PlateLocations
                          where slp.Value.Places.Contains( place)
                          select slp.Key).FirstOrDefault();
            if( stage != null){
                int orientation = place.Name.Contains( "(landscape)") ? 0 : 1;
                Model.ProtocolDispatcher.DispatchMoveStageJob( new MoveStageJob( stage, orientation));
            }
        }
        // ----------------------------------------------------------------------
        public void AddJob( ActivePlate active_plate)
        {
            new Thread( () => ExecuteThreadRunner( active_plate, active_plate.GetCurrentToDo().Command)){ Name = GetType().ToString(), IsBackground = true }.Start();
        }
        // ----------------------------------------------------------------------
        public void EnqueueWorklist( Worklist worklist)
        {
            WorklistQueue.Enqueue( worklist);
            foreach( Plate plate in worklist.SourcePlates){
                PlateQueue.Enqueue( plate);
            }
            Model.Scheduler.AddTransfers( worklist.TransferOverview);
        }
        // ----------------------------------------------------------------------
        // PlateSchedulerDeviceInterface support:
        // ----------------------------------------------------------------------
        private readonly IDictionary< ActivePlate, PlateLocation> LocationAllocation = new Dictionary< ActivePlate, PlateLocation>();
        private readonly ConcurrentQueue< Worklist> WorklistQueue = new ConcurrentQueue< Worklist>();
        private readonly ConcurrentQueue< Plate> PlateQueue = new ConcurrentQueue< Plate>();
        // ----------------------------------------------------------------------
        private void ExecuteThreadRunner( ActivePlate active_plate, string command)
        {
            active_plate.WaitForPlate();
            // once plate has arrived:
            // figure out the plate's location.
            PlateLocation location = LocationAllocation[ active_plate];
            // look up the plate's location's stage.
            var stage = ( from slp in Model.PlateLocations
                          where slp.Value == location
                          select slp.Key).FirstOrDefault();
            // tell shared memory that plate is available.
            Model.SharedMemory.SetStagePlate( stage, active_plate.Plate);
            while( !Model.SharedMemory.PlateDone( active_plate.Plate)){
                Thread.Sleep( 100);
            }
            // tell shared memory that plate has left the building.
            Model.SharedMemory.SetStagePlate( stage, null);
            active_plate.MarkJobCompleted();
        }
        // ----------------------------------------------------------------------
        #endregion
        // ----------------------------------------------------------------------
        #region ServicesDevice Members
        // ----------------------------------------------------------------------
        public void StartServices()
        {
            Model.StartScheduler();
        }
        // ----------------------------------------------------------------------
        public void StopServices()
        {
            Model.StopScheduler();
        }
        // ----------------------------------------------------------------------
        #endregion
    }
}
