using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using BioNex.Shared.Utils;
using System.IO;

namespace BioNex.LiquidLevelDevice
{
    //
    // class that logs capture data to a file
    // used by capture state machine
    //
    public class CaptureDataFile
    {
        public CaptureDataFile(LLProperties properties)
        {
            _properties = properties;

            _volume_filename = _raw_filename = _path.IsAbsolutePath() ? _path : _path.ToAbsoluteAppPath();

            _raw_filename += string.Format("\\BNX_LV_RAW_{0}.txt", DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss"));
            _volume_filename += string.Format("\\BNX_LV_VOLUME_{0}.txt", DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss"));
        }

        string HL = @"\\ ";
        string SOH = @"\\=============================================================================";
        string EOH = @"\\-----------------------------------------------------------------------------";

        public void StartRaw(int run, string labware, LabwareDetails details)
        {
            if (!_logHeader)
                return;
            
            Write(_raw_filename, SOH);
            Write(_raw_filename, HL + string.Format("Run: '{0}' Labware: '{1}' Sensitivity: '{2}' Offset: '{3:0.00}'", run, labware, details.Sensitivity, details.CaptureOffset));
            Write(_raw_filename, HL + GetLogLineDescription(false));
            Write(_raw_filename, EOH);
        }

        public void StartVolume(int run, string labware, LabwareDetails details)
        {
            Write(_volume_filename, SOH);
            Write(_volume_filename, HL + string.Format("Run: '{0}' Labware: '{1}' Sensitivity: '{2}' Offset: '{3:0.00}'", run, labware, details.Sensitivity, details.CaptureOffset));
            Write(_volume_filename, HL + GetLogLineDescription(true));
            Write(_volume_filename, EOH);
        }

        public void WriteRaw(int run, int channel, int column, int row, double x, double y, double z)
        {
            Write(_raw_filename, GetLogLine(run, channel, column, row, x, y, z));
        }

        public void WriteVolume(int run, int channel, int column, int row, double x, double y, double z)
        {
            Write(_volume_filename, GetLogLine(run, channel, column, row, x, y, z));
        }

        void Write(string file_name, string line)
        {
            try
            {
                using (StreamWriter writer = File.AppendText(file_name))
                {
                    writer.WriteLine(line);
                    writer.Flush();
                    writer.Close();
                }
            }
            catch (Exception e)
            {
                if (_logged_error)
                    return;
                _log.Error(string.Format("Data logging failure: could not write to file '{0}'", file_name), e);
                _logged_error = true;
            }
        }

        string GetLogLine(int run, int channel, int column, int row, double x, double y, double z)
        {
            var line = new StringBuilder();
            if (_logTimestamp)
                line.Append(DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss.fff"));
            if (_logRunCounter)
            {
                line.Append(_delimeter);
                line.Append(run);
            }
            if (_logChannel)
            {
                line.Append(_delimeter);
                line.Append(channel);
            }
            if (_logColumn)
            {
                line.Append(_delimeter);
                line.Append(column);
            }
            if (_logRow)
            {
                line.Append(_delimeter);
                line.Append(row);
            }
            if (_logX)
            {
                line.Append(_delimeter);
                line.Append(string.Format("{0:0.000}", x));
            }
            if (_logY)
            {
                line.Append(_delimeter);
                line.Append(string.Format("{0:0.000}", y));
            }

            if (line.Length > 0)
                line.Append(_delimeter);
            line.Append(string.Format("{0:0.000}", z));

            return line.ToString();
        }

        string GetLogLineDescription(bool volume)
        {
            var line = new StringBuilder();
            if (_logTimestamp)
                line.Append("TIMESTAMP");
            if (_logRunCounter)
            {
                line.Append(_delimeter);
                line.Append("RUN_COUNTER");
            }
            if (_logChannel)
            {
                line.Append(_delimeter);
                line.Append("CHANNEL");
            }
            if (_logColumn)
            {
                line.Append(_delimeter);
                line.Append("COLUMN");
            }
            if (_logRow)
            {
                line.Append(_delimeter);
                line.Append("ROW");
            }
            if (_logX)
            {
                line.Append(_delimeter);
                line.Append("X");
            }
            if (_logY)
            {
                line.Append(_delimeter);
                line.Append("Y");
            }

            if (line.Length > 0)
                line.Append(_delimeter);
            line.Append( volume ? "VOLUME" : "HEIGHT");

            return line.ToString();
        }

        LLProperties _properties;
        string _raw_filename;
        string _volume_filename;

        bool _logged_error;
        static readonly ILog _log = LogManager.GetLogger(typeof(CaptureDataFile));

        string _path { get { return _properties.GetString(LLProperties.OutputFilePath); } }
        string _delimeter { get { return _properties.GetString(LLProperties.Delimeter); } }
        bool _logHeader { get { return _properties.GetBool(LLProperties.LogHeader); } }
        bool _logTimestamp { get { return _properties.GetBool(LLProperties.LogTimestamp); } }
        bool _logRunCounter { get { return _properties.GetBool(LLProperties.LogRunCounter); } }
        bool _logChannel { get { return _properties.GetBool(LLProperties.LogChannel); } }
        bool _logColumn { get { return _properties.GetBool(LLProperties.LogColumn); } }
        bool _logRow { get { return _properties.GetBool(LLProperties.LogRow); } }
        bool _logX { get { return _properties.GetBool(LLProperties.LogX); } }
        bool _logY { get { return _properties.GetBool(LLProperties.LogY); } }
    }
}
