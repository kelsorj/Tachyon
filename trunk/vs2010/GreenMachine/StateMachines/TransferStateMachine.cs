using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.IError;

namespace BioNex.GreenMachine.StateMachines
{
    public class TransferStateMachine : BioNex.Shared.Utils.StateMachineWrapper2<TransferStateMachine.State,TransferStateMachine.Trigger>
    {
        private IGreenMachineController _controller;

        public enum State
        {
            Idle,
            SetupPositions, SetupPositionsError,
            MoveAboveWashOrWasteIfNecessary, MoveAboveWashOrWasteIfNecessaryError,
            Preaspirate, PreaspirateError,
            EnterWells, EnterWellsError,
            DispensePump1, DispensePump1Error,
            LeaveWells, LeaveWellsError,
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

        public TransferStateMachine( IGreenMachineController controller, IError error_interface)
            : base( typeof(TransferStateMachine), State.Idle, State.Done, State.Done,
                    Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, error_interface, false)
        {
            _controller = controller;

            ConfigureState( State.Idle, NullStateFunction, State.SetupPositions);
            ConfigureState( State.SetupPositions, SetupPositions, State.MoveAboveWashOrWasteIfNecessary, State.SetupPositionsError);
            ConfigureState( State.MoveAboveWashOrWasteIfNecessary, MoveAboveWashOrWasteIfNecessary, State.Preaspirate, State.MoveAboveWashOrWasteIfNecessaryError);
            ConfigureState( State.Preaspirate, Preaspirate, State.EnterWells, State.PreaspirateError);
            ConfigureState( State.EnterWells, EnterWells, State.DispensePump1, State.EnterWellsError);
            ConfigureState( State.DispensePump1, DispensePump1, State.LeaveWells, State.DispensePump1Error);
            ConfigureState( State.LeaveWells, LeaveWells, State.Done, State.LeaveWellsError);
            ConfigureState( State.Done, EndStateFunction);
        }

        private void SetupPositions()
        {
            Log.Info( "Transfer: SetupPositions");
            Fire( Trigger.Success);
        }

        private void MoveAboveWashOrWasteIfNecessary()
        {
            Log.Info( "Transfer: MoveAboveWashOrWasteIfNecessary");
            Fire( Trigger.Success);
        }

        private void Preaspirate()
        {
            Log.Info( "Transfer: Preaspirate");
            Fire( Trigger.Success);
        }

        private void EnterWells()
        {
            Log.Info( "Transfer: EnterWells");
            Fire( Trigger.Success);
        }

        private void DispensePump1()
        {
            Log.Info( "Transfer: DispensePump1");
            Fire( Trigger.Success);
        }

        private void LeaveWells()
        {
            Log.Info( "Transfer: LeaveWells");
            Fire( Trigger.Success);
        }
    }
}
