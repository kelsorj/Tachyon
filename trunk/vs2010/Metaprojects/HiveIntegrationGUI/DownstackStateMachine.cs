using System;
using System.Collections.Generic;
using System.Linq;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.IError;
using BioNex.Shared.Utils;
using log4net;

namespace BioNex.HiveIntegration
{
    public class DownstackStateMachine : StateMachineWrapper2<DownstackStateMachine.State,DownstackStateMachine.Trigger>
    {
        private ILog _log = LogManager.GetLogger( typeof( DownstackStateMachine));
        private HiveIntegration.IntegrationGui _plugin;
        private DeviceInterface _platemover;
        private RobotInterface _robot;
        private string _labware;
        private string _expected_barcode;
        private bool _allow_unload_from_carts;

        public enum State
        {
            Idle,
            MoveStageToInternal,
            TransferPlate,
            MoveStageToExternal,
            Done,
            Failed
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

        /// <summary>
        /// Assumes that the caller checked for presence of the requested barcode before trying to unload the plate
        /// </summary>
        /// <param name="error_interface"></param>
        /// <param name="plugin"></param>
        /// <param name="labware"></param>
        /// <param name="called_from_diags"></param>
        /// <param name="expected_barcode"></param>
        /// <param name="allow_unload_from_carts"></param>
        public DownstackStateMachine( IError error_interface, HiveIntegration.IntegrationGui plugin, string labware, bool called_from_diags, string expected_barcode,
                                      bool allow_unload_from_carts=false)
            : base( null, error_interface, State.Idle, State.Done, State.Done,
                    Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, called_from_diags)
        {
            _plugin = plugin;
            _platemover = plugin._platemover;
            _robot = plugin._robot;
            _labware = labware;
            _expected_barcode = expected_barcode;
            _allow_unload_from_carts = allow_unload_from_carts;

            // need to try SMW2's specialized functions later.  for now I am doing it the old way.
            SM.Configure( State.Idle)
                .Permit( Trigger.Success, State.MoveStageToInternal);
            SM.Configure(State.MoveStageToInternal)
                .Permit(Trigger.Success, State.TransferPlate)
                .Permit(Trigger.Failure, State.Failed)
                .OnEntry(MoveStageToInternal);
            SM.Configure(State.TransferPlate)
                .Permit(Trigger.Success, State.MoveStageToExternal)
                .Permit(Trigger.Failure, State.Failed)
                .OnEntry(TransferPlate);
            SM.Configure(State.MoveStageToExternal)
                .Permit(Trigger.Success, State.Done)
                .Permit(Trigger.Failure, State.Failed)
                .OnEntry(MoveStageToExternal);
            SM.Configure( State.Done)
                .OnEntry( Done);
            SM.Configure( State.Failed)
                .OnEntry( Failed);
        }

        private void MoveStageToInternal()
        {
            _log.Info( "Moving PlateMover to internal teachpoint");
            if( !_platemover.ExecuteCommand( PlateMover.PlateMoverPlugin.PlateMoverCommands.MoveToInternalPortraitTeachpoint, null)) {
                var msg = string.Format("Failure during Downstack MoveStageToInternal");
                _log.Error(msg);
                LastError = msg;
                Fire( Trigger.Failure);
            } else {
                Fire( Trigger.Success);
            }
        }

        private void MoveStageToExternal()
        {
            _log.Info( "Moving PlateMover to external teachpoint");
            if( !_platemover.ExecuteCommand( PlateMover.PlateMoverPlugin.PlateMoverCommands.MoveToExternalTeachpoint, null)) {
                var msg = string.Format("Failure during Downstack MoveStageToExternal");
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
                var possible_location_names = (from kvp in plates_available where kvp.Key == _expected_barcode select kvp.Value);
                
                // these are the variables used to retrieve the plate, whether it is in static storage or carts
                // actually, I am going to re-use the storage reference instead, which defaults to the robot but could be changed to point to a valid cart
                string from_location = "";

                // DKM 2012-05-17 added ability to unload from carts, just in case.  you can switch this on and off easily
                if( possible_location_names.Count() == 0) {
                    // loop over the available carts if we're allowing carts
                    if( _allow_unload_from_carts) {
                        // loop over the carts
                        foreach( var dock in _plugin._docks.Values) {
                            var cart_plates_available = dock.GetInventory( robot_name);
                            var cart_location_name = (from kvp in cart_plates_available where kvp.Key == _expected_barcode select kvp.Value);
                            if( cart_location_name.Count() != 0) {
                                storage = dock;
                                from_location = cart_location_name.First();
                                break;
                            }                            
                        }
                    } else {
                        throw new Exception( String.Format( "The barcode '{0}' was not found in static storage, and unloading from carts is not allowed", _expected_barcode));
                    }
                } else {
                    // we're not allowing carts, but thankfully we have found the barcode in static storage!
                    from_location = possible_location_names.First();
                }

                bool is_portrait = true;
                _robot.TransferPlate( (storage as DeviceInterface).Name, from_location, (_platemover as DeviceInterface).Name, is_portrait ? "PlateMover (portrait)" : "PlateMover (landscape)",
                                      _labware, new MutableString(_expected_barcode));
                storage.Unload( _labware, _expected_barcode, from_location);
                Fire( Trigger.Success);
            } catch( Exception e) {
                var msg = string.Format("Failure during Downstack TransferPlate, error message was '{0}'", e.Message);
                _log.Error(msg);
                LastError = msg;
                Fire( Trigger.Failure);
            }
        }

        private void Failed()
        {
            _log.Info( "Unload failed");
            AbortedStateFunction();
            throw new Exception( LastError);
        }

        private void Done()
        {
            _log.Info( "Done unloading plate");
            EndStateFunction();
        }
    }
}
