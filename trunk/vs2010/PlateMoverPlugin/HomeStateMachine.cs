using System;
using System.Threading;
using BioNex.Shared.IError;
using BioNex.Shared.Utils;

namespace BioNex.PlateMover
{
    public class HomeStateMachine : StateMachineWrapper2<HomeStateMachine.State, HomeStateMachine.Trigger>
    {
        public enum State
        {
            Idle,
            HomeY,
            HomeYError,
            HomeR,
            HomeRError,
            WaitForHomeComplete,
            WaitForHomeCompleteError,
            Done
        }

        public enum Trigger
        {
            Success,
            Failure,
            Retry,
            Ignore,
            Abort,
        }

        private readonly Model _model;

        public HomeStateMachine( Model model, IError error_interface, bool called_from_diags)
            : base(null, error_interface, State.Idle, State.Done, State.Done, Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, called_from_diags)
        {
            _model = model;

            SM.Configure(State.Idle)
                .Permit(Trigger.Success, State.HomeY);
            SM.Configure(State.HomeY)
                .Permit(Trigger.Success, State.HomeR)
                .Permit(Trigger.Failure, State.HomeYError)
                .OnEntry(HomeY);
            SM.Configure(State.HomeYError)
                .Permit(Trigger.Success, State.HomeR)
                .Permit(Trigger.Retry, State.HomeY)
                .Permit(Trigger.Ignore, State.HomeR)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry( f => HandleErrorWithRetryOnly());
            SM.Configure(State.HomeR)
                .Permit(Trigger.Success, State.WaitForHomeComplete)
                .Permit(Trigger.Failure, State.HomeRError)
                .OnEntry(HomeR);
            SM.Configure(State.HomeRError)
                .Permit(Trigger.Success, State.WaitForHomeComplete)
                .Permit(Trigger.Retry, State.HomeR)
                .Permit(Trigger.Ignore, State.WaitForHomeComplete)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry( f => HandleErrorWithRetryOnly());
            SM.Configure(State.WaitForHomeComplete)
                .Permit(Trigger.Success, State.Done)
                .Permit(Trigger.Failure, State.WaitForHomeCompleteError)
                .OnEntry(WaitForHomeComplete);
            SM.Configure(State.WaitForHomeCompleteError)
                .Permit(Trigger.Success, State.Done)
                .Permit(Trigger.Retry, State.HomeY)
                .Permit(Trigger.Ignore, State.Done)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry( f => HandleErrorWithRetryOnly());
            SM.Configure(State.Done)
                .OnEntry(EndStateFunction);
        }

        private void HomeY()
        {
            try
            {
                _model.HomeY();
                Fire(Trigger.Success);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                Fire(Trigger.Failure);
            }
        }

        private void HomeR()
        {
            try
            {
                _model.HomeR();
                Fire(Trigger.Success);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                Fire(Trigger.Failure);
            }
        }

        private void WaitForHomeComplete()
        {
            DateTime start = DateTime.Now;
            const double timeout_sec = 30;
            while( !_model.Homed && (DateTime.Now - start).TotalSeconds < timeout_sec)
                Thread.Sleep( 100);
            if ((DateTime.Now - start).TotalSeconds >= timeout_sec)
            {
                LastError = String.Format("Timed out while waiting for {0} to home", _model.DeviceInstanceName);
                Fire(Trigger.Failure);
            }
            else
            {
                Fire(Trigger.Success);
            }
                
        }
    }
}
