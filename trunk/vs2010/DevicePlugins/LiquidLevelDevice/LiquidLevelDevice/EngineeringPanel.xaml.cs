using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.Utils;
using GalaSoft.MvvmLight.Command;
using log4net;

namespace BioNex.LiquidLevelDevice
{
    /// <summary>
    /// Interaction logic for EngineeringPanel.xaml
    /// </summary>
    public partial class EngineeringPanel : UserControl, INotifyPropertyChanged
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(EngineeringPanel));

        public EngineeringPanel()
        {
            InitializeComponent();
            DataContext = this;
            this.Dispatcher.ShutdownStarted += UserControl_Shutdown;
        }

        ILLSensorModel _model;
        private void SetModel()
        {
            _model = _plugin.Model;

            XAxisHomeCommand = new RelayCommand(() => HomeAxis(_model.XAxis), () => IsIdle);
            YAxisHomeCommand = new RelayCommand(() => HomeAxis(_model.YAxis), () => IsIdle);
            ZAxisHomeCommand = new RelayCommand(() => HomeAxis(_model.ZAxis), () => IsIdle);
            RAxisHomeCommand = new RelayCommand(() => HomeAxis(_model.RAxis), () => IsIdle);

            XAxisJogPosCommand = new RelayCommand(() => RelativeMove(_model.XAxis, XAxisIncrement, XAxisVelocity, XAxisAcceleration), () => IsIdle);
            XAxisJogNegCommand = new RelayCommand(() => RelativeMove(_model.XAxis, -XAxisIncrement, XAxisVelocity, XAxisAcceleration), () => IsIdle);
            YAxisJogPosCommand = new RelayCommand(() => RelativeMove(_model.YAxis, YAxisIncrement, YAxisVelocity, YAxisAcceleration), () => IsIdle);
            YAxisJogNegCommand = new RelayCommand(() => RelativeMove(_model.YAxis, -YAxisIncrement, YAxisVelocity, YAxisAcceleration), () => IsIdle);
            ZAxisJogPosCommand = new RelayCommand(() => RelativeMove(_model.ZAxis, ZAxisIncrement, ZAxisVelocity, ZAxisAcceleration), () => IsIdle);
            ZAxisJogNegCommand = new RelayCommand(() => RelativeMove(_model.ZAxis, -ZAxisIncrement, ZAxisVelocity, ZAxisAcceleration), () => IsIdle);
            RAxisJogPosCommand = new RelayCommand(() => RelativeMove(_model.RAxis, RAxisIncrement, RAxisVelocity, RAxisAcceleration), () => IsIdle);
            RAxisJogNegCommand = new RelayCommand(() => RelativeMove(_model.RAxis, -RAxisIncrement, RAxisVelocity, RAxisAcceleration), () => IsIdle);

            XAxisServoOnCommand = new RelayCommand(() => _model.XAxis.Enable(true, true), () => IsIdle);
            XAxisServoOffCommand = new RelayCommand(() => _model.XAxis.Enable(false, true), () => IsIdle);
            YAxisServoOnCommand = new RelayCommand(() => _model.YAxis.Enable(true, true), () => IsIdle);
            YAxisServoOffCommand = new RelayCommand(() => _model.YAxis.Enable(false, true), () => IsIdle);
            ZAxisServoOnCommand = new RelayCommand(() => _model.ZAxis.Enable(true, true), () => IsIdle);
            ZAxisServoOffCommand = new RelayCommand(() => _model.ZAxis.Enable(false, true), () => IsIdle);
            RAxisServoOnCommand = new RelayCommand(() => _model.RAxis.Enable(true, true), () => IsIdle);
            RAxisServoOffCommand = new RelayCommand(() => _model.RAxis.Enable(false, true), () => IsIdle);

            HomeAllCommand = new RelayCommand(HomeAll, () => IsIdle);

            TeachHereCommand = new RelayCommand(TeachHere, () => IsIdle);
            MoveToTeachCommand = new RelayCommand(() => MoveToTeachPosition(), () => IsIdle);

            MoveToParkCommand = new RelayCommand(() => MoveToParkPosition(), () => IsIdle);

            JogIncrements = new List<double> { 0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1.0, 2.0, 5.0, 10.0, 30.0, 45.0 };
            XAxisIncrement = 0.1;
            YAxisIncrement = 0.1;
            ZAxisIncrement = 0.1;
            RAxisIncrement = 0.1;

            TeachpointTypes = new List<string> { "Capture Position", "Portrait Load Position (R Axis Only)", "Landscape Load Position (R Axis Only)" };
            SelectedTeachpoint = TeachpointTypes[0];

            XAxisTP = _model.Properties.GetDouble(LLProperties.X_TP);
            YAxisTP = _model.Properties.GetDouble(LLProperties.Y_TP);
            ZAxisTP = _model.Properties.GetDouble(LLProperties.Z_TP);
            RAxisTP = _model.Properties.GetDouble(LLProperties.R_TP);

            _engineering_position_updater = new ThreadedUpdates("LLS enginnering tab position update thread", UpdatePositionThread, 500);

            _model.ConnectionStateChangedEvent += ConnectionStateChanged;
        }

        void ConnectionStateChanged(object sender, int index, bool connected, string status)
        {
            if (connected)
            {
                OnPropertyChanged("XAxisVelocity");
                OnPropertyChanged("XAxisAcceleration");
                OnPropertyChanged("YAxisVelocity");
                OnPropertyChanged("YAxisAcceleration");
                OnPropertyChanged("ZAxisVelocity");
                OnPropertyChanged("ZAxisAcceleration");
                OnPropertyChanged("RAxisVelocity");
                OnPropertyChanged("RAxisAcceleration");
            }
        }

        ILLSensorPlugin _plugin;
        public ILLSensorPlugin Plugin
        {
            set { _plugin = (ILLSensorPlugin)value; SetModel(); }
        }

        public RelayCommand XAxisHomeCommand { get; set; }
        public RelayCommand YAxisHomeCommand { get; set; }
        public RelayCommand ZAxisHomeCommand { get; set; }
        public RelayCommand RAxisHomeCommand { get; set; }


        public List<string> TeachpointTypes { get; set; }

        string _selected_teachpoint;
        public string SelectedTeachpoint 
        {
            get { return _selected_teachpoint; }
            set 
            { 
                _selected_teachpoint = value;
                switch((TeachpointType)SelectedTeachpointCombo.SelectedIndex)
                {
                    default: RAxisTP = _model.Properties.GetDouble(LLProperties.R_TP); MoveToTeachpointButton.IsEnabled = true; break;
                    case TeachpointType.Portrait: RAxisTP = _model.Properties.GetDouble(LLProperties.R_PORTRAIT_TP); MoveToTeachpointButton.IsEnabled = false;  break;
                    case TeachpointType.Landscape: RAxisTP = _model.Properties.GetDouble(LLProperties.R_LANDSCAPE_TP); MoveToTeachpointButton.IsEnabled = false; break;
                }
            }
        }

        public Visibility RAxisVisibility
        {
            get
            {
                return (_model != null && (_model as LLSensorModel).HasRAxis) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public List<double> JogIncrements { get; set; }
        public double XAxisIncrement { get; set; }
        public double YAxisIncrement { get; set; }
        public double ZAxisIncrement { get; set; }
        public double RAxisIncrement { get; set; }
 
        public double XAxisVelocity { get { return _model == null ? double.NaN : _model.XAxisVelocity; } set { _model.XAxisVelocity = value; } }
        public double XAxisAcceleration { get { return _model == null ? double.NaN : _model.XAxisAcceleration; } set { _model.XAxisAcceleration = value; } }

        public double YAxisVelocity { get { return _model == null ? double.NaN : _model.YAxisVelocity; } set { _model.YAxisVelocity = value; } }
        public double YAxisAcceleration { get { return _model == null ? double.NaN : _model.YAxisAcceleration; } set { _model.YAxisAcceleration = value; } }

        public double ZAxisVelocity { get { return _model == null ? double.NaN : _model.ZAxisVelocity; } set { _model.ZAxisVelocity = value; } }
        public double ZAxisAcceleration { get { return _model == null ? double.NaN : _model.ZAxisAcceleration; } set { _model.ZAxisAcceleration = value; } }

        public double RAxisVelocity { get { return _model == null ? double.NaN : _model.RAxisVelocity; } set { _model.RAxisVelocity = value; } }
        public double RAxisAcceleration { get { return _model == null ? double.NaN : _model.RAxisAcceleration; } set { _model.RAxisAcceleration = value; } }


        public RelayCommand XAxisJogPosCommand { get; set; }
        public RelayCommand XAxisJogNegCommand { get; set; }
        public RelayCommand YAxisJogPosCommand { get; set; }
        public RelayCommand YAxisJogNegCommand { get; set; }
        public RelayCommand ZAxisJogPosCommand { get; set; }
        public RelayCommand ZAxisJogNegCommand { get; set; }
        public RelayCommand RAxisJogPosCommand { get; set; }
        public RelayCommand RAxisJogNegCommand { get; set; }

        public RelayCommand XAxisServoOffCommand { get; set; }
        public RelayCommand XAxisServoOnCommand { get; set; }
        public RelayCommand YAxisServoOffCommand { get; set; }
        public RelayCommand YAxisServoOnCommand { get; set; }
        public RelayCommand ZAxisServoOffCommand { get; set; }
        public RelayCommand ZAxisServoOnCommand { get; set; }
        public RelayCommand RAxisServoOffCommand { get; set; }
        public RelayCommand RAxisServoOnCommand { get; set; }

        public RelayCommand HomeAllCommand { get; set; }

        public RelayCommand TeachHereCommand { get; set; }
        public RelayCommand MoveToTeachCommand { get; set; }

        public RelayCommand MoveToParkCommand { get; set; }

        #region double_XAxisPosition
        double _x_pos;
        public double XAxisPosition
        {
            get { return _x_pos; }
            set
            {
                _x_pos = value;
                OnPropertyChanged("XAxisPosition");
            }
        }
        #endregion

        #region double_YAxisPosition
        double _y_pos;
        public double YAxisPosition
        {
            get { return _y_pos; }
            set
            {
                _y_pos = value;
                OnPropertyChanged("YAxisPosition");
            }
        }
        #endregion

        #region double_ZAxisPosition
        double _z_pos;
        public double ZAxisPosition
        {
            get { return _z_pos; }
            set
            {
                _z_pos = value;
                OnPropertyChanged("ZAxisPosition");
            }
        }
        #endregion

        #region double_RAxisPosition
        double _r_pos;
        public double RAxisPosition
        {
            get { return _r_pos; }
            set
            {
                _r_pos = value;
                OnPropertyChanged("RAxisPosition");
            }
        }
        #endregion

        public double XAxisTP { get; set; }
        public double YAxisTP { get; set; }
        public double ZAxisTP { get; set; }

        double _raxistp;
        public double RAxisTP { get { return _raxistp; } set { _raxistp = value; OnPropertyChanged("RAxisTP"); } }

        private void TeachHere()
        {
            var tpt = (TeachpointType)SelectedTeachpointCombo.SelectedIndex;
            _model.TeachHere(tpt);
            
            XAxisTP = _model.Properties.GetDouble(LLProperties.X_TP);
            YAxisTP = _model.Properties.GetDouble(LLProperties.Y_TP);
            ZAxisTP = _model.Properties.GetDouble(LLProperties.Z_TP);
            switch(tpt)
            {
                default:                        RAxisTP = _model.Properties.GetDouble(LLProperties.R_TP); break;
                case TeachpointType.Portrait:   RAxisTP = _model.Properties.GetDouble(LLProperties.R_PORTRAIT_TP); break;
                case TeachpointType.Landscape:  RAxisTP = _model.Properties.GetDouble(LLProperties.R_LANDSCAPE_TP); break;
            }
            OnPropertyChanged("XAxisTP");
            OnPropertyChanged("YAxisTP");
            OnPropertyChanged("ZAxisTP");
            OnPropertyChanged("RAxisTP");
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string property_name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property_name));
        }

        #endregion

        ThreadedUpdates _engineering_position_updater;

        void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.IsVisible && _engineering_position_updater != null && !_engineering_position_updater.Running)
                _engineering_position_updater.Start();
        }

        void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_engineering_position_updater != null && _engineering_position_updater.Running)
                _engineering_position_updater.Stop();
        }

        void UserControl_Shutdown(object sender, EventArgs e)
        {
            UserControl_Unloaded(sender, null);
        }

        void UpdatePositionThread()
        {
            if (_model == null || !_model.MotorsConnected)
                return;
            XAxisPosition = _model.GetAxisPositionMM(LLSensorModelConsts.XAxis);
            YAxisPosition = _model.GetAxisPositionMM(LLSensorModelConsts.YAxis);
            ZAxisPosition = _model.GetAxisPositionMM(LLSensorModelConsts.ZAxis);
            if( (_model as LLSensorModel).HasRAxis)
                RAxisPosition = _model.GetAxisPositionMM(LLSensorModelConsts.RAxis);
        }

        bool IsIdle { get { return _model.MotorsConnected 
                            && _home_thread == null 
                            && _home_all_thread == null
                            && _relative_move_thread == null 
                            && _move_to_teach_thread == null 
                            && _move_to_park_thread == null; } }

        Thread _home_thread = null;
        void HomeAxis(IAxis axis)
        {
            _home_thread = new Thread(() =>
                {
                    try
                    {
                        axis.Home(true);
                    }
                    catch (AxisException e)
                    {
                        _log.Error(string.Format("LiquidLevelDevice: Error while homing {0} : {1}", axis.Name, e.ToString()));
                    }
                    _home_thread = null;
                    Dispatcher.Invoke(new Action(() => { CommandManager.InvalidateRequerySuggested(); }));
                });
            _home_thread.Start();
        }

        Thread _home_all_thread = null;
        void HomeAll()
        {
            _home_all_thread = new Thread(() =>
            {
                try
                {
                    _model.Home();
                }
                catch (Exception e)
                {
                    _log.Error("LiquidLevelDevice: Error while homing ", e);
                }
                _home_all_thread = null;
                Dispatcher.Invoke(new Action(() => { CommandManager.InvalidateRequerySuggested(); }));
            });
            _home_all_thread.Start();
        }

        Thread _relative_move_thread = null;
        void RelativeMove(IAxis axis, double offset, double velocity, double acceleration)
        {
            _relative_move_thread = new Thread(() =>
                {
                    try
                    {
                        var current_position = axis.GetPositionMM();
                        axis.MoveAbsolute(current_position + offset, velocity, acceleration, use_trap: true); // default - uses TPOS settling to determine end of move (short timeout, but may not reach position if tuning is wrong)
                        //axis.MoveAbsolute(current_position + offset, velocity, acceleration, 0, 0, true, axis.Settings.MoveDoneWindow, axis.Settings.SettlingTimeMS, true, true, false);  // non-default -- uses Window in MotorSettings.xml to determine end of move (10 sec timeout)
                    }
                    catch (AxisException e)
                    {
                        _log.Error(string.Format("LiquidLevelDevice: Error while moving {0} : {1}", axis.Name, e.ToString()));
                    }
                    _relative_move_thread = null;
                    Dispatcher.Invoke(new Action(() => { CommandManager.InvalidateRequerySuggested(); }));
                });
            _relative_move_thread.Start();
        }

        Thread _move_to_teach_thread = null;
        void MoveToTeachPosition()
        {
            _move_to_teach_thread = new Thread(() =>
            {
                _model.MoveRelativeToTeachpoint(0, 0, 0, true);
                _move_to_teach_thread = null;
                Dispatcher.Invoke(new Action(() => { CommandManager.InvalidateRequerySuggested(); }));
            });
            _move_to_teach_thread.Start();
        }

        Thread _move_to_park_thread = null;
        void MoveToParkPosition()
        {
            var tpt = (TeachpointType)SelectedTeachpointCombo.SelectedIndex;
            _move_to_park_thread = new Thread(() =>
            {
                _model.MoveToPark(tpt);
                _move_to_park_thread = null;
                Dispatcher.Invoke(new Action(() => { CommandManager.InvalidateRequerySuggested(); }));
            });
            _move_to_park_thread.Start();
        }
    }
}