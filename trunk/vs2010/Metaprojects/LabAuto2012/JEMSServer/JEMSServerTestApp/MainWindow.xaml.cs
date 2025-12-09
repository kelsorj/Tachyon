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
using System.Xml.Linq;

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

        private string _workset_file;
        public string WorkSetFile { get { return _workset_file; } set { _workset_file = value; OnPropertyChanged("WorkSetFile"); } }
        public RelayCommand SelectWorkSetCommand { get; set; }
        public RelayCommand AddWorkSetCommand { get; set; }
        public RelayCommand ShowTransferMapCommand { get; set; }

        private Dictionary<string, TransferMap[]> _transfer_map = new Dictionary<string, TransferMap[]>();
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
            if (fate == "Desintation Complete")
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
            _workset_file = "Select a work set file";

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

            SelectWorkSetCommand = new RelayCommand(ExecuteSelectWorkSetCommand);
            AddWorkSetCommand = new RelayCommand(ExecuteAddWorkSetCommand, CanExecuteAddWorkSetCommand);
            ShowTransferMapCommand = new RelayCommand(ExecuteShowTransferMapCommand, CanExecuteShowTransferMapCommand);
        }

        private void ExecuteClearInventoryCommand()
        {
            _transfer_map.Clear();
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
            return Fate != null && Fate != "Unknown Fate" && Fate != "Destination Complete";
        }

        private void ExecuteReconnectCommand()
        {
            try
            {
                _server = new JemsServer(ref _transfer_map, ref _inventory, RefreshInventoryView, int.Parse(ListenPort));
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
            return Fate != null && Fate != "Unknown Fate" && Fate != "Destination Complete" && InventoryGrid.SelectedItems.Count > 0;
        }

        private void ExecuteSelectWorkSetCommand()
        {
            var ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.InitialDirectory = BioNex.Shared.Utils.FileSystem.GetAppPath();
            ofd.Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*";
            if (ofd.ShowDialog() == true)
                WorkSetFile = ofd.FileName;
            else
                WorkSetFile = "";
        }

        private void ExecuteAddWorkSetCommand()
        {
            try {
                var in_doc = XDocument.Load(_workset_file);
                var out_doc = new XDocument();

                var in_root = in_doc.Root;
                var in_project = in_root.Elements("project").First();
                var in_source = in_project.Elements("source").First();

                var out_root = new XElement(in_doc.Root);
                var out_project = new XElement(in_project);
                out_root.Elements().Remove();
                out_project.Elements().Remove();

                int source_plate_counter = 1;
                int destination_plate_counter = 1;

                var temp_source = new XElement(in_source);
                int num_sample_hits = temp_source.Elements( "hitpick").Where( hit => hit.Attribute( "type").Value.ToString() == "sample").Count();
                int num_control_hits = temp_source.Elements( "hitpick").Where( hit => hit.Attribute( "destination_plate") != null).Count();
                // destination name does not imply number of wells in destination plate.
                // int num_wells_in_plate = int.Parse( in_project.Attribute("destination").Value.ToString());
                int num_wells_in_plate = 96;
                int control_plate_interval = ((num_wells_in_plate - num_control_hits) / num_sample_hits) + (num_wells_in_plate / num_sample_hits);

                foreach (BindingClass item in InventoryGrid.SelectedItems)
                {
                    var out_source = new XElement(in_source);
                    out_source.Attribute("bc").Value = item.Barcode;
                    out_project.Add(out_source);
                    var control_hits = out_source.Elements( "hitpick").Where( hit => hit.Attribute( "destination_plate") != null);
                    if( source_plate_counter % control_plate_interval == 1){
                        foreach( var control_hit in control_hits){
                            control_hit.Attribute( "destination_plate").Value = destination_plate_counter.ToString();
                        }
                        destination_plate_counter++;
                    } else{
                        control_hits.Remove();
                    }
                    source_plate_counter++;
                }

                out_root.Add(out_project);
                out_doc.Add(out_root);

                _client.AddWorkSet(out_doc.ToString());
            } catch (Exception ex) {
                MessageBox.Show(ex.ToString());
            }
        }

        private bool CanExecuteAddWorkSetCommand()
        {
            return WorkSetFile != "" && WorkSetFile != "Select a work set file";
        }

        private void ExecuteShowTransferMapCommand()
        {
            foreach (BindingClass item in InventoryGrid.SelectedItems)
                ShowTransferMap(item.Barcode);
        }

        private bool CanExecuteShowTransferMapCommand()
        {
            foreach (BindingClass item in InventoryGrid.SelectedItems)
                if( _transfer_map.ContainsKey(item.Barcode))
                    return true;
            return false;
        }

        private void ShowTransferMap(string barcode)
        {
            if( !_transfer_map.ContainsKey(barcode))
                return;
            TransferMap[] mapping = _transfer_map[barcode];
            string caption = string.Format("Transfer Map for '{0}'", barcode);
            string message = "";
            for( int i=0; i<mapping.Length; ++i)
                message += string.Format( "source: {0} {{source well ({1},{2}) destination well ({3},{4}) transfer volume {5}}}\n",
                    mapping[i].source_barcode, 
                    mapping[i].source_row, 
                    mapping[i].source_column, 
                    mapping[i].destination_row, 
                    mapping[i].destination_column, 
                    mapping[i].transfer_volume);
            MessageBox.Show(message, caption);
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
