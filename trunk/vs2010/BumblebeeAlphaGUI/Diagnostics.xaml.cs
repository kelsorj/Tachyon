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
using System.Windows.Shapes;
using System.Windows.Threading;
using BioNex.BumblebeeAlphaGUI.ViewModel;

namespace BioNex.BumblebeeAlphaGUI
{
    /// <summary>
    /// Interaction logic for Diagnostics.xaml
    /// </summary>
    public partial class Diagnostics : Window
    {
        private DispatcherTimer _timer;
        private MainViewModel _vm;

        public Diagnostics( MainViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            teachpoint_control.ViewModel = _vm;
        }

        /// <summary>
        /// closes the dialog without applying any configuration changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            teachpoint_control.ViewModel = _vm;
            jog_move_control.ViewModel = _vm;
            // GUI update timer
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds( 100);
            _timer.Tick += MotorSensorUpdateThread;
            _timer.Start();
        }

        private void MotorSensorUpdateThread( object sender, EventArgs e)
        {
            // rather than update the GUI as before, let's try something different and
            // cache the position values and then use databinding from XAML.  This is 
            // because there are now multiple consumers of the same data.
            try {
                _vm.UpdatePositions();                
            } catch( Exception) {
                // don't keep updating the log, it's really annoying in this timer
                //WriteToLog( ex.Message);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //! \todo replace hardcoded units with calculated value
            /*
            teachpoint_control.Height = ((Window)sender).ActualHeight - 105;
            teachpoint_control.Width = ((Window)sender).ActualWidth - 25;
            jogmove_control.Height = ((Window)sender).ActualHeight - 105;
            jogmove_control.Width = ((Window)sender).ActualWidth - 25;
            */
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            _timer.Tick -= MotorSensorUpdateThread;
            _timer.Stop();
        }
    }
}
