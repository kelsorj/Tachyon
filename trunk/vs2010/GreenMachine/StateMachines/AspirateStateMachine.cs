using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.Utils;
using BioNex.Shared.IError;

namespace BioNex.GreenMachine.StateMachines
{
    public class AspirateStateMachine : StateMachineWrapper2<AspirateStateMachine.State, AspirateStateMachine.Trigger>
    {
        private IGreenMachineController _controller;

        public enum State
        {
            Idle,
            SetupPositions, SetupPositionsError,
            MoveZAbovePlate, MoveZAbovePlateError,
            Preaspirate, PreaspirateError,
            ApproachFluid, ApproachFluidError,
            EnterWells, EnterWellsError,
            Aspirate, AspirateError,
            PostAspirateDispense, PostAspirateDispenseError,
            LeaveFluid, LeaveFluidError,
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

        public AspirateStateMachine( IGreenMachineController controller, IError error_interface)
            : base( typeof(AspirateStateMachine), State.Idle, State.Done, State.Done, Trigger.Success,
                    Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, error_interface, false)
        {
            _controller = controller;

            ConfigureState( State.Idle, NullStateFunction, State.SetupPositions);
            ConfigureState( State.SetupPositions, SetupPositions, State.MoveZAbovePlate, State.SetupPositionsError);
            ConfigureState( State.MoveZAbovePlate, MoveZAbovePlate, State.Preaspirate, State.MoveZAbovePlateError);
            ConfigureState( State.Preaspirate, Preaspirate, State.ApproachFluid, State.PreaspirateError);
            ConfigureState( State.ApproachFluid, ApproachFluid, State.EnterWells, State.ApproachFluidError);
            ConfigureState( State.EnterWells, EnterWells, State.Aspirate, State.EnterWellsError);
            ConfigureState( State.Aspirate, Aspirate, State.PostAspirateDispense, State.AspirateError);
            ConfigureState( State.PostAspirateDispense, PostAspirateDispense, State.LeaveFluid, State.PostAspirateDispenseError);
            ConfigureState( State.LeaveFluid, LeaveFluid, State.LeaveWells, State.LeaveFluidError);
            ConfigureState( State.LeaveWells, LeaveWells, State.Done, State.LeaveWellsError);
            ConfigureState( State.Done, EndStateFunction);
        }

        private void SetupPositions()
        {
            Log.Info( "Aspirate: SetupPositions");
            Fire( Trigger.Success);
        }

        private void MoveZAbovePlate()
        {
            Log.Info( "Aspirate: MoveZAbovePlate");
            Fire( Trigger.Success);
        }

        private void Preaspirate()
        {
            Log.Info( "Aspirate: Preaspirate");
            Fire( Trigger.Success);
        }

        private void ApproachFluid()
        {
            Log.Info( "Aspirate: ApproachFluid");
            Fire( Trigger.Success);
        }

        private void EnterWells()
        {
            Log.Info( "Aspirate: EnterWells");
            Fire( Trigger.Success);
        }

        private void Aspirate()
        {
            Log.Info( "Aspirate: Aspirate");
            Fire( Trigger.Success);
        }

        private void PostAspirateDispense()
        {
            Log.Info( "Aspirate: PostAspirateDispense");
            Fire( Trigger.Success);
        }

        private void LeaveFluid()
        {
            Log.Info( "Aspirate: LeaveFluid");
            Fire( Trigger.Success);
        }

        private void LeaveWells()
        {
            Log.Info( "Aspirate: LeaveWells");
            Fire( Trigger.Success);
        }
    }
}
