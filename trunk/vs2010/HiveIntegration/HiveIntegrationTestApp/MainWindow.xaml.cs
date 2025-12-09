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
using BioNex.Shared.Utils;
using System.Text.RegularExpressions;

namespace HiveIntegrationTestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private BioNex.HiveIntegration.Hive _hive_client;
        private BioNex.HiveIntegration.HiveServer _hive_server;
        private DummyHiveImpl _dummy_hive_impl;

        private string _server_address;
        public string ServerAddress
        {
            get { return _server_address; }
            set
            {
                _server_address = value;
                OnPropertyChanged("ServerAddress");
            }
        }
        
        string _server_port;
        public string ServerPort
        {
            get { return _server_port; }
            set
            {
                _server_port = value;
                OnPropertyChanged("ServerPort");
            }
        }

        private string _barcode;
        public string Barcode
        {
            get { return _barcode; }
            set
            {
                _barcode = value;
                OnPropertyChanged("Barcode");
            }
        }

        private string _labware_name;
        public string LabwareName
        {
            get { return _labware_name; }
            set
            {
                _labware_name = value;
                OnPropertyChanged("LabwareName");
            }
        }

        private string _group_name;
        public string GroupName
        {
            get { return _group_name; }
            set
            {
                _group_name = value;
                OnPropertyChanged("GroupName");
            }
        }
        
        private bool _server_has_barcode;
        public bool ServerHasBarcode
        {
            get { return _server_has_barcode; }
            set
            {
                if( _dummy_hive_impl != null)
                    _dummy_hive_impl.BarcodePresent = value;
                _server_has_barcode = value;
                OnPropertyChanged("ServerHasBarcode");
            }
        }

        private bool _server_api_error;
        public bool ServerApiError
        {
            get { return _server_api_error; }
            set
            {
                if( _dummy_hive_impl != null)
                    _dummy_hive_impl.SimulateApiError = value;
                _server_api_error = value;
                OnPropertyChanged("ServerApiError");
            }
        }
        
        public SimpleRelayCommand StartServerCommand { get; set; }
        public SimpleRelayCommand InitializeCommand { get; set; }
        public SimpleRelayCommand CloseCommand { get; set; }
        public SimpleRelayCommand UnloadPlateCommand { get; set; }
        public SimpleRelayCommand LoadPlateCommand { get; set; }
        public SimpleRelayCommand HasBarcodeCommand { get; set; }
        public SimpleRelayCommand GetInventoryCommand { get; set; }
        public SimpleRelayCommand ScanInventoryCommand { get; set; }
        public SimpleRelayCommand GetLastErrorCommand { get; set; }
        public SimpleRelayCommand MovePlateCommand { get; set; }
        public SimpleRelayCommand GetStatusCommand { get; set; }
        public SimpleRelayCommand PresentStageCommand { get; set; }

        public MainWindow()
        {
            ServerAddress = "localhost";
            ServerPort = "7777";
            Barcode = "barcode001";
            LabwareName = "Greiner 781101";
            // server side commands
            StartServerCommand = new SimpleRelayCommand( ExecuteStartServer);
            // client side commands
            InitializeCommand = new SimpleRelayCommand( ExecuteInitialize);
            CloseCommand = new SimpleRelayCommand( ExecuteClose);
            UnloadPlateCommand = new SimpleRelayCommand( ExecuteUnloadPlate);
            LoadPlateCommand = new SimpleRelayCommand( ExecuteLoadPlate);
            HasBarcodeCommand = new SimpleRelayCommand( ExecuteHasBarcode);
            GetInventoryCommand = new SimpleRelayCommand( ExecuteGetInventory);
            ScanInventoryCommand = new SimpleRelayCommand( ExecuteScanInventory);
            GetLastErrorCommand = new SimpleRelayCommand( ExecuteGetLastError);
            MovePlateCommand = new SimpleRelayCommand( ExecuteMovePlate);
            GetStatusCommand = new SimpleRelayCommand( ExecuteGetStatus);
            PresentStageCommand = new SimpleRelayCommand( ExecutePresentStage);

            this.DataContext = this;
            InitializeComponent();

            _hive_client = new BioNex.HiveIntegration.Hive();
            _dummy_hive_impl = new DummyHiveImpl();
        }

        private void ExecuteStartServer()
        {
            try {
                _hive_server = new BioNex.HiveIntegration.HiveServer( _dummy_hive_impl, int.Parse( ServerPort));
                MessageBox.Show( "Server started");
            } catch( Exception ex) {
                MessageBox.Show( "Server could not be started: " + ex.Message);
            }
        }

        private void ExecuteInitialize()
        {
            try {
                string xml = BioNex.HiveIntegration.HiveXmlHelper.InitializeParamsToXml( ServerAddress, 7777);
                if( !_hive_client.Initialize( xml)) {
                    throw new Exception( _hive_client.GetLastError());
                }
                MessageBox.Show( "Client initialized");
            } catch( Exception ex) {
                MessageBox.Show( "Client could not be initialized: " + ex.Message);
            }
        }

        private void ExecuteClose()
        {
            try {
                if( !_hive_client.Close()) {
                    throw new Exception( _hive_client.GetLastError());
                }
                MessageBox.Show( "Client closed connection to Hive");
            } catch( Exception ex) {
                MessageBox.Show( "Client could not close connection to Hive");
            }
        }

        private void ExecuteHelper( Func<bool> f)
        {
            // get the name of the method from the anonymous function name
            // it's just whatever is in between the <>s
            string method_name = f.Method.Name; // temporarily set to anonymous function name
            Regex regex = new Regex(@"<Execute(\w+)>");
            Match match = regex.Match( f.Method.Name);
            if( match.Groups.Count == 2)
                method_name = match.Groups[1].ToString();

            bool ret = f();
            if( !ret) {
                MessageBox.Show( method_name + " failed: " + _hive_client.GetLastError());
            } else {
                MessageBox.Show( method_name + " succeeded");
            }
        }

        private void ExecuteUnloadPlate()
        {
            ExecuteHelper( () => { return _hive_client.UnloadPlate( Barcode, LabwareName); });
        }

        private void ExecuteLoadPlate()
        {
            ExecuteHelper( () => { return _hive_client.LoadPlate( Barcode, LabwareName); });
        }

        private void ExecuteHasBarcode()
        {
            bool found;
            if( !_hive_client.HasBarcode( Barcode, out found)) {
                MessageBox.Show( "Failed to check for barcode presence in Hive: " + _hive_client.GetLastError());
            } else {
                MessageBox.Show( found ? "Barcode found." : "Barcode not found.");
            }
        }

        private void ExecuteGetInventory()
        {
            string xml = "";
            if( !_hive_client.GetInventory( out xml)) {
                MessageBox.Show( "Failed to get inventory from Hive: " + _hive_client.GetLastError());
            } else {
                MessageBox.Show( "Inventory XML: " + xml);
            }
        }

        private void ExecuteScanInventory()
        {
            ExecuteHelper( _hive_client.ScanInventory );
        }

        private void ExecuteGetLastError()        
        {
            // GetLastError cannot fail because it is a cached value in the client.  The error string only
            // gets updated when a command fails.
            MessageBox.Show( _hive_client.GetLastError());
        }

        private void ExecuteMovePlate()
        {
            ExecuteHelper( () => { return _hive_client.MovePlate( Barcode, LabwareName, GroupName); });
        }

        private void ExecuteGetStatus()
        {
            BioNex.HiveIntegration.HiveStatus status;
            if( !_hive_client.GetStatus( out status)) {
                MessageBox.Show( "Failed to get status from Hive: " + _hive_client.GetLastError());
            } else {
                MessageBox.Show( String.Format( "Hive status:\r\n{0}", status));
            }
        }

        private void ExecutePresentStage()
        {
            ExecuteHelper( _hive_client.PresentStage );
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
