using System;
using BioNex.Shared.DeviceInterfaces;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

namespace BioNex.BumblebeePlugin.ViewModel
{
    public partial class MainViewModel
    {
        // ----------------------------------------------------------------------
        // commands.
        // ----------------------------------------------------------------------
        public RelayCommand< bool> ConnectCommand { get; set; }
        public RelayCommand ServosOnCommand { get; set; }
        public RelayCommand ServosOffCommand { get; set; }
        public RelayCommand ResetInterlocksCommand { get; set; }
        public RelayCommand StopAllCommand { get; set; }
        // ----------------------------------------------------------------------
        public RelayCommand ReloadMotorSettingsCommand { get; set; }
        public RelayCommand ReloadTeachpointsCommand { get; set; }
        // ----------------------------------------------------------------------
        public RelayCommand< string> HomeAxisCommand { get; set; }
        /// <remarks>
        /// object parameter because we pass a multi command parameter
        /// </remarks>
        public RelayCommand< object> ServoAxisCommand { get; set; }
        public RelayCommand< string> JogPositiveCommand { get; set; }
        public RelayCommand< string> JogNegativeCommand { get; set; }
        public RelayCommand< string> MoveToULCommand { get; set; }
        public RelayCommand< string> MoveToLRCommand { get; set; }
        public RelayCommand MoveAboveTipShuckerCommand { get; set; }
        public RelayCommand MoveToTipShuckerCommand { get; set; }
        public RelayCommand MoveToLandscapePositionCommand { get; set; }
        public RelayCommand MoveToPortraitPositionCommand { get; set; }
        public RelayCommand MoveToWasherPositionCommand { get; set; }
        public RelayCommand< string> MoveToTipWasherTeachpointCommand { get; set; }
        // ----------------------------------------------------------------------
        public RelayCommand TeachULCommand { get; set; }
        public RelayCommand TeachLRCommand { get; set; }
        public RelayCommand TeachWashStationCommand { get; set; }
        public RelayCommand TeachLandscapePositionCommand { get; set; }
        public RelayCommand TeachPortraitPositionCommand { get; set; }
        public RelayCommand TeachWasherPositionCommand { get; set; }
        public RelayCommand< string> TeachTipWasherTeachpointCommand { get; set; }
        // ----------------------------------------------------------------------
        public RelayCommand HomeDeviceCommand { get; set; }
        public RelayCommand ReturnChannelHomeCommand { get; set; }
        public RelayCommand ReturnAllChannelsHomeCommand { get; set; }
        public RelayCommand< string> PressTipOnCommand { get; set; }
        public RelayCommand ShuckTipOffCommand { get; set; }
        public RelayCommand ShuckAllTipsOffCommand { get; set; }
        public RelayCommand RandomTipTestCommand { get; set; }
        public RelayCommand LoadTipShuttleCommand { get; set; }
        public RelayCommand UnloadTipShuttleCommand { get; set; }
        public RelayCommand< string> WashTipsCommand { get; set; }
        public RelayCommand TestCommand1 { get; set; }
        public RelayCommand TestCommand2 { get; set; }
        public RelayCommand TestCommand3 { get; set; }
        public RelayCommand TestCommand4 { get; set; }
        // ----------------------------------------------------------------------
        // public RelayCommand AutoTeachEverythingCommand { get; set; }
        // public RelayCommand TelemetryToggleCommand { get; set; }
        // ----------------------------------------------------------------------
        public string TipName { get; set; }
        public string ServiceToggleButtonContent { get { return Model.SchedulerIsRunning() ? "Pause scheduler" : "Resume scheduler"; }}
        // ----------------------------------------------------------------------
        private void InitializeRelayCommands()
        {
            ConnectCommand                      = new RelayCommand< bool>( Connect, ( connect) => CanExecuteConnect( connect));
            ServosOnCommand                     = new RelayCommand( () => Model.ServosOn( true, true), () => CanExecuteServoOn( true));
            ServosOffCommand                    = new RelayCommand( () => Model.ServosOn( false, true), () => CanExecuteServoOn( false));
            ResetInterlocksCommand              = new RelayCommand( ExecuteResetInterlocks);
            StopAllCommand                      = new RelayCommand( ExecuteStopAll, CanExecuteStopAll);
            // ------------------------------------------------------------------
            ReloadMotorSettingsCommand          = new RelayCommand( () => Model.LoadSettings());
            ReloadTeachpointsCommand            = new RelayCommand( () => Model.LoadTeachpointFile());
            // ------------------------------------------------------------------
            HomeAxisCommand                     = new RelayCommand< string>(( axis_name) => Model.HomeAxis( SelectedChannelID, SelectedStageID, axis_name), ( axis_name) => CanExecuteHomeAxis( SelectedChannelID, SelectedStageID, axis_name));
            ServoAxisCommand                    = new RelayCommand< object>( ServoAxis, CanExecuteServoAxis);
            JogPositiveCommand                  = new RelayCommand< string>(( axis_name) => Model.JogPositive( SelectedChannelID, SelectedStageID, axis_name, JogIncrements[ axis_name]), ( axis_name) => CanExecuteJogPositive( axis_name, JogIncrements));
            JogNegativeCommand                  = new RelayCommand< string>(( axis_name) => Model.JogPositive( SelectedChannelID, SelectedStageID, axis_name, -JogIncrements[ axis_name]), ( axis_name) => CanExecuteJogNegative( axis_name, JogIncrements));
            MoveToULCommand                     = new RelayCommand< string>(( mm_above) => Model.MoveToStageTeachpoint( SelectedChannelID, SelectedStageID, double.Parse( mm_above), true), ( mm_above) => CanExecuteMoveToUL());
            MoveToLRCommand                     = new RelayCommand< string>(( mm_above) => Model.MoveToStageTeachpoint( SelectedChannelID, SelectedStageID, double.Parse( mm_above), false), ( mm_above) => CanExecuteMoveToLR());
            MoveAboveTipShuckerCommand          = new RelayCommand( () => Model.MoveToTipShucker( SelectedChannelID, SelectedStageID, true), CanExecuteMoveAboveTipShucker);
            MoveToTipShuckerCommand             = new RelayCommand( () => Model.MoveToTipShucker( SelectedChannelID, SelectedStageID, false), CanExecuteMoveToTipShucker);
            MoveToLandscapePositionCommand      = new RelayCommand( () => Model.MoveToRobotTeachpoint( SelectedStageID, 0));
            MoveToPortraitPositionCommand       = new RelayCommand( () => Model.MoveToRobotTeachpoint( SelectedStageID, 1));
            MoveToWasherPositionCommand         = new RelayCommand( () => Model.MoveToRobotTeachpoint( SelectedStageID, 0));
            MoveToTipWasherTeachpointCommand    = new RelayCommand< string>(( command_arguments) => Model.MoveToTipWasherTeachpoint( SelectedStageID, command_arguments), ( command_arguments) => CanExecuteMoveToTipWasherTeachpoint( SelectedStageID, command_arguments));
            // ------------------------------------------------------------------
            TeachULCommand                      = new RelayCommand( () => Model.TeachUpperLeft( SelectedChannelID, SelectedStageID), () => CanExecuteTeach( SelectedChannelID, SelectedStageID));
            TeachLRCommand                      = new RelayCommand( () => Model.TeachLowerRight( SelectedChannelID, SelectedStageID), () => CanExecuteTeach( SelectedChannelID, SelectedStageID));
            TeachWashStationCommand             = new RelayCommand( () => Model.TeachWashStation( SelectedChannelID), CanExecuteTeachWash);
            TeachLandscapePositionCommand       = new RelayCommand( () => Model.TeachRobotPickup( SelectedStageID, 0), CanExecuteTeachRobotPickup);
            TeachPortraitPositionCommand        = new RelayCommand( () => Model.TeachRobotPickup( SelectedStageID, 1), CanExecuteTeachRobotPickup);
            TeachWasherPositionCommand          = new RelayCommand( () => Model.TeachRobotPickup( SelectedStageID, 0), CanExecuteTeachRobotPickup);
            TeachTipWasherTeachpointCommand     = new RelayCommand< string>(( command_arguments) => Model.TeachTipWasherTeachpoint( SelectedStageID, command_arguments), ( command_arguments) => CanExecuteTeachTipWasherTeachpoint( SelectedStageID, command_arguments));
            // ------------------------------------------------------------------
            HomeDeviceCommand                   = new RelayCommand( () => Model.Home( true), () => CanExecuteHome()); // pass this to Model.Home() so that we have a way to get to the error handler
            ReturnChannelHomeCommand            = new RelayCommand( () => Model.ReturnChannelHome( SelectedChannelID));
            ReturnAllChannelsHomeCommand        = new RelayCommand( () => Model.ReturnAllChannelsHome( true));
            PressTipOnCommand                   = new RelayCommand< string>(( position_press) => Model.PressTipOn( SelectedChannelID, SelectedStageID, TipName, bool.Parse( position_press)), ( position_press) => CanExecutePressTipOn( bool.Parse( position_press)));
            ShuckTipOffCommand                  = new RelayCommand( () => Model.ShuckTipOff( SelectedChannelID), CanExecuteShuckTipOff);
            ShuckAllTipsOffCommand              = new RelayCommand( () => Model.ShuckAllTipsOff(), CanExecuteShuckAllTipsOff);
            RandomTipTestCommand                = new RelayCommand( () => Model.RandomTipTest( SelectedStageID), CanExecuteRandomTipTest);
            LoadTipShuttleCommand               = new RelayCommand( () => Model.LoadTipShuttle( SelectedStageID), () => CanExecuteLoadTipShuttle( SelectedStageID));
            UnloadTipShuttleCommand             = new RelayCommand( () => Model.UnloadTipShuttle( SelectedStageID), () => CanExecuteUnloadTipShuttle( SelectedStageID));
            WashTipsCommand                     = new RelayCommand< string>(( command_arguments) => Model.WashTips( SelectedStageID, ( command_arguments == "debug")), ( command_arguments) => CanExecuteWashTips( SelectedStageID, command_arguments));
            TestCommand1                        = new RelayCommand( ExecuteTest1);
            TestCommand2                        = new RelayCommand( ExecuteTest2);
            TestCommand3                        = new RelayCommand( () => Model.CycleTips( SelectedStageID), () => CanExecuteTest3( SelectedStageID));
            TestCommand4                        = new RelayCommand( ExecuteTest4);
            // ------------------------------------------------------------------
            // AutoTeachEverythingCommand       = new RelayCommand( AutoTeachAllPoints, CanExecuteAutoTeachAllPoints);
            // TelemetryToggleCommand           = new RelayCommand( () => Model.TelemetryToggle(), () => CanExecuteTelemetryToggle());
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteConnect( bool connect)
        {
            return true;
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Allows diagnostics to connect to the hardware, then update the GUI 
        /// </summary>
        /// <param name="connect"></param>
        private void Connect( bool connect)
        {
            Model.Connect( connect);
            Initialize();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteServoOn( bool on)
        {
            return Connected;
        }
        // ----------------------------------------------------------------------
        private void ExecuteResetInterlocks()
        {
            Messenger.Default.Send( new ResetInterlocksMessage());
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteStopAll()
        {
            return Model.HasOnlyOneIoInterface();
        }
        // ----------------------------------------------------------------------
        public void ExecuteStopAll()
        {
            // DKM 2011-03-31 no longer use TS_Stop to do stop all axes.  Instead, we are going to
            // set output bit 3 on the IO module to trigger the laser curtain
            // _ts.StopAllAxes();
            Messenger.Default.Send< SoftwareInterlockCommand>( null);
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteHomeAxis( byte channel_id, byte stage_id, string axis_name)
        {
            if( axis_name != "X")
                return Connected;

            // ensure that the Z axis is above 1mm below home.  this will need to deal with axis flipping
            if (!Model.IsChannelAtSafeZ( channel_id)) {
                HomeXAxisToolTip = String.Format( "The Z axis for channel {0} is not at a safe position.  Please try homing Z first, then home X.", channel_id);
                return false;
            }

            // set the tooltip accordingly
            HomeXAxisToolTip = String.Format( "Home the X axis for channel {0}", channel_id);
            return true;
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteServoAxis( object parameter)
        {
            return true;
        }
        // ----------------------------------------------------------------------
        private void ServoAxis( object parameter)
        {
            var p = parameter as ServoOnOffAxisParameter;
            if( p == null)
                return;
            string axis_name = p.ID.ToUpper();
            Model.ServoEnable( SelectedChannelID, SelectedStageID, axis_name, p.ServoOn);
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteJogPositive( string axis_name, BioNex.BumblebeePlugin.ViewModel.Increments jog_increments)
        {
            if( !Connected){
                JogPositiveToolTip = "Device not connected";
                return false;
            }
            JogPositiveToolTip = "Jog axis in the positive direction";
            return true;
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteJogNegative( string axis_name, BioNex.BumblebeePlugin.ViewModel.Increments jog_increments)
        {
            if( !Connected){
                JogNegativeToolTip = "Device not connected";
                return false;
            }
            JogNegativeToolTip = "Jog axis in the negative direction";
            return true;
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteMoveToUL()
        {
            return CanExecuteMoveChannelStageHelper( String.Format( "Move channel {0} to the upper-left corner of stage {1}", SelectedChannelID, SelectedStageID), "MoveAboveULToolTip", ref _move_above_ul_tooltip );
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteMoveToLR()
        {
            return CanExecuteMoveChannelStageHelper( String.Format( "Move channel {0} to the lower-right corner of stage {1}", SelectedChannelID, SelectedStageID), "MoveAboveLRToolTip", ref _move_above_lr_tooltip );
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteMoveAboveTipShucker()
        {
            return CanExecuteMoveChannelStageHelper( String.Format( "Move channel {0} above the tip shuck station", SelectedChannelID), "MoveAboveTipShuckToolTip", ref _move_above_tipshuck_tooltip);
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteMoveToTipShucker()
        {
            return CanExecuteMoveChannelStageHelper( String.Format( "Move channel {0} to the tip shuck station", SelectedChannelID), "MoveToTipShuckToolTip", ref _move_to_tipshuck_tooltip);
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteMoveToTipWasherTeachpoint( byte shuttle_id, string command_arguments)
        {
            return true;
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteTeach( byte channel_id, byte stage_id)
        {
            return true;
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteTeachWash()
        {
            return CanExecuteMoveChannelStageHelper( String.Format( "Teach channel {0} tip shuck position", SelectedChannelID), "TeachTipShuckToolTip", ref _teach_tipshuck_tooltip);
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteTeachRobotPickup()
        {
            try {
                // get the current y and r positions for this stage
                double current_y = CurrentChannelAndStagePositions.Y;
                double current_r = CurrentChannelAndStagePositions.R;
                // get the current Y and R limits
                bool ok = Model.VerifyCheckStagePosition( SelectedStageID, current_y, current_r);
                TeachRobotPickupTooltip = ok ? "Click to teach the stage's robot teachpoint here" : "The stage cannot be taught at this position because it is outside of its travel limits.";
                return ok;
            } catch( NullReferenceException) {
                TeachRobotPickupTooltip = "You cannot teach the stage because it is disabled";
                return false;
            }
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteTeachTipWasherTeachpoint( byte shuttle_id, string command_arguments)
        {
            return true;
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteHome()
        {
            return Connected;
        }
        // ----------------------------------------------------------------------
        private bool CanExecutePressTipOn( bool position_press)
        {
            return CanExecuteMoveChannelStageHelper( String.Format( "Press tip onto channel {0}", SelectedChannelID), "PressTipOnToolTip", ref _press_tip_on_tooltip);
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteShuckTipOff()
        {
            return CanExecuteMoveChannelStageHelper( String.Format( "Shuck tip from channel {0}", SelectedChannelID), "ShuckTipOffToolTip", ref _shuck_tip_off_tooltip);
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteShuckAllTipsOff()
        {
            return true;
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteRandomTipTest()
        {
            return CanExecuteMoveChannelStageHelper( String.Format( "Execute random tip test for channel {0}", SelectedChannelID), "RandomTipTestToolTip", ref _random_tip_test_tooltip);
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteLoadTipShuttle( byte shuttle_id)
        {
            return Model.Hardware.GetTipShuttle( shuttle_id) != null;
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteUnloadTipShuttle( byte shuttle_id)
        {
            return Model.Hardware.GetTipShuttle( shuttle_id) != null;
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteWashTips( byte shuttle_id, string command_arguments)
        {
            return true;
        }
        // ----------------------------------------------------------------------
        private void ExecuteTest1()
        {
            Model.FlushDispatcher();
        }
        // ----------------------------------------------------------------------
        private void ExecuteTest2()
        {
            if( Model.SchedulerIsRunning()){
                Model.PauseScheduler();
            } else{
                Model.ResumeScheduler();
            }
            RaisePropertyChanged( "ServiceToggleButtonContent");
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteTest3( byte shuttle_id)
        {
            return Model.Hardware.GetTipShuttle( shuttle_id) != null;
        }
        // ----------------------------------------------------------------------
        private void ExecuteTest4()
        {
        }
        // ----------------------------------------------------------------------
        // helper functions.
        // ----------------------------------------------------------------------
        private bool CanExecuteMoveChannelStageHelper( string success_tooltip, string property_name, ref string tooltip)
        {
            if( !Connected){
                tooltip = "Not connected";
                RaisePropertyChanged( property_name);
                return false;
            }
            bool all_axes_homed = Model.Hardware.GetChannel( SelectedChannelID).IsHomed() && Model.Hardware.GetStage( SelectedStageID).IsHomed();
            if( !all_axes_homed){
                tooltip = "Not all of the necessary axes are homed";
                RaisePropertyChanged( property_name);
                return false;
            }
            tooltip = success_tooltip;
            RaisePropertyChanged( property_name);
            return true;
        }
        // ----------------------------------------------------------------------
        /* not used.
        // ----------------------------------------------------------------------
        private void AutoTeachAllPoints()
        {
            // assume that the teachpoint exists, or the button wouldn't be enabled
            // Positions tp = Model.GetULTeachpoint( SelectedChannelID, SelectedStageID);
            // based off of this teachpoint, teach the lower right corner
            Model.AutoTeach( false, SelectedChannelID, SelectedStageID);
            // now pop up the window that will let the user enter in stage position information
            AutoTeachEverything auto_teach_window = new AutoTeachEverything( Model);
            auto_teach_window.ShowDialog();
            auto_teach_window.Close();
        }
        // ----------------------------------------------------------------------
        private bool CanExecuteAutoTeachAllPoints()
        {
            return false;
            // make sure the teachpoint exists
            Positions tp;
            try {
                tp = Model.GetULTeachpoint( SelectedChannelID, SelectedStageID);
            } catch( KeyNotFoundException) {
                AutoTeachEverythingToolTip = String.Format( "You must teach the upper left corner of stage {0} with channel {1} before you can auto teach everything else", SelectedStageID, SelectedChannelID);
                return false;
            }
            
            return true;
        }
        */
    }
}
