using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BioNex.Hig;
using BioNex.HiGIntegration.StateMachines;
using BioNex.Shared.IError;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.Utils;
using log4net;
using BioNex.Hig.StateMachines;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using log4net.Repository.Hierarchy;
using log4net.Appender;
using log4net.Layout;
using System.Xml;
using System.Xml.Linq;
using System.IO;

[assembly: InternalsVisibleTo("HiGIntegrationTestApp")]
namespace BioNex.HiGIntegration
{
    /// <summary>
    /// This is the class that integrators will instantiate to control the HiG hardware
    /// </summary>
    public sealed class HiG : HiGInterface, IHigModel, INotifyPropertyChanged
    {
        public class ErrorEventArgs : EventArgs
        {
            public String Reason { get; private set; }
            
            public ErrorEventArgs( String reason)
            {
                Reason = reason;
            }
        }

        public class SpinCompleteEventArgs : EventArgs
        {
            public double AccelTimeInSeconds { get; private set; }
            public double CruiseTimeInSeconds { get; private set; }
            public double DecelTimeInSeconds { get; private set; }

            public SpinCompleteEventArgs( double accel_time_s, double cruise_time_s, double decel_time_s)
            {
                AccelTimeInSeconds = accel_time_s;
                CruiseTimeInSeconds = cruise_time_s;
                DecelTimeInSeconds = decel_time_s;
            }
        }

        // Events
        public event EventHandler InitializeComplete;
        public event EventHandler InitializeError;
        public event EventHandler HomeComplete;
        public event EventHandler HomeError;
        public event EventHandler OpenShieldComplete;
        public event EventHandler OpenShieldError;
        public event EventHandler SpinComplete;
        public event EventHandler SpinError;
        public event EventHandler SpinTimeRemainingUpdated;
        public event EventHandler ForcedDisconnection;
        public event EventHandler DiagnosticsClosed;

        // Properties for execution state
        /// <summary>
        /// ExeuctionState allows the user to perform diagnostics-level stuff after
        /// spinning and encountering an error.
        /// </summary>
        private readonly ExecutionState _execution_state;
        public bool Homing { get { return _execution_state.Homing; } }
        public bool InErrorState { get { return _execution_state.InErrorState; } }
        public bool Spinning { get { return _execution_state.Spinning; } }
        public bool Idle { get { return _execution_state.Idle; } }
        public bool IsHomed
        {
            get {
                if( ShieldAxis == null || SpindleAxis == null || _connected == false)
                    return false;
                return ShieldAxis.IsHomed && SpindleAxis.IsHomed;
            }
        }
        public bool IsConnected
        {
            get {
                return _connected;
            }
        }

        // Technosoft stuff
        private TechnosoftConnection _ts;
        private readonly string _motor_settings_base_path = BioNex.Shared.Utils.FileSystem.GetModulePath() + "\\config";
        // DKM 2012-01-18 need to append the device name before using!  This prevents multiple instances from simultaneously clobbering
        //                each other's .t.zip files.  Also, this allows users to run different versions of firmware on the same system
        //                without forcing them to first upgrade all devices.
        private readonly string _tsm_setup_base_path = BioNex.Shared.Utils.FileSystem.GetModulePath() + "\\config";
        private IAxis _spindle_axis;
        private IAxis _shield_axis;
        private bool _connected;
        private double _rotational_radius_mm = 110; 
        
        private Diagnostics _diagnostics_window;
        public bool Simulating { get; private set; } // set in Initialize
        private IntegrationSpinStateMachine _spin_sm;
        // used to track times for spin accel, cruise, and decel
        private double _accel_time_s;
        private double _cruise_time_s;
        private double _decel_time_s;

        private ThreadedUpdates _updater { get; set; }
        internal bool Busy
        {
            get {
                return !_execution_state.Idle;
            }
        }

        // DKM 2011-11-14 don't create the logger yet -- want to use the device name later
        private ILog _log;// = LogManager.GetLogger(typeof(HiG));

        // IHigModel implementation
        #region IHigModel implementation

        public IAxis SpindleAxis { get { return _spindle_axis; } }
        public IAxis ShieldAxis { get { return _shield_axis; } }
        public double SpindleTemperature { get { return 0; } }
        public string Name { get; private set; }
        public void AddError( ErrorData error) {}
        public string InternalSerialNumber { get; private set; }

        private double _spin_sec_remaining;
        private double _last_spin_sec_remaining;
        public double SpinSecRemaining {
            get { return _spin_sec_remaining; }
            set {
                _spin_sec_remaining = value > 0 ? value : 0;
                if( _spin_sec_remaining != _last_spin_sec_remaining) {
                    _last_spin_sec_remaining = _spin_sec_remaining;
                    if( SpinTimeRemainingUpdated != null)
                        SpinTimeRemainingUpdated( this, new SpinTimeUpdatedEventArgs( _spin_sec_remaining));
                }
                OnPropertyChanged( "SpinSecRemaining");
            }
        }
        public IList<EepromSetting> GetEepromSettings( bool default_values_only=false) { return new List<EepromSetting>(); }

        // HiG integration doesn't need a dispatcher for error handling since it uses events
        public Dispatcher MainDispatcher { get { return null; }}

        public bool CycleDoorOnly { get { return false; } }

        private int _shield_open_pos;
        public int ShieldOpenPosition
        {
            get { return _shield_open_pos; }
            set {
                _shield_open_pos = value;
                _shield_axis.WriteLongVarEEPROM( "door_open_pos_ptr", value);
            }
        }

        private int _shield_closed_pos;
        public int ShieldClosedPosition
        {
            get { return _shield_closed_pos; }
            set {
                _shield_closed_pos = value;
                _shield_axis.WriteLongVarEEPROM( "door_close_pos_ptr", value);
            }
        }

        private short _bucket2_offset;
        public short Bucket2Offset
        {
            get { return _bucket2_offset; }
            set {
                _bucket2_offset = value;
                _spindle_axis.WriteIntVarEEPROM("bucket2_offset_ptr", value);
            }
        }

        private int _bucket1_position;
        public int Bucket1Position
        {
            get { return _bucket1_position; }
            set {
                _bucket1_position = value;
                _spindle_axis.WriteLongVarEEPROM("my_homepos_ptr", value);
            }
        }

        private int _imbalance_threshold;
        public int ImbalanceThreshold
        {
            get { return _imbalance_threshold; }
            set {
                _imbalance_threshold = value;
                _spindle_axis.WriteIntVarEEPROM( "imb_ampl_max_ptr", (short)value);
            }
        }

        public bool SupportsImbalance { get { return SpindleAxis != null ? SpindleAxis.FirmwareMinorVersion >= 5 : false; } }
        public bool NoShieldMode { get { return false; } }

        #endregion

        public double TimeRemainingSec { get { return SpinSecRemaining; } }

        public double GetEstimatedCycleTime( double gs, double accel_percent, double decel_percent, double time_seconds)
        {
            if( SpindleAxis == null)
                return 0;

            try {
                return HigUtils.GetEstimatedCycleTime(gs, _rotational_radius_mm, SpindleAxis.Settings.Acceleration, accel_percent,
                                                       SpindleAxis.Settings.Acceleration, decel_percent, time_seconds);
            } catch( Exception) {
                return 0;
            }
        }

        public bool UpdateShieldFirmware(string path)
        {
            return UpdateFirmware(ShieldAxis, path);
        }

        public bool UpdateSpindleFirmware(string path)
        {
            return UpdateFirmware(SpindleAxis, path);
        }

        private static bool UpdateFirmware(IAxis which_axis, string path)
        {
            TSAxis axis = which_axis as TSAxis;
            if (axis == null)
                return false;
            try
            {
                axis.DownloadSwFile(path);
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                System.IO.File.Delete(path);
                axis.ResetDrive();
                Thread.Sleep(2000);
            }
            return true;
        }

        #region Diagnostics GUI properties and commands

        private void UpdateEstimatedCycleTime()
        {
            EstimatedCycleTimeSeconds = GetEstimatedCycleTime( DesiredGs, Accel, Decel, SpinTimeSeconds);                
        }

        private int _current_rpm;
        public int CurrentRpm
        {
            get { return _current_rpm; }
            set {
                _current_rpm = value;
                OnPropertyChanged( "CurrentRpm");
            }
        }

        private double _current_gs;
        public double CurrentGs
        {
            get { return _current_gs; }
            set
            {
                _current_gs = value;
                OnPropertyChanged( "CurrentGs");
            }
        }

        private int _accel;
        public int Accel
        {
            get { return _accel; }
            set {
                _accel = value;
                OnPropertyChanged( "Accel");
                UpdateEstimatedCycleTime();
            }
        }

        private int _decel;
        public int Decel
        {
            get { return _decel; }
            set {
                _decel = value;
                OnPropertyChanged( "Decel");
                UpdateEstimatedCycleTime();
            }
        }

        private int _desired_gs;
        public int DesiredGs
        {
            get { return _desired_gs; }
            set {
                _desired_gs = value;
                OnPropertyChanged( "DesiredGs");
                UpdateEstimatedCycleTime();
            }
        }

        private int _time_s;
        public int SpinTimeSeconds
        {
            get { return _time_s; }
            set {
                _time_s = value;
                OnPropertyChanged( "SpinTimeSeconds");
                UpdateEstimatedCycleTime();
            }
        }

        private Brush _at_bucket1_color;
        public Brush AtBucket1Color
        {
            get { return _at_bucket1_color; }
            set {
                _at_bucket1_color = value;
                OnPropertyChanged( "AtBucket1Color");
            }
        }

        private Brush _at_bucket2_color;
        public Brush AtBucket2Color
        {
            get { return _at_bucket2_color; }
            set {
                _at_bucket2_color = value;
                OnPropertyChanged( "AtBucket2Color");
            }
        }

        private double _estimated_cycle_time_s;
        public double EstimatedCycleTimeSeconds
        {
            get { return _estimated_cycle_time_s; }
            set {
                _estimated_cycle_time_s = value;
                OnPropertyChanged( "EstimatedCycleTimeSeconds");
            }
        }
        
        public SimpleRelayCommand SpinCommand { get; set; }
        public SimpleRelayCommand AbortSpinCommand { get; set; }
        public SimpleRelayCommand HomeCommand { get; set; }
        public SimpleRelayCommand CloseDoorCommand { get; set; }
        public SimpleRelayCommand OpenDoorToBucket1Command { get; set; }
        public SimpleRelayCommand OpenDoorToBucket2Command { get; set; }
        
        #endregion

        public HiG()
        {
            Accel = 100;
            Decel = 100;
            SpinTimeSeconds = 10;
            DesiredGs = 250;

            InitializeDiagnosticsCommands();

            _execution_state = new ExecutionState();
        }

        private void InitializeDiagnosticsCommands()
        {
            SpinCommand = new SimpleRelayCommand( () => {
                Task.Factory.StartNew( () => {
                    bool old_blocking = Blocking;
                    Blocking = true;
                    try {
                        // this is a little weird looking, but it's because I made HiG the datacontext for this GUI
                        Spin( DesiredGs, Accel, Decel, SpinTimeSeconds);
                        MessageBox.Show( "(Diagnostics) spin complete");
                    } catch( Exception ex) {
                        MessageBox.Show( "(Diagnostics) spin error: " + ex.Message);
                    } finally {
                        Blocking = old_blocking;
                    }
                });

            }, () => { return Idle; });

            AbortSpinCommand = new SimpleRelayCommand( () => { AbortSpin(); });

            OpenDoorToBucket1Command = new SimpleRelayCommand( () => {
                Task.Factory.StartNew( () => {
                    bool old_blocking = Blocking;
                    Blocking = true;
                    try {
                        OpenShield( 0);
                        MessageBox.Show( "(Diagnostics) open shield complete");
                    } catch( Exception ex) {
                        MessageBox.Show( "(Diagnostics) open shield error: " + ex.Message);
                    } finally {
                        Blocking = old_blocking;
                    }
                });
            }, () => { return Idle; });
            OpenDoorToBucket2Command = new SimpleRelayCommand( () => {
                Task.Factory.StartNew( () => {
                    bool old_blocking = Blocking;
                    Blocking = true;
                    try {
                        OpenShield( 1);
                        MessageBox.Show( "(Diagnostics) open shield complete");
                    } catch( Exception ex) {
                        MessageBox.Show( "(Diagnostics) open shield error: " + ex.Message);
                    } finally {
                        Blocking = old_blocking;
                    }
                });
            }, () => { return Idle; });

            HomeCommand = new SimpleRelayCommand( () => {
                Task.Factory.StartNew( () => {
                    bool old_blocking = Blocking;
                    Blocking = true;
                    try {
                        Home();
                        MessageBox.Show( "(Diagnostics) home complete");
                    } catch( Exception ex) {
                        MessageBox.Show( "(Diagnostics) home error: " + ex.Message);
                    } finally {
                        Blocking = old_blocking;
                    }
                });
            }, () => { return Idle; });

            CloseDoorCommand = new SimpleRelayCommand( () => {
                Task.Factory.StartNew( () => {
                    bool old_blocking = Blocking;
                    Blocking = true;
                    try {
                        CloseShield();
                        MessageBox.Show( "(Diagnostics) close shield complete");
                    } catch( Exception ex) {
                        MessageBox.Show( "(Diagnostics) close shield error: " + ex.Message);
                    } finally {
                        Blocking = old_blocking;
                    }
                });
            }, () => { return Idle; });
        }

        ~HiG()
        {
            Close();
        }

        //--------------- INITIALIZATION BEGIN
        public void Initialize( String device_name, String adapter_device_id, Boolean simulate)
        {
            LastError = "";

            // DKM 2012-04-03 check for valid adapter_device_id value
            int id_check = int.Parse( adapter_device_id);
            if( id_check < 0 || id_check > 254) {
                LastError = "The value for adapter_device_id must be within the range [0,254]";
                HandleBlockingNonBlockingError( LastError, InitializeError);
                return;
            }

            // DKM 2012-02-10 require HiG to be idle before opening shield
            if( !_execution_state.Idle) {
                LastError = String.Format( "{0} must be idle before initializing", Name);
                HandleBlockingNonBlockingError( LastError, InitializeError);
                return;
            }

            _execution_state.SetInitializing();

            if( _connected)
                Close();

            Name = device_name;
            // DKM 2011-11-14 trying to centralize device logging so we can associate specific
            //                error messages with the device they come from
            ConfigureLog( device_name);

            try {
                // log the assembly version to make debugging sessions easier
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var fileversioninfo = FileVersionInfo.GetVersionInfo( assembly.Location);
                _log.DebugFormat( "Loaded HiGIntegration DLL version {0}", fileversioninfo.FileVersion);

                _connected = false;
                Simulating = simulate;
                _log.DebugFormat( "{0} About to connect.  Simulating = {1}", Name, Simulating ? "true" : "false");
                if( !simulate)
#if !TML_SINGLETHREADED
                    _ts = new TechnosoftConnection(adapter_device_id, TMLLibConst.CHANNEL_SYS_TEC_USBCAN, 500000);
#else
                    _ts = new TechnosoftConnection(adapter_device_id, TML.TMLLib.CHANNEL_SYS_TEC_USBCAN, 500000);
#endif
                else
                    _ts = new TechnosoftConnection();
                _log.DebugFormat( "{0} created connection", Name);
            } catch( Exception ex) {
                _log.ErrorFormat( "{0} failed to create connection: {1}", Name, ex.Message);
                // got an error, immediately throw if Blocking
                if( Blocking)
                    throw new Exception( ex.Message);
                if( InitializeError != null)
                    InitializeError( this, new ErrorEventArgs( ex.Message));
                _execution_state.SetDone();
                return;
            }

            Action initialize_thread = new Action( () => {
                if( Thread.CurrentThread.Name == null)
                    Thread.CurrentThread.Name = "HiG integration initialization thread";
                _log.InfoFormat( "Initialize() called with device_name = {0}, adapter_device_id = {1}, simulate = {2}", device_name, adapter_device_id, simulate);
                // first, extract the latest (currently v1.3) .t.zips from the assembly to the user's temp folder
                // DKM 2012-01-18 to prevent writing to the same file, now have to use HiG device name, but remember to replace invalid characters
                string temp_path = System.IO.Path.GetTempPath() + (Name).ReplaceInvalidFilenameCharacters();
                ExtractTmlFiles( temp_path, 1, 4, 1, 5);
                // now load this configuration *temporarily*, just to get the version numbers
                string temp_motor_settings_path = temp_path + "\\motor_settings.xml";
                _ts.LoadConfiguration( temp_motor_settings_path, temp_path);

                _spindle_axis = _ts.GetAxes()[115];
                _log.DebugFormat( "{0} spindle axis during initialization", _spindle_axis != null ? "Found" : "Did not find");
                _shield_axis = _ts.GetAxes()[113];
                _log.DebugFormat( "{0} shield axis during initialization", _shield_axis != null ? "Found" : "Did not find");

                short spindle_major;
                short spindle_minor;
                HigUtils.GetFirmwareVersion( _spindle_axis, out spindle_major, out spindle_minor);
                short shield_major;
                short shield_minor;
                HigUtils.GetFirmwareVersion( _shield_axis, out shield_major, out shield_minor);
                // based on the firmware version, now extract the correct .t.zips to the config folder IF ONE EXISTS
                // we don't want to overwrite a newer firmware version's .t.zips because this hinders development and experimental
                // builds sent to customers.  ExtractTmlFiles will simply bail if the file doesn't exist in the assembly and
                // leave the one that's currently in the config folder
                string device_config_path = _tsm_setup_base_path + "\\" + (Name).ReplaceInvalidFilenameCharacters();
                ExtractTmlFiles( device_config_path, shield_major, shield_minor, spindle_major, spindle_minor);
                string motor_settings_path = _motor_settings_base_path + "\\" + (Name).ReplaceInvalidFilenameCharacters() + "\\motor_settings.xml";
                _ts.LoadConfiguration( motor_settings_path, device_config_path);
                _log.DebugFormat( "{0} loaded configuration from {1}", Name, motor_settings_path);
                // pull the rotational radius from the spindle axis controller
                if( !Simulating) {
                    if( !_spindle_axis.GetFixedVariable( "rotational_radius_mm", out _rotational_radius_mm)) {
                        _log.Error( "failed to read rotational radius from device.  Using default value of 110.0mm.");
                        _rotational_radius_mm = 110.0;
                    }
                    // load the serial number and application ID
                    string appID = _spindle_axis.ReadApplicationID();
                    _log.DebugFormat( "{0} Spindle Axis has internal AppID of: {1}\n", Name, appID);
                
                    string serialNumStr = _spindle_axis.ReadSerialNumber();
                    _log.InfoFormat("{0} S/N: {1}\n", Name, serialNumStr);
                    InternalSerialNumber = serialNumStr;
                } else {
                    InternalSerialNumber = "EVAL";
                }

                _shield_open_pos = _shield_axis.ReadLongVarEEPROM( "door_open_pos_ptr");
                _shield_closed_pos = _shield_axis.ReadLongVarEEPROM( "door_close_pos_ptr");
                _bucket1_position = _spindle_axis.ReadLongVarEEPROM( "my_homepos_ptr");
                _bucket2_offset = _spindle_axis.ReadIntVarEEPROM( "bucket2_offset_ptr");
                // DKM 2012-03-07 only check imbalance threshold if we're running spindle firmware v1.5 or higher
                _imbalance_threshold = SupportsImbalance ? _spindle_axis.ReadIntVarEEPROM( "imb_ampl_max_ptr") : 0;

                string firmware_info = HigUtils.GetFirmwareVersions( ShieldAxis, SpindleAxis);
                _log.InfoFormat( "{0} running {1}", Name, firmware_info);
            });

            if( !Blocking)
                initialize_thread.BeginInvoke( InitializeThreadComplete, null);
            else {
                try {
                    initialize_thread.Invoke();
                    HandleInitializeComplete();
                } finally {
                    _execution_state.SetDone();
                }
            }
        }

        private void HandleBlockingNonBlockingError( string error, EventHandler event_handler)
        {
            // DKM 2012-04-24 had to add this check since someone could try to Home before initializing, and
            //                in the process of reporting the error, a null ref exception will get thrown
            if( _log != null)
                _log.Error( LastError);                
            // got an error, immediately throw if Blocking
            if( Blocking)
                throw new Exception( error);
            if( event_handler != null)
                event_handler(this, new ErrorEventArgs( error));
        }

        private void ConfigureLog( string name)
        {
            // for log4net debugging, see: http://www.l4ndash.com/Log4NetMailArchive%2Ftabid%2F70%2Fforumid%2F1%2Fpostid%2F14345%2Fview%2Ftopic%2FDefault.aspx
            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();
            /*
            Logger logger_113 = hierarchy.LoggerFactory.CreateLogger( "axis_113");
            hierarchy.LoggerFactory.CreateLogger( "axis_115");
             */
            hierarchy.Root.Level = log4net.Core.Level.All;            
            hierarchy.Configured = true;

            string module_path = BioNex.Shared.Utils.FileSystem.GetModulePath();
            string folder = module_path + "\\logs\\";
            string path = String.Format("{0}{1}-log.txt", folder, name);

            _log = LogManager.GetLogger( name);

            var fa = new log4net.Appender.FileAppender() { Name = name, File = path, AppendToFile = true};
            var layout = new PatternLayout { ConversionPattern = "%date{{yyyy-MM-dd-HH_mm_ss.fff}} [%thread] %-5level %logger - %message%newline" };
            layout.ActivateOptions();
            fa.Layout = layout;
            fa.ActivateOptions();

            ((Logger)_log.Logger).AddAppender( fa);

            // DKM 2012-04-23 try to add axis_113 and axis_115 loggers to default repo
            /*
            var default_repo = (Hierarchy)LogManager.GetRepository( System.Reflection.Assembly.GetExecutingAssembly());
            var logger_113 = default_repo.LoggerFactory.CreateLogger( "axis_113");
            var fa_113 = new log4net.Appender.FileAppender() { Name = "axis_113", File = String.Format( "{0}{1}_axis113-log.txt", folder, "axis_113"), AppendToFile = true};
            fa_113.Layout = layout;
            fa_113.ActivateOptions();
            logger_113.AddAppender( fa_113);

            var logger_115 = default_repo.LoggerFactory.CreateLogger( "axis_115");
            var fa_115 = new log4net.Appender.FileAppender() { Name = "axis_115", File = String.Format( "{0}{1}_axis115-log.txt", folder, "axis_115"), AppendToFile = true};
            fa_115.Layout = layout;
            fa_115.ActivateOptions();
            logger_115.AddAppender( fa_115);
             */

            /* doesn't work
            BioNex.Shared.Utils.Logging.SetConfigurationFilePath( module_path + "\\logging.xml");
            BioNex.Shared.Utils.Logging.SetLogFilePath( module_path + "\\logs", "HiG_lowlevel_combined", false);
             */

            var logger_113 = LogManager.GetLogger( "axis_113");
            var fa_113 = new log4net.Appender.FileAppender() { Name = "axis_113", File = String.Format( "{0}{1}_axis113-log.txt", folder, name), AppendToFile = true};
            fa_113.Layout = layout;
            fa_113.ActivateOptions();
            ((Logger)logger_113.Logger).AddAppender( fa_113);

            var logger_115 = LogManager.GetLogger( "axis_115");
            var fa_115 = new log4net.Appender.FileAppender() { Name = "axis_115", File = String.Format( "{0}{1}_axis115-log.txt", folder, name), AppendToFile = true};
            fa_115.Layout = layout;
            fa_115.ActivateOptions();
            ((Logger)logger_115.Logger).AddAppender( fa_115);
        }

        // basic idea: http://www.dotnetscraps.com/dotnetscraps/post/Insert-any-binary-file-in-C-assembly-and-extract-it-at-runtime.aspx
        // addressing embedded path properly: http://www.codeproject.com/KB/dotnet/embeddedresources.aspx
        private void ExtractTmlFiles( string destination_folder, int shield_major_version_required, int shield_minor_version_required,
                                      int spindle_major_version_required, int spindle_minor_version_required)
        {            
            // DKM 2011-12-06 originally, I passed in a list of axes, but since this is only for HiG, and since we want to support
            //                different versions for the shield and spindle, I treat each axis separately here.
            // SHIELD AXIS
            string path = String.Format( "{0}\\{1}.t.zip", destination_folder, 113);
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            //In the next line you should provide NameSpace.FileName.Extension that you have embedded
            var input = assembly.GetManifestResourceStream( String.Format( "BioNex.HiGIntegration.TML.{0}_{1}.{2}.t.zip", 113, shield_major_version_required, shield_minor_version_required));
            // only overwrite the file if a matching version exists!
            if( input != null) {
                if( System.IO.File.Exists( path)) {
                    System.IO.File.Delete( path);
                }
                // DKM 2012-01-18 create the folder if necessary
                if( !System.IO.Directory.Exists( destination_folder)) {
                    System.IO.Directory.CreateDirectory( destination_folder);
                }
                var output = System.IO.File.Open( path, System.IO.FileMode.CreateNew);
                CopyStream( input, output);
                input.Dispose();
                output.Dispose();
            }
            // SPINDLE AXIS
            path = String.Format( "{0}\\{1}.t.zip", destination_folder, 115);
            //In the next line you should provide NameSpace.FileName.Extension that you have embedded
            input = assembly.GetManifestResourceStream( String.Format( "BioNex.HiGIntegration.TML.{0}_{1}.{2}.t.zip", 115, spindle_major_version_required, spindle_minor_version_required));
            if( input != null) {
                if( System.IO.File.Exists( path))
                    System.IO.File.Delete( path);
                var output = System.IO.File.Open( path, System.IO.FileMode.CreateNew);
                CopyStream( input, output);
                input.Dispose();
                output.Dispose();
            }
            // MOTOR SETTINGS XML
            path = String.Format( "{0}\\motor_settings.xml", destination_folder);
            //In the next line you should provide NameSpace.FileName.Extension that you have embedded
            input = assembly.GetManifestResourceStream( "BioNex.HiGIntegration.TML.motor_settings.xml");
            if( input != null) {
                // DKM 2012-04-05 normally, the config directory would already exist if the .t.zips were extracted, but in
                //                simulation this isn't always the case, so at least see if we need to create the directory
                //                so no exception is thrown.
                if( !System.IO.Directory.Exists( destination_folder)) {
                    System.IO.Directory.CreateDirectory( destination_folder);
                }

                if( System.IO.File.Exists( path))
                    System.IO.File.Delete( path);
                var output = System.IO.File.Open( path, System.IO.FileMode.CreateNew);
                CopyStream( input, output);
                input.Dispose();
                output.Dispose();
            }
        }

        private static void CopyStream( System.IO.Stream input, System.IO.Stream output)
        {
            byte[] buffer = new byte[32768];
            while (true)
            {
                int read = input.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                    return;
                output.Write(buffer, 0, read);
            }
        }

        private void InitializeThreadComplete( IAsyncResult iar)
        {
            // DKM 2012-02-17 here I had to change the way I change execution state.  The problem was with VWorks, where I would call Initialize, followed
            //                by Home.  Calling Home would always result in an error because InitializeComplete gets fired before _execution_state changes.
            //                This allows Home to get called before _execution_state has had a change to go back to the Idle state.
            try {
                AsyncResult ar = (AsyncResult)iar;
                Action callback = (Action)ar.AsyncDelegate;
                callback.EndInvoke( iar);
                // DKM 2012-03-29 MUST set execution state BEFORE firing event!!!
                _execution_state.SetDone();
                if (InitializeComplete != null)
                {
                    HandleInitializeComplete();
                    InitializeComplete(this, null);
                }
            } catch( Exception ex) {
                LastError = String.Format( "{0} failed to initialize: {1}", Name, ex.Message);
                _log.Error( LastError);
                // if there is an error, still want to let _execution_state go Idle
                _execution_state.SetDone();
                _log.Debug( "Firing InitializeError...");
                if( InitializeError != null)
                    InitializeError( this, new ErrorEventArgs( ex.Message));
            }
        }

        private void HandleInitializeComplete()
        {
            _connected = true;
            _updater = new ThreadedUpdates("HiG integration driver caching", UpdateControllerStats, 100, ForcedDisconnectionHandler);
            _updater.Start();
            _log.DebugFormat("{0} initialized successfully", Name);
            _log.Debug("Firing InitializeComplete...");
        }
        //--------------- INITIALIZATION END
        
        private void ForcedDisconnectionHandler()
        {
            _log.Error( "HiG was forcefully disconnected (was the power turned off?).  Please power up device, initialize, and re-home.");
            if( ForcedDisconnection != null) {
                ForcedDisconnection( this, null);
            }
        }

        public void Close()
        {
            _connected = false;

            if( _updater != null) {
                _updater.Stop();
                _updater = null;
            }

            // DKM 2012-04-05 do this to allow GUI to show latest data after closing -- i.e. won't say At Bucket 1 when it should be Unknown Bucket
            CurrentBucket = 0;

            if( _ts != null) {
                _ts.Close();
                _ts = null;
            }
        }

        public String FirmwareVersion
        {
            get { return HigUtils.GetFirmwareVersions( ShieldAxis, SpindleAxis); }
        }

        public String SerialNumber
        {
            get { return InternalSerialNumber; }
        }

        public void ShowDiagnostics( Boolean modal)
        {
            try {
                Action display_diags = new Action( () => {
                    if( _diagnostics_window == null) {
                        _diagnostics_window = new Diagnostics( this);
                        _diagnostics_window.Title = Name + " Diagnostics" + ( Simulating ? " (Simulating)" : "");
                        _diagnostics_window.Closed += new EventHandler(_diagnostics_window_Closed);
                        _diagnostics_window.Height = 480;
                        _diagnostics_window.Width = 480;
                    }
                    _diagnostics_window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                    if( modal) {
                        _diagnostics_window.ShowDialog();
                    } else {
                        _diagnostics_window.Show();
                        _diagnostics_window.Activate();
                    }
                } );

                // DKM 2011-10-14 is this correct?  If App.Current isn't null, then we've got a managed app
                if( Application.Current != null) {
                    Application.Current.Dispatcher.Invoke( display_diags);
                } else {
                    display_diags();
                }

            } catch( Exception ex) {
                _log.ErrorFormat( "{0} Could not display HiG diagnostics: {1}", Name, ex.Message);
            }
        }

        void _diagnostics_window_Closed(object sender, EventArgs e)
        {
            _diagnostics_window = null;
            if( DiagnosticsClosed != null)
                DiagnosticsClosed( this, null);
        }

        public String LastError { get; internal set; }

        // DKM 2012-03-13 added Blocking property to make state execution from within diagnostics not clobber error recovery from schedulers
        public Boolean Blocking { get; set; }
        // DKM 2012-04-03 added CurrentBucket for Andrew Carretta @ HRB
        public Int16 CurrentBucket { get; set; }

        //--------------- HOME BEGIN
        public void Home()
        {
            LastError = "";

            // can't open the shield if we're not connected
            if( !_connected) {
                LastError = String.Format( "{0} must be initialized before homing", Name);
                HandleBlockingNonBlockingError( LastError, HomeError);
                return;
            }

            // DKM 2012-02-10 require HiG to be idle before homing
            if( !_execution_state.Idle) {
                LastError = String.Format( "{0} must be idle before homing", Name);
                HandleBlockingNonBlockingError( LastError, HomeError);
                return;
            }

            Action home_thread = new Action( () => {
                if( Thread.CurrentThread.Name == null)
                    Thread.CurrentThread.Name = "HiG integration homing thread";
                _log.Info( "Home() called");
                IntegrationHomeStateMachine sm = new IntegrationHomeStateMachine( this, false, true, false);
                _execution_state.SetHoming();
                sm.Start();
            });

            if( !Blocking)
                home_thread.BeginInvoke( HomeThreadComplete, null);
            else {
                try {
                    home_thread.Invoke();
                } finally {
                    _execution_state.SetDone();
                }
            }
        }

        private void HomeThreadComplete( IAsyncResult iar)
        {
            try {
                AsyncResult ar = (AsyncResult)iar;
                Action callback = (Action)ar.AsyncDelegate;
                callback.EndInvoke( iar);
                // DKM 2012-03-29 MUST set execution state BEFORE firing event!!!
                _execution_state.SetDone();
                _log.Debug( "Firing HomeComplete...");
                if( HomeComplete != null)
                    HomeComplete( this, null);
            } catch( Exception ex) {
                LastError = String.Format( "{0} failed to home: {1}", Name, ex.Message);
                _log.Error( LastError);
                // DKM 2012-03-29 MUST set execution state BEFORE firing event!!!
                _execution_state.SetDone();
                _log.Debug( "Firing HomeError...");
                if( HomeError != null)
                    HomeError( this, new ErrorEventArgs(ex.Message));
            }
        }
        //--------------- HOME END

        //--------------- OPEN SHIELD BEGIN
        public void OpenShield( Int32 bucket_index)
        {
            LastError = "";

            // DKM 2012-02-10 require HiG to be idle before opening shield
            if( !_execution_state.Idle) {
                LastError = String.Format( "{0} must be idle before opening the shield", Name);
                HandleBlockingNonBlockingError( LastError, OpenShieldError);
                return;
            }

            // can't open the shield if we're not connected
            if( !_connected) {
                LastError = String.Format( "{0} must be initialized before opening the shield", Name);
                HandleBlockingNonBlockingError( LastError, OpenShieldError);
                return;
            }

            // can't open the shield if we're not homed
            if( !IsHomed) {
                LastError = String.Format( "{0} must be homed before opening the shield", Name);
                HandleBlockingNonBlockingError( LastError, OpenShieldError);
                return;
            }

            if( bucket_index < 0 || bucket_index > 1) {
                LastError = String.Format( "{0} The bucket index specified ({1}) is invalid, and must be 0 or 1.", Name, bucket_index);
                HandleBlockingNonBlockingError( LastError, OpenShieldError);
                return;
            }

            Action open_thread = new Action( () => {
                if( Thread.CurrentThread.Name == null)
                    Thread.CurrentThread.Name = "HiG integration open shield thread";
                IntegrationOpenShieldStateMachine sm = new IntegrationOpenShieldStateMachine( this, false);
                _execution_state.SetOpeningToBucket();
                _log.InfoFormat( "OpenShield called with bucket_index = {0}", bucket_index);
                sm.ExecuteOpenShield( bucket_index);
            });

            if( !Blocking)
                open_thread.BeginInvoke( OpenShieldThreadComplete, null);
            else {
                try {
                    open_thread.Invoke();
                } finally {
                    _execution_state.SetDone();
                }
            }
        }

        private void OpenShieldThreadComplete( IAsyncResult iar)
        {
            try {
                AsyncResult ar = (AsyncResult)iar;
                Action callback = (Action)ar.AsyncDelegate;
                callback.EndInvoke( iar);
                // DKM 2012-03-29 MUST set execution state BEFORE firing event!!!
                _execution_state.SetDone();
                _log.Debug( "Firing OpenShieldComplete...");
                if( OpenShieldComplete != null)
                    OpenShieldComplete( this, null);
            } catch( Exception ex) {
                LastError = String.Format( "{0} failed to open shield: {1}", Name, ex.Message);
                _log.Error( LastError);
                // DKM 2012-03-29 MUST set execution state BEFORE firing event!!!
                _execution_state.SetDone();
                _log.Debug( "Firing OpenShieldError...");
                if( OpenShieldError != null)
                    OpenShieldError( this, new ErrorEventArgs(ex.Message));
            }
        }
        //--------------- OPEN SHIELD END

        //--------------- CLOSE SHIELD BEGIN
        internal void CloseShield()
        {
            LastError = "";

            // can't close the shield if we're not connected
            if( !_connected) {
                LastError = String.Format( "{0} must be initialized before closing the shield", Name);
                _log.Error(LastError);
                throw new Exception(LastError);
            }

            // DKM 2012-02-10 require HiG to be idle before closing shield
            if( !_execution_state.Idle) {
                LastError = String.Format( "{0} must be idle before closing the shield", Name);
                _log.Error( LastError);
                throw new Exception(LastError);
            }

            // can't close the shield if we're not homed
            if( !IsHomed) {
                LastError = String.Format( "{0} must be homed before closing the shield", Name);
                _log.Error( LastError);
                throw new Exception(LastError);
            }

            Action close_thread = new Action( () => {
                if( Thread.CurrentThread.Name == null)
                    Thread.CurrentThread.Name = "HiG integration close shield thread";
                _log.Info( "CloseShield() called");
                IntegrationCloseShieldStateMachine sm = new IntegrationCloseShieldStateMachine( this, false);
                _execution_state.SetCloseShield();
                sm.ExecuteCloseShield();
            });

            if( !Blocking)
                close_thread.BeginInvoke( CloseShieldThreadComplete, null);
            else {
                try {
                    close_thread.Invoke();
                } finally {
                    _execution_state.SetDone();
                }
            }
        }

        private void CloseShieldThreadComplete( IAsyncResult iar)
        {
            try {
                AsyncResult ar = (AsyncResult)iar;
                Action callback = (Action)ar.AsyncDelegate;
                callback.EndInvoke( iar);
                // DKM 2012-03-29 MUST set execution state BEFORE firing event!!!
                _execution_state.SetDone();
            } catch( Exception ex) {
                LastError = String.Format( "{0} failed to close shield: {1}", Name, ex.Message);
                _log.Error( LastError);
            }
        }
        //--------------- CLOSE SHIELD END

        //--------------- SPIN BEGIN
        public void Spin( Double g, Double accel_percent, Double decel_percent, double time_seconds)
        {
            LastError = "";

            // can't sping if we're not connected
            if( !_connected) {
                LastError = String.Format( "{0} must be connected before spinning", Name);
                HandleBlockingNonBlockingError( LastError, SpinError);
                return;
            }

            // DKM 2012-02-10 require HiG to be idle before spinning
            if( !_execution_state.Idle) {
                LastError = String.Format( "{0} must be idle before spinning (currently in {1})", Name, _execution_state.CurrentState);
                HandleBlockingNonBlockingError( LastError, SpinError);
                return;
            }

            // can't spin if we're not homed
            if( !IsHomed) {
                LastError = String.Format( "{0} must be homed before spinning", Name);
                HandleBlockingNonBlockingError( LastError, SpinError);
                return;
            }

            // validate spin parameters
            Func<bool> InvalidG = new Func<bool>( () => { return g < HigUtils.GetMinimumGs() || g > HigUtils.GetMaximumGs(); } );
            Func<bool> InvalidAccel = new Func<bool>( () => { return accel_percent < 1 || accel_percent > 100; } );
            Func<bool> InvalidDecel = new Func<bool>( () => { return decel_percent < 1 || decel_percent > 100; } );

            if( InvalidG() || InvalidAccel() || InvalidDecel()) {
                StringBuilder sb = new StringBuilder();
                if( InvalidG()) { sb.AppendLine( String.Format( "The specified Gs ({0}) is invalid, and must be in the range [{1},{2}]", g, HigUtils.GetMinimumGs(), HigUtils.GetMaximumGs())); }
                if( InvalidAccel()) { sb.AppendLine( String.Format( "The specified acceleration ({0}) is invalid, and must be in the range [1,100]", accel_percent)); }
                if( InvalidDecel()) { sb.AppendLine( String.Format( "The specified deceleration ({0}) is invalid, and must be in the range [1,100]", decel_percent)); }
                LastError = sb.ToString();
                _log.Error( LastError);
                if( SpinError != null && !Blocking)
                    SpinError( this, new ErrorEventArgs( LastError));
                else
                    throw new Exception( LastError);
                return;
            }

            Action spin_thread = new Action( () => {
                if( Thread.CurrentThread.Name == null)
                    Thread.CurrentThread.Name = "HiG integration spin thread";
                _log.InfoFormat( "Spin() called with accel = {0}, decel = {1}, g = {2}, time = {3}", accel_percent, decel_percent, g, time_seconds);
                _spin_sm = new IntegrationSpinStateMachine( this, false);
                // convert Gs to RPM
                Debug.Assert( _rotational_radius_mm == 110.0);
                double rpm = HigUtils.CalculateRpmFromGs( g, _rotational_radius_mm);
                // reset times before spinning
                _accel_time_s = _cruise_time_s = _decel_time_s = 0;
                _execution_state.SetSpinning();
                _spin_sm.ExecuteSpin( accel_percent, decel_percent, rpm, time_seconds, ref _accel_time_s, ref _cruise_time_s, ref _decel_time_s);
            });

            if( !Blocking)
                spin_thread.BeginInvoke( SpinThreadComplete, null);
            else {
                try {
                    spin_thread.Invoke();
                } finally {
                    _execution_state.SetDone();
                }
            }
        }

        private void SpinThreadComplete( IAsyncResult iar)
        {
            try {
                AsyncResult ar = (AsyncResult)iar;
                Action callback = (Action)ar.AsyncDelegate;
                callback.EndInvoke( iar);
                // DKM 2012-03-29 MUST set execution state BEFORE firing event!!!
                _spin_sm = null;
                _execution_state.SetDone();
                _log.Debug( "Firing SpinComplete...");
                if( SpinComplete != null)
                    SpinComplete( this, new SpinCompleteEventArgs( _accel_time_s, _cruise_time_s, _decel_time_s));
            } catch( Exception ex) {
                LastError = String.Format( "{0} failed to spin: {1}", Name, ex.Message);
                _log.Error( LastError);
                _spin_sm = null;
                _execution_state.SetDone();
                _log.Debug( "Firing SpinError...");
                if( SpinError != null)
                    SpinError( this, new ErrorEventArgs(ex.Message));
            }
        }
        //--------------- SPIN END

        //--------------- ABORT BEGIN
        public void AbortSpin()
        {
            if( _spin_sm != null)
                _spin_sm.AbortSpin();
        }
        //--------------- ABORT END

        //--------------- HOMESHIELD BEGIN
        public void HomeShield( bool open_shield_after_home_complete)
        {
            LastError = "";

            // can't open the shield if we're not connected
            if (!_connected)
            {
                LastError = String.Format("{0} must be initialized before homing", Name);
                HandleBlockingNonBlockingError( LastError, HomeError);
                return;
            }

            // DKM 2012-02-10 require HiG to be idle before homing
            if( !_execution_state.Idle) {
                LastError = String.Format("{0} must be idle before homing", Name);
                HandleBlockingNonBlockingError( LastError, HomeError);
                return;
            }

            Action home_thread = new Action(() =>
            {
                if( Thread.CurrentThread.Name == null)
                    Thread.CurrentThread.Name = "HiG integration home shield thread";
                IntegrationHomeStateMachine sm = new IntegrationHomeStateMachine(this, true, open_shield_after_home_complete, false);
                _execution_state.SetHoming();
                sm.Start();
            });

            if( !Blocking)
                home_thread.BeginInvoke(HomeShieldThreadComplete, null);
            else {
                try {
                    home_thread.Invoke();
                } finally {
                    _execution_state.SetDone();
                }
            }
        }

        private void HomeShieldThreadComplete(IAsyncResult iar)
        {
            try
            {
                AsyncResult ar = (AsyncResult)iar;
                Action callback = (Action)ar.AsyncDelegate;
                callback.EndInvoke(iar);
                // DKM 2012-03-29 MUST set execution state BEFORE firing event!!!
                _execution_state.SetDone();
                _log.Debug( "Firing HomeComplete...");
                if (HomeComplete != null)
                    HomeComplete(this, null);
            }
            catch (Exception ex)
            {
                LastError = String.Format( "{0} failed to home shield: {1}", Name, ex.Message);
                _log.Error( LastError);
                // DKM 2012-03-29 MUST set execution state BEFORE firing event!!!
                _execution_state.SetDone();
                _log.Debug( "Firing HomeError...");
                if (HomeError != null)
                    HomeError(this, new ErrorEventArgs(ex.Message));
            }
        }
        //--------------- HOMESHIELD END

        // Imbalance calibration, not for external use

        /// <summary>
        /// This method does not call into IHigModel, because we only want to allow imbalance calibration from the test app
        /// </summary>
        internal void ExecuteCalibrateImbalance()
        {
            // we can display a messagebox if imbalance detection isn't supported
            if( !SupportsImbalance) {
                MessageBox.Show( "Imbalance calibration is only supported by spindle firmware v1.5 or later.");
                return;
            }

            // since this is not intended for external consumption, I feel comfortable using this cast because
            // I know that _hig is defined as a HiG object, which implements both HigInterface and IHigModel
            var sm = new ImbalanceCalibrationStateMachine( this as BioNex.Hig.IHigModel, false);
            sm.ExecuteCalibration();
        }


        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string property_name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property_name));
        }

        #endregion

        internal void UpdateControllerStats()
        {
            if (!_connected)
                return;

            try {
                CurrentRpm = (int)(SpindleAxis.GetActualSpeedDegPerSec() * 60 / 360.0);
                CurrentGs = HigUtils.CalculateGsFromRpm(CurrentRpm, _rotational_radius_mm);
                var num_encoder_lines = SpindleAxis.Settings.EncoderLines;
                var current_position = HigUtils.ConvertIUToDegrees( SpindleAxis.GetPositionCounts(), num_encoder_lines);
                var bucket2_position = HigUtils.ConvertIUToDegrees( Bucket2Offset, num_encoder_lines);
                // DKM 2012-04-03 increase the multiplier on the window from 2 to 8 so that the API property doesn't bounce all over the place
                var window = 8 * SpindleAxis.Settings.MoveDoneWindow;
                // DKM 2011-10-17 copied existing logic behind TestAngle to determine if we're at bucket 1 or bucket 2
                // DKM 2012-04-03 check for homed condition first, otherwise we're technically not sure where we
                if( IsHomed && Math.Abs( current_position) < window) {
                    AtBucket1Color = Brushes.Green;
                    AtBucket2Color = Brushes.Silver;
                    CurrentBucket = 1;
                } else if( IsHomed && Math.Abs( current_position - bucket2_position) < window) {
                    AtBucket1Color = Brushes.Silver;
                    AtBucket2Color = Brushes.Green;
                    CurrentBucket = 2;
                } else {
                    AtBucket1Color = Brushes.Silver;
                    AtBucket2Color = Brushes.Silver;
                    CurrentBucket = 0;
                }
            } catch( Exception ex) {
                _log.DebugFormat( "{0} Error in UpdateControllerStats(): {1}", Name, ex.Message);
                throw;
            }
        }
    }
}
