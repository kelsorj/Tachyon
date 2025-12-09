using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Hig.StateMachines;
using BioNex.Hig;
using System.Threading;

namespace BioNex.HiGIntegration.StateMachines
{
    public class IntegrationHomeStateMachine : HomeStateMachine
    {
        public IntegrationHomeStateMachine( IHigModel model, bool home_shield_only, bool open_shield_after_homing, bool called_from_diags)
            : base( model, home_shield_only, open_shield_after_homing, called_from_diags)
        {
        }

        protected override void InitializeStates()
        {
            SM.Configure(State.Idle)
                .Permit(Trigger.Execute, State.HomeShield);
            SM.Configure(State.HomeShield)
                .Permit(Trigger.Success, State.HomeSpindle)
                .Permit(Trigger.HomeShieldOnly, State.CheckOpenShield)
                .PermitReentry(Trigger.OvervoltageRecovery)
                .Permit(Trigger.Fail, State.FailedDone)
                .OnEntry(HomeShield);
            SM.Configure(State.CheckOpenShield)
                .Permit(Trigger.Success, State.OpenShield)
                .Permit(Trigger.LeaveShieldClosed, State.Done)
                .OnEntry(CheckOpenShield);
            SM.Configure(State.HomeSpindle)
                .Permit(Trigger.Success, State.SetAngle)
                .Permit(Trigger.Fail, State.FailedDone)
                .OnEntry(HomeSpindle);
            SM.Configure(State.SetAngle)
                .Permit(Trigger.Success, State.GotoAngle)
                .Permit(Trigger.Fail, State.FailedDone)
                .OnEntry(SetAngle);
            SM.Configure(State.GotoAngle)
                .Permit(Trigger.Success, State.OpenShield)
                .Permit(Trigger.LeaveShieldClosed, State.Done)
                .Permit(Trigger.Fail, State.FailedDone)
                .OnEntry(GotoAngle);
            SM.Configure(State.OpenShield)
                .Permit(Trigger.Success, State.Done)
                .Permit(Trigger.Fail, State.FailedDone)
                .OnEntry(OpenShield);
            SM.Configure(State.HomeShieldError)
                .Permit(Trigger.Retry, State.HomeShield)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.HomeSpindleError)
                .Permit(Trigger.Retry, State.HomeSpindle)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.SetAngleError)
                .Permit(Trigger.Retry, State.SetAngle)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.GotoAngleError)
                .Permit(Trigger.Retry, State.GotoAngle)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.OpenShieldError)
                .Permit(Trigger.Retry, State.OpenShield)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.FailedDone)
                .OnEntry(Failed);
            SM.Configure(State.Done)
                .OnEntry(EndStateFunction);
        }

        private void Failed()
        {
            AbortedStateFunction();
            ((HiG)Model).LastError = LastErrorMessage;
            throw new Exception( LastErrorMessage);
        }
    }

    public class IntegrationOpenShieldStateMachine : OpenShieldStateMachine
    {
        public IntegrationOpenShieldStateMachine( IHigModel model, bool called_from_diags)
            : base( model, called_from_diags)
        {
        }

        protected override void InitializeStates()
        {
            SM.Configure(State.Idle)
                .Permit(Trigger.Execute, State.TestAngle);
            SM.Configure(State.TestAngle)
                .Permit(Trigger.Fail, State.FailedDone)
                .Permit(Trigger.Success, State.CloseShield)
                .Permit(Trigger.Continue, State.OpenShield)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(TestAngle);
            SM.Configure(State.CloseShield)
                .Permit(Trigger.Success, State.SetAngle)
                .Permit(Trigger.Fail, State.FailedDone)
                .OnEntry(CloseShield);
            SM.Configure(State.SetAngle)
                .Permit(Trigger.Success, State.GotoAngle)
                .Permit(Trigger.Fail, State.FailedDone)
                .OnEntry(SetAngle);
            SM.Configure(State.GotoAngle)
                .Permit(Trigger.Success, State.OpenShield)
                .Permit(Trigger.Fail, State.FailedDone)
                .OnEntry(GotoAngle);
            SM.Configure(State.OpenShield)
                .Permit( Trigger.Success, State.Done)
                .Permit(Trigger.Fail, State.FailedDone)
                .OnEntry( OpenShield);
            SM.Configure(State.CloseShieldError)
                .Permit(Trigger.Retry, State.CloseShield)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.SetAngleError)
                .Permit(Trigger.Retry, State.SetAngle)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.GotoAngleError)
                .Permit(Trigger.Retry, State.GotoAngle)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.OpenShieldError)
                .Permit(Trigger.Retry, State.OpenShield)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.FailedDone)
                .OnEntry(Failed);
            SM.Configure(State.Done)
                .OnEntry( EndStateFunction);            
        }

        private void Failed()
        {
            AbortedStateFunction();
            ((HiG)Model).LastError = LastErrorMessage;
            throw new Exception(LastErrorMessage);
        }
    }

    public class IntegrationSpinStateMachine : SpinStateMachine
    {
        public IntegrationSpinStateMachine( IHigModel model, bool called_from_diags)
            : base( model, called_from_diags)
        {
        }

        protected override void InitializeStates()
        {
            SM.Configure(State.Idle)
                .Permit(Trigger.Execute, State.CloseShield);
            SM.Configure(State.CloseShield)
                .Permit(Trigger.Success, State.Accelerate)
                .Permit(Trigger.Abort, State.Done)
                .Permit(Trigger.Fail, State.FailedDone)
                .OnEntry(CloseShield);
            SM.Configure(State.Accelerate)
                .Permit(Trigger.Success, State.InterruptableWait)
                .Permit(Trigger.AbortAndProceed, State.Decelerate)
                .Permit(Trigger.Fail, State.FailedDone)
                .Permit(Trigger.FailDoor, State.FailedDone)
                .Permit(Trigger.FailWaitForSpinDown, State.AccelerateErrorWaitForSpinDown)
                .OnEntry(Accelerate);
            SM.Configure(State.InterruptableWait)
                .Permit(Trigger.Success, State.Decelerate)
                .Permit(Trigger.AbortAndProceed, State.Decelerate)
                .PermitReentry(Trigger.ContinueWait)
                .Permit(Trigger.Fail, State.FailedDone)
                .OnEntry(InterruptableWait);
            SM.Configure(State.Decelerate)
                .Permit( Trigger.Success, State.SetAngle)
                .Permit(Trigger.Fail, State.FailedDone)
                .Ignore(Trigger.Abort)
                .Ignore(Trigger.AbortAndProceed)
                .OnEntry( () => Decelerate());
            SM.Configure(State.SetAngle)
                .Permit(Trigger.Success, State.GotoAngle)
                .Permit(Trigger.Abort, State.Done)
                .Permit(Trigger.Fail, State.FailedDone)
                .OnEntry(SetAngle);
            SM.Configure(State.GotoAngle)
                .Permit(Trigger.Success, State.OpenShield)
                .Permit(Trigger.Abort, State.Done)
                .Permit(Trigger.Fail, State.FailedDone)
                .OnEntry(GotoAngle);
            SM.Configure(State.OpenShield)
                .Permit(Trigger.Success, State.Done)
                .Permit(Trigger.Abort, State.Done)
                .Permit(Trigger.Fail, State.FailedDone)
                .OnEntry(OpenShield);
            SM.Configure(State.AccelerateErrorWaitForSpinDown)
                .Permit(Trigger.Retry, State.Accelerate)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => {
                    try {
                        Decelerate( false);
                    } catch( Exception) {
                        // can't do anything about deceleration errors here...
                    }
                    Failed();
                });
            // DKM 2012-04-24 these states weren't used
            /*
            SM.Configure(State.CloseShieldError)
                .Permit(Trigger.Retry, State.CloseShield)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.AccelerateError)
                .Permit(Trigger.Retry, State.Accelerate)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.DecelerateError)
                .Permit(Trigger.Retry, State.Decelerate)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.SetAngleError)
                .Permit(Trigger.Retry, State.SetAngle)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.GotoAngleError)
                .Permit(Trigger.Retry, State.GotoAngle)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.OpenShieldError)
                .Permit(Trigger.Retry, State.OpenShield)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
             */
            SM.Configure(State.FailedDone)
                .OnEntry(Failed);
            SM.Configure(State.Done)
                .OnEntry( EndStateFunction);            
        }

        private void Failed()
        {
            AbortedStateFunction();
            ((HiG)Model).LastError = LastErrorMessage;
            throw new Exception( LastErrorMessage);
        }
    }

    public class IntegrationCloseShieldStateMachine : CloseShieldStateMachine
    {
        public IntegrationCloseShieldStateMachine( IHigModel model, bool called_from_diags)
            : base( model, called_from_diags)
        {
        }

        protected override void InitializeStates()
        {
            SM.Configure(State.Idle)
                .Permit(Trigger.Execute, State.CloseShield);
            SM.Configure(State.CloseShield)
                .Permit(Trigger.Success, State.Done)
                .Permit(Trigger.Fail, State.FailedDone)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(CloseShield);
            SM.Configure(State.FailedDone)
                .OnEntry(Failed);
            SM.Configure(State.Done)
                .OnEntry( EndStateFunction);
        }

        private void Failed()
        {
            AbortedStateFunction();
            ((HiG)Model).LastError = LastErrorMessage;
            throw new Exception( LastErrorMessage);
        }
    }

    public class IntegrationImbalanceCalibrationStateMachine : ImbalanceCalibrationStateMachine
    {
        public IntegrationImbalanceCalibrationStateMachine( IHigModel model, bool called_from_diags)
            : base( model, called_from_diags)
        {
        }

        protected override void InitializeStates()
        {
            SM.Configure(State.Idle)
                .Permit(Trigger.Execute, State.CloseShield);
            SM.Configure(State.CloseShield)
                .Permit(Trigger.Success, State.SetupImbalanceCalibration)
                .Permit(Trigger.Fail, State.CloseShieldError)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(() =>  {
                    Thread.Sleep(0);
                    CloseShield();
                });
            SM.Configure( State.SetupImbalanceCalibration)
                .Permit( Trigger.Success, State.Accelerate)
                .Permit( Trigger.Fail, State.FailedDone)
                .OnEntry( SetupImbalanceCalibration);
            SM.Configure(State.Accelerate)
                .Permit(Trigger.Success, State.Decelerate)
                .Permit(Trigger.AbortAndProceed, State.Decelerate)
                .Permit(Trigger.Fail, State.FailedDone)
                .Permit(Trigger.FailDoor, State.FailedDone)
                .Permit(Trigger.Invalid, State.FailedDone)
                .OnEntry(Accelerate);
            SM.Configure(State.Decelerate)
                .Permit( Trigger.Continue, State.Accelerate)
                .Permit( Trigger.Success, State.CalculateImbalance)
                .Permit( Trigger.Fail, State.FailedDone)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry( Decelerate);
            SM.Configure( State.CalculateImbalance)
                .Permit( Trigger.Success, State.SetAngle)
                .Permit( Trigger.Fail, State.FailedDone)
                .OnEntry( CalculateImbalance);
            SM.Configure(State.SetAngle)
                .Permit(Trigger.Success, State.GotoAngle)
                .Permit(Trigger.Fail, State.FailedDone)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(SetAngle);
            SM.Configure(State.GotoAngle)
                .Permit(Trigger.Success, State.OpenShield)
                .Permit(Trigger.Fail, State.FailedDone)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(GotoAngle);
            SM.Configure(State.OpenShield)
                .Permit(Trigger.Success, State.Done)
                .Permit(Trigger.Fail, State.FailedDone)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(OpenShield);
            SM.Configure(State.FailedDone)
                .OnEntry(Failed);
            SM.Configure(State.Done)
                .OnEntry( EndStateFunction);            
        }

        private void Failed()
        {
            AbortedStateFunction();
            ((HiG)Model).LastError = LastErrorMessage;
            throw new Exception( LastErrorMessage);
        }
    }
}
