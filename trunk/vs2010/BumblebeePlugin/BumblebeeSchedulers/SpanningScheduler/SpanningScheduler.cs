using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BioNex.HitpickXML;
using BioNex.TechnosoftLibrary;
using BioNex.PlateDefs;
using System.Workflow.Runtime;
using System.Workflow.Runtime.Hosting;
using System.Workflow.Activities;
using BumblebeeAlphaGUI;

namespace BumblebeeAlphaGUI.SpanningScheduler
{ 
    public class Scheduler : BumblebeeAlphaGUI.SchedulerInterface.IScheduler
    {
        private AlphaHardware _hw = null;
        private Teachpoints _teachpoints = null;
        private Dictionary<string,Stage> _reserved_source_stages = new Dictionary<string,Stage>();
        private AutoResetEvent _dispense_waithandle = new AutoResetEvent(false);
        private Dictionary<byte,Transfer> _tip_transfer_info = new Dictionary<byte,Transfer>(); // keeps track of the aspirates made
        private System.ComponentModel.BackgroundWorker _bgw = null;
        private BioNex.DeviceInterfaces.RobotInterface _robot = null;
        private BioNex.DeviceInterfaces.PlateStorageInterface _platehandler = null;

        // just used to lock the head so when we span using two source plates,
        // we don't allow multiple tips to go down at the same time
        private object _head_lock = new object();

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

        public void SetPlateHandler( object plate_handler_plugin)
        {
            // not sure about this method for doing things...
            _robot = (BioNex.DeviceInterfaces.RobotInterface)plate_handler_plugin;
            _platehandler = (BioNex.DeviceInterfaces.PlateStorageInterface)plate_handler_plugin;
        }

        /// <summary>
        /// MUST call Reset before each run, in order to clear out the map that tracks how
        /// many threads are running on each source plate.  If you don't call this, there's
        /// a good chance that the app will not spawn enough threads to keep all channels
        /// fully utilized.
        /// </summary>
        public void Reset()
        {
            _reserved_source_stages.Clear();
            _tip_transfer_info.Clear();
        }

        public string GetSchedulerName()
        {
            return "Grouped tip scheduler -- almost mimics the Evo and Star";
        }

        public delegate void TransferProcess( TransferOverview to);
        public delegate void DestinationStageScheduler( List<Transfer> transfers);
        public delegate void SourceStageScheduler( byte channels_per_head, List<Transfer> transfers, List<Transfer> unique, Stage dest_stage);
        public delegate void SourcePlateTransfer( byte start_aspirate_channel, string source_barcode, List<Transfer> transfers, Stage dest_stage, bool leave_plate, AutoResetEvent done_event, object head_lock);

        public void StartProcess( TransferOverview to)
        {
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
                    dest_stage = _hw.RequestStage();
                    Thread.Sleep( 50);
                }
                // request the destination plate
                dest_stage.MoveToRobotTeachpoint();
                Console.WriteLine( "Please place destination plate {0} on stage with id='{1}'", dest_plate.Barcode, dest_stage.GetID());
                //Thread.Sleep( 2000);
                dest_stage.Loaded = true;
                Console.WriteLine( "dest plate placed on stage");
                // unload the dest stage so that the source scheduler can use it
                _hw.UnlockStage( dest_stage);

                // SOURCE PLATE HANDLING
                // now start the thread that makes the source plates come in for this plate
                // but first we need to compile a list of unique source plates in the transfer list
                var unique_source_transfers = new HashSet<Transfer>(transfers_to_execute, new SourceBarcodeComparer());
                List<Transfer> unique = new List<Transfer>(unique_source_transfers);
                // pass the unique list of source plates to the source plate scheduler, along with the transfer list
                // now the scheduler will know which plates to process, and which wells in the plate are used
                SourceStageScheduler source_scheduler = new SourceStageScheduler( SourceSchedulerThread);
                source_scheduler.Invoke( _hw.GetNumberOfChannels(), transfers_to_execute, unique, dest_stage);
                // --- BLOCKING HERE UNTIL ALL SOURCE THREADS ARE DONE! --

                // TRANSFERS COMPLETE FROM ALL SOURCE PLATES
                // when the source stage scheduler thread is done, then we need to unload the dest plate
                dest_stage.MoveToRobotTeachpoint();
                Console.WriteLine( "Please remove the destination plate from stage with id='{0}'", dest_stage.GetID());
                //Thread.Sleep( 2000);
                dest_stage.Loaded = false;
                Console.WriteLine( "destination plate unloaded");
            }
        }

        /// <summary>
        /// launches threads for each source plate, but only does one plate
        /// at a time unless it needs more plates to fill up all channels
        /// </summary>
        /// <param name="transfers"></param>
        /// <param name="unique_sources"></param>
        /// <param name="hw"></param>
        private void SourceSchedulerThread( byte channels_per_head, List<Transfer> transfers, List<Transfer> unique_sources, Stage dest_stage)
        {
            // for the spanning implementation, need to loop over the transfers
            // and group them in sets of up to N transfers (1 per channel)
            List<Transfer> transfers_per_head = new List<Transfer>();
            List<string> source_plates_to_use = new List<string>();
            foreach( Transfer t in unique_sources) {
                // the idea here is to take each source plate, and figure out how many transfers
                // can still fit into transfers_per_head.  Three possibilities here:
                // 1. all transfers go in, but there's still room -- need to add this plate
                //    to the source transfer queue and move on to the next source plate
                // 2. all transfers go in, and transfers_per_head is full -- just start
                //    transferring and wait until done
                // 3. not enough room for all transfers in transfers_per_head -- start
                //    transferring, but wait until all transfers are done and loop
                //    within this loop until they are all done.

                // get all transfers from the full transfer list using this source plate
                List<Transfer> this_source_transfers = transfers.FindAll( (x) => (x.Source.Barcode == t.Source.Barcode));
                // how many transfers have been allocated so far?
                int space_available = channels_per_head - transfers_per_head.Count;
                // 1
                if( this_source_transfers.Count < space_available) {
                    transfers_per_head.AddRange( this_source_transfers);
                    // add this source plate to the list of plates, then continue
                    // so we can add more transfers from the next source plate
                    source_plates_to_use.Add( t.Source.Barcode);
                    continue;
                }
                // 2
                if( this_source_transfers.Count == space_available) {
                    transfers_per_head.AddRange( this_source_transfers);
                    // add this source plate to the list of plates
                    source_plates_to_use.Add( t.Source.Barcode);
                    // start the thread(s) needed to process this plate and
                    // any other source plates that may have come before it.
                    LaunchAndWaitForSourcePlates( source_plates_to_use, transfers_per_head, dest_stage, "");
                    continue;
                }
                // 3
                // if we're here, then we must not have enough tips available to process the
                // entire source plate, so keep looping until we've done all of the transfers
                while( this_source_transfers.Count > space_available) {
                    // only add as many source transfers as there is room in the head
                    transfers_per_head.AddRange( this_source_transfers.GetRange( 0, space_available));
                    // remember to remove these transfers from the source plate list or
                    // we'll transfer them multiple times
                    this_source_transfers.RemoveRange( 0, space_available);
                    // at this point, the sources are ready for transferring, so we need to
                    // launch all threads needed to move the plates in.  Remember that we
                    // need to look at a flag that tells us whether or not the source
                    // plate should stay in the system
                    if( !source_plates_to_use.Contains( t.Source.Barcode))
                        source_plates_to_use.Add( t.Source.Barcode);
                    // finally, spawn the threads that get the source plates in and
                    // deal with the transfers, and wait for them to complete
                    string source_that_stays = t.Source.Barcode;
                    LaunchAndWaitForSourcePlates( source_plates_to_use, transfers_per_head, dest_stage, source_that_stays);
                    space_available = channels_per_head - transfers_per_head.Count;
                }

                // now we have to check one last time, because there's a chance that we'll have
                // left over transfers from the last source.  For example, let's say the source initially
                // had 20 transfers, and there were 8 available channels in the head.  The first time
                // through, we'd get stuck in the while loop above and transfer 8 samples, then another
                // 8, leaving 4.  Well, here is where we need to queue up those 4 samples.  Also need
                // to consider the case where we get here and this_source_transfers.Count == space_available!
                // NOTE: LaunchAndWaitForSourcePlates will remove the transfers from
                //       transfers_per_head and remove the source plate barcodes from
                //       source_plates_to_use.   
                transfers_per_head.AddRange( this_source_transfers);
                // add this source plate to the list of plates
                source_plates_to_use.Add( t.Source.Barcode);
                // did the last set of source transfers fill up the remaining channels or not?
                space_available = channels_per_head - transfers_per_head.Count;
                if( space_available == 0) {
                    // take care of the case where we have just enough transfers from this
                    // source left over to fill up the head
                    LaunchAndWaitForSourcePlates( source_plates_to_use, transfers_per_head, dest_stage, "");
                } else {
                    // we didn't fill up the head, so let's just continue on through the main loop
                    continue;
                }
            }
            // process remaining transfers here
            if( transfers_per_head.Count > 0 && source_plates_to_use.Count > 0)
                LaunchAndWaitForSourcePlates( source_plates_to_use, transfers_per_head, dest_stage, "");
        }

        /// <summary>
        /// This function launches the necessary threads to do all of the aspirates from
        /// one or more plates, and then launches the workflow that performs all dispenses.
        /// It always gets called for a full set of channels, or the remaining subset at the
        /// end of a hitpick list.
        /// </summary>
        /// <param name="source_plate_barcodes"></param>
        /// <param name="transfers"></param>
        /// <param name="dest_stage"></param>
        /// <param name="source_that_stays"></param>
        private void LaunchAndWaitForSourcePlates( List<string> source_plate_barcodes, List<Transfer> transfers, Stage dest_stage, string source_that_stays)
        {
            // deal with aspirates first
            _tip_transfer_info.Clear();
            byte start_aspirate_channel = 1;
            AutoResetEvent[] aspirate_done_event = new AutoResetEvent[source_plate_barcodes.Count];
            int ctr = 0;
            foreach( string source_barcode in source_plate_barcodes) {
                // launch a thread for each source plate, passing it all of the
                // transfers, the barcode.  we want to run in BLOCKING mode
                // because we only want to process a plate at a time and not
                // deal with hw resource locking issues that could make the
                // head go back and forth between sources needlessly.
                SourcePlateTransfer spt = new SourcePlateTransfer( SourcePlateThread);
                aspirate_done_event[ctr] = new AutoResetEvent( false);
                spt.BeginInvoke( start_aspirate_channel, source_barcode, transfers, dest_stage, source_barcode == source_that_stays, aspirate_done_event[ctr], _head_lock, null, null);
                start_aspirate_channel += (byte)transfers.Count( (x) => x.Source.Barcode == source_barcode);
                if( start_aspirate_channel > _hw.GetNumberOfChannels())
                    start_aspirate_channel = 1;
                ctr++;
            }

            WaitHandle.WaitAll( aspirate_done_event);

            // now dispense into the dest plate
            Dictionary<string,object> parameters = new Dictionary<string,object>();
            // execute the workflow that will aspirate these samples
            using( WorkflowRuntime workflow_runtime = new WorkflowRuntime()) {
                AutoResetEvent waitHandle = new AutoResetEvent(false);
                // configure parameters
                parameters.Add( "Hardware", _hw);
                parameters.Add( "DestinationStage", dest_stage);
                parameters.Add( "Teachpoints", _teachpoints);
                parameters.Add( "TipTransferInfo", _tip_transfer_info);

                workflow_runtime.WorkflowCompleted += delegate(object sender, WorkflowCompletedEventArgs e) {waitHandle.Set();};
                workflow_runtime.WorkflowTerminated += delegate(object sender, WorkflowTerminatedEventArgs e)
                {
                    Console.WriteLine(e.Exception.Message);
                    waitHandle.Set();
                };

                WorkflowInstance instance = workflow_runtime.CreateWorkflow(typeof(SequentialWFSpanningDispenses.SpanningDispenseWorkflow), parameters);
                instance.Start();
                waitHandle.WaitOne();
            }

            // clear the containers!!!
            source_plate_barcodes.Clear();
            transfers.Clear();
        }

        private void SourcePlateThread( byte start_aspirate_channel, string source_barcode, List<Transfer> transfers, Stage dest_stage, bool leave_plate, AutoResetEvent done_event, object head_lock)
        {
            // request a stage if it hasn't been reserved already by a previous set of transfers
            // from this source plate
            Stage stage = null;
            if( _reserved_source_stages.ContainsKey( source_barcode))
                stage = _reserved_source_stages[source_barcode];
            else {
                stage = _hw.RequestStage();
                // request the plate
                stage.MoveToRobotTeachpoint();
                Console.WriteLine( "Please place source plate with barcode {0} on stage with id='{1}'", source_barcode, stage.GetID());
                //Thread.Sleep( 2000);
                Console.WriteLine( "source plate placed on stage");
                stage.Loaded = true;
                // if we need to leave the plate for another set of transfers, then we need to add it to
                // the reserved plates list
                if( leave_plate) {
                    if( !_reserved_source_stages.ContainsKey( source_barcode))
                        _reserved_source_stages.Add( source_barcode, stage);
                    else
                        _reserved_source_stages[source_barcode] = stage;
                }
                // unlock the source stage so that the channel threads can use it for access control
                // MUST be unlocked AFTER the Loaded property gets set!
                _hw.UnlockStage( stage);
            }

            // create a list of all of the transfers that apply to this source plate only!
            List<Transfer> temp = transfers.FindAll( (x) => (x.Source.Barcode == source_barcode));
            int num_transfers = temp.Count;

            Dictionary<string,object> parameters = new Dictionary<string,object>();
            
            // execute the workflow that will aspirate these samples
            using( WorkflowRuntime workflow_runtime = new WorkflowRuntime()) {
                //AutoResetEvent waitHandle = new AutoResetEvent(false);
                // configure parameters
                parameters.Add( "Hardware", _hw);
                parameters.Add( "SourceStage", stage);
                parameters.Add( "DestinationStage", dest_stage);
                parameters.Add( "Transfers", temp);
                parameters.Add( "Teachpoints", _teachpoints);
                //parameters.Add( "TipTransferInfo", tip_transfer_info);
                parameters.Add( "StartChannel", start_aspirate_channel);

                workflow_runtime.WorkflowCompleted += OnWorkflowCompletedHandler;
                workflow_runtime.WorkflowTerminated += delegate(object sender, WorkflowTerminatedEventArgs e)
                {
                    Console.WriteLine(e.Exception.Message);
                    _dispense_waithandle.Set();
                };

                try {
                    WorkflowInstance instance = workflow_runtime.CreateWorkflow(typeof(SequentialWFSpanningAspirates.SpanningAspirateWorkflow), parameters);
                    lock( head_lock) {
                        instance.Start();
                        _dispense_waithandle.WaitOne();
                    }
                } catch( System.Workflow.ComponentModel.Compiler.WorkflowValidationFailedException ex) {
                    Console.WriteLine( ex.Message);
                }
            }

            // unload the plate, only if we DON'T specify that we want to keep this source plate in the system!
            if( !leave_plate) {
                stage.MoveToRobotTeachpoint();
                Console.WriteLine( "transfers from source {0} complete -- please remove plate from stage with id='{1}'", source_barcode, stage.GetID());
                //Thread.Sleep( 2000);
                stage.Loaded = false;
                Console.WriteLine( "source plate unloaded");
            }

            done_event.Set();
        }

        private void OnWorkflowCompletedHandler( object sender, WorkflowCompletedEventArgs e)
        {
            // get the transfers made in the aspirate workflow
            Dictionary<byte,Transfer> aspirates = (Dictionary<byte,Transfer>)(e.OutputParameters["TipTransferInfo"]);
            foreach( KeyValuePair<byte,Transfer> kvp in aspirates)
                _tip_transfer_info.Add( kvp.Key, kvp.Value);
            _dispense_waithandle.Set();
        }
    }
}
