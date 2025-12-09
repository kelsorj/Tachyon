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
using System.Windows.Threading;

namespace BioNex.BNX1536Plugin
{
    /// <summary>
    /// Interaction logic for DiagnosticsPanel.xaml
    /// </summary>
    public partial class DiagnosticsPanel : UserControl
    {
        private ViewModel _vm;
        DispatcherTimer _timer;

        public DiagnosticsPanel( ViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            this.DataContext = _vm;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _timer = new DispatcherTimer();
            _timer.Interval = new TimeSpan( 0, 0, 0, 0, 500);
            _timer.Tick += new EventHandler(_timer_Tick);
            _timer.Start();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            _vm.QueryStatus();
        }
    }
}
