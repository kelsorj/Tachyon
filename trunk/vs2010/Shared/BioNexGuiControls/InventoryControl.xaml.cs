using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using log4net;

namespace BioNex.Shared.BioNexGuiControls
{
    /// <summary>
    /// Interaction logic for InventoryControl.xaml
    /// </summary>
    public partial class InventoryControl : UserControl
    {
        //------------------------------------------ Other Class Definitions ----------------------------------------------
        public class RackDefinition
        {
            public int NumberOfSlots { get; set; }
            public RackView.PlateTypeT PlateType { get; set; }

            // DKM 2011-05-10 for now, assume carts are always for barcoded plates, and not tipboxes
            public RackDefinition( int num_slots)
            {
                NumberOfSlots = num_slots;
            }
        }

        public delegate void RackPlateTypeChangedEventHandler( object sender, RackView.PlateTypeChangedEventArgs args);

        //-----------------------------------------------------------------------------------------------------------------

        public ObservableCollection<RackView> InventoryView { get; set; }
        public event RackPlateTypeChangedEventHandler RackPlateTypeChanged;
        private static readonly ILog _log = LogManager.GetLogger( typeof( InventoryControl));

        /// <summary>
        /// The name of the column for racks in the inventory database.  For example, the Hive has
        /// a column called "rack" in its plate storage database
        /// </summary>
        public string RackColumnName { get; set; }
        /// <summary>
        /// The name of the column for slots in the inventory database.  For example, the Hive has
        /// a column called "slot" in its plate storage database
        /// </summary>
        public string SlotColumnName { get; set; }
        public string LoadedColumnName { get; set; }

        public InventoryControl()
        {
            InitializeComponent();

            DataContext = this;
            InventoryView = new ObservableCollection<RackView>();
        }

        /// <summary>
        /// This should be called after construction of the control, so that we can set the number of racks, etc.
        /// </summary>
        /// <param name="rack_definitions"></param>
        public void SetRackConfigurations( List<RackDefinition> rack_definitions)
        {
            InventoryView.Clear();
            for( int i=0; i<rack_definitions.Count(); i++) {
                var rack = rack_definitions[i];
                // I just created a new RackView for cases where this might get called multiple times (unlikely)
                // so we don't have to worry about iterating over existing RackViews and unregistering event handlers
                RackView rackview = new RackView( i, Enumerable.Range( 0, rack.NumberOfSlots), RackView.PlateTypeT.Unavailable);
                rackview.PlateTypeChanged += new RackView.PlateTypeChangedEventHandler(rackview_PlateTypeChanged);
                InventoryView.Add( rackview);
            }
        }

        /// <summary>
        /// Every time a RackView gets its plate type modified, this event gets fired so the container can deal with it (e.g. save a config XML file)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void rackview_PlateTypeChanged(object sender, RackView.PlateTypeChangedEventArgs e)
        {
            if( RackPlateTypeChanged != null)
                RackPlateTypeChanged( this, e);
        }

        /// <summary>
        /// Inventory data is passed in as a map of barcode to [rack,slot] info
        /// </summary>
        /// <remarks>
        /// Key for outer dictionary is the barcode
        /// Key for inner dictionary is the database column name
        /// Value for inner dictionary is the column's value in the database
        /// </remarks>
        /// <param name="inventory_data"></param>
        public void UpdateFromInventory( Dictionary<string, Dictionary<string,string>> inventory_data)
        {
            if( string.IsNullOrEmpty( RackColumnName) || string.IsNullOrEmpty( SlotColumnName) || string.IsNullOrEmpty( LoadedColumnName)) {
                _log.Info( "The InventoryControl was not configured properly.  RackColumnName, SlotColumnName, and LoadedColumnName need to be set before displaying database contents.");
                return;
            }

            foreach( var kvp in inventory_data) {
                string barcode = kvp.Key;
                try {
                    int rack_number = int.Parse( kvp.Value[RackColumnName].ToString());
                    int slot_number = int.Parse( kvp.Value[SlotColumnName].ToString());
                    RackView rackview = InventoryView[rack_number - 1];
                    rackview.SetSlotPlate( slot_number, barcode, SlotView.SlotStatus.Loaded);
                } catch( Exception ex) {
                    // couldn't get the rack and/or slot for whatever reason, so log this and continue
                    _log.Info( "Rack and slot information was not present in inventory data", ex);
                }
            }
        }

        public void SetSlotLoadedOrUnloaded( int rack_index, int slot_index, string barcode, SlotView.SlotStatus loaded_or_unloaded)
        {
            RackView rackview = InventoryView[rack_index];
            rackview.SetSlotPlate( slot_index + 1, barcode, loaded_or_unloaded);
        }

        public void Clear()
        {
            foreach( var rack in InventoryView) {
                rack.Clear();
            }
        }
    }
}
