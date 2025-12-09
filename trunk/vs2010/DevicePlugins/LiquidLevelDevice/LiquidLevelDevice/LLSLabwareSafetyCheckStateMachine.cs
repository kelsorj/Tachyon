using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BioNex.Shared.Utils;
using log4net;
using System.Threading;
using BioNex.Shared.TechnosoftLibrary;
using System.Diagnostics;


namespace BioNex.LiquidLevelDevice
{
    internal class LLSLabwareSafetyCheckStateMachine : StateMachineWrapper<LLSLabwareSafetyCheckStateMachine.State, LLSLabwareSafetyCheckStateMachine.Trigger>
    {
        public enum State
        {
            Start,
            Init,
            MoveToSafeStart,
            StartZMotion,
            ReadSensors,
            StopZMotion,
            MoveToSafeEnd,
            Done,
            Aborted
        }

        public enum Trigger
        {
            Start,
            MoveToSafeStart,
            StartZMotion,
            ReadSensors,
            StopZMotion,
            MoveToSafeEnd,
            Done,
            Abort
        }

        ILLSensorModel _model;
        uint _sensor_count;
        ILevelSensor[] _sensors;
        bool _aborted;
        public bool IsAborted { get { return _aborted; } }

        bool _success;
        public bool Success { get { return _success; } }

        string _error_msg;
        public string ErrorMessage { get { return _error_msg; } }

        double _x_offset;
        double _y_offset;
        double _z_offset;
        double _labware_capture_offset;
        double _max_thickness;
        double _expected_thickness;
        double _detected_thickness;
        double _original_z_velocity;
        double _detection_zpos;

        public LLSLabwareSafetyCheckStateMachine(ILLSensorModel model, string labware_name)
            : base( State.Start, Trigger.Start, Trigger.Abort, true)
        {
            _model = model;
            _sensors = model.Sensors;
            _sensor_count = model.SensorCount;
            _success = false;

            // if we don't know the thickness, default to getting as far away from the labware as possible
            _max_thickness = model.Properties.GetDouble(LLProperties.Z_TP) - model.Properties.GetDouble(LLProperties.CaptureOffset);
            _x_offset = 0.0;
            _y_offset = -9.0;// model.Properties.GetDouble(LLProperties.CalibrationOffsetY);
            _z_offset = model.Properties.GetDouble(LLProperties.CaptureOffset);

            _expected_thickness = 0.0;
            _labware_capture_offset = 0.0;
            if (!string.IsNullOrWhiteSpace(labware_name))
            {
                int dummy_int;
                double dummy_dub;
                model.GetLabwareData(labware_name, out dummy_int, out dummy_int, out dummy_dub, out dummy_dub, out _expected_thickness, out dummy_dub);

                var labware_details = _model.VolumeMapDatabase.GetLabwareDetails(labware_name);
                _labware_capture_offset = labware_details.CaptureOffset;
            }

            SM.Configure(State.Start)
                .Permit(Trigger.Start, State.Init)
                .Permit(Trigger.Abort, State.Aborted);
            SM.Configure(State.Init)
                .Permit(Trigger.MoveToSafeStart, State.MoveToSafeStart)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(InitialState);
            SM.Configure(State.MoveToSafeStart)
                .Permit(Trigger.StartZMotion, State.StartZMotion)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(MoveToSafeStart);
            SM.Configure(State.StartZMotion)
                .Permit(Trigger.ReadSensors, State.ReadSensors)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(StartZMotion);
            SM.Configure(State.ReadSensors)
                .PermitReentry(Trigger.ReadSensors)
                .Permit(Trigger.StopZMotion, State.StopZMotion)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(ReadSensors);
            SM.Configure(State.StopZMotion)
                .Permit(Trigger.MoveToSafeEnd, State.MoveToSafeEnd)
                .Permit(Trigger.Done, State.Done)
                .OnEntry(StopZMotion);
            SM.Configure(State.MoveToSafeEnd)
                .Permit(Trigger.Done, State.Done)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(MoveToSafeEnd);
            SM.Configure(State.Done)
                .OnEntry(Done);
            SM.Configure(State.Aborted)
                .OnEntry(Aborted);
        }

        public new void Abort()
        {
            _aborted = true;
        }

        void ChangeState(Trigger trigger)
        {
            if (!_aborted)
                Fire(trigger);
            else
                Fire(Trigger.Abort);
        }

        void InitialState()
        {
            // intialize variables
            _model.SetSensitivity(LLSensorModelConsts.DefaultSensitivity);
            ChangeState(Trigger.MoveToSafeStart);
        }

        void MoveToSafeStart()
        {
            // move to a "start" safe location
            var z_correction = _y_offset * _model.ZYSlope;
            var epsilon = 0.000001; // our lame TechonosoftAxis soft limit check doesn't use an epsilon ... TODO fix that 
            _model.MoveRelativeToTeachpoint(_x_offset, -_y_offset, -(_z_offset + _max_thickness - z_correction - epsilon));
            ChangeState(Trigger.StartZMotion);
        }

        void StartZMotion()
        {
            _model.StartPeriodicRead();

            var SAFE_Z_VELOCITY = _model.ZAxisVelocity; // TODO -- set this to something slower probably
            _original_z_velocity = _model.ZAxisVelocity;
            _model.ZAxisVelocity = SAFE_Z_VELOCITY;
            // start an asynchronous move
            _model.MoveRelativeToTeachpoint(_x_offset, -_y_offset, -(_z_offset + _expected_thickness + _labware_capture_offset), false);
            ChangeState(Trigger.ReadSensors);
        }

        void ReadSensors()
        {
            double zpos = _model.Properties.GetDouble(LLProperties.Z_TP) - _model.GetAxisPositionMM(LLSensorModelConsts.ZAxis);
            double[] cal_reading = new double[_sensor_count];
            bool[] valid_read = new bool[_sensor_count];

            var action = new Action<int>(i =>
            {
                if (_sensors[i] == null)
                    return;
                try
                {
                    var reading = _sensors[i].GetReading(1, true); // returns 0..4095
                    cal_reading[i] = _sensors[i].GetCalibratedReading(reading);
                    valid_read[i] = true;
                }
                catch (LevelSensorException)
                {
                    // an exception here means we got a bad read from the sensor
                }
            });
            for (int i = 0; i < _sensor_count; i += _model.ParallelGroupSize)
                System.Threading.Tasks.Parallel.For(i, i + _model.ParallelGroupSize, action);

            var enabled_sensor_count = 0;
            var valid_count = 0;
            var avg = 0.0;
            for (int i = 0; i < _sensor_count; ++i)
            {
                if (_sensors[i] == null)
                    continue;
                ++enabled_sensor_count;
                if (!valid_read[i])
                    continue;
                ++valid_count;
                avg += cal_reading[i];
            }

            // if 50% of the sensors got a valid value, assume that it's accurate enough to detect plate presence
            var good_enough = enabled_sensor_count > 0 && valid_count >= (enabled_sensor_count / 2);
            if (!good_enough)
            {
                if (_model.ZAxis.ReadMotionCompleteFlag())
                {
                    _error_msg = string.Format("Collision Avoider: Failed to detect anything before reaching the labware height of {0:0.00} mm - aborting operation", _expected_thickness);
                    Log.ErrorFormat(_error_msg);
                    ChangeState(Trigger.StopZMotion);
                    return;
                }

                ChangeState(Trigger.ReadSensors);
                return;
            }
            avg /= valid_count;
            _detected_thickness = zpos - avg;
            if (_model.Properties.GetBool(LLProperties.CaptureFloorToZero) && _detected_thickness < 0.0)
                _detected_thickness = 0.0;

            // if we detect something bail out
            var delta = 1.0; // let it be within a millimeter of correct height
            _success = _detected_thickness <= (_expected_thickness + delta);
            _detection_zpos = zpos;

            ChangeState(Trigger.StopZMotion);
        }

        void StopZMotion()
        {
            _model.StopPeriodicRead();
            _model.ZAxisVelocity = _original_z_velocity;

            _model.ZAxis.SendTmlCommands("DINT;STOP;EINT");

            /*var z_correction = _y_offset * _model.ZYSlope;
            var epsilon = 0.000001; // our lame TechonosoftAxis soft limit check doesn't use an epsilon ... TODO fix that 
            _model.MoveRelativeToTeachpoint(_x_offset, -_y_offset, -(_z_offset + _max_thickness - z_correction - epsilon));*/
            if (!_success)
            {
                if (string.IsNullOrWhiteSpace(_error_msg))
                {
                    _error_msg = string.Format("Collision Avoider: Detected something at height of {0:0.00} mm, this is taller than the labware height of {1:0.00} mm - aborting operation", _detected_thickness, _expected_thickness);
                    Log.ErrorFormat(_error_msg);
                }
                ChangeState(Trigger.MoveToSafeEnd);
            }
            else
            {
                Log.InfoFormat("Collision Avoider: Detected something at height of {0:0.00} mm at zpos of {1:0.00} mm, this is within an acceptable distance to the labware height of {2:0.00} mm - continuing operation", _detected_thickness, _detection_zpos, _expected_thickness);
                ChangeState(Trigger.Done);
            }

        }

        void MoveToSafeEnd()
        {
            // move to a "finished" safe location in the ERROR CASE
            _model.MoveToPark();
            ChangeState(Trigger.Done);
        }

        void Done()
        {
            base.EndStateFunction();
        }

        void Aborted()
        {
            base.EndStateFunction();
        }
    }
}