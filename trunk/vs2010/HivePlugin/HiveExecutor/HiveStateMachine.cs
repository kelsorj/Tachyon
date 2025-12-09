using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using BioNex.Shared.IError;
using BioNex.Shared.StateMachineExecutor;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.Utils;

namespace BioNex.Hive.Executor
{
    public abstract class HiveStateMachine< TState, TTrigger> : StateMachineWrapper2< TState, TTrigger>, IStateMachineExecutorStateMachine
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        protected HiveExecutor Executor { get; set; }
        protected ManualResetEvent EndedAbortedEvent { get; set; }
        protected IAxis XAxis { get; set; }
        protected IAxis ZAxis { get; set; }
        protected IAxis TAxis { get; set; }
        protected IAxis GAxis { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        protected HiveStateMachine( HiveExecutor executor, ManualResetEvent ended_aborted_event, Type type, TState start_state, TState end_state, TState abort_state, TTrigger success_trigger, TTrigger failure_trigger, TTrigger retry_trigger, TTrigger ignore_trigger, TTrigger abort_trigger, ErrorEventHandler handle_error)
            : base(handle_error, null/* error_interface no longer used by HiveStateMachines */, start_state, end_state, abort_state, success_trigger, failure_trigger, retry_trigger, ignore_trigger, abort_trigger, false/* show_abort_label no longer matters to HiveStateMachines */)
        {
            Executor = executor;
            EndedAbortedEvent = ended_aborted_event;
            XAxis = Executor.Hardware.XAxis;
            ZAxis = Executor.Hardware.ZAxis;
            TAxis = Executor.Hardware.TAxis;
            GAxis = Executor.Hardware.GAxis;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public override void Start()
        {
            base.Start();
        }
        // ----------------------------------------------------------------------
        protected override void EndStateFunction()
        {
            if( EndedAbortedEvent != null){
                EndedAbortedEvent.Set();
            }
            base.EndStateFunction();
        }
        // ----------------------------------------------------------------------
        protected override void  AbortedStateFunction()
        {
            if( EndedAbortedEvent != null){
                EndedAbortedEvent.Set();
            }
            base.AbortedStateFunction();
        }
        // ----------------------------------------------------------------------
        public void Flush()
        {
            Execute( AbortTrigger);
        }
        // ----------------------------------------------------------------------
        public bool IsRunning()
        {
            return SMStopwatch.IsRunning;
        }
    }
}
