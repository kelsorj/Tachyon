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
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace BioNex.Shared.BioNexGuiControls
{
    /// <summary>
    /// Interaction logic for RackView.xaml
    /// </summary>
    public partial class RackView : UserControl, INotifyPropertyChanged
    {
        // for notifying others that a rack's plate type changed
        public delegate void PlateTypeChangedEventHandler( object sender, PlateTypeChangedEventArgs e);
        public event PlateTypeChangedEventHandler PlateTypeChanged;
        public class PlateTypeChangedEventArgs : EventArgs
        {
            public int RackNumber { get; set; }
            public PlateTypeT RackType { get; set; }

            public PlateTypeChangedEventArgs( int rack_number, PlateTypeT rack_type)
            {
                RackNumber = rack_number;
                RackType = rack_type;
            }
        }

        public int RackNumber { get; set; }
        // DKM 2011-04-25 with the new method, slot numbers could be sparsely populated
        public List<int> SlotIndexes{ get; set; }

        public PlateTypeT CurrentPlateType { get; set; }

        // plate type is no longer associated with the rack.  When you change the "rack" plate type,
        // you effectively change each of the slots' type.
        //public PlateTypeT PlateType { get; protected set; }
        public ObservableCollection<SlotView> Slots { get; protected set; }

        public bool IsSelected { get; set; }

        public ICollectionView PlateTypeView { get; set; }
        // DKM 2011-04-12 I don't think the whole mask idea makes sense anymore...
        // DKM 2011-06-03 when I changed from mask to non-mask, I forgot to change Tipbox to 0!!!
        //                and now we can no longer change this or we'll break existing systems (Igenica and Pioneer 2)
        // DKM 2011-06-03 Actually, this is not true, so I changed it.  The config.xml for Hive and BPS140
        //                had the element name "PlateTypeMask", but I had already changed it to
        //                "DefaultPlateType", so there's already an incompatibility that has to be
        //                dealt with later.
        public enum PlateTypeT { Tipbox, Barcode, Unavailable };
        // there has got to be a better way to do this
        private List<PlateTypeT> _plate_types = new List<PlateTypeT> {  PlateTypeT.Tipbox,
                                                                        PlateTypeT.Barcode };

        public Visibility PlateTypeVisibility { get; set; }

        public RackView( int rack_index, IEnumerable<int> slot_numbers_0_based, PlateTypeT plate_type)
        {
            InitializeComponent();
            DataContext = this;
            RackNumber = rack_index + 1;
            SlotIndexes = slot_numbers_0_based.ToList();
            Slots = new ObservableCollection<SlotView>();
            // now create all of the slots in the UserControl
            // DKM 2011-04-25 since we now might have to skip slots (i.e. Monsanto Phase 1), like
            //                going from Slot 3 to Slot 9, get the max slot number from SlotNumbers
            //                and loop over this counter.  This way, if the index of the loop is
            //                NOT in SlotNumbers, we know that we have to create a dummy SlotView.
            // e.g. rack has slots 1, 2, 3, 9, 10, and 11 defined.  We loop from 1 to 11 so that
            //      we can detect that racks 4, 5, 6, 7, and 8 should be drawn transparent so that
            //      the GUI will have a visible gap there.
            int max_slot_number = SlotIndexes.Max() + 1;
            for( int i=0; i<max_slot_number; i++) {
                // note that here we will arbitrarily set the plate type for each slot to Tipbox,
                // and assume that later on when the configuration XML file gets loaded, we
                // we specifically set each plate type again.
                SlotView slot;
                if( SlotIndexes.Contains( i)) {
                    slot = new SlotView( RackNumber, i + 1, SlotView.SlotStatus.Empty, PlateTypeT.Tipbox);
                } else {
                    slot = new SlotView( RackNumber, i + 1, SlotView.SlotStatus.NoSlot, PlateTypeT.Tipbox);
                }
                rack_stack_panel.Children.Add( slot.View);
                Slots.Add( slot);
            }

            // DKM 2011-05-10 for carts, allow disabling of the rack's plate type
            if( plate_type != PlateTypeT.Unavailable) {
                // DKM 2012-01-18 forgot to set type of rack upon instantiation
                CurrentPlateType = plate_type;
                PlateTypeView = CollectionViewSource.GetDefaultView( _plate_types);
                OnPropertyChanged( "PlateTypeView");
                // the rack could be all tipboxes, all barcodes, or mixed.  We can determine that the
                // rack is all of one type by counting the occurrences of Tipbox, for example, and
                // comparing the count to the number of slots
                PlateTypeView.MoveCurrentTo( plate_type);
                PlateTypeView.CurrentChanged += new EventHandler(PlateTypeView_CurrentChanged);
                PlateTypeVisibility = System.Windows.Visibility.Visible;
            } else {
                PlateTypeVisibility = System.Windows.Visibility.Collapsed;
            }
        }

        protected virtual void PlateTypeView_CurrentChanged(object sender, EventArgs e)
        {
            // set the rack's type here.  Needs to be serialized in the plugin that uses this control
            ICollectionView view = sender as ICollectionView;
            if( view == null || view.CurrentItem == null)
                return;

            var selected_type = (PlateTypeT)view.CurrentItem;
            // DKM 2012-01-18 in case rack type changes at runtime
            CurrentPlateType = selected_type;
            // notify anyone that has registered for plate type changes
            if( PlateTypeChanged != null)
                PlateTypeChanged( this, new PlateTypeChangedEventArgs( RackNumber, selected_type));
        }

        public void Clear()
        {
            for( int i=0; i<Slots.Count(); i++) {
                SetSlotPlate(i + 1, "", SlotView.SlotStatus.Empty);
            }
        }

        public void SetSlotPlate(int slot_number, string barcode, SlotView.SlotStatus slot_status)
        {
            PlateTypeT original_type = Slots[slot_number - 1].PlateType;
            SlotView slot = new SlotView( RackNumber, slot_number, slot_status, original_type, barcode);
            Slots[slot_number - 1] = slot;
            rack_stack_panel.Children.RemoveAt( slot_number - 1);
            rack_stack_panel.Children.Insert( slot_number - 1, slot.View);
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion
    }


    /// <summary>
    /// encapsulates everything about a slot - whether or not it's
    /// occupied, what barcode is present, what rack and slot location
    /// it's at.
    /// </summary>
    public class SlotView
    {
        // DKM 2011-04-25 added NoSlot to support gaps in a rack, like on the Monsanto Phase 1 systems
        public enum SlotStatus { Empty, Unloaded, Loaded, Unknown, NoSlot };

        public int Rack { get; private set; }
        public int Slot { get; private set; }
        public SlotStatus Occupied { get; set; }
        public string Barcode { get; set; }
        public UIElement View { get; private set; }
        public RackView.PlateTypeT PlateType { get; set; }

        public SlotView( int rack_number, int slot_number, SlotStatus occupied, RackView.PlateTypeT plate_type)
            : this(rack_number, slot_number, occupied, plate_type, "")
        {
            PlateType = plate_type;
        }

        public override string ToString()
        {
            return String.Format( "Rack {0}, Slot {1}, barcode ='{2}', occupied = {3}, plate type = {4}", Rack, Slot, Barcode, Occupied, PlateType);
        }

        public SlotView( int rack_number, int slot_number, SlotStatus occupied, RackView.PlateTypeT plate_type, string barcode)
        {
            Rack = rack_number;
            Slot = slot_number;
            Occupied = occupied;
            Barcode = barcode;

            Border border = new Border { MinHeight=25, CornerRadius=new CornerRadius(5) };
            TextBlock text = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.Black,
                Text = Barcode
            };

            switch( occupied) {
                case SlotStatus.Empty:
                    border.Background = Brushes.LightGray;
                    text.Foreground=Brushes.Transparent;
                    text.Text = "";
                    break;
                case SlotStatus.Loaded:
                    border.Background = Brushes.DarkGreen;
                    text.Foreground = Brushes.Yellow;
                    break;
                case SlotStatus.Unloaded:
                    border.Background = Brushes.DarkGoldenrod;
                    break;
                case SlotStatus.Unknown:
                    border.Background = Brushes.Purple;
                    text.Foreground = Brushes.Yellow;
                    break;
                case SlotStatus.NoSlot:
                default:
                    border.Background = Brushes.Transparent;
                    text.Foreground=Brushes.Transparent;
                    text.Text = "";
                    break;

            }

            border.Child = text;
            View = border;
        }
    }

    public class SideRackView : RackView
    {
        public int SideNumber { get; set; }
        public delegate void SidePlateTypeChangedEventHandler( object sender, SidePlateTypeChangedEventArgs e);
        new public event SidePlateTypeChangedEventHandler PlateTypeChanged;

        public class SidePlateTypeChangedEventArgs : PlateTypeChangedEventArgs
        {
            public int SideNumber { get; set; }

            public SidePlateTypeChangedEventArgs( int side_number, int rack_number, PlateTypeT rack_type)
                : base( rack_number, rack_type)
            {
                SideNumber = side_number;
            }
        }

        public SideRackView( int side_number, int rack_number, IEnumerable<int> slot_numbers, PlateTypeT plate_type)
            : base( rack_number, slot_numbers, plate_type)
        {
            SideNumber = side_number;
        }

        protected override void PlateTypeView_CurrentChanged(object sender, EventArgs e)
        {
            // set the rack's type here.  Needs to be serialized in the plugin that uses this control
            ICollectionView view = sender as ICollectionView;
            if( view == null || view.CurrentItem == null)
                return;

            var selected_type = (PlateTypeT)view.CurrentItem;
            // notify anyone that has registered for plate type changes
            if( PlateTypeChanged != null)
                PlateTypeChanged( this, new SidePlateTypeChangedEventArgs( SideNumber, RackNumber, selected_type));
        }
    }
}
