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
using BioNex.JEMSRpc;
using HiveServerTestApp;
using System.ComponentModel;
using System.Windows.Threading;
using System.Threading;

namespace BioNex.HiveRpc
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public string HostName { get; set; }
        public string RemotePort { get; set; }
        public string ListenPort { get; set; }

        public RelayCommand ReconnectCommand { get; set; }
        public RelayCommand ReinventoryCompleteCommand { get; set; }
        
        private BioNex.HiveRpc.HiveServer _hive_server { get; set; }
        private BioNex.JEMSRpc.JEMSClient _client { get; set; }

        private Thread _connection_thread { get; set; }
        private AutoResetEvent _stop_heartbeat_event { get; set; }

        private string _connection_status_text;
        public string ConnectionStatusText
        {
            get { return _connection_status_text; }
            set
            {
                _connection_status_text = value;
                OnPropertyChanged("ConnectionStatusText");
            }
        }
        private Brush _connection_status_color;
        public Brush ConnectionStatusColor
        {
            get { return _connection_status_color; }
            set
            {
                _connection_status_color = value;
                OnPropertyChanged("ConnectionStatusColor");
            }
        }

        public ICollectionView InventoryView { get; set; }
        public class BindingClass
        {
            public string Barcode { get; set; }
            public string Fate { get; set; }
            public BindingClass(string b, string f) { Barcode = b; Fate = f; }
        }
        private List<BindingClass> _inventory_data;

        public MainWindow()
        {
            _inventory_data = new List<BindingClass>() {
                new BindingClass( "Source1", "No Fate"),
                new BindingClass( "Source2", "No Fate"),
                new BindingClass( "Source3", "No Fate"),
                new BindingClass( "Source4", "No Fate")
            };
            InventoryView = CollectionViewSource.GetDefaultView(_inventory_data);
            HostName = "localhost";
            RemotePort = "6789";
            ListenPort = "5678";

            InitializeComponent();
            InitializeCommands();

            this.DataContext = this;

            ExecuteReconnectCommand();

            _stop_heartbeat_event = new AutoResetEvent(false);
            _connection_thread = new Thread(HeartbeatThread);
            _connection_thread.IsBackground = true;
            _connection_thread.Name = "JEMS heartbeat test";
            _connection_thread.Start();
        }


        private void HeartbeatThread()
        {
            ConnectionTest();
            while( !_stop_heartbeat_event.WaitOne( new TimeSpan( 0, 0, 5))) 
                ConnectionTest();
        }
        private void ConnectionTest()
        {
            try 
            {
                _client.Ping();
                ConnectionStatusText = "Connected";
                ConnectionStatusColor = Brushes.DarkGreen;
            } 
            catch( Exception) 
            {
                ConnectionStatusText = "No connection";
                ConnectionStatusColor = Brushes.DarkRed;
            }
        }

        private void InitializeCommands()
        {
            ReinventoryCompleteCommand = new RelayCommand( ExecuteReinventoryComplete);
            ReconnectCommand = new RelayCommand(ExecuteReconnectCommand);
        }

        private void ExecuteReinventoryComplete()
        {
            try{
                InventoryView = CollectionViewSource.GetDefaultView(_inventory_data);
                OnPropertyChanged("InventoryView");

                _client.ReinventoryComplete( "HiveA", "CartA", (from x in _inventory_data select x.Barcode).ToArray());
                MessageBox.Show( "Reinventory complete.");
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private void ExecuteReconnectCommand()
        {
            try
            {
                _hive_server = new HiveServer(OnUpdatePlateFate, OnEndFateBatch, int.Parse(ListenPort));
                _client = new JEMSClient(HostName, int.Parse(RemotePort));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnUpdatePlateFate(string barcode, string fate)
        {
            var match = (from x in _inventory_data where x.Barcode == barcode select x).FirstOrDefault();
            if (match == null)
            {
                MessageBox.Show(string.Format("Couldn't find plate '{0}' in inventory", barcode), "Can't set fate of missing plate");
            }
            match.Fate = fate;
            Dispatcher.Invoke(new Action(InventoryView.Refresh));
        }

        private void OnEndFateBatch(string finished_fate)
        {            
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _stop_heartbeat_event.Set();
            _connection_thread.Join();
        }
    }
}
