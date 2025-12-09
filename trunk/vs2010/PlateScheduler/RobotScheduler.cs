using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Location;
using BioNex.Shared.PlateWork;
using BioNex.Shared.Utils;
using BioNex.SynapsisPrototype;
using log4net;
using PathFinding;
using System.Text;

namespace BioNex.PlateScheduler
{
    [ PartCreationPolicy( CreationPolicy.Shared)]
    [ Export( typeof( IRobotScheduler))]
    [Export(typeof(IReportsStatus))]
    public class RobotScheduler : IRobotScheduler
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        protected DeviceManager DeviceManager { get; set; }

        protected ConcurrentQueue< ActivePlate> PendingJobs { get; set; }
        protected Thread RobotSchedulerThread { get; set; }
        protected ManualResetEvent RobotSchedulerThreadStopEvent { get; set; }

        // DKM 2011-10-07 hey i'm using a static for this
        private static readonly HashSet<int> DestPlatesAlreadyReassigned = new HashSet<int>();

        public event EventHandler EnteringMovePlate;
        public event EventHandler ExitingMovePlate;

        // ----------------------------------------------------------------------
        // members.
        // ----------------------------------------------------------------------
        protected static readonly ILog Log = LogManager.GetLogger( typeof( RobotScheduler));

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        [ ImportingConstructor]
        public RobotScheduler([ Import] DeviceManager device_manager)
        {
            DeviceManager = device_manager;
            PendingJobs = new ConcurrentQueue< ActivePlate>();
            RobotSchedulerThreadStopEvent = new ManualResetEvent( false);
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public void StartScheduler()
        {
            RobotSchedulerThread = new Thread( RobotSchedulerThreadRunner){ Name = GetType().ToString(), IsBackground = true};
            RobotSchedulerThread.Start();
        }
        // ----------------------------------------------------------------------
        public void StopScheduler()
        {
            RobotSchedulerThreadStopEvent.Set();
            if( RobotSchedulerThread != null){
                RobotSchedulerThread.Join();
            }
        }
        // ----------------------------------------------------------------------
        public void AddJob( ActivePlate active_plate)
        {
            PendingJobs.Enqueue( active_plate);
        }
        // ----------------------------------------------------------------------
        public void AddJob( string src_device_name, string src_location_name, string dst_device_name, string dst_location_name, string labware_name)
        {
            var pathPlanner = new PathPlanner( DeviceManager);

            pathPlanner.CreateWorld();
            PlateLocation src_location = pathPlanner.WorldLocations.FirstOrDefault( kvp => kvp.Key.Name == src_location_name && kvp.Value.Name == src_device_name).Key;
            PlateLocation dst_location = pathPlanner.WorldLocations.FirstOrDefault( kvp => kvp.Key.Name == dst_location_name && kvp.Value.Name == dst_device_name).Key;
            IList< Node< PlatePlace>> path = pathPlanner.PlanPath( src_location, dst_location);
            if( path == null){
                throw new Exception( "huh?");
            } else{
                // string route = String.Join( ",", from node in path select node.key.ToString());
                // Log.InfoFormat( "Simulated Moving {0} from {1} to {2} via {3}", active_plate, active_plate.CurrentLocation, active_plate.DestinationLocation, route);
                Debug.Assert( path.Count > 1, "path must contain multiple nodes");
                Node< PlatePlace> current_node = path[ 0];
                for( int loop = 1; loop < path.Count; ++loop){
                    Node< PlatePlace> next_node = path[ loop];
                    Object robot_object = ( from connection in path[ loop].connections
                                            where connection.node.key == current_node.key
                                            select connection.extra_info).FirstOrDefault();
                    RobotInterface robot = robot_object as RobotInterface;
                    // perform the move.
                    PlatePlace current_place = current_node.key;
                    PlateLocation current_location = pathPlanner.WorldPlaces[ current_place];
                    AccessibleDeviceInterface current_device = pathPlanner.WorldLocations[ current_location];
                    PlatePlace next_place = next_node.key;
                    PlateLocation next_location = pathPlanner.WorldPlaces[ next_place];
                    AccessibleDeviceInterface next_device = pathPlanner.WorldLocations[ next_location];
                    if( current_location != next_location){
                        ( current_device as PlateSchedulerDeviceInterface).LockPlace( current_place);
                        ( next_device as PlateSchedulerDeviceInterface).LockPlace( next_place);
                        if( EnteringMovePlate != null){
                            EnteringMovePlate( this, null);
                        }
                        robot.TransferPlate( current_device.Name, current_place.Name, next_device.Name, next_place.Name, labware_name, new MutableString());
                        if( ExitingMovePlate != null){
                            ExitingMovePlate( this, null);
                        }
                    }
                    current_node = next_node;
                }
            }
            // active_plate.CurrentLocation.Occupied.Reset();
            // active_plate.DestinationLocation.Occupied.Set();
        }
        // ----------------------------------------------------------------------
        protected void RobotSchedulerThreadRunner()
        {
            var pathPlanner = new PathPlanner( DeviceManager);

            while( !RobotSchedulerThreadStopEvent.WaitOne( 0)){
                Thread.Sleep( 100);
                ActivePlate active_plate;
                PendingJobs.TryDequeue( out active_plate);
                if( active_plate != null){
                    pathPlanner.CreateWorld();
                    // move_plate
                    Log.DebugFormat( "Need to move plate {0} from {1} to {2}",
                        active_plate, 
                        active_plate.CurrentLocation,
                        active_plate.DestinationLocation);
                    Debug.Assert( active_plate.CurrentLocation != active_plate.DestinationLocation, "Why are we moving the plate to where the plate already is?");
                    IList< Node< PlatePlace>> path = pathPlanner.PlanPath( active_plate.CurrentLocation, active_plate.DestinationLocation);
                    if( path == null){
                        Log.InfoFormat( "PLACEHOLDER Moving {0} from {1} to {2}",
                        active_plate, 
                        active_plate.CurrentLocation,
                        active_plate.DestinationLocation);
                    } else{
                        // string route = String.Join( ",", from node in path select node.key.ToString());
                        // Log.InfoFormat( "Simulated Moving {0} from {1} to {2} via {3}", active_plate, active_plate.CurrentLocation, active_plate.DestinationLocation, route);
                        Debug.Assert( path.Count > 1, "path must contain multiple nodes");
                        Node< PlatePlace> current_node = path[ 0];
                        for( int loop = 1; loop < path.Count; ++loop){
                            Node< PlatePlace> next_node = path[ loop];
                            Object robot_object = ( from connection in path[ loop].connections
                                                    where connection.node.key == current_node.key
                                                    select connection.extra_info).FirstOrDefault();
                            RobotInterface robot = robot_object as RobotInterface;
                            // perform the move.
                            PlatePlace current_place = current_node.key;
                            PlateLocation current_location = pathPlanner.WorldPlaces[ current_place];
                            AccessibleDeviceInterface current_device = pathPlanner.WorldLocations[ current_location];
                            PlatePlace next_place = next_node.key;
                            PlateLocation next_location = pathPlanner.WorldPlaces[ next_place];
                            AccessibleDeviceInterface next_device = pathPlanner.WorldLocations[ next_location];
                            if( current_location != next_location){
                                ( current_device as PlateSchedulerDeviceInterface).LockPlace( current_place);
                                ( next_device as PlateSchedulerDeviceInterface).LockPlace( next_place);
                                // Log.InfoFormat( "Simulated move plate {0} from {1}.{2}.{3} to {4}.{5}.{6} via {7}", active_plate, current_device, current_location, current_place, next_device, next_location, next_place, robot_object);
                                // DKM 2011-10-05 Here we probably need to check the ActivePlate to see if it's an ActiveDestinationPlate.  If it is,
                                //                we need to allow the barcode to change when the robot picks it.
                                //                e.g.
                                // DKM 2011-10-07 we also need to make sure that we only force-strobe the destination plate once
                                //                otherwise, we risk allowing the system to re-assign the barcode to a new value.
                                //                in one case, we actually assigned it to Constants.Strobe, which is obviously bad.
                                // check the map to see if we've strobed this plate before
                                bool active_plate_is_dest = active_plate is ActiveDestinationPlate;
                                bool active_plate_already_reassigned = DestPlatesAlreadyReassigned.Contains(active_plate.Plate.GetHashCode()); // prevents plate from being strobed twice
                                //! \TODO DKM 2012-01-18 this is a problem because unbarcoded dest plates is a customer-specific property!
                                string plate_barcode = (!active_plate_is_dest || active_plate_already_reassigned) ? active_plate.Barcode : Constants.Strobe;
                                if( active_plate_is_dest && !active_plate_already_reassigned)
                                    DestPlatesAlreadyReassigned.Add( active_plate.Plate.GetHashCode());
                                active_plate.Plate.Barcode.Value = plate_barcode; // kind of a waste of an op for source plates, but ok for now...
                                if( EnteringMovePlate != null){
                                    EnteringMovePlate( this, null);
                                }
                                robot.TransferPlate(current_device.Name, current_place.Name, next_device.Name, next_place.Name, active_plate.LabwareName, active_plate.Plate.Barcode);
                                if( ExitingMovePlate != null){
                                    ExitingMovePlate( this, null);
                                }
                            }
                            current_node = next_node;
                        }
                    }
                    // active plate's current location is now free.
                    active_plate.CurrentLocation.Occupied.Reset();
                    active_plate.DestinationLocation.Occupied.Set();
                    // active plate's new current location is the destination location.
                }
            }
        }

        /// <summary>
        /// RobotScheduler just has to report pending jobs
        /// </summary>
        /// <returns></returns>
        public string GetStatus()
        {
            StringBuilder sb = new StringBuilder();

            if( PendingJobs.Count() == 0) {
                return "RobotScheduler has no pending jobs";
            }

            foreach( ActivePlate plate in PendingJobs) {
                sb.AppendLine( plate.GetStatus());
            }

            return sb.ToString();            
        }
    }
}
