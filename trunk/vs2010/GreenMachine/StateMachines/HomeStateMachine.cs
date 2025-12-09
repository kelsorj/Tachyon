using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.IError;

namespace BioNex.GreenMachine.StateMachines
{
    public class HomeStateMachine : BioNex.Shared.Utils.StateMachineWrapper2<HomeStateMachine.State,HomeStateMachine.Trigger>
    {
        private IGreenMachineController _controller;

        public enum State
        {
            Start,
            HomeZ, HomeZError,
            HomeRemainingAxes,
            WaitForHomeCompleteOrErrors,
            WaitForHomeCompleteError,
            Done,
        }

        public enum Trigger
        {
            Success,
            Failure,
            Retry,
            Ignore,
            Abort,
        }

        public HomeStateMachine( IGreenMachineController controller, IError error_interface)
            : base( typeof( HomeStateMachine), State.Start, State.Done, State.Done,
                    Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, error_interface, false)
        {
            _controller = controller;

            ConfigureState( State.Start, NullStateFunction, State.HomeZ);
            ConfigureState( State.HomeZ, HomeZ, State.HomeRemainingAxes, State.HomeZError);
            ConfigureState( State.HomeRemainingAxes, HomeRemainingAxes, State.WaitForHomeCompleteOrErrors);
            SM.Configure( State.WaitForHomeCompleteOrErrors)
                .Permit( Trigger.Success, State.Done)
                .Permit( Trigger.Failure, State.WaitForHomeCompleteError);
            SM.Configure( State.WaitForHomeCompleteError)
                .Permit( Trigger.Retry, State.HomeRemainingAxes);
            ConfigureState( State.Done, EndStateFunction);
        }

        private void HomeZ()
        {
            Log.Info( "Home: HomeZ");
            try {
                _controller.Stage.HomeAxis( HardwareInterfaces.IXyz.Axes.Z, true);
                Fire( Trigger.Success);
            } catch( Exception ex) {
                LastError = ex.Message;
                Fire( Trigger.Failure);
            }
        }

        private void HomeRemainingAxes()
        {
            Log.Info( "Home: HomeRemainingAxes");
            try {
                _controller.Stage.HomeAxis( HardwareInterfaces.IXyz.Axes.X, false);
                _controller.Stage.HomeAxis( HardwareInterfaces.IXyz.Axes.Y, false);
                Fire( Trigger.Success);
            } catch( Exception ex) {
                LastError = ex.Message;
                Fire( Trigger.Failure);
            }
        }

        private void WaitForHomeCompleteOrErrors()
        {
            Log.Info( "Home: WaitForHomeCompleteOrErrors");
            try {
                Fire( Trigger.Success);
            } catch( Exception ex) {
                LastError = ex.Message;
                Fire( Trigger.Abort);
            }
        }
    }
}
