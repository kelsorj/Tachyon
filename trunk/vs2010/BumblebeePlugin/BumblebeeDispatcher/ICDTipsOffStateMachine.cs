using System;
using BioNex.BumblebeePlugin.Hardware;
using BioNex.Shared.LabwareDatabase;

namespace BioNex.BumblebeePlugin.Dispatcher
{
    public abstract class ICDTipOffToWasteStateMachine : ICDChannelStateMachine< ICDTipOffToWasteStateMachine.State, ICDTipOffToWasteStateMachine.Trigger>
    {
        // ----------------------------------------------------------------------
        // enumerations.
        // ----------------------------------------------------------------------
        public enum State
        {
            Start,
            ApproachFork, ApproachForkError,
            RemoveTip, RemoveTipError,
            DisableChannel, DisableChannelError,
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
            DisableChannel,
        }

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        protected double XFork { get; private set; }
        protected double ZFork { get; private set; }

        protected int RemoveTipTries { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        protected ICDTipOffToWasteStateMachine(ICDParameterBundle parameter_bundle, ICDEventBundle event_bundle, DispatcherJob job, Channel channel, double x_fork, double z_fork)
            : base(parameter_bundle, event_bundle, job, channel)
        {
            XFork = x_fork;
            ZFork = z_fork;

            RemoveTipTries = 0;

            InitializeStates();
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        private void InitializeStates()
        {
            ConfigureState( State.Start, NullStateFunction, State.ApproachFork);
            ConfigureState( State.ApproachFork, ApproachFork, State.RemoveTip, State.ApproachForkError);
            ConfigureState( State.RemoveTip, RemoveTip, State.End, State.RemoveTipError)
                .PermitReentry( Trigger.Retry);
            ConfigureState( State.End, EndStateFunction);
            ConfigureState( State.DisableChannel, DisableChannelStateFunction, State.Abort, State.DisableChannelError);
            ConfigureState( State.Abort, AbortedStateFunction);
        }

        // ----------------------------------------------------------------------
        // state functions.
        // ----------------------------------------------------------------------
        protected virtual void ApproachFork()
        {
            Console.WriteLine( "{0}.{1}", GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);
            FakeOperation( 3000, 5000);
            Fire( Trigger.Success);
        }
        // ----------------------------------------------------------------------
        protected virtual void RemoveTip()
        {
            Console.WriteLine( "{0}.{1}", GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);
            FakeOperation( 1000, 1000);
            Fire( Trigger.Success);
        }
        // ----------------------------------------------------------------------
        protected override void EndStateFunction()
        {
            ParameterBundle.Messenger.Send( new TipOffCompleteMessage( Channel));
            base.EndStateFunction();
        }
    }

    public class ICDZAxisTipOffToWasteStateMachine : ICDTipOffToWasteStateMachine
    {
        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public ICDZAxisTipOffToWasteStateMachine(ICDParameterBundle parameter_bundle, ICDEventBundle event_bundle, DispatcherJob job, Channel channel, double x_fork, double z_fork)
            : base(parameter_bundle, event_bundle, job, channel, x_fork, z_fork)
        {
        }

        // ----------------------------------------------------------------------
        // state functions.
        // ----------------------------------------------------------------------
        protected override void ApproachFork()
        {
            try{
                AutoRetry( () => Channel.MoveToTopLimit(), "Move to top limit");
                double x_approach = XFork - 20.0;
                AutoRetry( () => { Channel.MoveAbsoluteBlendedXZOffset( x_approach, ZFork, 0.0, false, false);
                                   Channel.WaitForMoveAbsoluteBlendedComplete(); }, "Approach fork");
                Fire( Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        protected override void RemoveTip()
        {
            RemoveTipTries++;
            // during this state function, use temporary current limit of max_amps.
            const double max_amps = 5.5;
            double old_SATS_amps = 0.0;
            try{
                // get old current limit and set temporary current limit of max_amps.
                Channel.ZAxis.GetCurrentAmpsCmdLimit( out old_SATS_amps);
                Channel.ZAxis.SetCurrentAmpsCmdLimit( max_amps);
                if( !Channel.VerifyZPosition( ZFork, 0.0, 0.5)){
                    Channel.MoveAbsoluteZOffset( ZFork, 0.0, true, false);
                }
                if( !Channel.VerifyXPosition( XFork, 0.5)){
                    Channel.XAxis.MoveAbsolute( XFork);
                }
                Channel.MoveToTopLimit( use_trap: true, acceleration: ParameterBundle.Configuration.ZTipShuckAcceleration, ignore_speed_limits: true);
                Fire( Trigger.Success);
            } catch( Exception ex){
                if( RemoveTipTries < 3){
                    Fire( Trigger.Retry);
                } else{
                    StandardRetry( ex);
                }
            } finally{
                // revert to old current limit.
                Channel.ZAxis.SetCurrentAmpsCmdLimit( old_SATS_amps);
            }
        }
    }

    public class ICDWAxisTipOffToWasteStateMachine : ICDTipOffToWasteStateMachine
    {
        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public ICDWAxisTipOffToWasteStateMachine(ICDParameterBundle parameter_bundle, ICDEventBundle event_bundle, DispatcherJob job, Channel channel, double x_fork, double z_fork)
            : base(parameter_bundle, event_bundle, job, channel, x_fork, z_fork)
        {
        }

        // ----------------------------------------------------------------------
        // state functions.
        // ----------------------------------------------------------------------
        protected override void ApproachFork()
        {
            try{
                AutoRetry( () => Channel.MoveToTopLimit(), "Move to top limit");
                double x_approach = XFork - 20.0;
                AutoRetry( () => { Channel.MoveAbsoluteBlendedXZOffset( x_approach, ZFork, 0.0, false, false);
                                   Channel.WaitForMoveAbsoluteBlendedComplete(); }, "Approach fork");
                Fire( Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        protected override void RemoveTip()
        {
            try{
                // shuck off the tip by moving to mechanical zero.
                Channel.WAxis.MoveToZero();
                // move to liquid-handling zero.
                Channel.MoveWToAbsoluteUl( 0.0, true);
                Fire( Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
    }

    public class ICDTipOffToCarrierPreMoveStateMachine : ICDChannelStateMachine< ICDTipOffToCarrierPreMoveStateMachine.State, ICDTipOffToCarrierPreMoveStateMachine.Trigger>
    {
        // ----------------------------------------------------------------------
        // enumerations.
        // ----------------------------------------------------------------------
        public enum State
        {
            Start,
            ApproachTipCarrier, ApproachTipCarrierError,
            DisableChannel, DisableChannelError,
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
            DisableChannel,
        }

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        private double ZOrigin { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public ICDTipOffToCarrierPreMoveStateMachine(ICDParameterBundle parameter_bundle, ICDEventBundle event_bundle, DispatcherJob job, Channel channel, double z_origin)
            : base(parameter_bundle, event_bundle, job, channel)
        {
            ZOrigin = z_origin;

            InitializeStates();
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        private void InitializeStates()
        {
            ConfigureState( State.Start, NullStateFunction, State.ApproachTipCarrier);
            ConfigureState( State.ApproachTipCarrier, ApproachTipCarrier, State.End, State.ApproachTipCarrierError);
            ConfigureState( State.End, EndStateFunction);
            ConfigureState( State.DisableChannel, DisableChannelStateFunction, State.Abort, State.DisableChannelError);
            ConfigureState( State.Abort, AbortedStateFunction);
        }

        // ----------------------------------------------------------------------
        // state functions.
        // ----------------------------------------------------------------------
        private void ApproachTipCarrier()
        {
            Tuple< double, double> xy_position = Channel.TipWell.GetXYPosition( Channel.ID);

            try{
                AutoRetry( () => Channel.MoveToTopLimit(), "Move to top limit");
                double z_above_well_offset = ParameterBundle.Configuration.GetZChannelWithTipAboveTipShuttleWithTipsOffset();
                AutoRetry( () => { Channel.MoveAbsoluteBlendedXZOffset( xy_position.Item1, ZOrigin, z_above_well_offset, false, false);
                                   Channel.WaitForMoveAbsoluteBlendedComplete(); }, "Approach tip carrier");
                Fire( Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        protected override void EndStateFunction()
        {
            ParameterBundle.Messenger.Send( new TipOffPreMoveCompleteMessage( Channel));
            base.EndStateFunction();
        }
    }

    public class ICDTipOffToCarrierPostMoveStateMachine : ICDChannelStateMachine< ICDTipOffToCarrierPostMoveStateMachine.State, ICDTipOffToCarrierPostMoveStateMachine.Trigger>
    {
        // ----------------------------------------------------------------------
        // enumerations.
        // ----------------------------------------------------------------------
        public enum State
        {
            Start,
            VerifyTipCarrierApproach, VerifyTipCarrierApproachError,
            MoveToTopLimit, MoveToTopLimitError,
            OnStartCritical,
            ApproachDropoff, ApproachDropoffError,
            RemoveTip, RemoveTipError,
            OnFinishCritical,
            DisableChannel, DisableChannelError,
            End, Abort,
        }
        // ----------------------------------------------------------------------
        public enum Trigger
        {
            Success,
            GoToMoveToTopLimit,
            Failure,
            Retry,
            Ignore,
            Abort,
            DisableChannel,
        }

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        private double XCoordinate { get; set; }
        private double ZOrigin { get; set; }
        private TipBox TipBox { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public ICDTipOffToCarrierPostMoveStateMachine(ICDParameterBundle parameter_bundle, ICDEventBundle event_bundle, DispatcherJob job, Channel channel, double x_coordinate, double z_origin, TipBox tip_box)
            : base(parameter_bundle, event_bundle, job, channel)
        {
            XCoordinate = x_coordinate;
            ZOrigin = z_origin;
            TipBox = tip_box;

            InitializeStates();
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        private void InitializeStates()
        {
            ConfigureState( State.Start, NullStateFunction, State.VerifyTipCarrierApproach);
            ConfigureState( State.VerifyTipCarrierApproach, VerifyTipCarrierApproach, State.OnStartCritical, State.VerifyTipCarrierApproachError)
                .Permit( Trigger.GoToMoveToTopLimit, State.MoveToTopLimit);
            ConfigureState( State.MoveToTopLimit, MoveToTopLimit, State.OnStartCritical, State.MoveToTopLimitError);
            ConfigureState( State.OnStartCritical, OnStartCritical, State.ApproachDropoff);
            ConfigureState( State.ApproachDropoff, ApproachDropoff, State.RemoveTip, State.ApproachDropoffError);
            ConfigureState( State.RemoveTip, RemoveTip, State.OnFinishCritical, State.RemoveTipError)
                .PermitReentry( Trigger.Retry);
            ConfigureState( State.OnFinishCritical, OnFinishCritical, State.End);
            ConfigureState( State.End, EndStateFunction);
            ConfigureState( State.DisableChannel, DisableChannelStateFunction, State.Abort, State.DisableChannelError);
            ConfigureState( State.Abort, AbortedStateFunction);
        }

        // ----------------------------------------------------------------------
        // state functions.
        // ----------------------------------------------------------------------
        private void VerifyTipCarrierApproach()
        {
            try{
                Fire( Channel.VerifyXPosition( XCoordinate, 0.5) ? Trigger.Success : Trigger.GoToMoveToTopLimit);
            }  catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void MoveToTopLimit()
        {
            try{
                AutoRetry( () => Channel.MoveToTopLimit(), "Move to top limit");
                Fire( Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void ApproachDropoff()
        {
            try{
                double z_tip_inserted_offset = ParameterBundle.Configuration.GetZChannelWithTipInsertedInTipNestOffset( TipBox != null) + 0.5 /* buffer space */;
                AutoRetry( () => { Channel.MoveWToAbsoluteUl( 0.0, false);
                                   Channel.MoveAbsoluteBlendedXZOffset( XCoordinate, ZOrigin, z_tip_inserted_offset, false, false);
                                   Channel.WaitForMoveAbsoluteBlendedComplete();
                                   Channel.MoveWToAbsoluteUl( 0.0, true); }, "Approach dropoff");
                Fire( Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void RemoveTip()
        {
            double z_tip_inserted_offset = ParameterBundle.Configuration.GetZChannelWithTipInsertedInTipNestOffset( TipBox != null) + 0.5 /* buffer space */;
            double z_tip_ejected_offset = z_tip_inserted_offset + 5.0 /* shuck length */;
            double z_press_ready_offset = ParameterBundle.Configuration.GetZChannelWithoutTipReadyForTipPress( TipBox != null);
            try{
                // borrowing "AspirateOrDispenseNew" for coordinated W shuck.
                Channel.AspirateOrDispenseNew(false, Channel.WAxis.ConvertMmToUl(-5.0), z_tip_ejected_offset, 0, z_tip_inserted_offset, Channel.WAxis.ConvertMmToUl(Channel.WAxis.Settings.Velocity - 0.000001 /* epsilon */), 1.0, ZOrigin, true);
                // pull W back to liquid-handling zero.
                Channel.MoveWToAbsoluteUl( 0.0, false);
                // pull Z back to above well offset (in preparation for tip press).
                Channel.MoveAbsoluteZOffset( ZOrigin, z_press_ready_offset, true, false);
                // announce that the tip has been shucked off.
                ParameterBundle.Messenger.Send( new TipOffCompleteMessage( Channel));
                Fire( Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
    }
}
