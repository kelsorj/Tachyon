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
using BioNex.Shared.LibraryInterfaces;
using System.ComponentModel.Composition;
using GalaSoft.MvvmLight.Command;
using System.Threading;
using log4net;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using BioNex.Shared.Utils;
using BioNex.SynapsisPrototype;
using BioNex.Shared.DeviceInterfaces;
using BioNex.AmgenProtocolXmlParser;

namespace AmgenGreenMachine
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    [Export(typeof(ICustomerGUI))]
    public partial class AmgenGui : UserControl, ICustomerGUI, INotifyPropertyChanged
    {
        private ILog _log = LogManager.GetLogger( typeof( AmgenGui));
        public RelayCommand SelectProtococolCommand { get; set; }
        public RelayCommand HomeAllDevicesCommand { get; set; }
        private string _selected_protocol_path;
        public string SelectedProtocolPath
        {
            get { return _selected_protocol_path; }
            set {
                _selected_protocol_path = value;
                OnPropertyChanged( "SelectedProtocolPath");
            }
        }

        private static class ProtocolCommands
        {
            public static readonly string Prime = "prime";
            public static readonly string LoadSyringe = "load";
            public static readonly string Wash = "wash";
            public static readonly string MoveXyz = "movett";
            public static readonly string StackPlate = "stackplate";
            public static readonly string Aspirate = "aspirate";
            public static readonly string Dispense = "dispense";
            public static readonly string Transfer = "transfer";
            public static readonly string Mix = "mix";
        }

        //! \todo fix this fugly task list definition?
        // used Dictionary<string,object> because eventually ExecuteCommand needs it for parameters, and I didn't want to convert later
        private IList<Tuple<string,Dictionary<string,object>>> _tasks = new List<Tuple<string,Dictionary<string,object>>>();

        [Import]
        public ICustomSynapsisQuery _synapsis_query { get; set; }
        [Import]
        public DeviceManager _device_manager { get; set; }

        private AccessibleDeviceInterface _greenmachine;

        public AmgenGui()
        {
            InitializeComponent();
            this.DataContext = this;

            SelectProtococolCommand = new RelayCommand( ExecuteSelectProtocol);
            HomeAllDevicesCommand = new RelayCommand( ExecuteHomeAllDevices);
        }

        private void ExecuteSelectProtocol()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "XML files (*.xml)|*.xml";

            if( dlg.ShowDialog() == true) {
                // got a filename, so set it in the GUI
                SelectedProtocolPath = dlg.FileName;
                _log.Info( "user selected protocol file '" + SelectedProtocolPath + "'");
                // validate file?

                try {
                    AmgenParser parser = new AmgenParser();
                    parser.LoadProtocolFile( SelectedProtocolPath);
                    // make a copy of the tasks just in case
                    _tasks = parser.Tasks.ToList();
                } catch( Exception ex) {
                    MessageBox.Show( "Could not load protocol file: " + ex.Message);
                }
            }
        }

        private void ExecuteHomeAllDevices()
        {
            _log.Info( "Homing all devices");
            // homes all devices in non-blocking fashion
            if( !_synapsis_query.HomeAllDevices()) {
                string message = "Failed to home all devices.  Please try again.";
                MessageBox.Show( message);
                _log.Info( message);
                return;
            }

            // now we have to spin until everything is homed
            DateTime start = DateTime.Now;
            double timeout_s = 30;
            while( !_synapsis_query.AllDevicesHomed && (DateTime.Now - start).TotalSeconds < timeout_s) {
                Thread.Sleep( 100);
            }
            if( (DateTime.Now - start).TotalSeconds >= timeout_s) {
                string message = "Timed out while homing all devices.  Please try again.";
                MessageBox.Show( message);
                _log.Info( message);
            } else {
                _log.Info( "Successfully homed all devices");
            }
        }

        private void RunProtocolThread()
        {
            _log.Info( "Starting protocol thread");

            foreach( var task in _tasks) {
                string task_name = task.Item1;
                string properties = task.Item2.ToDictionaryString<string,object>();
                _log.Info( String.Format( "Executing task '{0}' with properties: {1}", task_name, properties));
                ExecuteTask( task_name, task.Item2);
            }
        }
        
        private void ExecuteTask( string task_name, Dictionary<string,object> properties)
        {
            if( task_name.ToLower() == ProtocolCommands.Aspirate) {
                _greenmachine.ExecuteCommand( BioNex.AmgenProtocolXmlParser.AmgenParser.ProtocolCommands.Aspirate.ToString(), properties);
            } else if( task_name.ToLower() == ProtocolCommands.Dispense) {
                _greenmachine.ExecuteCommand( BioNex.AmgenProtocolXmlParser.AmgenParser.ProtocolCommands.Dispense.ToString(), properties);
            } else if( task_name.ToLower() == ProtocolCommands.LoadSyringe) {
                _greenmachine.ExecuteCommand( BioNex.AmgenProtocolXmlParser.AmgenParser.ProtocolCommands.LoadSyringe.ToString(), properties);
            } else if( task_name.ToLower() == ProtocolCommands.Mix) {
                _greenmachine.ExecuteCommand( BioNex.AmgenProtocolXmlParser.AmgenParser.ProtocolCommands.Mix.ToString(), properties);
            } else if( task_name.ToLower() == ProtocolCommands.MoveXyz) {
                _log.Error( "MoveXyz not implemented yet");
            } else if( task_name.ToLower() == ProtocolCommands.Prime) {
                _greenmachine.ExecuteCommand( BioNex.AmgenProtocolXmlParser.AmgenParser.ProtocolCommands.Prime.ToString(), properties);
            } else if( task_name.ToLower() == ProtocolCommands.StackPlate) {
                _log.Error( "StackPlate not implemented yet -- will execute via storage or stacker interfaces in Synapsis, i.e. NOT Green Machine");
            } else if( task_name.ToLower() == ProtocolCommands.Transfer) {
                _greenmachine.ExecuteCommand( BioNex.AmgenProtocolXmlParser.AmgenParser.ProtocolCommands.Transfer.ToString(), properties);
            } else if( task_name.ToLower() == ProtocolCommands.Wash) {
                _log.Error( "Wash not implemented yet");
            }
        }

        private void RunProtocolComplete( IAsyncResult iar)
        {
            try {
                AsyncResult ar = (AsyncResult)iar;
                Action caller = (Action)ar.AsyncDelegate;
                caller.EndInvoke( iar);
            } catch( Exception ex) {
                string message = "Protocol complete, but an error was reported: " + ex.Message;
                MessageBox.Show( message);
                _log.Info( message);
            } finally {
                if( ProtocolComplete != null)
                    ProtocolComplete( this, null);
            }
        }

        #region ICustomerGUI Members

        public event EventHandler ProtocolComplete;
        //! \todo I think we can get rid of these events because no one uses them
        public event EventHandler AbortableTaskStarted;
        public event EventHandler AbortableTaskComplete;

        public string GUIName
        {
            get { return "Amgen Green Machine"; }
        }

        public bool Busy
        {
            get { return false; }
        }

        public string BusyReason
        {
            get { return ""; }
        }

        public bool CanExecuteStart(out IEnumerable<string> failure_reasons)
        {
            //! \todo this is lame, why did I make this method take an IEnumerable instead of an IList?
            List<string> reasons = new List<string>();
            if( _tasks.Count == 0) {
                reasons.Add( "No tasks to execute");
            }

            failure_reasons = reasons;
            return reasons.Count() == 0;
        }

        public bool ExecuteStart()
        {
            // kick off the thread that will process _tasks
            Action execution_thread = new Action( RunProtocolThread);
            execution_thread.BeginInvoke( RunProtocolComplete, null);
            return true;
        }

        public bool ShowProtocolExecuteButtons()
        {
            return true;
        }

        public bool CanClose()
        {
            return true;
        }

        public bool CanPause()
        {
            return true;
        }

        public void Close()
        {
        }

        public void CompositionComplete()
        {
            _greenmachine = _device_manager.GetAccessibleDeviceInterfaces().First( x => x.ProductName == "Green Machine");
        }

        public bool AllowDiagnostics()
        {
            return true;
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion
    }
}
