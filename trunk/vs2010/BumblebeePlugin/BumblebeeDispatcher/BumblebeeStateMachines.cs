using System;

namespace BioNex.BumblebeePlugin.Dispatcher
{
    internal class DoActionStateMachine : ICDStateMachine< DoActionStateMachine.State, DoActionStateMachine.Trigger>
    {
        // ----------------------------------------------------------------------
        // constants.
        // ----------------------------------------------------------------------
        internal enum State
        {
            Start,
            OnStartCritical,
            DoAction, DoActionError,
            OnFinishCritical,
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
        // private DoActionJob Job { get; set; }
        private Action Action { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        internal DoActionStateMachine(ICDParameterBundle parameter_bundle, ICDEventBundle event_bundle, Action action)
            : base(parameter_bundle, event_bundle, null)
        {
            // Job = job;
            Action = action;
            InitializeStates();
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        private void InitializeStates()
        {
            ConfigureState( State.Start, NullStateFunction, State.OnStartCritical);
            ConfigureState( State.OnStartCritical, OnStartCritical, State.DoAction);
            ConfigureState( State.DoAction, DoAction, State.OnFinishCritical, State.DoActionError);
            ConfigureState( State.OnFinishCritical, OnFinishCritical, State.End);
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
