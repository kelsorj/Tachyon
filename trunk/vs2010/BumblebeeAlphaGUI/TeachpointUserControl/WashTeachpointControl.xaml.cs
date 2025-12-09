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

namespace TeachpointUserControl
{
    /// <summary>
    /// Interaction logic for WashTeachpointControl.xaml
    /// </summary>
    public partial class WashTeachpointControl : UserControl
    {
        public byte Channel { get; set; }

        public WashTeachpointControl()
        {
            InitializeComponent();
        }

        public void SetBackgroundColor( Brush c)
        {
            washgrid.Background = c;
        }

        public static readonly RoutedEvent CommonButtonClickEvent =
            EventManager.RegisterRoutedEvent( "CommonWashButtonClick", RoutingStrategy.Bubble, typeof( RoutedEventHandler), typeof( WashTeachpointControl));

        public event RoutedEventHandler CommonWashButtonClick
        {
            add { AddHandler( CommonButtonClickEvent, value); }
            remove { RemoveHandler( CommonButtonClickEvent, value); }
        }

        private void CommonButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent( new RoutedEventArgs( CommonButtonClickEvent, new ButtonEventIDWrapper( Channel, 0, ((Button)e.OriginalSource).Name)));
        }
    }
}
