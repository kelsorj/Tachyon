using System;
using System.Collections.Generic;
using System.Linq;

namespace BioNex.LiquidLevelDevice
{
    public class LLProperties : IDictionary<string, string>
    {
        // These properties apply to the entire device
        public const string SensorCount = "sensor count";
        public const string SensorCANDeviceID = "sensor CAN device id";
        public const string MotorCANDeviceID = "motor CAN device id";
        public const string ConfigFolder = "config folder";
        public const string VolumeMapFolder = "volume map folder";
        public const string X_TP = "teachpoint x";
        public const string Y_TP = "teachpoint y";
        public const string Z_TP = "teachpoint z";
        public const string R_TP = "teachpoint r";
        public const string R_PORTRAIT_TP = "portrait r";
        public const string R_LANDSCAPE_TP = "landscape r";
        public const string ParallelGroupSize = "parallel group size";
        public const string CaptureOffset = "capture offset";
        public const string SamplesPerWell = "samples per well";
        public const string CalibrationOffsetY = "calibration offset y";
        public const string CalibrationSamples = "calibration samples";
        public const string CalibrationPoints = "calibration points";
        public const string CalibrationMaxZA = "calibration max z mode a";
        public const string CalibrationMaxZB = "calibration max z mode b";
        public const string CalibrationMaxZC = "calibration max z mode c";
        public const string CalibrationMaxZD = "calibration max z mode d";
        public const string XYSlope = "xy slope";
        public const string ZYSlope = "zy slope";
        public const string XArcCorrection0 = "x arc correction 0";
        public const string XArcCorrection1 = "x arc correction 1";
        public const string XArcCorrection2 = "x arc correction 2";

        public const string HiResMinX = "hires min x";
        public const string HiResMaxX = "hires max x";
        public const string HiResStepX = "hires step x";
        public const string HiResMinY = "hires min y";
        public const string HiResMaxY = "hires max y";
        public const string HiResStepY = "hires step y";
        public const string HiResMinC = "hires min column";
        public const string HiResMaxC = "hires max column";
        public const string HiResStepC = "hires step column";
        public const string HiResFilterThreshold = "hires filter threshold";
        public const string HiResShowGraphs = "hires show graphs";
        public const string HiResFastScanVelocity = "hires fast scan velocity";
        public const string HiResFloorToZero = "hires clamp floor to zero";
        public const string HiResShowWellCenters = "hires show well centers";

        public const string SettleTime = "sample settle time";
        public const string CaptureRadiusStart = "capture start radius";
        public const string CaptureRadiusStep = "capture radius step";
        public const string CaptureRadiusMax = "capture max radius";
        public const string CaptureRejectionRadius = "capture rejection radius";
        public const string CaptureSeekDeviation = "capture seek deviation";
        public const string CaptureFloorToZero = "capture clamp floor to zero";

        public const string LocateTeachpointPinToEdgeX = "locate teachpoint pin to edge x";
        public const string LocateTeachpointPinToEdgeY = "locate teachpoint pin to edge y";
        public const string LocateTeachpointFeatureWidthX = "locate teachpoint feature width x";
        public const string LocateTeachpointFeatureWidthY = "locate teachpoint feature width y";
        public const string LocateTeachpointPinToWellX = "locate techpoint pin to well center x";
        public const string LocateTeachpointPinToWellY = "locate techpoint pin to well center y";
        public const string LocateTeachpointFeatureToFeatureY = "locate teachpoint feature to feature y";
        
        public const string Simulate = "simulate";
        public const string Disable3D = "disable 3d";
        public const string MaxFitOrder = "max fit order";
        public const string OutputFilePath = "output file path";
        public const string Delimeter = "delimeter";
        public const string NewFilePerCapture = "new file per capture";
        public const string LogHeader = "log header";
        public const string LogTimestamp = "log timestamp";
        public const string LogRunCounter = "log run counter";
        public const string LogChannel = "log channel";
        public const string LogColumn = "log column";
        public const string LogRow = "log row";
        public const string LogX = "log x";
        public const string LogY = "log y";
        public const string EnableSystemPanel = "enable synapsis system panel";
        public const string HasRAxis = "has r axis";
        public const string AllowUpgrade = "allow upgrade";
        public const string PromptIfUpgrading = "prompt if upgrading";

        // These properties are indexed by sensor - use the index(prop, i) method to access -- the actual property name is given by the Format string in the index() method
        // ---v
        public const string Port = "port";
        public const string Enable = "enable";
        public const string Mode = "measurement mode";
        public const string Format = "data format";
        public const string Sensitivity = "sensitivity";
        public const string Averaging = "averaging";
        public const string TemperatureCompensation = "temperature compensation";
        public const string XDeviation = "x deviation";
        public const string YDeviation = "y deviation";
        // ---^
        public static string index(string prop, int i) { return string.Format("{0} {1}", prop, i); }

        private Dictionary<string, string> _storage;
        public LLProperties(Dictionary<string, string> stored_properties)
        {
            _storage = new Dictionary<string, string>(stored_properties);

            SetDefaultValues();
        }

        void SetDefaultValues()
        {
            SetDefault(SensorCount, 8);
            SetDefault(SensorCANDeviceID, 0);
            SetDefault(MotorCANDeviceID, 1);
            SetDefault(ConfigFolder, "config");
            SetDefault(VolumeMapFolder, "config");
            SetDefault(X_TP, 0.0);
            SetDefault(Y_TP, 0.0);
            SetDefault(Z_TP, 0.0);
            SetDefault(R_TP, 0.0);
            SetDefault(R_PORTRAIT_TP, 0.0);
            SetDefault(R_LANDSCAPE_TP, 0.0);
            SetDefault(ParallelGroupSize, 8);
            SetDefault(CaptureOffset, 3.5);
            SetDefault(SamplesPerWell, 1);
            SetDefault(CalibrationOffsetY, 25.0);
            SetDefault(CalibrationSamples, 5);
            SetDefault(CalibrationPoints, 3);
            SetDefault(CalibrationMaxZA, 70.0);
            SetDefault(CalibrationMaxZB, 70.0);
            SetDefault(CalibrationMaxZC, 66.0);
            SetDefault(CalibrationMaxZD, 28.0);
            SetDefault(XYSlope, 0.0);
            SetDefault(ZYSlope, 0.0);
            SetDefault(XArcCorrection0, 0.0);
            SetDefault(XArcCorrection1, 0.0);
            SetDefault(XArcCorrection2, 0.0);
            SetDefault(HiResMinX, -4.5);
            SetDefault(HiResMaxX, 4.5);
            SetDefault(HiResStepX, 0.01);
            SetDefault(HiResMinY, -4.5);
            SetDefault(HiResMaxY, 4.5);
            SetDefault(HiResStepY, 0.01);
            SetDefault(HiResMinC, 1);
            SetDefault(HiResMaxC, 12);
            SetDefault(HiResStepC, 1);
            SetDefault(HiResFilterThreshold, 1.4);
            SetDefault(HiResShowGraphs, false);
            SetDefault(HiResFastScanVelocity, 1.0);
            SetDefault(HiResFloorToZero, false);
            SetDefault(HiResShowWellCenters, false);
            SetDefault(SettleTime, 0.0);
            SetDefault(CaptureRadiusStart, 0.1);
            SetDefault(CaptureRadiusStep, 0.1);
            SetDefault(CaptureRadiusMax, 0.5);
            SetDefault(CaptureRejectionRadius, 1000.0);
            SetDefault(CaptureSeekDeviation, false);
            SetDefault(CaptureFloorToZero, true);
            SetDefault(LocateTeachpointPinToEdgeX, 10.35);
            SetDefault(LocateTeachpointPinToEdgeY, 22.35);
            SetDefault(LocateTeachpointFeatureWidthX, 2.0);
            SetDefault(LocateTeachpointFeatureWidthY, 2.0);
            SetDefault(LocateTeachpointPinToWellX, 11.35);
            SetDefault(LocateTeachpointPinToWellY, 14.35);
            SetDefault(LocateTeachpointFeatureToFeatureY, 9.0);
            SetDefault(Simulate, false);
            SetDefault(Disable3D, false);
            SetDefault(MaxFitOrder, 3);
            SetDefault(OutputFilePath, "data");
            SetDefault(Delimeter, ",");
            SetDefault(NewFilePerCapture, true);
            SetDefault(LogHeader, true);
            SetDefault(LogTimestamp, true);
            SetDefault(LogRunCounter, true);
            SetDefault(LogChannel, true);
            SetDefault(LogColumn, true);
            SetDefault(LogRow, true);
            SetDefault(LogX, true);
            SetDefault(LogY, true);
            SetDefault(EnableSystemPanel, false);
            SetDefault(HasRAxis, false);
            SetDefault(AllowUpgrade, true);
            SetDefault(PromptIfUpgrading, true);

            for (int i = 0; i < GetInt(SensorCount); ++i)
            {
                SetDefault(index(Port, i), 120 + i);
                SetDefault(index(Enable, i), true);
                SetDefault(index(Mode, i), "A");
                SetDefault(index(Format, i), "A");
                SetDefault(index(Sensitivity, i), "D");
                SetDefault(index(Averaging, i), "D");
                SetDefault(index(TemperatureCompensation, i), "1");
                SetDefault(index(XDeviation, i), 0.0);
                SetDefault(index(YDeviation, i), 0.0);
            }

        }

        #region IDictionary implementation
        public void Add(string key, string value)
        {
            _storage.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return _storage.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get { return _storage.Keys; }
        }

        public bool Remove(string key)
        {
            return _storage.Remove(key);
        }

        public bool TryGetValue(string key, out string value)
        {
            return _storage.TryGetValue(key, out value);
        }

        public ICollection<string> Values
        {
            get { return _storage.Values; }
        }

        public string this[string key]
        {
            get
            {
                return _storage[key];
            }
            set
            {
                _storage[key] = value;
            }
        }

        void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item)
        {
            ((IDictionary<string, string>)_storage).Add(item);
        }

        public void Clear()
        {
            _storage.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return _storage.Contains(item);
        }

        void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            ((IDictionary<string, string>)_storage).CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _storage.Count; }
        }

        bool ICollection<KeyValuePair<string, string>>.IsReadOnly
        {
            get { return ((IDictionary<string, string>)_storage).IsReadOnly; }
        }

        bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item)
        {
            return ((IDictionary<string, string>)_storage).Remove(item);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _storage.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _storage.GetEnumerator();
        }
        #endregion

        #region default value support
        void SetDefault<T>(string key, T default_value)
        {
            if (ContainsKey(key))
                return;
            this[key] = default_value.ToString();
        }

        public string GetString(string key)
        {
            if (ContainsKey(key))
                return this[key];
            throw new KeyNotFoundException(string.Format("{0} was not present in properties database", key));
        }

        public double GetDouble(string key)
        {
            if (ContainsKey(key))
            {
                string value = this[key];
                double result;
                if (double.TryParse(value, out result))
                    return result;
                throw new FormatException(string.Format("key '{0}' value '{1}' could not be parsed as a double", key, value));
            }
            throw new KeyNotFoundException(string.Format("key '{0}' was not present in properties database", key));
        }

        public int GetInt(string key)
        {
            if (ContainsKey(key))
            {
                string value = this[key];
                int result;
                if (int.TryParse(value, out result))
                    return result;
                throw new FormatException(string.Format("key '{0}' value '{1}' could not be parsed as an int", key, value));
            }
            throw new KeyNotFoundException(string.Format("key '{0}' was not present in properties database", key));
        }

        public uint GetUInt(string key)
        {
            if (ContainsKey(key))
            {
                string value = this[key];
                uint result;
                if (uint.TryParse(value, out result))
                    return result;
                throw new FormatException(string.Format("key '{0}' value '{1}' could not be parsed as a uint", key, value));
            }
            throw new KeyNotFoundException(string.Format("key '{0}' was not present in properties database", key));
        }

        public bool GetBool(string key)
        {
            if (ContainsKey(key))
            {
                string value = this[key];
                bool result;
                if (bool.TryParse(value, out result))
                    return result;
                throw new FormatException(string.Format("key '{0}' value '{1}' could not be parsed as a bool", key, value));
            }
            throw new KeyNotFoundException(string.Format("key '{0}' was not present in properties database", key));
        }
        #endregion
    }
}