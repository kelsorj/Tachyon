using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.IO;

namespace HiveRobotLogger
{
    public class LogFile
    {
        StreamWriter _logFile = null;
        string _filename; // original filename stub when creating LogFile
        string _directory; // original directory when creating LogFile
        string _log_path; // full path including directory and filename
        TextWriterTraceListener _debugTextListener = null;
        int _max_debug_filesize_bytes = 4 * 1024 * 1024; // default of 4MB logfile size
        bool _added_to_SystemDebugListener = false;

        private LogFile ()
        {
            // Nothing here. Force programmer to call CreateNewLogFile
        } // LogFile

        public LogFile (string filename, string directory, bool add_to_SystemDebugListener)
        {
            CreateNewLogFile(filename, directory, add_to_SystemDebugListener);
        } // LogFile

        ~LogFile ()
        {
            // Nothing here. Force programmer to call CloseLogFile
        } // ~LogFile

        // Flush logfile to disk
        public void Flush()
        {
            _logFile.Flush();
        }

        public string CreateNewLogFile(string filename, bool add_to_SystemDebugListener)
        {
            return CreateNewLogFile(filename, "", add_to_SystemDebugListener);
        } // CreateNewLogFile

        public string CreateNewLogFile(string filename, string directory, bool add_to_SystemDebugListener)
        {
            try
            {
                string datestamp = DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss");
                // Check for directory first
                if (directory.Length == 0)
                {
                    directory = @"C:\Engineering\Logs\";
                }
                _directory = directory; // remember directory name in case we need to create a new file if this one gets too big
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                if (filename.Length == 0)
                {
                    filename = "LogFile";
                }
                _filename = filename; // remember filename stub in case we need to create a new file if this one gets too big
                _log_path = String.Format(directory + "{0}-" + filename + ".csv", datestamp);
                const int max_filerevs = 99999;
                int n;
                for (n = 1; n <= max_filerevs; ++n)
                {
                    if (!File.Exists(_log_path))
                        break;
                    _log_path = String.Format(directory + "{0}-" + filename + "_" + n + ".txt", datestamp);
                }
                if (n > max_filerevs)
                    throw (new Exception("Too many revisions (" + max_filerevs + ") of " + _log_path + " exist already."));
                _logFile = File.CreateText(_log_path);
                // Set AutoFlush to tru to write file after each line. Very resource intensive...
                _logFile.AutoFlush = false;

                // Create a new text writer using the output stream, and add it to
                // the debug listeners if the user wants it
                _debugTextListener = new TextWriterTraceListener(_logFile);
                _added_to_SystemDebugListener = add_to_SystemDebugListener;
                if (add_to_SystemDebugListener)
                {
                    Debug.Listeners.Add(_debugTextListener);
                }
                Debug.WriteLine(String.Format("Created {0} to log Debug messages.", _log_path));
                if (add_to_SystemDebugListener)
                {
                    WriteLine("Will be logging all System.Debug messages as well.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return _log_path;
        } // CreateNewLogFile

        public void CloseLogFile ()
        {            
            if (_debugTextListener == null || _logFile == null)
                return;
            
            string datestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ");

            Debug.WriteLine(datestamp + " : Closing " + _log_path + " gracefully.");

            if (_added_to_SystemDebugListener)
                Debug.Listeners.Remove(_debugTextListener);

            _logFile.Close();
            _logFile = null;
            _debugTextListener = null;
        } // CloseLogFile

        // Writes a timestamped msg to logfile
        public void WriteLine(string msg, bool timestamp=true)
        {
            if (_debugTextListener == null)
                return;

            if (_logFile.BaseStream.Length >= _max_debug_filesize_bytes)
            {
                CloseLogFile();
                CreateNewLogFile(_filename, _directory, _added_to_SystemDebugListener);
            }

            if( timestamp)
            {
                DateTime now = DateTime.Now;
                DateTime epoch = new DateTime(1970, 1, 1);
                string datestamp_secs = String.Format("{0}", (now - epoch).TotalSeconds);
                string datestamp1 = now.ToString("yyyy-MM-dd HH:mm:ss");
                string datestamp2 = now.ToString("yyyy-MM-dd,HH:mm:ss.ffff");

                _debugTextListener.WriteLine(datestamp_secs + "," + datestamp1 + "," + datestamp2 + "," + msg);
            }
            else
                _debugTextListener.WriteLine(msg);
        } // WriteLine

        public void PinchOffLogAndCreateNewLog ()
        {
            CloseLogFile();
            CreateNewLogFile(_filename, _directory, _added_to_SystemDebugListener);
        }

    } // Class LogFile
} // namespace HiveRobotLogger
