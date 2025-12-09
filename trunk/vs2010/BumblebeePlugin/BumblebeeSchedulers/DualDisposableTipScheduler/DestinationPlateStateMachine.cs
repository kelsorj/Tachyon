using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.PlateDefs;
using BioNex.BumblebeeAlphaGUI;
using System.Threading;
using log4net;
using System.Diagnostics;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using GalaSoft.MvvmLight.Messaging;
using System.ComponentModel.Composition;
using BioNex.BumblebeePlugin;

namespace BioNex.BumblebeeAlphaGUI.DualDisposableTipSplitScheduler
{
    public class DestinationPlateStateMachine : IDisposable
    {
        public enum State
        {
            Idle,
            PresentStageForDropoff,
            PresentStageForDropoffError,
            PlaceDestinationPlateOnStage,
            PlaceDestinationPlateOnStageError,
            WaitForAllTransfers,
            WaitForAllTransfersTimedOut,
            ReturnChannelsHome,
            ReturnChannelsHomeError,
            PresentStageForPickup,
            PresentStageForPickupError,
            LoadPlate,
            LoadPlateError,
            Done,
        }

        public enum Trigger
        {
            NoTrigger,
            Execute,
            Done,
            Error,
            Retry,
            Exit
        }

        private Stateless.StateMachine<State,Trigger> SM { get; set; }
        private Trigger LastTrigger { get; set; }

        private ILog Log = LogManager.GetLogger(typeof(DestinationPlateStateMachine));
        private string LastError { get; set; }
        private List<Transfer> DestinationTransfersOnly { get; set; }
        private AlphaHardware Hardware { get; set; }
        private DestinationPlate Destination { get; set; }
        private Teachpoints Teachpoints { get; set; }
        private Stage DestinationStage { get; set; }
        private Stage TipBoxStage { get; set; }
        private ExternalPlateTransferSchedulerInterface PlateHandler { get; set; }
        private IError ErrorInterface { get; set; }
        private TipTracker TipTracker { get; set; }
        private ILabwareDatabase LabwareDatabase { get; set; }
        private ILiquidProfileLibrary LiquidProfileLibrary { get; set; }
        public BumblebeeMessenger Messenger { get; set; }

        private bool Aborting { get; set; }

        public DestinationPlateStateMachine( AlphaHardware hardware, ExternalPlateTransferSchedulerInterface plate_handler, IError error_interface, TipTracker tip_tracker,
                                             Teachpoints teachpoints, ILabwareDatabase labware_database, ILiquidProfileLibrary liquid_profile_library,
                                             BumblebeeMessenger messenger)
        {
            SM = new Stateless.StateMachine<State,Trigger>( State.Idle);
            Messenger = messenger;
            Messenger.Register<AbortCommand>( this, Abort);

            Hardware = hardware;
            PlateHandler = plate_handler;
            ErrorInterface = error_interface;
            TipTracker = tip_tracker;
            Teachpoints = teachpoints;
            LabwareDatabase = labware_database;
            LiquidProfileLibrary = liquid_profile_library;

            SM.Configure(State.Idle)
                .Permit(Trigger.Execute, State.PresentStageForDropoff)
                .Ignore(Trigger.NoTrigger);
            SM.Configure(State.PresentStageForDropoff)
                .Permit(Trigger.Error, State.PresentStageForDropoffError)
                .Permit(Trigger.Done, State.PlaceDestinationPlateOnStage)
                .Ignore(Trigger.NoTrigger)
                .OnEntry(PresentStageForDropoff);
            SM.Configure(State.PresentStageForDropoffError)
                .Permit(Trigger.Retry, State.PresentStageForDropoff)
                .Ignore(Trigger.NoTrigger)
                .OnEntry(PresentStageForDropoffError);
            SM.Configure(State.PlaceDestinationPlateOnStage)
                .Permit(Trigger.Error, State.PlaceDestinationPlateOnStageError)
                .Permit(Trigger.Done, State.WaitForAllTransfers)
                .Ignore(Trigger.NoTrigger)
                .OnEntry(PlaceDestinationPlateOnStage);
            SM.Configure(State.PlaceDestinationPlateOnStageError)
                .Permit(Trigger.Retry, State.PlaceDestinationPlateOnStage)
                .Ignore(Trigger.NoTrigger)
                .OnEntry(PlaceDestinationPlateOnStageError);
            SM.Configure(State.WaitForAllTransfers)
                .Permit(Trigger.Error, State.WaitForAllTransfersTimedOut)
                .Permit(Trigger.Done, State.ReturnChannelsHome)
                .Ignore(Trigger.NoTrigger)
                .OnEntry(WaitForAllTransfers);
            SM.Configure(State.WaitForAllTransfersTimedOut)
                .Permit(Trigger.Retry, State.WaitForAllTransfers)
                .Ignore(Trigger.NoTrigger)
                .OnEntry(WaitForAllTransfersTimedOut);
            SM.Configure(State.ReturnChannelsHome)
                .Permit(Trigger.Error, State.ReturnChannelsHomeError)
                .Permit(Trigger.Done, State.PresentStageForPickup)
                .Ignore(Trigger.NoTrigger)
                .OnEntry(ReturnChannelsHome);
            SM.Configure(State.ReturnChannelsHomeError)
                .Permit(Trigger.Retry, State.ReturnChannelsHome)
                .Ignore(Trigger.NoTrigger)
                .OnEntry(ReturnChannelsHomeError);
            SM.Configure(State.PresentStageForPickup)
                .Permit(Trigger.Error, State.PresentStageForPickupError)
                .Permit(Trigger.Done, State.LoadPlate)
                .Ignore(Trigger.NoTrigger)
                .OnEntry(PresentStageForPickup);
            SM.Configure(State.PresentStageForPickupError)
                .Permit(Trigger.Retry, State.PresentStageForPickup)
                .Ignore(Trigger.NoTrigger)
                .OnEntry(PresentStageForPickupError);
            SM.Configure(State.LoadPlate)
                .Permit(Trigger.Error, State.LoadPlateError)
                .Permit(Trigger.Done, State.Done)
                .Ignore(Trigger.NoTrigger)
                .OnEntry(LoadPlate);
            SM.Configure(State.LoadPlateError)
                .Permit(Trigger.Retry, State.LoadPlate)
                .Ignore(Trigger.NoTrigger)
                .OnEntry( LoadPlateError);
            SM.Configure(State.Done)
                .OnEntry( Done);
        }

        private void Abort( AbortCommand command)
        {
            Aborting = true;
        }
        
        public void Execute( DestinationPlate dest_plate, List<Transfer> transfers, Stage tipbox_stage)
        {
            Destination = dest_plate;
            DestinationTransfersOnly = transfers;
            TipBoxStage = tipbox_stage;
            
            LastTrigger = Trigger.Execute;
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
        
        private void PresentStageForDropoff()
        {
            try {
                // request a stage for the destination plate
                DestinationStage = Hardware.RequestStage( Stage.ModeType.Destination);
                // request the destination plate
                DestinationStage.MoveToRobotTeachpoint();
                Log.Info( String.Format( "Unloading plate '{0}' to BB PM {1}", Destination.Barcode, DestinationStage.GetID()));
                LastTrigger = Trigger.Done;
            } catch( Exception ex) {
                LastError = ex.Message;
                LastTrigger = Trigger.Error;
            }
        }

        private void PresentStageForDropoffError()
        {
            HandleErrorWithRetryOnly( LastError);
        }

        private void PlaceDestinationPlateOnStage()
        {
            try {
                DateTime start = DateTime.Now;
                string to_teachpoint = String.Format("BB PM {0}", DestinationStage.GetID());
                
                //PlateHandler.RequestPlate( Destination.LabwareName, Destination.Barcode, to_teachpoint);
                Messenger.Default.Send<RequestPlateMessage>( new RequestPlateMessage { Barcode=Destination.Barcode, LabwareName=Destination.LabwareName, TeachpointName=to_teachpoint });

                TimeSpan task_time = DateTime.Now - start;
                Log.Debug( String.Format( "(gantt) robot unloading plate {0} to {1} took {2}s", Destination.LabwareName, to_teachpoint, task_time.TotalSeconds));
                Hardware.SetPlateOnStage( DestinationStage, Destination);
                // unload the dest stage so that the source scheduler can use it
                Hardware.UnlockStage( DestinationStage);
                LastTrigger = Trigger.Done;
            } catch( Exception ex) {
                LastError = ex.Message;
                LastTrigger = Trigger.Error;
            }
        }

        private void PlaceDestinationPlateOnStageError()
        {
            HandleErrorWithRetryOnly( LastError);
        }

        private void WaitForAllTransfers()
        {
            try {
                // create a list of unique source plates.
                List< SourcePlate> unique_sources = ( from t in DestinationTransfersOnly select t.Source).Distinct().ToList();
                
                // pass the unique list of source plates to the source plate scheduler, along with the transfer list
                // now the scheduler will know which plates to process, and which wells in the plate are used
                //DualTipSplitScheduler.SourceStageScheduler source_scheduler = new DualTipSplitScheduler.SourceStageScheduler( SourceSchedulerThread);
                //source_scheduler.Invoke( TransfersPerDestination, unique, DestinationStage);
                //***** START SOURCE STATE MACHINE HERE *****
                SourceSchedulerStateMachine sm = new SourceSchedulerStateMachine( Hardware, PlateHandler, Teachpoints, TipTracker,
                                                 DestinationStage, TipBoxStage, Destination, LabwareDatabase, LiquidProfileLibrary,
                                                 ErrorInterface, Messenger);
                // this is BLOCKING!
                // this state machine will separate the transfers by source
                sm.Execute( DestinationTransfersOnly, unique_sources);
                // if we get here, we know we finished up all of the source plates
                LastTrigger = Trigger.Done;
            } catch( Exception ex) {
                LastError = ex.Message;
                LastTrigger = Trigger.Error;
            }
        }

        private void WaitForAllTransfersTimedOut()
        {
            Debug.Fail( "Timed out waiting for transfers to complete.  Need to add error handling for this.");
        }

        private void ReturnChannelsHome()
        {
            try {
                Hardware.ReturnChannelsHome();
                LastTrigger = Trigger.Done;
            } catch( Exception ex) {
                LastError = ex.Message;
                LastTrigger = Trigger.Error;
            }
        }

        private void ReturnChannelsHomeError()
        {
            HandleErrorWithRetryOnly( LastError);
        }

        private void PresentStageForPickup()
        {
            try {
                DestinationStage = Hardware.RequestStage( Stage.ModeType.Destination);
                DestinationStage.MoveToRobotTeachpoint();
                Log.Info( String.Format( "Loading plate '{0}' from BB PM {1}", Destination.Barcode, DestinationStage.GetID()));
                LastTrigger = Trigger.Done;
            } catch( Exception ex) {
                LastError = ex.Message;
                LastTrigger = Trigger.Error;
            }
        }

        private void PresentStageForPickupError()
        {
            HandleErrorWithRetryOnly( LastError);
        }

        private void LoadPlate()
        {
            DateTime start = DateTime.Now;
            string from_teachpoint = String.Format("BB PM {0}", DestinationStage.GetID());
            
            //PlateHandler.ReturnPlate( Destination.LabwareName, Destination.Barcode, from_teachpoint);
            Messenger.Default.Send<ReturnPlateMessage>( new ReturnPlateMessage { Barcode=Destination.Barcode, LabwareName=Destination.LabwareName, TeachpointName=from_teachpoint });

            TimeSpan task_time = DateTime.Now - start;
            Log.Debug( String.Format( "(gantt) robot loading plate {0} from {1} took {2}s", Destination.LabwareName, from_teachpoint, task_time.TotalSeconds));
            Hardware.SetPlateOnStage( DestinationStage, null);
            LastTrigger = Trigger.Done;
        }

        private void LoadPlateError()
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
