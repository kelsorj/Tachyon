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
using System.Windows.Shapes;
using System.ComponentModel;
using System.IO.Ports;
using BioNex.Shared.TechnosoftLibrary;

namespace BioNex.HivePrototypePlugin
{
    /// <summary>
    /// Interaction logic for CommSelectionDialog.xaml
    /// </summary>
    public partial class CommSelectionDialog : Window, INotifyPropertyChanged
    {
        private List<string> _ports = new List<string>();
        public List<string> Ports
        {
            get { return _ports; }
            set {
                _ports = value;
                OnPropertyChanged( "Ports");
            }
        }

        private List<string> _devices;
        public List<string> Devices
        {
            get { return _devices; }
            set {
                _devices = value;
                OnPropertyChanged( "Devices");
            }
        }

        private string _current_device;
        public string CurrentDevice
        {
            get { return _current_device; }
            set {
                _current_device = value;
                OnPropertyChanged( "CurrentDevice");
            }
        }

        private string _current_port;
        public string CurrentPort
        {
            get { return _current_port; }
            set {
                _current_port = value;
                OnPropertyChanged( "CurrentPort");
            }
        }

        private const string RS232 = "RS-232";
        private const string IXXAT = "Ixxat CAN";
        private const string PEAK = "Peak PCAN-USB";
        private const string SYSTEC = "SysTec USB-CAN";
        private const string SIMULATOR = "Simulator";

        public CommSelectionDialog()
        {
            InitializeComponent();
            DataContext = this;
            // create list of devices
            Devices = new List<string> { RS232, IXXAT, PEAK, SYSTEC, SIMULATOR };
            // create list of ports
            // query the serial port library to find out what ports are actually available on the system
            string[] ports = SerialPort.GetPortNames();
            Ports.AddRange( ports);
            Ports.Add( "CAN0");
            Ports.Add( "CAN1");
            Ports.Add( "CAN2");
            CurrentDevice = SYSTEC;
            CurrentPort = "CAN0";
            //CurrentDevice = IXXAT;
            //CurrentPort = "CAN1";
        }

        public string GetPort()
        {
            switch( CurrentPort) {
                case "CAN0":
                    return "0";
                case "CAN1":
                    return "1";
                case "CAN2":
                    return "2";
                default:
                    return CurrentPort;
            }
        }

        public byte GetDevice()
        {
            switch( CurrentDevice) {
                case CommSelectionDialog.RS232:
                    return TMLLibConst.CHANNEL_RS232;
                case CommSelectionDialog.IXXAT:
                    return TMLLibConst.CHANNEL_IXXAT_CAN;
                case CommSelectionDialog.PEAK:
                    return TMLLibConst.CHANNEL_PEAK_SYS_PCAN_USB;
                case CommSelectionDialog.SYSTEC:
                    return TMLLibConst.CHANNEL_SYS_TEC_USBCAN;
                default:
                    return 69; // grow up, Mark!  lol
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for
        /// a given property.
        /// </summary>
        /// <param name="propertyName">The name of the changed property.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
