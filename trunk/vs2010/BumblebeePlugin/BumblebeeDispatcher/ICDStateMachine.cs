using System;
using System.Diagnostics;
using System.Threading;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.StateMachineExecutor;
using BioNex.Shared.Utils;

namespace BioNex.BumblebeePlugin.Dispatcher
{
    public abstract class ICDStateMachine< TState, TTrigger> : StateMachineWrapper2< TState, TTrigger>, IStateMachineExecutorStateMachine
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        protected ICDParameterBundle ParameterBundle { get; private set; }
        protected ICDEventBundle EventBundle { get; private set; }
        protected DispatcherJob Job { get; private set; }

        private ManualResetEvent MemberAbortEvent { get; set; }
        private bool FinishSignaled { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        protected ICDStateMachine(ICDParameterBundle parameter_bundle, ICDEventBundle event_bundle, DispatcherJob job)
            : base( parameter_bundle.HandleError, null, false)
        {
            ParameterBundle = parameter_bundle;
            EventBundle = event_bundle;
            Job = job;
            MemberAbortEvent = new ManualResetEvent( false);
            FinishSignaled = false;

            ParameterBundle.Messenger.Register< AbortCommand>( this, HandleAbort);
            ParameterBundle.Messenger.Register< PauseCommand>( this, HandlePause);
            ParameterBundle.Messenger.Register< ResumeCommand>( this, HandleResume);
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        protected bool WaitFor( WaitHandle handle)
        {
            switch( WaitHandle.WaitAny( new WaitHandle[]{ ParameterBundle.AbortEvent, MemberAbortEvent, handle})){
                case 0:
                case 1:
                    Fire( AbortTrigger);
                    return false;
                default:
                    return true;
            }
        }
        // ----------------------------------------------------------------------
        protected virtual void HandleAbort( AbortCommand command)
        {
            Abort();
        }
        // ----------------------------------------------------------------------
        protected virtual void HandlePause( PauseCommand command)
        {
            Pause();
        }
        // ----------------------------------------------------------------------
        protected virtual void HandleResume( ResumeCommand command)
        {
            Resume();
        }
        // ----------------------------------------------------------------------
        protected void AutoRetry( Action action, string action_description, int max_retries = 5)
        {
            int num_retries = 0;
            while( true){
                try{
                    action();
                    break;
                } catch( Exception ex){
                    if( num_retries++ < max_retries){
                        Log.DebugFormat( "Failed action '{0}', retry {1}/{2}", action_description, num_retries, max_retries);
                        continue;
                    }
                    throw ex;
                }
            }
        }
        // ----------------------------------------------------------------------
        // state functions.
        // ----------------------------------------------------------------------
        protected virtual void OnStartCritical()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            if( EventBundle != null && EventBundle.WaitBeforeStart != null){
                WaitFor( EventBundle.WaitBeforeStart);
                stopwatch.Stop();
                Log.DebugFormat( "{0}[{1}]: Waited {2} to start critical section of state machine", StateMachineType.Name, StateMachineNumber, stopwatch.Elapsed.ToString("G"));
            } else{
                Log.DebugFormat( "{0}[{1}]: No wait to start critical section of state machine", StateMachineType.Name, StateMachineNumber, stopwatch.Elapsed.ToString("G"));
            }
            Fire( SuccessTrigger);
        }
        // ----------------------------------------------------------------------
        protected virtual void OnFinishCritical()
        {
            SignalOnFinish();
            Stopwatch stopwatch = Stopwatch.StartNew();
            if( EventBundle != null && EventBundle.WaitBeforeFinish != null){
                WaitFor( EventBundle.WaitBeforeFinish);
                stopwatch.Stop();
                Log.DebugFormat( "{0}[{1}]: Waited {2} to finish critical section of state machine", StateMachineType.Name, StateMachineNumber, stopwatch.Elapsed.ToString("G"));
            } else{
                Log.DebugFormat( "{0}[{1}]: No wait to finish critical section of state machine", StateMachineType.Name, StateMachineNumber, stopwatch.Elapsed.ToString("G"));
            }
            Fire( SuccessTrigger);
        }
        // ----------------------------------------------------------------------
        protected override void AbortedStateFunction()
        {
            SignalOnFinish();
            base.AbortedStateFunction();
        }
        // ----------------------------------------------------------------------
        private void SignalOnFinish()
        {
            if( !FinishSignaled){
                if( EventBundle != null){
                    if( EventBundle.SetOnFinish != null){
                        EventBundle.SetOnFinish.Set();
                        Log.DebugFormat( "{0}[{1}]: Set event on finish", StateMachineType.Name, StateMachineNumber);
                    } else{
                        Log.DebugFormat( "{0}[{1}]: No event to set on finish", StateMachineType.Name, StateMachineNumber);
                    }
                    int countdown_events = EventBundle.CountdownsOnFinish.Count;
                    if( countdown_events > 0){
                        EventBundle.CountdownsOnFinish.ForEach( countdown => countdown.Signal());
                        Log.DebugFormat( "{0}[{1}]: Signaled {2} countdowns on finish", StateMachineType.Name, StateMachineNumber, countdown_events);
                    } else{
                        Log.DebugFormat( "{0}[{1}]: No countdowns to signal of finish", StateMachineType.Name, StateMachineNumber);
                    }
                } else{
                    Log.DebugFormat( "{0}[{1}]: No event or countdowns to set on finish", StateMachineType.Name, StateMachineNumber);
                }
                FinishSignaled = true;
            }
        }
        // ----------------------------------------------------------------------
        #region INSStateMachine Members
        // ----------------------------------------------------------------------
        public void Flush()
        {
            MemberAbortEvent.Set();
            Execute( AbortTrigger);
        }
        // ----------------------------------------------------------------------
        public bool IsRunning()
        {
            return SMStopwatch.IsRunning;
        }
        // ----------------------------------------------------------------------
        #endregion
    }
}
