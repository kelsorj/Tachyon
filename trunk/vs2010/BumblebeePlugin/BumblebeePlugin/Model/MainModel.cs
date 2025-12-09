using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Xml.Serialization;
using BioNex.BumblebeeGUI;
using BioNex.BumblebeePlugin.Dispatcher;
using BioNex.BumblebeePlugin.Hardware;
using BioNex.BumblebeePlugin.Scheduler;
using BioNex.BumblebeePlugin.Scheduler.DualChannelScheduler;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.IError;
using BioNex.Shared.LabwareDatabase;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Location;
using BioNex.Shared.PlateWork;
using BioNex.Shared.Teachpoints;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.ThreadsafeMessenger;
using BioNex.Shared.Utils;
using BioNex.Shared.Utils.WellMathUtil;
using GalaSoft.MvvmLight.Messaging;
using log4net;

[ assembly: InternalsVisibleTo( "BumblebeePluginUnitTests")]

namespace BioNex.BumblebeePlugin.Model
{
    public class AxisBoolStatus
    {
        public bool X { get; set; }
        public bool Y { get; set; }
        public bool Z { get; set; }
        public bool W { get; set; }
        public bool R { get; set; }
        public bool A { get; set; }
        public bool B { get; set; }
    }

    public class Positions
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double W { get; set; }
        public double R { get; set; }
        public double A { get; set; }
        public double B { get; set; }
    }

    [ PartCreationPolicy( CreationPolicy.NonShared)]
    [ Export]
    public class MainModel
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        [ Import]
        internal BumblebeeDispatcher ProtocolDispatcher { get; private set; }
        [ Import]
        internal IScheduler Scheduler { get; private set; }
        [ Import]
        private IError ErrorInterface { get; set; }
        [ Import]
        private ILabwareDatabase LabwareDatabase { get; set; }
        [ Import]
        private Lazy< ExternalDataRequesterInterface> DataRequestInterface { get; set; }
        [ Import]
        private BumblebeeDispatcher DiagnosticsDispatcher { get; set; }
        [ Import( "MainDispatcher")]
        private System.Windows.Threading.Dispatcher WindowsDispatcher { get; set; }
        [ Import]
        private ITipBoxManager TipBoxManager { get; set; }
        [ Import]
        private Lazy< IRobotScheduler> RobotScheduler { get; set; }

        internal Dictionary< string, string> DeviceProperties { get; set; } // INTERNAL SET ALSO!!

        internal TechnosoftConnection TechnosoftConnection { get; private set; }
        private IOInterface TipWasherIO { get; set; }
        internal BBHardware Hardware { get; private set; }
        private BumblebeeConfiguration Config { get; set; }

        internal IDictionary< Stage, PlateLocation> PlateLocations { get; private set; }
        internal bool Connected { get; private set; }
        internal ServiceSharedMemory SharedMemory { get; private set; }
        private Teachpoints Teachpoints { get; set; }

        /// <summary>
        /// This property is set when we try to load the delidding sensor information
        /// </summary>
        internal bool CanDelidPlates { get; private set; }
        // io bit index ----------vvv
        // io device name -vvvvvv
        private List< Tuple< string, int>> DeliddingSettings { get; set; }

        private Thread UpdateStatusThread { get; set; }
        private AutoResetEvent StopUpdateEvent { get; set; }

        private string ConfigurationFolder { get { return DeviceProperties[ DevicePropertyNames.ConfigFolder]; }}
        private string MotorSettingsFilepath { get { return ( ConfigurationFolder + "\\motor_settings.xml").ToAbsoluteAppPath(); }}
        private string HardwareConfigurationFilepath { get { return ( ConfigurationFolder + "\\hardware_configuration.xml").ToAbsoluteAppPath(); }}
        private string BumblebeeConfigurationFilepath { get { return ( ConfigurationFolder + "\\config.xml").ToAbsoluteAppPath(); }}

        private TipBox TipBox { get { return LabwareDatabase.GetLabware( "tipbox") as TipBox; }}

        // ----------------------------------------------------------------------
        // class members.
        // ----------------------------------------------------------------------
        private static readonly ThreadsafeMessenger BumblebeeMessenger  = new ThreadsafeMessenger();
        private static readonly ILog Log = LogManager.GetLogger( typeof(MainModel));

        // ----------------------------------------------------------------------
        // constants.
        // ----------------------------------------------------------------------
        internal static class DevicePropertyNames
        {
            internal const string ConfigFolder = "configuration folder";
            internal const string TeachpointFile = "teachpoint file";
            internal const string Simulate = "simulate";
            internal const string Port = "port";
            internal const string DelidSensors = "delid sensor map"; // e.g., device_name1,bit_number1;device_name2,bit_number2
        }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public MainModel()
        {
            Connected = false;
            Hardware = new BBHardware();
            Teachpoints = new Teachpoints();
            StopUpdateEvent = new AutoResetEvent( false);
            // technosoft connection is instantiated in Initialize since we need to
            // control the constructor that gets called based on preferences

            DeliddingSettings = new List< Tuple< string, int>>();
            Messenger.Default.Register< NumberOfTransferCompleteMessage>( this, HandleTransferCompleteMessage);
        }
        // ----------------------------------------------------------------------
        private void HandleTransferCompleteMessage( NumberOfTransferCompleteMessage msg)
        {
            Messenger.Default.Send( msg);
        }
        // ----------------------------------------------------------------------
        public bool VerifyCheckStagePosition( byte stage_id, double y, double r)
        {
            try{
                MotorSettings y_settings = Hardware.GetStage(stage_id).YAxis.Settings;
                double y_min = y_settings.MinLimit;
                double y_max = y_settings.MaxLimit;
                MotorSettings r_settings = Hardware.GetStage(stage_id).RAxis.Settings;
                double r_min = r_settings.MinLimit;
                double r_max = r_settings.MaxLimit;
                return (y >= y_min) && (y <= y_max) && (r >= r_min) && (r <= r_max);
            } catch( Exception){
                return false;
            }
        }
        // ----------------------------------------------------------------------
        public void Connect( bool connect)
        {
            if( connect) {
                Initialize();
                UpdateStatusThread = new Thread( UpdateStatus){ Name = "Bumblebee status updates", IsBackground = true };
                UpdateStatusThread.Start();
            } else {
                StopUpdateEvent.Set();
                UpdateStatusThread.Join();
                Close();
            }
        }
        // ----------------------------------------------------------------------
        private void Initialize()
        {
            // load the teachpoint file -- location is in the preferences passed
            // by the application after plugin instantiation
            try{
                LoadTeachpointFile();
            } catch( Exception ex){
                Log.Error( "Could not load teachpoint file", ex);
            }
            // create whatever technosoft connection is specified by preferences
            // if we are simulating, then use the simulator.  Otherwise, use
            // the specified SysTec port
            bool simulate = DeviceProperties[ DevicePropertyNames.Simulate] != "0";
            try{
                if( simulate) {
                    TechnosoftConnection = new TechnosoftConnection();
                } else {
                    string port = DeviceProperties[ DevicePropertyNames.Port];
                    TechnosoftConnection = new TechnosoftConnection( port, TML.TMLLib.CHANNEL_SYS_TEC_USBCAN, 500000);
                }
            } catch( Exception ex){
                string msg = ex.Message + ". Please restart the application.";
                MessageBox.Show( msg, "Bumblebee Plugin");
                Log.Fatal( ex);
                throw;
            }

            TipWasherIO = DataRequestInterface.Value.GetIOInterfaces().FirstOrDefault( i => ( i as DeviceInterface).Name == "TipWasherIO" /*prop["tip-washer i/o device name"*/);

            LoadSettings();

            Hardware.Stages.ForEach( s => s.SetSystemTeachpoints( Teachpoints));

            // turn off the pump
            Connected = true;

            // DKM 2012-01-18 open output file
            string exe_path = BioNex.Shared.Utils.FileSystem.GetAppPath();
            string datestamp = DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss");
            ( ProtocolDispatcher as BumblebeeDispatcher).Setup( Config, Hardware, Teachpoints, BumblebeeMessenger, ProtocolExecutor_HandleError, false);
            ( DiagnosticsDispatcher as BumblebeeDispatcher).Setup( Config, Hardware, Teachpoints, BumblebeeMessenger, DiagnosticsExecutor_HandleError, true);
            ProtocolDispatcher.StartDispatcher();
            DiagnosticsDispatcher.StartDispatcher();

            SharedMemory = new ServiceSharedMemory( Hardware, Config);

            Scheduler.SetHardware( Hardware);
            Scheduler.SetMessenger( MainModel.BumblebeeMessenger);
            Scheduler.SetDispatcher( ProtocolDispatcher);
            Scheduler.SetSharedMemory( SharedMemory);
            Scheduler.SetTipBoxManager( TipBoxManager);
            Scheduler.SetRobotScheduler( RobotScheduler.Value);
        }
        // ----------------------------------------------------------------------
        void ProtocolExecutor_HandleError( object sender, ErrorData error_data)
        {
            ErrorInterface.AddError( error_data);
        }
        // ----------------------------------------------------------------------
        void DiagnosticsExecutor_HandleError( object sender, ErrorData error_data)
        {
            error_data.AddEvent( "Abort");
            WindowsDispatcher.BeginInvoke( new AddErrorDelegate( AddErrorSTA), error_data);
        }
        // ----------------------------------------------------------------------
        private delegate void AddErrorDelegate( ErrorData error);
        // ----------------------------------------------------------------------
        private static void AddErrorSTA( ErrorData error)
        {
            if( Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "Add Hive GUI error";
            BioNex.Shared.ErrorHandling.ErrorDialog dlg = new BioNex.Shared.ErrorHandling.ErrorDialog( error);
            // #179 need to use modeless dialogs, because modal ones prevent us from being able to reset the interlocks via software
            dlg.Show();
        }
        // ----------------------------------------------------------------------
        public void Close()
        {
            ProtocolDispatcher.StopDispatcher();
            DiagnosticsDispatcher.StopDispatcher();
            StopUpdateEvent.Set();
            TechnosoftConnection.Close();
            Connected = false;
        }
        // ----------------------------------------------------------------------
        public bool SchedulerIsRunning()
        {
            return Scheduler.IsRunning;
        }
        // ----------------------------------------------------------------------
        public void StartScheduler()
        {
            Scheduler.StartScheduler();
        }
        // ----------------------------------------------------------------------
        public void StopScheduler()
        {
            Scheduler.StopScheduler();
        }
        // ----------------------------------------------------------------------
        public void PauseScheduler()
        {
            Scheduler.PauseScheduler();
        }
        // ----------------------------------------------------------------------
        public void ResumeScheduler()
        {
            Scheduler.ResumeScheduler();
        }
        // ----------------------------------------------------------------------
        internal void Pause()
        {
            TechnosoftConnection.Pause();
            Scheduler.Pause();
        }
        // ----------------------------------------------------------------------
        internal void Resume()
        {
            TechnosoftConnection.Resume();
            Scheduler.Resume();
        }
        // ----------------------------------------------------------------------
        private void LoadMotorSettings()
        {
            try{
                TechnosoftConnection.LoadConfiguration( MotorSettingsFilepath, ConfigurationFolder);
                // DKM 2011-08-12 need this for broadcasting servo enable message.  We don't care who the master is, so just pick the first one available.
                byte first_axis_id = TechnosoftConnection.GetAxes().First().Key;
                TechnosoftConnection.SetBroadcastMasterAxisID( first_axis_id);
                foreach( byte group_id in new[]{ 1, 2, 3, 4, 5, 6, 7, 8}){
                    TechnosoftConnection.SetGroupMasterAxisID( group_id, first_axis_id);
                }
            } catch( KeyNotFoundException ex){
                MessageBox.Show("Motor settings file and/or TSM setup folder has not been specified.  Please make selections and then restart the application.");
                throw ex;
            } catch( InvalidMotorSettingsException ex){
                string error = String.Format("Error parsing motor settings for axis {0}.  The current value for the setting named {1} is either missing, or invalid.  The valid range is between {2} and {3}.", ex.AxisID, ex.SettingName, ex.MinValue, ex.MaxValue);
                MessageBox.Show(error);
                Log.Error(error, ex);
            }
        }
        // ----------------------------------------------------------------------
        private void LoadHardwareConfiguration()
        {
            try{
                Hardware.LoadConfiguration( HardwareConfigurationFilepath, TechnosoftConnection, TipWasherIO);
                PlateLocations = new Dictionary< Stage, PlateLocation>();
                foreach( Stage stage in Hardware.Stages){
                    string stage_name = String.Format( "BB PM {0}", stage.ID);
                    IList< PlatePlace> stage_places = new List< PlatePlace>();
                    stage_places.Add( new PlatePlace( stage_name + " (landscape)"));
                    stage_places.Add( new PlatePlace( stage_name + " (portrait)"));
                    PlateLocations[ stage] = new PlateLocation( stage_name, stage_places);
                }
            } catch( KeyNotFoundException){
                MessageBox.Show("Hardware configuration file has not been specified.  Please select one and then restart the application.");
            }
        }
        // ----------------------------------------------------------------------
        private void LoadDeliddingConfiguration()
        {
            try{
                DeliddingSettings.Clear();
                string delidding_info;
                // DKM 2011-10-24 changed from catching a KeyNotFoundException to testing for the key, so we don't throw exceptions unnecessarily
                bool found_key = DeviceProperties.TryGetValue( DevicePropertyNames.DelidSensors, out delidding_info);
                if (found_key)
                {
                    // get all of the delid location I/O info, which is a lookup in order of plate mover ID,
                    // i.e. goes in order BB PM 1, BB PM 2, ... BB PM #
                    string[] all_io = delidding_info.Split(';');
                    // now split each of the locations up into [IO device name, IO bit index]
                    foreach (string io in all_io)
                    {
                        string[] temp = io.Split(',');
                        Debug.Assert(temp.Count() == 2, "Incorrect format for Bumblebee delid sensor configuration.  Should look like '<io device name #1>,<io index #1>;<io device name #2>,<io index #2>'");
                        string io_device_name = temp[0];
                        int io_index = temp[1].ToInt();
                        DeliddingSettings.Add(new Tuple<string, int>(io_device_name, io_index));
                    }
                    CanDelidPlates = true;
                }
                else
                {
                    // assume no delidding
                    CanDelidPlates = false;
                }
            } catch( Exception ex){
                // assume no delidding
                Log.DebugFormat( "Could not load delidding configuration, so assuming that delidding is not possible.  The error was '{0}'", ex.Message);
                CanDelidPlates = false;
            }
        }
        // ----------------------------------------------------------------------
        private void LoadBumblebeeConfiguration()
        {
            try{
                FileStream reader = new FileStream( BumblebeeConfigurationFilepath, FileMode.Open);
                XmlSerializer serializer = new XmlSerializer( typeof( BumblebeeConfiguration));
                Config = ( BumblebeeConfiguration)serializer.Deserialize( reader);
                reader.Close();
            } catch( KeyNotFoundException){
                MessageBox.Show( String.Format( "Could not load Bumblebee plugin because the 'configuration folder' property is missing from the device configuration database"));
            } catch( FileNotFoundException ex){
                MessageBox.Show( String.Format( "Could not load Bumblebee plugin: {0}", ex.Message));
            }
        }
        // ----------------------------------------------------------------------
        public IList< PlateLocation> GetPlateLocationNames()
        {
            return ( PlateLocations != null) ? PlateLocations.Values.ToList() : new List< PlateLocation>();
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Call this Home implementation from within the plugin -- puts errors up in
        /// dialog boxes over the diagnostics GUI
        /// </summary>
        public void Home()
        {
            Home( false);
        }
        // ----------------------------------------------------------------------
        public bool Homed
        {
            get { return Hardware.Homed; }
        }
        // ----------------------------------------------------------------------
        /// <remark>
        /// This is a good example of why the TechnosoftLibrary shouldn't have On() and Off()
        /// methods for IAxis.  Should be Enable( bool) instead.
        /// </remark>
        /// <param name="channel_id"></param>
        /// <param name="stage_id"></param>
        /// <param name="axis_name"></param>
        /// <param name="enable"></param>
        public void ServoEnable( byte channel_id, byte stage_id, string axis_name, bool on)
        {
            HardwareQuantum hardware_quantum = Hardware.GetHardwareQuantum( channel_id, stage_id, axis_name);
            try{
                DiagnosticsDispatcher.DispatchAction( new EnableAxisJob( hardware_quantum, axis_name, on));
            } catch( Exception){
                MessageBox.Show( String.Format( "Could not enable axis {0}{1}", axis_name, hardware_quantum.ID));
            }
        }
        // ----------------------------------------------------------------------
        public Positions GetLRTeachpoint( byte channel_id, byte stage_id)
        {
            Teachpoint tp = Teachpoints.GetStageTeachpoint( channel_id, stage_id).LowerRight;
            Positions p = new Positions();
            p.X = tp["x"];
            p.Y = tp["y"];
            p.Z = tp["z"];
            p.R = tp["r"];
            return p;
        }
        // ----------------------------------------------------------------------
        public Positions GetULTeachpoint( byte channel_id, byte stage_id)
        {
            Teachpoint tp = Teachpoints.GetStageTeachpoint( channel_id, stage_id).UpperLeft;
            Positions p = new Positions();
            p.X = tp["x"];
            p.Y = tp["y"];
            p.Z = tp["z"];
            p.R = tp["r"];
            return p;
        }
        // ----------------------------------------------------------------------
        public Positions GetWashTeachpoint( byte channel_id)
        {
            Teachpoint tp = Teachpoints.GetWashTeachpoint( channel_id);
            Positions p = new Positions();
            p.X = tp["x"];
            p.Z = tp["z"];
            return p;
        }
        // ----------------------------------------------------------------------
        public void WakeUp()
        {
            ServosOn( true, false);
        }
        // ----------------------------------------------------------------------
        private void UpdateStatus()
        {
            while( !StopUpdateEvent.WaitOne( 100)){
                if( !Connected){
                    continue;
                }
                try{
                    Hardware.AvailableHardwareQuanta.ForEach( q => q.UpdateCurrentStatus());
                } catch( Exception){
                    // ignore exceptions to ensure that update-status thread stays alive.
                }
            }
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Whether or not the Z axis for the specified channel is within 1mm of its home position
        /// </summary>
        /// <param name="channel_id"></param>
        /// <returns></returns>
        internal bool IsChannelAtSafeZ( byte channel_id)
        {
            try{
                // should be +-1mm from the home position for Z.  This is a quick and dirty way to do the validation, so I don't have to worry about axis flipping.
                return Hardware.GetChannel( channel_id).AxisStatuses[ "Z"].PositionMM < 1;
                // return CurrentChannelAndStagePositions[channel_id - 1, 0].Z < 1;
                // return Math.Abs(_hw.GetChannel(channel_id).ZAxis.GetPositionMM()) < 1;
            } catch( Exception){
                return false;
            }
        }
        // ----------------------------------------------------------------------
        internal bool HasOnlyOneIoInterface()
        {
            return DataRequestInterface.Value.GetIOInterfaces().Count() == 1;
        }
        // ----------------------------------------------------------------------
        internal void Reset()
        {
            TechnosoftConnection.ResetPauseAbort();
            Hardware.Reset();
        }
        // ----------------------------------------------------------------------
        internal Func<bool> GetSensorCallback(string location_name)
        {
            // use regex
            Regex regex = new Regex(@"BB PM (\d+) lid location");
            MatchCollection matches = regex.Matches( location_name);
            if (matches.Count == 0)
                throw new Exception("Could not parse location name for delidding");
            Debug.Assert( matches.Count == 1);
            Debug.Assert( matches[0].Groups.Count == 2);
            string location_number = matches[0].Groups[1].ToString();
            int location_index = location_number.ToInt() - 1;
            Tuple<string,int> sensor_info = DeliddingSettings[location_index];
            var io_device = DataRequestInterface.Value.GetIOInterfaces().Where( x => (x as DeviceInterface).Name == sensor_info.Item1).FirstOrDefault();
            if( io_device == null)
                return null;
            int io_bit_number = sensor_info.Item2;
            // note that we save the bit NUMBER in the device database, not the index!  GUI is always 1-based.
            return () => { return io_device.GetInput( io_bit_number - 1); };
        }
        // ----------------------------------------------------------------------
        public void FlushDispatcher()
        {
            DiagnosticsDispatcher.FlushDispatcher();
        }







        #region GUI commands implementation
        // ---------------------------------------------------------------------- ALSO USED OUTSIDE OF DIAGNOSTICS
        internal void ServosOn( bool on, bool called_from_diags)
        {
            Hardware.AvailableHardwareQuanta.ForEach( q => ( called_from_diags ? DiagnosticsDispatcher : ProtocolDispatcher).DispatchAction( new EnableHardwareQuantumJob( q, on)));
        }
        // ---------------------------------------------------------------------- ALSO USED OUTSIDE OF DIAGNOSTICS
        internal void LoadSettings()
        {
            LoadMotorSettings();
            LoadHardwareConfiguration();
            LoadDeliddingConfiguration();
            LoadBumblebeeConfiguration();
        }
        // ---------------------------------------------------------------------- ALSO USED OUTSIDE OF DIAGNOSTICS
        internal void LoadTeachpointFile()
        {
            Teachpoints.LoadTeachpointFile( DeviceProperties[ DevicePropertyNames.TeachpointFile].ToAbsoluteAppPath(), null);
        }
        // ----------------------------------------------------------------------
        internal void HomeAxis( byte channel_id, byte stage_id, string axis_name)
        {
            try{
                HardwareQuantum hardware_quantum = Hardware.GetHardwareQuantum( channel_id, stage_id, axis_name);
                DiagnosticsDispatcher.DispatchAction( new HomeAxisJob( hardware_quantum, axis_name));
            } catch( NullReferenceException){
                MessageBox.Show( "Cannot home axis " + axis_name + " because it is not connected");
            } catch( Exception){
                // do nothing
            }
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Jogs the specified axis in the positive direction by the selected jog increment
        /// </summary>
        /// <param name="channel_id"></param>
        /// <param name="stage_id"></param>
        /// <param name="axis_name"></param>
        /// <param name="jog_increment"></param>
        internal void JogPositive( byte channel_id, byte stage_id, string axis_name, double jog_increment)
        {
            try{
                HardwareQuantum hardware_quantum = Hardware.GetHardwareQuantum( channel_id, stage_id, axis_name);
                DiagnosticsDispatcher.DispatchAction( new JogAxisJob( hardware_quantum, axis_name, jog_increment));
            } catch( Exception ex){
                MessageBox.Show( String.Format( "Could not jog {0} axis by specified amount: {1}", axis_name, ex.Message));
            }
        }
        // ----------------------------------------------------------------------
        internal void MoveToStageTeachpoint( byte channel_id, byte stage_id, double distance_above_mm, bool move_to_upper_left)
        {
            try{
                // first, need to move all of the channels up so we don't crash other tips
                // that may have been deployed!
                Hardware.AvailableChannels.ForEach( c => c.ZAxis.MoveAbsolute( 0));
                Channel channel = Hardware.GetChannel( channel_id);
                Stage stage = Hardware.GetStage( stage_id);
                StageTeachpoint stp = Teachpoints.GetStageTeachpoint( channel_id, stage_id);
                Teachpoint tp = move_to_upper_left ? stp.UpperLeft : stp.LowerRight;
                // now move all other axes
                channel.XAxis.MoveAbsolute(tp["x"]);
                stage.YAxis.MoveAbsolute(tp["y"]);
                stage.RAxis.MoveAbsolute(tp["r"]);
                channel.MoveRelativeUpFrom( tp["z"], distance_above_mm);
            } catch( AxisException ex){
                string corner_name = move_to_upper_left ? "upper left" : "lower right";
                string message = ( distance_above_mm == 0)
                    ? String.Format("Could not move to {0} teachpoint for stage {1}: {2}", corner_name, stage_id, ex.Message)
                    : String.Format("Could not move {0}mm above {1} teachpoint for stage {2}: {3}", distance_above_mm, corner_name, stage_id, ex.Message);
                Log.Info( message, ex);
                MessageBox.Show( message);
            } catch( KeyNotFoundException ){
                MessageBox.Show( String.Format( "You need to save a teachpoint for stage {0}, channel {1} first.", stage_id, channel_id));
            }
        }
        // ----------------------------------------------------------------------
        internal void MoveToTipShucker( byte channel_id, byte stage_id, bool stay_above)
        {
            try{
                Channel channel = Hardware.GetChannel( channel_id);
                Teachpoint wtp = Teachpoints.GetWashTeachpoint( channel_id);
                // move to z = 0;
                channel.ZAxis.MoveAbsolute( 0);
                // now move all other axes
                channel.XAxis.MoveAbsolute( wtp["x"]);
                if( !stay_above){
                    channel.ZAxis.MoveAbsolute( wtp["z"]);
                }
            } catch( AxisException ex){
                string message = String.Format( "Could not move {0} the wash teachpoint for channel {1}, stage {2}: {3}", stay_above ? "above" : "to", channel_id, stage_id, ex.Message);
                Log.Info( message, ex);
                MessageBox.Show( message);
            }
        }
        // ---------------------------------------------------------------------- ALSO USED OUTSIDE OF DIAGNOSTICS
        internal void MoveToRobotTeachpoint( byte stage_id, int orientation)
        {
            try{
                Hardware.GetStage( stage_id).MoveToRobotTeachpoint( orientation);
            } catch( KeyNotFoundException){
                MessageBox.Show( String.Format( "You need to save a robot teachpoint for stage {0} first.", stage_id));
            } catch( Exception ex){
                MessageBox.Show( String.Format( "Could not move to robot teachpoint for stage {0}: {1}", stage_id, ex.Message));
            }
        }
        // ----------------------------------------------------------------------
        internal void MoveToTipWasherTeachpoint( byte shuttle_id, string command_arguments)
        {
            string[] argv = command_arguments.Split( '-');
            if( argv.Length != 2){
                throw new ArgumentException( "command_arguments must contain exactly one hyphen");
            }
            try{
                TipShuttle tip_shuttle = Hardware.GetTipShuttle( shuttle_id);
                tip_shuttle.MoveWasher( TipShuttle.ToWasherPosition( argv[ 1]), argv[ 0] == TipShuttle.PLENUM_PREFIX, argv[ 0] == TipShuttle.BATH_PREFIX);
            } catch( Exception ex){
                MessageBox.Show( String.Format( "Could not move washer {0} to {1} teachpoint: {2}", argv[ 0], argv[ 1], ex.Message));
            }
        }
        // ----------------------------------------------------------------------
        /// <remarks>
        /// This should ONLY get called from the GUI!!!
        /// </remarks>
        /// <param name="channel_id"></param>
        /// <param name="stage_id"></param>
        internal void TeachUpperLeft( byte channel_id, byte stage_id)
        {
            Teach( channel_id, stage_id, true);
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// This should ONLY get called from the GUI!!!
        /// </summary>
        /// <param name="channel_id"></param>
        /// <param name="stage_id"></param>
        internal void TeachLowerRight( byte channel_id, byte stage_id)
        {
            Teach( channel_id, stage_id, false);
        }
        // ----------------------------------------------------------------------
        // helper function of TeachUpperLeft and TeachLowerRight:
        private void Teach( byte channel_id, byte stage_id, bool teach_ul)
        {
            MessageBoxResult result = MessageBox.Show( "Are you sure you want to teach the tip here?", "Confirm teachpoint", MessageBoxButton.YesNo);
            if( result == MessageBoxResult.No)
                return;
            if( !Hardware.GetChannel( channel_id).IsHomed()){
                MessageBox.Show( String.Format( "You may not teach this point because channel {0} is not homed", channel_id));
                return;
            }
            if( !Hardware.GetStage( stage_id).IsHomed()){
                MessageBox.Show( String.Format( "You may not teach this point because stage {0} is not homed", stage_id));
                return;
            }
            try{
                Channel channel = Hardware.GetChannel(channel_id);
                Stage stage = Hardware.GetStage(stage_id);
                double x = channel.XAxis.GetPositionMM();
                double z = channel.ZAxis.GetPositionMM();
                double y = stage.YAxis.GetPositionMM();
                double r = stage.RAxis.GetPositionMM();

                // #148: prevent out-of-range teaching
                // verify that the positions are within range for each axis
                MotorSettings settings = channel.XAxis.Settings;
                if( x < settings.MinLimit || x > settings.MaxLimit) {
                    MessageBox.Show( "Cannot teach this point because the current X position is out of range.");
                    return;
                }
                settings = channel.ZAxis.Settings;
                if( z < settings.MinLimit || z > settings.MaxLimit) {
                    MessageBox.Show( "Cannot teach this point because the current Z position is out of range.");
                    return;
                }
                settings = stage.YAxis.Settings;
                if( y < settings.MinLimit || y > settings.MaxLimit) {
                    MessageBox.Show( "Cannot teach this point because the current Y position is out of range.");
                    return;
                }
                settings = stage.RAxis.Settings;
                if( r < settings.MinLimit || r > settings.MaxLimit) {
                    MessageBox.Show( "Cannot teach this point because the current theta position is out of range.");
                    return;
                }
                //! \todo given the current theta position and UL or LR flag, figure out if rotating that point
                //        in a circle allows Y to remain within its soft limits

                if (teach_ul) {
                    Teachpoints.AddUpperLeftStageTeachpoint(channel_id, stage_id, x, z, y, r);
                    // for the UL teachpoint only, give the user the option of teaching the LR corner
                    MessageBoxResult answer = MessageBox.Show( "Would you like to teach the approximate location of the lower right teachpoint?", "Confirm auto-teach", MessageBoxButton.YesNo);
                    if( answer == MessageBoxResult.Yes) {
                        AutoTeach( channel_id, stage_id, false);
                    }
                }
                else
                    Teachpoints.AddLowerRightStageTeachpoint(channel_id, stage_id, x, z, y, r);

                Teachpoints.SaveTeachpointFile();
            } catch( Exception ex){
                MessageBox.Show(String.Format("Could not save teachpoint file: {0}", ex.Message));
            }
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Teaches the opposite corner of the selected stage for the selected channel
        /// </summary>
        /// <param name="channel_id"></param>
        /// <param name="stage_id"></param>
        /// <param name="auto_teach_ul"></param>
        private void AutoTeach( byte channel_id, byte stage_id, bool auto_teach_ul)
        {
            // get current position information
            Channel channel = Hardware.GetChannel( channel_id);
            Stage stage = Hardware.GetStage( stage_id);
            double x = channel.XAxis.GetPositionMM();
            double z = channel.ZAxis.GetPositionMM();
            double y = stage.YAxis.GetPositionMM();
            double r = stage.RAxis.GetPositionMM();

            // apply rotation to figure out where the opposite corner is:
            // we assume a 96 well plate
            Tuple< double, double> ulxy = WellMathUtil.GetA1DistanceFromCenterOfPlate( LabwareFormat.LF_STANDARD_96); //! \todo FYC get rid of hardcoded 96-well labware format.
            // rotate these positions by r
            Tuple< double, double> ulxy_rotated = WellMathUtil.GetXYAfterRotation( ulxy, Math.Abs( r), r < 0);
            // figure out where the opposite well location is
            Tuple< double, double> lrxy = ( Hardware.GetStage( stage_id) is TipShuttle)
                ? Tuple.Create( ulxy.Item1 + 72, ulxy.Item2 - (( 15 * 9) + ( 2 * 7.5)))
                : Tuple.Create( ulxy.Item1 + ( 11 * 9), ulxy.Item2 - ( 7 * 9)); // for a 96 well plate, the x position is a1x + 11 * 9, and y position is a1y - 7 * 9.
            // rotate the LR point by r degrees
            Tuple< double, double> lrxy_rotated = WellMathUtil.GetXYAfterRotation( lrxy, Math.Abs( r), r < 0);

            // now that we have the rotated UL and LR points, we know the new X and Y offsets
            // and can apply them to one teachpoint to get the other
            double delta_x = lrxy_rotated.Item1 - ulxy_rotated.Item1;
            double delta_y = lrxy_rotated.Item2 - ulxy_rotated.Item2;
            double new_x, new_y;
            if( auto_teach_ul){
                // we want to teach the UL teachpoint based on the LR teachpoint
                new_x = x - delta_x;
                new_y = y + delta_y;
            } else{
                // we want to teach the LR teachpoint based on the UL teachpoint
                new_x = x + delta_x;
                new_y = y - delta_y;
            }

            // modify the teachpoints object -- it will get saved from the Teach()
            // method once this method returns

            if( auto_teach_ul){
                Teachpoints.AddUpperLeftStageTeachpoint( channel_id, stage_id, new_x, z, new_y, r);
            } else{
                Teachpoints.AddLowerRightStageTeachpoint( channel_id, stage_id, new_x, z, new_y, r);
            }

            // not needed, the caller will eventually save the file anyway
            // DeviceTeachpoints.SaveTeachpointFile();        
        }
        // ----------------------------------------------------------------------
        internal void TeachWashStation( byte channel_id)
        {
            MessageBoxResult result = MessageBox.Show( "Are you sure you want to teach the tip shucking station here?", "Confirm teachpoint", MessageBoxButton.YesNo);
            if( result == MessageBoxResult.No)
                return;

            Channel channel = Hardware.GetChannel( channel_id);
            double x = channel.XAxis.GetPositionMM();
            double z = channel.ZAxis.GetPositionMM();

            // #148: prevent out-of-range teaching
            MotorSettings settings = channel.XAxis.Settings;
            if( x < settings.MinLimit || x > settings.MaxLimit) {
                MessageBox.Show( "Cannot teach the tip shucking location because the current X position is out of range.");
                return;
            }
            settings = channel.ZAxis.Settings;
            if( z < settings.MinLimit || z > settings.MaxLimit) {
                MessageBox.Show( "Cannot teach the tip shucking location because the current Z position is out of range.");
                return;
            }

            Teachpoints.AddWashTeachpoint( channel_id, x, z);
            Teachpoints.SaveTeachpointFile();
        }
        // ----------------------------------------------------------------------
        internal void TeachRobotPickup( byte stage_id, int orientation)
        {
            Stage s = Hardware.GetStage( stage_id);
            bool stage_is_tip_shuttle = s is TipShuttle;

            MessageBoxResult result = MessageBox.Show( String.Format( "Are you sure you want to teach the {0} here?", stage_is_tip_shuttle ? "tip-shuttle washer position" : "robot pickup location"), "Confirm teachpoint", MessageBoxButton.YesNo);
            if( result == MessageBoxResult.No)
                return;

            double y = s.YAxis.GetPositionMM();
            double r = s.RAxis.GetPositionMM();

            // #148: prevent out-of-range teaching
            MotorSettings settings = s.YAxis.Settings;
            if( y < settings.MinLimit || y > settings.MaxLimit) {
                MessageBox.Show( "Cannot teach the robot pickup location because the current Y position is out of range.");
                return;
            }
            settings = s.RAxis.Settings;
            if( r < settings.MinLimit || r > settings.MaxLimit) {
                MessageBox.Show( "Cannot teach the robot pickup location because the current theta position is out of range.");
                return;
            }

            Teachpoints.AddRobotTeachpoint( stage_id, y, r, orientation);
            Teachpoints.SaveTeachpointFile();
        }
        // ----------------------------------------------------------------------
        internal void TeachTipWasherTeachpoint( byte shuttle_id, string command_arguments)
        {
            string[] argv = command_arguments.Split( '-');
            if( argv.Length != 2){
                throw new ArgumentException( "command_arguments must contain exactly one hyphen");
            }

            MessageBoxResult result = MessageBox.Show( String.Format( "Are you sure you want to teach the {0} {1} position here?", argv[ 0], argv[ 1]), "Confirm teachpoint", MessageBoxButton.YesNo);
            if( result == MessageBoxResult.No){
                return;
            }

            double position = 0.0;
            if( command_arguments.StartsWith( TipShuttle.PLENUM_PREFIX)){
                position = Hardware.GetTipShuttle( shuttle_id).AAxis.GetPositionMM();
            } else if( command_arguments.StartsWith( TipShuttle.BATH_PREFIX)){
                position = Hardware.GetTipShuttle( shuttle_id).BAxis.GetPositionMM();
            } else{
                throw new ArgumentException( "command_arguments must begin with 'plenum' or 'bath'");
            }
            Teachpoints.AddWasherTeachpoint( shuttle_id, command_arguments, position);
            Teachpoints.SaveTeachpointFile();
        }
        // ---------------------------------------------------------------------- ALSO USED OUTSIDE OF DIAGNOSTICS
        /// <summary>
        /// Call this Home implementation when you want to pass your own error handler object
        /// </summary>
        /// 
        /// <param name="called_from_diags"></param>
        internal void Home( bool called_from_diags)
        {
            ( called_from_diags ? DiagnosticsDispatcher : ProtocolDispatcher).DispatchHome();
        }
        // ----------------------------------------------------------------------
        internal void ReturnChannelHome( byte channel_id)
        {
            DiagnosticsDispatcher.DispatchAction( new ReturnChannelHomeJob( Hardware.GetChannel( channel_id)));
        }
        // ---------------------------------------------------------------------- ALSO USED OUTSIDE OF DIAGNOSTICS
        internal void ReturnAllChannelsHome( bool called_from_diags)
        {
            Hardware.AvailableChannels.ForEach( c => ( called_from_diags ? DiagnosticsDispatcher : ProtocolDispatcher).DispatchAction( new ReturnChannelHomeJob( c)));
        }
        // ----------------------------------------------------------------------
        internal void PressTipOn( byte channel_id, byte stage_id, string tip_name, bool position_press)
        {
            if( tip_name == null){
                MessageBox.Show( "Please enter a valid tip name.", "Invalid tip name");
                return;
            }
            Well well;
            try{
                well = new Well( tip_name);
            } catch( Well.InvalidWellNameException){
                MessageBox.Show( "Please enter a valid tip name.", "Invalid tip name");
                return;
            }
            throw new Exception( "FYC -- last param was well!!");
            DiagnosticsDispatcher.DispatchTipOnJob( new TipJob( Hardware.GetChannel( channel_id), Hardware.GetStage( stage_id), TipBox, null));
        }
        // ----------------------------------------------------------------------
        internal void ShuckTipOff( byte channel_id)
        {
            DiagnosticsDispatcher.DispatchTipOffJob( new TipOffJob( Hardware.GetChannel( channel_id)));
        }
        // ----------------------------------------------------------------------
        internal void ShuckAllTipsOff()
        {
            foreach( Channel channel in Hardware.AvailableChannels.Where( channel => channel.TipWell != null)){
                DiagnosticsDispatcher.DispatchWashableTipsOffWithMove( new TipJob( channel, channel.TipWell.Carrier.Stage, null, null));
            }
        }
        // ----------------------------------------------------------------------
        internal void RandomTipTest( byte stage_id)
        {
            // convert byte/string parameters into objects.
            Stage stage = Hardware.GetStage( stage_id);
            // filter down to available channels.
            IList< Channel> available_channels = Hardware.AvailableChannels;
            // log channel spacings.
            foreach( Channel outer_channel in available_channels){
                foreach( Channel inner_channel in available_channels){
                    byte outer_channel_id = outer_channel.ID;
                    byte inner_channel_id = inner_channel.ID;
                    if( outer_channel_id < inner_channel_id){
                        double channel_spacing = Hardware.GetChannelSpacing( outer_channel_id, inner_channel_id, stage_id);
                        Log.InfoFormat( "Channel spacing between channel {0} and channel {1} on stage {2} is {3}", outer_channel_id, inner_channel_id, stage_id, channel_spacing);
                    }
                }
            }
            // create a new random number generator.
            Random random = new Random();
            // save number of channels.
            int num_channels = available_channels.Count();
            // create a new tip tracker.
            TipTracker tip_tracker = new TipTracker();
            // load tip tracker with a fresh box of tips.
            // tip_tracker.TipBoxLoaded( 96);
            // keep going until we run out of tips to press.
            while( tip_tracker.CountTipsOfState( TipWellState.Clean) > 0){
                // 10% of the time, we do a single tip press.
                // also, we always single tip press on the last tip.
                if(( random.NextDouble() < 0.1) || ( tip_tracker.CountTipsOfState( TipWellState.Clean) == 1)){
                    // single tip press.
                    // pick a channel at random.
                    Channel channel = available_channels[ random.Next( 0, num_channels)];
                    // pick a tip from the tip tracker.
                    TipWell tip_well = tip_tracker.ReserveOneTip();
                    // dispatch tip on and tip off jobs.
                    DiagnosticsDispatcher.DispatchTipOnJob( new TipJob( channel, stage, TipBox, tip_well));
                    DiagnosticsDispatcher.DispatchTipOffJob( new TipOffJob( channel));
                } else{
                    // dual tip press.
                    // pick two channels at random.
                    int channel_index1 = random.Next( 0, num_channels);
                    int channel_index2 = random.Next( 0, num_channels);
                    while(( channel_index2 == channel_index1) || ( Math.Abs( channel_index1 - channel_index2) > 3)){
                        channel_index2 = random.Next( 0, num_channels);
                    }
                    if( channel_index2 < channel_index1){
                        int temp = channel_index1;
                        channel_index1 = channel_index2;
                        channel_index2 = temp;
                    }
                    Channel channel1 = available_channels[ channel_index1];
                    Channel channel2 = available_channels[ channel_index2];
                    // pick two tips from the tip tracker.
                    double channel_spacing = Hardware.GetChannelSpacing( channel1.ID, channel2.ID, stage_id);
                    Tuple< TipWell, TipWell> tips = tip_tracker.ReserveTwoCompatibleTips( channel_spacing);
                    // dispatch tips on and tip off jobs.
                    try{
                        double angle = 0.0;
                        double y = 0.0;
                        double x1 = 0.0;
                        double x2 = 0.0;
                        BumblebeeDispatcher.f( stage, channel1, channel2, LabwareFormat.LF_STANDARD_96, tips.Item1, tips.Item2, channel_spacing, out angle, out y, out x1, out x2); //! \todo FYC get rid of hardcoded 96-well labware format.
                        // if no exception, then press on two tips simultaneously.
                        channel1.Status = Channel.ChannelStatus.PressingTip;
                        channel2.Status = Channel.ChannelStatus.PressingTip;
                        DiagnosticsDispatcher.DispatchDualTipsOnJob( new DualTipsJob( new List< Channel>{ channel1, channel2}, stage, TipBox, new List< TipWell>{ tips.Item1, tips.Item2}));
                        DiagnosticsDispatcher.DispatchTipOffJob( new TipOffJob( channel1));
                        DiagnosticsDispatcher.DispatchTipOffJob( new TipOffJob( channel2));
                    } catch{
                        // else "give back" the two reserved tips.
                        tips.Item1.SetState( TipWellState.Clean);
                        tips.Item2.SetState( TipWellState.Clean);
                    }
                }
            }
        }
        // ----------------------------------------------------------------------
        internal void LoadTipShuttle( byte shuttle_id)
        {
            byte stage_id = ( byte)( shuttle_id - 2);

            Stage tip_src = Hardware.GetStage( stage_id);
            TipShuttle tip_dst = Hardware.GetTipShuttle( shuttle_id);

            TipTracker tip_tracker = new TipTracker();
            tip_tracker.SetAll( TipWellState.Clean);

            for( int tip_shuttle_half = 0; tip_shuttle_half < 16; tip_shuttle_half += 8){
                for( int odd_even_row_index = 0; odd_even_row_index < 2; odd_even_row_index++){
                    for( int col_index = 0; col_index < 4; col_index++){
                        for( int channel_index = 0; channel_index < 4; channel_index++){
                            // get channel.
                            Channel channel = Hardware.GetChannel(( byte)( channel_index + 1 + ( shuttle_id % 2 == 0 ? 0 : 4)));
                            // pick up a tip.
                            TipWell src_tip_well = tip_tracker.ReserveOneTip();
                            DiagnosticsDispatcher.DispatchTipOnJob( new TipJob( channel, tip_src, TipBox, src_tip_well));
                            // drop off a tip.
                            TipWell dst_tip_well = tip_dst.TipCarrier.GetTipWell( tip_shuttle_half + odd_even_row_index + channel_index * 2, col_index);
                            channel.TipWell = dst_tip_well;
                            DiagnosticsDispatcher.DispatchWashableTipsOffWithMove( new TipJob( channel, tip_dst, null, null));
                        }
                    }
                }
            }
        }
        // ----------------------------------------------------------------------
        internal void UnloadTipShuttle( byte shuttle_id)
        {
            byte stage_id = ( byte)( shuttle_id - 2);

            TipShuttle tip_src = Hardware.GetTipShuttle( shuttle_id);
            Stage tip_dst = Hardware.GetStage( stage_id);

            TipTracker tip_tracker = new TipTracker();
            tip_tracker.SetAll( TipWellState.Clean);

            for( int tip_shuttle_half = 0; tip_shuttle_half < 16; tip_shuttle_half += 8){
                for( int odd_even_row_index = 0; odd_even_row_index < 2; odd_even_row_index++){
                    for( int col_index = 0; col_index < 4; col_index++){
                        for( int channel_index = 0; channel_index < 4; channel_index++){
                            // get channel.
                            Channel channel = Hardware.GetChannel(( byte)( channel_index + 1 + ( shuttle_id % 2 == 0 ? 0 : 4)));
                            // pick up a tip.
                            TipWell src_tip_well = tip_src.TipCarrier.GetTipWell( tip_shuttle_half + odd_even_row_index + channel_index * 2, col_index);
                            channel.TipWell = src_tip_well;
                            DiagnosticsDispatcher.DispatchWashableTipsOn( channel, null, null, true);
                            // drop off a tip.
                            TipWell dst_tip_well = tip_tracker.ReserveOneTip();
                            DiagnosticsDispatcher.DispatchWashableTipOffToTipBox( new TipJob( channel, tip_dst, TipBox, dst_tip_well));
                        }
                    }
                }
            }
        }
        // ----------------------------------------------------------------------
        internal void WashTips( byte shuttle_id, bool debug = false)
        {
            WashTipsJob wash_tips_job = new WashTipsJob( Hardware.GetTipShuttle( shuttle_id), debug);
            DiagnosticsDispatcher.DispatchWashTipsJob( wash_tips_job);
        }
        // ----------------------------------------------------------------------
        internal void CycleTips( byte shuttle_id)
        {
            TipShuttle tip_shuttle = Hardware.GetTipShuttle( shuttle_id);

            int CYCLE_TIMES = 8;

            for( int loop = 0; loop < CYCLE_TIMES; loop++){
                for( int odd_even_row_index = 0; odd_even_row_index < 2; odd_even_row_index++){
                    for( int c = 0; c < 8; c++){
                        TipWell tip_well = tip_shuttle.TipCarrier.GetTipWell( odd_even_row_index + c * 2, 0);
                        Channel channel = Hardware.GetChannel(( byte)( c + 1));
                        channel.TipWell = tip_well;
                    }
                    ManualResetEvent stage_available = new ManualResetEvent( false);
                    CountdownEvent offset_countdown = new CountdownEvent( 64);
                    DiagnosticsDispatcher.DispatchWashableMoveShuttle( tip_shuttle, Hardware.Channels, stage_available);
                    for( int col_index = 0; col_index < 4; col_index++){
                        for( int channel_index = 0; channel_index < 8; channel_index++){
                            TipWell tip_well = tip_shuttle.TipCarrier.GetTipWell( odd_even_row_index + channel_index * 2, col_index);
                            Channel channel = Hardware.GetChannel(( byte)( channel_index + 1));
                            channel.TipWell = tip_well;
                            DiagnosticsDispatcher.DispatchWashableTipsOn( channel, stage_available, offset_countdown, false);
                            DiagnosticsDispatcher.DispatchWashableTipsOff( channel, stage_available, offset_countdown);
                        }
                    }
                    offset_countdown.Wait();
                }
                DiagnosticsDispatcher.DispatchWashTipsJob( new WashTipsJob( tip_shuttle));
            }
        }
        // ----------------------------------------------------------------------
        #endregion
    }
}
