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
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.ErrorHandling;
using BioNex.BumblebeeAlphaGUI.ViewModel;

namespace BioNex.BumblebeeAlphaGUI
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class JogMoveControl : UserControl
    {
        private MainViewModel _vm;
        public MainViewModel ViewModel
        {
            get { return _vm; }
            set {
                _vm = value;
                this.DataContext = _vm;
            }
        }

        public JogMoveControl()
        {
            InitializeComponent();
        }
    }
}
