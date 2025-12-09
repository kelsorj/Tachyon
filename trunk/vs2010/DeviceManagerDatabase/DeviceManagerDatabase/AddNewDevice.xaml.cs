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
using GalaSoft.MvvmLight.Command;

namespace BioNex.SynapsisPrototype
{
    /// <summary>
    /// Interaction logic for AddNewDevice.xaml
    /// </summary>
    public partial class AddNewDevice : Window
    {
        /// <summary>
        /// the name of the device INSTANCE to add
        /// </summary>
        public string DeviceName { get; set; }
        /// <summary>
        /// who makes the device?  e.g. BioNex, Keyence
        /// This had better match whatever the plugin returns from GetManufacturer()
        /// </summary>
        public string Manufacturer { get; set; }
        /// <summary>
        /// the name of the product, e.g. Hive, PlateMover
        /// This had better match whatever the plugin returns from GetProductName()
        /// </summary>
        public string ProductType { get; set; }

        public RelayCommand OkCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }

        private DeviceManager _device_manager { get; set; }

        public AddNewDevice( DeviceManager device_manager)
        {
            _device_manager = device_manager;

            InitializeComponent();
            InitializeCommands();

            DataContext = this;
        }

        private void InitializeCommands()
        {
            OkCommand = new RelayCommand( ExecuteOk);
            CancelCommand = new RelayCommand( ExecuteCancel);
        }

        private void ExecuteOk()
        {
            try {
                _device_manager.db.AddDevice( Manufacturer, ProductType, DeviceName, false, new Dictionary<string,string>());
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
            Close();
        }

        private void ExecuteCancel()
        {
            Close();
        }
    }
}
