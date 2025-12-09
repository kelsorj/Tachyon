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

namespace PlatePiercePluginTestApp
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
            _plugin = new PlatePiercePlugin.PlatePiercePlugin();
            Dictionary<string,string> properties = new Dictionary<string,string>();
            properties["Profile"] = "platepierce";
            DeviceManagerDatabase.DeviceInfo device_info = new DeviceManagerDatabase.DeviceInfo( "Velocity11", "PlatePierce", "PlatePierce instance", false, properties);
            uc_wrapper.SetPlugin( _plugin, device_info);
        }

        private void Pierce_Click(object sender, RoutedEventArgs e)
        {
            bool ret = _plugin.ExecuteCommand( PlatePiercePlugin.PlatePiercePlugin.Commands.Pierce.ToString(),
                                                                    new Dictionary<string,object> { { PlatePiercePlugin.PlatePiercePlugin.CommandParameterNames.pressure, 25 } } );
            if( !ret) {
                MessageBox.Show( "error");
            }
        }
    }
}
