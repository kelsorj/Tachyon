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

namespace BioNex.PlateMover
{
    /// <summary>
    /// Interaction logic for JogTeach.xaml
    /// </summary>
    public partial class JogTeach : UserControl
    {
        public static readonly DependencyProperty StageAngleProperty = DependencyProperty.Register( "StageAngle", typeof(double), typeof(JogTeach));
        public double? StageAngle
        {
            get { return this.GetValue(StageAngleProperty) as double?; }
            set { this.SetValue(StageAngleProperty, value); }
        }

        public static readonly DependencyProperty StagePositionProperty = DependencyProperty.Register( "StagePosition", typeof(double), typeof(JogTeach));
        public double? StagePosition
        {
            get { return this.GetValue(StagePositionProperty) as double?; }
            set { this.SetValue(StagePositionProperty, value); }
        }

        public static readonly DependencyProperty RVisibilityProperty = DependencyProperty.Register( "RVisibility", typeof(Visibility), typeof(JogTeach));
        public Visibility? RVisibility
        {
            get { return this.GetValue(RVisibilityProperty) as Visibility?; }
            set { this.SetValue(RVisibilityProperty, value); }
        }

        public static readonly DependencyProperty YVisibilityProperty = DependencyProperty.Register( "YVisibility", typeof(Visibility), typeof(JogTeach));
        public Visibility? YVisibility
        {
            get { return this.GetValue(YVisibilityProperty) as Visibility?; }
            set { this.SetValue(YVisibilityProperty, value); }
        }

        public static readonly DependencyProperty TrackLengthProperty = DependencyProperty.Register( "TrackLength", typeof(double), typeof(JogTeach));
        public double? TrackLength
        {
            get { return this.GetValue(TrackLengthProperty) as double?; }
            set { this.SetValue(TrackLengthProperty, value); }
        }

        public JogTeach()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
