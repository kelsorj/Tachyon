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
using UserControlPlate96;
using BioNex.Shared.HitpickXMLWriter;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.Utils;
using BioNex.HitpickListCreatorGUI;

namespace BioNex.HitpickListCreatorGUI
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private class Transfers : List<KeyValuePair<UserControlPlate96.Well,List<UserControlPlate96.Well>>>
        {
            private uint counter = 0;

            public void AddTransfer( UserControlPlate96.Well from, UserControlPlate96.Well to, bool ctrl_down)
            {
                from.Mark( from.Properties.MyPlate, String.Format( "{0}", counter + 1), ref to);
                to.Mark( from.Properties.MyPlate, String.Format( "{0}", counter + 1), ref from);
                // check to see if the current well exists in the list of transfers.  If it does,
                // then add the dest well to it.
                KeyValuePair<UserControlPlate96.Well,List<UserControlPlate96.Well>> kvp = Find( x => (x.Key == from));
                if( kvp.Value == null) {
                    List<UserControlPlate96.Well> dest_wells = new List<UserControlPlate96.Well>();
                    dest_wells.Add( to);
                    Add( new KeyValuePair<UserControlPlate96.Well, List<UserControlPlate96.Well>>( from, dest_wells));
                } else {
                    kvp.Value.Add( to);
                }
                if( !ctrl_down)
                    from = null;
                to = null;
                counter++;
            }
        }

        public List<UserControlPlate96.Well> GetOtherWells( UserControlPlate96.Well well)
        {
            List<UserControlPlate96.Well> wells = new List<UserControlPlate96.Well>();
            foreach( KeyValuePair<UserControlPlate96.Well,List<UserControlPlate96.Well>> w in transfers) {
                // if we clicked on a source well, we need to add it, plus all
                // of the destination wells associated with it
                if( w.Key == well) {
                    wells.Add( w.Key);
                    foreach( UserControlPlate96.Well temp_well in w.Value)
                        wells.Add( temp_well);
                } else {
                    // otherwise, we need to look through all of the destination transfers,
                    // and if it matches, we want to add this one well and its source
                    foreach( UserControlPlate96.Well temp_well in w.Value) {
                        if( temp_well == well) {
                            wells.Add( w.Key);
                            wells.Add( temp_well);
                        }
                    }
                }
            }
            return wells;
        }

        private Transfers transfers = new Transfers();
        
        public Window1()
        {
            InitializeComponent();
            stackpanel_source.Margin = new Thickness( 3);
            stackpanel_dest.Margin = new Thickness( 3);
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            stackpanel_source.Children.Add( CreateAddSourceButton());
            stackpanel_dest.Children.Add( CreateAddDestButton());
        }

        /// <summary>
        /// adds a plate to the source or dest side, as well as deals with
        /// the creation of a stackpanel that houses the delete button and
        /// any other buttons we might want to add later.
        /// </summary>
        /// <param name="panel"></param>
        /// <param name="plate_name"></param>
        private void AddPlate( StackPanel panel, string plate_name)
        {
            // figure out how many children are in the source list -- the button is always the last
            // create the plate
            int num_items = panel.Children.Count;
            UserControlPlate96.UserControl1 plate = new UserControlPlate96.UserControl1();
            plate.PlateName = String.Format( "{0}{1}", plate_name, num_items);
            plate.DeleteButtonClick += new RoutedEventHandler(plate_DeleteButtonClick);
            plate.WellClearClick += new RoutedEventHandler(plate_WellClearClick);
            // insert plate into panel
            panel.Children.Insert( num_items - 1, plate);
        }

        void plate_WellClearClick(object sender, RoutedEventArgs e)
        {
            UserControlPlate96.Well well = e.OriginalSource as UserControlPlate96.Well;
            if( well == null)
                return;
            UserControlPlate96.UserControl1 plate = sender as UserControlPlate96.UserControl1;
            if( plate == null)
                return;
            MessageBox.Show( String.Format( "deleting well {0} in plate named {1}", "adsf", "asdf"));
        }

        private void plate_DeleteButtonClick(object sender, RoutedEventArgs e)
        {
            UserControlPlate96.UserControl1 plate = sender as UserControlPlate96.UserControl1;
            if( sender == null)
                return;
            string plate_name = ((UserControlPlate96.UserControl1)sender).PlateName;
            //! \todo this is lame -- I have to look for "source" or "dest" to determin
            //!       which StackPanel we're supposed to manipulate
            StackPanel panel = plate_name.Contains( "source") ? stackpanel_source : stackpanel_dest;
            // delete all transfers from this plate
            DeleteTransfersUsingPlate( plate);
            // delete the plate from the GUI
            panel.Children.Remove( plate);
        }

        private void DeleteTransfersUsingPlate( UserControlPlate96.UserControl1 plate)
        {
            Transfers transfers_to_delete = new Transfers();
            foreach( KeyValuePair<UserControlPlate96.Well,List<UserControlPlate96.Well>> kvp in transfers) {
                UserControlPlate96.Well source_well = kvp.Key;
                List<UserControlPlate96.Well> dest_wells = kvp.Value;
                //! \todo isn't it lame that I'm comparing the names instead of the object refs?
                // delete the source well
                if( source_well.Properties.MyPlate.PlateName == plate.PlateName) {
                    source_well.Unmark();
                    transfers_to_delete.Add( kvp);
                }
                // check the first dest plate in the list.  if its plate name matches, then
                // also delete the kvp.  We can make this assumption because we won't let
                // the user do a one-to-many transfer between different destination plates
                if( dest_wells[0].Properties.MyPlate.PlateName == plate.PlateName) {
                    foreach( UserControlPlate96.Well temp_well in dest_wells)
                        temp_well.Unmark();
                    transfers_to_delete.Add( kvp);
                }
            }
            transfers.RemoveAll( (x)=>transfers_to_delete.Contains(x));
        }

        private void AddSourcePlate( object o, RoutedEventArgs e)
        {
            AddPlate( stackpanel_source, "source");
        }

        private Button CreateAddSourceButton()
        {
            Button b = new Button();
            b.Content = "Create New Source Plate";
            b.Click += new RoutedEventHandler( AddSourcePlate);
            return b;
        }

        private void AddDestPlate( object o, RoutedEventArgs e)
        {
            AddPlate( stackpanel_dest, "dest");
        }

        private Button CreateAddDestButton()
        {
            Button b = new Button();
            b.Content = "Create New Dest Plate";
            b.Click += new RoutedEventHandler( AddDestPlate);
            return b;
        }

        private UserControlPlate96.Well AspirateWell = null;
        private UserControlPlate96.Well DispenseWell = null;
        
        private void Window_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // bail if we didn't click on a well
            UserControlPlate96.UserControl1 plate = e.Source as UserControlPlate96.UserControl1;
            UserControlPlate96.Well well = e.OriginalSource as UserControlPlate96.Well;
            if( plate != null && well != null && AspirateWell != null) {
                DispenseWell = well;
                DispenseWell.Properties.MyPlate = plate.plate;
                // don't allow transfers from / to the same well
                if( AspirateWell != DispenseWell) {
                    // check to see if the left CTRL key is pressed -- if it is, we
                    // don't want to null out the AspirateWell so that we can support
                    // one-to-many transfers
                    bool ctrl_down = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
                    transfers.AddTransfer( AspirateWell, DispenseWell, ctrl_down);
                    // log to verify that transfers are being maintained properly
                    log.AppendText( "*** All transfers ***\n");
                    for( int i=0; i<transfers.Count; i++) {
                        KeyValuePair<UserControlPlate96.Well,List<UserControlPlate96.Well>> kvp = transfers[i];
                        /*
                        log.AppendText( String.Format( "{0}, barcode {1}, well index {2} -> {3}, barcode {4}, well index {5}\n",
                            kvp.Key.Properties.MyPlate.PlateName, kvp.Key.Properties.MyPlate.Barcode,
                            kvp.Key.Properties.Index, kvp.Value.Properties.MyPlate.PlateName,
                            kvp.Value.Properties.MyPlate.Barcode, kvp.Value.Properties.Index));                    
                        */
                    }
                } else if( AspirateWell != null) {
                    DispenseWell = well;
                    // here, this means we clicked on a well
                    // need to see if this well is part of a transfer or not
                    List<UserControlPlate96.Well> wells = GetOtherWells( DispenseWell);
                    foreach( UserControlPlate96.Well w in wells)
                        w.Animate();
                }
            } 
        }

        private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // if we're doing a one-to-many transfer, bail out since we already have
            // the AspirateWell selected
            if( (Keyboard.Modifiers & ModifierKeys.Control) != 0)
                return;
            // bail if we didn't click on a well
            UserControlPlate96.UserControl1 plate = e.Source as UserControlPlate96.UserControl1;
            UserControlPlate96.Well well = e.OriginalSource as UserControlPlate96.Well;
            if( plate != null && well != null) {
                AspirateWell = well;
                AspirateWell.Properties.MyPlate = plate.plate;
            }
        }

        private TransferOverview ConvertTransfersToTransferOverview( Transfers gui_transfers)
        {
            TransferOverview to = new TransferOverview();
            // loop over the transfers and:
            // 1. add all unique source and destination barcodes
            // 2. get labware name
            // 3. add the transfer
            PlateCatalog sources = new PlateCatalog();
            PlateCatalog destinations = new PlateCatalog();
            List<Transfer> transfers = new List<Transfer>();
            foreach( KeyValuePair<UserControlPlate96.Well,List<UserControlPlate96.Well>> kvp in gui_transfers) {
                UserControlPlate96.Well source_well = kvp.Key;
                //! \todo fix for 384 well plates
                string source_well_name = Wells.IndexToWellName( source_well.Properties.Index, 96);
                string source_barcode = source_well.Properties.MyPlate.Barcode;
                string source_labware = source_well.Properties.MyPlate.Labware;
                BioNex.Shared.PlateDefs.SourcePlate source_plate = new BioNex.Shared.PlateDefs.SourcePlate( source_labware, source_barcode);
                sources.Add( source_plate);
                List<UserControlPlate96.Well> dest_wells = kvp.Value;
                string dest_barcode = dest_wells[0].Properties.MyPlate.Barcode;
                string dest_labware = dest_wells[0].Properties.MyPlate.Labware;
                DestinationPlate dest_plate = new BioNex.Shared.PlateDefs.DestinationPlate( dest_labware, dest_barcode, "any");
                destinations.Add( dest_plate);
                List<string> dest_wellnames = new List<string>();
                foreach( UserControlPlate96.Well w in dest_wells)
                    dest_wellnames.Add( Wells.IndexToWellName( w.Properties.Index, 96));
                double transfer_volume = double.Parse( text_volume.Text);
                Transfer t = new Transfer( source_plate, source_well_name, transfer_volume, VolumeUnits.ul, 0, VolumeUnits.ul, "", dest_plate, dest_wellnames, null, null);
                transfers.Add( t);
            }
            to.SourcePlates = sources;
            to.DestinationPlates = destinations;
            to.Transfers = transfers;
            return to;
        }

        private void button_write_Click(object sender, RoutedEventArgs e)
        {
            TransferOverview to = ConvertTransfersToTransferOverview( transfers);
            Writer.Write( to, text_filepath.Text);
        }

        private void button_select_file_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".txt";
            dlg.InitialDirectory = "c:\\";
            if( dlg.ShowDialog() != null)
                text_filepath.Text = dlg.FileName;
        }
    }
}
