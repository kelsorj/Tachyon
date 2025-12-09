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

namespace BioNex.Shared.Utils
{
    /// <summary>
    /// Interaction logic for StatusIndicator.xaml
    /// </summary>
    public partial class StatusIndicator : UserControl
    {
        public static DependencyProperty OutlineWidthProperty = DependencyProperty.Register( "OutlineWidth", typeof(Int32), typeof(StatusIndicator));
        public static DependencyProperty OutlineHeightProperty = DependencyProperty.Register( "OutlineHeight", typeof(Int32), typeof(StatusIndicator));
        public static DependencyProperty OutlineRadiusProperty = DependencyProperty.Register( "OutlineRadius", typeof(Int32), typeof(StatusIndicator));
        public static DependencyProperty OutlineBorderThicknessProperty = DependencyProperty.Register( "OutlineBorderThickness", typeof(Thickness), typeof(StatusIndicator));
        public static DependencyProperty OutlineColorProperty = DependencyProperty.Register( "OutlineColor", typeof(Brush), typeof(StatusIndicator));
        public static DependencyProperty FillColorProperty = DependencyProperty.Register( "FillColor", typeof(Brush), typeof(StatusIndicator));
        public static DependencyProperty IndicatorTextProperty = DependencyProperty.Register( "IndicatorText", typeof(string), typeof(StatusIndicator));
        public static DependencyProperty IndicatorTextColorProperty = DependencyProperty.Register( "IndicatorTextColor", typeof(Brush), typeof(StatusIndicator));
        public static DependencyProperty IndicatorTextFontSizeProperty = DependencyProperty.Register( "IndicatorTextFontSize", typeof(Int32), typeof(StatusIndicator));

        public int OutlineWidth
        {
            get { return (Int32)GetValue( OutlineWidthProperty); }
            set { SetValue( OutlineWidthProperty, (Int32)value); }
        }

        public int OutlineHeight
        {
            get { return (Int32)GetValue( OutlineHeightProperty); }
            set { SetValue( OutlineHeightProperty, (Int32)value); }
        }

        public int OutlineRadius
        {
            get { return (Int32)GetValue( OutlineRadiusProperty); }
            set { SetValue( OutlineRadiusProperty, (Int32)value); }
        }

        public Thickness OutlineBorderThickness
        {
            get { return (Thickness)GetValue( OutlineBorderThicknessProperty); }
            set { SetValue( OutlineBorderThicknessProperty, (Thickness)value); }
        }

        public Brush OutlineColor
        {
            get { return (Brush)GetValue( OutlineColorProperty); }
            set { SetValue( OutlineColorProperty, (Brush)value); }
        }

        public Brush FillColor
        {
            get { return (Brush)GetValue( FillColorProperty); }
            set { SetValue( FillColorProperty, (Brush)value); }
        }

        public string IndicatorText
        {
            get { return (string)GetValue( IndicatorTextProperty); }
            set { SetValue( IndicatorTextProperty, (string)value); }
        }

        public Brush IndicatorTextColor
        {
            get { return (Brush)GetValue( IndicatorTextColorProperty); }
            set { SetValue( IndicatorTextColorProperty, (Brush)value); }
        }

        public Int32 IndicatorTextFontSize
        {
            get { return (Int32)GetValue( IndicatorTextFontSizeProperty); }
            set { SetValue( IndicatorTextFontSizeProperty, (Int32)value); }
        }

        public StatusIndicator()
        {
            InitializeComponent();
        }
    }
}
