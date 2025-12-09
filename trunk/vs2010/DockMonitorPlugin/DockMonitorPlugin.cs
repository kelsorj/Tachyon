using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using BioNex.Exceptions;
using BioNex.Shared.BioNexGuiControls;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Location;
using BioNex.Shared.PlateWork;
using BioNex.Shared.TaskListXMLParser;
using BioNex.Shared.Utils;
using FileHelpers;
using log4net;

namespace BioNex.Plugins.Dock
{
    [PartCreationPolicy(System.ComponentModel.Composition.CreationPolicy.NonShared)]
    [Export(typeof(DeviceInterface))]
    public class DockMonitorPlugin : DockablePlateStorageInterface, AccessibleDeviceInterface, PlateSchedulerDeviceInterface
    {
        private DeviceManagerProperties _device_properties { get; set; }
        private static readonly ILog _log = LogManager.GetLogger(typeof(DockMonitorPlugin));
        private IDockController _controller { get; set; }
        /// <summary>
        /// The external communications will usually be the customer GUI plugin
        /// </summary>
        internal DockExternalCommunications _external_communications { get; set; }

        private System.Windows.Window _diagnostics_panel { get; set; }

        public string CartBarcode
        {
            get { return _controller.CartBarcode; }
        }
        public string CartHumanReadable
        {
            get { return _controller.CartHumanReadable; }
        }

        [Import]
        public Lazy<ExternalDataRequesterInterface> DataRequestInterface { get; set; }
        [Import]
        public CartDefinitionFile CartLookup { get; set; }
        [Import("MainDispatcher")]
        internal Dispatcher _dispatcher { get; set; }

        private List<DockMonitorPlateLocation> _unbarcoded_plates { get; set; }

        public DockGUI GUI { get; private set; }

        public bool ReadyToUndock { get; private set; }

        // used to keep track of the rear door so we know when the robot can move
        private bool _safety_triggered;
        private bool _safety_overridden;

        public DockMonitorPlugin()
        {
            _unbarcoded_plates = new List<DockMonitorPlateLocation>();
            ReinventoryComplete += new EventHandler( HandleReinventoryComplete);
            // DKM 2011-11-02 I think these two handlers should be deleted
            PlateUnloaded += new PlateLoadUnloadEventHandler( HandleUnloadComplete);
            PlateLoaded += new PlateLoadUnloadEventHandler( HandleLoadComplete);
        }

        /// <summary>
        /// Tells caller how many racks and slots are available in the currently-docked cart.
        /// Could return 0,0 if the cart definition doesn't exist in cart_definitions.xml.
        /// </summary>
        /// <param name="num_racks"></param>
        /// <param name="num_slots"></param>
        public void GetCurrentCartConfiguration( out int num_racks, out int num_slots)
        {
            try {
                if (_controller.CartBarcode == "")
                {
                    num_racks = 0;
                    num_slots = 0;
                    return;
                }
                // even if the cart isn't "docked", return the number of racks and slots expected
                CartLookup.GetNumberOfRacksAndSlotsFromBarcode( _controller.CartBarcode, out num_racks, out num_slots);
            } catch( Exception ex) {
                _log.Info( ex.Message);
                num_racks = 0;
                num_slots = 0;
            }
        }

        #region dock-specific functions
        public void SetExternalCommunications( DockExternalCommunications source)
        {
            _external_communications = source;
        }
        #endregion

        #region DeviceInterface Members

        public string Manufacturer
        {
            get
            {
                return "BioNex";
            }
        }

        public string ProductName
        {
            get
            {
                return "Dock";
            }
        }

        public string Name {get; private set;}

        /// <summary>
        /// This is required if we want the accessible device to appear correctly (by device instance name) in the robot teaching tab
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }

        public string Description
        {
            get
            {
                return "Dock plugin that utilizes the Synapsis GUI for displaying information";
            }
        }

        public System.Windows.Controls.UserControl GetDiagnosticsPanel()
        {
            if( GUI == null)
                GUI = new DockGUI( this);

            return GUI;
        }

        public void SetProperties(DeviceManagerDatabase.DeviceInfo device_info)
        {
            Name = device_info.InstanceName;
            _device_properties = new DeviceManagerProperties( device_info.Properties);
        }

        public void ShowDiagnostics()
        {
            if( _diagnostics_panel == null) {
                _diagnostics_panel = new System.Windows.Window();
                _diagnostics_panel.Content = new DockGUI( this);
                _diagnostics_panel.Closed += new EventHandler(_diagnostics_panel_Closed);
                _diagnostics_panel.Title =  Name + "- Diagnostics";// + (Controller.Simulating ? " (Simulating)" : "");
            }

            _diagnostics_panel.Show();
            _diagnostics_panel.Activate();
        }

        void _diagnostics_panel_Closed(object sender, EventArgs e)
        {
            _diagnostics_panel = null;
        }

        public void Connect()
        {
            // DKM 2011-07-19 simulation is ONLY for reinventory now -- device simulation is
            // dependent upon IODevice now.
            //if( !_device_properties.Simulating) {
                IOInterface io_interface = DataRequestInterface.Value.GetIOInterfaces().FirstOrDefault( x => (x as DeviceInterface).Name == _device_properties.IODevice);
                RobotInterface robot_interface = DataRequestInterface.Value.GetRobotInterfaces().FirstOrDefault( x => (x as DeviceInterface).Name == _device_properties.RobotDevice);
                IOInterface system_io_interface = DataRequestInterface.Value.GetIOInterfaces().FirstOrDefault(x => (x as DeviceInterface).Name == _device_properties.SystemIODevice);
                _controller = new Controller( Name, _device_properties, io_interface, system_io_interface, robot_interface, CartLookup);
            //} else {
            //    _controller = new SimController();
            //}

            // add handlers for the safety interface(s)
            foreach( var safety in DataRequestInterface.Value.GetSafetyInterfaces()) {
                safety.SafetyEventReset += new EventHandler(safety_SafetyEventReset);
                safety.SafetyEventTriggered += new EventHandler(safety_SafetyEventTriggered);
                safety.SafetyOverrideEvent += new EventHandler<SafetyEventArgs>(safety_SafetyOverrideEvent);
            }
        }

        void safety_SafetyOverrideEvent(object sender, SafetyEventArgs e)
        {
            _safety_overridden = e.Overridden;
        }

        void safety_SafetyEventTriggered(object sender, EventArgs e)
        {
            _safety_triggered = true;
        }

        public void  safety_SafetyEventReset(object sender, EventArgs e)
        {
 	        _safety_triggered = false;
        }
        
        public bool Connected
        {
            get { return _controller.Connected; }
        }

        public void Home()
        {
            return;
        }

        public bool IsHomed
        {
            get
            {
                return true;
            }
        }

        public void Close()
        {
            //if( !_device_properties.Simulating)
                _controller.Close();
        }

        public bool ExecuteCommand(string command, IDictionary<string, object> parameters)
        {
            return false;
        }

        public IEnumerable<string> GetCommands()
        {
            return new List<string> { "Lock", "Unlock" };
        }

        public void Abort()
        {
            _controller.AbortDockOrUndock();
        }

        public void Pause() {}
        public void Resume() {}
        public void Reset() {}

        #endregion

        IDictionary< string, PlateLocation> PlateLocations = new Dictionary< string, PlateLocation>();

        /// <summary>
        /// map of barcode to location name
        /// </summary>
        readonly Dictionary<string,string> _barcoded_plate_locations = new Dictionary<string,string>();

        #region PlateStorageInterface Members

        public event EventHandler ReinventoryBegin { add {} remove {} }
        public event EventHandler ReinventoryComplete;
        public event EventHandler ReinventoryError;

        void HandleReinventoryComplete( object sender, EventArgs e)
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
        }

        void HandleUnloadComplete( object sender, PlateLoadUnloadEventArgs e)
        {
        }

        void HandleLoadComplete( object sender, PlateLoadUnloadEventArgs e)
        {
        }

        public class PlateLoadUnloadEventArgs : EventArgs
        {
            public int RackIndex { get; private set; }
            public int SlotIndex { get; private set; }
            public string Barcode { get; private set; }

            public PlateLoadUnloadEventArgs( string location_name, string barcode)
            {
                DockMonitorPlateLocation location = DockMonitorPlateLocation.FromString( location_name);
                RackIndex = location.RackNumber - 1;
                SlotIndex = location.SlotNumber - 1;
                Barcode = barcode;
            }
        }

        public delegate void PlateLoadUnloadEventHandler( object sender, PlateLoadUnloadEventArgs e);

        public event PlateLoadUnloadEventHandler PlateLoaded;
        public event PlateLoadUnloadEventHandler PlateUnloaded;

        /// <summary>
        /// This removes the plate from the in-memory inventory
        /// </summary>
        /// <param name="labware_name"></param>
        /// <param name="barcode"></param>
        /// <param name="to_teachpoint"></param>
        public void Unload(string labware_name, string barcode, string to_teachpoint)
        {
            // remove plate from memory map
            if( _barcoded_plate_locations.ContainsValue( to_teachpoint)) {
                _barcoded_plate_locations.Remove( _barcoded_plate_locations.First( x => x.Value == to_teachpoint).Key);
            }
            // update the GUI
            if( PlateUnloaded != null)
                PlateUnloaded( this, new PlateLoadUnloadEventArgs( to_teachpoint, barcode));
        }

        /// <summary>
        /// This puts the plate into the in-memory inventory
        /// </summary>
        /// <param name="labware_name"></param>
        /// <param name="barcode"></param>
        /// <param name="from_teachpoint"></param>
        public void Load(string labware_name, string barcode, string from_teachpoint)
        {
            // add plate to memory map
            // for now allow overwriting of barcode information
            if( _barcoded_plate_locations.ContainsValue( from_teachpoint))
            {
                // wipe out any existing entries at this teachpoint
                var existing = (from x in _barcoded_plate_locations where x.Value == from_teachpoint select x.Key).ToList();
                foreach( var plate in existing)
                    _barcoded_plate_locations.Remove(plate);
            }
            _barcoded_plate_locations[barcode] = from_teachpoint;
            // update the GUI
            if( PlateLoaded != null)
                PlateLoaded( this, new PlateLoadUnloadEventArgs( from_teachpoint, barcode));
        }

        public bool HasPlateWithBarcode(string barcode, out string location_name)
        {
            location_name = "";
            if( !_barcoded_plate_locations.ContainsKey( barcode))
                return false;

            location_name = _barcoded_plate_locations[barcode];
            return true;
        }

        public IEnumerable<string> GetLocationsForLabware(string labware_name)
        {
            return new List<string>();
        }

        public IEnumerable<string> GetStorageLocationNames()
        {
            int num_racks, num_slots;
            GetCurrentCartConfiguration( out num_racks, out num_slots);
            for( int rack=0; rack<num_racks; rack++) {
                for( int slot=0; slot<num_slots; slot++) {
                    yield return (new DockMonitorPlateLocation(rack + 1, slot + 1)).ToString();
                }
            }
        }

        /// <summary>
        /// Returns the plate inventory from this dock
        /// </summary>
        /// <param name="robot_name"></param>
        /// <returns>a bunch of key value pairs, where the key is the barcode and the value is the location name</returns>
        public IEnumerable<KeyValuePair<string, string>> GetInventory(string robot_name)
        {
            return from x in _barcoded_plate_locations select new KeyValuePair<string,string>( x.Key, x.Value.ToString());
        }

        private delegate List<Shared.DeviceInterfaces.BarcodeReadErrorInfo> ReinventoryDelegate();

        /// <summary>
        /// Whether or not the GUI should allow the user to reinventory.  To prevent over-complication and requiring
        /// communication with the Hive plugin (which owns the teachpoints), this method only ensures that the
        /// dock has a cart definition available.
        /// </summary>
        /// <returns>true if all teachpoints are present, false if not</returns>
        internal bool CartDefinitionAvailable()
        {
            if( string.IsNullOrEmpty( _controller.CartBarcode))
                return false;

            try
            {
                int num_racks;
                int num_slots;
                CartLookup.GetNumberOfRacksAndSlotsFromBarcode( _controller.CartBarcode, out num_racks, out num_slots);
            }
            catch( Exception) {
                return false;
            }
            return true;
        }

        public bool VerifyTeachpointsForReinventory(RobotInterface chosen_robot, out string failure_reason)
        {
            failure_reason = "";
            // look up the number of racks and slots
            // DKM 2011-05-10 at first, I thought we should cache this information when the cart is docked, but
            // we can't do it that way because a cart isn't "docked" until it's been reinventoried.
            int num_racks, num_slots;
            GetCurrentCartConfiguration( out num_racks, out num_slots);

            // get list of teachpoints from Robot
            var robot_name = ((DeviceInterface)chosen_robot).Name;
            var all_tps = chosen_robot.GetTeachpointNames();
            if( !all_tps.ContainsKey(Name))
            {
                failure_reason = string.Format("'{0}' does not have teachpoints for '{1}'", robot_name, Name); 
                return false;
            }
            var tps = all_tps[Name];

            // loop through teachpoints -- two at a time -- calling read barcode. -- verify these teachpoints all exists --
            List<BarcodeReadErrorInfo> missed_barcodes = new List<BarcodeReadErrorInfo>();
            for (int rack_index = 0; rack_index < num_racks; rack_index++) {
                var top_location = new DockMonitorPlateLocation(rack_index + 1, 1).ToString();
                if( !tps.Contains(top_location))
                {
                    failure_reason = string.Format("'{0}' has no teachpoint '{1}' for device '{2}' check the cart definition file", robot_name, top_location, Name);
                    return false;
                }
                var bottom_location = new DockMonitorPlateLocation(rack_index + 1, num_slots).ToString();
                if( !tps.Contains(bottom_location))
                {
                    failure_reason = string.Format("'{0}' has no teachpoint '{1}' for device '{2}' check the cart definition file", robot_name, bottom_location, Name);
                    return false;
                }
            }
            return true;
        }


        /// <summary>
        /// Spawned by a thread to perform the reinventory procedure
        /// </summary>
        /// <returns></returns>
        public bool Reinventory( bool park_robot_after)
        {
            // tell the main UI that we've started a reinventory so that it can set state
            if (!_external_communications.Reinventory(Name))
                return false;

            _barcoded_plate_locations.Clear();
            _unbarcoded_plates.Clear();

            // look up the number of racks and slots
            int num_racks, num_slots;
            CartLookup.GetNumberOfRacksAndSlotsFromBarcode(_controller.CartBarcode, out num_racks, out num_slots);
            RobotInterface chosen_robot = _external_communications.GetRobotForDock(Name);

            // if we can't find a robot to support reinventory, then fail.
            if (chosen_robot == null) {
                throw new Exception("Could not find a robot with all of the necessary teachpoints.  Please teach the top and bottom slots of each rack in the cart.");
            }
            // loop through teachpoints -- two at a time -- calling read barcode.
            List<BarcodeReadErrorInfo> missed_barcodes = new List<BarcodeReadErrorInfo>();

            // if we're simulating the dock, then open the text file that provides barcodes
            InventorySimulationData[] inventory = null;
            if( _device_properties.Simulating) {
                FileHelperEngine engine = new FileHelperEngine(typeof(InventorySimulationData));
                engine.Options.IgnoreFirstLines = 1;

                // hardcoding for now because some XP systems crash when opening openfiledialog
                string simulation_filepath = _device_properties.ConfigFolder + "\\inventory_simulation_data.csv";
                inventory = engine.ReadFile( simulation_filepath) as InventorySimulationData[];
            }

            for (int rack_index = 0; rack_index < num_racks; rack_index++) {
                List<string> barcodes = new List<string>();
                // if we're simulating, then just use the data loaded from the text file
                if( _device_properties.Simulating) {
                    // get the barcodes from the file that correspond to this rack number
                    int rack_number = rack_index + 1;
                    barcodes = (from x in inventory where x.RackNumber == rack_number select x.Barcode).ToList();
                } else { // otherwise, move the robot to read barcodes
                    // need top and bottom teachpoints
                    DockMonitorPlateLocation top_location = new DockMonitorPlateLocation(rack_index + 1, 1);
                    DockMonitorPlateLocation bottom_location = new DockMonitorPlateLocation(rack_index + 1, num_slots);
                    // hive and bps140 have a plate type configuration value, but in the dock's case, we are
                    // assuming that only barcoded plates are expected.
                    const byte mask = ScanningParameters.RereadMissedStrobe | ScanningParameters.RereadNoRead;
                    List<byte> reread_condition_masks = Enumerable.Repeat<byte>( mask, num_slots).ToList();
                    barcodes = chosen_robot.ReadBarcodes(this, top_location.ToString(), bottom_location.ToString(),
                                                                      rack_index + 1, num_slots, 250, 3000, reread_condition_masks, 5);

                }
                // if barcodes is null, the reinventorying process was aborted
                if (barcodes == null) {
                    // need to fire error so something like the BehaviorEngine will be able to change state accordingly
                    if( ReinventoryError != null)
                        ReinventoryError( this, new EventArgs());
                    return false;
                }

                //! FOR NOW WE ARE ASSUMING NO TIPBOXES ALLOWED IN CARTS ... WE WILL NEED TO REVISIT THIS
                // DKM 2011-05-10 I also made this assumption in the inventory GUI -- not going to allow switching of rack types
                for (int slot_index = 0; slot_index < barcodes.Count; ++slot_index) {
                    var barcode = barcodes[slot_index];
                    DockMonitorPlateLocation plate_location = new DockMonitorPlateLocation( rack_index + 1, slot_index + 1);
                    if (!Constants.IsNoRead(barcode) && !Constants.IsEmpty(barcode)) {
                        _unbarcoded_plates.Remove( plate_location);
                        Load("labware not needed in this context", barcode, new CartPlateLocation(rack_index + 1, slot_index + 1).ToString());
                    } else if( Constants.IsNoRead( barcode)) {
                        missed_barcodes.Add( new BarcodeReadErrorInfo( new DockMonitorPlateLocation( rack_index + 1, slot_index + 1).ToString(), barcode));
                        // this handles the tipbox / unbarcoded plate case
                        _unbarcoded_plates.Add( plate_location);
                        // although this seems weird, for docks, we do still need to load a plate because the GUI handler will
                        // mark it as Unknown.  The nice effect is that during reinventory, the GUI updates with NOREAD on a
                        // purple background so the user sees what's going on / wrong with reinventorying.
                        Load("labware not needed in this context", barcode, new CartPlateLocation(rack_index + 1, slot_index + 1).ToString());
                    }
                }
            }

            // after getting the basic misread information, we now need to move to each location and
            // take pictures of what's there
            foreach (BarcodeReadErrorInfo info in missed_barcodes) {
                // move to the location
                try {
                    chosen_robot.MoveToDeviceLocationForBCRStrobe(this, info.TeachpointName, false);
                    chosen_robot.SaveBarcodeImage(info.ImagePath);
                } catch (Exception) {
                    // if we couldn't move there or capture an image, just continue on and leave
                    // it up to the user to look at the plate
                }
            }

            // if we can't park, it's okay, just log it
            try {
                chosen_robot.Park();
            } catch (Exception ex) {
                _log.InfoFormat( "Could not park robot {0}: {1}", (chosen_robot as DeviceInterface).Name, ex.Message);
            }

            // take the misread information and present the user with a GUI to resolve the issues
            //! \todo we can probably just cast ?
            if (missed_barcodes.Count() > 0) {
                //! \todo move HandleBarcodeMisreads into a different assembly, rather than putting it
                //        into the RobotInterface.  This is because the displaying of the barcode
                //        misread dialog really doesn't have anything to do with a specific robot.
                //        Probably should go into the BarcodeMisread assembly, but then we need
                //        to create more assemblies so PlateLocation can be pulled out.  Perhaps we can
                //        have a RackSlotPlateLocation and SideRackSlotPlateLocation defined in another
                //        assembly.
                _external_communications.GetRobotForDock(Name).HandleBarcodeMisreads( missed_barcodes, (from x in _unbarcoded_plates select x.ToString()).ToList(),
                                                                                      () => {}, new UpdateInventoryLocationDelegate(UpdateInventoryLocation));
            }
            
            _controller.CartIsReinventoried = true;
            if( ReinventoryComplete != null)
                ReinventoryComplete( this, new EventArgs());
            
            return true;
        }

        private void UpdateInventoryLocation( string location_name, string updated_barcode)
        {
            if( Constants.IsEmpty( updated_barcode))
                Unload( "labware not needed in this context", Constants.Empty, location_name);
            else 
                Load("labware not needed in this context", updated_barcode, location_name);
        }

        public void DisplayInventoryDialog()
        {
            
        }

        #endregion

        #region DockableStorageInterface

        public string SubStorageName
        {
            get { 
                if( _controller != null)
                    return _controller.CartBarcode;
                else
                    return null;
            }
            set {}
        }

        public bool SubStoragePresent { get { return _controller.CartPresent; } }
        public bool SubStorageDocked { get { return SubStoragePresent && _controller.CartIsDocked; } }

        /// <summary>
        /// Although it would be nice to have a state machine for this, I don't think it's actually that necessary.
        /// The user can just click the button again.  But things like MovePlate and upstack/downstack definitely
        /// need to have error handling.
        /// </summary>
        public void Dock()
        {
            HourglassWindow hg = null;

            try {
                // activate the dock magnets ahead of time, in case someone wants to dock multiple carts simultaneously.
                // otherwise, the first dock action will block the second, and the second dock's magnets won't energize
                _controller.EnableDockMagnets();
                // now block on waiting for behavior engine to let us dock
                _external_communications.PrepareForDock(Name);

                // only prompt to open and dock if the cart isn't already present
                // wait for cart to dock
                const double timeout_sec = 150;
                if (!_controller.CartPresent)
                {
                    // DKM 2012-01-24 had to add door sensor check for LA2012
                    if( _controller.HasDoorSensor)
                        PromptUserToOpenDoor(timeout_sec);
                    PromptUserToDockCart(30);
                }
                // close door
                // DKM 2012-01-24 had to add door sensor check for LA2012
                if( _controller.HasDoorSensor)
                    PromptUserToCloseDoor( timeout_sec);
                // also need to wait until the user clicks Reset Interlocks if not overridden
                // DKM 2012-01-24 had to add door sensor check for LA2012
                if( _controller.HasDoorSensor)
                    PromptUserToResetInterlocks( timeout_sec);
                // need to allow time to enable
                Window owner = null;
                _dispatcher.Invoke( new Action( () => { owner = Window.GetWindow( GUI); } ));
                hg = HourglassWindow.ShowHourglassWindow( _dispatcher, owner, "Scanning cart", true);
                WaitForBusVoltageOk();
                // wait until the gui plugin is ready6
                _external_communications.ReadyToDock(Name);
                // lock the cart with magnets, scan ID with robot, load teachpoint file
                try {
                    _controller.DockCart();
                } catch( BarcodeException ex) {
                    PromptUserToEnterCartBarcode(ex.Message);
                }
                // reconstitute 
                PlateLocations = ( from rack_number in Enumerable.Range( 1, _controller.NumberOfRacks)
                                   from slot_number in Enumerable.Range( 1, _controller.NumberOfSlots)
                                   select String.Format( "Rack {0}, Slot {1}", rack_number, slot_number)).ToDictionary( location_name => location_name, location_name => new PlateLocation( location_name));
                // clear out in-memory inventory
                _barcoded_plate_locations.Clear();
                // notify whomever is interested, i.e. dock plugin GUI
                if( Docked != null)
                    Docked( this, new DockEventArgs( Name, CartBarcode, CartHumanReadable));
            } catch( Exception ex) {
                _log.InfoFormat( "Docking error at '{0}': {1}", Name, ex.Message);
                if( DockingError != null)
                    DockingError( this, new DockEventArgs( Name, "", ""));
                throw;
            } finally {
                HourglassWindow.CloseHourglassWindow( _dispatcher, hg);
            }
        }

        private void PromptUserToEnterCartBarcode( string message_if_cancelled)
        {
            _dispatcher.Invoke(new Action(() =>
            {
                try
                {
                    // if we couldn't read the cart, here is where we'll prompt the user to enter
                    // a barcode manually, and then set the barcode in the controller.
                    string manual_barcode;
                    UserInputDialog dlg = new UserInputDialog("Could not read cart barcode", "Please enter the cart barcode below:");
                    MessageBoxResult response = dlg.PromptUser(out manual_barcode);
                    if (response == MessageBoxResult.Cancel)
                        throw new BarcodeException( message_if_cancelled);
                    _controller.UserEnteredCartBarcode(dlg.UserText);
                }
                catch (Exception e)
                {
                    string message = String.Format("Docking error at '{0}': {1}", Name, e.Message);
                    _log.Info(message);
                    MessageBox.Show(message);
                    if (DockingError != null)
                        DockingError(this, new DockEventArgs(Name, "", ""));
                    _barcoded_plate_locations.Clear();
                    return;
                }
            }));
        }

        private void PromptUserToDockCart(double timeout_sec)
        {
            DateTime start = DateTime.Now;
            Window owner = null;
            _dispatcher.Invoke( new Action( () => { owner = Window.GetWindow( GUI); } ));
            HourglassWindow hg = HourglassWindow.ShowHourglassWindow( _dispatcher, owner, "Dock cart", false);
            while (!_controller.CartPresent && (DateTime.Now - start).TotalSeconds < timeout_sec)
                Thread.Sleep(100);
            if ((DateTime.Now - start).TotalSeconds >= timeout_sec) {
                HourglassWindow.CloseHourglassWindow( _dispatcher, hg);
                throw new Exception("Timed out waiting for user to dock a cart");
            }
            HourglassWindow.CloseHourglassWindow( _dispatcher, hg);
        }

        private void PromptUserToUndockCart(double timeout_sec)
        {
            DateTime start = DateTime.Now;
            Window owner = null;
            _dispatcher.Invoke( new Action( () => { owner = Window.GetWindow( GUI); } ));
            HourglassWindow hg = HourglassWindow.ShowHourglassWindow( _dispatcher, owner, "Remove cart", false);
            while (_controller.CartPresent && (DateTime.Now - start).TotalSeconds < timeout_sec)
                Thread.Sleep(100);
            if ((DateTime.Now - start).TotalSeconds >= timeout_sec) {
                HourglassWindow.CloseHourglassWindow( _dispatcher, hg);
                throw new Exception("Timed out waiting for user to remove a cart");
            }
            HourglassWindow.CloseHourglassWindow( _dispatcher, hg);
        }

        public void Undock()
        {
            HourglassWindow hg = null;

            try {
                _external_communications.PrepareForUndock();
                // DKM 2011-06-10 need to monitor the rear door switch to see when it's closed
                const double timeout_sec = 15;
                // make sure the door is closed before moving the robot, or ignore completely if safeties are overridden
                DateTime start = DateTime.Now;
                if( !_safety_overridden) {
                    // DKM 2012-01-24 had to add door sensor check for LA2012
                    if( _controller.HasDoorSensor)
                        PromptUserToCloseDoor(timeout_sec);
                    PromptUserToResetInterlocks(timeout_sec);
                }

                // move robot
                Window owner = null;
                _dispatcher.Invoke( new Action( () => { owner = Window.GetWindow( GUI); } ));
                hg = HourglassWindow.ShowHourglassWindow( _dispatcher, owner, "Parking robot", true);
                WaitForBusVoltageOk();
                HourglassWindow.CloseHourglassWindow( _dispatcher, hg);
                _external_communications.ReadyToUndock();

                // DKM 2012-01-24 had to add door sensor check for LA2012
                if( _controller.HasDoorSensor)
                    PromptUserToOpenDoor(timeout_sec);
                // wait for user to fully open door in 4 seconds
                WaitForCartRelease();
                PromptUserToUndockCart( timeout_sec);
                _barcoded_plate_locations.Clear();

                // ask user to close door again
                // DKM 2012-01-24 had to add door sensor check for LA2012
                if( _controller.HasDoorSensor)
                    PromptUserToCloseDoor(timeout_sec);
                // ask user to reset interlocks
                PromptUserToResetInterlocks( timeout_sec);

                if( Undocked != null)
                    Undocked( this, new DockEventArgs( Name, "", ""));
            } catch (Exception ex) {
                _log.InfoFormat( "Undocking error at '{0}': {1}", Name, ex.Message);
                if( UndockingError != null)
                    UndockingError( this, new DockEventArgs( Name, "", ""));
                throw;
            } finally {
                HourglassWindow.CloseHourglassWindow( _dispatcher, hg);
            }
        }

        private void WaitForBusVoltageOk()
        {
            RobotInterface chosen_robot = _external_communications.GetRobotForDock(Name);
            DateTime start = DateTime.Now;
            while (!chosen_robot.BusVoltageOk && ((DateTime.Now - start).TotalSeconds) < 4)
                Thread.Sleep(100);
        }

        private void WaitForCartRelease()
        {
            Window owner = null;
            _dispatcher.Invoke( new Action( () => { owner = Window.GetWindow( GUI); } ));
            HourglassWindow hg = HourglassWindow.ShowHourglassWindow( _dispatcher, owner, "Cart release in 4s", true);
            Thread.Sleep(4000); // give the user enough time to open the door completely
            _controller.UndockCart();
            HourglassWindow.CloseHourglassWindow( _dispatcher, hg);
        }

        private void PromptUserToOpenDoor(double timeout_sec)
        {
            // wait for the user to open the door regardless of safety override state
            DateTime start = DateTime.Now;
            Window owner = null;
            _dispatcher.Invoke( new Action( () => { owner = Window.GetWindow( GUI); } ));
            HourglassWindow hg = HourglassWindow.ShowHourglassWindow( _dispatcher, owner, "Open door", false);
            while (_controller.CartDoorClosed && (DateTime.Now - start).TotalSeconds < timeout_sec)
                Thread.Sleep(100);
            if ((DateTime.Now - start).TotalSeconds >= timeout_sec) {
                HourglassWindow.CloseHourglassWindow( _dispatcher, hg);
                throw new Exception("Timed out waiting for user to open the door");
            }
            HourglassWindow.CloseHourglassWindow( _dispatcher, hg);
        }

        private void PromptUserToResetInterlocks(double timeout_sec)
        {
            // wait for user to reset interlocks
            DateTime start = DateTime.Now;
            Window owner = null;
            _dispatcher.Invoke( new Action( () => { owner = Window.GetWindow( GUI); } ));
            HourglassWindow hg = HourglassWindow.ShowHourglassWindow( _dispatcher, owner, "Reset interlocks", false);
            while (_safety_triggered && (DateTime.Now - start).TotalSeconds < timeout_sec)
                Thread.Sleep(100);
            if ((DateTime.Now - start).TotalSeconds >= timeout_sec) {
                HourglassWindow.CloseHourglassWindow( _dispatcher, hg);
                throw new Exception("Timed out waiting for user to reset interlocks");
            }
            HourglassWindow.CloseHourglassWindow( _dispatcher, hg);
        }

        private void PromptUserToCloseDoor(double timeout_sec)
        {
            // wait for user to close door so robot can park
            DateTime start = DateTime.Now;
            Window owner = null;
            _dispatcher.Invoke( new Action( () => { owner = Window.GetWindow( GUI); } ));
            HourglassWindow hg = HourglassWindow.ShowHourglassWindow( _dispatcher, owner, "Close door", false);
            while (!_controller.CartDoorClosed && (DateTime.Now - start).TotalSeconds < timeout_sec)
                Thread.Sleep(100);
            if ((DateTime.Now - start).TotalSeconds >= timeout_sec) {
                HourglassWindow.CloseHourglassWindow( _dispatcher, hg);
                throw new Exception("Timed out waiting for user to close door");
            }
            HourglassWindow.CloseHourglassWindow( _dispatcher, hg);
        }

        public event DockEventHandler Docked;
        public event DockEventHandler DockingError;
        public event DockEventHandler Undocked;
        public event DockEventHandler UndockingError;

        #endregion

        #region RobotAccessibleInterface Members

        public IEnumerable<PlateLocation> PlateLocationInfo
        {
            get
            {
                // if the cart is undocked, then we don't have any plate locations
                // DKM 2011-09-26 removed reinventorying requirement because it prevents us from getting teachpoint names when teaching for the first time
                if (!_controller.CartIsDocked)
                    return new List<PlateLocation>();

                // if the cart is docked, then we should have the ID of the cart, and therefore we know the available plate locations
                return PlateLocations.Values;
            }
        }

        public PlateLocation GetLidLocationInfo( string location_name)
        {
            return null;
        }

        public string TeachpointFilenamePrefix
        {
            get
            {
                return Name + "_" + _controller.CartFilePrefix;
            }
        }

        public string GetTeachpointFilenamePrefix( string barcode)
        {
            string file_prefix = _controller.GetFilePrefixFromBarcode( barcode);
            return Name + "_" + file_prefix;
        }

        public IEnumerable<string> GetAvailableLocationNames()
        {
            var all_plate_locations = from x in PlateLocationInfo select x.Name;
            var occupied_plate_locations = _barcoded_plate_locations.Values;
            return all_plate_locations.Except( occupied_plate_locations);
        }

        #endregion

        /// <summary>
        /// The dock is a special case, in that it has two different WOIs -- one for the cart ID, and
        /// another for flyby barcode reading.  I can determine which should be used by looking at the
        /// location name.  If it's in the "Rack, Slot" format, I know it's for flyby reading.  Otherwise,
        /// it's for cart ID purposes.
        /// </summary>
        /// <param name="location_name"></param>
        /// <returns></returns>
        public int GetBarcodeReaderConfigurationIndex( string location_name)
        {
            if( DockMonitorPlateLocation.IsValidPlateLocationName( location_name))
                return 4;
            else
                return 3;
        }

        public void ShowReadyToUndockOverlay( bool show = true)
        {
            ReadyToUndock = show;
            GUI.UndockOverlayVisible = (show ? Visibility.Visible : Visibility.Hidden);
        }

        #region PlateSchedulerDeviceInterface Members
        public event JobCompleteEventHandler JobComplete { add {} remove {} }
        public PlateLocation GetAvailableLocation( ActivePlate active_plate)
        {
            if( active_plate.GetCurrentToDo().Command == "Unload"){
                string location_name;
                bool has_plate = HasPlateWithBarcode( active_plate.Barcode, out location_name);
                return has_plate ? PlateLocations[ location_name] : null;
            } else if( active_plate.GetCurrentToDo().Command == "Load"){
                PlateTask.Parameter device_instance_parameter = active_plate.GetCurrentToDo().ParametersAndVariables.FirstOrDefault( param => param.Name == "device_instance");
                // if a device instance is specified and this dock is not the one, then don't return an available location.
                if( device_instance_parameter != null && device_instance_parameter.Value != Name){
                    return null;
                }
                return PlateLocations.Values.Where( plate_location => plate_location.Available).FirstOrDefault();
            } else{
                return null;
            }
        }

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

        public void LockPlace( PlatePlace place)
        {
        }

        public void AddJob( ActivePlate active_plate)
        {
            new Thread( () => JobThread( active_plate)){ Name = GetType().ToString() + " Job Thread", IsBackground = true}.Start();
        }

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

        public void EnqueueWorklist( Worklist worklist)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
