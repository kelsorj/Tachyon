using System;
using BioNex.Shared.Utils;
using System.Collections.Generic;

namespace BioNex.LiquidLevelDevice
{
    internal class LLSLocateTeachpointStateMachine : StateMachineWrapper<LLSLocateTeachpointStateMachine.State, LLSLocateTeachpointStateMachine.Trigger>
    {
        public enum State
        {
            Start,
            Init,
            RunXHiResScan,
            RunYHiResScan,
            Done,
            Aborted
        }

        public enum Trigger
        {
            Start,
            RunXHiResScan,
            RunYHiResScan,
            Done,
            Abort
        }

        ILLSensorModel _model;

        IDictionary<Coord, List<Measurement>> _x_measurements;
        IDictionary<Coord, List<Measurement>> _y_measurements;
        public IDictionary<Coord, List<Measurement>> X_Measurements { get { return _x_measurements; } }
        public IDictionary<Coord, List<Measurement>> Y_Measurements { get { return _y_measurements; } }

        const double STEP_SIZE = 0.01;
        const double RANGE = 3;

        public LLSLocateTeachpointStateMachine(ILLSensorModel model)
            : base( State.Start, Trigger.Start, Trigger.Abort, true)
        {
            _model = model;

            SM.Configure(State.Start)
                .Permit(Trigger.Start, State.Init)
                .Permit(Trigger.Abort, State.Aborted);
            SM.Configure(State.Init)
                .Permit(Trigger.RunXHiResScan, State.RunXHiResScan)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(InitialState);
            SM.Configure(State.RunXHiResScan)
                .Permit(Trigger.RunYHiResScan, State.RunYHiResScan)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(RunXHiResScan);
            SM.Configure(State.RunYHiResScan)
                .Permit(Trigger.Done, State.Done)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(RunYHiResScan);
            SM.Configure(State.Done)
                .OnEntry(Done);
            SM.Configure(State.Aborted)
                .OnEntry(Aborted);
        }

        public void ManualAbort()
        {
            if (_subordinate_sm != null)
                _subordinate_sm.ManualAbort();
            Abort();
        }

        private void InitialState()
        {
            Fire(Trigger.RunXHiResScan);
        }

        LLSHiResScanStateMachine _subordinate_sm;
        private void RunXHiResScan()
        {
            var param = new LLSHiResScanStateMachine.HiResScanParams(_model, "", false);

            param.min_x = -RANGE; // _model.Properties.GetDouble(LLProperties.HiResMinX);
            param.max_x = RANGE; // _model.Properties.GetDouble(LLProperties.HiResMaxX);
            param.step_x = STEP_SIZE; // _model.Properties.GetDouble(LLProperties.HiResStepX);

            param.min_y = 0;
            param.max_y = 0;
            param.step_y = STEP_SIZE; // _model.Properties.GetDouble(LLProperties.HiResStepY);

            param.last_column = 0;
            param.first_column = 0;
            param.step_column = 1;

            _subordinate_sm = new LLSHiResScanStateMachine(_model, param);
            _subordinate_sm.Start();
            _x_measurements = new SortedDictionary<Coord, List<Measurement>>(_subordinate_sm.Measurements);
            _subordinate_sm = null;

            Fire(Trigger.RunYHiResScan);
        }

        private void RunYHiResScan()
        {
            var y_offset = _model.Properties.GetDouble(LLProperties.LocateTeachpointFeatureToFeatureY);
            var param = new LLSHiResScanStateMachine.HiResScanParams(_model, "");

            param.min_y = -RANGE; // _model.Properties.GetDouble(LLProperties.HiResMinY);
            param.max_y = RANGE; // _model.Properties.GetDouble(LLProperties.HiResMaxY);
            param.step_y = STEP_SIZE; // _model.Properties.GetDouble(LLProperties.HiResStepY);

            param.min_x = 0.0;
            param.max_x = 0.0;
            param.step_x = STEP_SIZE; // _model.Properties.GetDouble(LLProperties.HiResStepX);

            param.min_y += y_offset;
            param.max_y += y_offset;

            param.last_column = 0;
            param.first_column = 0;
            param.step_column = 1;
          
            _subordinate_sm = new LLSHiResScanStateMachine(_model, param);
            _subordinate_sm.Start();
            _y_measurements = new SortedDictionary<Coord, List<Measurement>>(_subordinate_sm.Measurements);
            _subordinate_sm = null;

            Fire(Trigger.Done);
        }

        private void Done()
        {
            base.EndStateFunction();
            Log.Info(String.Format("Locate Teachpoint: completed"));
        }

        private void Aborted()
        {
            base.AbortedStateFunction();

            // move to a "finished" safe location
            _model.MoveToPark();

            Log.Info(String.Format("Locate Teachpoint: aborted"));
        }
    }
}
