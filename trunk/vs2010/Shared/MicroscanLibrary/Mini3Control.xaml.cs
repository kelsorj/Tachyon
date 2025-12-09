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
using GalaSoft.MvvmLight.Command;
using System.Collections.ObjectModel;

namespace BioNex.Shared.Microscan
{
    /// <summary>
    /// Interaction logic for Mini3Control.xaml
    /// </summary>
    public partial class Mini3Control : UserControl
    {
        public Mini3Control( MicroscanReader reader)
        {
            InitializeComponent();
            configuration_tab._reader = reader;
            configuration_tools_tab.Reader = reader;
            terminal_tab.Reader = reader;
        }
    }
}
