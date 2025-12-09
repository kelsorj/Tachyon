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
using System.Windows.Media.Animation;

namespace _4AxisCoordinatedMovesTestGUI
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private class ChannelWrapper
        {
            private Channel.UserControl1 _channel;
            private double _y;
            private double _parent_height;

            public ChannelWrapper( Channel.UserControl1 c, double initial_y_pos, double parent_height)
            {
                _channel = c;
                _parent_height = parent_height;
                Y = initial_y_pos;
                X = 0;
            }

            public double Y
            {
                set {
                    TranslateTransform transform = _channel.RenderTransform as TranslateTransform;
                    if( transform != null)
                        transform.Y = _parent_height - value;    // need to account for inverted Y axis
                    _y = value;
                }
                get { return _y; }
            }

            public double X
            {
                set { _channel.X = value; }
                get { return _channel.X; }
            }

            public void MoveAbsolute( double x)
            {
                _channel.MoveAbsolute( x);
            }
        }

        private class PlateMoverWrapper
        {
            private PlateMover.UserControl1 _p;

            public PlateMoverWrapper( PlateMover.UserControl1 p, double initial_y_pos, double initial_angle, double parent_height)
            {
                _p = p;
                p.SetParentHeight( parent_height);
                Angle = initial_angle;
                Y = initial_y_pos;
            }

            public double Y
            {
                set { _p.Y = value; }
                get { return _p.Y; }
            }

            public double Angle
            {
                set { _p.Angle = value; }
                get { return _p.Angle; }
            }

            public void MoveYAbsolute( double y)
            {
                _p.MoveYAbsolute( y);
            }

            public void MoveThetaAbsolute( double angle)
            {
                _p.MoveThetaAbsolute( angle);
            }

            public void MoveAbsolute( double y, double theta)
            {
                _p.MoveAbsolute( y, theta);
            }
        }

        private ChannelWrapper c1 = null;
        private ChannelWrapper c2 = null;
        private PlateMoverWrapper p1 = null;
        private PlateMoverWrapper p2 = null;

        public Window1()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //BioNex.MotorLibrary.MotorController mc = new BioNex.MotorLibrary.MotorController( 6);

            c1 = new ChannelWrapper( channel1, 218, canvas.Height);
            c2 = new ChannelWrapper( channel2, 200, canvas.Height);
            p1 = new PlateMoverWrapper( source_plate, 0, 0, canvas.Height);
            p2 = new PlateMoverWrapper( dest_plate, 0, 0, canvas.Height);
        }

        private void button_apply_spacing_Click(object sender, RoutedEventArgs e)
        {
            // translate channel 2 X mm from channel 1
            c2.Y = c1.Y - double.Parse(channel_spacing.Text);
        }

        private void slider_tip1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            tip1_position.Text = e.NewValue.ToString();
            c1.X = double.Parse( tip1_position.Text);
        }

        private void slider_tip2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            tip2_position.Text = e.NewValue.ToString();
            c2.X = double.Parse( tip2_position.Text);
        }

        private void slider_sourceplate_angle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            sourceplate_angle.Text = e.NewValue.ToString();
            p1.Angle = double.Parse( sourceplate_angle.Text);
        }

        private void slider_sourceplate_y_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            sourceplate_y.Text = e.NewValue.ToString();
            p1.Y = double.Parse( sourceplate_y.Text);
        }

        private void slider_destplate_angle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            destplate_angle.Text = e.NewValue.ToString();
            p2.Angle = double.Parse( destplate_angle.Text);
        }

        private void slider_destplate_y_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            destplate_y.Text = e.NewValue.ToString();
            p2.Y = double.Parse( destplate_y.Text);
        }

        private void button_test_1_Click(object sender, RoutedEventArgs e)
        {
            p1.MoveYAbsolute( 100);
        }

        private void button_test_2_Click(object sender, RoutedEventArgs e)
        {
            p1.MoveYAbsolute( 200);
        }

        private void button_test_3_Click(object sender, RoutedEventArgs e)
        {
            p1.MoveYAbsolute( 300);
        }

        private void button_test_4_Click(object sender, RoutedEventArgs e)
        {
            p1.MoveAbsolute( 100, 45);
        }

        private void button_test_5_Click(object sender, RoutedEventArgs e)
        {
            p1.MoveAbsolute( 200, -45);
        }

        private void button_test_6_Click(object sender, RoutedEventArgs e)
        {
            c1.MoveAbsolute( 200);
            p1.MoveAbsolute( 200, 45);
        }
    }
}
