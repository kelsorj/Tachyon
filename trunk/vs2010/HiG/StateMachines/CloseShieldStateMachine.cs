using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.TechnosoftLibrary;

namespace BioNex.Hig.StateMachines
{
    public class CloseShieldStateMachine : HiGStateMachineCommon<CloseShieldStateMachine.State>
    {
        public enum State
        {
            Idle,
            TestAngle,
            TestAngleError,
            CloseShield,
            CloseShieldError,
            SetAngle,
            SetAngleError,
            GotoAngle,
            GotoAngleError,
            OpenShield,
            OpenShieldError,
            Done,
            FailedDone
        }

        public CloseShieldStateMachine(IHigModel model, bool show_abort_label)
            : base(model, State.Idle, show_abort_label)
        {
            InitializeStates();
        }

        protected virtual void InitializeStates()
        {
            SM.Configure(State.Idle)
                .Permit(Trigger.Execute, State.CloseShield);
            SM.Configure(State.CloseShield)
                .Permit(Trigger.Success, State.Done)
                .Permit(Trigger.Fail, State.CloseShieldError)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(CloseShield);
            SM.Configure(State.CloseShieldError)
                .Permit(Trigger.Retry, State.CloseShield)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.Done)
                .OnEntry( EndStateFunction);
        }

        public void ExecuteCloseShield()
        {
            Start();
        }
    }
}
