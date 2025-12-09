using System;
using System.Collections.Generic;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.Utils.Kinematics;
using log4net;

namespace BioNex.Hive.Hardware
{
    public class BlendingConstants
    {
        public static readonly double SafeZoneCornerBlend = 10.0;
        public static readonly double GripperEmptyCornerBlend = 1.0;
        public static readonly double GripperFullCornerBlend = 0.25;
        public static readonly double UngripBlend = 0.5;
        public static readonly double GripBlend = 0.0;
    }

    public class HiveMultiAxisTrajectory : MultiAxisTrajectory
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        private IAxis XAxis { get; set; }
        private IAxis ZAxis { get; set; }
        private IAxis TAxis { get; set; }
        private IAxis GAxis { get; set; }

        private double ArmLength { get; set; }
        private double FingerOffset { get; set; }

        private static readonly ILog Log = LogManager.GetLogger( typeof( HiveMultiAxisTrajectory));

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public HiveMultiAxisTrajectory( HiveHardware hardware, IDictionary< IAxis, double> initial_point, IDictionary< IAxis, double> move_done_window)
            : base( initial_point, move_done_window, 0.010)
        {
            XAxis = hardware.XAxis;
            ZAxis = hardware.ZAxis;
            TAxis = hardware.TAxis;
            GAxis = hardware.GAxis;
            ArmLength = hardware.Config.ArmLength;
            FingerOffset = hardware.Config.FingerOffsetZ;
        }

        // ----------------------------------------------------------------------
        // public methods.
        // ----------------------------------------------------------------------
        public void AddWaypoint( int marker,
                                 double dst_x = double.NaN, double dst_z = double.NaN, double dst_t = double.NaN, double dst_g = double.NaN, double dst_y = double.NaN,
                                 double vel_x = double.NaN, double vel_z = double.NaN, double vel_t = double.NaN, double vel_g = double.NaN,
                                 double acc_x = double.NaN, double acc_z = double.NaN, double acc_t = double.NaN, double acc_g = double.NaN,
                                 double pre_blend_distance = 0.0, double post_blend_distance = 0.0)
        {
            // if blending is disabled, then force pre_motion_blend_mm and post_motion_blend_mm to zero.
            const bool blending_disabled = false; // <-- we can make this a member variable if necessary.
            if( blending_disabled){
                pre_blend_distance = 0.0;
                post_blend_distance = 0.0;
            }

            try{
                // if dst_y is a number...
                if( !double.IsNaN( dst_y)){
                    // make sure dst_z and dst_t aren't numbers.
                    if( !double.IsNaN( dst_z) || !double.IsNaN( dst_t)){
                        throw new Exception();
                    }
                    // additionally, for now, don't allow dst_x and dst_g to be numbers.
                    if( !double.IsNaN( dst_x) || !double.IsNaN( dst_g)){
                        throw new Exception();
                    }
                    // if passed-in velocities/accelerations are double.NaN, then the client likely didn't specify them. in these cases, use the axis' default settings.
                    // if passed-in velocities/accelerations are valid numbers, then make sure they are in the range [1.0, default_value].
                    // !!assuming minimum velocity of 1.0mm/s or 1.0°/s!!
                    vel_t = double.IsNaN(vel_t) ? TAxis.Settings.Velocity : Math.Min(TAxis.Settings.Velocity, Math.Max(1.0, vel_t));
                    acc_t = double.IsNaN(acc_t) ? TAxis.Settings.Acceleration : Math.Min(TAxis.Settings.Acceleration, Math.Max(1.0, acc_t));
                    // convert current waypoint to tool coordinates.
                    Tuple< double, double> current_yz_tool = HiveMath.ConvertTZWorldToYZTool( ArmLength, FingerOffset, CurrentWaypoint[ TAxis], CurrentWaypoint[ ZAxis]);
                    double current_y_tool = current_yz_tool.Item1;
                    double current_z_tool = current_yz_tool.Item2;
                    // compute destination in world coordinates given dst_y (a tool coordinate).
                    dst_t = HiveMath.GetThetaFromY( ArmLength, dst_y);
                    dst_z = HiveMath.ConvertZToolToWorldUsingTheta( ArmLength, FingerOffset, current_z_tool, dst_t);
                    // calculate quickest s-curve trajectory for theta axis.
                    SCurve t_curve = new SCurve(dst_t, CurrentWaypoint[TAxis], vel_t, acc_t, TAxis.Settings.GetJerkRate());
                    // scale duration by speed factor and round up to the nearest time quantum; this becomes the move duration.
                    double move_duration = TimeQuantumMath.Ceiling( t_curve.GetDuration() / ( 1.25 * TAxis.GetSpeedFactor()));
                    // scale theta-axis trajectory to the calculated move duration.
                    t_curve.ScaleDuration( move_duration);
                    // calculate corresponding z-axis trajectory to follow t-axis with a sine wave.
                    FollowLineWithSine z_curve = new FollowLineWithSine( t_curve, current_z_tool, ArmLength);
                    // calculate magnitude and direction of theta-axis displacement from current waypoint to given waypoint.
                    double magnitude = Math.Abs( dst_t - CurrentWaypoint[ TAxis]);
                    int direction = Math.Sign( dst_t - CurrentWaypoint[ TAxis]);
                    // if pre-blend distance is less than or equal to zero, then pre-blend time is zero.
                    // if pre-blend distance is greater than or equal to magnitude of move, then pre-blend time is the entire move's duration.
                    // else pre-blend time is the time required for the trajectory to reach the pre-blend distance.
                    double pre_blend_time = TimeQuantumMath.Floor( pre_blend_distance <= 0.0 ? 0.0
                                                                                             : ( pre_blend_distance >= magnitude ? move_duration
                                                                                                                                 : t_curve.GetTimeOfPosition( 180.0 * Math.Asin( ( current_y_tool + direction * pre_blend_distance) / ArmLength) / Math.PI)));
                    // corresponding logic for post-blend time.
                    double post_blend_time = TimeQuantumMath.Floor( post_blend_distance <= 0.0 ? 0.0
                                                                                               : ( post_blend_distance >= magnitude ? move_duration
                                                                                                                                    : move_duration - t_curve.GetTimeOfPosition( 180.0 * Math.Asin( ( dst_y - direction * post_blend_distance) / ArmLength) / Math.PI)));
                    // start the move at the later of (1) the separator time or (2) pre-blend time earlier than the current blend time.
                    double start_time = Math.Max( SeparatorTime, TimeQuantumMath.Round( CurrentBlendTime - pre_blend_time));
                    // log results of blending.
                    // (negative time saved pre-blending result when separators prevent pre-blending from occurring.)
                    Log.DebugFormat( "On Marker {0}: saved {1:0.000}s by pre-blending and {2:0.000}s by post-blending a {3:0.000}s move to start at {4:0.000}s", marker, CurrentBlendTime - start_time, post_blend_time, move_duration, start_time);
                    // add t_curve and z_curve to the list of applied trajectories.
                    AppliedTrajectories.Add( new AppliedTrajectory( t_curve, TAxis, start_time, marker));
                    AppliedTrajectories.Add( new AppliedTrajectory( z_curve, ZAxis, start_time, marker));
                    // update the current waypoint to the given waypoint.
                    CurrentWaypoint[ TAxis] = dst_t;
                    CurrentWaypoint[ ZAxis] = dst_z;
                    // update the current blend time to the point in time to start the next move (exclusive of the next move's pre-move blend);
                    // so current blend time should be this move's finish time (exclusive of this move's post-move blend).
                    CurrentBlendTime = TimeQuantumMath.Round( start_time + move_duration - post_blend_time);
                    // separator time ensures that the move after this one won't blend into the move before this one.
                    // e.g., when adding waypoints A, B, and C, don't let C's move overlap with A's move whatsoever.
                    SeparatorTime = NextSeparatorTime;
                    // next separator time remembers when this move ends (INCLUSIVE of this move's post-move blend) so that two waypoints later we can avoid overlapping moves separated by one or more moves in between.
                    NextSeparatorTime = TimeQuantumMath.Round( start_time + move_duration);
                    // keep track of this multi-axis trajectory's non-blended duration just for kicks.
                    NonBlendedDuration += move_duration;
                    return;
                }

                dst_z = HiveMath.ConvertZToolToWorldUsingTheta(ArmLength, FingerOffset, dst_z, double.IsNaN(dst_t) ? CurrentWaypoint[TAxis] : dst_t);

                IList< WaycoordinateInfo> motion_infos = new List< WaycoordinateInfo>();
                if( !double.IsNaN( dst_x)){
                    motion_infos.Add( new WaycoordinateInfo( XAxis, dst_x, vel_x, acc_x));
                }
                if( !double.IsNaN( dst_z)){
                    motion_infos.Add( new WaycoordinateInfo( ZAxis, dst_z, vel_z, acc_z));
                }
                if( !double.IsNaN( dst_t)){
                    motion_infos.Add( new WaycoordinateInfo( TAxis, dst_t, vel_t, acc_t));
                }
                if( !double.IsNaN( dst_g)){
                    motion_infos.Add( new WaycoordinateInfo( GAxis, dst_g, vel_g, acc_g));
                }
                AddWaypoint( marker, motion_infos, pre_blend_distance, post_blend_distance);
            } catch( Exception ex){
                throw ex;
            }
        }
    }
}
