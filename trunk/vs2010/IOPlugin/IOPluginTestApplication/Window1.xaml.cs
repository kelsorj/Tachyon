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
using BioNex.IOPlugin;

namespace IOPluginTestApplication
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        IO io;
        public Window1()
        {
            InitializeComponent();
            io = new IO();
            Dictionary<string,string> properties = new Dictionary<string,string>();
            properties[IO.Simulate] = "0";
            properties[IO.CANDeviceID] = "0";
            properties[IO.CANDeviceChannel] = "0";
            properties[IO.ConfigFolder] = "config";
            properties[IO.CANOpenNodeID] = "63";
            properties[IO.InputByteCount] = "2";
            properties[IO.OutputByteCount] = "3";
            DeviceManagerDatabase.DeviceInfo info = new DeviceManagerDatabase.DeviceInfo("BioNex", "IODevice", "I/O Device", false, properties);
            io.SetProperties( info);
            io.Connect();
            main.Children.Add( io.GetDiagnosticsPanel());
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            io.Close();
        }
    }
}
