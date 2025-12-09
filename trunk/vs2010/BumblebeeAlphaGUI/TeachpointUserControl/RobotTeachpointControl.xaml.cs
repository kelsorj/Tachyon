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
    /// Interaction logic for RobotTeachpointControl.xaml
    /// </summary>
    public partial class RobotTeachpointControl : UserControl
    {
        private byte _stage_id;

        public byte Stage
        {
            get { return _stage_id; }
            set {
                _stage_id = value;
                label_stage.Content = String.Format( "Stage {0}", _stage_id);
            }
        }

        public RobotTeachpointControl()
        {
            InitializeComponent();
        }

        public static readonly RoutedEvent CommonButtonClickEvent =
            EventManager.RegisterRoutedEvent( "CommonRobotButtonClick", RoutingStrategy.Bubble, typeof( RoutedEventHandler), typeof( WashTeachpointControl));

        public event RoutedEventHandler CommonRobotButtonClick
        {
            add { AddHandler( CommonButtonClickEvent, value); }
            remove { RemoveHandler( CommonButtonClickEvent, value); }
        }

        private void CommonButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent( new RoutedEventArgs( CommonButtonClickEvent, new ButtonEventIDWrapper( 0, Stage, ((Button)e.OriginalSource).Name)));
        }
    }
}
