using System;
using System.Windows;
using System.ComponentModel;
using System.Threading;
using GalaSoft.MvvmLight.Command;
using BioNex.Shared.Utils;
using System.Windows.Media;
using GalaSoft.MvvmLight.Messaging;
using BioNex.Shared.DeviceInterfaces;
using System.Collections.Generic;

namespace BioNex.Hig
{
    /// <summary>
    /// This ViewModel could probably be broken up into two, where one is shared with the HiGIntegration assembly.
    /// Currently, there is duplicate functionality which should be refactored.
    /// </summary>
    public class ViewModel : INotifyPropertyChanged
    {
        public RelayCommand<bool> ConnectCommand { get; set; }
        public RelayCommand HomeAllAxesCommand { get; set; }
        public RelayCommand ServosOnCommand { get; set; }
        public RelayCommand ServosOffCommand { get; set; }
        public RelayCommand ResetInterlocksCommand { get; set; }
        public RelayCommand StopAllCommand { get; set; }
        public RelayCommand SpinCommand { get; set; }
        public RelayCommand AbortSpinCommand { get; set; }
        public RelayCommand OpenDoorToBucket1Command { get; set; }
        public RelayCommand OpenDoorToBucket2Command { get; set; }
        /*
        public RelayCommand WriteSerialNumberCommand { get; set; }
        public RelayCommand GetSerialNumberCommand { get; set; }
         */
        public RelayCommand CloseDoorCommand { get; set; }
        
        internal SynapsisModel _model { get; set; }
        private ThreadedUpdates _updater { get; set; }

        public ViewModel( SynapsisModel model)
        {
            InitializeCommands();
            _model = model;
            _model.SynapsisHiGModelConnected += new EventHandler(_model_SynapsisHiGModelConnected);
            _model.SynapsisHiGModelDisconnected += new EventHandler(_model_SynapsisHiGModelDisconnected);

            Accel = 100;
            Decel = 100;
            DesiredGs = 250;
            SpinTimeSeconds = 5;
            EngineeringTabVisibility = Visibility.Hidden;

            _updater = new ThreadedUpdates( "HiG position updater", PositionReaderCallback, 200);
        }

        void _model_SynapsisHiGModelDisconnected(object sender, EventArgs e)
        {
            StartPositionUpdateThread(false);
            PositionReaderCallback(); // call one last time to update connection state
        }

        void _model_SynapsisHiGModelConnected(object sender, EventArgs e)
        {
            StartPositionUpdateThread(true);
        }

        // DKM use IDispose instead?
        ~ViewModel()
        {
            StartPositionUpdateThread(false);
        }

        private void StartPositionUpdateThread( bool start)
        {
            if( start && !_updater.Running) {
                _updater.Start();
            } else if( !start && _updater.Running) {
                _updater.Stop();
            }
        }

        private bool _connected;
        public bool Connected
        {
            get { return _connected; }
            set
            {
                _connected = value;
                OnPropertyChanged( "Connected");
            }
        }

        private Visibility _engineering_tab_visibility;
        public Visibility EngineeringTabVisibility
        {
            get { return _engineering_tab_visibility; }
            set {
                _engineering_tab_visibility = value;
                OnPropertyChanged( "EngineeringTabVisibility");
            }
        }

        private void InitializeCommands()
        {
            ConnectCommand = new RelayCommand<bool>( ExecuteConnectCommand);
            HomeAllAxesCommand = new RelayCommand( ExecuteHomeAllAxesCommand, CanExecuteHomeAllAxesCommand);
            ServosOnCommand = new RelayCommand( ExecuteServosOnCommand, () => { return false; });
            ServosOffCommand = new RelayCommand( ExecuteServosOffCommand, () => { return false; });
            ResetInterlocksCommand = new RelayCommand( ExecuteResetInterlocksCommand);
            StopAllCommand = new RelayCommand( ExecuteStopAllCommand);
            SpinCommand = new RelayCommand(ExecuteSpin, CanExecuteSpin);
            AbortSpinCommand = new RelayCommand(ExecuteAbortSpin, CanExecuteAbortSpin);
            OpenDoorToBucket1Command = new RelayCommand( () => { ExecuteOpenDoorCommand( 0); }, CanExecuteOpenDoorCommand);
            OpenDoorToBucket2Command = new RelayCommand( () => { ExecuteOpenDoorCommand( 1); }, CanExecuteOpenDoorCommand);
            /*
            WriteSerialNumberCommand = new RelayCommand(ExecuteWriteSerialNumberCommand);
            GetSerialNumberCommand = new RelayCommand(ExecuteGetSerialNumberCommand);
             */
            CloseDoorCommand = new RelayCommand(ExecuteCloseDoorCommand, CanExecuteCloseDoorCommand);
        }

        private void UpdateEstimatedCycleTime()
        {
            try {
                // DKM 2012-04-05 allowing a NullReferenceException here making debugging painful.
                if( _model.SpindleAxis == null) {
                    EstimatedCycleTimeSeconds = 0;
                    return;
                }
                EstimatedCycleTimeSeconds = HigUtils.GetEstimatedCycleTime(DesiredGs, _model.RotationalRadiusMm, _model.SpindleAxis.Settings.Acceleration,
                                            Accel, _model.SpindleAxis.Settings.Acceleration, Decel, SpinTimeSeconds);                
            } catch( Exception) {
                EstimatedCycleTimeSeconds = 0;
            }
        }

        private int _accel;
        public int Accel
        {
            get { return _accel; }
            set {
                _accel = value;
                OnPropertyChanged( "Accel");
                UpdateEstimatedCycleTime();
            }
        }

        private int _decel;
        public int Decel
        {
            get { return _decel; }
            set {
                _decel = value;
                OnPropertyChanged( "Decel");
                UpdateEstimatedCycleTime();
            }
        }

        private int _desiredG;
        public int DesiredGs
        {
            get { return _desiredG; }
            set {
                _desiredG = value;
                OnPropertyChanged( "DesiredGs");
                UpdateEstimatedCycleTime();
            }
        }

        private int _spinTimeS;
        public int SpinTimeSeconds
        {
            get { return _spinTimeS; }
            set {
                _spinTimeS = value;
                OnPropertyChanged( "SpinTimeSeconds");
                UpdateEstimatedCycleTime();
            }
        }

        private double _estimated_cycle_time_s;
        public double EstimatedCycleTimeSeconds
        {
            get { return _estimated_cycle_time_s; }
            set {
                _estimated_cycle_time_s = value;
                OnPropertyChanged( "EstimatedCycleTimeSeconds");
            }
        }

        private double _currentGs;
        public double CurrentGs
        {
            get { return _currentGs; }
            set {
                _currentGs = value;
                OnPropertyChanged( "CurrentGs");
            }
        }

        private Brush _at_bucket1_color;
        public Brush AtBucket1Color
        {
            get { return _at_bucket1_color; }
            set {
                _at_bucket1_color = value;
                OnPropertyChanged( "AtBucket1Color");
            }
        }

        private Brush _at_bucket2_color;
        public Brush AtBucket2Color
        {
            get { return _at_bucket2_color; }
            set {
                _at_bucket2_color = value;
                OnPropertyChanged( "AtBucket2Color");
            }
        }

        private string _spin_tooltip;
        public string SpinCommandToolTip
        {
            get { return _spin_tooltip; }
            set
            {
                _spin_tooltip = value;
                OnPropertyChanged("SpinCommandToolTip");
            }
        }

        private string _abort_spin_tooltip;
        public string AbortSpinCommandToolTip
        {
            get { return _abort_spin_tooltip; }
            set
            {
                _abort_spin_tooltip = value;
                OnPropertyChanged("AbortSpinCommandToolTip");
            }
        }

        private void ExecuteConnectCommand(bool connect)
        {
            try {
                _model.Connected = connect;
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private void ExecuteHomeAllAxesCommand()
        {
            new Thread( () => _model.Home( true)) {Name = "HiG Home Thread"}.Start();
        }

        private bool CanExecuteHomeAllAxesCommand()
        {
            return Connected && (_model._execution_state.Idle || _model._execution_state.InErrorState);
        }

        private void ExecuteServosOnCommand()
        {
        }

        private void ExecuteServosOffCommand()
        {
        }

        private void ExecuteResetInterlocksCommand()
        {
            Messenger.Default.Send<ResetInterlocksMessage>( new ResetInterlocksMessage());
        }

        private void ExecuteStopAllCommand()
        {
            Messenger.Default.Send<SoftwareInterlockCommand>( null);
        }

        private void ExecuteSpinCommand(int accel, int decel, int g, int timeSeconds)
        {
            new Thread(() => _model.Spin(accel, decel, g, timeSeconds, true)) {Name = "HiG Spin Thread"}.Start();
        }

        private void ExecuteAbortSpin()
        {
            _model.Abort();
        }

        private bool CanExecuteAbortSpin()
        {
            bool result = ToolTipHelper.EvaluateToolTip("Abort the current spin in progress", ref _abort_spin_tooltip,
                          new Dictionary<Func<bool>, string> {
                              { () => Connected, "Not connected" },
                              { () => _model.Spinning, "Not spinning" }}
                          );
            OnPropertyChanged("AbortSpinCommandToolTip");
            return Connected && _model.Spinning;
        }

        private void ExecuteOpenDoorCommand( int bucket_index)
        {
            new Thread(() => _model.OpenShield( bucket_index, true)) {Name = "HiG Open Shield Thread"}.Start();
        }

        private bool CanExecuteOpenDoorCommand()
        {
            return Connected && _model.Homed && _model._execution_state.Idle;
        }

        private void ExecuteCloseDoorCommand()
        {
            new Thread(() => _model.CloseShield(true)) {Name = "HiG Close Shield Thread"}.Start();
        }

        private bool CanExecuteCloseDoorCommand()
        {
            return Connected && _model.Homed && (_model._execution_state.Idle || _model._execution_state.InErrorState);
        }

        private double CalculateGsFromRpm(double rpm)
        {
            return HigUtils.CalculateGsFromRpm(rpm, _model.RotationalRadiusMm);
        }

        private double CalculateRpmFromGs(double g)
        {
            return HigUtils.CalculateRpmFromGs( g, _model.RotationalRadiusMm);
        }

        private void PositionReaderCallback()
        {
            try {
                Connected = _model.Connected;
                CurrentGs = _model.CurrentGs;
                var num_encoder_lines = _model.SpindleAxis.Settings.EncoderLines;
                var current_position = HigUtils.ConvertIUToDegrees( _model.SpindleAxis.GetPositionCounts(), num_encoder_lines);
                var bucket2_position = HigUtils.ConvertIUToDegrees( _model.Bucket2Offset, num_encoder_lines);
                var window = 2 * _model.SpindleAxis.Settings.MoveDoneWindow;
                // DKM 2011-10-17 copied existing logic behind TestAngle to determine if we're at bucket 1 or bucket 2
                if( Math.Abs( current_position) < window) {
                    AtBucket1Color = Brushes.Green;
                    AtBucket2Color = Brushes.Silver;
                } else if( Math.Abs( current_position - bucket2_position) < window) {
                    AtBucket1Color = Brushes.Silver;
                    AtBucket2Color = Brushes.Green;
                } else {
                    AtBucket1Color = Brushes.Silver;
                    AtBucket2Color = Brushes.Silver;
                }
            } catch( Exception) {
            }
        }

        private void ExecuteSpin()
        {
            ExecuteSpinCommand(Accel, Decel, DesiredGs, SpinTimeSeconds);
        }

        /*
        private void ExecuteWriteSerialNumberCommand()
        {
            _model.WriteSerialNumber();
        }

        private void ExecuteGetSerialNumberCommand()
        {
            SerialNumberEntry = _model.InternalSerialNumber;
        }
         */

        /*
        private string _serial_number_entry;
        public string SerialNumberEntry
        {
            get
            {
                _serial_number_entry = _model.InternalSerialNumber;
                return _serial_number_entry;
            }
            set
            {
                _serial_number_entry = value;
                _model.InternalSerialNumber = _serial_number_entry;
            }
        }
         */

        private bool CanExecuteSpin()
        {
            bool result = ToolTipHelper.EvaluateToolTip("Spin plate", ref _spin_tooltip,
                          new Dictionary<Func<bool>, string> {
                              { () => Connected, "Not connected" },
                              { () => _model.Homed, "Not homed" },
                              { () => _model._execution_state.Idle, String.Format( "Busy with another operation: {0}", _model._execution_state.CurrentState) }}
                          );
            OnPropertyChanged("SpinCommandToolTip");
            return Connected && _model.Homed && _model._execution_state.Idle;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged( string propertyName)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( propertyName));
        }

        #endregion
    }
}
