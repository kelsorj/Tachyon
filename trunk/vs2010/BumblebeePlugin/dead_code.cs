// ----------------------------------------------------------------------
/* dead code from AlphaHardware -- hardware.cs
 * OLD HOMING TECHNIQUES
 * OTHER OLD STUFF
// ----------------------------------------------------------------------
public bool IsOn()
{
    var available_channels = from c in Channels where c.IsAvailable() select c;
    foreach( Stage s in Stages)
        if( !s.IsOn())
            return false;
    foreach( Channel c in available_channels)
        if( !c.IsOn())
            return false;
    return true;
}
public void SetSystemSpeed( int speed)
{
    // iterate over all of the motor assemblies and set the speed for each axis\
    foreach( Channel c in Channels) {
        c.SetSpeedFactor( speed);
    }
    foreach( Stage s in Stages) {
        s.SetSpeedFactor( speed);
    }
}
public System.Collections.IEnumerator GetEnumerator()
{
    foreach( Channel c in Channels)
        yield return c;
}
System.Collections.IEnumerator IEnumerable.GetEnumerator()
{
    foreach( Channel c in Channels)
        yield return c;
}
public void HomeY()
{
    HomeAxesHelper( (from s in Stages select s.GetY()), false);
}
public void HomeR()
{
    HomeAxesHelper( (from s in Stages select s.GetR()), false);
}
public void ResetAxisFault( byte axis_id)
{
    _ts.GetAxes()[axis_id].ResetFaults();
}
public void StopAllMotors()
{
    //! \todo use first item in dictionary -- how?
    _ts.StopAllAxes();
}
public void PumpsOn( bool on)
{
    _ts.GetAxes()[11].SetOutput( 13, !on);
    _ts.GetAxes()[11].SetOutput( 25, !on);
}
// ----------------------------------------------------------------------
#region Homing
// ----------------------------------------------------------------------
public delegate void HomeDelegate( IError error_interface, bool called_from_diags);
// ----------------------------------------------------------------------
private void ResetAllControllers()
{
    foreach( IAxis axis in _ts.GetAxes().Values)
        axis.ResetDrive();
}
// ----------------------------------------------------------------------
/// <summary>
/// Homes all axes in their proper order, i.e.
/// Z, X/Y/R, W after fluid check
/// Blocks until complete
/// </summary>
public void Home( IError error_interface, bool called_from_diags)
{
    // DKM 2012-01-16 I ended up having to do this because the home status would flip back and forth between homed and not homed.  It turns out
    //                that this is because GetCurrentHomeStatus would query the axis and overwrite the values set before homing starts.
    ResetAllControllers();

    // we just need to mark some axes ahead of time as "homing", or the homing check
    // will misreport the status.
    lock( AxisStatusLock) {
        foreach( IAxis axis in Channels.Select( x => x.XAxis)) {
            SetServoEvent( axis, false);
            SetServoStatus( axis, ServoStatus.HomeStatusT.Homing);
        }
    }
    HomeDelegate d = new HomeDelegate( HomeThread);
    d.BeginInvoke( error_interface, called_from_diags, HomeComplete, null);
}
// ----------------------------------------------------------------------
private void HomeComplete( IAsyncResult iar)
{
    AsyncResult ar = (AsyncResult)iar;
    HomeDelegate caller = (HomeDelegate)ar.AsyncDelegate;
    try {
        caller.EndInvoke( iar);
    } catch( Exception ex) {
        _log.Warn( ex.Message);
    }
}
// ----------------------------------------------------------------------
private void HomeThread( IError error_interface, bool called_from_diags)
{
    if( Thread.CurrentThread.Name == null)
        Thread.CurrentThread.Name = "Bumblebee homing";
    HomeStateMachine sm = new HomeStateMachine( this, error_interface, called_from_diags);
    sm.Start();
}
// ----------------------------------------------------------------------
public void HomeX()
{
    var available_channels = from c in Channels where c.Available select c;
    // Pioneer has 8 channels, doesn't do simultaneous X homing well yet, so do one at a time
    if( available_channels.Count() <= 4)
        HomeAxesHelper( (from c in available_channels select c.XAxis), false);
    else
    {
        const bool home_one_at_a_time = false;
        if( home_one_at_a_time) {
            // this line homes all axes one at a time
            HomeAxesHelper( (from c in available_channels select c.XAxis), true);
        } else {
            // home 4 at a time
            IEnumerable<Channel> first_group = available_channels.Take( 4);
            HomeAxesHelper( first_group.Select( x => x.XAxis), false);
            // wait for this group to finish homing
            WaitForAxesToHome( from x in first_group select x.XAxis.GetID());
            IEnumerable<Channel> remaining = available_channels.Except( first_group);
            HomeAxesHelper( remaining.Select( x => x.XAxis), false);
            WaitForAxesToHome( from x in remaining select x.XAxis.GetID());
        }
    }
}
// ----------------------------------------------------------------------
public void HomeZAB()
{
    var available_channels = from c in Channels where c.Available select c;
    var tip_shuttles = from s in Stages where s is TipShuttle select s as TipShuttle;
    var axes = ( from c in available_channels select c.ZAxis).Union( from ts in tip_shuttles select ts.AAxis).Union( from ts in tip_shuttles select ts.BAxis);
    HomeAxesHelper( axes, false);
    // wait for all of the axes to finish homing
    var ids = from a in axes select a.GetID();
    WaitForAxesToHome( ids);
}
// ----------------------------------------------------------------------
public void HomeW()
{
    var available_channels = from c in Channels where c.Available select c;
    foreach( Channel c in available_channels)
        c.EEPROMreadSN();
    HomeAxesHelper( (from c in available_channels select c.WAxis.UseSparingly()), false);
}
// ----------------------------------------------------------------------
public void HomeStages()
{
    HomeAxesHelper(( from s in Stages select s.YAxis).Union( from s in Stages select s.RAxis), false);
}
// ----------------------------------------------------------------------
public void HomeAxesHelper( IEnumerable< IAxis> axes, bool wait_for_complete)
{
    // clear homing status cache and home, non-blocking
    lock( AxisStatusLock) {
        foreach( IAxis axis in axes) {
            SetServoEvent( axis, false);
            SetServoStatus( axis, ServoStatus.HomeStatusT.Homing);
        }
    }
    lock( AxisStatusLock) {
        foreach( IAxis axis in axes) {
            axis.HomeComplete += new MotorEventHandler(Hardware_HomeComplete);
            axis.HomeError += new MotorEventHandler(Hardware_HomeError);
            axis.Home( wait_for_complete);
        }
    }
}
// ----------------------------------------------------------------------
void Hardware_HomeComplete(object sender, MotorEventArgs e)
{
    IAxis axis = sender as IAxis;
    Debug.Assert( axis != null);
    lock( AxisStatusLock) {
        //_log.DebugFormat( "'Bumblebee' axis '{0}' homed", axis.Name);
        SetServoStatus( axis, ServoStatus.HomeStatusT.Homed);
        SetServoEvent( axis, true);
    }
}
// ----------------------------------------------------------------------
void Hardware_HomeError(object sender, MotorEventArgs e)
{
    IAxis axis = sender as IAxis;
    Debug.Assert( axis != null);
    lock( AxisStatusLock) {
        SetServoStatus( axis, ServoStatus.HomeStatusT.Error);
        SetServoEvent( axis, true);
    }
}
// ----------------------------------------------------------------------
internal void WaitForStagesToHome()
{
    // here, we check the status of all of the stage axes and wait until they are done homing
    // then we compile a list of axes that had errors, and throw an exception for the state 
    // machine to handle
    WaitForAxesToHome(( from s in Stages select s.YAxis.GetID()).Union( from s in Stages select s.RAxis.GetID()));
}
// ----------------------------------------------------------------------
internal void WaitForChannelsToHome()
{
    var available_channels = from c in Channels where c.Available select c;
    var ids = from c in available_channels select c.XAxis.GetID();
    ids = ids.Union( from c in available_channels select c.WAxis.GetID());
    WaitForAxesToHome( ids);
}
// ----------------------------------------------------------------------
internal void WaitForAxesToHome( IEnumerable<byte> ids)
{
    var handles = (from status in AxisStatus 
                    where ids.Contains( status.Key) && status.Value.HomeStatus != ServoStatus.HomeStatusT.Homed
                    select status.Value.EventState).ToArray();
    foreach( var handle in handles)
        handle.WaitOne();
    // check for errors
    var ids_with_errors = from s in AxisStatus 
                            where (ids.Contains( s.Key))
                            where (s.Value.HomeStatus == ServoStatus.HomeStatusT.Error || s.Value.HomeStatus == ServoStatus.HomeStatusT.Uninitialized)
                            select s.Key;
    var axes_with_errors = from i in ids_with_errors select _ts.GetAxes()[i];
    if( axes_with_errors.Count() > 0) {
        string error = String.Format( "The following axis, or axes failed to home: {0}", String.Join( ", ", (from i in ids_with_errors select _ts.GetAxes()[i].Name).ToArray()) );
        throw new AxisException( axes_with_errors.ToList(), error);
    }
}
// ----------------------------------------------------------------------
private void SetServoEvent( IAxis axis, bool signaled)
{
    byte id = axis.GetID();
    Debug.Assert( AxisStatus.ContainsKey( id));
    if( signaled)
        AxisStatus[id].EventState.Set();
    else 
        AxisStatus[id].EventState.Reset();
}
// ----------------------------------------------------------------------
#endregion
// ----------------------------------------------------------------------
 * DEAD CODE FROM BBHARDWARE -- HomeStateMachine.cs
 * 
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BioNex.BumblebeePlugin.Hardware;
using BioNex.Shared.IError;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.Utils;
using log4net;

namespace BioNex.BumblebeeGUI
{
    public class HomeStateMachine : StateMachineWrapper< HomeStateMachine.State, HomeStateMachine.Trigger>
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(HomeStateMachine));
        private IError ErrorInterface { get; set; }
        private BBHardware Hardware { get; set; }
        private bool _home_zab_first = false;
        private string LastError { get; set; }
        private List<IAxis> StageErrors { get; set; }
        private List<IAxis> ChannelErrors { get; set; }

        public enum State
        {
            Idle,
            CheckWHomingStatus,
            PromptUserToRehome,
            PromptUserBeforeHomingW, 
            HomeAllW, 
            HomeAllWError,
            HomeAllZAB, 
            HomeAllZABError, 
            HomeAllStagesNoWait, 
            HomeAllStagesError, 
            HomeAllChannelsNoWait,
            WaitForAllStages, 
            WaitForAllStagesError, 
            WaitForAllChannels,
            WaitForAllChannelsError, 
            CheckWHomingStatusError,
            RehomeErrorStages,
            RehomeErrorChannels,
            Done
        }

        public enum Trigger
        {
            Execute, 
            Error, 
            Retry, 
            Ignore, 
            Done, 
            WHomed, 
            WNotHomed, 
            HomeW,
            HomeZ, 
            DoneGoBackToW,
            Abort,
        }

        public HomeStateMachine( BBHardware hw, IError error_interface, bool called_from_diags)
            : base( State.Idle, Trigger.Execute, Trigger.Retry, Trigger.Ignore, Trigger.Abort, called_from_diags)
        {
            Hardware = hw;
            ErrorInterface = error_interface;

            SM.Configure(State.Idle)
                .Permit(Trigger.Abort, State.Done)
                .Permit(Trigger.Execute, State.CheckWHomingStatus)
                .OnEntry( Idle);

            SM.Configure(State.CheckWHomingStatus)
                .Permit(Trigger.Abort, State.Done)
                .Permit(Trigger.WHomed, State.PromptUserToRehome)
                .Permit(Trigger.WNotHomed, State.PromptUserBeforeHomingW)
                .Permit(Trigger.Error, State.CheckWHomingStatusError)
                .OnEntry( CheckWHomingStatus);

            SM.Configure(State.CheckWHomingStatusError)
                .Permit(Trigger.Retry, State.CheckWHomingStatus)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry( f => HandleErrorWithRetryOnly( LastError));

            SM.Configure(State.PromptUserToRehome)
                .Permit(Trigger.HomeW, State.HomeAllW)
                .Permit(Trigger.HomeZ, State.HomeAllZAB)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry( f => PromptUser( true));

            SM.Configure(State.HomeAllW)
                .Permit(Trigger.Abort, State.Done)
                .Permit(Trigger.Done, State.HomeAllZAB)
                .Permit(Trigger.Error, State.HomeAllWError)
                .OnEntry( HomeAllW);
            SM.Configure(State.HomeAllWError)
                .Permit(Trigger.Retry, State.HomeAllW)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry( f => HandleErrorWithRetryOnly( LastError));

            SM.Configure(State.PromptUserBeforeHomingW)
                .Permit(Trigger.HomeW, State.HomeAllW)
                .Permit(Trigger.HomeZ, State.HomeAllZAB)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry( f => PromptUser( false));

            SM.Configure(State.HomeAllZAB)
                .Permit(Trigger.Abort, State.Done)
                .Permit(Trigger.Done, State.HomeAllStagesNoWait)
                .Permit(Trigger.DoneGoBackToW, State.PromptUserBeforeHomingW)
                .Permit(Trigger.Error, State.HomeAllZABError)
                .OnEntry( HomeAllZAB);
            SM.Configure(State.HomeAllZABError)
                .Permit(Trigger.Retry, State.HomeAllZAB)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry( f => HandleErrorWithRetryOnly( LastError));

            SM.Configure(State.HomeAllStagesNoWait)
                .Permit(Trigger.Abort, State.Done)
                .Permit(Trigger.Done, State.HomeAllChannelsNoWait)
                .Permit(Trigger.Error, State.HomeAllStagesError)
                .OnEntry( HomeAllStagesNoWait);
            SM.Configure(State.HomeAllStagesError)
                .Permit(Trigger.Retry, State.HomeAllStagesNoWait)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry( f => HandleErrorWithRetryOnly( LastError));

            SM.Configure(State.HomeAllChannelsNoWait)
                .Permit(Trigger.Abort, State.Done)
                .Permit(Trigger.Done, State.WaitForAllStages)
                .OnEntry( HomeAllChannelsNoWait);

            SM.Configure(State.WaitForAllStages)
                .Permit(Trigger.Abort, State.Done)
                .Permit(Trigger.Done, State.WaitForAllChannels)
                .Permit(Trigger.Error, State.WaitForAllStagesError)
                .OnEntry( WaitForAllStages);
            SM.Configure(State.WaitForAllStagesError)
                .Permit(Trigger.Retry, State.RehomeErrorStages)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry( f => HandleErrorWithRetryOnly( LastError));
            SM.Configure(State.RehomeErrorStages)
                .Permit(Trigger.Abort, State.Done)
                .Permit(Trigger.Error, State.WaitForAllStagesError)
                .Permit(Trigger.Done, State.WaitForAllChannels)
                .OnEntry( RehomeErrorStages);

            SM.Configure(State.WaitForAllChannels)
                .Permit(Trigger.Abort, State.Done)
                .Permit(Trigger.Done, State.Done)
                .Permit(Trigger.Error, State.WaitForAllChannelsError)
                .OnEntry( WaitForAllChannels);
            SM.Configure(State.WaitForAllChannelsError)
                .Permit(Trigger.Retry, State.RehomeErrorChannels)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry( f => HandleErrorWithRetryOnly( LastError));
            SM.Configure(State.RehomeErrorChannels)
                .Permit(Trigger.Abort, State.Done)
                .Permit(Trigger.Error, State.WaitForAllChannelsError)
                .Permit(Trigger.Done, State.Done)
                .OnEntry( RehomeErrorChannels);

            SM.Configure(State.Done)
                .OnEntry( EndStateFunction);
        }

        private void Idle()
        {
            _log.Debug( "Waiting for homing command");
        }

        private void CheckWHomingStatus()
        {
            _log.Debug( "CheckWHomingStatus()");
            try {
                var available_channels = from c in Hardware.Channels where c.Available select c;
                var first_unhomed_channels = available_channels.FirstOrDefault( ac => !ac.WAxis.IsHomed);
                Fire( first_unhomed_channels != null ? Trigger.WNotHomed : Trigger.WHomed);
            } catch( AxisException ex) {
                LastError = ex.Message;
                Fire( Trigger.Error);
            }
        }

        private void PromptUser( bool w_already_homed)
        {
            _log.DebugFormat( "PromptUser(w_already_homed = {0})", w_already_homed);
            try {
                string message = w_already_homed 
                               ? "All W axes are already homed, but some need to be returned to their home positions.  If there is currently liquid in the tips, please place a reservoir under the tips first."
                               : "The W axes need to be homed.  If there is currently liquid in the tips, please place a reservoir under the tips first.";
                string home_w = "Home W axes now";
                string home_z = w_already_homed ? "Skip the W homing step" : "Tips are in a plate, so home Z axes first";
                List< string> error_strings = new List< string>{ home_w, home_z};
                if( _show_abort_label){
                    error_strings.Add( ABORT_LABEL);
                }
                ErrorData error = new ErrorData(message, error_strings);
                SMStopwatch.Stop();
                ErrorInterface.AddError(error);
                List< ManualResetEvent> events = new List< ManualResetEvent>{ _main_gui_abort_event};
                events.AddRange( error.EventArray);
                int event_index = WaitHandle.WaitAny( events.ToArray());
                SMStopwatch.Start();
                if( error.TriggeredEvent == home_w){
                    Fire( Trigger.HomeW);
                } else if( error.TriggeredEvent == home_z){
                    if( !w_already_homed){
                        _home_zab_first = true;
                    }
                    Fire( Trigger.HomeZ);
                } else if(( error.TriggeredEvent == ABORT_LABEL) || ( event_index == 0)){
                    Fire( Trigger.Abort);
                } else{
                    Debug.Assert( false, UNEXPECTED_EVENT_STRING);
                    Fire( Trigger.Abort);
                }
            } catch( Exception ex) {
                LastError = ex.Message;
                _log.Error( ex.Message);
            }
        }

        private void HomeAllW()
        {
            _log.Debug( "HomeAllW()");
            try {
                Hardware.HomeW();
                Fire( Trigger.Done);
            } catch( AxisException ex) {
                LastError = ex.Message;
                Fire( Trigger.Error);
            }
        }

        private void HomeAllZAB()
        {
            _log.Debug( "HomeAllZAB()");
            try {
                Hardware.HomeZAB(); // internally, this waits for ALL Z, A, and B axes to finish

                if( _home_zab_first) {
                    _home_zab_first = false; // don't want to keep re-entering this state!
                    Fire( Trigger.DoneGoBackToW);
                } else
                    Fire(Trigger.Done);
            } catch( AxisException ex) {
                LastError = ex.Message;
                Fire(Trigger.Error);
            }
        }

        private void HomeAllStagesNoWait()
        {
            _log.Debug( "HomeAllStagesNoWait()");
            try {
                Hardware.HomeStages();
                Fire(Trigger.Done);
            } catch( AxisException ex) {
                LastError = ex.Message;
                Fire(Trigger.Error);
            }
        }

        private void HomeAllChannelsNoWait()
        {
            _log.Debug( "HomeAllChannelsNoWait()");
            try {
                Hardware.HomeX();
            } catch( AxisException) {
                // it turns out that the best way to handle errors from this state is to let the X axes rehome in the last state, where we
                // check all of them to see if they have homed.  This is easier than my first attempt at fixing the bug when it comes up
                // on an 8 channel device, where this error occurring in the first four grouped channels causes the next group of four
                // channels to not home at all.
            } finally {
                Fire(Trigger.Done);
            }
        }

        private void WaitForAllStages()
        {
            _log.Debug( "WaitForAllStages()");
            try {
                Hardware.WaitForStagesToHome();
                Fire(Trigger.Done);
            } catch( AxisException ex) {
                LastError = ex.Message;
                StageErrors = ex.Axes;
                Fire(Trigger.Error);
            }
        }

        private void WaitForAllChannels()
        {
            _log.Debug( "WaitForAllChannels()");
            try {
                Hardware.WaitForChannelsToHome();
                Fire(Trigger.Done);
            } catch( AxisException ex) {
                LastError = ex.Message;
                ChannelErrors = ex.Axes;
                Fire(Trigger.Error);
            }
        }

        private void RehomeErrorStages()
        {
            try {
                Hardware.HomeAxesHelper( StageErrors, false);
                Hardware.WaitForStagesToHome();
                Fire(Trigger.Done);
            } catch( AxisException ex) {
                LastError = ex.Message;
                Fire(Trigger.Error);
            }
        }

        private void RehomeErrorChannels()
        {
            try {
                Hardware.HomeAxesHelper( ChannelErrors, false);
                Hardware.WaitForChannelsToHome();
                Fire(Trigger.Done);
            } catch( AxisException ex) {
                LastError = ex.Message;
                Fire(Trigger.Error);
            }
        }

        /// <summary>
        /// Registers only the default "retry" behavior with the ErrorInterface.  It is assumed
        /// that this is getting called because of a servo drive fault, so it will reset
        /// faults and enable the axis before continuing.
        /// </summary>
        /// <param name="message"></param>
        private void HandleErrorWithRetryOnly(string message)
        {
            try {
                const string retry = "Try move again";
                List< string> error_strings = new List< string>{ retry};
                if( _show_abort_label){
                    error_strings.Add( ABORT_LABEL);
                }
                ErrorData error = new ErrorData(message, error_strings);
                SMStopwatch.Stop();
                ErrorInterface.AddError(error);
                List< ManualResetEvent> events = new List< ManualResetEvent>{ _main_gui_abort_event};
                events.AddRange( error.EventArray);
                int event_index = WaitHandle.WaitAny( events.ToArray());
                SMStopwatch.Start();
                if( error.TriggeredEvent == retry){
                    Fire( Trigger.Retry);
                } else if(( error.TriggeredEvent == ABORT_LABEL) || ( event_index == 0)){
                    Fire( Trigger.Abort);
                } else{
                    Debug.Assert( false, UNEXPECTED_EVENT_STRING);
                    Fire( Trigger.Abort);
                }
            } catch( Exception ex) {
                LastError = ex.Message;
                _log.Error( ex.Message);
            }
        }
    }
}
// ----------------------------------------------------------------------
 * dead code from BumblebeePlugin/Model/MainModel.cs -- mostly pioneer functionality.
 * PIONEER RUNS EACH PROTOCOL THROUGH EXECUTE HIT PICK
 * HAS VERIFICATION STEPS (BARCODES ALL THERE, Z HEIGHTS CORRECT, TRANSFERS CORRECT)
// ----------------------------------------------------------------------
internal static class ExecuteHitpickFileParameterNames
{
internal const string BumblebeeDevice = "BumblebeeDevice";
internal const string PlateHandler = "PlateHandler";
internal const string TipHandlingMethodName = "TipHandlingMethodName";
internal const string HitpickFilepath = "HitpickFilepath";
internal const string SchedulerName = "SchedulerName";
internal const string Callback = "Callback";
}
// ----------------------------------------------------------------------
public bool ExecuteHitpickFileCommand( IDictionary<string,object> parameters)
{
    var available_channels = from c in _hw.Channels where c.IsAvailable() select c;

    // before we do anything at all, make sure that the W axes are very, very close to 0.
    try{
        foreach( Channel c in available_channels) {
            if( Math.Abs( c.GetW().GetPositionMM()) > 0.01) {
                MessageBoxResult answer = MessageBox.Show( "One or more channels might have liquids in its tip.  Would you like to zero all channels first?  If no tips are currently pressed on, press Yes.", "Confirm W axis zeroing", MessageBoxButton.YesNo);
                if( answer == MessageBoxResult.Yes) {
                    _hw.HomeW();
                    _hw.WaitForAxesToHome( from x in available_channels select x.GetW().GetID());
                }
                else if( answer == MessageBoxResult.No) {
                    MessageBox.Show( "Protocol aborted.  Please open Bumblebee diagnostics to resolve the issues.");
                    return false;
                }
            }
        }
    } catch( Exception ex){
        MessageBox.Show( ex.Message + ". Please check axes in diagnostics and restart the protocol.");
        return false;
    }

    // verify the command parameters -- we need the following pieces of info
    // 1. Hardware - internally named Hardware
    // 2. Teachpoints - internally named DeviceTeachpoints
    // 3. RobotStorageInterface
    // 4. tip handling method name
    // 5. hitpick filename
    // 6. scheduler name
    if( !parameters.ContainsKey( BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.BumblebeeDevice))
        throw new MissingCommandParameterException( BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.BumblebeeDevice);
    if( !parameters.ContainsKey( BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.PlateHandler))
        throw new MissingCommandParameterException( BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.PlateHandler);
    if( !parameters.ContainsKey( BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.TipHandlingMethodName))
        throw new MissingCommandParameterException( BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.TipHandlingMethodName);
    if( !parameters.ContainsKey( BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.HitpickFilepath))
        throw new MissingCommandParameterException( BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.HitpickFilepath);
    if( !parameters.ContainsKey( BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.Callback))
        throw new MissingCommandParameterException( BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.Callback);
    // figure out how to expose the scheduler names to the app -- perhaps we either need a way for
    // it to talk to basic devices and get information from them, or we need to make a new
    // interface specifically for the Bumblebee
    //if( !parameters.ContainsKey( BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.SchedulerName))
    //    throw new MissingCommandParameterException( BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.SchedulerName);

    // also ensure that the values aren't null
    if( (bumblebee_device_ = parameters[BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.BumblebeeDevice] as AccessibleDeviceInterface) == null)
        throw new InvalidCommandParameterException( BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.BumblebeeDevice);
    if( (_plate_handler = parameters[BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.PlateHandler] as ExternalPlateTransferSchedulerInterface) == null)
        throw new InvalidCommandParameterException( BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.PlateHandler);
    if( (_tip_handling_method = parameters[BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.TipHandlingMethodName].ToString()) == null)
        throw new InvalidCommandParameterException( BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.TipHandlingMethodName);
    if( (_hitpick_filename = parameters[BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.HitpickFilepath].ToString()) == null)
        throw new InvalidCommandParameterException( BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.HitpickFilepath);
    //if( (_selected_scheduler_name = parameters[BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.SchedulerName].ToString()) == null)
    //    throw new InvalidCommandParameterException( BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.SchedulerName);

    // ok, we're good, go ahead and execute the command
    // 5. load the hitpick file
    TransferOverview to;
    // #198
    try{
        to = LoadHitpickFile( _hitpick_filename);
    } catch( LabwareNotFoundException ex){
        MessageBox.Show( ex.Message + "  Please check the Labware Editor, and then try running again.");
        return false;
    } catch( InvalidWellNameException ex){
        MessageBox.Show( String.Format( "A well named '{0}' is invalid for the selected labware types.  Please correct the error in the hitpick file and then restart the protocol.", ex.WellName));
        return false;
    }

    foreach( string x in to.GetLiquidProfilesUsed()) {
        try{
            ILiquidProfile profile = LiquidProfileLibrary.LoadLiquidProfileByName( x);
            Log.Info( profile.DumpInfo());
        } catch( Exception){
            string message = String.Format( "The liquid profile '{0}' referenced by the hitpick file does not exist in the liquid profile database.  Please check the Liquid Profile editor, and then try running again. ", x);
            Log.Error( message);
            MessageBox.Show( message);
            return false;
        }
    }

    // DKM 2010-11-18 no longer need Reset here because the plugin now registers ResetCommand, which
    // is sent when Synapsis starts a protocol
    //_hw.Reset();

    // validate the file to make sure that none of the volumes requested are out of
    // range.  This should also take the liquid class paramters into account
    string error_message;
    List<Teachpoint> stage_teachpoints = _teachpoints.GetAllStageTeachpoints();
    if( !Validate( available_channels.ToList(), stage_teachpoints, LabwareDatabase, LiquidProfileLibrary, to, out error_message)) {
        error_message = "Hitpick file has some inconsistencies: " + error_message;
        MessageBox.Show( error_message);
        Log.Fatal( error_message);
        return false;
    }

    // make sure the system has enough tipboxes and all of the plates used by the hitpick file
    if( !CheckTipboxes( to.Transfers))
        return false;

    if( !VerifyPlateBarcodes( to.Transfers))
        return false;

    // set the up scheduler
    _scheduler_selector.LoadSchedulers();
    foreach( KeyValuePair<string,IScheduler> kvp in _scheduler_selector.SchedulerPluginsAvailable) {
        string name = kvp.Key;
        // for now, we are forcing the system to use the disposable tip scheduler
        if( name.Contains( "disposable")) {
            Scheduler = kvp.Value;
            break;
        }
    }

    // set the progressbar max size via default Messenger
    Messenger.Default.Send<TotalTransfersMessage>( new TotalTransfersMessage( to.Transfers.Count()));
                        
    Debug.Assert( Scheduler != null, "Disposable tip scheduler was not loaded");
    // set up the scheduler
    Scheduler.SetHardware( _hw);
    Scheduler.SetTeachpoints( _teachpoints);
    Scheduler.SetPlateHandler( _plate_handler);
    Scheduler.SetTipHandlingMethod( _tip_handling_method);
    Scheduler.SetConfiguration(( bumblebee_device_ as Bumblebee).Config);
    Scheduler.SetMessenger( MainModel.BBMessenger);
    Scheduler.SetDispatcher( ProtocolDispatcher);
    Scheduler.StartScheduler();

    // move channels to ready position.
    try{
        _hw.MoveChannelsToReady();
    } catch( Exception){
        MessageBox.Show( "One or more channels failed to move to the ready position.  Please try running the protocol again.");
        return false;
    }

    // run it!
    _start = DateTime.Now;
    TransferProcess process = new TransferProcess( Scheduler.StartProcess);
    AsyncCallback callback = parameters[BioNex.BumblebeePlugin.Bumblebee.ExecuteHitpickFileParameters.Callback] as AsyncCallback;
    Debug.Assert( callback != null);
    process.BeginInvoke( to, callback, DateTime.Now);
    Log.InfoFormat( "Starting protocol '{0}'", _hitpick_filename);

    // get the customer-specific output plugin ready for logging
    string logs_path = BioNex.Shared.Utils.FileSystem.GetAppPath() + "\\logs";
    string filename_only = _hitpick_filename.Substring( _hitpick_filename.LastIndexOf( '\\') + 1);
    string output_plugin_filename = String.Format( "{0}\\{1}_{2}.transfers.csv", logs_path, filename_only, DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss"));
    if( OutputPlugin == null)
        return true;
    OutputPlugin.Open( output_plugin_filename);
    return true;
}
// ----------------------------------------------------------------------
public delegate void TransferProcess( TransferOverview to);
// ----------------------------------------------------------------------
/* bumblebee plugin should not be responsible for parsing work
    bumblebee plugin should only accept a single format for incoming work
private bool CheckTipboxes( List<Transfer> transfers)
{
    // query for the plate storage devices
    var plate_storage_devices = DataRequestInterface.Value.GetPlateStorageInterfaces();
    // ask each plate storage device how many "tipbox"es it has
    int num_tipboxes_available = 0;
    string tipbox_labware_name = "tipbox";
    TipBox labware = LabwareDatabase.GetLabware( tipbox_labware_name) as TipBox;
    if( labware == null) {
        string error = String.Format( "The specified tipbox labware named '{0}' is invalid", tipbox_labware_name);
        MessageBox.Show( error);
        Log.Fatal( error);
        return false;
    }
    foreach( PlateStorageInterface p in plate_storage_devices)
        num_tipboxes_available += p.GetLocationsForLabware( tipbox_labware_name).Count();

    int num_tipboxes_required = (int)Math.Ceiling( (double)transfers.Count() / labware[LabwarePropertyNames.NumberOfWells].ToInt()) + _config.ExtraTipboxesRequired;
    if( num_tipboxes_available < num_tipboxes_required) {
        string error = String.Format( "There are not enough tipboxes available to process the hitpick file.  You have {0} tipboxes, but need {1}.",
                                        num_tipboxes_available, num_tipboxes_required);
        MessageBox.Show( error);
        Log.Fatal( error);
        return false;
    }

    return true;
}
//-----------------------------------------------------------------------------------------
private bool VerifyPlateBarcodes( List<Transfer> transfers)
{
    // compile list of all of the barcodes needed by this hitpick list
    // DKM 2011-10-05 after changing Barcode from string to MutableString in the Plate class,
    //                I needed to use .Value so I could use the Distinct method
    IEnumerable<string> temp = from t in transfers select t.SrcPlate.Barcode.Value;
    temp = temp.Concat( from t in transfers select t.DstPlate.Barcode.Value);
    List<string> all_barcodes_needed = temp.Distinct().ToList();
    // query for the plate storage devices
    var plate_storage_devices = DataRequestInterface.Value.GetPlateStorageInterfaces();
    List<string> barcodes_found = new List<string>();
    foreach( var p in plate_storage_devices) {
        foreach( var b in all_barcodes_needed) {
            string location_name;
            if( p.HasPlateWithBarcode( b, out location_name))
                barcodes_found.Add( b);
        }
    }

    bool found_all_barcodes = all_barcodes_needed.Intersect( barcodes_found).OrderBy( x => x, StringComparer.Ordinal).SequenceEqual( all_barcodes_needed.OrderBy( x => x, StringComparer.Ordinal));

    if( !found_all_barcodes) {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine( "The system could not find the following required barcoded plates:");
        foreach( string barcode in all_barcodes_needed.Except(barcodes_found))
            sb.AppendLine( barcode.Replace( " ", "•"));
        MessageBox.Show( sb.ToString());
        Log.Fatal( sb.ToString());
        return false;
    }

    return CheckForExtraneousBarcodes(all_barcodes_needed, plate_storage_devices);
}
//-----------------------------------------------------------------------------------------
//[Conditional("CheckForExtraneousBarcodes")]
private bool CheckForExtraneousBarcodes(List<string> all_barcodes_needed, IEnumerable<PlateStorageInterface> plate_storage_devices)
{
    // #248 we apparently also want to check for plates that are there that AREN'T supposed to be there.
    // I added this check here to keep it separate, just in case we don't like this behavior (and in
    // testing, my guess is that we don't...
    List<KeyValuePair<string,string>> barcodes_not_needed = new List<KeyValuePair<string,string>>();
    foreach (var p in plate_storage_devices)
    {
        var barcodes_not_needed_this_device_only = from x in p.GetInventory((p as DeviceInterface).Name)
                                                    where !all_barcodes_needed.Contains( x.Key)
                                                    select x;
        barcodes_not_needed.AddRange( barcodes_not_needed_this_device_only);
    }

    if( (( bumblebee_device_ as Bumblebee).Config.WarnForExtraPlates) && ( barcodes_not_needed.Count() > 0)){
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Plates not used by this hitpick file are present in the plate storage system.  Please remove all plates with the following barcodes:");
        foreach( KeyValuePair<string,string> barcode in barcodes_not_needed)
            sb.AppendLine( String.Format( "{0} ({1})", barcode.Key, barcode.Value));
        MessageBox.Show(sb.ToString());
        Log.Fatal(sb.ToString());
        return false;
    }

    return true;
}
//-----------------------------------------------------------------------------------------
private TransferOverview LoadHitpickFile( string filepath)
{
    Reader reader = new Reader( LabwareDatabase);
    return reader.Read( filepath, FileSystem.GetAppPath() + "\\test_hitpick.xsd");
}
// ----------------------------------------------------------------------
public void MoveChannelsToReady()
{
    _hw.MoveChannelsToReady();
}
// ----------------------------------------------------------------------
public short GetAnalogReading(byte channel_id)
{
    var channel = _hw.GetChannel(channel_id);
    return channel.ZAxis.GetAnalogReading( 5);
}
// ----------------------------------------------------------------------
// from BumblebeePlugin -- Bumblebee.cs
// ----------------------------------------------------------------------
public class Well3
{
    public int row_index_;
    public int col_index_;
    public override string ToString()
    {
        return String.Format( "({0},{1})", row_index_, col_index_);
    }
}

public static HashSet< Well3> FindCompatibilitySet( int x_offset_i, int y_offset_i, int num_wells)
{
    HashSet< Well3> retval = new HashSet< Well3>(){ new Well3(){ row_index_ = 0, col_index_ = 0}};
    double x_offset = ( double)x_offset_i;
    double y_offset = ( double)y_offset_i;
    int num_rows = ( int)( Math.Sqrt( num_wells / 1.5));
    int num_cols = ( int)( num_rows * 1.5);

    double edge = Math.Sqrt( ( x_offset * x_offset) + ( y_offset * y_offset));
    double angle = Math.Atan( x_offset / y_offset);
    double plate_edge = Math.Sqrt( ( num_rows * num_rows) + ( num_cols * num_cols));
    double cos = Math.Cos( angle);
    double sin = Math.Sin( angle);

    for( double x_grid = 0; x_grid < plate_edge; x_grid += edge){
        for( double y_grid = edge; y_grid < plate_edge; y_grid += edge){
            int x_rot = ( int)( Math.Round( x_grid * cos + y_grid * sin));
            int y_rot = ( int)( Math.Round( y_grid * cos - x_grid * sin));
            retval.Add( new Well3{ row_index_ = x_rot, col_index_ = y_rot});
            retval.Add( new Well3{ row_index_ = y_rot, col_index_ = -x_rot});
            retval.Add( new Well3{ row_index_ = -y_rot, col_index_ = x_rot});
            retval.Add( new Well3{ row_index_ = -x_rot, col_index_ = -y_rot});
        }
    }

    return retval;
}

public static void TestCompatibilitySet()
{
    HashSet< Well3> wells = FindCompatibilitySet( 2, 1, 384);
    string[] well_names = ( from w in wells select w.ToString()).ToArray();
    String s = String.Join( ",", well_names);
    Console.WriteLine( s);
}
// ----------------------------------------------------------------------
// ----------------------------------------------------------------------
// PIONEER VALIDATION CODE
// ----------------------------------------------------------------------
// ----------------------------------------------------------------------
private delegate double ConvertFunction( double val);
// ----------------------------------------------------------------------
internal static bool ValidateZMoves( IList< Channel> channels, IList< Teachpoint> stage_teachpoints, ILabwareDatabase labware_database, ILiquidProfileLibrary liquid_library, TransferOverview to, out string result)
{
    DateTime start = DateTime.Now;

    var limits = from c in channels select new{ Name = c.ZAxis.Name,
                                                MinLimit = c.ZAxis.Settings.MinLimit,
                                                MaxLimit = c.ZAxis.Settings.MaxLimit };

    StringBuilder validation_errors = new StringBuilder();

    Dictionary<string,ILiquidProfile> liquid_profile_cache = new Dictionary<string,ILiquidProfile>();
    Dictionary<string,ILabware> labware_cache = new Dictionary<string,ILabware>();
    // these offsets need to be compared using all of the available reference points, i.e. every
    // stage teachpoint's Z value
    var all_absolute_z_positions = from z in stage_teachpoints select z["z"];

    foreach( Transfer transfer in to.Transfers) {
        ILiquidProfile liquid;
        if( liquid_profile_cache.ContainsKey( transfer.LiquidProfileName))
            liquid = liquid_profile_cache[ transfer.LiquidProfileName];
        else {
            liquid = liquid_library.LoadLiquidProfileByName( transfer.LiquidProfileName);
            liquid_profile_cache.Add( transfer.LiquidProfileName, liquid);
        }

        // gather all relevant parameters for source
        ILabware source_labware;
        if( labware_cache.ContainsKey( transfer.SrcPlate.LabwareName))
            source_labware = labware_cache[ transfer.SrcPlate.LabwareName];
        else {
            source_labware = labware_database.GetLabware( transfer.SrcPlate.LabwareName);
            labware_cache.Add( transfer.SrcPlate.LabwareName, source_labware);
        }

        double source_well_bottom_offset = source_labware[LabwarePropertyNames.Thickness].ToDouble() - source_labware[LabwarePropertyNames.WellDepth].ToDouble();
        // gather all relevant parameters for dest
        // ILabware dest_labware = labware_database.GetLabware( transfer.DstPlate.LabwareName);
        double dest_well_bottom_offset = source_labware[LabwarePropertyNames.Thickness].ToDouble() - source_labware[LabwarePropertyNames.WellDepth].ToDouble();
                
        // validate Z moves in source and dest
        // get the Z travel values
        //! \todo Felix to replace with two separate values, ZMoveDuringAspirate and ZMoveDuringDispense
        double aspirate_total_offset = source_well_bottom_offset + transfer.AspirateDistanceFromWellBottomMm.Value + liquid.ZMoveDuringAspirating;
        double dispense_total_offset = dest_well_bottom_offset + transfer.DispenseDistanceFromWellBottomMm.Value + liquid.ZMoveDuringDispensing;

        // make sure the Z axis can make these moves
        // MAX
        var aspirate_Z_errors_max = from l in limits
                                    from z in all_absolute_z_positions
                                    where l.MaxLimit < (z + aspirate_total_offset)
                                    select new{
                                        DesiredPosition = z + aspirate_total_offset,
                                        Name = l.Name,
                                        MaxLimit = l.MaxLimit
                                    };
        foreach( var x in aspirate_Z_errors_max) {
            validation_errors.AppendLine( String.Format( @"source {0} well {1} to destination {2} well {3}:
                                                            bottom of travel in source is at {4}mm and this exceeds axis
                                                            {5}'s maximum travel limit of {6}mm",
                                                            transfer.SrcPlate.Barcode, transfer.SrcWell.WellName,
                                                            transfer.DstPlate.Barcode, transfer.DstWell.WellName,
                                                            x.DesiredPosition, x.Name, x.MaxLimit));
        }
        // MIN
        var aspirate_Z_errors_min = from l in limits
                                    from z in all_absolute_z_positions
                                    where l.MinLimit > (z + aspirate_total_offset)
                                    select new{
                                        DesiredPosition = z + aspirate_total_offset,
                                        Name = l.Name,
                                        MinLimit = l.MinLimit
                                    };
        foreach( var x in aspirate_Z_errors_min) {
            validation_errors.AppendLine( String.Format( @"source {0} well {1} to destination {2} well {3}:
                                                            bottom of travel in source is at {4}mm and this exceeds axis
                                                            {5}'s minimum travel limit of {6}mm",
                                                            transfer.SrcPlate.Barcode, transfer.SrcWell.WellName,
                                                            transfer.DstPlate.Barcode, transfer.DstWell.WellName,
                                                            x.DesiredPosition, x.Name, x.MinLimit));
        }
        // MAX
        var dispense_Z_errors_max = from l in limits
                                    from z in all_absolute_z_positions
                                    where l.MaxLimit < (z + dispense_total_offset)
                                    select new{
                                        DesiredPosition = z + dispense_total_offset,
                                        Name = l.Name,
                                        MaxLimit = l.MaxLimit
                                    };
        foreach( var x in dispense_Z_errors_max) {
            validation_errors.AppendLine( String.Format( @"source {0} well {1} to destination {2} well {3}:
                                                            bottom of travel in destination is at {4}mm and this exceeds axis
                                                            {5}'s maximum travel limit of {6}mm",
                                                            transfer.SrcPlate.Barcode, transfer.SrcWell.WellName,
                                                            transfer.DstPlate.Barcode, transfer.DstWell.WellName,
                                                            x.DesiredPosition, x.Name, x.MaxLimit));
        }
        // MIN
        var dispense_Z_errors_min = from l in limits
                                    from z in all_absolute_z_positions
                                    where l.MaxLimit < (z + dispense_total_offset)
                                    select new{
                                        DesiredPosition = z + dispense_total_offset,
                                        Name = l.Name,
                                        MaxLimit = l.MaxLimit
                                    };
        foreach( var x in dispense_Z_errors_min) {
            validation_errors.AppendLine( String.Format( @"source {0} well {1} to destination {2} well {3}:
                                                            bottom of travel in destination is at {4}mm and this exceeds axis
                                                            {5}'s maximum travel limit of {6}mm",
                                                            transfer.SrcPlate.Barcode, transfer.SrcWell.WellName,
                                                            transfer.DstPlate.Barcode, transfer.DstWell.WellName,
                                                            x.DesiredPosition, x.Name, x.MaxLimit));
        }
    }

    result = validation_errors.ToString();
    Debug.WriteLine( String.Format( "Time through ValidateZMoves loop: {0}ms", (DateTime.Now - start).TotalMilliseconds));
    return validation_errors.Length == 0;
}
// ----------------------------------------------------------------------
internal static bool ValidateTransfers( IList< Channel> channels, IList< Teachpoint> stage_teachpoints, ILabwareDatabase labware_database, ILiquidProfileLibrary liquid_library,TransferOverview to, out string result)
{
    var limits = from c in channels select new{ Name = c.WAxis.Name,
                                                Min = c.WAxis.Settings.MinLimit,
                                                Max = c.WAxis.Settings.MaxLimit,
                                                ConvertFunction = new ConvertFunction( c.WAxis.ConvertUlToMm)
                                                };

    StringBuilder validation_errors = new StringBuilder();

    DateTime start = DateTime.Now;

    // cache the liquids
    Dictionary<string,ILiquidProfile> liquid_profile_cache = new Dictionary<string,ILiquidProfile>();
    foreach( Transfer transfer in to.Transfers) {
        string liquid_name = transfer.LiquidProfileName;
        if( !liquid_profile_cache.ContainsKey( liquid_name))
            liquid_profile_cache.Add( liquid_name, liquid_library.LoadLiquidProfileByName( liquid_name));
    }

    // loop over the transfers, look at the volumes requested, but
    // also pull in the liquid profile information and factor it
    // into the boundary checks

    // to minimize the number of repetitive calculations, only get distinct values for transfer volumes
    var distinct_transfer_volumes = (from t in to.Transfers
                                        select new{ 
                                            TransferVolume = t.TransferVolume,
                                            TransferUnits = t.TransferUnits,
                                            LiquidProfileName = t.LiquidProfileName
                                        }).Distinct();
    var distinct_volumes = from t in distinct_transfer_volumes
                            select new{
                                PreAspirateVolumeUl = liquid_profile_cache[t.LiquidProfileName].PreAspirateVolume,
                                AspirateVolume = t.TransferVolume,
                                AspirateVolumeUnits = t.TransferUnits
                            };
    var error_volumes = from w in limits
                        from v in distinct_volumes
                        where w.Max < w.ConvertFunction( v.PreAspirateVolumeUl + (v.AspirateVolumeUnits == VolumeUnits.ul ? v.AspirateVolume : v.AspirateVolume * 1000))
                        select new{
                            VolumeInfo = v,
                            ChannelName = w.Name
                        };

    foreach( var e in error_volumes) {
        validation_errors.AppendLine( String.Format( @"aspirate volume of {0}uL exceeds maximum travel
                                                        for channel {1}", 
                                                        e.VolumeInfo.AspirateVolume, e.ChannelName));
    }

    result = validation_errors.ToString();
    Debug.WriteLine( String.Format( "Total time in ValidateTransfers: {0}ms", (DateTime.Now - start).TotalMilliseconds));
    return validation_errors.Length == 0;
}
// ----------------------------------------------------------------------
private bool Validate( IList< Channel> channels, IList< Teachpoint> stage_teachpoints, ILabwareDatabase labware_database, ILiquidProfileLibrary liquid_library, TransferOverview to, out string result)
{
    try{
        // pipetting checks
        string move_results;
        bool moves_ok = ValidateZMoves( channels, stage_teachpoints, labware_database, liquid_library, to, out move_results);
        string aspirate_results;
        bool aspirates_ok = ValidateTransfers( channels, stage_teachpoints, labware_database, liquid_library, to, out aspirate_results);
                
        result = move_results + aspirate_results;
        return moves_ok && aspirates_ok;
    } catch( Exception ex){
        result = ex.Message;
        return false;
    }
}
// ----------------------------------------------------------------------
// ----------------------------------------------------------------------
// old SchedulerSelector
// ----------------------------------------------------------------------
// ----------------------------------------------------------------------
namespace BioNex.BumblebeePlugin.Model
{
    [Export(typeof(SchedulerSelector))]
    public class SchedulerSelector
    {
        [ImportMany(typeof(IScheduler))]
        public IEnumerable<IScheduler> SchedulerPlugins { get; set; }

        public Dictionary<string,IScheduler> SchedulerPluginsAvailable { get; private set; }

        private static readonly ILog _log = LogManager.GetLogger( typeof(SchedulerSelector));

        public SchedulerSelector()
        {
            SchedulerPluginsAvailable = new Dictionary<string,IScheduler>();
            //LoadSchedulers();
        }

        public void LoadSchedulers()
        {
            SchedulerPluginsAvailable.Clear();
            foreach( IScheduler scheduler in SchedulerPlugins) {
                SchedulerPluginsAvailable.Add( scheduler.GetSchedulerName(), scheduler);
                _log.Info( "Loaded Bumblebee scheduler: " + scheduler.GetSchedulerName());
            }
        }
    }
}
//-----------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------
// dead code from old channel service.
// OLD CHANNEL SERVICE ACTUALLY RAN QUITE WELL BUT WAS POORLY FORMED -- FOCUS ATTENTION ON NEW CHANNEL SERVICE, BUT THIS MIGHT BE USEFUL FOR HINTS.
//-----------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------
private bool TryDispatchTransfer( Channel selected_channel, SourcePlate selected_source_plate, IEnumerable< TransferPair> transfer_pairs, IEnumerable< Transfer> transfers)
{
    // get the narrowest transfer pair (which should be the first in the list of unstarted transfer pairs for the selected source plate).
    TransferPair narrowest_transfer_pair = transfer_pairs.FirstOrDefault();
    // if we found a pair, then break up the pair by selecting one of its transfers for single dispatch, else select the first transfer from the list of unstarted transfers for the selected source plate.
    Transfer selected_transfer = ( narrowest_transfer_pair != null) ? narrowest_transfer_pair.Transfer1 : transfers.FirstOrDefault();
    // if we found a possibility, then perform a single dispatch.
    if( selected_transfer != null){
        DispatchLiquidHandlingJob( selected_channel, selected_transfer);
        return true;
    }
    return false;
}
//-----------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------
/// <summary>
/// This thread runs until the main scheduler stops it by signaling StopEvent.  It
/// will consume Transfer objects from the Stages and allocate Channels accordingly.
/// </summary>
private void ChannelServiceThreadRunner()
{
    // run until told to stop.
    while( !StopEvent.WaitOne( 0)){
        // yield to other threads.
        Thread.Sleep( 10);

        bool dequeue_succeeded;
        do{
            Channel channel_to_disable;
            dequeue_succeeded = ChannelsToDisable.TryDequeue( out channel_to_disable);
            if( dequeue_succeeded){
                SharedMemory.RemoveChannel( channel_to_disable);
            }
        } while( dequeue_succeeded);

        // declare local variables:
        // channel assignments = channels assigned to perform paired transfers.
        // source angle = source-stage angle at which to perform transfer.
        // dest angle = dest-stage angle at which to perform transfer.
        List<KeyValuePair<Channel,Transfer>> channel_assignments;
        double source_angle, dest_angle;

        // use our brilliant logic to determine which is the least busy stage, and schedule work for
        // that stage if it has transfers assigned to it still
        // var source_stage_queue_depths = from stage in SourceStages
        //                                 select new{ Stage = stage, QueueDepth = ProtocolDispatcher.GetQueueDepth( stage)};
        // var source_stages_ordered_by_preference = from ssqd in source_stage_queue_depths
        //                                           where ssqd.QueueDepth < 2
        //                                           orderby ssqd.QueueDepth
        //                                           select ssqd.Stage;
        // build this up later so that we have simpler logic for determining source stage to use.                

        Stage source_stage = SharedMemory.GetLeastBusySourceStage();
        // no stage found.
        if( source_stage == null){
            continue;
        }
        // stage found already has plenty of work to do.
        if( ProtocolDispatcher.GetQueueDepth( source_stage) > 1){
            continue;
        }

        bool all_channels_used = SharedMemory.CountChannelsByStatus( ServiceSharedMemory.ChannelStatus.CleanTip) == 0;
        Plate source_plate = SharedMemory.GetStagePlate( source_stage);
        DestinationPlate dest_plate = SharedMemory.GetStagePlate( DestinationStage) as DestinationPlate;
        if( all_channels_used || source_plate == null || dest_plate == null) {
            continue;
        }

        IList<Transfer> source_transfers = SharedMemory.GetUnstartedTransfersBySrcAndDstBarcode( source_plate.Barcode, dest_plate.Barcode);
        // it is possible that the other thread already claimed the last transfers, so
        // if there aren't any left, 
        if( source_transfers.Count == 0)
            continue;

        if( !GetAllTransferParameters( source_transfers, source_stage, DestinationStage, dest_plate, 
                                        out channel_assignments,
                                        out source_angle, out dest_angle)){
            continue;
        }
        // get the transfers we've scheduled, and the remaining transfers.  Where
        // they overlap are the working transfers that we want to mark as InProgress
        var transfers_scheduled = from kvp in channel_assignments select kvp.Value;
        StringBuilder sb = new StringBuilder();
        foreach( KeyValuePair<Channel,Transfer> kvp in channel_assignments){
            string liquid_profile_display = kvp.Value.LiquidProfileName;
            ILiquidProfile liquid_profile = LiquidProfileLibrary.LoadLiquidProfileByName( liquid_profile_display);
            if( liquid_profile != null){
                string pre_aspirate_liquid_profile_name = liquid_profile.PreAspirateMixLiquidProfile;
                if( !string.IsNullOrEmpty( pre_aspirate_liquid_profile_name)){
                    ILiquidProfile pre_aspirate_liquid_profile = LiquidProfileLibrary.LoadLiquidProfileByName( pre_aspirate_liquid_profile_name);
                    if(( pre_aspirate_liquid_profile != null) && ( pre_aspirate_liquid_profile.IsMixingProfile)){
                        liquid_profile_display += ( " pre-aspirate mix with " + pre_aspirate_liquid_profile_name);
                    }
                }
                string post_dispense_liquid_profile_name = liquid_profile.PostDispenseMixLiquidProfile;
                if( !string.IsNullOrEmpty( post_dispense_liquid_profile_name)){
                    ILiquidProfile post_dispense_liquid_profile = LiquidProfileLibrary.LoadLiquidProfileByName( post_dispense_liquid_profile_name);
                    if(( post_dispense_liquid_profile != null) && ( post_dispense_liquid_profile.IsMixingProfile)){
                        liquid_profile_display += ( " post-dispense mix with " + post_dispense_liquid_profile_name);
                    }
                }
            }
            sb.Append( String.Format( "\t{0}: {1} [{2}]", kvp.Key.ID, kvp.Value.ToString(), liquid_profile_display));
        }
        Log.InfoFormat( "Scheduled the following transfers: {0}", sb.ToString());
        foreach( Transfer transfer_scheduled in transfers_scheduled){
            SharedMemory.SetTransferStatus( transfer_scheduled, ServiceSharedMemory.TransferStatus.Aspirating);
        }
        // set the channel states to Used
        foreach( KeyValuePair<Channel,Transfer> kvp in channel_assignments)
            SharedMemory.SetChannelState( kvp.Key, ServiceSharedMemory.ChannelStatus.UsingTip);
        // launch a thread to handle these transfers
        IDictionary< Channel, Transfer> transfers_to_perform = channel_assignments.ToDictionary( x => x.Key, x => x.Value);
        LiquidTransferJob job = new LiquidTransferJob( source_stage, DestinationStage, transfers_to_perform);
        ProtocolDispatcher.DispatchLiquidTransferJob( job);
    }
}
//-----------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------
/// <summary>
/// figures out which tips should go where, and how many at once.  This is constrained
/// by the layout of the source and destination transfers, as well as the relative
/// spacing of the remaining tips, and the number of possible tips that can be pressed
/// on simultaneously.  For example, even if there are two possible transfers from source
/// to dest, but there's only one tip available in the tip box, only one transfer will
/// get queued.
/// </summary>
/// <param name="transfers">
///     The total number of transfers from this source plate.  For now, it
///     modifies the original list that gets passed to the thread, so we
///     can easily tell when we're done.
/// </param>
/// <param name="source_stage"></param>
/// <param name="dest_stage"></param>
/// <param name="dest_plate"></param>
/// <param name="channel_assignments">
///     Maps a tip to the transfer that has been assigned to it by this function
/// </param>
/// <param name="source_angle">
///     The angle that the source plate needs to rotate to
/// </param>
/// <param name="dest_angle">
///     The angle that the dest plate needs to rotate to
/// </param>
private bool GetAllTransferParameters( IList<Transfer> transfers, Stage source_stage, Stage dest_stage, DestinationPlate dest_plate, 
                                        out List<KeyValuePair<Channel,Transfer>> channel_assignments, out double source_angle, out double dest_angle)
{
    source_angle = 0;
    dest_angle = 0;
    channel_assignments = new List<KeyValuePair<Channel,Transfer>>();

    // no need to lock shared memory because the caller does it
    // If there is only one tip, then reserve it now, and skip the code that tries to do a dual-tip transfer
    // also check to see if we can even get two channels from shared memory.  If we can't, then also only
    // attempt to schedule a single tip transfer
    // List<Channel> available_channels = SharedMemory.GetTwoAvailableChannels();
    List< Channel> available_channels = SharedMemory.GetChannelsByStatus( ServiceSharedMemory.ChannelStatus.CleanTip);

    // now loop over the transfers, and (for now) blindly pick pairs.  check the source
    // well and dest well spacing

    // for each pair of transfers, compute the transfer separation at the source and the transfer separation at the dest.
    List< TransferPair> transfer_separation_data = new List< TransferPair>();
    for( int i = 0; i < transfers.Count - 1; ++i){
        for( int j = i + 1; j < transfers.Count; ++j){
            // no need to lock shared memory because the caller does it

            // pass these to the util function that calculates the necessary angles.  it will
            // return false if there is no solution.  in this case, try another pair of transfers
            // the next pair should be composed of the 2nd transfer in the first pair, with the
            // next transfer in the list
            Transfer t1 = transfers[ i];
            Transfer t2 = transfers[ j];
            // need to get the labware definitions here, because I don't have a way to get
            // the labware definitions in Hardware -- need to figure out Unity + MEF
            // ILabware source_labware = LabwareDatabase.GetLabware( t1.Source.LabwareName);
            // ILabware dest_labware = LabwareDatabase.GetLabware( t1.Destination.LabwareName);
            // pass the two transfer requests to the hardware layer, since it can deal with
            // locking tips, calculating the tip spacing, and determining the wells to use
            // NOTE: GetSourceAndDestSolutions should unlock the two tips if it can't get
            // a solution for BOTH SOURCE AND DEST!

            // NOTE: big changes -- now use the SharedMemory for "locking" channels.  Get two channels
            // here, then pass into GetSourceAndDestSolutions to see if they work

            // double max_source_separation = Wells.GetDistanceBetweenWells( source_labware[LabwarePropertyNames.NumberOfWells].ToInt(), t1.SourceWell.WellName, t2.SourceWell.WellName);
            double max_source_separation = WellMathUtil.CalculateWellSpacing( t1.SrcPlate.LabwareFormat, t1.SrcWell, t2.SrcWell);
            // double max_dest_separation = Wells.GetDistanceBetweenWells( dest_labware[LabwarePropertyNames.NumberOfWells].ToInt(), t1.DestinationWell.WellName, t2.DestinationWell.WellName);
            double max_dest_separation = WellMathUtil.CalculateWellSpacing( t1.DstPlate.LabwareFormat, t1.DstWell, t2.DstWell);
            transfer_separation_data.Add( new TransferPair{ SrcSeparation = max_source_separation, DstSeparation = max_dest_separation, MinSeparation = Math.Min( max_source_separation, max_dest_separation), Transfer1 = t1, Transfer2 = t2});
        }
    }

    // for each pair of existing channels, compute the channel separation at the source and the channel separation at the dest.
    List< Channel> schedulable_channels = SharedMemory.GetSchedulableChannels();
    List< ChannelSeparationData> channel_separation_data = new List< ChannelSeparationData>();
    for( int i = 0; i <= schedulable_channels.Count - 2; ++i){
        for( int j = i + 1; j <= schedulable_channels.Count - 1; ++ j){
            double source_stage_channel_spacing = Hardware.GetChannelSpacing( schedulable_channels[ i].ID, schedulable_channels[ j].ID, source_stage.ID);
            double dest_stage_channel_spacing = Hardware.GetChannelSpacing( schedulable_channels[ i].ID, schedulable_channels[ j].ID, dest_stage.ID);
            channel_separation_data.Add( new ChannelSeparationData{ source_separation_ = source_stage_channel_spacing, dest_separation_ = dest_stage_channel_spacing, channel1_ = schedulable_channels[ i], channel2_ = schedulable_channels[ j]});
        }
    }

    // compute the physically possible transfers (transfers where the transfer separations exceed the channel separations on both source and dest).
    List< SeparationData> physically_possible_transfers =
        ( from tsd in transfer_separation_data from csd in channel_separation_data
            where ( tsd.SrcSeparation >= csd.source_separation_ && tsd.DstSeparation >= csd.dest_separation_)
            select new SeparationData{ tsd_ = tsd, csd_ = csd}).ToList();

    // if there are no physically possible transfers, then default to single tip mode.
    if( physically_possible_transfers.Count() == 0){
        // if we get here, then we just want to do a single pick since we couldn't find a solution
        QueueSingleChannelTransfer( transfers[0], available_channels, channel_assignments, dest_plate);
        return true;
    }

    // compute the currently possible transfers (physically possible transfers where the channels involved are available).
    var currently_possible_transfers = physically_possible_transfers.Where( x => ( available_channels.Contains( x.csd_.channel1_) && available_channels.Contains( x.csd_.channel2_)));

    // compute the currently possible adjacent transfers (currently possible transfers where the channel IDs are 1 apart).
    var currently_possible_adjacent_transfers = currently_possible_transfers.Where( x => Math.Abs(( int)( x.csd_.channel1_.ID) - ( int)( x.csd_.channel2_.ID)) == 1);

    // determine transfers to attempt:
    // if there is a currently possible adjacent transfer, then try that, else try a currently possible (non-adjacent) transfer.
    SeparationData transfers_to_attempt = currently_possible_adjacent_transfers.FirstOrDefault();
    if( transfers_to_attempt == null){
        transfers_to_attempt = currently_possible_transfers.FirstOrDefault();
    }

    // if there is nothing currently possible (but still something physically possible), then sleep a bit.
    if( transfers_to_attempt == null){
        Thread.Sleep( 10);
        return false;
    }

    if( Hardware.GetSourceAndDestSolutions( transfers_to_attempt.tsd_.Transfer1, transfers_to_attempt.tsd_.Transfer2, source_stage, dest_stage, LabwareDatabase.GetLabware( transfers_to_attempt.tsd_.Transfer1.SrcPlate.LabwareName), LabwareDatabase.GetLabware( transfers_to_attempt.tsd_.Transfer1.DstPlate.LabwareName), dest_plate,
                                            transfers_to_attempt.csd_.channel1_, transfers_to_attempt.csd_.channel2_, out channel_assignments,
                                            out source_angle, out dest_angle)) {
    } else {
        Debug.Assert( false, "Should never get here!");
    }

    return true;
}
public class SeparationData
{
    public TransferPair tsd_;
    public ChannelSeparationData csd_;
}
public class ChannelSeparationData
{
    public double source_separation_;
    public double dest_separation_;
    public Channel channel1_;
    public Channel channel2_;
}
/// <summary>
/// When we queue a single transfer, we end up just using a source and dest angle of 0 to keep it simple.
/// </summary>
/// <param name="transfer"></param>
/// <param name="available_channels"></param>
/// <param name="channel_assignments"></param>
/// <param name="dest_plate"></param>
private static void QueueSingleChannelTransfer( Transfer transfer, IList< Channel> available_channels, ICollection<KeyValuePair<Channel,Transfer>> channel_assignments, DestinationPlate dest_plate)
{
    // if the dest well is "any", need to select a well
    if( transfer.DstWell.IsAny()) {
        transfer.SetDestinationWell( dest_plate.GetFirstAvailableWell());
        dest_plate.SetWellUsageState( transfer.DstWell, WellMathUtil.WellUsageStates.Reserved);
    }

    channel_assignments.Add( new KeyValuePair<Channel,Transfer>( available_channels[ 0], transfer));
}
//-----------------------------------------------------------------------------------------
// MOSTLY REDUNDANT ANGLE CALCULATION CODE
//-----------------------------------------------------------------------------------------
public void ReturnAllChannelsHome()
{
    var available_channels = from c in Channels where c.Available select c;
    foreach (Channel c in available_channels)
        c.ReturnHome();
}
// ----------------------------------------------------------------------
public void MoveChannelsToReady()
{
    var available_channels = from c in Channels where c.Available select c;
    // get last stage.
    Stage s = GetStage( ( byte)( Stages.Count));
    // for each channel....
    foreach( Channel c in available_channels){
        // get last stage's channel teachpoint.
        StageTeachpoint stp = s.GetChannelTeachpoint( c.ID);
        // average x position for last stage's channel teachpoint.
        double x_position = ( stp.UpperLeft[ "x"] + stp.LowerRight[ "x"]) / 2;
        // go to ready....
        c.MoveToReady( x_position);
    }
}
// ----------------------------------------------------------------------
/// <summary>
/// Tries to figure out what angle to set the source and dest stages to to complete a transfer
/// </summary>
/// <param name="t1"></param>
/// <param name="t2"></param>
/// <param name="source_stage"></param>
/// <param name="dest_stage"></param>
/// <param name="source_labware"></param>
/// <param name="dest_labware"></param>
/// <param name="dest_plate"></param>
/// <param name="tip1"></param>
/// <param name="tip2"></param>
/// <param name="tip_assignments"></param>
/// <param name="source_angle"></param>
/// <param name="dest_angle"></param>
/// <returns></returns>
public bool GetSourceAndDestSolutions( Transfer t1, Transfer t2, Stage source_stage, Stage dest_stage,
                                        ILabware source_labware, ILabware dest_labware, DestinationPlate dest_plate,
                                        Channel tip1, Channel tip2,
                                        out List<KeyValuePair<Channel,Transfer>> tip_assignments,
                                        out double source_angle, out double dest_angle)
{
    SourcePlate source_plate = t1.SrcPlate;
    tip_assignments = new List<KeyValuePair<Channel,Transfer>>();

    // given the tips in the tip_assignments, figure out how far they are spaced apart
    double source_tip_spacing = GetChannelSpacing( tip1.ID, tip2.ID, source_stage.ID);
    double dest_tip_spacing = GetChannelSpacing( tip1.ID, tip2.ID, dest_stage.ID);
            
    // need to be able to change the well names that we are going to transfer from
    Well transfer1_well = t1.DstWell;
    Well transfer2_well = t2.DstWell;
    // now break the task up into three specific cases:
    // 1. neither t1 nor t2 has a destination well of "any"
    // 2. both t1 and t2 have a destination well of "any"
    // 3. either t1 or t2 has a destination well of "any"
    bool dest1_is_any = transfer1_well.IsAny();
    bool dest2_is_any = transfer2_well.IsAny();
    bool source_has_solution, dest_has_solution;
    // 1: neither dest well is "any"
    if( !dest1_is_any && !dest2_is_any) {
        source_has_solution = WellMathUtil.GetAngleForTwoTips( source_plate.LabwareFormat, t1.SrcWell, t2.SrcWell, tip1.ID, tip2.ID, source_tip_spacing, out source_angle);
        dest_has_solution = WellMathUtil.GetAngleForTwoTips( dest_plate.LabwareFormat, t1.DstWell, t2.DstWell, tip1.ID, tip2.ID, dest_tip_spacing, out dest_angle);
    }
    // 2: both dest wells are "any"
    else if( dest1_is_any && dest2_is_any) {
        source_has_solution = WellMathUtil.GetAngleForTwoTips( source_plate.LabwareFormat, t1.SrcWell, t2.SrcWell, tip1.ID, tip2.ID, source_tip_spacing, out source_angle);
        dest_has_solution = GetAngleForTwoAnyDestTips( ref transfer1_well, ref transfer2_well, tip1.ID, tip2.ID, dest_tip_spacing, dest_plate, out dest_angle);
        if( dest_has_solution && source_has_solution) {
            _log.DebugFormat( "found two dest 'any' well solutions: {0} and {1}, angle = {2}", transfer1_well.WellName, transfer2_well.WellName, dest_angle);
        }
        if( !dest_has_solution)
            _log.Debug( "no dest well solution found for two 'any' wells");
        if( !source_has_solution)
            _log.Debug( "no source well solution found for two 'any' wells");
    }
    // 3: either dest well is "any"
    else if( dest1_is_any || dest2_is_any) {
        source_has_solution = WellMathUtil.GetAngleForTwoTips( source_plate.LabwareFormat, t1.SrcWell, t2.SrcWell, tip1.ID, tip2.ID, source_tip_spacing, out source_angle);
        dest_has_solution = GetAngleForOneAnyDestTip( ref transfer1_well, ref transfer2_well, tip1.ID, tip2.ID, dest_tip_spacing, dest_plate, out dest_angle);
        if( dest_has_solution) {
            _log.DebugFormat( "found 'any' well solution: {0} and {1}, angle = {2}", transfer1_well.WellName, transfer2_well.WellName, dest_angle);
        }
        if( !dest_has_solution)
            _log.Debug( "no dest well solution found for one 'any' well");
        if( !source_has_solution)
            _log.Debug( "no source well solution found for one 'any' well");
    } else {
        source_angle = dest_angle = 0;
        source_has_solution = dest_has_solution = false;
    }

    // if we have an answer, we're done!  otherwise, loop again
    if( source_has_solution && dest_has_solution) {
        t1.SetDestinationWell( transfer1_well);
        t2.SetDestinationWell( transfer2_well);
        // make the tip assignments so we know which tip is transferring what
        tip_assignments.Add( new KeyValuePair< Channel,Transfer>( tip1, t1));
        tip_assignments.Add( new KeyValuePair< Channel,Transfer>( tip2, t2));
        return true;
    } else {
        return false;
    }
}
// ----------------------------------------------------------------------
/// <summary>
/// Used for the case where we have two destination wells marked as "any".  Ideally, we'd try to keep the plate
/// oriented so that we can get all four tips in the plate at the same time, i.e. the angle would likely need
/// to be around 0 degrees for the best result.
/// </summary>
/// <param name="dest1_well"></param>
/// <param name="dest2_well"></param>
/// <param name="tip1_id"></param>
/// <param name="tip2_id"></param>
/// <param name="dest_tip_spacing"></param>
/// <param name="dest_plate"></param>
/// <param name="dest_angle"></param>
/// <returns></returns>
private static bool GetAngleForTwoAnyDestTips( ref Well dest1_well, ref Well dest2_well, byte tip1_id, byte tip2_id, double dest_tip_spacing, DestinationPlate dest_plate, out double dest_angle)
{
    dest_angle = 0;

    // the idea here is to look at the tips requested, and then figure out if there is a way to make
    // the dest plate angle remain in the same position for the next pair.  For now, it's only going
    // to be smart enough to deal with the case where we have 4 channels, because handling 6 or 8
    // channels would require there to be another layer of intelligence that knows which pair of
    // channels will access the destination plate after these two tips.

    // 1. look at tips 1 and 2.  What are their IDs?  Is 1 above or below 2?  What's their spacing?
    // 2. look at the channel definitions.  What are the other IDs remaining?  What's their spacing?
    // 3. get the current stage position.
    //    a. check to see if there are two wells that tips 1 and 2 can hit without moving the dest stage!  If
    //       so, great, we don't need to move the stage
    //    b. if tips 1 and 2 can't hit any two wells at the current location, try to solve the problem
    //       for 0 degrees.  Ideally, we'd have tip id 1 on row A, tip id 2 on row C, tip id 3 on row E,
    //       and tip id 4 on row G for a 96 well plate and tips with 18mm spacing.
    // ***  FOR NOW, just do the easiest thing, which is to get any two wells that aren't already used.
    //      optimize for minimized contention at dest plate later.

    // yes, starting i and j from 0 doesn't make sense because you can't put two tips in the same
    // dest well, but it should be smart enough to automatically skip this answer, and it just
    // makes the code easier to read.
    for( int i = 0; i < dest_plate.LabwareFormat.NumWells; ++i){
        for( int j = 0; j < dest_plate.LabwareFormat.NumWells; ++j){
            dest1_well = new Well( dest_plate.LabwareFormat, i);
            dest2_well = new Well( dest_plate.LabwareFormat, j);
            if( dest_plate.GetWellUsageState( dest1_well) != WellMathUtil.WellUsageStates.Available ||
                dest_plate.GetWellUsageState( dest2_well) != WellMathUtil.WellUsageStates.Available) {
                continue;
            }

            // figure out if the tip spacing will work for the well spacing, and
            // the destination stage angle for these two wells
            if( !WellMathUtil.GetAngleForTwoTips( dest_plate.LabwareFormat, dest1_well, dest2_well, tip1_id, tip2_id, dest_tip_spacing, out dest_angle))
                continue;
            return true;
        }
    }

    return false;
}
// ----------------------------------------------------------------------
private static bool GetAngleForOneAnyDestTip( ref Well dest1_well, ref Well dest2_well, byte tip1_id, byte tip2_id, double dest_tip_spacing, DestinationPlate dest_plate, out double dest_angle)
{
    dest_angle = 0;
            
    Debug.Assert( !dest1_well.IsAny());
    Debug.Assert( !dest2_well.IsAny());
    // need to know which well was specified by the hitpick file
    Well specific_wellname = !dest1_well.IsAny() ? dest1_well : dest2_well;
    for( int i = 0; i < dest_plate.LabwareFormat.NumWells; ++i){
        // continue if the well isn't available
        // string any_new_well_name = Wells.IndexToWellName( i, dest_number_of_wells);
        Well any_new_well_name = new Well( dest_plate.LabwareFormat, i);
        if( dest_plate.GetWellUsageState( any_new_well_name) != WellMathUtil.WellUsageStates.Available)
            continue;
        // figure out if the tip spacing will work for the well spacing, and
        // the destination stage angle for these two wells
        if( !WellMathUtil.GetAngleForTwoTips( dest_plate.LabwareFormat, any_new_well_name, specific_wellname, tip1_id, tip2_id, dest_tip_spacing, out dest_angle))
            continue;
        return true;
    }
    return false;
}
// ----------------------------------------------------------------------
// PREFER USING DISPATCHER TO SEND STOPS
// - MORE CONSISTENT WITH THE REST OF THE CODE
// ----------------------------------------------------------------------
internal void ServosOn( bool on)
{
    try{
        _hw.Enable( on);
    } catch( Exception ex){
        MessageBox.Show( "Could not turn on servos: " + ex.Message);
    }
}
// ----------------------------------------------------------------------
public void Enable( bool enable)
{
    // enable all axes.
    if( enable){
        _ts.EnableAllAxes();
        return; // leave if enabling so we don't try to enable again sequentially
    }
    var available_channels = from c in Channels where c.Available select c;
    foreach( Stage s in Stages)
        s.Enable( false, true);
    foreach( Channel c in available_channels)
        c.Enable( false, true);
}
// ----------------------------------------------------------------------
// no longer used: MOSTLY PIONEER CODE FOR STARTING A PROCESS -- hopefully, to be replaced by persistent scheduler
         
// private Teachpoints _teachpoints = null;
// private BioNex.Shared.DeviceInterfaces.ExternalPlateTransferSchedulerInterface _platehandler = null;
// private object _transfer_lock = new object();
// private string TipHandlingMethod { get; set; }
// private readonly Dictionary<int, byte> SourceTransferThreads = new Dictionary<int, byte>();
// private readonly Dictionary<int, AutoResetEvent> SourceTransferThreadAvailableEvents = new Dictionary<int,AutoResetEvent>();
public void TestTipShuttle()
{
    TipCarrier tip_carrier = new TipCarrier( null, 12, 8, 9.0, 9.0);
    tip_carrier.SetAll( TipCarrierWellState.Clean);
    var ans1 = tip_carrier.GetClean( new HashSet< int>{ 1, 2, 5});
    TipCarrier.ReserveTips( ans1.Values);
    var ans2 = tip_carrier.GetClean( new HashSet< int>{ 1, 2, 5});
    TipCarrier.ReserveTips( ans2.Values);
    tip_carrier.GetEmpty( new HashSet< int>{ 1, 2, 5});
}
public void SetTeachpoints( Teachpoints tps)
{
    _teachpoints = tps;
}
public void SetPlateHandler( ExternalPlateTransferSchedulerInterface plate_handler)
{
    _platehandler = ( BioNex.Shared.DeviceInterfaces.ExternalPlateTransferSchedulerInterface)plate_handler;
}
public void SetTipHandlingMethod( string method_name)
{
    TipHandlingMethod = method_name;
}
public delegate void TransferProcess( TransferOverview to);
public delegate void DestinationStageScheduler( List<Transfer> transfers);
public delegate void SourcePlateScheduler( List<Transfer> source_transfers_only, Stage source_stage, Stage dest_stage);
public void StartProcess( TransferOverview to)
{
    shared_memory_ = new ServiceSharedMemory( _hw.Stages, _hw.Channels, config_);

    RunState = RunStateT.Normal;

    // move to sched. constructor
    ChannelService channel_service = new ChannelService( _hw, LabwareDatabase, ErrorInterface, LiquidProfileLibrary, _teachpoints, Messenger, shared_memory_, OutputPlugin, ProtocolDispatcher);
    channel_service.StartService();

    // move to sched. constructor
    shared_memory_.AddTransfers( to.Transfers);
    var src_labwares = ( from t in to.Transfers select t.Source.LabwareName).Distinct();
    var dst_labwares = ( from t in to.Transfers select t.Destination.LabwareName).Distinct();
    var labwares = src_labwares.Union( dst_labwares).Union( new List< string>{ "tipbox"});
    // double max_plate_thickness = ( labwares).Max( l => double.Parse( LabwareDatabase.GetLabware( l).Properties[ LabwarePropertyNames.Thickness].ToString()));
    // double max_tallest_teachpoint = ( from c in _hw.Channels
    //                                   from s in _hw.Stages
    //                                   select new{ Channel = c, Stage = s}).Max( cs => _teachpoints.GetStageTeachpoint( cs.Channel.GetID(), cs.Stage.GetID())[ "z"]);

    // aborting is accomplished by setting Aborting, which makes the following two 
    // while loops fall through.  This then stops the tip and plate services
    while( !shared_memory_.AreAllTransfersDone() && RunState != RunStateT.Aborting)
        Thread.Sleep( 100);
    // wait for all channels to finish what they are doing, and wait for stages to become empty
    while(( shared_memory_.CountChannelsByStatus( ServiceSharedMemory.ChannelStatus.CleanTip) != shared_memory_.GetNumSchedulableChannels() || shared_memory_.GetNumAvailableStages() != _hw.Stages.Count) && RunState != RunStateT.Aborting)
        Thread.Sleep( 100);

    // move to scheduler constructor
    channel_service.StopService();

    channel_service.Dispose();

    if( RunState == RunStateT.Aborting)
        _log.Info( "Aborted");
}
//-----------------------------------------------------------------------------------------
// dead code from servicesharedmemory -- removal of channel usage:
//-----------------------------------------------------------------------------------------
/// <summary>
/// ChannelUsage helps us to keep track of which channels are being used, so that we
/// don't ask a channel to do multiple transfers at the same time
/// </summary>
// private Dictionary< Channel, Channel.ChannelStatus> ChannelUsage { get; set; }
//-----------------------------------------------------------------------------------------
// -- from constructor:
    // #warning Disposable tip scheduler assumes dirty tips on channels.  First action is to shuck.  Washable tip scheduler assumes no tips on channel.  First action is to press.  Should we still shuck off dirty tips to trash?  This would mean supporting two different shucks.
    // ChannelUsage = hardware.AvailableChannels.ToDictionary( c => c, c => Channel.Status.DirtyTip);
    ChannelUsage = hardware.AvailableChannels.ToDictionary( c => c, c => Channel.ChannelStatus.NoTip);
//-----------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------
// CHANNEL USAGE
//-----------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------
public IDictionary< Channel, Channel.ChannelStatus> GetChannelStatuses()
{
    lock_.EnterReadLock();
    try{
        return new Dictionary< Channel, Channel.ChannelStatus>( ChannelUsage);
    } catch( Exception){
    } finally{
        lock_.ExitReadLock();
    }
    return null;
}
//-----------------------------------------------------------------------------------------
public void RemoveChannel( Channel c)
{
    lock_.EnterWriteLock();
    try {
        ChannelUsage.Remove( c);
    } catch( Exception) {
    } finally {
        lock_.ExitWriteLock();
    }
}
//-----------------------------------------------------------------------------------------
public bool GetChannelState( Channel c)
{
    lock_.EnterReadLock();
    try {
        return ChannelUsage[ c];
    } catch( Exception){
    } finally{
        lock_.ExitReadLock();
    }
    return true;
}
//-----------------------------------------------------------------------------------------
public void SetChannelState( Channel c, Channel.ChannelStatus channel_status)
{
    lock_.EnterWriteLock();
    try {
        if( ChannelUsage.ContainsKey( c)){
            ChannelUsage[ c] = channel_status;
        }
    } catch( Exception){
    } finally{
        lock_.ExitWriteLock();
    }
}
//-----------------------------------------------------------------------------------------
public List< Channel> GetSchedulableChannels() -- NOT named GetAvailableChannels because available implies that the channels are idle and ready for work
{
    lock_.EnterReadLock();
    try {
        return ChannelUsage.Keys.ToList();
    } catch( Exception){
    } finally{
        lock_.ExitReadLock();
    }
    return null;
}
//-----------------------------------------------------------------------------------------
public int GetNumSchedulableChannels()
{
    lock_.EnterReadLock();
    try {
        return ChannelUsage.Keys.Count;
    } catch( Exception){
    } finally{
        lock_.ExitReadLock();
    }
    return 0;
}
//-----------------------------------------------------------------------------------------
public List< Channel> GetChannelsByStatus( Channel.ChannelStatus channel_status)
{
    lock_.EnterReadLock();
    try {
        return ChannelUsage.Where( cu => cu.Value == channel_status).Select( c => c.Key).ToList();
    } catch( Exception){
    } finally{
        lock_.ExitReadLock();
    }
    return null;
}
//-----------------------------------------------------------------------------------------
public int CountChannelsByStatus( Channel.ChannelStatus channel_status)
{
    lock_.EnterReadLock();
    try {
        return ChannelUsage.Count( cu => cu.Value == channel_status);
    } catch( Exception){
    } finally{
        lock_.ExitReadLock();
    }
    return 0;
}
//-----------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------
// VERY OLD SERVICE SHARED MEMORY CODE BACK TO WHEN WE FIRST STARTED THE SERVICE SHARED MEMORY CONCEPT.
//-----------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------
/*
/// <summary>
/// Given a bunch of barcodes, only return those that involve the specified SOURCE barcodes
/// </summary>
/// <param name="barcodes"></param>
/// <returns></returns>
public IEnumerable< Transfer> GetTransfersForSourceStages( IEnumerable<Transfer> transfers, IEnumerable<Stage> stages)
{
    lock_.EnterReadLock();
    try {
        List<Transfer> total = new List<Transfer>();
        foreach( Stage stage in stages) {
            string barcode = StageUsage.First( p => p.Stage == stage).Plate.Barcode;
            var transfers_with_this_barcode = from t in transfers where t.Source.Barcode == barcode select t;
            total.Union( transfers_with_this_barcode);
        }
        return total;
    } catch( Exception){
    } finally{
        lock_.ExitReadLock();
    }
    return null;
}
//-----------------------------------------------------------------------------------------
// DKM 2010-09-13 I had to change back to the other logic -- passing in source_stages doesn't work
//                because we eventually hit the condition where a stage is done with all of its transfers,
//                and it then ALWAYS becomes the least busy source stage.  But then the tip service
//                thread checks its plate reference to see if there are any transfers to schedule,
//                and since the stage's plate is null, it doesn't schedule any more.  We want
//                to instead compare the stages with transfers in progress with the stages with transfers.
//-----------------------------------------------------------------------------------------
private class TransferOnDeck
{
    public WorkingTransfer working_transfer;
    public StageInfo src_stage_info;
    public StageInfo dst_stage_info;
}
//-----------------------------------------------------------------------------------------
public Stage GetLeastBusySourceStage()
{
    lock_.EnterReadLock();
    try{
        // get all unfinished transfers on deck.
        var unfinished_transfers_on_deck = from su1 in StageUsage from su2 in StageUsage from wt in WorkingTransfers
                                            where su1.Plate != null && su1.Plate.Barcode == wt.Transfer.SrcPlate.Barcode &&
                                                    su2.Plate != null && su2.Plate.Barcode == wt.Transfer.DstPlate.Barcode &&
                                                    wt.Status != TransferStatus.Done
                                            select new TransferOnDeck{ working_transfer = wt, src_stage_info = su1, dst_stage_info = su2};
        // find the source stages with unstarted work.
        var stages_with_transfers_not_started = from tod in unfinished_transfers_on_deck 
                                                where tod.working_transfer.Status == TransferStatus.NotStarted
                                                group tod.src_stage_info by tod.src_stage_info.Stage into g
                                                select g.Key;
        // if there is no more work to schedule, then break.
        if( stages_with_transfers_not_started.Count() == 0){
            return null;
        }
        // if there is work to schedule on only one of the stages, then return that stage.
        if( stages_with_transfers_not_started.Count() == 1){
            return stages_with_transfers_not_started.First();
        }
        // otherwise, there must be work to schedule on multiple stages.
        // find the source stages with work in progress.
        var stages_with_transfers_in_progress = from tod in unfinished_transfers_on_deck 
                                                where tod.working_transfer.Status == TransferStatus.Aspirating || tod.working_transfer.Status == TransferStatus.Dispensing
                                                // FYC notes that we shouldn't look at OR DISPENSING since we're looking for least busy SOURCE stage (if already dispensing, then SOURCE stage has already been freed)!!
                                                group tod.src_stage_info by tod.src_stage_info.Stage into g
                                                select g.Key;
        // if no work is currently in progress, then return the first stage.
        if( stages_with_transfers_in_progress.Count() == 0){
            return stages_with_transfers_not_started.First();
        }
        // if there is work in progress on only one of the stages, then pick the first of the other stage(s).
        if( stages_with_transfers_in_progress.Count() == 1){
            return stages_with_transfers_not_started.Except( stages_with_transfers_in_progress).FirstOrDefault();
        }
        // otherwise, there must be work in progress on multiple stages.
        // pick the least busiest.
        var in_progress_transfers_per_source = from tod in unfinished_transfers_on_deck
                                                where tod.working_transfer.Status == TransferStatus.Aspirating || tod.working_transfer.Status == TransferStatus.Dispensing
                                                // FYC notes that we shouldn't look at OR DISPENSING since we're looking for least busy SOURCE stage (if already dispensing, then SOURCE stage has already been freed)!!
                                                group tod.working_transfer by tod.working_transfer.Transfer.SrcPlate into g
                                                select new{ Source = g.Key, InProgress = g.Count()};
        if( in_progress_transfers_per_source.Count() == 0)
            return null;
        int min = in_progress_transfers_per_source.Min( x => x.InProgress);
        Plate plate = in_progress_transfers_per_source.FirstOrDefault( x => x.InProgress == min).Source;
        StageInfo pi = StageUsage.FirstOrDefault( x => x.Plate.Barcode == plate.Barcode);
        return ( pi == null) ? null : pi.Stage;
    } catch( Exception){
    } finally{
        lock_.ExitReadLock();
    }
    return null;
}
//-----------------------------------------------------------------------------------------
public string GetLabwareTypeForBarcode( string barcode)
{
    lock_.EnterReadLock();
    try {
        WorkingTransfer source_transfer = WorkingTransfers.FirstOrDefault( t => ( t.Transfer.Source.Barcode == barcode));
        if( source_transfer != null){
            return source_transfer.Transfer.Source.LabwareName;
        }
        WorkingTransfer dest_transfer = WorkingTransfers.FirstOrDefault( t => ( t.Transfer.Destination.Barcode == barcode));
        if( dest_transfer != null){
            return dest_transfer.Transfer.Destination.LabwareName;
        }
    } catch( Exception){
    } finally{
        lock_.ExitReadLock();
    }
    return "";
}
//-----------------------------------------------------------------------------------------
// DEVELOPMENT
//-----------------------------------------------------------------------------------------
public bool f( List< Stage> plate_stages, out bool load, out string barcode, out Stage stage)
{
    // all the empty plate stages.
    var empty_plate_stages = from ps in plate_stages where GetStageStatus( ps) == StageStatus.Empty orderby ps.GetID() select ps;
    // all the full plate stages.
    var full_plate_stages = from ps in plate_stages where GetStageStatus( ps) == StageStatus.Full orderby ps.GetID() select ps;
    // all the barcode on deck.
    var barcodes_on_deck = from su in StageUsage
                            where su.Status == StageStatus.Full && su.Plate != null && su.Plate.Barcode != null && su.Plate.Barcode != ""
                            select su.Plate.Barcode;
    // all the destination barcodes on and off deck.
    var dst_barcodes_on_deck = DstBarcodes.Intersect( barcodes_on_deck);
    var dst_barcodes_off_deck = DstBarcodes.Except( dst_barcodes_on_deck);
    // all the source barcodes on and off deck.
    var src_barcodes_on_deck = SrcBarcodes.Intersect( barcodes_on_deck);
    var src_barcodes_off_deck = SrcBarcodes.Except( src_barcodes_on_deck);
    // all the destination barcodes on deck that are still involved in unfinished transfers.
    // note: some of the unfinished transfers may involve source plates that aren't on deck.
    var dst_barcodes_on_deck_with_unfinished_transfers = ( from wt in WorkingTransfers from bc in dst_barcodes_on_deck
                                                            where wt.Status != TransferStatus.Done && wt.Transfer.Destination.Barcode == bc
                                                            select bc).Distinct();
    // all the destination barcodes on deck that have nothing more to do, ever!
    var completely_finished_dst_barcodes_on_deck = dst_barcodes_on_deck.Except( dst_barcodes_on_deck_with_unfinished_transfers);
    // all the source barcodes on deck that are still involved in unfinished transfers.
    // note: some of the unfinished transfers may involve destination plates that aren't on deck.
    var src_barcodes_on_deck_with_unfinished_transfers = ( from wt in WorkingTransfers from bc in src_barcodes_on_deck
                                                            where wt.Status != TransferStatus.Done && wt.Transfer.Source.Barcode == bc
                                                            select bc).Distinct();
    // all the source barcodes on deck that have nothing more to do, ever!
    var completely_finished_src_barcodes_on_deck = src_barcodes_on_deck.Except( src_barcodes_on_deck_with_unfinished_transfers);
    // all the source barcodes on deck that are still involved in unfinished transfers where the destination is also on deck.
    var src_barcodes_on_deck_with_unfinished_transfers_on_deck = ( from wt in WorkingTransfers from bc_s in src_barcodes_on_deck from bc_d in dst_barcodes_on_deck
                                                                    where wt.Status != TransferStatus.Done && wt.Transfer.Source.Barcode == bc_s && wt.Transfer.Destination.Barcode == bc_d
                                                                    select bc_s).Distinct();
    // if there are empty plate stages, then try to load them.
    if( empty_plate_stages.Count() > 0){
        load = true; // set the load flag to true.
        // if there aren't any destination barcodes on deck with unfinished transfers (any on-deck destination plate is completely finished), then try to load a destination plate.
        if( dst_barcodes_on_deck_with_unfinished_transfers.Count() == 0){
            stage = empty_plate_stages.Last(); // destination plates should be placed "in the back."
            // try to load the (off-deck) destination plate that would result in the most transfers on deck with the (on-deck) source plates.
            var transfers_on_deck_for_off_deck_dst_barcodes = from wt in WorkingTransfers from bc_s in src_barcodes_on_deck_with_unfinished_transfers from bc_d in dst_barcodes_off_deck
                                                                where wt.Status != TransferStatus.Done && wt.Transfer.Source.Barcode == bc_s && wt.Transfer.Destination.Barcode == bc_d
                                                                group bc_d by bc_d into g
                                                                select new{ Barcode = g.Key, Count = g.Count()};
            // if there are (off-deck) destination plates that would result in transfers on deck with the (on-deck) source plates, then load the destination plate that would result in the most transfers on deck.
            if( transfers_on_deck_for_off_deck_dst_barcodes.Count() > 0){
                barcode = transfers_on_deck_for_off_deck_dst_barcodes.FirstOrDefault( bc_t => bc_t.Count == transfers_on_deck_for_off_deck_dst_barcodes.Max( bc_t2 => bc_t2.Count)).Barcode;
                return true;
            }
            // try to load any destination plate with unstarted transfers.
            // (such a destination plate is guaranteed to be off deck because we've checked that there are no on-deck destination barcodes with unfinished transfers.)
            if( GetFirstUnstartedTransfer().Destination.Barcode != null){
                barcode = GetFirstUnstartedTransfer().Destination.Barcode;
                return true;
            }
        }

        // at this point, either:
        // 1. there are destination barcodes on deck with unfinished transfers, or
        // 2. there aren't destination barcodes on deck with unfinished transfers because we're done.
        // if there aren't any source barcodes on deck
        if( src_barcodes_on_deck_with_unfinished_transfers_on_deck.Count() == 0){
            stage = empty_plate_stages.First(); // source plates should be placed "in the front."
            // try to load the (off-deck) source plate...
            var transfers_on_deck_for_off_deck_src_barcodes = from wt in WorkingTransfers from bc_s in src_barcodes_off_deck from bc_d in dst_barcodes_on_deck_with_unfinished_transfers
                                                                where wt.Status != TransferStatus.Done && wt.Transfer.Source.Barcode == bc_s && wt.Transfer.Destination.Barcode == bc_d
                                                                group bc_s by bc_s into g
                                                                select new{ Barcode = g.Key, Count = g.Count()};
            var x = from wt in WorkingTransfers from bc_t in transfers_on_deck_for_off_deck_src_barcodes from bc_d in dst_barcodes_off_deck
                    where wt.Status != TransferStatus.Done && wt.Transfer.Source.Barcode == bc_t.Barcode && wt.Transfer.Destination.Barcode == bc_d
                    group bc_t by bc_t into g
                    select new{ TransfersOnDeckForOffDeckSrcBarcodes = g.Key, Count = g.Count()};
        }
    }

    // if there are full plate stages, then try to unload them.
    if( full_plate_stages.Count() > 0){
        load = false;
        barcode = null;
        stage = null;
        if( completely_finished_dst_barcodes_on_deck.Count() > 0){
            return true;
        }
        if( completely_finished_src_barcodes_on_deck.Count() > 0){
            return true;
        }
    }
    load = false;
    barcode = null;
    stage = null;
    return false;
}
//-----------------------------------------------------------------------------------------
// dead STAGE USAGE code from SERVICE SHARED MEMORY
//-----------------------------------------------------------------------------------------
public StageStatus GetStageStatus( Stage stage)
{
    lock_.EnterReadLock();
    try {
        StageInfo stage_info = StageUsage.FirstOrDefault( si => si.Stage == stage);
        Debug.Assert( stage_info != null);
        return stage_info.Status;
    } catch( Exception){
    } finally{
        lock_.ExitReadLock();
    }
    return StageStatus.Full;
}
//-----------------------------------------------------------------------------------------
public void SetStageStatus( Stage stage, StageStatus status)
{
    lock_.EnterWriteLock();
    try {
        StageInfo stage_info = StageUsage.FirstOrDefault( si => si.Stage == stage);
        Debug.Assert( stage_info != null);
        stage_info.Status = status;
    } catch( Exception){
    } finally{
        lock_.ExitWriteLock();
    }
}
//-----------------------------------------------------------------------------------------
public Plate GetStagePlate( Stage stage)
{
    lock_.EnterReadLock();
    try {
        if( StageUsage.FirstOrDefault( si => si.Stage == stage && si.Stage.Plate != null) == null)
            return null;
        return StageUsage.First( si => si.Stage == stage).Stage.Plate;
    } catch( Exception){
    } finally{
        lock_.ExitReadLock();
    }
    return null;
}
//-----------------------------------------------------------------------------------------
public Stage StageLookup( Plate plate)
{
    lock_.EnterReadLock();
    try{
        StageInfo stage_info = StageUsage.FirstOrDefault( si => si.Stage.Plate == plate);
        return stage_info == null ? null : stage_info.Stage;
    } catch( Exception){
    } finally{
        lock_.ExitReadLock();
    }
    return null;
}
//-----------------------------------------------------------------------------------------
/// <summary>
/// this is used to figure out when we should report that the protocol is done -- all stages
/// should be empty!
/// </summary>
/// <returns></returns>
//-----------------------------------------------------------------------------------------
public int GetNumAvailableStages()
{
    lock_.EnterReadLock();
    try {
        return StageUsage.Count( su => su.Status == StageStatus.Empty);
    } catch( Exception) {
    } finally {
        lock_.ExitReadLock();
    }
    return 0;
}
//-----------------------------------------------------------------------------------------
// dead TRANSFER STATUS code from SERVICE SHARED MEMORY
//-----------------------------------------------------------------------------------------
public TransferStatus GetTransferStatus( Transfer transfer) 
{
    lock_.EnterReadLock();
    try {
        return WorkingTransfers.First( wt => wt.Transfer == transfer).Status;
    } catch( Exception){
    } finally{  
        lock_.ExitReadLock();
    }
    return TransferStatus.InProgress;
}
//-----------------------------------------------------------------------------------------
public bool AreAllTransfersDone() 
{
    lock_.EnterReadLock();
    try {
        return WorkingTransfers.Count( wt => wt.Status != TransferStatus.Done) == 0;
    } catch( Exception){
    } finally{
        lock_.ExitReadLock();
    }
    return false;
}
//-----------------------------------------------------------------------------------------
public bool AreAllTransfersForDstBarcodeDone( string dst_barcode)
{
    lock_.EnterReadLock();
    try {
        return WorkingTransfers.Count( wt => wt.Transfer.Destination.Barcode == dst_barcode && wt.Status != TransferStatus.Done) == 0;
    } catch( Exception){
    } finally{
        lock_.ExitReadLock();
    }
    return false;
}
//-----------------------------------------------------------------------------------------
public bool AreAllTransfersForSrcAndDstBarcodesDone( string src_barcode, string dst_barcode)
{
    lock_.EnterReadLock();
    try {
        return WorkingTransfers.Count( wt => wt.Transfer.Source.Barcode == src_barcode && wt.Transfer.Destination.Barcode == dst_barcode && wt.Status != TransferStatus.Done) == 0;
    } catch( Exception){
    } finally{
        lock_.ExitReadLock();
    }
    return false;
}
//-----------------------------------------------------------------------------------------
public Transfer GetFirstUnstartedTransfer()
{
    lock_.EnterReadLock();
    try {
        WorkingTransfer working_transfer = WorkingTransfers.FirstOrDefault( wt => wt.Status == TransferStatus.NotStarted);
        if( working_transfer == null){
            return null;
        }
        return working_transfer.Transfer;
    } catch( Exception){
    } finally{
        lock_.ExitReadLock();
    }
    return null;
}
//-----------------------------------------------------------------------------------------
public IEnumerable< Transfer> GetUnstartedTransfersByDstBarcode( string dst_barcode)
{
    lock_.EnterReadLock();
    try {
        return from wt in WorkingTransfers 
                where wt.Transfer.Destination.Barcode == dst_barcode && wt.Status == TransferStatus.NotStarted
                select wt.Transfer;
    } catch( Exception){
    } finally{
        lock_.ExitReadLock();
    }
    return null;
}
//-----------------------------------------------------------------------------------------
public IEnumerable< Transfer> GetUnstartedTransfersOnDeck()
{
    lock_.EnterReadLock();
    try{
        return from wt in WorkingTransfers
                where StageUsage.Where( su => su.Plate != null).Select( su => su.Plate.Barcode).Contains( wt.Transfer.Source.Barcode) && StageUsage.Where( su => su.Plate != null).Select( su => su.Plate.Barcode).Contains( wt.Transfer.Destination.Barcode) && wt.Status == TransferStatus.NotStarted
                select wt.Transfer;
    } catch( Exception){
    } finally{
        lock_.ExitReadLock();
    }
    return null;
}
//-----------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------
// removal of TipShuckAcceleration.
//-----------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------
/// <summary>
/// Tip shuck acceleration is in the same units as motor settings, i.e. mm/s^2
/// </summary>
/// <param name="node"></param>
/// <returns></returns>
private static TipShuckSettings ParseTipShuckSettings( XmlNode node)
{
    TipShuckSettings settings = new TipShuckSettings();

    foreach( XmlNode n in node) {
        if( n.Name == "acceleration")
            settings.Acceleration = double.Parse( n.InnerText);
    }

    return settings;
}
// ----------------------------------------------------------------------
public double GetTipShuckAcceleration()
{
    if( _tip_shuck_settings == null) {
        Log.DebugFormat( "No tip shuck acceleration specified in hardware_configuration.xml for {0}.  Using default value of {1}", this, ZAxis.Settings.Acceleration);
        return ZAxis.Settings.Acceleration;
    }

    return _tip_shuck_settings.Acceleration;
}
// ----------------------------------------------------------------------
// ----------------------------------------------------------------------
// ----------------------------------------------------------------------
// REMOVED AS A RESULT OF MERGING TIPCARRIERWELL INTO WELL
// ----------------------------------------------------------------------
// ----------------------------------------------------------------------
// ----------------------------------------------------------------------
private Dictionary< int, TipHomeLocation> GetState( HashSet< int> channel_ids, double preferred_offset, double channel_spacing, TipHomeLocationState state)
{
    // get channel positions zeroed against the smallest ordinal channel's position.
    IDictionary< int, double> zeroed_channel_positions = channel_ids.OrderBy( x => x).ToDictionary( y => y, y => ( y - channel_ids.Min()) * channel_spacing);

    int num_zeroed_channel_positions = zeroed_channel_positions.Count();
    // caller must supply non-empty set of valid channel ids.
    if( num_zeroed_channel_positions  == 0){
        throw new Exception( "error -- no channel positions to search for");
    }

    // create dictionary that maps well position (row position) to first well in the row that matches the desired state.
    // (there may not be a well in the row that matches the desired state, in which case the value would be null.)
    IDictionary< double, TipHomeLocation> first_well_of_state_in_each_position = TipHomeLocations.ToDictionary( x => x.Key * RowToRowSpacing, x => x.Value.FirstOrDefault( tcw => tcw.State == state));
    // from the dictionary above, pull out just the well positions that correspond to rows that have well(s) that match the desired state.
    IDictionary< double, TipHomeLocation> well_positions = first_well_of_state_in_each_position.Where( x => x.Value != null).ToDictionary( x => x.Key, x => x.Value);

    // convert range of integers from 0 to carrier_rows - 1 to an enumeration of "offsets" -- different row positions to start matching against zeroed_channel_positions.
    // for each offset, create dictionary that maps channel ids to tip-carrier wells.
    List< Dictionary< int, TipHomeLocation>> channel_well_matches = Enumerable.Range( 0, TipHomeLocations.Count()).Select( row => row * RowToRowSpacing).Select( offset =>
        ( from ocp in zeroed_channel_positions.ToDictionary( x => x.Key, x => x.Value + offset)
            from wp in well_positions
            where ocp.Value == wp.Key
            select new{ ChannelId = ocp.Key, Tip = wp.Value}).ToDictionary( y => y.ChannelId, y => y.Tip)).ToList();

    // search the list of dictionaries that map channel ids to tip-carrier wells for the dictionaries with the most mappings.
    int max_channel_well_matches_count = channel_well_matches.Max( x => x.Count);
    List< Dictionary< int, TipHomeLocation>> max_channel_well_matches = channel_well_matches.Where( x => x.Count == max_channel_well_matches_count).ToList();

    Dictionary< int, TipHomeLocation> found_match = max_channel_well_matches.Aggregate(( shallowest, iter) => ( shallowest == null || ( iter.Sum( cwm => cwm.Value.ColIndex) < shallowest.Sum( cwm => cwm.Value.ColIndex))) ? iter : shallowest);

    return found_match;
}
// ----------------------------------------------------------------------
public Dictionary< int, Tip> GetEmpty( HashSet< int> channel_ids, double preferred_offset = double.NaN, double channel_spacing = 18.0)
{
    return GetState( channel_ids, preferred_offset, channel_spacing, Tip.Empty);
}
public Dictionary< int, Tip> GetClean( HashSet< int> channel_ids, double preferred_offset = double.NaN, double channel_spacing = 18.0)
{
    return GetState( channel_ids, preferred_offset, channel_spacing, TipCarrierWellState.Clean);
}
public static bool ReserveTips( ICollection< Tip> tips)
{
    lock( TipStateLock){
        // make sure tips are all clean:
        if( tips.Count( t => t.State != TipCarrierWellState.Clean) > 0){
            return false;
        }
        foreach( Tip tip_carrier_well in tips){
            tip_carrier_well.SetState( TipCarrierWellState.InUse);
        }
        return true;
    }
}
// ----------------------------------------------------------------------
// REMOVED FROM TIPTRACKER AS A RESULT OF MERGER INTO TIPCARRIER
// ----------------------------------------------------------------------
public void ReserveTwoCompatibleTipsNew( double channel_spacing, out int well1_index, out int well2_index)
{
    // Debug.Assert( Tips.Count() >= 2);
    well1_index = Array.IndexOf< TipHomeLocationState>( Tips, TipHomeLocationState.Clean, 0);
    well2_index = well1_index + (((( well1_index / 6) % 2) == 0) ? 18 : 6);

    // DKM 2010-09-26 #191 I think the best way to implement the feature that Ben and Mark want is to make this
    //                method always try to return a tip that's two rows below the first.  So it should be something like:
    //well2_index = well1_index + (int)(Math.Round(channel_spacing + 0.5) / 18) * 24;
    // then in the TipsOnStateMachine, we need to use a window of acceptable distances.  In other words, the channels
    // won't be exactly 18mm apart, but the tips are.  So let's always try to use 0 degrees, and allow a window of up
    // to 0.5mm (for example) to make 0 degrees work.  If it's outside of this window, then pick an angle.  It's unknown
    // to me right now what will happen over time when things are outside of the window, and when non-adjacent channels
    // get picked (thus throwing off the desired tip spacing in a given column)

    // string well1_name = Wells.IndexToWellName( well1_index, 96);
    Well well1 = new Well( LabwareFormat.LF_STANDARD_96, well1_index);
    // string well2_name = Wells.IndexToWellName( well2_index, 96);
    Well well2 = new Well( LabwareFormat.LF_STANDARD_96, well2_index);
    if( well2_index < 96) {
        // if(( Tips[ well2_index] == TipState.Unused) && (Wells.GetDistanceBetweenWells( 96, well1_name, well2_name) >= channel_spacing)){
        if(( Tips[ well2_index] == TipHomeLocationState.Clean) && ( WellMathUtil.CalculateWellSpacing( LabwareFormat.LF_STANDARD_96, well1, well2) >= channel_spacing)){
            SetTipState( well1_index, TipHomeLocationState.InUse);
            SetTipState( well2_index, TipHomeLocationState.InUse);
            return;
        }
    }
    ReserveTwoCompatibleTips( channel_spacing, out well1_index, out well2_index);
}
// ----------------------------------------------------------------------
/// <summary>
/// calling this function will mark both tips as TipState.Reserved
/// </summary>
/// <param name="tip_spacing"></param>
/// <param name="well1_index"></param>
/// <param name="well2_index"></param>
/// <returns></returns>
private void ReserveTwoCompatibleTips( double tip_spacing, out int well1_index, out int well2_index)
{
    // Debug.Assert( Tips.Count() >= 2);
    // get the first available tip
    well1_index = Array.IndexOf< TipHomeLocationState>( Tips, TipHomeLocationState.Clean, 0);
    // now loop over the remaining available tips to see which ones are within tip_spacing distance
    well2_index = -1;
    int ctr = 1;
    while( true) {
        int temp = Array.IndexOf< TipHomeLocationState>( Tips, TipHomeLocationState.Clean, well1_index + ctr++);
        if( temp == -1)
            break;
        // check to see if the distance between tip1_index and temp is >= tip_spacing
        Well well1 = new Well( LabwareFormat.LF_STANDARD_96, well1_index);
        Well temp_well = new Well( LabwareFormat.LF_STANDARD_96, temp);

        // double well_spacing = Wells.GetDistanceBetweenWells( 96, well1_name, temp_name);
        double well_spacing = WellMathUtil.CalculateWellSpacing( LabwareFormat.LF_STANDARD_96, well1, temp_well);
        if( well_spacing >= tip_spacing) {
            well2_index = temp;
            break;
        }
    }
    // this is the case where we couldn't find an optimal two-tip solution.  Since the
    // tips on state machine can handle a case where the two tips supplied will press
    // on one tip, then the next, it's perfectly ok to return a pair of tips that aren't
    // accessible by the channels at the same time.
    if( well2_index == -1)
        well2_index = Array.IndexOf< TipHomeLocationState>( Tips, TipHomeLocationState.Clean, well1_index + 1); // grab the next available tip after tip #1
            
    SetTipState( well1_index, TipHomeLocationState.InUse);
    SetTipState( well2_index, TipHomeLocationState.InUse);
}
// ----------------------------------------------------------------------
public void TipBoxLoaded( int number_of_tips)
{
    Tips = new TipHomeLocationState[number_of_tips];
    _num_tips_in_box = number_of_tips;
    if( config_.TipsToUsePerTipbox != 0){
        // DKM 2010-10-11 this allows us to limit the usage of tips per tipbox, to allow
        //                us to test tipbox teachpoints more quickly
        // REED LOOK HERE FOR TIP BOX HACK
        for( int i = config_.TipsToUsePerTipbox; i < _num_tips_in_box; i++)
            Tips[ i] = TipHomeLocationState.Dirty;
    }
}
// ----------------------------------------------------------------------
public void TipBoxUnloaded()
{
    Tips = null;
    _num_tips_in_box = 0;
}
// ----------------------------------------------------------------------
// not needed -- replaced by TipCarrier IsAllDirty.
/// <summary>
/// Whether or not all of the tips have been used, i.e. TipState == Used.  We need
/// this to to determine whether or not to replace a tipbox
/// </summary>
public bool AreAllTipsUsedUp
{
    get {
        if( Tips == null)
            return false;
        return Tips.Count( x => x == TipHomeLocationState.Dirty) == _num_tips_in_box;
    }
}
// ----------------------------------------------------------------------
// not needed -- replaced by TipCarrier CountTipsOfState( TipHomeLocationState.Clean.
/// <summary>
/// how many tips remain in use in the system, either TipState == Unused
/// We need this to determine whether or not the tip service should process any transfers
/// </summary>
public int NumUnusedTips
{
    get {
        return ( Tips == null) ? 0 : Tips.Count( x => x == TipHomeLocationState.Clean);
    }
}
// ----------------------------------------------------------------------
// not used.
public void Reset()
{
    Index = 0;
}
/// <summary>
/// calling this function will mark the tip as TipState.Reserved
/// </summary>
/// <returns></returns>
public int ReserveOneTip()
{
    if( Tips == null) // tipbox got unloaded by plate service
        return -1;
    int available_index = Array.IndexOf<TipState>( Tips, TipState.Unused);
    if( available_index == -1) // tip got reserved just before we asked for one here
        return -1;
    SetTipState( available_index, TipState.Reserved);
    return available_index;
}
[Test]
public void TestTipsLeft()
{
    TipTracker tt = new TipTracker();
    tt.TipBoxLoaded( 96);
    Assert.IsTrue( tt.TipsLeft == 96);
    tt.TipPressedOn( "A1");
    Assert.IsTrue( tt.TipsLeft == 95);
}
[Test]
public void TestFindTwoCompatibleTips()
{
    TipTracker tt = new TipTracker();
    tt.TipBoxLoaded( 96);
    int well1_index, well2_index;
    Assert.IsTrue( FindTwoCompatibleTips( 18, out well1_index, out well2_index));
    Assert.AreEqual( 0, well1_index);
    Assert.AreEqual( 2, well2_index);
}
[Test]
public void TestTipBoxBarcodes()
{
    TipTracker tt = new TipTracker();
    tt.TipBoxLoaded( 96);
    Assert.AreEqual( "tipbox1", tt.CurrentBarcode);
    tt.TipBoxLoaded( 96);
    Assert.AreEqual( "tipbox2", tt.CurrentBarcode);
    tt.Reset();
    tt.TipBoxLoaded( 96);
    Assert.AreEqual( "tipbox1", tt.CurrentBarcode);
}
// ----------------------------------------------------------------------
// not needed in ServiceSharedMemory
public void SetTipState( TipWell tip_well, TipWellState state)
{
    lock_.EnterWriteLock();
    try {
        TipTracker.SetTipState( tip_well, state);
    } catch( Exception){
    } finally{
        lock_.ExitWriteLock();
    }
}
//-----------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------
// TIP TRACKER FROM SERVICE SHARED MEMORY.
//-----------------------------------------------------------------------------------------
//-----------------------------------------------------------------------------------------
public void LoadTipBox( int num_tips)
{
    lock_.EnterWriteLock();
    try {
        TipTracker.TipBoxLoaded( num_tips);
    } catch( Exception){
    } finally{
        lock_.ExitWriteLock();
    }
}
//-----------------------------------------------------------------------------------------
public void UnloadTipBox()
{
    lock_.EnterWriteLock();
    try {
        TipTracker.TipBoxUnloaded();
    } catch( Exception){
    } finally{
        lock_.ExitWriteLock();
    }
}
//-----------------------------------------------------------------------------------------
public int GetNumUnusedTips()
{
    lock_.EnterReadLock();
    try {
        return TipTracker.CountTipsOfState( TipWellState.Clean);
    } catch( Exception){
    } finally{
        lock_.ExitReadLock();
    }
    return 0;
}
//-----------------------------------------------------------------------------------------
public bool AreAllTipsUsedUp()
{
    lock_.EnterReadLock();
    try {
        return TipTracker.IsAllDirty();
    } catch( Exception){
    } finally{
        lock_.ExitReadLock();
    }
    return false;
}
//-----------------------------------------------------------------------------------------
public TipWell ReserveOneTip()
{
    // internally, TipTracker.GetFirstAvailableTip() changes data
    lock_.EnterWriteLock();
    try {
        return TipTracker.ReserveOneTip();
    } catch( Exception){
    } finally{
        lock_.ExitWriteLock();
    }
    return null;
}
//-----------------------------------------------------------------------------------------
public Tuple< TipWell, TipWell> ReserveTwoCompatibleTips( double tip_spacing)
{
    // internally, FindTwoCompatibleTips() changes data
    lock_.EnterWriteLock();
    try {
        return TipTracker.ReserveTwoCompatibleTips( tip_spacing);
    } catch( Exception){
    } finally{
        lock_.ExitWriteLock();
    }
    return null;
}
*/
//-----------------------------------------------------------------------------------------
