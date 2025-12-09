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

namespace Channel
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        private double _x;

        public UserControl1()
        {
            InitializeComponent();
        }

        public double X
        {
            set {
                TranslateTransform transform = tip.RenderTransform as TranslateTransform;
                if( transform != null && value < Width) {
                    transform.X = value;
                    _x = value;
                }
            }
            get { return _x; }
        }

        public void MoveAbsolute( double x)
        {
            DoubleAnimation anim = new DoubleAnimation();
            anim.From = X;
            anim.To = x;
            anim.Duration = new Duration( new TimeSpan( 0, 0, 0, 0, 500));

            TranslateTransform transform = tip.RenderTransform as TranslateTransform;
            if( transform != null) {
                transform.BeginAnimation( TranslateTransform.XProperty, anim);
                X = x;
            }
        }
    }
}
