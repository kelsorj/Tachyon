using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using BioNex.Shared.DeviceInterfaces;
using GalaSoft.MvvmLight.Command;
using log4net;

namespace BioNex.LiquidLevelDevice
{
    /// <summary>
    /// Interaction logic for StatusPanel.xaml
    /// </summary>
    public partial class StatusPanel : UserControl
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(StatusPanel));

        public StatusPanel()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void SetModel()
        {
            ILLSensorModel model = ((LLSensorPlugin)Plugin).Model;
            for (int i = 0; i < model.SensorCount; ++i)
                try
                {
                    _grid_data.Add(new SensorGridData()
                    {
                        SensorIndex = (i + 1).ToString(),
                        Port = model.Properties.GetInt(LLProperties.index(LLProperties.Port, i)).ToString(),
                        Enabled = model.Properties.GetBool(LLProperties.index(LLProperties.Enable, i)).ToString(),
                        Connected = (model.Sensors != null && model.Sensors.Length > i && model.Sensors[i] != null && model.Sensors[i].Connected).ToString(),
                        Status = model.LastConfig[i],
                        LastRawValue = "0",
                        LastHeight = "0",
                        DistanceToSurface = "0"
                    });
                }
                catch (KeyNotFoundException)
                {
                    _log.Error(string.Format("LiquidLevelDevice: device configuration is missing property 'port {0}' or 'enable {0}'", i));
                }
            ConnectCommand = new RelayCommand(Plugin.Connect, () => !((LLSensorPlugin)Plugin).Model.Connected);
            DisconnectCommand = new RelayCommand(Plugin.Close, () => ((LLSensorPlugin)Plugin).Model.SensorsConnected || ((LLSensorPlugin)Plugin).Model.MotorsConnected);

            model.ConnectionStateChangedEvent += ConnectionStateChanged;
            model.IntegerSensorReadingReceivedEvent += IntegerReadingReceived;
        }

        void ConnectionStateChanged(object sender, int index, bool connected, string status)
        {
            _grid_data[index].Connected = connected.ToString();
            _grid_data[index].Status = status;
        }

        void IntegerReadingReceived(object sender, int index, int value)
        {
            _grid_data[index].LastRawValue = value.ToString();
            
            var model = ((LLSensorPlugin)Plugin).Model;
            var reading = model.Sensors[index].GetCalibratedReading(value);
            var z_pos = model.Properties.GetDouble(LLProperties.Z_TP) - model.GetAxisPositionMM(LLSensorModelConsts.ZAxis);

            if (((LLSensorPlugin)Plugin).Model.Properties.GetBool(LLProperties.CaptureFloorToZero) && reading > z_pos)
                reading = z_pos;
            reading = z_pos - reading;
            _grid_data[index].LastHeight = string.Format("{0:0.0}", reading);

            var distance = z_pos - reading;
            _grid_data[index].DistanceToSurface = string.Format("{0:0.0}", distance);
        }

        DeviceInterface _plugin;
        public DeviceInterface Plugin
        {
            get { return _plugin; }
            set { _plugin = value; SetModel(); }
        }

        public RelayCommand ConnectCommand { get; set; }
        public RelayCommand DisconnectCommand { get; set; }

        public class SensorGridData : INotifyPropertyChanged
        {
            public string SensorIndex { get; set; }
            public string Port { get; set; }
            public string Enabled { get; set; }
            string _connected;
            public string Connected { get { return _connected; } set { _connected = value; OnPropertyChanged("Connected"); } }
            string _status;
            public string Status { get { return _status; } set { _status = value; OnPropertyChanged("Status"); } }
            string _last_raw;
            public string LastRawValue { get { return _last_raw; } set { _last_raw = value; OnPropertyChanged("LastRawValue"); } }
            string _last_height;
            public string LastHeight { get { return _last_height; } set { _last_height = value; OnPropertyChanged("LastHeight"); } }
            string _distance_to_surface;
            public string DistanceToSurface { get { return _distance_to_surface; } set { _distance_to_surface = value; OnPropertyChanged("DistanceToSurface"); } }

            #region INotifyPropertyChanged Members
            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
            #endregion
        }
        ObservableCollection<SensorGridData> _grid_data = new ObservableCollection<SensorGridData>();
        public ObservableCollection<SensorGridData> GridData { get { return _grid_data; } }

        public void OnGridDataUpdate(object sender, DataTransferEventArgs e)
        {
            var view = sensor_list.View as GridView;
            foreach (var column in view.Columns)
            {
                if (double.IsNaN(column.Width))
                    column.Width = 1;
                column.Width = double.NaN;
            }
        }
    }
}