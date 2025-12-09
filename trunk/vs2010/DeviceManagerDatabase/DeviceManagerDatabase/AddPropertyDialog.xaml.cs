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
using System.ComponentModel;

namespace BioNex.SynapsisPrototype
{
    /// <summary>
    /// Interaction logic for AddPropertyDialog.xaml
    /// </summary>
    public partial class AddPropertyDialog : Window
    {
        private DeviceManager _device_manager { get; set; }

        public string CompanyName { get; private set; }
        public string ProductName { get; private set; }
        public string DeviceName { get; private set; }
        public string PropertyName { get; set; }
        public string PropertyValue { get; set; }

        public RelayCommand OkCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }

        public AddPropertyDialog( DeviceManager device_manager, string company_name, string product_name, string device_name)
        {
            _device_manager = device_manager;
            CompanyName = company_name;
            ProductName = product_name;
            DeviceName = device_name;
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
            _device_manager.db.AddDeviceProperty( CompanyName, ProductName, DeviceName, PropertyName, PropertyValue);
            Close();
        }

        private void ExecuteCancel()
        {
            Close();
        }
    }
}
