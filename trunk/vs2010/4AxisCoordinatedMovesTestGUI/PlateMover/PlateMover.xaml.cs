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

namespace PlateMover
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        double _parent_height = 0;
        double _y;
        double _angle;

        public UserControl1()
        {
            InitializeComponent();
            WellGrid.Margin = new Thickness( 9.5, 4.5, 9.5, 4.5);
            for( int i=0; i<96; i++) {
                Ellipse e = new Ellipse();
                e.Height = 7;
                e.Width = 7;
                e.Margin = new Thickness( 1);
                e.Fill = Brushes.Aqua;
                e.Opacity = 1;
                WellGrid.Children.Add( e);
            }
        }

        public void SetParentHeight( double height)
        {
            _parent_height = height;
        }

        public double Angle
        {
            set {
                _angle = value;
                TransformGroup group = plate.RenderTransform as TransformGroup;
                RotateTransform transform = group.Children[0] as RotateTransform;
                if( transform != null) {
                    transform.Angle = _angle;
                    return;
                }
            }
            get { return _angle; }
        }

        public double Y
        {
            set {
                _y = _parent_height - Height - value;
                TransformGroup group = plate.RenderTransform as TransformGroup;
                TranslateTransform transform = group.Children[1] as TranslateTransform;
                if( transform == null) {
                    transform.Y = _y;
                    return;
                }
            }
            get { return _y; }
        }

        private double ConvertYFromReal( double real_position)
        {
            return _parent_height - Height - real_position;
        }

        public void MoveYAbsolute( double y)
        {
            DoubleAnimation anim = new DoubleAnimation();
            anim.From = Y;
            anim.To = ConvertYFromReal( y);
            anim.Duration = new Duration( new TimeSpan( 0, 0, 0, 0, 500));

            TransformGroup group = plate.RenderTransform as TransformGroup;
            if( group == null)
                return;
            TranslateTransform transform = group.Children[1] as TranslateTransform;
            if( transform != null) {
                transform.BeginAnimation( TranslateTransform.YProperty, anim);
                _y = ConvertYFromReal( y);
            }
        }

        public void MoveThetaAbsolute( double angle)
        {
            DoubleAnimation anim = new DoubleAnimation();
            anim.From = Angle;
            anim.To = angle;
            anim.Duration = new Duration( new TimeSpan( 0, 0, 0, 0, 500));

            TransformGroup group = plate.RenderTransform as TransformGroup;
            if( group == null)
                return;
            RotateTransform transform = group.Children[0] as RotateTransform;
            if( transform != null) {
                transform.BeginAnimation( RotateTransform.AngleProperty, anim);
                Angle = angle;
            }
        }

        public void MoveAbsolute( double y, double theta)
        {
            DoubleAnimation translation_anim = new DoubleAnimation();
            translation_anim.From = Y;
            translation_anim.To = ConvertYFromReal( y);
            translation_anim.Duration = new Duration( new TimeSpan( 0, 0, 0, 0, 500));
            DoubleAnimation rotation_anim = new DoubleAnimation();
            rotation_anim.From = Angle;
            rotation_anim.To = theta;
            rotation_anim.Duration = new Duration( new TimeSpan( 0, 0, 0, 0, 500));

            TransformGroup group = plate.RenderTransform as TransformGroup;
            if( group == null)
                return;
            // here, I assume an order to the transformations, since order matters anyway
            // according to the XAML, rotate, then translate
            RotateTransform rt = group.Children[0] as RotateTransform;
            if( rt != null) {
                rt.BeginAnimation( RotateTransform.AngleProperty, rotation_anim);
                Angle = theta;
            }
            TranslateTransform tt = group.Children[1] as TranslateTransform;
            if( tt != null) {
                tt.BeginAnimation( TranslateTransform.YProperty, translation_anim);
                _y = ConvertYFromReal( y);
            }
        }
    }
}
