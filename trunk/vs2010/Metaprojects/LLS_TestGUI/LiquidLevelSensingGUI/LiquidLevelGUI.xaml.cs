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
using BioNex.Shared.LibraryInterfaces;
using System.ComponentModel.Composition;
using GalaSoft.MvvmLight.Command;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.BioNexGuiControls;
using System.Windows.Media.Media3D;
using BioNex.LiquidLevelDevice;
using log4net;
using BioNex.SynapsisPrototype;

namespace LiquidLevelSensingGUI
{
    [Export(typeof(ICustomerGUI))]
    public partial class LiquidLevelGUI : UserControl, ICustomerGUI, ICustomerGUIPauseListener
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(LiquidLevelGUI));
        bool _running_protocol = false;

        public RelayCommand HomeAllDevicesCommand { get; set; }
        public RelayCommand CalibrateSensorsCommand {get; set; }
        public bool ShowResultsSummary { get { return _model.ShowResultsSummary; } set { _model.ShowResultsSummary = value; } }

        [Import]
        private Lazy<ICustomSynapsisQuery> SynapsisQuery { get; set; }
        private LLSGUIModel _model;

        DeviceManager _device_manager;
        ILabwareDatabase _labware_database;

        [ImportingConstructor]
        public LiquidLevelGUI([Import] ExternalDataRequesterInterface dri, [Import] DeviceManager device_manager, [Import]ILabwareDatabase labware_database)
        {
            InitializeComponent();
            DataContext = this;
            HomeAllDevicesCommand = new RelayCommand(HomeAllDevices, CanExecuteHomeAllDevicesCommand);
            CalibrateSensorsCommand = new RelayCommand(CalibrateSensors, CanExecuteCalibrateSensors);

            _device_manager = device_manager;
            _labware_database = labware_database;

            _model = new LLSGUIModel(dri);
            _model.ProtocolComplete += LogCaptureComplete;
            _model.Sensor.SavePropertiesEvent += LSESaveProperties;

            labware_database.LabwareChanged += LabwareDatabase_LabwareChanged;
            LabwareCombo.ItemsSource = labware_database.GetLabwareNames();
            LabwareCombo.SelectedIndex = 0;

            ScanProgress.Plugin = _model.Sensor;
        }

        void LabwareDatabase_LabwareChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(new Action(() => 
            {
                LabwareCombo.ItemsSource = ((ILabwareDatabase)sender).GetLabwareNames();
                LabwareCombo.SelectedIndex = 0;
                CommandManager.InvalidateRequerySuggested(); 
            }));
        }

        private bool _homing;
        private void HomeAllDevices()
        {
            // do this in a thread since Synapsis doesn't and blocking the UI is retarded
            var thread = new System.Threading.Thread(() => { _homing = true; SynapsisQuery.Value.HomeAllDevices(); _homing = false; });
            thread.IsBackground = true;
            thread.Start();
        }

        private bool CanExecuteHomeAllDevicesCommand()
        {
            string reason;
            return !_homing && !_calibrating && !_running_protocol && _model.Device.Connected && SynapsisQuery != null  && SynapsisQuery.Value.ClearToHome(out reason);
        }

        private bool _calibrating;
        private void CalibrateSensors()
        {
            var message = string.Format("Make sure you've removed any plate from the stage, calibration will move the sensors to {0} mm above the teachpoint.", _model.Sensor.Properties[LLProperties.CaptureOffset]);
            var result = MessageBox.Show(message, "Proceed with calibration", MessageBoxButton.OKCancel);
            if( result == MessageBoxResult.Cancel)
                return;

            var thread = new System.Threading.Thread(() => { _calibrating = true; _model.CalibrateSensors(); _calibrating = false; });
            thread.IsBackground = true;
            thread.Start();
        }

        private bool CanExecuteCalibrateSensors()
        {
            return !_homing && !_calibrating && !_running_protocol && _model != null && _model.Device.Connected && _model.Device.IsHomed;
        }

        #region ICustomerGUI
        public event EventHandler ProtocolComplete { add { _model.ProtocolComplete += value; } remove { _model.ProtocolComplete -= value; } }
        public event EventHandler AbortableTaskStarted { add { ;} remove { ;} }
        public event EventHandler AbortableTaskComplete { add { ;} remove { ;} }
        string ICustomerGUI.GUIName { get { return "Liquid Level Sensing"; } }
        bool ICustomerGUI.Busy { get { return _running_protocol; } }
        string ICustomerGUI.BusyReason { get { return "Running Protocol"; } }
        bool ICustomerGUI.CanExecuteStart(out IEnumerable<string> failure_reasons) 
        { 
            var reasons = new List<string>();  
            failure_reasons = reasons;
            if (!_model.Device.Connected)
                reasons.Add("Not connected");
            if (_homing)
                reasons.Add("Currently homing");
            if (_calibrating)
                reasons.Add("Currently calibrating");
            return reasons.Count == 0; 
        }
        bool ICustomerGUI.ShowProtocolExecuteButtons() { return true; }
        bool ICustomerGUI.CanClose() { return true; }
        bool ICustomerGUI.CanPause() { return _running_protocol; }
        void ICustomerGUI.Close() { _model.Close(); }
        bool ICustomerGUI.AllowDiagnostics() { return true; }
        bool ICustomerGUI.ExecuteStart()
        {
            _running_protocol = true;
            LabwareCombo.IsEnabled = false;
            _model.Start((string)LabwareCombo.SelectedItem);
            return true;
        }
        void ICustomerGUI.CompositionComplete() { }
        #endregion

        void LogCaptureComplete(object Sender, EventArgs e)
        {
            _running_protocol = false;
            Dispatcher.BeginInvoke(new Action(() =>{ LabwareCombo.IsEnabled = true;}));
        }

        // TODO -- these two functions could be handled in the plugin itself, provided the DeviceManager was passed in
        void ReLoadProperties()
        {
            var db = _device_manager.db;
            var dict = db.GetProperties(_model.Device.Manufacturer, _model.Device.ProductName, _model.Device.Name);
            var device_info = new DeviceManagerDatabase.DeviceInfo(_model.Device.Manufacturer, _model.Device.ProductName, _model.Device.Name, false, dict);
            _model.Device.SetProperties(device_info);
        }
        void LSESaveProperties(object sender, IDictionary<string, string> properties)
        {
            _device_manager.db.UpdateDevice(_model.Device.Manufacturer, _model.Device.ProductName, _model.Device.Name, properties);

            // reload the properties to make sure the model has the correct values
            ReLoadProperties();
        }


        public void Pause()
        {
            _model.Pause();    
        }

        public void Resume()
        {
            _model.Resume();
        }

        public void Abort()
        {
            _model.Abort();
        }

        private void ScanProgress_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _model.ShowLastResults();
        }
    }
}
