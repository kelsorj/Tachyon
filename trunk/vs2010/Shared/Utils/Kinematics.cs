using System;
using System.Collections.Generic;
using System.Linq;

namespace BioNex.Shared.Utils.Kinematics
{
    /// <summary>
    /// Interface for a single-axis trajectory describing an object's motion in one dimension.
    /// </summary>
    public interface ITrajectory
    {
        // ----------------------------------------------------------------------
        /// <summary>
        /// Get the duration of the trajectory.
        /// </summary>
        /// <returns>Duration of the trajectory in time units.</returns>
        double GetDuration();
        // ----------------------------------------------------------------------
        /// <summary>
        /// Get the position of the object at a given time during the trajectory.
        /// </summary>
        /// <param name="time">The given time.</param>
        /// <returns>The position of the object.</returns>
        double GetPosition( double time);
        // ----------------------------------------------------------------------
        /// <summary>
        /// Get the velocity of the object at a given time during the trajectory.
        /// </summary>
        /// <param name="time">The given time.</param>
        /// <returns>The velocity of the object.</returns>
        double GetVelocity( double time);
        // ----------------------------------------------------------------------
        /// <summary>
        /// Get the acceleration of the object at a given time during the trajectory.
        /// </summary>
        /// <param name="time">The given time.</param>
        /// <returns>The acceleration of the object.</returns>
        double GetAcceleration( double time);
        // ----------------------------------------------------------------------
        /// <summary>
        /// Scale the entire trajectory by the given speed factor.
        /// Velocities are increased by the given speed factor.
        /// Accelerations are increased by the square and jerks are increased by the cube of the given speed factor.
        /// Duration (times) are decreased by the given speed factor.
        /// Positions remain unchanged.
        /// </summary>
        /// <param name="speed_factor">The given speed factor.</param>
        void ScaleTrajectory( double speed_factor);
        // ----------------------------------------------------------------------
        void ScaleDuration( double new_duration);
        // ----------------------------------------------------------------------
        IEnumerable< double> GetRelevantTimes();
        // ----------------------------------------------------------------------
    }

    /// <summary>
    /// Exception thrown by all methods within this namespace.
    /// </summary>
    public class KinematicsException : ApplicationException
    {
        public KinematicsException( string message)
            : base( message)
        {
        }
    }

    /// <summary>
    /// Class for a single-axis s-curve trajectory describing an object's motion in one dimension.
    /// </summary>
    public class SCurve : ITrajectory
    {
        // ----------------------------------------------------------------------
        // inner classes.
        // ----------------------------------------------------------------------
        /// <summary>
        /// Each s-curve trajectory consists of seven segments.
        /// Each segment lasts for a certain duration during which the jerk is held constant.
        /// Each segment has an initial time, position, velocity, and acceleration.
        /// As time progresses, the position, velocity, and acceleration of the object change due to the influence of the constant jerk.
        /// </summary>
        private class Segment
        {
            public double Duration { get; set; }
            public double Jerk { get; set; }
            public double Acc0 { get; set; }
            public double Vel0 { get; set; }
            public double Pos0 { get; set; }
            public double Time0 { get; set; }
            public double TimeF { get{ return Time0 + Duration; }}

            public Segment()
            {
            }

            public Segment( Segment original)
            {
                Duration = original.Duration;
                Jerk = original.Jerk;
                Acc0 = original.Acc0;
                Vel0 = original.Vel0;
                Pos0 = original.Pos0;
                Time0 = original.Time0;
            }
        }

        // ----------------------------------------------------------------------
        // members.
        // ----------------------------------------------------------------------
        /// <summary>
        /// The seven segments of the s-curve trajectory.
        /// segment[0] = period of constant jerk, raising the acceleration of the object.
        /// segment[1] = period of NO jerk but constant acceleration, raising the speed of the object.
        /// segment[2] = period of reverse jerk, lowering the acceleration of the object yet continuing to raise the speed of the object.
        /// segment[3] = period of NO jerk and NO acceleration, allowing object to cruise at constant speed.
        /// segment[4] = period of reverse jerk, raising the deceleration of the object.
        /// segment[5] = period of NO jerk but constant deceleration, lowering the speed of the object.
        /// segment[6] = period of constant jerk, lowering the deceleration of the object and continuing to lower the speed of the object until standstill.
        /// </summary>
        private readonly Segment[] segments = new Segment[ 7];

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public SCurve( double pos_dst, double pos_src, double max_speed, double max_scalar_accel, double scalar_jerk)
        {
            // make sure max speed, max scalar accel, and scalar jerk are positive numbers (i.e., not less than or equal to zero and not NaN).
            if( !( max_speed > 0.0)){
                throw new KinematicsException( String.Format( "Invalid max_speed {0}", max_speed));
            }
            if( !( max_scalar_accel > 0.0)){
                throw new KinematicsException( String.Format( "Invalid max_scalar_accel {0}", max_scalar_accel));
            }
            if( !( scalar_jerk > 0.0)){
                throw new KinematicsException( String.Format( "Invalid scalar_jerk {0}", scalar_jerk));
            }
            // distance traveled from pos_src to pos_dst.
            double distance = Math.Abs( pos_dst - pos_src);
            double direction = Math.Sign( pos_dst - pos_src);
            // jerk is high enough to pull acceleration up to max_accel before velocity reaches max_vel.
            bool trapezoidal_acceleration_possible = ( scalar_jerk > ( max_scalar_accel * max_scalar_accel) / max_speed);
            // the maximum distance that can be traveled while maintaining a pure-S profile (first/last quarter jerk = opposite of middle half jerk; never any non-zero jerk).
            double max_pure_s_distance = 0.0;
            if( trapezoidal_acceleration_possible){
                // if trapezoidal acceleration is possible, then this maximum distance is acceleration bound.
                max_pure_s_distance = ( 2.0 * ( max_scalar_accel * max_scalar_accel * max_scalar_accel) / ( scalar_jerk * scalar_jerk));
            } else{
                // if triangular accelerations only, then this maximum distance is velocity bound.
                max_pure_s_distance = ( 2.0 * ( Math.Sqrt( max_speed * max_speed * max_speed / scalar_jerk)));
            }
            // assuming a trapezoidal acceleration, the distance that is traveled in accelerating up to max_vel.
            double trapezoidal_acc_to_max_vel_distance = ( max_speed / 2.0) * ( ( max_speed / max_scalar_accel) + ( max_scalar_accel / scalar_jerk));
            // categorize the move as a 4, 5, 6, or 7 segment move.
            bool is_4_segment_move = ( distance <= max_pure_s_distance);
            bool is_5_segment_move = ( !is_4_segment_move && !trapezoidal_acceleration_possible);
            bool is_6_segment_move = ( !is_4_segment_move && !is_5_segment_move && ( distance <= ( 2.0 * trapezoidal_acc_to_max_vel_distance)));
            bool is_7_segment_move = ( !is_4_segment_move && !is_5_segment_move && !is_6_segment_move);
            // note: segment indices are 0-based.
            // segment 3 distance is the distance over which the move occurs at max_vel.
            double segment_3_distance = distance - ( is_5_segment_move ? max_pure_s_distance : ( is_7_segment_move ? 2.0 * trapezoidal_acc_to_max_vel_distance : distance));
            // initialize the segments with duration and jerk data.
            // seed segment 0 with initial acceleration, velocity, position, and time.
            segments[ 0] = new Segment{ Duration = ( is_4_segment_move ? Math.Pow( distance / ( 2.0 * scalar_jerk), 1.0 / 3.0) :
                                                   ( is_5_segment_move ? Math.Sqrt( max_speed / scalar_jerk) :
                                                     max_scalar_accel / scalar_jerk)),
                                        Jerk = scalar_jerk * direction,
                                        Acc0 = 0.0,
                                        Vel0 = 0.0,
                                        Pos0 = pos_src,
                                        Time0 = 0.0};
            segments[ 1] = new Segment{ Duration = ( is_6_segment_move ? ( Math.Sqrt( ( max_scalar_accel * distance) + ( max_scalar_accel * max_scalar_accel * max_scalar_accel * max_scalar_accel / ( 4.0 * scalar_jerk * scalar_jerk))) - (( 1.5 * max_scalar_accel * max_scalar_accel) / scalar_jerk)) / max_scalar_accel :
                                                   ( is_7_segment_move ? ( ( max_speed / max_scalar_accel) - ( max_scalar_accel / scalar_jerk)) :
                                                     0.0)),
                                        Jerk = 0.0};
            segments[ 2] = new Segment{ Duration = segments[ 0].Duration,
                                        Jerk = -segments[ 0].Jerk};
            segments[ 3] = new Segment{ Duration = segment_3_distance / max_speed,
                                        Jerk = 0.0};
            segments[ 4] = new Segment{ Duration = segments[ 0].Duration,
                                        Jerk = -segments[ 0].Jerk};
            segments[ 5] = new Segment{ Duration = segments[ 1].Duration,
                                        Jerk = 0.0};
            segments[ 6] = new Segment{ Duration = segments[ 0].Duration,
                                        Jerk = segments[ 0].Jerk};
            // compute all segment accelerations, velocities, positions, and times.
            for( int loop = 1; loop <= 6; ++loop){
                Segment current_segment = segments[ loop];
                Segment previous_segment = segments[ loop - 1];
                double previous_duration = previous_segment.Duration;
                current_segment.Acc0 = previous_segment.Acc0 + previous_segment.Jerk * previous_duration;
                current_segment.Vel0 = previous_segment.Vel0 + previous_segment.Acc0 * previous_duration + ( previous_segment.Jerk * previous_duration * previous_duration) / 2.0;
                current_segment.Pos0 = previous_segment.Pos0 + previous_segment.Vel0 * previous_duration + ( previous_segment.Acc0 * previous_duration * previous_duration) / 2.0 + ( previous_segment.Jerk * previous_duration * previous_duration * previous_duration) / 6.0;
                current_segment.Time0 = previous_segment.Time0 + previous_duration;
            }
        }
        // ----------------------------------------------------------------------
        public SCurve( SCurve original)
        {
            for( int loop = 0; loop < 7; ++loop){
                segments[ loop] = new Segment( original.segments[ loop]);
            }
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        /// <summary>
        /// Get the duration of the trajectory.
        /// </summary>
        /// <returns>Duration of the trajectory in time units.</returns>
        public double GetDuration()
        {
            return segments[ 6].TimeF;
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Get the position of the object at a given time during the trajectory.
        /// </summary>
        /// <param name="time">The given time.</param>
        /// <returns>The position of the object.</returns>
        public double GetPosition( double time)
        {
            // get segment corresponding to given time; exception thrown if time invalid.
            Segment segment = GetSegment( time);
            // segment time = time elapsed since time0 of segment.
            double segment_time = time - segment.Time0;
            // calculate object's position.
            return segment.Pos0 + segment.Vel0 * segment_time + ( segment.Acc0 * segment_time * segment_time) / 2.0 + ( segment.Jerk * segment_time * segment_time * segment_time) / 6.0;
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Get the velocity of the object at a given time during the trajectory.
        /// </summary>
        /// <param name="time">The given time.</param>
        /// <returns>The velocity of the object.</returns>
        public double GetVelocity( double time)
        {
            // get segment corresponding to given time; exception thrown if time invalid.
            Segment segment = GetSegment( time);
            // segment time = time elapsed since time0 of segment.
            double segment_time = time - segment.Time0;
            // calculate object's velocity.
            return segment.Vel0 + segment.Acc0 * segment_time + ( segment.Jerk * segment_time * segment_time) / 2.0;
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Get the acceleration of the object at a given time during the trajectory.
        /// </summary>
        /// <param name="time">The given time.</param>
        /// <returns>The acceleration of the object.</returns>
        public double GetAcceleration( double time)
        {
            // get segment corresponding to given time; exception thrown if time invalid.
            Segment segment = GetSegment( time);
            // segment time = time elapsed since time0 of segment.
            double segment_time = time - segment.Time0;
            // calculate object's acceleration.
            return segment.Acc0 + segment.Jerk * segment_time;
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Scale the entire trajectory by the given speed factor.
        /// Velocities are increased by the given speed factor.
        /// Accelerations are increased by the square and jerks are increased by the cube of the given speed factor.
        /// Duration (times) are decreased by the given speed factor.
        /// Positions remain unchanged.
        /// </summary>
        /// <param name="speed_factor">The given speed factor.</param>
        public void ScaleTrajectory( double speed_factor)
        {
            // make sure speed factor is a positive number (i.e., not less than or equal to zero and not NaN).
            if( !( speed_factor > 0.0)){
                throw new KinematicsException( String.Format( "Invalid speed_factor {0}", speed_factor));
            }
            foreach( Segment segment in segments){
                segment.Duration /= speed_factor;
                segment.Jerk *= ( speed_factor * speed_factor * speed_factor);
                segment.Acc0 *= ( speed_factor * speed_factor);
                segment.Vel0 *= ( speed_factor);
                segment.Time0 /= speed_factor;
            }
        }
        // ----------------------------------------------------------------------
        public void ScaleDuration( double new_duration)
        {
            ScaleTrajectory( GetDuration() / new_duration);
        }
        // ----------------------------------------------------------------------
        public IEnumerable< double> GetRelevantTimes()
        {
            return ( from s in segments
                     select s.Time0).Concat( new[]{ GetDuration()});
        }
        // ----------------------------------------------------------------------
        public double GetTimeOfPosition( double position)
        {
            Segment segment = GetSegmentOfPosition( position);
            Tuple< double, double, double> solution = MathUtil.SolveCubicFunction( segment.Jerk / 6.0, segment.Acc0 / 2.0, segment.Vel0, segment.Pos0 - position);
            MathUtil.QuantumMath qm = new MathUtil.QuantumMath( 0.000001);
            double segment_time = double.NaN;
            if( qm.Round( solution.Item1) >= 0.0 && qm.Round( solution.Item1) <= qm.Round( segment.Duration)){
                segment_time = solution.Item1;
            }
            if( qm.Round( solution.Item2) >= 0.0 && qm.Round( solution.Item2) <= qm.Round( segment.Duration)){
                segment_time = solution.Item2;
            }
            if( qm.Round( solution.Item3) >= 0.0 && qm.Round( solution.Item3) <= qm.Round( segment.Duration)){
                segment_time = solution.Item3;
            }
            return segment.Time0 + segment_time;
        }
        // ----------------------------------------------------------------------
        private Segment GetSegment( double time)
        {
            // make sure given time is a number greater than or equal to zero (i.e., not negative and not NaN).
            if( !( time >= 0.0)){
                throw new KinematicsException( String.Format( "Invalid time {0}", time));
            }
            // make sure given time doesn't exceed duration; allow 1e-6 leeway due to possible floating-point imprecision.
            double duration = GetDuration();
            if( time - duration > 1e-6){
                throw new KinematicsException( String.Format( "Invalid time {0} past duration {1}", time, duration));
            }
            // loop through all segments, looking for segment corresponding to given time (segment in which final time, TimeF, equals or exceeds given time).
            foreach( Segment segment in segments){
                if( time <= segment.TimeF){
                    return segment;
                }
            }
            // boundary case: if, due to floating-point imprecision, given time exceeds duration, then just return the last segment.
            return segments[ 6];
        }
        // ----------------------------------------------------------------------
        private Segment GetSegmentOfPosition( double position)
        {
            double pos_src = GetPosition( 0);
            double pos_dst = GetPosition( GetDuration());
            double sign = ( double)( Math.Sign( pos_dst - pos_src));

            int loop = 0;
            while( loop < 7){
                if( position * sign < segments[ loop].Pos0 * sign){
                    break;
                }
                ++loop;
            }

            return segments[ loop - 1];
        }
        // ----------------------------------------------------------------------
        public static void Test()
        {
            SCurve s = new SCurve( -250, 0, 250, 1000, 12500);
            double duration = s.GetDuration();
            // position = s.GetPosition( -0.000000001); // throws exception.
            // velocity = s.GetVelocity( -0.000000001); // throws exception.
            double position = s.GetPosition( -0);
            double velocity = s.GetVelocity( -0);
            position = s.GetPosition( 0);
            velocity = s.GetVelocity( 0);
            position = s.GetPosition( duration);
            velocity = s.GetVelocity( duration);
            position = s.GetPosition( duration + 0.000000001);
            velocity = s.GetVelocity( duration + 0.000000001);
            // position = s.GetPosition( duration + 0.00001); // throws exception.
            // velocity = s.GetVelocity( duration + 0.00001); // throws exception.
            s = new SCurve( 0, 250, 250, 1000, 12500);
            for( double loop = 0; loop <= 250; ++loop){
                double time = s.GetTimeOfPosition( loop);
                Console.WriteLine( "{0}\t{1}", loop, time);
            }
            s = new SCurve( 83.3, 96.0, 250, 2250, 32142.857);
            for( double loop = 83.3; loop <= 96.0; loop += 0.01){
                double time = s.GetTimeOfPosition( loop);
                Console.WriteLine( "{0}\t{1}", loop, time);
            }
        }
    }

    /// <summary>
    /// Class for a single-axis trajectory describing an object's motion in a dimension that is dependent on the sine of a separate dimension.
    /// </summary>
    public class FollowLineWithSine : ITrajectory
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        private SCurve SCurve { get; set; }
        private double Center { get; set; }
        private double Radius { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public FollowLineWithSine( SCurve s_curve, double center, double radius)
        {
            SCurve = new SCurve( s_curve);
            Center = center;
            Radius = radius;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        /// <summary>
        /// Get the duration of the trajectory.
        /// </summary>
        /// <returns>Duration of the trajectory in time units.</returns>
        public double GetDuration()
        {
            return SCurve.GetDuration();
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Get the position of the object at a given time during the trajectory.
        /// </summary>
        /// <param name="time">The given time.</param>
        /// <returns>The position of the object.</returns>
        public double GetPosition( double time)
        {
            return Center + ( Radius * Math.Cos( Math.PI * SCurve.GetPosition( time) / 180.0));
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Get the velocity of the object at a given time during the trajectory.
        /// </summary>
        /// <param name="time">The given time.</param>
        /// <returns>The velocity of the object.</returns>
        public double GetVelocity( double time)
        {
            return -Radius * Math.Sin( Math.PI * SCurve.GetPosition( time) / 180.0) * ( Math.PI * SCurve.GetVelocity( time) / 180.0);
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Get the acceleration of the object at a given time during the trajectory.
        /// </summary>
        /// <param name="time">The given time.</param>
        /// <returns>The acceleration of the object.</returns>
        public double GetAcceleration( double time)
        {
            throw new Exception( "the following calculations have not been vetted");
            /*
            double s_p = SCurve.GetPosition( time);
            double s_v = SCurve.GetVelocity( time);
            double s_a = SCurve.GetAcceleration( time);
            double pi_180 = Math.PI / 180.0;
            return -Radius * pi_180 * (( pi_180 * Math.Cos( pi_180 * s_p) * s_v * s_v) + ( Math.Sin( pi_180 * s_p) * s_a));
            */
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Scale the entire trajectory by the given speed factor.
        /// Velocities are increased by the given speed factor.
        /// Accelerations are increased by the square and jerks are increased by the cube of the given speed factor.
        /// Duration (times) are decreased by the given speed factor.
        /// Positions remain unchanged.
        /// </summary>
        /// <param name="speed_factor">The given speed factor.</param>
        public void ScaleTrajectory( double speed_factor)
        {
            SCurve.ScaleTrajectory( speed_factor);
        }
        // ----------------------------------------------------------------------
        public void ScaleDuration( double new_duration)
        {
            SCurve.ScaleDuration( new_duration);
        }
        // ----------------------------------------------------------------------
        public IEnumerable< double> GetRelevantTimes()
        {
            HashSet< double> retval = new HashSet< double>();
            double duration = GetDuration();
            for( double loop = 0.0; loop <= 8.0; loop += 1.0){
                retval.Add( loop * duration / 8.0);
            }
            return retval;
        }
    }
}
