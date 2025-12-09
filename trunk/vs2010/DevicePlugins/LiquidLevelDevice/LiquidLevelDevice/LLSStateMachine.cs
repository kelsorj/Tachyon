#define REED_LOGGING

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.Utils;
using log4net;
using BioNex.BumblebeePlugin;
using System.Threading;

namespace LiquidLevelSensingGUI
{
    public class LLSStateMachine : StateMachineWrapper<LLSStateMachine.State, LLSStateMachine.Trigger>
    {
        public enum State
        {
            Start,
            Init,
            MoveToNextWell,
            MoveWithinWell,
            ReadSensor,
            Done,
        }

        public enum Trigger
        {
            Start,
            MoveToNextWell,
            MoveWithinWell,
            MoveToNextZHeight,
            ReadSensor,
            Done,
            Abort,
        }

        public delegate void GraphCallback(int row, int col, double  x, double y, double z);

        private readonly ILog _log = LogManager.GetLogger(typeof(LLSStateMachine));
        private Bumblebee _bee;
        private GraphCallback _graph_callback;

        private int _sensor_count;
        //private int[] port_ids = { 20, 15, 16, 14, 18, 19, 17, 21};
        private int[] port_ids = { 14, 15, 16, 17, 18, 19, 20};
        private int[] port_rows = { 1, 2, 3, 4, 5, 6, 7};
        private LevelSensor[] _sensors;
        public int logi = 1;

        public LLSStateMachine(Bumblebee bee, LevelSensor[] sensors, GraphCallback graph_callback) 
            : base(typeof(LLSStateMachine), State.Start, Trigger.Start, Trigger.Abort, true)
        {
            _bee = bee;
            _graph_callback = graph_callback;
            _sensors = sensors;
            _sensor_count = sensors.Length;

            SM.Configure(State.Start)
                .Permit(Trigger.Start, State.Init);
            SM.Configure(State.Init)
                .Permit(Trigger.MoveToNextWell, State.MoveToNextWell)
                .OnEntry( InitialState);
            SM.Configure( State.MoveToNextWell)
                .Permit( Trigger.MoveWithinWell, State.MoveWithinWell)
                .OnEntry( MoveToNextWell);
            SM.Configure( State.MoveWithinWell)
                .Permit( Trigger.ReadSensor, State.ReadSensor)
                .OnEntry( MoveWithinWell);
            SM.Configure( State.ReadSensor)
                .Permit( Trigger.MoveWithinWell, State.MoveWithinWell)
                .Permit( Trigger.MoveToNextWell, State.MoveToNextWell)
                .Permit( Trigger.MoveToNextZHeight, State.Init)
                .Permit( Trigger.Done, State.Init)
                .OnEntry( ReadSensor);
            SM.Configure( State.Done)
                .OnEntry( Done);
        }

        private readonly int ROWS = 1;
        private readonly int COLS = 12;
        private readonly double SPACING_MM = 9.0;
        // GRENINER 655101
        //private readonly double SAMPLE_HEIGHT_MM = 16;
        // THERMO BLOCK
        private readonly double SAMPLE_HEIGHT_MM = 33;
        // Y Test Rig
        //private readonly double SAMPLE_HEIGHT_MM = 15;
        private readonly double plate_x = -0.0;
        private readonly double plate_y = -0.0;

        private int _row;
        private int _col;
        private int _sample_y;
        private int _sample_x;
        private byte _channel_id;
        private byte _stage_id;
        private double _sample_height_mm;
        private double _well_x_mm;
        private double _well_y_mm;

        private int _row_inc; // increment for well to well serpentine path
        private int _z_counter = 0;

        private double[] Z_OFFSETS = { 0 };
        
        // regular 96 well plate, scan entire well w/ 32400 points per well
        // in the current state 
        // _well_x_mm = _row * SPACING_MM;
        // _well_y_mm = _col * SPACING_MM;
        // Y walks in the column (eg from left side of 1 to right side) 
        // X walks the row (from top of A to bottom)
        private double[] Y_OFFSETS = { -.5,0 };
        private double[] X_OFFSETS = { -.5,0 };
        private double Y_STEP_SIZE = 0.25;
        private double X_STEP_SIZE = 0.25;

        // brought these out for logging purposes
        private double _y_offset; 
        private double _x_offset;

#if REED_LOGGING
        public static String logLocation = @"C:\code\trunk\vs2010\bin\Debug\logs\" + System.DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss") +
            "-sensor-readings.txt"; // create a log file path for the current instance
        public void LogMessageToFile(string message)
        {
            using(System.IO.StreamWriter sw = System.IO.File.AppendText(logLocation))
            {
                string logLine = System.String.Format("{0:G}, {1}", System.DateTime.Now, message);
                sw.WriteLine(logLine);
            }
        }
#else
        public void LogMessageToFile(string message){}
#endif

        private void InitialState()
        {
            _row_inc = 1;
            _row = 0;
            _col = 0;
            _channel_id = 1;
            _stage_id = 4;
            _sample_height_mm = SAMPLE_HEIGHT_MM + Z_OFFSETS[_z_counter];

            if( _bee != null) // _bee is null if we're simulating
                _bee.Model.MoveAboveUL(_channel_id, _stage_id, _sample_height_mm, 0, 0, false);
            //for( int i=0; i<_sensor_count; ++i)
            //    _sensors[i] = new LevelSensor(port_ids[i]);
            // NOTE: DO NOT RESET _z_counter here or we'll loop forever
            Fire(Trigger.MoveToNextWell);
        }

        private void MoveToNextWell()
        {
            _sample_y = 0;
            _sample_x = 0;
            //_well_x_mm = _col * SPACING_MM;
            //_well_y_mm = _row * SPACING_MM;
            // RJK switch so the Y axis increments to move columns
            _well_x_mm = _row * SPACING_MM;
            _well_y_mm = _col * SPACING_MM;
            _log.Info(string.Format("Moving to row: {0}, col: {1}", _row + 1, _col + 1));            
            Fire(Trigger.MoveWithinWell);
        }

        private void MoveWithinWell()
        {
            _x_offset = X_OFFSETS[0] + plate_x + _sample_x * X_STEP_SIZE;
            _y_offset = _row_inc == 1 
                ? Y_OFFSETS[0] + plate_y + _sample_y * Y_STEP_SIZE 
                : Y_OFFSETS[1] + plate_y - _sample_y * Y_STEP_SIZE;
            
            _log.Info(String.Format("Moving to offset for sample {0},{1}", _sample_x + 1, _sample_y + 1));
            if( _bee != null) // _bee is null if we're simulating
                _bee.Model.MoveAboveUL(_channel_id, _stage_id, _sample_height_mm, _well_x_mm + _x_offset, _well_y_mm + _y_offset, false);
            System.Threading.Thread.Sleep(500);
            Fire(Trigger.ReadSensor);
        }

        private void ReadSensor()
        {
            int Y_SAMPLES_PER_WELL = (int)((Y_OFFSETS[1] - Y_OFFSETS[0]) / Y_STEP_SIZE);
            int X_SAMPLES_PER_WELL = (int)((X_OFFSETS[1] - X_OFFSETS[0]) / X_STEP_SIZE);

            string[] log_messages = new string[_sensor_count];
            bool[] error = new bool[_sensor_count];
            bool[] retry = new bool[_sensor_count];
            System.Threading.Tasks.Parallel.For( 0, _sensor_count, i => {
            //for( int i=0; i<_sensor_count; ++i){
                try{
                    error[i] = false;
                    retry[i] = false; // did we retry this read
                    double reading = _sensors[i].GetIntegerReading(); // returns 0..4095
                    double raw = reading;
                    if (i == 0) reading = 0.0999 * (double)reading + 1.9278;
                    if (i == 1) reading = 0.1027 * (double)reading + 0.1283;
                    if (i == 2) reading = 0.0901 * (double)reading + 0.2446;
                    if (i == 3) reading = 0.1013 * (double)reading + (-0.8593);
                    if (i == 4) reading = 0.1035 * (double)reading + (-2.9016);

                    /*var logmessage = String.Format("Sensor:{0}: Math:{{0,0,{1:0.00000}}},",
                        i,
                        reading
                        );
                     */ 
                    retry[i] = _sensors[i].Retries > 1;
                    log_messages[i] = String.Format("Sensor:{0}: {7} : Math:{{{1:0.00},{2:0.00},{6:0.}}},: Math:{{{1:0.00},{2:0.00},{3:0}}},: Math:{{{4:0.00},{5:0.00},{6:0.}}},: Math:Text[\"{0}\",{{{1:0.00},{2:0.00},{3:0}}}],: Math:Text[\"{0}\",{{{4:0.00},{5:0.00},{3:0}}}], retries: {8}",
                        i,
                        _well_y_mm + _y_offset, 
                        _well_x_mm + _x_offset + (port_rows[i] * SPACING_MM),
                        reading,
                        _y_offset,
                        _x_offset,
                        raw,
                        logi,
                        _sensors[i].Retries
                        );
                }
                catch(LevelSensorException e)
                {
                    error[i] = true;
                    log_messages[i] = String.Format("Sensor:{0}: {1}", i, e.Message);
                }
               

                /*
                Log.Info(logmessage);
                LogMessageToFile(logmessage);
                 */

                //if (_graph_callback != null)
                //    _graph_callback(_row, _col, _well_x_mm + _x_offset + i * (X_SAMPLES_PER_WELL + 1 * X_STEP_SIZE), _well_y_mm + _y_offset, reading);
            });

            for( int i=0; i<_sensor_count; i++) {
                if( error[i])
                    Log.Error(log_messages[i]);
                else if( retry[i])
                    Log.Warn(log_messages[i]);
                else
                    Log.Info(log_messages[i]);
                LogMessageToFile(log_messages[i]);
            }

            bool done = false;
            if( ++_sample_y == Y_SAMPLES_PER_WELL)
            {
                _sample_y = 0;
                if( ++_sample_x == X_SAMPLES_PER_WELL)
                    done = true;
            }

            if (!done)
            {
                Fire(Trigger.MoveWithinWell);
            }
            else
            {
                // increment row, bump col if necessary, if col is past end, then we're done
                _row += _row_inc;
                bool row_complete = _row_inc == -1 ? _row < 0 : _row == ROWS;
                if ( row_complete)
                {
                    _row -= _row_inc;
                    _row_inc = -_row_inc;
                    if (++_col == COLS)
                    {
                        // we are done when we have used all of the Z_OFFSETS
                        //if( ++_z_counter == Z_OFFSETS.Length) {
                            Fire(Trigger.Done);
                            logi++;
                            return;
                        //} else {
                            //Fire(Trigger.MoveToNextZHeight);
                            //return;
                        //}
                    }
                }
                Fire(Trigger.MoveToNextWell);
            }
        }

        private void Done()
        {
            _log.Info(String.Format("Scan complete"));
            base.EndStateFunction();
        }
    }
}
