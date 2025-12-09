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
using BioNex.Shared.Utils;

namespace BioNex.Bumblebee2DSimulatorControl
{
    /// <summary>
    /// Interaction logic for Plate.xaml
    /// </summary>
    public partial class Plate : UserControl
    {
        private PlateViewModel _vm;
        public PlateViewModel ViewModel
        {
            get { return _vm; }
            set {
                _vm = value;
                this.DataContext = _vm;
            }
        }

        public Plate()
        {
            InitializeComponent();
        }
    }
}
