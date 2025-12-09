using System;
using System.Diagnostics;
using System.IO;

namespace BioNex.Shared.Utils
{
#if !HIG_INTEGRATION
    public class DebugFile
    {
        StreamWriter _debugFile = null;
        string _filename; // original filename stub when creating DebugFile
        string _directory; // original directory when creating DebugFile
        string _debug_path; // full path including directory and filename
        TextWriterTraceListener _debugTextListener = null;
        private const int _max_debug_filesize_bytes = 4*1024*1024; // default of 4MB logfile size
        bool _added_to_SystemDebugListener = false;

        private DebugFile ()
        {
            // Nothing here. Force programmer to call CreateNewDebugFile
            // Mark, if you want to force the programmer to call the Create method, then just make the default constructor private.
        } // DebugFile

        public DebugFile (string filename, string directory, bool add_to_SystemDebugListener)
        {
            CreateNewDebugFile(filename, directory, add_to_SystemDebugListener);
        } // DebugFile

        ~DebugFile ()
        {
            // Nothing here. Force programmer to call CloseDebugFile
        } // ~DebugFile

        public string CreateNewDebugFile(string filename, bool add_to_SystemDebugListener)
        {
            return CreateNewDebugFile(filename, @"", add_to_SystemDebugListener);
        } // CreateNewDebugFile

        public string CreateNewDebugFile(string filename, string directory, bool add_to_SystemDebugListener)
        {
            try
            {
                // Create a file for debugging output named HivePluginDebugLog.txt.
                string datestamp = DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss");
                // DKM 031810 check for directory first
                if (directory.Length == 0)
                {
                    directory = @"C:\Engineering\Logs\";
                }
                _directory = directory; // remember directory name in case we need to create a new file if this one gets too big
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                if (filename.Length == 0)
                {
                    filename = @"debugLog";
                }
                _filename = filename; // remember filename stub in case we need to create a new file if this one gets too big
                _debug_path = String.Format(directory + "{0}-" + filename + ".txt", datestamp);
                const int max_filerevs = 20;
                int n;
                for (n = 1; n <= max_filerevs; ++n)
                {
                    if (!File.Exists(_debug_path))
                        break;
                    _debug_path = String.Format(directory + "{0}-" + filename + "_" + n + ".txt", datestamp);
                }
                if (n > max_filerevs)
                    throw (new Exception("Too many revisions (" + max_filerevs + ") of " + _debug_path + " exist already."));
                _debugFile = File.CreateText(_debug_path);
                _debugFile.AutoFlush = true;

                // Create a new text writer using the output stream, and add it to
                // the debug listeners if the user wants it
                _debugTextListener = new TextWriterTraceListener(_debugFile);
                _added_to_SystemDebugListener = add_to_SystemDebugListener;
                if (add_to_SystemDebugListener)
                {
                    Debug.Listeners.Add(_debugTextListener);
                }
                WriteLine(String.Format("Created {0} to log Debug messages.", _debug_path));
                if (add_to_SystemDebugListener)
                {
                    WriteLine(@"Will be logging all System.Debug messages as well.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return _debug_path;
        } // CreateNewDebugFile

        public void CloseDebugFile ()
        {            
            if (_debugTextListener == null || _debugFile == null)
                return;
            
            string datestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ");

            _debugTextListener.WriteLine(datestamp + " : Closing " + _debug_path + " gracefully.");

            if (_added_to_SystemDebugListener)
                Debug.Listeners.Remove(_debugTextListener);

            _debugFile.Close();
            _debugFile = null;
            _debugTextListener = null;
        } // CloseDebugFile


        public void WriteLine (string msg)
        {
            WriteLine (@"", msg);
        } // WriteLine

        // Writes a timestamped msg to debugfile
        public void WriteLine (string function_name, string msg)
        {
            if (_debugTextListener == null)
                return;

            if (_debugFile.BaseStream.Length >= _max_debug_filesize_bytes)
            {
                CloseDebugFile();
                CreateNewDebugFile(_filename, _directory, _added_to_SystemDebugListener);
            }

             string datestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ");

            _debugTextListener.WriteLine(datestamp + function_name + ": " + msg);
        } // WriteLine

    } // Class Debugging
#endif
} // namespace BioNex.Shared.Utils
