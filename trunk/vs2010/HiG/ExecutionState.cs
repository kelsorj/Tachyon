using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Hig
{
    // this is basically the same as the one in the HiGIntegration assembly, except that the state transitions
    // are a little different.  Could have done something like what I did with the operational state machines,
    // but figured that would be overkill for something that is intended to just track state and not
    // actually make the device do something useful.
    public class ExecutionState : Stateless.StateMachine<ExecutionState.State, ExecutionState.Trigger>
    {
        new public enum State
        {
            Idle,
            Homing,
            Spinning,
            OpeningToBucket,
            ClosingShield,
            InError,
            InErrorSubstate
        }

        public enum Trigger
        {
            Home,
            Spin,
            Error,
            OpenToBucket,
            CloseShield,
            Done
        }

        public ExecutionState()
            : base(State.Idle)
        {
            Configure( State.Idle)
                .Permit( Trigger.Home, State.Homing)
                .Permit( Trigger.Spin, State.Spinning)
                .Permit( Trigger.OpenToBucket, State.OpeningToBucket)
                .Permit( Trigger.CloseShield, State.ClosingShield)
                .Permit( Trigger.Error, State.InError);
            Configure( State.Homing)
                .Permit( Trigger.Done, State.Idle)
                .Permit( Trigger.Error, State.InError);
            Configure( State.Spinning)
                .Permit( Trigger.Done, State.Idle)
                .Permit( Trigger.Error, State.InError);
            Configure( State.OpeningToBucket)
                .Permit( Trigger.Done, State.Idle)
                .Permit( Trigger.Error, State.InError);
            Configure( State.ClosingShield)
                .Permit( Trigger.Done, State.Idle)
                .Permit( Trigger.Error, State.InError);
            Configure( State.InError)
                .Permit( Trigger.Done, State.Idle)
                .Permit( Trigger.Home, State.InErrorSubstate)
                .Permit( Trigger.CloseShield, State.InErrorSubstate)
                .Ignore( Trigger.Error)
                .Permit( Trigger.OpenToBucket, State.InErrorSubstate);
            Configure( State.InErrorSubstate) // used for homing and other operations while in the error state
                .SubstateOf( State.InError)
                .PermitReentry( Trigger.Error)
                .Permit( Trigger.Done, State.InError);
        }

        public void SetHoming()
        {
            Fire( Trigger.Home);
        }
        
        public void SetSpinning()
        {
            Fire( Trigger.Spin);
        }
        
        public void SetOpeningToBucket()
        {
            Fire( Trigger.OpenToBucket);
        }

        public void SetCloseShield()
        {
            Fire( Trigger.CloseShield);
        }

        public void SetError()
        {
            Fire( Trigger.Error);
        }

        public void SetDone()
        {
            Fire( Trigger.Done);
        }

        public bool Idle { get { return IsInState( State.Idle); } }
        public bool Homing { get { return IsInState( State.Homing); } }
        public bool Spinning { get { return IsInState( State.Spinning); } }
        public bool OpeningToBucket { get { return IsInState( State.OpeningToBucket); } }
        public bool ClosingShield { get { return IsInState( State.ClosingShield); } }
        public bool InErrorState { get { return IsInState( State.InError); } }

        /// <summary>
        /// used for reporting current state via diagnostics if the HiG is apparently busy
        /// </summary>
        public ExecutionState.State CurrentState
        {
            get
            {
                if (Idle) return State.Idle;
                else if (Homing) return State.Homing;
                else if (Spinning) return State.Spinning;
                else if (OpeningToBucket) return State.OpeningToBucket;
                else if (ClosingShield) return State.ClosingShield;
                else return State.InError;
            }
        }
    }
}
