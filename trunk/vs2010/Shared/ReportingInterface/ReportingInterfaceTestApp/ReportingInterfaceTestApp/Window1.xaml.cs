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

namespace ReportingInterfaceTestApp
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        ViewModel _vm = new ViewModel();

        public Window1()
        {
            InitializeComponent();
            this.DataContext = _vm;
        }

        private void button_select_plugin_directory_Click(object sender, RoutedEventArgs e)
        {
            // open file dialog
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
            dlg.ShowNewFolderButton = false;
            System.Windows.Forms.DialogResult result = dlg.ShowDialog();
            if( result == System.Windows.Forms.DialogResult.Cancel)
                return;
            _vm.PluginPath = dlg.SelectedPath;
            // load the plugins in this folder
            _vm.LoadPlugins();
        }
    }
}
