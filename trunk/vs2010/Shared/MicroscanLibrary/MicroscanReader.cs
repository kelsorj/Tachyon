using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Controls;
using BioNex.Shared.Utils;
using log4net;

[assembly:InternalsVisibleTo( "MicroscanTests")]
namespace BioNex.Shared.Microscan
{
    public class SaveImageException : ApplicationException
    {
        public SaveImageException( string filename) : base( String.Format( "Could not save barcode image named '{0}'", filename))
        {
        }
    }

    public class IncomingSerialDataEventArgs
    {
        public string Data { get; private set; }
        public IncomingSerialDataEventArgs( string data)
        {
            Data = data;
        }
    }

    public class MicroscanFilename
    {
        public string Folder { get; private set; }
        public string Filename { get; private set; }
        public int Height { get; private set; }
        public int Width { get; private set; }
        public int ImageId { get; private set; }
        public int Age { get; private set; }
        /// <summary>
        /// This should be set after downloading the image so that the GUI can render it
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// takes a full filename and breaks it up into its components
        /// e.g. /saved/noread/0/300x400_gs.bmp 00031 00015 gets broken up into:
        /// Folder: /saved/noread/0/
        /// Filename: 300x300_gs.bmp
        /// Height: 400
        /// Width: 300
        /// ImageId: 31
        /// Age: 15
        /// </summary>
        /// <param name="raw_filename"></param>
        public MicroscanFilename( string raw_filename)
        {
            Regex regex = new Regex( @"(/saved/noread/\d+/)((\d+)x(\d+)_gs.bmp) 0*(\d+) 0*(\d+)");
            Match match = regex.Match( raw_filename);
            GroupCollection group = match.Groups;
            Folder = group[1].ToString();
            Filename = group[2].ToString();
            Width = group[3].ToInt();
            Height = group[4].ToInt();
            ImageId = group[5].ToInt();
            Age = group[6].ToInt();
        }

        public string ToJpegFilename( int new_width, int new_height, int quality)
        {
            return String.Format( "{0}{1}x{2}_q{3}.jpg", Folder, Width, Height, quality);
        }

        public override string ToString()
        {
            return String.Format( "{0}{1}", Folder, Filename);
        }
    }

    public delegate void IncomingSerialDataEventHandler( object sender, IncomingSerialDataEventArgs e);

    public interface IMicroscanReader
    {
        event IncomingSerialDataEventHandler SerialDataReceived;

        bool Connected { get; set; }

        void SendNoSave();
        void SendAndSave();
        void SendAndSaveAsCustomerDefaults();

        void DeleteAllImages();
        List<MicroscanFilename> GetImageFilenames();
        /// <summary>
        /// Downloads the image with the given filename, and places it in the computer's temp folder
        /// </summary>
        /// <param name="file"></param>
        /// <param name="scaling"></param>
        /// <param name="quality"></param>
        /// <returns>the filename on the host PC that was saved</returns>
        string DownloadImage( MicroscanFilename file, double scaling, int quality);
    }

    public class MicroscanReader : IMicroscanReader
    {
        public static int ImageCounter { get; set; }
        private SerialPort _port { get; set; }
        private WOIControl _woi_control { get; set; }
        //private IAsyncResult _flyby_thread_result { get; set; }
        public bool Connected { get; set; }
        private static readonly ILog _log = LogManager.GetLogger( typeof( MicroscanReader));
        private ConfigurationDatabase _configuration_database;
        public DecodeSettings ReaderDecodeSettings { get; private set; }
        public TimeSpan LastImageTime { get; set; }
        private int _last_index = -1;

        public event IncomingSerialDataEventHandler SerialDataReceived;

        public class PropertyNames
        {
            public static string Gain = "Gain";
            public static string ShutterSpeed = "Shutter speed";
            public static string FocalDistance = "Focal distance [in]";
            public static string SubSampling = "Sub-sampling";
            public static string RowPointer = "WOI row pointer";
            public static string ColumnPointer = "WOI column pointer";
            public static string RowDepth = "WOI row depth";
            public static string ColumnWidth = "WOI column width";
            public static string NarrowMargins = "Narrow margins";
            public static string BackgroundColor = "Background color";
        }

        #region Window of interest class
        public class WindowOfInterest
        {
            public int RowPointer { get; set; }
            public int ColumnPointer { get; set; }
            public int RowDepth { get; set; }
            public int ColumnWidth { get; set; }

            public WindowOfInterest()
            {
                RowPointer = 0;
                ColumnPointer = 0;
                RowDepth = 1536;
                ColumnWidth = 2048;
            }

            public WindowOfInterest( int rp, int cp, int rd, int cw)
            {
                RowPointer = rp;
                ColumnPointer = cp;
                RowDepth = rd;
                ColumnWidth = cw;
            }

            public override bool Equals(object obj)
            {
                if( this == obj)
                    return true;

                WindowOfInterest other = (WindowOfInterest)obj;
                return this.ColumnPointer == other.ColumnPointer && this.ColumnWidth == other.ColumnWidth &&
                       this.RowDepth == other.RowDepth && this.RowPointer == other.RowPointer;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }
        #endregion

        #region Configuration database class
        public class ConfigurationDatabase
        {
            private static readonly ILog _log = LogManager.GetLogger( typeof( ConfigurationDatabase));

            public class ConfigurationDatabaseIndex
            {
                public int Index { get; set; }
                public DecodeSettings Settings { get; set; }

                public ConfigurationDatabaseIndex()
                {
                    Settings = new DecodeSettings();
                }

                public static ConfigurationDatabaseIndex FromReaderResponseString( string response)
                {
                    Regex regex = new Regex( @"\<K255,((\d+,?){15})\>");
                    Match match = regex.Match( response);
                    GroupCollection groups = match.Groups;
                    // group 1 is the one with all of the values in it
                    string comma_delimited_values = groups[1].ToString();
                    string[] values = comma_delimited_values.Split( ',');

                    ConfigurationDatabaseIndex new_index = new ConfigurationDatabaseIndex();
                    new_index.Index = values[0].ToInt();
                    new_index.Settings.ShutterSpeed = values[1].ToInt();
                    new_index.Settings.Gain = values[2].ToInt();
                    new_index.Settings.FocalDistanceInches = values[3].ToDouble() / 100;
                    new_index.Settings.SubSampling = values[4].ToInt();
                    int row_pointer = values[5].ToInt();
                    int column_pointer = values[6].ToInt();
                    int row_depth = values[7].ToInt();
                    int column_width = values[8].ToInt();
                    new_index.Settings.ThresholdMode = values[9].ToInt();
                    new_index.Settings.FixedThresholdValue = values[10].ToInt();
                    new_index.Settings.ProcessingMode = values[11].ToInt();
                    new_index.Settings.NarrowMargins = values[12].ToInt();
                    new_index.Settings.BackgroundColor = values[13].ToInt();
                    new_index.Settings.Symbologies = values[14].ToInt();
                    new_index.Settings.WOI = new WindowOfInterest ( row_pointer, column_pointer, row_depth, column_width );

                    return new_index;
                }
            }

            public List<ConfigurationDatabaseIndex> Configurations { get; set; }

            public ConfigurationDatabase()
            {
                Configurations = new List<ConfigurationDatabaseIndex>();
            }

            /// <summary>
            /// Takes the settings in the ConfigurationDatabase and saves them into the reader
            /// </summary>
            public void SaveToReader( MicroscanReader reader)
            {
                // loop over all of the configuration indexes
                // 1. get the settings
                // 2. save current settings as index settings

                for( int i=0; i<Configurations.Count(); i++) {
                    // 1
                    ConfigurationDatabaseIndex config_index = Configurations[i];
                    // 2
                    bool try_again = true;
                    const int num_tries = 5;
                    int ctr = 0;
                    while( try_again && ctr++ < num_tries) {
                        reader.SaveConfigurationIndexSettings( i, config_index.Settings, false);
                        DecodeSettings settings_just_saved = reader.GetConfigurationIndexSettings( i);
                        if( !settings_just_saved.Equals( config_index.Settings))
                            try_again = true;
                        else
                            try_again = false;
                    }
                    if( ctr >= num_tries)
                        _log.Error( "One of the barcode configurations did not load properly.  Please try again by reloading the barcode configuration database from diagnostics.");
                }
                reader.SendAndSave();
            }
        }
        #endregion

        #region DecodeSettings class
        public class DecodeSettings
        {
            public int Gain { get; set; }
            public int ShutterSpeed { get; set; }
            public double FocalDistanceInches { get; set; }
            public int SubSampling { get; set; }
            public WindowOfInterest WOI { get; set; }

            // DKM 2011-05-11 there isn't a standalone property for Threshold Mode and Fixed Threshold Value, except in
            //                pharmacode.  So just keep this as part of the configuration database index properties
            // DKM 2011-05-19 added this back in to make requesting configuration index properties easier
            /// <summary>
            /// only relevant for configuration database
            /// </summary>
            public int ThresholdMode { get; set; }
            /// <summary>
            /// only relevant for configuration database
            /// </summary>
            public int FixedThresholdValue { get; set; }

            // DKM 2011-05-11 I removed this because there isn't a reader setting for it.  It is specific to the configuration database
            // DKM 2011-05-19 added this back in to make requesting configuration index properties easier
            /// <summary>
            /// only relevant for configuration database
            /// </summary>
            public int ProcessingMode { get; set; }
            public int NarrowMargins { get; set; }
            public int BackgroundColor { get; set; }
            // DKM 2011-05-11 I removed this because there isn't a reader setting for it.  It is specific to the configuration database
            // DKM 2011-05-19 added this back in to make requesting configuration index properties easier
            /// <summary>
            /// only relevant for configuration database
            /// </summary>
            public int Symbologies { get; set; }

            public int LineSpeed { get; set; }

            public DecodeSettings()
            {
                WOI = new WindowOfInterest( 0, 0, 1536, 2048);
            }

            /// <summary>
            /// Note that this only checks equality with settings that are individually
            /// readable from the reader settings.  Some parameters, like Thresholding,
            /// Fixed Threshold Value, and Processing Mode can't be parsed out directly,
            /// so I can't use them for comparing equality.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                if( this == obj)
                    return true;

                DecodeSettings other = (DecodeSettings)obj;

                return this.BackgroundColor == other.BackgroundColor &&
                       this.FocalDistanceInches == other.FocalDistanceInches && this.Gain == other.Gain &&
                       this.LineSpeed == other.LineSpeed && this.NarrowMargins == other.NarrowMargins &&
                       this.ShutterSpeed == other.ShutterSpeed &&
                       this.SubSampling == other.SubSampling && 
                       this.WOI.Equals( other.WOI);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }
        #endregion

        public string ConfigurationPath { get; private set; }

        public MicroscanReader()
        {
        }

        public UserControl GetConfigurationGui()
        {
            return new Mini3Control(this); // to prevent same control from getting databound to new instances of Hive diagnostics
        }

        /// <summary>
        /// Sends the K255? command to get the settings for all 10 capture indexes
        /// </summary>
        /// <param name="config_filepath">The XML config file that holds the configuration database settings</param>
        /// <param name="use_reader_values_if_no_config_file">
        /// If the config file doesn't exist, then create the config file using the reader's current database settings
        /// </param>
        public void LoadConfigurationDatabase( string config_filepath, bool use_reader_values_if_no_config_file)
        {
            ConfigurationPath = config_filepath;
            // if desired, load the current database configuration from the reader
            ConfigurationDatabase reader_database = null;
            if( use_reader_values_if_no_config_file) {
                // send K255?
                string response = SendCommandAndGetResponse( "<K255?>");
                // parse out all of the capture index information
                Regex regex = new Regex( @"\<.*?\>");
                MatchCollection matches = regex.Matches( response);
                Debug.Assert( matches.Count == 10, "Unable to parse configuration database response from barcode reader");
                reader_database = new ConfigurationDatabase();
                foreach( var match in matches) {
                    reader_database.Configurations.Add( ConfigurationDatabase.ConfigurationDatabaseIndex.FromReaderResponseString( match.ToString()));
                }
            }

            // load the specified configuration file
            _configuration_database = FileSystem.LoadXmlConfiguration<ConfigurationDatabase>( config_filepath, reader_database);
            // push all of the configuration indexes into the reader
            _configuration_database.SaveToReader( this);
        }

        /// <summary>
        /// reads data from the serial port and also notifies any event subscribers
        /// </summary>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public string ReadTo( string delimiter)
        {
            string incoming = _port.ReadTo( delimiter);
            if( SerialDataReceived != null)
                SerialDataReceived( this, new IncomingSerialDataEventArgs( incoming));
            return incoming;
        }

        public string SendCommandAndGetResponse( string command)
        {
            _port.DiscardInBuffer();
            _log.DebugFormat( "Sending command: {0}", command);
            _port.Write( command);
            return ReadTo( "\r\n");
        }

        public void SendCommandNoResponse( string command)
        {
            _port.DiscardInBuffer();
            _log.DebugFormat( "Sending command: {0}", command);
            _port.Write( command);
        }

        /// <summary>
        /// Need a separate send method for this, because op codes like <op,9> return data, but don't end the data with \r\n
        /// </summary>
        /// <param name="op"></param>
        public string SendOpCodeAndGetResponse( string op)
        {
            _port.DiscardInBuffer();
            _port.Write( op);
            _log.DebugFormat( "Sending command: {0}", op);
            return ReadTo( ">");
        }

        public void Connect( string portname)
        {
            try {
                _port = new SerialPort( portname, 115200, Parity.None, 8, StopBits.One);
                _port.ReadTimeout = 10000;                
                _port.Open();
                DisableImagePush();
                Connected = true;
            } catch( Exception ex) {
                _log.InfoFormat( "Could not connect to barcode reader on port {0}: {1}", _port.PortName, ex.Message);
                Connected = false;
            }
        }

        public int ShutterSpeed
        {
            get {
                return ParseConfigurationSetting( SendCommandAndGetResponse( "<K541?>"), "K541", 2)[0].ToInt();
            }
            set {
                int gain = Gain;
                SendCommandNoResponse( String.Format( "<K541,{0},{1}>", value, gain));
            }
        }

        public int Gain
        {
            get { 
                return ParseConfigurationSetting( SendCommandAndGetResponse( "<K541?>"), "K541", 2)[1].ToInt();
            }
            set {
                int shutter_speed = ShutterSpeed;
                SendCommandNoResponse( String.Format( "<K541,{0},{1}>", shutter_speed, value));
            }
        }

        public double FocalDistanceInches
        {
            get {
                return ParseConfigurationSetting( SendCommandAndGetResponse( "<K525?>"), "K525", 1)[0].ToInt() / 100.0;
            }
            set {
                SendCommandNoResponse( String.Format( "<K525,{0:000}>", value * 100));
            }
        }

        public int SubSampling
        {
            get {
                return ParseConfigurationSetting( SendCommandAndGetResponse( "<K542?>"), "K542", 1)[0].ToInt();
            }
            set {
                SendCommandNoResponse( String.Format( "<K542,{0}>", value));
            }
        }

        public WindowOfInterest WOI
        {
            get {
                string[] values = ParseConfigurationSetting( SendCommandAndGetResponse( "<K516?>"), "K516", 4);
                return new WindowOfInterest( values[0].ToInt(), values[1].ToInt(), values[2].ToInt(), values[3].ToInt());
            }
            set {
                SendCommandNoResponse( String.Format( "<K516,{0},{1},{2},{3}>", value.RowPointer, value.ColumnPointer, value.RowDepth, value.ColumnWidth));
            }
        }

        public int NarrowMargins
        {
            get {
                return ParseConfigurationSetting( SendCommandAndGetResponse( "<K450?>"), "K450", 2)[0].ToInt();
            }
            set {
                SendCommandNoResponse( String.Format( "<K450,{0},0>", value));
            }
        }

        public int BackgroundColor
        {
            get {
                return ParseConfigurationSetting( SendCommandAndGetResponse( "<K451?>"), "K451", 1)[0].ToInt();
            }
            set {
                SendCommandNoResponse( String.Format( "<K451,{0}>", value));
            }
        }

        /// <summary>
        /// sends <K?> to the reader to get all of the current settings
        /// </summary>
        public DecodeSettings ReadCurrentSettings()
        {
            _log.Debug( "Reading current reader settings");
            string all_settings = SendCommandAndGetResponse( "<K?>");
            string[] values = ParseConfigurationSetting( all_settings, "K541", 2);
            // DKM 2011-05-17 I found a bug in the K? command, where issuing it makes the Mini-3 ignore the following command
            // if that command actually sets data in the reader.  So immediately after sending K?, send K541?
            string dummy = SendCommandAndGetResponse( "<K541?>");

            ReaderDecodeSettings = new DecodeSettings();
            ReaderDecodeSettings.ShutterSpeed = values[0].ToInt();
            ReaderDecodeSettings.Gain = values[1].ToInt();
            ReaderDecodeSettings.FocalDistanceInches = ParseConfigurationSetting( all_settings, "K525", 1)[0].ToInt() / 100.0;
            ReaderDecodeSettings.SubSampling = ParseConfigurationSetting( all_settings, "K542", 1)[0].ToInt();
            // WOI
            values = ParseConfigurationSetting( all_settings, "K516", 4);
            ReaderDecodeSettings.WOI = new WindowOfInterest ( values[0].ToInt(), values[1].ToInt(), values[2].ToInt(), values[3].ToInt() );
            ReaderDecodeSettings.NarrowMargins = ParseConfigurationSetting( all_settings, "K450", 2)[0].ToInt();
            ReaderDecodeSettings.BackgroundColor = ParseConfigurationSetting( all_settings, "K451", 1)[0].ToInt();

            return ReaderDecodeSettings;
        }

        public void SetAsCurrentSettings( DecodeSettings settings)
        {
            _log.Debug( "Setting current reader settings");
            SendCommandNoResponse( String.Format( "<K541,{0},{1}>", settings.ShutterSpeed, settings.Gain));
            SendCommandNoResponse( String.Format( "<K525,{0:000}>", settings.FocalDistanceInches * 100));
            SendCommandNoResponse( String.Format( "<K542,{0}>", settings.SubSampling));
            SendCommandNoResponse( String.Format( "<K516,{0},{1},{2},{3}>", settings.WOI.RowPointer, settings.WOI.ColumnPointer, settings.WOI.RowDepth, settings.WOI.ColumnWidth));
            SendCommandNoResponse( String.Format( "<K450,{0},0>", settings.NarrowMargins));
            SendCommandNoResponse( String.Format( "<K451,{0}>", settings.BackgroundColor));
        }

        public static string[] ParseConfigurationSetting( string all_settings, string k_code, int number_of_parameters)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append( String.Format( @"<{0}", k_code));
            for( int i=0; i<number_of_parameters; i++) {
                sb.Append( @",(\d+)");
            }
            Regex r = new Regex( sb.ToString());
            MatchCollection matches = r.Matches( all_settings);
            if( matches.Count != 1 || matches[0].Groups.Count != number_of_parameters + 1)
                throw new Exception( "Barcode reader configuration setting parse error");
            string[] settings = new string[number_of_parameters];
            for( int i=0; i<number_of_parameters; i++) {
                settings[i] = matches[0].Groups[i + 1].ToString();
            }
            return settings;
        }

        /// <summary>
        /// downloads the requested configuration index from the reader and returns the settings in a structure
        /// </summary>
        /// <param name="index_0_based"></param>
        /// <returns></returns>
        public DecodeSettings GetConfigurationIndexSettings( int index_0_based)
        {
            _log.DebugFormat( "Retrieving settings for configuration database index {0}", index_0_based);

            // the mini-3 protocol is 1-based
            // DKM 2011-05-16 I couldn't figure out why sending K255? worked, but sending K255?,1 didn't.  When I send
            // K255?,1 from the library, _port.ReadTo() times out.  However, Docklight can send the string and get
            // a response w/o problems.  I had to work around this issue for now by always requesting every configuration
            // index with K255?, and then parsing out the index I'm interested in.
            string response = SendCommandAndGetResponse( "<K255?>");
            string[] values = MicroscanReader.ParseConfigurationSetting( response, String.Format( "K255,{0}", index_0_based + 1), 14);
            DecodeSettings settings = new DecodeSettings();
            settings.ShutterSpeed = values[0].ToInt();
            settings.Gain = values[1].ToInt();
            settings.FocalDistanceInches = values[2].ToInt() / 100.0;
            settings.SubSampling = values[3].ToInt();
            settings.WOI.RowPointer = values[4].ToInt();
            settings.WOI.ColumnPointer = values[5].ToInt();
            settings.WOI.RowDepth = values[6].ToInt();
            settings.WOI.ColumnWidth = values[7].ToInt();
            settings.ThresholdMode = values[8].ToInt();
            settings.FixedThresholdValue = values[9].ToInt();
            settings.ProcessingMode = values[10].ToInt();
            settings.NarrowMargins = values[11].ToInt();
            settings.BackgroundColor = values[12].ToInt();
            settings.Symbologies = values[13].ToInt();            
            return settings;
        }

        public void LoadConfigurationIndex( int index_0_based, bool verify_after_load=true)
        {
            if( index_0_based == _last_index)
                return;
            _last_index = index_0_based;

            string command = String.Format( "<K255-,{0}>", index_0_based + 1);
            _log.DebugFormat( "Loading reader configuration index {0} ({1})", index_0_based, command);
            SendCommandNoResponse( command);

            if( verify_after_load) {
                const int max_retries = 5;
                int num_retries = 0;
                while( !VerifyConfigurationIndex( index_0_based) && num_retries++ < max_retries) {
                    _log.DebugFormat( "Failed barcode configuration verification attempt #{0}", num_retries);
                }
                if( num_retries >= max_retries)
                    throw new Exception( String.Format( "Exceeded maximum number of retries while loading barcode configuration index {0}", index_0_based + 1));
                _log.DebugFormat( "Successfully changed to barcode configuration index {0}", index_0_based);
            }
        }

        /// <summary>
        /// Ensures that the configuration that was loaded matches the current settings in the reader
        /// </summary>
        /// <param name="index_0_based"></param>
        /// <returns></returns>
        private bool VerifyConfigurationIndex( int index_0_based)
        {
            // get the current reader settings
            DecodeSettings current = ReadCurrentSettings();
            // compare them with the database settings
            DecodeSettings config = GetConfigurationIndexSettings( index_0_based);
            return current.Equals( config);
        }

        public void SaveConfigurationIndexSettings( int index_0_based, DecodeSettings settings, bool send_and_save_after=true)
        {
            _log.DebugFormat( "Saving settings for reader configuration index {0}", index_0_based);

            // save the configuration to the reader memory
            string command = String.Format( "<K255,{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14}>",
                                            index_0_based + 1, settings.ShutterSpeed, settings.Gain,
                                            String.Format( "{0:000}", settings.FocalDistanceInches * 100),
                                            settings.SubSampling, settings.WOI.RowPointer, settings.WOI.ColumnPointer,
                                            settings.WOI.RowDepth, settings.WOI.ColumnWidth, settings.ThresholdMode,
                                            settings.FixedThresholdValue, settings.ProcessingMode, settings.NarrowMargins,
                                            settings.BackgroundColor, settings.Symbologies);
            SendCommandNoResponse( command);
            if( send_and_save_after)
                SendAndSave();
        }

        public void SendNoSave()
        {
            SendCommandNoResponse( "<A>");
        }        

        public void SendAndSave()
        {
            SendCommandNoResponse( "<Z>");
        }

        public void SendAndSaveAsCustomerDefaults()
        {
            SendCommandNoResponse( "<Zc>");
        }

        /// <summary>
        /// Closes the serial port used by the reader
        /// </summary>
        /// <remarks>I need to decide whether or not it makes sense to have this class implement IDisposable</remarks>
        public void Close()
        {
            _port.Close();
            Connected = false;
        }

        public String Read()
        {
            // send the read command
            string command = MicroscanCommands.Read();
            _port.DiscardInBuffer();
            _port.Write( command);
            // get the response
            return ReadTo( "\r\n"); // leaves the rest of the stuff in the buffer
        }

        public bool RS422Status
        {
            get;
            set;
        }

        public void Calibrate( Boolean enable_gain, MicroscanCommands.ShutterSpeed shutter_speed, 
                               MicroscanCommands.FocusPosition focus_position, Boolean enable_symbol_type,
                               MicroscanCommands.WOIFraming woi_framing, Int16 woi_margin,
                               MicroscanCommands.Processing processing)
        {
            string command = MicroscanCommands.Calibrate( enable_gain, shutter_speed, focus_position,
                                                          enable_symbol_type, woi_framing, woi_margin,
                                                          processing);
            _port.Write( command);
        }

        private void EnableImagePush( MicroscanCommands.ImageFormat image_format, Byte image_quality)
        {
            // enable image saving
            String command = MicroscanCommands.EnableImagePush( image_format, image_quality);
            _port.Write( command);
        }

        public String SaveImage( String filename, MicroscanCommands.ImageFormat image_format, Byte image_quality)
        {
            EnableImagePush( image_format, image_quality);
            // read the barcode
            string barcode = Read();

            DateTime start = DateTime.Now;
            // create a buffer for the maximum file size for now
            const int max_filesize = 3145728;
            byte[] buffer = new byte[max_filesize];
            // keep reading bytes on the serial port until nothing else comes in
            // set the timeout to 500ms, which I think is reasonable for the data transfer to be complete
            _port.ReadTimeout = 500;
            int total_bytes_read = 0;
            int bytes_to_read;

            // wait for image data to arrive, or a timeout to occur
            DateTime timeout_start = DateTime.Now;
            const double timeout_s = 30;
            while( _port.BytesToRead == 0 && (DateTime.Now - timeout_start).TotalSeconds <= timeout_s)
                Thread.Sleep( 100);
            if( (DateTime.Now - timeout_start).TotalSeconds > timeout_s)
                throw new Exception( "Timed out while waiting for barcode reader to send image data");

            do {
                bytes_to_read = _port.BytesToRead;
                int bytes_actually_read = _port.Read( buffer, total_bytes_read, bytes_to_read);
                total_bytes_read += bytes_to_read;
                Thread.Sleep( 250); // give time to receive more bytes
            } while( bytes_to_read > 0);
            //! \todo figure out what the next 13 bytes are -- I have no idea!  to be safest,
            //!       strip off everything before 0x42 0x4D for BMP, and 0xFF 0xD8 for JPEG
            try {
                WriteBufferToImage( buffer, total_bytes_read, filename, image_format);
                LastImageTime = DateTime.Now - start;
                Debug.WriteLine( String.Format( "Image transfer and saving took {0:0.00}s", LastImageTime.TotalSeconds));
            } catch( BioNex.Shared.Utils.ExtensionMethods.HeaderNotInByteArrayException) {
                throw new SaveImageException( filename);
            } catch( IOException ex) {
                // for now, do the same as other exception, but this is the case where the file is
                // "currently in use by another process".  I close the writer and stream, so not
                // sure why this error occurs.
                _log.Debug( ex.Message);
                throw new SaveImageException( filename);
            } catch( Exception ex) {
                _log.Debug( ex.Message);
                throw;
            } finally {
                // disable image saving
                DisableImagePush();
            }
            // send the barcode back
            return barcode;
        }

        private void DisableImagePush()
        {
            string command = MicroscanCommands.DisableImagePush();
            _port.Write( command);
        }

        private static void WriteBufferToImage( byte[] buffer, int filesize, string filename, MicroscanCommands.ImageFormat image_format)
        {
            // DKM 2011-05-26 I used to have a comment about this here somewhere.... but it basically said
            // that there's something weird about the data coming from the reader, and I needed to strip
            // of a couple of bytes so that the files are formatted properly.
            if( image_format == MicroscanCommands.ImageFormat.Bitmap)
                buffer = buffer.RemoveFromByteArrayUntil( new Byte[] { 0x42, 0x4D });
            else
                buffer = buffer.RemoveFromByteArrayUntil( new Byte[] { 0xFF, 0xD8 });
            if( filesize <= buffer.Length)
                buffer = buffer.StripBytesAfter( filesize);
            using( FileStream stream = new FileStream( filename, FileMode.Create)) {
                using( BinaryWriter writer = new BinaryWriter( stream)) {
                    writer.Write( buffer, 0, buffer.Count());
                    writer.Close();
                    stream.Close();
                }
            }
        }

        /// <summary>
        /// This method launches a thread that monitors the serial input buffer for incoming
        /// barcodes.  It flushes the input buffer before starting, and the thread automatically
        /// exits when the last barcode comes in.  Callback gets called with the data.
        /// </summary>
        /// <param name="number_of_reads"></param>
        /// <param name="callback"></param>
        public void FlyByRead( int number_of_reads, AsyncCallback callback)
        {
            _port.DiscardInBuffer();
            // start the thread that will read data from the serial port
            ReadBarcodesDelegate read_thread = ReadBarcodes;
            // I don't think I need to do anything with the result... caller needs to handle callback somehow
            List<string> barcodes;
            read_thread.BeginInvoke( number_of_reads, out barcodes, callback, null);
        }
 
        private void FlyByResultsCallback( IAsyncResult ar)
        {
            AsyncResult result = ar as AsyncResult;
            ReadBarcodesDelegate caller = (ReadBarcodesDelegate) result.AsyncDelegate;
            Debug.Assert( result != null);
            try {
                List<string> barcodes;
                string result_message = caller.EndInvoke( out barcodes, ar);
                if( result_message != String.Empty) {
                    Console.WriteLine( "Failed to read barcodes: " + result_message);
                    return;
                }
                foreach( var barcode in barcodes)
                    Console.WriteLine( "barcode found: " + barcode);
            } catch( Exception) {
                throw;
            }
        }

        public delegate string ReadBarcodesDelegate( int number_of_reads, out List<string> barcodes);

        private string ReadBarcodes( int number_of_reads, out List<string> barcodes)
        {
            if( Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "Microscan fly-by reading";

            barcodes = new List<string>();
            _port.ReadTimeout = 30000; // made this a really long time to allow us to use a fake serial port and do the "dynamic setup" diagnostic test with ESP on the real port
            while( barcodes.Count < number_of_reads) {
                try {
                    string barcode = ReadTo( "\r\n");
                    // DKM 2011-11-22 I saw a case where a null character was sneaking into the serial stream from the BCR side
                    if( barcode.Contains( '\x00'))
                        _log.Debug( "found null character in barcode -- stripping out");
                    barcode = barcode.Trim( new char[] { '\x00' });
                    // DKM 2011-02-17 we'll change the low-level barcode from ## to EMPTY if necessary
                    if( barcode == "##")
                        barcode = BioNex.Shared.LibraryInterfaces.Constants.Empty;
                    barcodes.Add( barcode);
                } catch( TimeoutException ex) {
                    // we either timed out on one of the reads, or we have an inconsistency
                    // between the number of reads we were told to capture, and the number
                    // we told the robot to capture.
                    return ex.Message;
                }
                Thread.Sleep( 10);
            }
             
            _log.Debug( "Fly-by barcode results: " + String.Join( ", ", (from x in barcodes select x).ToArray()));
            return String.Empty;
        }

        public void StartDecodeRateTest()
        {
        }

        public void StartPercentRateTest()
        {
        }

        public void StopTest()
        {
        }

        public Double GetDecodeRate()
        {
            return 0;
        }

        public Double GetPercentageRate()
        {
            return 0;
        }
        
        public void DeleteAllImages()
        {
            string ignore = SendOpCodeAndGetResponse( "<op,9,del*.*>");
        }

        public List<MicroscanFilename> GetImageFilenames()
        {
            string files = SendOpCodeAndGetResponse( "<op,9,*.*>");
            // parse the response to get the individual file names
            return ParseImageFilenames( files);
        }

        internal static List<MicroscanFilename> ParseImageFilenames( string filenames)
        {
            // only care about those in the /saved/noread folder
            Regex regex = new Regex( @"/saved/noread/(\d+)/(\d+)x(\d+)_gs.bmp 0*(\d+) 0*(\d+)");
            MatchCollection matches = regex.Matches( filenames);
            // the number of matches equals the number of files in the /saved folder in the reader
            List<MicroscanFilename> files = new List<MicroscanFilename>();
            foreach( var x in matches)
                files.Add( new MicroscanFilename( x.ToString()));
            return files;

        }

        public string DownloadImage( MicroscanFilename file, double scaling, int quality)
        {
            _log.DebugFormat( "Loading image '{0}', scaling {1}, quality {2}", file, scaling, quality);
            string command = String.Format( "<uy,{0}>", file.ToJpegFilename( (int)(file.Width * scaling), (int)(file.Height * scaling), quality));
            byte[] image_data = _port.ReceiveFile( command, true);
            string temp_filename = System.IO.Path.GetTempPath() + String.Format( "{0}x{1}_q{2} ({3}) ({4}).jpg", file.Width, file.Height, quality, file.ImageId, file.Age);
            WriteBufferToImage( image_data, image_data.Length, temp_filename, MicroscanCommands.ImageFormat.JPEG);
            return temp_filename;
        }
    }
}
