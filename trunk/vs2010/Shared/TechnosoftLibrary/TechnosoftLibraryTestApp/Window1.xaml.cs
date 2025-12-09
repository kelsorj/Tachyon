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
using BioNex.Shared.TechnosoftLibrary;

namespace TechnosoftLibraryTestApp
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private TechnosoftConnection _connection { get; set; }

        public Window1()
        {
            InitializeComponent();
            _connection = new TechnosoftConnection();
            string exe_path = BioNex.Shared.Utils.FileSystem.GetAppPath();
            _connection.LoadConfiguration( "tsm_testapp_motor_settings.xml", exe_path);
            cycler.SetTechnosoftConnection( _connection);
        }
    }
}
