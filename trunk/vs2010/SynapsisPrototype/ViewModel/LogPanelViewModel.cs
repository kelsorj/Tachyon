using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;

namespace BioNex.SynapsisPrototype.ViewModel
{
    /// <summary>
    /// The LogPanelViewModel is in charge of getting all of the data out of
    /// the memory appenders (main and pipette), and then pushing the data
    /// into all of its respective logs.  I guess it will also need to look
    /// at the database loggers as well, to support the query-based logs.
    /// </summary>
    [Export(typeof(LogPanelViewModel))]
    public class LogPanelViewModel : ViewModelBase, IDisposable
    {
        private MemoryAppender _main_memory_appender { get; set; }
        private MemoryAppender _pipette_memory_appender { get; set; }

        public ObservableCollection<MainLogData> MainLog { get; set; }
        public ObservableCollection<PipetteLogData> PipetteLog { get; set; }

        private DispatcherTimer _timer { get; set; }

        /// <summary>
        /// the maximum number of rows we want in the log when we rollover
        /// </summary>
        private const int MaxLogRows = 1000;

        /// <summary>
        /// the number of log entries PAST the max, where we remove old log entries
        /// </summary>
        private const int LogRowsOverflow = 500;

        // classes for holding log data
        public struct MainLogData
        {
            public string Timestamp { get; set; }
            public string Level { get; set; }
            public string Message { get; set; }
        }

        public struct PipetteLogData
        {
            public string Timestamp { get; set; }
            public string Level { get; set; }
            public string Message { get; set; }
            public string DeviceName { get; set; }
            public string ChannelID { get; set; }
            public string SourceBarcode { get; set; }
            public string SourceWell { get; set; }
            public string DestinationBarcode { get; set; }
            public string DestinationWell { get; set; }
            public string Volume { get; set; }
        }

        public LogPanelViewModel()
        {
            // get reference to appenders that we need to keep track of
            Hierarchy h = LogManager.GetRepository() as Hierarchy;
            IAppender[] appenders = h.GetAppenders();
            _main_memory_appender = appenders.Where( appender => appender.Name == "Memory").First() as MemoryAppender;
            _pipette_memory_appender = appenders.Where( appender => appender.Name == "PipetteLogMemory").First() as MemoryAppender;

            MainLog = new ObservableCollection<MainLogData>();
            PipetteLog = new ObservableCollection<PipetteLogData>();

            _timer = new DispatcherTimer();
            _timer.Interval = new TimeSpan( 0, 0, 0, 0, 100);
            _timer.Tick += UpdateLogsCallback;
            _timer.Start();
        }

        /// <remarks>
        /// At first, I started by putting all memory appenders into a list so I could loop
        /// over them, but I soon realized that this wouldn't work because we need to put
        /// each appender's data into its own ObservableCollection for databinding to the
        /// proper datagrid.
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateLogsCallback( object sender, EventArgs e)
        {
            // handle main log first
            try {
                // grab stuff out of the log buffer and add to the datagrid
                LoggingEvent[] events = _main_memory_appender.GetEvents();
                if( events != null && events.Length > 0) {
                    _main_memory_appender.Clear();
                    foreach( LoggingEvent log_event in events) {
                        if( log_event == null)
                            continue;
                        string t = log_event.TimeStamp.ToString();
                        string l = log_event.Level.ToString();
                        string m = log_event.MessageObject.ToString();
                        MainLog.Add( new MainLogData { Timestamp=t, Level=l, Message=m });
                    }
                    // check the log for overflow
                    if( MainLog.Count() > (MaxLogRows + LogRowsOverflow)) {
                        int difference = MainLog.Count() - MaxLogRows;
                        for( int i=0; i<difference; i++)
                            MainLog.RemoveAt( 0);
                    }
                }
            } catch( ArgumentException ex) {
                Console.WriteLine( ex.Message);
            }

            // disabled pipette memory logging for now, because the pipette memory appender ref is
            // null -- I tried to grab it from root, which is wrong -- I need to get the pipette
            // logger object and pull the appender from that instead!
            // now handle
            try {
                // grab stuff out of the log buffer and add to the datagrid
                LoggingEvent[] events = _pipette_memory_appender.GetEvents();
                if( events != null && events.Length > 0) {
                    _pipette_memory_appender.Clear();
                    foreach( LoggingEvent log_event in events) {
                        string t = log_event.TimeStamp.ToString();
                        string l = log_event.Level.ToString();
                        string m = log_event.MessageObject.ToString();
                        string d = log_event.Properties["DeviceName"].ToString();
                        string c = log_event.Properties["ChannelID"].ToString();
                        string sbc = log_event.Properties["SourceBarcode"].ToString();
                        string sw = log_event.Properties["SourceWell"].ToString();
                        string dbc = log_event.Properties["DestinationBarcode"].ToString();
                        string dw = log_event.Properties["DestinationWell"].ToString();
                        string v = log_event.Properties["Volume"].ToString();
                        PipetteLog.Add( new PipetteLogData { 
                                            Timestamp=t,
                                            Level=l,
                                            Message=m,
                                            DeviceName=d,
                                            ChannelID=c,
                                            SourceBarcode=sbc,
                                            SourceWell=sw,
                                            DestinationBarcode=dbc,
                                            DestinationWell=dw,
                                            Volume=v
                                    });
                    }
                    // check the log for overflow
                    if( PipetteLog.Count() > (MaxLogRows + LogRowsOverflow)) {
                        int difference = PipetteLog.Count() - MaxLogRows;
                        for( int i=0; i<difference; i++)
                            PipetteLog.RemoveAt( 0);
                    }
                }
            } catch( ArgumentException ex) {
                Console.WriteLine( ex.Message);
            }
        }

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            _timer.Stop();
        }

        #endregion

        public class RowBackgroundConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                MainLogData data = (MainLogData)value;
                if (data.Level == "ERROR")
                    return new SolidColorBrush(Colors.Pink);
                if (data.Level == "FATAL")
                    return new SolidColorBrush(Colors.Tomato);
                if (data.Level == "WARN")
                    return new SolidColorBrush(Colors.Khaki);
                if (data.Level == "DEBUG")
                    return new SolidColorBrush(Colors.PaleGreen);
                return Colors.White;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}
