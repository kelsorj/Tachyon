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
using System.ComponentModel;
using BioNex.Shared.HitpickXMLWriter;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.LabwareDatabase;

namespace BioNex.BumblebeeRandomHitpickGenerator
{
    public class CreateHitpickFileCommand : ICommand
    {
        private Window1 _vm;

        public CreateHitpickFileCommand( Window1 vm)
        {
            _vm = vm;
        }

        #region ICommand Members

        bool ICommand.CanExecute(object parameter)
        {
            return true;
        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        void ICommand.Execute(object parameter)
        {
            _vm.CreateHitpickFileHandler();
        }

        #endregion
    }

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window, INotifyPropertyChanged
    {
        public int NumberOfSourcePlates { get; set; }
        public PlateType SourcePlateType { get; set; }
        public int Source96Min { get; set; }
        public int Source96Max { get; set; }
        public int Source384Min { get; set; }
        public int Source384Max { get; set; }
        public int NumberOfDestPlates { get; set; }
        public string DestPlateOrdering { get; set; }
        public PlateType DestPlateType { get; set; }
        public bool OneToMany { get; set; }
        public double Volume { get; set; }
        public bool AnyDest { get; set; }
        
        public ICommand CreateHitpickFileCommand { get; set; }

        private ILabwareDatabase _labware_database { get; set; }

        public Window1()
        {
            InitializeComponent();
            CreateHitpickFileCommand = new CreateHitpickFileCommand( this);

            NumberOfSourcePlates = 16;
            Source96Min = 5;
            Source96Max = 30;
            Source384Min = 10;
            Source384Max = 20;
            NumberOfDestPlates = 1;
            SourcePlateType = PlateType.Plate96;
            DestPlateType = PlateType.Plate384;
            DestPlateOrdering = "Random";
            Volume = 2;

            _labware_database = new LabwareDatabase( "labware.s3db");

            this.DataContext = this;
        }

        public void CreateHitpickFileHandler()
        {
            string hitpick_range;
            switch( SourcePlateType) {
                case PlateType.Plate96:
                    hitpick_range = String.Format( "[{0},{1}]", Source96Min, Source96Max);
                    break;
                case PlateType.Plate384:
                    hitpick_range = String.Format( "[{0},{1}]", Source384Min, Source384Max);
                    break;
                case PlateType.Random:
                    hitpick_range = String.Format( "[{0},{1}] for 96 well plates and [{2},{3}] for 384 well plates", Source96Min, Source96Max, Source384Min, Source384Max);
                    break;
                default:
                    hitpick_range = "!!undefined!!";
                    break;
            }
            string ordering = DestPlateOrdering == "Row" ? "by row" : (DestPlateOrdering == "Column" ? "by column" : "randomly");
            string summary = String.Format( "You want to pick from ({0}) {1} well source plates, with a hitpick number between {2} and transfer into ({3}) {4} well dest plate, ordered {5}, and {6}.  Is this correct?", NumberOfSourcePlates, SourcePlateType, hitpick_range, NumberOfDestPlates, DestPlateType, ordering, OneToMany ? "one-to-many" : "one-to-one");
            /*
            MessageBoxResult result = MessageBox.Show( summary, "Confirm selections", MessageBoxButton.YesNo);
            if( result == MessageBoxResult.No)
                return;
             */
            // prompt for output file
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "Hitpick XML File (*.xml)|*.xml";
            if( dlg.ShowDialog() == false) {
                return;
            }
            // create hitpick file
            string filepath = dlg.FileName;
            CreateHitpickFile( filepath);
            // tell user we're done
            MessageBox.Show( String.Format( "Created hitpick file '{0}' successfully", filepath));
        }

        private class SourceTransferInfo
        {
            public string Barcode { get; set; }
            public string Wellname { get; set; }
            public string Labware { get; set; }
        }

        private class DestinationTransferInfo
        {
            public string Barcode { get; set; }
            public string Wellname { get; set; }
            public string Labware { get; set; }
            public double Volume { get; set; }
        }

        private void CreateHitpickFile( string filepath)
        {
            // create source barcodes
            List<string> source_barcodes = new List<string>();
            for( int i=0; i<NumberOfSourcePlates; i++)
                source_barcodes.Add( String.Format( "s{0}", i + 1));
            // this is used to keep track of all of the data that goes into the hitpick file
            TransferOverview transfer_overview = new TransferOverview();
            // create transfers for all of the sources, based on user's preferences
            List<SourceTransferInfo> source_transfer_info = new List<SourceTransferInfo>();
            foreach( string source_barcode in source_barcodes) {
                string labware_name;
                List<string> wells = RandomFunctions.GetRandomWells( SourcePlateType, out labware_name, Source96Min, Source96Max, Source384Min, Source384Max);
                // add each of the transfers to the transfer list
                foreach( string well in wells) {
                    source_transfer_info.Add( new SourceTransferInfo { Barcode = source_barcode, Labware = labware_name, Wellname = well });
                }
                Labware labware = _labware_database.GetLabware( labware_name);
                transfer_overview.SourcePlates.Add( new SourcePlate( labware, source_barcode));
            }
            // find out how many source transfers there are
            int total_source_transfers = source_transfer_info.Count;            
            // create transfers for all of the destinations
            // given the types of plates desired for the destination plates, figure out if there 
            // are enough wells
            List<PlateType> dest_plates = new List<PlateType>();
            if( DestPlateType == PlateType.Random)
                dest_plates = RandomFunctions.GetRandomPlateTypes(NumberOfDestPlates);
            else {
                for( int i=0; i<NumberOfDestPlates; i++)
                    dest_plates.Add( DestPlateType);
            }
            RandomFunctions.AddDestPlatesIfNecessary( dest_plates, total_source_transfers);
            // OK, so at this point, we've got our randomized source wells, and we've got a list of destination plate
            // TYPES that we want to transfer to.  So now, we just have to loop over all of the source transfers,
            // and while we do that, assign the destination wells.
            // compile the dest wells into their own list
            List<DestinationTransferInfo> dest_transfer_info = new List<DestinationTransferInfo>();
            int dest_counter = 0; // only used in the loop to create the barcode name
            foreach( PlateType type in dest_plates) {
                int num_wells = RandomFunctions.GetNumberOfWellsFromPlateType( type);
                string labware = RandomFunctions.GetLabwareNameFromPlateType( type);
                string barcode = String.Format( "d{0}", ++dest_counter);
                for( int i=0; i<num_wells; i++) {
                    string wellname = BioNex.Shared.Utils.Wells.IndexToWellName( i, num_wells);
                    if( AnyDest) {
                        dest_transfer_info.Add( new DestinationTransferInfo { Barcode = barcode, Labware = labware, Volume = this.Volume, Wellname = "any" });
                    } else {
                        dest_transfer_info.Add( new DestinationTransferInfo { Barcode = barcode, Labware = labware, Volume = this.Volume, Wellname = wellname });
                    }
                }
                Labware lw = _labware_database.GetLabware( labware);
                transfer_overview.DestinationPlates.Add( new DestinationPlate( lw, barcode, "any"));
            }
            // randomize sources
            List<SourceTransferInfo> randomized_source_transfer_info = new List<SourceTransferInfo>( source_transfer_info);
            randomized_source_transfer_info.Shuffle();
            // assign transfers between sources and dest
            for( int i=0; i<randomized_source_transfer_info.Count; i++) {
                SourceTransferInfo sti = source_transfer_info[i];
                DestinationTransferInfo dti = dest_transfer_info[i];
                Labware slw = _labware_database.GetLabware( sti.Labware);
                Labware dlw = _labware_database.GetLabware( dti.Labware);
                SourcePlate source_plate = new SourcePlate( slw, sti.Barcode);
                DestinationPlate dest_plate = new DestinationPlate( dlw, dti.Barcode, "any");
                transfer_overview.AddTransfer( source_plate, sti.Wellname, dest_plate, dti.Wellname, dti.Volume, VolumeUnits.ul, "", "", "");
            }
            BioNex.Shared.HitpickXMLWriter.Writer.Write( transfer_overview, filepath);
            // finally, write the master hitpick file that will take full 96 well plates and
            // create the random 96 and 384 well source plates
            CreateMasterHitpickFile( source_transfer_info, filepath + ".create.xml");
        }

        private class MasterWellTracker
        {
            List<int> _tracker;
            int _plate_index;
            PlateType _plate_type;
            int _max_well_usage;
            SourcePlate _source_plate;
            Random _rand = new Random( DateTime.Now.Second * DateTime.Now.Minute + DateTime.Now.Hour);

            private ILabwareDatabase _labware_database { get; set; }

            public MasterWellTracker( PlateType plate_type, int max_well_usage, ILabwareDatabase labware_database)
            {
                _plate_index = 0;
                _plate_type = plate_type;
                _max_well_usage = max_well_usage;
                _labware_database = labware_database;
                NextPlate();
            }

            public void GetAvailableSourcePlateAndWell( out SourcePlate source_plate, out string available_wellname)
            {
                // first, we should see if there are even any wells available by
                // looking over the tracker values
                List<int> wells_remaining = _tracker.FindAll( i => _tracker[i] > 0);
                if( wells_remaining.Count == 0)
                    NextPlate();
                
                int well_index;
                do {
                    well_index = _rand.Next( 0, _tracker.Count - 1);
                } while( _tracker[well_index] == 0);
                _tracker[well_index]--;
                source_plate = _source_plate;
                int num_wells = RandomFunctions.GetNumberOfWellsFromPlateType( _plate_type);
                available_wellname = BioNex.Shared.Utils.Wells.IndexToWellName( well_index, num_wells);
            }

            private void NextPlate()
            {
                _plate_index++;
                int num_wells = RandomFunctions.GetNumberOfWellsFromPlateType( _plate_type);
                _tracker = new List<int>( num_wells);
                for( int i=0; i<num_wells; i++) {
                    _tracker.Add( _max_well_usage);
                }
                string barcode = String.Format( "m{0}", _plate_index);
                Labware labware = _labware_database.GetLabware( RandomFunctions.GetLabwareNameFromPlateType( _plate_type));
                _source_plate = new SourcePlate( labware, barcode);
            }
        }

        private void CreateMasterHitpickFile( List<SourceTransferInfo> output, string filepath)
        {
            TransferOverview transfer_overview = new TransferOverview();
            // count the number of sources
            int num_transfers = output.Count;
            PlateType master_plate_type = PlateType.Plate96;
            int transfers_per_master_well = 3;
            MasterWellTracker tracker = new MasterWellTracker( master_plate_type, transfers_per_master_well, _labware_database);
            // create the initial destination plate
            foreach( SourceTransferInfo dti in output) {
                SourcePlate source_plate;
                string source_wellname;
                tracker.GetAvailableSourcePlateAndWell( out source_plate, out source_wellname);    
                // adding the same plate over and over again is okay, it's just ignored
                transfer_overview.SourcePlates.Add( source_plate);
                // check to see if the output plate changed at all
                Labware labware = _labware_database.GetLabware( dti.Labware);
                DestinationPlate dest_plate = new DestinationPlate( labware, dti.Barcode, "any");
                transfer_overview.DestinationPlates.Add( dest_plate);
                transfer_overview.AddTransfer( source_plate, source_wellname, dest_plate, dti.Wellname, Volume, VolumeUnits.ul, "", "", "");
            }
            
            BioNex.Shared.HitpickXMLWriter.Writer.Write( transfer_overview, filepath);
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for
        /// a given property.
        /// </summary>
        /// <param name="propertyName">The name of the changed property.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
