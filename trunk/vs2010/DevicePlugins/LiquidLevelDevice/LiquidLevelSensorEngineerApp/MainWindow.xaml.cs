using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using BioNex.Shared.Utils;
using BioNex.Shared.DeviceInterfaces;
using BioNex.LiquidLevelDevice;
using GalaSoft.MvvmLight.Command;
using System.Threading;
using log4net;
using log4net.Core;
using log4net.Repository.Hierarchy;
using log4net.Appender;
using System.IO;
using BioNex.Shared.LibraryInterfaces;
using System.Runtime.InteropServices;

namespace LiquidLevelSensorEngineerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [Export(typeof(ExternalDataRequesterInterface))]
    public partial class MainWindow : Window, ExternalDataRequesterInterface
    {
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]
        private static extern uint TimeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod", SetLastError = true)]
        private static extern uint TimeEndPeriod(uint uMilliseconds);

        [Import]
        public DeviceInterface Plugin { get; set; }
        ILLSensorPlugin LLSPlugin { get { return (ILLSensorPlugin)Plugin; } }

        private MemoryAppender _main_memory_appender { get; set; }
        ILog _log = LogManager.GetLogger(typeof(MainWindow));

        [Import]
        ILabwareDatabase _labware_database { get; set; }

        [Export("LabwareDatabase.filename")]
        public string LabwareDBPath { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            TimeBeginPeriod(1);

            string exe_path = BioNex.Shared.Utils.FileSystem.GetAppPath();
            LabwareDBPath = exe_path + "\\labware.s3db";

            LoadPlugins();
            LoadProps();
            StartLog();

            Diags.Plugin = Plugin;
            Diags.EngineeringTabVisibility = Visibility.Visible;

            LLSPlugin.SavePropertiesEvent += LSESaveProperties;
        }

        void LoadPlugins()
        {
            var catalog = new DirectoryCatalog(".");
            var container = new CompositionContainer(catalog);
            try
            {
                container.ComposeParts(this);
            }
            catch (CompositionException ex)
            {
                foreach (CompositionError e in ex.Errors)
                    MessageBox.Show(e.Description + ": " + e.Exception.Message);
                throw;
            }
        }

        void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {            
            StopLog();
            Plugin.Close();

            TimeEndPeriod(1);
        }

        void LoadProps()
        {
            var db = new BioNex.SynapsisPrototype.DeviceManagerDatabase();
            var dict = db.GetProperties("BioNex", "BeeSure", "Liquid Level Sensor");
            var device_info = new DeviceManagerDatabase.DeviceInfo("BioNex", "BeeSure", "Liquid Level Sensor", false, dict);
            Plugin.SetProperties(device_info);
        }
        
        void LSESaveProperties(object sender, IDictionary<string, string> properties)
        {
            var db = new BioNex.SynapsisPrototype.DeviceManagerDatabase();
            db.UpdateDevice("BioNex", "BeeSure", "Liquid Level Sensor", properties);

            // reload the properties to make sure the model has the correct values
            LoadProps();
        }

        Thread _logging_thread;
        bool _stop_logging = false;
        void StartLog()
        {
            try
            {
                StopLog();
                _stop_logging = false;
                log4net.Config.XmlConfigurator.Configure(new FileInfo(BioNex.Shared.Utils.FileSystem.GetAppPath() + "\\logging.xml"));
                Hierarchy h = LogManager.GetRepository() as Hierarchy;
                IAppender[] appenders = h.GetAppenders();
                _main_memory_appender = appenders.Where(appender => appender.Name == "Memory").First() as MemoryAppender;

                var file_appender = appenders.Where(appender => appender.Name == "File").First() as FileAppender;
                file_appender.File = string.Format(BioNex.Shared.Utils.FileSystem.GetAppPath() + "\\logs\\liquid_level_sensor_{0}.txt", DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss"));
                file_appender.AppendToFile = true;
                file_appender.ActivateOptions();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                MessageBox.Show(e.ToString());
                Application.Current.Shutdown();
            }
            
            _logging_thread = new Thread(() =>
            {
                var message = "";
                var action = new Action(() =>
                {
                    // magic to keep the textbox scrolling to the newest append to the textbox unless the user has moved the scrollbox up
                    double vpos = TextLog.VerticalOffset + TextLog.ViewportHeight;
                    double maxpos = TextLog.ExtentHeight;
                    bool bottomFlag = false;
                    if (vpos >= maxpos - 1)
                        bottomFlag = true;

                    const int MAX_LINES = 100000;
                    // cut lines off the top of the message
                    if (TextLog.LineCount > MAX_LINES)
                    {
                        while (TextLog.LineCount > (MAX_LINES / 2))
                        {
                            var text = TextLog.Text;
                            text = text.Substring(text.Length / 2);
                            text = text.Substring(text.IndexOf('\n') + 1);
                            TextLog.Text = text;
                        }
                    }

                    // append log message
                    TextLog.AppendText(message);
                    message = "";

                    if (bottomFlag)
                    {
                        TextLog.CaretIndex = TextLog.Text.Length;
                        TextLog.ScrollToEnd();
                    }
 
                });

                while (!_stop_logging)
                {
                    try
                    {
                        // grab stuff out of the log buffer and add to the textbox
                        LoggingEvent[] events = _main_memory_appender.GetEvents();
                        if (events != null && events.Length > 0)
                        {
                            _main_memory_appender.Clear();
                            message = "";
                            foreach (LoggingEvent log_event in events)
                            {
                                if (log_event == null)
                                    continue;
                                var t = log_event.TimeStamp.ToString();
                                var l = log_event.Level.ToString();
                                var m = log_event.MessageObject.ToString() + " " + (log_event.ExceptionObject != null ? log_event.ExceptionObject.ToString() : "");
                                message += string.Format("{0} : {1} : {2}\n", t, l, m);
                            }
                               
                            // log to window
                            if (Dispatcher.CheckAccess()) action(); else Dispatcher.Invoke(action);
                        }
                        else
                        {
                            GC.Collect();
                            Thread.Sleep(500); // lame polling since there's no event to wait for w/ log4net
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            });
            _logging_thread.Start();

/*
            var log_test_thread = new Thread(() =>
            {
                int i = 0;
                while (!_stop_logging)
                {
                    _log.InfoFormat("test line: {0}", ++i);
                    if( i % 1000 == 0)
                        System.Threading.Thread.Sleep(200);
                }
            });
            log_test_thread.Start();
*/
        }

        void StopLog()
        {
            _stop_logging = true;
            if (_logging_thread == null)
                return;
            _logging_thread.Join(100);
            _logging_thread = null;
        }

       #region ExternalDataRequesterInterface

       SystemStartupCheckInterface.SafeToMoveDelegate ExternalDataRequesterInterface.SafeToMove
       {
           get
           {
               throw new NotImplementedException();
           }
           set
           {
               throw new NotImplementedException();
           }
       }

       int ExternalDataRequesterInterface.GetDeviceTypeId(string company_name, string product_name)
       {
           throw new NotImplementedException();
       }

       IEnumerable<DeviceInterface> ExternalDataRequesterInterface.GetDeviceInterfaces()
       {
           throw new NotImplementedException();
       }

       IEnumerable<AccessibleDeviceInterface> ExternalDataRequesterInterface.GetAccessibleDeviceInterfaces()
       {
           throw new NotImplementedException();
       }

       IEnumerable<RobotInterface> ExternalDataRequesterInterface.GetRobotInterfaces()
       {
           throw new NotImplementedException();
       }

       IEnumerable<PlateStorageInterface> ExternalDataRequesterInterface.GetPlateStorageInterfaces()
       {
           throw new NotImplementedException();
       }

       IEnumerable<IOInterface> ExternalDataRequesterInterface.GetIOInterfaces()
       {
           throw new NotImplementedException();
       }

       IEnumerable<SystemStartupCheckInterface> ExternalDataRequesterInterface.GetSystemStartupCheckInterfaces()
       {
           throw new NotImplementedException();
       }

       IEnumerable<SafetyInterface> ExternalDataRequesterInterface.GetSafetyInterfaces()
       {
           throw new NotImplementedException();
       }

       IEnumerable<StackerInterface> ExternalDataRequesterInterface.GetStackerInterfaces()
       {
           throw new NotImplementedException();
       }

       IEnumerable<DockablePlateStorageInterface> ExternalDataRequesterInterface.GetDockablePlateStorageInterfaces()
       {
           throw new NotImplementedException();
       }

       Func<bool> ExternalDataRequesterInterface.ReadSensorCallback(string device_name, string location_name)
       {
           throw new NotImplementedException();
       }
       #endregion
    }
}
