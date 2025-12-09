using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.Utils;
using BioNex.Shared.IError;

namespace BioNex.GreenMachine.StateMachines
{
    public class LoadSyringeStateMachine : StateMachineWrapper2<LoadSyringeStateMachine.State, LoadSyringeStateMachine.Trigger>
    {
        private IGreenMachineController _controller;

        public enum State
        {
            Idle,
            AspiratePump1, AspiratePump1Error,
            AspiratePump2, AspiratePump2Error,
            Done
        }

        public enum Trigger
        {
            Success,
            Failure,
            Retry,
            Ignore,
            Abort
        }

        public LoadSyringeStateMachine( IGreenMachineController controller, IError error_interface)
            : base( typeof(LoadSyringeStateMachine), State.Idle, State.Done, State.Done,
                    Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, error_interface, false)
        {
            _controller = controller;

            ConfigureState( State.Idle, NullStateFunction, State.AspiratePump1, State.AspiratePump1Error);
            ConfigureState( State.AspiratePump1, AspiratePump1, State.AspiratePump2, State.AspiratePump1Error);
            ConfigureState( State.AspiratePump2, AspiratePump2, State.Done, State.AspiratePump2Error);
            ConfigureState( State.Done, EndStateFunction);
        }

        private void AspiratePump1()
        {
            Log.Info( "LoadSyringe: AspiratePump1");
            Fire( Trigger.Success);
        }

        private void AspiratePump2()
        {
            Log.Info( "LoadSyringe: AspiratePump2");
            Fire( Trigger.Success);
        }
    }
}
