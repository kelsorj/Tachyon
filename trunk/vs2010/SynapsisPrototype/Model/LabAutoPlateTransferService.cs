using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.PlateDefs;
using GalaSoft.MvvmLight.Messaging;
using log4net;

namespace BioNex.SynapsisPrototype.Model
{
    [Export(typeof(ExternalPlateTransferSchedulerInterface))]
    public class LabAutoPlateTransferService : ExternalPlateTransferSchedulerInterface, IDisposable
    {
        public class PlateReturnInfo
        {
            public PlateStorageInterface PlateStorageDevice { get; set; }
            public string LocationName { get; set; }
        }

        /// <summary>
        /// we need to keep track of all of the locations that we picked tipboxes / labware from,
        /// so store them here and remove them from the list of possible locations reported by
        /// plate storage devices
        /// </summary>
        /// <remarks>
        /// the issue with this implementation is that it's not device-specific.  For now, we can live with it.
        /// </remarks>
        /// locations ----vvvvvvvvvvvv
        /// labware -vvvv
        Dictionary<string,List<string>> UsedLabwareLocations { get; set; }
        // for now, this is the safest way to load labware back into its original location -- use another collection
        Dictionary<string,List<string>> ReplacedLabwareLocations { get; set; }
        
        private DeviceManager _device_manager { get; set; }
        private ILog Log = LogManager.GetLogger( typeof( PlateTransferService));
        private bool _aborting { get; set; }
        private TaskExecutionStateMachine _sm { get; set; }

        /// <summary>
        /// Keeps track of which robots are currently moving a plate from one location to another
        /// </summary>
        private Dictionary<RobotInterface, bool> _robots_in_use { get; set; }
        private ReaderWriterLockSlim _robot_usage_lock { get; set; }

        /// <summary>
        /// Maps plates by barcode to the location that it came from
        /// </summary>
        private Dictionary<string,PlateReturnInfo> _original_plate_locations { get; set; }

        private IError _error_interface { get; set; }

        // for labauto post hitpick tasks
        private Dictionary<Plate,AutoResetEvent> _post_hitpick_task_events { get; set; }
        
        [ImportingConstructor]
        public LabAutoPlateTransferService( [Import] DeviceManager device_manager)
        {
            _device_manager = device_manager;
            _original_plate_locations = new Dictionary<string,PlateReturnInfo>();
            UsedLabwareLocations = new Dictionary<string,List<string>>();
            ReplacedLabwareLocations = new Dictionary<string,List<string>>();
            Messenger.Default.Register<AbortCommand>( this, Abort);
            Messenger.Default.Register<ResetCommand>( this, Reset);
            _robots_in_use = new Dictionary<RobotInterface,bool>();
            _robot_usage_lock = new ReaderWriterLockSlim();
            _post_hitpick_task_events = new Dictionary<Plate,AutoResetEvent>();
        }

        public string GetPlateTransferStrategyName()
        {
            return "LabAuto";
        }

        #region ExternalPlateTransferSchedulerInterface Members

        public void SetErrorInterface( IError error_interface)
        {
            _error_interface = error_interface;
        }

        /// <summary>
        /// Moves a plate from storage to a device
        /// </summary>
        /// <param name="caller">Device that wants the plate</param>
        /// <param name="labware_name">The type of labware, if relevant</param>
        /// <param name="barcode">Barcode on the plate, if any</param>
        /// <param name="to_location_name">The location on "caller" that should get the plate</param>
        public void RequestPlate( AccessibleDeviceInterface caller, Plate plate, string to_location_name)
        {
            // if plate is null, then it's a tipbox
            string barcode = plate == null ? "" : plate.Barcode;
            string labware_name = plate == null ? "tipbox" : plate.LabwareName;
            // we'll either want to ask for a barcode, or for a labware type, but probably not both
            // scenario 1: barcode
            // scenario 2: labware

            Log.Info( String.Format( "Device '{0}' is requesting the plate with barcode '{1}', labware '{2}' at location named '{3}'", caller.Name, barcode, labware_name, to_location_name));

            // 1
            if( barcode != "") {
                RobotInterface robot_that_reaches_plate = null;
                try {
                    // query all of the plate storage plugins to see which one has the desired barcode
                    string plate_location_name;
                    PlateStorageInterface device_with_plate = _device_manager.GetPlateStorageWithBarcode( barcode, out plate_location_name);
                    if( device_with_plate == null)
                        throw new PlateNotFoundException( labware_name, barcode);

                    // now figure out if any robot can reach this plate
                    robot_that_reaches_plate = _device_manager.GetRobotThatReachesLocation( device_with_plate as AccessibleDeviceInterface, plate_location_name);
                    if( robot_that_reaches_plate == null)
                        throw new PlateNotReachableException( labware_name, barcode, plate_location_name);

                    // now figure out if any robot can reach the destination point
                    RobotInterface robot_that_reaches_destination = _device_manager.GetRobotThatReachesLocation( caller, to_location_name);
                    if( robot_that_reaches_destination == null)
                        throw new LocationNotReachableException( to_location_name);

                    // TEMPORARY CODE: this works since we already know we only have one robot in system
                    // make sure that robot_that_reaches_plate is the same as the robot_that_reaches_destination
                    Debug.Assert( robot_that_reaches_plate == robot_that_reaches_destination);

                    // save where the plate came from
                    _original_plate_locations.Add( barcode, new PlateReturnInfo { LocationName = plate_location_name, PlateStorageDevice = device_with_plate } );

                    // if we get here, then we know where a plate is, and we know we can move it from A
                    // to B with a robot
                    //! \todo get the labware information for picking and placing!
                    device_with_plate.Unload( labware_name, barcode, "");
                    RequestRobot( robot_that_reaches_plate);
                    /*
                    if( !_aborting)
                        robot_that_reaches_plate.Pick( (device_with_plate as AccessibleDeviceInterface).Name, plate_location_name, labware_name, true);
                    if( !_aborting)
                        robot_that_reaches_plate.Place( caller.Name, to_location_name, labware_name, true);
                     */
                    if( !_aborting)
                        robot_that_reaches_plate.TransferPlate( (device_with_plate as AccessibleDeviceInterface).Name, plate_location_name,
                                                                caller.Name, to_location_name, labware_name, true, barcode);
                } catch( Exception) {
                    throw;
                } finally {
                    FreeRobot( robot_that_reaches_plate);
                }
            }
            // 2
            // this case is mainly for tipboxes.  We'll ask for a "tipbox" and it should find whatever is
            // available.
            else if( labware_name != "") {
                RobotInterface robot_that_reaches_plate = null;
                try {
                    Debug.Assert( labware_name == "tipbox");
                    // find a storage device that has this sort of labware
                    var storage_devices = from p in _device_manager.PlateStoragePluginsAvailable select p.Value;
                    PlateStorageInterface device_with_labware = null;
                    IEnumerable<string> locations;
                    string from_location_name = "";
                    foreach( var p in storage_devices) {
                        locations = p.GetLocationsForLabware( labware_name);
                        if( locations.Count() == 0)
                            continue;
                        else{
                            device_with_labware = p;
                            // create the list of location names for the labware name, if it doesn't exist already
                            if( !UsedLabwareLocations.ContainsKey( labware_name))
                                UsedLabwareLocations.Add( labware_name, new List<string>());
                            if( !ReplacedLabwareLocations.ContainsKey( labware_name))
                                ReplacedLabwareLocations.Add( labware_name, new List<string>());
                            // get all location names with this labware, EXCEPT for those that have been used already
                            from_location_name = locations.Except( UsedLabwareLocations[labware_name]).First();
                            // store this location name so we don't use it again
                            UsedLabwareLocations[labware_name].Add( from_location_name);
                            break;
                        }
                    }
                    if( device_with_labware == null)
                        throw new LabwareNameNotAvailableException( labware_name);
                    // we've got locations, now determine which one to use, using the TipTracker, or something like it
                    robot_that_reaches_plate = _device_manager.GetRobotThatReachesLocation( device_with_labware as AccessibleDeviceInterface, from_location_name);
                    RobotInterface robot_that_reaches_destination = _device_manager.GetRobotThatReachesLocation( caller as AccessibleDeviceInterface, to_location_name);
                    Debug.Assert( robot_that_reaches_plate == robot_that_reaches_destination, 
                                  String.Format( "Expected robot that reaches plate [{0}] to be the same as the robot that reaches the destination location [{1}].  Either teachpoint '{2}' or '{3}' is missing???",
                                  robot_that_reaches_plate != null ? robot_that_reaches_plate.ToString() : "null",
                                  robot_that_reaches_destination != null ? robot_that_reaches_destination.ToString() : "null",
                                  from_location_name, to_location_name));

                    // save where the plate came from
                    if( !_original_plate_locations.ContainsKey( barcode))
                        _original_plate_locations.Add( barcode, new PlateReturnInfo { LocationName = from_location_name, PlateStorageDevice = device_with_labware} );

                    device_with_labware.Unload( labware_name, barcode, from_location_name);
                    RequestRobot( robot_that_reaches_plate);
                    /*
                    if( !_aborting)
                        robot_that_reaches_plate.Pick( (device_with_labware as DeviceInterface).Name, from_location_name, labware_name, true);
                    if( !_aborting)
                        robot_that_reaches_plate.Place( caller.Name, to_location_name, labware_name, true);
                     */
                    if( !_aborting)
                        robot_that_reaches_plate.TransferPlate( (device_with_labware as DeviceInterface).Name, from_location_name,
                                                                caller.Name, to_location_name, labware_name, true, barcode);
                    _aborting = false;
                } catch( Exception) {
                    throw;
                } finally {
                    FreeRobot( robot_that_reaches_plate);
                }
            }
        }

        /// <summary>
        /// Moves a plate from the device's (caller) location named from_location_name and puts it back into storage.
        /// Executes all tasks requested before going to storage.
        /// </summary>
        /// <param name="caller">Device that has the plate that needs to be put back into storage</param>
        /// <param name="labware_name"></param>
        /// <param name="barcode"></param>
        /// <param name="from_location_name">the location on caller that has the plate</param>
        public void ReturnPlate( AccessibleDeviceInterface caller, Plate plate, string from_location_name)
        {
            // if plate is null, then it's a tipbox
            string barcode = plate == null ? "" : plate.Barcode;
            string labware_name = plate == null ? "tipbox" : plate.LabwareName;

            Debug.Assert( _original_plate_locations.ContainsKey( barcode));

            // if a tipbox, just return the plate without starting a thread since tipboxes don't have post-hitpick tasks associated with them
            if( (( barcode == "") && ( labware_name == "tipbox")) || plate is SourcePlate) {
                RobotInterface robot_that_reaches_plate = null;
                try {
                    Log.Info( String.Format( "Device '{0}' is returning the plate with barcode '{1}', labware '{2}' from location named '{3}'", caller.Name, barcode, labware_name, from_location_name));

                    // find the robot that can reach this location
                    robot_that_reaches_plate = _device_manager.GetRobotThatReachesLocation( caller, from_location_name);
                    if( robot_that_reaches_plate == null)
                        throw new PlateNotReachableException( labware_name, barcode, from_location_name);
                    // pull information about where the plate should go next
                    PlateReturnInfo plate_return_info = _original_plate_locations[barcode];
                    _original_plate_locations.Remove( barcode);
                    if(( barcode == "") && ( labware_name == "tipbox")){
                        // REED, look here to change destination of return tipbox.
                        bool return_plate_to_storage = Properties.Settings.Default.ReturnTipBoxToOriginalLocation;
                        if( return_plate_to_storage) {
                            plate_return_info.LocationName = UsedLabwareLocations[labware_name].Except( ReplacedLabwareLocations[labware_name]).First();
                            ReplacedLabwareLocations[labware_name].Add( plate_return_info.LocationName);
                        } else {
                            plate_return_info.LocationName = robot_that_reaches_plate.GetTrashLocationName();
                        }
                    }
                    // find the robot that can reach the final plate location
                    RobotInterface robot_that_reaches_destination = _device_manager.GetRobotThatReachesLocation( plate_return_info.PlateStorageDevice as AccessibleDeviceInterface, plate_return_info.LocationName);
                    // for now, both robots should be the same
                    //string error_message = String.Format( "Device '{0}' does not have teachpoint '{1}'", (plate_return_info.PlateStorageDevice as DeviceInterface).Name, plate_return_info.LocationName);
                    //Debug.Assert( robot_that_reaches_plate == robot_that_reaches_destination, error_message);
                    Debug.Assert( robot_that_reaches_plate == robot_that_reaches_destination, 
                                    String.Format( "Expected robot that reaches plate [{0}] to be the same as the robot that reaches the destination location [{1}].  Either teachpoint '{2}' or '{3}' is missing???",
                                    robot_that_reaches_plate != null ? robot_that_reaches_plate.ToString() : "null",
                                    robot_that_reaches_destination != null ? robot_that_reaches_destination.ToString() : "null",
                                    from_location_name, plate_return_info.LocationName));
                    RequestRobot( robot_that_reaches_plate);
                    /*
                    if( !_aborting)
                        robot_that_reaches_plate.Pick( caller.Name, from_location_name, labware_name, true);
                    if( !_aborting)
                        robot_that_reaches_plate.Place( (plate_return_info.PlateStorageDevice as DeviceInterface).Name, plate_return_info.LocationName, labware_name, true);
                     */
                    if( !_aborting)
                        robot_that_reaches_plate.TransferPlate( caller.Name, from_location_name,
                                                                (plate_return_info.PlateStorageDevice as DeviceInterface).Name, plate_return_info.LocationName,
                                                                labware_name, true, barcode);
                    plate_return_info.PlateStorageDevice.Load( labware_name, barcode, plate_return_info.LocationName);
                    _aborting = false;
                } catch( Exception) {
                    throw;
                } finally {
                    FreeRobot( robot_that_reaches_plate);
                }
            } else {
                // if we're here, we have a barcoded DESTINATION plate
                // start thread and pass it this plate.  Task list is already in plate.  Thread will handle moving plates using RobotsInUse to limit robot usage
                AutoResetEvent plate_moved_off_event = new AutoResetEvent( false);
                MoveDestinationPlateDelegate dest_thread = new MoveDestinationPlateDelegate( MoveDestinationPlateToStorage);
                dest_thread.BeginInvoke( plate as DestinationPlate, caller, from_location_name, plate_moved_off_event, DestinationPlateThreadComplete, plate);
                _post_hitpick_task_events.Add( plate, new AutoResetEvent( false));
                plate_moved_off_event.WaitOne();
            }
        }

        private delegate void MoveDestinationPlateDelegate( DestinationPlate plate, AccessibleDeviceInterface origin_device, string origin_location_name, AutoResetEvent plate_moved_off);

        /// <summary>
        /// Moves the destination plate from the Bumblebee to the storage device, executing tasks along the way.
        /// This thread will use RobotsInUse to release the robot at intermediate points, so it can do useful
        /// stuff while waiting for each task to complete.
        /// </summary>
        /// <param name="plate"></param>
        private void MoveDestinationPlateToStorage( DestinationPlate plate, AccessibleDeviceInterface origin_device, string origin_location_name, AutoResetEvent plate_moved_off)
        {
            // uncomment this code if you want to run the task state machine
            ///*
            string barcode = plate.Barcode;
            string labware_name = plate.LabwareName; 

            PlateReturnInfo plate_return_info = _original_plate_locations[barcode];
            TaskExecutionStateMachine sm = new TaskExecutionStateMachine( plate, plate.PostHitpickTasks, origin_device, origin_location_name,
                                                                          plate_return_info, this, _device_manager, _error_interface, plate_moved_off);
            sm.Start();
            //_original_plate_locations.Remove( barcode);
            //*/

            // uncomment this code if you want the original behavior, i.e. bumblebee -> storage
            /*
            // find the robot that can reach this location
            RobotInterface robot_that_reaches_plate = _device_manager.GetRobotThatReachesLocation( origin_device, origin_location_name);
            if( robot_that_reaches_plate == null)
                throw new PlateNotReachableException( plate.LabwareName, plate.Barcode, origin_location_name);
            // pull information about where the plate should go next
            PlateReturnInfo plate_return_info = _original_plate_locations[plate.Barcode];
            _original_plate_locations.Remove( plate.Barcode);
            // find the robot that can reach the final plate location
            RobotInterface robot_that_reaches_destination = _device_manager.GetRobotThatReachesLocation( plate_return_info.PlateStorageDevice as AccessibleDeviceInterface, plate_return_info.LocationName);
            // for now, both robots should be the same
            Debug.Assert( robot_that_reaches_plate == robot_that_reaches_destination, 
                            String.Format( "Expected robot that reaches plate [{0}] to be the same as the robot that reaches the destination location [{1}].  Either teachpoint '{2}' or '{3}' is missing???",
                            robot_that_reaches_plate != null ? robot_that_reaches_plate.ToString() : "null",
                            robot_that_reaches_destination != null ? robot_that_reaches_destination.ToString() : "null",
                            origin_location_name, plate_return_info.LocationName));

            if( !_aborting)
                robot_that_reaches_plate.Pick( origin_device.Name, origin_location_name, plate.LabwareName, true);
            if( !_aborting)
                robot_that_reaches_plate.Place( (plate_return_info.PlateStorageDevice as DeviceInterface).Name, plate_return_info.LocationName, plate.LabwareName, true);
            plate_return_info.PlateStorageDevice.Load( plate.LabwareName, plate.Barcode, plate_return_info.LocationName);
            _aborting = false;
             */
        }

        private void DestinationPlateThreadComplete( IAsyncResult iar)
        {
            AsyncResult ar = (AsyncResult)iar;
            MoveDestinationPlateDelegate caller = (MoveDestinationPlateDelegate)ar.AsyncDelegate;

            try {
                caller.EndInvoke( iar);
                DestinationPlate plate = ar.AsyncState as DestinationPlate;
                // if plate is null, then it's a tipbox
                string barcode = plate == null ? "" : plate.Barcode;
                string labware_name = plate == null ? "tipbox" : plate.LabwareName;
                PlateReturnInfo plate_return_info = _original_plate_locations[barcode];
                plate_return_info.PlateStorageDevice.Load( labware_name, barcode, plate_return_info.LocationName);
                _original_plate_locations.Remove( barcode);
                _post_hitpick_task_events[plate].Set();
            } catch( Exception ex) {
                Log.Error( ex);
            }
        }

        private void Abort( AbortCommand command)
        {
            _aborting = true;
            if( _sm != null)
                _sm.Abort();
        }

        private void Reset( ResetCommand command)
        {
            _aborting = false;
            ResetPlateLocationTracking();
        }

        public void ResetPlateLocationTracking()
        {
            _original_plate_locations.Clear();
            UsedLabwareLocations.Clear();
            ReplacedLabwareLocations.Clear();
        }

        public void RequestRobot( RobotInterface robot)
        {
            if( robot == null)
                return;

            try {
                while( true) {
                    Thread.Sleep( 100);
                    _robot_usage_lock.EnterUpgradeableReadLock();
                    if( !_robots_in_use.ContainsKey( robot)) {
                        _robot_usage_lock.EnterWriteLock();
                        _robot_usage_lock.ExitUpgradeableReadLock();
                        _robots_in_use.Add( robot, true);
                        Debug.WriteLine( String.Format( "thread {0} locked robot '{1}'", Thread.CurrentThread.GetHashCode(), (robot as DeviceInterface).Name));
                        _robot_usage_lock.ExitWriteLock();
                        return;
                    } else {
                        if( _robots_in_use[robot] == true) {
                            _robot_usage_lock.ExitUpgradeableReadLock();
                            continue;
                        }
                        _robot_usage_lock.EnterWriteLock();
                        _robot_usage_lock.ExitUpgradeableReadLock();
                        _robots_in_use[robot] = true;
                        Debug.WriteLine( String.Format( "thread {0} locked robot '{1}'", Thread.CurrentThread.GetHashCode(), (robot as DeviceInterface).Name));
                        _robot_usage_lock.ExitWriteLock();
                        return;
                    }
                    _robot_usage_lock.ExitUpgradeableReadLock();
                }
            } catch( Exception) {
                Debug.Assert( false, "Failed in RequestRobot");
            } finally {

            }
        }

        public void FreeRobot( RobotInterface robot)
        {
            if( robot == null)
                return;

            try {
                _robot_usage_lock.EnterWriteLock();
                _robots_in_use[robot] = false;
                Debug.WriteLine( String.Format( "thread {0} freed robot '{1}'", Thread.CurrentThread.GetHashCode(), (robot as DeviceInterface).Name));
            } catch( Exception) {
                Debug.Assert( false, "Failed in FreeRobot");
            } finally {
                _robot_usage_lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Allows us to wait until all of the tasks are complete before signaling the completion of a protocol
        /// </summary>
        public void WaitForDestinationPostHitpickTasks()
        {
            foreach( var x in _post_hitpick_task_events.Select( x => x.Value))
                x.WaitOne();
            _post_hitpick_task_events.Clear();
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Messenger.Default.Unregister( this);
            GC.SuppressFinalize( this);
        }

        #endregion
    }
}
