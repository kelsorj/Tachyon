using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using BioNex.Shared.Utils;
using System.Threading;
using System.Collections.Generic;
using BioNex.Shared.DeviceInterfaces;

namespace BioNex.LiquidLevelDevice
{
    /// <summary>
    /// Interaction logic for VolumeMapPanel.xaml
    /// </summary>
    public partial class VolumeMapPanel : UserControl
    {
        public VolumeMapPanel()
        {
            InitializeComponent();
            DataContext = this;

            AddVolumeMapCommand = new RelayCommand(ExecuteAddVolumeMap, CanExecuteAddVolumeMap);
            DeleteVolumeMapCommand = new RelayCommand(ExecuteDeleteVolumeMap);
            AbortScanCommand = new RelayCommand(AbortScan);
            
            Graph.XLegend = "height";
            Graph.YLegend = "volume";
        }

        public RelayCommand AddVolumeMapCommand { get; private set; }
        public RelayCommand DeleteVolumeMapCommand { get; private set; }
        public RelayCommand AbortScanCommand { get; private set; }

        ILLSensorPlugin _plugin;
        DeviceInterface _device;
        public DeviceInterface Plugin
        {
            set { _device = value; _plugin = (ILLSensorPlugin)value; SetModel(); }
        }

        ILLSensorModel _model;
        private void SetModel()
        {
            _model = _plugin.Model;
            _model.LabwareDatabase.LabwareChanged += new EventHandler(LabwareDatabase_LabwareChanged);
            _database = _model.VolumeMapDatabase;

            LabwareCombo.ItemsSource = _model.LabwareDatabase.GetLabwareNames();

            LabwareSensitivityCombo.Items.Add("A : 3 to 150 mm (tubes, >8.5 mm diameter)");
            LabwareSensitivityCombo.Items.Add("B : 3 to 110 mm (96 well plates, 7 to 8.5 mm diameter)");
            LabwareSensitivityCombo.Items.Add("C : 3 to 70 mm (96 well plates, 5 to 7 mm diameter)");
            LabwareSensitivityCombo.Items.Add("D : 3 to 30 mm (384 well plates, <5 mm diameter)");

            for (int i = 0; i < _model.Properties.GetInt(LLProperties.MaxFitOrder); ++i)
                DataSetFitOrder.Items.Add(string.Format("{0}", i + 1));
            DataSetFitOrder.SelectedIndex = 0;

            LabwareCombo.SelectionChanged += LabwareCombo_SelectionChanged;
            LabwareSensitivityCombo.SelectionChanged += LabwareSensitivityCombo_SelectionChanged;
            LabwareCaptureOffset.TextChanged += LabwareCaptureOffset_TextChanged;

            DataSetCombo.SelectionChanged += DataSetCombo_SelectionChanged;
            DataSetFitOrder.SelectionChanged += DataSetFitOrder_SelectionChanged;
            DataSetEnabled.Checked += DataSetEnabled_Checked;
            DataSetEnabled.Unchecked += DataSetEnabled_Unchecked;
            DataSetMinVolume.TextChanged += DataSetMinVolume_TextChanged;
            DataSetMaxVolume.TextChanged += DataSetMaxVolume_TextChanged;

            LabwareCombo.SelectedIndex = 0;
        }

        void LabwareDatabase_LabwareChanged(object sender, EventArgs e)
        {
            Action action = () =>
            {
                LabwareCombo.ItemsSource = _model.LabwareDatabase.GetLabwareNames();
                LabwareCombo.SelectedIndex = 0;
                CommandManager.InvalidateRequerySuggested();
            };
            if (Dispatcher.CheckAccess()) action(); else Dispatcher.Invoke(action);
        }

        void LabwareCombo_SelectionChanged(object sender, EventArgs e)
        {
            FillDataSetCombo();
            UpdateLabwareDetails();
        }

        void UpdateLabwareDetails()
        {
            // set content of sensitivity and capture offset boxes
            var labware_name = (string)LabwareCombo.SelectedItem;
            var details = _database.GetLabwareDetails(labware_name);

            // turn off event notification
            LabwareSensitivityCombo.SelectionChanged -= LabwareSensitivityCombo_SelectionChanged;
            LabwareCaptureOffset.TextChanged -= LabwareCaptureOffset_TextChanged;

            var index = System.Text.Encoding.ASCII.GetBytes(details.Sensitivity)[0] - System.Text.Encoding.ASCII.GetBytes("A")[0];
            LabwareSensitivityCombo.SelectedIndex = index;
            LabwareCaptureOffset.Text = string.Format("{0:0.00}", details.CaptureOffset);

            // turn events back on
            LabwareSensitivityCombo.SelectionChanged += LabwareSensitivityCombo_SelectionChanged;
            LabwareCaptureOffset.TextChanged += LabwareCaptureOffset_TextChanged;
        }

        void LabwareSensitivityCombo_SelectionChanged(object sender, EventArgs e)
        {
            var labware_name = (string)LabwareCombo.SelectedItem;
            var details = _database.GetLabwareDetails(labware_name);
            var index = LabwareSensitivityCombo.SelectedIndex;
            var ascii = new byte[] { (byte)(System.Text.Encoding.ASCII.GetBytes("A")[0] + index) };
            details.Sensitivity = System.Text.Encoding.UTF8.GetString(ascii, 0, 1);
            _database.WriteLabwareDetails(labware_name, details);
        }

        void LabwareCaptureOffset_TextChanged(object sender, EventArgs e)
        {
            var labware_name = (string)LabwareCombo.SelectedItem;
            var details = _database.GetLabwareDetails(labware_name);
            double value = 0.0;
            if (!double.TryParse(LabwareCaptureOffset.Text, out value))
                return;

            details.CaptureOffset = value;
            _database.WriteLabwareDetails(labware_name, details);
        }

        void DataSetCombo_SelectionChanged(object sender, EventArgs e)
        {
            UpdateDataSetComboData();
        }

        void DataSetFitOrder_SelectionChanged(object sender, EventArgs e)
        {
            FitOrderChanged();
        }

        void DataSetEnabled_Checked(object sender, EventArgs e)
        {
            DataSetEnabledChanged(true);
        }
        void DataSetEnabled_Unchecked(object sender, EventArgs e)
        {
            DataSetEnabledChanged(false);
        }

        void DataSetMinVolume_TextChanged(object sender, EventArgs e)
        {
            DataSetMinVolumeChanged();
            FitOrderChanged(); // so we get a redraw w/ the new min line
        }

        void DataSetMaxVolume_TextChanged(object sender, EventArgs e)
        {
            DataSetMaxVolumeChanged();
            FitOrderChanged(); // so we get a redraw w/ the new max line
        }

        ILiquidLevelVolumeMapDatabase _database;

        void DisableDataSetUI()
        {
            DataSetMinVolume.Text = "0";
            DataSetMaxVolume.Text = "100";
            DataSetCombo.IsEnabled = false;
            DataSetMinVolume.IsEnabled = false;
            DataSetMaxVolume.IsEnabled = false;
            DataSetEnabled.IsEnabled = false;
            DataSetFitOrder.IsEnabled = false;
            DataSetDelete.IsEnabled = false;
        }

        void EnableDataSetUI()
        {
            DataSetMinVolume.IsEnabled = true;
            DataSetMaxVolume.IsEnabled = true;
            DataSetEnabled.IsEnabled = true;
            DataSetFitOrder.IsEnabled = true;
            DataSetDelete.IsEnabled = true;
        }

        void FillDataSetCombo()
        {
            DataSetCombo.Items.Clear();

            var labware_name = (string)LabwareCombo.SelectedItem;

            var data = _database.GetMapIDsForLabware(labware_name);
            if (data.Count == 0)
            {
                DisableDataSetUI();
                return;
            }
            foreach (var map_id in data)
                DataSetCombo.Items.Add(map_id.ToString());
            DataSetCombo.SelectedIndex = 0;
            DataSetCombo.IsEnabled = true;
        }

        void UpdateDataSetComboData()
        {
            // get the data associated with this selection, then
            // (5) fill in all the UI elements
            
            // as side effect of filling in "fit_order" field:
            // (6) run fit, graph data
            // (7) save fit data to database

            var map_id = Convert.ToInt32(DataSetCombo.SelectedItem);
            var min_volume = _database.GetMinVolumeForMap(map_id);
            var max_volume = _database.GetMaxVolumeForMap(map_id);
            var enabled = _database.GetEnabledForMap(map_id);
            var fit_order = _database.GetFitOrderForMap(map_id);

            DataSetMinVolume.Text = min_volume.ToString();
            DataSetMaxVolume.Text = max_volume.ToString();
            DataSetEnabled.IsChecked = enabled;
            DataSetFitOrder.SelectedItem = fit_order.ToString();

            EnableDataSetUI();

            Graph.ClearColors(); // pick new colors for new data set

            FitOrderChanged();
        }

        void FitOrderChanged()
        {
            // fit order changed, 
            // (6) run fit, graph data
            // (7) save fit data to database

            // (a) get capture details

            Graph.Clear();
            Correlation.Text = "Correlation: 0.00%";

            var item = DataSetCombo.SelectedItem;
            if (item == null)
            {
                Graph.Refresh();
                return;
            }

            var map_id = Convert.ToInt32(item);
            var min_volume = _database.GetMinVolumeForMap(map_id);
            var max_volume = _database.GetMaxVolumeForMap(map_id);
            var details = _database.GetCaptureDetailsForMap(map_id);
            details = details.OrderBy(x => x.Volume).ToList();     // get the data in an order that makes sense for graphing 
            var xs = details.Select(m => m.Measurement).ToArray();
            var ys = details.Select(m => m.Volume).ToArray();

            var fit_details = details.Where(m => m.Volume >= min_volume && m.Volume <= max_volume);
            var fit_xs = fit_details.Select(m => m.Measurement).ToArray();
            var fit_ys = fit_details.Select(m => m.Volume).ToArray();

            var order = Convert.ToInt32(DataSetFitOrder.SelectedItem);
            
            var fit = new PolynomialRegression(fit_xs, fit_ys, order); 
            var C = fit.Coefficients;
            Correlation.Text = string.Format("Correlation: {0:0.00}%", 100.0*fit.Correlation);

            // Write coefficients & fit_order to database
            _database.WriteMapCoefficients(map_id, C.ToList());

            // get bounds so we can show min / max lines without messing up the auto scale
            var min_y = min_volume;
            var max_y = max_volume;
            var min_x = xs.Min();
            var max_x = xs.Max();

            // Graph the raw data
            for (int i = 0; i < details.Count; ++i)
                Graph.AddPoint(xs[i], ys[i], "Measurement vs Volume");

            // graph the fit curve by walking from min to max in small increments of X
            var num_points = 12;
            for (int i = 0; i < num_points; ++i)
            {
                var x = min_x + i * (max_x - min_x) / (num_points-1);
                var y = fit.FitPoint(x);
                Graph.AddPoint(x, y, "Fit Measurement vs Volume");
            }
            
            // graph a min & max line
            min_x = Math.Min(xs.Min(), min_x);
            max_x = Math.Max(xs.Max(), max_x);
            Graph.AddPoint(min_x, min_y, "Min");
            Graph.AddPoint(max_x, min_y, "Min");
            Graph.AddPoint(min_x, max_y, "Max");
            Graph.AddPoint(max_x, max_y, "Max");

            Graph.Refresh();            
        }

        bool CanExecuteAddVolumeMap()
        {
            return _model != null && _model.Connected;
        }

        void ExecuteAddVolumeMap()
        {
            var labware_name = (string)LabwareCombo.SelectedItem;
            int columns;
            int rows;
            double col_spacing;
            double row_spacing;
            double thickness;
            double radius;
            _model.GetLabwareData(labware_name, out columns, out rows, out col_spacing, out row_spacing, out thickness, out radius);

            var wizard = new VolumeMapWizard(Window.GetWindow(this), columns);
            wizard.ShowDialog();
            if (wizard.Cancelled)
                return;

            // Launch a thread to run the scan
            Capture(labware_name, wizard.Map);
        }

        void ExecuteDeleteVolumeMap()
        {
            var labware_name = (string)LabwareCombo.SelectedItem;
            var map_id = Convert.ToInt32(DataSetCombo.SelectedItem);

            // (1) prompt to make sure
            var msg = string.Format("Delete volume map record '{0}' for labware '{1}'\n\nAre you sure you want to delete this record?  This cannot be undone.", map_id, labware_name);
            var result = MessageBox.Show(msg, "Delete Volume Map Record", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
                return;

            // (2) delete database records
            _database.DeleteVolumeMap(map_id);

            // (3) delete record frmo Data combobox, select value above this one --> this should have the side effect of resetting the UI
            var selected_index = DataSetCombo.SelectedIndex;
            DataSetCombo.Items.Remove(map_id.ToString());
            if (DataSetCombo.Items.Count == 0)
            {
                DisableDataSetUI();
                Graph.Clear();
                Graph.Refresh();
                return;
            }
            DataSetCombo.SelectedIndex = Math.Max(0, selected_index - 1);
        }

        void DataSetEnabledChanged(bool enabled)
        {
            var map_id = Convert.ToInt32(DataSetCombo.SelectedItem);
            _database.WriteMapEnabled(map_id, enabled);
        }

        void DataSetMinVolumeChanged()
        {
            var map_id = Convert.ToInt32(DataSetCombo.SelectedItem);
            double min_volume = 0;
            if (!double.TryParse(DataSetMinVolume.Text, out min_volume))
                return;
            _database.WriteMapMinVolume(map_id, min_volume);
        }

        void DataSetMaxVolumeChanged()
        {
            var map_id = Convert.ToInt32(DataSetCombo.SelectedItem);
            double max_volume = 0;
            if (!double.TryParse(DataSetMaxVolume.Text, out max_volume))
                return;
            _database.WriteMapMaxVolume(map_id, max_volume);
        }

        void Capture(string labware_name, MapDetails[] map)
        {
            AbortScanButton.Visibility = Visibility.Visible;
#if true
            var thread = new Thread(() =>
            {
                List<Averages> averages = _plugin.Capture(labware_name);
                Dispatcher.Invoke(new Action(() => { CaptureComplete(labware_name, map, averages); }));
            });
            thread.Start();
#else
            // FAKE IT 
            var data = new SortedDictionary<Coord,List<Measurement>>();
            var rand = new Random();
            var multiplier = 100.0;// _plugin.IsCalibrated ? 100.0 : 4096.0;
            for (int i = 0; i < 12; ++i)
                for (int j = 0; j < 8; ++j)
                    for (int k = 0; k < 3; ++k)
                    {
                        var value = (rand.NextDouble() * multiplier * 2.0) - (multiplier / 10.0);
                        var measure = new Measurement(j, 0, i, -j * 9.0, -i * 9.0, value);
                        var hash = new Coord(measure.channel, measure.row, measure.column);
                        List<Measurement> values;
                        if (!data.ContainsKey(hash))
                        {
                            values = new List<Measurement>();
                            data[hash] = values;
                        }
                        else
                            values = data[hash];
                        values.Add(measure);
                    }
            
            var averages = new List<Averages>();
            foreach (var well in data.Keys)
                averages.Add(_plugin.Model.CalculateWellAverages(data[well]));
            CaptureComplete(labware_name, map, averages);
#endif
        }

        void AbortScan()
        {
            AbortScanButton.Visibility = Visibility.Collapsed;
            _device.Abort();
        }

        void CaptureComplete(string labware_name, MapDetails[] map, List<Averages> averages)
        {
            AbortScanButton.Visibility = Visibility.Collapsed;

            var details = map;

            // (1) run scan, generating a measurement data-point for each column of the target labware

            // averages is a list, ordered by well of average measured value for that well
            // turn averages into average measurement per column
            var counts = new SortedDictionary<int, int>();
            var scan_results = new SortedDictionary<int, double>();
            foreach (var avg in averages)
            {
                var col = avg.Column;
                if (!scan_results.ContainsKey(col))
                {
                    scan_results[col] = 0;
                    counts[col] = 0;
                }
                scan_results[avg.Column] += avg.Average;
                counts[avg.Column] += 1;
            }
            foreach (var column in counts.Keys)
            {
                scan_results[column] /= counts[column];
            }

            if (scan_results.Count < details.Length)
            {
                MessageBox.Show(string.Format("Something went wrong during the scan, I was expecting {0} columns, but received {1} instead.\n\nAborting the process.", details.Length, scan_results.Count));
                return;
            }

            // (2) create record for volume_map_detail & N capture_details w/ default values
            var map_id = _database.CreateVolumeMap(labware_name);

            // (3) save acquired data / reference volume pairs to database
            foreach (var detail in details)
                _database.AddCaptureDetail(map_id, detail.Column - 1, detail.Volume, scan_results[detail.Column - 1]);

            // (4) update Data combobox, selecting the most recently added entry --> this has the following side effects
            // (5) fill in all the UI elements
            // (6) run fit, graph data
            // (7) save fit data to database
            //
            DataSetCombo.Items.Add(map_id.ToString());
            DataSetCombo.SelectedIndex = DataSetCombo.Items.Count - 1;
            DataSetCombo.IsEnabled = true;

            var volumes = map.Select(m => m.Volume).ToArray();
            var min_volume = volumes.Min();
            var max_volume = volumes.Max();
            DataSetMinVolume.Text = min_volume.ToString();
            DataSetMaxVolume.Text = max_volume.ToString();
            DataSetEnabled.IsChecked = true;
        }

    }
}
