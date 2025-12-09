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
using BioNex.Shared.BarcodeMisreadDialog;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.Utils;

namespace BarcodeMisreadDialogTestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            List<BarcodeReadErrorInfo> info = new List<BarcodeReadErrorInfo>();
            info.Add( new BarcodeReadErrorInfo( "Rack 1, Slot 1", "", "Images\\Rack 1, Slot 1.jpg".ToAbsoluteAppPath()));
            info.Add( new BarcodeReadErrorInfo( "Rack 2, Slot 2", "", "Images\\Rack 2, Slot 2.jpg".ToAbsoluteAppPath()));
            info.Add( new BarcodeReadErrorInfo( "Rack 2, Slot 3", "", "Images\\Rack 2, Slot 3.jpg".ToAbsoluteAppPath()));

            BarcodeMisread dlg = new BarcodeMisread( info);
            dlg.ShowDialog();
            dlg.Close();
        }
    }
}
