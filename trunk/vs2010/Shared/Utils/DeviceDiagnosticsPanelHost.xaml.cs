using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BioNex.Shared.Utils
{
    /// <summary>
    /// Interaction logic for DeviceDiagnosticsPanelHost.xaml
    /// </summary>
    public partial class DeviceDiagnosticsPanelHost : Window
    {
        public DeviceDiagnosticsPanelHost()
        {
            //InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try {
                Uri iconUri = new Uri("pack://application:,,,/Images/BioNex.ico", UriKind.RelativeOrAbsolute);
                Icon = BitmapFrame.Create(iconUri);
            } catch( Exception) {
            }
        }
    }
}
