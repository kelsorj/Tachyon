using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using BioNex.Shared.BarcodeMisreadDialog;
using BioNex.Shared.BioNexGuiControls;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Location;
using BioNex.Shared.Utils;
using log4net;

namespace BioNex.BPS140Plugin
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(DeviceInterface))]
    [Export(typeof(PlateStorageInterface))]
    [Export(typeof(SystemStartupCheckInterface))]
    public class BPS140 : SystemStartupCheckInterface, AccessibleDeviceInterface, PlateStorageInterface, INotifyPropertyChanged
    {
        public event EventHandler ReinventoryBegin
        {
            add { _reinventory_strategy.ReinventoryStrategyBegin += value; }
            remove { _reinventory_strategy.ReinventoryStrategyBegin -= value; } 
        }
        public event EventHandler ReinventoryComplete;
        public event EventHandler ReinventoryError;

        internal Controller Controller { get; set; }
        [Import("MainDispatcher")]
        internal Dispatcher Dispatcher { get; set; }
        private static readonly ILog Log = LogManager.GetLogger( typeof( BPS140));

        private IReinventoryStrategy _reinventory_strategy { get; set; }

        public bool AllowUserOverride
        {
            get { return Controller.AllowUserOverride; }
        }

        // inventory stuff
        internal PlateLocationManager PlateLocationManager { get; set; }
        internal List<BPS140PlateLocation> UnbarcodedPlates { get; set; }
        internal IDictionary< string, PlateLocation> PlateLocations { get; set; }

        // these three properties are passed through from the underlying Controller, so that the
        // ViewModel will be able to change its status LEDs
        public bool LockedState { 
            get { return Controller.IsLocked; }
        }
        public bool Side1State {
            get { return Controller.LimitSwitchAOn; }
        }
        public bool Side2State {
            get { return Controller.LimitSwitchBOn; }
        }

        // device properties, comes from Device Manager database
        private Dictionary<string,string> DeviceProperties { get; set; }
        internal static readonly string ConfigFolder = "configuration folder";
        internal static readonly string Simulate = "simulate";
        internal static readonly string RackConfigurationA = "rack configuration, side A";
        internal static readonly string RackConfigurationB = "rack configuration, side B";
        internal static readonly string IODeviceName = "i/o device name";
        internal static readonly string Self = "self"; // DKM 2012-01-16 added this so that we can use plate hotels as a single-sided BPS140
        internal static readonly string SimulateBCR = "simulate BCR";

        private System.Windows.Window _diagnostics_panel { get; set; }

        [Import]
        internal BioNex.Shared.LibraryInterfaces.IInventoryManagement Inventory { get; set; }
        [Import]
        public Lazy<ExternalDataRequesterInterface> DataRequestInterface { get; set; }

        public BPS140()
        {
            Controller = new Controller( this);
            UnbarcodedPlates = new List<BPS140PlateLocation>();
            PlateLocations = new Dictionary< string, PlateLocation>();
        }

        public void Unlock()
        {
            Controller.Unlock();
        }

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
                return "BPS140";
            }
        }

        public string Name { get; private set; }

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
                return "Dual-sided rotating plate storage shelves";
            }
        }

        public System.Windows.Controls.UserControl GetDiagnosticsPanel()
        {
            if( !Controller.Connected) {
                try {
                    Connect();
                } catch( Exception ex) {
                    MessageBox.Show( ex.Message);
                    return null;
                }
            }

            DiagnosticsPanel panel = new DiagnosticsPanel( this);
            return panel;
        }

        public void SetProperties(DeviceManagerDatabase.DeviceInfo device_info)
        {
            Name = device_info.InstanceName;
            DeviceProperties = new Dictionary<string,string>( device_info.Properties);
        }

        public void ShowDiagnostics()
        {
            if( !Controller.Connected) {
                try {
                    Connect();
                } catch( Exception ex) {
                    MessageBox.Show( ex.Message);
                }
            }

            if( _diagnostics_panel == null) {
                _diagnostics_panel = new System.Windows.Window();
                _diagnostics_panel.Content = new BioNex.BPS140Plugin.DiagnosticsPanel( this);
                _diagnostics_panel.Closed += new EventHandler(_diagnostics_panel_Closed);
                _diagnostics_panel.Title =  Name + "- Diagnostics" + (Controller.Simulating ? " (Simulating)" : "");
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
            try {
                // initialize inventory stuff first since we'll need to populate it from the teachpoint processing code
                //! \todo remove InventoryFile from device manager since we want to tie the database name to the device name
                string inventory_path = (DeviceProperties[ConfigFolder] + "\\inventory.s3db").ToAbsoluteAppPath();
                try {
                    Inventory.LoadDatabase( inventory_path, new List<string> { "rack", "slot", "side"});
                } catch( BioNex.Shared.LibraryInterfaces.InventoryFileDoesNotExistException ex) {
                    MessageBoxResult response = MessageBox.Show( String.Format( "The inventory file {0} was not found.  Would you like to create it?", ex.FilePath), "BPS140 Inventory", MessageBoxButton.YesNo);
                    if( response == MessageBoxResult.Yes) {
                        Inventory.CreateDatabase( ex.FilePath, new List<string> { "rack", "slot", "side" });
                    }
                }

                // connect to hardware here
                try {
                    bool simulate = DeviceProperties[Simulate] != "0";

                    if (simulate) {
                        Controller.Connect(null, DeviceProperties, DataRequestInterface.Value.SafeToMove, simulate);
                    } else {
                        IEnumerable<IOInterface> io_interfaces = DataRequestInterface.Value.GetIOInterfaces();
                        IOInterface io_interface = (from i in io_interfaces where (i as DeviceInterface).Name == DeviceProperties[IODeviceName] select i).FirstOrDefault();
                        // DKM 2012-01-16 added check for "Self" to allow a null IODevice, in cases where we want to support a "one-sided" BPS140, e.g. plate hotels on LA2012 software running on SYS2358 hardware
                        if (io_interface == null && DeviceProperties[IODeviceName] != Self) {
                            MessageBox.Show(String.Format("Could not find IO provider '{0}'.", DeviceProperties[IODeviceName]));
                            return;
                        }

                        // DKM 2012-01-16 remember that now io_interface can be null to support a one-sided BPS140
                        Controller.Connect(io_interface, DeviceProperties, DataRequestInterface.Value.SafeToMove, simulate);
                    }
                } catch (Exception ex) {
                    // DKM 2011-03-22 there is something weird about this exception handler.  It does not have the inner
                    // exception that I had set when LoadRackTypes fails.
                    MessageBox.Show( "Connection failed: " + ex.Message);
                }

                // initialize plate location manager and rack configuration
                // device manager handles the rack configuration.  stored as a comma-delimited set of numbers
                try {
                    string config_string1 = DeviceProperties[RackConfigurationA];
                    List<int> rack1_configuration = (from s in config_string1.Split( ',') select int.Parse( s)).ToList();
                    string config_string2 = DeviceProperties[RackConfigurationB];
                    List<int> rack2_configuration = (from s in config_string2.Split( ',') select int.Parse( s)).ToList();

                    // create platelocationinfos:
                    IEnumerable< string> side_1_location_names = from rack_number in Enumerable.Range( 1, rack1_configuration.Count)
                                                                 from slot_number in Enumerable.Range( 1, rack1_configuration[ rack_number - 1])
                                                                 select new BPS140PlateLocation( 1, rack_number, slot_number).ToString();
                    IEnumerable< string> side_2_location_names = from rack_number in Enumerable.Range( 1, rack2_configuration.Count)
                                                                 from slot_number in Enumerable.Range( 1, rack2_configuration[ rack_number - 1])
                                                                 select new BPS140PlateLocation( 2, rack_number, slot_number).ToString();
                    PlateLocations = side_1_location_names.Union( side_2_location_names).ToDictionary( loc => loc, loc => new PlateLocation( loc));
                    // end create platelocationinfos.

                    PlateLocationManager = new PlateLocationManager( Inventory, Controller.Config, rack1_configuration, rack2_configuration);
                } catch (KeyNotFoundException) {
                    MessageBox.Show( String.Format( "Cannot configure plate locations in '{0}' because the 'rack configuration' property is not specified in the device manager database", Name));
                }


                // ---------------- BCR HANDLING -----------------
                // DKM 2011-10-24 replaced catching of KeyNotFoundException with a TryGetValue instead
                string val;
                bool simulate_bcr = false;
                if( DeviceProperties.TryGetValue( SimulateBCR, out val))
                    simulate_bcr = (val != "0");
                if( simulate_bcr) {
                    _reinventory_strategy = new SimulatedReinventory( this);
                } else {
                    _reinventory_strategy = new BarcodeReinventory( this);
                }
                _reinventory_strategy.ReinventoryStrategyComplete += new EventHandler(_reinventory_strategy_ReinventoryStrategyComplete);
                _reinventory_strategy.ReinventoryStrategyError += new EventHandler(_reinventory_strategy_ReinventoryStrategyError);
                // -----------------------------------------------

            } catch( Exception ex) {
                MessageBox.Show( "Barcode reader connection failed: " + ex.Message);
            }
        }

        void _reinventory_strategy_ReinventoryStrategyError(object sender, EventArgs e)
        {
            if( ReinventoryError != null)
                ReinventoryError( this, e);
        }

        void _reinventory_strategy_ReinventoryStrategyComplete(object sender, EventArgs e)
        {
            if( ReinventoryComplete != null)
                ReinventoryComplete( this, e);
        }

        public bool Connected { get { return Controller.Connected; } }

        public void Home()
        {
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
            Controller.Close();
        }

        public bool ExecuteCommand(string command, IDictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetCommands()
        {
            return new List<string>();
        }

        public void Abort() {}
        public void Pause() {}
        public void Resume() {}
        public void Reset() {}

        public IEnumerable<PlateLocation> PlateLocationInfo
        {
            get
            {
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
                return Name;
            }
        }


        #endregion

        #region PlateStorageInterface Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="labware_name"></param>
        /// <param name="barcode"></param>
        /// <param name="location_name">only needed if you're loading unbarcoded labware</param>
        public void Unload(string labware_name, string barcode, string location_name)
        {
            // temporary workaround
            if( Controller.Simulating)
                return;

            if( barcode != "") 
                Inventory.Unload( barcode);
            else
                UnbarcodedPlates.Remove( BPS140PlateLocation.FromString( location_name));
        }

        public void Load(string labware_name, string barcode, string location_name)
        {
            if( barcode == "")
                return;

            BPS140PlateLocation plate_location = BPS140PlateLocation.FromString( location_name);
            Inventory.Load( barcode, new Dictionary<string,string> { 
                                                                        {"side", plate_location.SideNumber.ToString()},
                                                                        {"rack", plate_location.RackNumber.ToString()},
                                                                        {"slot", plate_location.SlotNumber.ToString()} 
                                                                   });
        }

        public bool HasPlateWithBarcode(string barcode, out string plate_location)
        {
            // I had to remove this because we have cases where we want to simulate the BPS140 with an actual robot
            // Unforunately, now I have a case where I want to simulate the BPS so that it can return a fake plate
            // location to a simulated Hive!
            if( Controller.Simulating) {
                plate_location = String.Format( "Side {0}: Rack 1, Slot 1", Controller.SideFacingRobot);
                return true;
            }

            // look in storage database to find barcode
            try {
                Dictionary<string,string> location = Inventory.GetLocation( barcode);
                if( !bool.Parse( location["loaded"])) {
                    Log.Info( String.Format( "The barcode '{0}' is present in the inventory for device '{1}', but it was already unloaded", barcode, Name));
                    plate_location = "";
                    return false;
                }
                // DKM 2010-10-06 better make sure that this location is present on the side facing the robot!!!!!
                if( Controller.SideFacingRobot != int.Parse( location["side"])) {
                    plate_location = "";
                    return false;
                }

                // otherwise, we found the barcode and it's on the right side of the BPS140
                plate_location = new BPS140PlateLocation( int.Parse( location["side"]), int.Parse( location["rack"]), int.Parse( location["slot"])).ToString();
                return true;
            } catch( InventoryBarcodeNotFoundException) {
                plate_location = "";
                return false;
            }
        }

        public IEnumerable<string> GetLocationsForLabware( string labware_name)
        {
            return new List<string>();
        }

        public IEnumerable<string> GetStorageLocationNames()
        {
            return (from x in PlateLocationManager.GetPlateLocations( Controller.SideFacingRobot) select x.ToString());
        }

        public IEnumerable<KeyValuePair<string,string>> GetInventory( string robot_name)
        {
            Dictionary<string, Dictionary<string,string>> inventory_data = Inventory.GetInventoryData();

            List<KeyValuePair<string,string>> locations = new List<KeyValuePair<string,string>>();
            foreach( KeyValuePair<string, Dictionary<string,string>> kvp in inventory_data) {
                string barcode = kvp.Key;
                try {
                    int rack_number = int.Parse( kvp.Value["rack"].ToString());
                    int slot_number = int.Parse( kvp.Value["slot"].ToString());
                    int side_number = int.Parse( kvp.Value["side"].ToString());
                    bool loaded = bool.Parse( kvp.Value["loaded"].ToString());
                    if( loaded && side_number == Controller.SideFacingRobot)
                        locations.Add( new KeyValuePair<string,string>( barcode, new BPS140PlateLocation( side_number, rack_number, slot_number).ToString()));
                } catch( Exception ex) {
                    // couldn't get the rack and/or slot for whatever reason, so log this and continue
                    Log.Info( "Rack and slot information was not present in inventory data", ex);
                }
            }

            return locations;
        }

        public bool IsInUnsafeState()
        {
            return !Controller.IsLocked || (!Controller.LimitSwitchAOn && !Controller.LimitSwitchBOn);
        }

        /// <summary>
        /// this method gets called by Synapsis to reinventory from the main GUI, so no update callback is used
        /// </summary>
        /// <returns></returns>
        public bool Reinventory( bool park_robot_after)
        {
            // here, we'll reinventory all of the racks on the current side facing the robot
            IEnumerable<int> racks_to_reinventory = null;
            if( Controller.SideFacingRobot == 1)
                racks_to_reinventory = from x in PlateLocationManager.Side1Racks select x.RackNumber;
            else if( Controller.SideFacingRobot == 2)
                racks_to_reinventory = from x in PlateLocationManager.Side2Racks select x.RackNumber;
            Reinventory( racks_to_reinventory, null);
            return true;
        }

        internal void Reinventory( IEnumerable<int> racks_to_reinventory, Action update_callback)
        {
            ReinventoryDelegate reinventory = new ReinventoryDelegate( _reinventory_strategy.ReinventorySelectedRacksThread);
            // if update_callback isn't null, then we know we're getting called from diags
            reinventory.BeginInvoke( racks_to_reinventory, update_callback, !(update_callback == null), _reinventory_strategy.ReinventoryThreadComplete, true);
        }

        internal void HandleBarcodeMisreads( List<BarcodeReadErrorInfo> misread_barcode_info)
        {
            if( Dispatcher == null) {
                const string error = "Could not display manual barcode entry dialog";
                Log.Info( error);
                MessageBox.Show( error);
                return;
            }

            Dispatcher.Invoke( new ShowDialogDelegate( ShowBarcodeMisreadsDialog), misread_barcode_info); 
        }

        private delegate void ShowDialogDelegate( List<BarcodeReadErrorInfo> misread_barcode_info);

        private void ShowBarcodeMisreadsDialog( List<BarcodeReadErrorInfo> misread_barcode_info)
        {
            BarcodeMisread dlg = new BarcodeMisread( misread_barcode_info);
            dlg.ShowDialog();
            try {
                // now that we have the user-entered barcodes, go ahead and update inventory accordingly
                foreach( BarcodeReadErrorInfo info in dlg.Barcodes) {
                    if( !info.NoPlatePresent && info.NewBarcode.Trim() != "") {
                        UnbarcodedPlates.Remove( BPS140PlateLocation.FromString( info.TeachpointName));
                        Load( "", info.NewBarcode.Trim(), info.TeachpointName);
                    } else if( info.NoPlatePresent) {
                        UnbarcodedPlates.Remove( BPS140PlateLocation.FromString( info.TeachpointName));
                        Load( "", "EMPTY", info.TeachpointName);
                    }
                }
                
            } catch( Exception ex) {
                MessageBox.Show( String.Format( "Could not save the manually-entered barcodes: {0}\r\nPlease reinventory the {1} plate storage.", ex.Message, Name));
            }
        }

        internal void SaveBarcodeMisreadInfo( int side, int rack_number, List<string> barcodes, List<BarcodeReadErrorInfo> missed_barcode_info, List<byte> reread_condition_masks)
        {
            for( int i=0; i<barcodes.Count(); i++) {
                byte mask = reread_condition_masks[i];
                string barcode = barcodes[i];
                int slot_number = i + 1;
                // check the barcode against the mask
                if( (mask & ScanningParameters.RereadNoRead) != 0 && Constants.IsNoRead(barcode))
                    missed_barcode_info.Add( new BarcodeReadErrorInfo( (new BPS140PlateLocation( side, rack_number, slot_number)).ToString(), barcode));
                if( (mask & ScanningParameters.RereadMissedStrobe) != 0 && barcode == "")
                    missed_barcode_info.Add( new BarcodeReadErrorInfo( (new BPS140PlateLocation( side, rack_number, slot_number)).ToString(), barcode));
                if( (mask & ScanningParameters.FoundBarcode) != 0 && !Constants.IsNoRead(barcode))
                    missed_barcode_info.Add( new BarcodeReadErrorInfo( (new BPS140PlateLocation( side, rack_number, slot_number)).ToString(), barcode));
            }
        }

        public void DisplayInventoryDialog()
        {
            ShowDiagnostics();
        }

        #endregion

        IEnumerable<RackView> GetRacksNeededForReinventory( int side, IEnumerable<int> racks_to_reinventory)
        {
            ObservableCollection<SideRackView> all_racks = ( ( side == 1) ? PlateLocationManager.Side1Racks : PlateLocationManager.Side2Racks);
            return from x in all_racks where racks_to_reinventory.Contains( x.RackNumber) select x;
        }

        internal void EnterPlatesIntoInventory( List< string> barcodes, int side_number, int rack_number)
        {
            for( int loop = 1; loop <= barcodes.Count; ++loop){
                string barcode = barcodes[loop - 1];
                if( Constants.IsNoRead(barcode))
                    UnbarcodedPlates.Add( new BPS140PlateLocation( side_number, rack_number, loop));
                else {
                    // this plate could have replaced a previously unbarcoded plate, so do a lookup
                    // and remove from UnbarcodedPlates if present
                    UnbarcodedPlates.Remove(new BPS140PlateLocation( side_number, rack_number, loop));
                    Inventory.Load( barcodes[ loop - 1], new Dictionary< string, string> { { "side", side_number.ToString()},
                                                                                           { "rack", rack_number.ToString()},
                                                                                           { "slot", loop.ToString()}
                                                                                         }
                                   );
                }
            }
        }

        public int GetBarcodeReaderConfigurationIndex( string location_name)
        {
            return 2;
        }

        #region SystemStartupCheckInterface Members

        public override bool IsReady( out string reason)
        {
            reason = "";

            if( !Controller.Connected) {
                reason = Name + " not connected";
                return false;
            }
            if( !Controller.IsLocked) {
                reason = Name + " not locked";
                return false;
            }
            return true;
        }

        public override System.Windows.Controls.UserControl GetSystemPanel()
        {
            if( !Controller.Connected) {
                try {
                    Connect();
                } catch( Exception) {
                }
            }

            MiniSystemPanel panel = new MiniSystemPanel( this);
            return panel;
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion
    }
}
