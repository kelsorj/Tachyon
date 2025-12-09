using System;
using BioNex.Shared.Utils;
using System.Collections.Generic;

namespace BioNex.LiquidLevelDevice
{
    internal class LLSXArcCorrectionScanStateMachine : StateMachineWrapper<LLSXArcCorrectionScanStateMachine.State, LLSXArcCorrectionScanStateMachine.Trigger>
    {
        public enum State
        {
            Start,
            Init,
            RunHiResScan,
            Branch,
            Done,
            Aborted
        }

        public enum Trigger
        {
            Start,
            RunHiResScan,
            Branch,
            Done,
            Abort
        }

        ILLSensorModel _model;

        IList<IDictionary<Coord, List<Measurement>>> _measurements;
        public IList<IDictionary<Coord, List<Measurement>>> Measurements { get { return _measurements; } }

        const double X_STEP_SIZE = 4.5;
        const double X_RANGE = 4.5;
        const double Y_STEP_SIZE = 0.01;
        const double Y_RANGE = 3;

        int _samples;
        int _sample;

        public LLSXArcCorrectionScanStateMachine(ILLSensorModel model)
            : base( State.Start, Trigger.Start, Trigger.Abort, true)
        {
            _model = model;
            _measurements = new List<IDictionary<Coord, List<Measurement>>>();

            SM.Configure(State.Start)
                .Permit(Trigger.Start, State.Init)
                .Permit(Trigger.Abort, State.Aborted);
            SM.Configure(State.Init)
                .Permit(Trigger.RunHiResScan, State.RunHiResScan)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(InitialState);
            SM.Configure(State.RunHiResScan)
                .Permit(Trigger.Branch, State.Branch)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(RunHiResScan);
            SM.Configure(State.Branch)
                .Permit(Trigger.RunHiResScan, State.RunHiResScan)
                .Permit(Trigger.Done, State.Done)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(Branch);
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
            _samples = (int)((X_RANGE - -X_RANGE) / X_STEP_SIZE); // we will sample _samples + 1 times!
            _sample = 0;
            Fire(Trigger.RunHiResScan);
        }

        LLSHiResScanStateMachine _subordinate_sm;
        private void RunHiResScan()
        {           
            var x_offset = -X_RANGE + _sample * X_STEP_SIZE;
            var y_offset = _model.Properties.GetDouble(LLProperties.LocateTeachpointFeatureToFeatureY);

            var param = new LLSHiResScanStateMachine.HiResScanParams(_model, "", _sample == _samples, true);
            param.min_x = x_offset; // hi res scan will start at min_x but won't move in x if min_x == max_x
            param.max_x = x_offset;   
            param.step_x = X_STEP_SIZE;

            param.min_y = -Y_RANGE + y_offset;
            param.max_y = Y_RANGE + y_offset;
            param.step_y = Y_STEP_SIZE;

            param.last_column = 0;
            param.first_column = 0;
            param.step_column = 1;

            _subordinate_sm = new LLSHiResScanStateMachine(_model, param);
            _subordinate_sm.Start();
            _measurements.Add(new SortedDictionary<Coord, List<Measurement>>(_subordinate_sm.Measurements));
            _subordinate_sm = null;

            Fire(Trigger.Branch);
        }

        private void Branch()
        {
            if (++_sample <= _samples)
            {
                Fire(Trigger.RunHiResScan);
                return;
            }

            Fire(Trigger.Done);
        }

        private void Done()
        {
            base.EndStateFunction();
            Log.Info(String.Format("X Arc Correction Scan: completed"));
        }

        private void Aborted()
        {
            base.AbortedStateFunction();

            // move to a "finished" safe location
            _model.MoveToPark();
            
            Log.Info(String.Format("X Arc Correction Scan: aborted"));
        }
    }
}
