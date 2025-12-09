using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using BioNex.Shared.BioNexGuiControls;

namespace BioNex.LiquidLevelDevice
{
    /// <summary>
    /// Interaction logic for ScanProgressPanel.xaml
    /// </summary>
    public partial class ScanProgressPanel : UserControl
    {
        public ScanProgressPanel()
        {
            InitializeComponent();
            DataContext = this;

            _capture_progress_thread = new Thread(CaptureProgressThread);
            _capture_progress_thread.IsBackground = true;
            _capture_progress_thread.Start();
        }

        ILLSensorPlugin _plugin;
        public ILLSensorPlugin Plugin
        {
            set
            {
                _plugin = (ILLSensorPlugin)value;
                _plugin.CaptureStartEvent += CaptureStart;
                _plugin.CaptureProgressEvent += CaptureProgress;
                _plugin.CaptureStopEvent += CaptureStop;

                Graph3D.PlaneBorder = 4.5;
                Graph3D.ResetTransformEvent += Graph3D_ResetTransform;
                DoTransform();
            }
        }

        void Graph3D_ResetTransform(object sender, EventArgs e)
        {
            DoTransform();
        }
        void DoTransform()
        {
            Graph3D.RotateAroundAxis(new Vector3D(0, 0, 1), 90); // rotate around the Z
            Graph3D.RotateAroundAxis(new Vector3D(1, 0, 0), -45); // rotate around x
            Graph3D.Scale = 25.0; // scale should be automatic, based on a best fit somehow...
        }

        IDictionary<Coord, List<Measurement>> _measurements;
        double _column_width;
        int _last_column;
        int _last_row;
        double _column_spacing;
        double _row_spacing;
        double _well_radius;
        double _min_measured;
        double _max_measured;
        bool _summarize;
        bool _gradient = true;
        Random _rand = new Random();
        const double PLANE_THICKNESS = 0.1;
        
        void FakeCapture()
        {
            var thread = new System.Threading.Thread(() =>
            {
                CaptureStart(null, true, "");
                for (int i = 0; i < 12; ++i)
                {
                    var measurements = new List<Measurement>();
                    for (int j = 0; j < 8; ++j)
                        for (int k = 0; k < 3; ++k)
                        {
                            var multiplier = 10.0;
                            var value = (_rand.NextDouble() * multiplier * 2.0) - (multiplier / 10.0);
                            measurements.Add(new Measurement(j, 0, i, -j * 9.0, -i * 9.0, value));
                            System.Threading.Thread.Sleep(1);
                        }
                    CaptureProgress(null, measurements);
                }
                CaptureStop(null);
            });
            thread.IsBackground = true;
            thread.Start();
        }

        void CaptureStart(object sender, bool summarize, string labware_name)
        {
            int columns = 12;
            int rows = 8;
            _column_spacing = 9.0;
            _row_spacing = 9.0;
            double thickness = 15;
            _well_radius = 4.5;
            if( !string.IsNullOrEmpty(labware_name))
                _plugin.Model.GetLabwareData(labware_name, out columns, out rows, out _column_spacing, out _row_spacing, out thickness, out _well_radius);
            _column_width = columns <= 12 ? 6.0 : 3.0;

            _summarize = summarize;             // summarize == false for hi res scan
            _min_measured = double.MaxValue;
            _max_measured = double.MinValue;

            _batch = new List<Data3DGraphControl.GraphData>();

            _last_row = int.MaxValue;
            _last_column = int.MaxValue;

            _measurements = new SortedDictionary<Coord, List<Measurement>>();
            _recolor_list = new List<double>();
            Graph3D.PlaneThickness = PLANE_THICKNESS;
            Graph3D.Clear();
            //Graph3D.AddPoint(0.0, 0.0, 4.0); // zero marker
        }

        Thread _capture_progress_thread;
        List<Measurement> _capture_progress_queue = new List<Measurement>();
        bool _end_of_capture = false;
        AutoResetEvent _capture_event = new AutoResetEvent(false);
        
        void CaptureProgressThread()
        {
            while (true)
            {
                _capture_event.WaitOne(100);
                var end_of_capture = false;
                var work_list = new List<Measurement>();
                lock (_capture_progress_queue)
                {
                    work_list.AddRange(_capture_progress_queue);
                    _capture_progress_queue = new List<Measurement>();
                    end_of_capture = _end_of_capture;
                    _end_of_capture = false;
                }
                foreach (var value in work_list)
                {
                    var hash = new Coord(value.channel, value.row, value.column);
                    List<Measurement> values;
                    if (!_measurements.ContainsKey(hash))
                    {
                        values = new List<Measurement>();
                        _measurements[hash] = values;
                    }
                    else
                        values = _measurements[hash];
                    values.Add(value);

                    _min_measured = Math.Min(_min_measured, value.measured_value);
                    _max_measured = Math.Max(_max_measured, value.measured_value);

                    var pt_size = Math.Min(_plugin.Model.Properties.GetDouble(LLProperties.HiResStepX), _plugin.Model.Properties.GetDouble(LLProperties.HiResStepY));
                    var color = _gradient ? GetGradientColor(value.measured_value, 0xFF, _min_measured, _max_measured) : Color.FromArgb(0xFF, 0x7F, 0xFF, 0xD4);
                    var zero = _summarize ? value.measured_value <= 0.0 ? -PLANE_THICKNESS : 0.0 : value.measured_value - 0.1;// pt_size; // use min resolution instead of x/y resolution
                    var point = new Point3D(value.x, value.y, value.measured_value);
                    var mark = !_summarize;
                    var data = new Data3DGraphControl.GraphData(point, zero, mark, pt_size, color);

                    if (_summarize)
                    {
                        Graph3D.AddPoint(data);
                        _recolor_list.Add(value.measured_value);
                    }
                    else
                    {
                        _batch.Add(data);
                        _batch_size = Math.Max(8, work_list.Count);

                        if (_batch.Count == _batch_size)
                        {
                            Graph3D.AddPoints(_batch);
                            _recolor_list.AddRange(_batch.Select(p => p.point.Z));
                            _batch = new List<Data3DGraphControl.GraphData>();
                        }
                    }

                    // when we move onto a new column, draw the average value for the previous column
                    if (_last_column == int.MaxValue || _last_row == int.MaxValue)
                    {
                        _last_column = value.column;
                        _last_row = value.row;
                        continue;
                    }

                    if (value.column == _last_column && value.row == _last_row)
                        continue;


                    if (_summarize)
                        RenderColumn(_last_column, _last_row);
                    else if( _plugin.Model.Properties.GetBool(LLProperties.HiResShowWellCenters))
                        RenderWellCenters(_last_column, _last_row);

                    _last_column = value.column;
                    _last_row = value.row;

                    // if viewing gradient, reprocess gradient at the end of every column
                    if (_gradient)
                        ResetGraphColors();

                }

                if (end_of_capture)
                {
                    if (_summarize)
                        RenderColumn(_last_column, _last_row);
                    else if (_batch.Count > 0)
                    {
                        Graph3D.AddPoints(_batch);
                        _recolor_list.AddRange(_batch.Select(p => p.point.Z));
                        _batch = new List<Data3DGraphControl.GraphData>();
                    }
                    if (!_summarize && _plugin.Model.Properties.GetBool(LLProperties.HiResShowWellCenters))
                        RenderWellCenters(_last_column, _last_row);

                    if (_gradient)
                        ResetGraphColors();
                }
            }
        }

        List<Data3DGraphControl.GraphData> _batch;
        List<double> _recolor_list;
        int _batch_size = 8;

        void CaptureProgress(object sender, IList<Measurement> new_values)
        {
            if (_measurements == null)
                return;

            if (_plugin.Model.Properties.GetBool(LLProperties.Disable3D))
                return;

            lock (_capture_progress_queue)
            {
                _capture_progress_queue.AddRange(new_values);
                _capture_event.Set();
            }
        }

        void CaptureStop(object sender)
        {
            if (_measurements == null)
                return;

            if (_plugin.Model.Properties.GetBool(LLProperties.Disable3D))
                return;

            lock (_capture_progress_queue)
            {
                _end_of_capture = true;
                _capture_event.Set();
            }
        }

        void RenderColumn(int column, int row)
        {
            var points = new List<Data3DGraphControl.GraphData>();

            foreach (var well in _measurements.Keys)
            {
                if (well.column != column || well.row != row)
                    continue;

                var averages = _plugin.Model.CalculateWellAverages(_measurements[well]);

                if (averages.StandardDeviation != 0)
                {
                    // Add bar for standard deviation
                    var bottom = averages.Average - averages.StandardDeviation;
                    var top = averages.Average + averages.StandardDeviation;
                    var point = new Point3D(averages.XAverage, averages.YAverage, top);
                    points.Add( new Data3DGraphControl.GraphData( point, bottom, false, 0.75, Color.FromArgb(0xFF, 0xFF, 0x80, 0x6A)));
                    _recolor_list.Add(double.PositiveInfinity); // mark std_dev as pos_inf for recolor
                }

                // Add well average
                var is_A1 = well.channel == 0 && well.column == 0 && well.row == 0;

                var z = averages.Average;
                var eps = 0.001;

                var t = z < 0.0 ? -(PLANE_THICKNESS + eps) : z == 0.0 ? eps : z;
                var b = z < 0.0 ? z - (PLANE_THICKNESS + eps) : eps;
                
                var point2 = new Point3D(averages.XAverage, averages.YAverage, t);
                var color = _gradient ? GetGradientColor(z, 0x60, _min_measured, _max_measured) : Color.FromArgb(0x60, 0x7F, 0xFF, 0xD4);
                points.Add( new Data3DGraphControl.GraphData( point2, b, is_A1, _column_width, color)); 
                _recolor_list.Add(averages.Average);
            }
            Graph3D.AddPoints(points);
        }

        void RenderWellCenters(int column, int row)
        {
            var props = _plugin.Model.Properties;
            var points = new List<Data3DGraphControl.GraphData>();

            var t = 10.0;
            var b = 0.0;
            var pdx = _plugin.Model.PlateDX;
            var pdy = _plugin.Model.PlateDY;
            var cx = 4.5 - _well_radius - row * _row_spacing - pdx;
            var cy = 4.5 - _well_radius - column * _column_spacing - pdy;
            var sensor_spacing = 9.0;

            for (int i = 0; i < _plugin.Model.SensorCount; ++i)
            {
                var dx = 0.0;
                var dy = 0.0;
                if (!props.GetBool(LLProperties.CaptureSeekDeviation))
                {
                    dx = props.GetDouble(LLProperties.index(LLProperties.XDeviation, i));
                    dy = props.GetDouble(LLProperties.index(LLProperties.YDeviation, i));
                }

                // add correction stuff?
                var x = cx - dx - i * sensor_spacing;
                var y = cy - dy;
                points.Add(new Data3DGraphControl.GraphData(new Point3D(x, y, t), b, false, 0.1, Color.FromArgb(0xFF, 0xFF, 0x80, 0x6A))); 
                _recolor_list.Add(double.PositiveInfinity);
            }
            Graph3D.AddPoints(points);
        }

        void ResetGraphColors()
        {
            var colors = new List<Color>();
            foreach (var z in _recolor_list)
            {
                var color = _gradient ? 
                    double.IsPositiveInfinity(z) ? 
                        Color.FromArgb(0xFF, 0xFF, 0x80, 0x6A) : 
                        GetGradientColor(z, 0xFF, _min_measured, _max_measured) : 
                    Color.FromArgb(0xFF, 0x7F, 0xFF, 0xD4);
                colors.Add(color);
            }
            Graph3D.ReplaceColors(colors);
        }


        Color GetGradientColor(double value, byte alpha, double min, double max)
        {
            // GNUPLOT FORMULAS
            //  * there are 37 available rgb color mapping formulae:
            //     0: 0               1: 0.5             2: 1              
            //     3: x               4: x^2             5: x^3            
            //     6: x^4             7: sqrt(x)         8: sqrt(sqrt(x))  
            //     9: sin(90x)       10: cos(90x)       11: |x-0.5|        
            //    12: (2x-1)^2       13: sin(180x)      14: |cos(180x)|    
            //    15: sin(360x)      16: cos(360x)      17: |sin(360x)|    
            //    18: |cos(360x)|    19: |sin(720x)|    20: |cos(720x)|    
            //    21: 3x             22: 3x-1           23: 3x-2           
            //    24: |3x-1|         25: |3x-2|         26: (3x-1)/2       
            //    27: (3x-2)/2       28: |(3x-1)/2|     29: |(3x-2)/2|     
            //    30: x/0.32-0.78125 31: 2*x-0.84       32: 4x;1;-2x+1.84;x/0.08-11.5
            //    33: |2*x - 0.5|    34: 2*x            35: 2*x - 0.5      
            //    36: 2*x - 1        
            //  * negative numbers mean inverted=negative colour component
            //  * thus the ranges in `set pm3d rgbformulae' are -36..36
            //
            // default: 7, 5, 15
            // Green-Red-Violet: 3, 11, 6
            // Ocean: 23, 28, 3
            // Hot: 21, 22, 23
            // Color printable on gray: 30, 31, 32

            var range = max - min;
            var norm = range == 0.0 ? 0.0 : (value - min) / (range);

            // OCEAN 
            //double r = 3.0 * norm - 2.0;
            //double g = Math.Abs((3.0 * norm - 1.0) / 2.0);
            //double b = norm;

            //HOT
            //double r = 3.0 * norm;
            //double g = 3.0 * norm - 1.0;
            //double b = 3.0 * norm - 2.0;

            //RAINBOW
            double r = Math.Abs(2.0 * (1.0 - norm) - 0.5);
            double g = Math.Sin(Math.PI * (1.0 - norm));
            double b = Math.Cos(Math.PI / 2.0 * (1.0 - norm));

            // clamp
            r = Math.Max(0.0, Math.Min(1.0, r));
            g = Math.Max(0.0, Math.Min(1.0, g));
            b = Math.Max(0.0, Math.Min(1.0, b));

            return Color.FromArgb(alpha, (byte)(r * 0xff), (byte)(g * 0xff), (byte)(b * 0xff));
        }

        private void Graph3D_KeyUp(object sender, KeyEventArgs e)
        {
            bool control_down = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            bool alt_down = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
            bool shift_down = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            if (control_down)
                switch (e.Key)
                {
                    case Key.K:
                        if( alt_down && shift_down)
                            FakeCapture();
                        break;
                    case Key.G:
                        _gradient = !_gradient;
                        ResetGraphColors();
                        break;
                }
        }
    }
}
