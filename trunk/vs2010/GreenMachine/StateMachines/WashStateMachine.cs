using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.Utils;
using BioNex.Shared.IError;

namespace BioNex.GreenMachine.StateMachines
{
    public class WashStateMachine : StateMachineWrapper2<WashStateMachine.State, WashStateMachine.Trigger>
    {
        private IGreenMachineController _controller;
        
        //! \todo replace this with a passed-in parameter
        private int _temp_counter = 2;

        public enum State
        {
            Idle,
            MoveZToClear, MoveZToClearError,
            EnterWash, EnterWashError,
            CheckRemainingCycles,
            AllPumpsTransfer, AllPumpsTransferError,
            WaitForTimer,
            LeaveWells, LeaveWellsError,
            DipSyringeAndLeave, DipSyringeAndLeaveError,
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

        public WashStateMachine( IGreenMachineController controller, IError error_interface)
            : base( typeof(WashStateMachine), State.Idle, State.Done, State.Done,
                    Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, error_interface, false)
        {
            _controller = controller;

            ConfigureState( State.Idle, NullStateFunction, State.MoveZToClear);
            ConfigureState( State.MoveZToClear, MoveZToClear, State.EnterWash, State.MoveZToClearError);
            ConfigureState( State.EnterWash, EnterWash, State.CheckRemainingCycles, State.EnterWashError);
            SM.Configure( State.CheckRemainingCycles)
                .Permit( Trigger.Success, State.AllPumpsTransfer)
                .Permit( Trigger.NoMoreCycles, State.LeaveWells)
                .OnEntry( CheckRemainingCycles);
            ConfigureState( State.AllPumpsTransfer, AllPumpsTransfer, State.WaitForTimer, State.AllPumpsTransferError);
            ConfigureState( State.WaitForTimer, WaitForTimer, State.CheckRemainingCycles);
            ConfigureState( State.LeaveWells, LeaveWells, State.DipSyringeAndLeave, State.LeaveWellsError);
            ConfigureState( State.DipSyringeAndLeave, DipSyringeAndLeave, State.Done, State.DipSyringeAndLeaveError);
            ConfigureState( State.Done, EndStateFunction);
        }

        private void MoveZToClear()
        {
            Log.Info( "Wash: MoveZToClear");
            Fire( Trigger.Success);
        }

        private void EnterWash()
        {
            Log.Info( "Wash: EnterWash");
            Fire( Trigger.Success);
        }

        private void CheckRemainingCycles()
        {
            Log.Info( "Wash: CheckRemainingCycles");
            if( _temp_counter-- > 0) {
                Fire( Trigger.Success);
            } else {
                Fire( Trigger.NoMoreCycles);
            }
        }

        private void AllPumpsTransfer()
        {
            Log.Info( "Wash: AllPumpsTransfer");
            Fire( Trigger.Success);
        }

        private void WaitForTimer()
        {
            Log.Info( "Wash: WaitForTimer");
            Fire( Trigger.Success);
        }

        private void LeaveWells()
        {
            Log.Info( "Wash: LeaveWells");
            Fire( Trigger.Success);
        }

        private void DipSyringeAndLeave()
        {
            Log.Info( "Wash: DipSyringeAndLeave");
            Fire( Trigger.Success);
        }
    }
}
