using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using BioNex.BumblebeePlugin.Dispatcher;
using BioNex.BumblebeePlugin.Hardware;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.ThreadsafeMessenger;
using BioNex.Shared.Utils.WellMathUtil;
using log4net;

namespace BioNex.BumblebeePlugin.Scheduler.DualChannelScheduler
{
    /// <summary>
    /// This class provides a service that executes only during a protocol, and
    /// is responsible for consuming Transfer objects from source plate
    /// stages' queues and assigning channel(s) to each transfer
    /// </summary>
    public class ChannelService : IDisposable
    {
        // ----------------------------------------------------------------------
        // inner classes.
        // ----------------------------------------------------------------------
        public class TransferPair
        {
            public double SrcSeparation;
            public double DstSeparation;
            public double MinSeparation;
            public Transfer Transfer1;
            public Transfer Transfer2;
        }

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        private BBHardware Hardware { get; set; }
        private ILiquidProfileLibrary LiquidProfileLibrary { get; set; }
        private ThreadsafeMessenger BumblebeeMessenger { get; set; }
        private ServiceSharedMemory SharedMemory { get; set; }
        private BumblebeeDispatcher ProtocolDispatcher { get; set; }

        private AutoResetEvent StopEvent { get; set; }
        private ManualResetEvent RunEvent { get; set; }
        private Thread ChannelServiceThread { get; set; }

        // stage cache -- doesn't really ever change
        private IEnumerable< Stage> SourceStages { get; set; }
        private Stage DestinationStage { get; set; }

        private ConcurrentQueue< Channel> ChannelsToDisable { get; set; }

        // ----------------------------------------------------------------------
        // members.
        // ----------------------------------------------------------------------
        private static readonly ILog Log = LogManager.GetLogger( typeof( ChannelService));

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public ChannelService( BBHardware hardware, ILiquidProfileLibrary liquid_profile_library, ThreadsafeMessenger messenger, ServiceSharedMemory shared_memory, BumblebeeDispatcher protocol_dispatcher)
        {
            Hardware = hardware;
            LiquidProfileLibrary = liquid_profile_library;
            BumblebeeMessenger = messenger;
            SharedMemory = shared_memory;
            ProtocolDispatcher = protocol_dispatcher;

            BumblebeeMessenger.Register< DisableChannelMessage>( this, HandleDisableChannelMessage);
            BumblebeeMessenger.Register< TipPressedOnMessage>( this, HandleTipsPressedOnMessage);
            BumblebeeMessenger.Register< AspirateCompleteMessage>( this, HandleAspirateCompleteMessage);
            BumblebeeMessenger.Register< TransferCompleteMessage>( this, HandleTransferCompleteMessage);
            BumblebeeMessenger.Register< TipOffPreMoveCompleteMessage>( this, HandleTipOffPreMoveCompleteMessage);
            BumblebeeMessenger.Register< TipOffCompleteMessage>( this, HandleTipsOffCompleteMessage);
            BumblebeeMessenger.Register< TipOnCompleteMessage>( this, HandleTipsOnCompleteMessage);
            BumblebeeMessenger.Register< TransferAbortedMessage>( this, HandleTransferAbortedMessage);
            BumblebeeMessenger.Register< WashTipsCompleteMessage>( this, HandleWashTipsCompleteMessage);
            BumblebeeMessenger.Register< AbortCommand>( this, SystemAbort);
            BumblebeeMessenger.Register< PauseCommand>( this, SystemPause);
            BumblebeeMessenger.Register< ResumeCommand>( this, SystemResume);

            // cache the stage references
            // we always assume that the last stage in a group of stages is the tipbox stage
            IEnumerable< Stage> all_stages = Hardware.Stages.OrderBy( stage => stage.ID);
            SourceStages = all_stages.Take( 2);
            DestinationStage = all_stages.Skip( 2).Take( 1).First();

            ChannelsToDisable = new ConcurrentQueue< Channel>();

            StopEvent = new AutoResetEvent( false);
            RunEvent = new ManualResetEvent( true);
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        private void HandleDisableChannelMessage( DisableChannelMessage message)
        {
            ChannelsToDisable.Enqueue( message.Channel);
        }
        // ----------------------------------------------------------------------
        private void HandleTipsPressedOnMessage( TipPressedOnMessage message)
        {
            message.TipWell.SetState( TipWellState.InUse);
        }
        // ----------------------------------------------------------------------
        private void HandleAspirateCompleteMessage( AspirateCompleteMessage message)
        {
            SharedMemory.SetTransferStatus( message.Transfer, ServiceSharedMemory.TransferStatus.Dispensing);
        }
        // ----------------------------------------------------------------------
        private void HandleTransferCompleteMessage( TransferCompleteMessage message)
        {
            Log.DebugFormat( "Finished LiquidTransfer for {0}, {1}, Ready to pre-shuck", message.Channel, message.Channel.TipWell.Carrier.Stage);
            message.Channel.Status = Channel.ChannelStatus.DirtyTip;
            SharedMemory.RemoveTransfer( message.Transfer);
        }
        // ----------------------------------------------------------------------
        private void HandleTransferAbortedMessage( TransferAbortedMessage message)
        {
            Log.DebugFormat( "Finished LiquidTransfer (by error) for {0}, {1}, Ready to pre-shuck", message.Channel, message.Channel.TipWell.Carrier.Stage);
            message.Channel.Status = Channel.ChannelStatus.DirtyTip;
            SharedMemory.SetTransferStatus( message.Transfer, ServiceSharedMemory.TransferStatus.NotStarted);
        }
        // ----------------------------------------------------------------------
        private void HandleTipOffPreMoveCompleteMessage( TipOffPreMoveCompleteMessage message)
        {
            Log.DebugFormat( "Finished TipOffPreMove for {0}, {1}, Ready to shuck", message.Channel, message.Channel.TipWell.Carrier.Stage);
            message.Channel.Status = Channel.ChannelStatus.ReadyToShuckTip;
        }
        // ----------------------------------------------------------------------
        private void HandleTipsOffCompleteMessage( TipOffCompleteMessage message)
        {
            if( message.Channel.TipWell != null){
                Log.DebugFormat( "Finished TipOffPostMove for {0}, {1}, Ready to press", message.Channel, message.Channel.TipWell.Carrier.Stage);
            } else{
                Log.DebugFormat( "Finished TipOffPostMove for {0}", message.Channel);
            }
            message.Channel.Status = Channel.ChannelStatus.NoTip;
            message.Channel.TipWell.SetState( TipWellState.Dirty);
            message.Channel.TipWell = null;
        }
        // ----------------------------------------------------------------------
        private void HandleTipsOnCompleteMessage( TipOnCompleteMessage message)
        {
            if( message.Channel.TipWell != null){
                Log.DebugFormat( "Finished TipOn for {0}, {1}", message.Channel, message.Channel.TipWell.Carrier.Stage);
            } else{
                Log.DebugFormat( "Finished TipOn for {0}", message.Channel);
            }
            message.Channel.Status = Channel.ChannelStatus.CleanTip;
        }
        // ----------------------------------------------------------------------
        private void HandleWashTipsCompleteMessage( WashTipsCompleteMessage message)
        {
            message.TipShuttle.TipCarrier.SetAll( TipWellState.Clean);
            message.TipShuttle.SetState( TipShuttleState.ServingTips);
        }
        // ----------------------------------------------------------------------
        public void StartService()
        {
            ChannelServiceThread = new Thread( new ThreadStart( RevisedChannelServiceThreadRunner)){ Name = "DS channel service", IsBackground = true};
            ChannelServiceThread.Start();
        }
        // ----------------------------------------------------------------------
        public void StopService()
        {
            if( ChannelServiceThread == null)
                return;
            StopEvent.Set();
        }
        // ----------------------------------------------------------------------
        private void SystemAbort( AbortCommand command)
        {
            Log.Info( "Channel service received Abort() call");
            StopEvent.Set();
        }
        // ----------------------------------------------------------------------
        private void SystemPause( PauseCommand command)
        {
            Log.Info( "Channel service received Pause() call");
        }
        // ----------------------------------------------------------------------
        private void SystemResume( ResumeCommand command)
        {
            Log.Info( "Channel service received Resume() call");
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
        private void DispatchLiquidHandlingJob( Channel channel, Transfer transfer, string message)
        {
            DispatchLiquidHandlingJob( new Dictionary< Channel, Transfer>{{ channel, transfer}}, message);
        }
        // ----------------------------------------------------------------------
        private void DispatchLiquidHandlingJob( IList< Channel> channels, IList< Transfer> transfers, string message)
        {
            Debug.Assert( channels.Count == 2);
            Debug.Assert( transfers.Count == 2);
            DispatchLiquidHandlingJob( new Dictionary< Channel, Transfer>{{ channels[ 0], transfers[ 0]},
                                                                          { channels[ 1], transfers[ 1]}}, message);
        }
        // ----------------------------------------------------------------------
        private void DispatchLiquidHandlingJob( IDictionary< Channel, Transfer> transfers_to_perform, string message = "")
        {
            StringBuilder sb = new StringBuilder();
            foreach( KeyValuePair< Channel,Transfer> kvp in transfers_to_perform){
                string liquid_profile_display = kvp.Value.LiquidProfileName;
                ILiquidProfile liquid_profile = LiquidProfileLibrary.LoadLiquidProfileByName( liquid_profile_display);
                if( liquid_profile != null){
                    string pre_aspirate_liquid_profile_name = liquid_profile.PreAspirateMixLiquidProfile;
                    if( !string.IsNullOrEmpty( pre_aspirate_liquid_profile_name)){
                        ILiquidProfile pre_aspirate_liquid_profile = LiquidProfileLibrary.LoadLiquidProfileByName( pre_aspirate_liquid_profile_name);
                        if(( pre_aspirate_liquid_profile != null) && ( pre_aspirate_liquid_profile.IsMixingProfile)){
                            liquid_profile_display += ( " pre-aspirate mix with " + pre_aspirate_liquid_profile_name);
                        }
                    }
                    string post_dispense_liquid_profile_name = liquid_profile.PostDispenseMixLiquidProfile;
                    if( !string.IsNullOrEmpty( post_dispense_liquid_profile_name)){
                        ILiquidProfile post_dispense_liquid_profile = LiquidProfileLibrary.LoadLiquidProfileByName( post_dispense_liquid_profile_name);
                        if(( post_dispense_liquid_profile != null) && ( post_dispense_liquid_profile.IsMixingProfile)){
                            liquid_profile_display += ( " post-dispense mix with " + post_dispense_liquid_profile_name);
                        }
                    }
                }
                sb.Append( String.Format( " {0}: {1} [{2}]", kvp.Key.ID, kvp.Value.ToString(), liquid_profile_display));
            }
            Log.InfoFormat( "Scheduled the following transfers: {0}{1}", sb.ToString(), String.IsNullOrEmpty( message) ? "" : String.Format( " Annotation: {0}", message));
            foreach( Transfer transfer_scheduled in transfers_to_perform.Values){
                SharedMemory.SetTransferStatus( transfer_scheduled, ServiceSharedMemory.TransferStatus.Aspirating);
            }
            // set the channel states to Used
            foreach( Channel c in transfers_to_perform.Keys){
                c.Status = Channel.ChannelStatus.UsingTip;
            }
            Stage source_stage = SourceStages.First( ss => ss.Plate == transfers_to_perform.Values.Select( ttp => ttp.SrcPlate).Distinct().First());
            LiquidTransferJob job = new LiquidTransferJob( source_stage, DestinationStage, transfers_to_perform);
            ProtocolDispatcher.DispatchLiquidTransferJob( job);
        }
        // ----------------------------------------------------------------------
        private void RevisedChannelServiceThreadRunner()
        {
            if( SourceStages.Count() != 2){
                throw new Exception( "this scheduler designed with assumption of exactly two source stages");
            }

            // run until told to stop.
            while( !StopEvent.WaitOne( 0)){

                RunEvent.WaitOne();

                // yield to other threads.
                Thread.Sleep( 10);

                bool dequeue_succeeded;
                do{
                    Channel channel_to_disable;
                    dequeue_succeeded = ChannelsToDisable.TryDequeue( out channel_to_disable);
                    if( dequeue_succeeded){
                        channel_to_disable.Status = Channel.ChannelStatus.Disabled;
                    }
                } while( dequeue_succeeded);

                Stopwatch stopwatch = Stopwatch.StartNew();

                // get snapshot of channel statuses.
                IDictionary< Channel, Channel.ChannelStatus> channel_statuses = Hardware.AvailableChannels.ToDictionary( c => c, c => c.Status);

                string csstring = String.Join( "", channel_statuses.Select( y => y.Value.ToString().First()));

                // clean-channel info.
                IEnumerable< Channel> clean_channels = channel_statuses.Where( channel_status => channel_status.Value == Channel.ChannelStatus.CleanTip).Select( channel_status => channel_status.Key);
                int num_clean_channels = clean_channels.Count();

                // if there aren't any clean channels, then there's nothing to schedule.
                if( num_clean_channels == 0){
                    continue;
                }

                // compute clean adjacency groups.
                IList< IList< Channel>> clean_adjacency_groups = SplitIntoAdjacencyGroups( clean_channels);

                // pressing-channel info.
                IEnumerable< Channel> pressing_channels = channel_statuses.Where( channel_status => channel_status.Value == Channel.ChannelStatus.PressingTip).Select( channel_status => channel_status.Key);
                int num_pressing_channels = pressing_channels.Count();
                IList< IList< Channel>> pressing_adjacency_groups = SplitIntoAdjacencyGroups( pressing_channels);

                IEnumerable< Channel> no_tip_channels = channel_statuses.Where( channel_status => channel_status.Value == Channel.ChannelStatus.NoTip).Select( channel_status => channel_status.Key);
                int num_no_tip_channels = no_tip_channels.Count();

                IEnumerable< Channel> shucking_channels = channel_statuses.Where( channel_status => channel_status.Value == Channel.ChannelStatus.ShuckingTipInAction).Select( channel_status => channel_status.Key);
                int num_shucking_channels = shucking_channels.Count();

                // aggregate clean- and pressing-channel info.
                IEnumerable< Channel> clean_or_pressing_channels = clean_channels.Union( pressing_channels);
                int num_clean_or_pressing_channels = clean_or_pressing_channels.Count();
                IList< IList< Channel>> clean_or_pressing_adjacency_groups = SplitIntoAdjacencyGroups( clean_or_pressing_channels);

                IEnumerable< byte> pressing_channel_neighbor_ids = pressing_channels.Select( channel => ( byte)( channel.ID - 1)).Union( pressing_channels.Select( channel => ( byte)( channel.ID + 1)));

                IEnumerable< Channel> clean_channels_not_next_to_pressing_channels = clean_channels.Where( channel => !pressing_channel_neighbor_ids.Contains( channel.ID));
                IList< IList< Channel>> clean_not_next_to_pressing_adjacency_groups = SplitIntoAdjacencyGroups( clean_channels_not_next_to_pressing_channels);

                // get snapshot of unstarted transfers on deck.
                IList< Transfer> unstarted_transfers_on_deck = SharedMemory.GetUnstartedTransfersOnDeck();

                // if there aren't any unstarted transfers on deck, then there's nothing to schedule.
                if( unstarted_transfers_on_deck.Count() == 0){
                    continue;
                }

                // ----- from this point forward, transfers (and transfer pairs) refer implicitly to UNSTARTED transfers (and transfer pairs) ON DECK -----
                // ----- from this point forward, source plates refer implicitly to source plates ON DECK THAT HAVE UNSTARTED TRANSFERS -----

                // determine the source plates (on deck) that have unstarted transfers.
                IEnumerable< SourcePlate> source_plates = unstarted_transfers_on_deck.Select( transfer => transfer.SrcPlate).Distinct();

                // reorganize unstarted transfers (on deck) by source plate.
                // this dictionary maps source plate (on deck) to unstarted transfers involving that source plate.
                Dictionary< SourcePlate, IEnumerable< Transfer>> transfers_by_source_plate = source_plates.ToDictionary( plate => plate, plate => unstarted_transfers_on_deck.Where( transfer => transfer.SrcPlate == plate));

                // generate lists of unstarted transfer pairs (on deck) by source plate.
                // this dictionary maps source plate (on deck) to unstarted transfer pairs involving that source plate.
                // we eliminate redundant transfer pairs (e.g., pair1=(T1,T2), pair2=(T2,T1)) by only listing pairs where transfer1's well index is less than transfer2's well index.
                // we sort the unstarted transfer pairs by the sum of their separations.
                Dictionary< SourcePlate, IEnumerable< TransferPair>> transfer_pairs_by_source_plate = source_plates.ToDictionary( plate => plate, plate => 
                    from transfer1 in transfers_by_source_plate[ plate]
                    from transfer2 in transfers_by_source_plate[ plate]
                    where WellComparer.TheWellComparer.Compare( transfer1.SrcWell, transfer2.SrcWell) < 0
                    let src_separation = WellMathUtil.CalculateWellSpacing( transfer1.SrcPlate.LabwareFormat, transfer1.SrcWell, transfer2.SrcWell)
                    let dst_separation = WellMathUtil.CalculateWellSpacing( transfer1.DstPlate.LabwareFormat, transfer1.DstWell, transfer2.DstWell)
                    let min_separation = Math.Min( src_separation, dst_separation)
                    orderby min_separation, src_separation, dst_separation
                    select new TransferPair{ Transfer1 = transfer1, Transfer2 = transfer2, SrcSeparation = src_separation, DstSeparation = dst_separation, MinSeparation = min_separation});

                // determine the idle source plates (on deck) that have unstarted transfers.
                IEnumerable< SourcePlate> idle_source_plates = source_plates.Where( plate => !ProtocolDispatcher.GetBusy( Hardware.GetStage( plate)));

                if( idle_source_plates.Count() > 0){
                    if( num_clean_channels > 1){
                        // order clean adjacency groups: prefer even-sized adjacency groups (as we're going to be picking off two channels), then prefer larger adjacency groups (to avoid breaking up smaller groups).
                        IEnumerable< IList< Channel>> clean_adjacency_groups_ordered_for_pair = from adjacency_group in clean_adjacency_groups
                                                                                                let adjacency_group_size = adjacency_group.Count
                                                                                                orderby adjacency_group_size % 2 == 0 descending, adjacency_group_size descending
                                                                                                select adjacency_group;
                        // the first clean adjacency group is the preferred one.
                        IList< Channel> preferred_clean_adjacency_group = clean_adjacency_groups_ordered_for_pair.First();
                        // if the preferred clean adjacency group has multiple channels, then select its first two channels, else select the first two clean channels (guaranteed to be non-adjacent).
                        IList< Channel> selected_channels = (( preferred_clean_adjacency_group.Count > 1) ? preferred_clean_adjacency_group : clean_channels).Take( 2).ToList();
                        // try to dispatch a transfer pair (among all the idle source plates): may fail if a wide enough transfer pair cannot be found.
                        if( null != idle_source_plates.FirstOrDefault( plate => TryDispatchTransferPair( selected_channels, plate, transfer_pairs_by_source_plate[ plate]))){
                            stopwatch.Stop();
                            Log.DebugFormat( "(c) Decision ({1}) made and dispatched in {0}", stopwatch.Elapsed.ToString("G"), csstring);
                            Thread.Sleep( 250);
                            continue;
                        }
                        // on failure to dispatch a transfer pair, fall through to single dispatch.
                    }
                    // sanity check, there should be clean channels.
                    Debug.Assert( num_clean_channels != 0, "num_clean_channels should not be zero");
                    // order clean adjacency groups: prefer singletons first, then prefer odd-sized adjacency groups (as we're going to be picking off one channel), then prefer larger adjacency groups (to avoid breaking up smaller groups).
                    IEnumerable< IList< Channel>> clean_adjacency_groups_ordered_for_singleton = from adjacency_group in clean_adjacency_groups
                                                                                                 let adjacency_group_size = adjacency_group.Count
                                                                                                 orderby adjacency_group_size == 1 descending, adjacency_group_size % 2 == 1 descending, adjacency_group_size descending
                                                                                                 select adjacency_group;
                    // select the preferred clean adjacency group's first channel.
                    Channel selected_channel = clean_adjacency_groups_ordered_for_singleton.First().First();
                    // if there is an idle source plate that only has one unstarted transfer, then pull out its list of unstarted transfers.
                    SourcePlate idle_source_plate_with_one_transfer = idle_source_plates.FirstOrDefault( plate => transfers_by_source_plate[ plate].Count() == 1);
                    if( idle_source_plate_with_one_transfer != null){
                        // single dispatch the only clean channel to handle the singleton transfer.
                        DispatchLiquidHandlingJob( selected_channel, transfers_by_source_plate[ idle_source_plate_with_one_transfer].First(), String.Format( "(1) idle sources = {0}", String.Join( ",", idle_source_plates)));
                        stopwatch.Stop();
                        Log.DebugFormat( "(d) Decision ({1}) made and dispatched in {0}", stopwatch.Elapsed.ToString("G"), csstring);
                        Thread.Sleep( 250);
                        continue;
                    }

                    bool no_clean_channels_on_the_way = ( num_pressing_channels + num_shucking_channels == 0);
                    bool multiple_clean_channels_couldnt_find_work = num_clean_channels > 1;

                    if( no_clean_channels_on_the_way || multiple_clean_channels_couldnt_find_work){

                        // on failure to find a singleton list, the remaining lists of unstarted transfer pairs for the idle source plates must contain transfer pairs.
                        // find the narrowest transfer pair among the lists of unstarted transfer pairs for the idle source plates.
                        TransferPair narrowest_transfer_pair = transfer_pairs_by_source_plate.Where( kvp => idle_source_plates.Contains( kvp.Key)).Select( kvp => kvp.Value.FirstOrDefault()).Where( transfer_pair => transfer_pair != null).OrderBy( transfer_pair => transfer_pair.MinSeparation).FirstOrDefault();
                        Debug.Assert( narrowest_transfer_pair != null, "the remaining lists of unstarted transfer pairs for the idle source plates must contain transfer pairs");
                        // break up the pair by selecting one of its transfers for single dispatch.
                        DispatchLiquidHandlingJob( selected_channel, narrowest_transfer_pair.Transfer1, String.Format( "(2) idle sources = {0}", String.Join( ",", idle_source_plates)));
                        stopwatch.Stop();
                        Log.DebugFormat( "(e) Decision ({1}) made and dispatched in {0}", stopwatch.Elapsed.ToString("G"), csstring);
                        Thread.Sleep( 250);
                        continue;

                    }
                }

                // ----- from this point forward, we're dealing with source plates that are currently busy -----

                SourcePlate source_plate_with_one_transfer = source_plates.FirstOrDefault( plate => transfers_by_source_plate[ plate].Count() == 1);
                if( source_plate_with_one_transfer != null){
                    // order clean adjacency groups: prefer singletons first, then prefer odd-sized adjacency groups (as we're going to be picking off one channel), then prefer larger adjacency groups (to avoid breaking up smaller groups).
                    IEnumerable< IList< Channel>> clean_adjacency_groups_ordered_for_singleton = from adjacency_group in clean_not_next_to_pressing_adjacency_groups
                                                                                                 let adjacency_group_size = adjacency_group.Count
                                                                                                 orderby adjacency_group_size == 1 descending, adjacency_group_size % 2 == 1 descending, adjacency_group_size descending
                                                                                                 select adjacency_group;
                    IList< Channel> preferred_group = clean_adjacency_groups_ordered_for_singleton.FirstOrDefault();
                    if( preferred_group == null){
                        clean_adjacency_groups_ordered_for_singleton = from adjacency_group in clean_adjacency_groups
                                                                                                 let adjacency_group_size = adjacency_group.Count
                                                                                                 orderby adjacency_group_size == 1 descending, adjacency_group_size % 2 == 1 descending, adjacency_group_size descending
                                                                                                 select adjacency_group;
                        preferred_group = clean_adjacency_groups_ordered_for_singleton.First();
                    }
                    Channel selected_channel = preferred_group.First();
                    DispatchLiquidHandlingJob( selected_channel, transfers_by_source_plate[ source_plate_with_one_transfer].First(), String.Format( "(4) busy singleton = {0}", String.Join( ",", idle_source_plates)));
                    stopwatch.Stop();
                    Log.DebugFormat( "(f) Decision ({1}) made and dispatched in {0}", stopwatch.Elapsed.ToString("G"), csstring);
                    Thread.Sleep( 250);
                    continue;
                }

                var prioritized_src_plates = from plate in source_plates
                                             let stage = Hardware.GetStage( plate)
                                             let queue_depth = ProtocolDispatcher.GetQueueDepth( stage)
                                             where queue_depth == 0
                                             orderby queue_depth, ProtocolDispatcher.GetBusy( stage)
                                             select plate;
                if( prioritized_src_plates.Count() > 0){
                    // order clean adjacency groups: prefer even-sized adjacency groups (as we're going to be picking off two channels), then prefer larger adjacency groups (to avoid breaking up smaller groups).
                    IEnumerable< IList< Channel>> clean_adjacency_groups_ordered_for_pair = from adjacency_group in clean_not_next_to_pressing_adjacency_groups
                                                                                            let adjacency_group_size = adjacency_group.Count
                                                                                            where adjacency_group_size > 1
                                                                                            orderby adjacency_group_size % 2 == 0 descending, adjacency_group_size descending
                                                                                            select adjacency_group;
                    IList< Channel> preferred_group = clean_adjacency_groups_ordered_for_pair.FirstOrDefault();
                    if( preferred_group == null){
                        clean_adjacency_groups_ordered_for_pair = from adjacency_group in clean_adjacency_groups
                                                                  let adjacency_group_size = adjacency_group.Count
                                                                  where adjacency_group_size > 1
                                                                  orderby adjacency_group_size % 2 == 0 descending, adjacency_group_size descending
                                                                  select adjacency_group;                    
                        preferred_group = clean_adjacency_groups_ordered_for_pair.FirstOrDefault();
                    }
                    if( preferred_group != null){
                        IList< Channel> selected_channels = preferred_group.Take( 2).ToList();
                        if( null != prioritized_src_plates.FirstOrDefault( plate => TryDispatchTransferPair( selected_channels, plate, transfer_pairs_by_source_plate[ plate]))){
                            stopwatch.Stop();
                            Log.DebugFormat( "(g) Decision ({1}) made and dispatched in {0}", stopwatch.Elapsed.ToString("G"), csstring);
                            Thread.Sleep( 250);
                            continue;
                        }
                        continue;
                    }
                }
                stopwatch.Stop();
                Log.DebugFormat( "(h) Decision ({1}) made (nothing to schedule) in {0}", stopwatch.Elapsed.ToString("G"), csstring);
                Thread.Sleep( 250);
            }
        }
        // ----------------------------------------------------------------------
        private bool TryDispatchTransferPair( IList< Channel> selected_channels, SourcePlate selected_source_plate, IEnumerable< TransferPair> transfer_pairs)
        {
            // calculate source and destination separations for the selected channels.
            double selected_channels_src_separation = Hardware.GetChannelSpacing( selected_channels[ 0].ID, selected_channels[ 1].ID, Hardware.GetStage( selected_source_plate).ID);
            double selected_channels_dst_separation = Hardware.GetChannelSpacing( selected_channels[ 0].ID, selected_channels[ 1].ID, DestinationStage.ID);
            // select the narrowest possible (possible = well separations are wider than channel separations of selected channels at both source and destination)
            // transfer pair remaining in the list of unstarted transfer pairs for the selected source plate.
            TransferPair narrowest_possible_transfer_pair = transfer_pairs.FirstOrDefault( transfer_pair => transfer_pair.SrcSeparation > selected_channels_src_separation && transfer_pair.DstSeparation > selected_channels_dst_separation);
            // if we found a possibility, then perform a double dispatch.
            if( narrowest_possible_transfer_pair != null){
                DispatchLiquidHandlingJob( selected_channels, new List< Transfer>{ narrowest_possible_transfer_pair.Transfer1, narrowest_possible_transfer_pair.Transfer2}, String.Format( "(3) selected source = {0}", selected_source_plate));
                return true;
            }
            return false;
        }
        // ----------------------------------------------------------------------
        #region IDisposable Members
        public void Dispose()
        {
            BumblebeeMessenger.Unregister( this);
            GC.SuppressFinalize( this);
        }
        #endregion

        // ----------------------------------------------------------------------
        // class methods.
        // ----------------------------------------------------------------------
        private static IList< IList< Channel>> SplitIntoAdjacencyGroups( IEnumerable< Channel> channels)
        {
            // start with empty list of adjacency groups.
            IList< IList< Channel>> retval = new List< IList< Channel>>();
            // start with an invalid previous channel id and invalid adjacency group.
            int previous_channel_id = int.MinValue;
            IList< Channel> adjacency_group = null;
            // for each channel....
            foreach( Channel channel in channels.OrderBy( channel => channel.ID)){
                // if this channel is not adjacent to the previous channel....
                if( channel.ID != previous_channel_id + 1){
                    // if we've collected a valid adjacency group, then add it to the list of adjacency groups.
                    if( adjacency_group != null){
                        retval.Add( adjacency_group);
                    }
                    // create a new adjacency group.
                    adjacency_group = new List< Channel>();
                }
                // add this channel into the current adjacency group.
                adjacency_group.Add( channel);
                // update previous channel id for next loop iteration.
                previous_channel_id = channel.ID;
            }
            // add the final adjacency group to the list of adjacency groups.
            if( adjacency_group != null){
                retval.Add( adjacency_group);
            }
            // return the list of adjacency groups.
            return retval;
        }
    }
}
