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
using BioNex.BumblebeeAlphaGUI.ViewModel;
using System.Windows.Forms;

namespace BioNex.BumblebeeAlphaGUI
{
    /// <summary>
    /// Interaction logic for Setup.xaml
    /// </summary>
    public partial class Setup : Window
    {
        MainViewModel _vm;

        public Setup( MainViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            text_motor_settings_path.Text = _vm.MotorSettingsPath;
            text_hardware_config_path.Text = _vm.HardwareConfigurationPath;
            text_tsm_setup_folder.Text = _vm.TSMSetupFolder;
            text_teachpoint_path.Text = _vm.TeachpointPath;
        }

        private void button_ok_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            _vm.MotorSettingsPath = text_motor_settings_path.Text;
            _vm.HardwareConfigurationPath = text_hardware_config_path.Text;
            _vm.TSMSetupFolder = text_tsm_setup_folder.Text;
            _vm.TeachpointPath = text_teachpoint_path.Text;
            Hide();
        }

        private void button_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Hide();
        }

        private void button_select_motor_settings_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = "*.xml";
            dlg.Filter = "XML Files (*.xml)|*.xml";
            dlg.FileName = text_motor_settings_path.Text;
            if( dlg.ShowDialog() == true) {
                text_motor_settings_path.Text = dlg.FileName;
            }
        }

        private void button_select_hardware_config_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = "*.xml";
            dlg.Filter = "XML Files (*.xml)|*.xml";
            dlg.FileName = text_hardware_config_path.Text;
            if( dlg.ShowDialog() == true) {
                text_hardware_config_path.Text = dlg.FileName;
            }
        }

        private void button_select_tsm_setup_folder_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folder_dialog = new FolderBrowserDialog();
            DialogResult result = folder_dialog.ShowDialog();
            if( folder_dialog.SelectedPath != String.Empty) {
                text_tsm_setup_folder.Text = folder_dialog.SelectedPath;
            }
        }

        private void button_select_teachpoint_path_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = "*.xml";
            dlg.Filter = "XML Files (*.xml)|*.xml";
            dlg.FileName = text_teachpoint_path.Text;
            if( dlg.ShowDialog() == true) {
                text_teachpoint_path.Text = dlg.FileName;
            }
        }
    }
}
