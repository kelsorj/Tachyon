#define EXECUTE_WITHOUT_TASKS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.Utils;
using System.Threading;
using System.Diagnostics;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.TaskListXMLParser;
using BioNex.Shared.DeviceInterfaces;
using System.Collections;

namespace BioNex.SynapsisPrototype.Model
{
    // TODO -- this is really for motion related tasks only -- maybe rename it to that effect and add a non-motion task execution state machine e.g. for IO triggers
    public class TaskExecutionStateMachine : StateMachineWrapper<TaskExecutionStateMachine.State, TaskExecutionStateMachine.Trigger>
    {
        private string _last_error_message { get; set; }
        private IError _error_interface { get; set; }
        private Plate _plate { get; set; }
        private IList<PlateTask> _tasks { get; set; }
        private AccessibleDeviceInterface _current_device { get; set; }
        private string _current_location_name { get; set; }
        private IEnumerator _iterator { get; set; }
        private LabAutoPlateTransferService _plate_transfer_service { get; set; }
        private LabAutoPlateTransferService.PlateReturnInfo _plate_return_info { get; set; }
        private RobotInterface _robot_field;
        private RobotInterface _robot
        {
            get { return _robot_field; }
            set {
                _robot_field = value;
            }
        }
        private DeviceManager _device_manager { get; set; }
        private AutoResetEvent _plate_moved_off { get; set; }

        // for labauto only
        private const string PlateMoverInstanceName = "PlateMover";
        
        public enum State
        {
            Idle,
            RotateStageForBumblebee,
            RotateStageForBumblebeeError,
            MoveFromBumblebeeToStage,
            MoveFromBumblebeeToStageError,
            RotateStageToLandscape,
            RotateStageToLandscapeError,
            GetNextTask,
            PickPlate,
            PickPlateError,
            PlacePlate,
            PlacePlateError,
            ExecuteTask,
            ExecuteTaskError,
            PickForReturnToStorage,
            PickForReturnToStorageError,
            PlaceForReturnToStorage,
            PlaceForReturnToStorageError,
            RotateForStorage,
            RotateForStorageError,
            Done,
        }

        public enum Trigger
        {
            Execute,
            NoMoreTasks,
            Success,
            Failure,
            Retry,
            Ignore,
            GoToStorage,
            PlateAtLocationAlready,
            Abort,
        }

        public TaskExecutionStateMachine( Plate plate, IList<PlateTask> tasks, AccessibleDeviceInterface origin_device, string origin_location_name,
                                          LabAutoPlateTransferService.PlateReturnInfo plate_return_info, LabAutoPlateTransferService plate_transfer_service,
                                          DeviceManager device_manager, IError error_interface, AutoResetEvent plate_moved_off)
            : base( typeof( TaskExecutionStateMachine), State.Idle, Trigger.Execute, Trigger.Retry, Trigger.Ignore, Trigger.Abort, false)
        {
            _plate = plate;
            _tasks = tasks;
            _iterator = tasks.GetEnumerator();
            _error_interface = error_interface;
            _current_device = origin_device;
            _current_location_name = origin_location_name;
            _plate_transfer_service = plate_transfer_service;
            _plate_return_info = plate_return_info;
            _device_manager = device_manager;
            _plate_moved_off = plate_moved_off;
            InitializeStates();
            Debug.WriteLine( String.Format( "Destination plate barcode {0} has {1} tasks", plate.Barcode, tasks.Count()));
        }



        private void InitializeStates()
        {
            SM.Configure( State.Idle)
                //.Permit( Trigger.Execute, State.RotateStageForBumblebee)
                .Permit( Trigger.Execute, State.PickForReturnToStorage)
                .Permit( Trigger.Abort, State.Done);
            SM.Configure( State.RotateStageForBumblebee)
                .Permit( Trigger.Success, State.MoveFromBumblebeeToStage)
                .Permit( Trigger.Failure, State.RotateStageForBumblebeeError)
                .Permit( Trigger.Abort, State.Done)
                .OnEntry( RotateStageForBumblebee);
            SM.Configure( State.RotateStageForBumblebeeError)
                .Permit( Trigger.Retry, State.RotateStageForBumblebee)
                .Permit( Trigger.Abort, State.Done)
                .OnEntry( f => HandleErrorWithRetryOnly( _last_error_message));
            SM.Configure( State.MoveFromBumblebeeToStage)
                .Permit( Trigger.Success, State.RotateStageToLandscape)
                .Permit( Trigger.Failure, State.MoveFromBumblebeeToStageError)
                .Permit( Trigger.Abort, State.Done)
                .OnEntry( MoveFromBumblebeeToStage);
            SM.Configure( State.MoveFromBumblebeeToStageError)
                .Permit( Trigger.Retry, State.MoveFromBumblebeeToStage)
                .Permit( Trigger.Abort, State.Done)
                .OnEntry( f => HandleErrorWithRetryOnly( _last_error_message));
            SM.Configure( State.RotateStageToLandscape)
                .Permit( Trigger.Success, State.GetNextTask)
                .Permit( Trigger.Failure, State.RotateStageToLandscapeError)
                .Permit( Trigger.Abort, State.Done)
                .OnEntry( RotateStageToLandscape);
            SM.Configure( State.RotateStageToLandscapeError)
                .Permit( Trigger.Retry, State.RotateStageToLandscape)
                .Permit( Trigger.Abort, State.Done)
                .OnEntry( f => HandleErrorWithRetryOnly( _last_error_message));
            SM.Configure( State.GetNextTask)
                .Permit( Trigger.NoMoreTasks, State.RotateForStorage)
                .Permit( Trigger.Success, State.PickPlate)
                .Permit( Trigger.Abort, State.Done)
                .OnEntry( GetNextTask);
            SM.Configure( State.PickPlate)
                .Permit( Trigger.Failure, State.PickPlateError)
                .Permit( Trigger.Success, State.PlacePlate)
                .Permit( Trigger.PlateAtLocationAlready, State.ExecuteTask)
                .Permit( Trigger.Abort, State.Done)
                .OnEntry( PickPlate);
            SM.Configure( State.RotateForStorage)
                .Permit( Trigger.Failure, State.RotateForStorageError)
                .Permit( Trigger.Success, State.PickForReturnToStorage)
                .Permit( Trigger.Abort, State.Done)
                .OnEntry( RotateForStorage);
            SM.Configure( State.RotateForStorageError)
                .Permit( Trigger.Retry, State.RotateForStorage)
                .Permit( Trigger.Ignore, State.PickForReturnToStorage)
                .Permit( Trigger.Abort, State.Done)
                .OnEntry( f => HandleErrorWithRetryOnly( _last_error_message));
            // end
            SM.Configure( State.PickPlateError)
                .Permit( Trigger.Retry, State.PickPlate)
                .Permit( Trigger.Abort, State.Done)
                .OnEntry( f => HandleErrorWithRetryOnly( _last_error_message));
            SM.Configure( State.PlacePlate)
                .Permit( Trigger.Failure, State.PlacePlateError)
                .Permit( Trigger.Success, State.ExecuteTask)
                .Permit( Trigger.Abort, State.Done)
                .OnEntry( PlacePlate);
            SM.Configure( State.PlacePlateError)
                .Permit( Trigger.Retry, State.PlacePlate)
                .Permit( Trigger.Abort, State.Done)
                .OnEntry( f => HandleErrorWithRetryOnly( _last_error_message));
            SM.Configure( State.ExecuteTask)
                .Permit( Trigger.Failure, State.ExecuteTaskError)
                .Permit( Trigger.Success, State.GetNextTask)
                .Permit( Trigger.Abort, State.Done)
                .OnEntry( ExecuteTask);
            SM.Configure( State.ExecuteTaskError)
                .Permit( Trigger.Retry, State.ExecuteTask)
                .Permit( Trigger.Abort, State.Done)
                .OnEntry( f => HandleErrorWithRetryOnly( _last_error_message));
            SM.Configure( State.PickForReturnToStorage)
                .Permit( Trigger.Success, State.PlaceForReturnToStorage)
                .Permit( Trigger.Failure, State.PickForReturnToStorageError)
                .Permit( Trigger.Abort, State.Done)
                .OnEntry( PickForReturnToStorage);
            SM.Configure( State.PickForReturnToStorageError)
                .Permit( Trigger.Retry, State.PickForReturnToStorage)
                .Permit( Trigger.Abort, State.Done)
                .OnEntry( f => HandleErrorWithRetryOnly( _last_error_message));
            SM.Configure( State.PlaceForReturnToStorage)
                .Permit( Trigger.Success, State.Done)
                .Permit( Trigger.Failure, State.PlaceForReturnToStorageError)
                .Permit( Trigger.Abort, State.Done)
                .OnEntry( PlaceForReturnToStorage);
            SM.Configure( State.PlaceForReturnToStorageError)
                .Permit( Trigger.Retry, State.PlaceForReturnToStorage)
                .Permit( Trigger.Abort, State.Done)
                .OnEntry( f => HandleErrorWithRetryOnly( _last_error_message));
            SM.Configure( State.Done)
                .OnEntry( EndStateFunction);
        }

        public void Abort()
        {
            base.Abort();
        }

        private void RotateStageForBumblebee()
        {
            try {
                // rotate the stage
                RotatePortrait();

                // pick the plate from the bumblebee
                // note that this is not really correct, and is just good enough for us to get by.  Getting a plate
                // from point A to point B will require one or more robots.
                _robot = _device_manager.GetRobotThatReachesLocation( _current_device, _current_location_name);
                Debug.Assert( _robot != null, String.Format( "Couldn't find a robot to reach device '{0}', location '{1}'", _current_device.Name, _current_location_name));
                // for now, we are assuming that each device only has one plate location
                // getting devices like this is tedious -- maybe I should put a ref to the device in PlateTask instead.
                var task_device = _device_manager.AccessibleDevicePluginsAvailable[PlateMoverInstanceName];
                // need to reserve both teachpoints for the
                _device_manager.ReservePlateLocations( task_device);
                _plate_transfer_service.RequestRobot( _robot);
                Debug.WriteLine( String.Format( "picking plate from device '{0}', location '{1}'", _current_device.Name, _current_location_name));
                _robot.Pick( _current_device.Name, _current_location_name, _plate.LabwareName, true, _plate.Barcode);
                _device_manager.FreePlateLocation( _current_device, _current_location_name);
                // allow plate scheduler for hitpicks to bring another destination plate on
                _plate_moved_off.Set();
                Fire( Trigger.Success);
            } catch( Exception ex) {
                _last_error_message = ex.Message;
                Fire( Trigger.Failure);
            }
        }

        private void MoveFromBumblebeeToStage()
        {
            try {
                // place the plate at the stage
                Debug.Assert( _robot != null);
                // getting devices like this is tedious -- maybe I should put a ref to the device in PlateTask instead.
                string device_instance = PlateMoverInstanceName;
                string task_location = "Stage, Portrait";
                DeviceInterface task_device = _device_manager.DevicePluginsAvailable[device_instance]; // unused variable for exception check?
                Debug.WriteLine( String.Format( "placing plate at device '{0}', location '{1}'", device_instance, task_location));
                // assumption here is that the robot plugin will implement error handling, so when
                // this method returns, we assume everything went ok
                _robot.Place( device_instance, task_location, _plate.LabwareName, true, _plate.Barcode);
                _current_device = _device_manager.AccessibleDevicePluginsAvailable[device_instance];
                //! \todo fix for devices that have more than one location
                _current_location_name = task_location;
                Fire( Trigger.Success);
            } catch( Exception ex) {
                _last_error_message = ex.Message;
                Fire( Trigger.Failure);
            } finally {
                _plate_transfer_service.FreeRobot( _robot);
            }
        }

        private void RotateStageToLandscape()
        {
            try {
                RotateLandscape();
                _current_location_name = "Stage, Landscape";
                Fire( Trigger.Success);
            } catch( Exception ex) {
                _last_error_message = ex.Message;
                Fire( Trigger.Failure);
            }
        }

        private void GetNextTask()
        {
            if( !_iterator.MoveNext()) {
                Debug.WriteLine( "No more tasks");
                Fire( Trigger.NoMoreTasks);
                return;
            }
            PlateTask current_task = (PlateTask)(_iterator.Current);
            Debug.WriteLine( String.Format( "getting next task: {0}", current_task.Command));
            Fire( Trigger.Success);
        }

        /// <summary>
        /// Here, we'll either be picking a plate from Bumblebee in portrait to go to the plate rotator, or
        /// from the rotator in landscape to go to devices
        /// </summary>
        private void PickPlate()
        {
            // next, ensure that the destination plate location is available
            PlateTask task = (PlateTask)(_iterator.Current);
            // for now, we are assuming that each device only has one plate location
            // getting devices like this is tedious -- maybe I should put a ref to the device in PlateTask instead.
            AccessibleDeviceInterface task_device = _device_manager.AccessibleDevicePluginsAvailable[task.DeviceInstance];
            Debug.Assert(task_device != null);

            // need to be careful here -- if this is the first task after the plate is dropped off at the plate mover,
            // then we don't want to move the plate at all.  We don't want to reserve the plate location, either.  This
            // is obviously suboptimal, and the real implementation later on should be smart enough to know that if a
            // plate is already at its task location, don't do anything!
            if( task_device.Name == PlateMoverInstanceName && task == _tasks.First()) {
                Fire( Trigger.PlateAtLocationAlready);
                return;
            }                
                
            // note that this is not really correct, and is just good enough for us to get by.  Getting a plate
            // from point A to point B will require one or more robots.
            _robot = _device_manager.GetRobotThatReachesLocation( _current_device, _current_location_name);
            Debug.Assert( _robot != null, String.Format( "Couldn't find a robot to reach device '{0}', location '{1}'", _current_device.Name, _current_location_name));
            
            // note that if we're picking a plate from the PlateMover, it'll magically work because "Stage, Landscape" should
            // be the first teachpoint location sent back by the plugin
            string task_location = task_device.PlateLocationInfo.First().LocationName;

            _device_manager.ReservePlateLocation( task_device, task_location);
            _plate_transfer_service.RequestRobot( _robot);

            try {
                Debug.WriteLine( String.Format( "picking plate from device '{0}', location '{1}'", _current_device.Name, _current_location_name));
                _robot.Pick( _current_device.Name, _current_location_name, _plate.LabwareName, false, _plate.Barcode);

                _device_manager.FreePlateLocations( _current_device);
                Fire( Trigger.Success);
            } catch( Exception) {
                _plate_transfer_service.FreeRobot( _robot);
                Fire( Trigger.Success);
            }
        }

        private void PlacePlate()
        {
            try {
                Debug.Assert( _robot != null);
                // the location to place to is the device for the current task
                PlateTask task = (PlateTask)(_iterator.Current);
                // getting devices like this is tedious -- maybe I should put a ref to the device in PlateTask instead.
                var task_device = _device_manager.AccessibleDevicePluginsAvailable[task.DeviceInstance];
                string task_location = task_device.PlateLocationInfo.First().LocationName;
                Debug.WriteLine( String.Format( "placing plate at device '{0}', location '{1}'", task.DeviceInstance, task_location));
                // assumption here is that the robot plugin will implement error handling, so when
                // this method returns, we assume everything went ok
                _robot.Place( task.DeviceInstance, task_location, _plate.LabwareName, false, _plate.Barcode);
                _current_device = _device_manager.AccessibleDevicePluginsAvailable[ task.DeviceInstance];
                //! \todo fix for devices that have more than one location
                _current_location_name = _current_device.PlateLocationInfo.First().LocationName;
                Fire( Trigger.Success);            
            } catch( Exception) {
                Fire( Trigger.Failure);
            } finally {
                _plate_transfer_service.FreeRobot( _robot);
            }
        }

        /// <summary>
        /// this TEMPORARY state rotates the stage to the PORTRAIT position, so the robot can pick it up from
        /// here after dropping off for the RotateStage task.  It will then move the plate into storage, which
        /// is expecting plates in the portrait orientation.
        /// </summary>
        private void RotateForStorage()
        {
            try {
                RotatePortrait();
                Fire( Trigger.Success);
            } catch( Exception ex) {
                _last_error_message = ex.Message;
                Fire( Trigger.Failure);
            }
        }

        /// <exception cref="AxisException">
        /// </exception>
        private void RotatePortrait()
        {
            DeviceInterface plate_mover = _device_manager.DevicePluginsAvailable[PlateMoverInstanceName];
            plate_mover.ExecuteCommand( "RotatePlate", new Dictionary<string,object> { {"orientation", "portrait"} });
        }

        /// <exception cref="AxisException">
        /// </exception>
        private void RotateLandscape()
        {
            DeviceInterface plate_mover = _device_manager.DevicePluginsAvailable[PlateMoverInstanceName];
            plate_mover.ExecuteCommand( "RotatePlate", new Dictionary<string,object> { {"orientation", "landscape"} });
        }

        private void ExecuteTask()
        {
            try {
                PlateTask task = (PlateTask)(_iterator.Current);
                Debug.WriteLine( String.Format( "executing task {0}", task.Command));
                DeviceInterface device = _device_manager.DevicePluginsAvailable[task.DeviceInstance];
                Dictionary<string,object> taskparams = new Dictionary<string,object>();
                foreach( var param in task.ParametersAndVariables)
                    taskparams.Add( param.Name, param.Value);
                device.ExecuteCommand( task.Command, taskparams);
                Fire( Trigger.Success);
            } catch( Exception ex) {
                _last_error_message = ex.Message;
                Fire( Trigger.Failure);
            }
        }

        private void PickForReturnToStorage()
        {
            _robot = _device_manager.GetRobotThatReachesLocation( _current_device, _current_location_name);
            Debug.Assert( _robot != null, String.Format( "Couldn't find a robot to reach device '{0}', location '{1}'", _current_device.Name, _current_location_name));

            _plate_transfer_service.RequestRobot( _robot);

            try {
                Debug.WriteLine( String.Format( "returning plate -- picking from device '{0}', location '{1}'", _current_device.Name, _current_location_name));
                _robot.Pick( _current_device.Name, _current_location_name, _plate.LabwareName, true, _plate.Barcode);
                _device_manager.FreePlateLocations( _current_device);
                Fire( Trigger.Success);
            } catch( Exception) {
                _plate_transfer_service.FreeRobot( _robot);
                Fire( Trigger.Success);
            }
        }

        private void PlaceForReturnToStorage()
        {
            try {
                Debug.Assert( _robot != null);
                DeviceInterface plate_return_device = _plate_return_info.PlateStorageDevice as DeviceInterface;
                Debug.WriteLine( String.Format( "returning plate -- placing plate at device '{0}', location '{1}'",
                                 plate_return_device.Name, _plate_return_info.LocationName));
                _robot.Place( plate_return_device.Name, _plate_return_info.LocationName, _plate.LabwareName, true, _plate.Barcode);
            } catch( Exception) {

            } finally {
                _plate_transfer_service.FreeRobot( _robot);
                Fire( Trigger.Success);
            }
        }

        private void HandleErrorWithRetryOnly(string message)
        {
            // if we're pausing, we would get an error and the Paused flag will be set
            // DKM 2011-03-30 have new behavior because the main GUI abort button now pauses first.  We need to
            //                break out on an SMPauseEvent or _main_gui_abort_event.
            int which_event = WaitHandle.WaitAny( new WaitHandle[] { SMPauseEvent, _main_gui_abort_event } );
            if( which_event == 1) { // _main_gui_abort_event
                Fire( Trigger.Abort);
                return;
            }

            try {
                string retry = "Try move again";
                List< string> error_strings = new List< string>{ retry};
                if( _called_from_diags){
                    error_strings.Add( ABORT_LABEL);
                }
                BioNex.Shared.LibraryInterfaces.ErrorData error = new BioNex.Shared.LibraryInterfaces.ErrorData(message, error_strings);
                SMStopwatch.Stop();
                _error_interface.AddError(error);
                List< ManualResetEvent> events = new List< ManualResetEvent>{ _main_gui_abort_event};
                events.AddRange( error.EventArray);
                int event_index = WaitHandle.WaitAny( events.ToArray());
                SMStopwatch.Start();
                if( error.TriggeredEvent == retry){
                    Fire( Trigger.Retry);
                } else if(( error.TriggeredEvent == ABORT_LABEL) || ( event_index == 0)){
                    Fire( Trigger.Abort);
                } else{
                    Debug.Assert( false, UNEXPECTED_EVENT_STRING);
                    Fire( Trigger.Abort);
                }
            } catch( Exception ex) {
                _last_error_message = ex.Message;
                Debug.Assert( false, ex.Message);
            }
        }
    }
}
