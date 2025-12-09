using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using BioNex.LiquidLevelDevice;
using BioNex.Shared.Utils;
using System.Windows.Input;
using System.ComponentModel;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.LabwareDatabase;
using BioNex.Shared.Utils.WellMathUtil;

namespace BeeSureTestApp
{
    /// <summary>
    /// Interaction logic for BeeSurePanel.xaml
    /// </summary>
    public partial class BeeSurePanel : UserControl, INotifyPropertyChanged
    {
        private Window _owner;
        private int _index;
        private IBeeSureIntegration _beesure;
        private ICollectionView _available_labware;
        private Dictionary<string, IBeeSureLabwareProperties> _labware_database;

        public ICollectionView AvailableLabware
        {
            get { return _available_labware; }
            set
            {
                _available_labware = value;
                OnPropertyChanged("AvailableLabware");
            }
        }

        public SimpleRelayCommand InitializeCommand { get; set; }
        public SimpleRelayCommand HomeCommand { get; set; }
        public SimpleRelayCommand MoveToPortraitCommand { get; set; }
        public SimpleRelayCommand MoveToLandscapeCommand { get; set; }
        public SimpleRelayCommand DiagnosticsCommand { get; set; }
        public SimpleRelayCommand ScanCommand { get; set; }
        public SimpleRelayCommand CalibrateCommand { get; set; }

        public BeeSurePanel(Window owner, int index)
        {
            InitializeComponent();
            InitializeCommands();
            this.DataContext = this;

            _owner = owner;
            _index = index;

            LoadLabwareDatabase();
            _beesure = new BeeSureIntegration();
            _beesure.AttachScanProgressPanel(ScanProgress);
        }

        private void LoadLabwareDatabase()
        {
            // in case our labware database doesn't have "Well radius present for the selected labware, use this default value
            double default_well_radius = 4.5;

            _labware_database = new Dictionary<string, IBeeSureLabwareProperties>();
            string path = BioNex.Shared.Utils.FileSystem.GetAppPath();
            ILabwareDatabase labware_database = new LabwareDatabase(path + "\\labware.s3db");

            // for using our own labware database
            foreach (var name in labware_database.GetLabwareNames())
            {
                ILabware lw = labware_database.GetLabware(name);
                int num_wells = lw[LabwarePropertyNames.NumberOfWells].ToInt();
                double row_spacing = lw[LabwarePropertyNames.RowSpacing].ToDouble();
                double column_spacing = lw[LabwarePropertyNames.ColumnSpacing].ToDouble();
                double thickness = lw[LabwarePropertyNames.Thickness].ToDouble();

                // Well radius is sort of mis-named, it's actually a Beesure offset value that roughly corresponds to well radius, but is actually a spacing parameter.
                // We should probably rename this before release?
                object temp = lw[LabwarePropertyNames.WellRadius];
                double well_radius = temp != null ? temp.ToDouble() : default_well_radius;

                // allow format override of number of rows / columns : 
                //  legacy labware database stored number of wells, and only handled 96/384 plates.
                //  BeeSure has to handle odd formats as well, so can't use the legacy handling
                //  If NumberOfRows or NumberOfColumns is present in the db, use that value, otherwise use the legacy value from Format field
                int num_columns = 12;
                int num_rows = 8;
                LabwareFormat format = null;
                try
                {
                    format = LabwareFormat.GetLabwareFormat2(num_wells);
                }
                catch (LabwareFormat.InvalidWellCountException) { }


                if (lw.Properties.ContainsKey(LabwarePropertyNames.NumberOfColumns))
                    num_columns = lw[LabwarePropertyNames.NumberOfColumns].ToInt();
                else if (format != null)
                    num_columns = format.NumCols;
                if (lw.Properties.ContainsKey(LabwarePropertyNames.NumberOfRows))
                    num_rows = lw[LabwarePropertyNames.NumberOfRows].ToInt();
                else if (format != null)
                    num_rows = format.NumRows;


                _labware_database.Add(lw.Name, new BeeSureLabware(lw.Name, (short)num_rows, (short)num_columns, row_spacing, column_spacing, thickness, well_radius));
            }

            AvailableLabware = CollectionViewSource.GetDefaultView(_labware_database.Keys.ToList());
        }

        private IBeeSureLabwareProperties GetLabwareProperties(string labware_name)
        {
            return _labware_database[labware_name];
        }

        public void InitializeCommands()
        {
            InitializeCommand = new SimpleRelayCommand(ExecuteInitialize, () => { return !_scanning; });
            HomeCommand = new SimpleRelayCommand(ExecuteHome, () => { return !_scanning && _beesure.IsConnected; });
            MoveToPortraitCommand = new SimpleRelayCommand(ExecuteMoveToPortrait, () => { return !_scanning && _beesure.IsConnected; });
            MoveToLandscapeCommand = new SimpleRelayCommand(ExecuteMoveToLandscape, () => { return !_scanning && _beesure.IsConnected; });
            DiagnosticsCommand = new SimpleRelayCommand(ExecuteShowDiagnostics, () => { return _beesure.IsConnected; });
            ScanCommand = new SimpleRelayCommand(ExecuteScan, () => { return !_scanning && _beesure.IsHomed; });
            CalibrateCommand = new SimpleRelayCommand(ExecuteCalibrate, () => { return !_scanning && _beesure.IsHomed; });
        }

        public void Close()
        {
            _beesure.Close();
        }

        /////////////////////////////////////////////////
        // command handlers
        /////////////////////////////////////////////////
        private void ExecuteInitialize()
        {
            var db = new BioNex.SynapsisPrototype.DeviceManagerDatabase();
            var device_properties = db.GetProperties("BioNex", BioNexDeviceNames.BeeSure, "Liquid Level Sensor");

            device_properties["company"] = "BioNex";
            device_properties["product"] = BioNexDeviceNames.BeeSure;
            device_properties["name"] = String.Format("BeeSure{0}", _index + 1);
            device_properties["simulate"] = simulate.IsChecked.Value.ToString();

            int motor_adapter = adapter_id.Text.ToInt();
            int sensor_adapter = motor_adapter + 1;

            device_properties["motor CAN device id"] = motor_adapter.ToString();
            device_properties["sensor CAN device id"] = sensor_adapter.ToString();

            _beesure.Initialize(device_properties);
        }

        private void ExecuteHome()
        {
            _beesure.Home();
        }

        private void ExecuteMoveToPortrait()
        {
            _beesure.MoveToParkPosition(true);
        }

        private void ExecuteMoveToLandscape()
        {
            _beesure.MoveToParkPosition(false);
        }

        private void ExecuteShowDiagnostics()
        {
            _beesure.ShowDiagnostics(true, _labware_database.Values);
        }

        bool _scanning;
        private void ExecuteScan()
        {
            IBeeSureLabwareProperties properties = GetLabwareProperties(AvailableLabware.CurrentItem.ToString());

            // run scan in a thread so that we get progress updates in UI
            _scanning = true;
            new System.Threading.Thread(() =>
            {
                _beesure.Capture(properties);
                _scanning = false;   
            }).Start();

        }

        private void ExecuteCalibrate()
        {
            IBeeSureLabwareProperties properties = GetLabwareProperties(AvailableLabware.CurrentItem.ToString());
            _beesure.Calibrate(properties);
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string property_name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property_name));
            }
        }

        #endregion
    }
}
