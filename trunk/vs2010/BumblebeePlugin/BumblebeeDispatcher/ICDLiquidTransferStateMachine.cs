using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BioNex.BumblebeePlugin.Hardware;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.Utils;

namespace BioNex.BumblebeePlugin.Dispatcher
{
    public abstract class ICDLiquidTransferStateMachine : ICDChannelStateMachine< ICDLiquidTransferStateMachine.State, ICDLiquidTransferStateMachine.Trigger>
    {
        // constants.
        public enum State
        {
            Start,
            ApproachWell, ApproachWellError,
            OnStartCritical,
            PierceLiquid, PierceLiquidError,
            TransferLiquid, TransferLiquidError,
            UnpierceLiquid, UnpierceLiquidError,
            OnFinishCritical,
            LeaveWell, LeaveWellError,
            DisableChannel, DisableChannelError,
            End, Abort,
        }

        public enum Trigger
        {
            Success,
            Failure,
            Retry,
            Ignore,
            Abort,
            DisableChannel,
        }

        // properties.
        protected Transfer Transfer { get; private set; }
        protected ILabware LabwareType { get; private set; }
        protected ILiquidProfile LiquidProfile { get; private set; }
        protected ILiquidProfile MixLiquidProfile { get; private set; }
        protected double XCoordinate { get; private set; }
        protected double ZOrigin { get; private set; }
        protected double ZAbovePlates { get; private set; }

        protected double ZAboveWell { get; private set; }

        protected double ZStartLiquidTransfer { get; set; }
        protected double ZEndLiquidTransfer { get; set; }
        protected double WStartLiquidTransfer { get; set; }
        protected double WEndLiquidTransfer { get; set; }

        protected long MixCycles { get; set; }
        protected double ZMix { get; set; }
        protected double WMix { get; set; }

        // constructors.
        protected ICDLiquidTransferStateMachine(ICDParameterBundle parameter_bundle, ICDEventBundle event_bundle, DispatcherJob job, Channel channel, Transfer transfer, ILabware labware_type, ILiquidProfile liquid_profile, ILiquidProfile mix_liquid_profile, double x_coordinate, double z_origin, double z_above_plates, double distance_from_well_bottom, double volume)
            : base(parameter_bundle, event_bundle, job, channel)
        {
            Transfer = transfer;
            LabwareType = labware_type;
            LiquidProfile = liquid_profile;
            MixLiquidProfile = ( mix_liquid_profile != null && mix_liquid_profile.IsMixingProfile ? mix_liquid_profile : null);
            XCoordinate = x_coordinate;
            ZOrigin = z_origin;
            ZAbovePlates = z_above_plates;

            ZAboveWell = LabwareType[ LabwarePropertyNames.Thickness].ToDouble() + 5.0;

            InitializeStates();
        }

        // methods.
        private void InitializeStates()
        {
            ConfigureState( State.Start, NullStateFunction, State.ApproachWell);
            ConfigureState( State.ApproachWell, ApproachWell, State.OnStartCritical, State.ApproachWellError);
            ConfigureState( State.OnStartCritical, OnStartCritical, State.PierceLiquid);
            ConfigureState( State.PierceLiquid, PierceLiquid, State.TransferLiquid, State.PierceLiquidError);
            ConfigureState( State.TransferLiquid, TransferLiquid, State.UnpierceLiquid, State.TransferLiquidError);
            ConfigureState( State.UnpierceLiquid, UnpierceLiquid, State.OnFinishCritical, State.UnpierceLiquidError);
            ConfigureState( State.OnFinishCritical, OnFinishCritical, State.LeaveWell);
            ConfigureState( State.LeaveWell, LeaveWell, State.End, State.LeaveWellError);
            ConfigureState( State.End, EndStateFunction);
            ConfigureState( State.DisableChannel, DisableChannelStateFunction, State.Abort, State.DisableChannelError);
            ConfigureState( State.Abort, AbortedStateFunction);
        }

        // state functions.
        protected virtual void ApproachWell()
        {
            Console.WriteLine( "{0}.{1}", GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);
            FakeOperation( 1000, 1500);
            Fire( Trigger.Success);
        }

        protected virtual void PierceLiquid()
        {
            Console.WriteLine( "{0}.{1}", GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);
            FakeOperation( 500, 750);
            Fire( Trigger.Success);
        }

        protected virtual void TransferLiquid()
        {
            Console.WriteLine( "{0}.{1}", GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);
            FakeOperation( 1000, 1500);
            Fire( Trigger.Success);
        }

        protected virtual void UnpierceLiquid()
        {
            Console.WriteLine( "{0}.{1}", GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);
            FakeOperation( 500, 750);
            Fire( Trigger.Success);
        }

        protected virtual void LeaveWell()
        {
            FakeOperation( 1000, 1000);
            Fire( Trigger.Success);
        }
    }

    public abstract class ICDLiquidTransferStateMachine2 : ICDLiquidTransferStateMachine
    {
        // properties.
        private bool PVTTechnologyDemo { get; set; }

        // constructors.
        protected ICDLiquidTransferStateMachine2(ICDParameterBundle parameter_bundle, ICDEventBundle event_bundle, DispatcherJob job, Channel channel, Transfer transfer, ILabware labware_type, ILiquidProfile liquid_profile, ILiquidProfile mix_liquid_profile, double x_coordinate, double z_origin, double z_above_plates, double distance_from_well_bottom, double volume)
            : base(parameter_bundle, event_bundle, job, channel, transfer, labware_type, liquid_profile, mix_liquid_profile, x_coordinate, z_origin, z_above_plates, distance_from_well_bottom, volume)
        {
            PVTTechnologyDemo = false;
        }

        // methods.
        protected virtual void PreApproachWell() {}
        protected virtual void PostApproachWell() {}
        protected virtual double GetZPierceDst() { return ZStartLiquidTransfer; }
        protected virtual double GetZUnpierceSrc() { return ZEndLiquidTransfer; }

        private static double RampUpTime( double vi, double vf, double j)
        {
            return Math.Sqrt( Math.Abs( ( vf - vi) / j));
        }

        private double DistanceRequiredToAccelerate( double vi, double vf, double a, double j)
        {
            if( Math.Sqrt( Math.Abs( ( vf - vi) / j)) <= Math.Abs( a / j)){
                return ( vi + vf) * RampUpTime( vi, vf, j);
            } else{
                return ( ( vi + vf) / 2 * ( a / j)) + ( vf * vf - vi * vi) / ( 2 * a);
            }
        }

        private const double NearTopXBlend = 10.0;
        private const double NearTopZBlend = 2.5;
        private const double PreStartPierceZBlend = 2.5;
        private const double PreStartLeaveZBlend = 2.5;

        private void BlendedLiquidHandling()
        {
            IAxis x_axis = Channel.XAxis;
            IAxis z_axis = Channel.ZAxis;
            IAxis w_axis = Channel.WAxis.UseSparingly();

            double w_start_liquid_transfer_mm = w_axis.ConvertUlToMm( WStartLiquidTransfer);
            double w_end_liquid_transfer_mm = w_axis.ConvertUlToMm( WEndLiquidTransfer);
            double w_mix_mm = w_axis.ConvertUlToMm( WMix);
            const bool aspirate_true_or_dispense_false = true;
            double w_velocity = w_axis.ConvertUlToMm( aspirate_true_or_dispense_false ? LiquidProfile.RateToAspirate : LiquidProfile.RateToDispense);
            double w_accel_factor = Math.Max( 0.01, Math.Min( 1.0, aspirate_true_or_dispense_false ? LiquidProfile.MaxAccelDuringAspirate : LiquidProfile.MaxAccelDuringDispense));
            double w_acceleration = w_accel_factor * w_axis.Settings.Acceleration;

            IDictionary< IAxis, double> initial_position = new[]{ x_axis, z_axis, w_axis}.ToDictionary( axis => axis, axis => axis.GetPositionMM());
            MultiAxisTrajectory multi_axis_trajectory = new MultiAxisTrajectory( initial_position, new Dictionary< IAxis, double>(), 0.001);
            // add points here.
            // pre-aspirate (!!but no pre-aspirate for dispenses!!).
            // move to top.
            multi_axis_trajectory.AddWaypoint( 0, new List< MultiAxisTrajectory.WaycoordinateInfo>{ new MultiAxisTrajectory.WaycoordinateInfo( z_axis, Channel.GetAbsoluteZ( ZOrigin, ZAbovePlates), double.NaN, double.NaN),
                                                                                                    new MultiAxisTrajectory.WaycoordinateInfo( w_axis, w_start_liquid_transfer_mm, double.NaN, double.NaN)}, post_blend_distance: NearTopZBlend);
            // move over well.
            multi_axis_trajectory.AddWaypoint( 1, new List< MultiAxisTrajectory.WaycoordinateInfo>{ new MultiAxisTrajectory.WaycoordinateInfo( x_axis, XCoordinate, double.NaN, double.NaN)}, pre_blend_distance: NearTopXBlend, post_blend_distance: NearTopXBlend);

            bool stage_available_for_use = EventBundle.WaitBeforeStart.WaitOne( 0);
            if( !stage_available_for_use){
                multi_axis_trajectory.AddWaypoint( 2, new List< MultiAxisTrajectory.WaycoordinateInfo>{ new MultiAxisTrajectory.WaycoordinateInfo( z_axis, Channel.GetAbsoluteZ( ZOrigin, ZAboveWell), double.NaN, double.NaN)}, pre_blend_distance: NearTopZBlend);
                return;
            }
            // approach well.
            // pierce liquid.
            double z_full_velocity = z_axis.Settings.Velocity;
            double z_pierce_dst = GetZPierceDst();
            double z_pierce_distance = Math.Abs( z_pierce_dst - ZAboveWell);
            double z_desired_pierce_velocity = z_pierce_distance / LiquidProfile.TimeToEnterLiquid;
            bool pierce_liquid_at_full_velocity = ( z_desired_pierce_velocity >= z_full_velocity);
            if( pierce_liquid_at_full_velocity){
                multi_axis_trajectory.AddWaypoint( 3, new List< MultiAxisTrajectory.WaycoordinateInfo>{ new MultiAxisTrajectory.WaycoordinateInfo( z_axis, Channel.GetAbsoluteZ( ZOrigin, z_pierce_dst), double.NaN, double.NaN)}, pre_blend_distance: NearTopZBlend, post_blend_distance: 0.0);
            } else{
                multi_axis_trajectory.AddWaypoint( 2, new List< MultiAxisTrajectory.WaycoordinateInfo>{ new MultiAxisTrajectory.WaycoordinateInfo( z_axis, Channel.GetAbsoluteZ( ZOrigin, ZAboveWell), z_desired_pierce_velocity, double.NaN)}, pre_blend_distance: NearTopZBlend, post_blend_distance: 0.0);
                multi_axis_trajectory.AddWaypoint( 3, new List< MultiAxisTrajectory.WaycoordinateInfo>{ new MultiAxisTrajectory.WaycoordinateInfo( z_axis, Channel.GetAbsoluteZ( ZOrigin, z_pierce_dst), double.NaN, double.NaN)}, pre_blend_distance: PreStartPierceZBlend, post_blend_distance: 0.0);
            }
            // pre-aspirate mix.
            // fill this out later.
            // perform liquid transfer.
            multi_axis_trajectory.AddWaypoint( 4, new List< MultiAxisTrajectory.WaycoordinateInfo>{ new MultiAxisTrajectory.WaycoordinateInfo( z_axis, Channel.GetAbsoluteZ( ZOrigin, ZEndLiquidTransfer), double.NaN, double.NaN),
                                                                                                    new MultiAxisTrajectory.WaycoordinateInfo( w_axis, w_end_liquid_transfer_mm, w_velocity, w_acceleration)}, pre_blend_distance: 0.1, post_blend_distance: 0.1);
            // post-dispense mix.
            // fill this out later.
            // unpierce liquid.
            double z_unpierce_src = GetZUnpierceSrc();
            double z_unpierce_distance = Math.Abs( ZAboveWell - z_unpierce_src);
            double z_desired_unpierce_velocity = z_unpierce_distance / LiquidProfile.TimeToExitLiquid;
            bool unpierce_liquid_at_full_velocity = ( z_desired_unpierce_velocity >= z_full_velocity);
            if( unpierce_liquid_at_full_velocity){
                multi_axis_trajectory.AddWaypoint( 5, new List< MultiAxisTrajectory.WaycoordinateInfo>{ new MultiAxisTrajectory.WaycoordinateInfo( z_axis, Channel.GetAbsoluteZ( ZOrigin, ZAbovePlates), double.NaN, double.NaN)}, pre_blend_distance: 0.0);
            } else{
                multi_axis_trajectory.AddWaypoint( 5, new List< MultiAxisTrajectory.WaycoordinateInfo>{ new MultiAxisTrajectory.WaycoordinateInfo( z_axis, Channel.GetAbsoluteZ( ZOrigin, ZAboveWell), z_desired_unpierce_velocity, double.NaN)}, pre_blend_distance: 0.0, post_blend_distance: 0.0);
                multi_axis_trajectory.AddWaypoint( 6, new List< MultiAxisTrajectory.WaycoordinateInfo>{ new MultiAxisTrajectory.WaycoordinateInfo( z_axis, Channel.GetAbsoluteZ( ZOrigin, ZAbovePlates), double.NaN, double.NaN)}, pre_blend_distance: PreStartLeaveZBlend);
            }

            multi_axis_trajectory.GeneratePVTPoints();
            Debug.Write( multi_axis_trajectory.ToString());
            IAxis.ExecuteCoordinatedPVTTrajectory( ( byte)( Channel.ID + 1), multi_axis_trajectory);
            /*
            PreApproachWell();
            Channel.MoveToTopLimit();
            // bool stage_available_for_use = StageAvailableForUse.WaitOne( 0);
            StageAvailableForUse.WaitOne();
            double z = ZOrigin;
            // z = ZTop;
            z = ZAboveWell;
            z = ZStartLiquidTransfer;
            z = ZEndLiquidTransfer;

            IAxis z_axis = Channel.GetZ();
            IAxis w_axis = Channel.GetW();
            Trajectory z_trajectory = new Trajectory();
            Trajectory w_trajectory = new Trajectory();
            
            // displacements.
            double seg0_d = 0;
            double seg1_d = ( ZOrigin - ZAboveWell) - ( 0);
            double seg2_d = ( ZOrigin - ZStartLiquidTransfer) - ( ZOrigin - ZAboveWell);
            double seg3_d = ( ZOrigin - ZEndLiquidTransfer) - ( ZOrigin - ZStartLiquidTransfer);
            double seg4_d = ( ZOrigin - ZAboveWell) - ( ZOrigin - ZEndLiquidTransfer);
            double seg5_d = ( 0) - ( ZOrigin - ZAboveWell);

            // speeds.
            double seg0_s = 0;
            double seg1_s = z_axis.Settings.Velocity;
            double seg2_s = Math.Abs( ZAboveWell - ZStartLiquidTransfer) / LiquidProfile.TimeToEnterLiquid;
            double seg3_s = ( this as ICDAspirateStateMachine2 != null) ? LiquidProfile.RateToAspirate : LiquidProfile.RateToDispense; // this is a W speed being expressed.
            double seg4_s = Math.Abs( ZEndLiquidTransfer - ZAboveWell) / LiquidProfile.TimeToEnterLiquid;
            double seg5_s = z_axis.Settings.Velocity;
            
            // distances required to accelerate.
            double j = 0;
            double a = 0;
            double s01_d = DistanceRequiredToAccelerate( 0, seg1_s, a, j);
            double s12_d = DistanceRequiredToAccelerate( seg1_s, seg2_s, a, j);
            double s23_d = DistanceRequiredToAccelerate( seg2_s, seg3_s, a, j);
            double s34_d = DistanceRequiredToAccelerate( seg3_s, seg4_s, a, j);
            double s45_d = DistanceRequiredToAccelerate( seg4_s, seg5_s, a, j);
            double s56_d = DistanceRequiredToAccelerate( seg5_s, 0, a, j);

            TrajectoryPoint z_trajectory_pt = null;
            TrajectoryPoint w_trajectory_pt = null;
            z_trajectory.Enqueue( z_trajectory_pt);
            w_trajectory.Enqueue( w_trajectory_pt);

            MultiAxisTrajectory trajectory = new MultiAxisTrajectory();
            trajectory.Add( z_axis, z_trajectory);
            trajectory.Add( w_axis, w_trajectory);
            IAxis.ExecuteCoordinatedPVTTrajectory( 2, trajectory, true);
            StageUseCountdown.Signal();
            */
        }

        // state functions.
        protected override void ApproachWell()
        {
            if( PVTTechnologyDemo){
                BlendedLiquidHandling();
            }

            // DKM 2011-10-21 we have intermittent issues with Z axis errors, so I added auto-retry here.
            //                I have separated the preaspirate from the move to top limit and the blended
            //                move.  Since 99% of the errors come from the blended move, that's the critical
            //                one to add auto-retry to, but I ended up using it for move to top limit as well.
            
            try{
                PreApproachWell();
                AutoRetry( () => Channel.MoveToTopLimit(), "Move to top limit");
                bool stage_available_for_use = EventBundle.WaitBeforeStart.WaitOne( 0);
                double full_velocity = Channel.ZAxis.Settings.Velocity;
                double desired_velocity = Math.Abs( ZAboveWell - ZEndLiquidTransfer) / LiquidProfile.TimeToEnterLiquid;
                bool pierce_liquid_at_full_velocity = ( desired_velocity >= full_velocity);
                AutoRetry( () => {
                    if( true){
                        if( stage_available_for_use && pierce_liquid_at_full_velocity){
                            // move channel straight into the wells.
                            Channel.MoveAbsoluteBlendedXZOffset( XCoordinate, ZOrigin, ZStartLiquidTransfer, false, false);
                            Channel.WaitForMoveAbsoluteBlendedComplete();
                        } else{
                            // move channel above the wells.
                            Channel.MoveAbsoluteBlendedXZOffset( XCoordinate, ZOrigin, ZAboveWell, false, false);
                            Channel.WaitForMoveAbsoluteBlendedComplete();
                        }
                    } /* else{ -- unreachable
                        Channel.GetX().MoveAbsolute( XCoordinate, false);
                        while( !Channel.VerifyXPosition( XCoordinate, 2.5)){
                            Thread.Sleep( 10);
                        }
                        Channel.MoveAbsoluteZOffset( ZOrigin, ZAboveWell, false, true);
                        while( !Channel.VerifyXPosition( XCoordinate, 0.1)){
                            Thread.Sleep( 10);
                        }
                    } */
                }, "Move to enter well");
                PostApproachWell();
                Fire( Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }

        protected override void PierceLiquid()
        {
            try{
                double z_pierce_dst = GetZPierceDst();
                double velocity = Math.Abs( ZAboveWell - z_pierce_dst) / LiquidProfile.TimeToEnterLiquid;
                Channel.MoveAbsoluteZOffset( ZOrigin, z_pierce_dst, velocity, true, true, false);
                Fire( Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }

        protected override void UnpierceLiquid()
        {
            try{
                double full_velocity = Channel.ZAxis.Settings.Velocity;
                double z_unpierce_src = GetZUnpierceSrc();
                double desired_velocity = Math.Abs( ZAboveWell - z_unpierce_src) / LiquidProfile.TimeToExitLiquid;
                bool unpierce_liquid_at_full_velocity = ( desired_velocity >= full_velocity);
                if( true){
                    if( unpierce_liquid_at_full_velocity){
                        Channel.MoveToTopLimit();
                    } else{
                        Channel.MoveAbsoluteZOffset( ZOrigin, ZAboveWell, desired_velocity, true, true, false);
                    }
                } /* else{ -- unreachable
                    if( unpierce_liquid_at_full_velocity){
                        Channel.MoveToTopLimit();
                    } else{
                        Channel.MoveAbsoluteZOffset( ZOrigin, ZAboveWell, desired_velocity, true, false, true);
                        while( !Channel.VerifyZPosition( ZOrigin, ZAboveWell, 5.0)){
                            Thread.Sleep( 10);
                        }
                        Channel.MoveToTopLimit( use_trap: true);
                    }
                } */
                Fire( Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }

        protected override void LeaveWell()
        {
            try{
                Channel.MoveToTopLimit();
                Fire( Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }

        protected void AspirateOrDispense( ILiquidProfile liquid_profile, bool aspirate_true_or_dispense_false, double w_position_dst/* absolute position */, double z_position_dst/* absolute positions */, double w_position_src, double z_position_src)
        {
            double w_velocity = aspirate_true_or_dispense_false ? liquid_profile.RateToAspirate : liquid_profile.RateToDispense;
            double w_accel_factor = Math.Max( 0.01, Math.Min( 1.0, aspirate_true_or_dispense_false ? liquid_profile.MaxAccelDuringAspirate : liquid_profile.MaxAccelDuringDispense));
            int delay_ms = ( int)( aspirate_true_or_dispense_false ? liquid_profile.PostAspirateDelay : liquid_profile.PostDispenseDelay * 1000);
            Channel.AspirateOrDispenseNew( aspirate_true_or_dispense_false, w_position_dst, z_position_dst, w_position_src, z_position_src, w_velocity, w_accel_factor, ZOrigin, true);
            TimeSpan delay_interval = new TimeSpan( 0, 0, 0, 0, delay_ms);
            Thread.Sleep( delay_interval);
        }
    }
}
/*
private delegate void ScriptRunnerDelegate( string script_path);

private void ScriptRunnerThread( string script_path)
{
    ScriptScope scope = PythonEngine.CreateScope();
    // in order to leverage the existing aspirate / dispense code for grouped channels,
    // just create a new grouped channels object here to wrap the channel before
    // passing into the scripting engine
    GroupedChannels group = new GroupedChannels( new List< Channel>{ Channel});
    double z_source_teachpoint = Teachpoints.GetStageTeachpoint( Channel.GetID(), Stage.GetID()).UpperLeft[ "z"];
    ChannelWrapper wrapper = new ChannelWrapper( group, src_labware_, SourceStage, z_source_teachpoint, LiquidProfileLibrary);
    scope.SetVariable( "channel", wrapper);
    ScriptSource source = PythonEngine.CreateScriptSourceFromFile( script_path.ToAbsoluteAppPath());
    CompiledCode code = source.Compile();
    try{
        code.Execute( scope);
    } catch( Exception ex){
        log_.Error( ex.Message);
    }
}

private void ScriptComplete( IAsyncResult iar)
{
    AsyncResult ar = ( AsyncResult)iar;
    ScriptRunnerDelegate caller = ( ScriptRunnerDelegate)ar.AsyncDelegate;
    try{
        caller.EndInvoke( iar);
    } catch( Exception ex){
        List< Exception> script_exceptions = ( List< Exception>)ar.AsyncState;
        script_exceptions.Add( ex);
    }
}
*/
