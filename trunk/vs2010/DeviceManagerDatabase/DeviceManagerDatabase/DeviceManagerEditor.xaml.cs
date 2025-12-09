using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using DeviceManagerDatabase;
using GalaSoft.MvvmLight.Command;

namespace BioNex.SynapsisPrototype
{
    /// <summary>
    /// Interaction logic for DeviceManagerPanel.xaml
    /// </summary>
    
    public partial class DeviceManagerEditor : Window
    {
        private class DeviceProperty
        {
            public string PropertyName { get; set; }
            public string PropertyValue { get; set; }

            public DeviceProperty( string name, string value)
            {
                PropertyName = name;
                PropertyValue = value;
            }
        }

        private class DeviceSelection
        {
            public DeviceInfo Device { get; set; }
            public string CellHeader { get; set; }
        }

        private class PropertySelection
        {
            public DeviceProperty Property { get; set; }
            public string CellHeader { get; set; }
        }

        private DeviceManager _device_manager { get; set; }
        public ICollectionView Devices { get; private set; }
        public ICollectionView Properties { get; private set; }
        /// <summary>
        /// this ObservableCollection is used to hold the data from the properties list so that the View can reference it upon construction time.
        /// </summary>
        private readonly ObservableCollection<DeviceProperty> _properties = new ObservableCollection<DeviceProperty>();

        // device
        private const string _device_name_header = "Device Name";
        private const string _disable_header = "Disabled";
        private const string _product_name_header = "Product Name";
        private const string _manufacturer = "Manufacturer";
        // properties
        private const string _property_name = "Property Name";
        private const string _property_value = "Property Value";
        private string _last_clicked_cell_header;

        public string DeviceNameHeader { get { return _device_name_header; } }
        public string DisableHeader { get { return _disable_header; } }
        public string ProductNameHeader { get { return _product_name_header; } }
        public string ManufacturerNameHeader { get { return _manufacturer; } }

        private DeviceSelection _device_selection { get; set; }
        private PropertySelection _property_selection { get; set; }

        public RelayCommand AddNewDeviceCommand { get; set; }
        public RelayCommand DeleteDeviceCommand { get; set; }
        public RelayCommand AddPropertyCommand { get; set; }
        public RelayCommand DeletePropertyCommand { get; set; }

        public DeviceManagerEditor( DeviceManager device_manager)
        {
            _device_manager = device_manager;
            Devices = CollectionViewSource.GetDefaultView( _device_manager.db.GetAllDeviceInfo());
            Properties = CollectionViewSource.GetDefaultView( _properties);

            AddNewDeviceCommand = new RelayCommand( ExecuteAddNewDevice);
            DeleteDeviceCommand = new RelayCommand( ExecuteDeleteDevice, CanExecuteDeleteDevice);
            AddPropertyCommand = new RelayCommand( ExecuteAddProperty, CanExecuteAddProperty);
            DeletePropertyCommand = new RelayCommand( ExecuteDeleteProperty, CanExecuteDeleteProperty);

            InitializeComponent();
            DataContext = this;
        }

        private void ExecuteAddNewDevice()
        {
            AddNewDevice dlg = new AddNewDevice( _device_manager);
            dlg.ShowDialog();
            // make sure the view updates -- again, I think this is not the optimal way to do it
            Devices = CollectionViewSource.GetDefaultView( _device_manager.db.GetAllDeviceInfo());
        }

        private void ExecuteDeleteDevice()
        {

            if( _device_selection == null || _device_selection.Device == null)
                return;
            if( MessageBox.Show( String.Format( "Are you sure you want to delete the device '{0}'?", _device_selection.Device.InstanceName), "Confirm delete", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;

            // DKM 2011-10-03 this sure seems like a hack, but I can't think of any other way to fix the problem.  If I don't
            //                temporarily disable the databinding, when I handle datagrid events for deleting devices, the
            //                info for the deleted device wants to be updated, but it was deleted by the database already!
            DataContext = null;
            _device_manager.db.DeleteDevice( _device_selection.Device.CompanyName, _device_selection.Device.ProductName, _device_selection.Device.InstanceName);
            DataContext = this;

            // IMO this is the wrong way to deal with this.
            Devices = CollectionViewSource.GetDefaultView( _device_manager.db.GetAllDeviceInfo());
            // DKM 2012-04-11 prevent the user from trying to then delete properties from a device that was just deleted
            UpdatePropertiesView();
        }

        private bool CanExecuteDeleteDevice()
        {
            return _device_selection != null;
        }

        private void ExecuteAddProperty()
        {
            if( _device_selection == null || _device_selection.Device == null)
                return;

            DeviceInfo di = _device_selection.Device;

            try {
                AddPropertyDialog dlg = new AddPropertyDialog( _device_manager, di.CompanyName, di.ProductName, di.InstanceName);
                dlg.ShowDialog();

                // update the properties for the selected device
                UpdatePropertiesView();
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private bool CanExecuteAddProperty()
        {
            return _device_selection != null && _device_selection.Device != null;
        }

        private void ExecuteDeleteProperty()
        {
            if( _property_selection == null)
                return;

            if( MessageBox.Show( String.Format( "Are you sure you want to delete the property '{0}'?", _property_selection.Property.PropertyName), "Confirm delete", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;

            try {
                _device_manager.db.DeleteDeviceProperty( _device_selection.Device.CompanyName,
                                                          _device_selection.Device.ProductName,
                                                          _device_selection.Device.InstanceName,
                                                          _property_selection.Property.PropertyName);
                // IMO this is the wrong way to deal with this.
                ObservableCollection<DeviceInfo> device_info = _device_manager.db.GetAllDeviceInfo();
                Devices = CollectionViewSource.GetDefaultView( device_info);
                UpdatePropertiesView();
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private bool CanExecuteDeleteProperty()
        {
            return _property_selection != null;
        }

        /// <summary>
        /// called when a new cell is selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeviceDataGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            Debug.WriteLine( "CurrentCellChanged");
        }

        /// <summary>
        /// called when a new row in the datagrid is selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeviceDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine( "SelectionChanged");

            try {
                if( (sender as DataGrid).CurrentItem == null)
                    return;
                _device_selection = new DeviceSelection { Device = (sender as DataGrid).CurrentItem as DeviceInfo, CellHeader = (sender as DataGrid).CurrentCell.Column.Header.ToString() };
                if( _device_selection == null)
                    return;
                        
                UpdatePropertiesView();
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private void UpdatePropertiesView()
        {
            DeviceInfo di = _device_selection.Device;

            // DKM 2012-04-12 user could have deleted the device, so catch the exception and then result empty results
            try {
                _properties.Clear();
                // get the properties for the selected device FROM THE DATABASE
                var property_map = from kvp in _device_manager.db.GetProperties( di.CompanyName, di.ProductName, di.InstanceName) select new DeviceProperty( kvp.Key, kvp.Value);
                foreach( var property in property_map)
                    _properties.Add( property);
            } catch( Exception) {
                // do nothing, just leave _properties empty
            }
            Properties = CollectionViewSource.GetDefaultView( _properties);
        }

        /// <summary>
        /// called when a cell is entering edit mode, before PreparingCellForEdit
        /// not sure why this is needed for us, since the old value is still in the device_info object at edit completion time
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeviceDataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            Debug.WriteLine( "BeginningEdit");
        }

        /// <summary>
        /// called when cell editing is complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeviceDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try {
                Debug.WriteLine( "CellEditEnding");

                // I don't really like this, and I need to think about another way to implement it
                switch( _device_selection.CellHeader) {
                    case _device_name_header:
                        _device_manager.db.RenameDevice( _device_selection.Device.CompanyName, _device_selection.Device.ProductName,
                                                          _device_selection.Device.InstanceName, (e.EditingElement as TextBox).Text);
                        break;
                    case _disable_header:
                        _device_manager.db.DisableDevice( _device_selection.Device.CompanyName, _device_selection.Device.ProductName,
                                                           _device_selection.Device.InstanceName, (e.EditingElement as CheckBox).IsChecked.Value);
                        break;
                }
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        /// <summary>
        /// called when a cell is entering edit mode, after BeginningEdit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeviceDataGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            Debug.WriteLine( "PreparingCellForEdit");
        }

        private void PropertiesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try {
                if( (sender as DataGrid).CurrentCell.Column == null)
                    return;
                _property_selection = new PropertySelection { Property = (sender as DataGrid).CurrentItem as DeviceProperty, CellHeader = (sender as DataGrid).CurrentCell.Column.Header.ToString() };
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private void PropertiesDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try {
                switch( _last_clicked_cell_header) {
                    case _property_name:
                        _device_manager.db.RenameDeviceProperty( _device_selection.Device.CompanyName,
                                                                  _device_selection.Device.ProductName,
                                                                  _device_selection.Device.InstanceName,
                                                                  _property_selection.Property.PropertyName,
                                                                  (e.EditingElement as TextBox).Text);
                        break;
                    case _property_value:
                        _device_manager.db.UpdateDeviceProperty( _device_selection.Device.CompanyName,
                                                                  _device_selection.Device.ProductName,
                                                                  _device_selection.Device.InstanceName,
                                                                  _property_selection.Property.PropertyName,
                                                                  (e.EditingElement as TextBox).Text);
                        break;
                }
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private void DeviceDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if( e.Key != Key.Delete)
                return;
            e.Handled = true;
            ExecuteDeleteDevice();
        }

        private void PropertiesDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if( e.Key != Key.Delete)
                return;
            e.Handled = true;
            ExecuteDeleteProperty();
        }

        private void PropertiesDataGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            var datagrid = sender as DataGrid;
            if( datagrid == null)
                return;
            // DKM 2012-04-18 when closing, avoid issue accessing invalid cells
            if( datagrid.CurrentCell != null && datagrid.CurrentCell.Column != null && datagrid.CurrentCell.Column.Header != null) {
                _last_clicked_cell_header = datagrid.CurrentCell.Column.Header.ToString();
            }
        }
    }
}
