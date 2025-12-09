using System;
using System.IO;
using BioNex.Shared.Utils;
using log4net;
using System.Collections.Generic;

namespace BioNex.LiquidLevelDevice
{
    // class used to serialize calibratino
    public class CalibrationConfig
    {
        public double Slope { get; set; }
        public double Intercept { get; set; }
        public double Correlation { get; set; }
    }

    public interface ILevelSensor
    {
        string Name { get; }
        bool Connected { get; }

        void Open(bool reset_bus = false);
        void Close();

        int Retries { get; }
        int GetReading(int max_tries = 3, bool periodic_read = false);

        string GetResponse(string command, int max_tries = 30);

        string CurrentMode { get; set; } // used to pick which calibration will be used
        CalibrationConfig Calibration { get; }
        double GetCalibratedReading(double reading);
        void Calibrate(double[] actual_z, double[] measured_z, string mode);
        void LoadCalibration(string path);
        void SaveCalibration(string path);
    }

    public class LevelSensorException : Exception
    {
        public LevelSensorException(string msg) : base(msg) { }
    }

    public class LevelSensor : ILevelSensor
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(LevelSensor));
        public enum Command { Measure, CalibrateNear, CalibrateFar, GetConfiguration, SetConfiguration, PeriodicOn, PeriodicOff, SetSensitivity };
        public static string[] Commands = { "{0M}", "{0X}", "{0Y}", "{0V}", "{0U", "{0P}", "{0R}", "{0B" };
        bool _simulate;
        static Random _random;
        int _can_device_id;

        public string Name { get { return _port.Name; } }
        public LevelSensor(int can_device_id, int id, bool simulate = false, bool reset_bus = false)
        {
            _can_device_id = can_device_id;
            _simulate = simulate;
            CurrentMode = LLSensorModelConsts.DefaultSensitivity;
            if (simulate)
            {
                if (_random == null) _random = new Random();
                _port = new SimulatedLevelSensorPort(id);
            }
            else
            {
                _port = LevelSensorPortSelector.SelectPort(can_device_id, id);
            }
            Open(reset_bus);
        }

        public bool Connected { get; private set; }
        ILevelSensorPort _port;
        public void Open(bool reset_bus = false)
        {
            if (_simulate)
                return;
            if (_port.IsOpen)
                _port.Close();
            _port.Open(reset_bus);
            if (!_port.IsOpen)
                throw new LevelSensorException(_can_device_id == -1 ? "Couldn't open serial port" : "Could not initialize CAN hardware");

            // the sensors appear to require >250ms to gather data at their highest averaging setting 'F', and when running calibration
            // even then, if the sensor is reading over a long distance it will sometimes fail to respond
            // Emprically i've seen up to 19 retries to get it to respond
            _port.ReadTimeout = 500;
            Connected = true;
        }

        public void Close()
        {
            Connected = false;
            if (!_simulate)
                _port.Close();
        }

        private static bool VerifyChecksum(string message)
        {
            var message_sum = message.Substring(message.Length - 3, 2);

            // add up the telegram information
            int sum = 0;
            for (int i = 1; i < message.Length - 3; ++i)
            {
                sum += (int)message[i];
            }
            var sum_string = sum.ToString();
            if (sum_string.Length > 2)
                sum_string = sum_string.Substring(sum_string.Length - 2);

            return sum_string == message_sum;
        }

        public string GetResponse(string command, int max_tries = 30)
        {
            string parsed_buffer = "";

            int MAX_TRIES = max_tries;
            int retry = 0;
            bool read_success = false;
            do
            {
                try
                {
                    _port.DiscardInBuffer();
                    if (!string.IsNullOrEmpty(command))
                        _port.Write(command);
                    var buffer = "{" + _port.ReadFromTo("{", "}") + "}";

                    var start_of_message = buffer.IndexOf('{');
                    if (start_of_message < 0)
                    {
                        _log.Debug(string.Format("{0} missing SOH, message was '{1}', try #{2}", _port.Name, buffer, retry + 1));
                        continue;
                    }

                    parsed_buffer = buffer.Substring(start_of_message);
                    if (!VerifyChecksum(parsed_buffer))
                    {
                        _log.Debug(string.Format("{0} invalid checksum, message was: {1}, try #{2}", _port.Name, parsed_buffer, retry + 1));
                        continue;
                    }

                    read_success = true;
                }
                catch (TimeoutException)
                {
                    _log.Debug(string.Format("{0} timed out, try #{1}", _port.Name, retry + 1));
                }

            } while ((++retry != MAX_TRIES) && !read_success);

            if (!read_success)
            {
                _log.Debug(string.Format("{0} giving up after {1} tries", _port.Name, retry));
                throw new LevelSensorException(string.Format("Failed read a valid response to command '{0}' after {1} attempts", command, MAX_TRIES));
            }

            if (retry > 1)
                _log.Debug(string.Format("{0} succeeded after {1} tries", _port.Name, retry));

            return parsed_buffer;
        }

        public int Retries { get; private set; }
        public int GetReading(int MAX_TRIES = 3, bool periodic_read = false)
        {
            if (_simulate)
                return _random.Next(4096);

            Retries = 0;
            string parsed_buffer = "";
            bool object_within_sensing_range = false;
            bool echo_width_big = false;
            bool valid_parse = false;
            int measurement = 0;
            do
            {
                parsed_buffer = GetResponse(periodic_read ? "" : Commands[(int)Command.Measure]);

                valid_parse = false;
                object_within_sensing_range = false;
                echo_width_big = false;

                if (parsed_buffer.Length == 12)
                {
                    object_within_sensing_range = parsed_buffer[3] != '0';
                    echo_width_big = parsed_buffer[4] != '0';
                    // remove everything except the measurement value
                    valid_parse = int.TryParse(parsed_buffer.Substring(5, parsed_buffer.Length - 3 - 5), out measurement);
                }

                if (echo_width_big && object_within_sensing_range && valid_parse)
                    break;

                if (++Retries > MAX_TRIES)
                {
                    // accept a small echo if we're giving up anyway
                    if (object_within_sensing_range && valid_parse)
                    {
                        _log.WarnFormat("Accepting a small echo response from sensor port '{0}' after trying '{1} times for a better read", _port.Name, Retries - 1);
                        break;
                    }

                    throw new LevelSensorException(string.Format("Failed to get a valid reading after {0} tries. Echo big: '{1}' Object within range: '{2}'", Retries - 1, echo_width_big, object_within_sensing_range));
                }

            } while (true);

            return measurement;
        }



        private Dictionary<string, double> _intercept = new Dictionary<string, double>() { { "A", 0.0 }, { "B", 0.0 }, { "C", 0.0 }, { "D", 0.0 } };
        private Dictionary<string, double> _slope = new Dictionary<string, double>() { { "A", 1.0 }, { "B", 1.0 }, { "C", 1.0 }, { "D", 1.0 } };
        private Dictionary<string, double> _correlation = new Dictionary<string, double>() { { "A", 1.0 }, { "B", 1.0 }, { "C", 1.0 }, { "D", 1.0 } };
        public CalibrationConfig Calibration { get { return new CalibrationConfig() { Intercept = _intercept[CurrentMode], Slope = _slope[CurrentMode], Correlation = _correlation[CurrentMode] }; } }
        public string CurrentMode { get; set; }

        public void Calibrate(double[] actual_z, double[] measured_z, string mode)
        {
            // assume measurements are 10 * mm --> convert to mm
            for (int i = 0; i < measured_z.Length; ++i) measured_z[i] /= 10.0;

            var regression = new SimpleLinearRegression(actual_z, measured_z);
            _slope[mode] = regression.Slope;
            _intercept[mode] = regression.Intercept;
            _correlation[mode] = regression.Correlation;
        }

        public double GetCalibratedReading(double reading)
        {
            // assume measurements are 10 * mm --> convert to mm
            return ((reading / 10.0) - _intercept[CurrentMode]) / _slope[CurrentMode];
        }

        public void LoadCalibration(string path)
        {
            if (_simulate)
                return;

            string[] modes = {"A","B","C","D"};
            for (int i = 0; i < 4; ++i)
            {
                var mode = modes[i];
                var mode_path = path + "\\beesure_calibration_" + Name + "_" + mode + ".xml";

                if (!File.Exists(mode_path))
                {
                    _intercept[mode] = 0.0;
                    _slope[mode] = 1.0;
                    _correlation[mode] = 1.0;
                    continue;
                }

                var data = FileSystem.LoadXmlConfiguration<CalibrationConfig>(mode_path);
                _intercept[mode] = data.Intercept;
                _slope[mode] = data.Slope;
                _correlation[mode] = data.Correlation;
            }
        }

        public void SaveCalibration(string path)
        {
            if (_simulate)
                return;

            string[] modes = {"A","B","C","D"};
            for (int i = 0; i < 4; ++i)
            {
                var mode = modes[i];
                var mode_path = path + "\\beesure_calibration_" + Name + "_" + mode + ".xml";

                var current = CurrentMode;
                CurrentMode = mode;
                try
                {
                    FileSystem.SaveXmlConfiguration<CalibrationConfig>(Calibration, mode_path);
                }
                finally
                {
                    CurrentMode = current;
                }
            }
        }
    }
}
