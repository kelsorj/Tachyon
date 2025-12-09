using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.PlateDefs;
using BioNex.BumblebeeAlphaGUI;
using System.Threading;
using BioNex.Shared.DeviceInterfaces;
using log4net;
using BioNex.Shared.Utils;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.LabwareDatabase;
using System.Diagnostics;
using System.ComponentModel.Composition;
using BioNex.BumblebeeAlphaGUI.TipOperations;
using GalaSoft.MvvmLight.Messaging;
using System.Runtime.Remoting.Messaging;
using BioNex.BumblebeePlugin;

namespace BioNex.BumblebeeAlphaGUI.DualDisposableTipSplitScheduler
{
    public class SourcePlateStateMachine : IDisposable
    {
        private enum State
        {
            Idle,
            LoadSourcePlate,
            LoadSourcePlateError,
            CheckTransfersRemaining,
            CheckTransfersRemainingError,
            UnloadSourcePlate,
            UnloadSourcePlateError,
            Done
        }

        private enum Trigger
        {
            NoTrigger,
            LoadNewSource,
            NoMoreTransfers,
            DoneWithSource,
            DisposableTip,
            Wash,
            Shuck,
            LastSourceTransferDone,
            Error,
            Retry,
            Abort,
            Done,
            Exit
        }

        private Stateless.StateMachine<State,Trigger> SM { get; set; }
        private bool Aborting { get; set; }
        private Trigger LastTrigger { get; set; }

        private ILog Log = LogManager.GetLogger(typeof(SourcePlateStateMachine));
        private SourcePlate Source { get; set; }
        private Stage SourceStage { get; set; }
        private Stage DestinationStage { get; set; }
        private Stage TipBoxStage { get; set; }
        /// <summary>
        /// TransferLock is needed to prevent other threads from clobbering the master transfer
        /// list, which gets modified by each of the state machines as transfers are completed.
        /// Need to replace this with a copy, and then have the caller remove the transfers
        /// allocated to this state machine when the execution is successful.
        /// </summary>
        private ExternalPlateTransferSchedulerInterface PlateHandler { get; set; }
        private AlphaHardware Hardware { get; set; }
        private DestinationPlate CurrentDestinationPlate { get; set; }
        private Teachpoints Teachpoints { get; set; }
        private TipTracker TipTracker { get; set; }
        private string LastError { get; set; }

        private ILabwareDatabase LabwareDatabase { get; set; }
        public IError ErrorInterface { get; set; }
        public ILiquidProfileLibrary LiquidProfileLibrary { get; set; }
        
        // these properties are for a particular pair of transfers only
        private List<Transfer> TransfersForThisSource { get; set; }
        private List<KeyValuePair<Channel,Transfer>> TipAssignments;

        public SourcePlateStateMachine( AlphaHardware hardware, ExternalPlateTransferSchedulerInterface plate_handler, Teachpoints teachpoints, TipTracker tip_tracker,
                                        ILabwareDatabase labware_database, ILiquidProfileLibrary liquid_profile_library, IError error_interface,
                                        List<Transfer> transfers_this_source_only, Stage source_stage, Stage dest_stage, Stage tipbox_stage,
                                        BumblebeeMessenger messenger)
        {
            SM = new Stateless.StateMachine<State,Trigger>( State.Idle);
            Messenger = messenger;
            Messenger.Register<AbortCommand>( this, Abort);

            Hardware = hardware;
            PlateHandler = plate_handler;
            Teachpoints = teachpoints;
            TipTracker = tip_tracker;
            LabwareDatabase = labware_database;
            LiquidProfileLibrary = liquid_profile_library;
            ErrorInterface = error_interface;
            TipAssignments = new List<KeyValuePair<Channel,Transfer>>();
            TransfersForThisSource = transfers_this_source_only;
            SourceStage = source_stage;
            DestinationStage = dest_stage;
            TipBoxStage = tipbox_stage;
            Source = transfers_this_source_only[0].Source;

            SM.Configure(State.Idle)
                .Permit(Trigger.LoadNewSource, State.LoadSourcePlate)
                .Ignore(Trigger.NoTrigger);
            SM.Configure(State.LoadSourcePlate)
                .Permit(Trigger.Error, State.LoadSourcePlateError)
                .Permit(Trigger.Done, State.CheckTransfersRemaining)
                .Ignore(Trigger.NoTrigger)
                .OnEntry( LoadSourcePlate);
            SM.Configure(State.LoadSourcePlateError)
                .Permit(Trigger.Retry, State.LoadSourcePlate)
                .Ignore(Trigger.NoTrigger)
                .OnEntry(LoadSourcePlateError);
            SM.Configure(State.CheckTransfersRemaining)
                .Permit(Trigger.NoMoreTransfers, State.UnloadSourcePlate)
                .Ignore(Trigger.NoTrigger)
                .OnEntry( CheckTransfersRemaining);
            SM.Configure(State.UnloadSourcePlate)
                .Permit(Trigger.Done, State.Done)
                .Ignore(Trigger.NoTrigger)
                .OnEntry( UnloadSourcePlate);
            SM.Configure(State.UnloadSourcePlateError)
                .Permit(Trigger.Retry, State.UnloadSourcePlate)
                .Ignore(Trigger.NoTrigger)
                .OnEntry(UnloadSourcePlateError);
            SM.Configure(State.Done)
                .OnEntry( Done);
        }

        private void Abort( AbortCommand command)
        {
            Aborting = true;
        }

        public void Execute()
        {
            LastTrigger = Trigger.LoadNewSource;
            while( !Aborting) {
                if( LastTrigger == Trigger.Exit)
                    break;
                if( LastTrigger != Trigger.NoTrigger) {
                    Fire( SM, LastTrigger);
                }
                Thread.Sleep( 10);
            }
        }

        private void Fire( Stateless.StateMachine<State,Trigger> state_machine, Trigger trigger)
        {
            state_machine.Fire( trigger);
        }

        private void LoadSourcePlate()
        {
            // DKM 2010-08-30 need to re-justify why I'm locking TransferLock here when I don't mess with the transfer list
            //                Could be something from the old scheduler code
            // request the plate
            DateTime start = DateTime.Now;
            SourceStage.MoveToRobotTeachpoint();

            string to_teachpoint = String.Format("BB PM {0}", SourceStage.GetID());
            start = DateTime.Now;

            //PlateHandler.RequestPlate( this, Source.LabwareName, Source.Barcode, to_teachpoint);
            Messenger.Default.Send<RequestPlateMessage>( new RequestPlateMessage { Barcode=Source.Barcode, LabwareName=Source.LabwareName, TeachpointName=to_teachpoint });

            TimeSpan task_time = DateTime.Now - start;
            Log.Debug( String.Format( "(gantt) robot unloading plate {0} to {1} took {2}s", Source.LabwareName, to_teachpoint, task_time.TotalSeconds));
            Hardware.SetPlateOnStage( SourceStage, Source);
            // unlock the source stage so that the channel threads can use it for access control
            // MUST be unlocked AFTER calling SetPlateOnStage()!
            Hardware.UnlockStage( SourceStage);
            Hardware.TransfersOnDeck.AddTransfers( TransfersForThisSource);
            // SourceStage.Increment();

            LastTrigger = Trigger.Done;
        }

        private void LoadSourcePlateError()
        {
            HandleErrorWithRetryOnly( LastError);
        }

        private void CheckTransfersRemaining()
        {
            // sit around and wait for all of the source's transfers to be used up,
            // or until the user clicks Abort
            while( Hardware.TransfersOnDeck.CountTransfersOfSrc( Source.Barcode) > 0 && !Aborting){
                Thread.Sleep( 50);
            }
            
            LastTrigger = Trigger.NoMoreTransfers;
        }

        private void CheckTransfersRemainingError()
        {
            HandleErrorWithRetryOnly( LastError);
        }

        private void UnloadSourcePlate()
        {
            try {
                SourceStage.MoveToRobotTeachpoint();
                Log.Info(String.Format("Loading plate '{0}' from BB PM {1}", Source.Barcode, SourceStage.GetID()));
                DateTime start = DateTime.Now;
                string from_teachpoint = String.Format("BB PM {0}", SourceStage.GetID());
                
                //PlateHandler.ReturnPlate( Source.LabwareName, Source.Barcode, from_teachpoint);
                Messenger.Default.Send<ReturnPlateMessage>( new ReturnPlateMessage { Barcode=Source.Barcode, LabwareName=Source.LabwareName, TeachpointName=from_teachpoint });

                TimeSpan task_time = DateTime.Now - start;
                Log.Debug( String.Format( "(gantt) robot loading plate {0} from {1} took {2}s", Source.LabwareName, from_teachpoint, task_time.TotalSeconds));
                Hardware.SetPlateOnStage( SourceStage, null);
                
                LastTrigger = Trigger.Exit;
            } catch( Exception ex) {
                LastError = ex.Message;
                LastTrigger = Trigger.Error;
            }
        }

        private void UnloadSourcePlateError()
        {
            HandleErrorWithRetryOnly( LastError);
        }

        private void Done()
        {
            LastTrigger = Trigger.Exit;
        }
        
        private void HandleErrorWithRetryOnly(string message)
        {
            try {
                string retry = "Try move again";
                BioNex.Shared.LibraryInterfaces.ErrorData error = new BioNex.Shared.LibraryInterfaces.ErrorData(message, new List<string> { retry });
                ErrorInterface.AddError(error);
                WaitHandle.WaitAny(error.EventArray);
                if (error.TriggeredEvent == retry)
                    LastTrigger = Trigger.Retry;
            } catch( Exception ex) {
                LastError = ex.Message;
                Debug.Assert( false, ex.Message);
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Messenger.Unregister<AbortCommand>( this);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
