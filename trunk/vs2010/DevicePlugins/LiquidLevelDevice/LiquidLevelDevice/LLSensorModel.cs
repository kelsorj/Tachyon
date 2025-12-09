using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.Utils;
using BioNex.Shared.Utils.WellMathUtil;
using log4net;

namespace BioNex.LiquidLevelDevice
{
    // ILLSensorModel is the lowest level device interface, interfacing between the hardware and higher level operations or plugin modules
    public enum TeachpointType { Capture, Portrait, Landscape };

    public interface ILLSensorModel
    {
        string InstanceName { get; set; }
        LLProperties Properties { get; set; }
        uint SensorCount { get; }
        ILevelSensor[] Sensors { get; }
        int InterSensorDelayms { get; }
        int RunCounter { get; }

        int ParallelGroupSize { get; }

        ILabwareDatabase LabwareDatabase { get; set; }
        ILiquidLevelVolumeMapDatabase VolumeMapDatabase { get; }

        double XYSlope { get; set; }
        double ZYSlope { get; set; }
        double[] XArcCorrection { get; set; }

        IAxis XAxis { get; }
        IAxis YAxis { get; }
        IAxis ZAxis { get; }
        IAxis RAxis { get; }
        double XAxisVelocity { get; set; }
        double XAxisAcceleration { get; set; }
        double YAxisVelocity { get; set; }
        double YAxisAcceleration { get; set; }
        double ZAxisVelocity { get; set; }
        double ZAxisAcceleration { get; set; }
        double RAxisVelocity { get; set; }
        double RAxisAcceleration { get; set; }

        double PlateDX { get; }
        double PlateDY { get; }

        event ConnectionStateChangedEventHandler ConnectionStateChangedEvent;
        event DisconnectingEventHandler DisconnectingEvent;

        event IntegerSensorReadingReceivedEventHandler IntegerSensorReadingReceivedEvent;
        void FireIntegerSensorReadingReceivedEvent(int index, int value);

        event SavePropertiesEventHandler SavePropertiesEvent;
        void FireSavePropertiesEvent();

        event CaptureStartEventHandler CaptureStartEvent;
        event CaptureProgressEventHandler CaptureProgressEvent;
        event CaptureStopEventHandler CaptureStopEvent;
        void FireCaptureStartEvent(bool summarize, string labware);
        void FireCaptureProgressEvent(IList<Measurement> measurement);
        void FireCaptureStopEvent();

        string[] LastConfig { get; }

        bool Connected { get; }
        bool SensorsConnected { get; }
        bool MotorsConnected { get; }
        void Connect();
        void Disconnect();

        void ReadAllInteger();

        void StartPeriodicRead();
        void ReadPeriodic();
        void StopPeriodicRead();

        void Calibrate();

        List<Averages> Capture(string labware_name);
        IDictionary<Coord, List<Measurement>> HiResScan(bool fast, string labware_name);
        IDictionary<Coord, List<Measurement>>[] LocateTeachpoint();
        IDictionary<Coord, List<Measurement>> XYAlignmentScan(bool reset_slope);
        IDictionary<Coord, List<Measurement>> ZYAlignmentScan(bool reset_slope);
        IList<IDictionary<Coord, List<Measurement>>> XArcCorrectionScan(bool reset_correction);

        void Abort();

        bool IsHomed { get; }
        void Home();

        double GetAxisPositionMM(int index);
        void MoveRelativeToTeachpoint(double x, double y, double z, bool wait_for_move_complete = true);
        void MoveToPark(TeachpointType tpt = TeachpointType.Portrait);

        void TeachHere(TeachpointType tpt);
        void OffsetTeachpoint(double x, double y);
        void SaveSensorDeviations(double[] x, double[] y);

        Averages CalculateWellAverages(List<Measurement> values);
        List<double> GetVolumesFromAverages(string labware, List<Averages> values);

        void GetLabwareData(string labware_name, out int columns, out int rows, out double col_spacing, out double row_spacing, out double thickness, out double well_radius);

        // sensitivty mode -- keep all sensors at same sensitivity mode, so this goes in model interface, not individual sensor interface
        void SetSensitivity(string mode);

    }

    // events
    public delegate void CaptureStartEventHandler(object sender, bool summarize, string labware);
    public delegate void CaptureProgressEventHandler(object sender, IList<Measurement> value);
    public delegate void CaptureStopEventHandler(object sender);
    public delegate void ConnectionStateChangedEventHandler(object sender, int index, bool connected, string status = "disconnected");
    public delegate void DisconnectingEventHandler(object sender, int index);
    public delegate void IntegerSensorReadingReceivedEventHandler(object sender, int index, int value);
    public delegate void SavePropertiesEventHandler(object sender, IDictionary<string, string> properties);

    public static class LLSensorModelConsts
    {
        public const int XAxis = 0;
        public const int YAxis = 1;
        public const int ZAxis = 2;
        public const int RAxis = 3;
        public const string DefaultSensitivity = "D";
    }

    public class Measurement
    {
        public int channel;
        public int row;
        public int column;
        public double x;
        public double y;
        public double measured_value;

        public Measurement(int channel, int row, int column, double x, double y, double measured_value)
        {
            this.channel = channel;
            this.row = row;
            this.column = column;
            this.x = x;
            this.y = y;
            this.measured_value = measured_value;
        }
    }

    public class Coord : IComparable<Coord>
    {
        public int channel { get; private set; }
        public int row { get; private set; }
        public int column { get; private set; }
        public Coord(int channel, int row, int column)
        {
            this.channel = channel;
            this.row = row;
            this.column = column;
        }
        public override bool Equals(object obj)
        {
            var foo = obj as Coord;
            if (foo == null)
                return false;
            return this.channel == foo.channel && this.column == foo.column && this.row == foo.row;
        }
        public override int GetHashCode()
        {
            return column * 1000 + row * 10 + channel;
        }

        public int CompareTo(Coord other)
        {
            return GetHashCode() - other.GetHashCode();
        }
    }

    public class Averages : IBeeSureCaptureResults
    {
        public int Channel { get { return _channel; } }
        public int Column { get { return _column; } }
        public int Row { get { return _row; } }
        public double XAverage { get { return _x_average; } }
        public double YAverage { get { return _y_average; } }
        public double PopulationAverage { get { return _population_average; } }
        public double StandardDeviation { get { return _standard_deviation; } }
        public double Average { get { return _average; } }

        int _channel;
        int _column;
        int _row;
        double _x_average;
        double _y_average;
        double _population_average;
        double _standard_deviation;
        double _average;
        public Averages(int channel, int column, int row, double x, double y, double population_average, double standard_deviation, double average)
        {
            _channel = channel;
            _column = column;
            _row = row;
            _x_average = x;
            _y_average = y;
            _population_average = population_average;
            _standard_deviation = standard_deviation;
            _average = average;
        }
        public Averages() { }
    }

    class LLSensorModel : ILLSensorModel//, IFirmwareUpdateable GB 12-5-2 i don't like the idea of embedding a firmware version in the assembly
    {
        ILLSensorPlugin _owner;
        public ILabwareDatabase LabwareDatabase
        {
            get { return _owner.LabwareDatabase; }
            set { _owner.LabwareDatabase = value; }
        }

        ILiquidLevelVolumeMapDatabase _volume_map_db;
        public ILiquidLevelVolumeMapDatabase VolumeMapDatabase { get { return _volume_map_db; } }

        public LLSensorModel(ILLSensorPlugin owner)
        {
            _owner = owner; // context object for events
        }

        public string InstanceName { get; set; }
        LLProperties _properties;
        public LLProperties Properties { get { return _properties; } set { _properties = value; PropertiesChanged(); } }
        public event ConnectionStateChangedEventHandler ConnectionStateChangedEvent;
        public event DisconnectingEventHandler DisconnectingEvent;

        public event IntegerSensorReadingReceivedEventHandler IntegerSensorReadingReceivedEvent;
        public void FireIntegerSensorReadingReceivedEvent(int index, int value)
        {
            if (IntegerSensorReadingReceivedEvent != null)
                IntegerSensorReadingReceivedEvent(_owner, index, value);
        }

        public double XYSlope { get { return Properties.GetDouble(LLProperties.XYSlope); } set { Properties[LLProperties.XYSlope] = value.ToString(); FireSavePropertiesEvent(); } }
        public double ZYSlope { get { return Properties.GetDouble(LLProperties.ZYSlope); } set { Properties[LLProperties.ZYSlope] = value.ToString(); FireSavePropertiesEvent(); } }

        public double[] XArcCorrection
        {
            get
            {
                return new double[] { 
                    Properties.GetDouble(LLProperties.XArcCorrection0), 
                    Properties.GetDouble(LLProperties.XArcCorrection1), 
                    Properties.GetDouble(LLProperties.XArcCorrection2) };
            }
            set
            {
                Properties[LLProperties.XArcCorrection0] = value[0].ToString();
                Properties[LLProperties.XArcCorrection1] = value[1].ToString();
                Properties[LLProperties.XArcCorrection2] = value[2].ToString();
                FireSavePropertiesEvent();
            }
        }


        void PropertiesChanged()
        {
            string volume_map_path = VolumeMapPath + "\\LiquidLevelVolumeMap.db3";
            _volume_map_db = new LiquidLevelVolumeMapDatabase(volume_map_path);

            if (LastConfig == null)
            {
                LastConfig = new string[SensorCount];
                for (int i = 0; i < SensorCount; ++i)
                    LastConfig[i] = "disconnected";
            }
        }

        public int RunCounter { get; private set; }

        private static readonly ILog _log = LogManager.GetLogger(typeof(LLSensorModel));

        public uint SensorCount { get { return Properties.GetUInt(LLProperties.SensorCount); } }
        public int SensorCANDeviceID { get { return Properties.GetInt(LLProperties.SensorCANDeviceID); } } // -1 means use RS232 directly
        ILevelSensor[] _sensors;
        public ILevelSensor[] Sensors { get { return _sensors; } }
        public int InterSensorDelayms { get { return 25; } }

        public string[] LastConfig { get; private set; }

        TechnosoftConnection _connection;
        List<bool> _homed;
        List<IAxis> _axes;
        public IAxis XAxis { get { return _axes == null ? null : _axes[LLSensorModelConsts.XAxis]; } }
        public IAxis YAxis { get { return _axes == null ? null : _axes[LLSensorModelConsts.YAxis]; } }
        public IAxis ZAxis { get { return _axes == null ? null : _axes[LLSensorModelConsts.ZAxis]; } }
        public IAxis RAxis { get { return _axes == null ? null : !HasRAxis ? null : _axes[LLSensorModelConsts.RAxis]; } }
        public double XAxisVelocity { get; set; }
        public double XAxisAcceleration { get; set; }
        public double YAxisVelocity { get; set; }
        public double YAxisAcceleration { get; set; }
        public double ZAxisVelocity { get; set; }
        public double ZAxisAcceleration { get; set; }
        public double RAxisVelocity { get; set; }
        public double RAxisAcceleration { get; set; }

        public double PlateDX 
        {
            get
            {
                double fiducial_x = Properties.GetDouble(LLProperties.LocateTeachpointPinToEdgeX) + 0.5 * Properties.GetDouble(LLProperties.LocateTeachpointFeatureWidthX);
                double dx = Properties.GetDouble(LLProperties.LocateTeachpointPinToWellX) - fiducial_x;
                return dx;
            }

        }
        public double PlateDY
        {
            get
            {
                double fiducial_y = Properties.GetDouble(LLProperties.LocateTeachpointPinToEdgeY) + 0.5 * Properties.GetDouble(LLProperties.LocateTeachpointFeatureWidthY) - Properties.GetDouble(LLProperties.LocateTeachpointFeatureToFeatureY);
                double dy = Properties.GetDouble(LLProperties.LocateTeachpointPinToWellY) - fiducial_y;
                return dy;
            }
        }

        public bool HasRAxis { get { return Properties.GetBool(LLProperties.HasRAxis); } }

        public int ParallelGroupSize { get { return Properties.GetInt(LLProperties.ParallelGroupSize); } }

        public int MotorCANDeviceID { get { return Properties.GetInt(LLProperties.MotorCANDeviceID); } }
        public string ConfigPath { get { return Properties.GetString(LLProperties.ConfigFolder).ToAbsoluteAppPath(); } }

        public string MotorSettingsFileName { get { return "motor_settings.xml"; } }
        public string MotorSettingsPath { get { return (Properties.GetString(LLProperties.ConfigFolder) + "\\" + MotorSettingsFileName).ToAbsoluteAppPath(); } }

        public string VolumeMapPath { get { return Properties.GetString(LLProperties.VolumeMapFolder).ToAbsoluteAppPath(); } }

        public bool SensorsConnected { get; private set; }
        public bool MotorsConnected { get; private set; }
        public bool Connected { get; private set; }

        public void Connect()
        {
            Disconnect();
            bool simulate = Properties.GetBool(LLProperties.Simulate);
            MotorsConnected = ReloadMotorSettings(simulate);

            _sensors = new ILevelSensor[SensorCount];
            LastConfig = new string[SensorCount];
            bool reset_first = true;

            var action = new Action<int>(i =>
            {
                LastConfig[i] = "disconnected";
                var enabled = Properties.GetBool(LLProperties.index(LLProperties.Enable, i));
                if (!enabled)
                    return;

                // reset the bus on the first enabled sensor -- resetting ensures that we're not stuck in Periodic mode during a connect attempt, and doing it wih this bool flag ensures that we only
                // send one NMT RESET / STARTUP message, so the nodes LEDs stay synchronized 
                bool reset = false;
                lock (this)
                {
                    if (reset_first)
                    {
                        reset = reset_first;
                        reset_first = false;
                    }
                }

                var port = Properties.GetInt(LLProperties.index(LLProperties.Port, i));
                try
                {
                    _sensors[i] = new LevelSensor(SensorCANDeviceID, port, simulate, reset);

                }
                catch (Exception e)
                {
                    _log.Error(string.Format("LiquidLevelDevice: Error opening port {0}.", port), e);
                    _sensors[i] = null;
                    return;
                }

                if (simulate) return; // no need to do anything else for simulated sensors

                // before anything else, try to shut off fast read mode, since the spam this generates prevents any other comms
                _sensors[i].GetResponse(LevelSensor.Commands[(int)LevelSensor.Command.PeriodicOff]);

                string response = "";
                bool requires_calibration = false;
                bool valid_config = ReadConfiguration(i, port, out requires_calibration, out response);
                if (!valid_config)
                {
                    _sensors[i].Close();
                    _sensors[i] = null;
                    return;
                }

                int calibration_tries = 0;
                const int MAX_TRIES = 3;
                while (requires_calibration)
                {
                    if (++calibration_tries > MAX_TRIES)
                    {
                        _log.Error(string.Format("LiquidLevelDevice: Error, port {0} will not accept a new configuration.", port));
                        _sensors[i].Close();
                        _sensors[i] = null;
                        return;
                    }

                    try
                    {
                        string config = string.Format("{0}{1}{2}{3}{4}{5}}}"
                                , LevelSensor.Commands[(int)LevelSensor.Command.SetConfiguration]
                                , Properties.GetString(LLProperties.index(LLProperties.Mode, i))
                                , Properties.GetString(LLProperties.index(LLProperties.Format, i))
                                , Properties.GetString(LLProperties.index(LLProperties.Sensitivity, i))
                                , Properties.GetString(LLProperties.index(LLProperties.Averaging, i))
                                , Properties.GetString(LLProperties.index(LLProperties.TemperatureCompensation, i))
                                );
                        response = _sensors[i].GetResponse(config);
                    }
                    catch (KeyNotFoundException e)
                    {
                        _log.Error(string.Format("LiquidLevelDevice: Error connecting to port {0}, couldn't read configuration data from database.", port), e);
                        _sensors[i].Close();
                        _sensors[i] = null;
                        return;
                    }

                    valid_config = ReadConfiguration(i, port, out requires_calibration, out response);
                    if (!valid_config)
                    {
                        _sensors[i].Close();
                        _sensors[i] = null;
                        return;
                    }
                }

                try
                {
                    _sensors[i].LoadCalibration(ConfigPath);
                }
                catch (Exception e)
                {
                    _log.Error(string.Format("LiquidLevelDevice: Failed to load calibration file for sensor {0}. You must recalibrate sensors.", i), e);
                }

                try
                {
                    LastConfig[i] = response;
                    if (ConnectionStateChangedEvent != null)
                        ConnectionStateChangedEvent(_owner, i, true, response);
                }
                catch (Exception e)
                {
                    _log.Error(string.Format("LiquidLevelDevice: Error processing response from port {0}.", port), e);
                    _sensors[i].Close();
                    _sensors[i] = null;
                    return;
                }
            });

            // only connect to 1 device at a time, otherwise there's a race between the bus reset and device configuration, and we get retry messages
            for (int i = 0; i < _sensors.Length; ++i)// += ParallelGroupSize)
                System.Threading.Tasks.Parallel.For(i, i + 1, action); //+ ParallelGroupSize, action);


            //if (MotorsConnected)
            //    MotorsConnected = HomeIfNecessary();
            //if (MotorsConnected)
            //    MoveToPark();

            bool any_sensors = false;
            SensorsConnected = true;
            foreach (var sensor in _sensors)
            {
                if (sensor == null)
                    continue;
                any_sensors = true;
                SensorsConnected &= sensor.Connected;
            }
            SensorsConnected &= (any_sensors || _sensors.Count() == 0);

            if (!MotorsConnected)
                _log.Warn("LiquidLevelDevice: Not all motors are responding");
            else
            {
                string serial = "(null)";
                try
                {
                    if (XAxis != null) serial = XAxis.ReadSerialNumber();
                    if (string.IsNullOrWhiteSpace(serial)) serial = "(null)";
                }
                catch (Exception) { }
                _log.DebugFormat("Connected to {0} '{1}'", BioNexDeviceNames.BeeSure, InstanceName);
                _log.DebugFormat("Serial number '{0}'", serial);
                _log.DebugFormat("X Axis firmware version '{0}'", XAxis != null ? XAxis.FirmwareVersion : "(null)");
                _log.DebugFormat("Y Axis firmware version '{0}'", YAxis != null ? YAxis.FirmwareVersion : "(null)");
                _log.DebugFormat("Z Axis firmware version '{0}'", ZAxis != null ? ZAxis.FirmwareVersion : "(null)");
                if (HasRAxis)
                    _log.DebugFormat("T Axis firmware version '{0}'", RAxis != null ? RAxis.FirmwareVersion : "(null)");
            }
            if (!SensorsConnected)
                _log.Warn("LiquidLevelDevice: Some sensors are not connected");

            Connected = (MotorsConnected && SensorsConnected) || (simulate && MotorsConnected);
            if (Connected)
                _log.Info(string.Format("LiquidLevelDevice: Connected to device at CAN ID ({0}, {1})", MotorCANDeviceID, SensorCANDeviceID));
        }

        public void Disconnect()
        {
            Connected = false;

            // abort any running state machines
            Abort();

            if (_connection != null && MotorsConnected)
                _connection.Close();

            MotorsConnected = false;

            if (_sensors == null || !SensorsConnected)
                return;

            SensorsConnected = false;

            var action = new Action<int>(i =>
            {
                LastConfig[i] = "disconnected";
                if (_sensors[i] == null)
                    return;

                var port = _sensors[i].Name;
                try
                {
                    if (DisconnectingEvent != null)
                        DisconnectingEvent(_owner, i);

                    _sensors[i].Close();
                    _sensors[i] = null;

                    if (ConnectionStateChangedEvent != null)
                        ConnectionStateChangedEvent(_owner, i, false);
                }
                catch (System.InvalidOperationException e)
                {
                    _log.Error(string.Format("LiquidLevelDevice: Error closing port {0}", port), e);
                    _sensors[i] = null;
                }
            });

            for (int i = 0; i < _sensors.Length; i += ParallelGroupSize)
                System.Threading.Tasks.Parallel.For(i, i + ParallelGroupSize, action);

            _log.Info(string.Format("LiquidLevelDevoce: Disconnected from device at CAN ID ({0}, {1})", MotorCANDeviceID, SensorCANDeviceID));
        }

        public void ReadAllInteger()
        {
            GetReading(false);
        }

        void GetReading(bool periodic_read)
        {
            if (_sensors == null)
                return;

            var action = new Action<int>(i =>
            {
                if (i > _sensors.Length || _sensors[i] == null)
                    return;

                Stopwatch start = Stopwatch.StartNew();
                try
                {
                    var value = _sensors[i].GetReading(3, periodic_read);
                    _log.Debug(string.Format("Sensor {0} completed read in {1} seconds", i, start.Elapsed));

                    if (IntegerSensorReadingReceivedEvent != null)
                        IntegerSensorReadingReceivedEvent(_owner, i, value);
                }
                catch (Exception e)
                {
                    _log.Error(string.Format("LiquidLevelDevice: Error when trying to read sensor {0}", i), e);
                }
                finally
                {
                    start.Stop();
                }
            });

            for (int i = 0; i < _sensors.Length; i += ParallelGroupSize)
                System.Threading.Tasks.Parallel.For(i, i + ParallelGroupSize, action);
        }

        public void StartPeriodicRead()
        {
            if (_sensors == null)
                return;

            bool simulate = Properties.GetBool(LLProperties.Simulate);
            if (simulate)
                return;

            var action = new Action<int>(i =>
            {
                if (i > _sensors.Length || _sensors[i] == null)
                    return;
                try
                {
                    _sensors[i].GetResponse(LevelSensor.Commands[(int)LevelSensor.Command.PeriodicOn]);
                    _log.Debug(string.Format("Sensor {0} start periodic read", i));
                }
                catch (Exception e)
                {
                    _log.Error(string.Format("LiquidLevelDevice: Error when trying to start periodic read on sensor {0}", i), e);
                }
            });

            for (int i = 0; i < _sensors.Length; i += ParallelGroupSize)
                System.Threading.Tasks.Parallel.For(i, i + ParallelGroupSize, action);
        }

        public void StopPeriodicRead()
        {
            if (_sensors == null)
                return;

            bool simulate = Properties.GetBool(LLProperties.Simulate);
            if (simulate)
                return;

            var action = new Action<int>(i =>
            {
                if (i > _sensors.Length || _sensors[i] == null)
                    return;
                try
                {
                    _sensors[i].GetResponse(LevelSensor.Commands[(int)LevelSensor.Command.PeriodicOff]);
                    _log.Debug(string.Format("Sensor {0} stop periodic read", i));
                }
                catch (Exception e)
                {
                    _log.Error(string.Format("LiquidLevelDevice: Error when trying to stop periodic read on sensor {0}", i), e);
                }
            });

            for (int i = 0; i < _sensors.Length; i += ParallelGroupSize)
                System.Threading.Tasks.Parallel.For(i, i + ParallelGroupSize, action);
        }

        public void ReadPeriodic()
        {
            GetReading(true);
        }

        public void Abort()
        {
            // abort any running operation
            AbortCalibration();
            AbortCapture();
            AbortHiResScan();
            AbortLocateTeachpoint();
            AbortXYAlignmentScan();
            AbortZYAlignmentScan();
            AbortXArcCorrectionScan();
        }

        ManualResetEvent _calibration_complete = new ManualResetEvent(false);
        LLSCalibrationStateMachine _calibration_sm;
        public void Calibrate()
        {
            _calibration_complete.Reset();
            _calibration_sm = new LLSCalibrationStateMachine(this);

            if (LabwareSafetyCheck(""))
                _calibration_sm.Start();
            
            _calibration_sm = null;
            _calibration_complete.Set();
        }
        void AbortCalibration()
        {
            if (_calibration_sm == null)
                return;
            _calibration_sm.Abort();
            _calibration_complete.WaitOne();
        }

        public event CaptureStartEventHandler CaptureStartEvent;
        public void FireCaptureStartEvent(bool summarize, string labware)
        {
            if (CaptureStartEvent != null)
                CaptureStartEvent(_owner, summarize, labware);
        }
        public event CaptureProgressEventHandler CaptureProgressEvent;
        public void FireCaptureProgressEvent(IList<Measurement> measurements)
        {
            if (CaptureProgressEvent != null)
                CaptureProgressEvent(_owner, measurements);
        }
        public event CaptureStopEventHandler CaptureStopEvent;
        public void FireCaptureStopEvent()
        {
            if (CaptureStopEvent != null)
                CaptureStopEvent(_owner);
        }

        CaptureDataFile _data_logger = null;
        ManualResetEvent _capture_complete = new ManualResetEvent(false);
        LLSCaptureStateMachine _capture_sm;
        public List<Averages> Capture(string labware_name)
        {
            ++RunCounter;
            _capture_complete.Reset();

            if (_data_logger == null || Properties.GetBool(LLProperties.NewFilePerCapture))
                _data_logger = new CaptureDataFile(Properties);

            _capture_sm = new LLSCaptureStateMachine(this, labware_name, _data_logger);
            
            if( LabwareSafetyCheck(labware_name))
                _capture_sm.Start();

            var result = _capture_sm.Averages != null ? new List<Averages>(_capture_sm.Averages) : new List<Averages>();
            
            _capture_sm = null;
            _capture_complete.Set();
            return result;
        }
        void AbortCapture()
        {
            if (_capture_sm == null)
                return;
            _capture_sm.ManualAbort();
            _capture_complete.WaitOne();
        }

        LLSHiResScanStateMachine _hires_scan_sm;
        LLSFastHiResScanStateMachine _fast_hires_scan_sm;
        ManualResetEvent _hires_scan_complete = new ManualResetEvent(false);
        public IDictionary<Coord, List<Measurement>> HiResScan(bool fast, string labware_name)
        {
            ++RunCounter;
            _hires_scan_complete.Reset();

            SortedDictionary<Coord, List<Measurement>> result;
            if (fast)
            {
                _fast_hires_scan_sm = new LLSFastHiResScanStateMachine(this, new LLSHiResScanStateMachine.HiResScanParams(this, labware_name, true, true, Properties.GetBool(LLProperties.HiResFloorToZero), true));

                if(LabwareSafetyCheck(labware_name))
                    _fast_hires_scan_sm.Start();
                
                result = _fast_hires_scan_sm.Measurements != null ? new SortedDictionary<Coord, List<Measurement>>(_fast_hires_scan_sm.Measurements) : new SortedDictionary<Coord, List<Measurement>>();
                _fast_hires_scan_sm = null;
            }
            else
            {
                _hires_scan_sm = new LLSHiResScanStateMachine(this, new LLSHiResScanStateMachine.HiResScanParams(this, labware_name, true, true, Properties.GetBool(LLProperties.HiResFloorToZero), true));

                if( LabwareSafetyCheck(labware_name))
                    _hires_scan_sm.Start();
                
                result = _hires_scan_sm.Measurements != null ? new SortedDictionary<Coord, List<Measurement>>(_hires_scan_sm.Measurements) : new SortedDictionary<Coord, List<Measurement>>();
                _hires_scan_sm = null;
            }
            _hires_scan_complete.Set();
            return result;
        }
        void AbortHiResScan()
        {
            if (_hires_scan_sm == null && _fast_hires_scan_sm == null)
                return;
            if( _hires_scan_sm != null)
                _hires_scan_sm.ManualAbort();
            else 
                _fast_hires_scan_sm.ManualAbort();
            _hires_scan_complete.WaitOne();
        }

        ManualResetEvent _locate_teachpoint_complete = new ManualResetEvent(false);
        LLSLocateTeachpointStateMachine _locate_teachpoint_sm;
        public IDictionary<Coord, List<Measurement>>[] LocateTeachpoint()
        {
            _locate_teachpoint_complete.Reset();
            _locate_teachpoint_sm = new LLSLocateTeachpointStateMachine(this);

            if( LabwareSafetyCheck(""))
                _locate_teachpoint_sm.Start();

            var result = (_locate_teachpoint_sm.X_Measurements == null || _locate_teachpoint_sm.Y_Measurements == null) ? null :
                new SortedDictionary<Coord, List<Measurement>>[] 
                    { 
                        new SortedDictionary<Coord, List<Measurement>>(_locate_teachpoint_sm.X_Measurements), 
                        new SortedDictionary<Coord, List<Measurement>>(_locate_teachpoint_sm.Y_Measurements)
                    };

            _locate_teachpoint_sm = null;
            _locate_teachpoint_complete.Set();
            return result;
        }
        void AbortLocateTeachpoint()
        {
            if (_locate_teachpoint_sm == null)
                return;
            _locate_teachpoint_sm.ManualAbort();
            _locate_teachpoint_complete.WaitOne();
        }

        double DEFAULT_ALIGN_RANGE = 3.0;
        double DEFAULT_ALIGN_STEP = 0.01;

        ManualResetEvent _xy_alignment_scan_complete = new ManualResetEvent(false);
        LLSHiResScanStateMachine _xy_alignment_scan_sm;
        public IDictionary<Coord, List<Measurement>> XYAlignmentScan(bool reset_slope)
        {
            _xy_alignment_scan_complete.Reset();


            var param = new LLSHiResScanStateMachine.HiResScanParams(this, "");

            param.min_x = -DEFAULT_ALIGN_RANGE; // Properties.GetDouble(LLProperties.HiResMinX);
            param.max_x = DEFAULT_ALIGN_RANGE; // Properties.GetDouble(LLProperties.HiResMaxX);
            param.step_x = DEFAULT_ALIGN_STEP; // Properties.GetDouble(LLProperties.HiResStepX);

            param.min_y = 0;
            param.max_y = 0;
            param.step_y = 1;

            param.first_column = 12 - 1;
            param.last_column = 1 - 1;
            param.step_column = 11;

            _xy_alignment_scan_sm = new LLSHiResScanStateMachine(this, param);

            if (LabwareSafetyCheck(""))
            {
                if (reset_slope)
                    XYSlope = 0.0;
                _xy_alignment_scan_sm.Start();
            }

            var result = _xy_alignment_scan_sm.Measurements == null ?  null :
                new SortedDictionary<Coord, List<Measurement>>(_xy_alignment_scan_sm.Measurements);
            
            _xy_alignment_scan_sm = null;
            _xy_alignment_scan_complete.Set();
            return result;
        }

        void AbortXYAlignmentScan()
        {
            if (_xy_alignment_scan_sm == null)
                return;
            _xy_alignment_scan_sm.ManualAbort();
            _xy_alignment_scan_complete.WaitOne();
        }

        ManualResetEvent _zy_alignment_scan_complete = new ManualResetEvent(false);
        LLSHiResScanStateMachine _zy_alignment_scan_sm;
        public IDictionary<Coord, List<Measurement>> ZYAlignmentScan(bool reset_slope)
        {
            _zy_alignment_scan_complete.Reset();

            var param = new LLSHiResScanStateMachine.HiResScanParams(this, "");

            param.min_x = 0;
            param.max_x = 0;
            param.step_x = 1;

            param.min_y = 0;
            param.max_y = 0;
            param.step_y = 1;

            param.first_column = 9 - 1;
            param.last_column = 4 - 1;
            param.step_column = 1;

            _zy_alignment_scan_sm = new LLSHiResScanStateMachine(this, param);

            if (LabwareSafetyCheck(""))
            {
                if (reset_slope)
                    ZYSlope = 0.0;
                _zy_alignment_scan_sm.Start();
            }

            var result = _zy_alignment_scan_sm.Measurements == null ? null :
                new SortedDictionary<Coord, List<Measurement>>(_zy_alignment_scan_sm.Measurements);

            _zy_alignment_scan_sm = null;
            _zy_alignment_scan_complete.Set();
            
            return result;
        }

        void AbortZYAlignmentScan()
        {
            if (_zy_alignment_scan_sm == null)
                return;
            _zy_alignment_scan_sm.ManualAbort();
            _zy_alignment_scan_complete.WaitOne();
        }

        ManualResetEvent _x_arc_correction_scan_complete = new ManualResetEvent(false);
        LLSXArcCorrectionScanStateMachine _x_arc_correction_scan_sm;
        public IList<IDictionary<Coord, List<Measurement>>> XArcCorrectionScan(bool reset_correction)
        {
            _x_arc_correction_scan_complete.Reset();
            _x_arc_correction_scan_sm = new LLSXArcCorrectionScanStateMachine(this);

            if (LabwareSafetyCheck(""))
            {
                if (reset_correction)
                    XArcCorrection = new double[] { 0.0, 0.0, 0.0 };
                _x_arc_correction_scan_sm.Start();
            }
            var result = _x_arc_correction_scan_sm.Measurements; // rooting the state machine yerg ... but i can't figure out how to make a copy of the complex type easily

            _x_arc_correction_scan_sm = null;
            _x_arc_correction_scan_complete.Set();
            
            return result;
        }

        void AbortXArcCorrectionScan()
        {
            if (_x_arc_correction_scan_sm == null)
                return;
            _x_arc_correction_scan_sm.ManualAbort();
            _x_arc_correction_scan_complete.WaitOne();
        }

        bool ReloadMotorSettings(bool simulate)
        {
            try
            {
                if (simulate || MotorCANDeviceID == -1)
                    _connection = new TechnosoftConnection();
                else
#if !TML_SINGLETHREADED
                    _connection = new TechnosoftConnection(MotorCANDeviceID.ToString(), TMLLibConst.CHANNEL_SYS_TEC_USBCAN, 500000);
#else
                    _connection = new TechnosoftConnection(MotorCANDeviceID.ToString(), TML.TMLLib.CHANNEL_SYS_TEC_USBCAN, 500000);
#endif

                // DKM 2012-03-02 extract motor settings file if missing
                TechnosoftConnection.ExtractMotorSettingsFile(System.Reflection.Assembly.GetExecutingAssembly(), ConfigPath, "BioNex.LiquidLevelDevice.TML", embedded_settings_name: MotorSettingsFileName, output_settings_name: MotorSettingsFileName);

                var settings = MotorSettings.LoadMotorSettings(MotorSettingsPath);
                if (!HasRAxis && settings.ContainsKey(LLSensorModelConsts.RAxis + 1)) // indices are zero based, but keys are 1 based
                    settings.Remove(LLSensorModelConsts.RAxis + 1);

                _connection.LoadConfiguration(settings, ConfigPath);
                _axes = (from x in _connection.GetAxes() select x.Value).ToList();

                // determine which IDs are missing from _axes
                ExtractAndVerifyTmlFiles(settings);

                _homed = new List<bool>();
                foreach (var axis in _axes)
                {
                    bool homed = false;

                    if (axis == null)
                    {
                        return false;
                    }
                    try
                    {
                        axis.GetPositionMM();
                        homed = axis.IsHomed;
                    }
                    catch (Exception e)
                    {
                        _log.Error(string.Format("LiquidLevelDevice: Failed to read position on '{0}'.  Is the device fully powered?", axis.Name), e);
                        return false;
                    }
                    finally
                    {
                        _homed.Add(homed);
                    }
                }

                // load defaults from motor settings file
                XAxisVelocity = XAxis.Settings.Velocity;
                XAxisAcceleration = XAxis.Settings.Acceleration;
                YAxisVelocity = YAxis.Settings.Velocity;
                YAxisAcceleration = YAxis.Settings.Acceleration;
                ZAxisVelocity = ZAxis.Settings.Velocity;
                ZAxisAcceleration = ZAxis.Settings.Acceleration;
                RAxisVelocity = RAxis == null ? 0.0 : RAxis.Settings.Velocity;
                RAxisAcceleration = RAxis == null ? 0.0 : RAxis.Settings.Acceleration;
            }
            catch (TechnosoftException e)
            {
                _log.Error("LiquidLevelDevice: Failed to open CAN connection to motors.", e);
                return false;
            }
            return true;
        }

        bool IsAxisVersionsCorrect(IAxis axis)
        {
            int latest_major = axis.Settings.FirmwareMajor;  // version in motor_settings.xml
            int latest_minor = axis.Settings.FirmwareMinor;  // version in motor_settings.xml

            // if the motor settings file lists firmware as zero, zero then skip the embedded firmware check
            if (latest_major == 0 && latest_minor == 0)
                return true;

            int current_major = axis.FirmwareMajorVersion;        // embedded firmware
            int current_minor = axis.FirmwareMinorVersion;        // embedded firmware
            // if the firmware version on the controller doesn't match the .t.zip file we just extracted, then re-extract the matching one
            return current_major == latest_major && current_minor == latest_minor;
        }

        /// <summary>
        /// 1) extract t.zip files for any null axes, try reloading configuration
        /// 2) query device firmware, compare with firmware value in motor settings file
        ///   -- on mismatch, extract t.zip, download to device, try reloading configuration
        /// </summary>
        /// <param name="axis_id"></param>
        private void ExtractAndVerifyTmlFiles(IDictionary<byte, MotorSettings> settings)
        {
            // 1.
            var axes_without_setup_files = settings.Keys.ToList();
            var axes_with_setup_files = from x in _axes where x != null select x.GetID();
            axes_without_setup_files.RemoveAll(x => axes_with_setup_files.Contains(x));

            if (axes_without_setup_files.Count != 0)
            {
                _log.Error("LiquidLevelDevice: Null axis found after establishing connection to motors, this is probably due to a failed TS_LoadSetup call and missing or corrupt config file(s).  New, matching config file(s) will be generated now.");

                // extract the t.zips for axes that were missing
                foreach (var axis_id in axes_without_setup_files)
                    TechnosoftConnection.ExtractTmlFiles(System.Reflection.Assembly.GetExecutingAssembly(), ConfigPath, axis_id, "BioNex.LiquidLevelDevice.TML", 0, 0, true, false);

                // reload the configuration - this associates the extracted T.Zip with the axes so that we know we're getting the correct firmware version... 
                _connection.LoadConfiguration(settings, ConfigPath);
                _axes = (from x in _connection.GetAxes() select x.Value).ToList();
            }

            // 1b. -- check to see if both firmware values are 0(ZERO) this indicates no connection
            var connected = new Dictionary<IAxis, bool>();
            foreach (var axis in _axes)
            {
                var bad = axis.FirmwareMajorVersion == 0 && axis.FirmwareMinorVersion == 0;
                connected[axis] = !bad;
                if (bad)
                    _log.ErrorFormat("LiquidLevelDevice: Could not read firmware version on axis '{0}'.  Device may require a power cycle or further troubleshooting.", axis.Name);
            }

            // 2. -- check for mismatch, extract .t.zip files if mismatched
            var version_differed = false;
            foreach (var axis in _axes)
            {
                if (!connected[axis])
                    continue;

                // if the firmware version on the controller doesn't match motor settings, then re-extract the matching .t.zip
                if (!IsAxisVersionsCorrect(axis))
                {
                    version_differed = true;
                    _log.WarnFormat("LiquidLevelDevice: The firmware programmed on axis '{0}' does not match the firmware in the motor settings file.  Extracting local .t.zip files to see if it's really a firmware mismatch.", axis.Name);
                    TechnosoftConnection.ExtractTmlFiles(System.Reflection.Assembly.GetExecutingAssembly(), ConfigPath, axis.GetID(), "BioNex.LiquidLevelDevice.TML", 0, 0, true, false);
                }
            }

            if (!version_differed)
                return;

            _connection.LoadConfiguration(settings, ConfigPath);
            _axes = (from x in _connection.GetAxes() select x.Value).ToList();

            //2. -- check for mismatch, update firmware if mismatched
            if (Properties.GetBool(LLProperties.AllowUpgrade))
            {

                version_differed = false;
                foreach (var axis in _axes)
                {
                    if (!connected[axis])
                        continue;

                    // if the firmware version on the controller still doesn't match the .t.zip file we just extracted, then re-extract the matching one
                    if (!IsAxisVersionsCorrect(axis))
                    {
                        version_differed = true;
                        var msg = string.Format("The firmware programmed on axis '{0}' does not match the firmware in the motor settings file.", axis.Name);
                        _log.Warn("LiquidLevelDevice: " + msg + " Attempting to download new firmware.");
                        bool do_upgrade = true;
                        if (Properties.GetBool(LLProperties.PromptIfUpgrading))
                        {
                            msg += " Should we download the firmware embedded in the assembly to the device?";
                            do_upgrade = System.Windows.MessageBox.Show(msg, "Allow firmware download?", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes;
                        }
                        if( do_upgrade)
                            UpgradeFirmware(axis);
                        else
                            _log.Warn("LiquidLevelDevice: User denied request to download firmware to device.");
                    }
                }

                if (!version_differed)
                    return;

                _connection.LoadConfiguration(settings, ConfigPath);
                _axes = (from x in _connection.GetAxes() select x.Value).ToList();
            }

            // sanity check -- in case we embedded a firmware version but it doesn't match what's in the motor settings file
            version_differed = false;
            foreach (var axis in _axes)
            {
                if (!connected[axis])
                    continue;

                if (!IsAxisVersionsCorrect(axis))
                {
                    version_differed = true;
                    _log.WarnFormat("LiquidLevelDevice: The firmware programmed on axis '{0}' still does not match the version in the motor settings file.", axis.Name);
                }
            }
            if (version_differed)
                _log.Warn("LiquidLevelDevice: Either the firmware update failed, the motor settings file has been manipulated, or the assembly needs to be rebuilt.  Please call technical support.");
        }

        public bool IsHomed
        {
            get
            {
                return MotorsConnected && (_homed[LLSensorModelConsts.XAxis] && _homed[LLSensorModelConsts.YAxis] && _homed[LLSensorModelConsts.ZAxis]);
            }
        }

        public void Home()
        {
            HomeIfNecessary(true);
            MoveToPark();
        }

        bool HomeIfNecessary(bool force)
        {
            // home Z first for safety
            if (!Home(LLSensorModelConsts.ZAxis, true, force))
                return false;

            // servo off X, then Home Y -- X will snap to center when servoed off
            try
            {
                _axes[LLSensorModelConsts.XAxis].Enable(false, true);
            }
            catch (AxisException e)
            {
                _log.Error(string.Format("LiquidLevelDevice: Failed to disable '{0}' when trying to home -- maybe a power cycle will resolve this.", _axes[LLSensorModelConsts.XAxis].Name), e);
                return false;
            }

            if (!Home(LLSensorModelConsts.YAxis, true, force))
                return false;

            if (!Home(LLSensorModelConsts.XAxis, true, force))
                return false;

            if (HasRAxis)
                if (!Home(LLSensorModelConsts.RAxis, true, force))
                    return false;
            return true;
        }

        bool Home(int axis_index, bool block, bool force)
        {
            var axis = _axes[axis_index];
            bool homed = _homed[axis_index];

            try
            {
                if (force || !homed)
                    axis.Home(block);
                _homed[axis_index] = true;
            }
            catch (AxisException e)
            {
                _log.Error(string.Format("LiquidLevelDevice: Home failed on '{0}'", axis.Name), e);
                _homed[axis_index] = false;
                return false;
            }
            return true;
        }

        public double GetAxisPositionMM(int index)
        {
            if (!MotorsConnected)
                return double.NaN;
            try
            {
                return _axes[index].GetPositionMM();
            }
            catch (AxisException e)
            {
                _log.Error(string.Format("LiquidLevelDevice: Failed to get position for '{0}'", _axes[index].Name), e);
                return double.NaN;
            }
        }

        /// <summary>
        /// Teachpoint is at Stage Fiducial Center.  Therefore, all moves are relative to stage fiducial.
        /// LLSCaptureStateMachine additionally uses Plate location measurements to make Capture moves relative to the A1 well.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="wait_for_move_complete"></param>
        /// <param name="teach_point_type"></param>
        public void MoveRelativeToTeachpoint(double x, double y, double z, bool wait_for_move_complete = true)
        {
            if (!MotorsConnected)
                return;

            double tp_x = Properties.GetDouble(LLProperties.X_TP);
            double tp_y = Properties.GetDouble(LLProperties.Y_TP);
            double tp_z = Properties.GetDouble(LLProperties.Z_TP);
            double tp_r = Properties.GetDouble(LLProperties.R_TP);

            // apply an X correction based on our X / Y Linearization calibration 
            // -- we generated a line representing how X needs to change with Y in order to drive in a straight line
            // -- this correction needs to be added to desired X to get us to a linear position with respect to the stage
            // -- in other words, if we only request a Y move, we need an X move as well to get a straight line
            double xy_slope = XYSlope;
            const double xy_intercept = 0.0; // XYIntercept;
            double x_correction = y * xy_slope + xy_intercept;

            // apply a Y correction based on our Y / X Polynomial calibration
            // -- X moves in a parabolic arc, we need to correct in Y in order to compensate
            double x_c = x + x_correction;
            double y_correction = (x_c * x_c * XArcCorrection[2] + x_c * XArcCorrection[1]);// + XArcCorrection[0]);

            // apply a Z correction based on our Y / Z Linearization calibration (uses CORRECTED Y)
            // -- we generated a line representing how Z needs to change with Y in order to read the same value at different Y positions (stage flatness)
            double zy_slope = ZYSlope;
            const double zy_intercept = 0.0;
            double z_correction = (y + y_correction) * zy_slope + zy_intercept;

            try
            {
                // now move all axes -- call non blocking to start the moves at the same time
                XAxis.MoveAbsolute(tp_x + x + x_correction, XAxisVelocity, XAxisAcceleration, wait_for_move_complete: false);
                YAxis.MoveAbsolute(tp_y + y + y_correction, YAxisVelocity, YAxisAcceleration, wait_for_move_complete: false);
                ZAxis.MoveAbsolute(tp_z + z + z_correction, ZAxisVelocity, ZAxisAcceleration, wait_for_move_complete: false);
                if (HasRAxis)
                    RAxis.MoveAbsolute(tp_r, RAxisVelocity, RAxisAcceleration, wait_for_move_complete: false);

                if (wait_for_move_complete)
                {
                    // call again but blocking to wait for motion to complete
                    XAxis.MoveAbsolute(tp_x + x + x_correction, XAxisVelocity, XAxisAcceleration);
                    YAxis.MoveAbsolute(tp_y + y + y_correction, YAxisVelocity, YAxisAcceleration);
                    ZAxis.MoveAbsolute(tp_z + z + z_correction, ZAxisVelocity, ZAxisAcceleration);
                    if (HasRAxis)
                        RAxis.MoveAbsolute(tp_r, RAxisVelocity, RAxisAcceleration);
                }
            }
            catch (AxisException e)
            {
                string msg = HasRAxis 
                    ? String.Format("LiquidLevelDevice: Failed to reach position X:{0} Y:{1} Z:{2} R{3}.", tp_x + x - x_correction, tp_y + y, tp_z + z, tp_r)
                    : String.Format("LiquidLevelDevice: Failed to reach position X:{0} Y:{1} Z:{2}.", tp_x + x - x_correction, tp_y + y, tp_z + z);
                _log.Error(msg, e);
            }
        }

        public void MoveToPark(TeachpointType tpt = TeachpointType.Portrait)
        {
            if (!MotorsConnected)
                return;

            double park_x = 0.0;
            double park_y = 0.0;
            double park_z = 0.0;
            double park_r = 0.0;
            switch (tpt)
            {
                default: park_r = Properties.GetDouble(LLProperties.R_PORTRAIT_TP); break;
                case TeachpointType.Landscape: park_r = Properties.GetDouble(LLProperties.R_LANDSCAPE_TP); break;
            }

            try
            {
                // z needs to be out of the way to make sure we clear the lip, so give it a second
                ZAxis.MoveAbsolute(park_z, ZAxisVelocity, ZAxisAcceleration, wait_for_move_complete: false);
                Thread.Sleep(500);
                // now move all axes -- call non-blocking to start the moves at the same time
                XAxis.MoveAbsolute(park_x, XAxisVelocity, XAxisAcceleration, wait_for_move_complete: false);
                YAxis.MoveAbsolute(park_y, YAxisVelocity, YAxisAcceleration, wait_for_move_complete: false);
                if (HasRAxis)
                    RAxis.MoveAbsolute(park_r, RAxisVelocity, RAxisAcceleration, wait_for_move_complete: false);

                // call again but blocking to wait for motion to complete
                ZAxis.MoveAbsolute(park_z, ZAxisVelocity, ZAxisAcceleration);
                XAxis.MoveAbsolute(park_x, XAxisVelocity, XAxisAcceleration);
                YAxis.MoveAbsolute(park_y, YAxisVelocity, YAxisAcceleration);
                if (HasRAxis)
                    RAxis.MoveAbsolute(park_r, RAxisVelocity, RAxisAcceleration);
            }
            catch (AxisException e)
            {
                string msg = HasRAxis
                    ? String.Format("LiquidLevelDevice: Failed to reach park position X:{0} Y:{1} Z:{2} R{3}.", park_x, park_y, park_z, park_r)
                    : String.Format("LiquidLevelDevice: Failed to reach park position X:{0} Y:{1} Z:{2}.", park_x, park_y, park_z);
                _log.Error(msg, e);
                _log.Error(String.Format("LiquidLevelDevice: Failed to reach park position X:{0} Y:{1} Z:{2}.", park_x, park_y, park_z), e);
            }
        }

        public event SavePropertiesEventHandler SavePropertiesEvent;
        public void FireSavePropertiesEvent()
        {
            if (SavePropertiesEvent != null)
                SavePropertiesEvent(_owner, _properties);
        }

        public void TeachHere(TeachpointType tpt)
        {
            switch (tpt)
            {
                default:                        
                    _properties[LLProperties.X_TP] = XAxis.GetPositionMM().ToString();
                    _properties[LLProperties.Y_TP] = YAxis.GetPositionMM().ToString();
                    _properties[LLProperties.Z_TP] = ZAxis.GetPositionMM().ToString();
                    if( HasRAxis)
                        _properties[LLProperties.R_TP] = RAxis.GetPositionMM().ToString(); 
                    break;
                case TeachpointType.Portrait:   
                    if( HasRAxis)
                        _properties[LLProperties.R_PORTRAIT_TP] = RAxis.GetPositionMM().ToString();                     
                    break;
                case TeachpointType.Landscape:  
                    if( HasRAxis)
                        _properties[LLProperties.R_LANDSCAPE_TP] = RAxis.GetPositionMM().ToString();                                         
                    break;
            }
                
            FireSavePropertiesEvent();
        }

        public void OffsetTeachpoint(double x_offset, double y_offset)
        {
            var x_tp = _properties.GetDouble(LLProperties.X_TP) + x_offset;
            var y_tp = _properties.GetDouble(LLProperties.Y_TP) + y_offset;
            _properties[LLProperties.X_TP] = x_tp.ToString();
            _properties[LLProperties.Y_TP] = y_tp.ToString();
            FireSavePropertiesEvent();
        }

        public void SaveSensorDeviations(double[] x, double[] y)
        {
            for (int i = 0; i < x.Length; ++i)
                _properties[LLProperties.index(LLProperties.XDeviation, i)] = x[i].ToString();
            for (int i = 0; i < y.Length; ++i)
                _properties[LLProperties.index(LLProperties.YDeviation, i)] = y[i].ToString();
            FireSavePropertiesEvent();
        }

        public Averages CalculateWellAverages(List<Measurement> values)
        {
            const double ACCEPTABLE_STD_DEV = 1.0;
            var count = values.Count;
            if (values.Count == 0)
                return new Averages();

            int channel = values[0].channel;
            int column = values[0].column;
            int row = values[0].row;
            double x = 0.0;
            double y = 0.0;

            double pop_avg = 0.0;
            for (int i = 0; i < count; ++i)
            {
                pop_avg += values[i].measured_value;
            }
            pop_avg /= count;

            double std_dev = 0.0;
            for (int i = 0; i < count; ++i)
                std_dev += Math.Pow(values[i].measured_value - pop_avg, 2);
            std_dev = Math.Sqrt(std_dev / count);

            double avg = 0.0;
            int final_count = 0;
            for (int i = 0; i < count; ++i)
            {
                double value = values[i].measured_value;
                if (Math.Abs(value - pop_avg) > ACCEPTABLE_STD_DEV * std_dev)
                    continue;
                avg += value;
                x += values[i].x;
                y += values[i].y;
                ++final_count;
            }
            if (final_count == 0)
            {
                avg = 0.0;
                x = values[0].x;
                y = values[0].y;
            }
            else
            {
                avg /= final_count;
                x /= final_count;
                y /= final_count;
            }

            _log.Debug(String.Format("Channel: {0} : Column: {1} : Row: {2} : Pop Avg: {3:0.00} : Std Dev: {4:0.00} : Avg: {5:0.00}", channel + 1, column + 1, row + 1, pop_avg, std_dev, avg));

            return new Averages(channel, column, row, x, y, pop_avg, std_dev, avg);
        }

        public List<double> GetVolumesFromAverages(string labware_name, List<Averages> values)
        {
            var result = new List<double>();
            var maps = VolumeMapDatabase.GetEnabledMapIDsForLabware(labware_name);
            if (maps.Count == 0)
                return result;

            foreach (var well in values)
            {
                var avg = well.Average;
                double avg_volume = 0.0;
                int count = 0;
                
                foreach (var map in maps)
                {
                    var C = VolumeMapDatabase.GetCoefficientsForMap(map);
                    double map_volume = 0.0;
                    for (int i = 0; i < C.Count; ++i)
                        map_volume += C[i] * (i == 0 ? 1.0 : Math.Pow(avg, i));

                    var max = VolumeMapDatabase.GetMaxVolumeForMap(map);
                    if (map_volume > max)
                        continue;

                    var min = VolumeMapDatabase.GetMinVolumeForMap(map);
                    if (map_volume < min)
                        continue;

                    avg_volume += map_volume;
                    ++count;
                }

                if (count > 0)
                {
                    avg_volume /= count;
                    result.Add(avg_volume);
                }
                else // add dummy values so that volumes[] doesn't get out of sync from averages[] even though we couldn't compute a volume for this well
                {
                    result.Add(double.NaN);
                }
            }
            return result;
        }

        const int DEFAULT_WELLS = 96;
        const int DEFAULT_COLUMNS = 12;
        const int DEFAULT_ROWS = 8;
        const double DEFAULT_SPACING = 9.0;

        public void GetLabwareData(string labware_name, out int columns, out int rows, out double col_spacing, out double row_spacing, out double thickness, out double radius)
        {
            var labware = LabwareDatabase.GetLabware(labware_name);
            var num_wells = DEFAULT_WELLS;
            columns = DEFAULT_COLUMNS;
            rows = DEFAULT_ROWS;
            row_spacing = col_spacing = DEFAULT_SPACING;

            // if we don't know the thickness, default to getting as far away from the labware as possible
            thickness = Properties.GetDouble(LLProperties.Z_TP) - Properties.GetDouble(LLProperties.CaptureOffset);

            try
            {
                num_wells = (int)labware.Properties[LabwarePropertyNames.NumberOfWells];
            }
            catch (Exception)
            {
                _log.ErrorFormat("Labware '{0}' has no value for '{2}' : using default value of '{1}'", labware_name, num_wells, LabwarePropertyNames.NumberOfWells);
            }

            try
            {
                columns = (int)labware.Properties[LabwarePropertyNames.NumberOfColumns];
            }
            catch (Exception)
            {
                try
                {
                    var format = LabwareFormat.GetLabwareFormat(num_wells);
                    columns = format.NumCols;
                    _log.WarnFormat("Labware '{0}' has no value for '{2}' : using default value of '{1}' based on well count", labware_name, columns, LabwarePropertyNames.NumberOfColumns);
                }
                catch (LabwareFormat.InvalidWellCountException)
                {
                    _log.ErrorFormat("Labware '{0}' has no value for '{2}' : using default value of '{1}'", labware_name, columns, LabwarePropertyNames.NumberOfColumns);
                }
            }

            try
            {
                rows = (int)labware.Properties[LabwarePropertyNames.NumberOfRows];
            }
            catch (Exception)
            {
                try
                {
                    var format = LabwareFormat.GetLabwareFormat(num_wells);
                    rows = format.NumRows;
                    _log.WarnFormat("Labware '{0}' has no value for '{2}' : using default value of '{1}' based on well count", labware_name, rows, LabwarePropertyNames.NumberOfRows);
                }
                catch (LabwareFormat.InvalidWellCountException)
                {
                    _log.ErrorFormat("Labware '{0}' has no value for '{2}' : using default value of '{1}'", labware_name, rows, LabwarePropertyNames.NumberOfRows);
                }
            }

            try
            {
                col_spacing = (double)labware.Properties[LabwarePropertyNames.ColumnSpacing];
            }
            catch (Exception)
            {
                try
                {
                    var format = LabwareFormat.GetLabwareFormat(num_wells);
                    col_spacing = format.ColToColSpacing;
                    _log.WarnFormat("Labware '{0}' has no value for '{2}' : using default value of '{1}' based on well count", labware_name, col_spacing, LabwarePropertyNames.ColumnSpacing);
                }
                catch (LabwareFormat.InvalidWellCountException)
                {
                    _log.ErrorFormat("Labware '{0}' has no value for '{2}' : using default value of '{1}'", labware_name, col_spacing, LabwarePropertyNames.ColumnSpacing);
                }
            }

            try
            {
                row_spacing = (double)labware.Properties[LabwarePropertyNames.RowSpacing];
            }
            catch (Exception)
            {
                try
                {
                    var format = LabwareFormat.GetLabwareFormat(num_wells);
                    row_spacing = format.RowToRowSpacing;
                    _log.WarnFormat("Labware '{0}' has no value for '{2}' : using default value of '{1}' based on well count", labware_name, row_spacing, LabwarePropertyNames.RowSpacing);
                }
                catch (LabwareFormat.InvalidWellCountException)
                {
                    _log.ErrorFormat("Labware '{0}' has no value for '{2}' : using default value of '{1}'", labware_name, row_spacing, LabwarePropertyNames.RowSpacing);
                }
            }

            try
            {
                thickness = (double)labware.Properties[LabwarePropertyNames.Thickness];
            }
            catch (Exception)
            {
                _log.ErrorFormat("Labware '{0}' has no value for '{2}' : using default value of '{1}'", labware_name, thickness, LabwarePropertyNames.Thickness);
            }

            try
            {
                radius = (double)labware.Properties[LabwarePropertyNames.WellRadius];
            }
            catch (Exception)
            {
                radius = col_spacing / 2.0;
                _log.WarnFormat("Labware '{0}' has no value for '{2}' : using default value of '{1}' based on column spacing", labware_name, radius, LabwarePropertyNames.WellRadius);
            }
        }

        bool ReadConfiguration(int i, int port, out bool requires_calibration, out string response)
        {
            response = "";
            int tries = 0;
            const int MAX_TRIES = 3;
            do
            {
                requires_calibration = true;

                if (++tries > MAX_TRIES)
                {
                    _log.Error(string.Format("LiquidLevelDevice: Failed to receive a valid config on port {0} after {1} tries", port, tries - 1));
                    return false;
                }

                if (tries > 1)
                    Thread.Sleep(10);   // don't sleep first time through

                // First, READ the config, and only modify it if it is different from what's expected
                try
                {
                    response = _sensors[i].GetResponse(LevelSensor.Commands[(int)LevelSensor.Command.GetConfiguration], 5);
                }
                catch (Exception e)
                {
                    _log.Error(string.Format("LiquidLevelDevice: Error receiving response from port {0}.", port), e);
                    continue;
                }

                var regex = new System.Text.RegularExpressions.Regex("^{0V([AB])([AB])([ABCD])([ABCDEFG])([01])");
                var match = regex.Match(response);
                var groups = match.Groups;

                if (groups.Count != 6
                || groups[0].Captures.Count != 1
                || groups[1].Captures.Count != 1
                || groups[2].Captures.Count != 1
                || groups[3].Captures.Count != 1
                || groups[4].Captures.Count != 1
                || groups[5].Captures.Count != 1
                )
                {
                    _log.Debug(string.Format("LiquidLevelDevice: response from port {0}: '{1}' does not match config regex.", port, response));
                    continue;
                }

                var same_mode = Properties.GetString(LLProperties.index(LLProperties.Mode, i)) == groups[1].Captures[0].Value;
                var same_format = Properties.GetString(LLProperties.index(LLProperties.Format, i)) == groups[2].Captures[0].Value;
                var same_sensitivity = Properties.GetString(LLProperties.index(LLProperties.Sensitivity, i)) == groups[3].Captures[0].Value;
                var same_averaging = Properties.GetString(LLProperties.index(LLProperties.Averaging, i)) == groups[4].Captures[0].Value;
                var same_temp_comp = Properties.GetString(LLProperties.index(LLProperties.TemperatureCompensation, i)) == groups[5].Captures[0].Value;

                requires_calibration = !(same_mode && same_format && same_sensitivity && same_averaging && same_temp_comp);
                return true;
            } while (true);
        }

        public void SetSensitivity(string mode)
        {
            if (Properties.GetBool(LLProperties.Simulate)) return;

            var action = new Action<int>(i =>
                {
                    string config = string.Format("{0}{1}}}"
                            , LevelSensor.Commands[(int)LevelSensor.Command.SetSensitivity]
                            , mode
                            );
                    var response = _sensors[i].GetResponse(config);
                    bool requires_calibration;
                    var port = Properties.GetInt(LLProperties.index(LLProperties.Port, i));
                    ReadConfiguration(i, port, out requires_calibration, out response);
                    LastConfig[i] = response;
                    _sensors[i].CurrentMode = mode; // tell sensor which calibration to use

                    if (ConnectionStateChangedEvent != null)
                        ConnectionStateChangedEvent(_owner, i, true, response);
                });

            for (int i = 0; i < _sensors.Length; i += ParallelGroupSize)
                System.Threading.Tasks.Parallel.For(i, i + ParallelGroupSize, action);
        }

        bool LabwareSafetyCheck(string labware_name)
        {
            // run labware safety check first
            var safety = new LLSLabwareSafetyCheckStateMachine(this, labware_name);
            safety.Start();
            if (!safety.Success)
                _log.Error(safety.ErrorMessage);
            return safety.Success;
        }

        void UpgradeFirmware(IAxis axis)
        {
            var id = axis.GetID();
            var resource_string = string.Format("{0}.{1}.sw", "BioNex.LiquidLevelDevice.TML", id);
            var filename = String.Format("{0}{1}.sw", System.IO.Path.GetTempPath(), id);

            ExtractFirmware(resource_string, filename);
            try{
            axis.DownloadSwFile(filename);
            }catch(AxisException e){
                _log.Error(e);
            }

            axis.ResetDrive();
        }

        void ExtractFirmware(string resource_string, string destination_filename)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (var input = assembly.GetManifestResourceStream(resource_string))
            {
                using (var output = System.IO.File.Open(destination_filename, System.IO.FileMode.Create))
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
            }
        }
    }
}
