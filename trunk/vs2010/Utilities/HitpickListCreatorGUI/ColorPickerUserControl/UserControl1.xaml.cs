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

namespace BioNex.HitpickListCreatorGUI.ColorPickerUserControl
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        public UserControl1()
        {
            InitializeComponent();
        }

        public void SetColorRGBString( string s)
        {
            text_rgb.Text = s;
        }

        public static readonly RoutedEvent ColorChangedEvent =
            EventManager.RegisterRoutedEvent( "ColorChanged", RoutingStrategy.Bubble, typeof( RoutedEventHandler), typeof( UserControl1));

        public event RoutedEventHandler ColorChanged
        {
            add { AddHandler( ColorChangedEvent, value); }
            remove { RemoveHandler( ColorChangedEvent, value); }
        }

        private void text_rgb_TextChanged(object sender, TextChangedEventArgs e)
        {
            try {
                SolidColorBrush brush = new SolidColorBrush();
                string color = text_rgb.Text;
                Color temp = brush.Color;
                // bail if the length is wrong
                if( color.Length != 6 && color.Length != 8)
                    return;
                // otherwise, change the color of the label background as a preview
                if( color.Length == 6) {
                    temp.A = 255;
                    temp.R = Convert.ToByte( color.Substring( 0, 2), 16);
                    temp.G = Convert.ToByte( color.Substring( 2, 2), 16);
                    temp.B = Convert.ToByte( color.Substring( 4, 2), 16);
                } else if( color.Length == 8) {
                    temp.A = Convert.ToByte( color.Substring( 0, 2), 16);
                    temp.R = Convert.ToByte( color.Substring( 2, 2), 16);
                    temp.G = Convert.ToByte( color.Substring( 4, 2), 16);
                    temp.B = Convert.ToByte( color.Substring( 6, 2), 16);
                }
                brush.Color = temp;
                label_rgb_preview.Background = brush;
                // raise the event to the containing window and pass the color object
                RaiseEvent( new RoutedEventArgs( ColorChangedEvent, brush));
            } catch( Exception ex) {
                // do nothing
            }
        }
    }
}
