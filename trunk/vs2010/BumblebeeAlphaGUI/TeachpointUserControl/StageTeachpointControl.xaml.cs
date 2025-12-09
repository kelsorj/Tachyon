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
    /// Interaction logic for StageTeachpointControl.xaml
    /// </summary>
    public partial class StageTeachpointControl : UserControl
    {
        private byte _stage_id;
        public byte Channel { get; set; }

        public byte Stage
        {
            get { return _stage_id; }
            set {
                _stage_id = value;
                label_stage.Content = String.Format( "Stage {0}", _stage_id);
            }
        }

        public void SetBackgroundColor( Brush c)
        {
            maingrid.Background = c;
        }

        public StageTeachpointControl()
        {
            InitializeComponent();
        }

        public static readonly RoutedEvent CommonButtonClickEvent =
            EventManager.RegisterRoutedEvent( "CommonStageButtonClick", RoutingStrategy.Bubble, typeof( RoutedEventHandler), typeof( StageTeachpointControl));

        public event RoutedEventHandler CommonStageButtonClick
        {
            add { AddHandler( CommonButtonClickEvent, value); }
            remove { RemoveHandler( CommonButtonClickEvent, value); }
        }

        private void CommonButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent( new RoutedEventArgs( CommonButtonClickEvent, new ButtonEventIDWrapper( Channel, Stage, ((Button)e.OriginalSource).Name)));
        }
    }
}
