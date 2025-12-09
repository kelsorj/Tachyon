using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace BioNex.Shared.TechnosoftLibrary
{
    public class InvalidMotorSettingsException : ApplicationException
    {
        public byte AxisID { get; set; }
        public string SettingName { get; set; }
        public string CurrentValue { get; set; }
        public string MinValue { get; set; }
        public string MaxValue { get; set; }

        public InvalidMotorSettingsException( byte axis_id, string setting_name, string current_value, string min_value, string max_value)
        {
            AxisID = axis_id;
            SettingName = setting_name;
            CurrentValue = current_value;
            MinValue = min_value;
            MaxValue = max_value;
        }
    }

    public class MotorSettings
    {
        public string AxisName { get; protected set; }
        public double Velocity { get; protected set; }
        public double Acceleration { get; protected set; }
        public int Jerk { get; protected set; }
        public int EncoderLines{ get; protected set; }
        public double GearRatio { get; protected set; }
        public double MinLimit { get; protected set; }
        public double MaxLimit { get; protected set; }
        public double MoveDoneWindow { get; protected set; }
        public short SettlingTimeMS { get; protected set; }
        public double SlowLoopServoTimeS { get; protected set; }
        public string ForceEquation { get; protected set; }
        public bool FlipAxisDirection { get; protected set; }
        public int DefaultServoTimeoutS { get; protected set; }
        public int MinJerk { get; protected set; }
        public double JerkFactor { get; protected set; }
        public bool Simulate { get; protected set; }

        public int FirmwareMajor { get; protected set; }
        public int FirmwareMinor { get; protected set; }

        /* copy constructor if necessary.
        public MotorSettings( MotorSettings settings)
        {
            AxisName = settings.AxisName;
            Velocity = settings.Velocity;
            Acceleration = settings.Acceleration;
            Jerk = settings.Jerk;
            EncoderLines = settings.EncoderLines;
            GearRatio = settings.GearRatio;
            MinLimit = settings.MinLimit;
            MaxLimit = settings.MaxLimit;
            MoveDoneWindow = settings.MoveDoneWindow;
            SettlingTimeMS = settings.SettlingTimeMS;
            SlowLoopServoTimeS = settings.SlowLoopServoTimeS;
            ForceEquation = settings.ForceEquation;
            FlipAxisDirection = settings.FlipAxisDirection;
            DefaultServoTimeoutS = settings.DefaultServoTimeoutS;
            MinJerk = settings.MinJerk;
            JerkFactor = settings.JerkFactor;
            Simulate = settings.Simulate;
        }
        */

        public MotorSettings()
        {
            //_already_converted = false;
            FirmwareMajor = int.MinValue;
            FirmwareMinor = int.MinValue;
        }

        public MotorSettings( string name, double v, double a, int j, int encoder_lines, double ratio,
                              double min, double max, double dw /*done window*/, short st /*settling time*/,
                              double slow_loop_servo_time_s, bool simulate=false, int firmware_major=int.MinValue, int firmware_minor=int.MinValue)
        {
            AxisName = name;
            Velocity = v;
            Acceleration = a;
            Jerk = j;
            EncoderLines = encoder_lines;
            GearRatio = ratio;
            MinLimit = min;
            MaxLimit = max;
            MoveDoneWindow = dw;
            SettlingTimeMS = st;
            SlowLoopServoTimeS = slow_loop_servo_time_s;
            // need to change this to a valid default equation???
            ForceEquation = "1.5";
            FlipAxisDirection = false;
            Simulate = simulate;
            FirmwareMajor = firmware_major;
            FirmwareMinor = firmware_minor;
        }

        public MotorSettings( string name, double v, double a, int j, int encoder_lines, double ratio,
                              double min, double max, double dw /*done window*/, short st /*settling time*/,
                              double slow_loop_servo_time_s, bool flip_axis_direction, bool simulate=false, 
                              int firmware_major=int.MinValue, int firmware_minor=int.MinValue)
            : this( name, v, a, j, encoder_lines, ratio, min, max, dw, st, slow_loop_servo_time_s, simulate, firmware_major, firmware_minor)
        {
            FlipAxisDirection = flip_axis_direction;  // wow did Sib write this crap or what?
        }

        public double GetJerkRate()
        {
            return ( Acceleration / Jerk) / SlowLoopServoTimeS;
        }

        public int CalculateTrapezoidalMoveTime( double distance_mm)
        {
            // given the parameters, is this a triagular or trap move?
            // distance, in mm = v^2 / (2 * a) + v^2 / (2 * d)
            double v = Velocity;
            double a = Acceleration;
            double d = Acceleration;
            double accel_distance = v*v/a;
            double decel_distance = v*v/d;
            double ramping_distance = accel_distance + decel_distance;
            double time;
            // we need the total time to accelerate in both cases
            double accel_time = 2 * accel_distance / v;
            double decel_time = 2 * decel_distance / v;
            if( ramping_distance >= distance_mm) {
                // triangular move => v * t / 2 = x => t = 2 * x / v
                // what if distance required is less than total_distance?  we should
                // probably factor this in, since we won't be accelerating and decelerating
                // for the same amount of time.
                double accel_percentage = accel_time / (accel_time + decel_time); // so <1 means accelerates slower than decelerates
                double new_accel_distance = accel_percentage * distance_mm;
                double new_decel_distance = (1 - accel_percentage) * distance_mm;
                double new_accel_time = 2 * new_accel_distance / v;
                double new_decel_time = 2 * new_decel_distance / v;
                time = new_accel_time + new_decel_time;
            } else {
                // trapezoidal move
                double time_at_velocity = (distance_mm - ramping_distance) / v;
                time = accel_time + time_at_velocity + decel_time;
            }
            // time up to this point is in seconds, and we want to return ms
            return (int)(time * 1000);
        }

        public double GetCurrentFromForce( double force_pounds)
        {
            // for now, we are just going to use the current value as the "equation"
            // eventually, we will calibrate each Z axis and fit a curve to the data
            return double.Parse( ForceEquation);
        }

        /// <summary>
        /// Loads motors settings from XML file, and does NOT convert from engineering units to IU!
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Dictionary<byte,MotorSettings> LoadMotorSettings( string path)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load( path);
            Dictionary<byte,MotorSettings> settings = new Dictionary<byte,MotorSettings>();
            Debug.Assert( doc.DocumentElement.Name == "MotorSettings");
            // parse axes
            foreach( XmlNode node in doc.DocumentElement.ChildNodes) {
                if( node.Name == "axes") {
                    foreach( XmlNode n in node)
                        if( n.Name == "axis")
                            ParseAxis( n, settings);
                }
            }

            return settings;
        }

        private static class AxisElementNames
        {
            public const string Id = "id";
            public const string Velocity = "velocity";
            public const string Acceleration = "acceleration";
            public const string Jerk = "jerk";
            public const string EncoderLines = "encoder_lines";
            public const string GearRatio = "gear_ratio";
            public const string MinLimit = "min_limit";
            public const string MaxLimit = "max_limit";
            public const string MoveDoneWindow = "move_done_window";
            public const string SettlingTime = "settling_time";
            public const string SlowLoopServoTime = "slow_loop_servo_time_sec";
            public const string ForceEquation = "force_equation";
            public const string FlipAxisDirection = "flip_axis_direction";
            public const string DefaultServoTimeout = "default_servo_timeout";
            public const string MinJerk = "min_jerk";
            public const string JerkFactor = "jerk_factor";
            public const string Simulate = "simulate";
            public const string FirmwareMajor = "firmware_major";
            public const string FirmwareMinor = "firmware_minor";
        }

        /// <summary>
        /// Parses XML, which should now be in ENGINEERING UNITS!!! (except for TJERK)
        /// </summary>
        /// <remarks>
        /// position in [mm], velocity in [mm/s], acceleration in [mm/s^2], move_done_window in [mm], settling_time_ms in [ms]
        /// default_servo_timeout in [s], min_jerk in [IU], jerk_factor (unitless)
        /// </remarks>
        /// <param name="node"></param>
        /// <param name="settings"></param>
        private static void ParseAxis( XmlNode node, IDictionary<byte,MotorSettings> settings)
        {
            // loop over the child nodes for <axis> and write those values to a MotorSettings object
            MotorSettings ms = new MotorSettings();
            byte id = 0;
            ms.AxisName = node.Attributes["name"].InnerText;
            foreach( XmlNode n in node) {
                switch( n.Name) {
                    case AxisElementNames.Id:
                        id = byte.Parse( n.InnerText);
                        break;
                    case AxisElementNames.Velocity:
                        ms.Velocity = double.Parse( n.InnerText);
                        break;
                    case AxisElementNames.Acceleration:
                        ms.Acceleration = double.Parse( n.InnerText);
                        break;
                    case AxisElementNames.Jerk:
                        ms.Jerk = int.Parse( n.InnerText);
                        break;
                    case AxisElementNames.EncoderLines:
                        ms.EncoderLines = int.Parse( n.InnerText);
                        break;
                    case AxisElementNames.GearRatio:
                        ms.GearRatio = double.Parse( n.InnerText);
                        break;
                    case AxisElementNames.MinLimit:
                        ms.MinLimit = double.Parse( n.InnerText);
                        break;
                    case AxisElementNames.MaxLimit:
                        ms.MaxLimit = double.Parse( n.InnerText);
                        break;
                    case AxisElementNames.MoveDoneWindow:
                        ms.MoveDoneWindow = double.Parse( n.InnerText);
                        break;
                    case AxisElementNames.SettlingTime:
                        ms.SettlingTimeMS = short.Parse( n.InnerText);
                        break;
                    case AxisElementNames.SlowLoopServoTime:
                        ms.SlowLoopServoTimeS = double.Parse( n.InnerText);
                        break;
                    case AxisElementNames.ForceEquation:
                        ms.ForceEquation = n.InnerText;
                        break;
                    case AxisElementNames.FlipAxisDirection:
                        ms.FlipAxisDirection = bool.Parse( n.InnerText);
                        break;
                    case AxisElementNames.DefaultServoTimeout:
                        ms.DefaultServoTimeoutS = int.Parse( n.InnerText);
                        break;
                    case AxisElementNames.MinJerk:
                        ms.MinJerk = int.Parse( n.InnerText);
                        break;
                    case AxisElementNames.JerkFactor:
                        ms.JerkFactor = double.Parse( n.InnerText);
                        break;
                    case AxisElementNames.Simulate:
                        ms.Simulate = bool.Parse( n.InnerText);
                        break;
                    case AxisElementNames.FirmwareMajor:
                        ms.FirmwareMajor = int.Parse(n.InnerText);
                        break;
                    case AxisElementNames.FirmwareMinor:
                        ms.FirmwareMinor = int.Parse(n.InnerText);
                        break;

                }
            }
            ValidateMotorSettings( id, ms);
            settings[id] = ms;
        }

        /// <summary>
        /// checks the values in the MotorSettings object after parsing (presumably, successfully)
        /// and throws an exception if any of the values seem bogus
        /// </summary>
        /// <exception cref="InvalidMotorSettingsException" />
        /// <param name="id"></param>
        /// <param name="ms"></param>
        private static void ValidateMotorSettings( byte id, MotorSettings ms)
        {
            if( ms.SlowLoopServoTimeS == 0)
                throw new InvalidMotorSettingsException( id, AxisElementNames.SlowLoopServoTime, ms.SlowLoopServoTimeS.ToString(), "0.001", "0.1");
            if( ms.EncoderLines == 0)
                throw new InvalidMotorSettingsException( id, AxisElementNames.EncoderLines, ms.EncoderLines.ToString(), "512", "16000");
            if( ms.GearRatio == 0)
                throw new InvalidMotorSettingsException( id, AxisElementNames.GearRatio, ms.GearRatio.ToString(), "0.0001", "10000");
            
            if( ms.DefaultServoTimeoutS == 0)
            {
                ms.DefaultServoTimeoutS = 300; // 300 == 5 minutes
            } else if( ms.DefaultServoTimeoutS < 0) // use negative number to signify infinitity (or as close as you can get to it with a 31-bit number)
            {
                ms.DefaultServoTimeoutS = 0; // 0 here means infinite when the Set function is called
            }

            if( ms.MinJerk <= 0)
            {
                const double jerk_min_ms = 10.0; // default jerk_min in milliseconds
                ms.MinJerk = (int)((jerk_min_ms / 1000.0) / ms.SlowLoopServoTimeS);
                if (ms.MinJerk < 1)
                    ms.MinJerk = 1; // must be a minimum of 1 IU
            }

            if( ms.JerkFactor <= 0.0001) // be careful with double-precision comparisons to 0
                ms.JerkFactor = 1.5;
        }
    }
}
