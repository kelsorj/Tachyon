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
using System.ComponentModel;
using System.Windows.Threading;

namespace BioNex.BPS140Plugin
{
    /// <summary>
    /// Interaction logic for DiagnosticsPanel.xaml
    /// </summary>
    public partial class DiagnosticsPanel : UserControl
    {
        private ViewModel VM { get; set; }
        private DispatcherTimer Timer { get; set; }

        public DiagnosticsPanel( BPS140 controller)
        {
            VM = new ViewModel( controller);
            InitializeComponent();
            DataContext = VM;

            Timer = new DispatcherTimer();
            Timer.Interval = new TimeSpan( 0, 0, 0, 0, 100);
            Timer.Tick += new EventHandler(Timer_Tick);
            Timer.Start();
        }

        void Timer_Tick(object sender, EventArgs e)
        {
            VM.UpdateSensors();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            VM.UpdateInventoryView();
        }
    }
}
