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
using BioNex.HiGIntegration;
using System.ComponentModel;
using System.Windows.Threading;
using BioNex.Shared.SimpleWizard;
using System.Threading;
using BioNex.Shared.Utils;

namespace HiGIntegrationTestApp
{
    /// <summary>
    /// Interaction logic for HiGPanel.xaml
    /// </summary>
    public partial class HiGPanel : UserControl, INotifyPropertyChanged
    {
        private Window _owner;
        internal HiGInterface _hig;
        private int _index;
        private AutoResetEvent _first_time_setup_initialize_complete_event;
        private AutoResetEvent _first_time_setup_initialize_error_event;
        private AutoResetEvent _first_time_setup_home_complete_event;
        private AutoResetEvent _first_time_setup_home_error_event;

        // DKM 2011-11-09 I had to cache these values from the button handlers, because adding the first-time setup
        //                wizard required calling ExecuteInitialize from a thread.  If we access the GUI elements
        //                from the worker thread we get an STA error.
        private int _adapter_id;
        private bool? _simulate;
        // DKM 2012-04-04 changed from text box to combobox
        public ICollectionView AvailableAdapterIds { get; set; }
        // DKM 2012-04-04 add blocking functionality
        public bool? _blocking;

        public SimpleRelayCommand SpinCommand { get; set; }
        public SimpleRelayCommand OpenShieldToBucket1Command { get; set; }
        public SimpleRelayCommand OpenShieldToBucket2Command { get; set; }
        public SimpleRelayCommand HomeCommand { get; set; }
        public SimpleRelayCommand AbortSpinCommand { get; set; }
        public SimpleRelayCommand InitializeCommand { get; set; }
        public SimpleRelayCommand CloseCommand { get; set; }
        public SimpleRelayCommand ShowCycleTimeCommand { get; set; }
        public SimpleRelayCommand FirstTimeSetupCommand { get; set; }
        public SimpleRelayCommand PackForShipmentCommand { get; set; }

        // DKM 2012-04-02 added support for homed / not homed indicator
        private string _homed_not_homed_text;
        public string HomedNotHomedText
        {
            get { return _homed_not_homed_text; }
            set
            {
                _homed_not_homed_text = value;
                OnPropertyChanged("HomedNotHomedText");
            }
        }
        
        private Brush _homed_not_homed_background;
        public Brush HomedNotHomedBackground
        {
            get { return _homed_not_homed_background; }
            set
            {
                _homed_not_homed_background = value;
                OnPropertyChanged("HomedNotHomedBackground");
            }
        }

        private Brush _homed_not_homed_foreground;
        public Brush HomedNotHomedForeground
        {
            get { return _homed_not_homed_foreground; }
            set
            {
                _homed_not_homed_foreground = value;
                OnPropertyChanged("HomedNotHomedForeground");
            }
        }

        private string _initialized_not_initialized_text;
        public string InitializedNotInitializedText
        {
            get { return _initialized_not_initialized_text; }
            set
            {
                _initialized_not_initialized_text = value;
                OnPropertyChanged("InitializedNotInitializedText");
            }
        }

        private Brush _initialized_not_initialized_background;
        public Brush InitializedNotInitializedBackground
        {
            get { return _initialized_not_initialized_background; }
            set
            {
                _initialized_not_initialized_background = value;
                OnPropertyChanged("InitializedNotInitializedBackground");
            }
        }

        private Brush _initialized_not_initialized_foreground;
        public Brush InitializedNotInitializedForeground
        {
            get { return _initialized_not_initialized_foreground; }
            set
            {
                _initialized_not_initialized_foreground = value;
                OnPropertyChanged("InitializedNotInitializedForeground");
            }
        }

        // DKM 2012-04-03 added bucket indicator to API for Andrew Carretta @ HRB

        private string _current_bucket_text;
        public string CurrentBucketText
        {
            get { return _current_bucket_text; }
            set
            {
                _current_bucket_text = value;
                OnPropertyChanged("CurrentBucketText");
            }
        }

        private Brush _current_bucket_foreground;
        public Brush CurrentBucketForeground
        {
            get { return _current_bucket_foreground; }
            set
            {
                _current_bucket_foreground = value;
                OnPropertyChanged("CurrentBucketForeground");
            }
        }

        private Brush _current_bucket_background;
        public Brush CurrentBucketBackground
        {
            get { return _current_bucket_background; }
            set
            {
                _current_bucket_background = value;
                OnPropertyChanged("CurrentBucketBackground");
            }
        }
        
        
        
        private DispatcherTimer _timer;

        private readonly string HomedString = "Homed";
        private readonly string NotHomedString = "Not Homed";
        private readonly Brush HomedBackground = Brushes.DarkGreen;
        private readonly Brush HomedForeground = Brushes.Goldenrod;
        private readonly Brush NotHomedBackground = Brushes.DarkRed;
        private readonly Brush NotHomedForeground = Brushes.Goldenrod;
        private readonly string InitializedString = "Initialized";
        private readonly string NotInitializedString = "Not Initialized";

        private readonly string UnknownBucketString = "Unknown Bucket";
        private readonly Brush KnownBucketBackground = Brushes.DarkGreen;
        private readonly Brush UnknownBucketBackground = Brushes.DarkRed;
        private readonly Brush KnownBucketForeground = Brushes.Goldenrod;
        private readonly Brush UnknownBucketForeground = Brushes.Goldenrod;

        private Dispatcher _main_dispatcher;

        public HiGPanel( Window owner)
        {
            InitializeComponent();
            _main_dispatcher = this.Dispatcher;
            this.DataContext = this;
            _owner = owner;
            _first_time_setup_initialize_complete_event = new AutoResetEvent(false);
            _first_time_setup_initialize_error_event = new AutoResetEvent(false);
            _first_time_setup_home_complete_event = new AutoResetEvent(false);
            _first_time_setup_home_error_event = new AutoResetEvent(false);

            AvailableAdapterIds = CollectionViewSource.GetDefaultView( Enumerable.Range( 0, 255));
            AvailableAdapterIds.MoveCurrentToFirst();
            _hig = new HiG();

            InitializeCommands();

            // DKM 2012-04-02 homed / not homed
            HomedNotHomedText = NotHomedString;
            HomedNotHomedBackground = NotHomedBackground;
            HomedNotHomedForeground = NotHomedForeground;
            InitializedNotInitializedText = NotInitializedString;
            InitializedNotInitializedBackground = NotHomedBackground;
            InitializedNotInitializedForeground = NotHomedForeground;
            // DKM 2012-04-03 known / unknown bucket
            CurrentBucketText = UnknownBucketString;
            CurrentBucketBackground = UnknownBucketBackground;
            CurrentBucketForeground = UnknownBucketForeground;
            _timer = new DispatcherTimer();
            _timer.Interval = new TimeSpan( 0, 0, 1);
            _timer.Tick += new EventHandler(_timer_Tick);
            _timer.IsEnabled = true;

            // register event handlers
            _hig.InitializeComplete += new EventHandler(_hig_InitializeComplete);
            _hig.InitializeError += new EventHandler(_hig_InitializeError);
            _hig.HomeComplete += new EventHandler(_hig_HomeComplete);
            _hig.HomeError += new EventHandler(_hig_HomeError);
            _hig.OpenShieldComplete += new EventHandler(_hig_OpenShieldComplete);
            _hig.OpenShieldError += new EventHandler(_hig_OpenShieldError);
            _hig.SpinComplete += new EventHandler(_hig_SpinComplete);
            _hig.SpinError += new EventHandler(_hig_SpinError);
            _hig.SpinTimeRemainingUpdated += new EventHandler(_hig_SpinTimeRemainingUpdated);
            _hig.ForcedDisconnection += new EventHandler(_hig_ForcedDisconnection);
        }

        private void InitializeCommands()
        {
            SpinCommand = new SimpleRelayCommand( ExecuteSpin, () => { return _hig.Idle; });
            OpenShieldToBucket1Command = new SimpleRelayCommand( () => { ExecuteOpenShield( 0); }, () => { return _hig.Idle; });
            OpenShieldToBucket2Command = new SimpleRelayCommand( () => { ExecuteOpenShield( 1); }, () => { return _hig.Idle; });
            HomeCommand = new SimpleRelayCommand( ExecuteHome, () => { return _hig.Idle; });
            AbortSpinCommand = new SimpleRelayCommand( ExecuteAbortSpin);
            InitializeCommand = new SimpleRelayCommand( ExecuteInitialize, () => { return _hig.Idle; });
            CloseCommand = new SimpleRelayCommand( ExecuteClose, () => { return _hig.Idle; });
            ShowCycleTimeCommand = new SimpleRelayCommand( ExecuteShowCycleTime, () => { return _hig.Idle; });
            FirstTimeSetupCommand = new SimpleRelayCommand( ExecuteFirstTimeSetup, () => { return _hig.Idle; });
            PackForShipmentCommand = new SimpleRelayCommand( ExecutePackForShipment, () => { return _hig.Idle; });
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            bool is_homed = _hig.IsHomed;
            HomedNotHomedText = is_homed ? HomedString : NotHomedString;
            HomedNotHomedBackground = is_homed ? HomedBackground : NotHomedBackground;
            HomedNotHomedForeground = is_homed ? HomedForeground : NotHomedForeground;
            bool is_connected = _hig.IsConnected;
            InitializedNotInitializedText = is_connected ? InitializedString : NotInitializedString;
            // DKM 2012-04-02 using same consts as homing properties
            InitializedNotInitializedBackground = is_connected ? HomedBackground : NotHomedBackground;
            InitializedNotInitializedForeground = is_connected ? HomedForeground : NotHomedForeground;
            // DKM 2012-04-03 known / unknown position
            if( _hig.CurrentBucket == 0) {
                CurrentBucketText = UnknownBucketString;
                CurrentBucketBackground = UnknownBucketBackground;
                CurrentBucketForeground = UnknownBucketForeground;
            } else {
                CurrentBucketText = String.Format( "At Bucket {0}", _hig.CurrentBucket);
                CurrentBucketBackground = KnownBucketBackground;
                CurrentBucketForeground = KnownBucketForeground;
            }
        }

        void _hig_ForcedDisconnection(object sender, EventArgs e)
        {
            _main_dispatcher.Invoke( new Action( () => { MessageBox.Show( String.Format( "HiG #{0} was disconnected.  Please check power, re-initialize, and re-home.", _index + 1)); } ));
        }

        public void SetHiGIndex( int index)
        {
            _index = index;
        }

        public void Close()
        {
            _hig.Close();
        }

        void _hig_SpinError(object sender, EventArgs e)
        {
            TimeOverlayVisibility = System.Windows.Visibility.Hidden;
            Dispatcher.Invoke( new Action( () => { MessageBox.Show( "Spin error: " + (e as BioNex.HiGIntegration.HiG.ErrorEventArgs).Reason); }));
        }

        void _hig_SpinComplete(object sender, EventArgs e)
        {
            TimeOverlayVisibility = System.Windows.Visibility.Hidden;
            HiG.SpinCompleteEventArgs args = e as HiG.SpinCompleteEventArgs;
            string message = String.Format( "HiG #{0} Spin complete", _index + 1);
            if( args != null)
                message += String.Format( "\r\naccel time = {0:0.00}s, cruise time = {1:0.00}s, decel time = {2:0.00}s", args.AccelTimeInSeconds, args.CruiseTimeInSeconds, args.DecelTimeInSeconds);
            Dispatcher.Invoke( new Action( () => { MessageBox.Show( message); } ));
        }

        void _hig_OpenShieldError(object sender, EventArgs e)
        {
            Dispatcher.Invoke( new Action( () => { MessageBox.Show( "OpenShield error: " + (e as BioNex.HiGIntegration.HiG.ErrorEventArgs).Reason); }));
        }

        void _hig_OpenShieldComplete(object sender, EventArgs e)
        {
            Dispatcher.Invoke( new Action( () => { MessageBox.Show( String.Format( "HiG #{0} OpenShield complete", _index + 1)); }));
        }

        void _hig_HomeError(object sender, EventArgs e)
        {
            Dispatcher.Invoke( new Action( () => { MessageBox.Show( "Home error: " + (e as BioNex.HiGIntegration.HiG.ErrorEventArgs).Reason); }));
        }

        void _hig_HomeComplete(object sender, EventArgs e)
        {
            Dispatcher.Invoke( new Action( () => { MessageBox.Show( String.Format( "HiG #{0} Home complete", _index + 1)); }));
        }

        void _hig_InitializeError(object sender, EventArgs e)
        {
            Dispatcher.Invoke( new Action( () => { MessageBox.Show( "Initialization error: " + (e as BioNex.HiGIntegration.HiG.ErrorEventArgs).Reason); }));
        }

        void _hig_InitializeComplete(object sender, EventArgs e)
        {
            Dispatcher.Invoke( new Action( () => { 
                MessageBox.Show( String.Format( "HiG #{0} Initialization complete", _index + 1)); 
                // bail on firmware update if the HiG is being simulated
                if( _simulate == true)
                    return;
                
                // now check the HiG version number against the embedded one.  If the device firmware is < the embedded resource,
                // then prompt the user to see if he wants to update the firmware in the device.
                var version = _hig.FirmwareVersion;
                // lame, we have to parse out the major / minor version information because of the way I report firmware
                short shield_major, shield_minor, spindle_major, spindle_minor;
                BioNex.Hig.HigUtils.ParseCombinedFirmwareString(version, out shield_major, out shield_minor, out spindle_major, out spindle_minor);
                
                // this is where we set the current version stored in the assembly.  I didn't want to spend the time right now
                // parsing the embedded resources to figure out the latest version.
                short current_shield_major = 1;
                short current_shield_minor = 4;
                short current_spindle_major = 1;
                short current_spindle_minor = 6;
                // if the version number is < the latest embedded in this assembly, offer to update it
                bool newer_shield_firmware = NewerFirmwareAvailable( current_shield_major, current_shield_minor, shield_major, shield_minor);
                bool newer_spindle_firmware = NewerFirmwareAvailable( current_spindle_major, current_spindle_minor, spindle_major, spindle_minor);
                if( newer_shield_firmware || newer_spindle_firmware) {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("There is newer firmware available for this device.  ");
                    if (newer_shield_firmware)
                        sb.Append(String.Format("The current shield version is {0}.{1} and the latest is {2}.{3}.  ", shield_major, shield_minor, current_shield_major, current_shield_minor));
                    if (newer_spindle_firmware)
                        sb.Append(String.Format("The current spindle version is {0}.{1} and the latest is {2}.{3}.  ", spindle_major, spindle_minor, current_spindle_major, current_spindle_minor));
                    sb.Append(String.Format("Would you like to update {0} now?", newer_shield_firmware && newer_spindle_firmware ? "them" : "it"));
                    MessageBoxResult answer = MessageBox.Show( sb.ToString(), "Update firmware?", MessageBoxButton.YesNo);
                    if( answer == MessageBoxResult.Yes) {
                        try {
                            BioNex.HiGIntegration.HiG hig = _hig as BioNex.HiGIntegration.HiG;
                            if( hig == null) {
                                MessageBox.Show( "Could not update firmware because the integration driver does not support this feature.");
                                return;
                            }
                            if( newer_shield_firmware) {                            
                                string path;
                                if (ExtractFirmware(113, current_shield_major, current_shield_minor, out path))
                                    hig.UpdateShieldFirmware(path);
                                else
                                    MessageBox.Show("Could not update shield firmware because the firmware could not be extracted.");
                            }
                            if (newer_spindle_firmware) {
                                string path;
                                if (ExtractFirmware(115, current_spindle_major, current_spindle_minor, out path))
                                    hig.UpdateSpindleFirmware(path);
                                else
                                    MessageBox.Show("Could not update spindle firmware because the firmware could not be extracted.");
                            }

                            // DKM 2012-02-14 need to prompt user to re-initialize -- can't call initialize from here because we're
                            //                still technically in the Initializing state.  Didn't want to allow Initialize to get
                            //                called again for firmware updates because this would allow the developer to call
                            //                Initialize multiple times simultaneously.
                            MessageBox.Show( "Firmware updated successfully.  Please re-initialize the device.");
                        } catch( Exception ex) {
                            MessageBox.Show( String.Format( "Firmware update failed: {0}.  You can initialize the device and try again."), ex.Message);
                        }
                    }
                }
            }));
        }

        // basic idea: http://www.dotnetscraps.com/dotnetscraps/post/Insert-any-binary-file-in-C-assembly-and-extract-it-at-runtime.aspx
        // addressing embedded path properly: http://www.codeproject.com/KB/dotnet/embeddedresources.aspx
        private bool ExtractFirmware( int axis_id, short major, short minor, out string firmware_path)
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string temp_path = System.IO.Path.GetTempPath();
            firmware_path = String.Format("{0}\\{1}.sw", temp_path, axis_id);
            var input = assembly.GetManifestResourceStream(String.Format("HiGIntegrationTestApp.Firmware.{0}_{1}.{2}.sw", axis_id, major, minor));
            // only overwrite the file if a matching version exists!
            if (input != null)
            {
                if (System.IO.File.Exists(firmware_path))
                    System.IO.File.Delete(firmware_path);
                var output = System.IO.File.Open(firmware_path, System.IO.FileMode.CreateNew);
                CopyStream(input, output);
                input.Dispose();
                output.Dispose();
            }

            return input != null;
        }

        private void CopyStream(System.IO.Stream input, System.IO.Stream output)
        {
            byte[] buffer = new byte[32768];
            while (true)
            {
                int read = input.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                    return;
                output.Write(buffer, 0, read);
            }
        }

        /// <summary>
        /// Compares device's major and minor firmware information to the latest version stored in the assembly
        /// </summary>
        /// <param name="device_major"></param>
        /// <param name="device_minor"></param>
        /// <returns></returns>
        private bool NewerFirmwareAvailable( short current_major, short current_minor, short device_major, short device_minor)
        {
            return current_major > device_major || (current_major == device_major && current_minor > device_minor);
        }

        void _hig_SpinTimeRemainingUpdated(object sender, EventArgs e)
        {
            SpinTimeUpdatedEventArgs args = e as SpinTimeUpdatedEventArgs;
            if( args == null)
                return;
            TimeOverlayString = String.Format( "{0:0.0}", args.SecondsRemaining);
            TimeOverlayVisibility = args.SecondsRemaining == 0 ? Visibility.Hidden : Visibility.Visible;
        }

        void FirstTimeSetup_InitializeComplete(object sender, EventArgs e)
        {
            _first_time_setup_initialize_complete_event.Set();            
        }

        void FirstTimeSetup_InitializeError(object sender, EventArgs e)
        {
            _first_time_setup_initialize_error_event.Set();
        }

        void FirstTimeSetup_HomeComplete(object sender, EventArgs e)
        {
            _first_time_setup_home_complete_event.Set();
        }

        void FirstTimeSetup_HomeError(object sender, EventArgs e)
        {
            _first_time_setup_home_error_event.Set();
        }

        internal void CacheAdapterIdAndSimulationFlag()
        {
            //_adapter_id = adapter_id.Text;
            _adapter_id = int.Parse( AvailableAdapterIds.CurrentItem.ToString());
            _simulate = simulate.IsChecked;
        }

        internal void ExecuteInitialize()
        {
            // DKM 2012-04-04 the try/catch block is to catch errors in blocking mode
            try {
                _main_dispatcher.Invoke( new Action( () => {
                    CacheAdapterIdAndSimulationFlag();
                    blocking.IsEnabled = false;
                    _hig.Blocking = blocking.IsChecked.Value;
                }));
                _hig.Initialize( String.Format("HiG{0}", _index + 1), _adapter_id.ToString(), _simulate ?? false);
            } catch( Exception ex) {
                MessageBox.Show( "Could not initialize: " + ex.Message);
            }
        }

        internal void ExecuteFirstTimeSetup()
        {
            try
            {
                IEnumerable<Wizard.WizardStep> steps = CreateFirstTimeSetupSteps();
                Wizard wiz = new Wizard(String.Format("HiG #{0} First-Time Setup", _index + 1), steps, this.Dispatcher);
                wiz.Top = _owner.Top;
                wiz.Width = 600;
                wiz.Left = _owner.Left + _owner.Width;
                wiz.Show();
                wiz.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Could not run the first-time setup wizard: {0}", ex.Message));
            }
        }

        internal void ExecutePackForShipment()
        {
            try
            {
                IEnumerable<Wizard.WizardStep> steps = CreatePackForShipmentSteps();
                Wizard wiz = new Wizard(String.Format("HiG #{0} Pack for Shipment Steps", _index + 1), steps, this.Dispatcher);
                wiz.Top = _owner.Top;
                wiz.Width = 600;
                wiz.Left = _owner.Left + _owner.Width;
                wiz.Show();
                wiz.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Could not run the first-time setup wizard: {0}", ex.Message));
            }
        }

#region First Time Setup Steps
        internal IEnumerable<Wizard.WizardStep> CreateFirstTimeSetupSteps()
        {
            Func<bool> Initialize = new Func<bool>(() =>
            {
                _hig.InitializeComplete += new EventHandler(FirstTimeSetup_InitializeComplete);
                _hig.InitializeError += new EventHandler(FirstTimeSetup_InitializeError);
                ExecuteInitialize();
                int which = WaitHandle.WaitAny(new WaitHandle[] { _first_time_setup_initialize_complete_event, _first_time_setup_initialize_error_event });
                _hig.InitializeComplete -= new EventHandler(FirstTimeSetup_InitializeComplete);
                _hig.InitializeError -= new EventHandler(FirstTimeSetup_InitializeError);
                if (which == 0)
                    return true;
                else
                    return false;
            });

            Func<bool> HomeShield = new Func<bool>(() =>
            {
                _hig.HomeComplete += new EventHandler(FirstTimeSetup_HomeComplete);
                _hig.HomeError += new EventHandler(FirstTimeSetup_HomeError);
                _hig.HomeShield( true);
                int which = WaitHandle.WaitAny(new WaitHandle[] { _first_time_setup_home_complete_event, _first_time_setup_home_error_event });
                _hig.HomeComplete -= new EventHandler(FirstTimeSetup_HomeComplete);
                _hig.HomeError -= new EventHandler(FirstTimeSetup_HomeError);
                if (which == 0)
                    return true;
                else
                    return false;
            });

            Func<bool> Home = new Func<bool>(() =>
            {
                _hig.HomeComplete += new EventHandler(FirstTimeSetup_HomeComplete);
                _hig.HomeError += new EventHandler(FirstTimeSetup_HomeError);
                _hig.Home();
                int which = WaitHandle.WaitAny(new WaitHandle[] { _first_time_setup_home_complete_event, _first_time_setup_home_error_event });
                _hig.HomeComplete -= new EventHandler(FirstTimeSetup_HomeComplete);
                _hig.HomeError -= new EventHandler(FirstTimeSetup_HomeError);
                if (which == 0)
                    return true;
                else
                    return false;
            });

            List<Wizard.WizardStep> steps = new List<Wizard.WizardStep>();
            steps.Add(new Wizard.WizardStep("Connect power cord, disable button, and USB cable, and click Next when ready", null, true));
            steps.Add(new Wizard.WizardStep("Turn on power to HiG and click Next when ready", null, true));
            steps.Add(new Wizard.WizardStep("Initializing...", Initialize, false));
            steps.Add(new Wizard.WizardStep("Homing shield...", HomeShield, false));
            steps.Add(new Wizard.WizardStep("Slide the wooden stick under the hinged door to keep it open, and click Next when ready", null, true));
            steps.Add(new Wizard.WizardStep("Turn off power, unplug the AC cord, and click Next when ready", null, true));
            steps.Add(new Wizard.WizardStep("Remove foam block from the bucket and click Next when ready", null, true));
            steps.Add(new Wizard.WizardStep("Remove wooden stick from the bucket and click Next when ready", null, true));
            steps.Add(new Wizard.WizardStep("Remove wooden stick propping the hinged door open and click Next when ready", null, true));
            steps.Add(new Wizard.WizardStep("Connect power cord, turn on power, then click Next when ready", null, true));
            steps.Add(new Wizard.WizardStep("Rehoming device...", Home, false));
            steps.Add(new Wizard.WizardStep("Done!  You may now close this dialog.", null, false));
            return steps;
        }
#endregion

#region Pack For Shipment Steps
        internal IEnumerable<Wizard.WizardStep> CreatePackForShipmentSteps()
        {
            Func<bool> Initialize = new Func<bool>(() =>
            {
                _hig.InitializeComplete += new EventHandler(FirstTimeSetup_InitializeComplete);
                _hig.InitializeError += new EventHandler(FirstTimeSetup_InitializeError);
                ExecuteInitialize();
                int which = WaitHandle.WaitAny(new WaitHandle[] { _first_time_setup_initialize_complete_event, _first_time_setup_initialize_error_event });
                _hig.InitializeComplete -= new EventHandler(FirstTimeSetup_InitializeComplete);
                _hig.InitializeError -= new EventHandler(FirstTimeSetup_InitializeError);
                if (which == 0)
                    return true;
                else
                    return false;
            });

            Func<bool> HomeShield = new Func<bool>(() =>
            {
                _hig.HomeComplete += new EventHandler(FirstTimeSetup_HomeComplete);
                _hig.HomeError += new EventHandler(FirstTimeSetup_HomeError);
                _hig.HomeShield( false);
                int which = WaitHandle.WaitAny(new WaitHandle[] { _first_time_setup_home_complete_event, _first_time_setup_home_error_event });
                _hig.HomeComplete -= new EventHandler(FirstTimeSetup_HomeComplete);
                _hig.HomeError -= new EventHandler(FirstTimeSetup_HomeError);
                if (which == 0)
                    return true;
                else
                    return false;
            });

            Func<bool> Home = new Func<bool>(() =>
            {
                _hig.HomeComplete += new EventHandler(FirstTimeSetup_HomeComplete);
                _hig.HomeError += new EventHandler(FirstTimeSetup_HomeError);
                _hig.Home();
                int which = WaitHandle.WaitAny(new WaitHandle[] { _first_time_setup_home_complete_event, _first_time_setup_home_error_event });
                _hig.HomeComplete -= new EventHandler(FirstTimeSetup_HomeComplete);
                _hig.HomeError -= new EventHandler(FirstTimeSetup_HomeError);
                if (which == 0)
                    return true;
                else
                    return false;
            });

            List<Wizard.WizardStep> steps = new List<Wizard.WizardStep>();
            steps.Add(new Wizard.WizardStep("Turn on power to HiG and click Next when ready", null, true));
            steps.Add(new Wizard.WizardStep("Initializing...", Initialize, false));
            steps.Add(new Wizard.WizardStep("Homing...", Home, false));
            steps.Add(new Wizard.WizardStep("Slide the wooden stick under the hinged door to keep it open, and click Next when ready", null, true));
            steps.Add(new Wizard.WizardStep("Turn off power, unplug the AC cord, and click Next when ready", null, true));
            steps.Add(new Wizard.WizardStep("Insert packing foam block into the bucket and click Next when ready", null, true));
            steps.Add(new Wizard.WizardStep("Insert wooden stick into the bucket and click Next when ready", null, true));
            steps.Add(new Wizard.WizardStep("Remove wooden stick from under the hinged door and click Next when ready", null, true));
            steps.Add(new Wizard.WizardStep("Plug in AC cord, turn on power, and click Next when ready", null, true));
            steps.Add(new Wizard.WizardStep("Closing shield...", HomeShield, false));
            steps.Add(new Wizard.WizardStep("Done!  You may now close this dialog.", null, false));
            return steps;
        }
#endregion

        internal void ExecuteOpenShield( int bucket_index)
        {
            try {
                _hig.OpenShield( bucket_index);
            } catch( Exception ex) {
                MessageBox.Show( "Could not open shield: " + ex.Message);
            }
        }

        internal void ExecuteSpin()
        {
            try {
                _hig.Spin( double.Parse(g.Text), double.Parse(accel.Text), double.Parse(decel.Text), double.Parse(time.Text));
            } catch( Exception ex) {
                MessageBox.Show( "Could not spin: " + ex.Message);
            }
        }

        internal void ExecuteHome()
        {
            try {
                _hig.Home();
            } catch( Exception ex) {
                MessageBox.Show( "Could not home: " + ex.Message);
            }
        }

        internal void ExecuteAbortSpin()
        {
            _hig.AbortSpin();
        }

        internal void ExecuteClose()
        {
            _hig.Close();
            blocking.IsEnabled = true;
        }

        internal void ExecuteShowCycleTime()
        {
            double estimate = _hig.GetEstimatedCycleTime( double.Parse(g.Text), double.Parse(accel.Text), double.Parse(decel.Text), double.Parse(time.Text));
            MessageBox.Show( String.Format( "Estimated time is {0:0.00}s", estimate));
        }

        private void GetFirmwareVersion_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show( _hig.FirmwareVersion);
        }

        private void GetSerialNumber_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(_hig.SerialNumber);
        }

        private void GetLastError_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show( _hig.LastError);
        }

        /*
        private void CloseShield_Click(object sender, RoutedEventArgs e)
        {
            _hig.CloseShield();
        }
         */

        private void ShowDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            _hig.ShowDiagnostics( false);
        }

        private string _time_overlay_string;
        public string TimeOverlayString
        {
            get { return _time_overlay_string; }
            set {
                _time_overlay_string = value;
                OnPropertyChanged( "TimeOverlayString");
            }
        }

        private Visibility _time_overlay_visibility;
        public Visibility TimeOverlayVisibility
        {
            get { return _time_overlay_visibility; }
            set {
                _time_overlay_visibility = value;
                OnPropertyChanged( "TimeOverlayVisibility");
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion

        private void EditEepromSettings_Click(object sender, RoutedEventArgs e)
        {
            HiG hig = _hig as HiG;
            if( hig == null)
                return;
            EepromSettings dlg = new EepromSettings( hig);
            dlg.ShowDialog();
        }
    }
}
