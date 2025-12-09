using System;
using BioNex.BumblebeePlugin.Hardware;

namespace BioNex.BumblebeePlugin.Dispatcher
{
    public abstract class ICDMoveStageStateMachine : ICDStateMachine< ICDMoveStageStateMachine.State, ICDMoveStageStateMachine.Trigger>
    {
        // ----------------------------------------------------------------------
        // enumerations.
        // ----------------------------------------------------------------------
        public enum State
        {
            Start,
            MoveStage, MoveStageError,
            OnFinishCritical,
            End, Abort,
        }
        // ----------------------------------------------------------------------
        public enum Trigger
        {
            Success,
            Failure,
            Retry,
            Ignore,
            Abort,
        }

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        protected Stage Stage { get; private set; }
        protected double YCoordinate { get; private set; }
        protected double RCoordinate { get; private set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        protected ICDMoveStageStateMachine(ICDParameterBundle parameter_bundle, ICDEventBundle event_bundle, DispatcherJob job, Stage stage, double y_coordinate, double r_coordinate)
            : base(parameter_bundle, event_bundle, job)
        {
            Stage = stage;
            YCoordinate = y_coordinate;
            RCoordinate = r_coordinate;

            InitializeStates();
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        private void InitializeStates()
        {
            ConfigureState( State.Start, NullStateFunction, State.MoveStage);
            ConfigureState( State.MoveStage, MoveStage, State.OnFinishCritical, State.MoveStageError);
            ConfigureState( State.OnFinishCritical, OnFinishCritical, State.End);
            ConfigureState( State.End, EndStateFunction);
            ConfigureState( State.Abort, AbortedStateFunction);
        }

        // ----------------------------------------------------------------------
        // state functions.
        // ----------------------------------------------------------------------
        protected virtual void MoveStage()
        {
            Console.WriteLine( "{0}.{1}", GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);
            FakeOperation( 3000, 5000);
            Fire( Trigger.Success);
        }
    }

    public class ICDMoveStageStateMachine2 : ICDMoveStageStateMachine
    {
        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public ICDMoveStageStateMachine2(ICDParameterBundle parameter_bundle, ICDEventBundle event_bundle, DispatcherJob job, Stage stage, double y_coordinate, double r_coordinate)
            : base(parameter_bundle, event_bundle, job, stage, y_coordinate, r_coordinate)
        {
        }

        // ----------------------------------------------------------------------
        // state functions.
        // ----------------------------------------------------------------------
        protected override void MoveStage()
        {
            try{
                Stage.ClearForStage();
                Stage.MoveAbsolute( YCoordinate, RCoordinate);
                Fire( Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        protected override void EndStateFunction()
        {
            if( Stage is TipShuttle){
                Log.DebugFormat( "Finished Moving {0}", Stage);
            }
            base.EndStateFunction();
        }
    }
}
