using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.IError;
using System.Windows.Threading;

namespace BioNex.Hig
{
    public interface IHigModel
    {
        IAxis ShieldAxis { get; }
        IAxis SpindleAxis { get; }
        void AddError(ErrorData error);
        // if we decide that it's useful, we could add a Temp property to IAxis
        double SpindleTemperature { get; }
        string Name { get; }
        // added this so customers can see how much time is remaining in the spin
        double SpinSecRemaining { get; set; }
        string InternalSerialNumber { get; }
        IList<EepromSetting> GetEepromSettings( bool default_values_only=false);
        bool Spinning { get; }
        bool Simulating { get; }
        short Bucket2Offset { get; set; }
        Dispatcher MainDispatcher { get; }
        bool CycleDoorOnly { get; }
        bool UpdateShieldFirmware(string path);
        bool UpdateSpindleFirmware(string path);
        // DKM 2011-12-21 had to allow user to change the door positions
        int ShieldOpenPosition { get; set; }
        int ShieldClosedPosition { get; set; }
        // DKM 2012-01-20 had to allow user to change the home and bucket offset
        int Bucket1Position { get; set; }
        // DKM 2012-02-03 had to allow user to change the imbalance threshold, just in case
        int ImbalanceThreshold { get; set; }
        bool SupportsImbalance { get; }
        bool NoShieldMode { get; }
    }

    public class EepromSetting
    {
        public enum VariableTypeT { Int, Long, Fixed };

        public string HumanReadableName { get; private set; }
        public string VariableName { get; private set; }
        public VariableTypeT VariableType { get; private set; }
        /// <summary>
        /// Whether or not the variable is saved in the spindle controller.
        /// </summary>
        /// <remarks>
        /// Variables can be saved in either the spindle or shield controllers, so we need to
        /// be able to determine the target controller.  Should be true to save to spindle
        /// controller, or false to save to shield controller
        /// </remarks>
        public bool SpindleParameter { get; private set; }
        public object Value { get; set; }

        public Action<string> SetterFunc;
        public  Func<string> GetterFunc;

        public EepromSetting( string hr, string variable, VariableTypeT vartype, object val, bool save_to_spindle_controller)
        {
            HumanReadableName = hr;
            VariableName = variable;
            VariableType = vartype;
            Value = val;
            SpindleParameter = save_to_spindle_controller;
        }

        public EepromSetting( string hr, Action<string> setter, Func<string> getter)
        {
            HumanReadableName = hr;
            SetterFunc = setter;
            GetterFunc = getter;
        }

        public EepromSetting( EepromSetting other)
        {
            HumanReadableName = other.HumanReadableName;
            VariableName = other.VariableName;
            VariableType = other.VariableType;
            SpindleParameter = other.SpindleParameter;
            GetterFunc = other.GetterFunc;
            SetterFunc = other.SetterFunc;

            if( GetterFunc == null)
                Value = other.Value;
            else
                Value = GetterFunc();
        }

        /// <summary>
        /// uses the GetterFunc to read the value that should get displayed
        /// </summary>
        public void Load()
        {
            this.Value = GetterFunc();
        }

        /// <summary>
        /// uses the SetterFunc to write the value that is in the datagrid
        /// </summary>
        public void Save()
        {
            SetterFunc( this.Value.ToString());
        }
    }

    public static class HigUtils
    {
        /// <summary>
        /// There is a lower limit for Gs.  Needs to be higher than Gs used for imbalance detection.
        /// </summary>
        /// <returns></returns>
        public static int GetMinimumGs()
        {
            return 250;
        }

        public static int GetMaximumGs()
        {
            return 5000;
        }

        public static double GetEstimatedCycleTime( double gs, double rotational_radius_mm, double accel, double accel_percent, double decel, double decel_percent, double time_seconds)
        {
            try {
                double rpm = HigUtils.CalculateRpmFromGs( gs, rotational_radius_mm);
                double degPerSecFromRpm = rpm * 360 / 60.0;
                double degPerSec2FromAccel = accel * accel_percent / 100.0;
                double degPerSec2FromDecel = decel * decel_percent / 100.0;
                double accel_time_s = degPerSecFromRpm / degPerSec2FromAccel;
                double decel_time_s = degPerSecFromRpm / degPerSec2FromDecel;
                return accel_time_s + time_seconds + decel_time_s + 12.0; // added 12s to account for door opening and closing (for HG3 rotating shield)
            } catch( Exception) {
                // user probably passed an invalid value, so this is to catch div by zero
                return 0;
            }
        }

        // http://www.ehow.com/how_5018706_convert-centrifuge-rpm-rcf-_g_force_.html
        public static double CalculateGsFromRpm(double rpm, double rotational_radius_mm)
        {
            double rad_per_sec = rpm / 60.0 * 2.0 * 3.14159;
            return rad_per_sec * rad_per_sec * rotational_radius_mm / 1000.0 / 9.8;
        }
        public static double CalculateRpmFromGs(double g, double rotational_radius_mm)
        {
            double rad_per_sec = Math.Sqrt(g * 9.8 * 1000.0 / rotational_radius_mm);
            return rad_per_sec / 3.14159 / 2.0 * 60.0;
        }

        // The following constants for specifically for ISD 860 on spindle axis for HG2
        const double max_adc_range = 20.0; // -10..+10 VDC for ISD 860 on HG1 and HG2
        const double adc_offset = 10.0; // 10.0 VDC for ISD 860 on HG1 and HG2
        const double Kuf_adc = 65472 / max_adc_range;
        const double sensor_range_mm = 4.0 - 1.0; // sensor on HG2 is 1.0..4.0 mm
        const double sensor_range_offset_mm = 1.0; // sensor on HG2 has 0V == 1.0 mm offset
        const double sensor_range_volts = 10.0 - 0.0; // sensor on HG2 is 0.0..10.0 V output for sensor_range_mm

        // The following constants for specifically for ISD 860 on spindle axis
        const double temp_sensor_gain = 0.01; // Volts/degC for ISD860
        const double temp_sensor_output_at_0deg = 0.5; // Volts at 0 degC for ISD860
        const double KTf = temp_sensor_gain / 3.3 * 65472; // 198.4 bits/degC
        const double temp_offset = (temp_sensor_output_at_0deg * 65472 / 3.3) / KTf; // 50 degC

        // This should be fed a raw number from AD5_1 - AD5_0 where _1 and _0 are two separate readings
        public static double ConvertImbalanceAmplitutde_IU_to_mm(UInt16 imb_ampl_iu)
        {
            double imb_ampl_mm = ((double)imb_ampl_iu / Kuf_adc) * (sensor_range_mm /  sensor_range_volts); // we don't care about offset here because this is an amplitutde, and offset was already subtracted out
            return imb_ampl_mm;
        }

        // This will compute a raw number which can be compared to two AD5 readings such as AD5_1 - AD5_0 where _1 and _0 are two separate readings
        public static UInt16 ConvertImbalanceAmplitutde_mm_to_IU(double imb_ampl_mm)
        {
            UInt16 imb_ampl_iu = (UInt16)(imb_ampl_mm / (sensor_range_mm / sensor_range_volts) * Kuf_adc); // we don't care about offset here because this is an amplitutde, and offset was already subtracted out
            return imb_ampl_iu;
        }

        // This should be fed the raw number from AD5
        public static double ConvertImbalanceMeasurement_IU_to_mm(UInt16 imb_iu)
        {
            double imb_mm = (((double)imb_iu / Kuf_adc) - adc_offset) * (sensor_range_mm / sensor_range_volts) + sensor_range_offset_mm;
            return imb_mm;
        }

        // This will compute a raw number that is comparable to AD5
        public static UInt16 ConvertImbalanceMeasurement_mm_to_IU(double imb_mm)
        {
            UInt16 imb_iu = (UInt16)(((imb_mm - sensor_range_offset_mm) / (sensor_range_mm / sensor_range_volts) + adc_offset) * Kuf_adc);
            return imb_iu;
        }

        /* Temperature conversions:
             temp_sensor_gain = 0.01 Volts/degC
             temp_sensor_output_at_0deg = 0.5 Volts
             KTf = temp_sensor_gain / 3.3 * 65472 bits/degC
             KTf = 198.4
             temp_offset = (temp_sensor_output_at_0deg * 65472 / 3.3) / KTf degC
             temp_offset = 50 degC
             degC = AD7 / KTf - temp_offset
             AD7 = KTf * (degC + temp_offset)
        */
        // This should be fed the raw number from AD7
        public static double ConvertDriveTemperature_IU_to_degC(UInt16 temp_iu)
        {
            double temp_degC = (double)temp_iu / KTf - temp_offset;
            return temp_degC;
        }

        // This computes a raw value that is comparable to AD7 from a temperature in degC
        public static UInt16 ConvertDriveTemperature_degC_to_IU(double temp_degC)
        {
            UInt16 temp_iu = (UInt16)((temp_degC + temp_offset) * KTf);
            return temp_iu;
        }

        public static double ConvertIUToDegrees( int iu, int num_encoder_lines)
        {
            double conversion = num_encoder_lines * 4;
            var angle = iu / conversion * 360.0;
            // spindle position is a signed number, so we could be in the negative region
            // first modulo gets to -360:360, add 360 to get to 0:720 then final modulo to get 0:360
            return (360.0 + angle % 360.0) % 360.0;
        }

        public static void GetFirmwareVersion( IAxis axis, out short major, out short minor)
        {
            // default to 1.0
            major = 1;
            minor = 0;
            try {
                // if we fail to read the variable, then assume v1.0.  This is unlikely because, as it turns out,
                // firmware_major DOES exist in the v1.3 firmware that is embedded in the assembly.  Therefore,
                // we'll read whatever value is at its address, and it will be garbage.  From testing v1.0 firmware
                // I know that it's 16674
                if (!axis.GetIntVariable("firmware_major", out major) || !axis.GetIntVariable("firmware_minor", out minor)) {
                    // reset in case one succeeds and the other fails
                    major = 1;
                    minor = 0;
                    return;
                }
                if (major > 1000 || minor > 1000) {
                    major = 1;
                    minor = 0;
                }
            } catch( Exception) {
            }
        }

        public static string GetFirmwareVersions( IAxis shield_axis, IAxis spindle_axis)
        {
            // DKM 2011-11-15 read the serial numbers from the shield and spindle axes
            short major;
            short minor;
            GetFirmwareVersion( shield_axis, out major, out minor);
            string shield_firmware_version = String.Format( "{0}.{1}", major, minor);
            GetFirmwareVersion( spindle_axis, out major, out minor);
            string spindle_firmware_version = String.Format( "{0}.{1}", major, minor);
            return String.Format( "shield firmware {0}, spindle firmware {1}", shield_firmware_version, spindle_firmware_version);
        }

        public static void ParseCombinedFirmwareString(string firmware, out short shield_major, out short shield_minor, out short spindle_major, out short spindle_minor)
        {
            shield_major = 1;
            shield_minor = 0;
            spindle_major = 1;
            spindle_minor = 0;

            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"\s*shield\s+firmware\s+(\d+).(\d+),\s*spindle\s+firmware\s+(\d+).(\d+)");
            System.Text.RegularExpressions.MatchCollection matches = regex.Matches(firmware);
            if (matches.Count == 0)
                return;
            shield_major = short.Parse(matches[0].Groups[1].Value);
            shield_minor = short.Parse(matches[0].Groups[2].Value);
            spindle_major = short.Parse(matches[0].Groups[3].Value);
            spindle_minor = short.Parse(matches[0].Groups[4].Value);
        }
    }
}
