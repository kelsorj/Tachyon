using System;
using System.Collections.Generic;
using System.Diagnostics;
using BioNex.BumblebeePlugin.Hardware;
using BioNex.Shared.LabwareDatabase;

namespace BioNex.BumblebeePlugin.Dispatcher
{
    public abstract class ICDTipOnStateMachine : ICDChannelStateMachine< ICDTipOnStateMachine.State, ICDTipOnStateMachine.Trigger>
    {
        // ----------------------------------------------------------------------
        // enumerations.
        // ----------------------------------------------------------------------
        public enum State
        {
            Start,
            ApproachTip, ApproachTipError,
            OnStartCritical,
            AttachTip, AttachTipError,
            MoveToClear, MoveToClearError,
            OnFinishCritical,
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
        protected TipWell TipWell { get; private set; }
        protected double XCoodinate { get; private set; }
        protected double ZOrigin { get; private set; }
        protected TipBox TipBox { get; private set; }
        protected bool PositionPress { get; private set; }

        protected int ApproachTipAndAttachTipTries { get; set; }

        // ----------------------------------------------------------------------
        // class members.
        // ----------------------------------------------------------------------
        protected static readonly Object attach_tip_mutex_ = new Object();
        protected static readonly IDictionary< Object, Object> TipAttachMuti = new Dictionary< Object, Object>();

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        protected ICDTipOnStateMachine(ICDParameterBundle parameter_bundle, ICDEventBundle event_bundle, DispatcherJob job, Channel channel, TipWell tip_well, double x_coordinate, double z_origin, double z_above_plates, TipBox tip_box, bool position_press)
            : base(parameter_bundle, event_bundle, job, channel)
        {
            TipWell = tip_well;
            XCoodinate = x_coordinate;
            ZOrigin = z_origin;
            TipBox = tip_box;
            PositionPress = position_press;

            ApproachTipAndAttachTipTries = 0;

            InitializeStates();
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        private void InitializeStates()
        {
            ConfigureState( State.Start, NullStateFunction, State.ApproachTip);
            ConfigureState( State.ApproachTip, ApproachTip, State.OnStartCritical, State.ApproachTipError);
            ConfigureState( State.OnStartCritical, OnStartCritical, State.AttachTip);
            ConfigureState( State.AttachTip, AttachTip, State.MoveToClear, State.AttachTipError)
                .Permit( Trigger.Retry, State.ApproachTip);
            ConfigureState( State.MoveToClear, MoveToClear, State.OnFinishCritical, State.MoveToClearError);
            ConfigureState( State.OnFinishCritical, OnFinishCritical, State.End);
            ConfigureState( State.End, EndStateFunction);
            ConfigureState( State.DisableChannel, DisableChannelStateFunction, State.Abort, State.DisableChannelError);
            ConfigureState( State.Abort, AbortedStateFunction);
        }

        // ----------------------------------------------------------------------
        // state functions.
        // ----------------------------------------------------------------------
        protected virtual void ApproachTip()
        {
            Console.WriteLine( "{0}.{1}", GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);
            FakeOperation( 1000, 1500);
            Fire( Trigger.Success);
        }
        // ----------------------------------------------------------------------
        protected virtual void AttachTip()
        {
            Console.WriteLine( "{0}.{1}", GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);
            FakeOperation( 3000, 5000);
            Fire( Trigger.Success);
        }
        // ----------------------------------------------------------------------
        protected virtual void MoveToClear()
        {
            Console.WriteLine( "{0}.{1}", GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name);
            FakeOperation( 1000, 1000);
            Fire( Trigger.Success);
        }
        // ----------------------------------------------------------------------
        protected override void EndStateFunction()
        {
            // tell the tipbox that the tip has been used.
            ParameterBundle.Messenger.Send( new TipPressedOnMessage( TipWell));
            // tell the channel that the tip has been pressed on.
            ParameterBundle.Messenger.Send( new TipOnCompleteMessage( Channel));
            base.EndStateFunction();
        }
        // ----------------------------------------------------------------------
        protected override void AbortedStateFunction()
        {
            // tell the tipbox that the tip has been used.
            ParameterBundle.Messenger.Send( new TipPressedOnMessage( TipWell));
            base.AbortedStateFunction();
        }
    }

    public class ICDTipOnStateMachine2 : ICDTipOnStateMachine
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        private bool UsingTipBox { get; set; }
        private double XChannelSafe { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public ICDTipOnStateMachine2(ICDParameterBundle parameter_bundle, ICDEventBundle event_bundle, DispatcherJob job, Channel channel, TipWell tip_well, double x_coordinate, double z_origin, double z_above_plates, bool using_tipbox, TipBox tip_box, bool position_press, double x_channel_safe = double.NaN)
            : base(parameter_bundle, event_bundle, job, channel, tip_well, x_coordinate, z_origin, z_above_plates, tip_box, position_press)
        {
            UsingTipBox = using_tipbox;
            XChannelSafe = x_channel_safe;
        }

        // ----------------------------------------------------------------------
        // state functions.
        // ----------------------------------------------------------------------
        protected override void ApproachTip()
        {
            double z_press_ready_offset = ParameterBundle.Configuration.GetZChannelWithoutTipReadyForTipPress( UsingTipBox);
            try{
                Channel.MoveWToAbsoluteUl( 0.0, false);
                if( double.IsNaN( XChannelSafe) || Channel.XAxis.GetPositionMM() < XChannelSafe){
                    Channel.MoveToTopLimit();
                } else{
                    double z_press_ready_absolute = Channel.GetAbsoluteZ( ZOrigin, z_press_ready_offset);
                    double z_current_absolute = Channel.ZAxis.GetPositionMM();
                    if( z_current_absolute < ( z_press_ready_absolute - 0.5)){
                        Channel.MoveAbsoluteZOffset( ZOrigin, z_press_ready_offset, true, false);
                    }
                }
                Channel.MoveAbsoluteBlendedXZOffset( XCoodinate, ZOrigin, z_press_ready_offset, false, false);
                Channel.WaitForMoveAbsoluteBlendedComplete();
                Channel.MoveWToAbsoluteUl( 0.0, true);
                Fire( Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private Object GetAttachTipMutex( Channel channel)
        {
            if( Channel.TipWell == null){
                return attach_tip_mutex_;
            }
            TipShuttle tip_shuttle = ( Channel.TipWell.Carrier.Stage as TipShuttle);
            if( !TipAttachMuti.ContainsKey( tip_shuttle)){
                TipAttachMuti[ tip_shuttle] = new Object();
            }
            return TipAttachMuti[ tip_shuttle];
        }
        // ----------------------------------------------------------------------
        protected override void AttachTip()
        {
            ApproachTipAndAttachTipTries++;
            try{
                Stopwatch stopwatch = Stopwatch.StartNew();
                lock( GetAttachTipMutex( Channel)){
                    stopwatch.Stop();
                    Log.DebugFormat( "{0}[{1}]: Waited {2} for attach tip mutex", StateMachineType.Name, StateMachineNumber, stopwatch.Elapsed.ToString("G"));
                    double z_bottom_out_mm = Channel.TipOnHere( TipBox, ZOrigin, ParameterBundle.Configuration.GetZChannelWithTipInsertedInTipNestOffset( UsingTipBox), UsingTipBox ? ParameterBundle.Configuration.TipPressTipBoxAdditionalPush : ParameterBundle.Configuration.TipPressTipShuttleAdditionalPush, PositionPress);
                    Log.DebugFormat( "Attach tip bottomed out at {0}mm", z_bottom_out_mm);
                }
                Channel.MoveToTopLimit();
                Fire( Trigger.Success);
            } catch( Exception ex){
                if( ApproachTipAndAttachTipTries < 3){
                    Fire( Trigger.Retry);
                } else{
                    StandardRetry( ex);
                }
            }
        }
        // ----------------------------------------------------------------------
        protected override void MoveToClear()
        {
            try{
                Channel.MoveToTopLimit();
                Fire( Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
    }
}
