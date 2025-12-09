using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using BioNex.HivePrototypePlugin;
using BioNex.Plugins.Dock;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.IError;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.ThreadsafeMessenger;
using BioNex.Shared.Utils;
using BioNex.SynapsisPrototype.ViewModel;
using log4net;
using SMTPReporting;
using BioNex.PlateMover;

namespace BioNex.HiveIntegration
{
    /// <summary>
    /// Exception class to report failed barcode to labware decode 
    /// </summary>
    public class CouldNotDecodeLabwareFromBarcodeException : Exception
    {
        public CouldNotDecodeLabwareFromBarcodeException(string msg) : base(msg) { }
        public CouldNotDecodeLabwareFromBarcodeException(string msg, Exception e) : base(msg, e) { }
    }

    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    [Export(typeof(ICustomerGUI))]
    public partial class IntegrationGui : UserControl, ICustomerGUI, DockExternalCommunications, IHasSystemPanel, INotifyPropertyChanged
    {
        [Import("MainDispatcher")]
        Dispatcher _dispatcher { get; set; }
        [Import(typeof(ILabwareDatabase))]
        internal ILabwareDatabase _labware_db { get; set; }

        private ILog _log = LogManager.GetLogger(typeof(IntegrationGui));
        public ExternalDataRequesterInterface DataRequestInterface { get; set; }
        internal BehaviorEngine _engine { get; set; }
        internal ThreadsafeMessenger _messenger { get; set; }
        private Configuration _configuration { get; set; }

        // devices that we need to move plates from BioCel to Hive storage
        internal Dictionary<string, DockMonitorPlugin> _docks { get; set; }
        /// <summary>
        /// Reference to the Hive robot specified in HiveIntegrationConfig.xml
        /// </summary>
        internal RobotInterface _robot { get; set; }
        /// <summary>
        /// Reference to the PlateMover specified in HiveIntegrationConfig.xml
        /// </summary>
        internal PlateMoverPlugin _platemover { get; set; }

        private Thread _heartbeat_thread { get; set; }
        //private ManualResetEvent _stop_heartbeat_thread { get; set; }

        private Thread _fate_processing_thread;
        private ManualResetEvent _stop_fate_processing_thread;
        private FateProcessing _fate_processing_db;

        private HiveIntegration.HiveServer _hive_api;
        private HiveServerImpl _hive_server_impl;

        private BioNex.Shared.Utils.HourglassWindow _hg;
        private BioNex.CustomerGUIPlugins.SystemPanel _system_panel;

        /// <summary>
        /// _assigned_fates: dock name to string map to track the fate that is assigned to the cart currently docked.  
        /// Fate assigned to a Dock is cleared when cart is docked or undocked
        /// Fates assigned is set when a new plate fate is assigned and there is a cart with no assigned fate
        /// </summary>
        private Dictionary<string, string> _assigned_fates = new Dictionary<string, string>();
        // honey badger don't care for arrays.  we iterate over available device manager entries.
        private List<string> _dock_names = new List<string>();

        internal HiveStatus Status = new HiveStatus();

        // UI for fates -- MY GOD I NEED TO LEARN HOW TO ARRAY PROPERTIES
        // DKM 2011-06-03  Me too.
        public string FateDock1
        {
            get { lock (_assigned_fates) { string fate = _assigned_fates[_dock_names[1]]; return string.IsNullOrEmpty(fate) ? "no fate assigned" : fate; } }
            set { OnPropertyChanged("FateDock1"); FateDock1Color = FateDock1Color; FateDock1TextColor = FateDock1TextColor; }
        }
        public Brush FateDock1Color
        {
            get { lock (_assigned_fates) { string fate = _assigned_fates[_dock_names[1]]; return string.IsNullOrEmpty(fate) ? Brushes.LightGray : Brushes.DarkGreen; } }
            set { OnPropertyChanged("FateDock1Color"); }
        }
        public Brush FateDock1TextColor
        {
            get { lock (_assigned_fates) { string fate = _assigned_fates[_dock_names[1]]; return string.IsNullOrEmpty(fate) ? Brushes.Black : Brushes.Goldenrod; } }
            set { OnPropertyChanged("FateDock1TextColor"); }
        }
        public string FateDock2
        {
            get { lock (_assigned_fates) { string fate = _assigned_fates[_dock_names[0]]; return string.IsNullOrEmpty(fate) ? "no fate assigned" : fate; } }
            set { OnPropertyChanged("FateDock2"); FateDock2Color = FateDock2Color; FateDock2TextColor = FateDock2TextColor; }
        }
        public Brush FateDock2Color
        {
            get { lock (_assigned_fates) { string fate = _assigned_fates[_dock_names[0]]; return string.IsNullOrEmpty(fate) ? Brushes.LightGray : Brushes.DarkGreen; } }
            set { OnPropertyChanged("FateDock2Color"); }
        }
        public Brush FateDock2TextColor
        {
            get { lock (_assigned_fates) { string fate = _assigned_fates[_dock_names[0]]; return string.IsNullOrEmpty(fate) ? Brushes.Black : Brushes.Goldenrod; } }
            set { OnPropertyChanged("FateDock2TextColor"); }
        }

        enum SpecialFate { Hold, Trash };
        private string[] _special_fates = Enum.GetNames(typeof(SpecialFate));

        internal IError ErrorInterface { get; set; }

        private SMTPReporter _mailer { get; set; }

        #region databinding members
        // DKM 2012-05-17 not sure I can use this yet -- need a way to query server for connection status, because Phase 1
        //                relied on Ping()ing the VWorks plugin, which we don't want to do in this case.
        private Brush _rpcclient_connection_status_color;
        public Brush RpcClientConnectionStatusColor
        {
            get { return _rpcclient_connection_status_color; }
            set
            {
                _rpcclient_connection_status_color = value;
                OnPropertyChanged("RpcClientConnectionStatusColor");
            }
        }

        private Brush _storage_full_status_color;
        public Brush StorageFullStatusColor
        {
            get { return _storage_full_status_color; }
            set
            {
                _storage_full_status_color = value;
                OnPropertyChanged("StorageFullStatusColor");
            }
        }

        private Brush _storage_empty_status_color;
        public Brush StorageEmptyStatusColor
        {
            get { return _storage_empty_status_color; }
            set
            {
                _storage_empty_status_color = value;
                OnPropertyChanged("StorageEmptyStatusColor");
            }
        }

        private Brush _idle_color;
        public Brush IdleColor
        {
            get { return _idle_color; }
            set
            {
                _idle_color = value;
                OnPropertyChanged("IdleColor");
            }
        }

        private Brush _loading_color;
        public Brush LoadingColor
        {
            get { return _loading_color; }
            set
            {
                _loading_color = value;
                OnPropertyChanged("LoadingColor");
            }
        }

        private Brush _unloading_color;
        public Brush UnLoadingColor
        {
            get { return _unloading_color; }
            set
            {
                _unloading_color = value;
                OnPropertyChanged("UnLoadingColor");
            }
        }

        private Brush _moving_color;
        public Brush MovingColor
        {
            get { return _moving_color; }
            set
            {
                _moving_color = value;
                OnPropertyChanged("MovingColor");
            }
        }

        private Brush _reinventorying_color;
        public Brush ReinventoryingColor
        {
            get { return _reinventorying_color; }
            set
            {
                _reinventorying_color = value;
                OnPropertyChanged("ReinventoryingColor");
            }
        }

        private Brush _undocking_color;
        public Brush UndockingColor
        {
            get { return _undocking_color; }
            set
            {
                _undocking_color = value;
                OnPropertyChanged("UndockingColor");
            }
        }

        private Brush _shutdown_color;
        public Brush ShutdownColor
        {
            get { return _shutdown_color; }
            set
            {
                _shutdown_color = value;
                OnPropertyChanged("ShutdownColor");
            }
        }

        private Brush _paused_color;
        public Brush PausedColor
        {
            get { return _paused_color; }
            set
            {
                _paused_color = value;
                OnPropertyChanged("PausedColor");
            }
        }

        private Brush _homing_color;
        public Brush HomingColor
        {
            get { return _homing_color; }
            set
            {
                _homing_color = value;
                OnPropertyChanged("HomingColor");
            }
        }

        private Brush _docking_color;
        public Brush DockingColor
        {
            get { return _docking_color; }
            set
            {
                _docking_color = value;
                OnPropertyChanged("DockingColor");
            }
        }

        public string ResumeButtonText
        {
            // DKM 2012-05-17 for Hive API, always use "Resume" for now
            //get { return _engine.WaitingForGo ? "Go Live" : "Resume"; }
            get { return "Resume"; }
        }

        private Brush _idle_indicator_text_color;
        public Brush IdleIndicatorTextColor
        {
            get { return _idle_indicator_text_color; }
            set
            {
                _idle_indicator_text_color = value;
                OnPropertyChanged("IdleIndicatorTextColor");
            }
        }

        private Brush _homing_indicator_text_color;
        public Brush HomingIndicatorTextColor
        {
            get { return _homing_indicator_text_color; }
            set
            {
                _homing_indicator_text_color = value;
                OnPropertyChanged("HomingIndicatorTextColor");
            }
        }

        private Brush _loading_indicator_text_color;
        public Brush LoadingIndicatorTextColor
        {
            get { return _loading_indicator_text_color; }
            set
            {
                _loading_indicator_text_color = value;
                OnPropertyChanged("LoadingIndicatorTextColor");
            }
        }

        private Brush _unloadingIndicatorTextColor;
        public Brush UnloadingIndicatorTextColor
        {
            get { return _unloadingIndicatorTextColor; }
            set
            {
                _unloadingIndicatorTextColor = value;
                OnPropertyChanged("UnloadingIndicatorTextColor");
            }
        }

        private Brush _moving_indicator_text_color;
        public Brush MovingIndicatorTextColor
        {
            get { return _moving_indicator_text_color; }
            set
            {
                _moving_indicator_text_color = value;
                OnPropertyChanged("MovingIndicatorTextColor");
            }
        }

        private Brush _reinventory_indicator_text_color;
        public Brush ReinventoryingIndicatorTextColor
        {
            get { return _reinventory_indicator_text_color; }
            set
            {
                _reinventory_indicator_text_color = value;
                OnPropertyChanged("ReinventoryingIndicatorTextColor");
            }
        }

        private Brush _docking_indicator_text_color;
        public Brush DockingIndicatorTextColor
        {
            get { return _docking_indicator_text_color; }
            set
            {
                _docking_indicator_text_color = value;
                OnPropertyChanged("DockingIndicatorTextColor");
            }
        }

        private Brush _brush;
        public Brush UndockingIndicatorTextColor
        {
            get { return _brush; }
            set
            {
                _brush = value;
                OnPropertyChanged("UndockingIndicatorTextColor");
            }
        }
        

        #endregion

        [ImportingConstructor]
        public IntegrationGui([Import] ExternalDataRequesterInterface data_request_interface,
                                 [Import] ILabwareDatabase labware_database,
                                 [Import] IError error_interface,
                                 [Import] SMTPReporter mailer,
                                 [Import("SynapsisViewModel")] SynapsisViewModel synapsis_viewmodel)
        {
            InitializeComponent();
            InitializeCommands();
            this.DataContext = this;

            ResetBehaviorEngineColors();

            LoadDllConfiguration();
            ErrorInterface = error_interface;

            // adjust Synapsis Main Window
            synapsis_viewmodel.MenuVisibility = Visibility.Collapsed;
            synapsis_viewmodel.CustomerGUITabVisibility = Visibility.Collapsed;
            synapsis_viewmodel.SetPauseRowMinHeightAndTextSize(80, 30);


            RpcClientConnectionStatusColor = Brushes.LightGray;
            StorageFullStatusColor = StorageEmptyStatusColor = Brushes.LightGray;

            //-----------------------------------------------------------------
            // first, let's figure out if we've got everything we need to operate.  This includes:
            // 1. docks
            // 2. robot
            // 3. plate mover
            DataRequestInterface = data_request_interface;
            var devices = DataRequestInterface.GetDeviceInterfaces();
            // 1
            _docks = new Dictionary<string, DockMonitorPlugin>();
            // first handle docked / undocked events
            var temp = from x in devices where x is DockMonitorPlugin select x;
            foreach (var x in temp)
            {
                DockMonitorPlugin dock = x as DockMonitorPlugin;
                string dock_name = x.Name;
                _docks.Add(dock_name, dock);
                lock (_assigned_fates) { _assigned_fates[dock_name] = ""; }
                _dock_names.Add(dock_name);
                dock.Docked += new DockEventHandler(dock_Docked);
                dock.DockingError += new DockEventHandler(dock_DockingError);
                dock.Undocked += new DockEventHandler(dock_Undocked);
                dock.UndockingError += new DockEventHandler(dock_UndockingError);

                dock.ReinventoryComplete += new EventHandler(All_ReinventoryComplete);
            }

            // DKM 2011-06-15 reversed the docks
            if (_docks.Count() == 1)
                DockPanel1.Children.Add(_docks[_dock_names[0]].GetDiagnosticsPanel());
            else if (_docks.Count() == 2)
            {
                DockPanel1.Children.Add(_docks[_dock_names[1]].GetDiagnosticsPanel());
                DockPanel2.Children.Add(_docks[_dock_names[0]].GetDiagnosticsPanel());
            }
            else
            {
                Debug.Assert(false, "There can only be one or two docks in this system.");
            }
            foreach (var kvp in _docks)
                kvp.Value.SetExternalCommunications(this);


            // 2
            Debug.Assert(_configuration.RobotDeviceName != null, "You need to set the UpstackingRobotName in the config file");
            _robot = (from x in devices where x.Name == _configuration.RobotDeviceName select x).FirstOrDefault() as RobotInterface;
            Debug.Assert(_robot != null, String.Format("Could not find a robot named '{0}' in the device database", _configuration.RobotDeviceName));
            _platemover = (from x in devices where x.Name == _configuration.PlateMoverDeviceName select x).FirstOrDefault() as PlateMoverPlugin;
            Debug.Assert(_platemover != null, String.Format("Could not find a plate mover named '{0}' in the device database", _configuration.PlateMoverDeviceName));
            // register inventory complete handler for the hive robot separately
            var hive = (_robot as PlateStorageInterface);
            hive.ReinventoryComplete += new EventHandler(All_ReinventoryComplete);
            hive.ReinventoryBegin += new EventHandler(StaticStorage_ReinventoryBegin);
            // 3
            //-----------------------------------------------------------------

            _messenger = new ThreadsafeMessenger();
            _engine = new BehaviorEngine(this);

            try
            {                
                // DKM 2012-05-11 this is bad design, but let's start here first by allowing the server implementation
                //                to call back directly into the GUI.  Later, we can refactor and make HiveServerImpl
                //                the model for the GUI.
                _hive_server_impl = new HiveServerImpl( this);
                _hive_api = new HiveIntegration.HiveServer( _hive_server_impl, _configuration.ListenerPort);

                _hive_server_impl.ClientConnected += new EventHandler(_hive_server_impl_ClientConnected);
                _hive_server_impl.ClientDisconnected += new EventHandler(_hive_server_impl_ClientDisconnected);
            }
            catch (Exception ex)
            {
                _log.Info(String.Format("Could not configure RPC: {0}.  Please check settings in plugins\\HiveIntegrationConfig.xml", ex.Message));
            }

            _system_panel = new BioNex.CustomerGUIPlugins.SystemPanel();
            _system_panel.DataContext = this;

            /*
            // start the heartbeat thread to monitor JEMS connection
            _stop_heartbeat_thread = new ManualResetEvent(false);
            _heartbeat_thread = new Thread(Heartbeat);
            //_heartbeat_thread.IsBackground = true;  --> we call JOIN on this, so it should NOT be a background thread, otherwise we AbExit as the thread is aborted
            _heartbeat_thread.Name = "TODO heartbeat test";
            _heartbeat_thread.Start();
             */

            // start the fate monitor thread
            _fate_processing_db = new FateProcessing("fate_processing_db.s3db");
            _stop_fate_processing_thread = new ManualResetEvent(false);
            _fate_processing_thread = new Thread(PendingFatesThread);
            _fate_processing_thread.Name = "Pending fates";
            _fate_processing_thread.Start();

            // grab the mailer and tie it into the Synapsis ErrorEvent
            _mailer = mailer;
            _mailer.HiveName = _configuration.HiveName;
            error_interface.ErrorEvent += (object sender, ErrorData error) => _mailer.SendMessage("Error", string.Format("Synapsis encountered an error:\n\n{0}\n\n{1}", error.ErrorMessage, error.Details));

            // set up the event handlers for Behavior Engine
            _engine.InDockingCart += new EventHandler(_engine_InDockingCart);
            _engine.InNotHomed += new EventHandler(_engine_InNotHomed);
            _engine.InHoming += new EventHandler(_engine_InHoming);
            _engine.InIdle += new EventHandler(_engine_InIdle);
            _engine.InLoadingPlate += new EventHandler(_engine_InLoadingPlate);
            _engine.InMovingPlates += new EventHandler(_engine_InMovingPlates);
            _engine.InPaused += new EventHandler(_engine_InPaused);
            _engine.InReinventorying += new EventHandler(_engine_InReinventorying);
            _engine.InShutdown += new EventHandler(_engine_InShutdown);
            _engine.InUndockingCart += new EventHandler(_engine_InUndockingCart);
            _engine.InUnloadingPlate += new EventHandler(_engine_InUnloadingPlate);
        }

        void _hive_server_impl_ClientDisconnected(object sender, EventArgs e)
        {
            RpcClientConnectionStatusColor = Brushes.DarkRed;
        }

        void _hive_server_impl_ClientConnected(object sender, EventArgs e)
        {
            RpcClientConnectionStatusColor = Brushes.DarkGreen;
        }

        internal bool DevicesAreHomed()
        {
            return (_robot as DeviceInterface).IsHomed && (_platemover as DeviceInterface).IsHomed;
        }

        void _engine_InNotHomed(object sender, EventArgs e)
        {
            ResetBehaviorEngineColors();
        }

        void _engine_InUnloadingPlate(object sender, EventArgs e)
        {
            ResetBehaviorEngineColors();
            UnLoadingColor = Brushes.DarkGreen;
            Status.Busy = true;
            Status.MovingPlate = false;
            Status.LoadingPlate = false;
            Status.UnloadingPlate = false;
            Status.ScanningInventory = false;
        }

        void _engine_InUndockingCart(object sender, EventArgs e)
        {
            ResetBehaviorEngineColors();
            UndockingColor = Brushes.DarkGreen;
        }

        void _engine_InShutdown(object sender, EventArgs e)
        {
            ResetBehaviorEngineColors();
            ShutdownColor = Brushes.DarkRed;
        }

        void _engine_InReinventorying(object sender, EventArgs e)
        {
            ResetBehaviorEngineColors();
            ReinventoryingColor = Brushes.DarkGreen;
            Status.Busy = true;
            Status.MovingPlate = false;
            Status.LoadingPlate = false;
            Status.UnloadingPlate = false;
            Status.ScanningInventory = true;
            // check storage space
            UpdateFullEmptyStatus();
        }

        internal void UpdateFullEmptyStatus()
        {
            Status.Full = GetAvailableStaticLocations().Count() == 0;
            Status.Empty = GetAvailableStaticLocations().Count() == GetStaticStorageLocationNames().Count();

            StorageFullStatusColor = Status.Full ? Brushes.DarkRed : Brushes.LightGray;
            StorageEmptyStatusColor = Status.Empty ? Brushes.DarkRed : Brushes.LightGray;
        }

        void _engine_InPaused(object sender, EventArgs e)
        {
            ResetBehaviorEngineColors();
            PausedColor = Brushes.DarkGreen;
        }

        void _engine_InMovingPlates(object sender, EventArgs e)
        {
            ResetBehaviorEngineColors();
            MovingColor = Brushes.DarkGreen;
            Status.Busy = true;
            Status.MovingPlate = true;
            Status.LoadingPlate = false;
            Status.UnloadingPlate = false;
            Status.ScanningInventory = false;
        }

        void _engine_InLoadingPlate(object sender, EventArgs e)
        {
            ResetBehaviorEngineColors();
            LoadingColor = Brushes.DarkGreen;
            Status.Busy = true;
            Status.MovingPlate = false;
            Status.LoadingPlate = true;
            Status.UnloadingPlate = false;
            Status.ScanningInventory = false;
        }

        void _engine_InIdle(object sender, EventArgs e)
        {
            ResetBehaviorEngineColors();
            IdleColor = Brushes.DarkGreen;
            Status.Busy = false;
            Status.MovingPlate = false;
            Status.LoadingPlate = false;
            Status.UnloadingPlate = false;
            Status.ScanningInventory = false;
        }

        void _engine_InHoming(object sender, EventArgs e)
        {
            ResetBehaviorEngineColors();
            HomingColor = Brushes.DarkGreen;
        }

        void _engine_InDockingCart(object sender, EventArgs e)
        {
            ResetBehaviorEngineColors();
            DockingColor = Brushes.DarkGreen;
        }

        private void ResetBehaviorEngineColors()
        {
            // set the background color
            IdleColor = LoadingColor = UnLoadingColor = MovingColor = ReinventoryingColor =
                UndockingColor = ShutdownColor = PausedColor = HomingColor = DockingColor = Brushes.LightGray;
            // set the foreground text color
            IdleIndicatorTextColor = LoadingIndicatorTextColor = UnloadingIndicatorTextColor = MovingIndicatorTextColor =
                ReinventoryingIndicatorTextColor = UndockingIndicatorTextColor = HomingIndicatorTextColor = DockingIndicatorTextColor = Brushes.Black;
        }

        void dock_UndockingError(object sender, DockEventArgs e)
        {
            // just in case, don't leave the "ready to undock" overlay on the screen
            DockMonitorPlugin dock_plugin = (DockMonitorPlugin)sender;
            dock_plugin.ShowReadyToUndockOverlay(false);
            // don't let the behavior engine hang while waiting for cart to undock
            _engine.FireUndockCartComplete();
        }

        void dock_DockingError(object sender, DockEventArgs e)
        {
            // don't let the behavior engine hang while waiting for cart to dock
            _engine.FireDockCartComplete();
        }

        /// <summary>
        /// This is the handler for the event that the object derived from PlateStorageInterface will fire when it starts to scan inventory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void StaticStorage_ReinventoryBegin(object sender, EventArgs e)
        {
            ReinventoryEventArgs args = e as ReinventoryEventArgs;
            if (args != null && args.CalledFromDiags)
                return;
            _engine.FireReinventoryBegin();
        }

        /// <summary>
        /// This handler is called anytime ANY storage device (static storage and carts) finishes scanning their inventory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void All_ReinventoryComplete(object sender, EventArgs e)
        {
            try
            {
                string device_name = (sender as DeviceInterface).Name;
                // get the inventory from the specified dock / cart
                IEnumerable<KeyValuePair<string, string>> inventory = (sender as PlateStorageInterface).GetInventory("robot name not used");
                // DKM 2012-05-10 TODO do something with inventory here, or just cache locally?
            }
            catch (Exception ex)
            {
                _log.Error(string.Format("There was an error when attempting to send ReinventoryComplete message to GEMS.\nYou will need to redo the inventory operation for GEMS to receive the inventory information.\nThe error message was '{0}'", ex.Message));
            }
        }

        void dock_Docked(object sender, DockEventArgs e)
        {
            try
            {
                lock (_assigned_fates) { _assigned_fates[((DockMonitorPlugin)sender).Name] = ""; }  // this is coming from a thread, so there's a race on _assigned_fates access
                FateDock1 = ""; FateDock2 = ""; // update UI
                _robot.ReloadTeachpoints();
            }
            catch (Exception ex)
            {
                string message = String.Format("An error occurred after docking a cart at '{0}': {1}", e.DockName, ex.Message);
                MessageBox.Show(message);
                _log.Info(message);
            }
            finally
            {
                // I put the fire here so that the behavior engine won't get stuck if the teachpoints fail to reload
                _engine.FireDockCartComplete();
            }
        }

        // not sure that I like it this way -- maybe I can have the behavior engine be in the same state for docking and undocking?
        void dock_Undocked(object sender, DockEventArgs e)
        {
            try
            {
                lock (_assigned_fates)
                {
                    DockMonitorPlugin dock_plugin = (DockMonitorPlugin)sender;
                    _assigned_fates[dock_plugin.Name] = "";
                    dock_plugin.ShowReadyToUndockOverlay(false);
                } // this is coming from a thread, so there's a race on _assigned_fates access
                FateDock1 = ""; FateDock2 = ""; // update UI
                _robot.ReloadTeachpoints();
            }
            catch (Exception ex)
            {
                string message = String.Format("An error occurred after docking a cart at '{0}': {1}", e.DockName, ex.Message);
                MessageBox.Show(message);
                _log.Info(message);
            }
            finally
            {
                // I put the fire here so that the behavior engine won't get stuck if the teachpoints fail to reload
                _engine.FireUndockCartComplete();
            }
        }

        private void LoadDllConfiguration()
        {
            try
            {
                // use this to deserialize a configuration file
                string config_path = BioNex.Shared.Utils.FileSystem.GetAppPath() + "\\config\\HiveIntegrationConfig.xml";
                _configuration = FileSystem.LoadXmlConfiguration<Configuration>(config_path);
            }
            catch (Exception ex)
            {
                _log.Info(String.Format("Could not load settings for customer GUI plugin: {0}", ex.Message));
            }
        }

        /*
        private void Heartbeat()
        {
            while (!_stop_heartbeat_thread.WaitOne(new TimeSpan(0, 0, 5)))
            {
                
            }
        }
         */

        #region ICustomerGUI Members

        public event EventHandler ProtocolComplete;

        public string GUIName
        {
            get { return "Hive Integration GUI"; }
        }

        public bool Busy
        {
            get { return true; }
        }

        public string BusyReason
        {
            get { return "because I am."; }
        }

        public bool CanExecuteStart(out IEnumerable<string> failure_reasons)
        {
            // needed to do this so that the AbortPauseResume state machine in Synapsis would be happy.  It's probably
            // OK since we don't have a Start button anyway.  I guess at one point it made sense to have the CanExecuteAbort
            // handler piggyback off of the CanExecuteStart code.
            failure_reasons = new List<string>();
            return true;
        }

        public bool ExecuteStart()
        {
            return false;
        }

        public bool ShowProtocolExecuteButtons()
        {
            return false;
        }

        public void Close() // not really sure why we don't just use window_closing here, but I suppose this gives us more explicit control ...
        {
            _stop_fate_processing_thread.Set();
            _fate_processing_thread.Join();

            /*
            _stop_heartbeat_thread.Set();
            _heartbeat_thread.Join();
             */
        }

        #endregion

        #region DockExternalCommunications Members

        public bool PrepareForDock(string dock_name)
        {
            return _engine.FireDockCartRequested();
        }

        public void ReadyToDock(string dock_name)
        {
            // only park the robot if the cart has not arrived yet (presence sensor is off)
            if (!_docks[dock_name].SubStoragePresent)
                _robot.Park();
        }

        public bool PrepareForUndock()
        {
            return _engine.FireUndockCartRequested();
        }

        public void ReadyToUndock()
        {
            _robot.Park();
        }

        public bool BarcodeReaderAvailable { get { return _robot.CanReadBarcode(); } }

        public bool Reinventorying
        {
            get { return _engine.IsInState(BehaviorEngine.State.Reinventorying) || _engine.UserRequestedReinventory; }
        }

        public bool Homing
        {
            get { return _engine.IsInState(BehaviorEngine.State.Homing); }
        }

        /// <summary>
        /// Transitions BehaviorEngine and updates state GUI
        /// </summary>
        /// <param name="dock_name"></param>
        public bool Reinventory(string dock_name)
        {
            // send off the message that we're going to reinventory, so that the state machine can transition properly
            if (!_engine.FireReinventoryBegin())
                return false;

            return true;
        }

        public bool Undocking(string dock_name, out string error)
        {
            error = "";
            return true;
        }

        public bool ReinventoryAllowed(string dock_name, out string reason_not_allowed)
        {
            if (_engine == null)
            {
                reason_not_allowed = "GUI plugin not initialized yet";
                return false;
            }

            if (_engine.IsInState(BehaviorEngine.State.NotHomed))
            {
                reason_not_allowed = "Devices not homed yet";
                return false;
            }


            // check to make sure teachpoints have been loaded for dock 
            // check to make sure that the loaded teachpoints match the cart definition file
            if (!_docks[dock_name].VerifyTeachpointsForReinventory(_robot, out reason_not_allowed))
                return false;

            return true;
        }

        public bool UndockAllowed(string dock_name, out string reason_not_allowed)
        {
            reason_not_allowed = "GUI plugin not initialized yet";
            if (_engine == null)
                return false;
            if (_engine.IsInState(BehaviorEngine.State.NotHomed))
            {
                reason_not_allowed = "Devices not homed yet";
                return false;
            }
            if (_engine.Paused)
            {
                reason_not_allowed = "Click the Resume button first";
                return false;
            }
            return true;
        }

        public bool DockAllowed(string dock_name, out string reason_not_allowed)
        {
            if (_engine == null)
            {
                reason_not_allowed = "GUI plugin not initialized yet";
                return false;
            }
            if (_engine.IsInState(BehaviorEngine.State.NotHomed))
            {
                reason_not_allowed = "Devices not homed yet";
                return false;
            }
            if (_engine.Paused)
            {
                reason_not_allowed = "Click the Resume button first";
                return false;
            }
            bool allowed = !(_docks[dock_name].SubStoragePresent && _docks[dock_name].SubStorageDocked);
            reason_not_allowed = allowed ? "" : "Already docked";
            return allowed;
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string property_name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property_name));
        }

        #endregion

        public UserControl GetSystemPanel()
        {
            return _system_panel;
        }

        /// <summary>
        /// OnUpdatePlateFate handles the XML-RPC message originating in GEMS
        ///   1. Check to see if plate is in inventory
        ///   2. If not, discard message
        ///   3. Otherwise, persist message to DB in the form [barcode, fate, timestamp]
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="fate"></param>        
        internal void OnUpdatePlateFate(string barcode, string fate)
        {
            DateTime timestamp = DateTime.Now;

            // discard message if plate is not in inventory
            if (!PlateIsInAnyCart(barcode) && !PlateIsInStaticStorage(barcode))
                return;

            _fate_processing_db.SaveWork(barcode, fate);
        }

        /// <summary>
        /// OnEndBatch handles the XML-RPC message originating in GEMS
        ///  1. Persist message to DB in the form ["##END_BATCH##-GUID", fate, timestamp]
        /// </summary>
        /// <param name="fate"></param>
        void OnEndBatch(string fate)
        {
            _fate_processing_db.SaveWork(string.Format("{0}-{1}", FateProcessing.END_BATCH_TOKEN, Guid.NewGuid()), fate);
        }

        void PendingFatesThread()
        {
            while (!_stop_fate_processing_thread.WaitOne(new TimeSpan(0, 0, 0, 0, 100)))
            {
                string barcode;
                string fate;
                if (!_fate_processing_db.GetNextWorkItem(out barcode, out fate))
                    continue;

                if (barcode.StartsWith(FateProcessing.END_BATCH_TOKEN)) ProcessPendingEndBatch(barcode, fate);
                else ProcessPendingFates(barcode, fate);
            }
        }

        void ProcessPendingFates(string barcode, string fate)
        {
            Func<string> GetNextAvailableLocationCallback;

            // 1. Check for special fates : 'Hold', 'Trash'
            //      A. Process 'Hold' fate:  if plate is in cart and there is an available slot, fire a message to behavior engine to move plate to slot
            //      B. Process 'Trash' fate: fire a message to behavior engine to move plate to trash 
            bool is_trash_fate = fate.ToLower() == _special_fates[(int)SpecialFate.Trash].ToLower();
            // non-hold fate implies that the plate is moving to a cart
            bool is_non_hold_fate = fate != SpecialFate.Hold.ToString();
            if (_special_fates.Contains(fate))
            {
                if (fate.ToLower() == _special_fates[(int)SpecialFate.Hold].ToLower())
                {
                    if (!PlateIsInAnyCart(barcode)) // if it doesn't exist, or is in static storage already
                    {
                        _fate_processing_db.DeleteWork(barcode);
                        return;
                    }
                    if (!IsThereAnAvailableStaticLocation())
                    {
                        // Leave record in DB for next iteration
                        return;
                    }

                    GetNextAvailableLocationCallback = () =>
                    {
                        // -- if GetNextAvailableLocation() returns null, we need to reprocess the plate --
                        var location = GetNextAvailableLocation();
                        return location == null ? "" : location.LocationName;
                    };
                }
                else if (is_trash_fate)
                {
                    if (!PlateIsInAnyCart(barcode) && !PlateIsInStaticStorage(barcode))
                    {
                        _fate_processing_db.DeleteWork(barcode);
                        return;
                    }
                    GetNextAvailableLocationCallback = GetTrashLocationName;
                }
                else
                    throw new NotSupportedException("We don't yet support special fates other than 'Hold' or 'Trash'");

                // figure out which cart the plate is in so that we can clear the overlay if necessary
                string cart = GetCartWithPlate(barcode);

                // DKM 2010-05-10 TODO get labware name
                MovePlate( (MutableString)barcode, ((DeviceInterface)_robot).Name, GetNextAvailableLocationCallback, "TODO", () => { OnUpdatePlateFate(barcode, fate); });

                // remove from DB after motion success
                _fate_processing_db.DeleteWork(barcode);

                // Finally, if we emptied up a cart, mark it as not full in the UI
                if (cart != "")
                    _docks[cart].ShowReadyToUndockOverlay(CartIsFull(cart));
                return;
            }

            // 2. Otherwise, Find a cart for this fate 
            string dock = GetCartThatCanAcceptFate(fate);
            if (dock == "")
            {
                // Leave data in DB for processing on next iteration
                return;
            }

            // 2a. If the plate is already in a cart that can accept this plate, don't move it
            string unused;
            if (_docks[dock].HasPlateWithBarcode(barcode, out unused))
            {
                _fate_processing_db.DeleteWork(barcode);
                return;
            }

            GetNextAvailableLocationCallback = () =>
            {
                // -- if GetNextAvailableLocation() returns null, we need to reprocess the plate --
                var location = GetNextAvailableCartLocation(dock);
                return location == null ? "" : location.LocationName;
            };

            // 3. Fire a message to the behavior engine to move the plate who's fate was changed to the destination we found
            // DKM 2010-05-10 TODO get labware name
            MovePlate( (MutableString)barcode, dock, GetNextAvailableLocationCallback, "TODO", () => { OnUpdatePlateFate(barcode, fate); });

            // remove from DB after motion success
            _fate_processing_db.DeleteWork(barcode);


            // Finally, if we filled up a cart, mark it as full in the UI so that the operator gets the message to undock it
            bool full = CartIsFull(dock);
            _docks[dock].ShowReadyToUndockOverlay(full);
            if (full)
                _mailer.SendMessage(string.Format("{0} Full", dock), string.Format("{0} is ready to undock", dock));

        }

        void ProcessPendingEndBatch(string batch_token, string fate)
        {
            // When we reach an end batch marker, 
            // We need to 
            // 1. ask the DB if the batch is complete (i.e. are there any pending work items that have an earlier ID but belong to this fate)
            // 2. if the batch is complete mark the cart as complete and put an overlay on the UI somewhere
            //
            // It's more simple to have the db only hand us END_BATCH work items that it has pre-determined are complete, so we skip step 1

            // look at the assigned fates to determine which dock(s) need to display their user notification
            var docks_to_notify = from x in _assigned_fates where x.Value == fate select x.Key;
            foreach (var dock_name in docks_to_notify)
            {
                _docks[dock_name].ShowReadyToUndockOverlay(true);
                _mailer.SendMessage(string.Format("{0} Batch Complete", dock_name), string.Format("{0} is ready to undock", dock_name));
            }

            _fate_processing_db.DeleteWork(batch_token);
        }


        #region Storage tracking classes for Upstack / Downstack
        public class AvailableLocation
        {
            public string DeviceName { get; set; }
            public string LocationName { get; set; }
        }

        /// <summary>
        /// Returns a list of available (i.e. unoccupied) storage locations.
        /// </summary>
        /// <remarks>
        /// Takes all of the Hive storage locations, and removes any that the inventory database has marked as loaded
        /// </remarks>
        /// <returns></returns>
        internal IEnumerable<string> GetAvailableStaticLocations()
        {
            List<string> available_locations = new List<string>();
            // here we want to only offer up the locations in the Hive, not the Docks!
            PlateStorageInterface storage = _robot as PlateStorageInterface;
            if (storage == null)
                return available_locations;

            // get the plates in storage
            IEnumerable<KeyValuePair<string, string>> plates_in_storage = storage.GetInventory((_robot as DeviceInterface).Name);
            // get the list of location names.  I'm not sure if this is cheating or not, but the robot has a list
            // of teachpoint names.  We can easily get its own teachpoints, and then only keep the ones that can be
            // mapping to a rack and slot.
            IDictionary< string, IList< string>> all_teachpoints = _robot.GetTeachpointNames();
            IList< string> hive_teachpoints_only = all_teachpoints[(_robot as DeviceInterface).Name];
            IEnumerable<string> storage_teachpoints = from x in hive_teachpoints_only where BioNex.HivePrototypePlugin.HivePlateLocation.IsValidPlateLocationName(x) select x;
            // now that we have all of the occupied slots, as well as the total slots, we can figure out which ones are unoccupied
            return storage_teachpoints.Except(from x in plates_in_storage select x.Value);
        }

        /// <summary>
        /// Gets the names of the storage locations in the Hive
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<string> GetStaticStorageLocationNames()
        {
            PlateStorageInterface storage = _robot as PlateStorageInterface;
            if (storage == null)
            {
                _log.Debug("Queried storage for all static location names, but the device was invalid.");
                return new List<string>();
            }
            return storage.GetStorageLocationNames();
        }

        public AvailableLocation GetNextAvailableLocation()
        {
            // return null for no location available so that GetNextAvailableLocationCallback can return the plate to process queue
            var location_name = GetAvailableStaticLocations().FirstOrDefault();
            return (location_name == null || location_name == "") ? null : new AvailableLocation { DeviceName = (_robot as DeviceInterface).Name, LocationName = location_name };
        }
        #endregion

        internal bool PlateIsInAnyCart(string barcode)
        {
            string unused;
            foreach (var dock in _docks.Values)
                if (dock.HasPlateWithBarcode(barcode, out unused))
                    return true;
            return false;
        }

        string GetCartWithPlate(string barcode)
        {
            string unused;
            foreach (var dock in _docks.Keys)
                if (_docks[dock].HasPlateWithBarcode(barcode, out unused))
                    return dock;
            return "";
        }

        internal bool PlateIsInStaticStorage(string barcode)
        {
            // here we want to only offer up the locations in the Hive, not the Docks!
            PlateStorageInterface storage = _robot as PlateStorageInterface;
            if (storage == null)
                return false;
            // get the plates in storage
            IEnumerable<KeyValuePair<string, string>> plates_in_storage = storage.GetInventory((_robot as DeviceInterface).Name);
            var barcode_in_storage = from x in plates_in_storage where x.Key == barcode select x;
            return barcode_in_storage.Count() != 0;
        }

        // DKM 2012-05-11 added this method to support reporting of barcodes in inventory via the API
        // Key = device name
        // Value = list of barcodes
        internal Dictionary<string, List<string>> GetInventory()
        {
            Dictionary<string, List<string>> barcodes = new Dictionary<string,List<string>>();
            // first get the static storage barcodes
            PlateStorageInterface storage = _robot as PlateStorageInterface;
            string robot_name = (_robot as DeviceInterface).Name;
            barcodes[robot_name] = new List<string>();
            if (storage != null) {
                IEnumerable<KeyValuePair<string, string>> plates_in_static_storage = storage.GetInventory( robot_name);
                barcodes[robot_name].AddRange( (from plate in plates_in_static_storage select plate.Key).ToList());
            }
            // next get the cart storage barcodes
            foreach( var d in _docks) {
                DockMonitorPlugin dock = d.Value;
                if( dock.CartBarcode != null && dock.CartBarcode != "") {
                    barcodes[dock.CartBarcode] = new List<string>();
                    IEnumerable<KeyValuePair<string, string>> plates_in_cart = dock.GetInventory( robot_name);
                    barcodes[dock.CartBarcode].AddRange( (from plate in plates_in_cart select plate.Key).ToList());
                }
            }

            return barcodes;
        }

        bool IsThereAnAvailableStaticLocation()
        {
            return GetAvailableStaticLocations().Count() > 0;
        }

        string GetTrashLocationName()
        {
            // return the teachpoint reserved for the Trash location...  hopefully this is "Trash"
            return _special_fates[(int)SpecialFate.Trash];
        }

        string GetCartThatCanAcceptFate(string fate)
        {
            //      Find a cart that has already been assigned this fate, and is not full
            //      or Find an EMPTY cart that has not been assigned a fate yet, or an empty cart that can be reassigned (no plates with assigned fates match its fate)
            //      or if no cart is available put this plate back into the pending queue (return empty string)
            //      i.e. when a cart reinventory result says it's empty, it will get assigned the fate of the next plate in the 'pending' queue, and the reinventory call can call this function to process the plate

            // first search for a cart that is servicing this fate that is not full
            lock (_assigned_fates) // _assigned_fates could be changed by a thread when docking or undocking a cart, so there's a race on access that needs to be locked out
            {
                foreach (var dock in _assigned_fates.Keys)
                    if (_assigned_fates[dock] == fate && !CartIsFull(dock) && !_docks[dock].ReadyToUndock)
                        return dock;

                // otherwise, search for a dock with an unassigned fate or one that can be reassigned
                foreach (var dock in _assigned_fates.Keys)
                    if (CanReassignCartsFate(dock))
                    {
                        _assigned_fates[dock] = fate; FateDock1 = FateDock1; FateDock2 = FateDock2; // weird assignments for UI update when _assigned_fates changes
                        return dock;
                    }
            }
            return "";
        }

        bool CanReassignCartsFate(string dock)
        {
            if (!DockHasTeachpoints(dock))
                return false;

            if (!CartIsEmpty(dock))
                return false;

            if (GetNextAvailableCartLocation(dock) == null)
                return false;

            string fate;
            lock (_assigned_fates)
            {
                if (_assigned_fates[dock] == "")
                    return true; // Early TRUE result 

                fate = _assigned_fates[dock];
            }

            // if there are ZERO pending fates going to this cart, we can reassign it
            // this implies that we assigned plates to a fate, they got assigned to this cart,
            // but then we changed our mind and assigned them to a different fate
            return !_fate_processing_db.FatesPending(fate);
        }

        bool DockHasTeachpoints(string dock)
        {
            return _robot.GetTeachpointNames().ContainsKey(dock);
        }

        internal IEnumerable<string> GetAvailableCartLocations(string dock)
        {
            List<string> available_locations = new List<string>();
            // here we want to only offer up the locations in the Hive, not the Docks!
            PlateStorageInterface storage = _docks[dock] as PlateStorageInterface;
            if (storage == null)
                return available_locations;

            // get the plates in storage
            IEnumerable<KeyValuePair<string, string>> plates_in_storage = storage.GetInventory("unused");
            // get the list of location names.  I'm not sure if this is cheating or not, but the robot has a list
            // of teachpoint names.  We can easily get its own teachpoints, and then only keep the ones that can be
            // mapping to a rack and slot.
            IDictionary< string, IList< string>> all_teachpoints = _robot.GetTeachpointNames();
            IList< string> dock_teachpoints_only = all_teachpoints[dock];
            IEnumerable<string> storage_teachpoints = from x in dock_teachpoints_only where HivePlateLocation.IsValidPlateLocationName(x) select x;
            // now that we have all of the occupied slots, as well as the total slots, we can figure out which ones are unoccupied
            return storage_teachpoints.Except(from x in plates_in_storage select x.Value);
        }

        private bool CartIsEmpty(string dock)
        {
            return _docks.ContainsKey(dock) && _docks[dock].GetInventory("robot name not used").Count() == 0;
        }

        private bool CartIsFull(string dock)
        {
            var available_cart_locations = _docks[dock].GetAvailableLocationNames();
            return available_cart_locations.Count() == 0;
        }

        private AvailableLocation GetNextAvailableCartLocation(string dock)
        {
            var available_cart_teachpoints = GetAvailableCartLocations(dock);
            var available_cart_locations = _docks[dock].GetAvailableLocationNames();
            string location_to_use = available_cart_teachpoints.Intersect(available_cart_locations).FirstOrDefault();
            if (location_to_use == null)
                return null;
            return new AvailableLocation { DeviceName = (_docks[dock] as DeviceInterface).Name, LocationName = location_to_use };
        }

        public RobotInterface GetRobotForDock(string dock_name)
        {
            return _robot;
        }

        private void MovePlate( MutableString barcode, string device, Func<string> GetLocationCallBack, string labware, Action NoLocationAvailableCallback)
        {
            if (!_engine.FireMovePlate())
                return;
            try
            { // make sure we return to idle state when complete

                var storage_plugins = DataRequestInterface.GetPlateStorageInterfaces();
                bool has_plate = false;
                DeviceInterface storage_with_plate = null;
                string plate_location_name = "";
                foreach (var storage in storage_plugins)
                {
                    has_plate = storage.HasPlateWithBarcode(barcode, out plate_location_name);
                    if (has_plate)
                    {
                        storage_with_plate = (DeviceInterface)storage;
                        break;
                    }
                }


                if (!has_plate)
                {
                    _log.Info("Could not find plate '" + barcode + "' for fate processing, skipping it");
                    return;
                }

                string destination = GetLocationCallBack();
                if (destination == "")
                {
                    // Failed to find a spot to move to - put the plate back in the processing queue
                    NoLocationAvailableCallback();
                    return;
                }

                // now we know we have a plate, so go ahead and grab it
                _robot.TransferPlate( storage_with_plate.Name, plate_location_name, device, destination, labware, barcode);
                // labware and from_teachpoint_name aren't even needed for Unload -- from_teachpoint_name was only useful for the Hive
                // where it needed to be able to remove unbarcoded labware from a memory map.
                (storage_with_plate as PlateStorageInterface).Unload(labware, barcode, plate_location_name);
                // handle Trash as a special case -- Hive has a teachpoint called "Trash"
                if (destination.ToLower() == _special_fates[(int)SpecialFate.Trash].ToLower())
                {
                    return;
                }
                else
                {
                    var storage = DataRequestInterface.GetPlateStorageInterfaces().Where(x => (x as DeviceInterface).Name == device).First();
                    storage.Load(labware, barcode, destination);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Could not move plate '{0}': {1}", barcode, ex.Message));
            }
            finally
            {
                _engine.FireMovePlateComplete();
            }
        }

        public bool CanClose()
        {
            _engine.FireShutdown();
            return true;
        }

        public bool CanPause()
        {
            return true;
        }

        public bool AllowDiagnostics()
        {
            // I know Giles set this to true, but forgot to check it in so I'm adding it for him.
            return true;
        }

        public void CompositionComplete()
        {
            CheckInitialHomeState();
        }
    }
}
