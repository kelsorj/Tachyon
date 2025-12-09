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
using System.IO.Ports;

namespace BioNex.Shared.Microscan
{
    /// <summary>
    /// Interaction logic for WOIControl.xaml
    /// </summary>
    public partial class WOIControl : UserControl
    {
        private SerialPort _port;

        public WOIControl( SerialPort port)
        {
            InitializeComponent();
            _port = port;
        }
    }
}
