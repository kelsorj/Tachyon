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
using BioNex.HiveRpc;
using BioNex.GemsRpc;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;
using BioNex.Shared.Utils;

namespace JEMSServerTestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public string Fate { get; set; }
        public string HostName { get { return _configuration.HostName; } set { _configuration.HostName = value; } }
        public string RemotePort { get { return _configuration.RemotePort; } set { _configuration.RemotePort = value; } }
        public string ListenPort { get { return _configuration.ListenPort; } set { _configuration.ListenPort = value; } }

        public string SelectedRows { get { return InventoryGrid.SelectedItems.Count.ToString(); } set { } }

        public RelayCommand EndBatchCommand { get; set; }
        public RelayCommand ClearInventoryCommand { get; set; }
        public RelayCommand ReconnectCommand { get; set; }
        public RelayCommand SetAllFatesCommand { get; set; }

        private Dictionary<string,string> _inventory = new Dictionary<string,string>();
        public ICollectionView InventoryView { get; set; }

        private BioNex.HiveRpc.HiveClient _client;
        private BioNex.GemsRpc.JemsServer _server;

        private Thread _connection_thread;
        private AutoResetEvent _stop_heartbeat_event;

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

        public class BindingClass
        {
            public delegate void ChangedFate(string barcode, string fate);
            public event ChangedFate FateChanged;

            public string Barcode { get; set; }
            public string HiveName { get; set; }
            string _fate;
            public string Fate {
                get { return _fate; }
                set
                {
                    _fate = value;
                    if (FateChanged != null)
                        FateChanged(Barcode, _fate);
                }
            }
            public BindingClass(string barcode, string fate, string hive_name)
            {
                Barcode = barcode;
                _fate = fate; // prevent event call on construction
                HiveName = hive_name;
            }
        }
        private List<BindingClass> _binding_class = new List<BindingClass>();

        private void RefreshInventoryView(string hive_name)
        {
            _binding_class.Clear();
            _binding_class.AddRange( from x in _inventory select new BindingClass(x.Key, x.Value, hive_name));
            foreach (var c in _binding_class) 
                c.FateChanged += PlateFateChanged;
            Dispatcher.Invoke(new Action(InventoryView.Refresh));
        }

        private void PlateFateChanged(string barcode, string fate)
        {
            if (fate == "Unknown Fate")
                return;
            _inventory[barcode] = fate; // save fate value so it's not replaced w/ 'unknown fate' if we re-inventory this plate
            try
            {
                _client.SetPlateFate(barcode, fate);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }


        public class JEMSServerTestAppConfiguration
        {
            public string HostName { get; set; }
            public string RemotePort { get; set; }
            public string ListenPort { get; set; }
            public JEMSServerTestAppConfiguration()
            {
                HostName = "localhost";
                RemotePort = "5678";
                ListenPort = "6789";
            }
        }
        JEMSServerTestAppConfiguration _configuration;
        string _configPath;

        public MainWindow()
        {
            _configPath = BioNex.Shared.Utils.FileSystem.GetAppPath() + "\\config\\JEMSServerTestAppSettings.xml"; ;
            _configuration = FileSystem.LoadXmlConfiguration<JEMSServerTestAppConfiguration>(_configPath);

            InitializeComponent();
            InitializeCommands();
            this.DataContext = this;

            InventoryView = CollectionViewSource.GetDefaultView(_binding_class);

            ExecuteReconnectCommand();

            _stop_heartbeat_event = new AutoResetEvent(false);
            _connection_thread = new Thread(HeartbeatThread);
            _connection_thread.IsBackground = true;
            _connection_thread.Name = "JEMS heartbeat test";
            _connection_thread.Start();
        }


        ~MainWindow()
        {
            if (_server != null)
                _server.Stop();
            FileSystem.SaveXmlConfiguration<JEMSServerTestAppConfiguration>(_configuration, _configPath);
        }

        private void HeartbeatThread()
        {
            ConnectionTest();
            while (!_stop_heartbeat_event.WaitOne(new TimeSpan(0, 0, 5)))
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
            catch (Exception )
            {
                ConnectionStatusText = "No connection";
                ConnectionStatusColor = Brushes.DarkRed;
            }
        }
        
        public void InitializeCommands()
        {
            EndBatchCommand = new RelayCommand( ExecuteEndBatchCommand, CanExecuteEndBatchCommand);
            ClearInventoryCommand = new RelayCommand( ExecuteClearInventoryCommand);
            ReconnectCommand = new RelayCommand(ExecuteReconnectCommand);
            SetAllFatesCommand = new RelayCommand(ExecuteSetAllFatesCommand, CanExecuteSetAllFatesCommand);
        }

        private void ExecuteClearInventoryCommand()
        {
            _inventory.Clear();
            RefreshInventoryView("");
        }

        private void ExecuteEndBatchCommand()
        {
            try {
                _client.EndBatch( Fate);
            } catch( Exception ex) {
                MessageBox.Show( ex.ToString());
            }
        }

        private bool CanExecuteEndBatchCommand()
        {
            return Fate != null && Fate != "Unknown Fate";
        }

        private void ExecuteReconnectCommand()
        {
            try
            {
                _server = new JemsServer(ref _inventory, RefreshInventoryView, int.Parse(ListenPort));
                _client = new HiveClient(HostName, int.Parse(RemotePort));
            }
            catch (System.Net.HttpListenerException ex)
            {
                MessageBox.Show(string.Format("Error - You're probably running with insufficient privileges. Error message was '{0}'", ex.Message));
            }
        }

        private void ExecuteSetAllFatesCommand()
        {
            foreach (BindingClass item in InventoryGrid.SelectedItems)
            {
                item.Fate = Fate;
                PlateFateChanged(item.Barcode, Fate);
                Dispatcher.Invoke(new Action(InventoryView.Refresh));
            }
        }

        private bool CanExecuteSetAllFatesCommand()
        {
            return Fate != null && Fate != "Unknown Fate" && InventoryGrid.SelectedItems.Count > 0;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string property_name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property_name));
        }

        #endregion

        private void InventoryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OnPropertyChanged("SelectedRows");
        }
    }
}
