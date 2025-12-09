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
using System.ComponentModel;

namespace BioNex.Shared.BioNexGuiControls
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class LED : UserControl
    {
        public static DependencyProperty LEDColorProperty = DependencyProperty.Register( "LEDColor", typeof(Brush), typeof(LED));
        public Brush LEDColor
        {
            get { return this.GetValue(LEDColorProperty) as Brush; }
            set { 
                this.SetValue( LEDColorProperty, value);
            }
        }

        public static DependencyProperty LEDSizeProperty = DependencyProperty.Register( "LEDSize", typeof(int), typeof(LED));
        public int LEDSize
        {
            get { return (int)GetValue(LEDSizeProperty); }
            set { 
                SetValue( LEDSizeProperty, value);
            }
        }

        public static DependencyProperty LEDLabelProperty = DependencyProperty.Register( "LEDLabel", typeof(string), typeof(LED));
        public string LEDLabel
        {
            get { return (string)GetValue(LEDLabelProperty); }
            set { 
                SetValue( LEDLabelProperty, value);
            }
        }

        public LED()
        {
            InitializeComponent();
        }
    }
}
