using System;
using System.Runtime.Remoting.Messaging;
using System.Windows;
using System.Windows.Controls;
using BioNex.Hive.Executor;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.IError;
using BioNex.Shared.Utils;
using GalaSoft.MvvmLight.Command;

namespace BioNex.HivePrototypePlugin
{
    /// <summary>
    /// Interaction logic for AutoTeach.xaml
    /// </summary>
    public partial class AutoTeach : UserControl
    {
        public RelayCommand NewPanelCommand { get; set; }
        public RelayCommand DeletePanelCommand { get; set; }
        public RelayCommand AutoTeachCommand { get; set; }

        private AutoTeachConfiguration _teach_config { get; set; }
        public HivePlugin Plugin { get; set; }
        public IOInterface IO { get; set; }
        public IError ErrorInterface { get; set; }

        public AutoTeach()
        {
            InitializeComponent();
            DataContext = this;

            AutoTeachCommand = new RelayCommand( ExecuteAutoTeach);
        }

        private void ExecuteAutoTeach()
        {
            try {
                // this was commented out because OpenFileDialog was crashing under Windows XP
                /*
                // prompt for a filename
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.Filter = "Auto teach configuration file (*.xml)|*.xml";
                dlg.InitialDirectory = FileSystem.GetAppPath();
                if( dlg.ShowDialog() == true) {
                    string filename = dlg.FileName;
                    // load the configuration
                    _teach_config = FileSystem.LoadXmlConfiguration<AutoTeachConfiguration>( filename);
                } else {
                    return;
                }
                 */
                _teach_config = FileSystem.LoadXmlConfiguration<AutoTeachConfiguration>( FileSystem.GetAppPath() + "\\config\\teach_config.xml");
            } catch( Exception ex) {
                MessageBox.Show( String.Format( "Could not select auto teach configuration file: {0}", ex.Message));
                return;
            }

            // now spawn a thread that does the auto teaching
            Action auto_teach = new Action( AutoTeachThread);
            auto_teach.BeginInvoke( AutoTeachComplete, null);
        }

        private void AutoTeachThread()
        {
            // loop over each panel
            foreach( AutoTeachConfiguration.Panel panel in _teach_config.Panels) {
                AutoTeachPanel( panel);
            }
        }

        private void AutoTeachPanel( AutoTeachConfiguration.Panel panel)
        {
            // run the state machine for this panel
            AutoTeachStateMachine sm = new AutoTeachStateMachine( Plugin.DiagnosticsExecutor, _teach_config, panel, Plugin.Hardware, IO);
            sm.Start();
        }

        private void AutoTeachComplete( IAsyncResult iar)
        {
            try {
                AsyncResult ar = (AsyncResult)iar;
                Action caller = (Action)ar.AsyncDelegate;
                caller.EndInvoke( iar);
            } catch( Exception ex) {
                MessageBox.Show( String.Format( "Failed to auto-teach: {0}", ex.Message));
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {

        }
    }
}
