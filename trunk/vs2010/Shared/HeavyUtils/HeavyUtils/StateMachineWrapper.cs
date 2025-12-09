using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using BioNex.Shared.IError;
using log4net;
using Stateless;

namespace BioNex.Shared.Utils
{
    public abstract class StateMachineWrapper<TState,TTrigger>
    {
        public List<TState> SkipLeavingStateLogging { get; private set; }
        // constants.
        protected static readonly string ABORT_LABEL = "Abort";
        protected static readonly string RETRY_LABEL = "Retry";
        protected static readonly string UNEXPECTED_EVENT_STRING = "Unexpected triggered event for error";

        // properties and members.
        protected Type StateMachineType { get; private set; }
        protected StateMachine< TState, TTrigger> SM { get; private set; }
        protected TTrigger SuccessTrigger { get; private set; }
        protected TTrigger RetryTrigger { get; private set; }
        protected TTrigger IgnoreTrigger { get; private set; }
        protected TTrigger AbortTrigger { get; private set; }

        // DKM 2012-04-05 removed static keyword because this was preventing me from allowing devices like the HiG from
        //                writing their state machine information to different log files
        protected ILog Log { get; set; }
        protected Stopwatch SMStopwatch { get; private set; }
        protected ManualResetEvent SMPauseEvent { get; private set; }

        private static int state_machine_counter_ = 0;
        protected int StateMachineNumber;

        private TTrigger LastTrigger { get; set; }
        private bool StopPumpingStateMachine { get; set; }

        protected bool _show_abort_label { get; set; }
        // DKM 2011-10-19 this is potentially confusing, but CalledFromDiags is the
        //                same as _show_abort_label.  If we are calling the SM from
        //                diagnostics, then we want to have an Abort option in any
        //                error dialog that pops up.
        public bool CalledFromDiags { get { return _show_abort_label; } }

        /// <summary>
        /// this is used to handle the main gui Abort button click -- we are no longer going to have an Abort button in the error handlers.
        /// </summary>
        protected ManualResetEvent _main_gui_abort_event { get; set; }

        // constructors.
        /// <summary>
        /// This constructor was added for cases where we want to have a statemachine dictate the behavior of a system, and
        /// therefore it is always running, and does not need to listen to abort/retry/etc.  It is up to the things that
        /// it controls to listen to such events.
        /// </summary>
        /// <remarks>
        /// DKM 2011-05-04 this constructor wasn't working correctly for the teachpoint cycler, because the
        /// triggers weren't set at all, so they were all being assigned the default value, which is the
        /// first one in the enum.  This was preventing the state machine from exiting properly.  I have now
        /// slightly modified this constructor to take the bare minimum triggers, which include Success and Abort.
        /// </remarks>
        /// 
        /// <param name="start_state"></param>
        /// <param name="start_or_success_trigger"></param>
        /// <param name="abort_trigger"></param>
        /// <param name="show_abort_label"></param>
        protected StateMachineWrapper( TState start_state, TTrigger start_or_success_trigger, TTrigger abort_trigger, bool show_abort_label)
        {
            SkipLeavingStateLogging = new List<TState>();

            LastTrigger = start_or_success_trigger; // bug fix -- was initializing to first state in state machine
            SuccessTrigger = start_or_success_trigger;
            AbortTrigger = abort_trigger;

            StateMachineType = this.GetType();
            SM = new StateMachine< TState, TTrigger>( start_state);
            _show_abort_label = show_abort_label;
            _main_gui_abort_event = new ManualResetEvent( false);

            Log = LogManager.GetLogger( StateMachineType.Name);
            SMStopwatch = new Stopwatch();

            StateMachineNumber = Interlocked.Increment( ref state_machine_counter_);
            Log.DebugFormat( "{0}[{1}]: Creating", StateMachineType.Name, StateMachineNumber);
            SMPauseEvent = new ManualResetEvent( true);
        }

        protected StateMachineWrapper( TState start_state, TTrigger start_trigger/* success_trigger */, TTrigger retry_trigger, TTrigger ignore_trigger, TTrigger abort_trigger, bool show_abort_label)
            : this( start_state, start_trigger, abort_trigger, show_abort_label)
        {
            RetryTrigger = retry_trigger;
            IgnoreTrigger = ignore_trigger;
        }

        protected StateMachineWrapper( bool show_abort_label)
        {
            TState start_state = ( TState)( Enum.Parse( typeof( TState), "Start"));
            TTrigger start_or_success_trigger = ( TTrigger)( Enum.Parse( typeof( TTrigger), "Success"));
            TTrigger retry_trigger = ( TTrigger)( Enum.Parse( typeof( TTrigger), "Retry"));
            TTrigger ignore_trigger = ( TTrigger)( Enum.Parse( typeof( TTrigger), "Ignore"));
            TTrigger abort_trigger = ( TTrigger)( Enum.Parse( typeof( TTrigger), "Abort"));

            SkipLeavingStateLogging = new List<TState>();

            LastTrigger = start_or_success_trigger; // bug fix -- was initializing to first state in state machine
            SuccessTrigger = start_or_success_trigger;
            RetryTrigger = retry_trigger;
            IgnoreTrigger = ignore_trigger;
            AbortTrigger = abort_trigger;

            StateMachineType = this.GetType();
            SM = new StateMachine< TState, TTrigger>( start_state);
            _show_abort_label = show_abort_label;
            _main_gui_abort_event = new ManualResetEvent( false);

            Log = LogManager.GetLogger( StateMachineType.Name);
            SMStopwatch = new Stopwatch();

            StateMachineNumber = Interlocked.Increment( ref state_machine_counter_);
            Log.DebugFormat( "{0}[{1}]: Creating", StateMachineType.Name, StateMachineNumber);
            SMPauseEvent = new ManualResetEvent( true);
        }

        // methods.
        public virtual void Start()
        {
            if( Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = String.Format( "State machine [{0}] starting thread", StateMachineType.Name);
            Log.DebugFormat( "{0}[{1}]: Starting", StateMachineType.Name, StateMachineNumber);
            SMStopwatch.Restart();
            Execute( SuccessTrigger);
        }

        protected virtual void ClientAbort() {}
        protected virtual void ClientPause() {}
        protected virtual void ClientResume() {}

        protected void Abort()
        {
            _main_gui_abort_event.Set();
            ClientAbort();
            Fire( AbortTrigger);
        }

        protected void Pause()
        {
            ClientPause();
            SMPauseEvent.Reset();
            // SMStopwatch.Stop(); this won't work (see resume below).
        }

        protected void Resume()
        {
            ClientResume();
            SMPauseEvent.Set();
            // SMStopwatch.Start(); this won't work because it will start the stopwatch on state machines that haven't even started.
        }

        protected void Fire( TTrigger trigger)
        {
            // DKM 2010-09-29 the problem I forsee is that having the Abort handler in the state machine
            //                won't work on its own because Fire() just sets LastTrigger.  So if we call
            //                Fire( Trigger.Abort) during execution of a state, at the end of that state
            //                LastTrigger will simply get overridden and it won't abort.  So now state
            //                machines need to tell the wrapper what their Abort trigger is, so we can
            //                prevent it from being overridden.
            if( !LastTrigger.Equals( AbortTrigger)){
                LastTrigger = trigger;
            }
        }

        protected void Execute( TTrigger initial_trigger)
        {
            if( !LastTrigger.Equals( AbortTrigger)){
                LastTrigger = initial_trigger;
            }
            while( !StopPumpingStateMachine){
                // DKM 2012-02-13 allow developer to decide whether or not to log this transition -- some states may re-enter at a very high frequency
                if( !SkipLeavingStateLogging.Contains( SM.State)) {
                    Log.DebugFormat( "{0}[{1}]: Leaving state {2} via trigger {3}, elapsed = {4}", StateMachineType.Name, StateMachineNumber, SM.State.ToString(), LastTrigger.ToString(), SMStopwatch.Elapsed.ToString("G"));
                }
                SM.Fire( LastTrigger);
                Thread.Sleep( 0);
            }
        }

        protected void FakeOperation( int lower_time_bound_ms, int upper_time_bound_ms)
        {
            Random r = new Random();
            int var_time_ms = r.Next( upper_time_bound_ms - lower_time_bound_ms);
            Thread.Sleep( lower_time_bound_ms + var_time_ms);
        }

        // state functions.
        protected void NullStateFunction()
        {
            Fire( SuccessTrigger);
        }

        protected virtual void EndStateFunction()
        {
            StopPumpingStateMachine = true;
            SMStopwatch.Stop();
            Log.DebugFormat( "{0}[{1}]: Ending normally, elapsed = {2}", StateMachineType.Name, StateMachineNumber, SMStopwatch.Elapsed.ToString("G"));
        }

        protected virtual void AbortedStateFunction()
        {
            StopPumpingStateMachine = true;
            SMStopwatch.Stop();
            Log.DebugFormat( "{0}[{1}]: Aborting, elapsed = {2}", StateMachineType.Name, StateMachineNumber, SMStopwatch.Elapsed.ToString("G"));
        }
    }

    public class StateMachineWrapper2< TState, TTrigger> : StateMachineWrapper< TState, TTrigger>
    {
        // ----------------------------------------------------------------------
        // events.
        // ----------------------------------------------------------------------
        private event ErrorEventHandler HandleError;

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        protected TState EndState { get; private set; }
        protected TState AbortState { get; private set; }
        protected TTrigger FailureTrigger { get; private set; }
        protected IError.IError ErrorInterface { get; private set; }
        protected String LastError { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        /// <summary>
        /// Intent is to make state machine framework easier to configure and use.  Recently modified constructor to specialize error handling
        /// for a given situation.  If you want to provide your own handler, specify handle_error and set error_interface to null.  Otherwise,
        /// set handle_error to null and specify the error_interface
        /// </summary>
        /// <param name="handle_error">null if you want to use built-in error handling, otherwise specify handler</param>
        /// <param name="error_interface">null if you want to specify handler, otherwise specify object to handle error reporting</param>
        /// <param name="start_state"></param>
        /// <param name="end_state"></param>
        /// <param name="abort_state"></param>
        /// <param name="success_trigger"></param>
        /// <param name="failure_trigger"></param>
        /// <param name="retry_trigger"></param>
        /// <param name="ignore_trigger"></param>
        /// <param name="abort_trigger"></param>
        /// <param name="show_abort_label"></param>
        protected StateMachineWrapper2(ErrorEventHandler handle_error, IError.IError error_interface, TState start_state, TState end_state, TState abort_state, TTrigger success_trigger, TTrigger failure_trigger, TTrigger retry_trigger, TTrigger ignore_trigger, TTrigger abort_trigger, bool show_abort_label)
            : base( start_state, success_trigger, retry_trigger, ignore_trigger, abort_trigger, show_abort_label)
        {
            EndState = end_state;
            AbortState = abort_state;
            FailureTrigger = failure_trigger;
            ErrorInterface = error_interface;
            LastError = "";
            HandleError += handle_error;
        }
        // ----------------------------------------------------------------------
        protected StateMachineWrapper2(ErrorEventHandler handle_error, IError.IError error_interface, bool show_abort_label)
            : base( show_abort_label)
        {
            EndState = ( TState)( Enum.Parse( typeof( TState), "End"));
            AbortState = ( TState)( Enum.Parse( typeof( TState), "Abort"));
            FailureTrigger = ( TTrigger)( Enum.Parse( typeof( TTrigger), "Failure"));
            ErrorInterface = error_interface;
            LastError = "";
            HandleError += handle_error;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        protected virtual StateMachine< TState, TTrigger>.StateConfiguration ConfigureState( TState state, Action action)
        {
            return SM.Configure( state)
                .Ignore( AbortTrigger)
                .OnEntry( action);
        }
        // ----------------------------------------------------------------------
        protected virtual StateMachine< TState, TTrigger>.StateConfiguration ConfigureState( TState state, Action action, TState next_state)
        {
            return SM.Configure( state)
                .Permit( SuccessTrigger, next_state)
                .Permit( AbortTrigger, AbortState)
                .OnEntry( action);
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Reduces typing needed to configure a state.  But beware of the params:
        /// </summary>
        /// <param name="state"></param>
        /// <param name="action"></param>
        /// <param name="next_state"></param>
        /// <param name="error_state"></param>
        /// <param name="configure_ignore">If you do not want to automatically use HandlErrorWithRetryOnly, then set this to true!</param>
        /// <returns></returns>
        protected virtual StateMachine< TState, TTrigger>.StateConfiguration ConfigureState( TState state, Action action, TState next_state, TState error_state, bool configure_ignore = false)
        {
            StateMachine< TState, TTrigger>.StateConfiguration state_config = SM.Configure( state)
                .Permit( SuccessTrigger, next_state)
                .Permit( FailureTrigger, error_state)
                .Permit( AbortTrigger, AbortState)
                .OnEntry( action);
            StateMachine< TState, TTrigger>.StateConfiguration error_state_config = SM.Configure( error_state)
                .Permit( RetryTrigger, state)
                .Permit( AbortTrigger, AbortState);
            if( !configure_ignore){
                error_state_config.OnEntry( f => HandleErrorWithRetryOnly());
            } else{
                error_state_config.Permit( IgnoreTrigger, next_state);
                error_state_config.OnEntry( f => HandleErrorWithRetryAndIgnore());
            }
            return state_config;
        }
        // ----------------------------------------------------------------------
        protected void StandardRetry( Exception ex, String error_reason = null)
        {
            if(( error_reason == null) || ( error_reason.Trim().Length == 0)){
                LastError = ex.Message;
            } else{
                LastError = String.Format( "{0}: {1}", error_reason, ex.Message);
            }
            Fire( FailureTrigger);
        }
        // ----------------------------------------------------------------------
        protected virtual void HandleErrorWithRetryOnly( string retry_label = "Try move again")
        {
            IDictionary< string, TTrigger> label_to_trigger = new Dictionary< string, TTrigger>();
            label_to_trigger[ retry_label] = RetryTrigger;
            HandleLabels( label_to_trigger);
        }
        // ----------------------------------------------------------------------
        protected void HandleErrorWithRetryAndIgnore( string retry_label = "Try move again", string ignore_label = "Skip to next move")
        {
            IDictionary< string, TTrigger> label_to_trigger = new Dictionary< string, TTrigger>();
            label_to_trigger[ retry_label] = RetryTrigger;
            label_to_trigger[ ignore_label] = IgnoreTrigger;
            HandleLabels( label_to_trigger);
        }
        // ----------------------------------------------------------------------
        protected void HandleLabels( IDictionary< string, TTrigger> label_to_trigger, string details = "")
        {
            try{
                if( _show_abort_label){
                    label_to_trigger[ ABORT_LABEL] = AbortTrigger;
                }
                ErrorData error_data = new ErrorData( LastError, label_to_trigger.Keys, details);
                SMStopwatch.Stop();
                if( ErrorInterface != null){
                    ErrorInterface.AddError( error_data);
                } else{
                    PostError( error_data);
                }
                List< ManualResetEvent> events = new List< ManualResetEvent>{ _main_gui_abort_event};
                events.AddRange( error_data.EventArray);
                int event_index = WaitHandle.WaitAny( events.ToArray());
                SMStopwatch.Start();
                if(( event_index == 0) || ( error_data.TriggeredEvent == ABORT_LABEL)){
                    Fire( AbortTrigger);
                } else{
                    Debug.Assert( label_to_trigger.ContainsKey( error_data.TriggeredEvent), UNEXPECTED_EVENT_STRING);
                    Fire( label_to_trigger[ error_data.TriggeredEvent]);
                }
            } catch( Exception ex){
                // unexpected exception.
                LastError = ex.Message;
                Debug.Assert( false, ex.Message);
            }
        }
        // ----------------------------------------------------------------------
        private void PostError( ErrorData error_data)
        {
            if( HandleError != null){
                HandleError( this, error_data);
            }
        }
    }
}
