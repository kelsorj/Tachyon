using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.Utils;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.DeviceInterfaces;
using log4net;
using System.Windows;
using BioNex.Shared.IError;

namespace BioNex.CustomerGUIPlugins
{
    public class PlateMoverStateMachine : StateMachineWrapper2<PlateMoverStateMachine.State, PlateMoverStateMachine.Trigger>
    {
        private ILog _log = LogManager.GetLogger(typeof(PlateMoverStateMachine));
        private DeviceInterface _platemover { get; set; }

        public enum State
        {
            Idle,
            MoveStageToExternal,
            MoveStageToExternalError,
            Done
        }

        public enum Trigger
        {
            Execute,
            Success,
            Failure,
            Retry,
            Ignore,
            Abort
        }

        public PlateMoverStateMachine(IError error_interface, DeviceInterface plate_mover)
            : base(typeof(PlateMoverStateMachine), State.Idle, State.Done, State.Done,
                    Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, error_interface, false)
        {
            _platemover = plate_mover;

            // need to try SMW2's specialized functions later.  for now I am doing it the old way.
            SM.Configure(State.Idle)
                .Permit(Trigger.Success, State.MoveStageToExternal);
            SM.Configure(State.MoveStageToExternal)
                .Permit(Trigger.Success, State.Done)
                .Permit(Trigger.Failure, State.MoveStageToExternalError)
                .OnEntry(MoveStageToExternal);
            SM.Configure(State.MoveStageToExternalError)
                .Permit(Trigger.Retry, State.MoveStageToExternal)
                .OnEntry(() => base.HandleErrorWithRetryOnly());
            SM.Configure(State.Done)
                .OnEntry(Done);
        }

        private void MoveStageToExternal()
        {
            _log.Info("Moving PlateMover stage to external teachpoint");
            if( !_platemover.ExecuteCommand("movetoexternalteachpoint", null)) {
                LastError = "Could not move plate mover to external teachpoint";
                Fire(Trigger.Failure);
            } else {
                Fire(Trigger.Success);
            }
        }

        private void Done()
        {
            _log.Info("Done with MakeLocationAvailable");
            EndStateFunction();
        }
    }
}
