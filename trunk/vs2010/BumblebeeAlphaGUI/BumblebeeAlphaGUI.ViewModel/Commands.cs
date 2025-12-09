using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Diagnostics;
using BioNex.BumblebeeAlphaGUI.Model;
using System.Windows;

namespace BioNex.BumblebeeAlphaGUI.ViewModel
{
    #region Menu commands
    namespace MenuCommands
    {
        public class ResetAllServosCommand : ICommand
        {
            private MainViewModel _vm;

            public ResetAllServosCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                try {
                    _vm.ResetServos();
                } catch( Exception ex) {
                    Debug.Assert( false, String.Format( "Failed to reset servos: {0}", ex.Message));
                }
            }

            #endregion
        }
        public class ServoOnAllMotorsCommand : ICommand
        {
            private MainViewModel _vm;

            public ServoOnAllMotorsCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                try {
                    _vm.ServoOn();
                } catch( Exception ex) {
                    Debug.Assert( false, String.Format( "Failed to servo on motors: {0}", ex.Message));
                }
            }

            #endregion
        }
        public class HomeAllAxesCommand : ICommand
        {
            private MainViewModel _vm;

            public HomeAllAxesCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                try {
                    _vm.Home();
                } catch( Exception ex) {
                    Debug.Assert( false, String.Format( "Failed to home device: {0}", ex.Message));
                }
            }

            #endregion
        }

        public class HomeAllDevicesCommand : ICommand
        {
            private MainViewModel _vm;

            public HomeAllDevicesCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                try {
                    _vm.HomeAllDevices();
                } catch( Exception ex) {
                    Debug.Assert( false, String.Format( "Failed to home all devices", ex.Message));
                }
            }

            #endregion
        }
    }
    #endregion

    #region Main commands
    namespace MainCommands
    {
        public class StartCommand : ICommand
        {
            private MainViewModel _vm;

            public StartCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                //List<IAxis> axes_not_homed = new List<IAxis>();
                //bool can_execute = _hw == null ? true : _hw.IsHomed( axes_not_homed);
                return _vm.AxesHomed && _vm.ProtocolState == Model.Model.ProtocolState.Idle;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.Execute();
            }

            #endregion
        }
        public class StopAllMotorsCommand : ICommand
        {
            private MainViewModel _vm;

            public StopAllMotorsCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                try {
                    _vm.StopAllMotors();
                } catch( Exception ex) {
                    Debug.Assert( false, String.Format( "Failed to stop all motors: {0}", ex.Message));
                }
            }

            #endregion
        }
        public class PauseCommand : ICommand
        {
            private MainViewModel _vm;

            public PauseCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return _vm.ProtocolState == Model.Model.ProtocolState.Running;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                try {
                    _vm.PauseScheduler();
                } catch( Exception ex) {
                    Debug.Assert( false, String.Format( "Failed to pause scheduler: {0}", ex.Message));
                }
            }
            #endregion
        }
        public class AbortCommand : ICommand
        {
            private MainViewModel _vm;

            public AbortCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return _vm.ProtocolState == Model.Model.ProtocolState.Paused || _vm.ProtocolState == Model.Model.ProtocolState.Running;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                try {
                    _vm.AbortScheduler();
                } catch( Exception ex) {
                    Debug.Assert( false, String.Format( "Failed to abort scheduler: {0}", ex.Message));
                }
            }

            #endregion
        }
        public class ResumeCommand : ICommand
        {
            private MainViewModel _vm;

            public ResumeCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return _vm.ProtocolState == Model.Model.ProtocolState.Paused;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                try {
                    _vm.ResumeScheduler();
                } catch( Exception ex) {
                    Debug.Assert( false, String.Format( "Failed to resume scheduler: {0}", ex.Message));
                }
            }

            #endregion
        }
    }
    #endregion

    #region Teach / move commands
    //-------------------------------------------------------------------------
    namespace TeachMoveCommands
    {        
        public class DisableXYAxesCommand : ICommand
        {
            private MainViewModel _vm;

            public DisableXYAxesCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return false;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                //! \todo how do you know whether or not the button is now toggled or not?
                bool disable = (bool)parameter;
                _vm.DisableXYAxes( disable);
            }

            #endregion            
        }
        public class ReloadTeachpointFileCommand : ICommand
        {
            private MainViewModel _vm;

            public ReloadTeachpointFileCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.LoadTeachpointFile();
            }

            #endregion            
        }
        public class ReturnChannelsHomeCommand : ICommand
        {
            private MainViewModel _vm;

            public ReturnChannelsHomeCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.ReturnChannelsHome();
            }

            #endregion            
        }
        public class ReturnStagesHomeCommand : ICommand
        {
            private MainViewModel _vm;

            public ReturnStagesHomeCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.ReturnStagesHome();
            }

            #endregion            
        }


        public class JogWUpCommand : ICommand
        {
            private MainViewModel _vm;

            public JogWUpCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.JogW( true);
            }

            #endregion
        }
        public class JogWDownCommand : ICommand
        {
            private MainViewModel _vm;

            public JogWDownCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.JogW( false);
            }

            #endregion
        }
        public class JogZUpCommand : ICommand
        {
            private MainViewModel _vm;

            public JogZUpCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.JogZ( false);
            }

            #endregion
        }
        public class JogZDownCommand : ICommand
        {
            private MainViewModel _vm;

            public JogZDownCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.JogZ( true);
            }

            #endregion
        }
        public class JogXLeftCommand : ICommand
        {
            private MainViewModel _vm;

            public JogXLeftCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.JogX( false);
            }

            #endregion
        }
        public class JogXRightCommand : ICommand
        {
            private MainViewModel _vm;

            public JogXRightCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.JogX( true);
            }

            #endregion
        }
        public class JogRCCWCommand : ICommand
        {
            private MainViewModel _vm;

            public JogRCCWCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.JogR( true);
            }

            #endregion
        }
        public class JogRCWCommand : ICommand
        {
            private MainViewModel _vm;

            public JogRCWCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.JogR( false);
            }

            #endregion
        }
        public class JogYAwayCommand : ICommand
        {
            private MainViewModel _vm;

            public JogYAwayCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.JogY( true);
            }

            #endregion
        }
        public class JogYTowardsCommand : ICommand
        {
            private MainViewModel _vm;

            public JogYTowardsCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.JogY( false);
            }

            #endregion
        }
        public class TeachULCommand : ICommand
        {
            private MainViewModel _vm;

            public TeachULCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.TeachUL();
                /*
                string message = String.Format("You may also teach the {0} teachpoint based on the current position.  Would you like to do this as well?", (teach_ul ? "lower right" : "upper left"));
                if (MessageBox.Show(message, "Confirm Auto-teach", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    AutoTeach(!teach_ul, channel_id, stage_id);
                */
            }

            #endregion
        }
        public class MoveAboveULCommand : ICommand
        {
            private MainViewModel _vm;

            public MoveAboveULCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                double distance = double.Parse( parameter.ToString());
                _vm.MoveAboveUL( distance);
            }

            #endregion
        }
        public class MoveToULCommand : ICommand
        {
            private MainViewModel _vm;

            public MoveToULCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.MoveToUL();
            }

            #endregion
        }
        public class TeachLRCommand : ICommand
        {
            private MainViewModel _vm;

            public TeachLRCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.TeachLR();
                /*
                string message = String.Format("You may also teach the {0} teachpoint based on the current position.  Would you like to do this as well?", (teach_ul ? "lower right" : "upper left"));
                if (MessageBox.Show(message, "Confirm Auto-teach", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    AutoTeach(!teach_ul, channel_id, stage_id);
                */
            }

            #endregion
        }
        public class MoveAboveLRCommand : ICommand
        {
            private MainViewModel _vm;

            public MoveAboveLRCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                double distance = double.Parse( parameter.ToString());
                _vm.MoveAboveLR( distance);
            }

            #endregion
        }
        public class MoveToLRCommand : ICommand
        {
            private MainViewModel _vm;

            public MoveToLRCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.MoveToLR();
            }

            #endregion
        }
        public class MoveAboveWashCommand : ICommand
        {
            private MainViewModel _vm;

            public MoveAboveWashCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.MoveAboveWash();
            }

            #endregion
        }
        public class MoveToWashCommand : ICommand
        {
            private MainViewModel _vm;

            public MoveToWashCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.MoveToWash();
            }

            #endregion
        }
        public class TeachWashCommand : ICommand
        {
            private MainViewModel _vm;

            public TeachWashCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                MessageBoxResult result = MessageBox.Show( "Are you sure you want to teach the wash station here?", "Confirm teachpoint", MessageBoxButton.YesNo);
                if( result == MessageBoxResult.No)
                    return;
                _vm.TeachWash();
            }

            #endregion
        }

        public class HomeXCommand : ICommand
        {
            private MainViewModel _vm;

            public HomeXCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                byte channel_id = byte.Parse( parameter.ToString());
                _vm.HomeX( channel_id);
            }

            #endregion
        }
        public class HomeYCommand : ICommand
        {
            private MainViewModel _vm;

            public HomeYCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                byte stage_id = byte.Parse( parameter.ToString());
                _vm.HomeY( stage_id);
            }

            #endregion
        }
        public class HomeZCommand : ICommand
        {
            private MainViewModel _vm;

            public HomeZCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                byte channel_id = byte.Parse( parameter.ToString());
                _vm.HomeZ( channel_id);
            }

            #endregion
        }
        public class HomeWCommand : ICommand
        {
            private MainViewModel _vm;

            public HomeWCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                byte channel_id = byte.Parse( parameter.ToString());
                _vm.HomeW( channel_id);
            }

            #endregion
        }
        public class HomeRCommand : ICommand
        {
            private MainViewModel _vm;

            public HomeRCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                byte stage_id = byte.Parse( parameter.ToString());
                _vm.HomeR( stage_id);
            }

            #endregion
        }

        public class TeachRobotPositionCommand : ICommand
        {
            private MainViewModel _vm;

            public TeachRobotPositionCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.TeachRobotPosition();
            }

            #endregion
        }

        public class TeachTipPositionCommand : ICommand
        {
            private MainViewModel _vm;

            public TeachTipPositionCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.TeachTipPosition();
            }

            #endregion
        }    

        public class TestTipOnCommand : ICommand
        {
            private MainViewModel _vm;

            public TestTipOnCommand( MainViewModel vm)
            {
                _vm = vm;
            }

            #region ICommand Members

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            public void Execute(object parameter)
            {
                _vm.TestTipOn();
            }

            #endregion
        }    

        
    }
    //-------------------------------------------------------------------------
    #endregion

    public partial class MainViewModel : BaseViewModel
    {
        #region Command properties
        // menu commands
        public ICommand ResetAllServosCommand { get; set; }
        public ICommand ServoOnAllMotorsCommand { get; set; }
        public ICommand HomeAllAxesCommand { get; set; }
        public ICommand HomeAllDevicesCommand { get; set; }
        // main commands
        public ICommand StartCommand { get; set; }
        public ICommand StopAllMotorsCommand { get; set; }
        public ICommand PauseCommand { get; set; }
        public ICommand AbortCommand { get; set; }
        public ICommand ResumeCommand { get; set; }
        // teach/move commands
        public ICommand ReturnChannelsHomeCommand { get; set; }
        public ICommand ReturnStagesHomeCommand { get; set; }
        public ICommand DisableXYAxesCommand { get; set; }
        public ICommand ReloadTeachpointFileCommand { get; set; }
        public ICommand JogWUpCommand { get; set; }
        public ICommand JogWDownCommand { get; set; }
        public ICommand JogZUpCommand { get; set; }
        public ICommand JogZDownCommand { get; set; }
        public ICommand JogXLeftCommand { get; set; }
        public ICommand JogXRightCommand { get; set; }
        public ICommand JogRCCWCommand { get; set; }
        public ICommand JogRCWCommand { get; set; }
        public ICommand JogYAwayCommand { get; set; }
        public ICommand JogYTowardsCommand { get; set; }
        public ICommand TeachULCommand { get; set; }
        public ICommand MoveAboveULCommand { get; set; }
        public ICommand MoveToULCommand { get; set; }
        public ICommand TeachLRCommand { get; set; }
        public ICommand MoveAboveLRCommand { get; set; }
        public ICommand MoveToLRCommand { get; set; }
        public ICommand MoveAboveWashCommand { get; set; }
        public ICommand MoveToWashCommand { get; set; }
        public ICommand TeachWashCommand { get; set; }
        public ICommand TeachRobotPositionCommand { get; set; }
        public ICommand TeachTipPositionCommand { get; set; }
        public ICommand TestTipOnCommand { get; set; }
        // homing
        public ICommand HomeXCommand { get; set; }
        public ICommand HomeYCommand { get; set; }
        public ICommand HomeZCommand { get; set; }
        public ICommand HomeWCommand { get; set; }
        public ICommand HomeRCommand { get; set; }
        #endregion

        private void InitializeCommands()
        {
            // menu commands
            ResetAllServosCommand = new MenuCommands.ResetAllServosCommand( this);
            ServoOnAllMotorsCommand = new MenuCommands.ServoOnAllMotorsCommand( this);
            HomeAllAxesCommand = new MenuCommands.HomeAllAxesCommand( this);
            HomeAllDevicesCommand = new MenuCommands.HomeAllDevicesCommand( this);
            // main commands
            StartCommand = new MainCommands.StartCommand( this);
            StopAllMotorsCommand = new MainCommands.StopAllMotorsCommand( this);
            PauseCommand = new MainCommands.PauseCommand( this);
            AbortCommand = new MainCommands.AbortCommand( this);
            ResumeCommand = new MainCommands.ResumeCommand( this);
            // teach / move commands
            ReturnChannelsHomeCommand = new TeachMoveCommands.ReturnChannelsHomeCommand( this);
            ReturnStagesHomeCommand = new TeachMoveCommands.ReturnStagesHomeCommand( this);
            DisableXYAxesCommand = new TeachMoveCommands.DisableXYAxesCommand( this);
            ReloadTeachpointFileCommand = new TeachMoveCommands.ReloadTeachpointFileCommand( this);
            JogWUpCommand = new TeachMoveCommands.JogWUpCommand( this);
            JogWDownCommand = new TeachMoveCommands.JogWDownCommand( this);
            JogZUpCommand = new TeachMoveCommands.JogZUpCommand( this);
            JogZDownCommand = new TeachMoveCommands.JogZDownCommand( this);
            JogXLeftCommand = new TeachMoveCommands.JogXLeftCommand( this);
            JogXRightCommand = new TeachMoveCommands.JogXRightCommand( this);
            JogRCCWCommand = new TeachMoveCommands.JogRCCWCommand( this);
            JogRCWCommand = new TeachMoveCommands.JogRCWCommand( this);
            JogYAwayCommand = new TeachMoveCommands.JogYAwayCommand( this);
            JogYTowardsCommand = new TeachMoveCommands.JogYTowardsCommand( this);
            TeachULCommand = new TeachMoveCommands.TeachULCommand( this);
            MoveAboveULCommand = new TeachMoveCommands.MoveAboveULCommand( this);
            MoveToULCommand = new TeachMoveCommands.MoveToULCommand( this);
            TeachLRCommand = new TeachMoveCommands.TeachLRCommand( this);
            MoveAboveLRCommand = new TeachMoveCommands.MoveAboveLRCommand( this);
            MoveToLRCommand = new TeachMoveCommands.MoveToLRCommand( this);
            MoveAboveWashCommand = new TeachMoveCommands.MoveAboveWashCommand( this);
            MoveToWashCommand = new TeachMoveCommands.MoveToWashCommand( this);
            TeachWashCommand = new TeachMoveCommands.TeachWashCommand( this);
            TeachRobotPositionCommand = new TeachMoveCommands.TeachRobotPositionCommand( this);
            TeachTipPositionCommand = new TeachMoveCommands.TeachTipPositionCommand( this);
            TestTipOnCommand = new TeachMoveCommands.TestTipOnCommand( this);
            // homing commands
            HomeXCommand = new TeachMoveCommands.HomeXCommand( this);
            HomeYCommand = new TeachMoveCommands.HomeYCommand( this);
            HomeZCommand = new TeachMoveCommands.HomeZCommand( this);
            HomeWCommand = new TeachMoveCommands.HomeWCommand( this);
            HomeRCommand = new TeachMoveCommands.HomeRCommand( this);
        }

        internal void LoadTeachpointFile()
        {
            _model.LoadTeachpointFile();
        }
    }
}
