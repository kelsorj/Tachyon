using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using BioNex.Hig.StateMachines;
using BioNex.Shared.IError;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.Utils;
using log4net;

namespace BioNex.Hig
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(SynapsisModel))]
    public class SynapsisModel : IHigModel
    {
        public event EventHandler SynapsisHiGModelConnected;
        public event EventHandler SynapsisHiGModelDisconnected;

        internal string DeviceInstanceName { get; set; }
        public string InternalSerialNumber { get; internal set; }
        
        /// <summary>
        /// Keeps track of what the HiG is doing, so we can allow the user to rehome and do other diagnostics
        /// stuff after spinning and encoutering an error.
        /// </summary>
        internal ExecutionState _execution_state;

        private TechnosoftConnection _connection;

        // 0 == homed
        private int _spindle_homed;
        private int _shield_homed;

        [Import]
        public IError AppErrorInterface { get; set; }
        public void AddError(ErrorData error)
        {
            AppErrorInterface.AddError(error);
        }

        [Import("MainDispatcher")]
        public Dispatcher MainDispatcher { get; internal set; }

        private List<IAxis> _axes;
        private const int IndexShieldAxis = 0;
        private const int IndexSpindleAxis = 1;
        // DKM 2011-11-14 don't instantiate yet -- want to set the logger name to the device name later on
        private ILog _log;
        public bool Simulating { get; private set; }
        public IAxis ShieldAxis { get { return _axes == null ? null : _axes[IndexShieldAxis]; } }
        public IAxis SpindleAxis { get { return _axes == null ? null : _axes[IndexSpindleAxis]; } }
        private int _debug_level;
        public bool CycleDoorOnly { get; private set; }
        public bool UpdateShieldFirmware(string path) { return false; }
        public bool UpdateSpindleFirmware(string path) { return false; }
        public bool NoShieldMode { get; private set; }

        public double CurrentSpindlePosition { get; private set; }
        public double CurrentShieldPosition { get; private set; }
        
        public int ShieldOpenPosition { get; set; }
        public int ShieldClosedPosition { get; set; }

        private int _imbalance_threshold;
        public int ImbalanceThreshold
        {
            get { return _imbalance_threshold; }
            set {
                _imbalance_threshold = value;
                SpindleAxis.WriteIntVarEEPROM( "imb_ampl_max_ptr", (short)value);
            }
        }

        public bool SupportsImbalance { get { return SpindleAxis != null ? SpindleAxis.FirmwareMinorVersion >= 5 : false; } }

        public double SpindleTemperature
        {
            get
            {
                short ad7;
                SpindleAxis.GetIntVariable("AD7", out ad7); // AD7 is the temperature sensor variable

                return HigUtils.ConvertDriveTemperature_IU_to_degC( (UInt16)ad7 );
            }
        }
        public string Name { get { return DeviceInstanceName; } }

        public void WriteEepromSetting( EepromSetting setting)
        {
            IAxis controller_to_use = setting.SpindleParameter ? SpindleAxis : ShieldAxis;
            if (setting.VariableType == EepromSetting.VariableTypeT.Int)
                controller_to_use.WriteIntVarEEPROM(setting.VariableName, short.Parse(setting.Value.ToString()));
            else if (setting.VariableType == EepromSetting.VariableTypeT.Long)
                controller_to_use.WriteLongVarEEPROM(setting.VariableName, int.Parse(setting.Value.ToString()));
            else if (setting.VariableType == EepromSetting.VariableTypeT.Fixed)
                controller_to_use.WriteFixedVarEEPROM(setting.VariableName, double.Parse(setting.Value.ToString()));
            else
                _log.Debug(String.Format("Invalid variable type specified when attempting to write '{0}'", setting.VariableName));
        }

        public void ReadEepromSetting( EepromSetting setting)
        {
            IAxis controller_to_use = setting.SpindleParameter ? SpindleAxis : ShieldAxis;
            if (setting.VariableType == EepromSetting.VariableTypeT.Int)
                setting.Value = (short)controller_to_use.ReadIntVarEEPROM(setting.VariableName);
            else if (setting.VariableType == EepromSetting.VariableTypeT.Long)
                setting.Value = (int)controller_to_use.ReadIntVarEEPROM(setting.VariableName);
            else if (setting.VariableType == EepromSetting.VariableTypeT.Fixed)
                setting.Value = (double)controller_to_use.ReadIntVarEEPROM(setting.VariableName);
            else
                _log.Debug(String.Format("Invalid variable type specified when attempting to read '{0}'", setting.VariableName));
        }

        public void CallInternalSaveAppEEPROM()
        {
            try
            {
                SpindleAxis.CallFunction("save_app_eeprom");
                ShieldAxis.CallFunction("save_app_eeprom");
            }
            catch (AxisException ex)
            {
                // TODO -- log error
                throw ex;
            }
        }

        public double SpinSecRemaining { get; set; }
        public bool Spinning { get; private set; }

        internal Dictionary<string,string> DeviceProperties { get; set; }
        private ThreadedUpdates LoggingHelper { get; set; }
        private const double UnderVoltageThreshold = 58.0;

        private double _rotational_radius_mm; // used private field since I can't use properties as out param
        public double RotationalRadiusMm
        {
            get { return _rotational_radius_mm; }
        }

        private ThreadedUpdates Updater { get; set; }
        private SpinStateMachine _spin_sm;

        private static class AnalogValues
        {
            public const int ImbalanceSensor = 0;
        }

        public static class Properties
        {
            public const string Simulate = "simulate";
            public const string Port = "port";
            public const string ConfigFolder = "configuration folder";
            public const string HostDeviceName = "host device name"; // either "self" to use own TechnosoftConnection, or device instance name to piggyback
            public const string Self = "self";
            // optional
            public const string DebugLevel = "debug level";
            public const string CycleDoorOnly = "cycle door only";
            public const string NoShieldMode = "no shield mode";
        }

        public SynapsisModel()
        {
            Action disconnect = new Action( () => {
                Disconnect();
                _log.Info( String.Format( "{0} was disconnected.  Please open diagnostics, reconnect, and then re-home.", DeviceInstanceName));
            });
            Updater = new ThreadedUpdates( "HiG property caching", UpdateControllerStats, 100, disconnect);
            _execution_state = new ExecutionState();
        }

        private void StartUpdateThread( bool start)
        {
            if( start && !Updater.Running)
                Updater.Start();
            else if( !start && Updater.Running)
                Updater.Stop();
        }

        /// <summary>
        /// Whether or not the plugin has a connection to the Technosoft controller,
        /// either in simulation or real hardware mode.
        /// </summary>
        private bool _connected;
        public bool Connected
        {
            get { return _connected; }
            set
            {
                if (value)
                {
                    _log = LogManager.GetLogger(DeviceInstanceName);
                    ReloadMotorSettings();
                    StartUpdateThread(true);
                    _connected = true;
                    if (SynapsisHiGModelConnected != null)
                        SynapsisHiGModelConnected(this, null);
                }
                else
                {
                    StartUpdateThread(false);
                    Disconnect();
                }
            }
        }

        private void Disconnect()
        {
            // DKM 2011-09-30 need to do this in case the device wasn't actually initialized / connected to begin with
            if (_connection != null)
            {
                _connection.Close();
                _connection = null;
            }

            _connected = false;
            if (SynapsisHiGModelDisconnected != null)
                SynapsisHiGModelDisconnected(this, null);
        }

        /// <summary>
        /// Whether or not the HiG is homed.
        /// </summary>
        public bool Homed 
        {
            get
            {
                if (!Connected)
                    return false;
                return _spindle_homed == 0 && _shield_homed == 0;
            }
        }

        // cheasy busy flag for diagnostics to look at before allowing thread launches
        //public bool IsBusy { get; set; }

        public void Home( bool called_from_diags)
        {
            _execution_state.SetHoming();
            var sm = new HomeStateMachine(this, false, true, called_from_diags);
            sm.Error += new EventHandler( (o,a) => { _execution_state.SetError(); });
            sm.Start(); 
            _execution_state.SetDone();
        }

        /// <summary>
        /// The current speed of the rotor, in RPM
        /// </summary>
        private int CurrentRpm { get; set; }
        public double CurrentGs { get; private set; }

        private short _bucket2_offset;
        public short Bucket2Offset
        {
            get { return _bucket2_offset; }
            set {
                _bucket2_offset = value;
                SpindleAxis.WriteIntVarEEPROM( "bucket2_offset_ptr", value);
            }
        }

        private int _bucket1_position;
        public int Bucket1Position
        {
            get { return _bucket1_position; }
            set {
                _bucket1_position = value;
                SpindleAxis.WriteLongVarEEPROM( "my_homepos_ptr", value);
            }
        }

        private void ReloadMotorSettings()
        {
            Simulating = DeviceProperties[Properties.Simulate] != "0";
            // DKM 2011-11-09 assume debug level is 0 (don't do any debug logging)
            _debug_level = 0;
            string val;
            if (DeviceProperties.TryGetValue(Properties.DebugLevel, out val))
                _debug_level = val.ToInt();
            // DKM 2011-11-09 added a way to cycle the door and not do any spins
            CycleDoorOnly = false;
            if (DeviceProperties.TryGetValue(Properties.CycleDoorOnly, out val))
                CycleDoorOnly = val != "0";
            // DKM 2012-04-17 added no shield mode to get rid of old branch
            NoShieldMode = false;
            if( DeviceProperties.TryGetValue(Properties.NoShieldMode, out val))
                NoShieldMode = val != "0";

            // the name of the device instance whose TechnosoftConnection we're going to use
            string host_device_name;
            if( DeviceProperties.TryGetValue( Properties.HostDeviceName, out host_device_name)) {
                _log.Info( "Host device name was specified in the HiG device configuration, but this feature is now deprecated.");
            }

            var port = DeviceProperties[Properties.Port];
            // with the config folder, we can now load TSM information, mechanical settings, etc.
            var motorSettingsPath = (DeviceProperties[Properties.ConfigFolder] + "\\motor_settings.xml").ToAbsoluteAppPath();
            var tsmSetupFolder = DeviceProperties[Properties.ConfigFolder].ToAbsoluteAppPath();

            _axes = null;
            // try to use shared connection first
            if( Simulating) {
                _connection = new TechnosoftConnection();
            } else if( host_device_name == Properties.Self || _connection == null) {
                _connection = new TechnosoftConnection( port, TMLLibConst.CHANNEL_SYS_TEC_USBCAN, 500000);
            }
            _connection.LoadConfiguration(motorSettingsPath, tsmSetupFolder);
            _axes = _connection.GetAxes().Values.ToList();

            // read in the home position and bucket 2 offset
            try {
                _bucket1_position = SpindleAxis.ReadLongVarEEPROM( "my_homepos_ptr");
                _bucket2_offset = SpindleAxis.ReadIntVarEEPROM("bucket2_offset_ptr");
            } catch( Exception) {
                _log.Debug( "Failed to read bucket2 offset and/or bucket 1 home position from EEPROM");
            }

            try
            {
                if( Simulating) {
                    _rotational_radius_mm = 110.0;
                } else if (!SpindleAxis.GetFixedVariable("rotational_radius_mm", out _rotational_radius_mm)) {
                    _log.Debug("Failed to read rotational radius from device.  Using default value of 110.0mm.");
                    _rotational_radius_mm = 110.0;
                }
            }
            catch (Exception)
            {
                _log.Debug("Failed to read rotational radius from device.  Using default value of 110.0mm.");
                _rotational_radius_mm = 110.0;
            }

            // Read application ID from Spindle
            string appID;
            try
            {
                // load the serial number and application ID
                appID = SpindleAxis.ReadApplicationID();
                _log.Debug(string.Format("{0} Spindle Axis has internal AppID of: {1}", Name, appID));
            }
            catch (Exception ex)
            {
                _log.DebugFormat( "{0} could not read Spindle Axis internal AppID: {1}", Name, ex.Message);
                appID = "Unknown";
            }

            // Read HiG Serial Number stored in Spindle controller
            string serialNumStr;
            try
            {
                serialNumStr = SpindleAxis.ReadSerialNumber();
                _log.Info(string.Format("{0} S/N: {1}", Name, serialNumStr));
                InternalSerialNumber = serialNumStr;
            }
            catch (Exception ex)
            {
                _log.InfoFormat( "Could not read S/N: {0}", ex.Message);
                serialNumStr = "Unknown";
            }

            string firmware_info = HigUtils.GetFirmwareVersions( ShieldAxis, SpindleAxis);
            _log.InfoFormat( "{0} running {1}", Name, firmware_info);

            // Take a bus voltage reading and log it
            _log.Debug(String.Format("{0} bus voltage = {1:0.0} VDC", Name, PowerSupplyVoltage));
        }

        /// <summary>
        /// Opens the door, regardless of the position of the rotor
        /// </summary>
        public void OpenShield(int bucket_index, bool called_from_diags)
        {
            _execution_state.SetOpeningToBucket();
            var sm = new OpenShieldStateMachine(this, called_from_diags);
            sm.Error += new EventHandler( (o,a) => { _execution_state.SetError(); });
            sm.ExecuteOpenShield( bucket_index);
            _execution_state.SetDone();
        }

        public void CloseShield(bool called_from_diags)
        {
            _execution_state.SetCloseShield();
            var sm = new CloseShieldStateMachine(this, called_from_diags);
            sm.Error += new EventHandler( (o,a) => { _execution_state.SetError(); });
            sm.ExecuteCloseShield();
            _execution_state.SetDone();
        }

        /// <summary>
        /// Returns the current state index of the homing function that runs in the TSM controller
        /// -1 on failure (maybe should throw exception?)
        /// </summary>
        /// <returns></returns>
        private int GetHomeState(IAxis axis) 
        {
                if( !Connected)
                    return -1;
                try
                {
                    short homeStatus;
                    var success = axis.GetIntVariable("homing_status", out homeStatus);
                    return success ? homeStatus : -1;
                }
                catch (AxisException )
                {
                    // TODO -- log error
                }
                return -1;
        }

        /// <summary>
        /// Starts spinning the rotor using the values specified by the properties
        /// AccelerationPercent, DecelerationPercent, MaxRPM, and TimeToSpinSeconds
        /// </summary>
        public void Spin(double accel, double decel, double g, double timeSeconds, bool called_from_diags)
        {
            _execution_state.SetSpinning();
            _spin_sm = new SpinStateMachine(this, called_from_diags);
            _spin_sm.Error += new EventHandler( (o,a) => { _execution_state.SetError(); });
            double accel_time_s = 0;
            double cruise_time_s = 0;
            double decel_time_s = 0;
            double rpm = Hig.HigUtils.CalculateRpmFromGs( g, _rotational_radius_mm);
            Spinning = true;
            _spin_sm.ExecuteSpin(accel, decel, rpm, timeSeconds, ref accel_time_s, ref cruise_time_s, ref decel_time_s);
            Spinning = false;
            _execution_state.SetDone();
        }

        public void Abort()
        {
            if( _spin_sm != null)
                _spin_sm.AbortSpin();
        }

        private void UpdateControllerStats()
        {
            if (!Connected)
                return;

            // DKM 2011-09-30 purposely allow exceptions to bubble up now.  Use error # threshold to force disconnect.
            var spindle = _axes[IndexSpindleAxis];
            CurrentRpm = (int)(spindle.GetActualSpeedDegPerSec() * 60 / 360.0);
            CurrentGs = HigUtils.CalculateGsFromRpm(CurrentRpm, RotationalRadiusMm);
            double spindle_angle_deg = spindle.GetPositionMM();
            CurrentSpindlePosition = Math.Sign(spindle_angle_deg) < 0 ? (spindle_angle_deg % 360.0) + 360.0 : (spindle_angle_deg % 360.0);
            var shield = _axes[IndexShieldAxis];
            CurrentShieldPosition = shield.GetPositionMM();
            _spindle_homed = GetHomeState(_axes[IndexSpindleAxis]);
            _shield_homed = GetHomeState(_axes[IndexShieldAxis]);
        }


        #region unused properties (for engineering interface)

        /// <summary>
        /// The actual voltage reading from the inductive proximity sensor
        /// </summary>
        public double ImbalanceSensorReading
        {
            get { return ReadRotorAnalog(AnalogValues.ImbalanceSensor); }
        }

        /// <summary>
        /// Whether or not the 60V power supply is in a valid state (e.g. not undervoltage)
        /// </summary>
        public bool PowerSupplyStatus
        {
            get { return PowerSupplyVoltage > UnderVoltageThreshold; }
        }

        /// <summary>
        /// The voltage of the 60V power supply, in volts DC
        /// </summary>
        public double PowerSupplyVoltage
        {
            get {
                const double vdc_max_measurable = 108.6; // 108.6 Vdc max measurable on ISD860
                const double Kuf_m = 65472 / vdc_max_measurable; // (bits/Volts) Formula from Page 849 of MackDaddyTechnosoftDoc
                Int16 ad4_IU = 0;
                try
                {
                    var spindle = _axes[IndexSpindleAxis];
                    spindle.GetIntVariable("AD4", out ad4_IU);
                }
                catch (AxisException)
                {
                    // ... we got an error when querying for speed, probably not really connected.  Log the error!
                    // TODO -- log error
                }

                double bus_voltage = (double)((UInt16)ad4_IU) / Kuf_m;
                return bus_voltage;
            }
        }

        /// <summary>
        /// The current angle of the door
        /// </summary>
        public double DoorAngle
        {
            get {
                double eng_pos = 0.0;
                try
                {
                    var shield = _axes[IndexShieldAxis];
                    eng_pos = shield.GetPositionMM();
                }
                catch (AxisException)
                {
                    // ... we got an error when querying for speed, probably not really connected.  Log the error!
                    // TODO -- log error
                }
                return eng_pos;
            }
        }

        /// <summary>
        /// The current angle of the rotor
        /// </summary>
        public double RotorAngle
        {
            get {
                double deg = 0.0;
                try
                {
                    var spindle = _axes[IndexSpindleAxis];
                    deg = spindle.GetPositionMM();
                }
                catch (AxisException)
                {
                    // ... we got an error when querying for speed, probably not really connected.  Log the error!
                    // TODO -- log error
                }

                return deg;
            }
        }
        /// <summary>
        /// Reads the specified analog channel, 0 based, from the rotor controller
        /// </summary>
        /// <returns></returns>
        private int ReadRotorAnalog(int index)
        {
            return 0;
        }

        /// <summary>
        /// Reads the specified analog channel, 0 based, from the door controller
        /// </summary>
        /// <returns></returns>
        private int ReadDoorAnalog(int index)
        {
            return index;
        }

        /// <summary>
        /// Reads the specified digital input, 0 based, from the rotor controller
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private bool ReadRotorDigitalInput(int index)
        {
            return false;
        }

        /// <summary>
        /// Reads the specified digital input, 0 based, from the door controller
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private bool ReadDoorDigitalInput(int index)
        {
            return false;
        }

        /// <summary>
        /// Homes the spindle axis, and wait for completion
        /// </summary>
        public void RotorServoHome()
        {
            try
            {
                var spindle = _axes[IndexSpindleAxis];
                spindle.Home(wait_for_complete: true);
            }
            catch (Exception ex)
            {
                // TODO -- log error
                throw ex;
            }
        }

        /// <summary>
        /// Enables or Disables the Rotor axis servo controller in terms of AXISON or AXISOFF TML command
        /// </summary>
        /// <param name="turn_on"></param>
        public void RotorServoOnOff(bool turn_on)
        {
            try
            {
                var spindle = _axes[IndexSpindleAxis];
                spindle.Enable(enable: turn_on, blocking: true);
            }
            catch (Exception ex)
            {
                // TODO -- log error
                throw ex;
            }
        }

        /// <summary>
        /// Homes the Shield axis, and wait for completion
        /// </summary>
        public void ShieldServoHome()
        {
            if( NoShieldMode) {
                return;
            }

            try
            {
                var Shield = _axes[IndexShieldAxis];
                Shield.Home(wait_for_complete: true);
            }
            catch (Exception ex)
            {
                // TODO -- log error
                throw (ex);
            }
        }

        /// <summary>
        /// Enables or Disables the Shield axis servo controller in terms of AXISON or AXISOFF TML command
        /// </summary>
        /// <param name="turn_on"></param>
        public void ShieldServoOnOff(bool turn_on)
        {
            try
            {
                var Shield = _axes[IndexShieldAxis];
                Shield.Enable(enable: turn_on, blocking: true);
            }
            catch (Exception ex)
            {
                // TODO -- log error
                throw ex;
            }
        }

        /// <summary>
        /// Jogs the rotor either CW or CCW by the specified number of degrees
        /// </summary>
        /// <param name="relative_degrees"></param>
        public void JogRotor(double relative_degrees)
        {
            try
            {
                var spindle = _axes[IndexSpindleAxis];
                spindle.MoveRelative(relative_degrees, velocity: 600.0, acceleration: 360.0, jerk: 30);
            }
            catch (AxisException)
            {
                // ... we got an error when querying for speed, probably not really connected.  Log the error!
                // TODO -- log error
            }
        }

        /// <summary>
        /// Jogs the door either up or down by the specified number of degrees
        /// </summary>
        /// <param name="relative_deg"></param>
        public void JogDoor(double relative_deg)
        {
            try
            {
                var shield = _axes[IndexShieldAxis];
                shield.MoveRelative(relative_deg, velocity: 250.0, acceleration: 1000.0, jerk: 225);
            }
            catch (AxisException)
            {
                // ... we got an error when querying for speed, probably not really connected.  Log the error!
                // TODO -- log error
            }
        }
        /// <summary>
        /// Engineering function to start/stop datalogging the axes
        /// </summary>
        public void StartLogging(bool start = true)
        {
            if (start && LoggingHelper == null)
            {
                SetupLogging();
                LoggingHelper = new ThreadedUpdates(thread_name: "HiG datalogger", callback: LogThread, update_frequency_ms: 10);
                LoggingHelper.Start();
            }
            else if (!start && LoggingHelper != null)
            {
                LoggingHelper.Stop();
                LoggingHelper = null;
            }
        }

        /// <summary>
        /// Does whatever configuration is necessary on the TSM side to support data logging
        /// </summary>
        private void SetupLogging()
        {
        }

        /// <summary>
        /// This function should ONLY contain the code necessary for periodic
        /// queries to the controller to get the data we want.  You must set up
        /// the datalogging from SetupLogging()!
        /// </summary>
        private void LogThread()
        {

        }
        #endregion


        internal void ResetPauseAbort()
        {
            if( _connection != null)
                _connection.ResetPauseAbort();
        }

        public void ReprogramSpindle()
        {
            try
            {
                var tsmSetupFolder = DeviceProperties[Properties.ConfigFolder].ToAbsoluteAppPath();
                String swFilePath = String.Format(@"{0}\{1}.sw", tsmSetupFolder, SpindleAxis.GetID());
                SpindleAxis.DownloadSwFile(swFilePath);
                SpindleAxis.ResetDrive(); // reset drive to apply new motor settings and TML program
            }
            catch (Exception ex)
            {
                // TODO -- log error
                throw ex;
            }
        }

        public void ReprogramShield()
        {
            try
            {
                var tsmSetupFolder = DeviceProperties[Properties.ConfigFolder].ToAbsoluteAppPath();
                String swFilePath = String.Format(@"{0}\{1}.sw", tsmSetupFolder, ShieldAxis.GetID());
                ShieldAxis.DownloadSwFile(swFilePath);
                ShieldAxis.ResetDrive(); // reset drive to apply new motor settings and TML program
            }
            catch (Exception ex)
            {
                // TODO -- log error
                throw ex;
            }
        }

        public void WriteSerialNumber(string serial_number)
        {
            SpindleAxis.WriteSerialNumber(serial_number);
        }

        public string ReadSerialNumber()
        {
            return SpindleAxis.ReadSerialNumber();
        }

        public bool FirmwareNeedsElposlHome()
        {
            return SpindleAxis.FirmwareMajorVersion == 1 && SpindleAxis.FirmwareMinorVersion < 5;
        }
        
        public IList<EepromSetting> GetEepromSettings( bool default_values_only=false)
        {
            if( SpindleAxis == null)
                return new List<EepromSetting>();

            // Spindle Variables stored in EEPROM
            int spindle_my_homepos = 0; // 0 = this might be true if the assembly of the encoder disc to the encoder shaft is perfect (probably not)
            int spindle_elposl_home = 0; // 0 = this will never be correct
            short spindle_tstat_high = (short)HigUtils.ConvertDriveTemperature_degC_to_IU(35.0); // 16864 = 35 degC for ISD860
            short spindle_tstat_low = (short)HigUtils.ConvertDriveTemperature_degC_to_IU(33.0); // 16467 = 33 degC for ISD860
            short imb_ampl_max = (short)HigUtils.ConvertImbalanceAmplitutde_mm_to_IU(1.00); // 10912 = 1.00 mm for 1..4mm sensor hooked up to -10..+10V ADC on ISD860 (see imb_mm in SpinStateMachine.cs for conversion)
            short bucket2_offset = (short)SpindleAxis.ConvertToCounts(180.0); // 8192 = 180.000 degrees from bucket 1 with 4096 line encoder
            double rotational_radius_mm = 110.0; // 110.0 mm = standard from center of spindle to bucket floor in the center of plate pad

            // Shield Variables stored in EEPROM
            // DKM 2011-11-14 new defaults for door open position
            int door_open_pos = 378000;// ShieldAxis.ConvertToCounts(-117.4); // -184882 = -117.4 mm on HG2 with 5000 line encoder
            int door_close_pos = -3000;

            if (!Simulating && !default_values_only) {
                try {
                    // Get Spindle Variables stored in EEPROM
                    spindle_my_homepos = SpindleAxis.ReadLongVarEEPROM("my_homepos_ptr");
                    spindle_elposl_home = SpindleAxis.ReadLongVarEEPROM("elposl_home_ptr");
                    spindle_tstat_high = SpindleAxis.ReadIntVarEEPROM("tstat_high_ptr");
                    spindle_tstat_low = SpindleAxis.ReadIntVarEEPROM("tstat_low_ptr");
                    imb_ampl_max = SpindleAxis.ReadIntVarEEPROM("imb_ampl_max_ptr");
                    bucket2_offset = SpindleAxis.ReadIntVarEEPROM("bucket2_offset_ptr");
                    rotational_radius_mm = SpindleAxis.ReadFixedVarEEPROM("rotational_radius_mm_ptr");

                    // Get Shield Variables stored in EEPROM
                    door_open_pos = ShieldAxis.ReadLongVarEEPROM("door_open_pos_ptr");
                    door_close_pos = ShieldAxis.ReadLongVarEEPROM("door_close_pos_ptr");
                } catch (AxisException) {
                    // TODO -- log error
                }
            }

            var eeprom_settings = new List<EepromSetting>() { 
                                    new EepromSetting( "Serial number", WriteSerialNumber, ReadSerialNumber),
                                    new EepromSetting( "Spindle my_HomePos [cnts]", "my_homepos_ptr", EepromSetting.VariableTypeT.Long, spindle_my_homepos.ToString(), true),
                                    //new EepromSetting( "Spindle elposl_home [cnts]", "elposl_home_ptr", EepromSetting.VariableTypeT.Long, spindle_elposl_home.ToString(), true),
                                    new EepromSetting( "TStat High [IU]", "tstat_high_ptr", EepromSetting.VariableTypeT.Int, spindle_tstat_high.ToString(), true),
                                    new EepromSetting( "TStat Low [IU]", "tstat_low_ptr", EepromSetting.VariableTypeT.Int, spindle_tstat_low.ToString(), true),
                                    new EepromSetting( "Imbalance Amp Max [IU]", "imb_ampl_max_ptr", EepromSetting.VariableTypeT.Int, imb_ampl_max.ToString(), true),
                                    new EepromSetting( "Bucket #2 position [cnts from Bucket #1]", "bucket2_offset_ptr", EepromSetting.VariableTypeT.Int, bucket2_offset.ToString(), true),
                                    new EepromSetting( "Rotational radius [mm]", "rotational_radius_mm_ptr", EepromSetting.VariableTypeT.Fixed, rotational_radius_mm.ToString(), true),

                                    new EepromSetting( "Shield open position [cnts]", "door_open_pos_ptr", EepromSetting.VariableTypeT.Long, door_open_pos.ToString(), false),
                                    new EepromSetting( "Shield closed position [cnts]", "door_close_pos_ptr", EepromSetting.VariableTypeT.Long, door_close_pos.ToString(), false)
                                  };

            if( FirmwareNeedsElposlHome()) {
                eeprom_settings.Add( new EepromSetting( "Spindle elposl_home [cnts]", "elposl_home_ptr", EepromSetting.VariableTypeT.Long, spindle_elposl_home.ToString(), true));
            }

            return eeprom_settings;
        }
    }
}
