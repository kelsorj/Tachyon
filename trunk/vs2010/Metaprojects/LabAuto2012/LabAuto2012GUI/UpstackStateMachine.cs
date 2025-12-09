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
    public class UpstackStateMachine : StateMachineWrapper2<UpstackStateMachine.State,UpstackStateMachine.Trigger>
    {
        private ILog _log = LogManager.GetLogger( typeof( UpstackStateMachine));
        private MonsantoPhase2GUI _plugin { get; set; }
        private DeviceInterface _platemover { get; set; }
        private RobotInterface _robot { get; set; }
        private string _labware_name { get; set; }
        private MutableString _barcode;

        public enum State
        {
            Idle,
            GetReservedStorageLocation,
            GetReservedStorageLocationError,
            MoveStageToInternal,
            MoveStageToInternalError,
            TransferPlate,
            TransferPlateError,
            Done,
        }

        public enum Trigger
        {
            Execute,
            Success,
            Failure,
            Retry,
            Ignore,
            Abort,
        }

        private static int _cycle_test_counter { get; set; }

        public UpstackStateMachine( IError error_interface, MonsantoPhase2GUI plugin, string labware_name, string barcode, bool called_from_diags)
            : base( typeof(UpstackStateMachine), State.Idle, State.Done, State.Done,
                    Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, error_interface, called_from_diags)
        {
            _plugin = plugin;
            _platemover = plugin._platemover;
            _robot = plugin._robot;
            _labware_name = labware_name;
            _barcode = (MutableString)barcode;

            // need to try SMW2's specialized functions later.  for now I am doing it the old way.
            SM.Configure( State.Idle)
                .Permit( Trigger.Success, State.GetReservedStorageLocation);
            SM.Configure( State.GetReservedStorageLocation)
                .Permit( Trigger.Failure, State.GetReservedStorageLocationError)
                .Permit( Trigger.Success, State.MoveStageToInternal)
                .OnEntry( GetReservedStorageLocation);
            SM.Configure( State.GetReservedStorageLocationError)
                .Permit( Trigger.Retry, State.GetReservedStorageLocation)
                .OnEntry( () => base.HandleErrorWithRetryOnly());
            SM.Configure( State.MoveStageToInternal)
                .Permit( Trigger.Success, State.TransferPlate)
                .Permit( Trigger.Failure, State.MoveStageToInternalError)
                .OnEntry( MoveStageToInternal);
            SM.Configure( State.MoveStageToInternalError)
                .Permit( Trigger.Retry, State.MoveStageToInternal)
                .OnEntry( () => base.HandleErrorWithRetryOnly());
            SM.Configure( State.TransferPlate)
                .Permit( Trigger.Success, State.Done)
                .Permit( Trigger.Failure, State.TransferPlateError)
                .OnEntry( TransferPlate);
            SM.Configure( State.TransferPlateError)
                .Permit( Trigger.Retry, State.TransferPlate)
                .OnEntry( () => base.HandleErrorWithRetryOnly());
            SM.Configure( State.Done)
                .OnEntry( Done);
        }

        private void GetReservedStorageLocation()
        {
            _log.Info( "Finding a location to store the upstacked plate");
            IEnumerable<string> available_locations = _plugin.GetAvailableStaticLocations();
            if( available_locations.Count() == 0) {
                LastError = "No storage space available for upstacking";
                Fire( Trigger.Failure);
            } else
                Fire( Trigger.Success);
        }

        private void MoveStageToInternal()
        {
            _log.Info( "Moving PlateMover to internal teachpoint");
            if( !_platemover.ExecuteCommand( "movetointernalteachpoint", null)) {
                LastError = "Failed to move plate mover to internal teachpoint";
                Fire( Trigger.Failure);
            } else {
                Fire( Trigger.Success);
            }
        }

        private void TransferPlate()
        {
            try {
                _log.Info( "Transferring plate from PlateMover to static storage location");
                // get next available plate location in inventory
                MonsantoPhase2GUI.AvailableLocation location = null;

                // wait here forever if we run out of locations 
                //! \todo -- this is essentially a race between upstacking and plate fate processing in the case where static storage fills up
                //! \todo -- since we're not reserving locations, we need to block here until one becomes available
                //! \todo -- I think this is fine, but we need to be able to handle ABORT through VWorks, and make sure that we don't cause an XMLRPC TIMEOUT
                //! \todo -- although timeout might be ok if we can recover via RETRY through VWorks (in which case TransferPlate state should probably just restart itself if location = null 
                while (location == null)
                {
                    location = _plugin.GetNextAvailableLocation();
                    System.Threading.Thread.Sleep(0);
                }                  

                // move the plate there
                bool is_portrait = true;
                _robot.TransferPlate( _platemover.Name, is_portrait ? "Stage, Portrait" : "Stage, Landscape",
                                      location.DeviceName, location.LocationName, _labware_name, is_portrait,
                                      _barcode);
                PlateStorageInterface storage = _robot as PlateStorageInterface;
                storage.Load( _labware_name, _plugin._robot.LastReadBarcode, location.LocationName);
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
            
            Fire( Trigger.Success);
        }

        private void Done()
        {
            _log.Info( "Done upstacking plate");
            EndStateFunction();
        }
    }
}
