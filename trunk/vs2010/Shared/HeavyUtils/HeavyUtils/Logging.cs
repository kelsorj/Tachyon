using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net.Config;
using log4net.Appender;
using log4net;
using System.IO;

namespace BioNex.Shared.Utils
{
    /// <summary>
    /// this is only for log4net logging
    /// </summary>
    public class Logging
    {
        public static void SetConfigurationFilePath(string path)
        {
            XmlConfigurator.Configure(new FileInfo(path));
        }

        /// <summary>
        /// Creates a new text log file by appending the new timestamp to the original log's timestamp.  Also
        /// starts a new database file.
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="filename"></param>
        /// <param name="append_date"></param>
        public static void SetLogFilePath(string folder, string filename, bool append_date)
        {
            log4net.Repository.ILoggerRepository repo = LogManager.GetRepository();
            foreach (log4net.Appender.IAppender appender in repo.GetAppenders())
            {
                FileAppender fa = appender as FileAppender;
                if (fa != null) {
                    string file_from_config = fa.File.Substring(fa.File.LastIndexOf('\\') + 1);
                    string datestamp = DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss");
                    if (!folder.EndsWith("\\"))
                        folder += "\\";
                    fa.File = String.Format("{0}{1}{2}-log.txt", folder, file_from_config, (append_date ? "-" + datestamp : ""));
                    fa.ActivateOptions();
                    continue;
                }
            }
        }

        public static void PinchDatabaseLogs()
        {
            log4net.Repository.ILoggerRepository repo = LogManager.GetRepository();
            foreach (log4net.Appender.IAppender appender in repo.GetAppenders())
            {
                AdoNetAppender aa = appender as AdoNetAppender;
                DateTime now = DateTime.Now;                
                string main_database_filename = FileSystem.GetAppPath() + "\\db\\synapsis_log.s3db";
                string pipette_database_filename = FileSystem.GetAppPath() + "\\db\\synapsis_pipette_log.s3db";
                string master_database_filename = FileSystem.GetAppPath() + "\\synapsis_log.s3db";
                string master_pipette_database_filename = FileSystem.GetAppPath() + "\\synapsis_pipette_log.s3db";                

                // if no database directory or files exist, create them
                string db_path = FileSystem.GetAppPath() + "\\db";
                if( !Directory.Exists( db_path)) {
                    Directory.CreateDirectory( db_path);
                    File.Copy( master_database_filename, main_database_filename, true);
                    File.Copy( master_pipette_database_filename, pipette_database_filename, true);
                }

                if( aa != null) {
                    aa.Close();

                    string new_connection_string;
                    string main_filename;
                    string master_filename;
                    if( aa.Name == "sqlite") {
                        new_connection_string = "Data Source=db/synapsis_log.s3db;Version=3;";
                        main_filename = main_database_filename;
                        master_filename = master_database_filename;
                    } else {
                        new_connection_string = "Data Source=db/synapsis_pipette_log.s3db;Version=3;";
                        main_filename = pipette_database_filename;
                        master_filename = master_pipette_database_filename;
                    }

                    // if we're here, then the directory exists and we just need to rename the files if they exist
                    if( File.Exists( main_filename))
                        File.Move( main_filename, main_filename + "." + now.ToString( "yyyy-MM-dd-HH_mm_ss") + ".s3db");
                    File.Copy( master_filename, main_filename, true);

                    aa.ConnectionString = new_connection_string;
                    aa.ActivateOptions();
                }
            }
        }
    }
}
