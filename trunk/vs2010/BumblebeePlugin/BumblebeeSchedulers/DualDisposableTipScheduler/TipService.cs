using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using BioNex.BumblebeePlugin.Dispatcher;
using BioNex.BumblebeePlugin.Hardware;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LabwareDatabase;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.PlateWork;
using BioNex.Shared.Utils.WellMathUtil;
using log4net;

namespace BioNex.BumblebeePlugin.Scheduler.DualChannelScheduler
{
    public class TipService
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        private BBHardware Hardware { get; set; }
        private System.Windows.Threading.Dispatcher WindowsDispatcher { get; set; }
        private ServiceSharedMemory SharedMemory { get; set; }
        private BumblebeeDispatcher ProtocolDispatcher { get; set; }
        private Stage TipStage { get; set; }
        private TipBox TipBox { get; set; }
        private BumblebeeConfiguration Config { get; set; }
        private ITipBoxManager TipBoxManager { get; set; }
        private IRobotScheduler RobotScheduler { get; set; }

        private AutoResetEvent StopEvent { get; set; }
        private Thread TipServiceThread { get; set; }
 
        // ----------------------------------------------------------------------
        // members.
        // ----------------------------------------------------------------------
        private static readonly ILog Log = LogManager.GetLogger( typeof( TipService));

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public TipService( BBHardware hardware, System.Windows.Threading.Dispatcher windows_dispatcher, ServiceSharedMemory shared_memory, BumblebeeDispatcher protocol_dispatcher, Stage tip_stage, TipBox tip_box, BumblebeeConfiguration config, ITipBoxManager tip_box_manager, IRobotScheduler robot_scheduler)
        {
            Hardware = hardware;
            WindowsDispatcher = windows_dispatcher;
            SharedMemory = shared_memory;
            ProtocolDispatcher = protocol_dispatcher;
            TipStage = tip_stage;
            TipBox = tip_box;
            Config = config;
            TipBoxManager = tip_box_manager;
            RobotScheduler = robot_scheduler;

            StopEvent = new AutoResetEvent( false);
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public void StartService()
        {
            TipServiceThread = new Thread( new ThreadStart( TipServiceThreadRunner)){ Name = "DS tip service", IsBackground = true};
            TipServiceThread.Start();
        }
        // ----------------------------------------------------------------------
        public void StopService()
        {
            if( TipServiceThread == null){
                return;
            }
            StopEvent.Set();
        }
        // ----------------------------------------------------------------------
        private void Abort( AbortCommand command)
        {
            Log.Info( "Tip service received Abort() call");
            StopEvent.Set();
        }
        // ----------------------------------------------------------------------
        private void Pause( PauseCommand command)
        {
            Log.Info( "Tip service received Pause() call");
        }
        // ----------------------------------------------------------------------
        private void Resume( ResumeCommand command)
        {
            Log.Info( "Tip service received Resume() call");
        }
        // ----------------------------------------------------------------------
        private bool DisplayedRefillMessage { get; set; }
        // ----------------------------------------------------------------------
        private void TipServiceThreadRunner()
        {
            TipTracker TipTracker = new TipTracker();

            Tuple< string, string> tip_box_source_location = Tuple.Create< string, string>( "", "");
            while( !StopEvent.WaitOne( 0)){
                // yield to other threads.
                Thread.Sleep( 10);

                if( TipStage.Plate == null){
                    if( Config.EverlastingTipbox){
                        throw new Exception( "is the following sufficient to set the stage as full?");
                        TipStage.Plate = new Plate( LabwareFormat.LF_STANDARD_96); //! \todo FYC get rid of hardcoded 96-well labware format.
                        TipTracker.SetAll( TipWellState.Clean);
                    } else{
                        // ask TipBoxManager for a tip box.
                        tip_box_source_location = TipBoxManager.AcquireTipBox();
                        if( tip_box_source_location == null){
                            if( !DisplayedRefillMessage){
                                WindowsDispatcher.Invoke( new Action( () => {
                                    MessageBox.Show( "New tip-boxes needed.  Please refresh tip-boxes and update tip-box status through the Tip-Box Manager.");
                                }));
                                DisplayedRefillMessage = true;
                            }
                        } else{
                            RobotScheduler.AddJob( tip_box_source_location.Item1, tip_box_source_location.Item2, BioNexDeviceNames.Bumblebee, "BB PM 4", "tipbox");
                            DisplayedRefillMessage = false;
                            throw new Exception( "is the following sufficient to set the stage as full?");
                            TipStage.Plate = new Plate( LabwareFormat.LF_STANDARD_96); //! \todo FYC get rid of hardcoded 96-well labware format.
                            TipTracker.SetAll( TipWellState.Clean);
                        }
                    }
                }

                if(( TipStage.Plate != null) && TipTracker.IsAllDirty()){
                    if( !Config.EverlastingTipbox){
                        RobotScheduler.AddJob( BioNexDeviceNames.Bumblebee, "BB PM 4", tip_box_source_location.Item1, tip_box_source_location.Item2, "tipbox");
                        TipBoxManager.ReleaseTipBox( tip_box_source_location);
                    }
                    TipStage.Plate = null;
                    TipTracker.SetAll( TipWellState.Empty);
                }

                // 2. find channels that have used tips and ask them to strip off their tips.
                //    don't allow more than 4 channels to be shucking simultaneously to limit current draw.
                Channel dirty_channel = Hardware.AvailableChannels.FirstOrDefault( c => c.Status == Channel.ChannelStatus.DirtyTip);
                int shucking_channels_count = Hardware.AvailableChannels.Count( c => c.Status == Channel.ChannelStatus.ShuckingTipInQueue || c.Status == Channel.ChannelStatus.ShuckingTipInAction);
                if(( dirty_channel != null) && ( shucking_channels_count < 4)){
                    dirty_channel.Status = Channel.ChannelStatus.ShuckingTipInAction;
                    ProtocolDispatcher.DispatchTipOffJob( new TipOffJob( dirty_channel));
                    // if( dirty_channels > 4){
                    // Thread.Sleep( 300);
                    // }
                }

                // 3. find channels that have no tips:
                //    a. if tip pressing can occur completely in parallel (not yet), then ask them to press on tips.
                //    b. if tip pressing cannot run in parallel (current situation), then determine how to get the next tip on.
                int num_unused_tips = TipTracker.CountTipsOfState( TipWellState.Clean);
                // bail if there aren't any tips available.
                if( num_unused_tips == 0){
                    continue;
                }
                IEnumerable< Channel> tipless_channels = Hardware.AvailableChannels.Where( c => c.Status == Channel.ChannelStatus.NoTip);
                IEnumerable< Channel> shucking_channels = Hardware.AvailableChannels.Where( c => c.Status == Channel.ChannelStatus.ShuckingTipInQueue || c.Status == Channel.ChannelStatus.ShuckingTipInAction);
                int num_tipless_channels = tipless_channels.Count();
                int num_shucking_channels = shucking_channels.Count();
                // if there aren't tipless channels, then there's nothing to do.
                if( num_tipless_channels == 0){
                    continue;
                }
                // tip stage already has plenty of work to do.
                if( ProtocolDispatcher.GetQueueDepth( TipStage) > 1){
                    continue;
                }
                // if there is just one tipless channel but some number of dirty channels, then wait around for a dirty channel to become tipless to maximize pairing.
                if( num_tipless_channels == 1 && num_shucking_channels > 0){
                    continue;
                }
                // if there is just one tipless channel or there is just one unused tip,
                // then dispatch a single tip-on job.
                if( num_tipless_channels == 1 || num_unused_tips == 1){
                    TipWell tip_well = TipTracker.ReserveOneTip();
                    Debug.Assert( tip_well != null, "This should never happen; we already checked to make sure there was an unused tip.");
                    tipless_channels.First().Status = Channel.ChannelStatus.PressingTip;
                    TipJob press_tip_job = new TipJob( tipless_channels.First(), TipStage, TipBox, tip_well);
                    // string s = string.Join( ",", SharedMemory.ChannelUsage.Select( x => x.Key.ToString() + x.Value.ToString()));
                    // Log.DebugFormat( "single tip press with channel status = {0}", s);
                    ProtocolDispatcher.DispatchTipOnJob( press_tip_job);
                    continue;
                }
                // otherwise, there are more than one tipless channel and there are more than one unused tip.
                var tipless_channels_with_least_separation = ( from ch1 in tipless_channels
                                                               from ch2 in tipless_channels
                                                               where ch1.ID < ch2.ID
                                                               orderby ch1.ID
                                                               orderby ch2.ID - ch1.ID
                                                               select new List< Channel>{ ch1, ch2}).First();
                // replace the following with logic to press on both tips simultaneously.
                Channel lo_channel = tipless_channels_with_least_separation.First();
                Channel hi_channel = tipless_channels_with_least_separation.Last();
                double channel_spacing = Hardware.GetChannelSpacing( lo_channel.ID, hi_channel.ID, TipStage.ID);
                Tuple< TipWell, TipWell> tips = TipTracker.ReserveTwoCompatibleTips( channel_spacing);
                try{
                    double angle = 0.0;
                    double y = 0.0;
                    double x1 = 0.0;
                    double x2 = 0.0;
                    BumblebeeDispatcher.f( TipStage, lo_channel, hi_channel, LabwareFormat.LF_STANDARD_96, tips.Item1, tips.Item2, channel_spacing, out angle, out y, out x1, out x2); //! \todo FYC get rid of hardcoded 96-well labware format.
                    // if no exception, then press on two tips simultaneously.
                    lo_channel.Status = Channel.ChannelStatus.PressingTip;
                    hi_channel.Status = Channel.ChannelStatus.PressingTip;
                    DualTipsJob dual_press_tip_job = new DualTipsJob( new List< Channel>{ lo_channel, hi_channel}, TipStage, TipBox, new List< TipWell>{ tips.Item1, tips.Item2});
                    // string s = string.Join( ",", SharedMemory.ChannelUsage.Select( x => x.Key.ToString() + x.Value.ToString()));
                    // Log.DebugFormat( "dual tip press with channel status = {0}", s);
                    ProtocolDispatcher.DispatchDualTipsOnJob( dual_press_tip_job);
                } catch{
                    // else just press on the first tip:
                    // "give back" the second tip that was reserved.
                    tips.Item2.SetState( TipWellState.Clean);
                    lo_channel.Status = Channel.ChannelStatus.PressingTip;
                    TipJob press_tip_job = new TipJob( lo_channel, TipStage, TipBox, tips.Item1);
                    // string s = string.Join( ",", SharedMemory.ChannelUsage.Select( x => x.Key.ToString() + x.Value.ToString()));
                    // Log.DebugFormat( "single tip press with channel status = {0}", s);
                    ProtocolDispatcher.DispatchTipOnJob( press_tip_job);
                }
            }
        }
    }

    public class CarrierBasedTipService
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        private BBHardware Hardware { get; set; }
        private System.Windows.Threading.Dispatcher WindowsDispatcher { get; set; }
        private ServiceSharedMemory SharedMemory { get; set; }
        private BumblebeeDispatcher ProtocolDispatcher { get; set; }
        private ITipBoxManager TipBoxManager { get; set; }
        private IRobotScheduler RobotScheduler { get; set; }

        private AutoResetEvent StopEvent { get; set; }
        private ManualResetEvent RunEvent { get; set; }
        private Thread TipServiceThread { get; set; }

        private IList< TipShuttle> TipShuttles { get; set; }
 
        // ----------------------------------------------------------------------
        // members.
        // ----------------------------------------------------------------------
        private static readonly ILog Log = LogManager.GetLogger( typeof( CarrierBasedTipService));

        private const int LO_CHANNEL_ID = 1;
        private const int HI_CHANNEL_ID = 8;
        private const int NUM_TIP_CARRIER_ROWS = 16;

        private IEnumerable< int> PossibleOffsets { get; set; }
        private IDictionary< TipShuttle, int> ActiveOffsets { get; set; }
        private IDictionary< TipShuttle, ManualResetEvent> StageAvailableEvents { get; set; }
        private IDictionary< TipShuttle, Dictionary< int, CountdownEvent>> OffsetCountdownEvents { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public CarrierBasedTipService( BBHardware hardware, System.Windows.Threading.Dispatcher windows_dispatcher, ServiceSharedMemory shared_memory, BumblebeeDispatcher protocol_dispatcher, ITipBoxManager tip_box_manager, IRobotScheduler robot_scheduler)
        {
            Hardware = hardware;
            WindowsDispatcher = windows_dispatcher;
            SharedMemory = shared_memory;
            ProtocolDispatcher = protocol_dispatcher;
            // Config = config;
            TipBoxManager = tip_box_manager;
            RobotScheduler = robot_scheduler;
            TipShuttles = hardware.TipShuttles;

            // calculate lowest possible offset, highest possible offset, and number of total offsets.
            int lowest_offset = TipShuttleOffsetFromChannelAndRow( HI_CHANNEL_ID, 0);
            int highest_offset = TipShuttleOffsetFromChannelAndRow( LO_CHANNEL_ID, NUM_TIP_CARRIER_ROWS - 1);
            int num_offsets = highest_offset - lowest_offset + 1;

            // compute all the possible offsets.
            PossibleOffsets = Enumerable.Range( lowest_offset, num_offsets);
            ActiveOffsets = TipShuttles.ToDictionary( ts => ts, ts => int.MaxValue);
            StageAvailableEvents = new Dictionary< TipShuttle, ManualResetEvent>();
            OffsetCountdownEvents = TipShuttles.ToDictionary( ts => ts, ts => PossibleOffsets.ToDictionary( offset => offset, offset => new CountdownEvent( 0)));

            StopEvent = new AutoResetEvent( false);
            RunEvent = new ManualResetEvent( true);
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public void StartService()
        {
            TipServiceThread = new Thread( new ThreadStart( RevisedTipServiceThreadRunner)){ Name = "DS tip service", IsBackground = true};
            TipServiceThread.Start();
        }
        // ----------------------------------------------------------------------
        public void StopService()
        {
            if( TipServiceThread == null){
                return;
            }
            StopEvent.Set();
        }
        // ----------------------------------------------------------------------
        private void SystemAbort( AbortCommand command)
        {
            Log.Info( "Tip service received Abort() call");
            StopEvent.Set();
        }
        // ----------------------------------------------------------------------
        private void SystemPause( PauseCommand command)
        {
            Log.Info( "Tip service received Pause() call");
        }
        // ----------------------------------------------------------------------
        private void SystemResume( ResumeCommand command)
        {
            Log.Info( "Tip service received Resume() call");
        }
        // ----------------------------------------------------------------------
        public void Pause()
        {
            RunEvent.Reset();
        }
        // ----------------------------------------------------------------------
        public void Resume()
        {
            RunEvent.Set();
        }
        // ----------------------------------------------------------------------
        private bool DisplayedRefillMessage { get; set; }
        // ----------------------------------------------------------------------
        private static int TipShuttleOffsetFromChannelAndRow( byte channel_id, int row_index)
        {
            return row_index - 2 * channel_id + 2;
        }
        // ----------------------------------------------------------------------
        private static int TipShuttleOffsetFromChannel( Channel channel)
        {
            return TipShuttleOffsetFromChannelAndRow( channel.ID, channel.TipWell.RowIndex);
        }
        // ----------------------------------------------------------------------
        private static int RowIndexFromChannelAndOffset( byte channel_id, int tip_shuttle_offset)
        {
            return tip_shuttle_offset + 2 * channel_id - 2;
        }
        // ----------------------------------------------------------------------
        private void RevisedTipServiceThreadRunner()
        {
            // initialize uninitialized tip shuttles.
            foreach( TipShuttle tip_shuttle in TipShuttles){
                // if shuttle is uninitialized, then ask user how to initialize it.
                if( tip_shuttle.GetState() == TipShuttleState.Uninitialized){
                    WindowsDispatcher.Invoke( new Action( () => {
                        switch( MessageBox.Show( String.Format( "Are tips in tip carrier {0} clean?  'Yes' if clean.  'No' if dirty.  'Cancel' to load clean tips from a tipbox on Stage 3.", tip_shuttle.ID), "Are tips clean?", MessageBoxButtons.YesNoCancel)){
                            case DialogResult.Yes:
                                tip_shuttle.TipCarrier.SetAll( TipWellState.Clean);
                                tip_shuttle.SetState( TipShuttleState.ServingTips);
                                break;
                            case DialogResult.No:
                                tip_shuttle.TipCarrier.SetAll( TipWellState.Dirty);
                                tip_shuttle.SetState( TipShuttleState.ServingTips);
                                break;
                            case DialogResult.Cancel:
                                tip_shuttle.TipCarrier.SetAll( TipWellState.Empty);
                                tip_shuttle.SetState( TipShuttleState.OutOfService);
                                // fill in with dispatched state machine to fill shuttle with tips from tip box.
                                break;
                        }
                    }));
                }
            }

            while( !StopEvent.WaitOne( 0)){

                RunEvent.WaitOne();

                // yield to other threads.
                Thread.Sleep( 10);

                // first deal with shuttle/carrier logic:
                foreach( TipShuttle tip_shuttle in TipShuttles){
                    // if shuttle all dirty and serving tips, then send to cleaners for cleaning.
                    // dispatched job will clean tips in separate thread; once done, tip states will be set to clean and shuttle state will be set to serving tips.
                    if( tip_shuttle.TipCarrier.IsAllDirty() && tip_shuttle.GetState() == TipShuttleState.ServingTips){
                        tip_shuttle.SetState( TipShuttleState.Washing);
                        ProtocolDispatcher.DispatchWashTipsJob( new WashTipsJob( tip_shuttle));
                    }
                }

                // take snapshot of channel statuses.
                var channel_statuses = Hardware.AvailableChannels.ToDictionary( c => c, c => c.Status);

                List< Channel> channels_to_shuck = channel_statuses.Where( cs => cs.Value == Channel.ChannelStatus.ReadyToShuckTip).Select( kvp => kvp.Key).ToList();
                if( channels_to_shuck.Count > 0){
                    channels_to_shuck.ForEach( channel_to_shuck => {
                        TipShuttle tip_shuttle = ( channel_to_shuck.TipWell.Carrier.Stage as TipShuttle);
                        int offset = TipShuttleOffsetFromChannel( channel_to_shuck);
                        if( !OffsetCountdownEvents[ tip_shuttle][ offset].TryAddCount()){
                            OffsetCountdownEvents[ tip_shuttle][ offset].Reset( 1);
                        }
                        // mark it shucking (even if we don't do it immediately).
                        if( ActiveOffsets[ tip_shuttle] == offset){
                            // dispatch now.
                            channel_to_shuck.Status = Channel.ChannelStatus.ShuckingTipInAction;
                            DispatchTipShuck( channel_to_shuck);
                        } else{
                            // dispatch later.
                            channel_to_shuck.Status = Channel.ChannelStatus.ShuckingTipInQueue;
                            Log.DebugFormat( "Queuing shuck tip {0}, {1}, row {2}, col {3}, off {4} because current offset is {5}. ", channel_to_shuck, tip_shuttle, channel_to_shuck.TipWell.RowIndex, channel_to_shuck.TipWell.ColIndex, offset, ActiveOffsets[ tip_shuttle]);
                        }
                    });
                    continue;
                };

                IEnumerable< Channel> channels_ready_to_press = channel_statuses.Where( cs => cs.Value == Channel.ChannelStatus.NoTip).Select( kvp => kvp.Key);
                IEnumerable< Channel> channels_shucking_in_queue = channel_statuses.Where( cs => cs.Value == Channel.ChannelStatus.ShuckingTipInQueue).Select( kvp => kvp.Key);
                IEnumerable< Channel> channels_shucking_in_action = channel_statuses.Where( cs => cs.Value == Channel.ChannelStatus.ShuckingTipInAction).Select( kvp => kvp.Key);

                string active_offsets_debug = String.Join( ";", ActiveOffsets.Select( kvp => String.Format( "shuttle {0} in offset {1}", kvp.Key.ID, kvp.Value)));
                string offset_countdownevents_debug = String.Join( ";", OffsetCountdownEvents.Select( outer => String.Format( "shuttle {0} countdowns (offset, pending) are {1}", outer.Key.ID,
                    String.Join( ", ", outer.Value.Select( inner => String.Format( "({0}, {1})", inner.Key, inner.Value.CurrentCount))))));

                // if there aren't any channels ready to press or shuck, then there's nothing to schedule.
                if( channels_ready_to_press.Count() + channels_shucking_in_queue.Count() + channels_shucking_in_action.Count() == 0){
                    continue;
                }

                // take snapshot of schedulable tip shuttles (filtering out tip shuttles that aren't serving tips).
                var schedulable_tip_shuttle_infos = ( from ts in TipShuttles
                                                      where ts.GetState() == TipShuttleState.ServingTips
                                                      let clean_tips = ts.TipCarrier.CountTipsOfState( TipWellState.Clean)
                                                      let queue_depth = ProtocolDispatcher.GetQueueDepth( ts)
                                                      let busy = ProtocolDispatcher.GetBusy( ts)
                                                      orderby busy, queue_depth, clean_tips
                                                      select new{ TipShuttle = ts, Busy = busy, QueueDepth = queue_depth, CleanTips = clean_tips}).ToList();

                // if there aren't any tip shuttles that are serving tips, then there's nothing to schedule.
                if( schedulable_tip_shuttle_infos.Count == 0){
                    continue;
                }

                schedulable_tip_shuttle_infos.Select( tsi => tsi.TipShuttle).Where( ts => ActiveOffsets[ ts] != int.MaxValue && OffsetCountdownEvents[ ts][ ActiveOffsets[ ts]].CurrentCount == 0).ToList().ForEach( ts => {
                    Log.DebugFormat( "Releasing {0} from offset {1} (1)", ts, ActiveOffsets[ ts]);
                    ActiveOffsets[ ts] = int.MaxValue;
                });

                // if there is a channel that can be pressed, then press it and restart the scheduling loop.
                var PressInfo = ( from ts in schedulable_tip_shuttle_infos
                                  // only schedule presses on shuttles where the queue is empty (this means the shuttle is either idle or in the process of executing the last press).
                                  where ts.QueueDepth == 0 
                                  let offset = ActiveOffsets[ ts.TipShuttle]
                                  // only schedule presses on shuttles that are engaged in a valid offset.
                                  where offset != int.MaxValue 
                                  let channels_that_can_press_now = channels_ready_to_press.Where( c => ts.TipShuttle.TipCarrier.CountTipsOfStateInRow( TipWellState.Clean, RowIndexFromChannelAndOffset( c.ID, offset)) > 0)
                                  let num_channels_that_can_press_now = channels_that_can_press_now.Count()
                                  // only schedule presses on shuttles that can accept channels presses in the currently engaged offset.
                                  where num_channels_that_can_press_now > 0
                                  // first priority, schedule idle shuttles.
                                  // second priority, schedule shuttles that have less channel flexibility.
                                  orderby ts.Busy, num_channels_that_can_press_now
                                  select new{ TipShuttle = ts.TipShuttle, ChannelsThatCanPressNow = channels_that_can_press_now}).FirstOrDefault();
                if( PressInfo != null){
                    TipShuttle tip_shuttle = PressInfo.TipShuttle;
                    Channel channel_to_press = PressInfo.ChannelsThatCanPressNow.First();
                    int active_offset = ActiveOffsets[ tip_shuttle];
                    if( !OffsetCountdownEvents[ tip_shuttle][ active_offset].TryAddCount()){
                        OffsetCountdownEvents[ tip_shuttle][ active_offset].Reset( 1);
                    }
                    channel_to_press.Status = Channel.ChannelStatus.PressingTip;
                    TipWell selected_tip_well = tip_shuttle.TipCarrier.ReserveTip( RowIndexFromChannelAndOffset( channel_to_press.ID, active_offset));
                    channel_to_press.TipWell = selected_tip_well;
                    // dispatch now.
                    DispatchTipPress( channel_to_press, null);
                    continue;
                }

                foreach( var ts in schedulable_tip_shuttle_infos.Select( tsi => tsi.TipShuttle)){
                    if( ActiveOffsets[ ts] == int.MaxValue || OffsetCountdownEvents[ ts][ ActiveOffsets[ ts]].CurrentCount == 0){
                        // temporarily set active offset to invalid offset value.
                        if( ActiveOffsets[ ts] != int.MaxValue){
                            Log.DebugFormat( "Releasing {0} from offset {1} (2)", ts, ActiveOffsets[ ts]);
                            ActiveOffsets[ ts] = int.MaxValue;
                        }
                        // possibly switch to a different offset.
                        int max_count = OffsetCountdownEvents[ ts].Max( kvp => kvp.Value.CurrentCount);
                        if( max_count > 0){
                            int new_offset = OffsetCountdownEvents[ ts].First( kvp => kvp.Value.CurrentCount == max_count).Key;
                            Log.DebugFormat( "Attaching {0} to offset {1} to shuck", ts, new_offset);
                            ActiveOffsets[ ts] = new_offset;
                            // dispatch shuckers.
                            var channels_to_dispatch = channels_shucking_in_queue.Where( c => c.TipWell.Carrier.Stage == ts && TipShuttleOffsetFromChannel( c) == new_offset);
                            bool stage_moved = false;
                            foreach( Channel channel_to_dispatch in channels_to_dispatch){
                                if( !stage_moved){
                                    ManualResetEvent stage_available = new ManualResetEvent( false);
                                    StageAvailableEvents[ ts] = stage_available;
                                    ProtocolDispatcher.DispatchWashableMoveShuttle( ts, new List< Channel>{ channel_to_dispatch}, stage_available);
                                    stage_moved = true;
                                }
                                // dispatch now.
                                channel_to_dispatch.Status = Channel.ChannelStatus.ShuckingTipInAction;
                                DispatchTipShuck( channel_to_dispatch);
                            }
                        } else{
                            var ordered_offsets = from o in PossibleOffsets
                                                  orderby Math.Abs( o), o descending
                                                  select o;
                            var x = from offset in ordered_offsets
                                    from channel in Hardware.AvailableChannels.Where( c => c.Status == Channel.ChannelStatus.NoTip)
                                    where ts.TipCarrier.CountTipsOfStateInRow( TipWellState.Clean, RowIndexFromChannelAndOffset( channel.ID, offset)) > 0
                                    select new{ Channel = channel, Offset = offset};
                            var y = x.FirstOrDefault();
                            if( y != null){
                                ActiveOffsets[ ts] = y.Offset;
                                Log.DebugFormat( "Attaching {0} to offset {1} to press", ts, y.Offset);
                                if( !OffsetCountdownEvents[ ts][ y.Offset].TryAddCount()){
                                    OffsetCountdownEvents[ ts][ y.Offset].Reset( 1);
                                }
                                y.Channel.Status = Channel.ChannelStatus.PressingTip;
                                TipWell selected_tip_well = ts.TipCarrier.ReserveTip( RowIndexFromChannelAndOffset( y.Channel.ID, y.Offset));
                                y.Channel.TipWell = selected_tip_well;
                                ManualResetEvent stage_available = new ManualResetEvent( false);
                                StageAvailableEvents[ ts] = stage_available;
                                DispatchTipPress( y.Channel, stage_available);
                            }
                        }
                    }
                }
            }
        }
        // ----------------------------------------------------------------------
        private void DispatchTipPress( Channel channel, ManualResetEvent stage_available)
        {
            TipShuttle tip_shuttle = ( channel.TipWell.Carrier.Stage as TipShuttle);
            int offset = TipShuttleOffsetFromChannel( channel);
            // message.
            Log.DebugFormat( "Dispatching press tip {0}, {1}, row {2}, col {3}, off {4}. ", channel, tip_shuttle, channel.TipWell.RowIndex, channel.TipWell.ColIndex, offset);
            // dispatch.
            ProtocolDispatcher.DispatchWashableTipsOn( channel, stage_available, OffsetCountdownEvents[ tip_shuttle][ offset], true);
        }
        // ----------------------------------------------------------------------
        private void DispatchTipShuck( Channel channel)
        {
            TipShuttle tip_shuttle = ( channel.TipWell.Carrier.Stage as TipShuttle);
            int offset = TipShuttleOffsetFromChannel( channel);
            // message.
            Log.DebugFormat( "Dispatching shuck tip {0}, {1}, row {2}, col {3}, off {4}. ", channel, tip_shuttle, channel.TipWell.RowIndex, channel.TipWell.ColIndex, offset);
            // dispatch.
            ProtocolDispatcher.DispatchWashableTipsOff( channel, StageAvailableEvents[ tip_shuttle], OffsetCountdownEvents[ tip_shuttle][ offset]);
        }
        // ----------------------------------------------------------------------
        // not used -- used for "batched" tip on/tips off, allowing at most one tip on and as many tips off as possible.
        //private void TipServiceThreadRunner()
        //{
        //    // initialize uninitialized tip shuttles.
        //    foreach( TipShuttle tip_shuttle in TipShuttles){
        //        // if shuttle is uninitialized, then ask user how to initialize it.
        //        if( tip_shuttle.GetState() == TipShuttleState.Uninitialized){
        //            WindowsDispatcher.Invoke( new Action( () => {
        //                switch( MessageBox.Show( "Are tips in the tip carrier clean?  'Yes' if clean.  'No' if dirty.  'Cancel' to load clean tips from a tipbox on Stage 3.", "Are tips clean?", MessageBoxButtons.YesNoCancel)){
        //                    case DialogResult.Yes:
        //                        tip_shuttle.TipCarrier.SetAll( TipCarrierWellState.Clean);
        //                        tip_shuttle.SetState( TipShuttleState.ServingTips);
        //                        break;
        //                    case DialogResult.No:
        //                        tip_shuttle.TipCarrier.SetAll( TipCarrierWellState.Dirty);
        //                        tip_shuttle.SetState( TipShuttleState.ServingTips);
        //                        break;
        //                    case DialogResult.Cancel:
        //                        tip_shuttle.TipCarrier.SetAll( TipCarrierWellState.Empty);
        //                        tip_shuttle.SetState( TipShuttleState.OutOfService);
        //                        // fill in with dispatched state machine to fill shuttle with tips from tip box.
        //                        break;
        //                }
        //            }));
        //        }
        //    }

        //    IDictionary< TipShuttle, DateTime> last_scheduled_job = TipShuttles.ToDictionary( ts => ts, ts => DateTime.Now);

        //    while( !StopEvent.WaitOne( 0)){
        //        // yield to other threads.
        //        Thread.Sleep( 10);

        //        // first deal with shuttle/carrier logic:
        //        foreach( TipShuttle tip_shuttle in TipShuttles){
        //            // if shuttle all dirty and serving tips, then send to cleaners for cleaning.
        //            // dispatched job will clean tips in separate thread; once done, tip states will be set to clean and shuttle state will be set to serving tips.
        //            if( tip_shuttle.TipCarrier.IsAllDirty() && tip_shuttle.GetState() == TipShuttleState.ServingTips){
        //                tip_shuttle.SetState( TipShuttleState.Washing);
        //                ProtocolDispatcher.DispatchWashTipsJob( new WashTipsJob( tip_shuttle));
        //            }
        //        }

        //        // a "tip-shuttle info" consists of a tip shuttle, whether or not that shuttle is busy, and how many more clean tips it has remaining.
        //        // for all tip shuttles that are serving tips and have nothing queued up (although it may still be busy with a current job), compile tip-shuttle info in schedulable_tip_shuttle_infos.
        //        var schedulable_tip_shuttle_infos = ( from tip_shuttle in TipShuttles
        //                                              where tip_shuttle.GetState() == TipShuttleState.ServingTips && ProtocolDispatcher.GetQueueDepth( tip_shuttle) == 0
        //                                              select new{ TipShuttle = tip_shuttle, Busy = ProtocolDispatcher.GetBusy( tip_shuttle), CleanTips = tip_shuttle.TipCarrier.CountTipsOfState( TipCarrierWellState.Clean)}
        //                                              ).ToList(); // call ToList to snapshot tip-shuttle infos.
        //        // get snapshot of channel statuses.
        //        var channel_statuses = SharedMemory.GetChannelStatuses();
        //        // determine channels that have no tips. (which channels need to press tips?)
        //        var channels_ready_for_press = from cs in channel_statuses
        //                                        where cs.Value == ServiceSharedMemory.ChannelStatus.NoTip
        //                                        select cs.Key;
        //        int num_channels_ready_for_press = channels_ready_for_press.Count();
        //        var channels_ready_for_shuck = from cs in channel_statuses
        //                                        where cs.Value == ServiceSharedMemory.ChannelStatus.ReadyToShuckTip
        //                                        select cs.Key;
        //        int num_channels_ready_for_shuck = channels_ready_for_shuck.Count();
        //        // if there aren't any channels that are ready to press or shuck, then there's nothing to schedule.
        //        if(( num_channels_ready_for_press == 0) && ( num_channels_ready_for_shuck == 0)){
        //            continue;
        //        }
        //        /*
        //        if( schedulable_tip_shuttle_infos.Count( stsi => !stsi.Busy) == 0){
        //            if( num_channels_ready_for_press == 0){
        //                continue;
        //            }
        //        }
        //        */
        //        var shuttles_with_shucks = channels_ready_for_shuck.Select( crfs => crfs.Tip.Parent.TipShuttle).Distinct();
        //        var ordered_schedulable_tip_shuttles = from stsi in schedulable_tip_shuttle_infos
        //                                               orderby shuttles_with_shucks.Contains( stsi.TipShuttle) descending, stsi.Busy, last_scheduled_job[ stsi.TipShuttle]
        //                                               select stsi.TipShuttle;
        //        foreach( TipShuttle tip_shuttle in ordered_schedulable_tip_shuttles){
        //            // get snapshot of channel statuses.
        //            channel_statuses = SharedMemory.GetChannelStatuses();
        //            // determine channels that have no tips. (which channels need to press tips?)
        //            var no_tip_channels = from cs in channel_statuses
        //                                    where cs.Value == ServiceSharedMemory.ChannelStatus.NoTip
        //                                    select cs.Key;
        //            // a "channel info" consists of a channel, its status (no tip, pressing, clean tip, using, dirty tip, shucking), its tip (which can be traced back to its tip carrier), and its offset.
        //            // aggregate channel info: statuses from shared memory and tip info from channel/tip map.
        //            var channel_infos = from cs in channel_statuses
        //                                where cs.Key.Tip != null
        //                                select new{ Channel = cs.Key, Status = cs.Value, Tip = cs.Key.Tip, Offset = TipShuttleOffsetFromChannelAndRow( cs.Key.ID, cs.Key.TipCarrierWellState.RowIndex)};
        //            // filter channel_infos down to channels that have dirty tips whose tips belong to this tip shuttle.
        //            // i.e., can we schedule tip shuck(s) for this tip shuttle?
        //            var dirty_tip_channel_infos_on_current_shuttle = channel_infos.Where( ci => ci.Status == ServiceSharedMemory.ChannelStatus.ReadyToShuckTip && ci.Tip.Parent == tip_shuttle.TipCarrier);
        //            // if there are tips to shuck for this tip shuttle, then prioritize shucking.
        //            // we prioritize shucking over pressing because
        //            // shucking channels can only go to a single place (they can only go to from where their tips came) whereas
        //            // pressing channels can pick from any place that has a clean tip.
        //            if( dirty_tip_channel_infos_on_current_shuttle.Count() > 0){
        //                // group dirty channels by the offset they need to perform tip shuck and count number of channels that want to shuck into that offset.
        //                var shuck_offset_counts = from dtciocs in dirty_tip_channel_infos_on_current_shuttle
        //                                            group dtciocs by dtciocs.Offset into offset_group
        //                                            select new{ Offset = offset_group.Key, Count = offset_group.Count()};

        //                var channel_offset_infos = from soc in shuck_offset_counts
        //                                            from c in channel_statuses.Keys
        //                                            select new{ Channel = c, Offset = soc.Offset, OffsetCount = soc.Count, ChannelHasNoTip = no_tip_channels.Contains( c), CleanTipCount = tip_shuttle.TipCarrier.CountTipsOfStateInRow( TipCarrierWellState.Clean, RowIndexFromChannelAndOffset( c.ID, soc.Offset))};

        //                var ordered_channel_offset_infos = from coi in channel_offset_infos
        //                                                    orderby coi.OffsetCount descending, coi.ChannelHasNoTip descending, coi.CleanTipCount != 0 descending, coi.CleanTipCount, Math.Abs( coi.Offset), coi.Offset descending, coi.Channel.ID
        //                                                    select coi;

        //                var selected_channel_offset_info = ordered_channel_offset_infos.FirstOrDefault();

        //                string message = "";
        //                if( selected_channel_offset_info != null){
        //                    Channel selected_channel = null;
        //                    Tip selected_tip_well = null;
        //                    if( selected_channel_offset_info.ChannelHasNoTip && selected_channel_offset_info.CleanTipCount > 0){
        //                        // parse out selected channel and selected row-index.
        //                        selected_channel = selected_channel_offset_info.Channel;
        //                        int selected_row_index = RowIndexFromChannelAndOffset( selected_channel.ID, selected_channel_offset_info.Offset);
        //                        // set the channel to pressing-tip status.
        //                        SharedMemory.SetChannelState( selected_channel, ServiceSharedMemory.ChannelStatus.PressingTip);
        //                        // reserve tip transitions the selected tip from clean to in use.
        //                        selected_tip_well = tip_shuttle.TipCarrier.ReserveTip( selected_row_index);
        //                        // add the assignment to the channel-to-tip map.
        //                        selected_channel.Tip = selected_tip_well;
        //                        // log the tip press.
        //                        message += String.Format( "Press tip channel {0}, shuttle {1}, row {2}, col {3}, off {4}. ", selected_channel.ID, tip_shuttle.ID, selected_tip_well.RowIndex, selected_tip_well.ColIndex, selected_channel_offset_info.Offset);
        //                    } else{
        //                        // if this tip shuttle is currently busy, then don't rush to schedule shucks without a press.
        //                        if( ProtocolDispatcher.GetBusy( tip_shuttle)){
        //                            continue;
        //                        }
        //                    }
        //                    var shuck_channels = dirty_tip_channel_infos_on_current_shuttle.Where( dtciocs => dtciocs.Offset == selected_channel_offset_info.Offset).Select( dtciocs => dtciocs.Channel);
        //                    foreach( var shuck_channel in shuck_channels){
        //                        SharedMemory.SetChannelState( shuck_channel, ServiceSharedMemory.ChannelStatus.ShuckingTipInAction);
        //                        message += String.Format( "Shuck tip channel {0}, shuttle {1}, row {2}, col {3}, off {4}. ", shuck_channel.ID, tip_shuttle.ID, shuck_channel.Tip.RowIndex, shuck_channel.TipCarrierWellState.ColIndex, selected_channel_offset_info.Offset);
        //                    }
        //                    // log the tip shuck(s) and the optional tip press.
        //                    foreach( TipShuttle ts in ordered_schedulable_tip_shuttles){
        //                        message += String.Format( "sh{0} is {1}. ", ts.ID, ProtocolDispatcher.GetBusy( ts) ? "busy" : "idle");
        //                    }
        //                    Log.InfoFormat( message);
        //                    // dispatch the job.
        //                    ProtocolDispatcher.DispatchWashableTipsJob( new WashableTipsJob( LabwareDatabase, tip_shuttle, selected_channel, shuck_channels.ToList()));
        //                    last_scheduled_job[ tip_shuttle] = DateTime.Now;
        //                } else{
        //                    throw new Exception( "what happened?");
        //                }
        //            } else /* there are no tips to shuck for this tip shuttle; try to schedule a press. */{
        //                // if there aren't any channels awaiting tip press, then continue (schedule activities on next tip shuttle)....
        //                if( no_tip_channels.Count() == 0){
        //                    continue;
        //                }
        //                // if there aren't any clean tips in the tip shuttle, then continue (schedule activities on next tip shuttle)....
        //                if( tip_shuttle.TipCarrier.CountTipsOfState( TipCarrierWellState.Clean) == 0){
        //                    continue;
        //                }
        //                // group channels that have tip assignments (active channels) by their offset and count number of channels assigned that offset.
        //                // these are the "active" offsets and their respective counts.
        //                var active_offset_counts = from ci in channel_infos
        //                                            where ci.Tip.Parent == tip_shuttle.TipCarrier
        //                                            group ci by ci.Offset into offset_group
        //                                            select new{ Offset = offset_group.Key, Count = offset_group.Count()};
        //                // calculate lowest possible offset, highest possible offset, and number of total offsets.
        //                int lowest_offset = TipShuttleOffsetFromChannelAndRow( 8, 0);
        //                int highest_offset = TipShuttleOffsetFromChannelAndRow( 1, tip_shuttle.TipCarrier.Rows - 1);
        //                int num_offsets = highest_offset - lowest_offset + 1;
        //                // any offset that isn't active is inactive.
        //                // create a list of inactive offsets and their respective counts (necessarily zero since they're inactive).
        //                var inactive_offset_counts = Enumerable.Range( lowest_offset, num_offsets).Except( active_offset_counts.Select( aoc => aoc.Offset)).Select( x => new{ Offset = x, Count = 0});
        //                // concatenate the active and inactive offset counts to arrive at a list of all offsets and their respective counts.
        //                var offset_counts = active_offset_counts.Concat( inactive_offset_counts);
        //                // cross offset counts with channels that have no tips to arrive at a list of channel-offset infos.
        //                // a "channel-offset info" consists of
        //                // - a channel (that doesn't have a tip),
        //                // - an offset,
        //                // - that offset's count of channels that are active on that offset, and
        //                // - the count of clean tips available in the row that corresponds to the subject offset for the subject channel.
        //                var channel_offset_infos = from oc in offset_counts
        //                                            from ntc in no_tip_channels
        //                                            select new{ Channel = ntc, Offset = oc.Offset, OffsetCount = oc.Count, CleanTipCount = tip_shuttle.TipCarrier.CountTipsOfStateInRow( TipCarrierWellState.Clean, RowIndexFromChannelAndOffset( ntc.ID, oc.Offset))};
        //                // eliminating channel-offset infos where clean tips are unavailable, prioritize the channel-offset infos.
        //                // first, prefer channel-offset infos where the offset has a high count (i.e., many channels are assigned to use the offset), doing so will set up the future potential for simultaneous shucks.
        //                // next, prefer lower clean-tip counts to preferentially use up all the clean tips in an offset and prevent bouncing back and forth between offsets.
        //                // next, prefer lower absolute ordinal offsets to stay near offset zero and minimize tip-shuttle movement.
        //                // next, prefer higher (non-absolute) ordinal offsets to prioritize offsetting in the positive direction rather than the negative direction.
        //                var ordered_channel_offset_infos = from coi in channel_offset_infos
        //                                                    where coi.CleanTipCount != 0
        //                                                    orderby coi.OffsetCount descending, coi.CleanTipCount, Math.Abs( coi.Offset), coi.Offset descending, coi.Channel.ID
        //                                                    select coi;
        //                // pick off the first channel-offet info that has been thus ordered.
        //                var selected_channel_offset_info = ordered_channel_offset_infos.FirstOrDefault();
        //                // if selected channel-offset info is valid, then dispatch the job.
        //                if( selected_channel_offset_info != null){
        //                    // parse out selected channel and selected row-index.
        //                    Channel selected_channel = selected_channel_offset_info.Channel;
        //                    int selected_row_index = RowIndexFromChannelAndOffset( selected_channel.ID, selected_channel_offset_info.Offset);
        //                    // set the channel to pressing-tip status.
        //                    SharedMemory.SetChannelState( selected_channel, ServiceSharedMemory.ChannelStatus.PressingTip);
        //                    // reserve tip transitions the selected tip from clean to in use.
        //                    Tip selected_tip_well = tip_shuttle.TipCarrier.ReserveTip( selected_row_index);
        //                    // add the assignment to the channel-to-tip map.
        //                    selected_channel.Tip = selected_tip_well;
        //                    // log the tip press.
        //                    string message = String.Format( "Press tip channel {0}, shuttle {1}, row {2}, col {3}, off {4}. ", selected_channel.ID, tip_shuttle.ID, selected_tip_well.RowIndex, selected_tip_well.ColIndex, selected_channel_offset_info.Offset);
        //                    foreach( TipShuttle ts in ordered_schedulable_tip_shuttles){
        //                        message += String.Format( "sh{0} is {1}. ", ts.ID, ProtocolDispatcher.GetBusy( ts) ? "busy" : "idle");
        //                    }
        //                    Log.InfoFormat( message);
        //                    // dispatch the job.
        //                    ProtocolDispatcher.DispatchWashableTipsJob( new WashableTipsJob( LabwareDatabase, tip_shuttle, selected_channel, new List< Channel>()));
        //                    last_scheduled_job[ tip_shuttle] = DateTime.Now;
        //                } else /* what happened? */{
        //                    throw new Exception( "what happened?");
        //                }
        //            }
        //        }
        //    }
        //}
    }
}
