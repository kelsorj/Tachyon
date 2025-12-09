using System;
using System.Threading;
using BioNex.Shared.Utils;
using System.Collections.Generic;
using System.Diagnostics;

namespace BioNex.LiquidLevelDevice
{
    internal class LLSHiResScanStateMachine : StateMachineWrapper<LLSHiResScanStateMachine.State, LLSHiResScanStateMachine.Trigger>
    {
        public enum State
        {
            Start,
            Init,
            MoveToSafeStart,
            MoveToNextRow,
            MoveToNextColumn,
            AdvanceToNextSample,
            MoveWithinWell,
            ReadSensors,
            Branch,
            MoveToSafeEnd,
            Done,
            Aborted
        }

        public enum Trigger
        {
            Start,
            MoveToSafeStart,
            MoveToNextRow,
            MoveToNextColumn,
            AdvanceToNextSample,
            MoveWithinWell,
            ReadSensors,
            Branch,
            MoveToSafeEnd,
            Done,
            Abort
        }

        public struct HiResScanParams
        {
            public string labware;
            public int columns; // irrelevant for now, since the min / max column setting trump this, but I left it in for symmetry w/ the Capture command
            public int labware_rows;    // likewise
            public int rows; // how many scan rows are there? (labware rows / 8)
            public double column_spacing;
            public double row_spacing;
            public double thickness;
            public double radius;

            public double z_offset;
            public double min_x;
            public double max_x;
            public double step_x;
            public double min_y;
            public double max_y;
            public double step_y;
            public int first_column;
            public int last_column;
            public int step_column;
            public int settle_time_ms;
            public bool return_to_park;
            public bool add_deviation;
            public bool floor_to_zero;
            public bool use_plate_offset;

            public HiResScanParams(ILLSensorModel model, string labware_name, bool park=true, bool add_deviations=false, bool flr_to_zero=false, bool use_plt_offset=false)
            {
                labware = labware_name;
                columns = 12;               // irrelevant for jig at the moment
                labware_rows = 8;           // likewise
                column_spacing = 9.0;       // spacing between well columns -- used by hires scan sm to translate column number into Y position
                row_spacing = 9.0;          // spacing between well rows -- used by hires scan sm to calculate start x position
                thickness = 0.0;            // jig is part of device stage
                radius = 4.5;
                var labware_offset = 0.0;

                // Now that we are using plates check to get the properties of the plate
                if (labware_name != "")
                {
                    model.GetLabwareData(labware_name, out columns, out labware_rows, out column_spacing, out row_spacing, out thickness, out radius);
                    labware_offset = model.VolumeMapDatabase.GetLabwareDetails(labware_name).CaptureOffset;
                }

                z_offset = model.Properties.GetDouble(LLProperties.CaptureOffset) + labware_offset;
                min_x = model.Properties.GetDouble(LLProperties.HiResMinX);
                max_x = model.Properties.GetDouble(LLProperties.HiResMaxX);
                step_x = model.Properties.GetDouble(LLProperties.HiResStepX);

                min_y = model.Properties.GetDouble(LLProperties.HiResMinY);
                max_y = model.Properties.GetDouble(LLProperties.HiResMaxY);
                step_y = model.Properties.GetDouble(LLProperties.HiResStepY);

                rows = labware_rows / 8; // -- is there a more sensible way to derive this base on spacings?
                first_column = model.Properties.GetInt(LLProperties.HiResMaxC) - 1;
                last_column= model.Properties.GetInt(LLProperties.HiResMinC) - 1;
                step_column = model.Properties.GetInt(LLProperties.HiResStepC);

                settle_time_ms = model.Properties.GetInt(LLProperties.SettleTime);

                return_to_park = park;

                add_deviation = add_deviations;

                floor_to_zero = flr_to_zero;

                use_plate_offset = use_plt_offset;
            }
        }

        ILLSensorModel _model;

        const int _average_sense_time_ms = 65; // for simulated sensing

        // sensors, provide by client
        uint _sensor_count;
        ILevelSensor[] _sensors;

        // state variables
        bool[] _read_error;   // did we get a valid read on this sample

        double _sensor_spacing;

        // We walk from start to stop, stop must be less than start
        int _sample_x;      // index into sample x & y offsets
        int _sample_y;
        int _column;        // current column
        int _row;           // current row

        int _x_samples;     // total sample count in x & y directions
        int _y_samples;

        HiResScanParams _params;
        Stopwatch _capture_start;

        IDictionary<Coord, List<Measurement>> _measurements;
        public IDictionary<Coord, List<Measurement>> Measurements { get { return _measurements; } }

        public LLSHiResScanStateMachine(ILLSensorModel model, HiResScanParams parameters)
            : base( State.Start, Trigger.Start, Trigger.Abort, true)
        {
            _model = model;
            _params = parameters;

            _x_samples = (int)((_params.max_x - _params.min_x) / _params.step_x);
            _y_samples = (int)((_params.max_y - _params.min_y) / _params.step_y);
            //if (minx == maxx) _x_samples = 0; // only take a single sample if max & min are equal
            //if (miny == maxy) _y_samples = 0;

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
                .Permit(Trigger.MoveToNextRow, State.MoveToNextRow)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(MoveToSafeStart);
            SM.Configure(State.MoveToNextRow)
                .Permit(Trigger.MoveToNextColumn, State.MoveToNextColumn)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(MoveToNextRow);
            SM.Configure(State.MoveToNextColumn)
                .Permit(Trigger.AdvanceToNextSample, State.AdvanceToNextSample)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(MoveToNextColumn);
            SM.Configure(State.AdvanceToNextSample)
                .Permit(Trigger.MoveWithinWell, State.MoveWithinWell)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(AdvanceToNextSample);
            SM.Configure(State.MoveWithinWell)
                .Permit(Trigger.ReadSensors, State.ReadSensors)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(MoveWithinWell);
            SM.Configure(State.ReadSensors)
                .Permit(Trigger.Branch, State.Branch)
                .Permit(Trigger.Abort, State.Aborted)
                .OnEntry(ReadSensor);
            SM.Configure(State.Branch)
                .Permit(Trigger.MoveWithinWell, State.MoveWithinWell)
                .Permit(Trigger.AdvanceToNextSample, State.AdvanceToNextSample)
                .Permit(Trigger.MoveToNextColumn, State.MoveToNextColumn)
                .Permit(Trigger.MoveToNextRow, State.MoveToNextRow)
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
            _row = 0;
            _read_error = new bool[_sensor_count];
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
            //_model.MoveToPark(); --> skip this for now, just a time kill since first move is always to a safe position above plate now
            //_model.MoveRelativeToTeachpoint(0, 0, -SAMPLE_HEIGHT_MM);
            Fire(Trigger.MoveToNextRow);
        }

        private void MoveToNextRow()
        {
            _column = _params.rows == 1 || _row % 2 == 1 ? _params.first_column : _params.last_column;
            Fire(Trigger.MoveToNextColumn);
        }

        private void MoveToNextColumn()
        {
            // reset sample position
            _sample_x = 0;
            _sample_y = _y_samples;

            var pdy = _params.use_plate_offset ? _model.PlateDY : 0.0;

            // move to column with X at zero offset to avoid hitting the sides of the opening
            double y_offset = 4.5 - _params.radius - _column * _params.column_spacing - _params.min_y - _sample_y * _params.step_y - pdy;
            _model.MoveRelativeToTeachpoint(0, y_offset, -(_params.z_offset + _params.thickness));

            Fire(Trigger.AdvanceToNextSample);
        }

        private void AdvanceToNextSample()
        {
            for (int i = 0; i < _sensor_count; ++i)
                _read_error[i] = true; // read error flags get unset on valid read

            Fire(Trigger.MoveWithinWell);
        }

        private void MoveWithinWell()
        {
            var pdx = _params.use_plate_offset ? _model.PlateDX : 0.0;
            var pdy = _params.use_plate_offset ? _model.PlateDY : 0.0;

            double x_offset = 4.5 - _params.radius - _row * _params.row_spacing + _params.min_x + _sample_x * _params.step_x - pdx;
            double y_offset = 4.5 - _params.radius - _column * _params.column_spacing - _params.min_y - _sample_y * _params.step_y - pdy;

            Log.Info(string.Format("Moving to column: {0} : sample_x: {1} : sample_y: {2}", _column + 1, _sample_x + 1, _sample_y + 1));

            _model.MoveRelativeToTeachpoint(x_offset, y_offset, -(_params.z_offset + _params.thickness));
            if (_params.settle_time_ms > 0)
                Thread.Sleep(_params.settle_time_ms);

            Fire(Trigger.ReadSensors);
        }

        private void ReadSensor()
        {
            string[] log_messages = new string[_sensor_count];
            bool[] retry = new bool[_sensor_count];
            int[] reading = new int[_sensor_count];
            double[] cal_reading = new double[_sensor_count];
            bool[] read_channel = new bool[_sensor_count];
            _read_error.CopyTo(read_channel, 0);

            // Remeber, this is the CORRECTED X offset if we haven't reset the XYSlope!
            double x_position = _model.GetAxisPositionMM(LLSensorModelConsts.XAxis) - _model.Properties.GetDouble(LLProperties.X_TP);

            // subtract off y_correction so measurement is in correct position
            double y_correction = (x_position * x_position * _model.XArcCorrection[2] + x_position * _model.XArcCorrection[1]);// + _model.XArcCorrection[0]);
            double y_position = _model.GetAxisPositionMM(LLSensorModelConsts.YAxis) - _model.Properties.GetDouble(LLProperties.Y_TP) - y_correction;

            // Now subtract off x_correction so measurement is in correct position
            double x_correction = y_position * _model.XYSlope;
            x_position -= x_correction;

            // subtract off z correction so measurement is in correct position -- use corrected y position as basis
            double z_correction = (y_position + y_correction) * _model.ZYSlope;
            double z_position = _model.Properties.GetDouble(LLProperties.Z_TP) - _model.GetAxisPositionMM(LLSensorModelConsts.ZAxis) - z_correction;

            var action = new Action<int>( i =>
            {
                if (_sensors[i] == null)
                {
                    Thread.Sleep(_average_sense_time_ms); // simulate sensor
                    cal_reading[i] = z_position; // shove a zero value into the sensor that is being ignored
                    _read_error[i] = false;
                    return;
                }

                double x_dev = 0.0;
                double y_dev = 0.0;
                if (_params.add_deviation)
                {
                    // deviation -- sign is oriented such that we subtract deviation to move to that position, so add deviation to tell where the sensor is relative to current position
                    x_dev = _model.Properties.GetDouble(LLProperties.index(LLProperties.XDeviation, i));
                    y_dev = _model.Properties.GetDouble(LLProperties.index(LLProperties.YDeviation, i));
                }

                var read_start = Stopwatch.StartNew();
                try
                {
                    if (!read_channel[i])
                        return;

                    reading[i] = _sensors[i].GetReading();   // returns 0..4095
                    retry[i] = _sensors[i].Retries > 1;             // did we retry this read?
                    _read_error[i] = false;                         // reset error flag on valid read

                    cal_reading[i] = _sensors[i].GetCalibratedReading(reading[i]);
                    if (_params.floor_to_zero && cal_reading[i] > z_position)
                        cal_reading[i] = z_position;

                    log_messages[i] = String.Format("Run: {0} : Column: {1} : Sensor: {2} : X: {3:0.00} : Y: {4:0.00} : Z: {5:0.00} : Raw: {6:0.00} : Calibrated: {7:0.00} : Duration: {8:0.00} ms",
                        _model.RunCounter, _column + 1, i + 1, x_position - i * _sensor_spacing + x_dev, y_position + y_dev, z_position, reading[i], z_position - cal_reading[i], (read_start.Elapsed).TotalMilliseconds);

                    _model.FireIntegerSensorReadingReceivedEvent((int)i, reading[i]);
                }
                catch (LevelSensorException e)
                {
                    // an exception here means we got a bad read from the sensor
                    log_messages[i] = String.Format("Run: {0} : Column: {1} : Sensor: {2} : X: {3:0.00} : Y: {4:0.00} : Z: {5:0.00} : Error: {6}",
                        _model.RunCounter, _column + 1, i + 1, x_position - i * _sensor_spacing + x_dev, y_position + y_dev, z_position, e.Message);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
                finally
                {
                    read_start.Stop();
                }
            });
            
            for (int i = 0; i < _sensor_count; i += _model.ParallelGroupSize)
                System.Threading.Tasks.Parallel.For(i, i + _model.ParallelGroupSize, action);


            for (int i = 0; i < _sensor_count; i++)
            {
                if (_sensors[i] == null)
                    continue;
                if (log_messages[i] == null) // empty string if we were retrying some other channel
                    continue;
                if (_read_error[i])
                    Log.Error(log_messages[i]);
                else if (retry[i])
                    Log.Warn(log_messages[i]);
                else
                    Log.Info(log_messages[i]);
            }

            var new_measurements = new List<Measurement>();
            for (int i = 0; i < _sensor_count; ++i)
            {
                if (_sensors[i] == null)
                    continue;
                if (_read_error[i] == true)
                    continue;
                if (read_channel[i])
                {
                    double x_dev = 0.0;
                    double y_dev = 0.0;
                    if (_params.add_deviation)
                    {
                        x_dev = _model.Properties.GetDouble(LLProperties.index(LLProperties.XDeviation, i));
                        y_dev = _model.Properties.GetDouble(LLProperties.index(LLProperties.YDeviation, i));
                    }

                    var value = _read_error[i] ? 0.0
                        : z_position - cal_reading[i];

                    var measurement = new Measurement(i, _row, _column, x_position - i * _sensor_spacing + x_dev, y_position + y_dev, value);
                    var hash = new Coord(i, _row, _column);
                    List<Measurement> values;
                    if (!_measurements.ContainsKey(hash))
                    {
                        values = new List<Measurement>();
                        _measurements[hash] = values;
                    }
                    else
                        values = _measurements[hash];
                    values.Add(measurement);
                    new_measurements.Add(measurement);
                }
            }

            _model.FireCaptureProgressEvent(new_measurements);
            Fire(Trigger.Branch);
        }

        private void Branch()
        {
            // serpentine_x
            var even_y = _sample_y % 2 == 0;
            var x_inc = even_y ? 1 : -1;
            _sample_x += x_inc;
            var x_test = even_y ? (_sample_x <= _x_samples) : (_sample_x >= 0);
            if (x_test)
            {
                Fire(Trigger.AdvanceToNextSample);
                return;
            }
            _sample_x = even_y ? _x_samples : 0;

            if (--_sample_y >= 0)
            {
                Fire(Trigger.AdvanceToNextSample);
                return;
            }

            int inc = (_params.rows == 1 || _row % 2 == 1) ? -_params.step_column : _params.step_column;
            _column += inc;
            bool test = (inc == -_params.step_column) ? (_column >= _params.last_column) : (_column < (_params.first_column + 1));
            if (test)
            {
                Fire(Trigger.MoveToNextColumn);
                return;
            }
           
            if (++_row < _params.rows)
            {
                Fire(Trigger.MoveToNextRow);
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

            LogMeasurementResults();
            _model.FireCaptureStopEvent();
        }

        private void Aborted()
        {
            base.AbortedStateFunction();
            Log.Info(String.Format("Run: {0} : aborted", _model.RunCounter));
            if (_capture_start != null) // null if we got aborted before starting the timer
                _capture_start.Stop();

            _model.MoveToPark();

            LogMeasurementResults();
            _model.FireCaptureStopEvent();
        }

        private void LogMeasurementResults()
        {
            // log all the measurements
            Log.Info("Raw Values - Channel, Column, X Offset, Y Offset, Measurement");
            foreach (var well in _measurements.Keys)
            {
                var values = _measurements[well];
                foreach (var sample in values)
                    Log.Info(string.Format(",{0},{1},{2:0.000},{3:0.000},{4:0.000}", sample.channel, sample.column, sample.x, sample.y, sample.measured_value));
            }

            // log averages with std-dev cull
            var results = new List<Averages>();
            foreach (var well in _measurements.Keys)
            {
                results.Add(_model.CalculateWellAverages(_measurements[well]));
            }

            // log separately since the Calculation logs and I wanted a clean copy & pastable table
            Log.Info("Average Values - Channel, Column, X Offset, Y Offset, pop Average, std_dev, average");
            foreach (var result in results)
            {   // note -- initial COMMA is to separate timestamp - do not remove!
                Log.Info(string.Format(",{0},{1},{2:0.000},{3:0.000},{4:0.000},{5:0.000},{6:0.000}", result.Channel, result.Column, result.XAverage, result.YAverage, result.PopulationAverage, result.StandardDeviation, result.Average));
            }
        }
    }
}
