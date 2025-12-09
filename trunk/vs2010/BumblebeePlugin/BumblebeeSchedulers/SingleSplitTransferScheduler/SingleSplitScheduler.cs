using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.Teachpoints;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.HitpickXMLReader;
using System.Threading;
using BioNex.Shared.PlateDefs;
using System.Workflow.Runtime;
using System.Workflow.Runtime.Hosting;
using System.Workflow.Activities;
using BioNex.Shared.DeviceInterfaces;
using System.Diagnostics;
using System.ComponentModel.Composition;
using BioNex.Shared.LabwareDatabase;
using BioNex.Shared.LibraryInterfaces;

namespace BioNex.BumblebeeAlphaGUI.SingleSplitTransferScheduler
{
    /// <summary>
    /// this is just a glorified int wrapper that will allow the source plate
    /// threads to intelligently bump up their tip utilization once all of the
    /// other source plates are done being processed
    /// </summary>
    public class SourcePlateManager
    {
        private int _counter;

        public SourcePlateManager( int total_source_plates)
        {
            _counter = total_source_plates;
        }

        public void Decrement()
        {
            Interlocked.Decrement( ref _counter);
        }

        public bool OnLastPlate()
        {
            return Interlocked.Equals( _counter, 1);
        }
    }

    [Export(typeof(BumblebeeAlphaGUI.SchedulerInterface.IScheduler))]
    public class Scheduler : BumblebeeAlphaGUI.SchedulerInterface.IScheduler
    {
        private AlphaHardware _hw = null;
        private Teachpoints _teachpoints = null;
        private Dictionary<int, byte> SourceTransferThreads = new Dictionary<int, byte>();
        private Dictionary<int, AutoResetEvent> SourceTransferThreadAvailableEvents = new Dictionary<int,AutoResetEvent>();
        private SourcePlateManager _source_plate_manager;
        private System.ComponentModel.BackgroundWorker _bgw = null;
        private BioNex.Shared.DeviceInterfaces.RobotInterface _robot = null;
        private BioNex.Shared.DeviceInterfaces.PlateStorageInterface _platehandler = null;
        private string TipHandlingMethod { get; set; }

        [Import]
        public ILabwareDatabase LabwareDatabase { get; set; }
        [Import]
        public IError ErrorInterface { get; set; }

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
            return "Split-head scheduler -- ideal for Bumblebee";
        }

        public delegate void TransferProcess( TransferOverview to);
        public delegate void DestinationStageScheduler( List<Transfer> transfers, AlphaHardware hw);
        public delegate void SourceStageScheduler( List<Transfer> transfers, List<Transfer> unique, Stage dest_stage, AlphaHardware hw);
        public delegate void SourcePlateScheduler( byte max_threads, List<Transfer> transfers, SourcePlate source_plate, Stage dest_stage, AlphaHardware hw, AutoResetEvent done_event, SourcePlateManager spm);
        public delegate void ChannelScheduler( Transfer t, Stage source_stage, Stage dest_stage, AlphaHardware hw, AutoResetEvent channel_done_event);

        public void StartProcess( TransferOverview to)
        {
            DestinationStageScheduler dest_scheduler = new DestinationStageScheduler( DestinationPlateThread);
            // invoke the destination thread in blocking fashion
            dest_scheduler.Invoke( to.Transfers, _hw);
        }

        /// <summary>
        /// locks a stage for the dest plate and starts the source plate scheduler
        /// does not exit until all dest plates have been processed
        /// </summary>
        /// <param name="transfers"></param>
        /// <param name="hw"></param>
        private void DestinationPlateThread( List<Transfer> transfers, AlphaHardware hw)
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
                    dest_stage = hw.RequestStage( Stage.ModeType.Destination);
                    Thread.Sleep( 50);
                }
                // request the destination plate
                dest_stage.MoveToRobotTeachpoint();
                Debug.WriteLine( String.Format( "Please place destination plate {0} on stage with id='{1}'", dest_plate.Barcode, dest_stage.GetID()));
                _platehandler.Unload( dest_plate.LabwareName, dest_plate.Barcode, String.Format("BB PM {0}", dest_stage.GetID()));
                //Thread.Sleep( 2000);
                dest_stage.Loaded = true;
                Debug.WriteLine( "dest plate placed on stage");
                // unload the dest stage so that the source scheduler can use it
                hw.UnlockStage( dest_stage);

                // SOURCE PLATE HANDLING
                // now start the thread that makes the source plates come in for this plate
                // but first we need to compile a list of unique source plates in the transfer list
                var unique_source_transfers = new HashSet<Transfer>(transfers_to_execute, new SourceBarcodeComparer());
                List<Transfer> unique = new List<Transfer>(unique_source_transfers);
                // instantiate the source plate manager and tell it how many sources are going to be used
                _source_plate_manager = new SourcePlateManager( unique.Count);
                // pass the unique list of source plates to the source plate scheduler, along with the transfer list
                // now the scheduler will know which plates to process, and which wells in the plate are used
                SourceStageScheduler source_scheduler = new SourceStageScheduler( SourceSchedulerThread);
                source_scheduler.Invoke( transfers_to_execute, unique, dest_stage, hw);
                // when the source stage scheduler thread is done, then we need to unload the dest plate
                dest_stage.MoveToRobotTeachpoint();
                Debug.WriteLine( String.Format( "Please remove the destination plate from stage with id='{0}'", dest_stage.GetID()));
                _platehandler.Load( dest_plate.LabwareName, dest_plate.Barcode, String.Format("BB PM {0}", dest_stage.GetID()));
                //Thread.Sleep( 2000);
                dest_stage.Loaded = false;
                Debug.WriteLine( "destination plate unloaded");
            }
        }

        /// <summary>
        /// launches threads for each source plate that needs to have transfers processed
        /// does not exit until all threads have signaled completion
        /// </summary>
        /// <param name="transfers"></param>
        /// <param name="unique_sources"></param>
        /// <param name="hw"></param>
        private void SourceSchedulerThread( List<Transfer> transfers, List<Transfer> unique_sources, Stage dest_stage, AlphaHardware hw)
        {
            int num_sources = unique_sources.Count;
            // create an event for each source plate, and pass this event to the
            // respective source plate scheduler
            AutoResetEvent[] events = new AutoResetEvent[num_sources];
            int i = 0;
            // at this point, I basically spawn all threads necessary to make all transfers
            // for all sources into the one dest, and then wait for each source plate thread
            // to signal its event -- then I know it's totally done with all source plates
            //! \todo this is a really weird place to use a List<Transfer>.  We only needed
            //!       the barcode, so should have just used a List<string>.
            foreach( Transfer t in unique_sources) {
                // create the event
                events[i] = new AutoResetEvent( false);
                // this thread lasts for the lifetime of an entire source plate
                SourcePlateScheduler sched = new SourcePlateScheduler( SourcePlateThread);
                byte num_tips_to_use_per_source = (unique_sources.Count > 1) ? (byte)(_hw.GetNumberOfChannels() / 2) : _hw.GetNumberOfChannels();
                // if it's the last transfer, then actually we want to use all of the tips
                //! \todo I thought that my implementation for the above comment was correct,
                //!       but it really didn't work -- since I call BeginInvoke, the problem
                //!       is that the number of tips used per plate is determined at the very
                //!       beginning of protocol execution, NOT as plate enter the system.
                if( i == ((byte)unique_sources.Count - 1))
                    num_tips_to_use_per_source = _hw.GetNumberOfChannels();
                // here, we pass in the desired number of tips to use per sourcce, but we
                // will allow the thread to bump up its usage by querying a "plate monitor"
                // object.  When the plate monitor says there are no more plates to process,
                // the thread should be smart enough to increase tip usage.
                sched.BeginInvoke( num_tips_to_use_per_source, transfers, t.Source, dest_stage, hw, events[i], _source_plate_manager, new AsyncCallback( SourcePlateCompleted), null);
                i++;
            }

            //WaitHandle.WaitAll( events);
            BioNex.Shared.Utils.Events.WaitForEvents( events);
        }

        private void SourcePlateCompleted( IAsyncResult iar)
        {
            _source_plate_manager.Decrement();
        }

        private void WaitForAvailableChannelThread( byte max_threads)
        {
            // figure out how many threads are currently in use
            int thread_id = Thread.CurrentThread.GetHashCode();
            byte num_threads = SourceTransferThreads[thread_id];
        }

        private void SourcePlateThread( byte max_threads, List<Transfer> transfers, SourcePlate source_plate, Stage dest_stage, AlphaHardware hw, AutoResetEvent done_event, SourcePlateManager spm)
        {
            // request a stage
            Stage stage = hw.RequestStage( Stage.ModeType.Source);
            // request the plate
            stage.MoveToRobotTeachpoint();
            Debug.WriteLine( String.Format( "Please place source plate with barcode {0} on stage with id='{1}'", source_plate.Barcode, stage.GetID()));
            _platehandler.Unload( source_plate.LabwareName, source_plate.Barcode, String.Format("BB PM {0}", stage.GetID()));
            //Thread.Sleep( 2000);
            Debug.WriteLine( "source plate placed on stage");
            stage.Loaded = true;
            // unlock the source stage so that the channel threads can use it for access control
            // MUST be unlocked AFTER the Loaded property gets set!
            hw.UnlockStage( stage);

            // process transfers -- fire off a thread for each transfer, each will
            // try to acquire a channel whenever it becomes available
            // need to size the events array -- figure out how many transfers use this source plate
            List<Transfer> temp = transfers.FindAll( (x) => (x.Source.Barcode == source_plate.Barcode));
            int num_transfers = temp.Count;
            AutoResetEvent[] events = new AutoResetEvent[num_transfers];

            // create an event that the foreach loop below waits on when no channels
            // are available for usage
            AutoResetEvent channel_available_event = new AutoResetEvent( true);
            int thread_id = Thread.CurrentThread.GetHashCode();
            lock( SourceTransferThreads) {
                if( !SourceTransferThreads.ContainsKey( thread_id))
                    SourceTransferThreads.Add( thread_id, 0);
                else
                    SourceTransferThreads[thread_id] = 0;
            }
            lock( SourceTransferThreadAvailableEvents) {
                if( !SourceTransferThreadAvailableEvents.ContainsKey(thread_id))
                    SourceTransferThreadAvailableEvents.Add( thread_id, channel_available_event);
                else
                    SourceTransferThreadAvailableEvents[thread_id] = channel_available_event;
            }

            int i = 0;
            foreach( Transfer t in transfers) {
                if( t.Source.Barcode != source_plate.Barcode)
                    continue;

                channel_available_event.WaitOne();
                lock( SourceTransferThreads) {
                    SourceTransferThreads[thread_id]++;
                    // here we will check with the source plate manager to see if the
                    // rest of the plates are done.  If so, we can use all of the tips!
                    if( spm.OnLastPlate())
                        max_threads = _hw.GetNumberOfChannels();
                    // reset the event again if we know that there are more available threads to use
                    if( SourceTransferThreads[thread_id] < max_threads)
                        channel_available_event.Set();
                }

                ChannelScheduler sched = new ChannelScheduler( ChannelThread);
                events[i] = new AutoResetEvent( false);
                sched.BeginInvoke( t, stage, dest_stage, hw, events[i], new AsyncCallback(ChannelThreadComplete), thread_id);
                i++;
            }

            // wait for all of the transfer threads to complete execution
            WaitHandle.WaitAll( events);

            // set the done event early, since if this is the last plate we also want
            // to let the dest plate get unloaded
            done_event.Set();

            // unload the plate
            stage.MoveToRobotTeachpoint();
            Debug.WriteLine( String.Format( "transfers from source {0} complete -- please remove plate from stage with id='{1}'", source_plate.Barcode, stage.GetID()));
            _platehandler.Load( source_plate.LabwareName, source_plate.Barcode, String.Format("BB PM {0}", stage.GetID()));
            //Thread.Sleep( 2000);
            stage.Loaded = false;
            Debug.WriteLine( "source plate unloaded");
        }

        private void ChannelThreadComplete( IAsyncResult iar)
        {
            // when we get here, we know that a ChannelThread is done processing a transfer
            // set the event corresponding to this thread
            int thread_id = (int)iar.AsyncState;
            lock( SourceTransferThreadAvailableEvents) {
                SourceTransferThreadAvailableEvents[thread_id].Set();
            }
            lock( SourceTransferThreads) {
                SourceTransferThreads[thread_id]--;
            }
        }

        private void ChannelThread( Transfer t, Stage source_stage, Stage dest_stage, AlphaHardware hw, AutoResetEvent channel_done_event)
        {
            Debug.WriteLine( String.Format( "Transferring well {0} from source plate {1} to well {2} in destination plate {3}", t.SourceWellName, t.Source.Barcode, t.DestinationWellNames[0], t.Destination.Barcode));

            Channel channel = hw.RequestChannel();
            SingleTransferStateMachine sm = new SingleTransferStateMachine( ErrorInterface);
            sm.Execute( hw, source_stage, dest_stage, t, channel, _teachpoints, TipHandlingMethod, LabwareDatabase);
            hw.UnlockChannel( channel);

            channel_done_event.Set();
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
