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
using BioNex.GemsRpc;
using BioNex.HivePrototypePlugin;
using BioNex.LiquidLevelDevice;
using BioNex.MonsantoOutputTransferPlugin;
using BioNex.Plugins.Dock;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.IError;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Utils;
using BioNex.Shared.Utils.WellMathUtil;
using BioNex.SynapsisPrototype;
using BioNex.SynapsisPrototype.ViewModel;
using log4net;
using SMTPReporting;

namespace BioNex.CustomerGUIPlugins
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
    public partial class MonsantoPhase2GUI : UserControl, ICustomerGUI, DockExternalCommunications, IHasSystemPanel, INotifyPropertyChanged
    {
        [ Import("MainDispatcher")]
        Dispatcher _dispatcher { get; set; }
        [ Import]
        ILimsOutputTransferLog OutputPlugin { get; set; }
        [ Import]
        ILabwareDatabase LabwareDatabase { get; set; }

        private DeviceManager _device_manager { get; set; }

        private ILog _log = LogManager.GetLogger(typeof(MonsantoPhase2GUI));
        public ExternalDataRequesterInterface DataRequestInterface { get; set; }
        internal BehaviorEngine _engine { get; set; }
        private BioNex.JEMSRpc.JEMSClient _jems_client { get; set; }
        private BioNex.HiveRpc.HiveServer _hive_server { get; set; }
        /*
        internal ThreadsafeMessenger _messenger { get; set; }
        private BioNex.IWorksCommandServer.RemoteStackerServer _stacker_server { get; set; }
        private BioNex.IWorksCommandServer.RemoteStackerPingBack _stacker_client { get; set; }
        private LabwareXmlRpcServer _labware_server { get; set; }
        private LabwareXmlRpcClient _labware_client { get; set; }
         */
        private Configuration _configuration { get; set; }

        // devices that we need to move plates from BioCel to Hive storage
        internal Dictionary<string, DockMonitorPlugin> _docks { get; set; }
        internal RobotInterface _robot { get; set; }
        internal Dictionary<string, StackerInterface> _stackers { get; set; }

        // for pause/resume only
        internal DeviceInterface _bumblebee { get; set; }
        internal DeviceInterface _hive { get; set; }
        internal LLSensorPlugin _beesure { get; set; }

        private Thread _heartbeat_thread { get; set; }
        private ManualResetEvent _stop_heartbeat_thread { get; set; }

        private SystemPanel _system_panel;

        private Thread _destination_data_processing_thread;
        private ManualResetEvent _stop_destination_data_processing_thread;
        private DestinationProcessing _destination_data_processing_db;

        /// <summary>
        /// _assigned_fates: dock name to string map to track the fate that is assigned to the cart currently docked.  
        /// Fate assigned to a Dock is cleared when cart is docked or undocked
        /// Fates assigned is set when a new plate fate is assigned and there is a cart with no assigned fate
        /// </summary>
        // refs #544: used so we can display active workset(s) in cart
        // DKM 2011-11-02 changed to HashSet<string> because "fates" now refers to one or more worklists per cart
        private Dictionary<string, HashSet<string>> _assigned_fates = new Dictionary<string, HashSet<string>>();
        // honey badger don't care for arrays.  we iterate over available device manager entries.
        private List<string> _dock_names = new List<string>();

        // UI for fates -- MY GOD I NEED TO LEARN HOW TO ARRAY PROPERTIES
        // DKM 2011-06-03  Me too.
        public string FateDock1
        {
            get { lock (_assigned_fates) { if( _dock_names.Count < 2) return "no dock available"; string fate = _assigned_fates[_dock_names[1]].ToCommaSeparatedString(); return string.IsNullOrEmpty(fate) ? "no workset assigned" : fate; } }
            set { OnPropertyChanged("FateDock1"); FateDock1Color = FateDock1Color; FateDock1TextColor = FateDock1TextColor; }
        }
        public Brush FateDock1Color
        {
            get { lock (_assigned_fates) { if( _dock_names.Count < 2) return Brushes.DarkRed; string fate = _assigned_fates[_dock_names[1]].ToCommaSeparatedString(); return string.IsNullOrEmpty(fate) ? Brushes.LightGray : Brushes.DarkGreen; } }
            set { OnPropertyChanged("FateDock1Color"); }
        }
        public Brush FateDock1TextColor
        {
            get { lock (_assigned_fates) { if( _dock_names.Count < 2) return Brushes.Goldenrod; string fate = _assigned_fates[_dock_names[1]].ToCommaSeparatedString(); return string.IsNullOrEmpty(fate) ? Brushes.Black : Brushes.Goldenrod; } }
            set { OnPropertyChanged("FateDock1TextColor"); }
        }
        public string FateDock2
        {
            get { lock (_assigned_fates) { if( _dock_names.Count < 1) return "no dock available"; string fate = _assigned_fates[_dock_names[0]].ToCommaSeparatedString(); return string.IsNullOrEmpty(fate) ? "no workset assigned" : fate; } }
            set { OnPropertyChanged("FateDock2"); FateDock2Color = FateDock2Color; FateDock2TextColor = FateDock2TextColor; }
        }
        public Brush FateDock2Color
        {
            get { lock (_assigned_fates) { if( _dock_names.Count < 2) return Brushes.DarkRed; string fate = _assigned_fates[_dock_names[0]].ToCommaSeparatedString(); return string.IsNullOrEmpty(fate) ? Brushes.LightGray : Brushes.DarkGreen; } }
            set { OnPropertyChanged("FateDock2Color"); }
        }
        public Brush FateDock2TextColor
        {
            get { lock (_assigned_fates) { if( _dock_names.Count < 2) return Brushes.Goldenrod; string fate = _assigned_fates[_dock_names[0]].ToCommaSeparatedString(); return string.IsNullOrEmpty(fate) ? Brushes.Black : Brushes.Goldenrod; } }
            set { OnPropertyChanged("FateDock2TextColor"); }
        }

        private string _home_all_command_tooltip;
        public string HomeAllCommandToolTip
        {
            get { return _home_all_command_tooltip; }
            set { _home_all_command_tooltip = value; OnPropertyChanged( "HomeAllCommandToolTip"); }
        }

        enum SpecialFate { Hold, Trash };
        private string[] _special_fates = Enum.GetNames(typeof(SpecialFate));

        internal IError ErrorInterface { get; set; }

        private SMTPReporter _mailer { get; set; }

        private SynapsisViewModel _synapsis_view_model { get; set; }

        #region databinding members
        private Brush _connection_status_color;
        public Brush ConnectionStatusColor
        {
            get { return _connection_status_color; }
            set
            {
                _connection_status_color = value;
                OnPropertyChanged("ConnectionStatusColor");
            }
        }

        private Brush _vworks_connection_status_color;
        public Brush VWorksConnectionStatusColor
        {
            get { return _vworks_connection_status_color; }
            set
            {
                _vworks_connection_status_color = value;
                OnPropertyChanged("VWorksConnectionStatusColor");
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

        // DKM 2011-10-13 hijack Phase 1 "Loading" state and use it for "Processing Worklist"
        private Brush _processing_color;
        public Brush ProcessingColor
        {
            get { return _processing_color; }
            set
            {
                _processing_color = value;
                OnPropertyChanged("ProcessingColor");
            }
        }

        /*
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
         */

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
            get { return _engine.WaitingForGo ? "Go Live" : "Resume"; }
        }


        #endregion

        [ImportingConstructor]
        public MonsantoPhase2GUI([Import] ExternalDataRequesterInterface data_request_interface,
                                 [Import] ILabwareDatabase labware_database,
                                 /*[Import] LabwareCloudSystemSetup labware_cloud,*/
                                 [Import] IError error_interface,
                                 [Import] SMTPReporter mailer,
                                 [Import("SynapsisViewModel")] SynapsisViewModel synapsis_viewmodel,
                                 [Import] DeviceManager device_manager
                                 )
        {
            InitializeComponent();
            InitializeCommands();
            this.DataContext = this;

            LoadDllConfiguration();
            ErrorInterface = error_interface;

            // adjust Synapsis Main Window
            synapsis_viewmodel.MenuVisibility = Visibility.Collapsed;
            synapsis_viewmodel.CustomerGUITabVisibility = Visibility.Collapsed;
            synapsis_viewmodel.SetPauseRowMinHeightAndTextSize(80, 30);
            _synapsis_view_model = synapsis_viewmodel;

            _device_manager = device_manager;

            ConnectionStatusColor = VWorksConnectionStatusColor = Brushes.LightGray;
            StorageFullStatusColor = StorageEmptyStatusColor = Brushes.LightGray;

            //-----------------------------------------------------------------
            // first, let's figure out if we've got everything we need to operate.  This includes:
            // 1. docks
            // 2. robot
            // 3. stackers
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
                lock (_assigned_fates) { _assigned_fates.Add( dock_name, new HashSet<string>()); }
                _dock_names.Add(dock_name);
                dock.Docked += new DockEventHandler(dock_Docked);
                dock.DockingError += new DockEventHandler(dock_DockingError);
                dock.Undocked += new DockEventHandler(dock_Undocked);
                dock.UndockingError += new DockEventHandler(dock_UndockingError);
                dock.ReinventoryComplete += new EventHandler(All_ReinventoryComplete);
                dock.PlateLoaded += new DockMonitorPlugin.PlateLoadUnloadEventHandler(dock_PlateLoaded);
            }

            var lls_dev = ( from x in devices where x.ProductName == "LLS" select x).FirstOrDefault();
            if( lls_dev != null){
                LLSPlaceholder.LLSPlaceholder lls = lls_dev as LLSPlaceholder.LLSPlaceholder;
                lls.JobComplete += new JobCompleteEventHandler( OnJobComplete);
            }

            // DKM 2011-06-15 reversed the docks
            if (_docks.Count() == 1)
                DockPanel1.Children.Add(_docks[_dock_names[0]].GetDiagnosticsPanel());
            else if (_docks.Count() == 2)
            {
                DockPanel1.Children.Add(_docks[_dock_names[1]].GetDiagnosticsPanel());
                DockPanel2.Children.Add(_docks[_dock_names[0]].GetDiagnosticsPanel());
            }
            else if (_docks.Count() == 0)
            {
            }
            else
            {
                Debug.Assert(false, "There can only be one or two docks in this system.");
            }
            foreach (var kvp in _docks)
                kvp.Value.SetExternalCommunications(this);


            // 2
            _robot = (from x in devices where x.ProductName=="Hive" select x).FirstOrDefault() as RobotInterface;
            // register inventory complete handler for the hive robot separately
            var hive = (_robot as PlateStorageInterface);
            // 3
            _stackers = new Dictionary<string,StackerInterface>();
            temp = from x in devices where x is StackerInterface select x;
            foreach( var x in temp) {
                StackerInterface stacker = x as StackerInterface;
                string stacker_name = x.Name;
                _stackers.Add( stacker_name, stacker);
            }
            // 4
            _bumblebee = (from x in devices where x.ProductName==BioNexDeviceNames.Bumblebee select x).FirstOrDefault();
            _hive = (from x in devices where x.ProductName==BioNexDeviceNames.Hive select x).FirstOrDefault();
            _beesure = ( from x in devices where x.ProductName==BioNexDeviceNames.BeeSure select x).FirstOrDefault() as LLSensorPlugin;
            Debug.Assert( _bumblebee != null);
            Debug.Assert( _hive != null);
            Debug.Assert( _beesure != null);
            _beesure.Model.SavePropertiesEvent += LSESaveProperties;
            _beesure.JobComplete += new JobCompleteEventHandler( OnJobComplete);

            //-----------------------------------------------------------------
            // _messenger = new ThreadsafeMessenger();
            _engine = new BehaviorEngine(this);

            try
            {
                
                //_stacker_server = new IWorksCommandServer.RemoteStackerServer(new StackerImplementation(this), _configuration.IWorksListenerPort);
                //_stacker_client = new IWorksCommandServer.RemoteStackerPingBack(_configuration.VWorksServerUrl, _configuration.IWorksListenerPort);

                _hive_server = new BioNex.HiveRpc.HiveServer(_configuration.JemsListenerPort, null, null, OnAddWorkset);
                _jems_client = new JEMSRpc.JEMSClient(_configuration.JemsServerUrl, _configuration.JemsServerPort);

                // DKM 2011-06-20 I was asked to remove this until we actually use it
                /*
                if (_configuration.LabwareCloudHost == "localhost")
                    _labware_server = new LabwareXmlRpcServer(labware_database, _configuration.LabwareCloudListenerPort);
                _labware_client = new LabwareXmlRpcClient(labware_database, _configuration.LabwareCloudHost, _configuration.LabwareCloudListenerPort);
                labware_cloud.Configure(_labware_client);
                */
            }
            catch (Exception ex)
            {
                _log.InfoFormat( "Could not configure RPC: {0}.  Please check settings in plugins\\MonsantoPhase2Config.xml", ex.Message);
            }

            _system_panel = new SystemPanel();
            _system_panel.DataContext = this;

            // start the heartbeat thread to monitor JEMS connection
            _stop_heartbeat_thread = new ManualResetEvent(false);
            _heartbeat_thread = new Thread(Heartbeat);
            //_heartbeat_thread.IsBackground = true;  --> we call JOIN on this, so it should NOT be a background thread, otherwise we AbExit as the thread is aborted
            _heartbeat_thread.Name = "JEMS heartbeat test";
            _heartbeat_thread.Start();

            // start the destination plate data monitor thread
            _destination_data_processing_db = new DestinationProcessing("destination_processing_db.s3db");
            _stop_destination_data_processing_thread = new ManualResetEvent(false);
            _destination_data_processing_thread = new Thread(PendingDestinationDataThread);
            _destination_data_processing_thread.Name = "Pending destination data";
            _destination_data_processing_thread.Start();

            // grab the mailer and tie it into the Synapsis ErrorEvent
            _mailer = mailer;
            _mailer.HiveName = _configuration.HiveName;
            error_interface.ErrorEvent += (object sender, ErrorData error) => _mailer.SendMessage("Error", string.Format("Synapsis encountered an error:\n\n{0}\n\n{1}", error.ErrorMessage, error.Details));

            // set up the event handlers for Behavior Engine
            _engine.InDockingCart += new EventHandler(_engine_InDockingCart);
            _engine.InNotHomed += new EventHandler(_engine_InNotHomed);
            _engine.InHoming += new EventHandler(_engine_InHoming);
            _engine.InIdle += new EventHandler(_engine_InIdle);
            _engine.InPaused += new EventHandler(_engine_InPaused);
            _engine.Resuming += new EventHandler(_engine_Resuming);
            _engine.InReinventorying += new EventHandler(_engine_InReinventorying);
            _engine.InShutdown += new EventHandler(_engine_InShutdown);
            _engine.InUndockingCart += new EventHandler(_engine_InUndockingCart);
            _engine.InProcessingWorklist += new EventHandler(_engine_InProcessingWorklist);
            _engine.InMovingPlates += new EventHandler(_engine_InMovingPlates);
            
            synapsis_viewmodel.RobotScheduler.EnteringMovePlate += new EventHandler( EnterMovePlate);
            synapsis_viewmodel.RobotScheduler.ExitingMovePlate += new EventHandler( ExitMovePlate);

            /*
            _engine.InUnloadingPlate += new EventHandler(_engine_InUnloadingPlate);
            */
        }

        void dock_PlateLoaded(object sender, DockMonitorPlugin.PlateLoadUnloadEventArgs e)
        {
            /* FYC -- not updating "fate" on plate load but rather at workset xml received.
            // need to update the cart "fate" by adding the plate's worklist name
            DockMonitorPlugin dock = sender as DockMonitorPlugin;
            if( dock == null)
                return;
            // look up the destination plate barcode in the worklist map
            string worklist_name;
            lock(( _synapsis_view_model.PlateScheduler as BioNex.PlateScheduler.PlateScheduler).DestinationWorklistMap){
                try {
                    worklist_name = ( from kvp in ( _synapsis_view_model.PlateScheduler as BioNex.PlateScheduler.PlateScheduler).DestinationWorklistMap where kvp.Key.Barcode == e.Barcode select kvp.Value).First();
                    if( worklist_name == null) {
                        _log.InfoFormat( "Could not find the worklist associated with destination plate '{0}'", e.Barcode);
                        return;
                    }
                } catch( Exception) {
                    return;
                }
            }
            _assigned_fates[dock.Name].Add( worklist_name);
            FateDock1 = FateDock1; FateDock2 = FateDock2; // weird assignments for UI update when _assigned_fates changes
            */
        }

        void _engine_InNotHomed(object sender, EventArgs e)
        {
            ResetBehaviorEngineColors();
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
        }

        void _engine_InPaused(object sender, EventArgs e)
        {
            ResetBehaviorEngineColors();
            PausedColor = Brushes.DarkGreen;
            if (_bumblebee == null || _hive == null)
            {
                _log.Error("Bumblebee or Hive device(s) not present while trying to pause system");
                return;
            }
            _bumblebee.Pause();
            _hive.Pause();
        }

        void _engine_Resuming(object sender, EventArgs e)
        {
            Debug.Assert( _bumblebee != null);
            _bumblebee.Resume();
            _hive.Resume();
        }
        
        void _engine_InMovingPlates(object sender, EventArgs e)
        {
            ResetBehaviorEngineColors();
            MovingColor = Brushes.DarkGreen;
        }

        void _engine_InProcessingWorklist(object sender, EventArgs e)
        {
            // DKM 2011-10-13 need to change behavior engine colors to be a mask so we can support
            //                worklist processing and reinventorying at the same time
            ResetBehaviorEngineColors();
            ProcessingColor = Brushes.DarkGreen;
        }

        /*
        void _engine_InUnloadingPlate(object sender, EventArgs e)
        {
            ResetBehaviorEngineColors();
            UnLoadingColor = Brushes.DarkGreen;
        }
         */
        void _engine_InIdle(object sender, EventArgs e)
        {
            ResetBehaviorEngineColors();
            IdleColor = Brushes.DarkGreen;
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
            IdleColor = Brushes.DarkGray;
            ProcessingColor = Brushes.DarkGray;
            /*
            UnLoadingColor = Brushes.DarkGray;
             */
            MovingColor = Brushes.DarkGray;
            ReinventoryingColor = Brushes.DarkGray;
            UndockingColor = Brushes.DarkGray;
            ShutdownColor = Brushes.DarkGray;
            PausedColor = Brushes.DarkGray;
            HomingColor = Brushes.DarkGray;
            DockingColor = Brushes.DarkGray;
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

        /*
        void StaticStorage_ReinventoryBegin(object sender, EventArgs e)
        {
            ReinventoryEventArgs args = e as ReinventoryEventArgs;
            if (args != null && args.CalledFromDiags)
                return;
            _engine.FireReinventoryBegin();
        }
         */

        void All_ReinventoryComplete(object sender, EventArgs e)
        {
            try
            {
                string device_name = (sender as DeviceInterface).Name;
                // get the inventory from the specified dock / cart
                IEnumerable<KeyValuePair<string, string>> inventory = (sender as PlateStorageInterface).GetInventory("robot name not used");
                // send this info to JEMS
                //! \todo here, we need to decide what to do about system naming.  I am currently assuming that we'd name
                // our docks something like "HiveA Left" and "HiveB Right", and this would be the way for JEMS to
                // identify which system has which plates.
                _jems_client.ReinventoryComplete(_configuration.HiveName, device_name, (from x in inventory select x.Key).ToArray());
            }
            catch (Exception ex)
            {
                _log.ErrorFormat( "There was an error when attempting to send ReinventoryComplete message to GEMS.\nYou will need to redo the inventory operation for GEMS to receive the inventory information.\nThe error message was '{0}'", ex.Message);
            }
        }

        void OnJobComplete( object sender, JobCompleteEventArguments args)
        {
            if(( sender is LLSPlaceholder.LLSPlaceholder || sender is LLSensorPlugin) && ( OutputPlugin is MonsantoOutputTransferPlugin.MonsantoOutputTransferPlugin)){
                MonsantoOutputTransferPlugin.MonsantoOutputTransferPlugin mop = OutputPlugin as MonsantoOutputTransferPlugin.MonsantoOutputTransferPlugin;
                IList< MonsantoTransferData> transfer_data = mop.ExtractDestinationPlateData( args.PlateBarcode);
                IList< GemsRpc.TransferMap> mappings = transfer_data.Select( td => new TransferMap(){ source_barcode = td.SourceBarcode,
                                                                                                      source_row = new Well( td.SourceWell).RowIndex,
                                                                                                      source_column = new Well( td.SourceWell).ColIndex,
                                                                                                      destination_row = new Well( td.DestinationWell).RowIndex,
                                                                                                      destination_column = new Well( td.DestinationWell).ColIndex,
                                                                                                      transfer_volume = td.TargetVolume,
                                                                                                      sensed_volume = td.SensedVolume}).ToList();
                _destination_data_processing_db.SaveTransferMap( args.PlateBarcode, mappings);
            }
        }

        public void UpstackComplete(string barcode)
        {
            try
            {
                string device_name = ""; // plate is always upstacked to static storage
                string[] barcodes = { barcode };
                _jems_client.ReinventoryComplete(_configuration.HiveName, device_name, barcodes);
            }
            catch (Exception ex)
            {
                _log.Error( ex.Message);
                _log.ErrorFormat( "There was an error when attempting to send ReinventoryComplete message to GEMS during an Upstack operation.\nYou will need to perform a re-inventory operation for GEMS to receive the inventory information.\nThe error message was '{0}'", ex.Message);
            }
        }

        void dock_Docked(object sender, DockEventArgs e)
        {
            try
            {
                lock (_assigned_fates) { _assigned_fates[((DockMonitorPlugin)sender).Name].Clear(); }  // this is coming from a thread, so there's a race on _assigned_fates access
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
                    _assigned_fates[dock_plugin.Name].Clear();
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
                string config_path = BioNex.Shared.Utils.FileSystem.GetAppPath() + "\\config\\MonsantoPhase2Config.xml";
                _configuration = FileSystem.LoadXmlConfiguration<Configuration>(config_path);
            }
            catch (Exception ex)
            {
                _log.InfoFormat( "Could not load settings for customer GUI plugin: {0}", ex.Message);
            }
        }

        private void Heartbeat()
        {
            while (!_stop_heartbeat_thread.WaitOne(new TimeSpan(0, 0, 5)))
            {
                ConnectionStatusColor = _jems_client.Ping() ? Brushes.DarkGreen : Brushes.DarkRed;
                //VWorksConnectionStatusColor = _stacker_client.Ping() ? Brushes.DarkGreen : Brushes.DarkRed;
            }
        }

        #region ICustomerGUI Members

        // UNUSED BUT REQUIRED BY INTERFACE -- REMOVE FROM INTERFACE?
#warning DAVE -- Can we remove these from the ICustomerGUI interface, maybe make a separate interface for them?
#pragma warning disable 67
        public event EventHandler ProtocolComplete;
        public event EventHandler AbortableTaskStarted;
        public event EventHandler AbortableTaskComplete;
#pragma warning restore 67

        public string GUIName
        {
            get { return "Monsanto Phase 1 GUI"; }
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
            _stop_destination_data_processing_thread.Set();
            _destination_data_processing_thread.Join();

            _stop_heartbeat_thread.Set();
            _heartbeat_thread.Join();

            //_hive_server.Stop();
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
            // DKM 2011-10-24 added because Monsanto2 has RTD enabled while we're homing, but you can't do this if
            //                the DC2 is homing.  Should be okay for Monsanto1 as well...
            if (_engine.IsInState(BehaviorEngine.State.NotHomed) || _engine.IsInState(BehaviorEngine.State.Homing))
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

            // DKM 2011-10-13 TODO here we should check the dock_name to see if it's the one that
            //                owns the plates that are currently being processed by the worklist

            if (_engine.Paused)
            {
                reason_not_allowed = "Click the Go Live or the Resume button first";
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
            // DKM 2011-10-24 added because Monsanto2 has RTD enabled while we're homing, but you can't do this if
            //                the DC2 is homing.  Should be okay for Monsanto1 as well...
            if (_engine.IsInState(BehaviorEngine.State.NotHomed) || _engine.IsInState( BehaviorEngine.State.Homing))
            {
                reason_not_allowed = "Devices not homed yet";
                return false;
            }
            if (_engine.Paused)
            {
                reason_not_allowed = "Click the Go Live or the Resume button first";
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
        /// OnAddWorkset handles the XML-RPC message originating in GEMS
        ///     1. workset_xml is a workset file, pre-process if necessary, and hand it to the scheduler
        /// </summary>
        /// <param name="workset_xml"></param>
        void OnAddWorkset( string workset_xml)
        {
            MonsantoWorkset reader = new MonsantoWorkset();
            MonsantoProjects monsanto_projects = reader.ReadWorkset( workset_xml);

            string project_names = String.Join( ", ", monsanto_projects.Projects.Select( project => project.Name));
            string message = String.Format( "At {0}, projects received: {1}", DateTime.Now, project_names);

            IEnumerable< MonsantoSource> monsanto_sources = monsanto_projects.Projects.SelectMany( project => project.Sources);

            foreach( DockMonitorPlugin dock in _docks.Values){
                string location_name;
                if( dock.HasPlateWithBarcode( monsanto_sources.FirstOrDefault().Barcode, out location_name)){
                    _assigned_fates[ dock.Name] = new HashSet< string>(){ message};
                    FateDock1 = FateDock1; FateDock2 = FateDock2;
                }
            }

            EnqueueMonsantoProjects( monsanto_projects);
        }

        public void EnqueueMonsantoProjects( MonsantoProjects monsanto_projects)
        {
            foreach( MonsantoProject monsanto_project in monsanto_projects.Projects){
                _synapsis_view_model.EnqueueTransferOverview( monsanto_project.ToTransferOverview( LabwareDatabase), monsanto_project.Name, monsanto_project == monsanto_projects.Projects.Last());
            }
        }

        /*
        /// <summary>
        /// OnUpdatePlateFate handles the XML-RPC message originating in GEMS
        ///   1. Check to see if plate is in inventory
        ///   2. If not, discard message
        ///   3. Otherwise, persist message to DB in the form [barcode, fate, timestamp]
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="fate"></param>        
        void OnUpdatePlateFate(string barcode, string fate)
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
         */

        void PendingDestinationDataThread()
        {
            while (!_stop_destination_data_processing_thread.WaitOne(new TimeSpan(0, 0, 0, 0, 500)))
            {
                string destination_barcode;
                IList< BioNex.GemsRpc.TransferMap> mapping;
                if( !_destination_data_processing_db.GetNextTransferMap( out destination_barcode, out mapping))
                    continue;

                else ProcessPendingDestinationData(destination_barcode, mapping);
            }
        }

        void ProcessPendingDestinationData(string destination_barcode, IList< BioNex.GemsRpc.TransferMap> mapping)
        {
            // transmit the data to GEMS
            try {
                _jems_client.DestinationPlateComplete( _configuration.HiveName, destination_barcode, mapping.ToArray());
            } catch( Exception ex) {
                _log.InfoFormat( "Could not send data for destination plate '{0}': {1}.  Will retry again.", destination_barcode, ex.Message);
                // DKM 2011-11-01 is 10s reasonable enough?
                Thread.Sleep( 10000);
                return;
            }

            // remove from DB after successful transmit to GEMS
            _destination_data_processing_db.DeleteWork( destination_barcode);
        }

        /*
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
         */


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
            IList< string> hive_teachpoints_only = all_teachpoints[( _robot as DeviceInterface).Name];
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

        bool PlateIsInAnyCart(string barcode)
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

        bool PlateIsInStaticStorage(string barcode)
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

        bool IsThereAnAvailableStaticLocation()
        {
            return GetAvailableStaticLocations().Count() > 0;
        }

        string GetTrashLocationName()
        {
            // return the teachpoint reserved for the Trash location...  hopefully this is "Trash"
            return _special_fates[(int)SpecialFate.Trash];
        }

        /*
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
         */

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
            IList< string> dock_teachpoints_only = all_teachpoints[ dock];
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

        private string DecodeLabwareFromBarcode(string barcode)
        {
            //
            // 1. Open file - filename is a variable in _configuration
            // 2. For each line in file, split on comma
            // 3. First string is a regex string to match barcode
            // 4. Second string is a labware name
            //

            // eg a sample file might look like this:
            //
            //  ^[E].*,Nunc block
            //  ^[F].*,Abgene block
            //  ^[D].*,Axygen 120SQ-C PP Diamond Plate square well
            //

            //1.
            try
            {
                using (StreamReader sr = new StreamReader(_configuration.LabwareDecoderFileName))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        var words = line.Split(',');
                        if (System.Text.RegularExpressions.Regex.IsMatch(barcode, words[0]))
                            return words[1];
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error("Could not decode labware from barcode.  Error was: " + e.Message);
                throw new CouldNotDecodeLabwareFromBarcodeException("Could not decode labware from barcode.", e);
            }
            var msg = string.Format("Could not decode labware from barcode.  Failed to match the barcode to a regular expression contained in the file '{0}'", _configuration.LabwareDecoderFileName);
            _log.Error(msg);
            throw new CouldNotDecodeLabwareFromBarcodeException(msg);
        }

        /*
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
                _robot.TransferPlate(storage_with_plate.Name, plate_location_name, device, destination, labware, true, barcode);
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
         */

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

        public void EnterMovePlate( object sender, EventArgs e)
        {
            _engine.FireMovePlate();
        }

        public void ExitMovePlate( object sender, EventArgs e)
        {
            _engine.FireMovePlateComplete();
        }

        public void StartServices()
        {
            _synapsis_view_model.StartServices();
        }

        public void StopServices()
        {
            _synapsis_view_model.StopServices();
        }

        public void LoadAllStackers( BioNex.Shared.PlateDefs.Plate plate)
        {
            foreach( var kvp in _stackers) {
                StackerInterface stacker = kvp.Value;
                // DKM 2011-10-13 TODO we need to be passed the correct labware type when we start processing the worklist.
                //                So maybe this method shouldn't be called when we go live -- it should be called when we
                //                start a worklist.
                stacker.LoadStack( plate);
            }
        }
        void ReLoadBeeSureProperties()
        {
            var db = _device_manager.db;
            var dict = db.GetProperties(_beesure.Manufacturer, _beesure.ProductName, _beesure.Name);
            var device_info = new DeviceManagerDatabase.DeviceInfo(_beesure.Manufacturer, _beesure.ProductName, _beesure.Name, false, dict);
            _beesure.SetProperties(device_info);
        }
        void LSESaveProperties( Object sender, IDictionary<string, string> properties)
        {
            _device_manager.db.UpdateDevice(_beesure.Manufacturer, _beesure.ProductName, _beesure.Name, properties);

            // reload the properties to make sure the model has the correct values
            ReLoadBeeSureProperties();
        }    
    }
}
