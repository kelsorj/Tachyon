using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using BioNex.Shared.IError;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.Utils;

namespace BioNex.Hig.StateMachines
{
    public class HiGStateMachineCommon<TState> : StateMachineWrapper<TState, HiGStateMachineCommon<TState>.Trigger>
    {
        public string LastErrorMessage { get; internal set; }
        internal IHigModel Model { get; set; }

        public event EventHandler Error;

        public enum Trigger
        {
            Execute,
            Success,
            Fail,
            FailDoor,
            FailWaitForSpinDown, // used for imbalance or any other state where the spindle may have started to spin
            Invalid, // used for invalid imbalance mass detected during calibration
            CycleDoor,
            Continue,
            ContinueWait,
            Retry,
            Ignore,
            Abort,
            AbortAndProceed,
            HomeShieldOnly,
            LeaveShieldClosed,
            OvervoltageRecovery
        }

        internal HiGStateMachineCommon(IHigModel model, TState start, bool show_abort_label)
            : base(start, Trigger.Execute, Trigger.Retry, Trigger.Ignore, Trigger.Abort, show_abort_label)
        {
            Model = model;
            Log = log4net.LogManager.GetLogger( model.Name);
        }

        internal virtual void SetAngle()
        {
            Log.DebugFormat( "{0} Entered SetAngle()", Model.Name);
            try
            {
                var spindle = Model.SpindleAxis;
                //spindle.SetIntVariable("angle_to_goto", 0); // Always First bucket after home or spin
                spindle.SetIntVariable("bucket_to_goto", 1); // Always First bucket after home or spin
                Fire(Trigger.Success);
            }
            catch (Exception e)
            {
                LastErrorMessage = e.Message;
                Log.Error( LastErrorMessage);
                Fire(Trigger.Fail);
            }
        }

        internal void GotoAngle()
        {
            Log.DebugFormat( "{0} Entered GotoAngle()", Model.Name);
            try
            {
                var spindle = Model.SpindleAxis;
                // DKM 2012-01-30 add auto-retry for goto_bucket in the case where we are still moving (for -3 error code only)
                bool use_auto_retry = true;
                int max_auto_retries = 5;
                int num_auto_retries = 0;
                while( use_auto_retry && num_auto_retries < max_auto_retries) {
                    use_auto_retry = false;
                    int func_done = spindle.CallFunctionAndWaitForDone("goto_bucket", TimeSpan.FromSeconds(5.0), return_func_done:true);
                    switch (func_done)
                    {
                        case 1: // Success! What we expected
                            Fire(Trigger.Success);
                            break;
                        case 0: // Timeout, but this case is handled in CallFunctionAndWaitForDone
                            LastErrorMessage = "Timed out when moving to bucket.  Please try again";
                            Log.Error( LastErrorMessage);
                            Log.Info(String.Format("{0} spindle controller timed out in GotoAngle function", Model.Name));
                            Fire(Trigger.Fail);
                            break;
                        case -1: // not homed
                            LastErrorMessage = "Please rehome HiG and try again";
                            Log.Error( LastErrorMessage);
                            Log.Info(String.Format("{0} spindle controller reports not home in GotoAngle function", Model.Name));
                            Fire(Trigger.Fail);
                            break;
                        case -2: // not enabled
                            LastErrorMessage = "Please reset safety interlock to enable Spindle and try again";
                            Log.Error( LastErrorMessage);
                            Log.Info(String.Format("{0} spindle controller reports not Enabled in GotoAngle function", Model.Name));
                            Fire(Trigger.Fail);
                            break;
                        case -3: // Not stopped
                            use_auto_retry = true;
                            if( num_auto_retries++ < max_auto_retries)
                                continue;

                            LastErrorMessage = "Please let HiG rotor come to a stop and try again";
                            Log.Error( LastErrorMessage);
                            Log.Info(String.Format("{0} spindle controller reports still spinning in GotoAngle function", Model.Name));
                            Fire(Trigger.Fail);
                            break;
                        case -4: // Door Not closed
                            LastErrorMessage = "Please close HiG door and try again";
                            Log.Error( LastErrorMessage);
                            Log.Info(String.Format("{0} spindle controller reports door not closed in GotoAngle function", Model.Name));
                            Fire(Trigger.Fail);
                            break;
                        default:
                            LastErrorMessage = "Unknown error during goto_bucket call";
                            Log.Error( LastErrorMessage);
                            Log.Info(String.Format("{0} spindle controller reports func_done=={1} in GotoAngle function", Model.Name, func_done));
                            Fire(Trigger.Fail);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                LastErrorMessage = e.Message;
                Log.Error( LastErrorMessage);
                Fire(Trigger.Fail);
            }
        }

        internal void OpenShield()
        {
            // DKM 2012-04-17 if in NoShieldMode, just exit
            if( Model.NoShieldMode) {
                Log.Debug( "HiG is in 'no shield' mode, leaving OpenShield()");
                Fire( Trigger.Success);
                return;
            }

            Log.DebugFormat( "{0} Entered OpenShield()", Model.Name);
            var shield = Model.ShieldAxis;
            try
            {
                shield.CallFunctionAndWaitForDone("func_open_shield", TimeSpan.FromSeconds(15.0));
                Fire(Trigger.Success);
            }
            catch (Exception ex)
            {
                LastErrorMessage = ex.Message;
                Log.Error( LastErrorMessage);
                Fire(Trigger.Fail);
            }
            finally
            {
                try
                {
                    short pos_err;
                    short iq;
                    short iqref;
                    shield.GetIntVariable("POSITION_ERROR", out pos_err);
                    shield.GetIntVariable("IQ", out iq);
                    shield.GetIntVariable("IQREF", out iqref);
                    const double imaxps = 16.5;
                    const double kif = 65472 / (2 * imaxps);
                    Log.InfoFormat("{0} 1st check position error: {1}, iq: {2:0.00}, iqref: {3:0.00}", Model.Name, pos_err, iq / kif, iqref / kif);
                    Thread.Sleep(2000);
                    shield.GetIntVariable("POSITION_ERROR", out pos_err);
                    shield.GetIntVariable("IQ", out iq);
                    shield.GetIntVariable("IQREF", out iqref);
                    Log.InfoFormat("{0} 2nd check position error: {1}, iq: {2:0.00}, iqref: {3:0.00}", Model.Name, pos_err, iq / kif, iqref / kif);
                }
                catch (Exception)
                {
                    Log.InfoFormat("{0} could not log position error, iq, and iqref after opening the door", Model.Name);
                }
            }
        }

        internal void CloseShield()
        {
            // DKM 2012-04-17 if in NoShieldMode, just exit
            if( Model.NoShieldMode) {
                Log.Debug( "HiG is in 'no shield' mode, leaving CloseShield()");
                Fire( Trigger.Success);
                return;
            }

            try
            {
                var shield = Model.ShieldAxis;
                shield.CallFunctionAndWaitForDone("func_close_shield", TimeSpan.FromSeconds(15.0));
                // DKM 2011-10-21 added extra delay for now for imbalance test when the lifter isn't installed
                Log.DebugFormat( "Shield is closed and currently at {0} counts", shield.GetPositionCounts());
                Fire(Trigger.Success);
            }
            catch (Exception e)
            {
                LastErrorMessage = e.Message;
                Log.Error( LastErrorMessage);
                Fire(Trigger.Fail);
            }
        }

#if !HIG_INTEGRATION
        private delegate void AddErrorDelegate( ErrorData error);
        
        private void AddErrorSTA( ErrorData error)
        {
            if( Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "Add HiG GUI error";
            BioNex.Shared.ErrorHandling.ErrorDialog dlg = new BioNex.Shared.ErrorHandling.ErrorDialog( error);
            // #179 need to use modeless dialogs, because modal ones prevent us from being able to reset the interlocks via software
            dlg.Show();
        }
#endif

        internal void HandleErrorWithRetryOnly(string message)
        {
            // DKM 2011-10-03 fire Error event so anyone that's interested (i.e. ExecutionState) can modify their state
            if( Error != null)
                Error( this, null);

            // if we're pausing, we would get an error and the Paused flag will be set
            SMPauseEvent.WaitOne();

            try
            {
                List< string> error_strings = new List< string>{ RETRY_LABEL};
                if( _show_abort_label){
                    error_strings.Add( ABORT_LABEL);
                }
                var error = new ErrorData(message, error_strings);
                SMStopwatch.Stop();

// DKM 2011-10-24 to remove unnecessary dependencies, I had to use #if so that references to PlateDefs, etc etc aren't needed for integrations
#if !HIG_INTEGRATION
                if( CalledFromDiags && Model.MainDispatcher != null) {
                    // error goes to separate ErrorDialog instance
                    Model.MainDispatcher.BeginInvoke( new AddErrorDelegate( AddErrorSTA), error);
                } else {
#endif
                    // error should be routed through Synapsis
                    Model.AddError(error);
#if !HIG_INTEGRATION
                }
#endif
                List< ManualResetEvent> events = new List< ManualResetEvent>{ _main_gui_abort_event};
                events.AddRange( error.EventArray);
                int event_index = WaitHandle.WaitAny( events.ToArray());
                SMStopwatch.Start();
                if (error.TriggeredEvent == RETRY_LABEL){
                    Fire(Trigger.Retry);
                } else if(( error.TriggeredEvent == ABORT_LABEL) || ( event_index == 0)){
                    Fire(Trigger.Abort);
                }
            }
            catch (Exception ex)
            {
                LastErrorMessage = ex.Message;
                Log.Error( LastErrorMessage);
                Debug.Assert(false, ex.Message);
            }
        }
    }
}
