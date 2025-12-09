using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.Utils;
using BioNex.Shared.IError;

namespace BioNex.GreenMachine.StateMachines
{
    public class MixStateMachine : StateMachineWrapper2<MixStateMachine.State, MixStateMachine.Trigger>
    {
        private IGreenMachineController _controller;

        //! \todo replace this with passed-in parameter
        private int _temp_counter = 2;

        public enum State
        {
            Idle,
            SetupPositions, SetupPositionsError,
            MoveAboveWell, MoveAboveWellError,
            PreaspiratePump1, PreaspiratePump1Error,
            ApproachWells, ApproachWellsError,
            CheckMixesRemaining,
            EnterFluid, EnterFluidError,
            AspiratePump1, AspiratePump1Error,
            MoveZRelative, MoveZRelativeError,
            DispensePump1, DispensePump1Error,
            MoveOutOfFluid, MoveOutOfFluidError,
            MoveOutOfPlate, MoveOutOfPlateError,
            Done
        }

        public enum Trigger
        {
            Success,
            Failure,
            Retry,
            Ignore,
            Abort,
            NoMixesRemaining
        }

        public MixStateMachine( IGreenMachineController controller, IError error_interface)
            : base( typeof( MixStateMachine), State.Idle, State.Done, State.Done,
                    Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, error_interface, false)
        {
            _controller = controller;

            ConfigureState( State.Idle, NullStateFunction, State.SetupPositions);
            ConfigureState( State.SetupPositions, SetupPositions, State.MoveAboveWell, State.SetupPositionsError);
            ConfigureState( State.MoveAboveWell, MoveAboveWell, State.PreaspiratePump1, State.MoveAboveWellError);
            ConfigureState( State.PreaspiratePump1, PreaspiratePump1, State.ApproachWells, State.PreaspiratePump1Error);
            ConfigureState( State.ApproachWells, ApproachWells, State.CheckMixesRemaining, State.ApproachWellsError);
            SM.Configure( State.CheckMixesRemaining)
                .Permit( Trigger.Success, State.EnterFluid)
                .Permit( Trigger.NoMixesRemaining, State.Done)
                .OnEntry( CheckMixesRemaining);
            ConfigureState( State.EnterFluid, EnterFluid, State.AspiratePump1, State.EnterFluidError);
            ConfigureState( State.AspiratePump1, AspiratePump1, State.MoveZRelative, State.AspiratePump1Error);
            ConfigureState( State.MoveZRelative, MoveZRelative, State.DispensePump1, State.MoveZRelativeError);
            ConfigureState( State.DispensePump1, DispensePump1, State.MoveOutOfFluid, State.DispensePump1Error);
            ConfigureState( State.MoveOutOfFluid, MoveOutOfFluid, State.MoveOutOfPlate, State.MoveOutOfFluidError);
            ConfigureState( State.MoveOutOfPlate, MoveOutOfPlate, State.CheckMixesRemaining, State.MoveOutOfPlateError);
            ConfigureState( State.Done, EndStateFunction);
        }

        public void SetupPositions()
        {
            Log.Info( "Mix: SetupPositions");
            Fire( Trigger.Success);
        }

        public void MoveAboveWell()
        {
            Log.Info( "Mix: MoveAboveWell");
            Fire( Trigger.Success);
        }

        public void PreaspiratePump1()
        {
            Log.Info( "Mix: PreaspiratePump1");
            Fire( Trigger.Success);
        }

        public void ApproachWells()
        {
            Log.Info( "Mix: ApproachWells");
            Fire( Trigger.Success);
        }

        public void CheckMixesRemaining()
        {
            Log.Info( "Mix: CheckMixesRemaining");
            if( _temp_counter-- > 0) {
                Fire( Trigger.Success);
            } else {
                Fire( Trigger.NoMixesRemaining);
            }
        }

        public void EnterFluid()
        {
            Log.Info( "Mix: EnterFluid");
            Fire( Trigger.Success);
        }

        public void AspiratePump1()
        {
            Log.Info( "Mix: AspiratePump1");
            Fire( Trigger.Success);
        }

        public void MoveZRelative()
        {
            Log.Info( "Mix: MoveZRelative");
            Fire( Trigger.Success);
        }

        public void DispensePump1()
        {
            Log.Info( "Mix: DispensePump1");
            Fire( Trigger.Success);
        }

        public void MoveOutOfFluid()
        {
            Log.Info( "Mix: MoveOutOfFluid");
            Fire( Trigger.Success);
        }

        public void MoveOutOfPlate()
        {
            Log.Info( "Mix: MoveOutOfPlate");
            Fire( Trigger.Success);
        }
    }
}
