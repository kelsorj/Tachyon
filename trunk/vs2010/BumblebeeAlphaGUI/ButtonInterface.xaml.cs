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
using BioNex.Shared.Utils;
using System.Windows.Controls.Primitives; // for Popup
using System.Windows.Threading;
using BioNex.BumblebeeAlphaGUI.ViewModel; // for DispatcherTimer

namespace BioNex.BumblebeeAlphaGUI
{
    /// <summary>
    /// Interaction logic for ButtonInterface.xaml
    /// </summary>
    public partial class ButtonInterface : UserControl
    {
        private MainViewModel _vm;
        public MainViewModel ViewModel
        {
            set { 
                _vm = value;
                this.DataContext = _vm;
            }
        }

        public ButtonInterface()
        {
            InitializeComponent();
        }
    }
}
