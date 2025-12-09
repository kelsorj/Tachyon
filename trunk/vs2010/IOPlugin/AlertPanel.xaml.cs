using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace BioNex.IOPlugin
{
    /// <summary>
    /// Interaction logic for AlertPanel.xaml
    /// </summary>
    public partial class AlertPanel : UserControl
    {
        // since IOConfiguration and HazardousBit are classes only used for serialization, wrap
        // them here with separate properties
        public class Bit : INotifyPropertyChanged
        {
            public string BitName { get; set; }
            public int BitIndex { get; set; }
            /// <summary>
            /// what the tooltip should say in the "hazardous" condition
            /// </summary>
            public string BitHazardMessage { get; set; }
            private string _tool_tip_text;
            /// <summary>
            /// databound to GUI
            /// </summary>
            public string ToolTipText
            {
                get { return _tool_tip_text; }
                set {
                    _tool_tip_text = value;
                    OnPropertyChanged( "ToolTipText");
                }
            }
            /// <summary>
            /// what state the bit should be in to be considered a safety issue
            /// </summary>
            public bool BitHazardState { get; set; }

            private Brush _status_color;
            public Brush StatusColor
            {
                get { return _status_color; }
                set {
                    _status_color = value;
                    OnPropertyChanged( "StatusColor");
                }
            }

            #region INotifyPropertyChanged Members

            public event PropertyChangedEventHandler PropertyChanged;
            public void OnPropertyChanged( string property_name)
            {
                if( PropertyChanged != null)
                    PropertyChanged( this, new PropertyChangedEventArgs( property_name));
            }

            #endregion
        }

        private IOPlugin.IO _io_plugin { get; set; }
        public List<Bit> Bits { get; set; }

        public bool GetBitState( int bit_number)
        {
            return _io_plugin.GetInput( bit_number - 1);
        }

        public AlertPanel( IOPlugin.IO io_plugin)
        {
            InitializeComponent();
            DataContext = this;
            _io_plugin = io_plugin;
            _io_plugin.InputChanged += new Shared.DeviceInterfaces.InputChangedEventHandler(_io_plugin_InputChanged);

            Bits = (from x in io_plugin._config.HazardousBits select new Bit { 
                BitName = x.BitName,
                BitIndex = x.BitNumber - 1,
                BitHazardState = x.HazardousLogicLevel != 0,
                BitHazardMessage = x.NotificationMessage
            }).ToList();

            // do an initial update on the sensor value so the GUI repaints
            foreach( var bit in Bits) {
                bool state = _io_plugin.GetInput( bit.BitIndex);
                SetBitColor( bit, state);
            }
        }

        void _io_plugin_InputChanged(object sender, Shared.DeviceInterfaces.InputChangedEventArgs e)
        {
            var bit = (from x in Bits where x.BitIndex == e.BitIndex select x).FirstOrDefault();
            if( bit == null)
                return;

            SetBitColor( bit, e.BitState);
        }

        private static void SetBitColor( Bit bit, bool for_state)
        {
            bit.StatusColor = for_state == bit.BitHazardState ? Brushes.DarkRed : Brushes.DarkGreen;
            bit.ToolTipText = for_state == bit.BitHazardState ? bit.BitHazardMessage : bit.BitName;
        }
    }
}
