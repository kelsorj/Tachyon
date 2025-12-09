using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using BioNex.Shared.Utils;
using BioNex.Shared.Utils.PVT;
using log4net;

namespace BioNex.Shared.TechnosoftLibrary
{
    public class TSAxis : IAxis
    {
#if !TML_SINGLETHREADED
        private ITMLChannel Channel { get; set; }
#else
        private class Channel : TML.TMLLib {}
#endif

#region PVT_IMPLEMENTATION
        //-------------------------------------------------------------------
        // private short _pvt_integrity_counter { get; set; }
        //-------------------------------------------------------------------
        public override int PVTNumPointsBuffered()
        {
            lock( _lock){
                if( !Channel.TS_SelectAxis( _axis_id))
                    throw new AxisException( this, String.Format( "Could not select axis {0}: {1}", _axis_id, GetTSError()));
                const string buffer_free_space_query_string = "var_i1=0x85d;var_i2=(var_i1),dm";
                if( !Channel.TS_Execute( buffer_free_space_query_string))
                    throw new AxisException( this, String.Format( "Could not query for number of pvt points buffered on axis {0}: {1}", _axis_id, GetTSError()));
                short buffer_free_space;
                GetIntVariable( "var_i2", out buffer_free_space);
                short buffer_allocated_space;
                GetIntVariable( "PVTBUFLEN", out buffer_allocated_space);
                return ( int)( buffer_allocated_space - buffer_free_space);
            }
        }
        //-------------------------------------------------------------------
        protected override void PVTWaitForSetupComplete()
        {
            // DKM 2011-05-28 added timeout, since during Pause test, I encountered a case where
            //                I would get stuck here

            DateTime start = DateTime.Now;
            const double timeout_sec = 10;
            while(( DateTime.Now - start).TotalSeconds < timeout_sec){
                int func_done;
                GetLongVariable( "func_done", out func_done);
                if( func_done == 10){
                    break;
                }
                if( func_done == 11){
                    throw new AxisException( this, String.Format( "Could not set up PVT because axis {0} not homed", _axis_id));
                }
                Thread.Sleep( 5);
            }

            // check for timeout condition
            if( (DateTime.Now - start).TotalSeconds >= timeout_sec)
                throw new AxisException( this, String.Format( "Timed out while waiting for PVT setup on axis {0}", _axis_id));
        }
        //-------------------------------------------------------------------
        protected override void PVTAddPoints( PVTTrajectory pvt_trajectory)
        {
            // get the points to send.
            IEnumerable< PVTPoint> pvt_points = pvt_trajectory.GetPVTPoints();
            // if no points to send, then do nothing.
            short num_points_to_send = ( short)( pvt_points.Count());
            if( num_points_to_send == 0){
                Debug.Assert( false, "pvt_trajectory contained no points");
                return;
            }
            if( num_points_to_send == 1){
                Debug.Assert( false, "pvt_trajectory consists only of initial point");
                return;
            }
            // take note of previous controller destination.
            int previous_controller_destination;
            GetLongVariable( "CPOS", out previous_controller_destination);
            // initialize string builder.
            StringBuilder sb = new StringBuilder();
            // set my_var1 to initial available pvt buffer space.
            sb.Append( "var_i1=0x85d;my_var1=(var_i1),dm;");
            // declare trajectory_destination and initialize pvt_integrity_counter to -1.
            int trajectory_destination = 0;
            short pvt_integrity_counter = -1;
            // send each pvt point:
            foreach( PVTPoint pvt_point in pvt_points){
                // convert the position from engineering units to controller units.
                trajectory_destination = ConvertToCounts( pvt_point.Position);
                // don't let trajectory_destination == previous_controller_destination for check below*.
                trajectory_destination += ( trajectory_destination != previous_controller_destination) ? 0 : 1;
                // send the pvt point:
                if( pvt_integrity_counter < 0){
                    // special case: first pvt point establishes the initial position.
                    sb.AppendFormat( "PVTPOS0,dm={0}L;", trajectory_destination);
                } else{
                    // common case: subsequent pvt points specify desired positions and velocities to be reached after specified duration.
                    double pvt_velocity = (( double)ConvertToCounts( pvt_point.Velocity)) / (( double)ConvertToTicks( 1000));
                    uint pvt_time = ( uint)ConvertToTicks(( short)( Math.Round( pvt_point.Time * 1000.0)));
                    sb.AppendFormat( "PVTP {0},{1:0.00000},{2},{3};", trajectory_destination, pvt_velocity, pvt_time, pvt_integrity_counter % 128);
                }
                // increment the pvt_integrity_counter.
                pvt_integrity_counter++;
            }
            // set my_var2 to final available pvt buffer space.
            sb.Append( "var_i1=0x85d;my_var2=(var_i1),dm;");
            // set my_var1 to pvt buffer space used by this attempt to add pvt points.
            sb.Append( "my_var1-=my_var2;");

            lock( _lock){
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format( "Could not select axis {0}: {1}", _axis_id, GetTSError()));
                // actually send pvt points to controller.
                _log.Debug( sb.ToString());
                if( !Channel.TS_Execute( sb.ToString()))
                    throw new AxisException( this, String.Format( "Could not send PVT points to axis {0}: {1}", _axis_id, GetTSError()));
                // check 1:
                // make sure number of points to send is exactly one greater than number of points buffered.
                // (initial point is set into PVTPOS0 and not buffered, so there is one less point buffered.)
                short num_points_buffered;
                GetIntVariable( "my_var1", out num_points_buffered);
                if( num_points_to_send != num_points_buffered + 1)
                    throw new PVTPointsRejectedException( this, String.Format( "Tried to send {0} but received {1} PVT points on axis {2}: {3}", num_points_to_send, num_points_buffered + 1, _axis_id, GetTSError()));
                // check 2:
                // make sure controller_destination matches trajectory_destination.
                // if we fail here, then controller received pvt points (as evidenced by check 1) but motion profiler isn't "listening."
                // *we need to make sure trajectory_destination never equals previous_controller_destination otherwise we'd be susceptible to false-negative checks.
                int controller_destination;
                GetLongVariable( "CPOS", out controller_destination);
                if( controller_destination != trajectory_destination)
                    throw new AxisException( this, String.Format( "Controller destination doesn't match trajectory destination on axis {0}.", _axis_id));
            }
        }
        //-------------------------------------------------------------------
        protected override void PVTGroupSetup( byte group_id)
        {
            _log.Debug("start PVT group setup");
            lock( _lock){
                if( !Channel.TS_SelectGroup( group_id))
                    throw new AxisException( this, String.Format( "Could not select group {0}: {1}", group_id, GetTSError()));
                if( !Channel.TS_CALL_Label( "func_my_pvtsetup"))
                    throw new AxisException( this, String.Format( "Could not execute group stop {0}: {1}", group_id, GetTSError()));
            }
            _log.Debug("finish PVT group setup");
        }
        //-------------------------------------------------------------------
        protected override void PVTGroupStartTrajectory( byte group_id)
        {
            lock( _lock){
                if( !Channel.TS_SelectGroup( group_id))
                    throw new AxisException( this, String.Format( "Could not select group {0}: {1}", group_id, GetTSError()));
                if (!Channel.TS_Execute("SRB SRL, 0xFBFF, 0x0000; UPD; !MC;"))
                    throw new AxisException( this, String.Format( "Could not start PVT trajectory to group {0}: {1}", group_id, GetTSError()));
            }
        }
        //-------------------------------------------------------------------
        /* deprecated in deference to PVTAddPoints.
        protected override void PVTAddFirstPoint( TrajectoryPoint pvt_datum, bool absolute_mode)
        {
            _pvt_integrity_counter = 0;
            var pvt_status_variable = PVTGetStatusVariable();
            lock( _lock){
                if( !TMLLib.TS_SelectAxis( _axis_id))
                    throw new AxisException( this, String.Format( "Could not select axis {0}: {1}", _axis_id, GetTSError()));
                int position = ConvertToCounts( pvt_datum.Position);
                double velocity = (( double)ConvertToCounts( pvt_datum.Velocity)) / (( double)ConvertToTicks( 1000));
                int time = ConvertToTicks(( short)( pvt_datum.Time * 1000.0));
                // _log.DebugFormat( "Calling TS_SendPVTFirstPoint POS0={0}, POS1={1}, VEL={2}, T={3}", GetPositionCounts(), position, velocity, time);
                if (!TMLLib.TS_SendPVTFirstPoint(position, velocity, time, (short)(_pvt_integrity_counter++ % 128), absolute_mode ? TMLLib.ABSOLUTE_POSITION : TMLLib.RELATIVE_POSITION, GetPositionCounts(), TMLLib.UPDATE_NONE, TMLLib.FROM_REFERENCE))
                    throw new AxisException( this, String.Format( "Could not send first PVT point to axis {0}: {1}", _axis_id, GetTSError()));
            }
        }
        */
        //-------------------------------------------------------------------
        /* deprecated in deference to PVTAddPoints.
        protected void PVTAddPointsTSLIB( Trajectory pvt_data)
        {
            int pvt_buffer_size = 128;
            while( true){
                // if there are no points to send, then bail.
                if( pvt_data.Count == 0){
                    return;
                }
                // get the PVT status variable.
                short pvt_status_variable = PVTGetStatusVariable();
                // DKM 2011-03-15
                // PVTSTS.15 : 0 = buffer is not empty, 1 = buffer is empty
                // PVTSTS.14 : 0 = buffer is not low, 1 = buffer is low
                // PVTSTS.13 : 0 = buffer is not full, 1 = buffer is full
                // PVTSTS.12 : 0 = no integrity counter error, 1 = integrity counter error
                // PVTSTS.11 : 0 = drive has kept PVT motion mode after PVT buffer empty, because the velocity of the last PVT point != 0
                // PVTSTS.10 : 0 = normal operation, data received is PVT
                // PVTSTS.6-0: integrity counter value
                // if the PVT buffer is full, then bail.
                bool pvt_buffer_is_full = (( pvt_status_variable & 0x2000) == 0x2000);
                if( pvt_buffer_is_full){
                    return;
                }
                // if the PVT buffer is low...
                bool pvt_buffer_is_low = (( pvt_status_variable & 0x4000) == 0x4000);
                if( pvt_buffer_is_low){
                    // then attempt a bulk fill.
                    int minimum_buffer_available = 3 * ( pvt_buffer_size / 4);
                    lock( _lock){
                        if( !TMLLib.TS_SelectAxis( _axis_id))
                            throw new AxisException( this, String.Format( "Could not select axis {0}: {1}", _axis_id, GetTSError()));
                        for( int loop = 0; loop < minimum_buffer_available && pvt_data.Count > 0; ++loop){
                            TrajectoryPoint pvt_datum = pvt_data.Dequeue();
                            int position = ConvertToCounts( pvt_datum.Position);
                            double velocity = (( double)ConvertToCounts( pvt_datum.Velocity)) / (( double)ConvertToTicks( 1000));
                            uint time = ( uint)ConvertToTicks(( short)( Math.Round( pvt_datum.Time * 1000.0)));
                            // _log.DebugFormat( "Calling TS_SendPVTPoint POS={0}, VEL={1}, T={2}", position, velocity, time);
                            if( !TMLLib.TS_SendPVTPoint( position, velocity, time, (short)(_pvt_integrity_counter++ % 128)))
                                throw new AxisException( this, String.Format( "Could not send PVT point to axis {0}: {1}", _axis_id, GetTSError()));
                        }
                    }
                } else{
                    // else attempt a single fill.
                    TrajectoryPoint pvt_datum = pvt_data.Dequeue();
                    int position = ConvertToCounts( pvt_datum.Position);
                    double velocity = (( double)ConvertToCounts( pvt_datum.Velocity)) / (( double)ConvertToTicks( 1000));
                    uint time = ( uint)ConvertToTicks(( short)( Math.Round( pvt_datum.Time * 1000.0)));
                    // _log.DebugFormat( "Calling TS_SendPVTPoint POS={0}, VEL={1}, T={2}", position, velocity, time);
                    lock( _lock){
                        if( !TMLLib.TS_SelectAxis( _axis_id))
                            throw new AxisException( this, String.Format( "Could not select axis {0}: {1}", _axis_id, GetTSError()));
                        if( !TMLLib.TS_SendPVTPoint(position, velocity, time, (short)(_pvt_integrity_counter++ % 128)))
                            throw new AxisException( this, String.Format( "Could not send PVT point to axis {0}: {1}", _axis_id, GetTSError()));
                    }
                }
            }
        }
        */
        //-------------------------------------------------------------------
#endregion

        private readonly byte _axis_id;
        private readonly object _lock; // used to prevent multiple threads from calling TML-LIB simultaneously
        private volatile bool _is_homing; // needs to be volatile since multiple threads check into this variable

        private readonly object _speed_lock = new object();
        private double _speed_percentage;
        private readonly double _controller_peak_current; // this is the published peak current on a controller (6.11A for 2304, 16.5A for 3605 and IDM640) 
        // DKM 2011-11-14 caching the setup file path so that we can report a more specific error if we ever load the wrong file again
        private readonly string _setup_file;

        private readonly ILog _log;

        public const UInt16 TMLLIB_SRH_BIT_FAULT                 = (1 << 15); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_SRH_BIT_INCAM                 = (1 << 14); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_SRH_BIT_INGEAR                = (1 << 12); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_SRH_BIT_I2TWARNINGDRIVE       = (1 << 11); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_SRH_BIT_I2TWARNINGMOTOR       = (1 << 10); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_SRH_BIT_TARGETREACHED         = (1 <<  9); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_SRH_BIT_CAPTUREEVENTINTERRUPT = (1 <<  8); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_SRH_BIT_LSNEVENTINTERRUPT     = (1 <<  7); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_SRH_BIT_LSPEVENTINTERRUPT     = (1 <<  6); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_SRH_BIT_AUTORUNENABLED        = (1 <<  5); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_SRH_BIT_OVERPOSITIONTRIGGER4  = (1 <<  4); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_SRH_BIT_OVERPOSITIONTRIGGER3  = (1 <<  3); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_SRH_BIT_OVERPOSITIONTRIGGER2  = (1 <<  2); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_SRH_BIT_OVERPOSITIONTRIGGER1  = (1 <<  1); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_SRH_BIT_ENDINITEXECUTED       = (1 <<  0); // At least this is the case for 3605, 2403, and IDM680

        public const UInt16 TMLLIB_SRL_BIT_AXISON                = (1 << 15); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_SRL_BIT_EVENTSETOCCURRED      = (1 << 14); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_SRL_BIT_MOTIONCOMPLETE        = (1 << 10); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_SRL_BIT_HOMINGCALLSACTIVE     = (1 <<  8); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_SRL_BIT_HOMINGCALLSWARNING    = (1 <<  7); // At least this is the case for 3605, 2403, and IDM680

        public const UInt16 TMLLIB_MER_BIT_ENABLEINPUTINACTIVE   = (1 << 15); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_MER_BIT_COMMANDERROR          = (1 << 14); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_MER_BIT_UNDERVOLTAGE          = (1 << 13); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_MER_BIT_OVERVOLTAGE           = (1 << 12); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_MER_BIT_OVERTEMPDRIVE         = (1 << 11); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_MER_BIT_OVERTEMPMOTOR         = (1 << 10); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_MER_BIT_I2T                   = (1 <<  9); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_MER_BIT_OVERCURRENT           = (1 <<  8); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_MER_BIT_LSNACTIVE             = (1 <<  7); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_MER_BIT_LSPACTIVE             = (1 <<  6); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_MER_BIT_POSITIONWRAPAROUND    = (1 <<  5); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_MER_BIT_SERIALCOMMERROR       = (1 <<  4); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_MER_BIT_CONTROLERROR          = (1 <<  3); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_MER_BIT_INVALIDSETUPDATA      = (1 <<  2); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_MER_BIT_SHORTCIRCUIT          = (1 <<  1); // At least this is the case for 3605, 2403, and IDM680
        public const UInt16 TMLLIB_MER_BIT_CANBUSERROR           = (1 <<  0); // At least this is the case for 3605, 2403, and IDM680

        public override object Lock { get { return _lock; } }
        //---------------------------------------------------------------------
        /// <summary>
        /// Loads the TSM servo drive setup file, resets faults, initializes the drive, and determines the slow loop servo rate.
        /// </summary>
        /// <param name="axis_id"></param>
        /// <param name="settings"></param>
        /// <param name="setup_file"></param>
        /// <param name="tml_lock"></param>
#if !TML_SINGLETHREADED
        internal TSAxis( ITMLChannel channel, byte axis_id, MotorSettings settings, string setup_file, object tml_lock)
#else
        internal TSAxis( byte axis_id, MotorSettings settings, string setup_file, object tml_lock)
#endif
        {
#if !TML_SINGLETHREADED
            Channel = channel;
#endif
            _axis_id = axis_id;
            _settings = settings;
            _is_homing = false;
            _speed_percentage = 1.0; // default to 100%
            _lock = tml_lock;
            _abort_move_speed_event = new ManualResetEvent(false);
            _setup_file = setup_file;

            // TODO: these values should be in the config file
            if (4 == (axis_id % 10)) // BB W axis
                _controller_peak_current = 6.11; // 6.11 Amps for 2403 as of 2010-10-12
            else if ((3 == (axis_id % 10)) && (axis_id > 10)) // BB Z axis
                _controller_peak_current = 6.11; // 6.11 Amps for 2403 as of 2010-10-12
            else if ((5 == (axis_id % 10)) && (axis_id > 110) && (axis_id < 200)) // HG2 spindle
                _controller_peak_current = 30.9; // 30.9 Amps for ISD860 as of 2011-08-08
            else
                _controller_peak_current = 16.5; // 16.5 Amps for 3605 and IDM640 as of 2010-10-12

            UseTrapezoidalProfileByDefault = false;

            // initialize the drive
            _idxSetup = 0;
            //UInt16 sAxiOn_flag;		// stores the value of the Status Register Low

            _log = LogManager.GetLogger( "axis_" + axis_id);

            lock (_lock)
            {
                _idxSetup = Channel.TS_LoadSetup(setup_file);
                if (_idxSetup < 0)
                    throw new AxisException(this, String.Format("TS_LoadSetup failed on axis {0}: failed while trying to load setup file '{1}'", _axis_id, setup_file));

                //	Setup the axis based on the setup data previously loaded  
                if (!Channel.TS_SetupAxis(axis_id, _idxSetup))
                    throw new AxisException(this, String.Format("TS_SetupAxis failed on axis {0}", _axis_id));
                //	Select the destination axis of the TML commands  
                if (!Channel.TS_SelectAxis(axis_id))
                    throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));
            } // lock

            // Need to get sleep_timeout_s from motor_settings config file
            AxisTimeoutSecs = _settings.DefaultServoTimeoutS;
            HomeError += TSAxis_HomeError;
        }
        //---------------------------------------------------------------------
        public override string ToString()
        {
            return String.Format( "id: {0}, name: {1}", GetID(), Name);
        }
        //---------------------------------------------------------------------
        // Downloads new motor settings and TML code to drive
        public override void DownloadSwFile(String swFilePath) 
        {
            lock (_lock)
            {
                //	Select the destination axis of the TML commands  
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                _log.InfoFormat( "Reprogramming axis ID {0} motor settings and TML program with {1}", _axis_id, swFilePath);
                if (!Channel.TS_DownloadSwFile(swFilePath))
                    throw new AxisException(this, String.Format("Could not reprogram sw file {0} on axis {1}: {2}", swFilePath, _axis_id, GetTSError()));
            }  
            return; 
        }
        //---------------------------------------------------------------------
        public override int AxisTimeoutSecs
        {
            // returns Axis Timeout setting in seconds
            get
            {
                int servo_off_timeout;
                if (!GetLongVariable("servo_off_timeout", out servo_off_timeout))
                {
                    _log.DebugFormat("GetAxisTimeoutSecs: TS_GetLongVariable(\"servo_off_timeout\") failed.");
                    throw new AxisException(this, String.Format("Could not read servo_off_timeout variable on axis {0}: {1}", _axis_id, GetTSError()));
                }
                int timeout_secs = (int)(servo_off_timeout * Settings.SlowLoopServoTimeS);
                return timeout_secs;
            }
            // if timeout_secs <= 0, then set the timeout to the max, which should be around 24 days for 1000Hz slow-servo rates
            set
            {
                var timeout_secs = value;
                _log.DebugFormat("SetAxisTimeout({0}) called.", timeout_secs);

                int servo_off_timeout; // measured in IU (slow-servo loop cycles)
                const UInt32 servo_off_timeout_infinite = (2147483648 - 1); // (2 ^ 31) -1

                if (timeout_secs <= 0)
                    servo_off_timeout = (int)(servo_off_timeout_infinite);
                else
                    servo_off_timeout = (int)((double)(timeout_secs) / Settings.SlowLoopServoTimeS);

                lock (_lock)
                {
                    //	Select the destination axis of the TML commands  
                    if (!Channel.TS_SelectAxis(_axis_id))
                        throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                    if (!Channel.TS_SetLongVariable("servo_off_timeout", servo_off_timeout))
                    {
                        _log.DebugFormat("SetAxisTimeoutSecs: TS_SetLongVariable(\"servo_off_timeout\") failed.");
                        throw new AxisException(this, String.Format("Could not set servo_off_timeout on axis_{0}: {1}", _axis_id, GetTSError()));
                    }
                } // lock
            }
        }
        //---------------------------------------------------------------------
        // returns Axis Timeout timer in seconds; i.e. how many seconds until timeout from reading the servo_off_delta variable
        public override int AxisTimeoutTimerSecs
        {
            get
            {
                int servo_off_delta;
                if (!GetLongVariable("servo_off_delta", out servo_off_delta))
                {
                    _log.DebugFormat("GetAxisTimeoutTimerSecs() TS_GetLongVariable(\"servo_off_delta\") failed.");
                    throw new AxisException(this, String.Format("Could not read servo_off_delta variable on axis {0}: {1}", _axis_id, GetTSError()));
                }
                int timeout_secs = (int)(servo_off_delta * Settings.SlowLoopServoTimeS);
                return timeout_secs;
            }
        } // GetAxisTimeoutTimerSecs
        //---------------------------------------------------------------------
        //private UInt64 test_data_64;
        //private Byte   test_data_8;
        // Warning: This function blocks until the data is read, and it will probably take about 110ms
        public override void ReadExtI2CPage(Byte page, out UInt64 data)
        {
            DateTime datetime_start_func = DateTime.Now;

            _log.DebugFormat( "ReadExtI2CPage({0}, ...) called.", page);

            // DKM 2011-10-22 I changed this back to 0 because it would mess with the loop logic later on if I used -99
            int    func_done  = 0; // for knowing when function is done executing on controller
            int    I2C_page_H;
            int    I2C_page_L;
            Byte   addr       = (Byte)((page-1) << 3); // multiply the page number by 8 to page align the address

            lock (_lock)
            {
                //	Select the destination axis of the TML commands  
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                if (!Channel.TS_SetLongVariable("func_done", func_done))
                {
                    _log.DebugFormat( "ReadExtI2CPage() TS_SetLongVariable(\"func_done\") failed.");
                    throw new AxisException( this, String.Format("Could not set func_done on axis_{0}: {1}", _axis_id, GetTSError()));
                }

                if (!Channel.TS_SetIntVariable("I2C_addr", (short)addr))
                {
                    _log.DebugFormat( "ReadExtI2CPage() TS_SetIntVariable(\"I2C_addr\") failed.");
                    throw new AxisException( this, String.Format("Could not set I2C_addr on axis_{0}: {1}", _axis_id, GetTSError()));
                }

                if (!Channel.TS_CALL_Label("I2C_PAGE_READ"))
                {
                    _log.DebugFormat( "ReadExtI2CPage() TS_CALL_Label(\"I2C_PAGE_READ\") failed.");
                    throw new AxisException( this, String.Format("Could not call I2C_PAGE_READ on axis_{0}: {1}", _axis_id, GetTSError()));
                }

            } // lock(_lock)

            DateTime t_start = DateTime.Now;
            while (0 == func_done)
            {
                TimeSpan ts_period = DateTime.Now - t_start;
                if (ts_period.TotalSeconds > 2)
                {
                    _log.DebugFormat( "ReadExtI2CPage() Timed out after {0} seconds waiting for I2C_PAGE_READ to complete.", ts_period.TotalSeconds);
                    throw new AxisException( this, String.Format("Timed out after {2} seconds waiting for I2C_PAGE_READ to complete on axis_{0}: {1}",
                        _axis_id, GetTSError(), ts_period.TotalSeconds));
                }

                Thread.Sleep(10);

                if (!GetLongVariable("func_done", out func_done))
                {
                    _log.DebugFormat( "ReadExtI2CPage() GetLongVariable(\"func_done\") failed.");
                    throw new AxisException( this, String.Format("Could not read func_done variable on axis_{0}: {1}", _axis_id, GetTSError()));
                }
            } // while (0 == func_done)

            if (!GetLongVariable("I2C_page_H", out I2C_page_H))
            {
                _log.DebugFormat( "ReadExtI2CPage() GetLongVariable(\"I2C_page_H\") failed.");
                throw new AxisException( this, String.Format("Could not read I2C_page_H variable on axis_{0}: {1}", _axis_id, GetTSError()));
            }

            if (!GetLongVariable("I2C_page_L", out I2C_page_L))
            {
                _log.DebugFormat( "ReadExtI2CPage() GetLongVariable(\"I2C_page_L\") failed.");
                throw new AxisException( this, String.Format("Could not read I2C_page_L variable on axis_{0}: {1}", _axis_id, GetTSError()));
            }

            data = (((UInt64)(I2C_page_H)) << 32) | ((uint)(I2C_page_L));

#if DEBUG_PRINT_I2C
            _log.DebugFormat( "ReadExtI2CPage({0}) Data={1:x08} {2:x08}  {3}{4}{5}{6} {7}{8}{9}{10}",
                              page, I2C_page_H, I2C_page_L,
                              char.ConvertFromUtf32((int)((I2C_page_H & 0xff000000) >> 24)), char.ConvertFromUtf32((int)((I2C_page_H & 0x00ff0000) >> 16)),
                              char.ConvertFromUtf32((int)((I2C_page_H & 0x0000ff00) >>  8)), char.ConvertFromUtf32((int)((I2C_page_H & 0x000000ff)      )),
                              char.ConvertFromUtf32((int)((I2C_page_L & 0xff000000) >> 24)), char.ConvertFromUtf32((int)((I2C_page_L & 0x00ff0000) >> 16)),
                              char.ConvertFromUtf32((int)((I2C_page_L & 0x0000ff00) >>  8)), char.ConvertFromUtf32((int)((I2C_page_L & 0x000000ff)      )));
#endif
            TimeSpan func_ts = DateTime.Now - datetime_start_func;
            _log.DebugFormat( "ReadExtI2CPage({0}, ...) Done in {1:0}ms.", page, func_ts.TotalMilliseconds);
        } // ReadExtI2CPage()
        //---------------------------------------------------------------------
        // Warning: This function blocks until the data is written, and it will probably take about 110ms
        public override void WriteExtI2CPage(Byte page, UInt64 data)
        {
            DateTime datetime_start_func = DateTime.Now;

            _log.DebugFormat( "WriteExtI2CPage({0}, ...) called.", page);

            // DKM 2011-10-22 I changed this back to 0 because it would mess with the loop logic later on if I used -99
            int    func_done  = 0; // for knowing when function is done executing on controller
            int    I2C_page_H = (int)(data >> 32);
            int    I2C_page_L = (int)(data & 0xffffffff);
            Byte   addr       = (Byte)((page-1) << 3); // multiply the page number by 8 to page align the address

#if DEBUG_PRINT_I2C
            _log.DebugFormat( "WriteExtI2CPage({0}) Data={1:x08} {2:x08}  {3}{4}{5}{6} {7}{8}{9}{10}",
                              page, I2C_page_H, I2C_page_L,
                              char.ConvertFromUtf32((int)((I2C_page_H & 0xff000000) >> 24)), char.ConvertFromUtf32((int)((I2C_page_H & 0x00ff0000) >> 16)),
                              char.ConvertFromUtf32((int)((I2C_page_H & 0x0000ff00) >>  8)), char.ConvertFromUtf32((int)((I2C_page_H & 0x000000ff)      )),
                              char.ConvertFromUtf32((int)((I2C_page_L & 0xff000000) >> 24)), char.ConvertFromUtf32((int)((I2C_page_L & 0x00ff0000) >> 16)),
                              char.ConvertFromUtf32((int)((I2C_page_L & 0x0000ff00) >>  8)), char.ConvertFromUtf32((int)((I2C_page_L & 0x000000ff)      )));
#endif
            lock (_lock)
            {
                //	Select the destination axis of the TML commands  
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                if (!Channel.TS_SetLongVariable("func_done", func_done))
                {
                    _log.DebugFormat( "ReadExtI2CPage() TS_SetLongVariable(\"func_done\") failed.");
                    throw new AxisException( this, String.Format("Could not set func_done on axis_{0}: {1}", _axis_id, GetTSError()));
                }

                if (!Channel.TS_SetIntVariable("I2C_addr", (short)addr))
                {
                    _log.DebugFormat( "WriteExtI2CPage() TS_SetIntVariable(\"I2C_addr\") failed.");
                    throw new AxisException( this, String.Format("Could not set I2C_addr on axis_{0}: {1}", _axis_id, GetTSError()));
                }

                if (!Channel.TS_SetLongVariable("I2C_page_H", I2C_page_H))
                {
                    _log.DebugFormat( "WriteExtI2CPage() TS_SetLongVariable(\"I2C_page_H\") failed.");
                    throw new AxisException( this, String.Format("Could not set I2C_page_H on axis_{0}: {1}", _axis_id, GetTSError()));
                }

                if (!Channel.TS_SetLongVariable("I2C_page_L", I2C_page_L))
                {
                    _log.DebugFormat( "WriteExtI2CPage() TS_SetLongVariable(\"I2C_page_L\") failed.");
                    throw new AxisException( this, String.Format("Could not set I2C_page_L on axis_{0}: {1}", _axis_id, GetTSError()));
                }

                if (!Channel.TS_CALL_Label("I2C_PAGE_WRITE"))
                {
                    _log.DebugFormat( "WriteExtI2CPage() TS_CALL_Label(\"I2C_PAGE_WRITE\") failed.");
                    throw new AxisException( this, String.Format("Could not call I2C_PAGE_WRITE on axis_{0}: {1}", _axis_id, GetTSError()));
                }

            } // lock(_lock)

            DateTime t_start = DateTime.Now;
            while (0 == func_done)
            {
                TimeSpan ts_period = DateTime.Now - t_start;
                if (ts_period.TotalSeconds > 2)
                {
                    _log.DebugFormat( "WriteExtI2CPage() Timed out after {0} seconds waiting for I2C_PAGE_WRITE to complete.", ts_period.TotalSeconds);
                    throw new AxisException( this, String.Format("Timed out after {2} seconds waiting for I2C_PAGE_WRITE to complete on axis_{0}: {1}",
                        _axis_id, GetTSError(), ts_period.TotalSeconds));
                }

                Thread.Sleep(10);

                if (!GetLongVariable("func_done", out func_done))
                {
                    _log.DebugFormat( "WriteExtI2CPage() GetLongVariable(\"func_done\") failed.");
                    throw new AxisException( this, String.Format("Could not read func_done variable on axis_{0}: {1}", _axis_id, GetTSError()));
                }
            } // while (0 == func_done)

            TimeSpan func_ts = DateTime.Now - datetime_start_func;
            _log.DebugFormat( "WriteExtI2CPage({0}, ...) Done in {1:0}ms.", page, func_ts.TotalMilliseconds);
        }
        //---------------------------------------------------------------------
        // Warning: This function blocks until the data is read, and it will probably take about 50ms
        public override void ReadExtI2CByte(Byte addr, out Byte data)
        {
            DateTime datetime_start_func = DateTime.Now;

            _log.DebugFormat( "ReadExtI2CByte(0x{0:x02}, ...) called.", addr);

            // DKM 2011-10-22 I changed this back to 0 because it would mess with the loop logic later on if I used -99
            int    func_done  = 0; // for knowing when function is done executing on controller
            short  I2C_word;

            lock (_lock)
            {
                //	Select the destination axis of the TML commands  
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                if (!Channel.TS_SetLongVariable("func_done", func_done))
                {
                    _log.DebugFormat( "ReadExtI2CPage() TS_SetLongVariable(\"func_done\") failed.");
                    throw new AxisException( this, String.Format("Could not set func_done on axis_{0}: {1}", _axis_id, GetTSError()));
                }

                if (!Channel.TS_SetIntVariable("I2C_addr", (short)addr))
                {
                    _log.DebugFormat( "ReadExtI2CByte() TS_SetIntVariable(\"I2C_addr\") failed.");
                    throw new AxisException( this, String.Format("Could not set I2C_addr on axis_{0}: {1}", _axis_id, GetTSError()));
                }

                if (!Channel.TS_CALL_Label("I2C_RAND_READ"))
                {
                    _log.DebugFormat( "ReadExtI2CByte() TS_CALL_Label(\"I2C_RAND_READ\") failed.");
                    throw new AxisException( this, String.Format("Could not call I2C_RAND_READ on axis_{0}: {1}", _axis_id, GetTSError()));
                }

            } // lock(_lock)

            DateTime t_start = DateTime.Now;
            while (0 == func_done)
            {
                TimeSpan ts_period = DateTime.Now - t_start;
                if (ts_period.TotalSeconds > 2)
                {
                    _log.DebugFormat( "ReadExtI2CByte() Timed out after {0} seconds waiting for I2C_RAND_READ to complete.", ts_period.TotalSeconds);
                    throw new AxisException( this, String.Format("Timed out after {2} seconds waiting for I2C_RAND_READ to complete on axis_{0}: {1}",
                        _axis_id, GetTSError(), ts_period.TotalSeconds));
                }

                Thread.Sleep(10);

                if (!GetLongVariable("func_done", out func_done))
                {
                    _log.DebugFormat( "ReadExtI2CByte() GetLongVariable(\"func_done\") failed.");
                    throw new AxisException( this, String.Format("Could not read func_done variable on axis_{0}: {1}", _axis_id, GetTSError()));
                }
            } // while (0 == func_done)

            if (!GetIntVariable("I2C_word", out I2C_word))
            {
                _log.DebugFormat( "ReadExtI2CByte() GetIntVariable(\"I2C_word\") failed.");
                throw new AxisException( this, String.Format("Could not read I2C_word variable on axis_{0}: {1}", _axis_id, GetTSError()));
            }

            data = (Byte)(I2C_word & 0xff);

#if DEBUG_PRINT_I2C
            _log.DebugFormat( "ReadExtI2CByte(0x{0:x02}) Data={1:x02} {2}", addr, data, 
                              char.ConvertFromUtf32((int)(data))));
#endif
            TimeSpan func_ts = DateTime.Now - datetime_start_func;
            _log.DebugFormat( "ReadExtI2CByte(0x{0:x02}, ...) Done in {1:0}ms.", addr, func_ts.TotalMilliseconds);
        }
        //---------------------------------------------------------------------
        // Warning: This function blocks until the data is written, and it will probably take about 50ms
        public override void WriteExtI2CByte(Byte addr, Byte data)
        {
            DateTime datetime_start_func = DateTime.Now;

            _log.DebugFormat( "WriteExtI2CByte(0x{0:x02}, ...) called.", addr);

            // DKM 2011-10-22 I changed this back to 0 because it would mess with the loop logic later on if I used -99
            int    func_done  = 0; // for knowing when function is done executing on controller

#if DEBUG_PRINT_I2C
            _log.DebugFormat( "WriteExtI2CByte(0x{0:x02}) Data={1:x02} {2}", addr, data, 
                              char.ConvertFromUtf32((int)(data))));
#endif
            lock (_lock)
            {
                //	Select the destination axis of the TML commands  
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                if (!Channel.TS_SetLongVariable("func_done", func_done))
                {
                    _log.DebugFormat( "ReadExtI2CPage() TS_SetLongVariable(\"func_done\") failed.");
                    throw new AxisException( this, String.Format("Could not set func_done on axis_{0}: {1}", _axis_id, GetTSError()));
                }

                if (!Channel.TS_SetIntVariable("I2C_addr", (short)addr))
                {
                    _log.DebugFormat( "WriteExtI2CByte() TS_SetIntVariable(\"I2C_addr\") failed.");
                    throw new AxisException( this, String.Format("Could not set I2C_addr on axis_{0}: {1}", _axis_id, GetTSError()));
                }

                if (!Channel.TS_SetIntVariable("I2C_byte", (short)(((short)data) & 0x00ff)))
                {
                    _log.DebugFormat( "WriteExtI2CByte() TS_SetIntVariable(\"I2C_byte\") failed.");
                    throw new AxisException( this, String.Format("Could not set I2C_byte on axis_{0}: {1}", _axis_id, GetTSError()));
                }

                if (!Channel.TS_CALL_Label("I2C_BYTE_WRITE"))
                {
                    _log.DebugFormat( "WriteExtI2CByte() TS_CALL_Label(\"I2C_BYTE_Write\") failed.");
                    throw new AxisException( this, String.Format("Could not call I2C_BYTE_Write on axis_{0}: {1}", _axis_id, GetTSError()));
                }

            } // lock(_lock)

            DateTime t_start = DateTime.Now;
            while (0 == func_done)
            {
                TimeSpan ts_period = DateTime.Now - t_start;
                if (ts_period.TotalSeconds > 2)
                {
                    _log.DebugFormat( "WriteExtI2CByte() Timed out after {0} seconds waiting for I2C_Byte_Write to complete.", ts_period.TotalSeconds);
                    throw new AxisException( this, String.Format("Timed out after {2} seconds waiting for I2C_Byte_Write to complete on axis_{0}: {1}",
                        _axis_id, GetTSError(), ts_period.TotalSeconds));
                }

                Thread.Sleep(10);

                if (!GetLongVariable("func_done", out func_done))
                {
                    _log.DebugFormat( "WriteExtI2CByte() GetLongVariable(\"func_done\") failed.");
                    throw new AxisException( this, String.Format("Could not read func_done variable on axis_{0}: {1}", _axis_id, GetTSError()));
                }
            } // while (0 == func_done)

            TimeSpan func_ts = DateTime.Now - datetime_start_func;
            _log.DebugFormat( "WriteExtI2CByte(0x{0:x02}, ...) Done in {1:0}ms.", addr, func_ts.TotalMilliseconds);
        }
        //---------------------------------------------------------------------
        // read application ID from Technosoft controller at 0x5fcf
        public override String ReadApplicationID()
        {
            const ushort appID_ptr = 0x5fcf; // this is the pointer to eeprom where the Technosoft Application ID can be found (at least for MCII devices)
            const int num_words = 15; // Technosoft says you get 40 characters which should fit in 20 16-bit words, but I only see room for 15 16-bit words for 30 chars max
            ushort[] buffer_words = new ushort[num_words];
            byte[] buffer_bytes = new byte[num_words*2];

            lock (_lock)
            {
                //	Select the destination axis of the TML commands  
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                if (!Channel.TS_GetBuffer(appID_ptr, buffer_words, num_words))
                {
                    _log.DebugFormat( "ReadApplicationID() TS_GetBuffer() failed.");
                    throw new AxisException(this, String.Format("Could not GetBuffer on axis_{0}: {1}", _axis_id, GetTSError()));
                }
            } // lock

            String appID = "";

            for (int n = 0; n < num_words; ++n)
            {
                buffer_bytes[n * 2] = (byte)(buffer_words[n] & 0x00ff);
                if ((buffer_bytes[n * 2] >= 32) && (buffer_bytes[n * 2] < 127))
                    appID += Encoding.UTF8.GetString(buffer_bytes, n * 2, 1);
                else
                    break;

                buffer_bytes[n * 2 + 1] = (byte)(buffer_words[n] >> 8);
                if ((buffer_bytes[n * 2 + 1] >= 32) && (buffer_bytes[n * 2 + 1] < 127))
                    appID += Encoding.UTF8.GetString(buffer_bytes, n * 2 + 1, 1);
                else
                    break;
            }

            return appID;
        }
        //---------------------------------------------------------------------
        // read serial number from Technosoft controller eeprom at serial_number_ptr
        public override String ReadSerialNumber()
        {
            const string ptr_name = "serial_number_ptr";
            short ptr; // this is the pointer to eeprom where the Bionex supplied serial number can be found
            const int num_words = 16; // Bionex allocates 16 words of eeprom for an arbitrary serial number string, which comes out to 31 characters + null termination
            ushort[] buffer_words = new ushort[num_words];
            byte[] buffer_bytes = new byte[num_words * 2];

            if (!GetIntVariable(ptr_name, out ptr))
                throw new AxisException(this, String.Format("Cannot find {0} in {1}", ptr_name, _setup_file));

            if (((ushort)ptr < (ushort)0x4000) || ((ushort)ptr > (ushort)0x6000))
                throw new AxisException(this, String.Format("Queried value for {0} not in valid range. {0} = 0x{1:x04}", ptr_name, (ushort)ptr));

            lock (_lock)
            {
                //	Select the destination axis of the TML commands  
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                if (!Channel.TS_GetBuffer((ushort)ptr, buffer_words, num_words))
                {
                    _log.DebugFormat( "ReadSerialNumber() TS_GetBuffer() failed.");
                    throw new AxisException(this, String.Format("Could not GetBuffer on axis_{0}: {1}", _axis_id, GetTSError()));
                }
            } // lock

            String serialNumStr = "";

            for (int n = 0; n < num_words; ++n)
            {
                buffer_bytes[n * 2] = (byte)(buffer_words[n] & 0x00ff);
                if ((buffer_bytes[n * 2] >= 32) && (buffer_bytes[n * 2] < 127))
                    serialNumStr += Encoding.UTF8.GetString(buffer_bytes, n * 2, 1);
                else
                    break;

                buffer_bytes[n * 2 + 1] = (byte)(buffer_words[n] >> 8);
                if ((buffer_bytes[n * 2 + 1] >= 32) && (buffer_bytes[n * 2 + 1] < 127))
                    serialNumStr += Encoding.UTF8.GetString(buffer_bytes, n * 2 + 1, 1);
                else
                    break;
            }

            return serialNumStr;
        }
        //---------------------------------------------------------------------
        // write serial number to Technosoft controller eeprom at serial_number_ptr
        public override void WriteSerialNumber(string SerialNumberStr)
        {
            const string ptr_name = "serial_number_ptr";
            short ptr; // this is the pointer to eeprom where the Bionex supplied serial number can be found
            const int num_words = 16; // Bionex allocates 16 words of eeprom for an arbitrary serial number string, which comes out to 31 characters + null termination
            ushort[] buffer_words = new ushort[num_words];
            byte[] buffer_bytes = new byte[num_words * 2];

            if (!GetIntVariable(ptr_name, out ptr))
                throw new AxisException(this, String.Format("Cannot find {0} in {1}", ptr_name, _setup_file));

            if (((ushort)ptr < (ushort)0x4000) || ((ushort)ptr > (ushort)0x6000))
                throw new AxisException(this, String.Format("Queried value for {0} not in valid range. {0} = 0x{1:x04}", ptr_name, (ushort)ptr));

            Encoding.UTF8.GetBytes(SerialNumberStr, 0, Math.Min(num_words * 2 - 1, SerialNumberStr.Length), buffer_bytes, 0);

            // copy byte array into word array and change endianess
            for (int n = 0; n < num_words; ++n)
            {
                buffer_words[n] = (ushort)( (ushort)buffer_bytes[n * 2 ] | (ushort)(buffer_bytes[n * 2 + 1] << 8) );
            }

            lock (_lock)
            {
                //	Select the destination axis of the TML commands  
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                if (!Channel.TS_SetBuffer((ushort)ptr, buffer_words, num_words))
                {
                    _log.DebugFormat( "WriteSerialNumber() TS_SetBuffer() failed.");
                    throw new AxisException(this, String.Format("Could not SetBuffer on axis_{0}: {1}", _axis_id, GetTSError()));
                }
            } // lock
        }
        //---------------------------------------------------------------------
        // read LONG (32-bit) variable from eeprom memory at ptr_name
        public override Int32 ReadLongVarEEPROM(String ptr_name)
        {
            short ptr; // this is the pointer to eeprom where 32 bit 'LONG' number can be found
            const int num_words = 2; // 32 bits
            ushort[] buffer_words = new ushort[num_words];

            if (!GetIntVariable(ptr_name, out ptr))
                throw new AxisException(this, String.Format("Cannot find {0} in {1}", ptr_name, _setup_file));

            if (((ushort)ptr < (ushort)0x4000) || ((ushort)ptr > (ushort)0x6000))
                throw new AxisException(this, String.Format("Queried value for {0} not in valid range. {0} = 0x{1:x04}", ptr_name, (ushort)ptr));

            lock (_lock)
            {
                //	Select the destination axis of the TML commands  
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                if (!Channel.TS_GetBuffer((ushort)ptr, buffer_words, num_words))
                {
                    _log.DebugFormat( "ReadLongVarEEPROM() TS_GetBuffer() failed.");
                    throw new AxisException(this, String.Format("Could not GetBuffer on axis_{0}: {1}", _axis_id, GetTSError()));
                }
            } // lock

            Int32 long_var = (Int32)((UInt32)(((UInt32)(buffer_words[1])) << 16) | ((UInt32)(buffer_words[0])));

            return long_var;
        }
        //---------------------------------------------------------------------
        // read INT (16-bit) variable from eeprom memory at ptr_name
        public override Int16 ReadIntVarEEPROM(String ptr_name)
        {
            short ptr; // this is the pointer to eeprom where 16 bit 'INTEGER' number can be found
            const int num_words = 1; // 16 bits
            ushort[] buffer_words = new ushort[num_words];

            if (!GetIntVariable(ptr_name, out ptr))
                throw new AxisException(this, String.Format("Cannot find {0} in {1}", ptr_name, _setup_file));

            if (((ushort)ptr < (ushort)0x4000) || ((ushort)ptr > (ushort)0x6000))
                throw new AxisException(this, String.Format("Queried value for {0} not in valid range. {0} = 0x{1:x04}", ptr_name, (ushort)ptr));

            lock (_lock)
            {
                //	Select the destination axis of the TML commands  
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                if (!Channel.TS_GetBuffer((ushort)ptr, buffer_words, num_words))
                {
                    _log.DebugFormat( "ReadIntVarEEPROM() TS_GetBuffer() failed.");
                    throw new AxisException(this, String.Format("Could not GetBuffer on axis_{0}: {1}", _axis_id, GetTSError()));
                }
            } // lock

            Int16 int_var = (Int16)(buffer_words[0]);

            return int_var;
        }
        //---------------------------------------------------------------------
        // read FIXED (32-bit) variable from eeprom memory at ptr_name
        // TS stores fixed point numbers as integral in upper 16-bits, and fraction of 65536 in lower 16 bits. Big Endian too...
        public override double ReadFixedVarEEPROM(String ptr_name)
        {
            short ptr; // this is the pointer to eeprom where 32 bit 'FIXED' number can be found
            const int num_words = 2; // 32 bits
            ushort[] buffer_words = new ushort[num_words];

            if (!GetIntVariable(ptr_name, out ptr))
                throw new AxisException(this, String.Format("Cannot find {0} in {1}", ptr_name, _setup_file));

            if (((ushort)ptr < (ushort)0x4000) || ((ushort)ptr > (ushort)0x6000))
                throw new AxisException(this, String.Format("Queried value for {0} not in valid range. {0} = 0x{1:x04}", ptr_name, (ushort)ptr));

            lock (_lock)
            {
                //	Select the destination axis of the TML commands  
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                if (!Channel.TS_GetBuffer((ushort)ptr, buffer_words, num_words))
                {
                    _log.DebugFormat( "ReadFixedVarEEPROM() TS_GetBuffer() failed.");
                    throw new AxisException(this, String.Format("Could not GetBuffer on axis_{0}: {1}", _axis_id, GetTSError()));
                }
            } // lock

            double fixed_var = (double)((Int16)(buffer_words[1])) + ((UInt16)(buffer_words[0]))/65536.0;

            return fixed_var;
        }
        //---------------------------------------------------------------------
        // write LONG (32-bit) variable into eeprom memory at ptr_name
        public override void WriteLongVarEEPROM(String ptr_name, Int32 long_var)
        {
            short ptr; // this is the pointer to eeprom where 32 bit 'LONG' number can be found
            const int num_words = 2; // 32 bits
            ushort[] buffer_words = new ushort[num_words];

            if (!GetIntVariable(ptr_name, out ptr))
                throw new AxisException(this, String.Format("Cannot find {0} in {1}", ptr_name, _setup_file));

            if (((ushort)ptr < (ushort)0x4000) || ((ushort)ptr > (ushort)0x6000))
                throw new AxisException(this, String.Format("Queried value for {0} not in valid range. {0} = 0x{1:x04}", ptr_name, (ushort)ptr));

            buffer_words[1] = (UInt16)((UInt32)(long_var) >> 16);
            buffer_words[0] = (UInt16)((UInt32)(long_var) & (UInt32)(0x0000ffff));

            lock (_lock)
            {
                //	Select the destination axis of the TML commands  
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                if (!Channel.TS_SetBuffer((ushort)ptr, buffer_words, num_words))
                {
                    _log.DebugFormat( "WriteLongVarEEPROM() TS_SetBuffer() failed.");
                    throw new AxisException(this, String.Format("Could not SetBuffer on axis_{0}: {1}", _axis_id, GetTSError()));
                }
            } // lock
        }
        //---------------------------------------------------------------------
        // write INT (16-bit) variable to eeprom memory at ptr_name
        public override void WriteIntVarEEPROM(String ptr_name, Int16 int_var)
        {
            short ptr; // this is the pointer to eeprom where 16 bit 'INTEGER' number can be found
            const int num_words = 1; // 16 bits
            ushort[] buffer_words = new ushort[num_words];

            if (!GetIntVariable(ptr_name, out ptr))
                throw new AxisException(this, String.Format("Cannot find {0} in {1}", ptr_name, _setup_file));

            if (((ushort)ptr < (ushort)0x4000) || ((ushort)ptr > (ushort)0x6000))
                throw new AxisException(this, String.Format("Queried value for {0} not in valid range. {0} = 0x{1:x04}", ptr_name, (ushort)ptr));

            buffer_words[0] = (UInt16)int_var;

            lock (_lock)
            {
                //	Select the destination axis of the TML commands  
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                if (!Channel.TS_SetBuffer((ushort)ptr, buffer_words, num_words))
                {
                    _log.DebugFormat( "WriteIntVarEEPROM() TS_SetBuffer() failed.");
                    throw new AxisException(this, String.Format("Could not SetBuffer on axis_{0}: {1}", _axis_id, GetTSError()));
                }
            } // lock
        }
        //---------------------------------------------------------------------
        // write FIXED (32-bit) variable to eeprom memory at ptr_name
        // TS stores fixed point numbers as integral in upper 16-bits, and fraction of 65536 in lower 16 bits. Big Endian too...
        public override void WriteFixedVarEEPROM(String ptr_name, double fixed_var)
        {
            short ptr; // this is the pointer to eeprom where 32 bit 'FIXED' number can be found
            const int num_words = 2; // 32 bits
            ushort[] buffer_words = new ushort[num_words];

            if (!GetIntVariable(ptr_name, out ptr))
                throw new AxisException(this, String.Format("Cannot find {0} in {1}", ptr_name, _setup_file));

            if (((ushort)ptr < (ushort)0x4000) || ((ushort)ptr > (ushort)0x6000))
                throw new AxisException(this, String.Format("Queried value for {0} not in valid range. {0} = 0x{1:x04}", ptr_name, (ushort)ptr));

            if (Math.Sign(fixed_var) < 0)
            {
                buffer_words[0] = (UInt16)((1.0 + (fixed_var - Math.Truncate(fixed_var))) * 65536.0);
                buffer_words[1] = (UInt16)((Int16)(Math.Truncate(fixed_var) - 1.0));
            }
            else
            {
                buffer_words[0] = (UInt16)((fixed_var - Math.Truncate(fixed_var)) * 65536.0);
                buffer_words[1] = (UInt16)((Int16)(Math.Truncate(fixed_var)));
            }

            lock (_lock)
            {
                //	Select the destination axis of the TML commands  
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                if (!Channel.TS_SetBuffer((ushort)ptr, buffer_words, num_words))
                {
                    _log.DebugFormat( "WriteFixedVarEEPROM() TS_SetBuffer() failed.");
                    throw new AxisException(this, String.Format("Could not SetBuffer on axis_{0}: {1}", _axis_id, GetTSError()));
                }
            } // lock
        }
        //---------------------------------------------------------------------
        private void TSAxis_HomeError(object sender, MotorEventArgs e)
        {
            //throw new AxisException( this, GetError());
        }
        //---------------------------------------------------------------------
        public override string GetError()
        {
            return (GetError(0x0000)); // do not mask any errors by default
        }
        //---------------------------------------------------------------------
        // Set a bit in mask if you don't want that bit to trigger an error. Default is to have mask==0.
        public override string GetError(UInt16 mask)
        {
            List<string> faults = GetFaults(mask);
            if( faults.Count != 0) {
                // fire homing error
                string[] fault_array = new string[faults.Count];
                faults.CopyTo( fault_array);
                return String.Join( ", ", fault_array);
            } else
                return "";
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// returns the error string coming from TML-LIB
        /// </summary>
        /// <remarks>
        /// should only be called from within a lock -- assumes that the axis in question has already been selected.
        /// </remarks>
        /// <returns></returns>
        private string GetTSError()
        {
            StringBuilder err = new StringBuilder(512);
            const int buffer_size = 512;
            lock (_lock)
            {
                Channel.TS_SelectAxis(_axis_id);
#if !TML_SINGLETHREADED
                TMLChannel.TS_Basic_GetLastErrorText(err, buffer_size);
#else				
                Channel.TS_Basic_GetLastErrorText(err, buffer_size);
#endif
            }
            return err.ToString();
        }
        //---------------------------------------------------------------------
        public override string Name
        {
            get
            {
                switch (_axis_id)
                {
                    // special case the HiG
                    case 113:
                        return _settings.AxisName;
                    case 115:
                        return _settings.AxisName;

                    // everything else uses 
                    default:    
                        var axis_number = (int)(_axis_id / 10);
                        return string.Format("{0}{1}", _settings.AxisName, axis_number == 0 ? "" : axis_number.ToString());
                }
            }
        }
        //---------------------------------------------------------------------
        public override byte GetID()
        {
            return _axis_id;
        }
        //---------------------------------------------------------------------
        /// <exception cref="AxisException" />
        /// <param name="enable"></param>
        /// <param name="blocking"></param>
        public override void Enable( bool enable, bool blocking)
        {
            if( enable) {
                // make sure axis enabled (i.e. allowed to be used) before continuing.
                if( !Enabled)
                    return;
                // be sure axis is seroving
                _log.DebugFormat( "TurnAxisOnIfNecessary(blocking={0}) called via Enable() for axis {1}", blocking, _axis_id);
                TurnAxisOnIfNecessary( blocking);
            } else {
                lock (_lock) {
                    //	Select the destination axis of the TML commands  
                    if (!Channel.TS_SelectAxis(_axis_id))
                        throw new AxisException( this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                    if (!Channel.TS_CALL_Label("func_my_axisoff"))
                        throw new AxisException( this, String.Format( "Could not turn off axis {0}: {1}", _axis_id, GetTSError()));
                } // lock
            }
        }

        public override void AbortCancellableCall()
        {
            lock( _lock) {
                //	Select the destination axis of the TML commands  
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                if (!Channel.TS_ABORT())
                    throw new AxisException( this, String.Format( "Could not cancel function on axis {0}: {1}", _axis_id, GetTSError()));
            }
        }

        //---------------------------------------------------------------------
        // refresh servo timeout parameter so servo doesn't time out early (used when jogging and teaching)
        public override void RefreshServoTimeout()
        {
            lock (_lock)
            {
                //	Select the destination axis of the TML commands  
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                if (!Channel.TS_Execute("time_since_move_complete = ATIME;"))
                {
                    _log.DebugFormat( "RefreshServoTimeout() TS_Execute(time_since_move_complete = ATIME;) failed.");
                    throw new AxisException(this, String.Format("Could not refresh servo timeout on axis {0}: {1}", _axis_id, GetTSError()));
                }
            } // lock
        }
        //---------------------------------------------------------------------
        public override void Home( bool wait_for_complete) 
        {
            _log.DebugFormat( "Home(wait_for_complete={0}) called.", (wait_for_complete ? "true" : "false"));

            _is_homing = true;  // flag so the GUI can update accordingly

            // now we want to support two behaviors - blocking and non-blocking
            HomingDelegate hd = HomingThread;
            //! \todo put the timeout into the axis configuration XML
            const long timeout_ms = 30000;
            if (wait_for_complete)
            { // obviously this is the blocking case 
                try {
                    hd.Invoke(timeout_ms);
                    OnHomeComplete( this, new MotorEventArgs( String.Format( "homed axis {0}", _axis_id)));
                    _is_homing = false;
                } catch( AxisException ex) {
                    // DKM 2011-11-15 temporary way to get more info to user until I can get details to propagate to expander properly
                    string message = String.Format("{0} ({1})", ex.Message, ex.Details);
                    _log.Warn( message);
                    OnHomeError( this, new MotorEventArgs( message));
                    throw;
                }
            }
            else
            {
                // launch a thread that checks for home completion as well as controller errors
                hd.BeginInvoke(timeout_ms, HomeCompleteCallback, null);
            }
        }
        //---------------------------------------------------------------------
        private void HomeCompleteCallback( IAsyncResult iar)
        {
            AsyncResult ar = (AsyncResult)iar;
            HomingDelegate caller = (HomingDelegate)ar.AsyncDelegate;
            try {
                caller.EndInvoke( iar);
                OnHomeComplete( this, new MotorEventArgs( String.Format( "homed axis {0}", _axis_id)));
                _log.DebugFormat( "Home(...) Done.");
            } catch( Exception ex) {
                OnHomeError( this, new MotorEventArgs( ex.Message));
            } finally {
                _is_homing = false;
            }
        }
        //---------------------------------------------------------------------
        private delegate void HomingDelegate( long timeout_ms);
        //---------------------------------------------------------------------
        public override void SendResetAndHome()
        {
            int num_reset_retries = 0;
            const int max_reset_retries = 3;
            short homing_status = 0;

            while( num_reset_retries++ < max_reset_retries){
                lock( _lock){
                    if( !Channel.TS_SelectAxis(_axis_id)){
                        throw new AxisException( this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));
                    }
                    if( !Channel.TS_Execute("RESET;")){
                        _log.DebugFormat( "HomingThread() \"RESET;\" failed.");
                        throw new AxisException( this, String.Format("Could not Reset axis_{0}: {1}", _axis_id, GetTSError()));
                    }
                }

                Thread.Sleep(1000); // need to allow the motor controller to reset. Is this really enough time for ALL technosoft controllers? Is there a better way to do this?

                // Set the servo_timeout_secs variable in controller so axis can go to sleep. Always need to call this after a RESET
                AxisTimeoutSecs = _settings.DefaultServoTimeoutS;

                if( !GetIntVariable("homing_status", out homing_status)){
                    _log.DebugFormat( "HomingThread() GetIntVariable(\"homing_status\") failed.");
                    throw new AxisException(this, String.Format("Could not set homing_status on axis_{0}: {1}", _axis_id, GetTSError()));
                }
                // SUCCESS
                if( homing_status == -1){
                    break;
                } else{
                    _log.DebugFormat( "Reset did not complete in time on attempt #{0}/{1}", num_reset_retries, max_reset_retries);
                }
            }

            if( num_reset_retries >= max_reset_retries){
                _log.DebugFormat( "HomingThread() homing_status was not -1. homing_status={0}", homing_status);
                throw new AxisException(this, String.Format("Could not reset homing_status on axis_{0}: {1}", _axis_id, GetTSError()));
            }

            lock( _lock){
                if( !Channel.TS_SelectAxis(_axis_id)){
                    throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));
                }
                if( !Channel.TS_CALL_Label("func_homeaxis")){
                    _log.DebugFormat( "HomingThread() TS_CALL_Label(\"func_homeaxis\") failed.");
                    throw new AxisException(this, String.Format("Could not call func_homeaxis on axis_{0}: {1}", _axis_id, GetTSError()));
                }
            }
        }
        //---------------------------------------------------------------------
        public override void WaitForHomeResult( long timeout_ms, bool check_is_homing)
        {
            String error_str = @"No homing errors to report";

            short homing_status = 0;

            // wait for the homing to complete
            DateTime start = DateTime.Now;
            while(( DateTime.Now - start).TotalMilliseconds < timeout_ms && ( !check_is_homing || _is_homing)){
                // check for homing_status == 0
                if( IsHomed){
                    int apos;
                    GetLongVariable( "apos", out apos);
                    int elposl;
                    GetLongVariable( "elposl", out elposl);
                    _log.DebugFormat( "HomingThread() finished successfully at APOS={0} with ELPOSL={1}", apos, elposl);
                    return; // Homing was successful
                } else{ // check for errors if homing_status != 0
                    string errors = GetError(TMLLIB_MER_BIT_LSNACTIVE | TMLLIB_MER_BIT_LSPACTIVE);
                    if( errors.Length != 0){
                        error_str = String.Format("Home(...) error: {0}", errors);
                        throw new AxisException( this, error_str);
                    }
                }
                Thread.Sleep(50); // chill out for a little bit before checking status again
            }

            // report homing timeout error if we get here
            String exp_msg = String.Format("axis_{0}: Homing failed because of timeout within {1} seconds: {2}", _axis_id, timeout_ms / 1000, error_str);
            _log.DebugFormat( "Home(...) Done, but timeout ({0} ms) occured.", timeout_ms);
            // DKM 2011-11-15 report the homing_status in details
            string details = ( GetIntVariable("homing_status", out homing_status)) ? String.Format("homing_status is {0}", homing_status) : "homing_status could not be read";
            throw new AxisException( this, exp_msg, details);
        }
        //---------------------------------------------------------------------
        private void HomingThread( long timeout_ms)
        {
            if( Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "Axis homing";

            SendResetAndHome();
            WaitForHomeResult( timeout_ms, true);
        }
        //-----------------------------------------------------------------------------------------

        private const int g_MoveAbsoluteTries = 3; // can only recurvsively try the move this many times before giving up and failing with an AxisException
        private bool wait_for_move_complete_last_call = false; // initialize to false. this is used for our hacked multi-axis coordination so we don't send double move commands
        private double mm_last_call = -6.9e69; // initialize to infinity. this is used for our hacked multi-axis coordination so we don't send double move commands
        //protected override int _MoveAbsoluteTry = 0; // current try number. Will try up to g_MoveAbsoluteTries until giving up
        //protected override String _MoveAbsoluteLastError; // last error message in MoveAbsolute. Will be printed in the exception after a number of retries

        //-----------------------------------------------------------------------------------------
        // (use_TS_MC == true) will use the builtin Technosoft Motion Complete algorithm based on position feedback instead of the default of trajectory position
        protected override void MoveAbsoluteHelper( double mm /* not ul!! */, double velocity, double acceleration, int jerk, int jerk_min, bool wait_for_move_complete, double move_done_window_mm, short settling_time_ms, bool use_TS_MC, bool use_trap)
        {
            // DKM 2011-03-02 need to check for homing status first!
            if( HomingStatus != 0)
                throw new AxisException( this, "Axis not homed");

            DateTime datetime_start_func = DateTime.Now;

            if (_MoveAbsoluteTry++ >= g_MoveAbsoluteTries) {
                // too many tries. Let's get out of dodge with an AxisException
                String err = String.Format("MoveAbsolute({0:0.000}, move_complete={1}, ...) failed after {2} tries: {3}", mm, (wait_for_move_complete ? "true" : "false"),
                    g_MoveAbsoluteTries, _MoveAbsoluteLastError);
                _log.Debug(err);
                throw new AxisException( this, err);
            }

            _log.DebugFormat( "MoveAbsolute({0:0.000}, move_complete={1}, ...) called. use_trap={2}", mm,
                (wait_for_move_complete ? "true" : "false"), use_trap?"true":"false");

            CheckSoftLimits(mm);

            // changing incoming parameters here so we don't risk calling a TMLLIB function
            // with the wrong units.
            double velocity_iu        = TechnosoftConnection.ConvertVelocityToIU(velocity, _settings.SlowLoopServoTimeS, _settings.EncoderLines, _settings.GearRatio);
            double acceleration_iu    = TechnosoftConnection.ConvertAccelerationToIU(acceleration, _settings.SlowLoopServoTimeS, _settings.EncoderLines, _settings.GearRatio);

            double cspd_iu            = velocity_iu * _speed_percentage;
            double cacc_iu            = acceleration_iu * (_speed_percentage * 0.5 + 0.5);
            int    tjerk_filtered_iu  = jerk_min; // 1 is the minimum
            int    cpos_iu            = TechnosoftConnection.ConvertPositionToIU( mm, _settings.EncoderLines, _settings.GearRatio);

            // DKM 2010-08-30 need to set the floor for cacc_iu and cspd_iu because if they are 0, the TS controllers
            //                will freak out.  There are probably other combinations of liquid profiles that will put
            //                us into this situation, but right now the problem we are addressing is with a liquid
            //                profile that uses a 0mm Z move during aspirate and dispense.  Mark's command formatter
            //                has 5 places after the decimal, so 0.00001 is the smallest value we should allow
            // DKM 2010-09-10 Mark looked in EMS and figured out more reasonable floors.
            cspd_iu = Math.Max( .1024 * 4, cspd_iu); // 0.1mm/s
            cacc_iu = Math.Max( .001 * 4, cacc_iu); // 1.0mm/s^2

            // wait if we have been asked to pause, ONLY IF we are in a worker thread
            // the reason why we do this is that diagnostics needs to be able to
            // jog and move axes, and if we pause before going into diagnostics, we will HANG.
            if(( SynchronizationContext.Current == null) && (( Thread.CurrentThread.Name == null) || ( !Thread.CurrentThread.Name.ToLower().Contains( "diagnostic executor"))))
            {
                int event_index = WaitHandle.WaitAny( new WaitHandle[] { AxisPauseEvent, AxisAbortEvent} );
                // bail if we got an abort
                if( event_index == 1) {
                    // DO NOT RESET AXISABORTEVENT HERE!!!  Will cause problems if you have back-to-back MoveAbsolute calls anywhere on the same axis.
                    return;
                }
            }

            // be sure axis is enabled and seroving
            _log.DebugFormat( "TurnAxisOnIfNecessary(true) called via MoveAbsoluteHelper() for axis {0}", _axis_id);
            TurnAxisOnIfNecessary( true);

            if ( (mm != mm_last_call) ||                      // most likely first time calling this routine 
                 (!wait_for_move_complete) ||         // most likely first time calling this routine 
                 (wait_for_move_complete_last_call) ) // most likely recursively calling this routine or first time calling with wait_for_move_complete==true
            { // we need to (re)send the move commands and (re)set the POSOKLIM
                if (use_TS_MC)
                {
                    SendPositionLimitAndSettingTime(move_done_window_mm, settling_time_ms);
                }
                else
                {
                    SendPositionLimitAndSettingTime();
                }

                if (!use_trap)
                    CalculateFilteredJerk(cpos_iu, cspd_iu, cacc_iu, jerk, jerk_min, out tjerk_filtered_iu);

                //	Command the Scurve positioning using the prescribed parameters; start the motion immediately  
                if (use_trap)
                {
                    StartTrapezoidalMove(cpos_iu, cspd_iu, cacc_iu);
                }
                else
                {
                    StartSCurveMove(cpos_iu, cspd_iu, cacc_iu, tjerk_filtered_iu);
                }
            } // if (need to send move commands)

            mm_last_call = mm; // save this for next next recursive call or whatever
            wait_for_move_complete_last_call = wait_for_move_complete; // remember this for next recursive call or whatever

            if (wait_for_move_complete)
            {
                DateTime move_timer_start = DateTime.Now;
                DateTime motion_complete_timer_start = DateTime.Now; // this is a B.S initialization just to make the compiler happy. Init is when MotionComplete flag is raised
                const double OverallMoveTimeout_secs = 10.0; // timeout for move + move_complete in this call only to MoveAbsoluteHelper()
                const double time_allowed_outside_move_window_sec = 1.0; // timeout for move_complete in this call only to MoveAbsoluteHelper()
                double total_time;
                bool motioncomplete_flag_oneshot_ = false;
                bool cpos_eq_tpos_oneshot_ = false;
                int mc_loop_cycles = 0;

                while ((total_time = (DateTime.Now - move_timer_start).TotalSeconds) < OverallMoveTimeout_secs)
                {
                    CheckMERErrors(); // throw if serious error -- don't auto retry
                    bool motioncomplete_flag = ReadMotionCompleteFlag();
                    // set the timer when we get motion complete
                    if (motioncomplete_flag)
                    {
                        // this is so we only log the motioncomplete_flag once
                        if (!motioncomplete_flag_oneshot_)
                        {
                            _log.DebugFormat( "motioncomplete_flag raised.");
                            motioncomplete_flag_oneshot_ = true;
                            motion_complete_timer_start = DateTime.Now;
                            mc_loop_cycles = 0;
                        }
                    } // if (motioncomplete_flag)
                    else if (!cpos_eq_tpos_oneshot_)
                    {
                        // didn't get MC flag yet, I wonder if TPOS is actually at CPOS. If so, that is MotionComplete. doh!
                        Int32 TPOS;
                        GetLongVariable("TPOS", out TPOS);
                        if (TPOS == cpos_iu)
                        {
                            _log.DebugFormat( "MoveAbsolute({0:0.000}, ...) in a condition where MC is not high, but TPOS == CPOS == {1} IU, so motioncomplete_flag raised essentially",
                                                     mm, TPOS);
                            cpos_eq_tpos_oneshot_ = true; // don't check for this again
                            motioncomplete_flag = true; // so rest of this loop executes correctly
                        }
                    } // else if (!cpos_eq_tpos_oneshot_)

                    // move again if the axis is off
                    ushort srl_reg;
#if !TML_SINGLETHREADED
                    ReadStatus(TMLLibConst.REG_SRL, out srl_reg);
#else
                    ReadStatus(TML.TMLLib.REG_SRL, out srl_reg);
#endif					
                    if (( srl_reg & TMLLIB_SRL_BIT_AXISON) != TMLLIB_SRL_BIT_AXISON)
                    {
                        //throw new AxisException(this, String.Format("Did not expect axis_{0} to shut off. Check to be sure everything is okay.", _axis_id));
                        _MoveAbsoluteLastError = String.Format("MoveAbsolute() Axis {0} shut off unexpectedly. Is everything okay?", _axis_id);
                        _log.Debug(_MoveAbsoluteLastError);
                        // try again
                        MoveAbsoluteHelper(mm, velocity, acceleration, jerk, jerk_min, wait_for_move_complete, move_done_window_mm, settling_time_ms, use_TS_MC, use_trap);
                        return;
                    }

                    // reset faults if we have a command error
                    // or send EINT if we have the weird FFL issue
                    bool execute_move_again = CorrectControllerFaultConditions();
                    if (execute_move_again)
                    {
                        // now try the move again
                        _MoveAbsoluteLastError = String.Format("MoveAbsolute({0:0.000}, ...) got a CMD_ERR using {1}. Calling again recursively.",
                                                       mm, (use_trap ? "Mode PP (TRAP)" : "Mode PSC (S-CURVE)"));
                        _log.Debug(_MoveAbsoluteLastError);
                        // try again
                        MoveAbsoluteHelper(mm, velocity, acceleration, jerk, jerk_min, wait_for_move_complete, move_done_window_mm, settling_time_ms, use_TS_MC, use_trap);
                        return;
                    }

                    int cur_pos = GetPositionCounts();
                    int err_pos = Math.Abs(cur_pos - ConvertToCounts(mm));
                    int move_done_window_counts = ConvertToCounts(move_done_window_mm);
                    if ((err_pos <= move_done_window_counts) && motioncomplete_flag) // we're done -- break out of loop!
                    {
                        break; // SUCCESS!
                    }


                    // if we get here, although we have a motion complete event, we're definitely not
                    // within the allowed position error window, so the move is NOT COMPLETE

                    // this particular case deals with something like tip pressing, where the trajectory
                    // generator says it's done generating points, but the channel hasn't gotten to its
                    // target position yet.  This only applies to the case where we are NOT using TS MC,
                    // which is where the controller reports "move done" when APOS is within range.  We
                    // are only using TPOS in this case.
                    if (motioncomplete_flag && !use_TS_MC)
                    {
                        if (0 == (mc_loop_cycles++ % 10)) // only print debug message every 10 loop cycles which should be approx every 100ms
                        {
                            _log.DebugFormat( "MoveAbsolute() dest: {0:0.000} mm, current: {1:0.000} mm, error: {2:0.000} mm = {3:0} IU",
                                mm, ConvertCountsToEng(cur_pos), ConvertCountsToEng(err_pos), err_pos);
                        }
                        TimeSpan mc_period = DateTime.Now - motion_complete_timer_start;
                        if (mc_period.TotalSeconds > time_allowed_outside_move_window_sec)
                        {
                            _MoveAbsoluteLastError = String.Format("MoveAbsolute({0:0.000}, ...) timed out after {2:0.000} seconds waiting for motion complete using {1}. Calling again recursively.",
                                                       mm, (use_trap ? "Mode PP (TRAP)" : "Mode PSC (S-CURVE)"), mc_period.TotalSeconds);
                            _log.Debug(_MoveAbsoluteLastError);
                            // do extra fancy variable reporting at this point, especially for Z axes
                            if (true /* change me to false to get rid of this */)
                            {
                                Int32 APOS, CPOS, TPOS;
                                GetLongVariable("CPOS", out CPOS);
                                GetLongVariable("APOS", out APOS);
                                GetLongVariable("TPOS", out TPOS);
                                _log.DebugFormat( "MoveAbsolute({0:0.000}mm={1}IU, move_complete={5}, ...) CPOS = {2}, APOS = {3}, TPOS = {4}, Err = {6}, ERR = {7:0.000} mm",
                                    mm, cpos_iu, CPOS, APOS, TPOS, (wait_for_move_complete ? "true" : "false"), APOS-cpos_iu, ConvertCountsToEng(APOS) - mm);
                            }

                            // try again
                            MoveAbsoluteHelper(mm, velocity, acceleration, jerk, jerk_min, wait_for_move_complete, move_done_window_mm, settling_time_ms, use_TS_MC, use_trap);
                            return;
                        }
                    }

                    Thread.Sleep(10);
                } // while (!move_done)

                // if we get here, we may not have finished moving in time
                if (total_time >= OverallMoveTimeout_secs)
                {
                    _MoveAbsoluteLastError = String.Format("MoveAbsolute({0:0.000}, ...) timed out after {2:0.000} seconds using {1}. Calling again recursively.",
                                             mm, (use_trap ? "Mode PP (TRAP)" : "Mode PSC (S-CURVE)"), total_time);
                    _log.Debug(_MoveAbsoluteLastError);

                    // do extra fancy variable reporting at this point, especially for Z axes
                    if (true /* change me to false to get rid of this */)
                    {
                        Int32 APOS, CPOS, TPOS;
                        GetLongVariable("CPOS", out CPOS);
                        GetLongVariable("APOS", out APOS);
                        GetLongVariable("TPOS", out TPOS);
                        _log.DebugFormat( "MoveAbsolute({0:0.000}mm={1}IU, move_complete={5}, ...) CPOS = {2}, APOS = {3}, TPOS = {4}, Err = {6}, ERR = {7:0.000} mm",
                            mm, cpos_iu, CPOS, APOS, TPOS, (wait_for_move_complete ? "true" : "false"), APOS-cpos_iu, ConvertCountsToEng(APOS) - mm);
                    }

                    MoveAbsoluteHelper(mm, velocity, acceleration, jerk, jerk_min, wait_for_move_complete, move_done_window_mm, settling_time_ms, use_TS_MC, use_trap);
                    return;
                }
            } // if (wait_for_move_complete)

            TimeSpan func_ts = DateTime.Now - datetime_start_func;
            _log.DebugFormat( "MoveAbsolute({0:0.000}, move_complete={1}, ...) Done in {2:0}ms.", mm, (wait_for_move_complete ? "true" : "false"), func_ts.TotalMilliseconds);

            // do extra fancy variable reporting at this point, especially for Z axes
            if (wait_for_move_complete)
            {
                Int32 APOS, CPOS, TPOS;
                Int16 IQ; // actual current through the windings
                double Kif = 65472 / 2 / _controller_peak_current; // (bits/Amps) Formula from Page 866 of MackDaddyTechnosoftDoc
                GetLongVariable("CPOS", out CPOS);
                GetLongVariable("APOS", out APOS);
                GetLongVariable("TPOS", out TPOS);
                GetIntVariable("IQ", out IQ);
                double IQ_amps = (double)IQ / Kif;
                _log.DebugFormat( "MoveAbsolute({0:0.000}mm={1}IU, move_complete={5}, ...) FINALLY CPOS = {2}, APOS = {3}, TPOS = {4}, Err = {6}, ERR = {7:0.000} mm, IQ = {8:0.000} A", 
                    mm, cpos_iu, CPOS, APOS, TPOS, (wait_for_move_complete?"true":"false"), APOS-cpos_iu, ConvertCountsToEng(APOS)-mm, IQ_amps);
            }
        }
        //-----------------------------------------------------------------------------------------
        public override bool IsAxisOnFlag
        {
            get
            {
                ushort srl_reg;
#if !TML_SINGLETHREADED					
                ReadStatus( TMLLibConst.REG_SRL, out srl_reg);
#else
                ReadStatus(TML.TMLLib.REG_SRL, out srl_reg);
#endif
                bool axis_on_flag = ((srl_reg & TMLLIB_SRL_BIT_AXISON) == TMLLIB_SRL_BIT_AXISON);
                return axis_on_flag;
            }
        }
        //-----------------------------------------------------------------------------------------
        // Checks to see if the current axis is On (SRL bit 15 is high), and if not, attempts to turn it on
        // returns 0 on success, otherwise returns the return value from func_done on controller
        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="AxisException" />
        /// <param name="blocking"></param>
        protected override void TurnAxisOnIfNecessary( bool blocking)
        {
            bool axis_on_flag = IsAxisOnFlag;
            if (axis_on_flag) {
                // do nothing. axis is alreadyt supposedly on.
                return;
            }

            _log.DebugFormat( "Before turning on axis {0}, MER is: {1}", _axis_id, GetTS_MER( 0));
            _log.DebugFormat( "Axis {0} is not on, so turning on automatically", _axis_id);

            Action enable_thread = ServoEnableThread;
            if( blocking)
                enable_thread.Invoke();
            else
                enable_thread.BeginInvoke( ServoEnableComplete, null);
        } // TurnAxisOnIfNecessary()

        private void ServoEnableThread()
        {
            _log.DebugFormat( "{0} enabling axis", Name);
            if( Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "Axis servo enable";
            CallAxisOnTMLFunction();
            WaitForAxisOnTMLFunctionComplete();
        }

        private void ServoEnableComplete( IAsyncResult iar)
        {
            try {
                AsyncResult ar = (AsyncResult)iar;
                Action callback = (Action)ar.AsyncDelegate;
                callback.EndInvoke( iar);
                OnEnableComplete( this, new EnableEventArgs( true));
            } catch( Exception ex) {
                _log.Error( "Could not enable servos: " + ex.Message);
                throw new AxisException( this, ex.Message);
            }
        }

        private void CheckFuncDoneFlag( string caller, int loop_if_matching_this_value, double timeout_sec, ref int func_done)
        {
            DateTime start_time = DateTime.Now;
            while( func_done == loop_if_matching_this_value) {
                TimeSpan ts_period = DateTime.Now - start_time;
                if (ts_period.TotalSeconds > timeout_sec)
                {
                    _log.DebugFormat( "{0}() Timed out after {1:0.000} seconds waiting for my_axison to complete.", caller, ts_period.TotalSeconds);
                    throw new AxisException(this, String.Format("{0} Timed out after {1} seconds waiting for my_axison to complete on axis_{2}: {3}",
                        caller, ts_period.TotalSeconds, _axis_id, GetTSError()));
                }

                if (!GetLongVariable("func_done", out func_done))
                    throw new AxisException(this, String.Format("Could not read func_done variable on axis_{0}: {1}", _axis_id, GetTSError()));

                Thread.Sleep(10); // sleep a little before polling the func_done variable again
            }
        }

        private int WaitForAxisOnTMLFunctionComplete()
        {
            int func_done = TechnosoftConnection.InitialFuncDone;
            DateTime t_start = DateTime.Now;

            // DKM 2011-10-22 need to wait for func_done to transition from -99 to 0 so we know that the function started
            CheckFuncDoneFlag( "WaitForAxisOnTMLFunctionComplete", TechnosoftConnection.InitialFuncDone, 3, ref func_done);
            // then wait for flag to change from 0
            CheckFuncDoneFlag( "WaitForAxisOnTMLFunctionComplete", 0, 3, ref func_done);
            // if we get here, func_done is no longer 0, so let's see if the function succeeded
            if (1 == func_done)
            { // completed successfully, now sleep some more for safety
                TimeSpan ts_elapsed = DateTime.Now - t_start;
                const int xtra_care_sleep_ms = 1000; // xtra care sleep for 1 second
                _log.DebugFormat( "func_my_axison took {0:0.000} ms to raise func_done flag. Sleeping another {1}ms for extra care.",
                    ts_elapsed.TotalMilliseconds, xtra_care_sleep_ms);
                Thread.Sleep(xtra_care_sleep_ms);
                return 0;
            }
            else
            {
                // axis did not enable without exception at controller level, but it may be benign. Let's put it in the log just in case someone cares.
                _log.DebugFormat( "func_my_axison returned ({0}), which is a value other than the expected value: 1", func_done);
                return func_done;
            }
        }

        private int CallAxisOnTMLFunction()
        {
            int func_done = TechnosoftConnection.InitialFuncDone;

            _log.DebugFormat( "CallAxisOnTMLFunction() calling my_axison");

            // 2011-03-11 (sib) Trying something new: Technosoft will trigger motion complete when trajectory generator (TPOS) gets to destination
            //                  The thought is that the 0 delta move in my_axison will will motion complete (MC) immediately after being commanded
            SendPositionLimitAndSettingTime();

            lock (_lock)
            {
                //	Select the destination axis of the TML commands  
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException(this, String.Format("Could not select axis_{0}: {1}", _axis_id, GetTSError()));

                if (!Channel.TS_SetLongVariable("func_done", func_done))
                throw new AxisException(this, String.Format("Could not set func_done on axis_{0}: {1}", _axis_id, GetTSError()));

                if (!Channel.TS_CALL_Label("func_my_axison"))
                {
                    _log.DebugFormat( "TurnAxisOnIfNecessary() TS_CALL_Label(\"func_my_axison\") failed.");
                    throw new AxisException(this, String.Format("Could not call func_my_axixon on axis_{0}: {1}", _axis_id, GetTSError()));
                }
            } // lock
            return func_done;
        }
        //-----------------------------------------------------------------------------------------
        // Sets POSOKLIM and TONPOSOK to follow the trajectory generator and not care about actual motor encoder position and time. Important for immediate MC
        private void SendPositionLimitAndSettingTime()
        {
            lock( _lock){
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));
                // Trying something new: Technosoft will trigger motion complete when trajectory generator gets to destination
                string exe_cmd_str = String.Format("SRB UPGRADE, 0xF7FF, 0x0; POSOKLIM = 0U; TONPOSOK = 65535U;");
                if (!Channel.TS_Execute(exe_cmd_str))
                {
                    _log.DebugFormat( "SendPositionLimitAndSettingTime() TS_Execute(\"{0}\") failed to execute.", exe_cmd_str);
                    throw new AxisException( this, String.Format("Could not execute \"{0}\" on axis_{1}: {2}", exe_cmd_str, _axis_id, GetTSError()));
                }
            } // lock
        } // SendPositionLimitAndSettingTime()
        //-----------------------------------------------------------------------------------------
        private void SendPositionLimitAndSettingTime( double limit_mm, short settling_time_ms)
        {
            lock( _lock) {
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                string exe_cmd_str = String.Format("POSOKLIM = {0}U; TONPOSOK = {1}U; SRB UPGRADE, 0xFFFF, 0x0800;", 
                                     (short)ConvertToCounts(limit_mm), ConvertToTicks(settling_time_ms));
                if (!Channel.TS_Execute(exe_cmd_str))
                {
                    _log.DebugFormat( "MoveAbsolute() TS_Execute(\"{0}\") failed to execute.", exe_cmd_str);
                    throw new AxisException( this, String.Format("Could not execute \"{0}\" on axis_{1}: {2}", exe_cmd_str, _axis_id, GetTSError()));
                }
            } // lock
        } // SendPositionLimitAndSettingTime()
        //-----------------------------------------------------------------------------------------
        private void StartTrapezoidalMove( int pos_iu, double vel_iu, double acc_iu)
        {
            lock( _lock) {
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));
                // string exe_cmd_str = String.Format("EINT; CACC = {0:0.00000}; CSPD = {1:0.00000}; CPOS = {2}L; CPA; MODE PP; UPD; !MC;", acc_iu, vel_iu, pos_iu); // remove EINT
                string exe_cmd_str = String.Format("CACC = {0:0.00000}; CSPD = {1:0.00000}; CPOS = {2}L; CPA; MODE PP; UPD; !MC;", acc_iu, vel_iu, pos_iu);
                _log.DebugFormat( "\"{0}\"", exe_cmd_str);
                if (!Channel.TS_Execute(exe_cmd_str))
                {
                    _log.DebugFormat( "MoveAbsolute() TS_Execute(\"{0}\") failed to execute.", exe_cmd_str);
                    throw new AxisException( this, String.Format("Could not execute \"{0}\" on axis_{1}: {2}", exe_cmd_str, _axis_id, GetTSError()));
                }
            } // lock
        } // StartTrapezoidalMove()
        //-----------------------------------------------------------------------------------------
        private void StartSCurveMove( int pos_iu, double vel_iu, double acc_iu, int jerk)
        {
            lock( _lock) {
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));
                // string exe_cmd_str = String.Format("EINT; TJERK = {0}; CACC = {1:0.00000}; CSPD = {2:0.00000}; CPOS = {3}L; CPA; MODE PSC; SRB ACR, 0xFFFE, 0x0000; UPD; !MC;", jerk, acc_iu, vel_iu, pos_iu); // remove EINT
                string exe_cmd_str = String.Format("TJERK = {0}; CACC = {1:0.00000}; CSPD = {2:0.00000}; CPOS = {3}L; CPA; MODE PSC; SRB ACR, 0xFFFE, 0x0000; UPD; !MC;", jerk, acc_iu, vel_iu, pos_iu);
                _log.DebugFormat( "\"{0}\"", exe_cmd_str);

                if (!Channel.TS_Execute(exe_cmd_str))
                {
                    _log.DebugFormat( "MoveAbsolute() TS_Execute(\"{0}\") failed to execute.", exe_cmd_str);
                    throw new AxisException( this, String.Format("Could not execute \"{0}\" on axis_{1}: {2}", exe_cmd_str, _axis_id, GetTSError()));
                }
            } // lock

            ushort mer_reg;
#if !TML_SINGLETHREADED
            ReadStatus(TMLLibConst.REG_MER, out mer_reg);
#else
            ReadStatus(TML.TMLLib.REG_MER, out mer_reg);
#endif
            List<String> mer_str_list = BuildTSFaultsStrings(mer_reg);
            if (mer_str_list.Count > 0)
            {
                string[] mer_error_array = new string[mer_str_list.Count];
                mer_str_list.CopyTo(mer_error_array);
                _log.DebugFormat( "MER shows faults --> {0}", String.Join(", ", mer_error_array, 0, mer_str_list.Count));
                // Note: We are not throwing an exception here. Should we?
            }

            bool cmd_err = (mer_reg & TMLLIB_MER_BIT_COMMANDERROR) == TMLLIB_MER_BIT_COMMANDERROR;
            if (cmd_err)
            {
                String str = String.Format("MoveAbsolute({0:0.000}, ...) cmd err immediately after TS_MoveSCurveAbsolute(). Calling again immediately.", pos_iu / GetCountsPerEngineeringUnit());
                _log.Debug(str);

                ResetFaults(); // call FAULTR on this axis to reset "Faults"

                Thread.Sleep(50);
                // String exe_cmd_str = String.Format("EINT; TJERK = {0}; CACC = {1:0.00000}; CSPD = {2:0.00000}; CPOS = {3}L; CPA; MODE PSC; SRB ACR, 0xFFFE, 0x0000; UPD; !MC;", jerk, acc_iu, vel_iu, pos_iu); // remove EINT
                String exe_cmd_str = String.Format("TJERK = {0}; CACC = {1:0.00000}; CSPD = {2:0.00000}; CPOS = {3}L; CPA; MODE PSC; SRB ACR, 0xFFFE, 0x0000; UPD; !MC;", jerk, acc_iu, vel_iu, pos_iu);
                lock (_lock)
                {
                    if (!Channel.TS_SelectAxis(_axis_id))
                        throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                    if (!Channel.TS_Execute(exe_cmd_str))
                    {
                        _log.DebugFormat( "MoveAbsolute() TS_Execute(\"{0}\") failed to execute.", exe_cmd_str);
                        throw new AxisException(this, String.Format("Could not execute \"{0}\" on axis_{1}: {2}", exe_cmd_str, _axis_id, GetTSError()));
                    }
                } // lock
            }
            
        }
        //-----------------------------------------------------------------------------------------
        private void CalculateFilteredJerk( int pos_iu, double vel_iu, double acc_iu, int jerk, int jerk_min, out int tjerk_filtered_iu)
        {
            int    tjerk_max;
            double tjerk_factor = _settings.JerkFactor;
            double cacc_cnt_s_s = acc_iu / _settings.SlowLoopServoTimeS / _settings.SlowLoopServoTimeS;
            double cspd_cnt_s = vel_iu / _settings.SlowLoopServoTimeS;
            double spd_over_acc = cspd_cnt_s / cacc_cnt_s_s;
            double spd_sq_over_acc = cspd_cnt_s * spd_over_acc;
            int    pos_before_move_iu;

            if (!GetLongVariable("APOS", out pos_before_move_iu)) {
                _log.DebugFormat( "MoveAbsolute(...) TS_GetLongVariable(APOS) failed...");
                throw new AxisException( this, String.Format("Could not read APOS register on axis_{0} after {2} tries: {1}", _axis_id, GetTSError(), max_read_retries));
            }
            // otherwise, we are good and make the calculation
            int pos_delta_iu = pos_iu - pos_before_move_iu;
            _log.DebugFormat( "MoveAbsolute({0} iu, ...) APOS= {1} iu, delta= {2} iu", pos_iu, pos_before_move_iu, pos_delta_iu);

            if (spd_sq_over_acc <= Math.Abs(pos_delta_iu)) {
                // 3 part move with accel, max vel, decel
                tjerk_max = (int)(spd_over_acc / _settings.SlowLoopServoTimeS * tjerk_factor);
                tjerk_filtered_iu = Math.Min(jerk, tjerk_max);
            } else {
                // 2 part move with accel, decel
                tjerk_max = (int) (Math.Sqrt((double)Math.Abs(pos_delta_iu) / cacc_cnt_s_s) / 2.0 / _settings.SlowLoopServoTimeS * tjerk_factor);
                tjerk_filtered_iu = Math.Min(jerk, tjerk_max);
            }
            
            tjerk_filtered_iu = Math.Max(jerk_min, tjerk_filtered_iu); // be sure it is at least jerk_min (which should be at least 1)
            if (tjerk_filtered_iu < 1)
                tjerk_filtered_iu = 1;

            // change first part to true if you want to see this output 
            if (false && tjerk_filtered_iu != jerk) 
                _log.DebugFormat( "MoveAbsolute({0}, jerk={1}, ...) will use a different TJERK of {2}", pos_iu / GetCountsPerEngineeringUnit(), jerk, tjerk_filtered_iu);
        }
        //-----------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="AxisException" />
        private ushort CheckMERErrors()
        {
            ushort mer_reg;
#if !TML_SINGLETHREADED
            ReadStatus(TMLLibConst.REG_MER, out mer_reg);
#else
            ReadStatus(TML.TMLLib.REG_MER, out mer_reg);
#endif

            List<String> mer_str_list = BuildTSFaultsStrings(mer_reg);
            if (mer_str_list.Count > 0) {
                string[] mer_error_array = new string[mer_str_list.Count];
                mer_str_list.CopyTo(mer_error_array);
                string error = String.Format("MER shows faults --> {0}", String.Join(", ", mer_error_array, 0, mer_str_list.Count));
                _log.Debug( error);
                // Note: We are not throwing an exception here. Should we? -- DKM we will before returning now
            }

            // throw exception if the error is not a short-circuit error
            //if( (mer_reg & TMLLIB_MER_BIT_SHORTCIRCUIT) != TMLLIB_MER_BIT_SHORTCIRCUIT)
            //    throw new AxisException( this, error);

            return mer_reg;
        }
        //-----------------------------------------------------------------------------------------
        public override bool ReadMotionCompleteFlag()
        {
            ushort srl_reg;

#if !TML_SINGLETHREADED
            ReadStatus(TMLLibConst.REG_SRL, out srl_reg);
#else
            ReadStatus(TML.TMLLib.REG_SRL, out srl_reg);
#endif
            bool motioncomplete_flag = ((srl_reg & TMLLIB_SRL_BIT_MOTIONCOMPLETE) == TMLLIB_SRL_BIT_MOTIONCOMPLETE);
            return motioncomplete_flag;
        }
        //-----------------------------------------------------------------------------------------
        public override bool ReadTrajectoryCompleteFlag()
        {
            ushort srh_reg;

#if !TML_SINGLETHREADED
            ReadStatus(TMLLibConst.REG_SRH, out srh_reg);
#else
            ReadStatus(TML.TMLLib.REG_SRH, out srh_reg);
#endif
            bool trajectorycomplete_flag = ((srh_reg & TMLLIB_SRH_BIT_TARGETREACHED) == TMLLIB_SRH_BIT_TARGETREACHED);
            return trajectorycomplete_flag;
        }
        //-----------------------------------------------------------------------------------------
        private bool CorrectControllerFaultConditions()
        {
            ushort mer_reg;

#if !TML_SINGLETHREADED
            ReadStatus(TMLLibConst.REG_MER, out mer_reg);
#else
            ReadStatus(TML.TMLLib.REG_MER, out mer_reg);
#endif
            if( (mer_reg & TMLLIB_MER_BIT_COMMANDERROR) == TMLLIB_MER_BIT_COMMANDERROR) {
                _log.DebugFormat( "MoveAbsolute(...) resulted in a command error. Sending FAULTR;");
                ResetFaults(); // reset fault by calling FAULTR
                Thread.Sleep(50);
                return true;
            }
            
#if FALSE
            // 2010-10-05 sib: This is not good. Negative FFL is allowed. It does not signify a fault

            // check for -FFL, which is what happens on the Hive theta axis when we added pause/resume
            short ffl;
            GetIntVariable( "FFL", out ffl);
            if( ffl < 0) {
                // need to re-enable global interrupts
                lock( _lock) {
                    if( !TMLLib.TS_SelectAxis( _axis_id))
                        throw new AxisException( this, "Could not select axis when trying to re-enable global interrupts");
                    if( !TMLLib.TS_Execute( "EINT;"))
                        throw new AxisException( this, "Could not re-enable global interrupts");
                }
            }
#endif
            return false;
        }
        //-----------------------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mm"></param>
        /// <exception cref="AxisException" />
        private void CheckSoftLimits(double mm)
        {
            if (mm < _settings.MinLimit)
            {
                throw new AxisException(this, String.Format("Cannot move to {0:0.000} because it is past the minimum travel limit of {1:0.000}", mm, _settings.MinLimit));
            }
            else if (mm > _settings.MaxLimit)
            {
                throw new AxisException(this, String.Format("Cannot move to {0:0.000} because it is past the maximum travel limit of {1:0.000}", mm, _settings.MaxLimit));
            }
        }
        //---------------------------------------------------------------------
        public override void MoveRelative( double mm_or_ul)
        {
            MoveRelative( mm_or_ul, _settings.Velocity, _settings.Acceleration, _settings.Jerk);
        }
        //---------------------------------------------------------------------
        public override void MoveRelative( double mm_or_ul, double velocity, double acceleration, int jerk)
        {
            double mm_or_ul_current = GetPositionMM();
            double mm_or_ul_target;
            
            if( GetConversionFormula() != null) {
                // for the W axis, I would rather have it complain about the limits
                mm_or_ul_target = GetPositionUl() + mm_or_ul;
                MoveAbsolute(mm_or_ul_target, velocity, acceleration, jerk);
            } else {
                mm_or_ul_target = mm_or_ul_current + mm_or_ul;
                mm_or_ul_target = Math.Min(mm_or_ul_target, _settings.MaxLimit);
                mm_or_ul_target = Math.Max(mm_or_ul_target, _settings.MinLimit);
                MoveAbsolute(mm_or_ul_target, velocity, acceleration, jerk);
            }
        }
        //---------------------------------------------------------------------
        //  - This is very similar to regular MoveAbsolute, but the motion complete window is different because we are torque limited. This function
        // opens up the position loop control error window to allow more position error when doing the move. This function may also do other things
        // like jack-hammer style moves just before motion is really complete.
        //  - settling_time_ms is how long we should be at zero velocity before calling the move complete
        //  - max_Amps is the torque limit output from the acceleration and feed-forward block which includes the PID position controller (SATS variable)
        //  - IMaxPS_Amps is the Peak current rating of the motor controller (6.11 Amps for 2403, 16.5 Amps for 3605)
        //  - torque_limiting_window_rel_mm is the distance from the destination point (mm) where we switch to torque mode and look for 0 velocity.
        //  - make mm further than your real destination so the P gain stays high. You should verify that you are getting your torque limit because things like
        //  gravity offset setting can affect the torque limiting.
        //  - This function always blocks until the move is complete.
        public override void MoveAbsoluteTorqueLimited(double mm, double velocity, double acceleration, int jerk, int jerk_min, 
            short settling_time_ms, double max_Amps, double IMaxPS_Amps, double torque_limiting_window_rel_mm)
        {
            DateTime datetime_start_func = DateTime.Now;

            _log.DebugFormat( "MoveAbsoluteTorqueLimited({0:0.000}, ...) called. use_trap={1}", mm, UseTrapezoidalProfileByDefault ? "true" : "false");

            CheckSoftLimits(mm);

            // changing incoming parameters here so we don't risk calling a TMLLIB function
            // with the wrong units.
            double velocity_iu = TechnosoftConnection.ConvertVelocityToIU(velocity, _settings.SlowLoopServoTimeS, _settings.EncoderLines, _settings.GearRatio);
            double acceleration_iu = TechnosoftConnection.ConvertAccelerationToIU(acceleration, _settings.SlowLoopServoTimeS, _settings.EncoderLines, _settings.GearRatio);
            int cpos_iu = TechnosoftConnection.ConvertPositionToIU(mm, _settings.EncoderLines, _settings.GearRatio);
            int torque_limiting_window_rel_iu = TechnosoftConnection.ConvertPositionToIU(torque_limiting_window_rel_mm, _settings.EncoderLines, _settings.GearRatio);
            double Kif = 65472/2/IMaxPS_Amps; // (bits/Amps) Formula from Page 866 of MackDaddyTechnosoftDoc

            UInt16 torque_lim_SATS_iu = (UInt16)(32767 - (int)(max_Amps * Kif)); // (iu) Formula from Page 921 of MackDaddyTechnosoftDoc
            UInt32 torque_lim_settling_time_iu = (UInt32)(TechnosoftConnection.ConvertTimeToIU(settling_time_ms, _settings.SlowLoopServoTimeS)); // (iu)
            Int32 torque_lim_apos_iu; // (iu) gets set after figuring out if we are going positive or negative in direction

            int pos_before_move_iu;
            double cspd_iu = velocity_iu * _speed_percentage;
            double cacc_iu = acceleration_iu * (_speed_percentage * 0.5 + 0.5);
            int tjerk_filtered_iu = jerk_min; // 1 is the minimum

            // be sure axis is enabled and seroving
            _log.DebugFormat( "TurnAxisOnIfNecessary(true) called via MoveAbsoluteTorqueLimited() for axis {0}", _axis_id);
            TurnAxisOnIfNecessary( true);

            // Query the controller to find current position (important for TJERK calc and whether the torque control window is in the negative or positive direction)
            if (!GetLongVariable("APOS", out pos_before_move_iu))
                throw new AxisException(this, String.Format("Could not read APOS register on axis_{0} after {2} tries: {1}", _axis_id, GetTSError(), max_read_retries));

            if ((cpos_iu - pos_before_move_iu) < 0)
            {
                // need to move in the negative direction
                torque_lim_apos_iu = (Int32)(cpos_iu + torque_limiting_window_rel_iu);
            }
            else
            {
                // need to move in the positive direction
                torque_lim_apos_iu = (Int32)(cpos_iu - torque_limiting_window_rel_iu);
            }

            // calculate the delta move needed
            int pos_delta_iu = cpos_iu - pos_before_move_iu;
            _log.DebugFormat( "MoveAbsoluteTorqueLimited({0} iu, ...) APOS= {1} iu, delta_step= {2} iu", cpos_iu, pos_before_move_iu, pos_delta_iu);

            // set the position limit and settling time for motion_complete trigger before moving
            // Trying something new: Technosoft will trigger motion complete when trajectory generator gets to destination
            string exe_cmd_str = String.Format("SRB UPGRADE, 0xF7FF, 0x0; POSOKLIM = 0U; TONPOSOK = 65535U; TORQUE_LIM_APOS = {0:0}L; TORQUE_LIM_SATS = {1:0}U; TORQUE_LIM_SETTLING_TIME = {2:0}L;",
                                               torque_lim_apos_iu, torque_lim_SATS_iu, torque_lim_settling_time_iu);
            _log.DebugFormat( "\"{0}\"", exe_cmd_str);

            lock (_lock)
            {
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                if (!Channel.TS_Execute(exe_cmd_str))
                    throw new AxisException(this, String.Format("Could not execute \"{0}\" on axis_{1}: {2}", exe_cmd_str, _axis_id, GetTSError()));
            } // lock

            if (!UseTrapezoidalProfileByDefault)
            {
                int tjerk_max;
                const double tjerk_factor = 1.5;
                double cacc_cnt_s_s = cacc_iu / _settings.SlowLoopServoTimeS / _settings.SlowLoopServoTimeS;
                double cspd_cnt_s = cspd_iu / _settings.SlowLoopServoTimeS;
                double spd_over_acc = cspd_cnt_s / cacc_cnt_s_s;
                double spd_sq_over_acc = cspd_cnt_s * spd_over_acc;

                if (spd_sq_over_acc <= Math.Abs(pos_delta_iu))
                {
                    // 3 part move with accel, max vel, decel
                    tjerk_max = (int)(spd_over_acc / _settings.SlowLoopServoTimeS * tjerk_factor);
                    tjerk_filtered_iu = Math.Min(jerk, tjerk_max);
                }
                else
                {
                    // 2 part move with accel, decel
                    tjerk_max = (int)(Math.Sqrt((double)Math.Abs(pos_delta_iu) / cacc_cnt_s_s) / 2.0 / _settings.SlowLoopServoTimeS * tjerk_factor);
                    tjerk_filtered_iu = Math.Min(jerk, tjerk_max);
                }

                tjerk_filtered_iu = Math.Max(jerk_min, tjerk_filtered_iu); // be sure it is at least jerk_min (which should be at least 1)
                if (tjerk_filtered_iu < 1)
                    tjerk_filtered_iu = 1; // really be sure it is no less than 1, because this will really screw things up

                if (false && tjerk_filtered_iu != jerk) // change first part to true if you want to see this output
                {
                    _log.DebugFormat( "MoveAbsoluteTorqueLimited({0}, jerk={1}, ...) will use a different TJERK of {2}", mm, jerk, tjerk_filtered_iu);
                }
            }
            
            // Send out the move command, which moves to mm destination. Open up control error to window, and set max torque as well.
            lock (_lock)
            {
                //	Select the destination axis of the TML commands  
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                //	Command the move using the prescribed parameters; start the motion immediately  
                //! \todo figure out how to get return value in Autos window so I don't have to make these temp variables!
                if (UseTrapezoidalProfileByDefault)
                {
                    /*
                    if (!TMLLib.TS_MoveAbsoluteTorqueLimited(cpos_iu, cspd_iu, cacc_iu, TMLLib.UPDATE_IMMEDIATE, TMLLib.FROM_MEASURE))
                        throw new AxisException( this, GetTSError());
                     */
                    // SRL = 0; // SRL:10 motion complete should be cleared before this is run, just to be sure
                    exe_cmd_str = String.Format("SRL = 0; FUNC_DONE = {0}; CACC = {1:0.00000}; CSPD = {2:0.00000}; CPOS = {3:0}L; CPA; MODE PP;",
                        TechnosoftConnection.InitialFuncDone, cacc_iu, cspd_iu, cpos_iu);
                    if (!Channel.TS_Execute(exe_cmd_str))
                    {
                        _log.DebugFormat( "MoveAbsoluteTorqueLimited() TS_Execute(\"{0}\") failed to execute.", exe_cmd_str);
                        throw new AxisException( this, String.Format("Could not execute \"{0}\" on axis_{1}: {2}", exe_cmd_str, _axis_id, GetTSError()));
                    }
                }
                else  // if(use_trap)
                {
                    // SRL = 0; // SRL:10 motion complete should be cleared before this is run, just to be sure
                    exe_cmd_str = String.Format("SRL = 0; FUNC_DONE = {0}; TJERK = {1}; CACC = {2:0.00000}; CSPD = {3:0.00000}; CPOS = {4}L; CPA; MODE PSC; SRB ACR, 0xFFFE, 0x0000;",
                        TechnosoftConnection.InitialFuncDone, tjerk_filtered_iu, cacc_iu, cspd_iu, cpos_iu);
                    _log.DebugFormat( "\"{0}\"", exe_cmd_str);

                    if (!Channel.TS_Execute(exe_cmd_str))
                    {
                        _log.DebugFormat( "MoveAbsoluteTorqueLimited() TS_Execute(\"{0}\") failed to execute.", exe_cmd_str);
                        throw new AxisException( this, String.Format("Could not execute \"{0}\" on axis_{1}: {2}", exe_cmd_str, _axis_id, GetTSError()));
                    }
                } // if(use_trap) else...

                if (!Channel.TS_CALL_Label("MOVE_TORQUE_LIM"))
                {
                    _log.DebugFormat( "MoveAbsoluteTorqueLimited() TS_CALL_Label(\"MOVE_TORQUE_LIM\") failed to execute.");
                    throw new AxisException( this, String.Format("Could not execute \"CALL MOVE_TORQUE_LIM\" on axis_{0}: {1}", _axis_id, GetTSError()));
                }
            } // lock

            DateTime mc_time_start = DateTime.Now; // motion_complete time start (gets reset on rising edges of MOTIONCOMPLETE flag)
            bool motioncomplete_flag = false;

            while( true)
            {
                // Get the latest values for MER, SRL, and func_done
                UInt16 mer_reg;
                UInt16 srl_reg;

#if !TML_SINGLETHREADED
                ReadStatus(TMLLibConst.REG_MER, out mer_reg);
                ReadStatus(TMLLibConst.REG_SRL, out srl_reg);
#else
                ReadStatus(TML.TMLLib.REG_MER, out mer_reg);
                ReadStatus(TML.TMLLib.REG_SRL, out srl_reg);
#endif

                int func_done;
                if (!GetLongVariable("func_done", out func_done))
                    throw new AxisException( this, String.Format("Could not read FUNC_DONE register on axis_{0}: {1}", _axis_id, GetTSError()));
                
                List<String> mer_str_list = BuildTSFaultsStrings(mer_reg);
                if (mer_str_list.Count > 0)
                {
                    string[] mer_error_array = new string[mer_str_list.Count];
                    mer_str_list.CopyTo(mer_error_array);
                    _log.DebugFormat( "MER shows faults --> {0}", String.Join(", ", mer_error_array, 0, mer_str_list.Count));
                    // Note: We are not throwing an exception here. Should we?
                }

                // look for rising edge of MOTIONCOMPLETE flag on controller
                if (!motioncomplete_flag && ( (srl_reg & TMLLIB_SRL_BIT_MOTIONCOMPLETE) == TMLLIB_SRL_BIT_MOTIONCOMPLETE ))
                {
                    motioncomplete_flag = true;
                    mc_time_start = DateTime.Now;
                    _log.DebugFormat( "motioncomplete_flag raised.");
                }
                else
                {
                    // This allows the flag to be lowered after it was raised
                    motioncomplete_flag = (srl_reg & TMLLIB_SRL_BIT_MOTIONCOMPLETE) == TMLLIB_SRL_BIT_MOTIONCOMPLETE;
                }

                if ((srl_reg & TMLLIB_SRL_BIT_AXISON) != TMLLIB_SRL_BIT_AXISON)
                {
                    _log.DebugFormat( "MoveAbsoluteTorqueLimited() Axis shut off unexpectedly. Is everything okay?");
                    throw new AxisException( this, String.Format("Did not expect axis_{0} to shut off. Check to be sure everything is okay.", _axis_id));
                }

                bool cmd_err = (mer_reg & TMLLIB_MER_BIT_COMMANDERROR) == TMLLIB_MER_BIT_COMMANDERROR;
                if (cmd_err)
                {
                    _log.DebugFormat( "MoveAbsoluteTorqueLimited({0:0.000}, ...) resulted in a command error using {1}.",
                               mm, (UseTrapezoidalProfileByDefault ? "Mode PP (TRAP)" : "Mode PSC (S-CURVE)"));
                    throw new AxisException( this, String.Format("Command Error was seen on axis_{0}.", _axis_id));
                } // if (cmd_err)

                // DKM 2011-10-22 wait for the function to execute...
                CheckFuncDoneFlag( "MoveAbsoluteTorqueLimited", TechnosoftConnection.InitialFuncDone, 3, ref func_done);

                // need to make sure we don't sit here forever in case motion_complete flag is raised and func_done does not get raised
                if ((1 != func_done) && motioncomplete_flag)
                {
                    const double timeout = 5.0;
                    TimeSpan period = DateTime.Now - mc_time_start;
                    if (period.TotalSeconds > timeout)
                    {
                        String str = String.Format("MoveAbsoluteTorqueLimited({0:0.000}, ...) timed out after {2:0.000} seconds after MC flag raised using {1}.",
                                                   mm, (UseTrapezoidalProfileByDefault ? "Mode PP (TRAP)" : "Mode PSC (S-CURVE)"), timeout);
                        _log.Debug(str);
                        throw new AxisException( this, String.Format("Timed out after {1:0.000} seconds after MC flag raised on axis_{0}.", _axis_id, timeout));
                    }
                }

                if ( 1 == func_done )
                {
                    break;
                }

                Thread.Sleep(10);
            } // while (!move_done)

            double APOS = GetPositionMM();
            TimeSpan func_ts = DateTime.Now - datetime_start_func;
            _log.DebugFormat( "MoveAbsoluteTorqueLimited({0:0.000}, ...) At {1:0.000}mm now. Done in {2:0}ms.",
                                     mm, APOS, func_ts.TotalMilliseconds);
        } // MoveAbsoluteTorqueLimited()
        //---------------------------------------------------------------------
        public override bool IsHomed
        {
            get
            {
                try
                {
                    return HomingStatus == 0;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
        //---------------------------------------------------------------------
        protected override int HomingStatus
        {
            get
            {
                lock (_lock)
                {
                    // check the value of the home_complete user variable
                    if (!Channel.TS_SelectAxis(_axis_id))
                        throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));
                    short homingstatus;
                    if (!GetIntVariable("homing_status", out homingstatus))
                    {
                        _log.DebugFormat("GetHomingStatus() TS_GetIntVariable(homing_status) failed.");
                        throw new AxisException(this, String.Format("Could not retrieve home status for axis_{0}: {1}", _axis_id, GetTSError()));
                    }
                    return homingstatus;
                }
            }
        }
        //---------------------------------------------------------------------
        public override bool ReadHomeSensor()
        {
            const byte pulse_input = 38; // 2010-10-05 sib: Where did this come from? Is this specific for IBL2403? This needs to be in a config file.
            byte val;
            if (!GetInput(pulse_input, out val))
                throw new AxisException(this, String.Format("Could not read home sensor (input#{0}) on axis {1}: {2}", pulse_input, _axis_id, GetTSError()));
            return val == 1;
        }
        //---------------------------------------------------------------------
        public override int GetPositionCounts()
        {
            int pos;
            if( !GetLongVariable( "APOS", out pos)) {
                _log.DebugFormat( "GetPositionCounts() TS_GetLongVariable(APOS) failed.");
                throw new AxisException( this, String.Format("Could not read APOS register on axis {0}: {1}", _axis_id, GetTSError()));
            }
            return pos;
        }
        //---------------------------------------------------------------------
        public override double GetPositionMM()
        {
            return (double)GetPositionCounts() / GetCountsPerEngineeringUnit();
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// execute a speed move (jog at speed with accel) with MODE SP1
        /// </summary>
        /// <param name="speed_deg_per_sec">Engineering units of deg/s at load</param>
        /// <param name="accel_deg_per_sec2">Engineering units of deg/s^2 at load</param>
        /// <param name="wait_for_traj_complete"></param>
        public override bool MoveSpeed(double speed_deg_per_sec, double accel_deg_per_sec2, bool wait_for_traj_complete)
        {
            _log.DebugFormat( "MoveSpeed({0:0.000} deg/s) called", speed_deg_per_sec);

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

            _log.DebugFormat( "TurnAxisOnIfNecessary(true) called via MoveSpeed() for axis {0}", _axis_id);
            TurnAxisOnIfNecessary(true);
            
            bool ret;
                double CSPD = speed_deg_per_sec * GetCountsPerEngineeringUnit() * Settings.SlowLoopServoTimeS / 360.0;
                double CACC = accel_deg_per_sec2 * GetCountsPerEngineeringUnit() * Settings.SlowLoopServoTimeS * Settings.SlowLoopServoTimeS / 360.0;
                var command = String.Format("CSPD={0:0.000};CACC={1:0.000};MODE SP1;UPD;", CSPD, CACC);
                lock (_lock)
                {
                    //	Select the destination axis of the TML commands  
                    if (!Channel.TS_SelectAxis(_axis_id))
                        throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                    //ret = TMLLib.TS_MoveVelocity(CSPD, CACC, TMLLib.UPDATE_IMMEDIATE, TMLLib.FROM_MEASURE);
                    ret = Channel.TS_Execute(command);
                }
                if (!ret)
                {
                    throw new AxisException(this, String.Format("MoveSpeed({0:0.000}) failed in call to TS_MoveVelocity", 
                        speed_deg_per_sec));
                }

            if (wait_for_traj_complete)
            {
                bool traj_complete = false;
                TimeSpan ts_xtra_timeout = TimeSpan.FromMilliseconds(1000.0); // extra amount of time to wait for trajectory complete besides accel time
                System.TimeSpan ts_accel = System.TimeSpan.FromSeconds(Math.Abs((GetActualSpeedDegPerSec() - speed_deg_per_sec) / accel_deg_per_sec2));
                DateTime start = DateTime.Now;
                _abort_move_speed_event.Reset();
                while ((DateTime.Now - start) < (ts_accel + ts_xtra_timeout) && !_abort_move_speed_event.WaitOne( 0))
                {
                    traj_complete = IsTargetReached();
                    if (traj_complete) 
                        break;
                    Thread.Sleep(100); // sleep for a little while we wait for the accel period
                }
                if (traj_complete) {
                    _log.DebugFormat( "MoveSpeed({0:0.000} deg/s) reached trajectory speed", speed_deg_per_sec);
                } else if (_abort_move_speed_event.WaitOne(0)) {
                    _log.Debug("Spin aborted");
                    return false;
                } else {
                    throw new AxisException(this, String.Format("MoveSpeed({0:0.000}) Did not reach trajectory speed in {1:0} secs on axis {2}", 
                        speed_deg_per_sec, (ts_accel + ts_xtra_timeout).TotalSeconds, _axis_id));
                }
            }
            else
            {
                _log.DebugFormat( "MoveSpeed({0:0.000} deg/s) commanded, but not waiting for trajectory complete", speed_deg_per_sec);
            }

            return true;
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// wrapper around TS_GetFixedVariable("ASPD")
        /// </summary>
        /// <returns>ASPD as a 32-bit float in internal units for motor speed</returns>
        public override double GetActualSpeedIU()
        {
            double aspd;
            if (!GetFixedVariable("ASPD", out aspd))
            {
                _log.DebugFormat( "GetActualSpeedIU() TS_GetFixedVariable(ASPD) failed.");
                throw new AxisException(this, String.Format("Could not read ASPD register on axis {0}: {1}", _axis_id, GetTSError()));
            }
            return aspd;
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// calls GetActualSpeedIU to query the controller for actual speed, and then converts to engineering units (deg/s) and converts to load gearing
        /// </summary>
        /// <returns></returns>
        public override double GetActualSpeedDegPerSec()
        {
            double aspd = GetActualSpeedIU(); // (Quad encoder counts) / (slow_servo_loop time)
            double load_speed_eng = aspd / GetCountsPerEngineeringUnit() / Settings.SlowLoopServoTimeS;
            return load_speed_eng;
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// return a value between 0 and 32767, for 0-10V respectively
        /// </summary>
        /// <returns></returns>
        public override short GetAnalogReading( uint channel_0_based)
        {
            short value;
            if (!GetIntVariable("AD5", out value))
                throw new AxisException(this, String.Format("Could not read AD5 register on axis {0}: {1}", _axis_id, GetTSError()));

            // do this math to make voltage go from 0 to -32767
            const int max = 32767;
            int result = max - (int)value - 64; // 64 because at 0, the AD5 value is 63, but the value increases in increments of 64
            return Math.Abs((short)result); // make value positive from 0 - 32767
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// Sets the output bit
        /// </summary>
        /// <remarks>
        /// 13 = error
        /// 25 = ready
        /// </remarks>
        public override void SetOutput( uint bit_0_based, bool logic_high)
        {
            lock( _lock) {
                if (!Channel.TS_SelectAxis(_axis_id)) 
                    throw new AxisException( this, String.Format( "Could not select axis {0}: {1}", _axis_id, GetTSError()));
#if !TML_SINGLETHREADED
                if (!Channel.TS_SetOutput((byte)bit_0_based, logic_high ? TMLLibConst.IO_LOW : TMLLibConst.IO_HIGH))
#else
                if (!Channel.TS_SetOutput((byte)bit_0_based, logic_high ? TML.TMLLib.IO_LOW : TML.TMLLib.IO_HIGH))
#endif
                    throw new AxisException( this, String.Format( "Could not set error output #{0}", bit_0_based));
            } // lock
        }
        //---------------------------------------------------------------------
        // wrapper around TS_ReadStatus
        /// <summary>
        /// Calls TS_SelectAxis, then TS_ReadStatus.  Throws exception if we can't read the register.
        /// </summary>
        /// <remarks>
        /// This doesn't technically belong in the IAxis interface but we can clean it up later
        /// </remarks>
        /// <param name="SelIndex">The register you want to read</param>
        /// <param name="Status">The value in the register</param>
        /// <exception cref="AxisException" />
        public override void ReadStatus(Int16 SelIndex, out UInt16 Status)
        {
            Status = 0;
            bool ret = false;

            for ( int n = 0; n < max_read_retries && !ret; ++n)
            {
                lock (_lock)
                {
                    if (!Channel.TS_SelectAxis(_axis_id))
                        throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));
                    ret = Channel.TS_ReadStatus(SelIndex, out Status);
                    if (ret == false)
                    {
                        string error = String.Format("TS_ReadStatus() failed to read {0} register {1} time{2}.", SelIndex, n + 1, n > 0 ? @"s" : @"");
                        _log.Debug(error);
                        if (n >= max_read_retries)
                            throw new AxisException(this, error);
                    }
                } // lock
            } // for
        }
        //---------------------------------------------------------------------
        // wrapper around TS_GetIntVariable
        public override bool GetIntVariable( String pszName, out Int16 value)
        {
            bool ret = false;
            value = 0;

            for ( int n = 0; n < max_read_retries && !ret; ++n)
            {
                lock (_lock)
                {
                    if (!Channel.TS_SelectAxis(_axis_id))
                        throw new AxisException( this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));
                    ret = Channel.TS_GetIntVariable(pszName, out value);
                    if (ret == false)
                    {
                        _log.DebugFormat( "TS_GetIntVariable() failed to read {0} {1} time(s).", pszName, n + 1);
                    }
                } // lock
            } // for

            return ret;
        }
        //---------------------------------------------------------------------
        // wrapper around TS_GetLongVariable
        public override bool GetLongVariable( String pszName, out Int32 value)
        {
            bool ret = false;
            value = 0;

            for ( int n = 0; n < max_read_retries && !ret; ++n)
            {
                lock (_lock)
                {
                    if (!Channel.TS_SelectAxis(_axis_id))
                        throw new AxisException( this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));
                    ret = Channel.TS_GetLongVariable(pszName, out value);
                    if (ret == false)
                    {
                        _log.DebugFormat( "TS_GetLongVariable() failed to read {0} {1} time(s).", pszName, n + 1);
                    }
                } // lock
            } // for

            return ret;
        }
        //---------------------------------------------------------------------
        // wrapper around TS_GetFixedVariable
        public override bool GetFixedVariable( String pszName, out Double value)
        {
            bool ret = false;
            value = 0.0;

            for ( int n = 0; n < max_read_retries && !ret; ++n)
            {
                lock (_lock)
                {
                    if (!Channel.TS_SelectAxis(_axis_id))
                        throw new AxisException( this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));
                    ret = Channel.TS_GetFixedVariable(pszName, out value);
                    if (ret == false)
                    {
                        _log.DebugFormat( "TS_GetFixedVariable() failed to read {0} {1} time(s).", pszName, n + 1);
                    }
                } // lock
            } // for

            return ret;
        }
        //---------------------------------------------------------------------
        // wrapper around TS_GetInput
        public override bool GetInput(Byte nIO, out Byte InValue)
        {
            bool ret = false;
            InValue = 0;

            for ( int n = 0; n < max_read_retries && !ret; ++n)
            {
                lock (_lock)
                {
                    if (!Channel.TS_SelectAxis(_axis_id))
                        throw new AxisException( this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));
                    ret = Channel.TS_GetInput(nIO, out InValue);
                    if (ret == false)
                    {
                        _log.DebugFormat( "TS_GetInput() failed to read {0} {1} time(s).", nIO, n + 1);
                    }
                } // lock
            } // for

            return ret;
        }
        //---------------------------------------------------------------------
        // wrapper around TS_SetIntVariable
        public override bool SetIntVariable( String pszName, Int16 value)
        {
            bool ret;

            lock (_lock)
            {
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));
                ret = Channel.TS_SetIntVariable(pszName, value);
                if (ret == false)
                {
                    _log.DebugFormat( "TS_SetIntVariable() failed to set {0}.", pszName);
                }
            } // lock

            return ret;
        }
        //---------------------------------------------------------------------
        // wrapper around TS_SetLongVariable
        public override bool SetLongVariable( String pszName, Int32 value)
        {
            bool ret;

            lock (_lock)
            {
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));
                ret = Channel.TS_SetLongVariable(pszName, value);
                if (ret == false)
                {
                    _log.DebugFormat( "TS_SetLongVariable() failed to set {0}.", pszName);
                }
            } // lock

            return ret;
        }
        //---------------------------------------------------------------------
        // wrapper around TS_SetFixedVariable
        public override bool SetFixedVariable( String pszName, Double value)
        {
            bool ret;

            lock (_lock)
            {
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));
                ret = Channel.TS_SetFixedVariable(pszName, value);
                if (ret == false)
                {
                    _log.DebugFormat( "TS_SetFixedVariable() failed to set {0}.", pszName);
                }
            } // lock

            return ret;
        }
        //---------------------------------------------------------------------
        // sets the max possible commanded current (torque) on an axis with the SATS variable
        public override bool SetCurrentAmpsCmdLimit (double current_amps)
        {
            double Kif = 65472 / 2 / _controller_peak_current; // (bits/Amps) Formula from Page 866 of MackDaddyTechnosoftDoc
            UInt16 torque_lim_SATS_iu = (UInt16)(32767 - (int)(current_amps * Kif)); // (iu) Formula from Page 921 of MackDaddyTechnosoftDoc
            
            SetIntVariable("SATS", (Int16)torque_lim_SATS_iu); // set new SATS
            // DKM 2011-02-28 added Mark's fix from the LabAuto branch to set current limits properly
            bool ret = SetIntVariable("SATP", (Int16)torque_lim_SATS_iu); // set new SATS
            _log.DebugFormat( "Setting Z axis SATS and SATP to {0:0.000} Amps == {1} IU", current_amps, torque_lim_SATS_iu);

            return ret;
        }
        //---------------------------------------------------------------------
        // gets the max possible commanded current (torque) already set on an axis from the SATS variable
        public override bool GetCurrentAmpsCmdLimit (out double current_amps) 
        {
            Int16 old_SATS_IU;
            bool ret = GetIntVariable("SATS", out old_SATS_IU); // remember old SATS
            double old_SATS_IU_f = (double)((UInt16)(old_SATS_IU));
            double Kif = 65472 / 2 / _controller_peak_current; // (bits/Amps) Formula from Page 866 of MackDaddyTechnosoftDoc
            current_amps = (32767.0 - old_SATS_IU_f)/Kif; // (Amps) Formula from Page 921 of MackDaddyTechnosoftDoc

            return ret;
        }
        //---------------------------------------------------------------------
        public override void ResetFaults()
        {
            lock( _lock) {
                if (!Channel.TS_SelectAxis(_axis_id)) 
                    throw new AxisException( this, String.Format( "Could not select axis {0}: {1}", _axis_id, GetTSError()));
                if (!Channel.TS_ResetFault())
                    throw new AxisException( this, String.Format("Could not call FAULTR on axis {0}: {1}", _axis_id, GetTSError()));
                //if( !TMLLib.TS_SetTargetPositionToActual())
                    //throw new AxisException( this, String.Format("Could not call STA on axis {0}: {1}", _axis_id, GetTSError()));
            } // lock
        }
        //---------------------------------------------------------------------
        public override void ResetDrive()
        {
            lock( _lock) {
                if (!Channel.TS_SelectAxis(_axis_id)) 
                    throw new AxisException( this, String.Format( "Could not select axis {0}: {1}", _axis_id, GetTSError()));
                if (!Channel.TS_Reset())
                    throw new AxisException( this, String.Format("Could not call RESET on axis {0}: {1}", _axis_id, GetTSError()));
            }
        }
        //---------------------------------------------------------------------
        // ResetFaultsOnAllAxes() looks really dangerous. I wouldn't call it if I were you. It resets the controller, not just reset faults...
        public override void ResetFaultsOnAllAxes()
        {
            lock( _lock) {
                if (!Channel.TS_SelectAxis(_axis_id)) 
                    throw new AxisException( this, String.Format( "Could not select axis {0}: {1}", _axis_id, GetTSError()));
                if (!Channel.TS_Execute("[B] { reset; }"))
                    throw new AxisException( this, String.Format("Could not call [B]RESET on axis {0}: {1}", _axis_id, GetTSError()));
            } // lock
        }
        //---------------------------------------------------------------------
        // Set a bit in mask if you don't want that bit to be high in the return vlaue. Default is to have mask==0.
        public UInt16 GetTS_MER (UInt16 mask)
        {
            UInt16 mer;

#if !TML_SINGLETHREADED
            ReadStatus(TMLLibConst.REG_MER, out mer);
#else
            ReadStatus(TML.TMLLib.REG_MER, out mer);
#endif

            mer = (UInt16)(mer & ~mask); // only keep the bits we care about

            return mer;
        }
        //---------------------------------------------------------------------
        public List<string> BuildTSFaultsStrings (UInt16 mer)
        {
            List<string> errors = new List<string>();

            // compare all of the bits of the MER
            if( (mer & TMLLIB_MER_BIT_ENABLEINPUTINACTIVE) != 0)
                errors.Add( "Enable input is inactive");
            if( (mer & TMLLIB_MER_BIT_COMMANDERROR) != 0)
                errors.Add( "Command error");
            if( (mer & TMLLIB_MER_BIT_UNDERVOLTAGE) != 0)
                errors.Add( "Under voltage error");
            if( (mer & TMLLIB_MER_BIT_OVERVOLTAGE) != 0)
                errors.Add( "Over voltage error");
            if( (mer & TMLLIB_MER_BIT_OVERTEMPDRIVE) != 0)
                errors.Add( "Drive over temperature error");
            if( (mer & TMLLIB_MER_BIT_OVERTEMPMOTOR) != 0)
                errors.Add( "Motor temperature error");
            if( (mer & TMLLIB_MER_BIT_I2T) != 0)
                errors.Add( "Drive or motor I2T error");
            if( (mer & TMLLIB_MER_BIT_OVERCURRENT) != 0)
                errors.Add( "Overcurrent error");
            if( (mer & TMLLIB_MER_BIT_LSNACTIVE) != 0)
                errors.Add( "LSN active");
            if ((mer & TMLLIB_MER_BIT_LSPACTIVE) != 0)
                errors.Add( "LSP active");
            if( (mer & TMLLIB_MER_BIT_POSITIONWRAPAROUND) != 0)
                errors.Add( "Hall sensor missing or Position wrap-around error");
            if( (mer & TMLLIB_MER_BIT_SERIALCOMMERROR) != 0)
                errors.Add( BuildSerialCommErrorString());
            if( (mer & TMLLIB_MER_BIT_CONTROLERROR) != 0)
                errors.Add( "Control error");
            if( (mer & TMLLIB_MER_BIT_INVALIDSETUPDATA) != 0)
                errors.Add( "Setup table invalid");
            if( (mer & TMLLIB_MER_BIT_SHORTCIRCUIT) != 0)
                errors.Add( "Short-circuit error");
            if( (mer & TMLLIB_MER_BIT_CANBUSERROR) != 0)
                errors.Add( "CANbus error");

            return errors;
        }
        //---------------------------------------------------------------------
        private static string BuildSerialCommErrorString()
        {
            /*
            // if we get a serial comm error, this means that we need to query CER
            ushort mer_reg = 0;
            // ReadStatus locks and selects axis
            ReadStatus(TMLLib.REG_MER, out mer_reg);
             */
            return "Serial communication error";
        }
        //---------------------------------------------------------------------
        public override List<string> GetFaults()
        {
            return GetFaults(0x000); // return all the errors
        }
        //---------------------------------------------------------------------
        // Set a bit in mask if you don't want that bit to trigger an error. Default is to have mask==0.
        public override List<string> GetFaults(UInt16 mask)
        {
            ushort mer = GetTS_MER(mask);
            return BuildTSFaultsStrings(mer);
        }
        //---------------------------------------------------------------------
        public override double GetCountsPerEngineeringUnit()
        {
            // need to take encoder resolution and gearing into account
            return _settings.EncoderLines * 4 / _settings.GearRatio;
        }
        //---------------------------------------------------------------------
        public override void Stop()
        {
            lock (_lock)
            {
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));
                CallFunctionAndWaitForDone( "func_my_stop", TimeSpan.FromMilliseconds( 5000));
            } // lock
        }
        //---------------------------------------------------------------------
        /*
        private int ConvertEUToIU( double eu)
        {
            // eu / gear_ratio * (encoder counts per eu)
            return (int)(eu / _settings.GearRatio * (_settings.EncoderLines * 4));
        }
         */
        //---------------------------------------------------------------------
        public override void AddToGroup( byte group_id)
        {
            lock( _lock) {
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format( "Could not select axis {0}: {1}", _axis_id, GetTSError()));
                if (!Channel.TS_Execute(String.Format("ADDGRID({0});", group_id)))
                    throw new AxisException( this, String.Format( "Could not add axis {0} to group {1}: {2}", _axis_id, group_id, GetTSError()));
            } // lock
        }
        //---------------------------------------------------------------------
        public override void RemoveFromGroup( byte group_id)
        {
            lock( _lock) {
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format( "Could not select axis {0}: {1}", _axis_id, GetTSError()));
                if (!Channel.TS_Execute(String.Format("REMGRID({0});", group_id)))
                    throw new AxisException( this, String.Format( "Could not remove axis {0} from group {1}: {2}", _axis_id, group_id, GetTSError()));
            } // lock
        }
        //---------------------------------------------------------------------
        public override bool IsMoveComplete()
        {
            short value;
            if (!GetIntVariable("move_complete", out value))
                throw new AxisException(this, String.Format("Could not get int variable 'move_complete' from axis {0}: {1}", _axis_id, GetTSError()));
            return value != 0;
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// Queries SRH:bit9 to see if target (pos/spd) is reached
        /// </summary>
        /// <returns></returns>
        public override bool IsTargetReached() 
        {
            ushort status;
#if !TML_SINGLETHREADED
            ReadStatus(TMLLibConst.REG_SRH, out status);
#else
            ReadStatus(TML.TMLLib.REG_SRH, out status);
#endif
            return (TMLLIB_SRH_BIT_TARGETREACHED == (status & TMLLIB_SRH_BIT_TARGETREACHED));
        } 
        //---------------------------------------------------------------------
        public override int ConvertToCounts(double mm_or_ul)
        {
            return (int)(GetCountsPerEngineeringUnit() * mm_or_ul);
        }
        //---------------------------------------------------------------------
        // does a conversion from quad counts to Engineering units
        public override double ConvertCountsToEng(int counts)
        {
            return (double)counts / GetCountsPerEngineeringUnit();
        }
        //---------------------------------------------------------------------
        public override short ConvertToTicks( short time_ms)
        {
            return TechnosoftConnection.ConvertTimeToIU( time_ms, _settings.SlowLoopServoTimeS);
        }
        //---------------------------------------------------------------------
        public override string GetConversionFormula()
        {
            return _conversion_formula;
        }
        //---------------------------------------------------------------------
        public override void SetConversionFormula( string formula)
        {
            _conversion_formula = formula;
        }
        //---------------------------------------------------------------------
        /* not used.
        private void CheckForErrorAndThrow()
        {
            CheckMER();
            //CheckMSR();
            CheckSRH();
            //CheckSRL();
        }
        */
        //---------------------------------------------------------------------
        /*
        private void CheckMER()
        {
            List<string> faults = GetFaults();
            if( faults.Count != 0) {
                string[] error_array = new string[faults.Count];
                faults.CopyTo( error_array);
                throw new AxisException( this, String.Join( ", ", error_array, 0, faults.Count));
            }
        }
        */
        //---------------------------------------------------------------------
        /*
        private void CheckSRH()
        {
            ushort srh;

#if !TML_SINGLETHREADED
            ReadStatus(TMLLibConst.REG_SRH, out srh);
#else
            ReadStatus(TML.TMLLib.REG_SRH, out srh);
#endif
            if( (srh & TMLLIB_SRH_BIT_FAULT) != 0)
                throw new AxisException( this, "Drive / motor in fault status");
            if( (srh & TMLLIB_SRH_BIT_I2TWARNINGDRIVE) != 0)
                throw new AxisException( this, "Drive I2T warning limit reached");
            if( (srh & TMLLIB_SRH_BIT_I2TWARNINGMOTOR) != 0)
                throw new AxisException( this, "Motor I2T warning limit reached");
            if( (srh & TMLLIB_SRH_BIT_ENDINITEXECUTED) == 0)
                throw new AxisException( this, "Drive / motor not initialized");
        }
        */
        //---------------------------------------------------------------------
        public override void StartLogging()
        {
            // for now, only log the w axes
            if( _axis_id % 10 != 4)
                return;
            lock( _lock) {
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format( "Could not select axis {0}: {1}", _axis_id, GetTSError()));
                // I took the buffer info from EMS in the logger window
                // load position address is from variables.cfg
                ushort[] vars = new ushort[1];
                vars[0] = 0x228;
                if (!Channel.TS_SetupLogger(0x8270, 512, vars, 1, 5))
                    throw new AxisException( this, String.Format("Could not SetupLogger on axis {0}: {1}", _axis_id, GetTSError()));
#if !TML_SINGLETHREADED
                if (!Channel.TS_StartLogger(0x8270, TMLLibConst.LOGGER_SLOW))
#else
                if (!Channel.TS_StartLogger(0x8270, TML.TMLLib.LOGGER_SLOW))
#endif
                    throw new AxisException( this, String.Format("Could not start logger on axis {0}: {1}", _axis_id, GetTSError()));
            } // lock
        }
        //---------------------------------------------------------------------
        public override void WaitForLoggingComplete( string filepath)
        {
            // for now, only log the w axes
            if( _axis_id % 10 != 4)
                return;
            lock( _lock) {
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format( "Could not select axis {0}: {1}", _axis_id, GetTSError()));

                ushort status;
                do {
                    if (!Channel.TS_CheckLoggerStatus(0x8270, out status))
                        throw new AxisException( this, String.Format("Could not CheckLoggerStatus on axis {0}: {1}", _axis_id,GetTSError()));
                } while( status != 0);

                ushort[] values = new ushort[512];
                ushort num_values = 512;
                if (!Channel.TS_UploadLoggerResults(0x8270, values, ref num_values))
                    throw new AxisException( this, String.Format("Could not UploadLoggerResults on axis {0}: {1}", _axis_id, GetTSError()));
                // write the data to the output file
                AppendDatapointsToFile( values, num_values, filepath);
            } // lock
        }
        //---------------------------------------------------------------------
        public override bool IsHoming()
        {
            return _is_homing;
        }
        //---------------------------------------------------------------------
        public override void SetSpeedFactor( int speed)
        {
            lock( _speed_lock)
                _speed_percentage = speed / 100.0;
        }
        //---------------------------------------------------------------------
        public override double GetSpeedFactor()
        {
            lock( _speed_lock)
                return _speed_percentage;
        }
        //---------------------------------------------------------------------
        public override bool IsOn()
        {
            // Check the status of the power stage
            ushort status;
#if !TML_SINGLETHREADED
            ReadStatus(TMLLibConst.REG_SRL, out status);
#else
            ReadStatus(TML.TMLLib.REG_SRL, out status);
#endif
            return ((status & TMLLIB_SRL_BIT_AXISON) == TMLLIB_SRL_BIT_AXISON);
        }
        //---------------------------------------------------------------------
        private static void AppendDatapointsToFile( ushort[] values, ushort num_values, string filepath)
        {
            // open the text file in append mode
            TextWriter writer = new StreamWriter( filepath, true);
            for( ushort i=0; i<num_values; i++)
                writer.WriteLine( values[i].ToString());
            writer.WriteLine();
            writer.Close();
        }
        //---------------------------------------------------------------------
        public override void MasterCamOnOff(byte slave_axis_id, bool on_off)
        {
            DateTime datetime_start_func = DateTime.Now;

            if( on_off)
            { // turn camming on
                // SRL = 0; // SRL:10 motion complete should be cleared before this is run, just to be sure
                // TUM1; // use last TPOS to command
                // SRB OSR, 0xFFFF, 0x8000; // Send Position Reference (as opposed to actual position)
                // SETSYNC 20L; // Send sync messages every 20ms
                // TODO: Change hardcoded 20L to the return value of a function call to get slow servo ticks for 20ms
                //string exe_str = String.Format("SRL = 0; CPOS = 0L; CPR; MODE PP; TUM1; SLAVEID = {0}; SRB OSR, 0xFFFF, 0x8000; SGM; UPD; SETSYNC 20L;", slave_axis_id);
                string exe_str = String.Format("SRL = 0; CPOS = 0L; CPR; MODE PP; TUM1; SLAVEID = {0}; SRB OSR, 0xFFFF, 0x8000; SGM; UPD;", slave_axis_id);
                _log.DebugFormat( "MasterCamOnOff(on) Sent \"{0}\"", exe_str);

                lock (_lock)
                {
                    if (!Channel.TS_SelectAxis(_axis_id))
                        throw new AxisException( this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                    if (!Channel.TS_Execute(exe_str))
                    {
                        _log.DebugFormat( "MasterCamOnOff(on) TS_Execute(\"{0}\") failed.", exe_str);
                        throw new AxisException( this, String.Format("Could not send \"{0}\" to axis {1} : {2}",
                            exe_str, _axis_id, GetTSError()));
                    }
                } // lock
                DateTime wait_time_start = DateTime.Now;
                bool waiting = true;
                TimeSpan wait_timeout = TimeSpan.FromSeconds(5.0);
                while (waiting)
                {
                    ushort srl_reg;
                    TimeSpan period = DateTime.Now - wait_time_start;

                    if (period >= wait_timeout)
                    {
                        _log.DebugFormat( "MasterCamOnOff(on) Timed out waiting for MOTIONCOMPLETE after {0:0.000} seconds.", period.TotalSeconds);
                        throw new AxisException( this, String.Format("Timed out waiting for Motion Complete bit to raise after STARTING MASTER cam on axis_{0}", _axis_id));
                    }

                    Thread.Sleep(10);
#if !TML_SINGLETHREADED
                    ReadStatus(TMLLibConst.REG_SRL, out srl_reg);
#else
                    ReadStatus(TML.TMLLib.REG_SRL, out srl_reg);
#endif
                    waiting = (srl_reg & TMLLIB_SRL_BIT_MOTIONCOMPLETE) != TMLLIB_SRL_BIT_MOTIONCOMPLETE;
                    if ((srl_reg & TMLLIB_SRL_BIT_AXISON) != TMLLIB_SRL_BIT_AXISON)
                    {
                        _log.DebugFormat( "MasterCamOnOff(on) Axis is off? Why?");
                        throw new AxisException(this, String.Format("Did axis_{0} fault? Check to make sure everything is okay.", _axis_id));
                    }
                } // while (waiting)
            } // if (turn camming ON)
            else
            { // turn camming off
                // SRL = 0; // SRL:10 motion complete should be cleared before this is run, just to be sure
                // TUM1; // use last TPOS to command
                // SETSYNC 0; // turn off synchronization messages
                //string exe_str = String.Format("SRL = 0; CPOS = 0L; CPR; MODE PP; TUM1; RGM; UPD; SETSYNC 0;");
                string exe_str = String.Format("SRL = 0; CPOS = 0L; CPR; MODE PP; TUM1; RGM; UPD;");
                _log.DebugFormat( "MasterCamOnOff(off) Sent \"{0}\"",  exe_str);
                lock (_lock)
                {
                    if (!Channel.TS_SelectAxis(_axis_id))
                        throw new AxisException( this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                    if (!Channel.TS_Execute(exe_str))
                    {
                        _log.DebugFormat( "MasterCamOnOff(off) TS_Execute(\"{0}\") failed.", exe_str);
                        throw new AxisException( this, String.Format("Could not send \"{0}\" to axis {1} : {2}",
                            exe_str, _axis_id, GetTSError()));
                    }
                } // lock
                DateTime wait_time_start = DateTime.Now;
                bool waiting = true;
                TimeSpan wait_timeout = TimeSpan.FromSeconds(5.0);
                while (waiting)
                {
                    ushort srl_reg;
                    TimeSpan period = DateTime.Now - wait_time_start;

                    if (period >= wait_timeout)
                    {
                        _log.DebugFormat( "MasterCamOnOff(off) Timed out waiting for MOTIONCOMPLETE after {0:0.000} seconds.", period.TotalSeconds);
                        throw new AxisException( this, String.Format("Timed out waiting for Motion Complete bit to raise after STOPPING MASTER cam on axis_{0}", _axis_id));
                    }

                    Thread.Sleep(10);

#if !TML_SINGLETHREADED
                    ReadStatus(TMLLibConst.REG_SRL, out srl_reg);
#else
                    ReadStatus(TML.TMLLib.REG_SRL, out srl_reg);
#endif
                    waiting = (srl_reg & TMLLIB_SRL_BIT_MOTIONCOMPLETE) != TMLLIB_SRL_BIT_MOTIONCOMPLETE;
                    if ((srl_reg & TMLLIB_SRL_BIT_AXISON) != TMLLIB_SRL_BIT_AXISON)
                    {
                        _log.DebugFormat( "MasterCamOnOff(off) Axis is off? Why?");
                        throw new AxisException(this, String.Format("Did axis_{0} fault? Check to make sure everything is okay.", _axis_id));
                    }
                } // while (waiting)
            } // if (turn camming off)

            TimeSpan func_ts = DateTime.Now - datetime_start_func;
            _log.DebugFormat( "MasterCamOnOff({0}) Done in {1:0}ms.", on_off ? "on" : "off", func_ts.TotalMilliseconds);
        }
        //---------------------------------------------------------------------
        public override void SlaveCamOnOff(UInt16 cam_address, bool on_off, double max_speed_iu, int cam_pos_offset_iu)
        {
            DateTime datetime_start_func = DateTime.Now;

            if( on_off)
            { // turn slave camming on
                // SRH = 0; // clears SRH:14 In Cam status bit
                // TUM1; // use last TPOS to command
                // SRB ACR, 0xFFFF, 0x1000; // Camming mode: Absolute
                // SRB UPGRADE, 0xFFFF, 0x4; // Limit intial speed to get to first point by CSPD
                // CSPD = ...; // Limit max speed to start camming (should never be used)
                string exe_str = String.Format("SRH = 0; CPOS = 0L; CPR; MODE PP; TUM0; CAMOFF = {0}L; CAMSTART = 0x{1:x4}; " +
                        "MPOS0 = MREF; MPOS0 -= CAMOFF; EXTREF 0; MASTERRES = 0x80000001; MODE CS; TUM1; " +
                        "SRB ACR, 0xFFFF, 0x1000; SRB UPGRADE, 0xFFFF, 0x4; CSPD = {2:0.00000}; UPD;",
                        cam_pos_offset_iu, cam_address, max_speed_iu);
                _log.DebugFormat( "SlaveCamOnOff(on) Sent \"{0}\"", exe_str);

                lock (_lock)
                {
                    if (!Channel.TS_SelectAxis(_axis_id))
                        throw new AxisException( this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                    if (!Channel.TS_Execute(exe_str)) {
                        _log.DebugFormat( "SlaveCamOnOff(on) TS_Execute(\"{0}\") failed.", exe_str);
                        throw new AxisException( this, String.Format("Could not send \"{0}\" to axis {1} : {2}",
                            exe_str, _axis_id, GetTSError()));
                    }
                } // lock

                DateTime wait_time_start = DateTime.Now;
                bool waiting = true;
                TimeSpan wait_timeout = TimeSpan.FromSeconds(5.0);
                while (waiting)
                {
                    ushort srh_reg;
                    ushort srl_reg;
                    TimeSpan period = DateTime.Now - wait_time_start;

                    if (period >= wait_timeout)
                    {
                        _log.DebugFormat( "SlaveCamOnOff(on) Timed out waiting for IN CAM to raise after {0:0.000} seconds.", period.TotalSeconds);
                        throw new AxisException( this, String.Format("Timed out waiting for In Cam bit to raise after STARTING SLAVE CAM on axis_{0}", _axis_id));
                    }

                    Thread.Sleep(10);
#if !TML_SINGLETHREADED
                    ReadStatus(TMLLibConst.REG_SRH, out srh_reg);
#else
                    ReadStatus(TML.TMLLib.REG_SRH, out srh_reg);
#endif
                    waiting = (srh_reg & TMLLIB_SRH_BIT_INCAM) != TMLLIB_SRH_BIT_INCAM;

#if !TML_SINGLETHREADED
                    ReadStatus(TMLLibConst.REG_SRL, out srl_reg);
#else
                    ReadStatus(TML.TMLLib.REG_SRL, out srl_reg);
#endif
                    if ((srl_reg & TMLLIB_SRL_BIT_AXISON) != TMLLIB_SRL_BIT_AXISON)
                    {
                        _log.DebugFormat( "SlaveCamOnOff(on) Axis is off? Why?");
                        throw new AxisException(this, String.Format("Did axis_{0} fault? Check to make sure everything is okay.", _axis_id));
                    }
                } // while (waiting)
            } // turn (slave camming on)
            else
            { // turn slave camming off
                // SRL = 0; // SRL:10 motion complete should be cleared before this is run, just to be sure
                // TUM1; // use last TPOS to command
                //string exe_str = String.Format("SRL = 0; CPOS = 0L; CPR; MODE PP; TUM1; UPD; SETSYNC 0L;");
                string exe_str = String.Format("SRL = 0; CPOS = 0L; CPR; MODE PP; TUM0; UPD;");
                _log.DebugFormat( "SlaveCamOnOff(off) Sent \"{0}\"", exe_str);
                lock (_lock)
                {
                    if (!Channel.TS_SelectAxis(_axis_id))
                        throw new AxisException( this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));

                    if (!Channel.TS_Execute(exe_str))
                    {
                        _log.DebugFormat( "SlaveCamOnOff(off) TS_Execute(\"{0}\") failed.", exe_str);
                        throw new AxisException( this, String.Format("Could not send \"{0}\" to axis {1} : {2}",
                            exe_str, _axis_id, GetTSError()));
                    }
                }
                DateTime wait_time_start = DateTime.Now;
                bool waiting = true;
                TimeSpan wait_timeout = TimeSpan.FromSeconds(5.0);
                while (waiting)
                {
                    ushort srl_reg;
                    TimeSpan period = DateTime.Now - wait_time_start;

                    if (period >= wait_timeout)
                    {
                        _log.DebugFormat( "SlaveCamOnOff(off) Timed out waiting for MOTIONCOMPLETE after {0:0.000} seconds.", period.TotalSeconds);
                        throw new AxisException( this, String.Format("Timed out waiting for Motion Complete bit to raise after STOPPING SLAVE cam on axis_{0}", _axis_id));
                    }

                    Thread.Sleep(10);

#if !TML_SINGLETHREADED
                    ReadStatus(TMLLibConst.REG_SRL, out srl_reg);
#else
                    ReadStatus(TML.TMLLib.REG_SRL, out srl_reg);
#endif

                    waiting = (srl_reg & TMLLIB_SRL_BIT_MOTIONCOMPLETE) != TMLLIB_SRL_BIT_MOTIONCOMPLETE;
                    if ((srl_reg & TMLLIB_SRL_BIT_AXISON) != TMLLIB_SRL_BIT_AXISON)
                    {
                        _log.DebugFormat( "SlaveCamOnOff(off) Axis is off? Why?");
                        throw new AxisException(this, String.Format("Did axis_{0} fault? Check to make sure everything is okay.", _axis_id));
                    }
                } // while (waiting)
            } // if (slave cam turn off)

            TimeSpan func_ts = DateTime.Now - datetime_start_func;
            _log.DebugFormat( "SlaveCamOnOff({0}) Done in {1:0}ms.", on_off ? "on" : "off", func_ts.TotalMilliseconds);
        } // SlaveCamOnOff()
        //---------------------------------------------------------------------
        public double blended_move_dest_position { get; private set; }
        public override void SetupBlendedMove( double position, bool set_event_on_complete, bool use_trap)
        {
            if( position < _settings.MinLimit) {
                throw new AxisException(this, String.Format("Cannot move to {0:0.000} because it is past the minimum travel limit of {1:0.000}", position, _settings.MinLimit));
            } else if( position > _settings.MaxLimit) {
                throw new AxisException(this, String.Format("Cannot move to {0:0.000} because it is past the maximum travel limit of {1:0.000}", position, _settings.MaxLimit));
            }

             // be sure axis is enabled and seroving
            _log.DebugFormat( "TurnAxisOnIfNecessary(true) called via SetupBlendedMove() for axis {0}", _axis_id);
            TurnAxisOnIfNecessary( true);

            lock( _lock) {
                _log.DebugFormat( "setting up blending for axis_{0} to go to {1}mm, set event on complete = {2}", _axis_id, position, set_event_on_complete);
                //	Select the destination axis of the TML commands  
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format( "Could not select axis_{0}: {1}", _axis_id, GetTSError()));

                //! \bug this is a hack for now.  We know that the Z axis is the only one that will
                //!      have set_event_on_complete = true.  So in this case, I want to change
                //!      the pos limits so the W axis doesn't move early.
                if( set_event_on_complete) {
                    // set the position limit and settling time before moving
                    if (!Channel.TS_SetIntVariable("POSOKLIM", (short)ConvertToCounts(_settings.MoveDoneWindow)))
                        throw new AxisException( this, String.Format("Could not set POSOKLIM on axis_{0}: {1}", _axis_id, GetTSError()));
                    if (!Channel.TS_SetIntVariable("TONPOSOK", ConvertToTicks(_settings.SettlingTimeMS)))
                        throw new AxisException( this, String.Format("Could not set TONPOSOK on axis_{0}: {1}", _axis_id, GetTSError()));
                }
            } // lock

            lock( _lock) {
                //	Select the destination axis of the TML commands  
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format( "Could not select axis_{0}: {1}", _axis_id, GetTSError()));
                if (!Channel.TS_SetIntVariable("xz_blending_complete", 0))
                    throw new AxisException( this, String.Format( "Could not setup axis_{0}: {1}", _axis_id, GetTSError()));
                _log.Debug( "setting xz_blending_complete to 0");
                //	Command the Scurve positioning using the prescribed parameters; start the motion immediately  
                //! \todo figure out how to get return value in Autos window so I don't have to make these temp variables!
                blended_move_dest_position = position; // save this for later
                int counts = ConvertToCounts( position);
                double velocity = TechnosoftConnection.ConvertVelocityToIU( _settings.Velocity, _settings.SlowLoopServoTimeS, _settings.EncoderLines, _settings.GearRatio);
                double acceleration = TechnosoftConnection.ConvertAccelerationToIU( _settings.Acceleration, _settings.SlowLoopServoTimeS, _settings.EncoderLines, _settings.GearRatio);
                int jerk = _settings.Jerk;

                if( UseTrapezoidalProfileByDefault || use_trap) {
#if !TML_SINGLETHREADED
                    if (!Channel.TS_MoveAbsolute(counts, velocity * _speed_percentage, acceleration * (_speed_percentage * 0.5 + 0.5), TMLLibConst.UPDATE_NONE, TMLLibConst.FROM_MEASURE))
#else
                    if (!Channel.TS_MoveAbsolute(counts, velocity * _speed_percentage, acceleration * (_speed_percentage * 0.5 + 0.5), TML.TMLLib.UPDATE_NONE, TML.TMLLib.FROM_MEASURE))
#endif
                        throw new AxisException( this, String.Format("Could not call TS_MoveAbsolute on axis_{0}: {1}", _axis_id, GetTSError()));
                } else {
#if !TML_SINGLETHREADED
                    if (!Channel.TS_MoveSCurveAbsolute(counts, velocity * _speed_percentage, acceleration * (_speed_percentage * 0.5 + 0.5), jerk, TMLLibConst.UPDATE_NONE, TMLLibConst.S_CURVE_SPEED_PROFILE))
#else
                    if (!Channel.TS_MoveSCurveAbsolute(counts, velocity * _speed_percentage, acceleration * (_speed_percentage * 0.5 + 0.5), jerk, TML.TMLLib.UPDATE_NONE, TML.TMLLib.S_CURVE_SPEED_PROFILE))
#endif
                        throw new AxisException( this, String.Format("Could not call TS_MoveSCurveAbsolute on axis_{0}: {1}", _axis_id, GetTSError()));
                }          

                if( set_event_on_complete) {
                    if (!Channel.TS_SetEventOnMotionComplete(false, false))
                    {
                        throw new AxisException( this, String.Format( "Could not setup event on axis_{0}: {1}", _axis_id, GetTSError()));
                    }
                }
            } // lock
        } // SetupBlendedMove()
        //---------------------------------------------------------------------
        public override void StartBlendedMove()
        {
            lock( _lock) {
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format( "Could not select axis_{0}: {1}", _axis_id, GetTSError()));
                _log.Debug( "call func_move_blended");
                if (!Channel.TS_CALL_Label("func_move_blended"))
                    throw new AxisException( this, String.Format( "Could not start move on axis_{0}: {1}", _axis_id, GetTSError()));
           } // lock
        }
        //---------------------------------------------------------------------
        public override void WaitForBlendedMoveComplete( IAxis master_axis)
        {
            short xz_blending_complete;
            short last_value = -1;
            DateTime start = DateTime.Now;
            //bool timeout_occurred = false; -- unused
            do {
                Thread.Sleep(50);
                short master_blending_complete;
                master_axis.GetIntVariable("xz_blending_complete", out master_blending_complete);
                _log.DebugFormat( "master axis_{0} reports xz_blending_complete = {1}", master_axis.GetID(), master_blending_complete);
                if (master_blending_complete == 0)
                {
                    throw new AxisException(master_axis, "Master axis did not start its move on time.  It is safe to try again.");
                }
                // check the master axis for failures
                List<string> faults = master_axis.GetFaults();
                if (faults.Count != 0)
                {
                    _log.DebugFormat( "master axis_{0} reports {1} faults. This is the first: {2}", master_axis.GetID(), faults.Count, faults[0]);
                    int CPOS;
                    master_axis.GetLongVariable("CPOS", out CPOS);
                    double commanded_pos = master_axis.ConvertCountsToEng(CPOS);
                    double actual_pos = master_axis.GetPositionMM();
                    _log.DebugFormat( "master axis_{0} commanded to {1:0.000}mm. At {2:0.000}mm now.", master_axis.GetID(), commanded_pos, actual_pos);
                    throw new AxisException(master_axis, faults[0]);
                }
                faults = GetFaults();
                if (faults.Count != 0)
                {
                    _log.DebugFormat( "slave axis_{0} reports {1} faults. This is the first: {2}", GetID(), faults.Count, faults[0]);
                    int CPOS;
                    GetLongVariable("CPOS", out CPOS);
                    double commanded_pos = ConvertCountsToEng(CPOS);
                    double actual_pos = GetPositionMM();
                    _log.DebugFormat( "slave axis_{0} commanded to {1:0.000}mm. At {2:0.000}mm now.", GetID(), commanded_pos, actual_pos);
                    throw new AxisException(this, faults[0]);
                }
                // if we get here, then the master axis is okay, and we should wait for the slave axis
                // to finish its move.
                if (!GetIntVariable("xz_blending_complete", out xz_blending_complete))
                    throw new AxisException(this, String.Format("Could not retrieve blending complete flag for axis_{0}: {1}", _axis_id, GetTSError()));
                if (xz_blending_complete != last_value)
                {
                    last_value = xz_blending_complete;
                    _log.DebugFormat( "axis_{0} reports xz_blending_complete = {1}", master_axis.GetID(), xz_blending_complete);
                }
            } while( xz_blending_complete != 2 && (DateTime.Now - start).TotalSeconds < 5); // for now, use a 5 second timeout on the move
            if( (DateTime.Now - start).TotalSeconds >= 5)
            {
                //timeout_occurred = true; -- unused
                _log.DebugFormat( "Slave axis_{0} did not complete its move on time within 5 secs. Trying traditional MoveAbs commands now...", GetID());
                //throw new AxisException( this, String.Format("Slave axis_{0} did not complete its move on time", GetID()));
            }

            // Now, Let's be absolutely sure we got to where we wanted to go...
            // First, check the master
            TSAxis ts_master_axis = (TSAxis)master_axis;
            int master_apos_iu = ts_master_axis.GetPositionCounts();
            if (Math.Abs(ts_master_axis.ConvertToCounts(ts_master_axis.blended_move_dest_position) -  master_apos_iu) > 
                ts_master_axis.ConvertToCounts(ts_master_axis._settings.MoveDoneWindow))
            {
                // did not make it to dest. Try traditional MoveAbsolute command to get it closer
                master_axis.MoveAbsolute(ts_master_axis.blended_move_dest_position);
            }
            // Second, check the slave
            int apos_iu = GetPositionCounts();
            if (Math.Abs(ConvertToCounts(blended_move_dest_position) - apos_iu) > ConvertToCounts(_settings.MoveDoneWindow))
            {
                // did not make it to dest. Try traditional MoveAbsolute command to get it closer
                MoveAbsolute(blended_move_dest_position, use_trap: true);
            }
        }
        //---------------------------------------------------------------------
        public override void CallFunctionWithPeriodicActions( string function_name, int first_action_pos, int interval,
                                                              short number_of_actions, double velocity, double accel, int jerk)
        {
            lock( _lock) {
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format( "Could not select axis {0}: {1}", _axis_id, GetTSError()));
                if (!Channel.TS_SetLongVariable("TRIG_POS_IU", first_action_pos))
                    throw new AxisException( this, String.Format( "Could not set trigger position: {0}", GetTSError()));
                if (!Channel.TS_SetFixedVariable("CSPD", velocity))
                    throw new AxisException( this, String.Format( "Could not set periodic action speed: {0}", GetTSError()));
                if (!Channel.TS_SetFixedVariable("CACC", accel))
                    throw new AxisException( this, String.Format( "Could not set periodic action acceleration: {0}", GetTSError()));
                if (!Channel.TS_SetLongVariable("TJERK", jerk))
                    throw new AxisException( this, String.Format( "Could not set periodic action jerk: {0}", GetTSError()));
                if (!Channel.TS_SetLongVariable("SHELF_DELTA_IU", interval))
                    throw new AxisException( this, String.Format( "Could not set trigger interval: {0}", GetTSError()));
                if (!Channel.TS_SetIntVariable("NUM_SHELVES", number_of_actions))
                    throw new AxisException( this, String.Format( "Could not set number of triggers: {0}", GetTSError()));
                //if( !TMLLib.TS_CALL_Label( function_name))
                    //throw new AxisException( this, String.Format( "Could not call periodic function: {0}", GetTSError()));
            }
            CallFunctionAndWaitForDone( function_name, TimeSpan.FromSeconds(30.0)); // hardcoded to 30 sec timeout for now. Todo: Make timeout a parameter.
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// non-blocking call which simply fires off a TS_CALL_Label command
        /// </summary>
        /// <param name="function_name"></param>
        public override void CallFunction(string function_name) 
        {
            lock (_lock)
            {
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));
                if (!Channel.TS_CALL_Label(function_name))
                    throw new AxisException(this, String.Format("Could not call function: {0}", GetTSError()));
            }
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// blocking call that waits for func_done variable to be non-zero after firing off a TS_CALL_Label command. 
        /// returns func_done variable read from controller if return_func_done is true, otherwise will throw exceptions if func_done is not either 0 or 1.
        /// </summary>
        /// <param name="function_name"></param>
        /// <param name="ts_timeout"></param>
        /// <param name="return_func_done"></param>
        /// <returns></returns>
        public override int CallFunctionAndWaitForDone(string function_name, TimeSpan ts_timeout, bool return_func_done=false) 
        {
            DateTime datetime_start_func = DateTime.Now;

            _log.DebugFormat( "CallFunctionAndWaitForDone({0}) called.", function_name);

            SetLongVariable("func_done", 0);
            CallFunction(function_name);
            Int32 func_done = 0;
            // wait for func_done to != 0
            while( (DateTime.Now - datetime_start_func) <= ts_timeout) {
                GetLongVariable("func_done", out func_done);
                if (0 != func_done) {
                    break;
                }
                Thread.Sleep(5); // Felix thinks 5ms is a good amount of time to sleep for this kind of polling for low latency PVT setup. Mark thinks it is too quick, but is okay with it.
            }

            var ElapsedTime = DateTime.Now - datetime_start_func;

            _log.DebugFormat( "CallFunctionAndWaitForDone({0}) returned func_done=={1} in {2:0} ms", 
                function_name, func_done, ElapsedTime.TotalMilliseconds);

            if ((ElapsedTime >= ts_timeout) || ((1 != func_done) && (!return_func_done)) )
            {
                string message = String.Format("Function '{0}' timed out after {1:0.0}s", function_name, ElapsedTime.TotalSeconds);
                // try to build the MER error string
                string mer_string = "";
                try
                {
                    ushort mer_reg;
#if !TML_SINGLETHREADED					
                    ReadStatus(TMLLibConst.REG_MER, out mer_reg);
#else
                    ReadStatus(TML.TMLLib.REG_MER, out mer_reg);
#endif
                    List<String> mer_str_list = BuildTSFaultsStrings(mer_reg);
                    mer_string = mer_str_list.ToCommaSeparatedString();
                }
                finally
                {
                    string details = String.Format("func_done={0}, servo errors={1}", func_done, mer_string);
                    //throw new AxisException(this, String.Format("TS_Call_Label({0}) timed out after {1:0.000} secs with func_done=={2}", function_name, ElapsedTime.TotalSeconds, func_done));
                    throw new AxisException(this, message, details);
                }
            }

            return func_done;
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// non-blocking call which simply fires off a TS_GOTO_Label command
        /// </summary>
        /// <param name="function_name"></param>
        public override void GotoFunction(string function_name) 
        {
            lock (_lock)
            {
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException(this, String.Format("Could not select axis {0}: {1}", _axis_id, GetTSError()));
                if (!Channel.TS_GOTO_Label(function_name))
                    throw new AxisException(this, String.Format("Could not goto label {0}: {1}", function_name, GetTSError()));
            }
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// blocking call that waits for func_done variable to be non-zero after firing off a TS_GOTO_Label command
        /// </summary>
        /// <param name="function_name"></param>
        /// <param name="ts_timeout"></param>
        public override void GotoFunctionAndWaitForDone(string function_name, TimeSpan ts_timeout) 
        {
            DateTime datetime_start_func = DateTime.Now;

            _log.DebugFormat( "GotoFunctionAndWaitForDone({0}) called.", function_name);

            // DKM 2011-10-22 I changed this back to 0 because it would mess with the loop logic later on if I used -99
            SetLongVariable("func_done", 0);
            GotoFunction(function_name);
            Int32 func_done = 0;
            while (DateTime.Now - datetime_start_func <= ts_timeout)
            {
                GetLongVariable("func_done", out func_done);
                if (0 != func_done)
                {
                    break;
                }
                Thread.Sleep(25); // 25ms seems like a good amount of time to sleep for this kind of polling
            }

            var ElapsedTime = DateTime.Now - datetime_start_func;

            _log.DebugFormat( "GotoFunctionAndWaitForDone({0}) returned func_done=={1} in {2:0} ms", 
                function_name, func_done, ElapsedTime.TotalMilliseconds);

            if (1 != func_done)
            {
                throw new AxisException(this, String.Format("TS_GOTO_Label({0}) timed out after {1:0.000} secs with func_done=={2}",
                    function_name, ElapsedTime.TotalSeconds, func_done));
            }
        }
        //---------------------------------------------------------------------

        public override void ZeroIqref()
        {
            lock( _lock) {
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format( "Could not select axis {0}: {1}", _axis_id, GetTSError()));
                // DKM 2011-06-04 Dave and Ben saw bad things happen with this code that calls the zero_iqref function.  Replacing
                //                the call with Mark's TS_Execute code instead.
                // DKM 2011-06-05 I messed up and forgot the lock -- that's why this didn't work.
                /*
                if( !TMLLib.TS_CALL_Label( "func_zero_iqref")) {
                    throw new AxisException( this, String.Format( "Could not zero IQREF on axis {0}: {1}", _axis_id, GetTSError()));
                }
                 */
                if (!Channel.TS_Execute("STA; var_i1 = 0x215; (var_i1), dm = 0L; var_i1 = 0x217; (var_i1), dm = 0;"))
                {
                    throw new AxisException( this, String.Format( "Could not execute zero IQREF code on axis {0}: {1}", _axis_id, GetTSError()));
                }
            }
        }

        public override void SendTmlCommands( string commands)
        {
            lock( _lock) {
                if (!Channel.TS_SelectAxis(_axis_id))
                    throw new AxisException( this, String.Format( "Could not select axis {0}: {1}", _axis_id, GetTSError()));
                if( !Channel.TS_Execute( commands))
                    throw new AxisException( this, String.Format( "Could not execute command string"));
            }
        }
    }
}