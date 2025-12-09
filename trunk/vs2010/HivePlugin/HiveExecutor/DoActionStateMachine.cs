using System;

namespace BioNex.Hive.Executor
{
    internal class DoActionStateMachine : HiveStateMachine< DoActionStateMachine.State, DoActionStateMachine.Trigger>
    {
        // ----------------------------------------------------------------------
        // constants.
        // ----------------------------------------------------------------------
        internal enum State
        {
            Start,
            DoAction, DoActionError,
            End, Abort,
        }
        // ----------------------------------------------------------------------
        internal enum Trigger
        {
            Success,
            Failure,
            Retry,
            Ignore,
            Abort,
        }

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        private Action Action { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        internal DoActionStateMachine( HiveExecutor executor, Action action)
            : base( executor, null, typeof( DoActionStateMachine), State.Start, State.End, State.Abort, Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, executor.HandleError)
        {
            Action = action;
            InitializeStates();
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        private void InitializeStates()
        {
            ConfigureState( State.Start, NullStateFunction, State.DoAction);
            ConfigureState( State.DoAction, DoAction, State.End, State.DoActionError);
            ConfigureState( State.End, EndStateFunction);
            ConfigureState( State.Abort, AbortedStateFunction);
        }
        // ----------------------------------------------------------------------
        private void DoAction()
        {
            try{
                Action();
                Fire( Trigger.Success);
            } catch( Exception ex){
                LastError = ex.Message;
                Fire( Trigger.Failure);
            }
        }
    }
}
