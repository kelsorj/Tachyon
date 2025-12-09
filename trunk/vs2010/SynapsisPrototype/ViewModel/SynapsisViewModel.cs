using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.ErrorHandling;
using BioNex.Shared.IError;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.PlateWork;
using BioNex.Shared.Utils;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using log4net;
using Microsoft.WindowsAPICodePack.ApplicationServices;
using SMTPReporting;

namespace BioNex.SynapsisPrototype.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm/getstarted
    /// </para>
    /// </summary>
    [Export("SynapsisViewModel")]
    public class SynapsisViewModel : ViewModelBase
    {
        private VersionDialog _version_dlg;

        public RelayCommand StartCommand { get; set; }
        public RelayCommand AbortCommand { get; set; }
        public RelayCommand PauseResumeCommand { get; set; }

        [ImportMany(typeof(IReportsStatus))]
        private List<IReportsStatus> _status_reporters;

        [Import(typeof(PreferencesDialog))]
        private PreferencesDialog _preferences_dialog;

        public class ProtocolCompleteEventArgs : EventArgs
        {
            public string Message { get; set; }
            public bool ShowMessageBox { get; set; }

            public ProtocolCompleteEventArgs() { Message = "Done"; ShowMessageBox = true; }
        }
        public event EventHandler ProtocolCompleteEvent;

        private Visibility _interlocks_overridden;
        public Visibility InterlocksOverridden
        {
            get { return _interlocks_overridden; }
            set {
                _interlocks_overridden = value;
                RaisePropertyChanged( "InterlocksOverridden");
            }
        }

        private string _start_command_tooltip;
        public string StartCommandToolTip
        {
            get { return _start_command_tooltip; }
            set {
                _start_command_tooltip = value;
                RaisePropertyChanged( "StartCommandToolTip");
            }
        }

        /*
        private string home_all_devices_tooltip_;
        public string HomeAllDevicesToolTip
        {
            get { return home_all_devices_tooltip_; }
            set {
                home_all_devices_tooltip_ = value;
                RaisePropertyChanged( "HomeAllDevicesToolTip");
            }
        }
         */

        string _interlocks_overridden_text;
        public string InterlocksOverriddenText
        {
            get { return _interlocks_overridden_text; }
            set {
                _interlocks_overridden_text = value;
                RaisePropertyChanged( "InterlocksOverriddenText");
            }
        }

        int _progressbar_maximum;
        public int ProgressBarMaximum
        {
            get { return _progressbar_maximum; }
            set {
                _progressbar_maximum = value;
                RaisePropertyChanged( "ProgressBarMaximum");
            }
        }

        int _progressbar_value;
        public int ProgressBarValue
        {
            get { return _progressbar_value; }
            set {
                _progressbar_value = value;
                RaisePropertyChanged( "ProgressBarValue");
            }
        }

        private string _estimated_time_remaining;
        public string EstimatedTimeRemaining
        {
            get { return _estimated_time_remaining; }
            set {
                _estimated_time_remaining = value;
                RaisePropertyChanged( "EstimatedTimeRemaining");
            }
        }

        private DateTime _protocol_start_time { get; set; }

        private int _selected_tab_index;
        public int SelectedTabIndex
        {
            get { return _selected_tab_index; }
            set {
                _selected_tab_index = value;
                RaisePropertyChanged( "SelectedTabIndex");
            }
        }

        private int _pause_button_glow_size;
        public int PauseButtonGlowSize
        {
            get { return _pause_button_glow_size; }
            set {
                _pause_button_glow_size = value;
                RaisePropertyChanged( "PauseButtonGlowSize");
            }
        }

        private int _pause_button_shadow_depth;
        public int PauseButtonShadowDepth
        {
            get { return _pause_button_shadow_depth; }
            set {
                _pause_button_shadow_depth = value;
                RaisePropertyChanged( "PauseButtonShadowDepth");
            }
        }

        //---------------------------------------------------------------------
        // DATABINDING PROPERTIES
        //---------------------------------------------------------------------

        public bool AllDevicesHomed
        {
            get { 
                return _model != null ? _model.AllDevicesHomed : false;
            }
            set {}
        }

        public ObservableCollection<WPFMenuItem> DiagnosticsPlugins
        { 
            get {
                return CreateDiagnosticsMenu();
            }
        }

        public ObservableCollection<WPFMenuItem> SystemSetupItems
        {
            get {
                return CreateSystemSetupItems();
            }
        }

        public ObservableCollection<ErrorPanel> Errors { get; set; }
        public ObservableCollection<UserControl> SystemCheckPanels
        { 
            get {
                if( _model == null)
                    return new ObservableCollection<UserControl>();

                var panels = _model.GetSystemCheckPanels();
                // hide the panels view if there aren't any panels
                if( panels.Count() == 0) {
                    SystemCheckPanelsWidth = 0;
                    SystemCheckPanelsVisibility = Visibility.Hidden;
                } else {
                    SystemCheckPanelsWidth = -1;
                    SystemCheckPanelsVisibility = Visibility.Visible;
                }
                return panels;
            }
        }

        private int _system_check_panels_width;
        public int SystemCheckPanelsWidth
        {
            get { return _system_check_panels_width; }
            set {
                _system_check_panels_width = value;
                RaisePropertyChanged( "SystemCheckPanelsWidth");
            }
        }

        private Visibility _system_check_panels_visibility;
        public Visibility SystemCheckPanelsVisibility
        {
            get { return _system_check_panels_visibility; }
            set {
                _system_check_panels_visibility = value;
                RaisePropertyChanged( "SystemCheckPanelsVisibility");
            }
        }
        
        private Visibility SystemStartupPanelVisibility
        {
            get {
                return SystemCheckPanels.Count() > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private Visibility _menu_visibility;
        public Visibility MenuVisibility { get { return _menu_visibility; } set { _menu_visibility = value; RaisePropertyChanged("MenuVisibility"); } }
        private Visibility _customer_gui_tab_visibility; // hides the TAB, not the whole UI -- for cases where we need more real estate
        public Visibility CustomerGUITabVisibility { get { return _customer_gui_tab_visibility; } set { _customer_gui_tab_visibility = value; RaisePropertyChanged("CustomerGUITabVisibility"); } }

        //---------------------------------------------------------------------
        // CLASS STUFF
        //---------------------------------------------------------------------
        internal Model.SynapsisModel _model { get; set; }
        public AbortPauseResumeStateMachine AbortPauseResumeSM { get; set; }
        // logging to _log goes into all of the main log's appenders
        private static readonly ILog _log = LogManager.GetLogger( typeof( SynapsisViewModel));
        private DateTime ProtocolStartTime { get; set; }

        // DKM 2011-01-11 added to support RunForever feature for LabAuto.  I didn't want to rerun the
        //                protocol recursively, so I have a timer run 
        private DispatcherTimer RunForeverCheckTimer { get; set; }

        /// <summary>
        /// Allows the main GUI (which loads plugins) to tell the ViewModel which customer GUI plugins
        /// are available, so that the ViewModel can present a selection option to the user.
        /// </summary>
        internal List<string> _customer_gui_names { get; set; }

        public IPlateScheduler PlateScheduler { get; protected set; }
        public IRobotScheduler RobotScheduler { get; protected set; }
        private SMTPReporter Mailer { get; set; }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        [ImportingConstructor]
        public SynapsisViewModel( [Import("SynapsisModel")] Model.SynapsisModel model, [ Import] IPlateScheduler plate_scheduler, [ Import] IRobotScheduler robot_scheduler, [Import] AbortPauseResumeStateMachine abort_pause_resume_sm, [ Import] IError error_interface, [ Import] SMTPReporter mailer)
        {
            _version_dlg = new VersionDialog();
            _log.Debug( _version_dlg.GetFileVersionsString());

            PlateScheduler = plate_scheduler;
            PlateScheduler.StartScheduler();
            RobotScheduler = robot_scheduler;
            RobotScheduler.StartScheduler();
            Mailer = mailer;
            Errors = new ObservableCollection<ErrorPanel>();
            _model = model;
            MenuVisibility = Visibility.Visible;
            CustomerGUITabVisibility = Visibility.Visible;
            StartCommand = new RelayCommand( ExecuteStartCommand, CanExecuteStartCommand);
            PauseResumeCommand = new RelayCommand( ExecutePauseResume, CanExecutePauseResume);
            AbortCommand = new RelayCommand( ExecuteAbort, CanExecuteAbortCommand);
            AbortPauseResumeSM = abort_pause_resume_sm;// new AbortPauseResumeStateMachine();
            AbortPauseResumeSM.GuiResumeEvent += new EventHandler(AbortPauseResumeSM_ResumeEvent);
            AbortPauseResumeSM.GuiPauseEvent += new EventHandler(AbortPauseResumeSM_PauseEvent);
            AbortPauseResumeSM.GuiAbortEvent += new EventHandler(AbortPauseResumeSM_AbortEvent);
            //PauseResumeText = "Pause";

            _model.ModelSafetyEventTriggered += new EventHandler(_model_ModelSafetyEventTriggered);
            _model.ModelSafetyEventReset += new EventHandler(_model_ModelSafetyEventReset);
            _model.ModelSafetyOverriddenEvent += new EventHandler<SafetyEventArgs>(_model_ModelSafetyOverriddenEvent);
            InterlocksOverridden = _model.InterlocksOverridden ? Visibility.Visible : Visibility.Hidden;

            Messenger.Default.Register<NumberOfTransferCompleteMessage>( this, (msg) => { 
                ProgressBarValue += msg.NumberOfTransfers;
                // the time estimation is pretty rudimentary.  Look at how much time we've spent running, then
                // look at how much more we have to do, and calculate from there.
                TimeSpan time_elapsed = DateTime.Now - _protocol_start_time;
                long time_per_hit = time_elapsed.Ticks / ProgressBarValue;
                int hits_remaining = ProgressBarMaximum - ProgressBarValue;
                long time_remaining_ticks = hits_remaining * time_per_hit;
                EstimatedTimeRemaining = String.Format( "Estimated time remaining: {0:hh\\:mm\\:ss}", new TimeSpan( time_remaining_ticks));
            });
            Messenger.Default.Register<TotalTransfersMessage>( this, (msg) => { 
                ProgressBarValue = 0;
                ProgressBarMaximum = msg.TotalTransfers;
                EstimatedTimeRemaining = "";
                _protocol_start_time = DateTime.Now;
            });

            // blank out the progressbar upon app startup so Giles can see where the border is.  ;)
            ProgressBarValue = 0;
            ProgressBarMaximum = int.MaxValue;

            PauseButtonGlowSize = 0;
            PauseButtonShadowDepth = 0;

            RunForeverCheckTimer = new DispatcherTimer();
            RunForeverCheckTimer.Interval = new TimeSpan( 0, 0, 1);
            RunForeverCheckTimer.Tick += new EventHandler(RunForeverCheckTimer_Tick);

            // DKM 2012-04-06 refs #559 Windows CodePack API is not compatible with Windows XP... only Vista and above
            if (Environment.OSVersion.Version.Major >= 6) { // Windows Vista or higher
                PowerManager.BatteryLifePercentChanged += new EventHandler(PowerManager_BatteryLifePercentChanged);
                PowerManager.PowerSourceChanged += new EventHandler(PowerManager_PowerSourceChanged);
            }
        }

        void RunForeverCheckTimer_Tick(object sender, EventArgs e)
        {
            RunForeverCheckTimer.Stop();
            ExecuteStartCommand();
        }

        internal ICustomerGUI CustomerGUI
        {
            get { return _model != null ? _model.CustomerGUI : null; }
            set { 
                if( _model == null)
                    return;

                //! \todo this needs re-examination -- I had to set the ProtocolComplete handler here
                //! because this is where the model's CustomerGUI reference gets set.  I had it in the
                //! constructor earlier, but at that point the model's CustomerGUI reference is null.
                _model.CustomerGUI = value;
                _model.CustomerGUI.ProtocolComplete += new EventHandler(CustomerProtocolComplete);
            }
        }

        public void Close()
        {
            // MAKE DAMN SURE our sub-windows are closed
            // I detect anger.  :)
            _preferences_dialog.Close();
            _version_dlg.Close();

            // DKM 2011-11-09 kill all pending error messages
            var pending_errors = from e in Errors where e.IsEnabled == true select e;
            foreach( var e in pending_errors) {
                e.Cancel();
            }

            RunForeverCheckTimer.Stop();
            PlateScheduler.StopScheduler();
            RobotScheduler.StopScheduler();
            if( _model != null)
                _model.deviceManager.Close();
        }

        void _model_ModelSafetyOverriddenEvent(object sender, SafetyEventArgs e)
        {
            InterlocksOverridden = e.Overridden ? Visibility.Visible : Visibility.Hidden;
            InterlocksOverriddenText = e.Message;
        }

        void CustomerProtocolComplete(object sender, EventArgs e)
        {
            // if paused, then resume to allow ProtocolComplete/ProtocolAborted to act.
            if( _model != null && _model.Paused){
                AbortPauseResumeSM.PauseResume();
            }

            bool protocol_was_aborted = _model == null ? false : _model.Aborting;

            foreach (ProtocolHooksInterface i in _model.deviceManager.GetProtocolHooksInterfaces())
            {
                if( protocol_was_aborted) {
                    i.ProtocolAborted();
                } else {
                    i.ProtocolComplete();
                }
            }

            // DKM 2011-06-08 #470 reset devices so that diags aren't inoperable in certain cases after aborting a protocol
            if( _model != null) {
                foreach( DeviceInterface di in _model.deviceManager.GetDeviceInterfaces())
                    di.Reset();
            }

            // stop the abort/pause/resume state machine.
            AbortPauseResumeSM.Done();

            // handle the time calculation
            TimeSpan run_duration = DateTime.Now - ProtocolStartTime;
            string done_message = String.Format( "DONE -- total time: {0:0.000} minutes", run_duration.TotalMinutes);
            _log.Info( done_message);
            // notify all of the system status devices
            if( _model != null) {
                var status_devices = _model.deviceManager.GetSystemStatusInterfaces();
                foreach( SystemStatusInterface si in status_devices)
                    si.ProtocolComplete( true);                        
            }
            // ding
            // play sound
            SoundPlayer player = new SoundPlayer( "\\tada.wav".ToAbsoluteAppPath());
            player.PlaySync();
            
            // check "RunForever" setting, and if it's set, set a flag that will cause the protocol to execute again
            if( _preferences_dialog.RunForever && !protocol_was_aborted) {
                QueueAnotherRun();
                return;
            }

            // pop up the message box in the main GUI, so we can do it modally
            if (ProtocolCompleteEvent != null)
            {
                var pce = e as SynapsisViewModel.ProtocolCompleteEventArgs;
                if (e == null)
                    pce = new ProtocolCompleteEventArgs { Message = done_message };
                else 
                    pce.Message = done_message;
                ProtocolCompleteEvent( this, pce);
            }
        }

        private void QueueAnotherRun()
        {
            // do customer plugin specific stuff here?

            // run the timer for a tick
            RunForeverCheckTimer.Start();
        }

        void _model_ModelSafetyEventTriggered(object sender, EventArgs e)
        {
            AbortPauseResumeSM.InterlockTriggered();
        }

        void _model_ModelSafetyEventReset(object sender, EventArgs e)
        {
            AbortPauseResumeSM.InterlockReset();
        }

        /// <summary>
        /// Called when user clicks the Abort button in the main GUI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AbortPauseResumeSM_AbortEvent(object sender, EventArgs e)
        {
            PauseButtonGlowSize = 0;
            PauseButtonShadowDepth = 0;
            if( _model != null)
                _model.Abort();
        }

        /// <summary>
        /// Called when user clicks the Pause button in the main GUI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AbortPauseResumeSM_PauseEvent(object sender, EventArgs e)
        {
            PauseButtonGlowSize = 50;
            PauseButtonShadowDepth = 1;
            if( _model != null)
                _model.Pause();
        }

        /// <summary>
        /// Called when user clicks the Resume button in the main GUI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AbortPauseResumeSM_ResumeEvent(object sender, EventArgs e)
        {
            PauseButtonGlowSize = 0;
            PauseButtonShadowDepth = 0;
            if( _model != null)
                _model.Resume();
        }

        private void ExecuteAbort()
        {
            bool dummy_return_I_dont_care_about = RequestAbort();
        }

        public bool RequestAbort()
        {
            if( _model != null && _model.Idle)
                return true;

            if( _model != null)
                _model.Pause();

            if( MessageBox.Show( "Are you sure you want to abort all currently-running operations?", "Confirm abort", MessageBoxButton.YesNo) == MessageBoxResult.No) {
                AbortPauseResumeSM.PauseResume();
                return false;
            }

            if( AbortPauseResumeSM != null)
                AbortPauseResumeSM.Abort();
            return true;
        }

        /*
        private bool CanExecuteHomeAllDevicesCommand()
        {
            StringBuilder sb = new StringBuilder();
            string reason;
            if( !_model.SystemCheckOK( out reason)) { sb.AppendLine( reason); }
            if( AbortPauseResumeSM.Running || AbortPauseResumeSM.Paused) { sb.AppendLine( "Protocol is running"); }
            if( _model.CustomerGUI != null && _model.CustomerGUI.Busy) { sb.AppendLine( _model.CustomerGUI.BusyReason); }

            if( sb.Length != 0) {
                HomeAllDevicesToolTip = sb.ToString();
                return false;
            }

            HomeAllDevicesToolTip = "Home all devices";
            return true;
        }
         */

        private readonly AutoResetEvent CloseHourglassWindowEvent = new AutoResetEvent( false);

        private void ProcessStartedWithHourglassWindow( Action action)
        {
            action.BeginInvoke( OnActionComplete, null);
            var hg = new BioNex.Shared.Utils.HourglassWindow
            {
                Title = "Protocol starting",
                Owner = Application.Current.MainWindow
            };
            hg.Show();
            //! \todo figure out another way to accomplish this non-blocking UI behavior without DoEvents
            while( !CloseHourglassWindowEvent.WaitOne( 10))
                System.Windows.Forms.Application.DoEvents();
            hg.Close();
        }

        private void OnActionComplete( IAsyncResult iar)
        {
            AsyncResult ar = ( AsyncResult)iar;
            Action caller = ( Action)ar.AsyncDelegate;
            CloseHourglassWindowEvent.Set();
        }

        // FYC: this is Monsanto specific functionality that should be moved into Monsanto plugin.
        private void ValidateTransferOverview( TransferOverview transfer_overview)
        {
            IList< PlateStorageInterface> plate_origins = new List< PlateStorageInterface>();
            foreach( KeyValuePair< string, Plate> kvp in transfer_overview.SourcePlates){
                string location_name;
                PlateStorageInterface plate_origin = _model != null ? _model.deviceManager.GetPlateStorageWithBarcode( kvp.Key, out location_name) : null;
                if( plate_origin != null){
                    plate_origins.Add( plate_origin);
                } else{
                    _log.InfoFormat( "Plate with barcode {0} is unavailable", kvp.Key);
                }
            }
            if (plate_origins.Count == 0)
                throw new Exception("No source plates from this project found in inventory");

            IEnumerable< PlateStorageInterface> distinct_plate_origins = plate_origins.Distinct();
            if( distinct_plate_origins.Count() > 1){
                throw new Exception( "Plates within this project come from different storage devices");
            }
            PlateStorageInterface the_origin = distinct_plate_origins.FirstOrDefault();
            Debug.Assert( the_origin != null);
            transfer_overview.PlateStorageInterfaceName = ( the_origin as DeviceInterface).Name;
        }

        void HandleLastWorklistComplete( object sender, EventArgs e)
        {
            Worklist worklist = sender as Worklist;
            string subject = string.Format( "[{0}] projects completed", Mailer.HiveName);
            string body = subject;
            Mailer.SendMessage( subject, body);
        }

        public void EnqueueTransferOverview( TransferOverview transfer_overview, string transfer_overview_name, bool last_worklist)
        {
            ValidateTransferOverview( transfer_overview);
            Worklist worklist = WorksetSequencer.DetermineSequence( transfer_overview_name, transfer_overview);
            if( last_worklist){
                worklist.WorklistComplete += new EventHandler( HandleLastWorklistComplete);
            }
            PlateScheduler.EnqueueWorklist( worklist);
        }

        private void ExecuteStartCommand()
        {
            var pending_errors = from e in Errors where e.IsEnabled == true select e;
            int num_pending_errors = pending_errors.Count();
            if( num_pending_errors > 0){
                if( MessageBox.Show( "Protocol may not be started with errors pending.  Dismiss all errors to continue running protocol?", "Run Protocol", MessageBoxButton.YesNo) == MessageBoxResult.No){
                    return;
                }
                foreach( ErrorPanel pending_error in pending_errors){
                    pending_error.Abort();
                    pending_error.IsEnabled = false;
                }
            }
            
            if( _model != null)
                _model.Reset();

            ProtocolStartTime = DateTime.Now;
            if( _model != null && _model.CustomerGUI != null) {
                ProcessStartedWithHourglassWindow( () => _model.ProcessStarting());
                if( _model.CustomerGUI.ExecuteStart()) {
                    // successful start
                    _model.ProcessStarted();
                    AbortPauseResumeSM.Start();
                } else {
                    // failed start
                    AbortPauseResumeSM.Done();
                }
            }
        }

        private bool CanExecuteStartCommand()
        {
            if( _model != null && _model.CustomerGUI == null) {
                StartCommandToolTip = "No GUI plugin was loaded";
                return false;
            } else if( AbortPauseResumeSM != null && AbortPauseResumeSM.Idle) {
                // test all system startup check interfaces to make sure they are ready to go, i.e. lazy susan
                string reason;
                if( _model != null && !_model.SystemCheckOK( out reason)) {
                    StartCommandToolTip = reason;
                    return false;
                }
            } else {
                StartCommandToolTip = "Protocol is currently running";
                return false;
            }

            StringBuilder sb = new StringBuilder();
            if( AbortPauseResumeSM != null && (AbortPauseResumeSM.Running || AbortPauseResumeSM.Paused)) sb.AppendLine( "Protocol is running");
            if( !AllDevicesHomed) sb.AppendLine( "Not all devices are homed");
            IEnumerable<string> customer_gui_failure_reasons;
            if( _model != null && _model.CustomerGUI != null && !_model.CustomerGUI.CanExecuteStart( out customer_gui_failure_reasons)) {
                foreach( string s in customer_gui_failure_reasons)
                    sb.AppendLine( s);
            }

            if( sb.Length != 0) {
                StartCommandToolTip = sb.ToString();
                return false;
            } else {
                StartCommandToolTip = "Click to start protocol";
                return true;
            }
        }

        private bool CanExecuteAbortCommand()
        {
            return (!CanExecuteStartCommand() && !AbortPauseResumeSM.Idle);
        }

        /// <remarks>
        /// This should go in the Model
        /// </remarks>
        private void ExecutePauseResume()
        {
            AbortPauseResumeSM.PauseResume();
        }

        private bool CanExecutePauseResume()
        {
            if( _model == null)
                return false;

            if( _model.CustomerGUI == null)
                return false;
            
            if( !_model.CustomerGUI.CanPause())
                return false;

            return AbortPauseResumeSM.PauseResumeButtonEnabled;
        }

        /// <summary>
        /// Give the GUI plugin an opportunity to change the display size of the buttons along the bottom
        /// </summary>
        /// <param name="min_height"></param>
        /// <param name="font_size"></param>
        public void SetPauseRowMinHeightAndTextSize(int min_height, int font_size)
        {
            MainWindow win = (MainWindow)App.Current.MainWindow;
            win.PauseResumeButton.MinHeight = min_height;
            win.StartButton.FontSize = font_size;
            win.PauseResumeButton.FontSize = font_size;
            win.AbortButton.FontSize = font_size;
        }

        private void HomeAllDevices()
        {
            // ensure the user really wants to rehome all devices
            if( AllDevicesHomed) {
                MessageBoxResult answer = MessageBox.Show( "All devices are already homed.  Are you sure you want to rehome them?", "Rehome all devices?", MessageBoxButton.YesNo);
                if( answer == MessageBoxResult.No) {
                    _log.Info( "user does not want to rehome all devices");
                    return;
                }
                _log.Info( "user requested to rehome all devices");
            }

            if( _model != null)
                _model.HomeAllDevices();
        }

        private ObservableCollection<WPFMenuItem>  CreateDiagnosticsMenu()
        {
            ObservableCollection<WPFMenuItem> menu = new ObservableCollection<WPFMenuItem>();
            if( _model == null || CustomerGUI == null)
                return menu;

            IEnumerable<DeviceInterface> devices = _model.GetAllDevices();
            foreach( DeviceInterface di in devices) {
                // need to assign a temporary variable because of closures.  foreach does something
                // different than you might think.  http://stackoverflow.com/questions/512166/c-the-foreach-identifier-and-closures
                DeviceInterface d = di;
                string device_name = d.Name;
                WPFMenuItem item = new WPFMenuItem( device_name);
                item.Command = new RelayCommand( () => ShowDiagnostics(d), () => CustomerGUI.AllowDiagnostics());
                menu.Add( item);
            }

            menu.Add( new WPFMenuItem("________________"));
            WPFMenuItem system_status_item = new WPFMenuItem( "Display System Status");
            system_status_item.Command = new RelayCommand( DisplaySystemStatus);
            menu.Add( system_status_item);

            return menu;
        }

        private void DisplaySystemStatus()
        {
            if( _status_reporters == null)
                return;

            foreach( var reporter in _status_reporters) {
                Debug.WriteLine( reporter.GetStatus());
            }
        }

        private SortedSet<DeviceInterface> _device_diagnostics_open = new SortedSet<DeviceInterface>();
        private static void ShowDiagnostics( DeviceInterface device)
        {
            /*
            // did we already open these diags?
            if( _device_diagnostics_open.Contains( device))
                return;
            device.ShowDiagnostics();
            _device_diagnostics_open.Add( device);
             */

            // DKM 2011-03-02 until we have an event that gets fired when Diagnostics is closing, we can't manage the diagnostics dialogs.
            // perhaps we can fall back to the mutex approach to get the job done.
            device.ShowDiagnostics();
        }

        private void SetActiveGui( string name)
        {
            _preferences_dialog.SelectedCustomerGui = name;
            _preferences_dialog.SavePreferences();

            MessageBoxResult answer = MessageBox.Show( "You will need to restart Synapsis for the changes to take effect.  Would you like to restart now?", "Restart required", MessageBoxButton.YesNo);
            if( answer == MessageBoxResult.Yes) {
                SpawnAndDie restart = new SpawnAndDie( BioNex.Shared.Utils.FileSystem.GetExePath(), true);
            }
        }

        /// <remarks>
        /// Need to make plugins that go into the System Setup menu export the right interface so we
        /// don't have to handle each tool separately.  Should be able to do this in a for loop.
        /// </remarks>
        /// <returns></returns>
        private ObservableCollection<WPFMenuItem> CreateSystemSetupItems()
        {
            ObservableCollection<WPFMenuItem> menu = new ObservableCollection<WPFMenuItem>();
            if( _model == null)
                return menu;

            // add the system setup tools, like labware and liquids
            foreach( BioNex.Shared.LibraryInterfaces.ISystemSetupEditor editor in _model.SystemSetupEditors) {
                BioNex.Shared.LibraryInterfaces.ISystemSetupEditor e = editor;
                WPFMenuItem menuitem = new WPFMenuItem( e.Name);
                menuitem.Command = new RelayCommand( e.ShowTool);
                menu.Add( menuitem);
            }

            // add another dropmenu to allow the user to select the desired customer GUI plugin
            WPFMenuItem customer_plugin_menu = new WPFMenuItem( "Customer GUI");
            if( _customer_gui_names != null) {
                foreach( string name in _customer_gui_names) {
                    // closures again!!!!
                    string name_copy = name;
                    WPFMenuItem item = new WPFMenuItem( name_copy);
                    // this is where closures get you -- lamdas. --------vvvvvvvvv
                    item.Command = new RelayCommand( () => SetActiveGui( name_copy));
                    customer_plugin_menu.Children.Add( item);
                    // set the selected "checkbox" next to the item if it's the currently active GUI
                    if( name_copy == CustomerGUI.GUIName)
                        item.IconUrl = "Images/success.png";
                    else
                        item.IconUrl = "";
                }
            }
            menu.Add( customer_plugin_menu);

            // now add the version dialog
            WPFMenuItem version_dialog = new WPFMenuItem( "Version information");
            version_dialog.Command = new RelayCommand( ShowVersionDialog);
            menu.Add( version_dialog);

            // add a utility to pinch off the database files
            WPFMenuItem database_pinch = new WPFMenuItem( "Reset all logs");
            database_pinch.Command = new RelayCommand( ResetLogs);
            menu.Add( database_pinch);

            // add a preferences option for the application
            WPFMenuItem preferences = new WPFMenuItem( "Preferences");
            preferences.Command = new RelayCommand( DisplayPreferences);
            menu.Add( preferences);

            return menu;
        }

        private void ShowVersionDialog()
        {
            _version_dlg = new VersionDialog();
            _version_dlg.ShowDialog();
            _version_dlg.Close();
        }

        private static void ResetLogs()
        {            
            string exe_path = BioNex.Shared.Utils.FileSystem.GetAppPath();
            BioNex.Shared.Utils.Logging.SetLogFilePath(exe_path + "\\logs", "synapsis", true);
            BioNex.Shared.Utils.Logging.PinchDatabaseLogs();
            _log.Info( "User reset logs");
        }

        private void DisplayPreferences()
        {
            _preferences_dialog.ShowDialog();
        }

        // DKM 2011-10-13 I think we need to have an event in ServicesDevice that tells the subscriber when
        //                the service is going to start doing some work.  Need to figure out how to make
        //                the service block until something like LoadStack has finished for all loaded
        //                stackers???
        public void StartServices()
        {
            if( _model == null)
                return;

            IEnumerable< ServicesDevice> services_devices = _model.deviceManager.DevicePluginsAvailable.Values.Where( device => device as ServicesDevice != null).Select( device => device as ServicesDevice);
            foreach( ServicesDevice services_device in services_devices){
                services_device.StartServices();
    }
        }

        public void StopServices()
        {
            if( _model == null)
                return;

            IEnumerable< ServicesDevice> services_devices = _model.deviceManager.DevicePluginsAvailable.Values.Where( device => device as ServicesDevice != null).Select( device => device as ServicesDevice);
            foreach( ServicesDevice services_device in services_devices){
                services_device.StopServices();
            }
        }

        internal void PowerManager_PowerSourceChanged(object sender, EventArgs e)
        {
            if( PowerManager.PowerSource != PowerSource.Battery) // note that when you plug the UPS back in, that's when PowerSource == UPS
                _log.Info( "System is on AC power");
            else
                _log.Info( "System is running on battery backup");
        }

        internal void PowerManager_BatteryLifePercentChanged(object sender, EventArgs e)
        {
            BatteryState battery_state = PowerManager.GetCurrentBatteryState();
            if( battery_state.ACOnline)
                return;

            var current_battery_percent = PowerManager.BatteryLifePercent;
            if( current_battery_percent < _preferences_dialog.LowBatteryThresholdPercentage) {
                // if we are in here, that means we're not on AC power and we're below the low battery threshold, so warn the user
                string message = String.Format("Lost AC power. Battery level is currently {0}%.  Please prepare to shut down Synapsis. Estimated run time remaining is {1}.", current_battery_percent, battery_state.EstimatedTimeRemaining.ToString());
                _log.FatalFormat( message);
                string subject = string.Format( "{0} - Lost AC power", Mailer.HiveName);
                string body = message;
                Mailer.SendMessage( subject, body);               
            }
        }
    }
}