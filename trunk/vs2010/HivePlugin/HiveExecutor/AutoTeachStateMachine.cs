using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using BioNex.Hive.Hardware;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.IError;
using BioNex.Shared.Teachpoints;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.Utils;
using log4net;

namespace BioNex.Hive.Executor
{
    public class AutoTeachStateMachine : HiveStateMachine< AutoTeachStateMachine.State, AutoTeachStateMachine.Trigger>
    {
        public GenericTeachpointCollection< HiveTeachpoint> FinalTeachpoints { get; private set; }

        private const int SEEK_SPEED = 10;
        private const double DEFAULT_APPROACH_HEIGHT = 9.0;
        private const HiveTeachpoint.TeachpointOrientation DEFAULT_ORIENTATION = HiveTeachpoint.TeachpointOrientation.Portrait;

        // ----------------------------------------------------------------------
        // enumerations.
        // ----------------------------------------------------------------------
        public enum State
        {
            Start,
            InitialTuck, InitialTuckError,
            MoveToNextShelf, MoveToNextShelfError,
            InitialSeekZ, InitialSeekZError,
            SeekY, SeekYError,
            SeekX, SeekXError,
            MoveToFinalSeekZ, MoveToFinalSeekZError,
            FinalSeekZ, FinalSeekZError,
            Tuck, TuckError,
            SaveData,
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
            NoMoreShelves,
            RedoZCalibration,
        }

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        private AutoTeachConfiguration _auto_teach_config;
        private readonly AutoTeachConfiguration.Panel _panel;
        private readonly HiveHardware _hardware;
        private readonly IOInterface _io_interface;
        private int _current_shelf;
        private int _current_rack;
        private readonly IAxis _x_axis;
        private readonly IAxis _z_axis;
        // cached motor settings
        private readonly MotorSettings _z_settings;
        private readonly MotorSettings _x_settings;
        // the sensed positions 
        private double _x_sensed;
        private double _y_sensed;
        private double _z_sensed;
        private double _z_tool;

        private bool _use_last_sensed_values;

        private string _last_error;
        private static readonly ILog _log = LogManager.GetLogger( typeof( AutoTeachStateMachine));

        private readonly double[] _y_values;
        private readonly double[] _z_values;

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public AutoTeachStateMachine( HiveExecutor executor, AutoTeachConfiguration auto_teach_config, AutoTeachConfiguration.Panel panel, HiveHardware hardware, IOInterface io_interface)
            : base( executor, null, typeof( AutoTeachStateMachine), State.Start, State.End, State.Abort, Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, executor.HandleError)
        {
            SetupLocalLogAppender();
            _log.DebugFormat( "********** AUTO-TEACHING PANEL '{0}' **********", panel.Name);

            FinalTeachpoints = new GenericTeachpointCollection< HiveTeachpoint>();
            _auto_teach_config = auto_teach_config;
            _panel = panel;
            Debug.Assert( _panel != null);
            _hardware = hardware;
            _io_interface = io_interface;
            _x_axis = _hardware.XAxis;
            _z_axis = _hardware.ZAxis;
            _x_settings = _x_axis.Settings;
            _z_settings = _z_axis.Settings;

            _y_values = new double[_panel.SlotCount];
            _z_values = new double[_panel.SlotCount];
            _use_last_sensed_values = false;

            InitializeStates();
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        private void InitializeStates()
        {
            ConfigureState( State.Start, NullStateFunction, State.InitialTuck);
            ConfigureState( State.InitialTuck, Tuck, State.MoveToNextShelf, State.InitialTuckError);
            ConfigureState( State.MoveToNextShelf, MoveToNextShelf, State.InitialSeekZ, State.MoveToNextShelfError)
                .Permit( Trigger.NoMoreShelves, State.Tuck);
            SM.Configure( State.InitialSeekZ)
                .Permit( Trigger.Abort, State.End)
                .Permit( Trigger.Success, State.SeekY)
                .Permit( Trigger.Failure, State.InitialSeekZError)
                .OnEntry( InitialSeekZ);
            SM.Configure( State.InitialSeekZError)
                .Permit( Trigger.Abort, State.End)
                .Permit( Trigger.Retry, State.MoveToNextShelf) // FYC -- are you sure?
                .OnEntry( InitialSeekZError);
            SM.Configure( State.SeekY)
                .Permit( Trigger.Abort, State.End)
                .Permit( Trigger.Success, State.SeekX)
                .Permit( Trigger.Failure, State.SeekYError)
                .OnEntry(SeekY);
            SM.Configure( State.SeekYError)
                .Permit( Trigger.Abort, State.End)
                .Permit( Trigger.Retry, State.MoveToNextShelf) // FYC -- are you sure?
                .OnEntry( SeekYError);
            SM.Configure( State.SeekX)
                .Permit( Trigger.Abort, State.End)
                .Permit( Trigger.Success, State.FinalSeekZ)
                .Permit( Trigger.Failure, State.SeekXError)
                .OnEntry( SeekX);
            SM.Configure( State.SeekXError)
                .Permit( Trigger.Abort, State.End)
                .Permit( Trigger.Retry, State.MoveToNextShelf) // FYC -- are you sure?
                .OnEntry( SeekXError);
            SM.Configure( State.FinalSeekZ)
                .Permit( Trigger.Abort, State.End)
                .Permit( Trigger.Success, State.MoveToNextShelf)
                .Permit( Trigger.Failure, State.FinalSeekZError)
                .Permit( Trigger.RedoZCalibration, State.MoveToNextShelf)
                .OnEntry( FinalSeekZ);
            SM.Configure( State.FinalSeekZError)
                .Permit( Trigger.Abort, State.End)
                .Permit( Trigger.Retry, State.MoveToNextShelf) // FYC -- are you sure?
                .OnEntry( InitialSeekZError); // reuse the code for seek Z since it's the same
            SM.Configure( State.Tuck)
                .Permit( Trigger.Abort, State.End)
                .Permit( Trigger.Success, State.SaveData)
                .Permit( Trigger.Failure, State.TuckError)
                .OnEntry( Tuck);
            SM.Configure( State.TuckError)
                .Permit( Trigger.Abort, State.End)
                .Permit( Trigger.Retry, State.Tuck)
                .OnEntry( f => HandleErrorWithRetryOnly());
            SM.Configure( State.SaveData)
                .Permit( Trigger.Success, State.End)
                .OnEntry( SaveData);
            SM.Configure( State.End)
                .OnEntry( EndStateFunction);
            SM.Configure( State.Abort)
                .OnEntry( AbortedStateFunction);
        }

        // ***** for Z sensor calibration curve to correct for different Y values
        bool _calibrating;
        const double _calibration_step_size = 0.1;
        double _current_calibration_offset { get; set; }
        /// <summary>
        /// this is the farthest delta we want to measure Z from the Y origin position
        /// </summary>
        const double _calibration_range = 3.0;

        private void MoveToNextShelf()
        {
            try {
                // reload the configuration before each shelf in case we're tweaking values
                _auto_teach_config = FileSystem.LoadXmlConfiguration<AutoTeachConfiguration>( FileSystem.GetAppPath() + "\\config\\teach_config.xml");
                _calibrating = _auto_teach_config.calibratejig;
                double _calibration_step_size = _auto_teach_config.calibration_step_size;
                double _calibration_range = _auto_teach_config.calibration_range;

                // if we've hit the last shelf in the panel, bail!
                if( !_calibrating &&  _current_rack == _panel.RackCount) {
                    Fire( Trigger.NoMoreShelves);
                    return;
                }

                HiveTeachpoint tp = null;
                if( !_calibrating) {

                    double x = _panel.Origin.X + _current_rack * _panel.SlotXSpacing + _auto_teach_config.XJigOffsetForTeachingZ;
                    double y = _panel.Origin.Y + _auto_teach_config.YJigOffsetForTeachingZ;
                    double z = _panel.Origin.Z - _current_shelf * _panel.SlotZSpacing + _auto_teach_config.ZJigOffsetForTeachingZ;

                    if (_use_last_sensed_values)
                    {
                        x = _x_sensed + _auto_teach_config.XSensedOffsetForTeachingZ;
                        y = _y_sensed + _auto_teach_config.YSensedOffsetForTeachingZ;
                    }                     

                    tp = new HiveTeachpoint( "temp", x, y, z, 0.0, DEFAULT_ORIENTATION);
                } else {
                    double x = _panel.Origin.X + _current_rack * _panel.SlotXSpacing + _auto_teach_config.XJigOffsetForTeachingZ;
                    _current_calibration_offset += _calibration_step_size;
                    double y = _panel.Origin.Y + _current_calibration_offset + _auto_teach_config.YJigOffsetForTeachingZ;
                    double z = _panel.Origin.Z - _current_shelf * _panel.SlotZSpacing + _auto_teach_config.ZJigOffsetForTeachingZ;
                    tp = new HiveTeachpoint( "temp", x, y, z, 0.0, DEFAULT_ORIENTATION);
                }
                // MoveTeachingJigToTeachpoing will automatically tuck the arms in
                _hardware.MoveTeachingJigToTeachpoint( tp);

                if (_calibrating) {
                    _hardware.UpdateCurrentPosition();
                    if (_hardware.CurrentToolPosition.Y > (_panel.Origin.Y + _auto_teach_config.YJigOffsetForTeachingZ + _calibration_range))
                    {
                        Fire(Trigger.NoMoreShelves);
                        return;
                    }
                }

                Fire( Trigger.Success);
            } catch( Exception ex) {
                _last_error = ex.Message;
                Fire( Trigger.Failure);
            }
        }

        private void InitialSeekZ()
        {
            int original_speed = _hardware.Speed;
            //int original_speed = 50;
            try {
                _hardware.Speed = SEEK_SPEED;

                if (_current_rack <= _panel.RackToSkipYAfter)
                    _z_sensed = SeekHelper(_z_axis, 1.0, 0.02, _auto_teach_config.ZSensorInputBitIndex, _auto_teach_config.ZSensorEdgeTriggerState, _auto_teach_config.ZSensorSeekPositive); // new jig needs to seek upward to not see pin
                else
                    _z_sensed = UseLastZSensedInRow();

                // save Z value so we can re-use it later if necessary
                _z_values[_current_shelf] = _z_sensed;

                if (!_calibrating)
                {
                    Fire(Trigger.Success);
                } else {
                    // log Z position here
                    _hardware.UpdateCurrentPosition();
                    _initial_y = _hardware.CurrentToolPosition.Y;
                    //_log.DebugFormat( "{0},{1}", _hive_plugin.CurrentToolPosition.Y, _z_sensed);
                    //Fire( Trigger.RedoZCalibration);
                    // now we want to continue to do Y calibration
                    Fire(Trigger.Success);
                }
            } catch (Exception ex) {
                _last_error = ex.Message;
                Fire(Trigger.Failure);
            } finally {
                _hardware.Speed = original_speed;
            }

        }

        private void InitialSeekZError()
        {
            SeekErrorHelper( _z_axis, 5.0);
        }

        private double UseLastYSensedInRow()
        {
            _hardware.UpdateCurrentPosition();
            double y_actual = _hardware.CurrentToolPosition.Y;
            double y_target = _current_rack == 0 ? _panel.Origin.Y : _y_values[_current_shelf];
            double y_delta = y_target - y_actual;

            _hardware.JogY(y_delta, true);

            return y_target;
        }

        private double UseLastZSensedInRow()
        {
            _hardware.UpdateCurrentPosition();
            double z_actual = _z_axis.GetPositionMM();
            double z_target = _current_rack == 0 ? _panel.Origin.Z : _z_values[_current_shelf];
            double z_delta = z_target - z_actual;

            MotorSettings settings = _z_axis.Settings;
            _z_axis.MoveAbsolute(z_actual + z_delta, settings.Velocity / 4, settings.Acceleration / 4);

            return z_target;
        }

        private void SeekY()
        {
            int original_speed = _hardware.Speed;
            try
            {
                // update just in case
                _hardware.UpdateCurrentPosition();
                // move Z into position for seeking Y
                _z_axis.MoveAbsolute(_z_sensed + _auto_teach_config.ZOffsetForTeachingY, _z_settings.Velocity / 4, _z_settings.Acceleration / 4, wait_for_move_complete: false);
                // move X into position for seeking Y
                _x_axis.MoveRelative(_auto_teach_config.XOffsetForTeachingY, _x_settings.Velocity / 2, _x_settings.Acceleration / 2, _x_settings.Jerk);
                // recommand Z since we wanted to fake a simultaneous move
                _z_axis.MoveAbsolute(_z_sensed + _auto_teach_config.ZOffsetForTeachingY, _z_settings.Velocity / 4, _z_settings.Acceleration / 4);
                // seek Y
                _hardware.Speed = SEEK_SPEED;
                _hardware.JogY(_auto_teach_config.YOffsetForTeachingY, true);

                if( _current_rack <= _panel.RackToSkipYAfter)
                    _y_sensed = SeekHelper(null, 1.0, 0.02, _auto_teach_config.YSensorInputBitIndex,  _auto_teach_config.YSensorEdgeTriggerState, _auto_teach_config.YSensorSeekPositive);
                else
                    _y_sensed = UseLastYSensedInRow();
               
                // save Y value so we can re-use it later if necessary
                _y_values[_current_shelf] = _y_sensed;

                Fire(Trigger.Success);
            }
            catch (Exception ex)
            {
                _last_error = ex.Message;
                Fire(Trigger.Failure);
            }
            finally
            {
                _hardware.Speed = original_speed;
            }
        }

        private void SeekYError()
        {
            SeekErrorHelper( null, 5.0);
        }

        private void SeekX()
        {
            int original_speed = _hardware.Speed;
            try
            {
                // move Z into position for seeking X
                // NOTE -- at this point, we are moving Z to the seek position, but when Y is out, Z could be in a bad spot,
                // i.e. you could crash.  We ended up just changing the X seek Z offset and bumped it up to avoid a crash.
                // But technically, the Z offsets for X and Y seek should be the same for the new teach jig made on 2011-05-05
                _z_axis.MoveAbsolute(_z_sensed + _auto_teach_config.ZOffsetForTeachingX, _z_settings.Velocity / 4, _z_settings.Acceleration / 4, wait_for_move_complete: false);
                // move X into position for seeking X
                _x_axis.MoveRelative(_auto_teach_config.XOffsetForTeachingX, _x_settings.Velocity / 2, _x_settings.Acceleration / 2, _x_settings.Jerk);
                // move Y into position for seeking X
                _hardware.Speed = SEEK_SPEED;
                _hardware.JogY(_auto_teach_config.YOffsetForTeachingX, true);
                // look for X edge
                _x_sensed = SeekHelper(_x_axis, 1.0, 0.02, _auto_teach_config.XSensorInputBitIndex,  _auto_teach_config.XSensorEdgeTriggerState, _auto_teach_config.XSensorSeekPositive); // x seeks to the right now

                Fire(Trigger.Success);
            }
            catch (Exception ex)
            {
                _last_error = ex.Message;
                Fire(Trigger.Failure);
            }
            finally
            {
                _hardware.Speed = original_speed;
            }
        }

        private void SeekXError()
        {
            SeekErrorHelper( _x_axis, 5.0);
        }

        private void FinalSeekZ()
        {
            int original_speed = _hardware.Speed;
            try
            {
                if (_current_shelf == _panel.SlotCount-1)
                {
                    _hardware.ZAxis.MoveRelative(17);
                }

                double x = _x_sensed + _auto_teach_config.XOffsetForFinalZ;
                double y = _y_sensed + _auto_teach_config.YOffsetForFinalZ; // I roughly measured the distance from rack flat part to rounded part, + the amount the Y sensor sticks out from the jig
                double z = _panel.Origin.Z - _current_shelf * _panel.SlotZSpacing + _auto_teach_config.ZOffsetForFinalZ;
                HiveTeachpoint tp = new HiveTeachpoint("temp", x, y, z, 0, DEFAULT_ORIENTATION);

                // MoveTeachingJigToTeachpoing will automatically tuck the arms in
                _hardware.MoveTeachingJigToTeachpoint(tp);
                // seek Z one last time
                _hardware.Speed = SEEK_SPEED;
                
                // this only gets used if calibrating
                double i = _z_sensed;

                // when you're on the right side you can't sense Z
                if (_current_rack <= _panel.RackToSkipYAfter)
                    _z_sensed = SeekHelper(_z_axis, 1.0, 0.02, _auto_teach_config.ZSensorInputBitIndex,  _auto_teach_config.ZSensorEdgeTriggerState, _auto_teach_config.ZSensorSeekPositive); // again, we need to see Z in the upward direction
                else
                    _z_sensed = UseLastZSensedInRow();
                
                _hardware.UpdateCurrentPosition();
                _z_tool = _hardware.CurrentToolPosition.Z;
                
                // log data for now
                //if (!_calibrating)
                //_log.DebugFormat( "{0},{1},{2},{3},{4},{5},{6}", DateTime.Now.ToString("yyyyMMddhhmmss"), _panel.Name, _current_rack, _current_shelf, _x_sensed, _y_sensed, _z_sensed);

                // RJK the problem with the Z teachpoint is that we were looking at Z global before and we needed to be looking at Z tool space
                // log data for now
                if (!_calibrating)
                _log.DebugFormat( "{0},{1},{2},{3},{4},{5},{6}", DateTime.Now.ToString("yyyyMMddhhmmss"), _panel.Name, _current_rack, _current_shelf, _x_sensed, _y_sensed, _z_tool);

                //var FinalTeachpoint = new Teachpoint(
                //    string.Format("{0}, Rack {1}, Slot {2}", _panel.Name, _current_rack, _current_shelf), 
                //    _x_sensed + _auto_teach_config.XSensorConstTerm,  
                //    _y_sensed + _auto_teach_config.YSensorConstTerm, 
                //    _z_sensed + _auto_teach_config.ZSensorConstTerm + _auto_teach_config.ZSensorFirstOrderTerm * _y_sensed + _auto_teach_config.ZSensorSecondOrderTerm * _y_sensed, 
                //    DEFAULT_APPROACH_HEIGHT, true);

                //var FinalTeachpoint = new Teachpoint(
                //    string.Format("{0}, Rack {1}, Slot {2}", _panel.Name, _current_rack, _current_shelf), 
                //    _x_sensed + _auto_teach_config.XSensorConstTerm,  
                //    _y_sensed + _auto_teach_config.YSensorConstTerm, 
                //    _z_sensed + _auto_teach_config.ZSensorConstTerm, 
                //    DEFAULT_APPROACH_HEIGHT, true);

                // RJK the problem with the Z teachpoint is that we were looking at Z global before and we needed to be looking at Z tool space
                var FinalTeachpoint = new HiveTeachpoint(
                    string.Format("{0}, Rack {1}, Slot {2}", _panel.Name, _current_rack, _current_shelf),
                    _x_sensed + _auto_teach_config.XSensorConstTerm,  
                    _y_sensed + _auto_teach_config.YSensorConstTerm, 
                    _z_tool + _auto_teach_config.ZSensorConstTerm, 
                    DEFAULT_APPROACH_HEIGHT,
                    DEFAULT_ORIENTATION,
                    true);

                // Save un-offset teachpoint
                FinalTeachpoints.SetTeachpoint( FinalTeachpoint);

                // use sensed x & y values to locate remaining shelves
                _use_last_sensed_values = true;

                if (_calibrating)
                {
                    _log.DebugFormat( "{0},{1},{2},{3},{4},{5}", DateTime.Now.ToString("yyyyMMddhhmmss"), _initial_y, _z_sensed, _y_sensed, _x_sensed, i);
                    Fire(Trigger.RedoZCalibration);
                    return;
                }

                // we're completely done with this shelf, so increment the counters and continue
                if (++_current_shelf >= _panel.SlotCount)
                {
                    _current_shelf = 0;
                    _current_rack++;
                    _use_last_sensed_values = false; // don't use last x if we're switching columns
                }

                Fire(Trigger.Success);
            }
            catch (Exception ex)
            {
                _last_error = ex.Message;
                Fire(Trigger.Failure);
            }
            finally
            {
                _hardware.Speed = original_speed;
            }
        }

        private void Seek(IAxis axis, double increment, bool seek_in_positive_direction)
        {
            // if axis == null, we're using the Y "axis"
            if (axis == null)
            {
                _hardware.JogY(increment, seek_in_positive_direction);
                Thread.Sleep(100);
            }
            else
            {
                MotorSettings settings = axis.Settings;
                double current_pos = axis.GetPositionMM();
                axis.MoveAbsolute(current_pos + (seek_in_positive_direction ? increment : -increment), settings.Velocity / 4, settings.Acceleration / 4);
            }
        }

        private const int DEBOUNCE_DELAY = 50;
        private bool GetDebouncedInput(int bit)
        {
            var state = _io_interface.GetInput(bit);
            bool same = false;
            do
            {
                System.Threading.Thread.Sleep(DEBOUNCE_DELAY);
                var temp = _io_interface.GetInput(bit);
                same = temp == state;
                state = temp;
            } while (!same);
            return state;
        }

        private double SeekHelper( IAxis axis, double major_increment, double final_increment, int sensor_input_bit_index, bool edge_trigger_state, bool seek_in_positive_direction)
        {
            // seek in seek direction by major_increment until we see the edge_trigger_state on sensor_input_bit_index
            // then back off a major_increment
            // do this over and over, halving major_increment each time until we are moving at final_increment amounts
            //double current_pos;

            // ***** MAKE SURE WE DON'T START ON AN EDGE
            // jog in opposite seek direction until we don't see the sensor state we want
            while( GetDebouncedInput( sensor_input_bit_index) == edge_trigger_state) 
                Seek(axis, major_increment, !seek_in_positive_direction);

            const double MaxDistanceBeforeRetry = 15;

            // ***** NORMAL SEEK OPERATION
            while( major_increment > final_increment) {
                // jog until we see sensor value we want

                // Postive direction -- seek until ON transition
                int steps = 0;
                while( GetDebouncedInput( sensor_input_bit_index) != edge_trigger_state && (steps++ * major_increment) < MaxDistanceBeforeRetry) 
                    Seek(axis, major_increment, seek_in_positive_direction);

                // check to make sure we didn't go past our limit without sensing an edge
                if( (steps * major_increment) >= MaxDistanceBeforeRetry) {
                    Debug.Assert( major_increment > final_increment, "If you can't find the edge by now, something is wrong with the sensor calibration");
                    // move back and try again
                    Seek(axis, steps * major_increment, !seek_in_positive_direction);
                }
                // reduce increment for next cycle
                major_increment /= 2;

                // Negative direction -- seek until OFF transition
                steps = 0;
                while( GetDebouncedInput( sensor_input_bit_index) == edge_trigger_state && (steps++ * major_increment) < MaxDistanceBeforeRetry) 
                    Seek(axis, major_increment, !seek_in_positive_direction);

                // check to make sure we didn't go past our limit without sensing an edge
                if( (steps * major_increment) >= MaxDistanceBeforeRetry) {
                    Debug.Assert( major_increment > final_increment, "If you can't find the edge by now, something is wrong with the sensor calibration");
                    // move back and try again
                    Seek(axis, steps * major_increment, seek_in_positive_direction);
                }
                // reduce increment for next cycle
                major_increment /= 2;
            }

            // we are at the edge now!
            if( axis == null) {
                _hardware.UpdateCurrentPosition();
                return _hardware.CurrentToolPosition.Y;
            } else if ( axis == _z_axis ){
                return axis.GetPositionMM();
                // this won't work because there's all sorts of stuff that wants world Z returned
                //_hive_plugin.UpdatePositionsAndCmds();
                //return _hive_plugin.CurrentToolPosition.Z;
            } else {
                return axis.GetPositionMM();
            }
        }

        private void SeekErrorHelper( IAxis axis, double amount_to_back_off)
        {
            IDictionary< string, Trigger> label_to_trigger = new Dictionary< string, Trigger>();
            label_to_trigger[ String.Format( "Retract and start auto-teaching this shelf again, axis = {0}, backoff = {1}", ( axis == null) ? "Y" : axis.Name, amount_to_back_off)] = RetryTrigger;
            HandleLabels( label_to_trigger);
        }

        private void Tuck()
        {
            try {
                //_hive_plugin.TuckYWithPVT();
                _hardware.MoveY(-18);
                Fire( Trigger.Success);
            } catch( Exception ex) {
                _last_error = ex.Message;
                Fire( Trigger.Failure);
            }
        }

        private static void SetupLocalLogAppender()
        {
            // Create a new file appender
            var appender = new log4net.Appender.FileAppender{ Name = "auto_teach_appender", File = "auto_teach_result.txt", AppendToFile = true};
            var layout = new log4net.Layout.PatternLayout{ ConversionPattern = "%message%newline"};
            layout.ActivateOptions();

            appender.Layout = layout;
            appender.ActivateOptions();

            // add the appender to our logger            
            var logger = (log4net.Repository.Hierarchy.Logger)_log.Logger;
            logger.AddAppender(appender);
        }

        private void SaveData()
        {
            if (!_calibrating)
            {
                FinalTeachpoints = RenameTeachpoints( FinalTeachpoints, string.Format( "{0}{1}.xml", _auto_teach_config.AutoTeachFileName, _panel.Name));
            }
        } 

        public static GenericTeachpointCollection< HiveTeachpoint> RenameTeachpoints( GenericTeachpointCollection< HiveTeachpoint> points, string filename)
        {
            var renamed_teachpoints = new GenericTeachpointCollection< HiveTeachpoint>();
            var rack_configuration = new AutoRackConfiguration( points).RackConfiguration;
            foreach (var rack in rack_configuration.Keys)
                foreach (var shelf in rack_configuration[rack])
                {
                    var tp = points.GetTeachpoint( shelf.original_tp_name);
                    tp.Name = string.Format( "Rack {0}, Slot {1}", rack + 1, shelf.shelf_number + 1);
                    tp.Orientation = DEFAULT_ORIENTATION;
                    tp.AutoGenerated = true;
                    renamed_teachpoints.SetTeachpoint( tp);
                }

            GenericTeachpointCollection< HiveTeachpoint>.SaveTeachpointsToFile( filename, renamed_teachpoints);
            return renamed_teachpoints;
        }

        public double _initial_y { get; set; }

    }
}
