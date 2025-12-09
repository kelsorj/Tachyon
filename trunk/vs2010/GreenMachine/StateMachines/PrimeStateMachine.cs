using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.Utils;
using BioNex.Shared.IError;

namespace BioNex.GreenMachine.StateMachines
{
    public class PrimeStateMachine : StateMachineWrapper2<PrimeStateMachine.State, PrimeStateMachine.Trigger>
    {
        private IGreenMachineController _controller;

        //! \todo replace this with passed-in parameters later on
        private int _temp_counter = 2;

        public enum State
        {
            Idle,
            MoveZToClear, MoveZToClearError,
            EnterWash, EnterWashError,
            CheckMixCyclesRemaining,
            TransferForAllPumps, TransferForAllPumpsError,
            WaitForTimer,
            Aspirate, AspirateError,
            LeaveWash, LeaveWashError,
            MoveAboveWaste, MoveAboveWasteError,
            Done
        }

        public enum Trigger
        {
            Success,
            Failure,
            Retry,
            Ignore,
            Abort,
            NoMoreCycles
        }

        public PrimeStateMachine( IGreenMachineController controller, IError error_interface)
            : base( typeof( PrimeStateMachine), State.Idle, State.Done, State.Done,
                    Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, error_interface, false)
        {
            _controller = controller;

            ConfigureState( State.Idle, NullStateFunction, State.MoveZToClear);
            ConfigureState( State.MoveZToClear, MoveZToClear, State.EnterWash, State.MoveZToClearError);
            ConfigureState( State.EnterWash, EnterWash, State.CheckMixCyclesRemaining, State.EnterWashError);
            SM.Configure( State.CheckMixCyclesRemaining)
                .Permit( Trigger.Success, State.TransferForAllPumps)
                .Permit( Trigger.NoMoreCycles, State.Aspirate)
                .OnEntry( CheckMixCyclesRemaining);
            ConfigureState( State.TransferForAllPumps, TransferForAllPumps, State.WaitForTimer, State.TransferForAllPumpsError);
            ConfigureState( State.WaitForTimer, WaitForTimer, State.CheckMixCyclesRemaining);
            ConfigureState( State.Aspirate, Aspirate, State.LeaveWash, State.AspirateError);
            ConfigureState( State.LeaveWash, LeaveWash, State.MoveAboveWaste, State.LeaveWashError);
            ConfigureState( State.MoveAboveWaste, MoveAboveWaste, State.Done, State.MoveAboveWasteError);
            ConfigureState( State.Done, EndStateFunction);
        }

        private void MoveZToClear()
        {
            Log.Info( "Prime: MoveZToClear");
            Fire( Trigger.Success);
        }

        private void EnterWash()
        {
            Log.Info( "Prime: EnterWash");
            Fire( Trigger.Success);
        }

        private void CheckMixCyclesRemaining()
        {
            Log.Info( "Prime: CheckMixCyclesRemaining");
            if( _temp_counter-- > 0) {
                Log.Info( "Prime: more cycles remaining");
                Fire( Trigger.Success);
            } else {
                Log.Info( "Prime: no more cycles remaining");
                Fire( Trigger.NoMoreCycles);
            }
        }

        private void TransferForAllPumps()
        {
            Log.Info( "Prime: TransferForAllPumps");
            Fire( Trigger.Success);
        }

        private void WaitForTimer()
        {
            Log.Info( "Prime: WaitForTimer");
            Fire( Trigger.Success);
        }

        private void Aspirate()
        {
            Log.Info( "Prime: Aspirate");
            Fire( Trigger.Success);
        }

        private void LeaveWash()
        {
            Log.Info( "Prime: LeaveWash");
            Fire( Trigger.Success);
        }

        private void MoveAboveWaste()
        {
            Log.Info( "Prime: MoveAboveWaste");
            Fire( Trigger.Success);
        }
    }
}
