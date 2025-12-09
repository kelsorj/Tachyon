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

namespace BioNex.HivePrototypePlugin
{
    /// <summary>
    /// Interaction logic for JogHome.xaml
    /// </summary>
    public partial class JogHome : UserControl
    {
        private HivePlugin _controller;
        public HivePlugin Controller
        {
            get { return _controller; }
            set {
                _controller = value;
                DataContext = _controller;
            }
        }

        public JogHome()
        {
            InitializeComponent();
        }
    }
}
