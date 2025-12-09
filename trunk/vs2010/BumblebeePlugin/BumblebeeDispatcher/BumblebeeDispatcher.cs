using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using BioNex.BumblebeeGUI;
using BioNex.BumblebeePlugin.Hardware;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.IError;
using BioNex.Shared.LabwareDatabase;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.StateMachineExecutor;
using BioNex.Shared.Teachpoints;
using BioNex.Shared.ThreadsafeMessenger;
using BioNex.Shared.Utils.WellMathUtil;
using log4net;

namespace BioNex.BumblebeePlugin.Dispatcher
{
    // --------------------------------------------------------------------------
    // DispatcherJobs:
    // --------------------------------------------------------------------------
    public class DispatcherJob
    {
    }
    // --------------------------------------------------------------------------
    public class TipJob : DispatcherJob
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public Channel Channel { get; private set; }
        public Stage Stage { get; private set; }
        public TipBox TipBox { get; private set; }
        public TipWell TipWell { get; private set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public TipJob( Channel channel, Stage stage, TipBox tip_box, TipWell tip_well)
        {
            Channel = channel;
            Stage = stage;
            TipBox = tip_box;
            TipWell = tip_well;
        }
    }
    // --------------------------------------------------------------------------
    public class DualTipsJob : DispatcherJob
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public IList< Channel> Channels { get; private set; }
        public Stage Stage { get; private set; }
        public TipBox TipBox { get; private set; }
        public IList< TipWell> TipWells { get; private set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public DualTipsJob( IList< Channel> channels, Stage stage, TipBox tip_box, IList< TipWell> tip_wells)
        {
            Channels = channels;
            Stage = stage;
            TipBox = tip_box;
            TipWells = tip_wells;
        }
    }
    // --------------------------------------------------------------------------
    public class TipOffJob : DispatcherJob
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public Channel Channel { get; private set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public TipOffJob( Channel channel)
        {
            Channel = channel;
        }
    }
    // --------------------------------------------------------------------------
    public class LiquidTransferJob : DispatcherJob
    {
        // stages.
        public Stage SrcStage { get; private set; }
        public Stage DstStage { get; private set; }
        // transfers.
        public IDictionary< Channel, Transfer> TransfersToPerform { get; private set; }

        public LiquidTransferJob( Stage src_stage, Stage dst_stage, IDictionary< Channel, Transfer> transfers_to_perform)
        {
            SrcStage = src_stage;
            DstStage = dst_stage;
            TransfersToPerform = transfers_to_perform;
        }
    }
    // --------------------------------------------------------------------------
    public class MoveStageJob : DispatcherJob
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public Stage Stage { get; private set; }
        public int Orientation { get; private set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public MoveStageJob( Stage stage, int orientation)
        {
            Stage = stage;
            Orientation = orientation;
        }
    }
    // --------------------------------------------------------------------------
    public class WashTipsJob : DispatcherJob
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public TipShuttle TipShuttle { get; private set; }
        public bool Debug { get; private set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public WashTipsJob( TipShuttle tip_shuttle, bool debug = false)
        {
            TipShuttle = tip_shuttle;
            Debug = debug;
        }
    }
    // --------------------------------------------------------------------------
    /* not used -- used for "batched" tip on/tips off, allowing at most one tip on and as many tips off as possible.
    public class WashableTipsJob : DispatcherJob
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public ILabwareDatabase LabwareDatabase { get; private set; } // FYC try to get rid of.
        public TipShuttle TipShuttle { get; private set; }
        public Channel PressingChannel { get; private set; }
        public IList< Channel> ShuckingChannels { get; private set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public WashableTipsJob( ILabwareDatabase labware_database, TipShuttle tip_shuttle, Channel pressing_channel, IList< Channel> shucking_channels)
        {
            LabwareDatabase = labware_database;
            TipShuttle = tip_shuttle;
            PressingChannel = pressing_channel;
            ShuckingChannels = shucking_channels;
        }
    }
    */
    // --------------------------------------------------------------------------
    // DoActionJobs:
    // --------------------------------------------------------------------------
    public class DoActionJob : DispatcherJob
    {
        // ----------------------------------------------------------------------
        // members.
        // ----------------------------------------------------------------------
        public Object Key { get; set; }
        public Action Action { get; set; }
        private ManualResetEvent ActionEndedOrAbortedEvent { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public DoActionJob( Object key, Action action, ManualResetEvent action_ended_or_aborted_event = null)
            : base()
        {
            Key = key;
            Action = action;
            ActionEndedOrAbortedEvent = action_ended_or_aborted_event;
        }
    }
    // --------------------------------------------------------------------------
    public class EnableHardwareQuantumJob : DoActionJob
    {
        public EnableHardwareQuantumJob( HardwareQuantum hardware_quantum, bool on) : base( hardware_quantum, null) { Action = () => hardware_quantum.Enable( on); }
    }
    // --------------------------------------------------------------------------
    public class EnableAxisJob : DoActionJob
    {
        public EnableAxisJob( HardwareQuantum hardware_quantum, string axis_name, bool on) : base( hardware_quantum, null) { Action = () => hardware_quantum.EnableAxis( axis_name, on); }
    }
    // --------------------------------------------------------------------------
    public class HomeAxisJob : DoActionJob
    {
        public HomeAxisJob( HardwareQuantum hardware_quantum, string axis_name) : base( hardware_quantum, null) { Action = () => hardware_quantum.HomeAxis( axis_name); }
    }
    // --------------------------------------------------------------------------
    public class JogAxisJob : DoActionJob
    {
        public JogAxisJob( HardwareQuantum hardware_quantum, string axis_name, double jog_increment) : base( hardware_quantum, null) { Action = () => hardware_quantum.JogAxis( axis_name, jog_increment); }
    }
    // --------------------------------------------------------------------------
    public class ReturnChannelHomeJob : DoActionJob
    {
        public ReturnChannelHomeJob( Channel channel) : base( channel, null) { Action = () => channel.ReturnHome(); }
    }
    // --------------------------------------------------------------------------
    public class HomeChannelWZ : DoActionJob
    {
        public HomeChannelWZ( Channel channel) : base( channel, null) { Action = () => channel.HomeWZ(); }
    }
    // --------------------------------------------------------------------------
    // DispatcherProgressMessages:
    // --------------------------------------------------------------------------
    public interface DispatcherProgressMessage
    {
    }
    // --------------------------------------------------------------------------
    public class TipPressedOnMessage : DispatcherProgressMessage
    {
        public TipWell TipWell { get; private set; }

        public TipPressedOnMessage( TipWell tip_well)
        {
            TipWell = tip_well;
        }
    }
    // --------------------------------------------------------------------------
    public class TipOnCompleteMessage : DispatcherProgressMessage
    {
        public Channel Channel { get; private set; }

        public TipOnCompleteMessage( Channel channel)
        {
            Channel = channel;
        }
    }
    // --------------------------------------------------------------------------
    public class TipOffPreMoveCompleteMessage : DispatcherProgressMessage
    {
        public Channel Channel { get; private set; }

        public TipOffPreMoveCompleteMessage( Channel channel)
        {
            Channel = channel;
        }
    }
    // --------------------------------------------------------------------------
    public class TipOffCompleteMessage : DispatcherProgressMessage
    {
        public Channel Channel { get; private set; }

        public TipOffCompleteMessage( Channel channel)
        {
            Channel = channel;
        }
    }
    // --------------------------------------------------------------------------
    public class AspirateCompleteMessage : DispatcherProgressMessage
    {
        public Channel Channel { get; private set; }
        public Transfer Transfer { get; private set; }

        public AspirateCompleteMessage( Channel channel, Transfer transfer)
        {
            Channel = channel;
            Transfer = transfer;
        }
    }
    // --------------------------------------------------------------------------
    public class TransferCompleteMessage : DispatcherProgressMessage
    {
        public Channel Channel { get; private set; }
        public Transfer Transfer { get; private set; }

        public TransferCompleteMessage( Channel channel, Transfer transfer)
        {
            Channel = channel;
            Transfer = transfer;
        }
    }
    // --------------------------------------------------------------------------
    public class TransferAbortedMessage : DispatcherProgressMessage
    {
        public Channel Channel { get; private set; }
        public Transfer Transfer { get; private set; }

        public TransferAbortedMessage( Channel channel, Transfer transfer)
        {
            Channel = channel;
            Transfer = transfer;
        }
    }
    // --------------------------------------------------------------------------
    public class WashTipsCompleteMessage : DispatcherProgressMessage
    {
        public TipShuttle TipShuttle { get; private set; }

        public WashTipsCompleteMessage( TipShuttle tip_shuttle)
        {
            TipShuttle = tip_shuttle;
        }
    }
    // --------------------------------------------------------------------------
    public class DisableChannelMessage : DispatcherProgressMessage
    {
        public Channel Channel { get; private set; }

        public DisableChannelMessage( Channel channel)
        {
            Channel = channel;
        }
    }
    // --------------------------------------------------------------------------
    public class ICDParameterBundle
    {
        public ErrorEventHandler HandleError { get; private set; }
        public ManualResetEvent AbortEvent { get; private set; }
        public ThreadsafeMessenger Messenger { get; private set; }
        public BumblebeeConfiguration Configuration { get; private set; }

        public ICDParameterBundle( ErrorEventHandler handle_error, ManualResetEvent abort_event, ThreadsafeMessenger messenger, BumblebeeConfiguration configuration)
        {
            HandleError = handle_error;
            AbortEvent = abort_event;
            Messenger = messenger;
            Configuration = configuration;
        }
    }
    // --------------------------------------------------------------------------
    public class ICDEventBundle
    {
        public WaitHandle WaitBeforeStart { get; private set; }
        public ManualResetEvent SetOnFinish { get; private set; }
        public List< CountdownEvent> CountdownsOnFinish { get; private set; }
        public WaitHandle WaitBeforeFinish { get; private set; }

        public ICDEventBundle( WaitHandle wait_before_start, ManualResetEvent signal_on_finish, WaitHandle wait_before_finish)
            : this( wait_before_start, signal_on_finish, new List< CountdownEvent>(), wait_before_finish)
        {
        }

        public ICDEventBundle( WaitHandle wait_before_start, ManualResetEvent signal_on_finish, CountdownEvent countdown_on_finish, WaitHandle wait_before_finish)
            : this( wait_before_start, signal_on_finish, new List< CountdownEvent>{ countdown_on_finish}, wait_before_finish)
        {
        }

        public ICDEventBundle( WaitHandle wait_before_start, ManualResetEvent signal_on_finish, List< CountdownEvent> countdowns_on_finish, WaitHandle wait_before_finish)
        {
            WaitBeforeStart = wait_before_start;
            SetOnFinish = signal_on_finish;
            CountdownsOnFinish = countdowns_on_finish;
            WaitBeforeFinish = wait_before_finish;
        }
    }
    // --------------------------------------------------------------------------
    [ Export( typeof( BumblebeeDispatcher))]
    [ PartCreationPolicy( CreationPolicy.NonShared)]
    public class BumblebeeDispatcher
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        [ Import]
        private ILabwareDatabase LabwareDatabase { get; set; }
        [ Import]
        private ILiquidProfileLibrary LiquidProfileLibrary { get; set; }
        [ Import( AllowDefault = true)]
        private ILimsOutputTransferLog output_plugin_ { get; set; }

        private BBHardware hardware_;
        private Teachpoints teachpoints_;
        private ICDParameterBundle parameter_bundle_;
        private readonly Dictionary< object, StateMachineExecutor> executors_ = new Dictionary< object, StateMachineExecutor>();
        private int ZHighest { get; set; }

        private static readonly ILog Log = LogManager.GetLogger( typeof( BumblebeeDispatcher));

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public BumblebeeDispatcher()
        {
            ZHighest = 5000;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public void Setup( BumblebeeConfiguration configuration, BBHardware hardware, Teachpoints teachpoints, ThreadsafeMessenger messenger, ErrorEventHandler handle_error, bool is_diagnostics_dispatcher)
        {
            hardware_ = hardware;
            teachpoints_ = teachpoints;
            parameter_bundle_ = new ICDParameterBundle( handle_error, new ManualResetEvent( false), messenger, configuration);

            hardware_.AvailableHardwareQuanta.ForEach( q => executors_.Add( q, new StateMachineExecutor( q, is_diagnostics_dispatcher ? String.Format( "Diagnostic executor - {0}", q) : null)));

            messenger.Register< AbortCommand>( this, HandleAbortCommand);
            messenger.Register< DisableChannelMessage>( this, HandleDisableChannelMessage);
        }
        // ----------------------------------------------------------------------
        public StateMachineExecutor.ExecutorInfo GetExecutorInfo( object key)
        {
            return executors_[ key].GetExecutorInfo();
        }
        // ----------------------------------------------------------------------
        private void HandleAbortCommand( AbortCommand message)
        {
            parameter_bundle_.AbortEvent.Set();
        }
        // ----------------------------------------------------------------------
        private void HandleDisableChannelMessage( DisableChannelMessage message)
        {
            StateMachineExecutor channel_executor = executors_[ message.Channel];
            channel_executor.Disable();
        }
        // ----------------------------------------------------------------------
        public static void f( Stage stage, Channel lo_channel, Channel hi_channel, LabwareFormat labware_format, Well well_a, Well well_b, double channel_spacing, out double angle, out double y, out double x1, out double x2)
        {
            angle = WellMathUtil.CalculateAnglesToProperlySpaceWells( labware_format, well_a, well_b, channel_spacing).Item1;
            Tuple< double, double> lo_well_xy = labware_format.CalculatePostRotationXYCoordinates( well_a, angle);
            Tuple< double, double> hi_well_xy = labware_format.CalculatePostRotationXYCoordinates( well_b, angle);

            angle = angle * 180 / Math.PI;

            Tuple< double, double> center_xy = stage.GetCenterPosition( lo_channel.ID);
            x1 = center_xy.Item1 + lo_well_xy.Item1;
            y = center_xy.Item2 - lo_well_xy.Item2;

            center_xy = stage.GetCenterPosition( hi_channel.ID);
            x2 = center_xy.Item1 + hi_well_xy.Item1;
        }
        // ----------------------------------------------------------------------
        public static Tuple< double, double, double> g( Stage stage, Channel channel, LabwareFormat labware_format, Well well)
        {
            double angle = 0.0;
            Tuple< double, double> well_xy = labware_format.CalculatePostRotationXYCoordinates( well, angle);

            angle = angle * 180 / Math.PI;

            Tuple< double, double> center_xy = stage.GetCenterPosition( channel.ID);
            double x = center_xy.Item1 + well_xy.Item1;
            double y = center_xy.Item2 - well_xy.Item2;

            return Tuple.Create( x, y, angle);
        }
        // ----------------------------------------------------------------------
        #region IDispatcher Members
        // ----------------------------------------------------------------------
        public void StartDispatcher()
        {
            foreach( StateMachineExecutor executor in executors_.Values){
                executor.Start();
            }
        }
        // ----------------------------------------------------------------------
        public void StopDispatcher()
        {
            foreach( StateMachineExecutor executor in executors_.Values){
                executor.Stop();
            }
        }
        // ----------------------------------------------------------------------
        public void FlushDispatcher()
        {
            Log.Debug( "Flushing dispatcher");
            foreach( StateMachineExecutor executor in executors_.Values){
                Log.DebugFormat( "Flushing executor '{0}'", executor.Name);
                executor.Flush();
            }
        }
        // ----------------------------------------------------------------------
        public IEnumerable< object> GetQueueKeys()
        {
            return executors_.Keys;
        }
        // ----------------------------------------------------------------------
        public int GetQueueDepth( object queue_key)
        {
            return executors_[ queue_key].GetQueueDepth();
        }
        // ----------------------------------------------------------------------
        public bool GetBusy( object queue_key)
        {
            return executors_[ queue_key].Busy;
        }
        // ----------------------------------------------------------------------
        // try to refactor with DispatchWashableTipOffToTipBox: they are exactly the same except for the last two lines.
        // ----------------------------------------------------------------------
        public void DispatchTipOnJob( TipJob job)
        {
            Tuple< double, double, double> xyr_coordinate = g( job.Stage, job.Channel, LabwareFormat.LF_STANDARD_96, job.TipWell); //! \todo FYC get rid of hardcoded 96-well labware format.

            double z_origin = teachpoints_.GetStageTeachpoint( job.Channel.ID, job.Stage.ID).UpperLeft[ "z"];

            ManualResetEvent stage_available = new ManualResetEvent( false);
            CountdownEvent stage_countdown = new CountdownEvent( 1);

            ICDMoveStageStateMachine move_stage_state_machine = new ICDMoveStageStateMachine2(parameter_bundle_, new ICDEventBundle(null, stage_available, stage_countdown.WaitHandle), job, job.Stage, xyr_coordinate.Item2, xyr_coordinate.Item3);
            executors_[ job.Stage].AddStateMachine( move_stage_state_machine);
            ICDTipOnStateMachine tips_on_state_machine = new ICDTipOnStateMachine2(parameter_bundle_, new ICDEventBundle(stage_available, null, stage_countdown, null), job, job.Channel, job.TipWell, xyr_coordinate.Item1, z_origin, ZHighest, !(job.Stage is TipShuttle), job.TipBox, true);
            executors_[ job.Channel].AddStateMachine( tips_on_state_machine);
        }
        // ----------------------------------------------------------------------
        public void DispatchDualTipsOnJob( DualTipsJob job)
        {
            Channel lo_channel = job.Channels[ 0];
            Channel hi_channel = job.Channels[ 1];
            double channel_spacing = hardware_.GetChannelSpacing( lo_channel.ID, hi_channel.ID, job.Stage.ID);
            TipWell lo_well = job.TipWells[ 0];
            TipWell hi_well = job.TipWells[ 1];
            double angle = 0.0;
            double y = 0.0;
            double lo_x = 0.0;
            double hi_x = 0.0;
            BumblebeeDispatcher.f( job.Stage, lo_channel, hi_channel, LabwareFormat.LF_STANDARD_96, lo_well, hi_well, channel_spacing, out angle, out y, out lo_x, out hi_x); //! \todo FYC get rid of hardcoded 96-well labware format.

            double lo_z_origin = teachpoints_.GetStageTeachpoint( lo_channel.ID, job.Stage.ID).UpperLeft[ "z"];
            double hi_z_origin = teachpoints_.GetStageTeachpoint( hi_channel.ID, job.Stage.ID).UpperLeft[ "z"];

            ManualResetEvent stage_available = new ManualResetEvent( false);
            CountdownEvent stage_countdown = new CountdownEvent( 2);

            ICDMoveStageStateMachine move_tip_stage_state_machine = new ICDMoveStageStateMachine2(parameter_bundle_, new ICDEventBundle(null, stage_available, stage_countdown.WaitHandle), job, job.Stage, y, angle);
            executors_[ job.Stage].AddStateMachine( move_tip_stage_state_machine);
            ICDTipOnStateMachine lo_tip_on_state_machine = new ICDTipOnStateMachine2(parameter_bundle_, new ICDEventBundle(stage_available, null, stage_countdown, null), job, lo_channel, lo_well, lo_x, lo_z_origin, ZHighest, !(job.Stage is TipShuttle), job.TipBox, true);
            executors_[ lo_channel].AddStateMachine( lo_tip_on_state_machine);
            ICDTipOnStateMachine hi_tip_on_state_machine = new ICDTipOnStateMachine2(parameter_bundle_, new ICDEventBundle(stage_available, null, stage_countdown, null), job, hi_channel, hi_well, hi_x, hi_z_origin, ZHighest, !(job.Stage is TipShuttle), job.TipBox, true);
            executors_[ hi_channel].AddStateMachine( hi_tip_on_state_machine);
        }
        // ----------------------------------------------------------------------
        public void DispatchTipOffJob( TipOffJob job)
        {
            Channel channel = job.Channel;
            double x_fork = teachpoints_.GetWashTeachpoint( channel.ID)[ "x"];
            double z_fork = teachpoints_.GetWashTeachpoint( channel.ID)[ "z"];
            if( parameter_bundle_.Configuration.UseWToShuck){
                ICDTipOffToWasteStateMachine tips_off_state_machine = new ICDWAxisTipOffToWasteStateMachine(parameter_bundle_, null, job, channel, x_fork, z_fork);
                executors_[ channel].AddStateMachine( tips_off_state_machine);
            } else{
                ICDTipOffToWasteStateMachine tips_off_state_machine = new ICDZAxisTipOffToWasteStateMachine(parameter_bundle_, null, job, channel, x_fork, z_fork);
                executors_[ channel].AddStateMachine( tips_off_state_machine);
            }
        }
        // ----------------------------------------------------------------------
        public void DispatchLiquidTransferJob( LiquidTransferJob job)
        {
            var transfer_channels = from transfer_to_perform in job.TransfersToPerform
                                    select transfer_to_perform.Key;

            int simultaneous_transfers = transfer_channels.Count();

            ManualResetEvent src_stage_available_for_use = new ManualResetEvent( false);
            ManualResetEvent dst_stage_available_for_use = new ManualResetEvent( false);
            CountdownEvent src_stage_use_countdown = new CountdownEvent( simultaneous_transfers);
            CountdownEvent dst_stage_use_countdown = new CountdownEvent( simultaneous_transfers);

            double src_y = 0.0;
            double dst_y = 0.0;
            double src_angle = 0.0;
            double dst_angle = 0.0;
            IDictionary< Channel, double> src_xs = new Dictionary< Channel, double>();
            IDictionary< Channel, double> dst_xs = new Dictionary< Channel, double>();

            // for now, we're not going multi-tip yet.
            // Debug.Assert(( job.TipsToPressOn.Count == 1) || ( job.TipsToPressOn.Count == 2));

            if( job.TransfersToPerform.Count == 1){
                //double x = 0.0;
                Channel channel = job.TransfersToPerform.First().Key;
                Transfer transfer = job.TransfersToPerform[ channel];

                Tuple< double, double, double> src_xyr_coordinate = g( job.SrcStage, channel, LabwareFormat.GetLabwareFormat( transfer.SrcPlate.NumberOfWells), job.TransfersToPerform[ channel].SrcWell);
                src_xs.Add( channel, src_xyr_coordinate.Item1);
                src_y = src_xyr_coordinate.Item2;
                src_angle = src_xyr_coordinate.Item3;

                Tuple< double, double, double> dst_xyr_coordinate = g( job.DstStage, channel, LabwareFormat.GetLabwareFormat( transfer.DstPlate.NumberOfWells), job.TransfersToPerform[ channel].DstWell);
                dst_xs.Add( channel, dst_xyr_coordinate.Item1);
                dst_y = dst_xyr_coordinate.Item2;
                dst_angle = dst_xyr_coordinate.Item3;
            }
            if( job.TransfersToPerform.Count == 2){
                var channels_ascending_order = from ttpo in job.TransfersToPerform
                                               orderby ttpo.Key.ID
                                               select ttpo.Key;
                Channel lo_channel = channels_ascending_order.First();
                Channel hi_channel = channels_ascending_order.Last();
                Transfer transfer = job.TransfersToPerform[ lo_channel];
                double src_channel_spacing = hardware_.GetChannelSpacing( lo_channel.ID, hi_channel.ID, job.SrcStage.ID);
                double dst_channel_spacing = hardware_.GetChannelSpacing( lo_channel.ID, hi_channel.ID, job.DstStage.ID);

                double x1 = 0.0;
                double x2 = 0.0;

                f( job.SrcStage, lo_channel, hi_channel, LabwareFormat.GetLabwareFormat( transfer.SrcPlate.NumberOfWells), job.TransfersToPerform[ lo_channel].SrcWell, job.TransfersToPerform[ hi_channel].SrcWell, src_channel_spacing, out src_angle, out src_y, out x1, out x2);
                src_xs.Add( lo_channel, x1);
                src_xs.Add( hi_channel, x2);

                f( job.DstStage, lo_channel, hi_channel, LabwareFormat.GetLabwareFormat( transfer.DstPlate.NumberOfWells), job.TransfersToPerform[ lo_channel].DstWell, job.TransfersToPerform[ hi_channel].DstWell, dst_channel_spacing, out dst_angle, out dst_y, out x1, out x2);
                dst_xs.Add( lo_channel, x1);
                dst_xs.Add( hi_channel, x2);
            }

            ICDMoveStageStateMachine move_src_stage_state_machine = new ICDMoveStageStateMachine2(parameter_bundle_, new ICDEventBundle(null, src_stage_available_for_use, src_stage_use_countdown.WaitHandle), job, job.SrcStage, src_y, src_angle);
            executors_[ job.SrcStage].AddStateMachine( move_src_stage_state_machine);
            ICDMoveStageStateMachine move_dst_stage_state_machine = new ICDMoveStageStateMachine2(parameter_bundle_, new ICDEventBundle(null, dst_stage_available_for_use, dst_stage_use_countdown.WaitHandle), job, job.DstStage, dst_y, dst_angle);
            executors_[ job.DstStage].AddStateMachine( move_dst_stage_state_machine);

            foreach( KeyValuePair< Channel, Transfer> transfer_to_perform in job.TransfersToPerform){
                Channel channel = transfer_to_perform.Key;
                Transfer transfer = transfer_to_perform.Value;
                ILiquidProfile liquid_profile = LiquidProfileLibrary.LoadLiquidProfileByName( transfer.LiquidProfileName);
                string pre_aspirate_mix_lp_name = liquid_profile.PreAspirateMixLiquidProfile;
                ILiquidProfile pre_aspirate_mix_lp = ( pre_aspirate_mix_lp_name == "") ? null : LiquidProfileLibrary.LoadLiquidProfileByName( pre_aspirate_mix_lp_name);
                string post_dispense_mix_lp_name = liquid_profile.PostDispenseMixLiquidProfile;
                ILiquidProfile post_dispense_mix_lp = ( post_dispense_mix_lp_name == "") ? null : LiquidProfileLibrary.LoadLiquidProfileByName( post_dispense_mix_lp_name);
                double src_z_origin = teachpoints_.GetStageTeachpoint( channel.ID, job.SrcStage.ID).UpperLeft[ "z"];
                double dst_z_origin = teachpoints_.GetStageTeachpoint( channel.ID, job.DstStage.ID).UpperLeft[ "z"];
                double tip_z_origin = teachpoints_.GetStageTeachpoint( channel.ID, channel.TipWell.Carrier.Stage.ID).UpperLeft[ "z"];
                double aspirate_distance_from_well_bottom = job.TransfersToPerform[ channel].AspirateDistanceFromWellBottomMm.Value;
                double dispense_distance_from_well_bottom = job.TransfersToPerform[ channel].DispenseDistanceFromWellBottomMm.Value;
                double volume = job.TransfersToPerform[ channel].TransferVolume;
                ICDLiquidTransferStateMachine aspirate_state_machine = new ICDAspirateStateMachine2(parameter_bundle_, new ICDEventBundle(src_stage_available_for_use, null, src_stage_use_countdown, null), job, channel, transfer, LabwareDatabase.GetLabware(job.TransfersToPerform.First().Value.SrcPlate.LabwareName), liquid_profile, pre_aspirate_mix_lp, src_xs[channel], src_z_origin, ZHighest, aspirate_distance_from_well_bottom, volume);
                executors_[ transfer_to_perform.Key].AddStateMachine( aspirate_state_machine);
                ICDLiquidTransferStateMachine dispense_state_machine = new ICDDispenseStateMachine2(parameter_bundle_, new ICDEventBundle(dst_stage_available_for_use, null, dst_stage_use_countdown, null), job, channel, transfer, LabwareDatabase.GetLabware(job.TransfersToPerform.First().Value.DstPlate.LabwareName), liquid_profile, post_dispense_mix_lp, dst_xs[channel], dst_z_origin, ZHighest, dispense_distance_from_well_bottom, volume, output_plugin_);
                executors_[ transfer_to_perform.Key].AddStateMachine( dispense_state_machine);
                ICDTipOffToCarrierPreMoveStateMachine tip_off_pre_move_state_machine = new ICDTipOffToCarrierPreMoveStateMachine(parameter_bundle_, null, job, channel, tip_z_origin);
                executors_[ transfer_to_perform.Key].AddStateMachine( tip_off_pre_move_state_machine);
            }
        }
        // ----------------------------------------------------------------------
        public void DispatchMoveStageJob( MoveStageJob job)
        {
            ManualResetEvent stage_ready = new ManualResetEvent( false);
            CountdownEvent stage_free = new CountdownEvent( 1);
            Teachpoint tp = teachpoints_.GetRobotTeachpoint( job.Stage.ID, job.Orientation);
            ICDMoveStageStateMachine move_stage_state_machine = new ICDMoveStageStateMachine2(parameter_bundle_, new ICDEventBundle(null, stage_ready, stage_free.WaitHandle), job, job.Stage, tp["y"], tp["r"]);
            executors_[ job.Stage].AddStateMachine( move_stage_state_machine);
            stage_ready.WaitOne();
            stage_free.Signal();
        }
        // ----------------------------------------------------------------------
        public void DispatchWashTipsJob( WashTipsJob job)
        {
            WashTipsStateMachine wash_tips_state_machine = new WashTipsStateMachine(parameter_bundle_, null, job, job.TipShuttle, job.Debug);
            executors_[ job.TipShuttle].AddStateMachine( wash_tips_state_machine);
        }
        // ----------------------------------------------------------------------
        public void DispatchWashableMoveShuttle( TipShuttle tip_shuttle, IEnumerable< Channel> channels, ManualResetEvent stage_available)
        {
            double average_y = channels.Average( c => c.TipWell.GetXYPosition( c.ID).Item2);
            ICDMoveStageStateMachine move_tip_shuttle_state_machine = new ICDMoveStageStateMachine2(parameter_bundle_, new ICDEventBundle(null, stage_available, new CountdownEvent(0).WaitHandle), null, tip_shuttle, average_y, 90.0);
            executors_[ tip_shuttle].AddStateMachine( move_tip_shuttle_state_machine);
        }
        // ----------------------------------------------------------------------
        public void DispatchWashableTipsOn( Channel channel, ManualResetEvent stage_available, CountdownEvent offset_countdown, bool move_stage)
        {
            Stage highest_ordinal_non_tip_shuttle_stage = hardware_.GetStage( hardware_.Stages.Where( stage => !( stage is TipShuttle)).Max( stage => stage.ID));
            double max_center_x_of_highest_ordinal_non_tip_shuttle_stage = hardware_.Channels.Max( c => highest_ordinal_non_tip_shuttle_stage.GetCenterPosition( c.ID).Item1);
            double channels_clear_of_non_tip_shuttle_stages_right_of_x = max_center_x_of_highest_ordinal_non_tip_shuttle_stage + Math.Sqrt(( 54.0 * 54.0) + ( 36.0 * 36.0)); // FYC we should use this more....

            stage_available = stage_available != null ? stage_available : new ManualResetEvent( false);
            stage_available.Reset();

            if( offset_countdown == null){
                offset_countdown = new CountdownEvent( 1);
            }

            CountdownEvent stage_countdown = new CountdownEvent( 1);

            TipBox tip_box = LabwareDatabase.GetLabware( "tipbox") as TipBox;
            Tuple< double, double> xy_position = channel.TipWell.GetXYPosition( channel.ID);
            double z_origin = teachpoints_.GetStageTeachpoint( channel.ID, channel.TipWell.Carrier.Stage.ID).UpperLeft[ "z"];

            if( move_stage){
                ICDMoveStageStateMachine move_tip_shuttle_state_machine = new ICDMoveStageStateMachine2(parameter_bundle_, new ICDEventBundle(null, stage_available, stage_countdown.WaitHandle), null, channel.TipWell.Carrier.Stage, xy_position.Item2, 90.0);
                executors_[ channel.TipWell.Carrier.Stage].AddStateMachine( move_tip_shuttle_state_machine);
            }

            ICDTipOnStateMachine tip_on_state_machine = new ICDTipOnStateMachine2(parameter_bundle_, new ICDEventBundle(stage_available, null, new List< CountdownEvent>{ stage_countdown, offset_countdown}, null), null, channel, channel.TipWell, xy_position.Item1, z_origin, ZHighest, false, tip_box, true, channels_clear_of_non_tip_shuttle_stages_right_of_x);
            executors_[ channel].AddStateMachine( tip_on_state_machine);
        }
        // ----------------------------------------------------------------------
        public void DispatchWashableTipsOff( Channel channel, ManualResetEvent stage_available, CountdownEvent offset_countdown)
        {
            double z_origin = teachpoints_.GetStageTeachpoint( channel.ID, channel.TipWell.Carrier.Stage.ID).UpperLeft[ "z"];
            ICDTipOffToCarrierPostMoveStateMachine tip_off_state_machine = new ICDTipOffToCarrierPostMoveStateMachine(parameter_bundle_, new ICDEventBundle(stage_available, null, offset_countdown, null), null, channel, channel.TipWell.GetXYPosition(channel.ID).Item1, z_origin, null);
            executors_[ channel].AddStateMachine( tip_off_state_machine);
        }
        // ----------------------------------------------------------------------
        public void DispatchWashableTipsOffWithMove( TipJob job)
        {
            double z_origin = teachpoints_.GetStageTeachpoint( job.Channel.ID, job.Channel.TipWell.Carrier.Stage.ID).UpperLeft[ "z"];

            ManualResetEvent stage_available = new ManualResetEvent( false);
            CountdownEvent stage_countdown = new CountdownEvent( 1);

            ICDMoveStageStateMachine move_stage_state_machine = new ICDMoveStageStateMachine2(parameter_bundle_, new ICDEventBundle(null, stage_available, stage_countdown.WaitHandle), job, job.Stage, job.Channel.TipWell.GetXYPosition(job.Channel.ID).Item2, 90.0);
            executors_[ job.Stage].AddStateMachine( move_stage_state_machine);
            ICDTipOffToCarrierPostMoveStateMachine tip_off_state_machine = new ICDTipOffToCarrierPostMoveStateMachine(parameter_bundle_, new ICDEventBundle(stage_available, null, stage_countdown, null), null, job.Channel, job.Channel.TipWell.GetXYPosition(job.Channel.ID).Item1, z_origin, null);
            executors_[ job.Channel].AddStateMachine( tip_off_state_machine);
        }
        // ----------------------------------------------------------------------
        public void DispatchWashableTipOffToTipBox( TipJob job)
        {
            Tuple< double, double, double> xyr_coordinate = g( job.Stage, job.Channel, LabwareFormat.LF_STANDARD_96, job.TipWell); //! \todo FYC get rid of hardcoded 96-well labware format.

            double z_origin = teachpoints_.GetStageTeachpoint( job.Channel.ID, job.Stage.ID).UpperLeft[ "z"];

            ManualResetEvent stage_available = new ManualResetEvent( false);
            CountdownEvent stage_countdown = new CountdownEvent( 1);

            ICDMoveStageStateMachine move_stage_state_machine = new ICDMoveStageStateMachine2(parameter_bundle_, new ICDEventBundle(null, stage_available, stage_countdown.WaitHandle), job, job.Stage, xyr_coordinate.Item2, xyr_coordinate.Item3);
            executors_[ job.Stage].AddStateMachine( move_stage_state_machine);
            ICDTipOffToCarrierPostMoveStateMachine tips_off_state_machine = new ICDTipOffToCarrierPostMoveStateMachine(parameter_bundle_, new ICDEventBundle(stage_available, null, stage_countdown, null), null, job.Channel, xyr_coordinate.Item1, z_origin, job.TipBox);
            executors_[ job.Channel].AddStateMachine( tips_off_state_machine);
        }
        // ----------------------------------------------------------------------
        public void DispatchAction( DoActionJob job)
        {
            DoActionStateMachine state_machine = new DoActionStateMachine(parameter_bundle_, null, job.Action);
            executors_[ job.Key].AddStateMachine( state_machine);
        }
        // ----------------------------------------------------------------------
        public void DispatchHome()
        {
            IEnumerable< Channel> available_channels = hardware_.AvailableChannels;
            IEnumerable< TipShuttle> tip_shuttles = hardware_.TipShuttles;
            int num_axis_countdown = available_channels.Count() + tip_shuttles.Count();
            CountdownEvent countdown = new CountdownEvent( num_axis_countdown);
            foreach( Channel c in available_channels){
                Channel ch = c;
                executors_[ch].AddStateMachine(new DoActionStateMachine(parameter_bundle_, new ICDEventBundle( null, null, countdown, null), () => ch.HomeWZ()));
                executors_[ch].AddStateMachine(new DoActionStateMachine(parameter_bundle_, null, () => ch.HomeX()));
            }
            foreach( TipShuttle ts in tip_shuttles){
                TipShuttle tip_shuttle = ts;
                executors_[tip_shuttle].AddStateMachine(new DoActionStateMachine(parameter_bundle_, new ICDEventBundle( null, null, countdown, null), () => tip_shuttle.HomeAB()));
            }
            foreach( Stage s in hardware_.Stages){
                Stage stage = s;
                executors_[ stage].AddStateMachine( new DoActionStateMachine( parameter_bundle_, new ICDEventBundle( countdown.WaitHandle, null, null), () => stage.HomeYR()));
            }
        }
        // ----------------------------------------------------------------------
        #endregion
        // ----------------------------------------------------------------------
        // do not move to dead_code.cs yet; functionality is unique enough to warrant investigating its use in the near future.
        /* not used -- used for "batched" tip on/tips off, allowing at most one tip on and as many tips off as possible.
        public void DispatchWashableTipsJob( WashableTipsJob job)
        {
            Stage highest_ordinal_non_tip_shuttle_stage = hardware_.GetStage( hardware_.Stages.Where( stage => !( stage is TipShuttle)).Max( stage => stage.ID));
            double max_center_x_of_highest_ordinal_non_tip_shuttle_stage = hardware_.Channels.Max( channel => highest_ordinal_non_tip_shuttle_stage.GetCenterPosition( channel.ID).Item1);
            double channels_clear_of_non_tip_shuttle_stages_right_of_x = max_center_x_of_highest_ordinal_non_tip_shuttle_stage + Math.Sqrt(( 54.0 * 54.0) + ( 36.0 * 36.0)); // FYC we should use this more....

            ManualResetEvent stage_available = new ManualResetEvent( false);
            CountdownEvent stage_countdown = new CountdownEvent( job.ShuckingChannels.Count + ( job.PressingChannel != null ? 1 : 0));

            double x = 0.0;
            double y = 0.0;
            if( job.PressingChannel != null){
                Tuple< double, double> xy_position = job.PressingChannel.Tip.GetXYPosition( job.PressingChannel.ID);
                x = xy_position.Item1;
                y = xy_position.Item2;
            } else{
                if( job.ShuckingChannels.Count() > 0){
                    var shucking_channel = job.ShuckingChannels.FirstOrDefault();
                    Tuple< double, double> xy_position = shucking_channel.Tip.GetXYPosition( shucking_channel.ID);
                    x = xy_position.Item1;
                    y = xy_position.Item2;
                } else{
                    throw new Exception( "should never happen!");
                }
            }

            ICDMoveStageStateMachine move_tip_stage_state_machine = new ICDMoveStageStateMachine2( , , job, stage_available, stage_countdown, , job.TipShuttle, y, 90.0);
            executors_[ job.TipShuttle].AddStateMachine( move_tip_stage_state_machine);

            if( job.PressingChannel != null){
                TipBox tip_box = job.LabwareDatabase.GetLabware( "tipbox") as TipBox;
                double z_origin = teachpoints_.GetStageTeachpoint( job.PressingChannel.ID, job.TipShuttle.ID).UpperLeft[ "z"];
                ICDTipOnStateMachine tip_on_state_machine = new ICDTipOnStateMachine2( , , , job.PressingChannel, job, stage_available, stage_countdown, null, , new Well( "A1"), x, z_origin, ZHighest, !( job.TipShuttle is TipShuttle), tip_box, true, channels_clear_of_non_tip_shuttle_stages_right_of_x);
                executors_[ job.PressingChannel].AddStateMachine( tip_on_state_machine);
            }

            foreach( var shucking_channel in job.ShuckingChannels){
                double z_origin = teachpoints_.GetStageTeachpoint( shucking_channel.ID, job.TipShuttle.ID).UpperLeft[ "z"];
                ICDTipOffPostMoveStateMachine tip_off_state_machine = new ICDTipOffPostMoveStateMachine( , , , shucking_channel, job, stage_available, stage_countdown, , shucking_channel.Tip.GetXYPosition( shucking_channel.ID).Item1, z_origin);
                executors_[ shucking_channel].AddStateMachine( tip_off_state_machine);
            }
        }
        */
    }
}
