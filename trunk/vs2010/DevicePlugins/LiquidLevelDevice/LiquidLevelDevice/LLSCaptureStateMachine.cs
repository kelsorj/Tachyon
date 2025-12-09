using System;
using System.Threading;
using BioNex.Shared.Utils;
using System.Collections.Generic;
using System.Diagnostics;

namespace BioNex.LiquidLevelDevice
{
    internal class LLSCaptureStateMachine : StateMachineWrapper<LLSCaptureStateMachine.State, LLSCaptureStateMachine.Trigger>
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

        ILLSensorModel _model;
        int _samples;

        const int _average_sense_time_ms = 65; // for simulated sensing

        private const double START_ANGLE = 45;
        double _radius_start;
        double _radius_step;
        double _radius_max;

        // sensors, provide by client
        uint _sensor_count;
        ILevelSensor[] _sensors;

        // state variables
        bool[] _channel_enabled;
        bool[] _read_error;
        int _column;        // current column
        int _row;           // current row (general 1 or 2 rows since there are 8 sensors)
        int _sample;        // index into sample x & y offsets
        double _radius;     // next distance from center -- changes if we're doing an error rescan

        // brought these out for logging purposes
        double _z_offset;

        Stopwatch _capture_start;
        int _columns;
        int _rows;                  // rows that the machine will process == labware / 8 (i.e. 1 for 96, 2 for 384)
        int _labware_rows;          // actual # of rows on the labware
        double _sensor_spacing;
        double _column_spacing;
        double _row_spacing;
        double _well_radius;

        int _settle_time_ms;

        string _labware_name;

        bool _seek_deviations;
        int _sensor_for_deviation;
        int[] _samples_acquired; // seek deviation allows us to acquire data asynchronously per channel, lets stop acquiring a given channel once its "queue" is full

        IDictionary<Coord, List<Measurement>> _measurements;
        public IDictionary<Coord, List<Measurement>> Measurements { get { return _measurements; } }

        List<Averages> _averages;
        public IList<Averages> Averages { get { return _averages; } }

        List<double> _volumes;
        //public IList<double> Volumes { get { return _volumes; } }

        CaptureDataFile _data_logger;
        LabwareDetails _labware_details;

        public LLSCaptureStateMachine(ILLSensorModel model, string labware_name, CaptureDataFile data_logger)
            : base( State.Start, Trigger.Start, Trigger.Abort, true)
        {
            _model = model;
            _labware_name = labware_name;
            _data_logger = data_logger;

            _sensor_spacing = 9.0; // spacing between sensors
            _column_spacing = 9.0; // spacing between labware columns
            _row_spacing = 9.0;    // spacing between labware rows
            _columns = 12;
            _labware_rows = 8;
            double thickness = 30.0;
            _well_radius = 4.5;
            _model.GetLabwareData(_labware_name, out _columns, out _labware_rows, out _column_spacing, out _row_spacing, out thickness, out _well_radius);
            _labware_details = _model.VolumeMapDatabase.GetLabwareDetails(labware_name);

            _samples = model.Properties.GetInt(LLProperties.SamplesPerWell);
            _z_offset = thickness + model.Properties.GetDouble(LLProperties.CaptureOffset) + _labware_details.CaptureOffset;

            _sensors = model.Sensors;
            _sensor_count = model.SensorCount;

            _rows = _labware_rows / 8; // -- is there a more sensible way to derive this base on spacings?

            _settle_time_ms = model.Properties.GetInt(LLProperties.SettleTime);
            _radius_start = model.Properties.GetDouble(LLProperties.CaptureRadiusStart);
            _radius_step = model.Properties.GetDouble(LLProperties.CaptureRadiusStep);
            _radius_max = model.Properties.GetDouble(LLProperties.CaptureRadiusMax);

            _seek_deviations = model.Properties.GetBool(LLProperties.CaptureSeekDeviation);
            _samples_acquired = new int[_sensor_count];
           
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
                .OnEntry(ReadSensors);
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
            _channel_enabled = new bool[_sensor_count];
            _measurements.Clear();
            _model.FireCaptureStartEvent(true, _labware_name);

            // disable even channels if labware row count < 8 --- todo make this more intelligent...
            var enable_even_channels = _labware_rows >= 8;
            for (int i = 0; i < _sensor_count; ++i)
                _channel_enabled[i] = (i % 2 == 0) ? true : enable_even_channels;

            // set the measurement sensitivity based on labware
            _model.SetSensitivity(_labware_details.Sensitivity);

            Fire(Trigger.MoveToSafeStart);
        }

        private void MoveToSafeStart()
        {
            //_model.MoveToPark(); --> skip this for now, just a time kill since first move is always to a safe position above plate now
            Fire(Trigger.MoveToNextRow);
        }

        private void MoveToNextRow()
        {
            _column = _rows == 1 || _row % 2 == 1 ? _columns - 1 : 0;
            Fire(Trigger.MoveToNextColumn);
        }

        private void MoveToNextColumn()
        {
            _sample = 0;
            for (int i = 0; i < _sensor_count; ++i)
                _samples_acquired[i] = 0;
            _sensor_for_deviation = 0;
            Fire(Trigger.AdvanceToNextSample);
        }

        private void AdvanceToNextSample()
        {
            _radius = _radius_start;
            for (int i = 0; i < _sensor_count; ++i)
                _read_error[i] = true; // read error flags get unset on valid read

            Fire(Trigger.MoveWithinWell);
        }

        private void MoveWithinWell()
        {
            // calculate X and Y offsets based on position on circle
            double angle_degrees = START_ANGLE + _sample * 360.0 / _samples;
            double angle_radians = Math.PI * angle_degrees / 180.0;

            double pdx = _model.PlateDX;
            double pdy = _model.PlateDY;

            // initial offset based on well position
            double x_offset = _radius * Math.Cos(angle_radians) + 4.5 - _well_radius - _row * _row_spacing - pdx;
            double y_offset = -_radius * Math.Sin(angle_radians) + 4.5 - _well_radius - _column * _column_spacing - pdy;

            // if "seek deviation" mode enabled, we move to an offset that puts our deviated sensor at the "zero" for this sample (i.e. subtract deviation)
            if (_seek_deviations)
            {
                double x_dev = _model.Properties.GetDouble(LLProperties.index(LLProperties.XDeviation, _sensor_for_deviation));
                double y_dev = _model.Properties.GetDouble(LLProperties.index(LLProperties.YDeviation, _sensor_for_deviation));
                x_offset -= x_dev;
                y_offset -= y_dev;
            }

            Log.Info(string.Format("Moving to column: {0} : row: {1} : sample: {2} : radius: {3} : angle: {4:0.00}", _column + 1, _row + 1, _sample + 1, _radius, angle_degrees));

            _model.MoveRelativeToTeachpoint(x_offset, y_offset, -_z_offset);
            if (_settle_time_ms > 0)
                Thread.Sleep(_settle_time_ms);

            Fire(Trigger.ReadSensors);
        }

        private void ReadSensors()
        {
            string[] log_messages = new string[_sensor_count];
            bool[] retry = new bool[_sensor_count];
            int[] reading = new int[_sensor_count];
            double[] cal_reading = new double[_sensor_count];
            bool[] read_channel = new bool[_sensor_count];    // whether or not this channel should be read this cycle, marked false to prevent reading, and marked false to show that a read did not occur
            _read_error.CopyTo(read_channel, 0);              // prevents reading channels that do not need a read this pass

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

            var action = new Action<int>(i =>
            {
                if (_sensors[i] == null)
                {
                    Thread.Sleep(_average_sense_time_ms); // simulate sensor
                    _read_error[i] = false;
                    read_channel[i] = false;
                    return;
                }

                if (!_channel_enabled[i])// || (_seek_deviations && i != _sensor_for_deviation)) <-- uncomment to disable channel batching
                {
                    _read_error[i] = false;
                    read_channel[i] = false;
                    Log.DebugFormat("Skipping read for disabled channel {0}", i);
                    return;
                }

                // break out early if we've acquired enough samples for this channel (via batch sampling)
                if(_samples_acquired[i] >= _samples)
                {
                    _read_error[i] = false;
                    read_channel[i] = false;
                    return;
                }

                // base is position on capture circle relative to teachpoint -- (x_base,y_base) == (0, 0) would be center of circle
                var pdx = _model.PlateDX;
                var pdy = _model.PlateDY;
                var x_base = x_position - 4.5 + _well_radius + _row * _row_spacing + pdx;
                var y_base = y_position - 4.5 + _well_radius + _column * _column_spacing + pdy;

                if (!InAcceptanceRegion(i, x_base, y_base))
                {
                    _read_error[i] = false;
                    read_channel[i] = false;
                    return;
                }

                if (!read_channel[i])
                    return;

                // Add the sensor deviation from the current position to get the location of the actual sensed point (see LLSLocateTeachpointAlgorithm.cs)
                var x_dev = _model.Properties.GetDouble(LLProperties.index(LLProperties.XDeviation, i));
                var y_dev = _model.Properties.GetDouble(LLProperties.index(LLProperties.YDeviation, i));

                Stopwatch read_start = Stopwatch.StartNew();
                try
                {
                    reading[i] = _sensors[i].GetReading(); // returns 0..4095 -- integer 0.1 mm increments
                    retry[i] = _sensors[i].Retries > 1; // did we retry this read?
                    _read_error[i] = false; // reset error flag on valid read

                    cal_reading[i] = _sensors[i].GetCalibratedReading(reading[i]);
                    if (_model.Properties.GetBool(LLProperties.CaptureFloorToZero) && cal_reading[i] > z_position)
                        cal_reading[i] = z_position;

                    log_messages[i] = String.Format("Run: {0} : Column: {1} : Row: {2} : Sensor: {3} : X: {4:0.00} : Y: {5:0.00} : Z: {6:0.00} : Raw: {7:0.00} : Calibrated: {8:0.00} : Duration: {9:0.00} ms",
                        _model.RunCounter, _column + 1, _row + 1, i + 1, x_position - i * _sensor_spacing + x_dev, y_position + y_dev, z_position, reading[i], z_position - cal_reading[i], read_start.ElapsedMilliseconds);


                    _samples_acquired[i] = _samples_acquired[i] + 1;
                    _model.FireIntegerSensorReadingReceivedEvent((int)i, reading[i]);
                }
                catch (LevelSensorException e)
                {
                    // an exception here means we got a bad read from the sensor
                    log_messages[i] = String.Format("Run: {0} : Column: {1} : Row: {2} : Sensor: {3} : X: {4:0.00} : Y: {5:0.00} : Z: {6:0.00} : Error: {7}",
                        _model.RunCounter, _column + 1, _row + 1, i + 1, x_position - i * _sensor_spacing + x_dev, y_position + y_dev, z_position, e.Message);
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
            //for (int i = 0; i < _sensor_count; ++i)
            //    action(i);

            for (int i = 0; i < _sensor_count; i++)
            {
                if (_sensors[i] == null)
                    continue;
                if (log_messages[i] == null) // empty string if we were retrying some other channel
                    continue;
                if (_read_error[i])
                    Log.Warn(log_messages[i]);
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
                if (!_channel_enabled[i])
                    continue;
                if (_read_error[i] == true)
                    continue;
                if (read_channel[i])
                {
                    var value = _read_error[i] ? 0.0
                        : z_position - cal_reading[i];

                    // Add the sensor deviation to current position to get the location of the actual sensed point
                    double x_dev = _model.Properties.GetDouble(LLProperties.index(LLProperties.XDeviation, i));
                    double y_dev = _model.Properties.GetDouble(LLProperties.index(LLProperties.YDeviation, i));

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
            // if we got any read errors, bump the radius and read the error channels again
            bool error = false;
            for (int i = 0; i < _sensor_count; ++i)
                error |= _read_error[i];
            if (error && (_radius + _radius_step) < _radius_max)
            {
                _radius += _radius_step;
                Fire(Trigger.MoveWithinWell);
                return;
            }

            if (++_sample < _samples)
            {
                Fire(Trigger.AdvanceToNextSample);
                return;
            }

            if (_seek_deviations && _sensor_for_deviation < (_sensor_count-1))
            {
                // skip past sensors that we have already acquired enough samples for
                // look for the next sensor which hasn't completed it's sampling 
                // -- always skip to the next sensor, since we've completed our sampling tries, but only stop at a sensor that hasn't completed
                do
                {
                    ++_sensor_for_deviation;
                }
                while (_sensor_for_deviation < _sensor_count && _samples_acquired[_sensor_for_deviation] >= _samples);

                // if we didn't run out of sensors, go read again, start over at sample 0
                if ( _sensor_for_deviation < _sensor_count)
                {
                    _sample = 0;
                    Fire(Trigger.AdvanceToNextSample);
                    return;
                }
            }


            int inc = (_rows == 1 || _row % 2 == 1) ? -1 : +1;
            _column += inc;
            bool test = (inc == -1) ? (_column >= 0) : (_column < _columns);
            if (test)
            {
                Fire(Trigger.MoveToNextColumn);
                return;
            }

            if (++_row < _rows)
            {
                Fire(Trigger.MoveToNextRow);
                return;
            }

            Fire(Trigger.MoveToSafeEnd);
        }

        private void MoveToSafeEnd()
        {
            Log.Info(String.Format("Run: {0} : sample capture complete, duration: {1} sec", _model.RunCounter, (_capture_start.Elapsed).TotalSeconds));
            _model.FireCaptureStopEvent();

            Fire(Trigger.Done);
        }

        private void Done()
        {
            base.EndStateFunction();
            Log.Info(String.Format("Run: {0} : complete, duration: {1} sec", _model.RunCounter, (_capture_start.Elapsed).TotalSeconds));
            _capture_start.Stop();

            _model.MoveToPark();

            LogMeasurementResults();
        }

        private void Aborted()
        {
            base.AbortedStateFunction();
            Log.Info(String.Format("Run: {0} : aborted", _model.RunCounter));
            if (_capture_start != null) // null if we got aborted before starting the timer
                _capture_start.Stop();

            _model.MoveToPark();

            LogMeasurementResults();
        }

        private void LogMeasurementResults()
        {
            // log all the measurements
            Log.Debug("Raw Values - Channel, Column, Row, X Offset, Y Offset, Measurement");

            _data_logger.StartRaw(_model.RunCounter, _labware_name, _labware_details);

            foreach (var well in _measurements.Keys)
            {
                var values = _measurements[well];
                foreach (var sample in values)
                {
                    Log.DebugFormat(",{0},{1},{2},{3:0.000},{4:0.000},{5:0.000}", sample.channel, sample.column, sample.row, sample.x, sample.y, sample.measured_value);
                    _data_logger.WriteRaw(_model.RunCounter, sample.channel, sample.column, sample.row, sample.x, sample.y, sample.measured_value);
                }
            }

            // log averages with std-dev cull
            _averages = new List<Averages>();
            foreach (var well in _measurements.Keys)
            {
                _averages.Add(_model.CalculateWellAverages(_measurements[well]));
            }
            // log separately since the Calculation logs and I wanted a clean copy & pastable table
            Log.Debug("Average Values - Channel, Column, Row, X Offset, Y Offset, pop Average, std_dev, average");
            foreach (var r in _averages)
                // note -- initial COMMA is to separate timestamp - do not remove!
                Log.DebugFormat(",{0},{1},{2},{3:0.000},{4:0.000},{5:0.000},{6:0.000},{7:0.000}", r.Channel, r.Column, r.Row, r.XAverage, r.YAverage, r.PopulationAverage, r.StandardDeviation, r.Average);

            // log Volumes from volume lookup if they're available
            _volumes = _model.GetVolumesFromAverages(_labware_name, _averages);
            if (_volumes.Count > 0 && _volumes.Count == _averages.Count)
            {
                Log.Info("Volumes - Channel, Column, Row, x, y, volume");
                _data_logger.StartVolume(_model.RunCounter, _labware_name, _labware_details);

                for (int i = 0; i < _averages.Count; ++i)
                {
                    var avg = _averages[i];
                    var vol = _volumes[i];

                    Log.Info(string.Format(",{0},{1},{2},{3:0.000},{4:0.000},{5:0.000}", avg.Channel, avg.Column, avg.Row, avg.XAverage, avg.YAverage, vol));
                    _data_logger.WriteVolume(_model.RunCounter, avg.Channel, avg.Column, avg.Row, avg.XAverage, avg.YAverage, vol);
                }
            }
        }

        // return FALSE if the sensor position is outside the reject radius
        bool InAcceptanceRegion(int sensor, double x, double y)
        {
            // turn this into distance from teachpoint
            double distance = Math.Sqrt(x * x + y * y); // debug only

            // default to large value so everything is accepted if the field is missing
            double rejection_radius = _model.Properties.GetDouble(LLProperties.CaptureRejectionRadius);

            double x_dev = _model.Properties.GetDouble(LLProperties.index(LLProperties.XDeviation, sensor));
            double y_dev = _model.Properties.GetDouble(LLProperties.index(LLProperties.YDeviation, sensor));

            double sample_x = x + x_dev;
            double sample_y = y + y_dev;

            // sample_distance is a measure of this sample's distance from dead-center for this sensor. 
            // This should essentially be where this sensor is on its sampling path (circle) plus any error in the motor position
            double sample_distance = Math.Sqrt(sample_x * sample_x + sample_y * sample_y);

            bool acceptable = sample_distance <= rejection_radius;

            if (! acceptable && sensor == _sensor_for_deviation)
                Log.WarnFormat("Skipping sample {0} for sensor {1} because it is outside of acceptance region (distance: {2:0.000})", _sample+1, sensor+1, sample_distance);

            return acceptable;
        }
    }
}
