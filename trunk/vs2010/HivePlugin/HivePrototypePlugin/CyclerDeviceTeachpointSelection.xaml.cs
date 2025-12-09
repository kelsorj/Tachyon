using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using BioNex.Shared.Utils;
using GalaSoft.MvvmLight.Command;

namespace BioNex.HivePrototypePlugin
{
    /// <summary>
    /// Interaction logic for CyclerDeviceTeachpointSelection.xaml
    /// </summary>
    public partial class CyclerDeviceTeachpointSelection : UserControl, INotifyPropertyChanged
    {
        public RelayCommand DeleteDeviceTeachpointsCommand { get; set; }
        public RelayCommand SelectAllTeachpointsCommand { get; set; }

        private readonly Action<CyclerDeviceTeachpointSelection> DeleteCallback;
        private readonly Func<string,IList<string>> TeachpointCallback;

        public ICollectionView DeviceNames { get; set; }
        public ICollectionView TeachpointNames { get; set; }

        private List<SelectableString> _location_names;

        private bool _enabled;
        public bool Enabled
        {
            get { return _enabled; }
            set {
                _enabled = value;
                OnPropertyChanged( "Enabled");
            }
        }

        public CyclerDeviceTeachpointSelection( int index, IList< string> device_names, Func< string, IList< string>> teachpoint_callback, Action< CyclerDeviceTeachpointSelection> delete_callback)
        {
            InitializeComponent();
            DataContext = this;

            DeleteDeviceTeachpointsCommand = new RelayCommand( ExecuteDeleteDeviceTeachpoints);
            SelectAllTeachpointsCommand = new RelayCommand( ExecuteSelectAllTeachpoints);

            DeleteCallback = delete_callback;

            DeviceNames = CollectionViewSource.GetDefaultView( device_names);
            DeviceNames.CurrentChanged += new EventHandler(DeviceName_CurrentChanged);
            DeviceNames.MoveCurrentToFirst();

            TeachpointCallback = teachpoint_callback;

            Enabled = true;
        }

        void DeviceName_CurrentChanged(object sender, EventArgs e)
        {
            IList<string> temp_names = TeachpointCallback( DeviceNames.CurrentItem.ToString());
            _location_names = (from x in temp_names select new SelectableString { Value = x }).ToList();
            TeachpointNames = CollectionViewSource.GetDefaultView( _location_names);
            OnPropertyChanged( "TeachpointNames");
        }

        private void ExecuteDeleteDeviceTeachpoints()
        {
            DeleteCallback( this);
        }

        private void ExecuteSelectAllTeachpoints()
        {
            foreach( var x in _location_names)
                x.IsSelected = true;
        }

        public List<string> GetSelectedLocations()
        {
            return (from x in _location_names where x.IsSelected select x.Value).ToList();
        }

        public string GetSelectedDevice()
        {
            return DeviceNames.CurrentItem.ToString();
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
}
