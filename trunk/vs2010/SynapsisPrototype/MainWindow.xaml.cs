using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.ErrorHandling;
using BioNex.Shared.IError;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Utils;
using BioNex.SynapsisPrototype.ViewModel;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using log4net;

namespace BioNex.SynapsisPrototype
{
    /// <summary>
    /// This application's main window.
    /// </summary>
    [Export(typeof(IError))]
    public partial class MainWindow : Window, IError
    {
        private static readonly ILog _log = LogManager.GetLogger( typeof( MainWindow));

        [Import("SynapsisViewModel")]
        public SynapsisViewModel Main { get; set; }
        [Import(typeof(PreferencesDialog))]
        private PreferencesDialog _preferences;
        [Export]
        public LoadPluginsErrorInterface PluginLoadingErrorManager { get; set; }
        [Export("DeviceManager.filename")]
        public string DeviceManagerFilename { get; set; }
        [Export("LabwareDatabase.filename")]
        public string LabwareDatabaseFilename { get; set; }
        [ Export( "LiquidProfileLibrary.filename")]
        public string LiquidProfileLibraryFilename { get; set; }
        [Export("MEFContainer")]
        public CompositionContainer Container;
        [Import(typeof(LogPanelViewModel))]
        public LogPanelViewModel LogPanelViewModel { get; set; }
        [ImportMany(typeof(IErrorNotification))]
        private List<IErrorNotification> _notifiers { get; set; }

        [Export("MainDispatcher")]
        public Dispatcher MainDispatcher
        {
            get { return Dispatcher; }
        }
        [ImportMany]
        internal IEnumerable<Lazy<ICustomerGUI>> _customer_guis { get; set; }

        // windows message which is used by UPS driver to signal to us to gracefully shut down
        static string PwrLossDetect_msgstr = "SynapsisUtilityPowerLossDetected";
        static uint PwrLossDetect_msg_id;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint RegisterWindowMessage(string lpString);

        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]
        private static extern uint TimeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint="timeEndPeriod", SetLastError=true)]
        private static extern uint TimeEndPeriod(uint uMilliseconds);

        private readonly List<ErrorData> _errors;
        private readonly DispatcherTimer _timer;

        // garbage collector will release mutex when application shuts down.
        private Mutex synapsis_application_mutex_;

        // used to prevent app from sending too many messages with the same data
        private int LastErrorCount { get; set; }

        private ManualResetEvent StopTimerEvent { get; set; }

        private readonly BioNexSplash.BioNexSplashControl _splash;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            // DKM 2012-02-16 in the event of an unhandled exception, dump the stack trace to the log
            AppDomain.CurrentDomain.UnhandledException += (sender,e) => { _log.DebugFormat( "Unhandled exception thrown by '{0}'.  Stack trace: {1}", sender.ToString(), e.ExceptionObject.ToString()); };

            try {
                PluginLoadingErrorManager = new LoadPluginsErrorInterface();
                _splash = new BioNexSplash.BioNexSplashControl();
                _splash.Show();

                bool created_new;
                synapsis_application_mutex_ = new Mutex( true, "SynapsisApplicationMutex", out created_new);
                if( !created_new){
                    MessageBox.Show( "Another instance of Synapsis is already running.", "Synapsis");
                    Application.Current.Shutdown();
                }

                TimeBeginPeriod(1); // set timer resolution to 1ms to get max performace from Sleep and USB->CANbus calls
                InitializeComponent();
                _errors = new List<ErrorData>();

                // set up logging
                string exe_path = BioNex.Shared.Utils.FileSystem.GetAppPath();
                BioNex.Shared.Utils.Logging.SetConfigurationFilePath( exe_path + "\\logging.xml");
                BioNex.Shared.Utils.Logging.SetLogFilePath( exe_path + "\\logs", "synapsis", true);

                DeviceManagerFilename = exe_path + "\\config\\devices.s3db";
                LabwareDatabaseFilename = exe_path + "\\config\\labware.s3db";
                LiquidProfileLibraryFilename = exe_path + "\\config\\liquids.s3db";

                StopTimerEvent = new ManualResetEvent( false);

                _timer = new DispatcherTimer();
                _timer.Tick += new EventHandler(_timer_Tick);
                _timer.Interval = new TimeSpan( 0, 0, 0, 0, 250);

                // DKM 2011-12-19 Windows 7 doesn't allow Mark's power loss method, so I am trying out the CodePack API
                // DKM 2012-04-06 refs #559 can't take this out anymore -- we need it for Windows XP!
                // Register windows message which is used by UPS driver to signal to us to gracefully shut down
                if( System.Environment.OSVersion.Version.Major < 6) {
                    PwrLossDetect_msg_id = RegisterWindowMessage(PwrLossDetect_msgstr);
                    if (0 == PwrLossDetect_msg_id)
                    {
                        Console.WriteLine("Registering '{0}' failed with: {1}. Will not be able to gracefully shutdown in event of utility power loss", PwrLossDetect_msgstr, Marshal.GetLastWin32Error().ToString());
                    }
                }
            } catch( Exception ex) {
                MessageBox.Show( "Error creating MainWindow: " + ex.Message);
            }
        }

        ~MainWindow() // Destructor doesn't get called until after all threads have died, so don't use this to shutdown GUI.  Use Window_Closed instead
        {
            TimeEndPeriod(1); // reset timer resolution back to default from 1ms to be nice to Windows
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Handle messages from UPS driver program: SendPowerLossMessage.exe...
            if (msg == PwrLossDetect_msg_id)
            {
                _log.FatalFormat( "Received notice of AC Utility Power Loss. Getting ready to shut down Synapsis gracefully.");
            }

            return IntPtr.Zero;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _splash.Activate();

            // DKM 2011-12-20 can't remember why I did this... why not specify the icon in the app properties?
            try {
                Uri iconUri = new Uri("pack://application:,,,/Images/BioNex.ico", UriKind.RelativeOrAbsolute);
                Icon = BitmapFrame.Create(iconUri);
            } catch( Exception ex) {
                _log.Error( ex.Message);
            }
            _log.Info( "Window loaded");

            try {
                LoadPlugins();
            } catch( Exception ex) {
                _log.Error( ex.Message);
                MessageBox.Show( "Error when loading plugins: " + ex.Message);
            }

            try {
                // load customer gui
                if( _customer_guis != null && _customer_guis.Count() != 0) {
                    // pass the names of the available GUIs so the user can select a different one, if desired
                    Main._customer_gui_names = (from x in _customer_guis select x.Value.GUIName).ToList();
                    // which one was selected last?
                    string selected_customer_gui = _preferences.SelectedCustomerGui;
                    // is that gui present?
                    var gui = _customer_guis.FirstOrDefault( x => x.Value.GUIName == selected_customer_gui);
                    // if gui isn't present, then log it and select the first one available
                    if( gui == null) {
                        gui = _customer_guis.First();
                        _log.InfoFormat( "The customer GUI plugin named '{0}' was not loaded.  Loading the first available customer plugin '{1}'.", selected_customer_gui, gui.Value.GUIName);
                        _preferences.SelectedCustomerGui = gui.Value.GUIName;
                        _preferences.SavePreferences();
                    }
                    // set the active GUI
                    CustomerGUITab.Header = gui.Value.GUIName;
                    CustomerGUITab.Content = gui.Value;
                    Main.CustomerGUI = gui.Value;

                    // set protocol execution button visibility based on selected GUI's preference
                    bool show_protocol_buttons = Main.CustomerGUI.ShowProtocolExecuteButtons();
                    // DKM 2011-06-08 we have determined that the main protocol start/pause/resume/abort buttons is
                    //                an all-or-nothing affair.  It is just too hard to make multiple GUI behavior
                    //                state machines cooperate.
                    StartButton.Visibility = PauseResumeButton.Visibility = AbortButton.Visibility = show_protocol_buttons ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

                    // CLOSE the remaining GUI's, since they've been initialized and have started running heartbeats, opened sockets, etc at this point.
                    foreach (var ui in _customer_guis)
                        if (ui != gui)
                            try
                            {
                                ui.Value.Close();
                            }
                            catch (NotImplementedException)
                            { }

                    // Tell our main CustomerGUI that composition has finished, so [Imports] are all available
                    Main.CustomerGUI.CompositionComplete();

                } else {
                    _log.Error( "Could not load customer GUI plugin");
                }
            } catch( Exception ex) {
                MessageBox.Show( "Error when loading customer GUI: " + ex, "Synapsis : Composition Error");
            }
            // not sure if this is the right way to do it, but since I can't set the
            // DataContext for the LogPanel during construction (MEF hasn't loaded
            // the parts yet), I am going to call a method in the LogPanel here to set it.
            logpanel.DataContext = LogPanelViewModel;

            if (Main == null)
            {
                MessageBox.Show("Plugin load failed before Synsapsis View Model was imported, Synapsis initialization cannot continue.", "Synapsis : Missing UI Component");
                DataContext = null;
            }
            else
            {
                Main.ProtocolCompleteEvent += new EventHandler(Main_ProtocolCompleteEvent);
                _timer.Start();
                DataContext = Main;
            }

            _splash.CloseAfter(this, 3000);

//            TestErrors();

        }

        void Main_ProtocolCompleteEvent(object sender, EventArgs e)
        {
            var pce = e as SynapsisViewModel.ProtocolCompleteEventArgs;
            if (pce != null && !pce.ShowMessageBox)
                return;
            string message = "Done";
            if( pce != null) 
                message = pce.Message;

            // DKM 2010-12-28 we're okay with #228 comment below because CustomerProtocolComplete is guaranteed
            //                to occur only after the event gets raised
            // #228 this MUST occur after ProtocolComplete, or interlocks will bite us
            // I want this to be modal -- might have to make my own modal messagebox since WPF doesn't support this natively
            MainDispatcher.Invoke( new Action( () => { MessageBox.Show( this, message, "Protocol complete!"); }));
        }

        public void LoadPlugins()
        {
            _log.Info( "Loading plugin DLLs");
            #region old plugin-loading code
            /*
            // MEF
            try {
                AggregateCatalog catalog = new AggregateCatalog();
                catalog.Catalogs.Add( new DirectoryCatalog( "."));
                try {
                    catalog.Catalogs.Add( new DirectoryCatalog( ".\\plugins"));
                } catch( System.IO.DirectoryNotFoundException) {
                    string exe_path = BioNex.Shared.Utils.FileSystem.GetAppPath();
                    System.IO.Directory.CreateDirectory( exe_path + "\\plugins");
                    string message = String.Format( "The /plugins folder was not present in {0}, and has been automatically created for you.  Although plugins are not necessary to run Synapsis, you should create this folder and populate it with device plugins if you want to at least be able to simulate protocols.", exe_path);
                    _log.Info( message);
                    MessageBox.Show( message);
                }
                // need to also add this assembly to the catalog, or we won't be able to import the ViewModel
                catalog.Catalogs.Add( new AssemblyCatalog( typeof(App).Assembly));
                Container = new CompositionContainer( catalog);
                try {
                    Container.ComposeParts( this);
                } catch( CompositionException ex) {
                    foreach( CompositionError e in ex.Errors) {
                        string description = e.Description;
                        string details = e.Exception.Message;
                        _log.Error( description + ": " + details);
                    }
                    throw;            
                } catch( System.Reflection.ReflectionTypeLoadException ex) {
                    foreach( Exception e in ex.LoaderExceptions) {
                        _log.Error( e.Message, e);
                        MessageBox.Show( e.Message);
                    }
                } catch( Exception ex) {
                    _log.Error( ex.Message, ex);
                    MessageBox.Show( ex.Message);
                }
            } catch( System.IO.DirectoryNotFoundException) {
                // couldn't find a plugins folder, so nothing else to do in this method
            } catch( Exception ex) {
                _log.Error( ex.Message);
                MessageBox.Show( ex.Message);
            }
             */
            #endregion
            try {
                BioNex.Shared.PluginManager.PluginLoader.LoadPlugins( this, typeof(App), out Container, new List<string> { "." }, "plugins", _log);
            } catch( AggregateException ae) {
                MessageBox.Show( ae.Flatten().Message);
            }
        }

        void ErrorPanelActionTaken(object sender)
        {
            var panel = sender as ErrorPanel;
            if (panel == null)
                return;
            if (!Main.Errors.Contains(panel))
                return;
            Main.Errors.Remove(panel);
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            // prevent interleaved events.  at least, this is what we had to do in MFC...
            _timer.Stop();

            try {
                if( StopTimerEvent.WaitOne( 0))
                    return;

                // DKM 2011-10-10 handling V11 device initialization errors with new plugin error caching class
                /*
                foreach( var err in PluginLoadingErrorManager.PendingErrors) {
                    Main.Errors.Add( new ErrorPanel( err));
                }
                PluginLoadingErrorManager.Clear();
                 */

                for( int i=_errors.Count - 1; i>=0; i--) {
                    var panel = new ErrorPanel( _errors[i]);
                    panel.ErrorActionTaken += ErrorPanelActionTaken;

                    Main.Errors.Add( panel);
                    _errors.RemoveAt( i);

                    // scroll to end when items are added
                    if( listbox_errors.Items.Count > 0) {
                        var border = VisualTreeHelper.GetChild( listbox_errors, 0) as Decorator;
                        if( border != null) {
                            var scroll = border.Child as ScrollViewer;
                            if (scroll != null)
                                scroll.ScrollToEnd();
                        }
                    }
                }

                //! \todo not sure if this is the best way to do this!!!
                // handle the Error SystemStatus condition by checking to see if any of
                // the errors in the list are not Handled, and if there are remaining errors,
                // send a message
                var unhandled_errors = from x in Main.Errors
                                       where !x.Handled
                                       select x;
                if( unhandled_errors.Count() != LastErrorCount) {
                    Messenger.Default.Send<UnhandledErrorCountMessage>( new UnhandledErrorCountMessage( unhandled_errors.Count()));
                    LastErrorCount = unhandled_errors.Count();
                }
            } catch( Exception ex) {
                MessageBox.Show( "Error in MainWindow timer: " + ex.Message);
            }

            CommandManager.InvalidateRequerySuggested();
            _timer.Start();
        }

        #region IError Members

/*        void TestErrors()
        {
            var error_strings = new List< string>{ "what", "why", "how"};
            ErrorData error_data = new ErrorData("error test1", error_strings);
            AddError(error_data);
            ErrorData error_data1 = new ErrorData("error test2", error_strings);
            AddError(error_data1);
            ErrorData error_data2 = new ErrorData("error test3", error_strings);
            AddError(error_data2);
        }*/

        public void AddError(ErrorData error)
        {
            // is this the right way to do this?
            Main.SelectedTabIndex = 1; // select the error tab
            _errors.Add( error);
            // play sound
            SoundPlayer player = new SoundPlayer( "\\chord.wav".ToAbsoluteAppPath());
            player.PlaySync();

            if (ErrorEvent != null)
                ErrorEvent(this, error);

            // report error via SMTP, if available
            foreach( var x in _notifiers)
                x.SendNotification( "Error notification", error.ErrorMessage);
        }

        public event ErrorEventHandler ErrorEvent;

        // DKM 2011-10-10 not really needed for main error interface
        public IEnumerable<ErrorData> PendingErrors { get { return new List<ErrorData>(); } }

        public bool WaitForUserToHandleError { get { return true; } }
        public void Clear() {}

        #endregion

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Main == null)
                return;

            // force everything to abort when closing, but if the user cancels the abort,
            // then we also want to cancel closing of the app.
            if (!Main.RequestAbort())
            {
                e.Cancel = true;
                return;
            }

            // now that we've aborted everything, we can let the customer GUI plugin do
            // its thing to determine if it's okay to close down now
            if (Main.CustomerGUI != null)
                if (!Main.CustomerGUI.CanClose())
                    e.Cancel = true;
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            if (Main == null)
                return;

            // added to confirm that app was getting Closed event -- it is.
            StopTimerEvent.Set(); // stop the timer before calling Main.Close, since the timer thread touches Main

            if (Main.CustomerGUI != null)
                Main.CustomerGUI.Close();
            Main.Close();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.B)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
                    (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt &&
                    (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {

                    var current_visibility = Main.MenuVisibility;
                    Main.MenuVisibility = (current_visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
                }
            }
        }
    }

    /// <summary>
    /// Needed to support MenuItem click handling via MVVM
    /// </summary>
    /// <remarks>
    /// http://www.codeproject.com/KB/WPF/CinchIII.aspx#WPFMenuItems
    /// </remarks>
    public class WPFMenuItem
    {
        public string Text { get; set; }
        public string IconUrl { get; set; }
        public List<WPFMenuItem> Children { get; private set; }
        public RelayCommand Command { get; set; }

        public WPFMenuItem( string item)
        {
            Text = item;
            Children = new List<WPFMenuItem>();
        }
    }
}
