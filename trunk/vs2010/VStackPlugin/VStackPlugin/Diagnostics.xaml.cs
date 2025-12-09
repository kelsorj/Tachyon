using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.PlateDefs;
using GalaSoft.MvvmLight.Command;

namespace VStackPlugin
{
    /// <summary>
    /// Interaction logic for Diagnostics.xaml
    /// </summary>
    public partial class Diagnostics : Window
    {
        private readonly VStackPlugin _plugin;
        public ICollectionView AvailableLabware { get; set; }

        public RelayCommand LoadStackCommand { get; set; }
        public RelayCommand ReleaseStackCommand { get; set; }
        public RelayCommand DownstackPlateCommand { get; set; }
        public RelayCommand UpstackPlateCommand { get; set; }

        public Diagnostics( VStackPlugin plugin)
        {
            _plugin = plugin;
            InitializeComponent();
            DataContext = this;

            // temporary to prevent crashing due to null pointer
            if( _plugin._labware_database != null) {
                var labware_names = _plugin._labware_database.GetLabwareNames();
                AvailableLabware = CollectionViewSource.GetDefaultView( labware_names);
                AvailableLabware.MoveCurrentToFirst();
            }

            Func<bool> is_connected_and_labware_valid = new Func<bool>( () => { return _plugin._labware_database != null &&  (_plugin as DeviceInterface).Connected; });

            LoadStackCommand = new RelayCommand( ExecuteLoadStack, is_connected_and_labware_valid);
            ReleaseStackCommand = new RelayCommand( ExecuteReleaseStack, is_connected_and_labware_valid);
            UpstackPlateCommand = new RelayCommand( ExecuteUpstackPlate, is_connected_and_labware_valid);
            DownstackPlateCommand = new RelayCommand( ExecuteDownstackPlate, is_connected_and_labware_valid);
        }

        private void ExecuteLoadStack()
        {
            // only need labware name in plate object that's passed to plugin method
            Plate plate = new DestinationPlate( _plugin._labware_database.GetLabware(AvailableLabware.CurrentItem.ToString()), "don't care", "any");
            _plugin.LoadStack( plate);
        }

        private void ExecuteReleaseStack()
        {
            // only need labware name in plate object that's passed to plugin method
            Plate plate = new DestinationPlate( _plugin._labware_database.GetLabware(AvailableLabware.CurrentItem.ToString()), "don't care", "any");
            _plugin.ReleaseStack( plate);
        }

        private void ExecuteUpstackPlate()
        {
            // only need labware name in plate object that's passed to plugin method
            Plate plate = new DestinationPlate( _plugin._labware_database.GetLabware(AvailableLabware.CurrentItem.ToString()), "don't care", "any");
            _plugin.Upstack( plate);
        }

        private void ExecuteDownstackPlate()
        {
            // only need labware name in plate object that's passed to plugin method
            Plate plate = new DestinationPlate( _plugin._labware_database.GetLabware(AvailableLabware.CurrentItem.ToString()), "don't care", "any");
            _plugin.Downstack( plate);
        }
    }
}
