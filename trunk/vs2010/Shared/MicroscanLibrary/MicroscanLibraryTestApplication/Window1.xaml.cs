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
using BioNex.Shared.Microscan;
using BioNex.Shared.Utils;
using System.Drawing;

namespace MicroscanLibraryTestApplication
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private MicroscanReader _reader;

        // calibration settings
        private Boolean Gain { get; set; }
        private Boolean SymbolType { get; set; }
        private Int16 WOIMargin { get; set; }
        private MicroscanCommands.ShutterSpeed ShutterSpeed { get; set; }
        private MicroscanCommands.FocusPosition FocusPosition { get; set; }
        private MicroscanCommands.ImageFormat ImageFormat { get; set; }
        private MicroscanCommands.Processing Processing { get; set; }
        private MicroscanCommands.WOIFraming WOIFraming { get; set; }

        public Window1()
        {
            InitializeComponent();
            Gain = true;
            SymbolType = true;
            WOIMargin = 20;
            text_woi_margin.Text = WOIMargin.ToString();
            ShutterSpeed = MicroscanCommands.ShutterSpeed.Disabled;
            FocusPosition = MicroscanCommands.FocusPosition.Enabled;
            ImageFormat = MicroscanCommands.ImageFormat.Bitmap;
            Processing = MicroscanCommands.Processing.Medium;
            WOIFraming = MicroscanCommands.WOIFraming.Disabled;
            
            _reader = new MicroscanReader();
            Mini3ControlTab.Content = new Mini3Control( _reader);
        }

        private void Connect(object sender, RoutedEventArgs e)
        {
            // get com port
            String selected_port = combo_comport.SelectedValue.ToString();
            try {
                _reader.Connect(selected_port);
                _reader.LoadConfigurationDatabase( FileSystem.GetAppPath() + "\\barcode_reader_config.xml", true);
                list_output.Items.Add( "Connected to reader");
            } catch( UnauthorizedAccessException ex) {
                MessageBox.Show( "Could not connect to reader: " + ex.Message);
            }
        }

        /// <summary>
        /// tells the reader to read the barcode and then adds the barcode to the log
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Read(object sender, RoutedEventArgs e)
        {
            string barcode;
            try {
                barcode = _reader.Read();
            } catch( TimeoutException ex) {
                list_output.Items.Add( ex.Message);
                return;
            }
            list_output.Items.Add( barcode);
        }

        private void Gain_Click(object sender, RoutedEventArgs e)
        {
            CheckBox check = sender as CheckBox;
            if( check != null)
                Gain = check.IsChecked == true;
        }

        /// <summary>
        /// performs calibration with the parameters set in the GUI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Calibrate(object sender, RoutedEventArgs e)
        {
            list_output.Items.Add( "calibrating...");
            _reader.Calibrate( Gain, ShutterSpeed, FocusPosition, SymbolType, WOIFraming, Int16.Parse(text_woi_margin.Text), Processing);
            list_output.Items.Add( "calibration complete.");
        }

        private void ShutterSpeed_Changed(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if( combo != null)
                ShutterSpeed = TypeConversions.StringToEnum<MicroscanCommands.ShutterSpeed>(combo.SelectedValue.ToString());
        }

        private void FocusPosition_Changed(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if( combo != null)
                FocusPosition = TypeConversions.StringToEnum<MicroscanCommands.FocusPosition>(combo.SelectedValue.ToString());
        }

        private void WOIFraming_Changed(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if( combo != null)
                WOIFraming = TypeConversions.StringToEnum<MicroscanCommands.WOIFraming>(combo.SelectedValue.ToString());
        }

        private void SymbolType_Click(object sender, RoutedEventArgs e)
        {
            CheckBox check = sender as CheckBox;
            if( check != null)
                SymbolType = check.IsChecked == true;
        }

        private void Processing_Changed(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if( combo != null)
                Processing = TypeConversions.StringToEnum<MicroscanCommands.Processing>(combo.SelectedValue.ToString());
        }

        private void SaveImage(object sender, RoutedEventArgs e)
        {
            // save image
            string file_extension = (ImageFormat == MicroscanCommands.ImageFormat.Bitmap) ? "bmp" : "jpg";
            string filename = "test." + file_extension;
            string barcode = _reader.SaveImage( filename, ImageFormat, 90);            
            // display image
            list_output.Items.Add( "barcode in image is: " + barcode);
            image_barcode.Source = new BitmapImage( new Uri( BioNex.Shared.Utils.FileSystem.GetAppPath() + "\\" + filename, UriKind.Absolute));
        }

        private void ImageFormat_Changed(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if( combo != null)
                ImageFormat = TypeConversions.StringToEnum<MicroscanCommands.ImageFormat>(combo.SelectedValue.ToString());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // populate com ports
            String[] port_names = SerialPort.GetPortNames();
            combo_comport.Items.Clear();
            foreach( String port in port_names)
                combo_comport.Items.Add( port);
            combo_comport.SelectedIndex = 0;
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            _reader.Close();
        }
    }
}
