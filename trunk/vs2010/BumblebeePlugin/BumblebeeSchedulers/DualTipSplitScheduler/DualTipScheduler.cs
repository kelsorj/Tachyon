using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.TechnosoftLibrary;
using System.Threading;
using BioNex.Shared.PlateDefs;
using System.Workflow.Runtime;
using System.Workflow.Runtime.Hosting;
using System.Workflow.Activities;
using System.Diagnostics;
using BioNex.Shared.Utils;
using BioNex.Shared.LabwareDatabase;
using Twitterizer.Framework;
using BioNex.BumblebeeAlphaGUI;
using BioNex.Shared.DeviceInterfaces;
using System.Workflow.Runtime.Tracking;
using Microsoft.Samples.Workflow.CustomPersistenceService; // for persistence service, which allows pause / resume / abort
using System.ComponentModel.Composition;
using BioNex.Shared.LibraryInterfaces;
using log4net;
using GalaSoft.MvvmLight.Messaging;

namespace BioNex.BumblebeeAlphaGUI.DualTipSplitScheduler
{
    [Export(typeof(BumblebeeAlphaGUI.SchedulerInterface.IScheduler))]
    public class DualTipSplitScheduler : BumblebeeAlphaGUI.SchedulerInterface.IScheduler
    {
        /// <summary>
        /// used to keep track of the currently-loaded destination plate, so we
        /// can mark the wells used after each transfer
        /// </summary>
        private DestinationPlate _current_dest_plate;

        private AlphaHardware _hw = null;
        private Teachpoints _teachpoints = null;
        private System.ComponentModel.BackgroundWorker _bgw = null;
        private BioNex.Shared.DeviceInterfaces.RobotInterface _robot = null;
        private BioNex.Shared.DeviceInterfaces.PlateStorageInterface _platehandler = null;
        //private Twitter _twitter;
        private object _transfer_lock = new object();
        private string TipHandlingMethod { get; set; }

        private ILog _log = LogManager.GetLogger(typeof(DualTipSplitScheduler));

        [Import]
        public ILabwareDatabase LabwareDatabase { get; set; }
        [ Import]
        public ILiquidProfileLibrary LiquidProfileLibrary { get; set; }
        [Import]
        public IError ErrorInterface { get; set; }

        private Dictionary<int, byte> SourceTransferThreads = new Dictionary<int, byte>();
        private Dictionary<int, AutoResetEvent> SourceTransferThreadAvailableEvents = new Dictionary<int,AutoResetEvent>();

        public DualTipSplitScheduler()
        {
        }

        public void SetHardware( AlphaHardware hw)
        {
            _hw = hw;
        }

        public void SetTeachpoints( Teachpoints tps)
        {
            _teachpoints = tps;
        }

        public void SetBackgroundWorker( System.ComponentModel.BackgroundWorker bgw)
        {
            _bgw = bgw;
        }

        public void SetPlateHandler(RobotStorageInterface plate_handler_plugin)
        {
            // not sure about this method for doing things...
            _robot = (BioNex.Shared.DeviceInterfaces.RobotInterface)plate_handler_plugin;
            _platehandler = (BioNex.Shared.DeviceInterfaces.PlateStorageInterface)plate_handler_plugin;
        }
        
        public void SetTipHandlingMethod( string method_name)
        {
            TipHandlingMethod = method_name;
        }

        /// <summary>
        /// MUST call Reset before each run, in order to clear out the map that tracks how
        /// many threads are running on each source plate.  If you don't call this, there's
        /// a good chance that the app will not spawn enough threads to keep all channels
        /// fully utilized.
        /// </summary>
        public void Reset()
        {
            SourceTransferThreads.Clear();
            SourceTransferThreadAvailableEvents.Clear();
        }

        public string GetSchedulerName()
        {
            return "Dual-tip split-head scheduler -- ideal for Bumblebee";
        }

        public delegate void TransferProcess( TransferOverview to);
        public delegate void DestinationStageScheduler( List<Transfer> transfers);
        public delegate void SourceStageScheduler( List<Transfer> transfers, List<SourcePlate> unique, Stage dest_stage);
        public delegate void SourcePlateScheduler( List<Transfer> transfers, SourcePlate source_plate, Stage source_stage, Stage dest_stage,
                                                   AutoResetEvent done_event, object transfer_lock, bool load_plate);
        public delegate void ChannelScheduler( Transfer t, Stage source_stage, Stage dest_stage, AutoResetEvent channel_done_event);


        public void StartProcess( TransferOverview to)
        {
            try {
                //_twitter = new Twitter( "bionex", "JqNjEQ2iCulvjlV7SkYl");
            } catch( Exception) {
            }

            DestinationStageScheduler dest_scheduler = new DestinationStageScheduler( DestinationPlateThread);
            // invoke the destination thread in blocking fashion
            dest_scheduler.Invoke( to.Transfers);
        }

        /// <summary>
        /// locks a stage for the dest plate and starts the source plate scheduler
        /// does not exit until all dest plates have been processed
        /// </summary>
        /// <param name="transfers"></param>
        /// <param name="hw"></param>
        private void DestinationPlateThread( List<Transfer> transfers)
        {
            while( transfers.Count > 0) {
                // DESTINATION PLATE HANDLING
                // grab the first destination plate -- we'll remove it from the list after it's done
                DestinationPlate dest_plate = transfers[0].Destination;
                // loop over all of the transfers to figure out which source plates are needed
                // for this destination plate
                List<Transfer> transfers_to_execute = new List<Transfer>();
                foreach( Transfer t in transfers) {
                    if( t.Destination == dest_plate)
                        transfers_to_execute.Add( t);
                }
                // remove the transfers from the master list so we don't repeat anything
                transfers.RemoveAll( x => x.Destination == dest_plate);
                // request a stage for the destination plate
                Stage dest_stage = null;
                while( dest_stage == null) {
                    dest_stage = _hw.RequestStage( Stage.ModeType.Destination);
                    Thread.Sleep( 10);
                }
                // request the destination plate
                dest_stage.MoveToRobotTeachpoint();
                _log.Info( String.Format( "Unloading plate '{0}' to BB PM {1}", dest_plate.Barcode, dest_stage.GetID()));
                _platehandler.Unload( dest_plate.LabwareName, dest_plate.Barcode, String.Format("BB PM {0}", dest_stage.GetID()));
                dest_stage.Loaded = true;
                dest_stage.Plate = dest_plate;
                _current_dest_plate = dest_plate;
                // unload the dest stage so that the source scheduler can use it
                _hw.UnlockStage( dest_stage);

                // SOURCE PLATE HANDLING
                // now start the thread that makes the source plates come in for this plate
                // but first we need to compile a list of unique source plates in the transfer list
                var unique_source_transfers = new HashSet<Transfer>(transfers_to_execute, new SourceBarcodeComparer());
                List<SourcePlate> unique = new List<SourcePlate>();
                foreach( Transfer t in unique_source_transfers) {
                    if( !unique.Contains( t.Source))
                        unique.Add( t.Source);
                }
                // pass the unique list of source plates to the source plate scheduler, along with the transfer list
                // now the scheduler will know which plates to process, and which wells in the plate are used
                SourceStageScheduler source_scheduler = new SourceStageScheduler( SourceSchedulerThread);
                source_scheduler.Invoke( transfers_to_execute, unique, dest_stage);
                // when the source stage scheduler thread is done, then we need to unload the dest plate
                _hw.ReturnChannelsHome();
                dest_stage.MoveToRobotTeachpoint();
                _log.Info( String.Format( "Loading plate '{0}' from BB PM {1}", dest_plate.Barcode, dest_stage.GetID()));
                _platehandler.Load(dest_plate.LabwareName, dest_plate.Barcode, String.Format("BB PM {0}", dest_stage.GetID()));
                dest_stage.Loaded = false;
                dest_stage.Plate = null;
                _current_dest_plate = null;

                try {
                    //_twitter.DirectMessages.New( "kelsorj", String.Format( "BB1 finished protocol"));
                    //_twitter.DirectMessages.New( "matz408", String.Format( "BB1 finished protocol"));
                    //_twitter.Status.Update( String.Format( "BB1 finished protocol"));
                } catch( Exception) {
                }

            }
        }

        /// <summary>
        /// this is a special case, where we are only processing a single source plate and want
        /// to be able to use all of the available channels
        /// </summary>
        /// <param name="transfers"></param>
        /// <param name="source_barcode"></param>
        /// <param name="dest_stage"></param>
        private void ProcessSingleSourcePlate( List<Transfer> transfers, SourcePlate source_plate, Stage dest_stage)
        {
            // at this point, I basically spawn all threads necessary to make all transfers
            // for all sources into the one dest, and then wait for each source plate thread
            // to signal its event -- then I know it's totally done with all source plates
            AutoResetEvent[] events = new AutoResetEvent[2];
            events[0] = new AutoResetEvent( false);
            events[1] = new AutoResetEvent( false);
            Stage source_stage = _hw.RequestStage( Stage.ModeType.Source);
            // this thread lasts for the lifetime of an entire source plate
            SourcePlateScheduler sched = new SourcePlateScheduler( SourcePlateThread);
            // start the first pair of tips
            sched.BeginInvoke( transfers, source_plate, source_stage, dest_stage, events[0], _transfer_lock, true, null, null);
            // start the next pair of transfers
            sched.BeginInvoke( transfers, source_plate, source_stage, dest_stage, events[1], _transfer_lock, false, null, null);
            WaitHandle.WaitAll( events);
        }

        /// <summary>
        /// launches threads for each source plate that needs to have transfers processed
        /// does not exit until all threads have signaled completion
        /// </summary>
        /// <param name="transfers"></param>
        /// <param name="unique_sources"></param>
        /// <param name="hw"></param>
        private void SourceSchedulerThread( List<Transfer> transfers, List<SourcePlate> unique_sources, Stage dest_stage)
        {
            if( unique_sources.Count == 1) {
                ProcessSingleSourcePlate( transfers, unique_sources[0], dest_stage);
                return;
            }
            // create an event for each source plate, and pass this event to the
            // respective source plate scheduler
            AutoResetEvent[] events = new AutoResetEvent[unique_sources.Count];
            int i = 0;
            int num_sources_complete = 0;
            // at this point, I basically spawn all threads necessary to make all transfers
            // for all sources into the one dest, and then wait for each source plate thread
            // to signal its event -- then I know it's totally done with all source plates
            Stage[] stages = new Stage[unique_sources.Count];
            List<string> source_barcode_processing_order = new List<string>();
            foreach( SourcePlate source_plate in unique_sources) {
                string barcode = source_plate.Barcode;
                // create the event
                events[i] = new AutoResetEvent( false);
                stages[i] = _hw.RequestStage( Stage.ModeType.Source);
                // this thread lasts for the lifetime of an entire source plate
                SourcePlateScheduler sched = new SourcePlateScheduler( SourcePlateThread);
                // the thread will always use 2 tips, unless there is only one transfer remaining
                source_barcode_processing_order.Add(barcode);
                sched.BeginInvoke( transfers, source_plate, stages[i], dest_stage, events[i], _transfer_lock, true, null, null);
                i++;
            }

            // figure out which source barcodes are still processing by above threads
            List<string> source_barcodes_in_process = new List<string>();
            for (int event_index=0; event_index<unique_sources.Count; event_index++)
            {
                AutoResetEvent e = events[event_index];
                if (e.WaitOne(0) == false)
                    source_barcodes_in_process.Add(source_barcode_processing_order[event_index]);
                else
                {
                    // is this code smell?  the problem with testing the already-set AutoResetEvents
                    // is that the test RESETS the event, and this will cause the later WaitAll()
                    // test to hang!!!
                    e.Set();
                }
            }

            // now we know which source plates are still being processed in a thread
            // determine whether or not these source plate barcodes are the same as the remaining transfers
            bool all_source_plates_accounted_for = true;
            foreach (Transfer t in transfers)
            {
                if (!source_barcodes_in_process.Contains(t.Source.Barcode))
                    all_source_plates_accounted_for = false;
            }

            // if all remaining source plates will be covered by the last threads that got launched,
            // then we want to exit this function, or the block of code after this will launch more
            // threads.
            if (all_source_plates_accounted_for)
            {
                BioNex.Shared.Utils.Events.WaitForEvents(events);                
                return;
            }

            // we need to have the source scheduler spin until all of the sources are complete.
            // it will "listen" for a thread to complete, which means that plate is done.  When
            // the plate it done, it needs to reset that event, and reallocate it to another
            // thread that will process a subset of the remaining plate's transfers.
            int num_sources_to_wait_for = unique_sources.Count;
            while( num_sources_complete < num_sources_to_wait_for) {
                int index_complete = WaitHandle.WaitAny( events);
                // increment this counter since a plate just got completed
                num_sources_complete++;
                // if there aren't any transfers left, loop back through because another
                // thread could still be processing its hitpicks.  the next time through
                // it will wait for an event from this thread and then will exit.
                if( transfers.Count == 0)
                    continue;
                // lock up the transfers so that the other plate doesn't process any more until
                // we reallocate the transfers
                lock( _transfer_lock) {
                    _log.Info( String.Format( "{0} transfers remaining", transfers.Count));
                    SourcePlateScheduler sched = new SourcePlateScheduler( SourcePlateThread);
                    SourcePlate other_plate = unique_sources[index_complete == 0 ? 1 : 0];
                    Stage other_stage = stages[index_complete == 0 ? 1 : 0];
                    AutoResetEvent this_event = events[index_complete];
                    this_event.Reset();
                    // here we need to call BeginInvoke with the special "load_plate" parameter so we don't try to
                    // reload the plate.  If we do, we'll obviously crash.  The right thing to do here is to
                    // reference count the stage access.  place the plate = increment, pick the plate = decrement.
                    // When the reference count == 0, then unload.
                    sched.BeginInvoke( transfers, other_plate, other_stage, dest_stage, this_event, _transfer_lock, false, null, null);
                    num_sources_to_wait_for++;
                }
            }
        }


        private void SourcePlateThread( List<Transfer> transfers, SourcePlate source_plate, Stage source_stage, Stage dest_stage,
                                        AutoResetEvent done_event, object transfer_lock, bool load_plate)
        {
            if( load_plate) {
                // request the plate
                source_stage.MoveToRobotTeachpoint();
                _log.Info( String.Format("Unloading plate '{0}' to BB PM {1}", source_plate.Barcode, source_stage.GetID()));
                _platehandler.Unload( source_plate.LabwareName, source_plate.Barcode, String.Format("BB PM {0}", source_stage.GetID()));
                source_stage.Loaded = true;
                source_stage.Plate = source_plate;
                // unlock the source stage so that the channel threads can use it for access control
                // MUST be unlocked AFTER the Loaded property gets set!
                _hw.UnlockStage( source_stage);
            }

            long transfer_count = 0;
            source_stage.Increment();

            while( true) {
                // get transfers for this plate only
                List<Transfer> this_source_transfers;
                List<KeyValuePair<Channel,Transfer>> tip_assignments;
                double source_angle, dest_angle;
                lock( transfer_lock) {
                    this_source_transfers = transfers.FindAll( (x) => (x.Source.Barcode == source_plate.Barcode));
                    if( this_source_transfers.Count == 0)
                        break;
                    GetTransfersTipsAndAngle( this_source_transfers, out tip_assignments, source_stage, dest_stage, _current_dest_plate, out source_angle, out dest_angle);

                    //! \todo this is technically not correct -- we shouldn't remove the transfer
                    //!       from the master transfer list until AFTER the transfer is successful!
                    // remove the transfers that got processed ok
                    foreach( KeyValuePair<Channel,Transfer> kvp in tip_assignments) {
                        Transfer t = kvp.Value;
                        transfers.Remove( t);
                    }
                }

                // wait for the stage to be ready
                if( tip_assignments.Count == 1) {
                    _hw.RequestStageForTransfer( tip_assignments[0].Key, source_stage, false);
                } else if( tip_assignments.Count == 2) {
                    _hw.RequestStageForTransfer( tip_assignments[0].Key, tip_assignments[1].Key, source_stage);
                }

                DualTipTransferStateMachine sm = new DualTipTransferStateMachine( ErrorInterface);
                sm.Execute( _hw, source_stage, dest_stage, tip_assignments, _teachpoints, source_angle, dest_angle, TipHandlingMethod, LabwareDatabase, LiquidProfileLibrary);

                // unlock the channels that got used
                foreach( KeyValuePair<Channel,Transfer> kvp in tip_assignments) {
                    _hw.UnlockChannel( kvp.Key);
                    // mark the dest_plate's well as Used
                    foreach( string wellname in kvp.Value.DestinationWellNames)
                        _current_dest_plate.SetWellUsageState( wellname, Wells.WellUsageStates.Used);
                }

                transfer_count += tip_assignments.Count;
            }

            // set the done event early, since if this is the last plate we also want
            // to let the dest plate get unloaded
            done_event.Set();
            source_stage.Decrement();

            // unload the plate only if the reference count for the stage is 0
            if (source_stage.ReadyToUnload())
            {
                source_stage.MoveToRobotTeachpoint();
                _log.Info( String.Format("Loading plate '{0}' from BB PM {1}", source_plate.Barcode, source_stage.GetID()));
                _platehandler.Load( source_plate.LabwareName, source_plate.Barcode, String.Format("BB PM {0}", source_stage.GetID()));
                source_stage.Loaded = false;
                source_stage.Plate = null;
            }
        }

        /// <summary>
        /// figures out which tips should go where, and how many at once
        /// </summary>
        /// <param name="transfers">
        ///     The total number of transfers from this source plate.  For now, it
        ///     modifies the original list that gets passed to the thread, so we
        ///     can easily tell when we're done.
        /// </param>
        /// <param name="tip_assignments"></param>
        ///     Maps a tip to the transfer that has been assigned to it by this function
        /// <param name="source_angle">
        ///     The angle that the source plate needs to rotate to
        /// </param>
        /// <param name="dest_angle">
        ///     The angle that the dest plate needs to rotate to
        /// </param>
        private void GetTransfersTipsAndAngle( List<Transfer> transfers, out List<KeyValuePair<Channel,Transfer>> tip_assignments,
                                               Stage source_stage, Stage dest_stage, DestinationPlate dest_plate, out double source_angle,
                                               out double dest_angle)
        {
            source_angle = 0;
            dest_angle = 0;
            tip_assignments = new List<KeyValuePair<Channel,Transfer>>();
            // now loop over the transfers, and (for now) blindly pick pairs.  check the source
            // well and dest well spacing
            int moving_incrementer = 1; // increases transfer spacing if we loop over all transfers 
                                        // and still can't dual pick
            while( true) {
                for( int i=0; i<transfers.Count; i++) {
                    // need to check for i+1 going out of bounds first.  if it's out of bounds,
                    // then we haven't found a pair that works, so just return one transfer.
                    // in the future, I will need to make this smarter.
                    if( transfers.Count == 1 || moving_incrementer == transfers.Count) {
                        // only need one tip assignment, or we've looped over the transfer list
                        // with the entire range allowed for moving_incrementer, and still
                        // couldn't find a solution
                        Channel c = _hw.RequestChannel();
                        Transfer t = transfers[i];
                        // if the dest well is "any", need to select a well
                        if( t.DestinationWellNames[0].ToLower() == "any") {
                            t.DestinationWellNames[0] = dest_plate.GetFirstAvailableWell();
                            dest_plate.SetWellUsageState( t.DestinationWellNames[0], Wells.WellUsageStates.Reserved);
                        }
 
                        tip_assignments.Add( new KeyValuePair<Channel,Transfer>( c, t));
                        _log.Info( String.Format( "Tip {0} transferring from stage{1} well {2} to dest well {3}", c.GetID(), source_stage.GetID(), t.SourceWellName, t.DestinationWellNames[0]));
                        transfers.RemoveAt( i);
                        return;
                    } else if( (i + moving_incrementer) >= transfers.Count) {
                        // bail out of this for loop before we go out of range in the array
                        // the outer while loop will execute this for loop again after
                        // incrementing moving_incrementer
                        break;
                    }

                    // pass these to the util function that calculates the necessary angles.  it will
                    // return false if there is no solution.  in this case, try another pair of transfers
                    // the next pair should be composed of the 2nd transfer in the first pair, with the
                    // next transfer in the list
                    Transfer t1 = transfers[i];
                    Transfer t2 = transfers[i + moving_incrementer];

                    // need to get the labware definitions here, because I don't have a way to get
                    // the labware definitions in AlphaHardware -- need to figure out Unity + MEF
                    ILabware source_labware = LabwareDatabase.GetLabware( t1.Source.LabwareName);
                    ILabware dest_labware = LabwareDatabase.GetLabware( t1.Destination.LabwareName);
                    // pass the two transfer requests to the hardware layer, since it can deal with
                    // locking tips, calculating the tip spacing, and determining the wells to use
                    // NOTE: GetSourceAndDestSolutions should unlock the two tips if it can't get
                    // a solution for BOTH SOURCE AND DEST!
                    if( _hw.GetSourceAndDestSolutions( t1, t2, source_stage, dest_stage, source_labware, dest_labware, dest_plate, out tip_assignments, out source_angle, out dest_angle)) {
                        // remove these transfers from the master list or we'll end up doing
                        // the same thing over and over again
                        foreach( KeyValuePair<Channel,Transfer> kvp in tip_assignments) {
                            Channel c = kvp.Key;
                            Transfer t = kvp.Value;
                            // mark the well as reserved, so it's not given to another transfer pair in another thread
                            dest_plate.SetWellUsageState( t.DestinationWellNames[0], Wells.WellUsageStates.Reserved);
                            _log.Info( String.Format( "Tip {0} transferring from stage{1} well {2} to dest well {3}", c.GetID(), source_stage.GetID(), t.SourceWellName, t.DestinationWellNames[0]));
                        }
                        transfers.RemoveAt( i + moving_incrementer);
                        transfers.RemoveAt( i);
                        return;
                    }
                }

                moving_incrementer++;
            }
        }

        private double GetTipSpacing( Teachpoints teachpoints, byte tip1_id, byte tip2_id, byte stage_id)
        {
            StageTeachpoint tp1 = teachpoints.GetStageTeachpoint( tip1_id, stage_id);
            StageTeachpoint tp2 = teachpoints.GetStageTeachpoint( tip2_id, stage_id);

            double tip_spacing_ul = Math.Abs( tp1.UpperLeft["y"] - tp2.UpperLeft["y"]);
            double tip_spacing_lr = Math.Abs( tp1.LowerRight["y"] - tp2.LowerRight["y"]);

            string possible_error = String.Format( "The teachpoints for each tip should be very close to each other, but the ones for tips {0} and {1} are off by {2}", tip1_id, tip2_id, tip_spacing_lr - tip_spacing_ul);
            if( Math.Abs( tip_spacing_ul - tip_spacing_lr) > 1.1) {
                Debug.Assert( false, possible_error);
            }

            return (tip_spacing_ul + tip_spacing_lr) / 2;
        }

        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public void Abort()
        {
        }
    }
}
