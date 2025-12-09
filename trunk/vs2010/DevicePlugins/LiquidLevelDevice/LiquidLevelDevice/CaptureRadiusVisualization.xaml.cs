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
using BioNex.Shared.BioNexGuiControls;
using System.Windows.Threading;

namespace BioNex.LiquidLevelDevice
{
    /// <summary>
    /// Interaction logic for CaptureRadiusVisualization.xaml
    /// </summary>
    public partial class CaptureRadiusVisualization : Window
    {
        public CaptureRadiusVisualization(ILLSensorModel model,  Window owner=null)
        {
            InitializeComponent();
            DataContext = this;

            _model = model;
            Owner = owner ?? Application.Current.MainWindow;
            Draw();
        }

        ILLSensorModel _model;
        double[] prev_xs;
        double[] prev_ys;
        double prev_tx;
        double prev_ty;
        double[] cache_prev_xs;
        double[] cache_prev_ys;
        double cache_prev_tx;
        double cache_prev_ty;


        void DoDrawing(DrawingContext dc)
        {
            var width = _size.Width - 1;
            var height = _size.Height - 1;

            var point_pen = new Pen(Brushes.LightBlue, 1.0);
            var reject_pen = new Pen(Brushes.LightCoral, 1.0);
            var sample_pen = new Pen(Brushes.BlueViolet, 1.0);
            var text_brush = Brushes.BlueViolet;

            // get our scaling factor, we want to fit Reject Radius, plus max Deviation + capture radius in our window....
            // get all the deviations

            // If we draw this diagram so that Pos Y is toward the top and Pos X is to the right (e.g. opposite Y of standard window coords)
            // then the diagram is oriented as if you're standing in front of the machine

            double[] xs = new double[_model.SensorCount];
            double[] ys = new double[_model.SensorCount];

            for (int i = 0; i < _model.SensorCount; ++i)
            {
                xs[i] = _model.Properties.GetDouble(LLProperties.index(LLProperties.XDeviation, i));
                ys[i] = -_model.Properties.GetDouble(LLProperties.index(LLProperties.YDeviation, i)); // Y Sign change HERE fixes orientation for screen coordinates -- make sure to take this into account if doing any relative adjustments later
            }

            double capture_radius = _model.Properties.GetDouble(LLProperties.CaptureRadiusStart);
            double reject_radius = _model.Properties.GetDouble(LLProperties.CaptureRejectionRadius);

            var min_x = Math.Min(-reject_radius, xs.Min() - capture_radius);
            var max_x = Math.Max(reject_radius, xs.Max() + capture_radius);

            var min_y = Math.Min(-reject_radius, ys.Min() - capture_radius);
            var max_y = Math.Max(reject_radius, ys.Max() + capture_radius);

            if (prev_xs != null)
            {
                var tdx = prev_tx - _model.Properties.GetDouble(LLProperties.X_TP);
                var tdy = prev_ty - _model.Properties.GetDouble(LLProperties.Y_TP);
                
                min_x = Math.Min(min_x, prev_xs.Min() - capture_radius + tdx);
                max_x = Math.Max(max_x, prev_xs.Max() + capture_radius + tdx);
                min_y = Math.Min(min_y, prev_ys.Min() - capture_radius + tdy);
                max_y = Math.Max(max_x, prev_ys.Max() + capture_radius + tdy);
            }

            min_x = Math.Min(min_x, min_y);
            min_y = min_x;
            max_x = Math.Max(max_x, max_y);
            max_y = max_x;

            // "teachpoint" is the average deviation -- represents where we think the minimum error location of a well is
            var tx = xs.Average(); // this should be zero
            var ty = ys.Average(); // this should be zero

            // delta gives us the window scaling factor
            var dx = max_x - min_x;
            var dy = max_y - min_y;
            var x_scale = dx == 0.0 ? 0.0 : width / dx;
            var y_scale = dy == 0.0 ? 0.0 : height / dy;

            for (int i = 0; i < _model.SensorCount; ++i)
            {
                var x = (xs[i] - min_x) * x_scale;    // screen X
                var y = (ys[i] - min_y) * y_scale;    // screen Y

                dc.DrawRectangle(Brushes.LightBlue, point_pen, new Rect(x - 1, y - 1, 3, 3));
                dc.DrawEllipse(null, point_pen, new Point(x, y), capture_radius * x_scale, capture_radius * y_scale);

                var font = System.Drawing.SystemFonts.MessageBoxFont;
                var text_offset = 3;
                var text = new FormattedText((i+1).ToString(), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(font.Name), 10, text_brush);
                dc.DrawText(text, new Point(x+text_offset, y-2*text_offset));

                // draw black points around circle perimeter where samples will be taken
                var START_ANGLE = 45.0; // match const in LLSCaptureStateMachine -- all of this math should match really
                var samples = _model.Properties.GetInt(LLProperties.SamplesPerWell);
                for (int j = 0; j < samples; ++j)
                {
                    var angle_degrees = START_ANGLE + j * 360.0 / samples;
                    var angle_radians = Math.PI * angle_degrees / 180.0;
                    var x_s = x + (capture_radius * Math.Cos(angle_radians) * x_scale);
                    var y_s = y + (capture_radius * Math.Sin(angle_radians) * y_scale);  // SIGN IS CHANGED HERE FOR SCREEN ORIENTATION (-capture_radius)

                    dc.DrawEllipse(null, sample_pen, new Point(x_s, y_s), 3, 3);
                }
                            
            }
            dc.DrawEllipse(null, reject_pen, new Point((tx - min_x) * x_scale, (ty - min_y) * y_scale), reject_radius * x_scale, reject_radius * y_scale);

            // Draw capture radius circles for PREVIOUS locations, relative to the old teachpoint
            if (prev_xs != null)
            {
                var prev_point_brush = new SolidColorBrush(Color.FromArgb(130, Colors.LightBlue.R, Colors.LightBlue.G, Colors.LightBlue.B));
                var prev_point_pen = new Pen(prev_point_brush, 1.0);
                var prev_text_brush = new SolidColorBrush(Color.FromArgb(130, Colors.BlueViolet.R, Colors.BlueViolet.G, Colors.BlueViolet.B));
                var prev_reject_brush = new SolidColorBrush(Color.FromArgb(130, Colors.LightCoral.R, Colors.LightCoral.G, Colors.LightCoral.B));
                var prev_reject_pen = new Pen(prev_reject_brush, 1.0);

                var tdx = prev_tx - _model.Properties.GetDouble(LLProperties.X_TP);
                var tdy = prev_ty - _model.Properties.GetDouble(LLProperties.Y_TP); 

                for (int i = 0; i < _model.SensorCount; ++i)
                {
                    // calculate prev deviations relative to new teachpoint 
                    var x = (prev_xs[i] + tdx - min_x) * x_scale;    // screen X
                    var y = (prev_ys[i] + tdy - min_y) * y_scale;    // screen Y

                    dc.DrawRectangle(Brushes.LightBlue, prev_point_pen, new Rect(x - 1, y - 1, 3, 3));
                    dc.DrawEllipse(null, prev_point_pen, new Point(x, y), capture_radius * x_scale, capture_radius * y_scale);

                    var font = System.Drawing.SystemFonts.MessageBoxFont;
                    var text_offset = 3;
                    var text = new FormattedText((i + 1).ToString(), System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(font.Name), 10, prev_text_brush);
                    dc.DrawText(text, new Point(x + text_offset, y - 2 * text_offset));
                }
                dc.DrawEllipse(null, prev_reject_pen, new Point((tdx - min_x) * x_scale, (tdy - min_y) * y_scale), reject_radius * x_scale, reject_radius * y_scale);
            }

            cache_prev_tx = _model.Properties.GetDouble(LLProperties.X_TP);
            cache_prev_ty = _model.Properties.GetDouble(LLProperties.Y_TP);
            cache_prev_xs = new double[xs.Length];
            cache_prev_ys = new double[ys.Length];
            xs.CopyTo(cache_prev_xs, 0);
            ys.CopyTo(cache_prev_ys, 0);
        }

        public void SavePrevValues()
        {
            prev_tx = cache_prev_tx;
            prev_ty = cache_prev_ty;

            prev_xs = new double[cache_prev_xs.Length];
            prev_ys = new double[cache_prev_ys.Length];

            cache_prev_xs.CopyTo(prev_xs, 0);
            cache_prev_ys.CopyTo(prev_ys, 0);
        }

        Size _size = new Size(200, 200);
        void the_img_SizeChanged(object sender, SizeChangedEventArgs e)
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
            Draw();
        }

        object draw_lock = new object();
        bool drawing = false;
        public void Draw()
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
                    if (Application.Current.MainWindow == null) // draw dots at design time
                        return;
                    DoDrawing(dc);
                }

                int w = Math.Max((int)(_size.Width), 1);
                int h = Math.Max((int)(_size.Height), 1);

                // render to a 96 dpi target since we already scaled the SIZE to the correct DPI 
                var rtb = new RenderTargetBitmap(w, h, 96.0, 96.0, PixelFormats.Pbgra32);
                rtb.Render(dv);
                the_img.Source = rtb;
                the_img.InvalidateVisual();

                lock (draw_lock)
                {
                    drawing = false;
                }
            };
            if (Dispatcher.CheckAccess()) action(); else Dispatcher.BeginInvoke(action, DispatcherPriority.Render);
        }

    }
}
