using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using BioNex.Hive.Executor;
using BioNex.Hive.Hardware;
using BioNex.Shared.BarcodeMisreadDialog;
using BioNex.Shared.BioNexGuiControls;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.IError;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Location;
using BioNex.Shared.Microscan;
using BioNex.Shared.PlateWork;
using BioNex.Shared.TaskListXMLParser;
using BioNex.Shared.Teachpoints;
using BioNex.Shared.TeachpointServer;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.ThreadsafeMessenger;
using BioNex.Shared.Utils;
using GalaSoft.MvvmLight.Messaging;
using log4net;

namespace BioNex.HivePrototypePlugin
{
    //[PartCreationPolicy(CreationPolicy.NonShared)]
    // temporarily change this to see if it solves the issue with the Dispatcher
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(DeviceInterface))]
    [Export(typeof(RobotInterface))]
    [Export(typeof(PlateStorageInterface))]
    public partial class HivePlugin : AccessibleDeviceInterface, RobotInterface, PlateStorageInterface, 
                                      INotifyPropertyChanged, ProtocolHooksInterface, PlateSchedulerDeviceInterface
    {
#pragma warning disable 649
        [Import("MainDispatcher")]
        internal Dispatcher _dispatcher;
#pragma warning restore 649

        internal BioNex.Hive.Hardware.Configuration Config { get; set; }
        internal MicroscanReader BarcodeReader { get { return Hardware.BarcodeReader; }}
        internal ManualResetEvent AbortReinventoryEvent = new ManualResetEvent( false);
        internal ThreadsafeMessenger HiveMessenger { get; set; }
        private FlyByBarcodeReadingStateMachine FlyBySM { get; set; }
        private System.Windows.Window _diagnostics_window { get; set; }
        /// <summary>
        /// Allows the Hive to simulate the reinventory process when a barcode reader is simulated
        /// </summary>
        private IReinventoryStrategy _reinventory_strategy { get; set; }

        public UserControl Mini3Control
        {
            get {
                return ( BarcodeReader == null) ? null : BarcodeReader.GetConfigurationGui(); 
            }
        }

        /// <summary>
        /// which rack is selected in the listbox in the GUI
        /// </summary>
        public int SelectedRackIndex { get; set; }

        /// <summary>
        /// Each item in the ObservableCollection represents a rack.  The rack is
        /// represented by a list of objects that contain the rack number, slot number,
        /// plate barcode (if any), and whether or not the slot is occupied.
        /// </summary>
        public ObservableCollection<RackView> StaticInventoryView { get; set; }

        public void UpdateStaticInventoryView()
        {
            StaticInventoryView.Clear();
            _plate_location_manager.Clear();
            // get all of the inventory data from the inventory database
            // this is a map of barcode to location information ("rack", "slot", "loaded", etc)
            Dictionary<string, Dictionary<string,string>> inventory_data = Inventory.GetInventoryData();

            // we need to register the PlateTypeChanged handler for all racks that DO NOT
            // have plates in inventory.  Use the following variable to keep track of
            // rack that ARE in inventory, and then register the handler for those not
            // in the set.
            HashSet<int> racks_in_inventory = new HashSet<int>();

            foreach( KeyValuePair<string, Dictionary<string,string>> kvp in inventory_data) {
                string barcode = kvp.Key;
                try {
                    int rack_number = int.Parse( kvp.Value["rack"].ToString());
                    int slot_number = int.Parse( kvp.Value["slot"].ToString());
                    bool loaded = bool.Parse( kvp.Value["loaded"].ToString());
                    RackView rackview = _plate_location_manager.Racks[rack_number - 1];
                    rackview.SetSlotPlate( slot_number, barcode, loaded ? SlotView.SlotStatus.Loaded : SlotView.SlotStatus.Empty);

                    // only register the handler once, please
                    if( !racks_in_inventory.Contains( rack_number)) {
                        rackview.PlateTypeChanged += new RackView.PlateTypeChangedEventHandler(rackview_PlateTypeChanged);
                    }
                    racks_in_inventory.Add( rack_number);
                } catch( Exception ex) {
                    // couldn't get the rack and/or slot for whatever reason, so log this and continue
                    _log.Info( "Rack and slot information was not present in inventory data", ex);
                }
            }

            // register the PlateTypeChanged handler for the racks that aren't in inventory
            var racks_not_in_inventory = from x in Enumerable.Range( 1, _plate_location_manager.Racks.Count()) where !racks_in_inventory.Contains(x) select x;
            foreach( var x in racks_not_in_inventory) {
                RackView rackview = _plate_location_manager.Racks[x - 1];
                rackview.PlateTypeChanged += new RackView.PlateTypeChangedEventHandler(rackview_PlateTypeChanged);
            }

            // now add in the unbarcoded plates
            foreach( string location in UnbarcodedPlates) {
                HivePlateLocation pl = HivePlateLocation.FromString( location);
                _plate_location_manager.Racks[pl.RackNumber - 1].SetSlotPlate( pl.SlotNumber, "", SlotView.SlotStatus.Unknown);
            }

            // update the inventory view
            foreach( RackView rackview in _plate_location_manager.Racks)
                StaticInventoryView.Add( rackview);
        }

        void rackview_PlateTypeChanged(object sender, RackView.PlateTypeChangedEventArgs e)
        {
            // set the values in the configuration object
            Config.SetRackPlateType( e.RackNumber, e.RackType);
            SaveXmlConfiguration();
            _log.DebugFormat( "Set plate type in rack {0} to {1}", e.RackNumber, e.RackType);
        }

        internal void RackReinventoryCompleteHandler( RackReinventoryCompleteEvent ev)
        {
            // DKM 2011-03-15 added abort handler to reinventory, so don't update inventory if we aborted.
            if (ev.Barcodes == null)
                return;
            UpdateInventory( ev.RackNumber, ev.Barcodes, ev.StartingSlotNumber);
        }

        private void UpdateInventory( int rack_number, IList<string> barcodes, int starting_slot_number)
        {
            for( int i=0; i<barcodes.Count; i++) {
                // I didn't feel good about changing inventory around to accommodate unbarcoded plates
                // -- it's just too dangerous IMO.  So we need to look for the NOREAD "barcode" coming
                // in from the inventory function.
                // if the barcode is NOREAD, we have to add the entry to a list that keeps track of
                // unbarcoded plates
                // otherwise, add to Inventory as usual
                int slot_number = starting_slot_number + i;
                string location = new HivePlateLocation( rack_number, slot_number).ToString();
                if( Constants.IsNoRead(barcodes[i])) {
                    if( !UnbarcodedPlates.Contains( location))
                        UnbarcodedPlates.Add( location);
                    //! \todo maybe make a public method specifically for clearing a plate from inventory when we've
                    //!       found a tipbox?
                    Inventory.Load( "", new Dictionary<string,string> { {"rack", rack_number.ToString()}, {"slot", slot_number.ToString()}});
                } else {
                    // this plate could have replaced a previously unbarcoded plate, so do a lookup
                    // and remove from UnbarcodedPlates if present
                    UnbarcodedPlates.Remove( location);
                    Inventory.Load( barcodes[i], new Dictionary<string,string> { {"rack", rack_number.ToString()}, {"slot", slot_number.ToString()}});
                }
            }
            // call the update button handler
        }

        internal void UpdateInventoryLocation( string location_name, string barcode)
        {
            HivePlateLocation location = HivePlateLocation.FromString( location_name);
            Inventory.Load( barcode, new Dictionary<string,string> { {"rack", location.RackNumber.ToString()}, {"slot", location.SlotNumber.ToString()}});
        }

        internal void UpdateInventoryView()
        {
            // call the update button handler
            UpdateStaticInventoryView();
        }

        private void ReinventoryAllRacks( bool update_gui, bool park_robot_after)
        {
            // now only clear the unbarcoded plates before we do a full reinventory.  We don't want to
            // clear when we reinventory only selected racks, because we might be doing a spot check
            // on certain racks.
            UnbarcodedPlates.Clear();
            // build up the list of racks that we want to reinventory
            var selected_rack_numbers = from x in _plate_location_manager.Racks select x.RackNumber;
            // create the thread for reinventorying
            ReinventoryDelegate reinventory = new ReinventoryDelegate( _reinventory_strategy.ReinventorySelectedRacksThread);
            // pass update_gui for called_from_diags since we assume we're in diags if we want to update the gui for the Hive
            reinventory.BeginInvoke( selected_rack_numbers, UpdateInventoryView, update_gui, _reinventory_strategy.ReinventoryThreadComplete, park_robot_after);
        }

        private delegate void ShowDialogDelegate( List<BarcodeReadErrorInfo> misread_barcode_info, List<string> unbarcoded_plate_locations, Action gui_update_callback, UpdateInventoryLocationDelegate inventory_update_callback);

        /// <summary>
        /// Displays the barcode dialog so users can manually enter barcodes.  Also modifies the
        /// passed-in List of PlateLocations so this dialog can modify the list of unbarcoded plates.
        /// </summary>
        /// <param name="misread_barcode_info"></param>
        /// <param name="unbarcoded_plate_locations"></param>
        /// <param name="gui_update_callback"></param>
        /// <param name="inventory_update_callback"></param>
        private static void ShowBarcodeMisreadsDialog( List<BarcodeReadErrorInfo> misread_barcode_info, List<string> unbarcoded_plate_locations, Action gui_update_callback, UpdateInventoryLocationDelegate inventory_update_callback)
        {
            BarcodeMisread dlg = new BarcodeMisread( misread_barcode_info);
            dlg.ShowDialog();
            try {
                // now that we have the user-entered barcodes, go ahead and update inventory accordingly
                foreach( BarcodeReadErrorInfo info in dlg.Barcodes) {
                    if( !info.NoPlatePresent && info.NewBarcode.Trim() != "") {
                        if( unbarcoded_plate_locations != null)
                            unbarcoded_plate_locations.Remove( info.TeachpointName);
                        inventory_update_callback( info.TeachpointName, info.NewBarcode.Trim());
                    } else if( info.NoPlatePresent) {
                        unbarcoded_plate_locations.Remove( info.TeachpointName);
                        inventory_update_callback( info.TeachpointName, BioNex.Shared.LibraryInterfaces.Constants.Empty);
                    }
                }
                // DKM 2011-06-16 added null check to support using the dialog during pick and place
                if( gui_update_callback != null)
                    gui_update_callback();
            } catch( Exception ex) {
                MessageBox.Show( String.Format( "Could not save the manually-entered barcodes: {0}\r\nPlease reinventory the device.", ex.Message));
            }
        }

        /// <summary>
        /// Based on the scanning parameters and barcodes read so far, populates missed_barcode_info so the caller knows which
        /// slots needs to be re-checked with static images. 
        /// </summary>
        /// <remarks>
        /// After looking over this code, there are several confusing sections due to many changes over time with the
        /// reinventory behavior.  Reinventory needs to be re-written.  For example, why is reread_condition_masks
        /// passed into this function when it's already a member of sp, which is also passed in?  And I bet the values
        /// aren't even the same.  While the functionality works, there are too many inconsistencies right now.
        /// </remarks>
        /// <param name="sp"></param>
        /// <param name="barcodes"></param>
        /// <param name="missed_barcode_info"></param>
        /// <param name="reread_condition_masks"></param>
        internal void SaveBarcodeMisreadInfo( ScanningParameters sp, List<string> barcodes, List<BarcodeReadErrorInfo> missed_barcode_info, List<byte> reread_condition_masks)
        {
            for( int i=0; i<barcodes.Count(); i++) {
                byte mask = reread_condition_masks[i];
                string barcode = barcodes[i];
                int slot_number = sp.TopShelfNumber + i;
                // check the barcode against the mask
                if( (mask & ScanningParameters.RereadNoRead) != 0 && Constants.IsNoRead(barcode))
                    missed_barcode_info.Add( new BarcodeReadErrorInfo( (new HivePlateLocation( sp.RackNumber, slot_number)).ToString(), barcode));
                if( (mask & ScanningParameters.RereadMissedStrobe) != 0 && barcode == string.Empty)
                    missed_barcode_info.Add(new BarcodeReadErrorInfo((new HivePlateLocation(sp.RackNumber, slot_number)).ToString(), barcode));
                if( (mask & ScanningParameters.FoundBarcode) != 0 && !Constants.IsNoRead(barcode))
                    missed_barcode_info.Add(new BarcodeReadErrorInfo((new HivePlateLocation(sp.RackNumber, slot_number)).ToString(), barcode));
            }
        }

        // for DeviceInterface
        internal Dictionary<string,string> DeviceProperties { get; set; }

        //public DebugFile _mainDebugFile;
        internal static readonly ILog _log = LogManager.GetLogger( typeof( HivePlugin));

        // DeviceProperties keys
        internal static readonly string ConfigFolder = "configuration folder";
        // DKM 2011-04-25 taking this out because we want to base our rack configuration off of teachpoint information from now on
        //public static readonly string RackConfiguration = "rack configuration";

        //! \todo this should probably end up in the TechnosoftLibrary DLL
        public CommSelectionDialog _comm_selection_dialog = new CommSelectionDialog();
        
        // Engineering Tests
        public int EngTCT_msg_num_total = 500;
        public int EngTCT_msg_num = 0;
        public byte EngTCT_axis_id = 1;
        public int EngTD_num_devices = 0;

        public ControllerStatusCache StatusCache { get; private set; }

        public int Speed
        {
            get { return Hardware.Speed; }
            set { Hardware.Speed = value; }
        }
        
        private DoubleCollection _allowable_speeds;
        public DoubleCollection AllowableSpeeds
        {
            get { return _allowable_speeds; }
            set {
                _allowable_speeds = value;
                OnPropertyChanged( "AllowableSpeeds");
            }
        }

        // refs #542: this bug is more of a pain to fix than I had anticipated
        private int _max_allowable_speed = 20;
        public int MaxAllowableSpeed
        {
            get { return _max_allowable_speed; }
            set {
                _max_allowable_speed = value;
                OnPropertyChanged( "MaxAllowableSpeed");
            }
        }

        private bool Simulating { get { return Hardware.SimulatingRobot; }}
        private bool SimulatingBarcodeReader { get { return Hardware.SimulatingBarcodeReader; }}

        public HiveHardware Hardware { get; private set; }
        internal HiveExecutor ProtocolExecutor { get; set; }
        internal HiveExecutor DiagnosticsExecutor { get; set; }

        /// <summary>
        /// Indicates that the Hive robot is busy with pick and / or place
        /// </summary>
        public bool Running { get; set; }

        public HiveTeachpoint ReteachOffset { get; set; }

        [Import]
        public BioNex.Shared.LibraryInterfaces.ILabwareDatabase LabwareDatabase { get; set; }
        [Import]
        public IError ErrorInterface { get; set; }        
        [Import]
        public BioNex.Shared.LibraryInterfaces.IInventoryManagement Inventory { get; set; }
        [Import]
        public Lazy<ExternalDataRequesterInterface> DataRequestInterface { get; set; }

        internal PlateLocationManager _plate_location_manager { get; set; }
        internal List<string> UnbarcodedPlates { get; set; }

        private TeachpointServer _tpServer;
        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        // had to remove IError from constructor because this results in a circular dependency --
        // ended up injecting as a property instead
        public HivePlugin()
        {
            try {
                // do this first so that if there are any errors with preferences, we can still set the file paths
                InitializeCommands();
                InitializeNudgeTool();

                EngineeringTabVisibility = Visibility.Hidden;
                MaintenanceTabVisibility = Visibility.Hidden;
                SelectedRackIndex = -1;
                PlateOrientation = HiveTeachpoint.TeachpointOrientation.Portrait; // portrait by default
                _comm_selection_dialog.Close();
                TelemetryEnabled = true;
                XIncrement = YIncrement = ZIncrement = ThetaIncrement = GripperIncrement = 1;
                _status_text = "Disconnected";
                AllowableSpeeds = new DoubleCollection();
                AllowableSpeeds.Add( 20);                

                // #155
                NewApproachHeight = 7;
                TelemetryEnabled = true;

                StatusCache = new ControllerStatusCache( this);
                StaticInventoryView = new ObservableCollection<RackView>();
                UnbarcodedPlates = new List<string>();
                //TipboxSavedInventory = new List<PlateLocation>();
                HiveMessenger = new ThreadsafeMessenger();

                TeachpointFilter = "";
                TeachpointAFilter = "";

                /*
                // just for writing the auto-teach config file... leave commented out unless needed
                AutoTeachConfiguration config = new AutoTeachConfiguration();
                config.Panels.Add( new AutoTeachConfiguration.Panel { Name = "test panel", Origin = new AutoTeachConfiguration.Origin { X = 0, Y = 0, Z = 0 },
                                                                      RackCount = 5, SlotCount = 12, SlotXSpacing = 106, SlotZSpacing = 40 } );
                FileSystem.SaveXmlConfiguration<AutoTeachConfiguration>( config, "sample_auto_teach_config.xml");
                 */
            } catch( Exception ex) {
                //! \todo need to handle errors like this one without using MessageBox!
                MessageBox.Show( ex.Message);
            }
        }
        // ----------------------------------------------------------------------
        ~HivePlugin()
        {
            Messenger.Default.Unregister( this);
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public void Connect( bool connect)
        {
            try {
                LoadXmlConfiguration();
            } catch( KeyNotFoundException){
                MessageBox.Show(String.Format("Could not load plugin '{0}' because the 'configuration folder' property is missing from the device configuration database", Name));
            } catch( FileNotFoundException ex){
                MessageBox.Show(String.Format("Could not load plugin '{0}': {1}", Name, ex.Message));
            }

            if( Hardware == null){
                Hardware = new HiveHardware( Name, Config, DeviceProperties, HiveMessenger, _dispatcher);
                ProtocolExecutor = new HiveExecutor( Hardware, "Hive Protocol Executor", new Shared.IError.ErrorEventHandler( ProtocolExecutor_HandleError));
                ProtocolExecutor.Start();
                DiagnosticsExecutor = new HiveExecutor( Hardware, "Hive Diagnostic Executor", new BioNex.Shared.IError.ErrorEventHandler( DiagnosticsExecutor_HandleError));
                DiagnosticsExecutor.Start();
            }

            if( connect){
                LabwareNames = LabwareDatabase.GetLabwareNames();
                // this is the best time to register our event handler for labware values changing
                LabwareDatabase.LabwareChanged += new EventHandler(LabwareDatabase_LabwareChanged);

                // #159: force the user to pick a labware name
                /*
                if( LabwareNames.Count > 0)
                    PickAndPlaceLabware = LabwareNames[0];
                */
                try {
                    // initialize inventory stuff first since we'll need to populate it from the teachpoint processing code
                    string inventory_path = (DeviceProperties[ConfigFolder] + "\\inventory.s3db").ToAbsoluteAppPath();
                    try {
                        Inventory.LoadDatabase( inventory_path, new List<string> { "rack", "slot"});
                    } catch( BioNex.Shared.LibraryInterfaces.InventoryFileDoesNotExistException ex) {
                        MessageBoxResult response = MessageBox.Show( String.Format( "The inventory file {0} was not found.  Would you like to create it?", ex.FilePath), "Hive Inventory", MessageBoxButton.YesNo);
                        if( response == MessageBoxResult.Yes) {
                            Inventory.CreateDatabase( ex.FilePath, new List<string> { "rack", "slot" });
                        }
                    }

                    // DKM 2011-04-25 no longer needed because we'll be basing rack configuration off of teachpoints
                    /*
                    // device manager handles the rack configuration.  stored as a comma-delimited set of numbers
                    List<int> rack_configuration = new List<int>();
                    try {
                        string config_string = DeviceProperties[RackConfiguration];
                        // I think a more sane way of doing this is to use:
                        // rack_configuration = (from s in config_string.Split( ',') select int.Parse( s)).ToList();
                        rack_configuration = new List<int>((from s in config_string.Split( ',') select int.Parse( s.ToString())).ToArray());
                    } catch (KeyNotFoundException ) {
                        MessageBox.Show( String.Format( "Cannot configure plate locations in '{0}' because the 'rack configuration' property is not specified in the device manager database", Name));
                    }
                     */

                    // DKM 2011-04-25 now load teachpoints before creating the plate location manager, since we
                    //                need Hive teachpoint information to figure out how to create our internal
                    //                storage representation
                    IEnumerable< DeviceInterface> accessible_devices = DataRequestInterface.Value.GetAccessibleDeviceInterfaces();
                                                // DKM 2010-09-20 need to fix this query, since it fails when I want to ignore devices w/o locations
                                                //where p.GetPlateLocationNames() != null && p.GetPlateLocationNames().Count() > 0 
                                                // select p;
                    // load the teachpoint file for each of the plate handler devices, if possible
                    foreach( DeviceInterface di in accessible_devices){
                        // note that we don't want to use LoadExternalTeachpoints for this device,
                        // since all Hive devices have the same teachpoint name
                        LoadDeviceTeachpoints(di.Name);
                    }

                    // DKM 2011-06-03 it is lame that the teachpoints we use for storage locations is tied to AutoGenerate
                    //                so now _plate_location_manager will get created based on the DeviceTeachpoints instead
                    //                of whatever AutoRackConfiguration.RackConfiguration spits out
                    /*
                    // DKM 2011-04-25 
                    AutoRackConfiguration arc = new AutoRackConfiguration( DeviceTeachpoints[Name]);
                    Dictionary<int,List<AutoShelfConfiguration>> rack_configuration = arc.RackConfiguration;                
                    AutoTeachStateMachine.RenameTeachpoints(DeviceTeachpoints[Name], "renamed_teachpoints.xml");
                     */
    
                    // DKM 2011-06-03 use DeviceTeachpoints instead of AutoRackConfiguration
                    Dictionary<int,List<AutoShelfConfiguration>> rack_configuration = ParseRackConfigurationFromTeachpoints( Hardware.GetTeachpointNames( Name));
                    _plate_location_manager = new PlateLocationManager( Inventory, Config, rack_configuration);
                    
                    Hardware.Connect();
                    Hardware.ConnectBcr();

                    StatusText = ( Hardware.SimulatingRobot) ? "Connected to Simulator" : "Connected to HW";

                    // DKM 2011-06-06 get the barcode confirmation flag from the database, if it exists
                    /*
                    string temp;
                    if( DeviceProperties.TryGetValue( SkipBarcodeConfirmationString, out temp))
                        SkipBarcodeConfirmation = temp != "0";
                    else
                        SkipBarcodeConfirmation = false;
                    */
                    
                    // start the teachpoint service
                    _tpServer = new TeachpointServer( new TeachpointServiceImplementation(this), Name, Config.TeachpointServicePort);

                    AccessibleDeviceView = CollectionViewSource.GetDefaultView( accessible_devices);
                    AccessibleDeviceView.CurrentChanged += new EventHandler(AccessibleDeviceView_CurrentChanged);
                    // IMPORTANT NOTE: here, I'm using accessible_devices.ToList() because I don't want changing the AccessibleDeviceView
                    //                 to be synchronized with the DeviceAView.  However, I DO want to synch up AccessibleDeviceView with
                    //                 DeviceBView, since device B is the one that gets nudged.
                    DeviceAView = CollectionViewSource.GetDefaultView( accessible_devices.ToList());
                    DeviceAView.CurrentChanged += new EventHandler(DeviceAView_CurrentChanged);
                    DeviceBView = CollectionViewSource.GetDefaultView( accessible_devices);
                    DeviceBView.CurrentChanged += new EventHandler(DeviceBView_CurrentChanged);
                   
                    ReloadMotorSettings();
                    // create the barcode reader object
                    // DKM 2011-03-17 I don't know why I didn't call it "BCR COM port"...
                    // int bcr_port = int.Parse( DeviceProperties["barcode COM port"]);
                    
                    // ---------------- BCR handling ------------------
                    /*
                    _bcr_simulated = false;
                    try {
                        _bcr_simulated = DeviceProperties[SimulateBCR] != "0";
                    } catch( KeyNotFoundException) {
                        // it's possible that the key for barcode simulation isn't available.  If not, then
                        // just assume that we don't want to simulate.
                    }
                    */

                    if( !Hardware.SimulatingBarcodeReader){
                        _reinventory_strategy = new BarcodeReinventory( this);
                    } else{
                        _reinventory_strategy = new SimulatedReinventory( this);
                    }

                    // wire up the events on Hive plugin that Synapsis needs
                    _reinventory_strategy.ReinventoryStrategyComplete += new EventHandler(_reinventory_strategy_ReinventoryStrategyComplete);
                    _reinventory_strategy.ReinventoryStrategyError += new EventHandler(_reinventory_strategy_ReinventoryStrategyError);
                    // -------------------------------------------------

                    Initialized = true;
                    // #393: need to reset the controller and home status every time we connect, because it's entirely possible
                    //       that 80V went down between instances of Synapsis, and we could think that the motor controller
                    //       is homed when it really isn't!
                    // DKM 2011-04-05 this is a really annoying "feature", so make it optional.
                    if( Config.ForceRehome)
                        Hardware.ResetAndSetHomeStatus( 0x1234);
                    StatusCache.Start();
                } catch( AxisException ex) {
                    string error_message = String.Format( "Failed to connect to device {0} on port {1}: {2}", _comm_selection_dialog.CurrentDevice, _comm_selection_dialog.CurrentPort, ex.Message);
                    throw new ApplicationException(error_message);
                } catch( InvalidMotorSettingsException ex) {
                    string error = String.Format( "Error parsing motor settings for axis {0}.  The current value for the setting named {1} is either missing, or invalid.  The valid range is between {2} and {3}.", ex.AxisID, ex.SettingName, ex.MinValue, ex.MaxValue);
                    MessageBox.Show( error + "\n\n(this messagebox will eventually be replaced by an entry in the log)");
                }
            } else {
                StatusCache.Stop();
                Hardware.Disconnect();
                Hardware.DisconnectBcr();
                Initialized = false;
                StatusText = "Disconnected";
            }
        }
        // ----------------------------------------------------------------------
        void ProtocolExecutor_HandleError( object sender, ErrorData error_data)
        {
            ErrorInterface.AddError( error_data);
        }
        // ----------------------------------------------------------------------
        void DiagnosticsExecutor_HandleError( object sender, ErrorData error_data)
        {
            error_data.AddEvent( "Abort");
            _dispatcher.BeginInvoke( new AddErrorDelegate( AddErrorSTA), error_data);
        }
        // ----------------------------------------------------------------------
        private static Dictionary< int, List< AutoShelfConfiguration>> ParseRackConfigurationFromTeachpoints( IEnumerable< string> teachpoint_names)
        {
            Dictionary<int, List<AutoShelfConfiguration>> config = new Dictionary<int,List<AutoShelfConfiguration>>();
            foreach( String teachpoint_name in teachpoint_names){
                if( !HivePlateLocation.IsValidPlateLocationName( teachpoint_name))
                    continue;

                HivePlateLocation pl = HivePlateLocation.FromString( teachpoint_name);
                // check to see if the rack already exists.  If not, create a new List<AutoShelfConfiguration>
                int rack_index = pl.RackNumber - 1;
                if( !config.ContainsKey( rack_index))
                    config.Add( rack_index, new List<AutoShelfConfiguration>());
                // Giles' stuff was all 0-based, even though the variable is named "*number"
                config[rack_index].Add( new AutoShelfConfiguration { original_tp_name = teachpoint_name, shelf_number = pl.SlotNumber - 1 });
            }
            return config;
        }
        // ----------------------------------------------------------------------
        void LabwareDatabase_LabwareChanged( object sender, EventArgs e)
        {
            LabwareNames = LabwareDatabase.GetLabwareNames();
        }
        // ----------------------------------------------------------------------
        void _reinventory_strategy_ReinventoryStrategyError( object sender, EventArgs e)
        {
            if( ReinventoryError != null)
                ReinventoryError( this, e);
        }
        // ----------------------------------------------------------------------
        void _reinventory_strategy_ReinventoryStrategyComplete( object sender, EventArgs e)
        {
            // mark the entire cart vacant.
            foreach( PlateLocation location in PlateLocations.Values){
                location.Occupied.Reset();
            }
            // figure out which locations are occupied.
            IEnumerable< KeyValuePair< string, string>> inventory = GetInventory( "not currently used");
            var occupied_locations = from location in PlateLocations
                                     from kvp in inventory
                                     where location.Key == kvp.Value
                                     select location.Value;
            // mark the occupied slots occupied.
            foreach( PlateLocation occupied_location in occupied_locations){
                occupied_location.Occupied.Set();
            }

            if( ReinventoryComplete != null)
                ReinventoryComplete( this, e);
        }
        // ----------------------------------------------------------------------
        void TeachpointBNames_CurrentChanged( object sender, EventArgs e)
        {
            ICollectionView view = sender as ICollectionView;
            if( view == null || TeachpointANames == null)
                return;
        }
        // ----------------------------------------------------------------------
        void TeachpointANames_CurrentChanged( object sender, EventArgs e)
        {
            ICollectionView view = sender as ICollectionView;
            if( view == null || TeachpointBNames == null)
                return;
        }
        // ----------------------------------------------------------------------
        void TeachpointNames_CurrentChanged( object sender, EventArgs e)
        {
            ICollectionView view = sender as ICollectionView;
            if( view == null || view.CurrentItem == null)
                return;

            // get the positions for this teachpoint and update the databound properties accordingly
            // need to treat the Hive and other plate storage devices differently
            // DKM 2012-01-19 seems that generic impl now throws a TeachpointNotFoundException instead of returning null, so made necessary changes here
            HiveTeachpoint tp = null;
            try
            {
                tp = Hardware.GetTeachpoint(SelectedDevice.Name, view.CurrentItem.ToString());
            }
            catch (TeachpointNotFoundException)
            { 
                _log.DebugFormat( "Teachpoint '{0}' is undefined, so x = y = z = approach_height = 0!", view.CurrentItem.ToString());
            }

            TeachpointPosition.X = tp == null ? 0 : tp.X;
            TeachpointPosition.Y = tp == null ? 0 : tp.Y;
            TeachpointPosition.Z = tp == null ? 0 : tp.Z;
            // #155
            TeachpointPosition.ApproachHeight = tp == null ? 7 : tp.ApproachHeight;
            TeachpointPosition.Orientation = tp == null ? HiveTeachpoint.TeachpointOrientation.Portrait : tp.Orientation;

            NewApproachHeight = TeachpointPosition.ApproachHeight;
            PlateOrientation = TeachpointPosition.Orientation;
            OnPropertyChanged( "TeachpointPosition");
            ToolTipMoveToTeachpoint = String.Format( "Move to teachpoint '{0}'", view.CurrentItem.ToString());
        }
        // ----------------------------------------------------------------------
        void DeviceBView_CurrentChanged( object sender, EventArgs e)
        {
            ICollectionView view = sender as ICollectionView;
            if (view == null)
                return;
            TeachpointFilter = "";

            ReloadDeviceBTeachpoints();
        }
        // ----------------------------------------------------------------------
        private void ReloadDeviceBTeachpoints()
        {
            AccessibleDeviceInterface selected_device_b = SelectedDeviceB;
            if( selected_device_b == null)
                return;

            TeachpointBNames = CollectionViewSource.GetDefaultView( LoadTeachpointNamesForDevice( selected_device_b));
            TeachpointBNames.CurrentChanged += new EventHandler( TeachpointBNames_CurrentChanged);
            TeachpointBNames.Filter = ApplyFilter;
            OnPropertyChanged( "TeachpointBNames");
        }
        // ----------------------------------------------------------------------
        void DeviceAView_CurrentChanged( object sender, EventArgs e)
        {
            ICollectionView view = sender as ICollectionView;
            if (view == null)
                return;
            TeachpointAFilter = "";

            ReloadDeviceATeachpoints();
        }
        // ----------------------------------------------------------------------
        private void ReloadDeviceATeachpoints()
        {
            AccessibleDeviceInterface selected_device_a = SelectedDeviceA;
            if( selected_device_a == null)
                return;

            TeachpointANames = CollectionViewSource.GetDefaultView( LoadTeachpointNamesForDevice( selected_device_a));
            TeachpointANames.CurrentChanged += new EventHandler( TeachpointANames_CurrentChanged);
            TeachpointANames.Filter = ApplyFilterA;
            OnPropertyChanged( "TeachpointANames");
        }
        // ----------------------------------------------------------------------
        private void ReloadMotorSettings()
        {
            Hardware.LoadMotorSettings();
        }
        // ----------------------------------------------------------------------
        /// <remarks>
        /// need to figure out a nice way to deal with GUI / non-GUI operation
        /// </remarks>
        /// <param name="from_gui"></param>
        public void ReloadAllDeviceTeachpoints( bool from_gui)
        {
            // cache current teachpoint selections.
            string last_selected_teachpoint = SelectedTeachpointName;
            string last_a = SelectedTeachpointAName;
            string last_b = SelectedTeachpointBName;

            // clear out all teachpoint names.
            TeachpointNames = null;
            TeachpointANames = null;
            TeachpointBNames = null;

            // reload all device teachpoints.
            foreach( DeviceInterface di in AccessibleDeviceView.SourceCollection)
                LoadDeviceTeachpoints( di.Name);

            // restore teachpoint names for the currently selected devices.
            ReloadDeviceTeachpoints();
            ReloadDeviceATeachpoints();
            ReloadDeviceBTeachpoints();

            // only use last_selected_teachpoint if it satisfies the filter
            if( last_selected_teachpoint != null){
                if( ApplyFilter( last_selected_teachpoint))
                    TeachpointNames.MoveCurrentTo( last_selected_teachpoint);
                else
                    TeachpointNames.MoveCurrentToFirst();
            }
            if( last_a != null){
                if( ApplyFilter( last_a))
                    TeachpointANames.MoveCurrentTo( last_a);
                else
                    TeachpointANames.MoveCurrentToFirst();
            }
            if( last_a != null){
                if( ApplyFilter( last_b))
                    TeachpointBNames.MoveCurrentTo( last_b);
                else
                    TeachpointBNames.MoveCurrentToFirst();
            }
            OnPropertyChanged( "TeachpointNames");
            OnPropertyChanged( "TeachpointANames");
            OnPropertyChanged( "TeachpointBNames");
        }
        // ----------------------------------------------------------------------
        void AccessibleDeviceView_CurrentChanged( object sender, EventArgs e)
        {
            ICollectionView view = sender as ICollectionView;
            if (view == null)
                return;

            // clear the teachpoint filter to avoid having to do null checks everywhere.  Also
            // makes sense since if you're using a filter, you only want to affect a single device
            TeachpointFilter = "";
            ReloadDeviceTeachpoints();
        }
        // ----------------------------------------------------------------------
        private void ReloadDeviceTeachpoints()
        {
            AccessibleDeviceInterface selected_device = SelectedDevice;
            if( selected_device == null)
                return;
            
            TeachpointNames = CollectionViewSource.GetDefaultView( LoadTeachpointNamesForDevice( selected_device));
            TeachpointNames.CurrentChanged += new EventHandler( TeachpointNames_CurrentChanged);
            TeachpointNames.Filter = ApplyFilter;
            OnPropertyChanged( "TeachpointNames");
        }
        // ----------------------------------------------------------------------
        private bool ApplyFilter( object obj)
        {
            string teachpoint_name = obj as string;
            if( teachpoint_name == null)
                return false;
            // if no filter, return all results
            if( TeachpointFilter == "")
                return true;
            // otherwise, only show items that match, case-insensitive
            return teachpoint_name.ToLower().Contains( TeachpointFilter.ToLower());
        }
        // ----------------------------------------------------------------------
        private bool ApplyFilterA( object obj)
        {
            string teachpoint_name = obj as string;
            if( teachpoint_name == null)
                return false;
            // if no filter, return all results
            if( TeachpointAFilter == "")
                return true;
            // otherwise, only show items that match, case-insensitive
            return teachpoint_name.ToLower().Contains( TeachpointAFilter.ToLower());
        }
        // ----------------------------------------------------------------------
        /// <remarks>
        /// GUI handler should catch the exceptions thrown from this method
        /// </remarks>
        /// <exception cref="AxisException" />
        /// <param name="axis_id"></param>
        public void ServoOn( byte axis_id)
        {
            new EnableAxisJob( DiagnosticsExecutor, axis_id).Dispatch();
            switch( axis_id) {
                case HiveHardware.ID_X_AXIS:
                    ServoOnStatus.X = true;
                    break;
                case HiveHardware.ID_Z_AXIS:
                    ServoOnStatus.Z = true;
                    break;
                case HiveHardware.ID_T_AXIS:
                    ServoOnStatus.T = true;
                    break;
                case HiveHardware.ID_G_AXIS:
                    ServoOnStatus.G = true;
                    break;
            }
            OnPropertyChanged( "ServoOnStatus");
        }
        // ----------------------------------------------------------------------
        public void ServoOff( byte axis_id)
        {
            new DisableAxisJob( DiagnosticsExecutor, axis_id).Dispatch();
            switch( axis_id) {
                case HiveHardware.ID_X_AXIS:
                    ServoOnStatus.X = false;
                    break;
                case HiveHardware.ID_Z_AXIS:
                    ServoOnStatus.Z = false;
                    break;
                case HiveHardware.ID_T_AXIS:
                    ServoOnStatus.T = false;
                    break;
                case HiveHardware.ID_G_AXIS:
                    ServoOnStatus.G = false;
                    break;
            }
            OnPropertyChanged( "ServoOnStatus");
        }
        // ----------------------------------------------------------------------
        public void UpdateHomingStatus()
        {
            AxisHomeStatus.X = Hardware.XAxis.IsHomed;
            AxisHomeStatus.Z = Hardware.ZAxis.IsHomed;
            AxisHomeStatus.T = Hardware.TAxis.IsHomed;
            AxisHomeStatus.G = Hardware.GAxis.IsHomed;
            OnPropertyChanged( "AxisHomeStatus");
        }
        // ----------------------------------------------------------------------
        public void UpdateServoOnStatus()
        {
            ServoOnStatus.X = Hardware.XAxis.IsOn();
            ServoOnStatus.Z = Hardware.ZAxis.IsOn();
            ServoOnStatus.T = Hardware.TAxis.IsOn();
            ServoOnStatus.G = Hardware.GAxis.IsOn();
            OnPropertyChanged( "ServoOnStatus");
        }
        // ----------------------------------------------------------------------
        private IDictionary< string, PlateLocation> PlateLocations { get; set; }
        // ----------------------------------------------------------------------
        public void LoadDeviceTeachpoints( string device_name, string dockable_id = null)
        {
            AccessibleDeviceInterface accessible_device = DataRequestInterface.Value.GetAccessibleDeviceInterfaces().FirstOrDefault( di => di.Name == device_name);

            try{
                Hardware.LoadTeachpoints( accessible_device);
                // if we're loading teachpoints for this device and we haven't yet created the list of PlateLocations, then create the list of PlateLocations.
                if(( accessible_device == this) && ( PlateLocations == null)){
                    PlateLocations = Hardware.GetTeachpointNames( device_name).ToDictionary( tp_name => tp_name, tp_name => new PlateLocation( tp_name));
                }
            } catch( FileNotFoundException){
                // it's totally okay if we don't have a teachpoint file for a specific device!
                // but we should still log it!
                _log.InfoFormat( "Couldn't find teachpoints for '{0}'", accessible_device);
            } catch( DirectoryNotFoundException){
                _log.InfoFormat( "Couldn't find teachpoints for '{0}'", accessible_device);
            } catch( Exception ex){
                _log.InfoFormat( "Could not load teachpoint for '{0}': {1}", accessible_device, ex.Message);
            }
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// called directly for hive device, to save custom teachpoints (i.e. those not
        /// defined by location name)
        /// </summary>
        /// <param name="teachpoint_name"></param>
        /// <param name="lw"></param>
        private void SaveExternalTeachpoint( string teachpoint_name, ILabware lw)
        {
            if( SelectedDevice == null){
                throw new Exception( "FYC later");
            }
            Hardware.SetTeachpoint( SelectedDevice.Name, new HiveTeachpoint( teachpoint_name,
                                                                             Hardware.CurrentToolPosition.X,
                                                                             Hardware.CurrentToolPosition.Y,
                                                                             Hardware.CurrentToolPosition.Z - lw[ LabwarePropertyNames.GripperOffset].ToDouble(),
                                                                             NewApproachHeight,
                                                                             PlateOrientation));
            Hardware.SaveTeachpoints( SelectedDevice);
        }
        // ----------------------------------------------------------------------
        public void ReteachHere( string teachpoint_name)
        {
            if( TeachpointNames.CurrentItem == null)
                return;

            if( !LabwareDatabase.IsValidLabwareName( PickAndPlaceLabware)) {
                if( MessageBoxResult.No ==  MessageBox.Show( "Are you sure you want to teach with the labware '" + PickAndPlaceLabware + "'?", "Confirm labware", MessageBoxButton.YesNo))
                    return;
            }

            Hardware.UpdateCurrentPosition();
            // calculate deltas for all teachpoints
            ReteachOffset = new HiveTeachpoint( SelectedTeachpoint);
            ReteachOffset -= Hardware.CurrentToolPosition;
            // remember that the old teachpoint stores the Z position where the gripper is touching the top of the platepad
            // so we have to take the current robot position and subtract off the labware's gripper offset as well
            // here, we have to ADD the labware's min gripper offset because the calculation for ReteachOffset is
            // really supposed to be something like ReteachOffset.Z -= CurrentToolPosition.Z - lw.MinGripperOffset,
            // so the sign gets inverted for when we do it as two separate steps
            ReteachOffset.Z += SelectedLabware[LabwarePropertyNames.GripperOffset].ToDouble();
            // overwrite previous teachpoint's values
            HiveTeachpoint new_tp = new HiveTeachpoint( SelectedTeachpoint.Name,
                                                        Hardware.CurrentToolPosition.X,
                                                        Hardware.CurrentToolPosition.Y,
                                                        Hardware.CurrentToolPosition.Z - SelectedLabware[ LabwarePropertyNames.GripperOffset].ToDouble(),
                                                        NewApproachHeight,
                                                        PlateOrientation);
            Hardware.SetTeachpoint( SelectedDevice.Name, new_tp);
            SaveTeachpointFile( SelectedDevice);
            // now allow the user to reteach all of the other teachpoints
            ReteachUserPrompt = String.Format( "The teachpoint '{0}' was retaught successfully.  Would you like to apply the same offsets to ALL other teachpoints?", SelectedTeachpoint.Name);
            // create a list of all of the teachpoint changes so the user can see them
            ReteachPreview = new ObservableCollection<ReteachPreviewInfo>();

            // DKM 2011-08-29 brought back this feature, but now it adjusts teachpoints across ALL devices
            IEnumerable< AccessibleDeviceInterface> accessible_devices = DataRequestInterface.Value.GetAccessibleDeviceInterfaces();
            foreach( AccessibleDeviceInterface accessible_device in accessible_devices){
                foreach( string tp_name in Hardware.GetTeachpointNames( accessible_device.Name)){
                    if(( accessible_device == SelectedDevice) || ( tp_name == SelectedTeachpointName)){
                        continue;
                    }
                    HiveTeachpoint tp = Hardware.GetTeachpoint( accessible_device.Name, tp_name);
                    // skip old_tp because that's the teachpoint we just retaught
                    ReteachPreview.Add( new ReteachPreviewInfo
                    {
                        Name = tp.Name,
                        OldX = tp.X,
                        OldY = tp.Y,
                        OldZ = tp.Z,
                        NewX = tp.X - ReteachOffset.X,
                        NewY = tp.Y - ReteachOffset.Y,
                        NewZ = tp.Z - ReteachOffset.Z,
                        IsSelected = false
                    });
                }
            }
            // now allow the user to apply these offsets everywhere
            ShowReteachOptions();
        }
        // ----------------------------------------------------------------------
        public void SaveTeachpointFile( AccessibleDeviceInterface device)
        {
            Hardware.SaveTeachpoints( device);
            LoadDeviceTeachpoints( device.Name);
        }
        // ----------------------------------------------------------------------
        private delegate void AddErrorDelegate( ErrorData error);
        // ----------------------------------------------------------------------
        private static void AddErrorSTA( ErrorData error)
        {
            if( Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "Add Hive GUI error";
            BioNex.Shared.ErrorHandling.ErrorDialog dlg = new BioNex.Shared.ErrorHandling.ErrorDialog( error);
            // #179 need to use modeless dialogs, because modal ones prevent us from being able to reset the interlocks via software
            dlg.Show();
        }
        // ----------------------------------------------------------------------
        internal ObservableCollection< string> LoadTeachpointNamesForDevice( AccessibleDeviceInterface device)
        {
            return ( device != null) 
                ? new ObservableCollection< string>( GetTeachpointNames( device, false))
                : new ObservableCollection< string>();
        }
        // ----------------------------------------------------------------------
        internal IList< string> GetTeachpointNames( string device_name)
        {
            var device = DataRequestInterface.Value.GetAccessibleDeviceInterfaces().FirstOrDefault( adi => adi.Name == device_name);
            return GetTeachpointNames( device, false);
        }
        // ----------------------------------------------------------------------
        private IList< string> GetTeachpointNames( AccessibleDeviceInterface device, bool places_only)
        {
            var plate_locations = device.PlateLocationInfo;
            var lid_locations = plate_locations.Select( plate_location => device.GetLidLocationInfo( plate_location.Name)).Where( lid_location => lid_location != null);
            var all_locations = plate_locations.Union( lid_locations);
            var retval = all_locations.SelectMany( location => location.Places).Select( place => place.Name);
            if( !places_only && ( device == this)){
                retval = retval.Union( Hardware.GetTeachpointNames( device.Name));
            }
            return retval.Distinct().OrderBy( name => name).ToList();
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Moving "with plate" will place the plate at the current teachpoint and will not ungrip
        /// Moving "without plate" was originally removed because it was unsafe for the new executor,
        /// but now it is intended to be used for barcode reading purposes.  It will move the robot
        /// to the teachpoint X and Z, but not do anything at all with theta or the gripper.
        /// </summary>
        /// <param name="device_name"></param>
        /// <param name="teachpoint_name"></param>
        /// <param name="with_plate"></param>
        /// <param name="blocking"></param>
        public void MoveToTeachpoint( string device_name, string teachpoint_name, bool with_plate, bool blocking)
        {
            ManualResetEvent job_complete = new ManualResetEvent( false);
            ILabware labware = LabwareDatabase.GetLabware( "tipbox"); // FYC -- hardcoded labware should be a method parameter.
            if( with_plate){
                new PickAndOrPlaceJob( ProtocolExecutor, job_complete, PickAndOrPlaceJob.PickAndOrPlaceJobOption.MoveToTeachpointWithPlate, Hardware.GetTeachpoint( device_name, teachpoint_name), labware).Dispatch();
            } else{
                new PickAndOrPlaceJob( ProtocolExecutor, job_complete, PickAndOrPlaceJob.PickAndOrPlaceJobOption.MoveToTeachpointWithoutPlate, DataRequestInterface.Value.GetAccessibleDeviceInterfaces().FirstOrDefault( adi => adi.Name == device_name), Hardware.GetTeachpoint( device_name, teachpoint_name), labware, new MutableString()).Dispatch();
            }
            if( blocking){
                job_complete.WaitOne();
            }
        }
        // ----------------------------------------------------------------------
        public void ShowReteachOptions()
        {
            // here, we want to give the user the option of reteaching all other points based off
            // the newly-calculated ReteachOffset value.
            ReteachDialog dlg = new ReteachDialog( this);
            dlg.ShowDialog();
            // check user selection
            switch( dlg.UserSelection) {
                case ReteachDialog.Selection.ApplyAll:
                    ApplyOffsetToAllTeachpoints();
                    break;
                //case ReteachDialog.Selection.ApplySelected:
                //    ApplyOffsetToSelectedTeachpoints();
                //    break;
                default:
                    break;
            }
            dlg.Close();
        }
        // ----------------------------------------------------------------------
        private void ApplyOffsetToAllTeachpoints()
        {
            // DKM 2011-08-29 need to write the file for all devices
            foreach( AccessibleDeviceInterface accessible_device in DataRequestInterface.Value.GetAccessibleDeviceInterfaces()){
                // iterate over all of the teachpoints except for the current teachpoint
                foreach( string teachpoint_name in Hardware.GetTeachpointNames( accessible_device.Name)){
                    // skip the retaught teachpoint.
                    if(( accessible_device == SelectedDevice) && ( teachpoint_name == SelectedTeachpointName)){
                        continue;
                    }
                    HiveTeachpoint tp = Hardware.GetTeachpoint( accessible_device.Name, teachpoint_name);
                    Hardware.SetTeachpoint( accessible_device.Name, new HiveTeachpoint( tp.Name, tp.X - ReteachOffset.X, tp.Y - ReteachOffset.Y, tp.Z - ReteachOffset.Z, tp.ApproachHeight, tp.Orientation));
                }
                SaveTeachpointFile( accessible_device);
            }
        }
        // ----------------------------------------------------------------------
        #region AccessibleDeviceInterface Members
        // ----------------------------------------------------------------------
        #region DeviceInterface Members
        // ----------------------------------------------------------------------
        public void Connect()
        {
            Connect( true);
            // DO NOT EVER HOME AXES FROM CONNECT()!!!
        }
        // ----------------------------------------------------------------------
        public bool Connected { get { return Initialized; }}
        // ----------------------------------------------------------------------
        public void Home()
        {
            new HomeAllAxesJob( ProtocolExecutor).Dispatch();
        }
        // ----------------------------------------------------------------------
        public bool IsHomed
        {
            get
            {
                try
                {
                    // DKM 2010-09-28 no longer need this because we update AxisHomeStatus in a worker thread
                    //UpdateHomingStatus();
                    return AxisHomeStatus.X && AxisHomeStatus.Z && AxisHomeStatus.T && AxisHomeStatus.G;
                }
                catch (NullReferenceException)
                {
                    // if here, the device hasn't initialized yet so try again later
                    return false;
                }
            }
        }
        // ----------------------------------------------------------------------
        public void Close()
        {
            Connect( false);
        }
        // ----------------------------------------------------------------------
        public bool ExecuteCommand( string command, IDictionary<string,object> parameters) { throw new NotImplementedException(); }
        // ----------------------------------------------------------------------
        public IEnumerable<string> GetCommands() { return new List<string>(); }
        // ----------------------------------------------------------------------
        public void Abort()
        {
            _log.Info( "Hive plugin received Abort() call");
            Running = false;
            HiveMessenger.Send<AbortCommand>( new AbortCommand());
            Hardware.Abort();
            Reset();
        }
        // ----------------------------------------------------------------------
        public void Pause()
        {
            _log.Info( "Hive plugin received Pause() call");
            // pause motor controllers.
            Hardware.Pause();
            // pause state machines.
            HiveMessenger.Send<PauseCommand>( new PauseCommand());

            // DKM 2011-05-31 I commented out this section because Giles and Reed were okay with Pause happening
            //                AFTER an operation is complete, provided that while we're waiting, the
            //                hourglass window animation pops up.
            /*
            // DKM 2011-05-28 not sure if this is a blended PVT-specific implementation or not.  Certainly
            //                will fail on older systems that don't have func_my_stop
            try {
                _log.Info( "Broadcasting stop command to all axes");
                ts.StopAllAxes();
            } catch( Exception ex) {
                _log.Info( "Could not stop all axes for pause command: " + ex.Message);
            }
             */
        }
        // ----------------------------------------------------------------------
        public void Resume()
        {
            _log.Info( "Hive plugin received Resume() call");
            // resume motor controllers.
            Hardware.Resume();
            // resume state machines.
            HiveMessenger.Send<ResumeCommand>( new ResumeCommand());
        }
        // ----------------------------------------------------------------------
        public void Reset()
        {
            Hardware.Reset();
        }
        // ----------------------------------------------------------------------
        #region IPluginIdentity Members
        // ----------------------------------------------------------------------
        public string Name { get; private set; }
        public string ProductName { get{ return "Hive";}}
        public string Manufacturer { get{ return "BioNex";}}
        public string Description { get{ return "Hive Prototype";}}
        // ----------------------------------------------------------------------
        public void SetProperties( DeviceManagerDatabase.DeviceInfo device_info)
        {
            Name = device_info.InstanceName;
            DeviceProperties = new Dictionary<string,string>( device_info.Properties);
        }
        // ----------------------------------------------------------------------
        #endregion
        // ----------------------------------------------------------------------
        #region IHasDiagnosticPanel Members
        // ----------------------------------------------------------------------
        public UserControl GetDiagnosticsPanel()
        {
            // need to initialize first to allow the cycler to work
            if( !Initialized) {
                try {
                    Connect();
                } catch( Exception ex) {
                    MessageBox.Show( ex.Message);
                    return null;
                }
            }

            DiagnosticsPanel panel = new DiagnosticsPanel( this);
            panel.DataContext = this;
            return panel;
        }
        // ----------------------------------------------------------------------
        public void ShowDiagnostics()
        {
            // need to initialize first to allow the cycler to work
            if( !Initialized) {
                try {
                    Connect();
                } catch( Exception ex) {
                    MessageBox.Show( ex.Message, "Error when attempting to connect to the device");
                }
            }

            if( _diagnostics_window == null) {
                _diagnostics_window = new System.Windows.Window();
                _diagnostics_window.Content = new BioNex.HivePrototypePlugin.DiagnosticsPanel( this);
                _diagnostics_window.Title =  Name + "- Diagnostics" + (Simulating ? " (Simulating)" : "");
                _diagnostics_window.Closed += new EventHandler(_diagnostics_window_Closed);
                _diagnostics_window.Height = 950;
                _diagnostics_window.Width = 950;
            }
            _diagnostics_window.Show();
            _diagnostics_window.Activate();
        }
        // ----------------------------------------------------------------------
        #endregion
        // ----------------------------------------------------------------------
        #endregion
        // ----------------------------------------------------------------------
        #region RobotAccessibleInterface Members
        // ----------------------------------------------------------------------
        public IEnumerable< PlateLocation> PlateLocationInfo
        {
            get
            {
                return PlateLocations.Values;
            }
        }
        // ----------------------------------------------------------------------
        public PlateLocation GetLidLocationInfo( string location_name)
        {
            return null;
        }
        // ----------------------------------------------------------------------
        public string TeachpointFilenamePrefix
        {
            get
            {
                return Name;
            }
        }
        // ----------------------------------------------------------------------
        public int GetBarcodeReaderConfigurationIndex( string location_name)
        {
            return 1;
        }
        // ----------------------------------------------------------------------
        #endregion
        // ----------------------------------------------------------------------
        #endregion
        // ----------------------------------------------------------------------
        #region RobotInterface Members
        // ----------------------------------------------------------------------
        public IDictionary< string, IList< string>> GetTeachpointNames()
        {
            return Hardware.GetTeachpointNames();
        }
        // ----------------------------------------------------------------------
        public double GetTransferWeight( DeviceInterface src_device, PlateLocation src_location, PlatePlace src_place, DeviceInterface dst_device, PlateLocation dst_location, PlatePlace dst_place)
        {
            HiveTeachpoint src_tp = Hardware.GetTeachpoint( src_device.Name, src_place.Name);
            if( src_tp == null){
                return double.PositiveInfinity;
            }
            HiveTeachpoint dst_tp = Hardware.GetTeachpoint( dst_device.Name, dst_place.Name);
            if( dst_tp == null){
                return double.PositiveInfinity;
            }
            return src_tp.Orientation == dst_tp.Orientation ? 1.0 : double.PositiveInfinity;
        }
        // ----------------------------------------------------------------------
        public void Pick(string from_device_name, string from_teachpoint_name, string labware_name, MutableString expected_barcode)
        {
            _log.InfoFormat( "Robot '{0}' moving labware '{1}' with barcode '{2}' from location '{3}' on device '{4}'", Name, labware_name, expected_barcode.Value, from_teachpoint_name, from_device_name);

            // waiting for job complete makes this operation blocking.
            // it is possible to make TransferPlate non-blocking, but for now, it will remain blocking.
            ManualResetEvent job_complete = new ManualResetEvent( false);
            AccessibleDeviceInterface from_device = DataRequestInterface.Value.GetAccessibleDeviceInterfaces().FirstOrDefault( adi => adi.Name == from_device_name);
            HiveTeachpoint from_teachpoint = Hardware.GetTeachpoint( from_device_name, from_teachpoint_name);
            new PickAndOrPlaceJob( ProtocolExecutor, job_complete, PickAndOrPlaceJob.PickAndOrPlaceJobOption.PickOnly, from_device, from_teachpoint, LabwareDatabase.GetLabware( labware_name), new MutableString()).Dispatch();
            job_complete.WaitOne();

            _log.InfoFormat( "Robot '{0}' moved labware '{1}' with barcode '{2}' from location '{3}' on device '{4}'", Name, labware_name, expected_barcode.Value, from_teachpoint_name, from_device_name);

            if( PickComplete != null) {
                PickComplete( this, new PickOrPlaceCompleteEventArgs( expected_barcode.Value, from_device_name, from_teachpoint_name));
            }
        }
        // ----------------------------------------------------------------------
        public void Place( string to_device_name, string to_teachpoint_name, string labware_name, string expected_barcode)
        {
            _log.InfoFormat( "Robot '{0}' moving labware '{1}' with barcode '{2}' to location '{3}' on device '{4}'", Name, labware_name, expected_barcode, to_teachpoint_name, to_device_name);

            // waiting for job complete makes this operation blocking.
            // it is possible to make TransferPlate non-blocking, but for now, it will remain blocking.
            ManualResetEvent job_complete = new ManualResetEvent( false);
            AccessibleDeviceInterface to_device = DataRequestInterface.Value.GetAccessibleDeviceInterfaces().FirstOrDefault( adi => adi.Name == to_device_name);
            HiveTeachpoint to_teachpoint = Hardware.GetTeachpoint( to_device_name, to_teachpoint_name);
            new PickAndOrPlaceJob( ProtocolExecutor, job_complete, PickAndOrPlaceJob.PickAndOrPlaceJobOption.PlaceOnly, to_teachpoint, LabwareDatabase.GetLabware( labware_name)).Dispatch();
            job_complete.WaitOne();

            _log.InfoFormat( "Robot '{0}' moved labware '{1}' with barcode '{2}' to location '{3}' on device '{4}'", Name, labware_name, expected_barcode, to_teachpoint_name, to_device_name);

            if( PlaceComplete != null) {
                PlaceComplete( this, new PickOrPlaceCompleteEventArgs( expected_barcode, to_device_name, to_teachpoint_name));
            }
        }
        // ----------------------------------------------------------------------
        public void TransferPlate( string from_device_name, string from_teachpoint_name, string to_device_name, string to_teachpoint_name, string labware_name, MutableString expected_barcode, bool no_retract_before_pick = false, bool no_retract_after_place = false)
        {
            TransferPlateHelper( from_device_name, from_teachpoint_name, to_device_name, to_teachpoint_name, labware_name, expected_barcode, no_retract_before_pick, no_retract_after_place);
        }
        // ----------------------------------------------------------------------
        public void Delid(string from_device_name, string from_teachpoint, string to_delid_device_name, string to_delid_teachpoint, string labware_name)
        {
            // Delid has to come up with the following information:
            // 1  parent labware - to get the plate's lid ID
            // 2. lid ID - to get the lid labware
            // 3. parent labware - to get the plate's lid offset value
            ILabware parent_plate = LabwareDatabase.GetLabware( labware_name);
            double additional_pick_zoffset = parent_plate.Properties[ LabwarePropertyNames.LidOffset].ToDouble();
            ILabware lid = LabwareDatabase.GetLabware( parent_plate.LidId);
            // I set lidding_operation to false for delidding, since it's the initial plate transfer that needs
            // to skip its arm retract state.
            // Also need to ask host application what iput bit is associated with the location we're delidding to
            TransferPlateHelper( from_device_name, from_teachpoint, to_delid_device_name, to_delid_teachpoint, lid.Name, new MutableString(), no_retract_before_pick: true, no_retract_after_place: false, additional_pick_zoffset: additional_pick_zoffset, additional_place_zoffset: 0, check_for_lid_callback: DataRequestInterface.Value.ReadSensorCallback(to_delid_device_name, to_delid_teachpoint), desired_lid_sensor_state: true);
        }
        // ----------------------------------------------------------------------
        public void Relid(string from_relid_device_name, string from_relid_teachpoint, string to_device_name, string to_teachpoint, string labware_name)
        {
            // Relid has to come up with the following information:
            // 1. parent labware - to get the plate's lid offset value
            ILabware parent_plate = LabwareDatabase.GetLabware( labware_name);
            double additional_place_zoffset = parent_plate.Properties[ LabwarePropertyNames.LidOffset].ToDouble();
            ILabware lid = LabwareDatabase.GetLabware( parent_plate.LidId);
            
            // I set lidding_operation to true for relidding, since the lid gets place before
            // the transfer to storage, relid needs to skip its arm retract state.
            TransferPlateHelper( from_relid_device_name, from_relid_teachpoint, to_device_name, to_teachpoint, lid.Name, new MutableString(), no_retract_before_pick: false, no_retract_after_place: true, additional_pick_zoffset: 0, additional_place_zoffset: additional_place_zoffset, check_for_lid_callback: DataRequestInterface.Value.ReadSensorCallback(from_relid_device_name, from_relid_teachpoint), desired_lid_sensor_state: false);
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Tells the Hive how to reinventory locations.
        /// </summary>
        /// <remarks>
        /// This is expected to always execute in blocking mode.
        /// </remarks>
        /// <param name="target_device">The device with the shelves we want to reinventory</param>
        /// <param name="from_teachpoint">The top teachpoint</param>
        /// <param name="to_teachpoint">The bottom teachpoint</param>
        /// <param name="rack_number">The rack number to scan.  Really only used for messages, and not for the actual barcode scanning process.</param>
        /// <param name="barcodes_expected">How many barcodes should be read from top to bottom</param>
        /// <param name="scan_velocity"></param>
        /// <param name="scan_acceleration"></param>
        /// <param name="reread_condition_masks">Specifies when the barcode reader should rescan this rack</param>
        /// <param name="barcode_misread_threshold"></param>
        /// <returns></returns>
        /// <exception cref="AxisException" />
        public List<string> ReadBarcodes( AccessibleDeviceInterface target_device, string from_teachpoint, string to_teachpoint, int rack_number,
                                          int barcodes_expected, int scan_velocity, int scan_acceleration, List<byte> reread_condition_masks, int barcode_misread_threshold=0)
        {
            //! \todo DKM 2011-05-23 load the configuration database settings
            // 0: one size fits all
            // 1: Hive storage
            // 2: BPS140 storage
            // 3: Dock cart ID
            // 4: Dock storage
            // 5: Kung Fu Bottom Location
            int configuration_index = target_device.GetBarcodeReaderConfigurationIndex( from_teachpoint);
            Debug.Assert( configuration_index >= 0 && configuration_index <= 4, "Invalid configuration index specified.  For now, it must be between 0 and 4.");
            if( BarcodeReader != null) {
                try {
                    BarcodeReader.LoadConfigurationIndex( configuration_index);
                } catch( Exception ex) {
                    _log.InfoFormat( "Could not load barcode reader configuration {0}: {1}", configuration_index, ex.Message);
                    return null;
                }
            }

            // #366 special case for only one shelf per rack -- just move to the teachpoint and read the barcode
            if( barcodes_expected == 1) {
                try {
                    MoveToTeachpoint( target_device.Name, from_teachpoint, false, true);
                    return new List<string> { Hardware.ReadBarcode() };
                } catch( Exception ex) {
                    _log.InfoFormat( "Could not read barcode at device {0}, location {1}: {2}", target_device.Name, from_teachpoint, ex.Message);
                    return new List<string> { Constants.NoRead };
                }
            }

            // DKM 2010-10-11 changed this because BPS140 scanning was getting hung up, but you could
            //                get the reinventorying to work if you lightly pressed up on Z
            const double distance_above_top_shelf_mm = 46;
            ScanningParameters scanning_parameters = new ScanningParameters( rack_number, distance_above_top_shelf_mm, 1, from_teachpoint,
                                                                             to_teachpoint, (short)barcodes_expected, scan_velocity, scan_acceleration,
                                                                             reread_condition_masks, barcode_misread_threshold);
            ManualResetEvent fly_by_bcr_done = new ManualResetEvent( false);
            FlyBySM = new FlyByBarcodeReadingStateMachine( ProtocolExecutor, fly_by_bcr_done, target_device, scanning_parameters);
            ProtocolExecutor.AddStateMachine( FlyBySM);
            fly_by_bcr_done.WaitOne();
            List<string> barcodes = FlyBySM.GetBarcodes();

            // reinventorying could have been interrupted, in which case we want to bail out
            if( barcodes == null)
                return null;

            foreach( string barcode in barcodes)
                _log.InfoFormat( "Read barcode in {0} rack {1}: {2}", target_device.Name, rack_number, barcode);
            return (from x in barcodes select x).ToList();
        }
        // ----------------------------------------------------------------------
        public void Park()
        {
            new ParkJob( ProtocolExecutor).Dispatch();
        }
        // ----------------------------------------------------------------------
        public void MoveToDeviceLocation( AccessibleDeviceInterface accessible_device, string location_name, bool with_plate)
        {
            MoveToTeachpoint( accessible_device.Name, location_name, with_plate, true);
        }
        // ----------------------------------------------------------------------
        // The following is how we do this in FlyByBarcodeStateMachine.cs
        public void MoveToDeviceLocationForBCRStrobe( AccessibleDeviceInterface device_to_move_to, string location_name, bool with_plate)
        {
            HiveTeachpoint teachpoint_copy = new HiveTeachpoint( Hardware.GetTeachpoint( device_to_move_to.Name, location_name));
            double world_x = teachpoint_copy.X;
            double world_z = Math.Min(HiveMath.ConvertZToolToWorldUsingY(Config.ArmLength, Config.FingerOffsetZ, teachpoint_copy.Z, teachpoint_copy.Y) + 10.0, Hardware.ZAxis.Settings.MaxLimit);
            ManualResetEvent tuck_finished_event = new ManualResetEvent( false);
            new TuckToXZJob( DiagnosticsExecutor, world_x, world_z, false, tuck_finished_event).Dispatch();
            tuck_finished_event.WaitOne();
            if( Hardware.BarcodeReader != null){
                Hardware.BarcodeReader.LoadConfigurationIndex( device_to_move_to.GetBarcodeReaderConfigurationIndex( location_name));
            }
        }
        // ----------------------------------------------------------------------
        public string SaveBarcodeImage( string filepath)
        {
            return ( BarcodeReader == null) ? "" : BarcodeReader.SaveImage( filepath, MicroscanCommands.ImageFormat.JPEG, 40);
        }
        // ----------------------------------------------------------------------
        public void SafetyEventTriggeredHandler( object sender, EventArgs e)
        {
            if( FlyBySM != null) {
                FlyBySM.AbortDueToSafetyEvent();
                AbortReinventoryEvent.Set();
            }
        }
        // ----------------------------------------------------------------------
        public void HandleBarcodeMisreads( List<BarcodeReadErrorInfo> misread_barcode_info, List<string> unbarcoded_plate_locations,
                                           Action gui_update_callback, UpdateInventoryLocationDelegate inventory_update_callback)
        {
            if( _dispatcher == null) {
                const string error = "Could not display manual barcode entry dialog";
                _log.Info( error);
                MessageBox.Show( error);
                return;
            }

            _dispatcher.Invoke( new ShowDialogDelegate( ShowBarcodeMisreadsDialog), misread_barcode_info, unbarcoded_plate_locations, gui_update_callback, inventory_update_callback);
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// moves the robot to the specified position, loads the one-size-fits-all barcode
        /// configuration, strobes the BCR, and returns the result
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <param name="bcr_config_index"></param>
        /// <returns></returns>
        public string ReadBarcode( double x, double z, int bcr_config_index = 0)
        {
            return Hardware.ReadBarcode( x, z, bcr_config_index);
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// This is experimental.
        /// </summary>
        /// <remarks>
        /// Now that RobotInterface has a ReloadTeachpoints method, we have to be careful about thread safety.
        /// I'm also trying out a call to ReloadAllDeviceTeachpoints, passing true for the from_gui parameter.
        /// I hope that it will work whether or not diagnostics is open.
        /// </remarks>
        public void ReloadTeachpoints()
        {
            ReloadAllDeviceTeachpoints( false);
        }
        // ----------------------------------------------------------------------
        public bool CanReadBarcode()
        {
            return (BarcodeReader != null && BarcodeReader.Connected) || SimulatingBarcodeReader;
        }
        // ----------------------------------------------------------------------
        public string LastReadBarcode { get { return Hardware.LastReadBarcode; } }
        // ----------------------------------------------------------------------
        public bool BusVoltageOk { get { return Hardware.BusVoltageOk; }}
        // ----------------------------------------------------------------------
        public event PickOrPlaceCompleteEventHandler PickComplete;
        public event PickOrPlaceCompleteEventHandler PlaceComplete;
        // ----------------------------------------------------------------------
        #endregion
        // ----------------------------------------------------------------------
        #region PlateStorageInterface Members
        // ----------------------------------------------------------------------
        // I left these in the HivePlugin because Synapsis hooks into this event, and I didn't want to have to
        // have Synapsis access the ReinventoryStrategy, or wire it so that the events bubble up.  So I
        // just have the IReinventoryStrategy fire the Hive's event directly and pulled it out of the interface.
        public event EventHandler ReinventoryBegin
        {
            add { if(_reinventory_strategy != null) _reinventory_strategy.ReinventoryStrategyBegin += value; }
            remove { if(_reinventory_strategy != null) _reinventory_strategy.ReinventoryStrategyBegin -= value; }
        }
        public event EventHandler ReinventoryComplete;
        public event EventHandler ReinventoryError;
        // ----------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="labware_name"></param>
        /// <param name="barcode"></param>
        /// <param name="location_name">only needed if you're unloading an unbarcoded plate</param>
        public void Unload( string labware_name, string barcode, string location_name)
        {
            if( barcode != "")
                Inventory.Unload( barcode);
            else
                UnbarcodedPlates.Remove( location_name);
        }
        // ----------------------------------------------------------------------
        public void Load( string labware_name, string barcode, string location_name)
        {
            if( barcode == "")
                return;

            HivePlateLocation plate_location = HivePlateLocation.FromString( location_name);
            Inventory.Load( barcode, new Dictionary<string,string> { {"rack", plate_location.RackNumber.ToString()}, {"slot", plate_location.SlotNumber.ToString()} });
        }
        // ----------------------------------------------------------------------
        public bool HasPlateWithBarcode( string barcode, out string location_name)
        {
            try {
                // this call will throw an exception if the barcode doesn't exist in inventory
                location_name = _plate_location_manager.GetTeachpoint( barcode);
                if( location_name == "") { // it was likely unloaded before
                    _log.InfoFormat( "The barcode '{0}' is present in the inventory for device '{1}', but it was already unloaded", barcode, Name);
                    return false;
                }
            } catch( BioNex.Shared.LibraryInterfaces.InventoryBarcodeNotFoundException) {
                location_name = "";
                return false;
            }
            return true;
        }
        // ----------------------------------------------------------------------
        public IEnumerable<string> GetLocationsForLabware( string labware_name)
        {
            // needed this so I can run a protocol with tipbox checking enabled
            if( Simulating) {
                List<string> locations = new List<string>();
                for( int rack=1; rack<=2; rack++)
                    for( int slot=1; slot<=8; slot++)
                        locations.Add( String.Format( "Rack {0}, Slot {1}", rack.ToString(), slot.ToString()));
                return locations;
            }

            return ( labware_name == "tipbox") ? UnbarcodedPlates : null;
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Returns the locations of plates in storage.  The first string is the barcode, and the second string is the location name.
        /// </summary>
        /// <param name="robot_name"></param>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<string,string>> GetInventory( string robot_name)
        {
            Dictionary<string, Dictionary<string,string>> inventory_data = Inventory.GetInventoryData();

            List<KeyValuePair<string,string>> locations = new List<KeyValuePair<string,string>>();
            foreach( KeyValuePair<string, Dictionary<string,string>> kvp in inventory_data) {
                string barcode = kvp.Key;
                try {
                    int rack_number = int.Parse( kvp.Value["rack"].ToString());
                    int slot_number = int.Parse( kvp.Value["slot"].ToString());
                    bool loaded = bool.Parse( kvp.Value["loaded"].ToString());
                    if( loaded)
                        locations.Add( new KeyValuePair<string,string>( barcode, new HivePlateLocation( rack_number, slot_number).ToString()));
                } catch( Exception ex) {
                    // couldn't get the rack and/or slot for whatever reason, so log this and continue
                    _log.Info( "Rack and slot information was not present in inventory data", ex);
                }
            }

            return locations;
        }
        // ----------------------------------------------------------------------
        public bool Reinventory( bool park_robot_after)
        {
            ReinventoryAllRacks( update_gui:false, park_robot_after:park_robot_after);
            return true;
        }
        // ----------------------------------------------------------------------
        public void DisplayInventoryDialog()
        {
            InventoryDialog dlg = new InventoryDialog( this);
            dlg.ShowDialog();
        }
        // ----------------------------------------------------------------------
        public IEnumerable<string> GetStorageLocationNames()
        {
            return _plate_location_manager.GetPlateLocationNames().Select( x => x.Name);
        }
        // ----------------------------------------------------------------------
        #endregion
        // ----------------------------------------------------------------------
        #region INotifyPropertyChanged Members
        // ----------------------------------------------------------------------
        public event PropertyChangedEventHandler PropertyChanged;
        // ----------------------------------------------------------------------
        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for
        /// a given property.
        /// </summary>
        /// <param name="propertyName">The name of the changed property.</param>
        private void OnPropertyChanged(string propertyName)
        {
            // DKM 2012-01-16 removed because this spams the output window
            //System.Console.WriteLine( propertyName);
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        // ----------------------------------------------------------------------
        #endregion
        // ----------------------------------------------------------------------
        #region ProtocolHooksInterface Members
        // ----------------------------------------------------------------------
        public void ProtocolStarting()
        {
            try{
                WakeUpRobot();
            } catch( Exception){
            }
        }
        // ----------------------------------------------------------------------
        public void ProtocolStarted()
        {
        }
        // ----------------------------------------------------------------------
        public void ProtocolComplete()
        {
            try{
                new ParkJob( ProtocolExecutor).Dispatch();
            } catch( Exception){
            }
        }
        // ----------------------------------------------------------------------
        public void ProtocolAborted()
        {
        }
        // ----------------------------------------------------------------------
        #endregion
        // ----------------------------------------------------------------------
        #region PlateSchedulerDeviceInterface Members
        // ----------------------------------------------------------------------
        public event JobCompleteEventHandler JobComplete { add {} remove {} }
        // ----------------------------------------------------------------------
        public PlateLocation GetAvailableLocation( ActivePlate active_plate)
        {
            // DKM 2012-01-18 when unloading, we just have to look up its location and return it.  When loading, we have to figure out where it
            //                should go.  We could go back to the original location, or first available.  If the latter, we have to also make
            //                sure that barcoded plates don't go to tipbox locations!

            if( active_plate.GetCurrentToDo().Command == "Unload"){
                string location_name;
                bool has_plate = HasPlateWithBarcode( active_plate.Barcode, out location_name);
                return has_plate ? PlateLocations[ location_name] : null;
            } else if( active_plate.GetCurrentToDo().Command == "Load"){
                //! DKM 2012-01-18 if we get here, then there isn't a spot already reserved for this plate.  Check if it's a source or dest
                //                 plate, and if so, avoid tipbox locations
                bool is_tipbox = !(active_plate is ActiveSourcePlate) && !(active_plate is ActiveDestinationPlate);
                // DKM 2012-01-18 figure out which racks are used for tipboxes
                var tipbox_rack_numbers = from x in StaticInventoryView where x.CurrentPlateType == RackView.PlateTypeT.Tipbox select x.RackNumber;

                PlateTask.Parameter device_instance_parameter = active_plate.GetCurrentToDo().ParametersAndVariables.FirstOrDefault( param => param.Name == "device_instance");
                // if a device instance is specified and this dock is not the one, then don't return an available location.
                if( device_instance_parameter != null && device_instance_parameter.Value != Name){
                    return null;
                }

                if (is_tipbox)
                {
                    return PlateLocations.Values.Where( plate_location => ( plate_location.Available &&  tipbox_rack_numbers.Contains(HivePlateLocation.FromString(plate_location.Name).RackNumber) )).FirstOrDefault();
                }
                else
                {
                    return PlateLocations.Values.Where( plate_location => ( plate_location.Available && !tipbox_rack_numbers.Contains(HivePlateLocation.FromString(plate_location.Name).RackNumber) )).FirstOrDefault();
                }
                
            } else{
                return null;
            }
        }
        // ----------------------------------------------------------------------
        public bool ReserveLocation( PlateLocation location, ActivePlate active_plate)
        {
            // not my location to reserve.
            if( !PlateLocations.Values.Contains( location)){
                return false;
            }
            // reserve location.
            location.Reserved.Set();
            return true;
        }
        // ----------------------------------------------------------------------
        public void LockPlace( PlatePlace place)
        {
        }
        // ----------------------------------------------------------------------
        public void AddJob( ActivePlate active_plate)
        {
            new Thread( () => JobThread( active_plate)){ Name = GetType().ToString() + " Job Thread", IsBackground = true}.Start();
        }
        // ----------------------------------------------------------------------
        public void JobThread( ActivePlate active_plate)
        {
            active_plate.WaitForPlate();
            if( active_plate.GetCurrentToDo().Command == "Unload"){
                Unload( active_plate.LabwareName, active_plate.Plate.Barcode, active_plate.DestinationLocation.Name);
            } else if( active_plate.GetCurrentToDo().Command == "Load"){
                Load( active_plate.LabwareName, active_plate.Plate.Barcode, active_plate.DestinationLocation.Name);
            }
            active_plate.MarkJobCompleted();
        }
        // ----------------------------------------------------------------------
        public void EnqueueWorklist( Worklist worklist)
        {
            throw new NotImplementedException();
        }
        // ----------------------------------------------------------------------
        #endregion
        // ----------------------------------------------------------------------
        void _diagnostics_window_Closed( object sender, EventArgs e)
        {
            _diagnostics_window.Content = null;
            _diagnostics_window = null;
        }
        // ----------------------------------------------------------------------
        private void LoadXmlConfiguration()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(BioNex.Hive.Hardware.Configuration));
            string config_path = DeviceProperties[ConfigFolder] + "\\config.xml";
            FileStream reader = new FileStream(config_path.ToAbsoluteAppPath(), FileMode.Open);
            Config = (BioNex.Hive.Hardware.Configuration)serializer.Deserialize(reader);
            reader.Close();
        }
        // ----------------------------------------------------------------------
        private void SaveXmlConfiguration()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(BioNex.Hive.Hardware.Configuration));
            // need to erase previous file, because just writing to the same file will leave
            // existing data intact if it doesn't get overwritten
            string filename = DeviceProperties[ConfigFolder] +  "\\config.xml";
            string backup = filename + ".backup";
            if( File.Exists( filename)) {
                try {
                    File.Copy( filename, backup, true);
                } catch( Exception ex) {
                    // if we have any sort of error in the backup, bail so we don't delete
                    // the existing config file.  It's better to not save the config than
                    // to crash.
                    string message = String.Format( "Could not backup existing config file for {0}: {1}", Name, ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                    _log.Info( message);
                    MessageBox.Show( message);
                    return;
                }
                File.Delete( filename);
            }
            try {
                using( FileStream writer = new FileStream( filename, FileMode.Create) ) {
                    serializer.Serialize( writer, Config);
                    writer.Close();
                }
            } catch( InvalidOperationException ex) {
                // this is to catch the case where an out-of-date config.xml is used.  For Igenica and Pioneer 2,
                // the file had an element name called PlateTypeMask for the default plate type, but I changed
                // this text to DefaultPlateType when I had to make improvements for Monsanto 1-4.
                if( ex.InnerException.Message.Contains( "Instance validation error")) {
                    File.Copy( backup, filename, true);
                    MessageBox.Show( String.Format( "The file '{0}' has an incorrect element name.  Please change all occurrences of 'PlateTypeMask' to 'DefaultPlateType' and reload Synapsis.", filename));
                }
            } catch( Exception ex) {
                _log.InfoFormat( "Could not save {0} configuration: {1}.  Restoring previous config.xml.", Name, ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                
            }
        }
        // ----------------------------------------------------------------------
        internal void ShowAllSpeeds( bool show)
        {
            AllowableSpeeds.Clear();
            AllowableSpeeds.Add( 20);
            if( show) {
                AllowableSpeeds.Add( 50);
                AllowableSpeeds.Add( 100);
                MaxAllowableSpeed = 100;
                Speed = 100;
            } else {
                MaxAllowableSpeed = 20;
                Speed = 20;
            }
            OnPropertyChanged( "AllowableSpeeds");
        }
        // ----------------------------------------------------------------------
        public void TransferPlateHelper( string from_device_name, string from_teachpoint_name, string to_device_name, string to_teachpoint_name, string labware_name, MutableString expected_barcode, bool no_retract_before_pick, bool no_retract_after_place, double additional_pick_zoffset = 0, double additional_place_zoffset = 0, Func<bool> check_for_lid_callback = null, bool desired_lid_sensor_state = false)
        {
            _log.InfoFormat( "Robot '{0}' transferring labware '{1}' from location '{2}' on device '{3}' to location '{4}' on device '{5}'.", Name, labware_name, from_teachpoint_name, from_device_name, to_teachpoint_name, to_device_name);

            // waiting for job complete makes this operation blocking.
            // it is possible to make TransferPlate non-blocking, but for now, it will remain blocking.
            ManualResetEvent job_complete = new ManualResetEvent( false);
            AccessibleDeviceInterface from_device = DataRequestInterface.Value.GetAccessibleDeviceInterfaces().FirstOrDefault( adi => adi.Name == from_device_name);
            AccessibleDeviceInterface to_device = DataRequestInterface.Value.GetAccessibleDeviceInterfaces().FirstOrDefault( adi => adi.Name == to_device_name);
            HiveTeachpoint from_teachpoint = Hardware.GetTeachpoint( from_device_name, from_teachpoint_name);
            HiveTeachpoint to_teachpoint = Hardware.GetTeachpoint( to_device_name, to_teachpoint_name);
            new PickAndOrPlaceJob( ProtocolExecutor, job_complete, PickAndOrPlaceJob.PickAndOrPlaceJobOption.PickAndPlace, LabwareDatabase.GetLabware( labware_name), expected_barcode, from_device, from_teachpoint, to_teachpoint).Dispatch();
            job_complete.WaitOne();

            _log.InfoFormat( "Robot '{0}' transferred labware '{1}' from location '{2}' on device '{3}' to location '{4}' on device '{5}'.", Name, labware_name, from_teachpoint_name, from_device_name, to_teachpoint_name, to_device_name);
        }
        // ----------------------------------------------------------------------
        private void WakeUpRobot()
        {
            new EnableAllAxesJob( ProtocolExecutor).Dispatch();
        }
        // ----------------------------------------------------------------------
        private void StopAllMotors()
        {
            try {
                Hardware.StopRobot();
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// This is required if we want the accessible device to appear correctly (by device instance name) in the robot teaching tab
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }
        // ----------------------------------------------------------------------
        public void UpdateGUI()
        {
            OnPropertyChanged( "CurrentWorldPosition");
            OnPropertyChanged( "CurrentToolPosition");
            OnPropertyChanged( "CommandedAxisPositions");
        }
        // ----------------------------------------------------------------------
        /* not used.
        public delegate void UpdateInventoryViewDelegate();
        public void AddError( ErrorData error, bool called_from_diags)
        {
            if( called_from_diags){
                _dispatcher.BeginInvoke( new AddErrorDelegate( AddErrorSTA), error);
            } else{
                ErrorInterface.AddError( error);
            }
        }
        public void ReloadTeachpointFile()
        {
            string teachpoint_path = ( DeviceProperties[ConfigFolder] + "\\hive_teachpoints.xml").ToAbsoluteAppPath();
            _tp.LoadTeachpointFile( teachpoint_path, null);
            //! \todo find a better way to deal with this -- perhaps the right thing to do
            //!       is to have a different ReloadTeachpointFile() method that's GUI
            //!       only.  The dispatcher will be null in cases where we want to be able
            //!       to reload a teachpoint file from the pick and place state machine.
            if( _dispatcher != null) {
                LoadTeachpointNamesForDevice( Name, TeachpointNames);
            }
        }
        private void ApplyOffsetToSelectedTeachpoints()
        {
            // look at ReteachPreview to see which ones were selected
            foreach( ReteachPreviewInfo rpi in ReteachPreview) {
                if( !rpi.IsSelected)
                    continue;
                Teachpoint tp = GetTeachpoint( SelectedDevice.Name, rpi.Name);
                AddTeachpoint( SelectedDevice.Name, new Teachpoint( tp.Name, tp["x"] - ReteachOffset.X, tp["y"] - ReteachOffset.Y, tp["z"] - ReteachOffset.Z, tp["approach_height"]));
            }
            SaveTeachpointFile( AccessibleDeviceView.CurrentItem as AccessibleDeviceInterface);
        }
        public void SelectCANAdapter()
        {
            // this whole comm select dialog stuff seems a little weird.  If you don't close it
            // before exiting the app, then the app hangs waiting for the dialog.  But if you close
            // it when the app starts, you can't redisplay it.  So the only "hack" approach is to
            // Close() the dialog on initialization, which allows us to read the default values
            // for the connection parameters, but then we have to re-instantiate the control when
            // requesting the dialog.
            // this is another workaround -- since we have to create a new dialog each time, we
            // really have to get the old values for device and port...
            string old_port = _comm_selection_dialog.CurrentPort;
            string old_device = _comm_selection_dialog.CurrentDevice;
            _comm_selection_dialog = new CommSelectionDialog();
            _comm_selection_dialog.CurrentPort = old_port;
            _comm_selection_dialog.CurrentDevice = old_device;
            _comm_selection_dialog.ShowDialog();
            _comm_selection_dialog.Close();
        }
        */
    }
    // --------------------------------------------------------------------------
    public class OrientationFormatter : IValueConverter
    {
        #region IValueConverter Members
        // ----------------------------------------------------------------------
        object IValueConverter.Convert( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ( HiveTeachpoint.TeachpointOrientation )value == HiveTeachpoint.TeachpointOrientation.Landscape ? 0 : 1;
        }
        // ----------------------------------------------------------------------
        object IValueConverter.ConvertBack( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.ToInt() == 0 ? HiveTeachpoint.TeachpointOrientation.Landscape : HiveTeachpoint.TeachpointOrientation.Portrait;
        }
        // ----------------------------------------------------------------------
        #endregion
    }
}
