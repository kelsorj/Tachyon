using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.Utils;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.IError;

namespace BioNex.CustomerGUIPlugins
{
    public class ReinventoryCartStateMachine : StateMachineWrapper2<ReinventoryCartStateMachine.State, ReinventoryCartStateMachine.Trigger>
    {
        public enum State
        {
            Idle,
            ScanDockBarcode,
            ScanDockBarcodeError,
            UserBarcodeEntry,
            LookupCartTeachpoints,
            LookupCartTeachpointsError,
            ExecuteFlyByBarcodeReadingStateMachine,
            Done,
        }

        public enum Trigger
        {
            Execute,
            Success,
            Failure,
            UserEntry,
            Done,
            Retry,
            Ignore,
            Abort,
            Cancel
        }

        public ReinventoryCartStateMachine( IError error_interface, bool called_from_diags)
            : base( typeof(ReinventoryCartStateMachine), State.Idle, State.Done, State.Done,
                    Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort,
                    error_interface, called_from_diags)
        {
            // Idle state is handled automagically by the SMW2 class
            // can't use Felix's ConfigureState method here because we want to allow the user to input data if there is a cart barcode misread
            SM.Configure( State.ScanDockBarcode)
                .Permit( Trigger.Success, State.LookupCartTeachpoints)
                .Permit( Trigger.Failure, State.ScanDockBarcodeError)
                .OnEntry( ScanDockBarcode);
            SM.Configure( State.ScanDockBarcodeError)
                .Permit( Trigger.Retry, State.ScanDockBarcode)
                .Permit( Trigger.UserEntry, State.UserBarcodeEntry)
                .OnEntry( ScanDockBarcodeError);
            SM.Configure( State.UserBarcodeEntry)
                .Permit( Trigger.Done, State.LookupCartTeachpoints)
                .Permit( Trigger.Cancel, State.ScanDockBarcode)
                .OnEntry( UserBarcodeEntry);
            SM.Configure( State.LookupCartTeachpoints)
                .Permit( Trigger.Failure, State.LookupCartTeachpointsError)
                .Permit( Trigger.Success, State.ExecuteFlyByBarcodeReadingStateMachine)
                .OnEntry( LookupCartTeachpoints);
            //SM.Configure( State.LookupCartTeachpointsError);
            SM.Configure( State.ExecuteFlyByBarcodeReadingStateMachine)
                .Permit( Trigger.Success, State.Done)
                .OnEntry( ExecuteFlyByBarcodeReadingStateMachine);
            //SM.Configure( Done);
        }

        private void ScanDockBarcode()
        {
            Fire( Trigger.Success);
        }

        private void ScanDockBarcodeError()
        {
            Fire( Trigger.Retry);
        }

        private void UserBarcodeEntry()
        {
            Fire( Trigger.Done);
        }

        private void LookupCartTeachpoints()
        {
            Fire( Trigger.Success);
        }

        private void ExecuteFlyByBarcodeReadingStateMachine()
        {
            Fire( Trigger.Success);
        }
    }
}
