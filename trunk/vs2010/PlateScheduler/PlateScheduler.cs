using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.IError;
using BioNex.Shared.Location;
using BioNex.Shared.PlateWork;
using BioNex.Shared.TaskListXMLParser;
using BioNex.SynapsisPrototype;
using log4net;
using System.Text;
using BioNex.Shared.LibraryInterfaces;

namespace BioNex.PlateScheduler
{
    [ PartCreationPolicy( CreationPolicy.Shared)]
    [ Export( typeof( IPlateScheduler))]
    [Export(typeof(IReportsStatus))]
    public class PlateScheduler : IPlateScheduler
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public IRobotScheduler RobotScheduler { get; protected set; }
        protected DeviceManager DeviceManager { get; set; }
        protected IError ErrorInterface { get; set; }
        protected ConcurrentQueue< Worklist> WorklistQueue { get; set; }
        protected Thread PlateSchedulerThread { get; set; }
        protected ManualResetEvent PlateSchedulerThreadStopEvent { get; set; }

        // map of destination plate reference to worklist name
        public Dictionary<ActivePlate,string> DestinationWorklistMap { get; private set; }

        // ----------------------------------------------------------------------
        // members.
        // ----------------------------------------------------------------------
        protected static readonly ILog Log = LogManager.GetLogger( typeof( PlateScheduler));

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        [ ImportingConstructor]
        public PlateScheduler( [ Import] IRobotScheduler robot_scheduler, [ Import] DeviceManager device_manager, [ Import] IError error_interface)
        {
            RobotScheduler = robot_scheduler;
            DeviceManager = device_manager;
            ErrorInterface = error_interface;
            WorklistQueue = new ConcurrentQueue< Worklist>();
            PlateSchedulerThreadStopEvent = new ManualResetEvent( false);

            DestinationWorklistMap = new Dictionary< ActivePlate, string>();
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public void StartScheduler()
        {
            PlateSchedulerThread = new Thread( PlateSchedulerThreadRunner){ Name = GetType().ToString(), IsBackground = true};
            PlateSchedulerThread.Start();
        }
        // ----------------------------------------------------------------------
        public void StopScheduler()
        {
            PlateSchedulerThreadStopEvent.Set();
            if( PlateSchedulerThread != null){
                PlateSchedulerThread.Join();
            }
        }
        // ----------------------------------------------------------------------
        public void EnqueueWorklist( Worklist worklist)
        {
            WorklistQueue.Enqueue( worklist);
            IEnumerable< DeviceInterface> bb_devices = from device in DeviceManager.DevicePluginsAvailable.Values
                                                       where device.ProductName == "Bumblebee"
                                                       select device;
            Debug.Assert( bb_devices.Count() == 1);
            PlateSchedulerDeviceInterface bb = bb_devices.FirstOrDefault() as PlateSchedulerDeviceInterface;
            Debug.Assert( bb != null);
            bb.EnqueueWorklist( worklist);
        }
        // ----------------------------------------------------------------------
        protected void PlateSchedulerThreadRunner()
        {
            while( !PlateSchedulerThreadStopEvent.WaitOne( 0)){
                Thread.Sleep( 100);
                Worklist worklist;
                bool worklist_dequeued = WorklistQueue.TryDequeue( out worklist);
                if( worklist != null){
                    DoWorklist( worklist);
                }
            }
        }
        // ----------------------------------------------------------------------
        protected void DoWorklist( Worklist worklist)
        {
            IList< ActivePlateFactory> active_plate_factories = new List< ActivePlateFactory>();
            active_plate_factories.Add( new ActiveSourcePlateFactory( worklist));
            active_plate_factories.Add( new ActiveDestinationPlateFactory( worklist));
            // active_plate_factories.Add( new ActiveTipboxFactory( worklist));

            while(( active_plate_factories.Sum( apf => apf.NumberOfPlatesToCreate) > 0) ||
                  ( ActivePlate.ActivePlates.Count > 0))
            {
                Thread.Sleep( 50);

                // DKM 2012-01-28 this is the region where ActivePlate.ActivePlates gets modified, so protect with
                //                a lock so GetStatus() is safe.
                lock( ActivePlate.ActivePlates) {
                    // remove finished active plates:
                    IList< ActivePlate> finished_plates = ActivePlate.ActivePlates.Where( ap => ap.IsFinished()).ToList();
                    foreach( ActivePlate finished_plate in finished_plates){
                        Log.InfoFormat( "Removed {0}", finished_plate);
                    }
                    ActivePlate.ActivePlates = ActivePlate.ActivePlates.Except( finished_plates).ToList();

                    // release active plates as necessary:
                    foreach( ActivePlateFactory active_plate_factory in active_plate_factories){
                        ActivePlate active_plate = active_plate_factory.TryReleaseActivePlate();
                        if( active_plate != null){
                            ActivePlate.ActivePlates.Add( active_plate);
                            // DKM 2011-11-03 refs #544: now associate a destination plate with a worklist so that we can
                            //                display the worklist(s) associated with a particular cart.  This doesn't
                            //                work because the plate barcode isn't known at this point!!!
                            if( active_plate is ActiveDestinationPlate) {
                                lock( DestinationWorklistMap) {
                                    DestinationWorklistMap[active_plate] = worklist.Name;
                                }
                            }
                        }
                    }
                }

                // advance active plates as necessary:
                foreach( ActivePlate active_plate in ActivePlate.ActivePlates){
                    if( ActivePlate.ActivePlates.Count( plate => plate != active_plate && plate.GetCurrentToDo() == active_plate.GetCurrentToDo() && plate.InstanceIndex < active_plate.InstanceIndex) > 0){
                        continue;
                    }
                    if( active_plate.Busy){
                        continue;
                    }
                    PlateTask current_to_do = active_plate.GetCurrentToDo();
                    if( current_to_do == null){
                        continue;
                    }
                    // insert criteria on whether or not to do task here.
                    // TRUE.
                    // find an available location for the task.
                    string device_name = current_to_do.DeviceType;
                    // START INSERTING REAL DEVICE MANAGER INTO PLAY HERE.

                    IDictionary< string, DeviceInterface> devices_of_type = ( from kvp in DeviceManager.DevicePluginsAvailable
                                                                              where kvp.Value.ProductName == device_name
                                                                              select kvp).ToDictionary( x => x.Key, x => x.Value);
                    if( devices_of_type.Count == 0){
                        throw new Exception( "The device type needed is not available in the device manager.");
                    }
                    bool device_scheduled = false;
                    bool stacker_occupied = false;
                    foreach( KeyValuePair< string, DeviceInterface> kvp in devices_of_type){
                        PlateSchedulerDeviceInterface device = kvp.Value as PlateSchedulerDeviceInterface;
                        if( device == null){
                            throw new Exception( "The device type needed is not plate scheduler compliant.");
                        }
                        if(( device is StackerInterface) && ( device as AccessibleDeviceInterface).PlateLocationInfo.First().Occupied.WaitOne( 0)){
                            stacker_occupied = true;
                        }
                        PlateLocation location = device.GetAvailableLocation( active_plate);
                        if( location == null){
                            continue;
                        }
                        if( !device.ReserveLocation( location, active_plate)){
                            continue;
                        }
                        Log.DebugFormat( "Available location {0}.{1} reserved for {2}", device, location, active_plate);

                        // commit active plate to a job:
                        active_plate.PlateIsFree.Reset();
                        if( active_plate.CurrentLocation == null){
                            // we're sourcing the plate for the first time.
                            // no robot move needed.
                            Log.DebugFormat( "Sourcing plate {0} at device {1}", active_plate, device);
                            active_plate.CurrentLocation = location;
                            active_plate.DestinationLocation = location;
                        } else{
                            // we're moving the plate from one location to another to perform the task at hand.
                            Log.DebugFormat( "Queueing up robot job to move plate {0} to device {1}", active_plate, device);
                            active_plate.DestinationLocation = location;
                            Debug.Assert( !active_plate.DestinationLocation.Occupied.WaitOne( 0));
                            RobotScheduler.AddJob( active_plate);
                        }
                        Log.DebugFormat( "Queueing up device job to execute {0} on plate {1}", active_plate.GetCurrentToDo().Command, active_plate);
                        device.AddJob( active_plate);
                        device_scheduled = true;
                        break;
                    }

                    // DKM 2011-10-13 we have a potential issue if the racks aren't loaded.  I was planning on calling LoadStack()
                    //                before each call to Upstack() and Downstack(), but the problem is that these methods will
                    //                never get called if the rack isn't loaded!  So we need to warn the user if we fall through
                    //                the previous loop and don't schedule a job.
                    if( !device_scheduled && !stacker_occupied && devices_of_type.First().Value is StackerInterface){
                        ErrorData error_data = new ErrorData( "No destination plates available in stackers -- please ensure that they are loaded properly", new List< string>{ "Retry"});
                        List< ManualResetEvent> events = new List< ManualResetEvent>();
                        events.AddRange( error_data.EventArray);
                        ErrorInterface.AddError( error_data);
                        int event_index = WaitHandle.WaitAny( events.ToArray());
                        if( error_data.TriggeredEvent == "Retry"){
                        } else{
                        }
                    }
                }
            }
            Log.InfoFormat( "Worklist completed");
            worklist.OnWorklistComplete();
        }

        /// <summary>
        /// The PlateScheduler owns the RobotScheduler, so GetStatus needs to call RobotScheduler's GetStatus method
        /// </summary>
        /// <returns></returns>
        public string GetStatus()
        {
            StringBuilder sb = new StringBuilder();

            // robot scheduler status
            sb.AppendLine( RobotScheduler.GetStatus());

            // plate scheduler status includes WorklistQueue and ActivePlates
            sb.AppendLine( "\tActive plates:");
            lock( ActivePlate.ActivePlates) {
                foreach( ActivePlate plate in ActivePlate.ActivePlates) {
                    sb.AppendLine( plate.GetStatus());
                }
            }

            return sb.ToString();
        }
    }
}
