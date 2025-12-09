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
using BioNex.Shared.ErrorHandling;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.IError;

namespace BioNex.Shared
{
    /// <summary>
    /// Makes writing test containers for Synapsis plugins easier.  Inherits from
    /// IError and does the MEF plugin loading.  Simplifies process of sending
    /// device manager properties without actually needing to use the Device Manager
    /// library.
    /// </summary>
    public partial class PluginTestContainerUserControl : UserControl
    {
        public ObservableCollection<ErrorPanel> Errors { get; set; }
        private List<ErrorData> _errors;
        private DispatcherTimer _timer;

        private DeviceInterface _plugin;
        
        public PluginTestContainerUserControl()
        {
            InitializeComponent();
            
        }

        /// <summary>
        /// Tells container what plugin we want to wrap, and sends over the properties
        /// that the Device Manager would normally send.
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="properties"></param>
        public void SetPlugin( DeviceInterface plugin, DeviceManagerDatabase.DeviceInfo properties)
        {
            _plugin = plugin;
            _plugin.SetProperties( properties);
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            try {
                _plugin.Connect();
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private void ShowDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            try {
                _plugin.ShowDiagnostics();
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }
    }
}
