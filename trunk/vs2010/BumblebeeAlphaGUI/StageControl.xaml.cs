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
using BioNex.BumblebeeAlphaGUI;
using System.Windows.Threading;
using BioNex.BumblebeeAlphaGUI.ViewModel;

namespace BioNex.BumblebeeAlphaGUI
{
    /// <summary>
    /// Interaction logic for StageControl.xaml
    /// </summary>
    public partial class StageControl : UserControl
    {
        private MainViewModel _vm;
        public StageControl( MainViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            this.DataContext = _vm;
        }
    }
}
