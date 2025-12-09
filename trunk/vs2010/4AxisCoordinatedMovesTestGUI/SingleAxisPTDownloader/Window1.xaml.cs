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
using BioNex.MotorLibrary;
using BioNex._3AxisPTGenerator;
using System.Windows.Threading;

namespace SingleAxisPTDownloader
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private MotorController _motor_controller = null;
        private MotorController.TSAxis _axis = null;
        private MotorController.MotorSettings _motor_settings = null;
        //private delegate void Executor( MotorController.IAxis axis, _1AxisPTTable table);
        //private Executor PTExecutor;
        private Queue<string> log_queue = new Queue<string>(32);    // used to keep a queue of messages for the Dispatcher thread to consume
        private DispatcherTimer timer = null;

        //private void ExecutePTTable( MotorController.IAxis axis, _1AxisPTTable table)
        //{
        //    foreach( _1AxisPTTable.PTTableRow row in table) {
        //        WriteToLog( String.Format( "{0}\t{1}", row.Position, row.Time));

        //    }
        //}

        private void WriteToLog( string msg)
        {
            // add the string to a queue, and the GUI thread will update the log for us via Dispatcher
            log_queue.Enqueue( msg);
        }

        public Window1()
        {
            InitializeComponent();
        }

        private void LogUpdateThread( object sender, EventArgs e)
        {
            if( log_queue.Count == 0)
                return;
            string msg = log_queue.Dequeue();
            text_log.AppendText( msg + "\n");
            text_log.ScrollToEnd();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            text_pt_filename.Text = @"C:\pt_test2.txt";
            try {
                _motor_controller = new MotorController( 6);
                _motor_settings = new MotorController.SlowMotorSettings();
                BioNex.MotorLibrary.MotorController.MotorSettings.Settings x_settings = _motor_settings["x"];
                _axis = new MotorController.TSAxis( "X", 255, 2000, 1, ref x_settings, @"..\..\setup.t.zip");
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds( 10);
            timer.Tick += LogUpdateThread;
            timer.Start();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            _axis.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            if( dlg.ShowDialog() == true)
                text_pt_filename.Text = dlg.FileName;
            ValidatePTFilename();
        }

        private void ValidatePTFilename()
        {
            //! \todo
        }

        private void PTExecutionComplete( IAsyncResult iar)
        {
            MessageBox.Show( "PT execution complete!");
        }

        private void button_execute_Click(object sender, RoutedEventArgs e)
        {
            if( text_pt_filename.Text == "") {
                MessageBox.Show( "You must select a file that contains valid PT data first!");
                return;
            }

            _axis.On();
            _1AxisPTTable table = new _1AxisPTTable( text_pt_filename.Text);
            // DKM 031610 I haven't used this in a long time, but it looks like I got rid of the StartPTMotion method.
            //_axis.StartPTMotion( table);
            // launch a thread in the thread pool to send the PT data
            //PTExecutor.BeginInvoke( _axis, table, new AsyncCallback(PTExecutionComplete), null);
        }
    }
}
