using System;
using System.Threading;
using BioNex.Shared.Utils;
using System.Collections.Generic;
using System.Diagnostics;

namespace BioNex.LiquidLevelDevice
{
    internal class LLSFastHiResScanStateMachine : StateMachineWrapper<LLSFastHiResScanStateMachine.State, LLSFastHiResScanStateMachine.Trigger>
    {
        public enum State
        {
            Start,
            Init,
            MoveToSafeStart,
            AdvanceToNextSample,
            SensorsOn,
            DriveAcrossRow,
            WaitForMotionComplete,
            SensorsOff,
            Branch,
            MoveToSafeEnd,
            Done,
            Aborted
        }

        public enum Trigger
        {
            Start,
            MoveToSafeStart,
            AdvanceToNextSample,
            SensorsOn,
            DriveAcrossRow,
            WaitForMotionComplete,
            SensorsOff,
            Branch,
            MoveToSafeEnd,
            Done,
            Abort
        }

        ILLSensorModel _model;

        // sensors, provide by client
        uint _sensor_count;
        ILevelSensor[] _sensors;

        double _sensor_spacing;

        // We walk from start to stop, stop must be less than start
        int _sample_x;      // index into sample x offsets
        int _x_samples; // total sample count in x direction


        LLSHiResScanStateMachine.HiResScanParams _params;
        Stopwatch _capture_start;

        IDictionary<Coord, List<Measurement>> _measurements;
        public IDictionary<Coord, List<Measurement>> Measurements { get { return _measurements; } }

        public LLSFastHiResScanStateMachine(ILLSensorModel model, LLSHiResScanStateMachine.HiResScanParams parameters)
            : base( State.Start, Trigger.Start, Trigger.Abort, true)
        {
            _model = model;
            _params = parameters;

            _x_samples = (int)((_params.max_x - _params.min_x) / _params.step_x);

            _sensors = model.Sensors;
            _sensor_count = model.SensorCount;
            _sensor_spacing = 9.0;

            _measurements = new SortedDictionary<Coord, List<Measurement>>();

            SM.Configure(State.Start)
                .Permit(Trigger.Start, State.Init)
                .Permit(Trigger.Abort, State.Aborted);
            SM.Configure(State.Init)
                .Permit(Trigger.MoveToSafeStart, State.MoveToSafeStart)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(InitialState);
            SM.Configure(State.MoveToSafeStart)
                .Permit(Trigger.AdvanceToNextSample, State.AdvanceToNextSample)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(MoveToSafeStart);
            SM.Configure(State.AdvanceToNextSample)
                .Permit(Trigger.SensorsOn, State.SensorsOn)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(AdvanceToNextSample);
            SM.Configure(State.SensorsOn)
                .Permit(Trigger.DriveAcrossRow, State.DriveAcrossRow)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(SensorsOn);
            SM.Configure(State.DriveAcrossRow)
                .Permit(Trigger.WaitForMotionComplete, State.WaitForMotionComplete)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(DriveAcrossRow);
            SM.Configure(State.WaitForMotionComplete)
                .Permit(Trigger.SensorsOff, State.SensorsOff)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(WaitForMotionComplete);
            SM.Configure(State.SensorsOff)
                .Permit(Trigger.Branch, State.Branch)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(SensorsOff);
            SM.Configure(State.Branch)
                .Permit(Trigger.AdvanceToNextSample, State.AdvanceToNextSample)
                .Permit(Trigger.MoveToSafeEnd, State.MoveToSafeEnd)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(Branch);
            SM.Configure(State.MoveToSafeEnd)
                .Permit(Trigger.Done, State.Done)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(MoveToSafeEnd);
            SM.Configure(State.Done)
                .OnEntry(Done);
            SM.Configure(State.Aborted)
                .OnEntry(Aborted);
        }

        public void ManualAbort()
        {
            Abort();
        }

        private void InitialState()
        {
            _capture_start = Stopwatch.StartNew();
            _sample_x = 0;

            _measurements.Clear();
            _model.FireCaptureStartEvent(false, _params.labware);

            // set the measurement sensitivity based on labware
            if (_params.labware != "")
                _model.SetSensitivity(_model.VolumeMapDatabase.GetLabwareDetails(_params.labware).Sensitivity);
            else
                _model.SetSensitivity(LLSensorModelConsts.DefaultSensitivity);

            Fire(Trigger.MoveToSafeStart);
        }

        private void MoveToSafeStart()
        {
            _model.MoveRelativeToTeachpoint(0, 0, -(_params.z_offset + _params.thickness));

            Fire(Trigger.AdvanceToNextSample);
        }

        private void AdvanceToNextSample()
        {
            double x_offset = _params.min_x + _sample_x * _params.step_x + 4.5 - _params.radius; // start from MIN side
            int column = (_sample_x % 2 == 0) ? _params.last_column : _params.first_column;
            int direction = (_sample_x % 2 == 0) ? 1 : -1;
            double y_offset = -(column * _params.column_spacing) + (direction * _params.column_spacing / 2.0) + 4.5 - _params.radius;

            var original_z_correction = _model.ZYSlope;
            _model.ZYSlope = 0.0;
            _model.MoveRelativeToTeachpoint(x_offset, y_offset, -(_params.z_offset + _params.thickness));
            _model.ZYSlope = original_z_correction;

            Fire(Trigger.SensorsOn);
        }

        private void SensorsOn()
        {
            _model.StartPeriodicRead();
            Fire(Trigger.DriveAcrossRow);
        }

        double _original_velocity = double.NaN;
        private void DriveAcrossRow()
        {
            double x_offset = _params.min_x + _sample_x * _params.step_x + 4.5 - (_params.row_spacing / 2.0); // start from MIN side
            int column = (_sample_x % 2 == 0) ? _params.first_column : _params.last_column;
            int direction = (_sample_x % 2 == 0) ? -1 : 1;
            double y_offset = -(column * _params.column_spacing) + (direction * _params.column_spacing / 2.0) + 4.5 - (_params.column_spacing / 2.0);

            _original_velocity = _model.YAxisVelocity;
            _model.YAxisVelocity = _model.Properties.GetDouble(LLProperties.HiResFastScanVelocity);
            var original_z_correction = _model.ZYSlope;
            _model.ZYSlope = 0.0;
            _model.MoveRelativeToTeachpoint(x_offset, y_offset, -(_params.z_offset + _params.thickness), false);
            _model.ZYSlope = original_z_correction;

            Fire(Trigger.WaitForMotionComplete);
        }

        private void WaitForMotionComplete()
        {
            const int min_batch_size = 80;
            var measurements = new List<Measurement>();
            var last_y_position = double.MinValue;
            var min_delta = _model.Properties.GetDouble(LLProperties.HiResStepY) / 4.0;
            var y_velocity = _model.Properties.GetDouble(LLProperties.HiResFastScanVelocity);
            var y_acceleration = _model.YAxisAcceleration;
            var direction = (_sample_x % 2 == 0) ? -1 : 1;

            while(!_model.YAxis.ReadMotionCompleteFlag())
            {
                // get position
                var x_position = _model.GetAxisPositionMM(LLSensorModelConsts.XAxis);
                var y_position = _model.GetAxisPositionMM(LLSensorModelConsts.YAxis);
                var z_position = _model.Properties.GetDouble(LLProperties.Z_TP) - _model.GetAxisPositionMM(LLSensorModelConsts.ZAxis);
                var position_timestamp = Stopwatch.StartNew();

                double delta = Math.Abs(last_y_position - y_position);
                if (delta < min_delta) // toss out data if it's coming too soon
                    continue;
                last_y_position = y_position; // last position we successfully sampled at

                // get reading
                var cal_reading = new double[_sensor_count];
                var timestamps = new double[_sensor_count];

                var action = new Action<int>(i =>
                {
                    if (_sensors[i] == null)
                        return;
                    try
                    {
                        var reading = _sensors[i].GetReading(1, true); // returns 0..4095
                        timestamps[i] = position_timestamp.Elapsed.TotalSeconds;

                        cal_reading[i] = _sensors[i].GetCalibratedReading(reading);
                        if (_params.floor_to_zero && cal_reading[i] > z_position)
                            cal_reading[i] = z_position;
                    }
                    catch (LevelSensorException)
                    {
                        // an exception here means we got a bad read from the sensor
                    }
                });
                for (int i = 0; i < _sensor_count; i += _model.ParallelGroupSize)
                    System.Threading.Tasks.Parallel.For(i, i + _model.ParallelGroupSize, action);

                // log it
                for (int i = 0; i < _sensor_count; ++i)
                {
                    if (_sensors[i] == null)
                        continue;
                    var value = z_position - cal_reading[i];

                    double x_dev = 0.0;
                    double y_dev = 0.0;
                    if (_params.add_deviation)
                    {
                        // deviation -- sign is oriented such that we subtract deviation to move to that position, so add deviation to tell where the sensor is relative to current position
                        x_dev = _model.Properties.GetDouble(LLProperties.index(LLProperties.XDeviation, i));
                        y_dev = _model.Properties.GetDouble(LLProperties.index(LLProperties.YDeviation, i));
                    }
                    
                    // linear interpolate position, neglecting acceleration 
                    var t = timestamps[i];
                    //var fudge = 0.01; // Mode A factor
                    var fudge = 0.0341; // this is a processing delay factor measured at sensor averaging mode D
                                        // it appears that there is a delay between when the sensor acquires data
                                        // and when it reports the data.  It definitely varies with Averaging mode (smaller value for mode A)
                    var sample_y = y_position + ((timestamps[i] - fudge) * y_velocity * direction);  

                    measurements.Add(new Measurement(i, 0, 0, x_position - i * _sensor_spacing + x_dev, sample_y + y_dev, value));
                    if (measurements.Count >= min_batch_size)
                    {
                        _model.FireCaptureProgressEvent(measurements);
                        measurements.Clear();
                    }
                }
            }

            // line complete, draw all the remaining measurements
            if( measurements.Count > 0)
                _model.FireCaptureProgressEvent(measurements);

            Fire(Trigger.SensorsOff);
        }

        private void SensorsOff()
        {
            _model.StopPeriodicRead();
            _model.YAxisVelocity = _original_velocity;
            Fire(Trigger.Branch);
        }

        private void Branch()
        {
            if (++_sample_x <= _x_samples)
            {
                Fire(Trigger.AdvanceToNextSample);
                return;
            }
            Fire(Trigger.MoveToSafeEnd);
        }

        private void MoveToSafeEnd()
        {
            Log.Info(String.Format("Run: {0} : sample capture complete, duration: {1} sec", _model.RunCounter, (_capture_start.Elapsed).TotalSeconds));
            Fire(Trigger.Done);
        }

        private void Done()
        {
            base.EndStateFunction();
            Log.Info(String.Format("Run: {0} : complete, duration: {1} sec", _model.RunCounter, (_capture_start.Elapsed).TotalSeconds));
            _capture_start.Stop();

            if (_params.return_to_park)
                _model.MoveToPark();

            _model.FireCaptureStopEvent();
        }

        private void Aborted()
        {
            base.AbortedStateFunction();

            _model.StopPeriodicRead();
            if( !double.IsNaN(_original_velocity))
                _model.YAxisVelocity = _original_velocity;

            Log.Info(String.Format("Run: {0} : aborted", _model.RunCounter));
            if (_capture_start != null) // null if we got aborted before starting the timer
                _capture_start.Stop();

            _model.MoveToPark();

            _model.FireCaptureStopEvent();
        }
    }
}
