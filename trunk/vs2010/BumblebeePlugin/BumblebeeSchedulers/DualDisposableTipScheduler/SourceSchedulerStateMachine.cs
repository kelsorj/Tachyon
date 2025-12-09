using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using BioNex.BumblebeeAlphaGUI;
using BioNex.Shared.PlateDefs;
using System.Threading;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using GalaSoft.MvvmLight.Messaging;
using System.Runtime.Remoting.Messaging;
using System.ComponentModel.Composition;
using BioNex.BumblebeePlugin;

namespace BioNex.BumblebeeAlphaGUI.DualDisposableTipSplitScheduler
{
    public class SourceSchedulerStateMachine : IDisposable
    {
        public enum State
        {
            Idle,
            ExecuteAllSourceTransfers,
            WaitForAllSourcesToComplete,
            Done
        }

        public enum Trigger
        {
            Execute,
            Done,
            Exit
        }

        private Stateless.StateMachine<State, Trigger> SM { get; set; }
        private bool Aborting { get; set; }
        private Trigger LastTrigger { get; set; }

        private ILog Log = LogManager.GetLogger(typeof(SourceSchedulerStateMachine));
        private List<Transfer> DestinationTransfersOnly { get; set; }
        private SourcePlate SourcePlate { get; set; }
        private Stage DestinationStage { get; set; }
        private int NumberOfSourcesLeft { get; set; }
        private AlphaHardware Hardware { get; set; }
        private ExternalPlateTransferSchedulerInterface PlateHandler { get; set; }
        private DestinationPlate CurrentDestination { get; set; }
        private Stage TipBoxStage { get; set; }
        private List<SourcePlate> UniqueSources  = new List<SourcePlate>();
        private AutoResetEvent[] SourceDoneEvents { get; set; }
        private TipTracker TipTracker { get; set; }
        private Teachpoints Teachpoints { get; set; }
        private Stage[] Stages { get; set; }
        // private Object TransferLock = new Object(); FYC, no references to this lock??
        private List<string> SourceBarcodeProcessingOrder = new List<string>();
        private int NumberOfTipPairs { get; set; }
        private ILabwareDatabase LabwareDatabase { get; set; }
        private ILiquidProfileLibrary LiquidProfileLibrary { get; set; }
        private IError ErrorInterface { get; set; }
        private int refcount;
        private BumblebeeMessenger Messenger { get; set; }
        
        public SourceSchedulerStateMachine( AlphaHardware hardware, ExternalPlateTransferSchedulerInterface plate_handler, Teachpoints teachpoints, TipTracker tip_tracker, Stage dest_stage,
                                            Stage tipbox_stage, DestinationPlate dest_plate, ILabwareDatabase labware_database, ILiquidProfileLibrary liquid_profile_library,
                                            IError error_interface, BumblebeeMessenger messenger)
        {
            SM = new Stateless.StateMachine<State,Trigger>( State.Idle);
            Messenger = messenger;
            Messenger.Register<AbortCommand>( this, Abort);

            Hardware = hardware;
            PlateHandler = plate_handler;
            Teachpoints = teachpoints;
            TipTracker = tip_tracker;
            DestinationStage = dest_stage;
            TipBoxStage = tipbox_stage;
            CurrentDestination = dest_plate;
            LabwareDatabase = labware_database;
            LiquidProfileLibrary = liquid_profile_library;
            ErrorInterface = error_interface;

            SM.Configure(State.Idle)
                .Permit(Trigger.Execute, State.ExecuteAllSourceTransfers);
            SM.Configure(State.ExecuteAllSourceTransfers)
                .Permit(Trigger.Done, State.WaitForAllSourcesToComplete)
                .OnEntry( ExecuteAllSourceTransfers);
            SM.Configure(State.WaitForAllSourcesToComplete)
                .Permit(Trigger.Done, State.Done)
                .OnEntry( WaitForAllSourcesToComplete);
            // this special configuration for the Done state is required because the
            // Abort message can come in at any time.  It fires Trigger.Abort, which
            // puts the state machine into Done, but Done must ignore the next
            // Trigger coming down the pipe.
            SM.Configure(State.Done)
                .OnEntry( Done);
        }


        public void Execute( List<Transfer> transfers_this_destination_only, List<SourcePlate> unique_sources)
        {
            UniqueSources = unique_sources;
            DestinationTransfersOnly = transfers_this_destination_only;
            Messenger.Register<AbortCommand>( this, Abort);
            
            LastTrigger = Trigger.Execute;
            while( !Aborting) {
                if( LastTrigger == Trigger.Exit)
                    break;
                Fire( SM, LastTrigger);
                Thread.Sleep( 10);
            }
        }

        private void Fire( Stateless.StateMachine<State,Trigger> state_machine, Trigger trigger)
        {
            state_machine.Fire( trigger);
        }

        private void Abort( AbortCommand command)
        {
            Aborting = true;
        }

        private void ExecuteAllSourceTransfers()
        {
            // this handles the specific case where we know that we want to dedicate a pair
            // of tips to a source plate because we have a bunch of plates coming in and
            // we want to keep each one of them in use
            foreach( SourcePlate source_plate in UniqueSources) {
                Interlocked.Increment( ref refcount);
                Stage source_stage = Hardware.RequestStage( Stage.ModeType.Source);
                // this thread lasts for the lifetime of an entire source plate
                DualDisposableTipSplitScheduler.SourcePlateScheduler sched = new DualDisposableTipSplitScheduler.SourcePlateScheduler( SourcePlateThread);
                // get only the transfers for this source
                List<Transfer> source_transfers = (from t in DestinationTransfersOnly where t.Source.Barcode == source_plate.Barcode select t).ToList();
                sched.BeginInvoke( source_transfers, source_stage, DestinationStage, new AsyncCallback( SourceTransfersDoneCallback), source_stage);
            }
            
            LastTrigger = Trigger.Done;
        }

        private void SourceTransfersDoneCallback( IAsyncResult iar)
        {
            try{
                AsyncResult ar = (AsyncResult)iar;
                DualDisposableTipSplitScheduler.SourcePlateScheduler callback = (DualDisposableTipSplitScheduler.SourcePlateScheduler)ar.AsyncDelegate;
                callback.EndInvoke( iar);
                Interlocked.Decrement( ref refcount);
            } catch( Exception){
                Hardware.UnlockStage(( Stage)( iar.AsyncState));
            }
        }

        private void SourcePlateThread( List<Transfer> source_transfers_only, Stage source_stage, Stage dest_stage)
        {
            // execute the source plate state machine here
            SourcePlateStateMachine sm = new SourcePlateStateMachine( Hardware, PlateHandler, Teachpoints, TipTracker,
                                                                      LabwareDatabase, LiquidProfileLibrary, ErrorInterface,
                                                                      source_transfers_only, source_stage, dest_stage, TipBoxStage,
                                                                      Messenger);
            
            sm.Execute();
            sm.Dispose();
        }

        private void WaitForAllSourcesToComplete()
        {
            // wait until all source stages are done being used or until
            // the user clicks Abort
            while( !Interlocked.Equals( refcount, 0) && !Aborting)
                Thread.Sleep( 10);
            LastTrigger = Trigger.Done;
        }

        private void Done()
        {
            LastTrigger = Trigger.Exit;
        }

        #region IDisposable Members

        public void Dispose()
        {
            Messenger.Unregister<AbortCommand>( this);
            GC.SuppressFinalize( this);
        }

        #endregion
    }
}
