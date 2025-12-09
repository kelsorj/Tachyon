using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BioNex.Hive.Hardware;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Teachpoints;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.Utils;

namespace BioNex.Hive.Executor
{
    internal class PlaceStateMachine : HiveStateMachine< PlaceStateMachine.State, PlaceStateMachine.Trigger>
    {
        // ----------------------------------------------------------------------
        // enumerations.
        // ----------------------------------------------------------------------
        internal enum State
        {
            Start,
            BlendedPlace, BlendedPlaceError,
            End, Abort,
        }
        // ----------------------------------------------------------------------
        internal enum Trigger
        {
            Success,
            Failure,
            Retry,
            Ignore,
            Abort,
        }
        // ----------------------------------------------------------------------
        internal enum BlendedPlaceOption
        {
            BPONormal,
            BPONoRetractAfterPlace,
            BPOTerminateAtTeachpoint,
        }
        // ----------------------------------------------------------------------
        private enum Marker
        {
            MErrorRise = 0,
            MRetract1,
            MXZPosition,
            MExtend,
            MDrop,
            MPlace,
            MRise,
            MRetract2,
        }

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        private HiveTeachpoint Teachpoint { get; set; }
        private ILabware Labware { get; set; }
        /// <summary>
        /// used to account for placing lids on top of plates, where we need to bias the teachpoint
        /// up by the "lid offset" amount in the labware database,.
        /// </summary>
        private double AdditionalZOffset { get; set; }
        private BlendedPlaceOption Option { get; set; }
        private Func< bool> CheckForLidCallback { get; set; }
        private bool DesiredLidSensorState { get; set; }

        /// <summary>
        /// This flag means that we have a teachpoint we can't get to with PVT without doing a "drop in place" move.
        /// We need to set a flag so that subsequent states know how to behave.  For example, MoveZDownToGrip would
        /// normally get bypassed for PVT, but we only want to bypass if we're NOT doing "drop in place".
        /// </summary>
        private bool UseKungFuMove { get; set; }

        private bool DoPreMoveZPositioning { get; set; }
        private bool DoRetractOnly { get; set; }
        private bool FlushedByPick { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        internal PlaceStateMachine( HiveExecutor executor, ManualResetEvent ended_aborted_event, HiveTeachpoint teachpoint, ILabware labware, double additional_z_offset = 0, BlendedPlaceOption option = BlendedPlaceOption.BPONormal, Func< bool> check_for_lid_callback = null, bool desired_lid_sensor_state = false)
            : base( executor, ended_aborted_event, typeof( PlaceStateMachine), State.Start, State.End, State.Abort, Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, executor.HandleError)
        {
            InitializeStates();

            executor.Hardware.Messenger.Register< AbortCommand>( this, Abort);
            executor.Hardware.Messenger.Register< PauseCommand>( this, Pause);
            executor.Hardware.Messenger.Register< ResumeCommand>( this, Resume);

            Teachpoint = teachpoint;
            Labware = labware;
            AdditionalZOffset = additional_z_offset;
            Option = option;
            CheckForLidCallback = check_for_lid_callback;
            DesiredLidSensorState = desired_lid_sensor_state;

            // this is support for the "kung fu move" when plates at the bottom of the hive can't actually be picked with PVT
            // because the plate will clip the cover at the bottom of the robot
            UseKungFuMove = ( Teachpoint.Z <= executor.Hardware.Config.MinimumAllowableToolZHeightForPVT);

            DoPreMoveZPositioning = false;
            DoRetractOnly = false;
            FlushedByPick = false;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        private void InitializeStates()
        {
            ConfigureState( State.Start, NullStateFunction, State.BlendedPlace);
            ConfigureState( State.BlendedPlace, BlendedPlace, State.End, State.BlendedPlaceError);
            // DKM 2011-06-02 new pick state machine wasn't using a End function like the old state machine,
            //                which unregistered HiveMessenger handlers
            ConfigureState( State.End, EndStateFunction);
            ConfigureState( State.Abort, AbortedStateFunction);
        }
        // ----------------------------------------------------------------------
        public void PickFlush()
        {
            FlushedByPick = true;
        }
        // ----------------------------------------------------------------------
        public override void Start()
        {
            if( FlushedByPick){
                Flush();
            } else{
                base.Start();
            }
        }
        // ----------------------------------------------------------------------
        protected override void EndStateFunction()
        {
            Executor.Hardware.Messenger.Unregister< AbortCommand>( this, Abort);
            Executor.Hardware.Messenger.Unregister< PauseCommand>( this, Pause);
            Executor.Hardware.Messenger.Unregister< ResumeCommand>( this, Resume);
            base.EndStateFunction();
        }
        // ----------------------------------------------------------------------
        protected override void AbortedStateFunction()
        {
            Executor.Hardware.Messenger.Unregister< AbortCommand>( this, Abort);
            Executor.Hardware.Messenger.Unregister< PauseCommand>( this, Pause);
            Executor.Hardware.Messenger.Unregister< ResumeCommand>( this, Resume);
            base.AbortedStateFunction();
        }
        // ----------------------------------------------------------------------
        private void Abort( AbortCommand command)
        {
            base.Abort();
        }
        // ----------------------------------------------------------------------
        private void Pause( PauseCommand command)
        {
            base.Pause();
        }
        // ----------------------------------------------------------------------
        private void Resume( ResumeCommand command)
        {
            base.Resume();
        }
        // ----------------------------------------------------------------------
        private void BlendedPlace()
        {
            // DKM 2011-06-02 since PVT has increased the granularity of a move, we can't pause in the middle of motion.  This
            //                is my best attempt at pausing pick and place without allowing the robot to complete the entire
            //                move.
            WaitHandle.WaitAny( new WaitHandle[] { SMPauseEvent, _main_gui_abort_event });

            HiveMultiAxisTrajectory hive_multi_axis_trajectory = null;

            try{
                // get configuration values.
                double arm_length = Executor.Hardware.Config.ArmLength;
                double finger_offset = Executor.Hardware.Config.FingerOffsetZ;
                double safe_t = Executor.Hardware.Config.ThetaSafe;
                double kung_fu_t = Executor.Hardware.Config.ThetaKungFu;

                // get current positions.
                double current_x = XAxis.GetPositionMM();
                double current_z = ZAxis.GetPositionMM();
                double current_t = TAxis.GetPositionMM();
                double current_g = GAxis.GetPositionMM();

                // convert current positions to tool space.
                Tuple< double, double> current_yz_tool = HiveMath.ConvertTZWorldToYZTool( arm_length, finger_offset, current_t, current_z);
                double current_y_tool = current_yz_tool.Item1;
                double current_z_tool = current_yz_tool.Item2;

                // compute y destinations.
                double safe_y = HiveMath.GetYFromTheta( arm_length, safe_t);
                double kung_fu_y = HiveMath.GetYFromTheta( arm_length, kung_fu_t);

                // compute z destinations.
                double entry_z_tool = Teachpoint.Z + AdditionalZOffset + Labware[ LabwarePropertyNames.GripperOffset].ToDouble() + Teachpoint.ApproachHeight;
                double place_z_tool = Teachpoint.Z + AdditionalZOffset + Labware[ LabwarePropertyNames.GripperOffset].ToDouble();
                double exit_z_tool = Teachpoint.Z + Math.Max( AdditionalZOffset + Labware[ LabwarePropertyNames.GripperOffset].ToDouble(), Teachpoint.ApproachHeight);

                double kung_fu_z = HiveMath.ConvertTZWorldToYZTool( arm_length, finger_offset, safe_t, HiveMath.ConvertZToolToWorldUsingTheta( arm_length, finger_offset, entry_z_tool, kung_fu_t)).Item2;

                // compute g destinations.
                double ungrip_width = Labware[ Teachpoint.Orientation == HiveTeachpoint.TeachpointOrientation.Portrait ? LabwarePropertyNames.MaxPortraitGripperPos : LabwarePropertyNames.MaxLandscapeGripperPos].ToDouble();
                double grip_width =   Labware[ Teachpoint.Orientation == HiveTeachpoint.TeachpointOrientation.Portrait ? LabwarePropertyNames.MinPortraitGripperPos : LabwarePropertyNames.MinLandscapeGripperPos].ToDouble();

                // start at current position.
                IDictionary< IAxis, double> init_pos = Executor.Hardware.TechnosoftConnection.GetAxes().Values.ToDictionary< IAxis, IAxis, double>( axis => axis, axis => axis.GetPositionMM());
                hive_multi_axis_trajectory = new HiveMultiAxisTrajectory( Executor.Hardware, init_pos, new Dictionary< IAxis, double>());

                Stopwatch sw1 = Stopwatch.StartNew();

                // if not retracting only, then...
                if( !DoRetractOnly){
                    // if recovering from error with a replace, then move z to entry height.
                    if( DoPreMoveZPositioning){
                        hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MErrorRise, dst_z: UseKungFuMove ? kung_fu_z : entry_z_tool, acc_z: 500.0);
                        hive_multi_axis_trajectory.AddSeparator();
                    }
                    // retract to safe.
                    if( safe_y - current_y_tool < -0.5){
                        hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MRetract1, dst_y: safe_y);
                        hive_multi_axis_trajectory.AddSeparator();
                    }
                    // position x and z for entry.
                    if( UseKungFuMove){
                        hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MXZPosition, dst_x: Teachpoint.X, dst_z: kung_fu_z, pre_blend_distance: BlendingConstants.SafeZoneCornerBlend, post_blend_distance: BlendingConstants.GripperFullCornerBlend);
                        hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MXZPosition, dst_t: kung_fu_t, pre_blend_distance: Math.Abs( safe_t - kung_fu_t) /2.0);
                    } else{
                        hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MXZPosition, dst_x: Teachpoint.X, dst_z: entry_z_tool, pre_blend_distance: BlendingConstants.SafeZoneCornerBlend, post_blend_distance: BlendingConstants.GripperFullCornerBlend);
                    }
                    // extend y to teachpoint.
                    hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MExtend, dst_y: Teachpoint.Y, pre_blend_distance: BlendingConstants.SafeZoneCornerBlend, post_blend_distance: BlendingConstants.GripperFullCornerBlend);
                    // drop down to place.
                    hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MDrop, dst_z: place_z_tool, acc_z: 500.0, pre_blend_distance: BlendingConstants.GripperFullCornerBlend, post_blend_distance: BlendingConstants.GripBlend);
                    // place (unless we're terminating at the teachpoint).
                    if( Option != BlendedPlaceOption.BPOTerminateAtTeachpoint){
                        hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MPlace, dst_g: ungrip_width, pre_blend_distance: BlendingConstants.GripBlend, post_blend_distance: BlendingConstants.UngripBlend);
                    }
                }
                // if not NOT (i.e., if) retracting after place AND not terminating at teachpoint, then...
                if(( Option != BlendedPlaceOption.BPOTerminateAtTeachpoint) && ( Option != BlendedPlaceOption.BPONoRetractAfterPlace)){
                    // rise up to exit (if necessary).
                    hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MRise, dst_z: exit_z_tool, acc_z: 1500.0, pre_blend_distance: BlendingConstants.GripperEmptyCornerBlend, post_blend_distance: BlendingConstants.GripperEmptyCornerBlend);
                    // retract to safe.
                    if( UseKungFuMove){
                        hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MRetract2, dst_y: kung_fu_y, pre_blend_distance: BlendingConstants.GripperEmptyCornerBlend, post_blend_distance: 5.0);
                        hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MRetract2, dst_t: safe_t, pre_blend_distance: 1.0);
                    } else{
                        hive_multi_axis_trajectory.AddWaypoint( ( int)Marker.MRetract2, dst_y: safe_y, pre_blend_distance: BlendingConstants.GripperEmptyCornerBlend);
                    }
                }

                long elapsed = sw1.ElapsedMilliseconds;
                Log.DebugFormat( "Place waveform generation took {0}ms", elapsed);

                hive_multi_axis_trajectory.GeneratePVTPoints();

                elapsed = sw1.ElapsedMilliseconds - elapsed;
                Log.DebugFormat( "Place point generation took {0}ms", elapsed);

                IAxis.ExecuteCoordinatedPVTTrajectory( ( byte)6, hive_multi_axis_trajectory);

                Fire( Trigger.Success);
            } catch( PVTSetupRejectedException psre){
                StandardRetry( psre);
            } catch( Exception ex){
                Marker marker_during_which_move_died = ( Marker)hive_multi_axis_trajectory.MarkerDuringWhichMoveDied();
                if( marker_during_which_move_died > Marker.MRetract1){
                    DoPreMoveZPositioning = true;
                }
                if( marker_during_which_move_died > Marker.MPlace){
                    DoRetractOnly = true;
                }
                StandardRetry( ex);
            }
        }
    }
}
