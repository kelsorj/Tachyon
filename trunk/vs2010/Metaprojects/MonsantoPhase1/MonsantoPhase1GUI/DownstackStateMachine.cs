using System;
using System.Collections.Generic;
using System.Linq;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.IError;
using BioNex.Shared.Utils;
using log4net;

namespace BioNex.CustomerGUIPlugins
{
    public class DownstackStateMachine : StateMachineWrapper2<DownstackStateMachine.State,DownstackStateMachine.Trigger>
    {
        private ILog _log = LogManager.GetLogger( typeof( DownstackStateMachine));
        private MonsantoPhase1GUI _plugin { get; set; }
        private DeviceInterface _platemover { get; set; }
        private RobotInterface _robot { get; set; }
        private string _labware { get; set; }

        public enum State
        {
            Idle,
            MoveStageToInternal,
            MoveStageToInternalError,
            TransferPlate,
            TransferPlateError,
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

        public DownstackStateMachine( IError error_interface, MonsantoPhase1GUI plugin, string labware, bool called_from_diags)
            : base( typeof(DownstackStateMachine), State.Idle, State.Done, State.Done,
                    Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, error_interface, called_from_diags)
        {
            _plugin = plugin;
            _platemover = plugin._platemover;
            _robot = plugin._robot;
            _labware = labware;

            // need to try SMW2's specialized functions later.  for now I am doing it the old way.
            SM.Configure( State.Idle)
                .Permit( Trigger.Success, State.MoveStageToInternal);
            SM.Configure(State.MoveStageToInternal)
                .Permit(Trigger.Success, State.TransferPlate)
                .Permit(Trigger.Failure, State.MoveStageToInternalError)
                .OnEntry(MoveStageToInternal);
            SM.Configure(State.MoveStageToInternalError)
                .Permit(Trigger.Retry, State.MoveStageToInternal)
                .OnEntry(() => base.HandleErrorWithRetryOnly());
            SM.Configure(State.TransferPlate)
                .Permit(Trigger.Success, State.Done)
                .Permit(Trigger.Failure, State.TransferPlateError)
                .OnEntry(TransferPlate);
            SM.Configure(State.TransferPlateError)
                .Permit(Trigger.Retry, State.TransferPlate)
                .OnEntry(() => base.HandleErrorWithRetryOnly());           
            SM.Configure( State.Done)
                .OnEntry( Done);
        }

        private void MoveStageToInternal()
        {
            _log.Info( "Moving PlateMover to internal teachpoint");
            if( !_platemover.ExecuteCommand( "movetointernalteachpoint", null)) {
                var msg = string.Format("Failure during Downstack MoveStageToInternal");
                _log.Error(msg);
                LastError = msg;
                Fire( Trigger.Failure);
            } else {
                Fire( Trigger.Success);
            }
        }

        private void TransferPlate()
        {
            try {
                _log.Info("Transferring plate from static storage location to PlateMover");
                // find any plate that's in inventory and send it out
                PlateStorageInterface storage = _robot as PlateStorageInterface;
                string robot_name = (_robot as DeviceInterface).Name;
                IEnumerable<KeyValuePair<string,string>> plates_available = storage.GetInventory( robot_name);
                string barcode = plates_available.Last().Key;
                string location_name = plates_available.Last().Value;
                bool is_portrait = true;
                _robot.TransferPlate( robot_name, location_name, (_platemover as DeviceInterface).Name, is_portrait ? "Stage, Portrait" : "Stage, Landscape",
                                      _labware, new MutableString());
                storage.Unload( _labware, barcode, location_name);
                Fire( Trigger.Success);
            } catch( Exception e) {
                var msg = string.Format("Failure during Downstack TransferPlate, error message was '{0}'", e.Message);
                _log.Error(msg);
                LastError = msg;
                Fire( Trigger.Failure);
            }
        }

        private void Done()
        {
            _log.Info( "Done downstacking plate");
            EndStateFunction();
        }
    }
}
