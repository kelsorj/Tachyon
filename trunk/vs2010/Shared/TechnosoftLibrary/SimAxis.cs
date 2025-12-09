using System;
using System.Collections.Generic;
using System.Threading;
using BioNex.Shared.Utils.PVT;
using log4net;

namespace BioNex.Shared.TechnosoftLibrary
{
    public class NonExistentAxis : IAxis
    {
        private byte ID { get; set; }
        public NonExistentAxis( byte id = 0)
        {
            ID = id;
            _settings = new MotorSettings();
        }
        private readonly object FakeLock = new object();
        public override object Lock { get { return FakeLock; } }
        public override string Name { get { return "NoAxis"; } }
        public override void DownloadSwFile(String swFilePath) {}
        public override void Pause() {}
        public override void Resume() {}
        public override void ResetPause() {}
        public override int AxisTimeoutSecs { get { return 0; } set { } }
        public override int AxisTimeoutTimerSecs { get { return 0; } }
        public override void ReadExtI2CPage(byte page, out ulong data) { data = 0; }
        public override void WriteExtI2CPage(byte page, ulong data) {}
        public override void ReadExtI2CByte(byte addr, out byte data) { data = 0; }
        public override void WriteExtI2CByte(byte addr, byte data) {}
        public override string ReadApplicationID() { return "NonExistent Axis"; }
        public override string ReadSerialNumber() { return "0123456789"; }
        public override void WriteSerialNumber(string SerialNumberStr) { }
        public override Int32 ReadLongVarEEPROM(String ptr_name) { return 0; }
        public override Int16 ReadIntVarEEPROM(String ptr_name) { return 0; }
        public override double ReadFixedVarEEPROM(String ptr_name) { return 0.0; }
        public override void WriteLongVarEEPROM(String ptr_name, Int32 long_var) { }
        public override void WriteIntVarEEPROM(String ptr_name, Int16 int_var) { }
        public override void WriteFixedVarEEPROM(String ptr_name, double fixed_var) { }

        public override void Enable(bool enable, bool blocking) { }
        public override void RefreshServoTimeout() {}
        public override void Home(bool wait_for_complete) {}
        public override void SendResetAndHome() {}
        public override void WaitForHomeResult( long timeout_ms, bool check_is_homing) {}
        protected override void MoveAbsoluteHelper(double mm, double velocity, double acceleration, int jerk, int jerk_min, bool wait_for_move_complete, double move_done_window_mm, short settling_time_ms, bool use_TS_MC, bool use_trap) {}
        public override void MoveRelative(double mm_or_ul) {}
        public override void MoveRelative(double mm_or_ul, double velocity, double acceleration, int jerk) {}
        public override void MoveAbsoluteTorqueLimited(double mm, double velocity, double acceleration, int jerk, int jerk_min, short settling_time_ms, double max_Amps, double IMaxPS_Amps, double torque_limiting_window_rel_mm) {}
        public override bool IsHomed { get { return true; } }
        public override bool ReadHomeSensor() { return true; }
        public override int GetPositionCounts() { return 0; }
        public override double GetPositionMM() { return 0; }
        public override short GetAnalogReading(uint channel_0_based) { return 0; }
        public override void SetOutput(uint bit_0_based, bool logic_high) {}
        public override void ReadStatus(short SelIndex, out ushort Status) { Status = 0;}
        public override bool GetIntVariable(string pszName, out short value) { value = 0; return true; }
        public override bool GetLongVariable(string pszName, out int value) { value = 0; return true; }
        public override bool GetFixedVariable(string pszName, out double value) { value = 0; return true; }
        public override bool GetInput(byte nIO, out byte InValue) { InValue = 0; return true; }
        public override bool SetIntVariable(string pszName, short value) { return true; }
        public override bool SetLongVariable(string pszName, int value) { return true; }
        public override bool SetFixedVariable(string pszName, double value) { return true; }
        public override bool SetCurrentAmpsCmdLimit(double current_amps) { return true; }
        public override bool GetCurrentAmpsCmdLimit(out double current_amps) { current_amps = 0; return true; }
        public override void ResetFaults() {}
        public override void ResetDrive() {}
        public override void ResetFaultsOnAllAxes() {}
        public override List<string> GetFaults() { return new List<string>(); }
        public override List<string> GetFaults(ushort mask) { return new List<string>(); }
        public override double GetCountsPerEngineeringUnit() { return 0; }
        public override int ConvertToCounts(double mm_or_ul) { return 0; }
        public override double ConvertCountsToEng(int counts) { return 0; }
        public override short ConvertToTicks(short time_ms) { return 0; }
        public override string GetConversionFormula() { return ""; }
        public override void SetConversionFormula(string formula) {}
        public override void Stop() {}
        public override void AddToGroup(byte group_id) {}
        public override void RemoveFromGroup(byte group_id) {}
        public override byte GetID() { return ID; }
        public override bool IsMoveComplete() { return true; }
        public override string GetError() { return ""; }
        public override string GetError(ushort mask) { return ""; }
        public override void MasterCamOnOff(byte slave_axis_id, bool on_off) {}
        public override void SlaveCamOnOff(ushort cam_address, bool on_off, double max_speed_iu, int cam_pos_offset_iu) {}
        public override void StartLogging() {}
        public override void WaitForLoggingComplete(string filepath) {}
        public override bool IsHoming() { return true; }
        public override void SetSpeedFactor(int speed) {}
        public override double GetSpeedFactor() { return 0; }
        public override void SetupBlendedMove(double position, bool set_event_on_complete, bool use_trap) {}
        public override void StartBlendedMove() {}
        public override void WaitForBlendedMoveComplete(IAxis master_axis) {}
        public override bool IsOn() { return true; }
        public override void CallFunctionWithPeriodicActions(string function_name, int first_action_pos, int interval, short number_of_actions, double velocity, double accel, int jerk) {}
        public override bool MoveSpeed(double speed_deg_per_sec, double accel_deg_per_sec2, bool wait_for_traj_complete) { return true; }
        public override double GetActualSpeedIU() { return 0; }
        public override double GetActualSpeedDegPerSec() { return 0; }
        public override bool IsTargetReached() { return true; }
        public override void CallFunction(string function_name) {}
        public override int CallFunctionAndWaitForDone(string function_name, TimeSpan ts_timeout, bool return_func_done = false) { return 1; }
        public override void GotoFunction(string function_name) {}
        public override void GotoFunctionAndWaitForDone(string function_name, TimeSpan ts_timeout) {}
        public override int PVTNumPointsBuffered() { return 0; }
        protected override void PVTWaitForSetupComplete() {}
        protected override void PVTGroupSetup( byte group_id) {}
        protected override void PVTGroupStartTrajectory( byte group_id) {}
        public override void ZeroIqref() {}
        public override bool IsAxisOnFlag { get { return true; } }
        protected override void TurnAxisOnIfNecessary(bool blocking) {}
        public override bool ReadMotionCompleteFlag() { return true; }
        public override bool ReadTrajectoryCompleteFlag() { return true; }
        protected override int HomingStatus { get { return 0; } }
        protected override void PVTAddPoints(PVTTrajectory pvt_trajectory) {}

        public override void AbortCancellableCall() {}
        public override void SendTmlCommands( string commands) {}
    }

    public class SimAxis : IAxis
    {
#region PVT_IMPLEMENTATION
        // DKM 2011-03-26 total HACK to make simulation work with PVT
        private bool _called_pvt_setup = false;
        public override int PVTNumPointsBuffered() { return 0; }
        protected override void PVTWaitForSetupComplete() { _called_pvt_setup = true; }
        protected override void PVTAddPoints( PVTTrajectory pvt_trajectory) {}
        protected override void PVTGroupSetup( byte group_id) {}
        protected override void PVTGroupStartTrajectory( byte group_id) {}
#endregion

        private int _homing_status = -2;
        private int _position = 0;
        private readonly byte _axis_id = 0;
        private bool _servo_on = false;
        private readonly ILog _log;
        public int SimulatedMoveTime { get; private set; }
        // DKM 2011-08-16 I'm not sure about the way HiG support was added.  MoveSpeed assumes that we are moving in deg/sec, but it should probably have been more general purpose
        private double _current_speed_deg_per_sec = 0;
        private readonly Dictionary<string,int> _long_variables = new Dictionary<string,int>();

        public override object Lock { get { return new object(); } }

        public SimAxis( byte axis_id, MotorSettings settings, bool start_homed = false)
        {
            _axis_id = axis_id;
            _settings = settings;
            _log = LogManager.GetLogger( "axis_" + axis_id);
            _abort_move_speed_event = new ManualResetEvent(false);
            if( start_homed){
                _homing_status = 0;
            }
        }

        public override void DownloadSwFile(String swFilePath) { return; }

        public override string Name
        {
            get
            {
                return String.Format("sim{0}{1}", _settings.AxisName, _axis_id);
            }
        }

        // The following functions deal with the axis timeout timer for power savings
        public override int AxisTimeoutSecs { get { return -1; } set {/* do nothing */} }
        public override int AxisTimeoutTimerSecs { get { return -1; } }

        public override void ReadExtI2CPage(Byte page, out UInt64 data)
        {
            // do nothing
            data = 0x0102030405060708; // 8 bytes of data
        }

        public override void WriteExtI2CPage(Byte page, UInt64 data)
        {
            // do nothing
        }

        public override void ReadExtI2CByte(Byte addr, out Byte data)
        {
            // do nothing
            data = 0x01; // 1 byte of data
        }

        public override void WriteExtI2CByte(Byte addr, Byte data)
        {
            // do nothing
        }

        public override String ReadApplicationID () { return "Simulated Axis"; }
        public override String ReadSerialNumber() { return "Sim001"; }
        public override void WriteSerialNumber(string SerialNumberStr) {  }
        public override Int32 ReadLongVarEEPROM(String ptr_name) { return 0; }
        public override Int16 ReadIntVarEEPROM(String ptr_name) { return 0; }
        public override double ReadFixedVarEEPROM(String ptr_name) { return 0.0; }
        public override void WriteLongVarEEPROM(String ptr_name, Int32 long_var) { }
        public override void WriteIntVarEEPROM(String ptr_name, Int16 int_var) { }
        public override void WriteFixedVarEEPROM(String ptr_name, double fixed_var) { }


        public override byte GetID()
        {
            return _axis_id;
        }

        public override void Enable( bool enable, bool blocking)
        {
            _servo_on = enable;
            OnEnableComplete( this, new EnableEventArgs( enable));
        }

        public override void AbortCancellableCall() {}

        //---------------------------------------------------------------------
        public override void RefreshServoTimeout() // refresh servo timeout parameter so servo doesn't time out early (used when jogging and teaching)
        {
            // nothing to see here
        }

        public override void Home( bool wait_for_complete) 
        {
            OnHomeComplete( this, new MotorEventArgs( String.Format( "{0} axis homing complete", Name)));
            _homing_status = 0;
            _position = 0;
        }

        public override void SendResetAndHome()
        {
            OnHomeComplete( this, new MotorEventArgs( String.Format( "{0} axis homing complete", Name)));
            _homing_status = 0;
            _position = 0;
        }

        public override void WaitForHomeResult( long timeout_ms, bool check_is_homing)
        {
        }

        public override bool IsAxisOnFlag { get { return true; } }

        protected override void TurnAxisOnIfNecessary(bool blocking)
        {
            Enable( true, true);
        }

        public override bool ReadMotionCompleteFlag()
        {
            if( _called_pvt_setup) {
                _called_pvt_setup = false;
                return false;
            }
            return true;
        }
        public override bool ReadTrajectoryCompleteFlag() { return true; }

        protected override void MoveAbsoluteHelper( double mm /* not ul!! */, double velocity, double acceleration, int jerk, int jerk_min, bool wait_for_move_complete, double move_done_window_mm, short settling_time_ms, bool use_TS_MC, bool use_trap)
        {
            if ( mm < _settings.MinLimit && Math.Abs( mm - _settings.MinLimit) > 0.001)
                throw new AxisException( this, String.Format("Cannot move past minimum travel limit of {0:0.000} (commanded position was {1:0.000}mm)", _settings.MinLimit, mm));
            else if ( mm > _settings.MaxLimit && Math.Abs( mm - _settings.MaxLimit) > 0.001)
                throw new AxisException( this, String.Format("Cannot move past maximum travel limit of {0:0.000} (commanded position was {1:0.000}mm)", _settings.MaxLimit, mm));

            _log.DebugFormat( "MoveAbsoluteHelper({0:0.000} mm) called in SimAxis", mm);

            // calculate the amount of time it would take to make the specified move
            // use trapezoidal profile calculations
            // first need to create a new MotorSettings object with the passed in parameters so we can calculated the time
            double move_delta = Math.Abs( mm - GetPositionMM());
            int move_time_ms = new MotorSettings( "temp", velocity, acceleration, jerk, _settings.EncoderLines,
                                                  _settings.GearRatio, _settings.MinLimit, _settings.MaxLimit,
                                                  _settings.MoveDoneWindow, _settings.SettlingTimeMS,
                                                  _settings.SlowLoopServoTimeS).CalculateTrapezoidalMoveTime( move_delta);
            // sleep for this amount of time if we want to wait for move completion
            if( wait_for_move_complete)
                Thread.Sleep( Math.Max( 0, move_time_ms));
            
            // sleep another few milliseconds if using Technosoft move complete based on actual position, since it will be at least this long after TPOS is done moving
            if (use_TS_MC)
                Thread.Sleep( settling_time_ms);
            // now we're "done" with the move
            OnMoveComplete(this, new MotorEventArgs(String.Format("{0} axis move to {1}mm complete", Name, mm)));
            _position = (int)( mm * GetCountsPerEngineeringUnit());
        }

        public override void MoveAbsoluteTorqueLimited(double mm, double velocity, double acceleration, int jerk, int jerk_min, short settling_time_ms, double max_Amps, double IMaxPS_Amps, double torque_limiting_window_rel_mm)
        {
            MoveAbsolute( mm, velocity, acceleration, jerk, 1, true, 0, settling_time_ms, use_trap: UseTrapezoidalProfileByDefault);
        }

        public override void MoveRelative( double mm_or_ul)
        {
            
            // if there's a conversion formula, then relative moves need to be careful and
            // have to have the current position converted back into uL.  Possible loss of
            // accuracy here, but probably not too bad.
            if( GetConversionFormula() != null) {
                double current_position_uL = GetPositionUl();
                MoveAbsolute( current_position_uL + mm_or_ul);
            } else {
                double current_position_mm = _position / GetCountsPerEngineeringUnit();
                MoveAbsolute( current_position_mm + mm_or_ul);
            }
        }

        public override void MoveRelative( double mm_or_ul, double velocity, double acceleration, int jerk)
        {
            double current_position_mm = _position / GetCountsPerEngineeringUnit();
            MoveAbsolute( current_position_mm, velocity, acceleration, jerk);
        }

        public override bool IsHomed { get { return _homing_status == 0; } }

        protected override int HomingStatus { get { return 0; } }

        public override bool ReadHomeSensor()
        {
            return _position == 0;
        }

        public override int GetPositionCounts()
        {
            return _position;
        }

        public override double GetPositionMM()
        {
            return (double)_position / GetCountsPerEngineeringUnit();
        }

        // execute a speed move (jog at speed)
        public override bool MoveSpeed(double speed_deg_per_sec, double accel_deg_per_sec2, bool wait_for_traj_complete) 
        {
            _log.DebugFormat( "MoveSpeed({0:0.000} deg/s) called in SimAxis", speed_deg_per_sec);

            if (speed_deg_per_sec > _settings.Velocity)
            {
                speed_deg_per_sec = _settings.Velocity;
                _log.DebugFormat( "MoveSpeed(...) clipping velocity to {0:0.000} deg/s", speed_deg_per_sec);
            }

            if (accel_deg_per_sec2 > _settings.Acceleration)
            {
                accel_deg_per_sec2 = _settings.Acceleration;
                _log.DebugFormat( "MoveSpeed(...) clipping acceleration to {0:0.000} deg/s^2", accel_deg_per_sec2);
            }

            _abort_move_speed_event.Reset();
            if (wait_for_traj_complete)
            {
                // DKM 2011-08-16 to support more accurate simulation, we need to save the last speed commanded.
                //                Otherwise, accel takes time, but decel is instantaneous.
                double speed_change = Math.Abs( speed_deg_per_sec - _current_speed_deg_per_sec);

                TimeSpan ts_accel = TimeSpan.FromSeconds(speed_change / accel_deg_per_sec2);
                DateTime start = DateTime.Now;
                double starting_speed_deg_per_sec = _current_speed_deg_per_sec;
                while (DateTime.Now - start < ts_accel && !_abort_move_speed_event.WaitOne( 0))
                {
                    // try to simulate accel / decel
                    double time_change = (DateTime.Now - start).TotalSeconds;
                    double speed_delta = accel_deg_per_sec2 * time_change;
                    _current_speed_deg_per_sec = starting_speed_deg_per_sec + (speed_deg_per_sec < starting_speed_deg_per_sec ? -speed_delta : speed_delta);
                    Thread.Sleep(10); // sleep for a little while we wait for the accel period
                }

                if (_abort_move_speed_event.WaitOne(0))
                {
                    _log.DebugFormat( "MoveSpeed aborted in SimAxis, last speed was {0:0.000} deg/s", _current_speed_deg_per_sec);
                    return false;
                }

                _log.DebugFormat( "MoveSpeed({0:0.000} deg/s) at speed now in SimAxis", speed_deg_per_sec);
                _current_speed_deg_per_sec = speed_deg_per_sec;
            }

            return true;
        }
        public override double GetActualSpeedIU() { return 0.0; } // wrapper around TS_GetFixedVariable("ASPD")
        public override double GetActualSpeedDegPerSec() { return _current_speed_deg_per_sec; } // calls GetActualSpeedIU and converts to eng units for the load

        public override short GetAnalogReading( uint channel_0_based)
        {
            return 0;
        }

        public override void SetOutput( uint bit_0_based, bool logic_high) {}
        public override void ReadStatus ( Int16 SelIndex, out UInt16 Status) { Status = 0; } // wrapper around TS_ReadStatus
        public override bool GetIntVariable( String pszName, out Int16 value) { value = 0; return true; } // wrapper around TS_GetIntVariable
        public override bool GetLongVariable( String pszName, out Int32 value)
        {
            value = 0;
            if( _long_variables.ContainsKey( pszName))
                value = _long_variables[pszName];
            return true;
        } // wrapper around TS_GetLongVariable
        public override bool GetFixedVariable( String pszName, out Double value) { value = 0.0; return true; } // wrapper around TS_GetFixedVariable
        public override bool GetInput(Byte nIO, out Byte InValue) { InValue = 0; return true; } // wrapper around TS_GetInput

        public override bool SetIntVariable(String pszName, Int16 value) { return true; } // wrapper around TS_SetIntVariable
        public override bool SetLongVariable( String pszName, Int32 value) { return true; } // wrapper around TS_SetLongVariable
        public override bool SetFixedVariable( String pszName, Double value) { return true; } // wrapper around TS_SetFixedVariable

        public override bool SetCurrentAmpsCmdLimit (double current_amps) { return true; } // sets the max possible commanded current (torque) on an axis
        public override bool GetCurrentAmpsCmdLimit (out double current_amps) {current_amps = 1.69; return true; } // gets the max possible commanded current (torque) already set on an axis

        public override void ResetFaults() {}
        public override void ResetDrive() {}
        public override void ResetFaultsOnAllAxes() {}
        public override List<string> GetFaults() { return new List<string>(); }
        public override List<string> GetFaults(UInt16 mask) { return new List<string>(); }
        public override double GetCountsPerEngineeringUnit()
        { 
            return 4096 * 4;
        }
        public override int ConvertToCounts( double mm_or_ul)
        {
            return (int)(GetCountsPerEngineeringUnit() * mm_or_ul);
        }

        // does a conversion from quad counts to Engineering units
        public override double ConvertCountsToEng(int counts)
        {
            return ((double)counts / (double)GetCountsPerEngineeringUnit());
        }

        public override short ConvertToTicks( short time_ms)
        {
            return time_ms;
        }

        public override string GetConversionFormula()
        {
            return _conversion_formula;
        }

        public override void SetConversionFormula( string formula)
        {
            _conversion_formula = formula;
        }

        public override void Stop() { }
        public override void AddToGroup( byte group_id) {}
        public override void RemoveFromGroup( byte group_id) {}
        public override bool IsMoveComplete()
        {
            return true;
        }
        public override bool IsTargetReached() { return true; }
        public override string GetError() { return ""; }
        public override string GetError(UInt16 mask) { return ""; }

        public override void MasterCamOnOff(byte slave_axis_id, bool on_off) {}
        public override void SlaveCamOnOff (UInt16 cam_address, bool on_off, double max_speed_iu, int cam_pos_offset_iu) {}

        public override void StartLogging() {}
        public override void WaitForLoggingComplete( string filepath) {}

        public override bool IsHoming() { return false; }
        public override void SetSpeedFactor( int speed) {}
        public override double GetSpeedFactor() { return 1.0; }

        public override bool IsOn()
        {
            return _servo_on;
        }

        public override void SetupBlendedMove( double position, bool set_event_on_complete, bool use_trap)
        {
            if( position < _settings.MinLimit) {
                throw new AxisException( this, String.Format("Cannot move past minimum travel limit of {0:0.000}", _settings.MinLimit));
            } else if( position > _settings.MaxLimit) {
                throw new AxisException( this, String.Format("Cannot move past maximum travel limit of {0:0.000}", _settings.MaxLimit));
            }        

            // not a perfect implementation, but wait for both moves to complete
            double move_delta = Math.Abs( position - GetPositionMM());
            SimulatedMoveTime = Settings.CalculateTrapezoidalMoveTime( move_delta);
            _position = ConvertToCounts( position);
        }

        public override void StartBlendedMove() {}
        public override void WaitForBlendedMoveComplete( IAxis master_axis)
        {
            SimAxis master = master_axis as SimAxis;
            // if for some reason the other axis isn't simulated (but it should be!) just use
            // the simulated time for this axis.
            if( master == null) {
                // Thread.Sleep( SimulatedMoveTime);
                return;
            }
            // int move_time = Math.Max( SimulatedMoveTime, master.SimulatedMoveTime);
            // Thread.Sleep( SimulatedMoveTime + master.SimulatedMoveTime);
        }

        public override void ZeroIqref() {}

        public override void CallFunctionWithPeriodicActions( string function_name, int first_action_pos, int interval,
                                                              short number_of_actions, double velocity, double accel, int jerk)
        {
        }

        public override void CallFunction(string function_name) { _long_variables["func_done"] = 1; } // non-blocking call which simply fires off a TS_CALL command

        /// <summary>
        //  blocking call that waits for func_done variable to be non-zero
        /// </summary>
        /// <param name="function_name"></param>
        /// <param name="ts_timeout"></param>
        /// <param name="return_func_done"></param>
        public override int CallFunctionAndWaitForDone(string function_name, TimeSpan ts_timeout, bool return_func_done=false)
        {
            // DKM 2011-08-23 since spinning down is now handled in a TML function, I needed to hack this together to make simulation work
            if( function_name == "func_spin_down") {
                MoveSpeed(0.0, 1080, true);
            } else if( function_name == "func_open_shield") {
                Thread.Sleep( 5000);
            }

            return 1;
        }

        public override void GotoFunction(string function_name) {} // non-blocking call which simply fires off a TS_GOTO command
        public override void GotoFunctionAndWaitForDone(string function_name, TimeSpan ts_timeout) { } // blocking call that waits for func_done variable to be non-zero

        public override void SendTmlCommands( string commands) {}
    }
}
