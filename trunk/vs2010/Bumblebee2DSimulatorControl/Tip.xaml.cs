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
using MVVM;

namespace BioNex.Bumblebee2DSimulatorControl
{
    /// <summary>
    /// Interaction logic for Tip.xaml
    /// </summary>
    public partial class Tip : UserControl
    {
        private TipViewModel _vm;
        public TipViewModel ViewModel
        {
            get { return _vm; }
            set {
                _vm = value;
                this.DataContext = _vm;
            }
        }

        public Tip()
        {
            InitializeComponent();
        }
    }
}
