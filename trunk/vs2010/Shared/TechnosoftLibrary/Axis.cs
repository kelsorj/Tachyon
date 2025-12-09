using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BioNex.Shared.Utils;
using BioNex.Shared.Utils.Kinematics;
using BioNex.Shared.Utils.PVT;
using log4net;

namespace BioNex.Shared.TechnosoftLibrary
{
    /// <summary>
    /// This is like MotorException, but it takes an IAxis.  This should eventually
    /// replace MotorException, but I didn't want to have to rewrite all of that 
    /// stuff right this second.  This will allow the error handler to reset
    /// faults on the axis.
    /// </summary>
    public class AxisException : ApplicationException
    {
        public List<IAxis> Axes { get; set; }
        private static readonly ILog _log = LogManager.GetLogger( typeof( IAxis));
        public string Details { get; private set; }

        public AxisException( IAxis axis, string message)
            : base( String.Format( "{0} on axis {1}", message, axis.Name))
        {
            _log.Error( Message);
            Axes = new List<IAxis> { axis };
        }

        public AxisException( List<IAxis> axes, string message)
            : base( message)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine( "Errors occurred on multiple axes:");
            foreach( IAxis axis in axes) {
                sb.AppendLine( String.Format( "axis {0}: {1}", axis.Name, axis.GetError()));
            }
            _log.Error( sb.ToString());
            Axes = axes;
        }

        public AxisException(IAxis axis, string message, string details)
            : this(axis, message)
        {
            Details = details;
        }
    }

    public class MotionParameterOutOfRangeException : AxisException
    {
        public MotionParameterOutOfRangeException( IAxis axis, string message)
            : base( axis, message)
        {
        }
    }

    #region Motor event handler code
    public delegate void MotorEventHandler( object sender, MotorEventArgs e);
    public delegate void ServoEnableEventHandler( object sender, EnableEventArgs e);
    //---------------------------------------------------------------------
    // MOTOR SETTINGS
    //---------------------------------------------------------------------
    public class EnableEventArgs : EventArgs
    {
        public bool Enabled { get; private set; }
        public EnableEventArgs( bool enabled)
        {
            Enabled = enabled;
        }
    }

    public class MotorEventArgs : EventArgs
    {
        public string msg { get; private set; }
        public MotorEventArgs( string message)
        {
            msg = message;
        }
    }
    #endregion

    public class MultiAxisTrajectory : Dictionary< IAxis, PVTTrajectory>
    {
        // ----------------------------------------------------------------------
        // inner classes.
        // ----------------------------------------------------------------------
        public class WaycoordinateInfo
        {
            public IAxis Axis { get; set; }
            public double Position { get; set; }
            public double MaxVel { get; set; }
            public double MaxAccel { get; set; }
            public SCurve SCurveTrajectory { get; set; }

            public WaycoordinateInfo( IAxis axis, double position, double max_vel, double max_accel)
            {
                if( double.IsNaN( position)){
                    throw new Exception();
                }
                Axis = axis;
                Position = position;
                // if passed-in velocities/accelerations are double.NaN, then the client likely didn't specify them. in these cases, use the axis' default settings.
                // if passed-in velocities/accelerations are valid numbers, then make sure they are in the range [1.0, default_value].
                // !!assuming minimum velocity of 1.0mm/s or 1.0°/s!!
                MaxVel = double.IsNaN( max_vel) ? axis.Settings.Velocity : Math.Min( axis.Settings.Velocity, Math.Max( 1.0, max_vel));
                MaxAccel = double.IsNaN( max_accel) ? axis.Settings.Acceleration : Math.Min( axis.Settings.Acceleration, Math.Max( 1.0, max_accel));
                SCurveTrajectory = null;
            }
        }
        // ----------------------------------------------------------------------
        protected class AppliedTrajectory
        {
            public ITrajectory Trajectory { get; set; }
            public IAxis Axis { get; set; }
            public double StartTime { get; set; }
            public int Marker { get; set; }

            public AppliedTrajectory( ITrajectory trajectory, IAxis axis, double start_time, int marker)
            {
                Trajectory = trajectory;
                Axis = axis;
                StartTime = start_time;
                Marker = marker;
            }
        }

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public IDictionary< IAxis, double> MoveDoneWindow
        {
            get{
                foreach( IAxis axis in Keys){
                    if( !_move_done_window.ContainsKey(axis)){
                        _move_done_window[axis] = axis.Settings.MoveDoneWindow;
                    }
                }
                return _move_done_window;
            }
            private set{
                _move_done_window = value;
            }
        }

        protected IDictionary< IAxis, double> CurrentWaypoint { get; set; }
        protected double CurrentBlendTime { get; set; }
        protected double SeparatorTime { get; set; }
        protected double NextSeparatorTime { get; set; }
        protected double NonBlendedDuration { get; set; }
        private static ILog Log { get; set; }

        // ----------------------------------------------------------------------
        // members.
        // ----------------------------------------------------------------------
        private IDictionary< IAxis, double> _move_done_window;
        protected List< AppliedTrajectory> AppliedTrajectories = new List< AppliedTrajectory>();
        protected MathUtil.QuantumMath TimeQuantumMath { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public MultiAxisTrajectory( IDictionary< IAxis, double> initial_position, IDictionary< IAxis, double> move_done_window, double time_quantum)
        {
            MoveDoneWindow = move_done_window;

            CurrentWaypoint = initial_position;
            CurrentBlendTime = 0.0;
            SeparatorTime = 0.0;
            NextSeparatorTime = 0.0;
            NonBlendedDuration = 0.0;

            Log = LogManager.GetLogger( typeof( MultiAxisTrajectory));

            TimeQuantumMath = new MathUtil.QuantumMath( time_quantum);

            foreach( IAxis axis in initial_position.Keys){
                Add( axis, new PVTTrajectory( initial_position[ axis]));
            }
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public double GetDuration()
        {
            return Values.Max( t => t.GetDuration());
        }
        // ----------------------------------------------------------------------
        public void AddWaypoint( int marker, IList< WaycoordinateInfo> waycoordinate_infos, double pre_blend_distance = 0.0, double post_blend_distance = 0.0)
        {
            // filter waycoordinate_infos to just those that are moving from the current waypoint.
            IList< WaycoordinateInfo> moving_waycoordinate_infos = ( from wi in waycoordinate_infos
                                                                     from cp in CurrentWaypoint
                                                                     where (( wi.Axis == cp.Key) &&
                                                                            ( wi.Position != cp.Value))
                                                                     select wi).ToList();
            // if none of the waycoordinates move from the current waypoint, then do nothing.
            // !!this waypoint addition gets skipped silently, meaning that the next waypoint addition will be blended with the current waypoint. is this OK??!!
            // !!otherwise, we could fix it by adding a separator (as commented out below)!!
            if( moving_waycoordinate_infos.Count() == 0){
                // AddSeparator();
                return;
            }
            // calculate quickest s-curve trajectories for each of the moving waycoordinates.
            foreach( WaycoordinateInfo mwi in moving_waycoordinate_infos){
                mwi.SCurveTrajectory = new SCurve( mwi.Position, CurrentWaypoint[ mwi.Axis], mwi.MaxVel, mwi.MaxAccel, mwi.Axis.Settings.GetJerkRate());
            }
            // get each moving waycoordinate's trajectory's duration and scale it with its axis' speed factor.
            // obtain the maximum of these scaled durations, and round it up to the nearest time quantum.
            // this is how long the move from the current waypoint to this new waypoint should take.
            double move_duration = TimeQuantumMath.Ceiling( moving_waycoordinate_infos.Max( mwi => mwi.SCurveTrajectory.GetDuration() / mwi.Axis.GetSpeedFactor()));
            // sanity check: if all we have are moving waycoordinates, how is it possible that the move duration isn't positive?
            if( !( move_duration > 0.0)){
                throw new Exception( "Unexpected non-positive time move");
            }
            // scale each moving waycoordinate's trajectory's duration to the calculated move duration.
            foreach( WaycoordinateInfo mwi in moving_waycoordinate_infos){
                mwi.SCurveTrajectory.ScaleDuration( move_duration);
            }
            // sorry for the long lines of code; bear with me:
            // if pre-blend distance is less than or equal to zero, then pre-blend time is zero.
            // otherwise, pre-blend time is the minimum of each moving waycoordinate's pre-blend time,
            // where each moving waycoordinate's pre-blend time
            // (1) is the entire move duration if the magnitude of the waycoordinate's displacement is shorter than pre-blend distance and
            // (2) is the amount of time it takes for the trajectory to reach the pre-blend distance otherwise.
            double pre_blend_time = TimeQuantumMath.Floor( pre_blend_distance <= 0.0 ? 0.0
                                                                                     : moving_waycoordinate_infos.Min( mwi => ( pre_blend_distance >= Math.Abs( mwi.Position - CurrentWaypoint[ mwi.Axis]) ? move_duration
                                                                                                                                                                                                           : mwi.SCurveTrajectory.GetTimeOfPosition( CurrentWaypoint[ mwi.Axis] + Math.Sign( mwi.Position - CurrentWaypoint[ mwi.Axis]) * pre_blend_distance))));
            // corresponding logic for post-blend time.
            double post_blend_time = TimeQuantumMath.Floor( post_blend_distance <= 0.0 ? 0.0
                                                                                       : moving_waycoordinate_infos.Min( mwi => ( post_blend_distance >= Math.Abs( mwi.Position - CurrentWaypoint[ mwi.Axis]) ? move_duration
                                                                                                                                                                                                              : move_duration - mwi.SCurveTrajectory.GetTimeOfPosition( mwi.Position - Math.Sign( mwi.Position - CurrentWaypoint[ mwi.Axis]) * post_blend_distance))));
            // start the move at the later of (1) the separator time or (2) pre-blend time earlier than the current blend time.
            double start_time = Math.Max( SeparatorTime, TimeQuantumMath.Round( CurrentBlendTime - pre_blend_time));
            // using start_time above for the current move, the current move possibly finishes before the previous move.
            // here, we ensure that the current move doesn't finish before the previous move.
            double time_by_which_this_move_finishes_before_previous_move = TimeQuantumMath.Round( NextSeparatorTime - ( start_time + move_duration));
            if( time_by_which_this_move_finishes_before_previous_move > 0){
                start_time = TimeQuantumMath.Round( start_time + time_by_which_this_move_finishes_before_previous_move);
            }
            // log results of blending.
            // (negative time saved pre-blending result when separators prevent pre-blending from occurring.)
            Log.DebugFormat( "On Marker {0}: saved {1:0.000}s by pre-blending and {2:0.000}s by post-blending a {3:0.000}s move to start at {4:0.000}s", marker, CurrentBlendTime - start_time, post_blend_time, move_duration, start_time);
            // add each moving waycoordinate's trajectory into the list of applied trajectories.
            // update the current waypoint to the given waypoint.
            foreach( WaycoordinateInfo mwi in moving_waycoordinate_infos){
                AppliedTrajectories.Add( new AppliedTrajectory( mwi.SCurveTrajectory, mwi.Axis, start_time, marker));
                CurrentWaypoint[ mwi.Axis] = mwi.Position;
            }
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
        }
        // ----------------------------------------------------------------------
        public void AddSeparator()
        {
            SeparatorTime = NextSeparatorTime;
        }
        // ----------------------------------------------------------------------
        public void GeneratePVTPoints()
        {
            double duration = TimeQuantumMath.Round( AppliedTrajectories.Max( t => t.StartTime + t.Trajectory.GetDuration()));

            Log.DebugFormat( "Congratulations, you saved {0:0.000}s by blending a {1:0.000}s-long move (old-time), saving {2:0.00}%", NonBlendedDuration - duration, NonBlendedDuration, 100 * ( NonBlendedDuration - duration) / NonBlendedDuration);

            foreach( IAxis axis in Keys){
                var axis_trajectory_infos = from t in AppliedTrajectories
                                            where t.Axis == axis
                                            orderby t.StartTime
                                            select t;

                SortedSet< double> relevant_times = new SortedSet< double>();

                foreach( double d in new[]{ 0.000, 0.010, 0.020, 0.040, 0.080, 0.160}){
                    if( duration > d){
                        relevant_times.Add( TimeQuantumMath.Round( duration - d));
                    }
                }

                foreach( AppliedTrajectory hti in axis_trajectory_infos){
                    relevant_times.UnionWith( from rt in hti.Trajectory.GetRelevantTimes()
                                              select TimeQuantumMath.Round( rt + hti.StartTime));
                }

                List< double> extra_times = new List< double>();
                double previous_time = 0.0;
                foreach( double relevant_time in relevant_times){
                    double gap = TimeQuantumMath.Round( relevant_time - previous_time);
                    double divisions = Math.Ceiling( gap / 0.500);
                    for( double loop = 1.0; loop < divisions; ++loop){
                        extra_times.Add( previous_time + TimeQuantumMath.Round( loop * gap / divisions));
                    }
                    previous_time = relevant_time;
                }
                relevant_times.UnionWith( extra_times);

                relevant_times.Remove( 0.0);

                double previous_position = this[ axis].GetPVTPointByIndex( 0).Position;
                int previous_marker = 0;
                previous_time = 0.0;
                foreach( double relevant_time in relevant_times){
                    var axis_time_trajectory_infos = from t in axis_trajectory_infos
                                                     where TimeQuantumMath.Between( relevant_time, t.StartTime, t.StartTime + t.Trajectory.GetDuration())
                                                     orderby t.StartTime
                                                     select t;
                    double position = previous_position;
                    double velocity = 0.0;
                    int marker = previous_marker;
                    if( axis_time_trajectory_infos.Count() != 0){
                        position = double.NaN;
                        foreach( AppliedTrajectory h in axis_time_trajectory_infos){
                            if( double.IsNaN( position)){
                                position = h.Trajectory.GetPosition( relevant_time - h.StartTime);
                            } else{
                                position += ( h.Trajectory.GetPosition( relevant_time - h.StartTime) - h.Trajectory.GetPosition( 0));
                            }
                        }
                        velocity = axis_time_trajectory_infos.Sum( t => t.Trajectory.GetVelocity( relevant_time - t.StartTime));
                        marker = axis_time_trajectory_infos.Max( t => t.Marker);
                    }
                    double time = relevant_time - previous_time;
                    PVTPoint tp = new PVTPoint( position, velocity, time, marker);
                    this[ axis].Enqueue( tp);
                    previous_time = relevant_time;
                    previous_position = position;
                    previous_marker = marker;
                }
            }
        }
        // ----------------------------------------------------------------------
        public int MarkerDuringWhichMoveDied()
        {
            return Keys.Max( axis => this[ axis].GetMarkerDuringWhichMoveDied( axis.PVTNumPointsBuffered()));
        }
        // ----------------------------------------------------------------------
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach( IAxis axis in Keys){
                sb.AppendFormat( "Axis {0}:", axis.Name);
                sb.AppendLine();
                sb.Append( this[ axis].ToString());
            }
            return sb.ToString();
        }
    }

    public class PVTSetupRejectedException : Exception
    {
        public PVTSetupRejectedException( string message)
            : base( message)
        {
        }
    }

    public class PVTPointsRejectedException : AxisException
    {
        public PVTPointsRejectedException( IAxis axis, string message)
            : base( axis, message)
        {
        }

        public PVTPointsRejectedException( List<IAxis> axes, string message)
            : base( axes, message)
        {
        }
    }

    public abstract class IAxis
    {
        private static readonly ILog _log = LogManager.GetLogger( typeof( IAxis));
#region PVT_IMPLEMENTATION
        //-------------------------------------------------------------------
        public abstract int PVTNumPointsBuffered();
        protected abstract void PVTWaitForSetupComplete();
        protected abstract void PVTAddPoints( PVTTrajectory pvt_trajectory);
        protected abstract void PVTGroupSetup( byte group_id);
        protected abstract void PVTGroupStartTrajectory( byte group_id);
        //-------------------------------------------------------------------
        public static void ExecuteCoordinatedPVTTrajectory( byte group_id, MultiAxisTrajectory trajectory, bool blocking = true)
        {
            double trajectory_time_s = trajectory.GetDuration();
            _log.Debug( "ExecuteCoordinatedPVTTrajectory loading trajectory");
            // get the axes involved in the coordinated PVT trajectory.
            ICollection< IAxis> axes = trajectory.Keys;
            IAxis first_axis = null;
            try{
                bool load_traj = true;
                for( int loop = 0; loop < 3 && load_traj; ++loop){
                    try{
                        load_traj = false;
                        // for each axis...
                        foreach( IAxis axis in axes){
                            if( first_axis == null){
                                first_axis = axis;
                            }
                            axis.AddToGroup( group_id);
                            if( axis.GetID() == 5){
                                axis.SetOutput( 13, false);
                            }
                        }
                        first_axis.PVTGroupSetup( group_id);
                        foreach( IAxis axis in axes){
                            axis.PVTWaitForSetupComplete();
                            axis.PVTAddPoints( trajectory[ axis]);
                        }
                    } catch( PVTPointsRejectedException pvtpre){
                        _log.Debug( pvtpre.Message);
                        load_traj = true;
                    }
                }
            } catch( Exception ex){
                throw new PVTSetupRejectedException( ex.Message);
            }
            _log.Debug( "ExecuteCoordinatedPVTTrajectory starting trajectory");
            first_axis.PVTGroupStartTrajectory( group_id);
            // ensure motion starts... (wait for all axes motion to NOT be complete)
            DateTime motion_complete_timeout_start = DateTime.Now;
            const double motion_complete_timeout_s = 5;
            while(( DateTime.Now - motion_complete_timeout_start).TotalSeconds <= motion_complete_timeout_s){
                Thread.Sleep( 10);
                bool all_started = true;
                foreach( IAxis axis in axes){
                    if( axis.ReadMotionCompleteFlag()){
                        // if motion is complete, then either
                        // motion actually completed else
                        // motion complete flag not yet cleared --> clear all started.
                        Int32 CPOS, APOS, TPOS;
                        axis.GetLongVariable("CPOS", out CPOS);
                        axis.GetLongVariable("APOS", out APOS);
                        axis.GetLongVariable("TPOS", out TPOS);
                        Int32 position_error = Math.Abs(TPOS - APOS);
                        Int32 allowed_error = axis.ConvertToCounts(axis.Settings.MoveDoneWindow);
                        if( position_error > allowed_error){
                            all_started = false;
                        }
                    }
                }
                if( all_started){
                    break;
                }
            }
            if(( DateTime.Now - motion_complete_timeout_start).TotalSeconds > motion_complete_timeout_s){
                List<IAxis> annoying = new List<IAxis>();
                List<string> errors = new List<string>();
                foreach (var x in axes) {
                    annoying.Add(x);
                    errors.AddRange( x.GetFaults());
                }
                string concat_error = String.Join( ", ", errors.ToArray());
                throw new AxisException(annoying, String.Format( "PVT motion timed out while waiting for motion complete flag: {0}", concat_error));
            }
            // wait for motion to complete.
            motion_complete_timeout_start = DateTime.Now;
            double motion_complete_timeout_ms = trajectory_time_s * 1000 + 5000;
            while(( DateTime.Now - motion_complete_timeout_start).TotalMilliseconds <= motion_complete_timeout_ms){
                Thread.Sleep( 10);
                bool all_done = true;
                foreach( IAxis axis in axes){
                    // Trajectory pvt_data = trajectory[ axis];
                    bool mc = axis.ReadMotionCompleteFlag();
                    if( !mc){
                        all_done = false;
                    } else{
                        /*
                        if( pvt_data.Count != 0){
                            List<string> faults_now = axis.GetFaults();
                            if (faults_now.Count != 0) {
                                throw new AxisException(axis, String.Format("PVT motion concluded before full trajectory was fed to controller, {0} faults found: {1}", faults_now.Count, String.Join( ", ", faults_now.ToArray())));
                            }
                            throw new AxisException(axis, "PVT motion concluded before full trajectory was fed to controller, no faults found. reword this!!");
                        }
                        */
                    }
                    List< string> faults = axis.GetFaults();
                    if( faults.Count != 0){
                        foreach( IAxis axis_to_stop in axes){
                            // stop all axes other than the one that faulted.
                            if( axis_to_stop != axis){
                                _log.DebugFormat( "stop axis sent to axis {0}", axis_to_stop.Name);
                                axis_to_stop.Stop();
                                _log.DebugFormat( "stopped axis {0}", axis_to_stop.Name);
                            }
                        }
                        throw new AxisException( axis, String.Format( "Fault on axis {0} while executing PVT trajectory: {1}", axis.Name, String.Join( ", ", faults.ToArray())));
                    }
                    // axis.PVTAddPoints( pvt_data, false);
                }
                if( all_done){
                    break;
                }
            }
            if(( DateTime.Now - motion_complete_timeout_start).TotalMilliseconds > motion_complete_timeout_ms){
                List<IAxis> annoying = new List<IAxis>();
                List<string> errors = new List<string>();
                foreach (var x in axes) {
                    annoying.Add(x);
                    errors.AddRange( x.GetFaults());
                }
                string concat_error = String.Join( ", ", errors.ToArray());
                throw new AxisException(annoying, String.Format( "PVT motion timed out while waiting for motion to complete: {0}", concat_error));
            }
            // wait for motion to settle.
            // DKM 2011-08-30 small enhancement to track position errors for each axis in the case where we timeout
            Dictionary<IAxis,bool> motion_complete = new Dictionary<IAxis,bool>();

            while(( DateTime.Now - motion_complete_timeout_start).TotalMilliseconds <= motion_complete_timeout_ms){
                Thread.Sleep( 10);
                bool all_done = true;
                foreach( IAxis axis in axes){
                    // Trajectory pvt_data = trajectory[ axis];
                    // bool motion_complete = axis.ReadMotionCompleteFlag();
                    Int32 CPOS, APOS, TPOS;
                    axis.GetLongVariable( "CPOS", out CPOS);
                    axis.GetLongVariable( "APOS", out APOS);
                    axis.GetLongVariable( "TPOS", out TPOS);
                    Int32 actual_error = Math.Abs( CPOS - APOS);
                    Int32 target_error = Math.Abs( CPOS - TPOS);
                    Int32 allowed_error = axis.ConvertToCounts( trajectory.MoveDoneWindow[ axis]);                    
                    motion_complete[axis] = ( actual_error < allowed_error) && ( target_error < allowed_error);
                    _log.DebugFormat( "Axis {0}: ExecutePVT TPOS={1}, CPOS={2}, APOS={3}, abs(CPOS-APOS)={4}, abs(CPOS-TPOS)={5}, allowed_error={6}", axis.GetID(), TPOS, CPOS, APOS, actual_error, target_error, allowed_error);
                    if( !motion_complete[axis]){
                        all_done = false;
                    } else{
                        /*
                        if( pvt_data.Count != 0){
                            List<string> faults_now = axis.GetFaults();
                            if (faults_now.Count != 0) {
                                throw new AxisException(axis, String.Format("PVT motion concluded before full trajectory was fed to controller, {0} faults found: {1}", faults_now.Count, String.Join( ", ", faults_now.ToArray())));
                            }
                            throw new AxisException(axis, "PVT motion concluded before full trajectory was fed to controller, no faults found. reword this!!");
                        }
                        */
                    }
                    List< string> faults = axis.GetFaults();
                    if( faults.Count != 0){
                        foreach( IAxis axis_to_stop in axes){
                            // stop all axes other than the one that faulted.
                            if( axis_to_stop != axis){
                                axis_to_stop.Stop();
                            }
                        }
                        throw new AxisException( axis, String.Format( "Fault on axis {0} while executing PVT trajectory: {1}", axis.Name, String.Join( ", ", faults.ToArray())));
                    }
                    // axis.PVTAddPoints( pvt_data, false);
                }
                if( all_done){
                    break;
                }
            }
            if(( DateTime.Now - motion_complete_timeout_start).TotalMilliseconds > motion_complete_timeout_ms){
                List<IAxis> annoying = new List<IAxis>();
                List<string> errors = new List<string>();
                foreach (var x in axes) {
                    annoying.Add(x);
                    errors.AddRange( x.GetFaults());
                }
                string concat_error = String.Join( ", ", errors.ToArray());
                // overwrite error if it's blank with position errors
                if (concat_error == "")
                {
                    foreach( IAxis axis in axes)
                    {
                        if (!motion_complete[axis])
                        {
                            concat_error += String.Format("Axis {0} did not complete its move.  ", axis.Name);
                        }
                    }
                }
                throw new AxisException(annoying, String.Format( "PVT motion timed out while waiting for position to be reached: {0}", concat_error));
            }
        }
        //-------------------------------------------------------------------
#endregion
        public event MotorEventHandler HomeComplete;
        public event MotorEventHandler HomeError;
        public event MotorEventHandler MoveComplete; 
        public event ServoEnableEventHandler EnableComplete;

        protected string _conversion_formula { get; set; }

        public string FirmwareVersion
        {
            get {
                try {
                    return String.Format("{0}.{1}", FirmwareMajorVersion, FirmwareMinorVersion);
                } catch (Exception) {
                    return "N/A";
                }
            }
        }

        public int FirmwareMajorVersion
        {
            get {
                try {
                    short major;
                    if( !GetIntVariable( TechnosoftConnection.FirmwareMajor, out major))
                        return 0;
                    return major;
                } catch( Exception) {
                    return 0;
                }
            }
        }

        public int FirmwareMinorVersion
        {
            get {
                try {
                    short minor;
                    if( !GetIntVariable( TechnosoftConnection.FirmwareMinor, out minor))
                        return 0;
                    return minor;
                } catch( Exception) {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets a lock for the axis controller.  In the TSAxis case, gets the lock that was passed down
        /// by the TechnosoftConnection class.  In the SimAxis case, doesn't do anything of interest.
        /// I think I need to come up with a better way to handle this.
        /// </summary>
        /// <returns></returns>
        public abstract object Lock { get; }
        public abstract string Name { get; }
        public bool IgnoreMotionParameterChecking { get; set; }

        /// <summary>
        /// _settings reflect the soft limits of this axis.
        /// </summary>
        protected MotorSettings _settings;
        protected int _idxSetup = 0;
        public int IdxSetup { get { return _idxSetup; } }

        public MotorSettings Settings { get { return _settings; } }

        // Download eeprom settings and TML code to drive
        public abstract void DownloadSwFile(String swFilePath);

        // for pause / resume functionality
        protected ManualResetEvent AxisPauseEvent = new ManualResetEvent( true);
        public virtual void Pause()
        {
            _log.DebugFormat( "Axis {0} pausing via AxisPauseEvent", Name);
            AxisPauseEvent.Reset();
        }
        public virtual void Resume()
        {
            AxisPauseEvent.Set();
        }
        public virtual void ResetPause() // allows things like PauseEvent to get Set so we don't have freezes after aborting protocols
        {
            AxisPauseEvent.Set();
        }
        // DKM 2011-03-30 added an Abort method so we could bail out of a motion command because of the main GUI abort button
        protected ManualResetEvent AxisAbortEvent = new ManualResetEvent( false);
        public virtual void Abort()
        {
            _log.DebugFormat( "Axis {0} aborting via AxisAbortEvent", Name);
            AxisAbortEvent.Set();
        }
        public void ResetAbort()
        {
            AxisAbortEvent.Reset();
        }
                         
        // The following functions deal with the axis timeout timer for power savings
        public abstract int AxisTimeoutSecs { get; set; } // setting of how many seconds until timeout from last motion complete
        public abstract int AxisTimeoutTimerSecs { get; } // how many seconds until timeout; if negative, then timeout already occurred

        // The following two functions are for BB W axis boards with Syringe ID boards attached. They block for approx 100ms before returning.
        public abstract void ReadExtI2CPage (Byte page, out UInt64 data); // Page is 1-32 where page 32 holds ROM S/N
        public abstract void WriteExtI2CPage(Byte page,     UInt64 data); // Page is 1-16 where the R/W EEPROM exists
        public abstract void ReadExtI2CByte (Byte addr, out Byte   data);
        public abstract void WriteExtI2CByte(Byte addr,     Byte   data);

        public abstract String ReadApplicationID(); // read application ID from Technosoft controller at 0x5fcf
        public abstract String ReadSerialNumber(); // read serial number from Technosoft controller eeprom at serial_number_ptr
        public abstract void WriteSerialNumber(string SerialNumberStr); // write serial number string to Technosoft controller eeprom at serial_number_ptr
        public abstract Int32 ReadLongVarEEPROM(String ptr_name); // read LONG (32-bit) variable from eeprom memory at long_ptr_name
        public abstract Int16 ReadIntVarEEPROM(String ptr_name); // read INT (16-bit) variable from eeprom memory at ptr_name
        public abstract double ReadFixedVarEEPROM(String ptr_name); // read FIXED (32-bit) variable from eeprom memory at ptr_name
        public abstract void WriteLongVarEEPROM(String ptr_name, Int32 long_var); // write LONG (32-bit) variable into eeprom memory at ptr_name
        public abstract void WriteIntVarEEPROM(String ptr_name, Int16 int_var); // write INT (16-bit) variable to eeprom memory at ptr_name
        public abstract void WriteFixedVarEEPROM(String ptr_name, double fixed_var); // write FIXED (32-bit) variable to eeprom memory at ptr_name

        public abstract void Enable( bool enable, bool blocking);
        // DKM 2012-04-04 added to allow us to bail from cancellable calls, e.g. imbalance_test in HiG spindle firmware (should be cancellable as of v1.6)
        public abstract void AbortCancellableCall();

        /// <summary>
        /// Mark wants to be able to prevent an axis from ever being servod
        /// on if it's been flagged as "disabled".  Enabling should allow the
        /// axis to be turned on, but doesn't actually turn it on.  Disabling
        /// the axis should turn it off, and prevent it from being turned on again.
        /// </summary>
        /// <remarks>
        /// At some point, we started to use Enable as a Servo On, so I have removed
        /// the Obsolete warning.
        /// </remarks>
        //[Obsolete("We don't need this anymore", false)]
        private bool _enabled = true;
        //[Obsolete("We don't need this anymore", false)]
        public bool Enabled
        {
            get { return _enabled; }
            set { 
                _enabled = value; 
                if( !_enabled)
                    Enable( false, true);
            }
        }

        public bool IsZAxis { get { return GetID() % 10 == 3; } }

        public abstract void RefreshServoTimeout(); // refresh servo timeout parameter so servo doesn't time out early (used when jogging and teaching)

        // DKM 2010-09-23 changed from 5 to 2 because Synapsis loading w/ system power off was painfully slow when going through all axes
        private int _max_read_retries = 2; // only try up to 2 times to read a variable from a controller over the comms bus
        public int max_read_retries { get { return _max_read_retries; } set { _max_read_retries = value; _max_read_retries = Math.Max(_max_read_retries, 1); } }

        public abstract void Home( bool wait_for_complete);
        public abstract void SendResetAndHome();
        public abstract void WaitForHomeResult( long timeout_ms, bool check_is_homing);

        public void MoveAbsolute( double mm_or_ul,
                                  double velocity = double.NaN,
                                  double acceleration = double.NaN,
                                  int jerk = int.MinValue,
                                  int jerk_min = int.MinValue,
                                  bool wait_for_move_complete = true,
                                  double move_done_window_mm = double.NaN,
                                  short settling_time_ms = short.MinValue,
                                  bool use_TS_MC = false,
                                  bool use_trap = false,
                                  bool ignore_motion_parameters = false)
        {
            if( double.IsNaN( velocity)){
                velocity = _settings.Velocity;
            }
            if( double.IsNaN( acceleration)){
                acceleration = _settings.Acceleration;
            }
            if( jerk == int.MinValue){
                jerk = _settings.Jerk;
            }
            if( jerk_min == int.MinValue){
                jerk_min = _settings.MinJerk;
            }
            if( double.IsNaN( move_done_window_mm)){
                move_done_window_mm = _settings.MoveDoneWindow;
            }
            if( settling_time_ms == short.MinValue){
                settling_time_ms = _settings.SettlingTimeMS;
            }

            // here, we will check the conversion factor to go from ul to mm.  Only the W axis should
            // have such a value.  Every other axis should just return 1.
            if( GetConversionFormula() != null) {
                mm_or_ul = ConvertUlToMm( mm_or_ul);
                velocity = ConvertUlToMm( velocity);
            }

            // DKM 2011-03-25 need to come up with a better way to deal with ignoring motion parameters
            // the first one is a parameter I added a while ago to allow users to ignore the checks when running a protocol and getting an error from liquid classes.
            // I think we should remove that feature and just make sure they never run with invalid values?
            // the second is to handle ignoring acceleration values when tip shucking, per #374.
            if( !IgnoreMotionParameterChecking && !ignore_motion_parameters) {
                if( velocity > _settings.Velocity){
                        throw new MotionParameterOutOfRangeException( this, String.Format( "Commanded velocity of {0:0.00} units/s exceeds velocity soft limit of {1:0.00} units/s", velocity, _settings.Velocity));
                }

                if( acceleration > _settings.Acceleration){
                        throw new MotionParameterOutOfRangeException( this, String.Format( "Commanded acceleration of {0:0.00} units/s^3 exceeds acceleration soft limit {1:0.00} units/s^2.", acceleration, _settings.Acceleration));
                }

                // DKM 2010-11-22 should ignore this boundary check if we're doing a trap move
                if( !use_trap && jerk > _settings.Jerk){
                        throw new MotionParameterOutOfRangeException( this, "Commanded jerk exceeds jerk soft limit.");
                }
            }

            _MoveAbsoluteTry = 0; // reset try counter to 0. MoveAbsoluteHelper() will increment at the top of the function
            _MoveAbsoluteLastError = String.Format("No error");
            MoveAbsoluteHelper( mm_or_ul, velocity, acceleration, jerk, jerk_min, wait_for_move_complete, move_done_window_mm, settling_time_ms, use_TS_MC, use_trap);
        }

        protected int _MoveAbsoluteTry; // current try number. Will try up to g_MoveAbsoluteTries before giving up
        protected String _MoveAbsoluteLastError; // last error message in MoveAbsolute. Will be printed in the exception after a number of retries

        public abstract bool IsAxisOnFlag { get; }
        protected abstract void TurnAxisOnIfNecessary( bool blocking);
        public abstract bool ReadMotionCompleteFlag();
        public abstract bool ReadTrajectoryCompleteFlag();
        protected abstract void MoveAbsoluteHelper( double mm /* not ul!! */, double velocity, double acceleration, int jerk, int jerk_min, bool wait_for_move_complete, double move_done_window_mm, short settling_time_ms, bool use_TS_MC, bool use_trap);
        public abstract void MoveRelative( double mm_or_ul);
        public abstract void MoveRelative( double mm_or_ul, double velocity, double acceleration, int jerk);
        public abstract void MoveAbsoluteTorqueLimited(double mm, double velocity, double acceleration, int jerk, int jerk_min,
            short settling_time_ms, double max_Amps, double IMaxPS_Amps, double torque_limiting_window_rel_mm);
        /// <summary>
        /// Simple true / false result to the question, "is this motor homed?"
        /// </summary>
        /// <remarks>
        /// Queries the motor controller every time it is called
        /// </remarks>
        /// <returns></returns>
        public abstract bool IsHomed { get; }
        protected abstract int HomingStatus { get; }
        public abstract bool ReadHomeSensor();
        public abstract int GetPositionCounts();
        public abstract double GetPositionMM();
        public double GetPositionUl()
        {
            return ConvertMmToUl( GetPositionMM());
        }
        public void WaitForPositionMM( double position, double within)
        {
            while( Math.Abs( GetPositionMM() - position) > within){
                Thread.Sleep( 50);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="speed_deg_per_sec"></param>
        /// <param name="accel_deg_per_sec2"></param>
        /// <param name="wait_for_traj_complete"></param>
        /// <returns>true if completed move to speed, false if aborted before reaching final speed</returns>
        public abstract bool MoveSpeed(double speed_deg_per_sec, double accel_deg_per_sec2, bool wait_for_traj_complete); // execute a speed move (jog at speed)
        protected ManualResetEvent _abort_move_speed_event; // used to stop a velocity move
        public virtual void AbortMoveSpeed() { _abort_move_speed_event.Set(); }
        public abstract double GetActualSpeedIU();
        public abstract double GetActualSpeedDegPerSec();
        
        public abstract short GetAnalogReading( uint channel_0_based);
        public abstract void SetOutput( uint bit_0_based, bool logic_high);
        public abstract void ReadStatus ( Int16 SelIndex, out UInt16 Status); // wrapper around TS_ReadStatus
        public abstract bool GetIntVariable( String pszName, out Int16 value); // wrapper around TS_GetIntVariable
        public abstract bool GetLongVariable( String pszName, out Int32 value); // wrapper around TS_GetLongVariable
        public abstract bool GetFixedVariable( String pszName, out Double value); // wrapper around TS_GetFixedVariable
        public abstract bool GetInput(Byte nIO, out Byte InValue); // wrapper around TS_GetInput

        public abstract bool SetIntVariable( String pszName, Int16 value); // wrapper around TS_SetIntVariable
        public abstract bool SetLongVariable( String pszName, Int32 value); // wrapper around TS_SetLongVariable
        public abstract bool SetFixedVariable( String pszName, Double value); // wrapper around TS_SetFixedVariable

        public abstract bool SetCurrentAmpsCmdLimit (double current_amps); // sets the max possible commanded current (torque) on an axis
        public abstract bool GetCurrentAmpsCmdLimit (out double current_amps); // gets the max possible commanded current (torque) already set on an axis

        public abstract void ResetFaults();
        public abstract void ResetDrive();
        public bool UseTrapezoidalProfileByDefault { get; internal set; }
        /// <summary>
        /// this doesn't really belong here -- need to see if I can remove / move it elsewhere
        /// </summary>
        public abstract void ResetFaultsOnAllAxes();
        public abstract List<string> GetFaults();
        public abstract List<string> GetFaults(UInt16 mask);
        /// <summary>
        /// personally, I think we should get rid of GetCountsPerEngineeringUnit and use ConvertToCounts
        /// </summary>
        /// <returns></returns>
        public abstract double GetCountsPerEngineeringUnit();
        public abstract int ConvertToCounts( double mm_or_ul);
        public abstract double ConvertCountsToEng(int counts); // does a conversion from quad counts to Engineering units
        public abstract short ConvertToTicks(short time_ms);
        public virtual double ConvertUlToMm( double ul)
        {
            // replace formula with this -----vvvvvv
            return ul / ( Math.PI * Math.Pow( (4.0383 / 2), 2));
        }
        public virtual double ConvertMmToUl( double mm)
        {
            return mm / ConvertUlToMm( 1.0);
        }

        /// <summary>
        /// Allows axis to convert from arbitrary units to mm.  Intended use is to allow
        /// W axes to convert from uL (which comes from the hitpick file) into mm.
        /// </summary>
        /// <remarks>
        /// If no conversion is specified, should return null.
        /// </remarks>
        /// <returns></returns>
        public abstract string GetConversionFormula();
        public abstract void SetConversionFormula( string formula);

        public abstract void Stop();
        public abstract void AddToGroup( byte group_id);
        public abstract void RemoveFromGroup( byte group_id);
        public abstract byte GetID();
        public abstract bool IsMoveComplete();
        public abstract bool IsTargetReached(); // queries SRH:bit9 to see if target (pos/spd) is reached
        public abstract string GetError();
        public abstract string GetError(UInt16 mask);


        public abstract void MasterCamOnOff(byte slave_axis_id, bool on_off);
        public abstract void SlaveCamOnOff (UInt16 cam_address, bool on_off, double max_speed_iu, int cam_pos_offset_iu);

        public abstract void StartLogging();
        public abstract void WaitForLoggingComplete( string filepath);

        public abstract bool IsHoming();
        /// <summary>
        /// This needs to be fixed.  I set it with an int and return a double, which is completely insane.
        /// </summary>
        /// <param name="speed"></param>
        public abstract void SetSpeedFactor( int speed);
        public abstract double GetSpeedFactor();

        // blending stuff
        public abstract void SetupBlendedMove( double position, bool set_event_on_complete, bool use_trap);
        public abstract void StartBlendedMove();
        /// <summary>
        /// Waits for the slave axis to finish its move.  Also checks the master axis for
        /// errors, since if the master axis' move fails, the slave axis will never get
        /// commanded.
        /// </summary>
        /// <param name="master_axis"></param>
        public abstract void WaitForBlendedMoveComplete( IAxis master_axis);

        public abstract bool IsOn();

        //! this is done as a workaround because derived classes may NOT use
        //! an event that was declared in the base class
        public virtual void OnHomeComplete( object sender, MotorEventArgs e)
        {
            if( HomeComplete == null)
                return;
            HomeComplete( sender, e);
        }

        public virtual void OnHomeError( object sender, MotorEventArgs e)
        {
            if( HomeError == null)
                return;
            HomeError( sender, e);
        }
        
        public virtual void OnMoveComplete( object sender, MotorEventArgs e)
        {
            if( MoveComplete == null)
                return;
            MoveComplete( sender, e);
        }

        public virtual void OnEnableComplete( object sender, EnableEventArgs e)
        {
            if( EnableComplete != null)
                EnableComplete( sender, e);
        }

        public abstract void ZeroIqref();

        /// <summary>
        /// My attempt at making the flyby barcode reading function as generically accessible as possible.
        /// All of the parameters need to be sent in servo controller units.  For the Technosoft controllers,
        /// this means IU.
        /// </summary>
        /// <remarks>
        /// For TSAxis, this function blocks now!!!
        /// </remarks>
        /// <param name="function_name"></param>
        /// <param name="first_action_pos"></param>
        /// <param name="interval"></param>
        /// <param name="number_of_actions"></param>
        /// <param name="velocity"></param>
        /// <param name="accel"></param>
        /// <param name="jerk"></param>
        public abstract void CallFunctionWithPeriodicActions( string function_name, int first_action_pos, int interval,
                                                              short number_of_actions, double velocity, double accel, int jerk);

        public abstract void CallFunction(string function_name); // non-blocking call which simply fires off a TS_CALL_Label command
        public abstract int CallFunctionAndWaitForDone(string function_name, TimeSpan ts_timeout, bool return_func_done=false); // blocking call that waits for func_done variable to be non-zero
        public abstract void GotoFunction(string function_name); // non-blocking call which simply fires off a TS_GOTO_Label command
        public abstract void GotoFunctionAndWaitForDone(string function_name, TimeSpan ts_timeout); // blocking call that waits for func_done variable to be non-zero
        public abstract void SendTmlCommands( string commands);
    }
}