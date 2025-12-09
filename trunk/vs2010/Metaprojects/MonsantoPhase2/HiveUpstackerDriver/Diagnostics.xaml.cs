using System;
using System.Collections.Generic;
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
using System.Threading;

namespace BioNex.IWorksPlugins
{
    /// <summary>
    /// Interaction logic for Diagnostics.xaml
    /// </summary>
    public partial class Diagnostics : Window, INotifyPropertyChanged
    {
        private HiveUpstacker _plugin { get; set; }
        public RelayCommand UpstackCommand { get; set; }
        public RelayCommand DownstackCommand { get; set; }

        private Thread _heartbeat_thread;
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

        public Diagnostics( HiveUpstacker plugin)
        {
            _plugin = plugin;
            InitializeComponent();
            InitializeCommands();
            this.DataContext = this;

            _stop_heartbeat_event = new AutoResetEvent(false);
            _heartbeat_thread = new Thread(ConnectionTest);
            _heartbeat_thread.IsBackground = true;
            _heartbeat_thread.Name = "Hive stacker heartbeat";
            _heartbeat_thread.Start();
        }

        private void ConnectionTest()
        {
            while (!_stop_heartbeat_event.WaitOne(new TimeSpan(0, 0, 5)))
            {
                try
                {
                    _plugin.Ping();
                    ConnectionStatusText = "Connected";
                    ConnectionStatusColor = Brushes.DarkGreen;
                }
                catch (Exception)
                {
                    ConnectionStatusText = "No connection";
                    ConnectionStatusColor = Brushes.DarkRed;
                }
            }
        }

        private void InitializeCommands()
        {
            UpstackCommand = new RelayCommand(ExecuteUpstack);
            DownstackCommand = new RelayCommand(ExecuteDownstack);
        }

        private void ExecuteUpstack()
        {
            try {
                _plugin.SinkPlate("96 Corning Destination", 0, "");
            } catch (Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private void ExecuteDownstack()
        {
            try
            {
                _plugin.SourcePlate("96 Corning Destination", 0, ""); 
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string property_name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property_name));
        }

        #endregion
    }
}
