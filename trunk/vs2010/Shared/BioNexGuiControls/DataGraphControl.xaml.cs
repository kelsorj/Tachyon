using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BioNex.Shared.BioNexGuiControls
{
    /// <summary>
    /// Interaction logic for DataGraphControl.xaml
    /// </summary>
    public partial class DataGraphControl : UserControl
    {
        public const int TOOLTIP_DURATION = int.MaxValue;
        const int MINOR_GRID_LINES = 10;
        const int MINOR_TICK_RATIO = 50;

        Size _size = new Size(200, 200);
        int _h_minor_tick_count = MINOR_GRID_LINES;
        int _v_minor_tick_count = MINOR_GRID_LINES;

        Dictionary<string, List<double>> _x_data = new Dictionary<string, List<double>>();
        Dictionary<string, List<double>> _y_data = new Dictionary<string, List<double>>();
        Dictionary<string, Color> _line_color = new Dictionary<string, Color>();
        Dictionary<string, Color> _square_color = new Dictionary<string, Color>();

        int _max_points = 0;
        public int MaxPoints { get { return _max_points; } set { _max_points = value; } }

        public void Clear(bool clear_color = false) { lock (this) { _x_data.Clear(); _y_data.Clear(); if (clear_color) ClearColors(); } }
        public void ClearColors() { lock (this) { _line_color.Clear(); _square_color.Clear(); } }

        public string XLegend { get; set; }
        public string YLegend { get; set; }


        public void AddPoint(double x, double y, string data_set = "primary")
        {
            lock (this)
            {
                if (!_x_data.Keys.Contains(data_set))
                    _x_data[data_set] = new List<double>();
                if (!_y_data.Keys.Contains(data_set))
                    _y_data[data_set] = new List<double>();
                _x_data[data_set].Add(x);
                _y_data[data_set].Add(y);

                if (_max_points > 0 && _x_data[data_set].Count > _max_points)
                {
                    _x_data[data_set].RemoveAt(0);
                    _y_data[data_set].RemoveAt(0);
                }
            }
        }
        public void Refresh() { Draw(); }

        bool _draw_squares;
        public bool DrawSquares { get { return _draw_squares; } set { _draw_squares = value; } }

        bool _draw_lines;
        public bool DrawLines { get { return _draw_lines; } set { _draw_lines = value; } }

        Random _random = new Random();

        public DataGraphControl()
        {
            _draw_squares = true;
            _draw_lines = true;
            XLegend = "x";
            YLegend = "y";
            InitializeComponent();
            Draw();
        }

        void the_image_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // We need to scale by DPI since window size is reported in "device independent" units (i.e. 96 DPI based)
            // If we used the size value without scaling our drawing won't be sized right on higher or lower DPI screens
            // DPI Ratio is stored in the composition transformation!  Easy ;P 
            double dpi_ratio_x = 1.0;
            double dpi_ratio_y = 1.0;
            if (Application.Current.MainWindow != null)
            {
                Matrix m = PresentationSource.FromVisual(Application.Current.MainWindow).CompositionTarget.TransformToDevice;
                dpi_ratio_x = m.M11;
                dpi_ratio_y = m.M22;
            }

            _size = new Size(e.NewSize.Width * dpi_ratio_x, e.NewSize.Height * dpi_ratio_y);
            _h_minor_tick_count = (int)Math.Min(MINOR_GRID_LINES, _size.Width / MINOR_TICK_RATIO);
            _v_minor_tick_count = (int)Math.Min(MINOR_GRID_LINES, _size.Height / MINOR_TICK_RATIO);

            Draw();
        }

        void DrawGrid(DrawingContext dc)
        {
            // grid is major and minor ticks on a rectangle around the margin
            const int major_tick_count = 10;
            int h_minor_tick_count = _h_minor_tick_count;
            int v_minor_tick_count = _v_minor_tick_count;
            const int major_tick_length = 8;
            const int minor_tick_length = 4;
            double width = _size.Width-1;
            double height = _size.Height-1;
            const double left = 0;
            double right = width;
            const double top = 0;
            double bottom = height;
            double w_step = width / major_tick_count;
            double h_step = height / major_tick_count;
            double wm_step = w_step / _h_minor_tick_count;
            double hm_step = h_step / _v_minor_tick_count;

            // draw border, light blue
            var pen = new Pen(Brushes.LightBlue, 1.0);
            dc.DrawRectangle(null, pen, new Rect(0, 0, width, height));

            for (int x = 0; x < major_tick_count; ++x)
            {
                var left_0 = left + w_step * x;
                var top_0 = top + h_step * x;
                dc.DrawLine(pen, new Point(left_0, top), new Point(left_0, top + major_tick_length));
                dc.DrawLine(pen, new Point(left_0, bottom - major_tick_length), new Point(left_0, bottom));
                dc.DrawLine(pen, new Point(left, top_0), new Point(left + major_tick_length, top_0));
                dc.DrawLine(pen, new Point(right - major_tick_length, top_0), new Point(right, top_0));
                for (int x1 = 0; x1 < Math.Max(_h_minor_tick_count, _v_minor_tick_count); ++x1)
                {
                    var left_1 = left_0 + wm_step * x1;
                    var top_1 = top_0 + hm_step * x1;
                    dc.DrawLine(pen, new Point(left_1, top), new Point(left_1, top + minor_tick_length));
                    dc.DrawLine(pen, new Point(left_1, bottom - minor_tick_length), new Point(left_1, bottom));
                    dc.DrawLine(pen, new Point(left, top_1), new Point(left + minor_tick_length, top_1));
                    dc.DrawLine(pen, new Point(right - minor_tick_length, top_1), new Point(right, top_1));
                }
            }
        }

        Color RandomColor()
        {
            return Color.FromArgb(255, (byte)_random.Next(0, 255), (byte)_random.Next(0, 255), (byte)_random.Next(0, 255));
        }

        void DrawData(DrawingContext dc)
        {
            ResetLimits();
            if (_x_data == null || _y_data == null)
                return;

            double biggest_y = double.MinValue;
            double smallest_y = double.MaxValue;
            double biggest_x = double.MinValue;
            double smallest_x = double.MaxValue;

            Dictionary<string, List<double>> all_data_x = null;
            Dictionary<string, List<double>> all_data_y = null;
            lock (this)
            {
                all_data_x = new Dictionary<string, List<double>>(_x_data);
                all_data_y = new Dictionary<string, List<double>>(_y_data);
            }

            bool no_data = true;
            foreach (var key in all_data_x.Keys)
            {
                var data_x = all_data_x[key];
                var data_y = all_data_y[key];
                int count = Math.Min(data_x.Count, data_y.Count);
                if (count == 0)
                    continue;
                no_data = false;
                biggest_y = Math.Max(biggest_y, data_y.Max());
                smallest_y = Math.Min(smallest_y, data_y.Min());
                biggest_x = Math.Max(biggest_x, data_x.Max());
                smallest_x = Math.Min(smallest_x, data_x.Min());
            }
            if (no_data)
                return;

            double y_delta = biggest_y - smallest_y;
            double x_delta = biggest_x - smallest_x;

            double width = _size.Width - 1;
            double height = _size.Height - 1;
            const double left = 0;
            double right = width;
            double bottom = width;

            double w_step = x_delta == 0.0 ? 0.0 : width / x_delta;
            double h_step = y_delta == 0.0 ? 0.0 : height / y_delta;

            _min_x = smallest_x;
            _min_y = smallest_y;
            _step_x = w_step;
            _step_y = h_step;

            foreach (var key in all_data_x.Keys)
            {
                var data_x = all_data_x[key];
                var data_y = all_data_y[key];
                int count = Math.Min(data_x.Count, data_y.Count);
                if (count == 0)
                    return;

                if (!_line_color.ContainsKey(key))
                    _line_color[key] = _x_data.Count == 1 ? Colors.Coral : RandomColor();
                if (!_square_color.ContainsKey(key))
                    _square_color[key] = _x_data.Count == 1 ? Colors.Firebrick : RandomColor();

                var data_brush = new SolidColorBrush(_line_color[key]);
                var square_brush = new SolidColorBrush(_square_color[key]);
                var data_pen = new Pen(data_brush, 1);
                var square_pen = new Pen(square_brush, 1);

                // special case -- 1 point or no variation
                if (y_delta == 0)
                {
                    var h_point = biggest_y <= 0.0 ? height : 0.0;
                    dc.DrawLine(data_pen, new Point(left, h_point), new Point(right, h_point));
                    return;
                }

                // normalize points so that smallest y value is at bottom, largest is at top
                //
                // norm = (data - smallest_y) * height / (biggest_y - smallest_y)
                // h_next & h_previous order is inverted since screen zero is upper left
                var x_last = left + (data_x[0] - smallest_x) * w_step;
                var y_last = height - (data_y[0] - smallest_y) * h_step;
                dc.DrawRectangle(data_brush, data_pen, new Rect(0, 0, 1, 1));

                if (_draw_squares)
                    for (int i = 0; i < count; ++i)
                    {
                        var x = left + (data_x[i] - smallest_x) * w_step;
                        var y = height - (data_y[i] - smallest_y) * h_step;
                        dc.DrawRectangle(data_brush, data_pen, new Rect(x - 1, y - 1, 3, 3));
                    }
                for (int i = 0; i < count; ++i)
                {
                    var x = left + (data_x[i] - smallest_x) * w_step;
                    var y = height - (data_y[i] - smallest_y) * h_step;
                    if (_draw_lines) dc.DrawLine(data_pen, new Point(x_last, y_last), new Point(x, y));
                    else dc.DrawRectangle(data_brush, data_pen, new Rect(x, y, 1, 1));

                    x_last = x;
                    y_last = y;
                }
            }
        }


        void DrawRubbish(DrawingContext dc)
        {
            Random rand = new Random();
            for (int i = 0; i < 200; i++)
                dc.DrawRectangle(Brushes.Red, null, new Rect(rand.NextDouble() * _size.Width, rand.NextDouble() * _size.Height, 1, 1));
            dc.Close();
        }

        object draw_lock = new object();
        bool drawing = false;
        void Draw()
        {
            // don't bother drawing if we're already inside the draw routine
            lock (draw_lock)
            {
                if (drawing) return;
                drawing = true;
            }

            Action action = () =>
            {
                var dv = new DrawingVisual();
                using (DrawingContext dc = dv.RenderOpen())
                {
                    DrawGrid(dc);
                    DrawData(dc);
                    if (Application.Current.MainWindow == null) // draw dots at design time
                        DrawRubbish(dc);
                }

                int w = Math.Max((int)(_size.Width), 1);
                int h = Math.Max((int)(_size.Height), 1);

                // render to a 96 dpi target since we already scaled the SIZE to the correct DPI 
                var rtb = new RenderTargetBitmap(w, h, 96.0, 96.0, PixelFormats.Pbgra32);
                rtb.Render(dv);
                the_image.Source = rtb;
                the_image.InvalidateVisual();

                lock (draw_lock)
                {
                    drawing = false;
                }
            };
            if (Dispatcher.CheckAccess()) action(); else Dispatcher.BeginInvoke(action, DispatcherPriority.Render);
        }


        // TOOL TIP 
        double _min_x;
        double _min_y;
        double _step_x;
        double _step_y;
        void ResetLimits()
        {
            _min_x = double.MaxValue;
            _min_y = double.MaxValue;
            _step_x = 0.0;
            _step_y = 0.0;
        }

        private void the_image_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var pos = e.GetPosition((IInputElement)e.Source);
            var x = _step_x == 0 ? 0.0 : _min_x + pos.X / _step_x;
            var y = _step_y == 0 ? 0.0 : _min_y + (_size.Height - pos.Y) / _step_y;

            the_tooltip.Content = string.Format("{0}: {1:0.00}\n{2}: {3:0.00}", XLegend, x, YLegend, y);
            the_tooltip.Placement = System.Windows.Controls.Primitives.PlacementMode.Relative;
            the_tooltip.HorizontalOffset = pos.X + 10;
            the_tooltip.VerticalOffset = pos.Y + 10;
        }
    }
}
