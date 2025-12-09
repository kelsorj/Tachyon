using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using BioNex.Shared.DeviceInterfaces;
using GalaSoft.MvvmLight.Command;
using log4net;
using System.Diagnostics;

namespace BioNex.LiquidLevelDevice
{
    /// <summary>
    /// Interaction logic for SetupAndTesting.xaml
    /// </summary>
    public partial class SetupAndTestingPanel : UserControl
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(SetupAndTestingPanel));

        public SetupAndTestingPanel()
        {
            InitializeComponent();
            DataContext = this;
            this.Dispatcher.ShutdownStarted += UserControl_Shutdown;

            StartCycleCommand = new RelayCommand(StartCycle, CanExecuteStartCycle);
            StopCycleCommand = new RelayCommand(StopCycle, CanExecuteStopCycle);
            CalibrateCommand = new RelayCommand(Calibrate, CanExecuteCalibrate);
            CaptureCommand = new RelayCommand(Capture, CanExecuteCapture);
            HiResScanCommand = new RelayCommand(ExecuteHiResScan, CanExecuteHiResScan);
            FastHiResScanCommand = new RelayCommand(ExecuteFastHiResScan, CanExecuteFastHiResScan);
            XYAlignmentCommand = new RelayCommand(ExecuteXYAlignment, CanExecuteXYAlignment);
            ZYAlignmentCommand = new RelayCommand(ExecuteZYAlignment, CanExecuteZYAlignment);
            XArcCorrectionCommand = new RelayCommand(ExecuteXArcCorrection, CanExecuteXArcCorrection);
            LocateTeachpointCommand = new RelayCommand(LocateTeachpoint, CanExecuteLocateTeachpoint);
            CaptureVisualizationCommand = new RelayCommand(ExecuteCaptureVisualization);

            CaptureCycles = 1;
        }

        void UserControl_Shutdown(object sender, EventArgs e)
        {

            _aborted = true;
            _device.Abort();
        }

        public RelayCommand StartCycleCommand { get; private set; }
        public RelayCommand StopCycleCommand { get; private set; }
        public RelayCommand CalibrateCommand { get; private set; }
        public RelayCommand CaptureCommand { get; private set; }
        public RelayCommand HiResScanCommand { get; private set; }
        public RelayCommand FastHiResScanCommand { get; private set; }
        public RelayCommand XYAlignmentCommand { get; private set; }
        public RelayCommand ZYAlignmentCommand { get; private set; }
        public RelayCommand XArcCorrectionCommand { get; private set; }
        public RelayCommand LocateTeachpointCommand { get; private set; }
        public RelayCommand CaptureVisualizationCommand { get; private set; }

        public int CaptureCycles { get; set; }
        public double CaptureOffset { get { return _model.Properties.GetDouble(LLProperties.CaptureOffset); } set { _model.Properties[LLProperties.CaptureOffset] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public int SamplesPerWell { get { return _model.Properties.GetInt(LLProperties.SamplesPerWell); } set { _model.Properties[LLProperties.SamplesPerWell] = value.ToString(); _model.FireSavePropertiesEvent(); RefreshVisualization(); } }
        public double CaptureRadius { get { return _model.Properties.GetDouble(LLProperties.CaptureRadiusStart); } set { _model.Properties[LLProperties.CaptureRadiusStart] = value.ToString(); _model.FireSavePropertiesEvent(); RefreshVisualization(); } }
        public double RejectionRadius { get { return _model.Properties.GetDouble(LLProperties.CaptureRejectionRadius); } set { _model.Properties[LLProperties.CaptureRejectionRadius] = value.ToString(); _model.FireSavePropertiesEvent(); RefreshVisualization(); } }
        public bool Disable3D { get { return _model.Properties.GetBool(LLProperties.Disable3D); } set { _model.Properties[LLProperties.Disable3D] = value.ToString(); _model.FireSavePropertiesEvent(); } }

        public double CalibrationOffsetY { get { return _model.Properties.GetDouble(LLProperties.CalibrationOffsetY); } set { _model.Properties[LLProperties.CalibrationOffsetY] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public double CalibrationSamples { get { return _model.Properties.GetDouble(LLProperties.CalibrationSamples); } set { _model.Properties[LLProperties.CalibrationSamples] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public double CalibrationPoints { get { return _model.Properties.GetDouble(LLProperties.CalibrationPoints); } set { _model.Properties[LLProperties.CalibrationPoints] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public double CalibrationMaxZA { get { return _model.Properties.GetDouble(LLProperties.CalibrationMaxZA); } set { _model.Properties[LLProperties.CalibrationMaxZA] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public double CalibrationMaxZB { get { return _model.Properties.GetDouble(LLProperties.CalibrationMaxZB); } set { _model.Properties[LLProperties.CalibrationMaxZB] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public double CalibrationMaxZC { get { return _model.Properties.GetDouble(LLProperties.CalibrationMaxZC); } set { _model.Properties[LLProperties.CalibrationMaxZC] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public double CalibrationMaxZD { get { return _model.Properties.GetDouble(LLProperties.CalibrationMaxZD); } set { _model.Properties[LLProperties.CalibrationMaxZD] = value.ToString(); _model.FireSavePropertiesEvent(); } }

        public double HiResMinX { get { return _model.Properties.GetDouble(LLProperties.HiResMinX); } set { _model.Properties[LLProperties.HiResMinX] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public double HiResMaxX { get { return _model.Properties.GetDouble(LLProperties.HiResMaxX); } set { _model.Properties[LLProperties.HiResMaxX] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public double HiResStepX { get { return _model.Properties.GetDouble(LLProperties.HiResStepX); } set { _model.Properties[LLProperties.HiResStepX] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public double HiResMinY { get { return _model.Properties.GetDouble(LLProperties.HiResMinY); } set { _model.Properties[LLProperties.HiResMinY] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public double HiResMaxY { get { return _model.Properties.GetDouble(LLProperties.HiResMaxY); } set { _model.Properties[LLProperties.HiResMaxY] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public double HiResStepY { get { return _model.Properties.GetDouble(LLProperties.HiResStepY); } set { _model.Properties[LLProperties.HiResStepY] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public double HiResMinC { get { return _model.Properties.GetDouble(LLProperties.HiResMinC); } set { _model.Properties[LLProperties.HiResMinC] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public double HiResMaxC { get { return _model.Properties.GetDouble(LLProperties.HiResMaxC); } set { _model.Properties[LLProperties.HiResMaxC] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public double HiResStepC { get { return _model.Properties.GetDouble(LLProperties.HiResStepC); } set { _model.Properties[LLProperties.HiResStepC] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public double HiResFilterThreshold { get { return _model.Properties.GetDouble(LLProperties.HiResFilterThreshold); } set { _model.Properties[LLProperties.HiResFilterThreshold] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public bool HiResShowGraphs { get { return _model.Properties.GetBool(LLProperties.HiResShowGraphs); } set { _model.Properties[LLProperties.HiResShowGraphs] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public bool HiResShowWellCenters { get { return _model.Properties.GetBool(LLProperties.HiResShowWellCenters); } set { _model.Properties[LLProperties.HiResShowWellCenters] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public bool HiResFloorToZero { get { return _model.Properties.GetBool(LLProperties.HiResFloorToZero); } set { _model.Properties[LLProperties.HiResFloorToZero] = value.ToString(); _model.FireSavePropertiesEvent(); } }

        public bool HiResResetSlope { get { return _hires_reset_xy_checked; } set { _hires_reset_xy_checked = value; } }
        public bool HiResResetSlopeZY { get { return _hires_reset_zy_checked; } set { _hires_reset_zy_checked = value; } }
        public bool HiResResetXArcCorrection { get { return _hires_reset_xarc_checked; } set { _hires_reset_xarc_checked = value; } }
        
        public double HiResFastScanVelocity { get { return _model.Properties.GetDouble(LLProperties.HiResFastScanVelocity); } set { _model.Properties[LLProperties.HiResFastScanVelocity] = value.ToString(); _model.FireSavePropertiesEvent(); } }


        bool _hires_reset_xy_checked = true; // checkbox state
        bool _hires_reset_zy_checked = true; // checkbox state
        bool _hires_reset_xarc_checked = true; // checkbox state

        bool _busy = false;

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

            LabwareCombo.ItemsSource = _model.LabwareDatabase.GetLabwareNames();
            LabwareCombo.SelectedIndex = 0;

            _model.DisconnectingEvent += Disconnecting;
        }

        void Disconnecting(object sender, int index)
        {
            StopCycle();
        }

        void LabwareDatabase_LabwareChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                LabwareCombo.ItemsSource = _model.LabwareDatabase.GetLabwareNames();
                LabwareCombo.SelectedIndex = 0;
                CommandManager.InvalidateRequerySuggested();
            }));
        }

        Thread _cycling_thread;
        bool _stop_cycling = false;
        void StartCycle()
        {
            StopCycle();
            _busy = true;
            _stop_cycling = false;
            _cycling_thread = new Thread(() =>
            {
                _model.StartPeriodicRead();
                while (!_stop_cycling)
                {
                    var start = Stopwatch.StartNew();
                    try
                    {
                        _model.ReadPeriodic();
                        _log.Debug(string.Format("Sensor read cycle completed in {0} seconds", start.Elapsed));
                    }
                    finally
                    {
                        start.Stop();
                    }
                }
                _model.StopPeriodicRead();
            });
            _cycling_thread.Start();
        }
        bool CanExecuteStartCycle()
        {
            return _model != null && _model.SensorsConnected && !_busy;

        }
        void StopCycle()
        {
            lock (this) // if we're stopped due to a disconnect, we get called by every sensor from the disconnect thread -- this lock prevents them from all joining the cycle thread
            {
                if (_cycling_thread == null)
                    return;
                _stop_cycling = true;
                _cycling_thread.Join();
                _cycling_thread = null;
                _busy = false;
            }
        }
        bool CanExecuteStopCycle()
        {
            return _cycling_thread != null;
        }

        Thread _calibration_thread;
        void Calibrate()
        {
            if (_calibration_thread == null)
            {
                _busy = true;
                CalibrateButton.Content = "Abort Calibration";
                _calibration_thread = new Thread(() =>
                {
                    _plugin.Calibrate();
                    Dispatcher.Invoke(new Action(() => { CalibrateButton.Content = "Calibrate"; CommandManager.InvalidateRequerySuggested(); }));
                    _calibration_thread = null;
                    _busy = false;
                });
                _calibration_thread.Start();
            }
            else
                _device.Abort();
        }
        bool CanExecuteCalibrate()
        {
            return _model != null && _model.Connected && (!_busy || _calibration_thread != null);
        }

        Thread _capture_thread;
        bool _aborted;
        void Capture()
        {
            if (_capture_thread == null)
            {
                _aborted = false;
                _busy = true;
                CaptureButton.Content = "Abort Capture";

                var labware_name = (string)LabwareCombo.SelectedItem;
                _capture_thread = new Thread(() =>
                {
                    uint capture_cycles = (uint)CaptureCycles;
                    while ((capture_cycles--) > 0 && !_aborted)
                    {
                        _plugin.Capture(labware_name);
                    }
                    Dispatcher.Invoke(new Action(() => { CaptureButton.Content = "Capture"; CommandManager.InvalidateRequerySuggested(); }));
                    _capture_thread = null;
                    _busy = false;
                });
                _capture_thread.Start();
            }
            else
            {
                _aborted = true;
                _device.Abort();
            }
        }
        bool CanExecuteCapture()
        {
            return _model != null && _model.Connected && (!_busy || _capture_thread != null);
        }

        void ExecuteHiResScan()
        {
            _hires_button = HiResScanButton;
            HiResScan("High Resolution Scan", "Abort High Resolution Scan");
        }

        void ExecuteFastHiResScan()
        {
            _hires_button = FastHiResScanButton;
            HiResScan("Fast High Resolution Scan", "Abort Fast High Resolution Scan");
        }

        Button _hires_button;
        bool CanExecuteHiResScan()
        {
            return _model != null && _model.Connected && (!_busy || _hires_button == HiResScanButton);
        }

        bool CanExecuteFastHiResScan()
        {
            return _model != null && _model.Connected && (!_busy || _hires_button == FastHiResScanButton);
        }

        Thread _hires_scan_thread;
        void HiResScan(string standard_text, string abort_text)
        {
            if (_hires_scan_thread == null)
            {
                _busy = true;
                _aborted = false;
                _hires_button.Content = abort_text;

                string labware_name = labware_name = (string)LabwareCombo.SelectedItem;
                bool fast_scan = _hires_button == FastHiResScanButton;

                _hires_scan_thread = new Thread(() =>
                {
                    IDictionary<Coord, List<Measurement>> measurements = null;

                    uint capture_cycles = (uint)CaptureCycles;
                    while ((capture_cycles--) > 0 && !_aborted)
                    {
                        measurements = _plugin.HiResScan(fast_scan, labware_name);
                    }

                    Dispatcher.Invoke(new Action(() => { _hires_button.Content = standard_text; CommandManager.InvalidateRequerySuggested(); }));
                    _hires_scan_thread = null;
                    _hires_button = null;
                    _busy = false;
                });
                _hires_scan_thread.Start();
            }
            else
            {
                _aborted = true;
                _device.Abort();
            }
        }

        Thread _locate_teachpoint_thread;
        void LocateTeachpoint()
        {
            if (_locate_teachpoint_thread == null)
            {
                _busy = true;
                _aborted = false;
                LocateTeachpointButton.Content = "Abort Locate Teachpoint";

                _locate_teachpoint_thread = new Thread(() =>
                {
                    IDictionary<Coord, List<Measurement>>[] measurements = null;
                    measurements = _plugin.LocateTeachpoint();

                    var x_measurements = measurements != null ? measurements[0] : null;
                    var y_measurements = measurements != null ? measurements[1] : null;

                    if (!_aborted && measurements != null)
                    {
                        LLSLocateTeachpointAlgorithm locator = null;
                        string message;
                        var button_type = MessageBoxButton.YesNo;

                        if (x_measurements.Count == 0 || y_measurements.Count == 0)
                        {
                            message = "Error, too few valid data points to find teachpoint location; change starting position and try again.";
                            button_type = MessageBoxButton.OK;
                        }
                        else
                        {
                            const double JIG_X_SPACING = 9.0;
                            double JIG_Y_OFFSET = _model.Properties.GetDouble(LLProperties.LocateTeachpointFeatureToFeatureY); 
                            locator = new LLSLocateTeachpointAlgorithm(JIG_X_SPACING, JIG_Y_OFFSET, x_measurements, y_measurements, HiResFilterThreshold, HiResShowGraphs, Dispatcher);

                            // bail if there is a NaN in either offset or in any deviation
                            if(!locator.IsAcceptable)
                            {
                                message = "Error, could not find teachpoint location from this starting position; change starting position and try again.";
                                button_type = MessageBoxButton.OK;
                            }
                            else                            
                                message = string.Format("Update teachpoint?\n\nX: {0}  Y: {1}", locator.XOffset, locator.YOffset);
                        }

                        Dispatcher.Invoke(new Action(() =>
                        {
                            if (MessageBox.Show(message, "Locate teachpoint results", button_type) == MessageBoxResult.Yes)
                            {
                                _model.OffsetTeachpoint(locator.XOffset, locator.YOffset);
                                _model.SaveSensorDeviations(locator.XDeviation, locator.YDeviation);
                                SaveVisualizationPreviousValues();
                                RefreshVisualization();
                            }
                        }));
                    }
                    Dispatcher.Invoke(new Action(() => { LocateTeachpointButton.Content = "Locate Teachpoint"; CommandManager.InvalidateRequerySuggested(); }));
                    _locate_teachpoint_thread = null;
                    _busy = false;
                });
                _locate_teachpoint_thread.Start();
            }
            else
            {
                _aborted = true;
                _device.Abort();
            }
        }
        bool CanExecuteLocateTeachpoint()
        {
            return _model != null && _model.Connected && (!_busy || _locate_teachpoint_thread != null);
        }


        Thread _xy_alignment_thread;
        void ExecuteXYAlignment()
        {
            if (_xy_alignment_thread == null)
            {
                _busy = true;
                _aborted = false;
                XYAlignmentButton.Content = "Abort X -> Y Alignment Scan";

                _xy_alignment_thread = new Thread(() =>
                {
                    var original_xyslope = _model.XYSlope;
                    IDictionary<Coord,List<Measurement>> measurements = null;
                    measurements = _plugin.XYAlignmentScan(_hires_reset_xy_checked);

                    if (!_aborted && measurements != null)
                    {
                        var aligner = new LLSXYAlignmentAlgorithm(measurements, HiResFilterThreshold, HiResShowGraphs, Dispatcher);
                        string message;
                        var button_type = MessageBoxButton.YesNo;
                        if (double.IsNaN(aligner.Slope))
                        {
                            message = "Error, could not fit a curve to points during alignment, restoring original slope; Probably a bad teachpoint / could not locate alignment features in expected position.";
                            button_type = MessageBoxButton.OK;
                        }
                        else
                            message = string.Format("Prior to alignment: y = {0:0.0000}x {1:+ 0.0000;- 0.0000} ({2:0.0000} degrees)\n\nafter alignment: y = {3:0.0000}x {4:+ 0.0000;- 0.0000} ({5:0.0000} degrees)\n\nUse this new value?  Selecting NO will restore the original alignment."
                                , original_xyslope, 0.0, Math.Atan(original_xyslope) * 180.0 / Math.PI
                                , aligner.Slope, 0.0, Math.Atan(aligner.Slope) * 180.0 / Math.PI);

                        Dispatcher.Invoke(new Action(() =>
                        {
                            var result = MessageBox.Show(message, "Alignment results", button_type) == MessageBoxResult.Yes;
                            _model.XYSlope = result ? aligner.Slope : original_xyslope;
                        }));
                    }

                    Dispatcher.Invoke(new Action(() => { XYAlignmentButton.Content = "X -> Y Alignment Scan"; CommandManager.InvalidateRequerySuggested(); }));
                    _xy_alignment_thread = null;
                    _busy = false;
                });
                _xy_alignment_thread.Start();
            }
            else
            {
                _aborted = true;
                _device.Abort();
            }
        }

        bool CanExecuteXYAlignment()
        {
            return _model != null && _model.Connected && (!_busy || _xy_alignment_thread != null);
        }

        Thread _zy_alignment_thread;
        void ExecuteZYAlignment()
        {
            if (_zy_alignment_thread == null)
            {
                _busy = true;
                _aborted = false;
                ZYAlignmentButton.Content = "Abort Z -> Y Alignment Scan";

                _zy_alignment_thread = new Thread(() =>
                {
                    var original_zyslope = _model.ZYSlope;
                    IDictionary<Coord, List<Measurement>> measurements = null;
                    measurements = _plugin.ZYAlignmentScan(_hires_reset_zy_checked);

                    if (!_aborted && measurements != null)
                    {
                        var aligner = new LLSYZAlignmentAlgorithm(measurements, HiResFilterThreshold, HiResShowGraphs, Dispatcher);

                        string message;
                        var button_type = MessageBoxButton.YesNo;
                        if (double.IsNaN(aligner.Slope))
                        {
                            message = "Error, could not fit a curve to points during alignment, restoring original slope.";
                            button_type = MessageBoxButton.OK;
                        }
                        else
                            message = string.Format("Prior to alignment: y = {0:0.0000}z {1:+ 0.0000;- 0.0000} ({2:0.0000} degrees)\n\nafter alignment: y = {3:0.0000}z {4:+ 0.0000;- 0.0000} ({5:0.0000} degrees)\n\nUse this new value?  Selecting NO will restore the original alignment."
                                , original_zyslope, 0.0, Math.Atan(original_zyslope) * 180.0 / Math.PI
                                , aligner.Slope, 0.0, Math.Atan(aligner.Slope) * 180.0 / Math.PI);

                        Dispatcher.Invoke(new Action(() =>
                        {
                            var result = MessageBox.Show(message, "Alignment results", button_type) == MessageBoxResult.Yes;
                            _model.ZYSlope = result ? aligner.Slope : original_zyslope;
                        }));
                    }

                    Dispatcher.Invoke(new Action(() => { ZYAlignmentButton.Content = "Z -> Y Alignment Scan"; CommandManager.InvalidateRequerySuggested(); }));
                    _zy_alignment_thread = null;
                    _busy = false;
                });
                _zy_alignment_thread.Start();
            }
            else
            {
                _aborted = true;
                _device.Abort();
            }
        }

        bool CanExecuteZYAlignment()
        {
            return _model != null && _model.Connected && (!_busy || _zy_alignment_thread != null);
        }

        Thread _x_arc_correction_thread;
        void ExecuteXArcCorrection()
        {
            if (_x_arc_correction_thread == null)
            {
                _busy = true;
                _aborted = false;
                XArcCorrectionButton.Content = "Abort X Arc Correction Scan";

                _x_arc_correction_thread = new Thread(() =>
                {
                    var original_correction = _model.XArcCorrection;
                    IList<IDictionary<Coord, List<Measurement>>> measurements = null;
                    measurements = _plugin.XArcCorrectionScan(_hires_reset_xarc_checked); 

                    if (!_aborted && measurements != null)
                    {
                        var aligner = new LLSXArcCorrectionAlgorithm(measurements, HiResFilterThreshold, HiResShowGraphs, Dispatcher);
                        var C0 = original_correction;
                        var C = aligner.Fit.Coefficients;

                        string message;
                        var button_type = MessageBoxButton.YesNo;

                        if (double.IsNaN(C[0]) || double.IsNaN(C[1]) || double.IsNaN(C[2]))
                        {
                            message = "Error, could not fit a curve to points during alignment, restoring original slope; Probably a bad teachpoint / could not locate alignment features in expected position.";
                            button_type = MessageBoxButton.OK;
                        }
                        else
                            message = string.Format("Prior to correction: y = {0:0.0000}x^2 {1:+ 0.0000;- 0.0000}x {2:+ 0.0000;- 0.0000}\n\nafter correction: y = {3:0.0000}x^2 {4:+ 0.0000;- 0.0000}x {5:+ 0.0000;- 0.0000}\n\nUse this new value?  Selecting NO will restore the original correction."
                                , C0[2], C0[1], C0[0]
                                , C[2], C[1], C[0]); 

                        Dispatcher.Invoke(new Action(() =>
                        {
                            var result = MessageBox.Show(message, "X Arc Correction results", button_type) == MessageBoxResult.Yes;
                            _model.XArcCorrection = result ? new double[] { C[0], C[1], C[2] } : new double[] { C0[0], C0[1], C0[2] };
                        }));
                    }

                    Dispatcher.Invoke(new Action(() => { XArcCorrectionButton.Content = "X Arc Correction Scan"; CommandManager.InvalidateRequerySuggested(); }));
                    _x_arc_correction_thread = null;
                    _busy = false;
                });
                _x_arc_correction_thread.Start();
            }
            else
            {
                _aborted = true;
                _device.Abort();
            }
        }

        bool CanExecuteXArcCorrection()
        {
            return _model != null && _model.Connected && (!_busy || _x_arc_correction_thread != null);
        }
        CaptureRadiusVisualization _viz = null;
        void ExecuteCaptureVisualization()
        {
            if (_viz == null)
            {
                _viz = new CaptureRadiusVisualization(_model, System.Windows.Window.GetWindow(this));
                _viz.Closed += VisualizationClosed;
            }
            _viz.Show();
        }

        void VisualizationClosed(object sender, EventArgs e)
        {
            _viz = null;
        }

        void SaveVisualizationPreviousValues()
        {
            if (_viz == null)
                return;
            _viz.SavePrevValues();
        }

        void RefreshVisualization()
        {
            if (_viz == null)
                return;
            _viz.Draw();
        }
    }
}
