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
using GalaSoft.MvvmLight.Command;
using System.ComponentModel.Composition;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using BioNex.HivePrototypePlugin;
using BioNex.Hig;
using System.Threading;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using BioNex.Shared.IError;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using BioNex.Shared.BioNexGuiControls;

namespace BioNex.Plugins.HiGTestGui
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    [Export(typeof(ICustomerGUI))]
    public partial class Gui : UserControl, ICustomerGUI, INotifyPropertyChanged
    {
        public RelayCommand ExecuteHomeAllCommand { get; set; }

        public ExternalDataRequesterInterface _data_request { get; set; }

        [Import] public IError _error_interface { get; set; }

        private HivePlugin _robot;
        private IList<HigPlugin> _higs;
        private bool _running;

        private DispatcherTimer _timer;
        private List<CycleStateMachine> _cyclers;
        private Dictionary<CycleStateMachine,bool> _cycler_complete_status;

        private bool _home_button_clicked;

        private int _cycle_count;
        public int CycleCount
        {
            get { return _cycle_count; }
            set {
                _cycle_count = value;
                OnPropertyChanged( "CycleCount");
            }
        }

        private bool _cycle_with_hive;
        public bool CycleWithHive
        {
            get { return _cycle_with_hive; }
            set {
                _cycle_with_hive = value;
                OnPropertyChanged( "CycleWithHive");
            }
        }

        private string _homeall_tooltip;
        public string HomeAllToolTip
        {
            get { return _homeall_tooltip; }
            set {
                _homeall_tooltip = value;
                OnPropertyChanged( "HomeAllToolTip");
            }
        }

        private int _accel;
        public int Accel
        {
            get { return _accel; }
            set {
                _accel = value;
                OnPropertyChanged( "Accel");
            }
        }

        private int _decel;
        public int Decel
        {
            get { return _decel; }
            set {
                _decel = value;
                OnPropertyChanged( "Decel");
            }
        }

        private int _desired_g;
        public int DesiredG
        {
            get { return _desired_g; }
            set {
                _desired_g = value;
                OnPropertyChanged( "DesiredG");
            }
        }

        private int _spin_time_s;
        public int SpinTimeSeconds
        {
            get { return _spin_time_s; }
            set {
                _spin_time_s = value;
                OnPropertyChanged( "SpinTimeSeconds");
            }
        }

        private double _delay_between_cycles;
        public double DelayBetweenCycles
        {
            get { return _delay_between_cycles; }
            set {
                _delay_between_cycles = value;
                OnPropertyChanged( "DelayBetweenCycles");
            }
        }

        
        private bool _home_after_each_spin;
        public bool HomeAfterEachSpin
        {
            get { return _home_after_each_spin; }
            set
            {
                _home_after_each_spin = value;
                OnPropertyChanged("HomeAfterEachSpin");
            }
        }
        
        public class ProgressInfo : INotifyPropertyChanged
        {
            private int _min; public int Min { get { return _min; } set { _min = value; OnPropertyChanged( "Min"); } }
            private int _max; public int Max { get { return _max; } set { _max = value; OnPropertyChanged( "Max"); } }
            private int _value; public int Value { get { return _value; } set { _value = value; OnPropertyChanged( "Value"); } }
            private string _text; public string Text { get { return _text; } set { _text = value; OnPropertyChanged( "Text"); } }

            #region INotifyPropertyChanged Members

            public event PropertyChangedEventHandler PropertyChanged;
            public void OnPropertyChanged( string property_name)
            {
                if( PropertyChanged != null)
                    PropertyChanged( this, new PropertyChangedEventArgs( property_name));
            }

            #endregion
        }

        // DKM 2011-11-15 TODO: get rid of private backing variable because I don't think it's necessary for the ObservableCollection
        private Dictionary<HigPlugin,ProgressInfo> _run_status;
        public Dictionary<HigPlugin,ProgressInfo> RunStatus
        {
            get { return _run_status; }
            set {
                _run_status = value;
                OnPropertyChanged( "RunStatus");
            }
        }

        public ObservableCollection<string> AvailableLabware { get; set; }
        public string SelectedLabware { get; set; }

        [ImportingConstructor]
        public Gui( [Import] ExternalDataRequesterInterface dr, [Import] ILabwareDatabase lwdb)
        {
            InitializeComponent();
            RunStatus = new Dictionary<HigPlugin,ProgressInfo>();
            this.DataContext = this;
            _cyclers = new List<CycleStateMachine>();
            _cycler_complete_status = new Dictionary<CycleStateMachine,bool>();
            _data_request = dr;

            ExecuteHomeAllCommand = new RelayCommand( ExecuteHomeAll, CanExecuteHomeAll);
            GetDeviceReferences();

            AvailableLabware = new ObservableCollection<string>();
            foreach( var lw in lwdb.GetLabwareNames())
                AvailableLabware.Add( lw);
            SelectedLabware = "48 well HiG demo plate";

            CycleCount = 1;
            CycleWithHive = false;
            DesiredG = 1000;
            Accel = 100;
            Decel = 100;
            SpinTimeSeconds = 10;
            DelayBetweenCycles = 0;
            _timer = new DispatcherTimer();
            _timer.Tick += new EventHandler(_timer_Tick);
            _timer.Interval = new TimeSpan( 0, 0, 1);
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            for( int i=0; i<_higs.Count(); i++) {
                HigPlugin hig = _higs[i] as HigPlugin;
                double time = hig.EstimatedTimeRemainingInSeconds;
                RunStatus[hig].Text = String.Format( "{0}: {1:0.0} seconds remaining, {2} cycles completed", hig.Name, time < 0 ? 0 : time, RunStatus[hig].Value);
            }
        }

        /// <summary>
        /// used to save references to the devices we need to use in this test gui
        /// i.e. Hive, Bumblebee, and HiG
        /// </summary>
        private void GetDeviceReferences()
        {
            _robot = _data_request.GetRobotInterfaces().Where( x => (x as DeviceInterface).ProductName == "Hive").FirstOrDefault() as HivePlugin;
            _higs = _data_request.GetAccessibleDeviceInterfaces().Where( x => (x as DeviceInterface).ProductName == "HiG").Select( x => x as HigPlugin).ToList();

            // add progressbar for each HiG
            foreach( var hig in _higs) {
                RunStatus[hig] = new ProgressInfo();
                RunStatus[hig].Value = 0;
                RunStatus[hig].Max = 0;
            }
        }

        private void ExecuteHomeAll()
        {
            if( !_home_button_clicked)
                _home_button_clicked = true;

            if( CycleWithHive)
                _robot.Home();
            foreach( var hig in _higs)
                hig.Home();
        }

        private bool CanExecuteHomeAll()
        {
            bool all_homed = true;
            foreach( var hig in _higs) {
                if( hig.IsHomed == false) {
                    all_homed = false;
                    break;
                }
            }
            if( all_homed) {
                HomeAllToolTip = "All devices are already homed";
                return false;
            }

            if( _home_button_clicked) {
                HomeAllToolTip = "Already homing";
                return false;
            }
            HomeAllToolTip = "";
            if( _running) {
                HomeAllToolTip = "Cannot home while running cycle tester";
                return false;
            }
            HomeAllToolTip = "Home all connected devices";
            return true;
        }

        #region ICustomerGUI Members

        public event EventHandler ProtocolComplete;

        public event EventHandler AbortableTaskStarted { add {} remove {} }

        public event EventHandler AbortableTaskComplete { add {} remove {} }

        public string GUIName
        {
            get { return "HiG Test GUI"; }
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
            List<string> reasons = new List<string>();
            if( _running)
                reasons.Add( "Already running");
            if( _cycle_count <= 0)
                reasons.Add( "Enter a valid cycle count");
            if( (from x in _higs where x.Connected == false select x).Count() > 0)
                reasons.Add( "One or more devices is not connected");
            
            failure_reasons = reasons;
            return failure_reasons.Count() == 0;
        }

        public bool ExecuteStart()
        {
            _running = true;

            if( !CycleWithHive) {
                _timer.Start();
            }

            // DKM 2011-11-15 it was really lame to have all HiGs stop cycling if one HiG has an error, so I'm now
            //                trying to run each in its own state machine.            
            _cyclers.Clear();
            _cycler_complete_status.Clear();

            // set up progressbar max as cyclecount

            if( CycleWithHive) {
                HigPlugin hig = _higs.First();
                RunStatus[hig].Min = 0;
                RunStatus[hig].Max = CycleCount;
                RunStatus[hig].Value = 0;
                _cyclers.Add( new CycleStateMachine( SelectedLabware, CycleCount, DesiredG, Accel, Decel, SpinTimeSeconds, DelayBetweenCycles, _robot, hig, null, _error_interface, HomeAfterEachSpin));
            } else {
                foreach( var hig in _higs) {
                    RunStatus[hig].Min = 0;
                    RunStatus[hig].Max = CycleCount;
                    RunStatus[hig].Value = 0;
                    // DKM 2011-11-16 here is where you'll either spin or home`
                    CycleStateMachine sm = new CycleMultipleHigsOnlyStateMachine( SelectedLabware, CycleCount, DesiredG, Accel, Decel, SpinTimeSeconds, DelayBetweenCycles, hig, _error_interface, HomeAfterEachSpin);
                    //CycleHomeStateMachine sm = new CycleHomeStateMachine(CycleCount, DelayBetweenCycles, hig, _error_interface);
                    sm.CycleComplete += new EventHandler(sm_CycleComplete);
                    _cyclers.Add( sm);
                }
            }

            // start cyclers
            foreach( var cycler in _cyclers) {
                // add a flag to the "I'm done cycling" map
                _cycler_complete_status[cycler] = false;
                // start the cycle testing for this HiG
                CycleStateMachine temp_cycler = cycler;
                new Action( () => { temp_cycler.Start(); }).BeginInvoke( CycleThreadComplete, temp_cycler);
            }

            foreach( var cycler in _cyclers) {
                Console.WriteLine( _cycler_complete_status[cycler].ToString());
            }

            return true;
        }

        void sm_CycleComplete(object sender, EventArgs e)
        {
            CycleStateMachine sm = (CycleStateMachine)sender;
            if( sm == null)
                return;
            RunStatus[sm.Hig].Value++;
        }

        private void CycleThreadComplete( IAsyncResult iar)
        {
            try {
                AsyncResult ar = (AsyncResult)iar;
                Action caller = (Action)ar.AsyncDelegate;
                CycleStateMachine cycler = (CycleStateMachine)ar.AsyncState;
                cycler.CycleComplete -= sm_CycleComplete;
                caller.EndInvoke( iar);
                _cycler_complete_status[cycler] = true;
            } catch( Exception ex) {
                MessageBox.Show( String.Format( "Cycle test failed: {0}", ex.Message));
            } finally {
                foreach( var cycler in _cyclers) {
                   Console.WriteLine( _cycler_complete_status[cycler].ToString());
                }

                if( (from x in _cycler_complete_status where x.Value == false select x).Count() == 0) {
                    _timer.Stop();
                    _running = false;
                    if( ProtocolComplete != null)
                        ProtocolComplete( this, null);
                    }
            }
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
