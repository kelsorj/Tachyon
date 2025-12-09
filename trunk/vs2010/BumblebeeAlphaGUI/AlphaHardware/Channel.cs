using System;
using System.Diagnostics;
using System.Threading;
using BioNex.Shared.LabwareDatabase;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.TechnosoftLibrary;
using log4net;

namespace BioNex.BumblebeePlugin.Hardware
{
    public class Channel : HardwareQuantum
    {
        // ----------------------------------------------------------------------
        // enumerations.
        // ----------------------------------------------------------------------
        public enum ChannelStatus
        {
            NoTip,
            PressingTip,
            CleanTip,
            UsingTip,
            DirtyTip,
            ReadyToShuckTip,
            ShuckingTipInQueue,
            ShuckingTipInAction,
            Disabled,
            Error,
        }

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public IAxis XAxis { get; private set; }
        public IAxis ZAxis { get; private set; }
        public WAxisWrapper WAxis { get; private set; }

        public bool Available { get; private set; }

        public ChannelStatus Status { get; set; }
        public TipWell TipWell { get; set; }

        // ----------------------------------------------------------------------
        // class members.
        // ----------------------------------------------------------------------
        private static SemaphoreSlim AllowXToHome = new SemaphoreSlim( 2);

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public Channel( byte id, byte x_axis_id, byte z_axis_id, byte w_axis_id, double w_shuck_offset_mm, bool available = true)
            : this( id, new NonExistentAxis( x_axis_id), new NonExistentAxis( z_axis_id), new NonExistentAxis( w_axis_id), w_shuck_offset_mm, available)
        {
        }
        // ----------------------------------------------------------------------
        public Channel( byte id, IAxis x_axis, IAxis z_axis, IAxis w_axis, double w_shuck_offset_mm, bool available = true)
            : base( id)
        {
            AddAxis( "X", XAxis = x_axis);
            AddAxis( "Z", ZAxis = z_axis);
            AddAxis( "W", w_axis);
            WAxis = new WAxisWrapper( w_axis, w_shuck_offset_mm);
            Available = available; 
#warning Disposable tip scheduler assumes dirty tips on channels.  First action is to shuck.  Washable tip scheduler assumes no tips on channel.  First action is to press.  Should we still shuck off dirty tips to trash?  This would mean supporting two different shucks.
            // Status = ChannelStatus.DirtyTip;
            Status = ChannelStatus.NoTip;
            TipWell = null;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public void HomeWZ()
        {
            Home( new string[]{ "W", "Z"});
        }
        // ----------------------------------------------------------------------
        public void HomeX()
        {
            try{
                AllowXToHome.Wait();
                XAxis.Home( true);
            } finally{
                AllowXToHome.Release();
            }
        }
        // ----------------------------------------------------------------------
        public double GetAbsoluteZ( double z_origin, double z_offset)
        {
            return z_origin + ( ZAxis.Settings.FlipAxisDirection ? z_offset : -z_offset);
        }
        // ----------------------------------------------------------------------
        public void MoveAbsoluteZOffset( double z_origin, double z_offset, bool wait_for_motion_to_complete, bool use_trap)
        {
            double velocity = ZAxis.Settings.Velocity;
            MoveAbsoluteZOffset( z_origin, z_offset, velocity, true, wait_for_motion_to_complete, use_trap);
        }
        // ----------------------------------------------------------------------
        public void MoveAbsoluteZOffset( double z_origin, double z_offset, double velocity, bool implement_velocity_ceiling, bool wait_for_move_to_complete, bool use_trap)
        {
            if( implement_velocity_ceiling){
                double velocity_ceiling = ZAxis.Settings.Velocity;
                if(( velocity > velocity_ceiling) || ( Double.IsNaN( velocity))){
                    velocity = velocity_ceiling;
                }
            }
            ZAxis.MoveAbsolute( GetAbsoluteZ( z_origin, z_offset), velocity, wait_for_move_complete: wait_for_move_to_complete, use_trap: use_trap);
        }
        // ----------------------------------------------------------------------
        public void MoveWToAbsoluteUl( double w_position_ul, bool wait_for_move_complete)
        {
            // temporary hack to "restore" maximum speed of w-axis:  MoveAbsolute expects w_axis values in ul, but w_axis settings are in mm, so convert w_axis velocity from mm to ul and pass to MoveAbsolute.
            double max_w_velocity = WAxis.ConvertMmToUl(WAxis.Settings.Velocity);
            WAxis.MoveAbsolute( w_position_ul, max_w_velocity, wait_for_move_complete: wait_for_move_complete);
        }
        // ----------------------------------------------------------------------
        public void AspirateOrDispenseNew( bool aspirate_true_or_dispense_false, double w_position_dst/* absolute position */, double z_position_dst/* absolute positions */, double w_position_src, double z_position_src, double w_velocity, double w_accel_factor, double z_origin, bool use_theoretical_positions, bool ignore_motion_parameter_checking = false)
        {
            double z_final_position = 0.0;
            double z_velocity = 0.0;
            double w_acceleration = 0.0;
            double z_acceleration = 0.0;

            // get w axis and its motor settings.
            MotorSettings w_axis_motor_settings = WAxis.Settings;

            // calculate w-axis distance in mm.
            // the following values are all in ul.
            double w_current_position = WAxis.GetPositionUl(); // <-- remove this line later.  trying to understand where W actually is regardless of whether or not we're using theoretical positions.
            w_current_position = ( use_theoretical_positions ? w_position_src : WAxis.GetPositionUl());
            double w_desired_position = w_position_dst;
            double w_distance = Math.Abs( w_desired_position - w_current_position);
            // the previous values are all in ul.
            double w_distance_mm = WAxis.ConvertUlToMm( w_distance);

            // get z axis and its motor settings.
            MotorSettings z_axis_motor_settings = ZAxis.Settings;

            // calcualte z-axis distance (obviously in mm).
            double z_current_position = ZAxis.GetPositionMM(); // <-- remove this line later.  trying to understand where Z actually is regardless of whether or not we're using theoretical positions.
            z_current_position = ( use_theoretical_positions ? z_origin + ( z_axis_motor_settings.FlipAxisDirection ? z_position_src : -z_position_src) : ZAxis.GetPositionMM());
            double z_desired_position = GetAbsoluteZ( z_origin, z_position_dst);
            double z_distance = Math.Abs( z_desired_position - z_current_position);

            // calculate z:w ratio.
            double z_w_ratio = z_distance / w_distance_mm;

            // final position corrected for axis flipping.
            z_final_position = z_desired_position;
            // z velocity in mm is ratio of w velocity after conversion to ul.
            z_velocity = WAxis.ConvertUlToMm( w_velocity) * z_w_ratio;
            // w acceleration must be scaled down by accel factor.
            w_acceleration = w_accel_factor * w_axis_motor_settings.Acceleration;
            // z acceleration is ratio of w acceleration (which is already in mm).
            z_acceleration = w_acceleration * z_w_ratio;

            // DKM 2010-10-08 we had issues with Pioneer1 when commanding a Z=0 move.  Basically, the scurve move would complete,
            //                and when the W axes aspirate, one of the Z axes would do some obscenely large move.  Sometimes, it
            //                would correct itself and bounce back to the right position, while other times it would time out, and
            //                the recommanded move would correct the situation.  By not commanding Z to move AT ALL if the z_distance
            //                is 0, we avoid this issue.

            // not sure if this is the right way to do it, but I am trying to avoid making the motion parameter
            // checking ignoring less intrusive in the Axis code.  So I am instead going to allow the developer
            // to modify a property of Axis to set the ignore state. Axis should observe the setting, and reset as necessary.
            // I only want the ignore flag to be active for one move at a time.  It's up to the state machine
            // to determine whether or not the flag should be set.
            WAxis.IgnoreMotionParameterChecking = ignore_motion_parameter_checking;
            ZAxis.IgnoreMotionParameterChecking = ignore_motion_parameter_checking;

            // DKM 2010-10-08
            const bool ignore_z0_moves = true;
            
            ILog _log = LogManager.GetLogger("axis_" + WAxis.GetID().ToString());
            _log.DebugFormat( "{0} START", aspirate_true_or_dispense_false ? "ASPIRATE" : "DISPENSE");
            // DKM 2010-10-08 this is a test -- if we know we don't want to move Z at all, just move W instead
            if( z_distance > 0 || !ignore_z0_moves){
                MoveAbsoluteWZ( w_position_dst, z_final_position, w_velocity, z_velocity, w_acceleration, z_acceleration, false);
                MoveAbsoluteWZ( w_position_dst, z_final_position, w_velocity, z_velocity, w_acceleration, z_acceleration, true);
            } else{
                WAxis.MoveAbsolute( w_position_dst, w_velocity, w_acceleration, wait_for_move_complete: false, use_trap: true);
                WAxis.MoveAbsolute( w_position_dst, w_velocity, w_acceleration, use_trap: true);
            }
            // DKM 2010-10-08 this is a test -- if we know we don't want to move Z at all, just move W instead
            _log.DebugFormat( "{0} END", aspirate_true_or_dispense_false ? "ASPIRATE" : "DISPENSE");
        }
        // ----------------------------------------------------------------------
        public bool VerifyXPosition( double x_position, double allowed_delta)
        {
            double x_actual_position = XAxis.GetPositionMM();
            double x_target_position = x_position;
            return Math.Abs( x_actual_position - x_target_position) <= allowed_delta;
        }
        // ----------------------------------------------------------------------
        public bool VerifyZPosition( double z_origin, double z_offset, double allowed_delta)
        {
            double z_actual_position = ZAxis.GetPositionMM();
            double z_target_position = GetAbsoluteZ( z_origin, z_offset);
            return Math.Abs( z_actual_position - z_target_position) <= allowed_delta;
        }
        // ----------------------------------------------------------------------
        public void ReturnHome()
        {
            ZAxis.MoveAbsolute( 0);
            XAxis.MoveAbsolute( 0, wait_for_move_complete: false);
        }
        // ----------------------------------------------------------------------
        public void MoveToTopLimit( bool use_trap = false, double acceleration = double.NaN, bool ignore_speed_limits = false)
        {
            MotorSettings ms = ZAxis.Settings;
            double z_target_position = ms.FlipAxisDirection ? ms.MaxLimit : ms.MinLimit;
            if( VerifyZPosition( z_target_position, 0.0, 0.5)){
                return;
            }
            double accel = !double.IsNaN( acceleration) ? acceleration : ms.Acceleration;
            ZAxis.MoveAbsolute( z_target_position, acceleration: accel, wait_for_move_complete: true, use_trap: use_trap, ignore_motion_parameters: ignore_speed_limits);
            // DKM 2011-06-04 I think this is the best place to call zero_iqref
            // DKM 2011-06-06 instead of calling zeroiqref, jog up 0.1mm
            // GetZ().ZeroIqref();
            // MoveRelativeUp( -0.1);
            ZAxis.MoveAbsolute( ms.FlipAxisDirection ? ms.MaxLimit - 0.1 : ms.MinLimit + 0.1, wait_for_move_complete: true, use_trap: true);
        }
        // ----------------------------------------------------------------------
        public void MoveRelativeUpFrom( double absolute_start_pos_mm, double increment_mm)
        {
            if (!ZAxis.Settings.FlipAxisDirection)
                increment_mm = -increment_mm;
            ZAxis.MoveAbsolute( absolute_start_pos_mm + increment_mm);
        }
        // ----------------------------------------------------------------------
        public void MoveAbsoluteBlendedXZOffset( double x_pos, double z_origin, double z_offset, bool x_use_trap, bool z_use_trap)
        {
            // loop over the axes in the group and send the motion parameters
            XAxis.SetupBlendedMove( x_pos, false, x_use_trap);
            ZAxis.SetupBlendedMove( GetAbsoluteZ( z_origin, z_offset), true, z_use_trap);
            // tell the master axis to go
            XAxis.StartBlendedMove();
        }
        // ----------------------------------------------------------------------
        public void WaitForMoveAbsoluteBlendedComplete()
        {
            //! \todo need to have xz_blended_move_complete on controller and query it here
            ZAxis.WaitForBlendedMoveComplete( XAxis);
        }
        // ----------------------------------------------------------------------
        public void MoveAbsoluteWZ( double w_pos, double z_pos, double w_velocity, double z_velocity, double w_accel, double z_accel, bool wait_for_move_complete)
        {
            MotorSettings wms = WAxis.Settings;
            DateTime start = DateTime.Now;
            if( !wait_for_move_complete){
                //_w.StartLogging();
                Log.DebugFormat( "Non-blocking liquid transfer W{0} axis started at {1:0.000}mm", ID, WAxis.GetPositionMM());
            }
            WAxis.MoveAbsolute( w_pos, w_velocity, w_accel, wms.Jerk, wait_for_move_complete, wms.MoveDoneWindow, wms.SettlingTimeMS, true);
            // we only want to log on move complete = true otherwise the timing data is going to be wrong
            if( wait_for_move_complete){
                Log.DebugFormat( "Liquid transfer W{0} axis move took {1:0.000}ms(ignore?) at {2:0.000}mm", ID, (DateTime.Now - start).TotalMilliseconds, WAxis.GetPositionMM());
                //_w.WaitForLoggingComplete( String.Format( @"c:\{0}_aspirate_pos.txt", _w.GetID()));
            }
            ZAxis.MoveAbsolute( z_pos, z_velocity, z_accel, wait_for_move_complete: wait_for_move_complete, use_trap: true);
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Performs a torque-limited or position-based tip press operation at the exact channel location, depending upon the position_press parameter.
        /// It is assumed that the stage is also in the right spot.
        /// </summary>
        /// <param name="tip"></param>
        /// <param name="z_teachpoint"></param>
        /// <param name="z_offset">Offset at which channel is ideally pressed into tip.</param>
        /// <param name="position_press">true to use the teachpoint for pressing, false to use torque pressing</param>
        /// <returns>
        /// Z position where the barrel bottoms out in the tip
        /// </returns>
        public double TipOnHere( ILabware tip, double z_teachpoint, double z_offset, double additional_push, bool position_press)
        {
            TipBox tipbox = tip as TipBox;
            Debug.Assert( tipbox != null, "You cannot press on tips using a non-tipbox labware");
            // double ideal_seal_offset_from_tp_mm = tipbox.TipProperties.SealOffset; // 10.6 seems good for Pioneer1 & Pilot as of 2010-09-29 (11mm == theortical value)
            // ideal_seal_offset_from_tp_mm = ZAxis.Settings.FlipAxisDirection ? ideal_seal_offset_from_tp_mm : -ideal_seal_offset_from_tp_mm;
            // double ideal_seal_offset_from_tp_mm = - 10.0 /* tip seating in carrier */ /* 46.2 /* tip box thickness */ - ( 57.47 - 22.76) /* total tip length - collar */ - 1.0 /* extra push */;
            double ideal_seal_offset_from_tp_mm = z_offset - additional_push /* extra push */;
            double press_position_mm = z_teachpoint + ideal_seal_offset_from_tp_mm;
            double z_pos_before_press_mm = ZAxis.GetPositionMM();
            double z_pos_after_press_mm = 0.0;
            bool press_success;

            if( !position_press){
                press_success = true; // assume it was good unless we prove it was bad.
                // do a torque limited press
                // to add to labware database for tipboxes
                // actually, this shouldn't be stored in the database, it should be an offset relative
                // to the teachpoint, so that if the user reteaches the stage, he won't have to reteach
                // all of his tipboxes!
                //double nominal_position_mm = 55; // 200uL Rainin tip
                double torque_move_dest_mm = press_position_mm + (ZAxis.Settings.FlipAxisDirection ? -2.0 : 2.0);
                const double allowable_position_window_mm = 1.8;
                // figure out how much current should be used to press the tip on with the expected force
                //! \todo change 0 to a real force value (in pounds) when the equation is implemented.
                //double tip_press_current = GetZ().Settings.GetCurrentFromForce( 0);
                const double tip_press_current = 4.3; // this should be slightly lower than the firmware set peak current of 4.5 Amps
                // DKM 05-11-2010 velocity was 366
                ZAxis.MoveAbsoluteTorqueLimited( torque_move_dest_mm, /* destination position */
                                                  20.0, /* velocity mm/s */
                                                  500.0, /* accel mm/s/s */ 
                                                  5, /* jerk (iu) */ 
                                                  1, /* min_jerk (iu) */ 
                                                  500, /* settling_time_ms */ 
                                                  tip_press_current, /* current (A) */ 
                                                  6.11, /* IMaxPS of controller [PIM2403=6.11, PIM3605=16.5] */ 
                                                  12.0); /* torque_limiting_window_rel_mm */
                z_pos_after_press_mm = ZAxis.GetPositionMM();
                //_log.DebugFormat( "Tip press torque_lim Axis {0} hit 0 velocity and stopped at {1:0.000} mm", _z.GetID(), z_after_press_mm);
                double flip_sign = ZAxis.Settings.FlipAxisDirection ? -1.0 : 1.0;
                if(( z_pos_after_press_mm - press_position_mm) * flip_sign > allowable_position_window_mm ){
                    press_success = false;
                    Log.DebugFormat( "Tip press torque_lim Axis {0} FAIL: NO TIP PRESENT", ZAxis.GetID());
                    // throw new NoTipPresentException( z_pos, nominal_position_mm, allowable_position_window_mm);
                } else if(( z_pos_after_press_mm - press_position_mm) * flip_sign < -allowable_position_window_mm ){
                    press_success = false;
                    Log.DebugFormat( "Tip press torque_lim Axis {0} FAIL: WRECKED", ZAxis.GetID());
                    // throw new MissedTipException( z_pos, nominal_position_mm, allowable_position_window_mm);
                }
            } else{
                press_success = false; // assume it was bad unless we prove it was good.
                // do a position limited press
                /*
                // for now, skip torque move and do a slow position move to 59mm
                double press_position = 59.2;
                press_position = _z.Settings.FlipAxisDirection ? -press_position : press_position;
                // DKM 2010-08-11 had to change max accel because it was higher than that specified in motor_settings.xml
                //_z.MoveAbsolute( press_position, 20, 732, 10, retract_after_press, 0.2, 50);
                _z.MoveAbsolute( press_position, 20, 500, 10, retract_after_press, 0.2, 50);
                if( retract_after_press)
                    _z.MoveAbsolute( 0, false);
                */
                // DKM 2010-08-20 Reed realized that this approach is flawed -- we can't move all tips to the same
                //                absolute position, because they all have slightly different Z heights due to
                //                tolerances.  We have to figure out what the offset is from the teachpoint to
                //                the position where we get a good seal
                // DKM 2010-08-27 6.2mm is the offset used for the eraser test -- 10.75 is for real tips!!!
                int num_tries = 2; // try to press this many times before giving up
                while( !press_success && --num_tries >= 0){
                    try{
                        // DKM 2012-04-30 refs #562 we used to hardcode accel to 500mm/s.  However, in a past service case at Pioneer, Reed
                        //                had changed the accel limits to be very low.  This caused the press to fail because of out-of-range
                        //                values.  Now let's try to use 20mm/s and 500mm/s/s, but cap it at the axis limits, and warn the user
                        //                via a log message if we wanted to operate out of range and got clipped.
                        double desired_v = 20.0;
                        double desired_a = 500.0;
                        double max_v = ZAxis.Settings.Velocity;
                        double max_a = ZAxis.Settings.Acceleration;
                        if( max_v < desired_v) {
                            Log.WarnFormat( "Tip pressing would like to use a velocity of {0}mm/s, but it was clipped at {1}mm/s because of the limits set in motor_settings.xml", desired_v, max_v);
                            desired_v = max_v;
                        }
                        if( max_a < desired_a) {
                            Log.WarnFormat( "Tip pressing would like to use an acceleration of {0}mm/s^2, but it was clipped at {1}mm/s^2 because of the limits set in motor_settings.xml", desired_a, max_a);
                            desired_a = max_a;
                        }
                        ZAxis.MoveAbsolute( press_position_mm /* mm */,
                                            desired_v /* mm/s velocity */,
                                            desired_a /* mm/s/s accel */,
                                            1 /* jerk in IU */,
                                            move_done_window_mm: 1.300,
                                            settling_time_ms: 50,
                                            use_trap: true);
                        press_success = true;
                        Thread.Sleep(50); // sleep an extra 50 ms to allow position controller extra time to really press tip on with lots of force
                    } catch( AxisException){
                        press_success = false;
                        z_pos_after_press_mm = ZAxis.GetPositionMM();
                        Log.DebugFormat( "axis_{3} - Tip press commanded position={0:0.000}mm, actual position={1:0.000}mm, Actual-Cmd={2:0.000}mm", press_position_mm, z_pos_after_press_mm, z_pos_after_press_mm - press_position_mm, ZAxis.GetID());
                        Log.DebugFormat( "axis_{0} - Tip press failed. PosError = {1:0.000}", ZAxis.GetID(),  z_pos_after_press_mm - press_position_mm);
                        if( num_tries > 0){
                            Log.DebugFormat( "axis_{0} - Tip press retrying.", ZAxis.GetID());
                            ZAxis.MoveAbsolute( z_pos_before_press_mm);
                        }
                    }
                }
                if( !press_success){
                    z_pos_after_press_mm = ZAxis.GetPositionMM();
                    throw new AxisException( ZAxis, String.Format( "Failed to press tip on after multiple tries. Last error: {0:0.000}", z_pos_after_press_mm - press_position_mm));
                }
            } // else (do a position terminated press)

            z_pos_after_press_mm = ZAxis.GetPositionMM();
            Int16 IQ; // current through motor winding in IU
            const double Kif = 65472 / 2 / 6.11; // (bits/Amps) Formula from Page 866 of MackDaddyTechnosoftDoc (assuming 2403 controller)
            ZAxis.GetIntVariable("IQ", out IQ);
            double IQ_amps = (double)IQ / Kif;
            Log.DebugFormat( "axis_{3} - Tip press {5} commanded position={0:0.000}mm, actual position={1:0.000}mm, Actual-Cmd={2:0.000}mm, IQ = {4:0.000} A, {6}",
                              press_position_mm, z_pos_after_press_mm, z_pos_after_press_mm - press_position_mm, ZAxis.GetID(), IQ_amps, press_success ? "SUCCESS" : "FAILED", 
                              position_press ? "position_press" : "torque_lim");

            return z_pos_after_press_mm;
        }
        // ----------------------------------------------------------------------
        public void EEPROMreadSN()
        {
            UInt64 buffer64;
            WAxis.ReadExtI2CPage(32, out buffer64);
            Log.InfoFormat( "W Axis on channel {0} has Syringe ID Board with S/N {1:x12}", (int)(WAxis.GetID()/10), buffer64 & 0xffffffffffff);
        }
        // ----------------------------------------------------------------------
        public override string ToString()
        {
            return String.Format( "Channel {0}", ID);
        }
    }

    public class WAxisWrapper
    {
        private IAxis WAxis { get; set; }
        public bool IgnoreMotionParameterChecking{
            get { return WAxis.IgnoreMotionParameterChecking; }
            set { WAxis.IgnoreMotionParameterChecking = value; }
        }

        public WAxisWrapper( IAxis w_axis, double w_shuck_offset_mm)
        {
            WAxis = w_axis;
            WOffsetUl = WAxis.ConvertMmToUl( w_shuck_offset_mm);
        }

        // pass-through functions.
        public byte GetID() { return WAxis.GetID(); }
        public string Name { get { return WAxis.Name; } }
        public bool IsOn() { return WAxis.IsOn(); }
        public bool IsHomed { get { return WAxis.IsHomed; } }
        public void Enable( bool enable, bool blocking) { WAxis.Enable( enable, blocking); }
        public void ResetPause() { WAxis.ResetPause(); }
        public MotorSettings Settings { get { return WAxis.Settings; } }
        public void Home( bool wait_for_complete) { WAxis.Home( wait_for_complete); }
        public void ReadExtI2CPage( byte page, out ulong data) { WAxis.ReadExtI2CPage( page, out data); }

        // pass-through functions to use with careful discretion.
        public void MoveRelative( double w_displacement) { WAxis.MoveRelative( w_displacement); }
        public double GetPositionMM() { return WAxis.GetPositionMM(); }
        public double ConvertMmToUl( double mm) { return WAxis.ConvertMmToUl( mm); }
        public double ConvertUlToMm( double ul) { return WAxis.ConvertUlToMm( ul); }
        public IAxis UseSparingly() { return WAxis; }

        // indirection implemented to achieve this offset.
        private double WOffsetUl { get; set; }
        public double GetPositionUl() { return WAxis.GetPositionUl() - WOffsetUl; }
        public void MoveToZero( bool wait_for_move_complete = true)
        {
            WAxis.MoveAbsolute( 0.0, wait_for_move_complete: wait_for_move_complete);
        }
        public void MoveAbsolute( double w_position, double w_velocity = double.NaN, double w_acceleration = double.NaN, int w_jerk = int.MinValue, bool wait_for_move_complete = true, double move_done_window_mm = double.NaN, short settling_time_ms = short.MinValue, bool use_trap = false)
        {
            WAxis.MoveAbsolute( w_position + WOffsetUl, w_velocity, w_acceleration, w_jerk, wait_for_move_complete: wait_for_move_complete, move_done_window_mm: move_done_window_mm, settling_time_ms: settling_time_ms, use_trap: use_trap);
        }
    }
}
