using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.HiGIntegration
{
    public class ExecutionState : Stateless.StateMachine<ExecutionState.State, ExecutionState.Trigger>
    {
        new public enum State
        {
            Idle,
            Initializing,
            Homing,
            Spinning,
            OpeningToBucket,
            ClosingShield,
            InError
        }

        public enum Trigger
        {
            Home,
            Initialize,
            Spin,
            Error,
            OpenToBucket,
            CloseShield,
            Done
        }

        public ExecutionState.State CurrentState { get { return base.State; } }

        public ExecutionState()
            : base(State.Idle)
        {
            Configure( State.Idle)
                .Permit( Trigger.Initialize, State.Initializing)
                .Permit( Trigger.Home, State.Homing)
                .Permit( Trigger.Spin, State.Spinning)
                .Permit( Trigger.OpenToBucket, State.OpeningToBucket)
                .Permit( Trigger.CloseShield, State.ClosingShield)
                .Permit( Trigger.Error, State.InError);
            Configure( State.Initializing)
                .Permit( Trigger.Done, State.Idle)
                .Permit( Trigger.Error, State.InError);
            Configure(State.Homing)
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
                .Permit( Trigger.Home, State.Homing)
                .Permit( Trigger.OpenToBucket, State.OpeningToBucket)
                .Permit( Trigger.CloseShield, State.ClosingShield)
                .Permit( Trigger.Spin, State.Spinning);
        }

        public void SetInitializing()
        {
            Fire( Trigger.Initialize);
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
        public bool Initializing { get { return IsInState( State.Initializing); } }
        public bool Homing { get { return IsInState( State.Homing); } }
        public bool Spinning { get { return IsInState( State.Spinning); } }
        public bool OpeningToBucket { get { return IsInState( State.OpeningToBucket); } }
        public bool ClosingShield { get { return IsInState( State.ClosingShield); } }
        public bool InErrorState { get { return IsInState( State.InError); } }
    }
}
