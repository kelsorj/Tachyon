using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using GalaSoft.MvvmLight.Command;
using log4net;

namespace BioNex.Shared.LabwareDatabase
{
    /// <summary>
    /// Interaction logic for LabwareEditor.xaml
    /// </summary>
    [Export( typeof( LabwareEditor))]
    public partial class LabwareEditor : Window, INotifyPropertyChanged
    {
        private class PropertySelection
        {
            public string LabwareName { get; set; }
            public TempLabwareProperty LabwareProperty { get; set; }
            public string CellHeader { get; set; }
        }

        private ILabwareDatabase _labware_database { get; set; }

        public ICollectionView Labwares { get; set; }
        
        private static readonly ILog _log = LogManager.GetLogger( typeof( LabwareEditor));
        
        public ObservableCollection<DeviceInterface> Devices { get; set; }

        private ExternalDataRequesterInterface _data_request_interface { get; set; }
        private HashSet<int> AvailableDeviceIds { get; set; }

        // commands
        public RelayCommand CreateLabwareCopyCommand { get; set; }
        public RelayCommand CreateLidCommand { get; set; }
        public RelayCommand NewLabwareCommand { get; set; }
        public RelayCommand DeleteLabwareCommand { get; set; }
        public RelayCommand RenameLabwareCommand { get; set; }
        public RelayCommand ReloadLabwareCommand { get; set; }

        // datagrid stuff
        private ILabware _selected_labware;
        public ILabware SelectedLabware
        {
            get { return _selected_labware; }
            set {
                _selected_labware = value;
                UpdatePropertiesForLabware( _selected_labware);
            }
        }
        private ICollectionView _labware_properties;
        public ICollectionView LabwareProperties
        {
            get { return _labware_properties; }
            private set {
                _labware_properties = value;
                OnPropertyChanged( "LabwareProperties");
            }
        }

        private string _create_lid_tooltip;
        public string CreateLidToolTip
        {
            get { return _create_lid_tooltip; }
            private set {
                _create_lid_tooltip = value;
                OnPropertyChanged( "CreateLidToolTip");
            }
        }

        private string _delete_labware_tooltip;
        public string DeleteLabwareToolTip
        {
            get { return _delete_labware_tooltip; }
            set {
                _delete_labware_tooltip = value;
                OnPropertyChanged( "DeleteLabwareToolTip");
            }
        }

        private string _create_labware_copy_tooltip;
        public string CreateLabwareCopyToolTip
        {
            get { return _create_labware_copy_tooltip; }
            set {
                _create_labware_copy_tooltip = value;
                OnPropertyChanged( "CreateLabwareCopyToolTip");
            }
        }

        private string _labware_notes;
        public string LabwareNotes
        {
            get { return _labware_notes; }
            set {
                _labware_notes = value;
                OnPropertyChanged( "LabwareNotes");
                // write to the database
                Labware labware = _labware_database.GetLabware( SelectedLabware.Name) as Labware;
                _labware_database.UpdateLabwareNotes( labware, _labware_notes);
            }
        }

        public ICollectionView GroupedLabwarePropertiesByDevice { get; private set; }

        private PropertySelection _labware_selection { get; set; }

        private class TempLabwareProperty
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public string Device { get; set; }
        }

        public LabwareEditor( ILabwareDatabase labware_database, ExternalDataRequesterInterface data_request_interface)
        {
            InitializeComponent();
            Devices = new ObservableCollection<DeviceInterface>();
            AvailableDeviceIds = new HashSet<int> { 0 };
            _labware_database = labware_database;
            _data_request_interface = data_request_interface;

            //GroupedLabwarePropertiesByDevice = new ListCollectionView( labwares);
            //GroupedLabwarePropertiesByDevice.GroupDescriptions.Add( new PropertyGroupDescription( "Device"));

            InitializeCommands();
            DataContext = this;
        }

        public void Initialize()
        {
            UpdateLabwareFromDatabase();
            CacheDeviceInfoFromDeviceManager();
        }

        private void InitializeCommands()
        {
            CreateLabwareCopyCommand = new RelayCommand( ExecuteCreateLabwareCopyCommand, () =>
                {
                    if( SelectedLabware == null) {
                        CreateLabwareCopyToolTip = "Please select a labware first";
                        return false;
                    }
                    CreateLabwareCopyToolTip = "Creates a copy of the selected labware";
                    return true;
                }
            );
            CreateLidCommand = new RelayCommand( ExecuteCreateLid, () => 
                {
                    if( SelectedLabware == null) {
                        CreateLidToolTip = "Please select a labware first";
                        return false;
                    }
                    CreateLidToolTip = "Creates a lid labware and associates it with the selected plate labware";
                    return true;                
                }
            );
            NewLabwareCommand = new RelayCommand( ExecuteNewLabwareCommand);
            DeleteLabwareCommand = new RelayCommand( ExecuteDeleteLabwareCommand, () => 
                {
                    if( SelectedLabware == null) {
                        DeleteLabwareToolTip = "Please select a labware first";
                        return false;
                    }
                    DeleteLabwareToolTip = "Deletes the selected labware and its associated lid, if any";
                    return true;
                }
            );
            RenameLabwareCommand = new RelayCommand( ExecuteRenameLabwareCommand, () => { return SelectedLabware != null; } );
            ReloadLabwareCommand = new RelayCommand( ExecuteReloadLabwareCommand, () => { return SelectedLabware != null; } );
        }

        private void ExecuteCreateLabwareCopyCommand()
        {
            // make a copy of this labware, but prepend "Copy of " to the name
            _labware_database.CloneLabware( SelectedLabware, "Copy of " + SelectedLabware.Name);
            UpdateLabwareFromDatabase();
        }

        private void ExecuteNewLabwareCommand()
        {
            AddLabwareDialog dlg = new AddLabwareDialog( _labware_database);
            dlg.ShowDialog();
            UpdateLabwareFromDatabase();
        }

        private void ExecuteDeleteLabwareCommand()
        {
            _labware_database.DeleteLabware( SelectedLabware.Name);
            UpdateLabwareFromDatabase();
        }

        private void ExecuteReloadLabwareCommand()
        {
            UpdateLabwareFromDatabase();
        }

        private void ExecuteRenameLabwareCommand()
        {
            RenameLabwareDialog dlg = new RenameLabwareDialog( _labware_database, SelectedLabware.Name);
            dlg.ShowDialog();
            UpdateLabwareFromDatabase();
        }

        private void ExecuteCreateLid()
        {
            ILabware last_selection = SelectedLabware;
            _labware_database.AddLid( SelectedLabware);
            UpdateLabwareFromDatabase();
            Labwares.MoveCurrentTo( last_selection);
        }

        private void UpdateLabwareFromDatabase()
        {
            try {
                _labware_database.ReloadLabware();
                List<string> labware_names = _labware_database.GetLabwareNames();
                List<ILabware> labwares = (from x in labware_names select _labware_database.GetLabware( x)).ToList();
                Labwares = CollectionViewSource.GetDefaultView( labwares);
                OnPropertyChanged( "Labwares");
            } catch( Exception ex) {
                _log.InfoFormat( "Could not update labware from database: {0}", ex.Message);
            }
        }

        /// <summary>
        /// here we get the device names from the device manager, along with their device IDs, since
        /// the IDs are the same as the "module_id" used by the labware database to determine which
        /// device(s) a particular property is relevant to.
        /// 
        /// FYI, a module_id of 0 means that the property is essentially global and has no affinity
        /// for a particular device.
        /// </summary>
        private void CacheDeviceInfoFromDeviceManager()
        {
            IEnumerable<DeviceInterface> devices = _data_request_interface.GetDeviceInterfaces();
            foreach( DeviceInterface di in devices)
                AvailableDeviceIds.Add( _data_request_interface.GetDeviceTypeId( di.Manufacturer, di.ProductName));
        }

        private void UpdatePropertiesForLabware( ILabware labware)
        {
            if( labware == null) {
                // DKM 2012-04-20 refs #573 & #574 prevent properties from remaining onscreen after deleting/renaming a labware
                LabwareProperties = null;
                return;
            }

            // DKM pull new labware properties from database
            labware = _labware_database.GetLabware( labware.Name);

            List<TempLabwareProperty> labwares = new List<TempLabwareProperty>();
            
            // get the master list of labware properties first
            IEnumerable<ILabwareProperty> master_properties = _labware_database.GetLabwareProperties();
            // remove all of the properties from this list if the device (module_id) is not present in the AvailableDeviceIds HashSet
            master_properties = from x in master_properties
                                where AvailableDeviceIds.Contains( (int)x.ModuleId)
                                select x;
            // now only show these properties of the labware
            foreach (ILabwareProperty x in master_properties) {
                string property_name = x.Name;
                object value = labware[x.Name];
                labwares.Add( new TempLabwareProperty { Name=x.Name, Value=(value == null ? "" : value.ToString()), Device=x.ModuleId.ToString() });
            }

            // #414
            LabwareNotes = labware.Notes;

            LabwareProperties = CollectionViewSource.GetDefaultView( labwares);
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try {
                if( (sender as DataGrid).CurrentCell == null || (sender as DataGrid).CurrentCell.Column == null)
                    return;
                TempLabwareProperty labware_property = (sender as DataGrid).CurrentItem as TempLabwareProperty;
                string header = (sender as DataGrid).CurrentCell.Column.Header.ToString();
                _labware_selection = new PropertySelection { LabwareName = SelectedLabware.Name, LabwareProperty = labware_property, CellHeader = header };
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            /*
            string message = String.Format( "Changed property '{0}' from '{1}' to '{2}' for labware '{3}'",
                                            _labware_selection.LabwareProperty.Name, _labware_selection.LabwareProperty.Value,
                                            (e.EditingElement as TextBox).Text, _labware_selection.LabwareName);
            MessageBox.Show( message);
             */
            try {
                Labware labware = _labware_database.GetLabware( _labware_selection.LabwareName) as Labware;
                TextBox tb = e.EditingElement as TextBox;
                if( tb == null)
                    return;
                labware[_labware_selection.LabwareProperty.Name] = tb.Text;
                _labware_database.UpdateLabware( labware);
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }
    }
}
