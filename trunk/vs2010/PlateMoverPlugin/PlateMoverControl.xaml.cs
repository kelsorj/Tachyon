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
    // converters
    public class OffsetFormatter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return double.Parse( value.ToString()) + 30;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // we aren't ever going to need to convert back for this formatter!
            return null;
        }

        #endregion
    }
    
    /// <summary>
    /// Interaction logic for PlateMoverControl.xaml
    /// </summary>
    public partial class PlateMoverControl : UserControl
    {
        public static readonly DependencyProperty RProperty = DependencyProperty.Register( "R", typeof(double), typeof(PlateMoverControl));
        public double? R
        {
            get { return this.GetValue(RProperty) as double?; }
            set { this.SetValue(RProperty, value); }
        }

        public static readonly DependencyProperty YProperty = DependencyProperty.Register( "Y", typeof(double), typeof(PlateMoverControl));
        public double? Y
        {
            get { return this.GetValue(YProperty) as double?; }
            set { this.SetValue(YProperty, value); }
        }

        public static readonly DependencyProperty RVisibilityProperty = DependencyProperty.Register( "RVisibility", typeof(Visibility), typeof(PlateMoverControl));
        public Visibility? RVisibility
        {
            get { return this.GetValue(RVisibilityProperty) as Visibility?; }
            set { this.SetValue(RVisibilityProperty, value); }
        }

        public static readonly DependencyProperty YVisibilityProperty = DependencyProperty.Register( "YVisibility", typeof(Visibility), typeof(PlateMoverControl));
        public Visibility? YVisibility
        {
            get { return this.GetValue(YVisibilityProperty) as Visibility?; }
            set { this.SetValue(YVisibilityProperty, value); }
        }

        public static readonly DependencyProperty TrackLengthProperty = DependencyProperty.Register( "TrackLength", typeof(double), typeof(PlateMoverControl));
        public double? TrackLength
        {
            get { return this.GetValue(TrackLengthProperty) as double?; }
            set { this.SetValue(TrackLengthProperty, value); }
        }

        public PlateMoverControl()
        {
            InitializeComponent();
        }
    }
}
