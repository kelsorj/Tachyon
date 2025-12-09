using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using BioNex.Exceptions;
using BioNex.Shared.BarcodeMisreadDialog;
using BioNex.Shared.BioNexGuiControls;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Microscan;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.ThreadsafeMessenger;
using BioNex.Shared.Utils;
using log4net;

[ assembly: InternalsVisibleTo( "HiveExecutor")]

namespace BioNex.Hive.Hardware
{
    public class HiveHardware
    {
        static class PropertyNames
        {
            internal const string ConfigurationFolder = "configuration folder";
            internal const string SimulateRobot = "simulate";
            internal const string SimulateBarcodeReader = "simulate BCR";
            internal const string PortRobot = "port";
            internal const string PortBarcodeReader = "barcode COM port";
            internal const string SkipBarcodeConfirmation = "skip barcode confirmation";
        }

        public const int ID_X_AXIS = 1;
        public const int ID_Z_AXIS = 3;
        public const int ID_T_AXIS = 5; // theta axis.
        public const int ID_G_AXIS = 6; // grip axis.

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        internal Configuration Config { get; private set; }
        private IDictionary< string, string> DeviceProperties { get; set; }
        internal ThreadsafeMessenger Messenger { get; private set; }
        private Dispatcher Dispatcher { get; set; }

        public TechnosoftConnection TechnosoftConnection { get; private set; } //!! trying to limit use of this property to HiveHardware and HiveExecutor, but currently exposed for diagnostic's cycler
        internal HiveTeachpointManager TeachpointManager { get; private set; }

        private delegate void ShowDialogDelegate( List<BarcodeReadErrorInfo> misread_barcode_info, List<string> unbarcoded_plate_locations,
                                                  Action gui_update_callback, UpdateInventoryLocationDelegate inventory_update_callback);

        private string ConfigFolderPath { get; set; }
        internal string Name { get; set; }

        // robot.
        public IAxis XAxis { get; set; }
        public IAxis ZAxis { get; set; }
        public IAxis TAxis { get; set; }
        public IAxis GAxis { get; set; }
        public bool SimulatingRobot { get; set; }
        private string RobotPort { get; set; }
        public HiveWorldPoint CurrentWorldPosition { get; set; }
        public HiveToolPoint CurrentToolPosition { get; set; }
        private int _speed;
        public int Speed
        {
            get{ return _speed; }
            set{
                _speed = value;
                if( Initialized){
                    XAxis.SetSpeedFactor( _speed);
                    ZAxis.SetSpeedFactor( _speed);
                    TAxis.SetSpeedFactor( _speed);
                    GAxis.SetSpeedFactor( 100);
                }
            }
        }
        public bool Initialized { get; set; }
        // barcode reader.
        private string BcrPortName { get; set; }
        public MicroscanReader BarcodeReader { get; set; }
        public bool SimulatingBarcodeReader { get; set; }
        public string LastReadBarcode { get; set; }
        public bool SkipBarcodeConfirmation { get; set; }

        private static readonly ILog Log = LogManager.GetLogger( typeof( HiveHardware));

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public HiveHardware( string device_name, Configuration config, IDictionary< string, string> device_properties, ThreadsafeMessenger messenger, Dispatcher dispatcher)
        {
            Config = config;
            DeviceProperties = device_properties;
            Messenger = messenger;
            Dispatcher = dispatcher;
            Name = device_name;

            ConfigFolderPath = DeviceProperties[ PropertyNames.ConfigurationFolder];

            TeachpointManager = new HiveTeachpointManager( ConfigFolderPath);
            Speed = 100;
            CurrentWorldPosition = new HiveWorldPoint();
            CurrentToolPosition = new HiveToolPoint();

            string temp;
            if( DeviceProperties.TryGetValue( PropertyNames.SkipBarcodeConfirmation, out temp))
                SkipBarcodeConfirmation = temp != "0";
            else
                SkipBarcodeConfirmation = false;

            BcrPortName = "COM" + int.Parse( DeviceProperties[ PropertyNames.PortBarcodeReader]);

            SimulatingBarcodeReader = false;
            try {
                SimulatingBarcodeReader = DeviceProperties[ PropertyNames.SimulateBarcodeReader] != "0";
            } catch( KeyNotFoundException) {
                // it's possible that the key for barcode simulation isn't available.  If not, then
                // just assume that we don't want to simulate.
            }

            RobotPort = DeviceProperties[ PropertyNames.PortRobot];
            SimulatingRobot = false;
            try{
                SimulatingRobot = DeviceProperties[ PropertyNames.SimulateRobot] != "0";
            } catch( KeyNotFoundException){
            }
            Initialized = false;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public void Connect()
        {
            TechnosoftConnection = ( SimulatingRobot)
                ? new TechnosoftConnection()
                : new TechnosoftConnection( RobotPort, TMLLibConst.CHANNEL_SYS_TEC_USBCAN, 500000);
            Initialized = true;
        }
        // ----------------------------------------------------------------------
        public void ConnectBcr()
        {
            if( !SimulatingBarcodeReader){
                BarcodeReader = new MicroscanReader();
                try {
                    BarcodeReader.Connect( BcrPortName);
                    string config_filepath = DeviceProperties[ PropertyNames.ConfigurationFolder] + "\\barcode_reader_configuration.xml";
                    BarcodeReader.LoadConfigurationDatabase( config_filepath, true);
                } catch (Exception ) {
                    // do nothing, assume no barcode reader
                    Log.Info( "No barcode reader present on " + Name);
                }
            }
        }
        // ----------------------------------------------------------------------
        public void Disconnect()
        {
            if(( TechnosoftConnection != null) && TechnosoftConnection.Connected){
                TechnosoftConnection.Close();
            }
            TechnosoftConnection = null;
            Initialized = false;
        }
        // ----------------------------------------------------------------------
        public void DisconnectBcr()
        {
            if( BarcodeReader != null){
                BarcodeReader.Close();
            }
            BarcodeReader = null;
        }
        // ----------------------------------------------------------------------
        public void LoadMotorSettings()
        {
            // load the motor and hardware configuration files into the setup dialog
            string motor_settings_path = ( ConfigFolderPath + "\\hive_motor_settings.xml").ToAbsoluteAppPath();
            string tsm_setup_folder = ConfigFolderPath;
            TechnosoftConnection.LoadConfiguration( motor_settings_path, tsm_setup_folder);
            TechnosoftConnection.SetBroadcastMasterAxisID( HiveHardware.ID_G_AXIS);
            TechnosoftConnection.SetGroupMasterAxisID( 1, HiveHardware.ID_G_AXIS);
            TechnosoftConnection.SetGroupMasterAxisID( 2, HiveHardware.ID_G_AXIS);
            TechnosoftConnection.SetGroupMasterAxisID( 3, HiveHardware.ID_G_AXIS);
            TechnosoftConnection.SetGroupMasterAxisID( 4, HiveHardware.ID_G_AXIS);
            TechnosoftConnection.SetGroupMasterAxisID( 5, HiveHardware.ID_G_AXIS);
            TechnosoftConnection.SetGroupMasterAxisID( 6, HiveHardware.ID_G_AXIS);
            TechnosoftConnection.SetGroupMasterAxisID( 7, HiveHardware.ID_G_AXIS);
            TechnosoftConnection.SetGroupMasterAxisID( 8, HiveHardware.ID_G_AXIS);

            XAxis = TechnosoftConnection.GetAxes()[ ID_X_AXIS];
            ZAxis = TechnosoftConnection.GetAxes()[ ID_Z_AXIS];
            TAxis = TechnosoftConnection.GetAxes()[ ID_T_AXIS];
            GAxis = TechnosoftConnection.GetAxes()[ ID_G_AXIS];

            XAxis.SetSpeedFactor( Speed);
            ZAxis.SetSpeedFactor( Speed);
            TAxis.SetSpeedFactor( Speed);
            GAxis.SetSpeedFactor( 100); // always want this at 100%
        }
        // ----------------------------------------------------------------------
        public void Reset()
        {
            if( TechnosoftConnection != null){
                TechnosoftConnection.ResetPauseAbort();
            }
        }
        // ----------------------------------------------------------------------
        public void Abort()
        {
            if( TechnosoftConnection != null){
                TechnosoftConnection.Abort();
            }
        }
        // ----------------------------------------------------------------------
        public void Pause()
        {
            if( TechnosoftConnection != null){
                TechnosoftConnection.Pause();
            }
        }
        // ----------------------------------------------------------------------
        public void Resume()
        {
            if( TechnosoftConnection != null){
                TechnosoftConnection.Resume();
            }
        }
        // ----------------------------------------------------------------------
        #region deprecated
        // DKM 2012-02-23 removed because buddies have been replaced by the TML-MT lib
        /*
        private List< IAxis> BuddyAxes { get; set; }
        // ----------------------------------------------------------------------
        public List< IAxis> LoadBuddyConfiguration( string buddy_name, bool simulate, string motor_settings_path, string tsm_setup_folder)
        {
            // call the TechnosoftConnection's method for loading a buddy configuration
            BuddyAxes = TechnosoftConnection.LoadBuddyConfiguration( buddy_name, simulate, motor_settings_path, tsm_setup_folder);
            return BuddyAxes;
        }
        // ----------------------------------------------------------------------
        public void AbortBuddy()
        {
            foreach( IAxis axis in BuddyAxes){
                axis.Abort();
            }
        }
        // ----------------------------------------------------------------------
        public void CloseBuddyConnection( string buddy_name)
        {
            TechnosoftConnection.CloseBuddyConnection( buddy_name);
        }
        // ----------------------------------------------------------------------
        public void ResetPauseAbort()
        {
            foreach( IAxis axis in BuddyAxes){
                axis.ResetPause();
                axis.ResetAbort();
            }
        }
         */
        #endregion
        // ----------------------------------------------------------------------
        public void TMLLibCheckUnrequestedMessages()
        {
            TechnosoftConnection.TMLLibCheckUnrequestedMessages();
        }
        // ----------------------------------------------------------------------
        public long GetAxisPositionCounts( byte axis_id)
        {
            return TechnosoftConnection.GetAxes()[ axis_id].GetPositionCounts();
        }
        // ----------------------------------------------------------------------
        /* not used
        public bool IsRobotOn()
        {
            // true if no axes are off.
            return TechnosoftConnection.GetAxes().Values.Count( axis => !axis.IsOn()) == 0;
        }
        */
        // ----------------------------------------------------------------------
        public void StopRobot()
        {
            TechnosoftConnection.StopAllAxes();
        }
        // ----------------------------------------------------------------------
        public bool BusVoltageOk
        {
            get{
                try{
                    short ad4;
                    XAxis.GetIntVariable( "AD4", out ad4);
                    const double vdc_max_measurable = 107.8; // 107.8 Vdc max measurable on IDM640-8EI
                    const double Kuf_m = 65472 / vdc_max_measurable; // (bits/Volts) Formula from Page 849 of MackDaddyTechnosoftDoc
                    double voltage = ((double)((UInt16)(ad4 + 65535)) / Kuf_m);
                    Log.DebugFormat( "Bus voltage: {0}", voltage);
                    return voltage > 70;
                } catch( Exception){
                    return false;
                }
            }
        }
        // ----------------------------------------------------------------------
        public void ResetAndSetHomeStatus( short new_home_status)
        {
            XAxis.ResetDrive();
            XAxis.SetIntVariable( "homing_status", new_home_status);
            ZAxis.ResetDrive();
            ZAxis.SetIntVariable( "homing_status", new_home_status);
        }
        // ----------------------------------------------------------------------
        public bool IsThetaTucked()
        {
            // we need to check here to see if we're even connected to the controller, because
            // the GUI is going to call CanExecute on all of the elements when it's first created,
            // and _ts is going to be null until we actually make a connection.
            if( TechnosoftConnection == null)
                return false;
            double theta_pos = TAxis.GetPositionMM();
            if( theta_pos < Config.ThetaSafe)
                return true;
            return Math.Abs( theta_pos - Config.ThetaSafe) < 0.5;
        }
        // ----------------------------------------------------------------------
        public void UpdateCurrentPosition()
        {
            CurrentWorldPosition.X = XAxis.GetPositionMM();
            CurrentWorldPosition.Z = ZAxis.GetPositionMM();
            CurrentWorldPosition.T = TAxis.GetPositionMM();
            CurrentWorldPosition.G = GAxis.GetPositionMM();

            CurrentToolPosition.ConvertFromHiveWorldPoint( CurrentWorldPosition, Config.ArmLength, Config.FingerOffsetZ);
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// strobes the barcode reader where the robot is currently at and confirms that it matches the passed in value
        /// </summary>
        /// <returns></returns>
        public bool ConfirmBarcode( string expected_barcode, out string read_barcode, int bcr_config_index = 0)
        {
            // check for DEVICE simulation
            if( SimulatingRobot) {
                Log.InfoFormat( "{0} is simulating so automatically assuming that the barcode matches", Name);
                read_barcode = expected_barcode;
                return true;
            }

            // check for real device with BARCODE READER simulation
            if( SimulatingBarcodeReader) {
                read_barcode = expected_barcode;
                return true;
            }

            if( BarcodeReader != null)
                BarcodeReader.LoadConfigurationIndex( bcr_config_index);
            read_barcode = BarcodeReader.Read();
            
            // we want to be able to read barcodes sometimes, yet not flag errors (e.g. upstack in monsanto system), so
            // use the dummy code "!STROBE!" to read the barcode, save it, and then bail before validation
            if( Constants.IsStrobe( expected_barcode) && !Constants.IsNoRead( read_barcode))
                return true;

            // the following logic will handle the case where we forced a strobe with !STROBE! and got a NOREAD in return
            bool matches = string.IsNullOrEmpty( expected_barcode) || read_barcode == expected_barcode;
            if( matches) {
                Log.InfoFormat( "{0} confirmed expected barcode '{1}'", Name, expected_barcode);
            } else {
                if( Constants.IsStrobe(expected_barcode) && Constants.IsNoRead( read_barcode))
                    Log.InfoFormat( "{0} expected a barcode, but could not read one", Name);
                else
                    Log.InfoFormat( "{0} expected barcode '{1}', but instead read '{2}'", Name, expected_barcode, read_barcode);
            }
            return matches;
        }
        // ----------------------------------------------------------------------
        public void LoadTeachpoints( AccessibleDeviceInterface accessible_device)
        {
            TeachpointManager.LoadTeachpoints( accessible_device);
        }
        // ----------------------------------------------------------------------
        public void SaveTeachpoints( AccessibleDeviceInterface accessible_device)
        {
            TeachpointManager.SaveTeachpoints( accessible_device);
        }
        // ----------------------------------------------------------------------
        public IDictionary< string, IList< string>> GetTeachpointNames()
        {
            return TeachpointManager.GetTeachpointNames();
        }
        // ----------------------------------------------------------------------
        public IList< string> GetTeachpointNames( string device_name)
        {
            return TeachpointManager.GetTeachpointNames( device_name);
        }
        // ----------------------------------------------------------------------
        public HiveTeachpoint GetTeachpoint( string device_name, string teachpoint_name)
        {
            return TeachpointManager.GetTeachpoint( device_name, teachpoint_name);
        }
        // ----------------------------------------------------------------------
        public void SetTeachpoint( string device_name, HiveTeachpoint teachpoint)
        {
            TeachpointManager.SetTeachpoint( device_name, teachpoint);
        }

        // ----------------------------------------------------------------------
        // internal methods.
        // ----------------------------------------------------------------------
        internal void HomeX( bool wait_for_complete)
        {
            XAxis.Home( wait_for_complete);
        }
        // ----------------------------------------------------------------------
        internal void HomeZ( bool wait_for_complete)
        {
            ZAxis.Home( wait_for_complete);
        }
        // ----------------------------------------------------------------------
        internal void HomeT( bool wait_for_complete)
        {
            TAxis.Home( wait_for_complete);
        }
        // ----------------------------------------------------------------------
        internal void HomeG( bool wait_for_complete)
        {
            GAxis.Home( wait_for_complete);
        }
        // ----------------------------------------------------------------------
        internal void JogX( double increment_mm, bool positive)
        {
            UpdateCurrentPosition();
            double x = CurrentWorldPosition.X + ( positive ? increment_mm : -increment_mm);
            XAxis.MoveAbsolute( x);
        }
        //---------------------------------------------------------------------
        internal void JogZ( double increment_mm, bool positive)
        {
            UpdateCurrentPosition();
            double z = CurrentWorldPosition.Z + ( positive ? increment_mm : -increment_mm);
            ZAxis.MoveAbsolute( z);
        }
        //---------------------------------------------------------------------
        internal void JogT( double increment_mm, bool positive)
        {
            UpdateCurrentPosition();
            double t = CurrentWorldPosition.T + ( positive ? increment_mm : -increment_mm);
            TAxis.MoveAbsolute( t);
        }
        //---------------------------------------------------------------------
        internal void JogG( double increment_mm, bool positive)
        {
            UpdateCurrentPosition();
            double g = CurrentWorldPosition.G + ( positive ? increment_mm : -increment_mm);
            GAxis.MoveAbsolute( g);
        }
        //---------------------------------------------------------------------
        internal void JogY( double increment_mm, bool positive)
        {
            UpdateCurrentPosition();
            double y = CurrentToolPosition.Y + ( positive ? increment_mm : -increment_mm);
            if( y >= Config.MaxY){
                y = Config.MaxY;
            }
            if( y <= HiveMath.GetYFromTheta( Config.ArmLength, Config.ThetaSafe)){
                y = HiveMath.GetYFromTheta( Config.ArmLength, Config.ThetaSafe);
            }
            MoveY( HiveMath.GetThetaFromY( Config.ArmLength, y));
        }
        //---------------------------------------------------------------------
        internal void MoveX( double position_mm)
        {
            XAxis.MoveAbsolute( position_mm);
        }
        //---------------------------------------------------------------------
        internal void MoveZ( double position_mm)
        {
            ZAxis.MoveAbsolute( position_mm);
        }
        //---------------------------------------------------------------------
        internal void MoveT( double position_degree)
        {
            TAxis.MoveAbsolute( position_degree);
        }
        //---------------------------------------------------------------------
        internal void MoveG( double position_mm)
        {
            GAxis.MoveAbsolute( position_mm);
        }
        //---------------------------------------------------------------------
        internal void MoveY( double theta_dst)
        {
            IDictionary< IAxis, double> init_pos = TechnosoftConnection.GetAxes().Values.ToDictionary( axis => axis, axis => axis.GetPositionMM());
            HiveMultiAxisTrajectory hive_multi_axis_trajectory = new HiveMultiAxisTrajectory( this, init_pos, new Dictionary< IAxis, double>());
            hive_multi_axis_trajectory.AddWaypoint( 0, dst_y: HiveMath.GetYFromTheta( Config.ArmLength, theta_dst));
            hive_multi_axis_trajectory.GeneratePVTPoints();
            IAxis.ExecuteCoordinatedPVTTrajectory( ( byte)7, hive_multi_axis_trajectory);
        }
        // ----------------------------------------------------------------------
        internal void MoveXZ( double position_x, double position_z, bool use_tool_space)
        {
            // if using world space, convert position_z to tool space (which is what HiveMultiAxisTrajectory works in).
            if( !use_tool_space){
                UpdateCurrentPosition();
                position_z = HiveMath.ConvertTZWorldToYZTool( Config.ArmLength, Config.FingerOffsetZ, CurrentWorldPosition.T, position_z).Item2;
            }
            IDictionary< IAxis, double> init_pos = TechnosoftConnection.GetAxes().Values.ToDictionary( axis => axis, axis => axis.GetPositionMM());
            HiveMultiAxisTrajectory hive_multi_axis_trajectory = new HiveMultiAxisTrajectory( this, init_pos, new Dictionary< IAxis, double>());
            hive_multi_axis_trajectory.AddWaypoint( 0, dst_x: position_x, dst_z: position_z);
            hive_multi_axis_trajectory.GeneratePVTPoints();
            IAxis.ExecuteCoordinatedPVTTrajectory( ( byte)7, hive_multi_axis_trajectory);
        }
        // ----------------------------------------------------------------------
        internal void TuckToXZ( double position_x, double position_z, bool use_tool_space)
        {
            // if using world space, convert position_z to tool space (which is what HiveMultiAxisTrajectory works in).
            if( !use_tool_space){
                UpdateCurrentPosition();
                position_z = HiveMath.ConvertTZWorldToYZTool( Config.ArmLength, Config.FingerOffsetZ, CurrentWorldPosition.T, position_z).Item2;
            }
            IDictionary< IAxis, double> init_pos = TechnosoftConnection.GetAxes().Values.ToDictionary( axis => axis, axis => axis.GetPositionMM());
            HiveMultiAxisTrajectory hive_multi_axis_trajectory = new HiveMultiAxisTrajectory( this, init_pos, new Dictionary< IAxis, double>());
            hive_multi_axis_trajectory.AddWaypoint( 0, dst_y: HiveMath.GetYFromTheta( Config.ArmLength, Config.ThetaSafe));
            hive_multi_axis_trajectory.AddWaypoint( 0, dst_x: position_x, dst_z: position_z);
            hive_multi_axis_trajectory.GeneratePVTPoints();
            IAxis.ExecuteCoordinatedPVTTrajectory( ( byte)7, hive_multi_axis_trajectory);
        }
        // ----------------------------------------------------------------------
        internal void TuckY()
        {
            if( IsThetaTucked()){
                return;
            }
            MoveY( Config.ThetaSafe);
        }
        // ----------------------------------------------------------------------
        internal void Park()
        {
            TuckToXZ( Config.ParkPositionX, Config.ParkPositionZ, false);
        }
        //---------------------------------------------------------------------
        /// <remarks>
        /// Warning: This is generally a bad function to call, unless you know what you are calling. Are you sure you don't want TuckY()?
        /// </remarks>
        internal void TuckTheta()
        {
            UpdateCurrentPosition();
            TAxis.MoveAbsolute( Config.ThetaSafe);
        }
        //---------------------------------------------------------------------
        internal void EnableAllAxes()
        {
            TechnosoftConnection.EnableAllAxes();
        }
        //---------------------------------------------------------------------
        /// <remarks>
        /// GUI handler should catch the exceptions thrown from this method
        /// </remarks>
        /// <exception cref="AxisException" />
        /// <param name="axis_id"></param>
        internal void EnableAxis( byte axis_id)
        {
            TechnosoftConnection.GetAxes()[ axis_id].Enable( true, true);
        }
        // ----------------------------------------------------------------------
        internal void DisableAllAxes()
        {
            DisableAxis( HiveHardware.ID_X_AXIS);
            DisableAxis( HiveHardware.ID_Z_AXIS);
            // never disable theta axis!
            // DisableAxis( HiveHardware.ID_T_AXIS);
            DisableAxis( HiveHardware.ID_G_AXIS);
        }
        // ----------------------------------------------------------------------
        internal void DisableAxis( byte axis_id)
        {
            TechnosoftConnection.GetAxes()[axis_id].Enable( false, true);
        }
        // ----------------------------------------------------------------------
        internal void MoveTeachingJigToTeachpoint( HiveTeachpoint tp)
        {
            // always start from the teachpoint, and calculate every Z move up front in worldspace.
            double z_tp_world = HiveMath.ConvertZToolToWorldUsingY( Config.ArmLength, Config.FingerOffsetZ, tp.Z, tp.Y);
            // when going down to the final Z position, make sure to use the approach_height if we're gripping a plate!
            double theta_at_tp_y = HiveMath.GetThetaFromY( Config.ArmLength, tp.Y);
            UpdateCurrentPosition();
            MoveY( Config.ThetaKungFu);

            // DKM 2011-03-26 figure out the Z world position we need to be at to PVT Y in at the approach height
            double z_delta_between_current_and_tp = Config.ArmLength * (Math.Cos((Math.PI * Config.ThetaKungFu / 180.0)) - Math.Cos((Math.PI * theta_at_tp_y / 180.0)));
            double z_moveto_start_world = z_tp_world + z_delta_between_current_and_tp;

            MotorSettings x_settings = XAxis.Settings;
            MotorSettings z_settings = ZAxis.Settings;
            // should do these together
            { 
                // X: move into pad position
                XAxis.MoveAbsolute( tp.X, x_settings.Velocity / 2, x_settings.Acceleration / 2, wait_for_move_complete: false);
                ZAxis.MoveAbsolute( z_moveto_start_world, z_settings.Velocity / 4, z_settings.Acceleration / 4);
                // this is a problem -- if X is way at the end of travel, it will not be done with its
                // move by the time we recommand it on the next line
                XAxis.MoveAbsolute( tp.X, x_settings.Velocity / 2, x_settings.Acceleration / 2);
            }

            int original_speed = Speed;
            try{
                Speed = 10;
                MoveY( theta_at_tp_y);
            } finally{
                Speed = original_speed;
            }
        }
        // ----------------------------------------------------------------------
        internal void MoveToDeviceLocationForBCRStrobe( AccessibleDeviceInterface device_to_move_to, string location_name, bool with_plate)
        {
            HiveTeachpoint teachpoint = GetTeachpoint( device_to_move_to.Name, location_name);
            XAxis.MoveAbsolute( teachpoint.X, wait_for_move_complete: false);
            double z_robot_pos = HiveMath.ConvertZToolToWorldUsingY( Config.ArmLength, Config.FingerOffsetZ, teachpoint.Z, teachpoint.Y);
            z_robot_pos += 10.0; // set the trigger position to 10.0mm above teachpoints
            double z_max_limit = ZAxis.Settings.MaxLimit;
            if( z_robot_pos > z_max_limit)
                z_robot_pos = z_max_limit;
            ZAxis.MoveAbsolute( z_robot_pos, wait_for_move_complete: false);
            XAxis.MoveAbsolute( teachpoint.X);
            ZAxis.MoveAbsolute( z_robot_pos);

            // DKM 2011-05-23 I am wrestling with putting the barcode configuration database switching
            //                at the most logical points in the code, but this seems like the best
            //                place to do it.
            if( BarcodeReader != null)
                BarcodeReader.LoadConfigurationIndex( device_to_move_to.GetBarcodeReaderConfigurationIndex( location_name));
        }

        // ----------------------------------------------------------------------
        // placeholder methods.
        // ----------------------------------------------------------------------
        public string ReadBarcode()
        {
            // #409 remove null chars from incoming stream, even though we shouldn't have had any
            //      in the first place (we purge the incoming rx buffer before reading)
            char[] remove_chars = new char[] { '\0' };
            if( BarcodeReader == null && !SimulatingBarcodeReader)
                return "";
            if( SimulatingBarcodeReader){
                string barcode = ( string)Dispatcher.Invoke( new Func< string>( () =>
                {
                    // if we couldn't read the cart, here is where we'll prompt the user to enter
                    // a barcode manually, and then set the barcode in the controller.
                    string manual_barcode;
                    UserInputDialog dlg = new UserInputDialog( "Please enter barcode manually", "Please enter the desired barcode below:");
                    dlg.ShowInTaskbar = true;
                    MessageBoxResult response = dlg.PromptUser( out manual_barcode);
                    if( response == MessageBoxResult.Cancel){
                        throw new BarcodeException( "User clicked Cancel");
                    }
                    return manual_barcode;
                }));
                return barcode;
            }
            return BarcodeReader.Read().TrimStart( remove_chars).TrimEnd( remove_chars);
        }
        // ----------------------------------------------------------------------
        public string ReadBarcode( double position_x, double position_y, int bcr_config_index = 0)
        {
            try {
                TuckToXZ( position_x, position_y, false);
                if( BarcodeReader != null){
                    BarcodeReader.LoadConfigurationIndex( bcr_config_index);
                }
                return ReadBarcode();
            } catch( Exception ex) {
                string message = String.Format( "Could not read cart identifier at Dock '{0}': {1}", Name, ex.Message);
                Log.Error( message);
                throw new BarcodeException( message);
            }
        }
        // ----------------------------------------------------------------------
        public string SaveBarcodeImage( string filepath)
        {
            if( BarcodeReader != null) {
                return BarcodeReader.SaveImage( filepath, MicroscanCommands.ImageFormat.JPEG, 40);
            }
            return "";
        }
        // ----------------------------------------------------------------------
        public void HandleBarcodeMisreads( List< BarcodeReadErrorInfo> misread_barcode_info, List< string> unbarcoded_plate_locations, Action gui_update_callback, UpdateInventoryLocationDelegate inventory_update_callback)
        {
            if( Dispatcher == null) {
                const string error = "Could not display manual barcode entry dialog";
                Log.Info( error);
                MessageBox.Show( error);
                return;
            }

            Dispatcher.Invoke( new ShowDialogDelegate( ShowBarcodeMisreadsDialog), misread_barcode_info, unbarcoded_plate_locations, gui_update_callback, inventory_update_callback);
        }

        /// <summary>
        /// Displays the barcode dialog so users can manually enter barcodes.  Also modifies the
        /// passed-in List of PlateLocations so this dialog can modify the list of unbarcoded plates.
        /// </summary>
        /// <param name="misread_barcode_info"></param>
        /// <param name="unbarcoded_plate_locations"></param>
        /// <param name="gui_update_callback"></param>
        /// <param name="inventory_update_callback"></param>
        private static void ShowBarcodeMisreadsDialog( List<BarcodeReadErrorInfo> misread_barcode_info, List<string> unbarcoded_plate_locations,
                                                Action gui_update_callback, UpdateInventoryLocationDelegate inventory_update_callback)
        {
            BarcodeMisread dlg = new BarcodeMisread( misread_barcode_info);
            dlg.ShowDialog();
            try {
                // now that we have the user-entered barcodes, go ahead and update inventory accordingly
                foreach( BarcodeReadErrorInfo info in dlg.Barcodes) {
                    if( !info.NoPlatePresent && info.NewBarcode.Trim() != "") {
                        if( unbarcoded_plate_locations != null)
                            unbarcoded_plate_locations.Remove( info.TeachpointName);
                        inventory_update_callback( info.TeachpointName, info.NewBarcode.Trim());
                    } else if( info.NoPlatePresent) {
                        unbarcoded_plate_locations.Remove( info.TeachpointName);
                        inventory_update_callback( info.TeachpointName, BioNex.Shared.LibraryInterfaces.Constants.Empty);
                    }
                }
                // DKM 2011-06-16 added null check to support using the dialog during pick and place
                if( gui_update_callback != null)
                    gui_update_callback();
            } catch( Exception ex) {
                MessageBox.Show( String.Format( "Could not save the manually-entered barcodes: {0}\r\nPlease reinventory the device.", ex.Message));
            }
        }
    }
}
