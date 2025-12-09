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
using System.Collections.ObjectModel;
using System.Windows.Threading;
using BioNex.Shared;
using BioNex.Shared.DeviceInterfaces;

namespace PlateLocPluginTestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DeviceInterface _plugin;

        public MainWindow()
        {
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _plugin = new PlateLocPlugin.PlateLocPlugin();
            Dictionary<string,string> properties = new Dictionary<string,string>();
            properties["Profile"] = "plateloc";
            DeviceManagerDatabase.DeviceInfo device_info = new DeviceManagerDatabase.DeviceInfo( "Velocity11", "PlateLoc", "PlateLoc instance", false, properties);
            uc_wrapper.SetPlugin( _plugin, device_info);
        }

        private void Seal_Click(object sender, RoutedEventArgs e)
        {
            bool ret = _plugin.ExecuteCommand( PlateLocPlugin.PlateLocPlugin.Commands.ApplySeal.ToString(),
                                                                    new Dictionary<string,object> { { PlateLocPlugin.PlateLocPlugin.CommandParameterNames.temp, 25 },
                                                                                                    { PlateLocPlugin.PlateLocPlugin.CommandParameterNames.time, 2.0 } } );
            if( !ret) {
                MessageBox.Show( "error");
            }
        }
    }
}
