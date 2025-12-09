using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.Utils;
using BioNex.Shared.IError;

namespace BioNex.GreenMachine.StateMachines
{
    public class DispenseStateMachine : StateMachineWrapper2<DispenseStateMachine.State, DispenseStateMachine.Trigger>
    {
        private IGreenMachineController _controller;

        public enum State
        {
            Idle,
            SetupPositions, SetupPositionsError,
            MoveAbovePlateOrWash, MoveAbovePlateOrWashError,
            EnterFluid, EnterFluidError,
            DispensePump1, DispensePump1Error,
            DispensePump2, DispensePump2Error,
            LeaveFluid, LeaveFluidError,
            LeaveWells, LeaveWellsError,
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

        public DispenseStateMachine( IGreenMachineController controller, IError error_interface)
            : base( typeof(DispenseStateMachine), State.Idle, State.Done, State.Done,
                    Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, error_interface, false)
        {
            _controller = controller;

            ConfigureState( State.Idle, NullStateFunction, State.SetupPositions);
            ConfigureState( State.SetupPositions, SetupPositions, State.MoveAbovePlateOrWash, State.SetupPositionsError);
            ConfigureState( State.MoveAbovePlateOrWash, MoveAbovePlateOrWash, State.EnterFluid, State.MoveAbovePlateOrWashError);
            ConfigureState( State.EnterFluid, EnterFluid, State.DispensePump1, State.EnterFluidError);
            ConfigureState( State.DispensePump1, DispensePump1, State.DispensePump2, State.DispensePump1Error);
            ConfigureState( State.DispensePump2, DispensePump2, State.LeaveFluid, State.DispensePump2Error);
            ConfigureState( State.LeaveFluid, LeaveFluid, State.LeaveWells, State.LeaveFluidError);
            ConfigureState( State.LeaveWells, LeaveWells, State.Done, State.LeaveWellsError);
            ConfigureState( State.Done, EndStateFunction);
        }

        private void SetupPositions()
        {
            Log.Info( "Dispense: SetupPositions");
            Fire( Trigger.Success);
        }
        private void MoveAbovePlateOrWash()
        {
            Log.Info( "Dispense: MoveAbovePlateOrWash");
            Fire( Trigger.Success);
        }

        private void EnterFluid()
        {
            Log.Info( "Dispense: EnterFluid");
            Fire( Trigger.Success);
        }

        private void DispensePump1()
        {
            Log.Info( "Dispense: DispensePump1");
            Fire( Trigger.Success);
        }

        private void DispensePump2()
        {
            Log.Info( "Dispense: DispensePump2");
            Fire( Trigger.Success);
        }

        private void LeaveFluid()
        {
            Log.Info( "Dispense: LeaveFluid");
            Fire( Trigger.Success);
        }

        private void LeaveWells()
        {
            Log.Info( "Dispense: LeaveWells");
            Fire( Trigger.Success);
        }
    }
}
