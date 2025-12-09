using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Media;
using BioNex.Shared.BioNexGuiControls;
using BioNex.Shared.DeviceInterfaces;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using log4net;

namespace BioNex.BPS140Plugin
{
    public class ViewModel : INotifyPropertyChanged
    {
        private BPS140 _bps140 { get; set; }
        private static readonly ILog _log = LogManager.GetLogger( typeof( BPS140));

        public Brush locked_color_;
        public Brush LockedColor
        {
            get { return locked_color_; }
            set {
                locked_color_ = value;
                OnPropertyChanged( "LockedColor");
            }
        }

        private Brush side1_color_;
        public Brush Side1Color
        {
            get { return side1_color_; }
            set {
                side1_color_ = value;
                OnPropertyChanged( "Side1Color");
            }
        }

        private Brush side2_color_;
        public Brush Side2Color
        {
            get { return side2_color_; }
            set {
                side2_color_ = value;
                OnPropertyChanged( "Side2Color");
            }
        }

        private string header_name_;
        public string HeaderName
        {
            get { return header_name_; }
            set {
                header_name_ = value;
                OnPropertyChanged( "HeaderName");
            }
        }

        private string reinventory_tooltip_;
        public string ReinventoryTooltip
        { 
            get {
                return reinventory_tooltip_;
            }

            set {
                reinventory_tooltip_ = value;
                OnPropertyChanged( "ReinventoryTooltip");
            }
        }

        string reinventory_selected_tooltip_;
        public string ReinventorySelectedToolTip
        {
            get { return reinventory_selected_tooltip_; }
            set {
                reinventory_selected_tooltip_ = value;
                OnPropertyChanged( "ReinventorySelectedToolTip");
            }
        }

        // these are used for databinding the PlateLocationManager's and inventory contents with the GUI
        public ObservableCollection<SideRackView> Side1InventoryView { get; set; }
        public ObservableCollection<SideRackView> Side2InventoryView { get; set; }

        public ViewModel( BPS140 controller)
        {
            _bps140 = controller;
            // command initialization
            ReinventoryCommand = new RelayCommand( ExecuteReinventory, CanExecuteReinventory);
            ReinventorySelectedCommand = new RelayCommand( ExecuteReinventorySelectedRacks, CanExecuteReinventorySelectedRacks);
            UpdateInventoryViewCommand = new RelayCommand( UpdateInventoryView);
            UnlockCommand = new RelayCommand( Unlock, CanExecuteUnlock);
            ResetInterlocksCommand = new RelayCommand( ExecuteResetInterlocks);

            Side1InventoryView = new ObservableCollection<SideRackView>();
            Side2InventoryView = new ObservableCollection<SideRackView>();

            _bps140.ReinventoryComplete += new EventHandler(Controller_ReinventoryComplete);
        }

        void Controller_ReinventoryComplete(object sender, EventArgs e)
        {
            //! \todo call via MainDispatcher!
            _bps140.Dispatcher.Invoke( new Action(UpdateInventoryView), null);
        }

        // diagnostics commands
        public RelayCommand ReinventoryCommand { get; set; }
        public RelayCommand ReinventorySelectedCommand { get; set; }
        public RelayCommand UpdateInventoryViewCommand { get; set; }
        public RelayCommand UnlockCommand { get; set; }
        public RelayCommand ResetInterlocksCommand { get; set; }

        public void UpdateSensors()
        {
            HeaderName = _bps140.Name + " Status";
            Side1Color = _bps140.Side1State ? Brushes.LightGreen : Brushes.LightGray;
            Side2Color = _bps140.Side2State ? Brushes.LightGreen : Brushes.LightGray;
            LockedColor = _bps140.LockedState ? Brushes.LightGray : Brushes.DarkRed;
        }

        private void Unlock()
        {
            _bps140.Unlock();
        }

        private bool CanExecuteUnlock()
        {
            return _bps140.AllowUserOverride;
        }

        private void ExecuteReinventory()
        {
            //! \todo I build the full list of racks from the plugin as well, so maybe I should
            //!       refactor into a method in PlateLocationManager?
            // here, we'll reinventory all of the racks on the current side facing the robot
            IEnumerable<int> racks_to_reinventory = null;
            if( _bps140.Controller.SideFacingRobot == 1)
                racks_to_reinventory = from x in _bps140.PlateLocationManager.Side1Racks select x.RackNumber;
            else if( _bps140.Controller.SideFacingRobot == 2)
                racks_to_reinventory = from x in _bps140.PlateLocationManager.Side2Racks select x.RackNumber;
            _bps140.Reinventory( racks_to_reinventory, UpdateInventoryView);
        }

        /// <summary>
        /// Checks the robot that can reach this BPS140 to see if it is homed
        /// </summary>
        /// <returns></returns>
        private bool IsRobotHomed()
        {
            IEnumerable<RobotInterface> robots = _bps140.DataRequestInterface.Value.GetRobotInterfaces();
            var robots_not_homed = from x in robots where x is DeviceInterface && !(x as DeviceInterface).IsHomed select x;
            if( robots_not_homed.Count() != 0)
                return false;
            return true;
        }

        private bool CanExecuteReinventory()
        {
            StringBuilder sb = new StringBuilder();

            if( _bps140.IsInUnsafeState())
                sb.AppendLine( "You cannot reinventory until the BPS140 is locked into position");
            if( !IsRobotHomed())
                sb.AppendLine( "Robot that accesses this device might not be homed");

            if( sb.Length != 0) {
                ReinventoryTooltip = sb.ToString();
                return false;
            }

            const bool valid = true;

            if( valid) {
                ReinventoryTooltip = "Reinventory all BPS140 racks";
            } 
/*            else 
            {
                ReinventoryTooltip = "You must teach the top and bottom slots for each rack before you can reinventory them";
            }*/
            return valid;
        }

        private void ExecuteReinventorySelectedRacks()
        {
            // only reinventory the side that's facing the robot
            ObservableCollection<SideRackView> side = _bps140.Controller.SideFacingRobot == 1 ? _bps140.PlateLocationManager.Side1Racks : _bps140.PlateLocationManager.Side2Racks;
            var selected_racks = from x in side where x.IsSelected select x.RackNumber;
            ObservableCollection<SideRackView> racks = _bps140.Controller.SideFacingRobot == 1 ? _bps140.PlateLocationManager.Side1Racks : _bps140.PlateLocationManager.Side2Racks;
            var racks_to_reinventory = from r in (racks.Where( x => selected_racks.Contains( x.RackNumber))) select r.RackNumber;
            _bps140.Reinventory( racks_to_reinventory, UpdateInventoryView);
        }

        private bool CanExecuteReinventorySelectedRacks()
        {
            // REFACTOR
            // only reinventory the side that's facing the robot
            ObservableCollection<SideRackView> side = _bps140.Controller.SideFacingRobot == 1 ? _bps140.PlateLocationManager.Side1Racks : _bps140.PlateLocationManager.Side2Racks;
            var selected_racks = from x in side where x.IsSelected select x.RackNumber;
            StringBuilder sb = new StringBuilder();
            if( selected_racks.Count() == 0)
                sb.AppendLine( "No racks have been selected yet");
            if( !IsRobotHomed())
                sb.AppendLine( "Robot that accesses this device might not be homed");

            bool taught_properly = CheckTopAndBottomSlotTeachpoints( selected_racks);
            if( !taught_properly) 
                sb.AppendLine( "You cannot reinventory until all of the selected racks have their top and bottom slots taught");

            if( sb.Length > 0) {
                ReinventorySelectedToolTip = sb.ToString();
                return false;
            }

            ReinventorySelectedToolTip = "Reinventory only the selected racks";
            return true;
        }

        private void ExecuteResetInterlocks()
        {
            Messenger.Default.Send<ResetInterlocksMessage>( new ResetInterlocksMessage());
        }

        private bool CheckTopAndBottomSlotTeachpoints( IEnumerable<int> selected_racks)
        {
            // DKM 2011-03-18 why is all of this commented out???
            /*
            // create teachpoint names for the top and bottom slots for all of the racks
            List<string> location_names = new List<string>();
            var rackviews_to_check = StaticInventoryView.Where( x => selected_racks.Contains( x.RackNumber));
            foreach( RackView rack in rackviews_to_check) {
                int slot_count = rack.SlotCount;
                if( slot_count == 0)
                    continue;
                PlateLocation top_slot = new PlateLocation( rack.RackNumber, rack.SlotCount);
                location_names.Add( top_slot.ToString());
                if( slot_count == 1)
                    continue;
                PlateLocation bottom_slot = new PlateLocation( rack.RackNumber, 1);
                location_names.Add( bottom_slot.ToString());
            }
            // now make sure all of those teachpoints actually exist
            List<string> teachpoint_names = DeviceTeachpoints[Name].GetNames().ToList();
            return location_names.Except( teachpoint_names).Count() == 0;
             */
            return true;
        }

        public void UpdateInventoryView()
        {
            UpdateInventorySide( 1);
            UpdateInventorySide( 2);
        } 

        public void UpdateInventorySide( int side)
        {
            ObservableCollection<SideRackView> view;
            ObservableCollection<SideRackView> plate_location_manager_racks;
            
            if( side == 1) {
                view = Side1InventoryView;
                plate_location_manager_racks = _bps140.PlateLocationManager.Side1Racks;
            } else {
                view = Side2InventoryView;
                plate_location_manager_racks = _bps140.PlateLocationManager.Side2Racks;
            }

            view.Clear();
            _bps140.PlateLocationManager.Clear( side_number:side);
            Dictionary<string, Dictionary<string,string>> inventory_data = _bps140.Inventory.GetInventoryData();

            // we need to register the PlateTypeChanged handler for all racks that DO NOT
            // have plates in inventory.  Use the following variable to keep track of
            // rack that ARE in inventory, and then register the handler for those not
            // in the set.
            HashSet<int> racks_in_inventory = new HashSet<int>();

            foreach( KeyValuePair<string, Dictionary<string,string>> kvp in inventory_data) {
                string barcode = kvp.Key;
                try {
                    int side_number = int.Parse( kvp.Value["side"].ToString());
                    if( side_number != side){
                        continue;
                    }
                    int rack_number = int.Parse( kvp.Value["rack"].ToString());
                    
                    int slot_number = int.Parse( kvp.Value["slot"].ToString());
                    bool loaded = bool.Parse( kvp.Value["loaded"].ToString());
                    SideRackView rackview = plate_location_manager_racks[rack_number - 1];
                    rackview.SetSlotPlate( slot_number, barcode, loaded ? SlotView.SlotStatus.Loaded : SlotView.SlotStatus.Unloaded);

                    // only register the handler once
                    if( !racks_in_inventory.Contains( rack_number)) {
                        rackview.PlateTypeChanged += new SideRackView.SidePlateTypeChangedEventHandler( rackview_PlateTypeChanged);
                    }
                    racks_in_inventory.Add( rack_number);
                } catch( Exception ex) {
                    // couldn't get the rack and/or slot for whatever reason, so log this and continue
                    _log.Info( "Rack and slot information was not present in inventory data", ex);
                }
            }

            // register the PlateTypeChanged handler for the racks that aren't in inventory
            var racks_not_in_inventory = from x in Enumerable.Range( 1, plate_location_manager_racks.Count()) where !racks_in_inventory.Contains(x) select x;
            foreach( var x in racks_not_in_inventory) {
                SideRackView rackview = plate_location_manager_racks[x - 1];
                rackview.PlateTypeChanged += new SideRackView.SidePlateTypeChangedEventHandler(rackview_PlateTypeChanged);
            }

            // now add in the unbarcoded plates
            foreach( BPS140PlateLocation pl in _bps140.UnbarcodedPlates) {
                // skip unbarcoded plates on the opposite side of the BPS140
                if( pl.SideNumber != side)
                    continue;

                plate_location_manager_racks[pl.RackNumber-1].SetSlotPlate( pl.SlotNumber, "", SlotView.SlotStatus.Unknown);
            }

            // now that the data is deconvolved, we can add it to StaticInventoryView
            foreach( SideRackView rackview in plate_location_manager_racks)
                view.Add( rackview);
        }

        void rackview_PlateTypeChanged(object sender, SideRackView.SidePlateTypeChangedEventArgs e)
        {
            // set the values in the configuration object
            _bps140.Controller.Config.SetRackPlateType( e.SideNumber, e.RackNumber, e.RackType);
            _bps140.Controller.SaveXmlConfiguration();
            _log.Debug( String.Format( "Set plate type in rack {0} to {1}", e.RackNumber, e.RackType));
        }

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
