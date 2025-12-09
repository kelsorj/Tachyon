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
    internal class LLSCalibrationStateMachine : StateMachineWrapper<LLSCalibrationStateMachine.State, LLSCalibrationStateMachine.Trigger>
    {
        public enum State
        {
            Start,
            Init,
            MoveToSafeStart,
            SetSensitivity,
            MoveToCapturePoint,
            ReadSensorAtPoint,
            Branch,
            MoveToSafeEnd,
            Done,
            Aborted
        }

        public enum Trigger
        {
            Start,
            MoveToSafeStart,
            SetSensitivity,
            MoveToCapturePoint,
            ReadSensorAtPoint,
            Branch,
            MoveToSafeEnd,
            Done,
            Abort
        }

        ILLSensorModel _model;
        uint _sensor_count;
        ILevelSensor[] _sensors;
        bool _aborted;
        public bool IsAborted { get { return _aborted; } }

        double _x_offset;
        double _y_offset;
        double _z_offset;

        double _max_z;

        double[][] _height;       // Z height where sampling occured, all sensors share the same Z

        const int _mode_count = 4;   // number of modes to generate calibration for
        string[] _modes = {"A", "B", "C", "D"};
        int _mode;
        int _samples_count;      // number of samples per point
        int _point_count;       // number of points for linear fit
        int _point;             // which point are we currently sampling for (0 <= point < _point_count)

        double[][][] _values;     // value averaged at sample height, i=sensor, j=height 
        bool[][][] _good_point;   // whether this sensor had a valid average at the indicated height, i=sensor, j=height
        
        public LLSCalibrationStateMachine(ILLSensorModel model)
            : base( State.Start, Trigger.Start, Trigger.Abort, true)
        {
            _model = model;
            _sensors = model.Sensors;
            _sensor_count = model.SensorCount;

            _x_offset = 0.0;
            _y_offset = _model.Properties.GetDouble(LLProperties.CalibrationOffsetY);
            _z_offset = _model.Properties.GetDouble(LLProperties.CaptureOffset);
            _samples_count = _model.Properties.GetInt(LLProperties.CalibrationSamples);
            _point_count = _model.Properties.GetInt(LLProperties.CalibrationPoints);
            _point = 0;
            _mode = 0;

            _values = new double[_mode_count][][];
            _good_point = new bool[_mode_count][][];
            _height = new double[_mode_count][];

            for( int i=0; i<_mode_count; ++i){
                _values[i] = new double[_sensor_count][];
                _good_point[i] = new bool[_sensor_count][];
                _height[i] = new double[_point_count];

                for( int j=0; j<_sensor_count; ++j){
                    _values[i][j] = new double[_point_count];
                    _good_point[i][j] = new bool[_point_count];
                }
            }

            SM.Configure(State.Start)
                .Permit(Trigger.Start, State.Init)
                .Permit(Trigger.Abort, State.Aborted);
            SM.Configure(State.Init)
                .Permit(Trigger.MoveToSafeStart, State.MoveToSafeStart)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(InitialState);
            SM.Configure(State.MoveToSafeStart)
                .Permit(Trigger.SetSensitivity, State.SetSensitivity)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(MoveToSafeStart);
            SM.Configure(State.SetSensitivity)
                .Permit(Trigger.MoveToCapturePoint, State.MoveToCapturePoint)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(SetSensitivity);
            SM.Configure(State.MoveToCapturePoint)
                .Permit( Trigger.ReadSensorAtPoint, State.ReadSensorAtPoint)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(MoveToCapturePoint);
            SM.Configure(State.ReadSensorAtPoint)
                .Permit(Trigger.Branch, State.Branch)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(ReadSensorAtPoint);
            SM.Configure(State.Branch)
                .Permit(Trigger.MoveToCapturePoint, State.MoveToCapturePoint)
                .Permit(Trigger.SetSensitivity, State.SetSensitivity)
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
            ChangeState(Trigger.MoveToSafeStart);
        }

        void MoveToSafeStart()
        {
            // move to a "start" safe location
            //_model.MoveRelativeToTeachpoint(_x_offset, -_y_offset, -_z_offset);
            ChangeState(Trigger.SetSensitivity);
        }

        void SetSensitivity()
        {
            switch (_mode)
            {
                case 0: _max_z = _model.Properties.GetDouble(LLProperties.CalibrationMaxZA); break;
                case 1: _max_z = _model.Properties.GetDouble(LLProperties.CalibrationMaxZB); break;
                case 2: _max_z = _model.Properties.GetDouble(LLProperties.CalibrationMaxZC); break;
                case 3: _max_z = _model.Properties.GetDouble(LLProperties.CalibrationMaxZD); break;
            }
            
            _model.SetSensitivity(_modes[_mode]);
            _point = 0;
            ChangeState(Trigger.MoveToCapturePoint);
        }

        void MoveToCapturePoint()
        {
            // interpolate Z position
            var inc = (_max_z - _z_offset) / (_point_count - 1);
            var z = _z_offset + inc * _point;
            _model.MoveRelativeToTeachpoint(_x_offset, -_y_offset, -z);
            ChangeState(Trigger.ReadSensorAtPoint);
        }

        void ReadSensorAtPoint()
        {
            double tp_z = _model.Properties.GetDouble(LLProperties.Z_TP);
            _height[_mode][_point] = tp_z - _model.GetAxisPositionMM(LLSensorModelConsts.ZAxis);

            for (int i = 0; i < _sensor_count; ++i)
            {
                if (_sensors[i] == null)
                    continue;

                double sum = 0.0;
                int count = 0;
                for (int j = 0; j < _samples_count; ++j)
                {
                    try
                    {
                        sum += _sensors[i].GetReading();
                        ++count;
                    }
                    catch (LevelSensorException)
                    {
                        // an exception here means we got a bad read from the sensor
                    }
                }
                _values[_mode][i][_point] = count > 0 ? sum / count : 0;
                _good_point[_mode][i][_point] = count != 0;
                Thread.Sleep(_model.InterSensorDelayms);
            }

            ChangeState(Trigger.Branch);
        }

        void Branch()
        {
            if (++_point < _point_count)
            {
                ChangeState(Trigger.MoveToCapturePoint);
                return;
            }

            if( ++_mode < _mode_count)
            {
                ChangeState(Trigger.SetSensitivity);
                return;
            }
            ChangeState(Trigger.MoveToSafeEnd);
        }

        void MoveToSafeEnd()
        {
            // move to a "finished" safe location
            _model.MoveToPark();

            ChangeState(Trigger.Done);
        }

        void Done()
        {
            base.EndStateFunction();


            bool all_points_good = true;
            for( int i = 0; i< _mode_count; ++i)
                for( int j = 0; j  < _sensor_count; ++j)
                    for (int k = 0; k < _point_count; ++k)
                    {
                        if (_sensors[j] == null)
                            continue;
                        all_points_good &= _good_point[i][j][k];
                        var msg = String.Format("Sensor: {0} : Mode: {1} : Height: {2:0.00} : Value: {3:0.00} : Quality: {4}",
                            j + 1,
                            _modes[i],
                            _height[i][k],
                            _values[i][j][k],
                            _good_point[i][j][k] ? "good" : "bad");
                        if (_good_point[i][j][k])   Log.Info(msg);
                        else                        Log.Error(msg);
                    }

            for (int i = 0; i < _mode_count; ++i)
            {
                for (int j = 0; j < _sensor_count; ++j)
                {
                    if (_sensors[j] == null)
                        continue;
                    var mode = _modes[i];
                    _sensors[j].Calibrate(_height[i], _values[i][j], mode);
                    var current = _sensors[j].CurrentMode;

                    try
                    {
                        _sensors[j].CurrentMode = mode;
                        var cal = _sensors[j].Calibration;
                        var message = String.Format("Sensor: {0} : Mode: {1} : Slope: {2:0.00} : Intercept: {3:0.00} : Confidence: {4:0.00}",
                                                j + 1,
                                                mode,
                                                cal.Slope,
                                                cal.Intercept,
                                                cal.Correlation);

                        if (cal.Correlation < 0.95)
                            Log.Warn(message);
                        else
                            Log.Info(message);
                    }
                    finally
                    {
                        _sensors[j].CurrentMode = current;
                    }
                }
            }
            if (all_points_good)
            {
                Log.Info("Calibration complete, writing calibration data!");
                foreach (var s in _sensors)
                    if (s != null)
                        s.SaveCalibration(((LLSensorModel)_model).ConfigPath);
            }
            else
            {
                Log.Error("Calibration completed with warnings, previous calibration restored.");
                foreach (var s in _sensors)
                    if (s != null)
                        s.LoadCalibration(((LLSensorModel)_model).ConfigPath);
            }

        }

        void Aborted()
        {
            base.EndStateFunction();

            // move to a "finished" safe location
            _model.MoveToPark();

            Log.Info("Calibration aborted, no change to calibration recorded");
        }
   }
}