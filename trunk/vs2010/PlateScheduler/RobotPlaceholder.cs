using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.Location;
using BioNex.Shared.PlateWork;
using BioNex.SynapsisPrototype;
using Facet.Combinatorics;
using log4net;
using PathFinding;
using BioNex.Shared.Utils;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.PlateDefs;

namespace BioNex.PlateScheduler
{
    public class RobotScheduler
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        protected DeviceManager DeviceManager { get; set; }
        public PathPlanner PathPlanner { get; set; }
        protected ConcurrentQueue< ActivePlate> PendingJobs { get; set; }
        protected Thread RobotSchedulerThread { get; set; }
        protected ManualResetEvent RobotSchedulerThreadStopEvent { get; set; }

        // DKM 2011-10-07 hey i'm using a static for this
        private static HashSet<int> DestPlatesAlreadyReassigned = new HashSet<int>();

        public event EventHandler EnteringMovePlate;
        public event EventHandler ExitingMovePlate;

        // ----------------------------------------------------------------------
        // members.
        // ----------------------------------------------------------------------
        protected static readonly ILog Log = LogManager.GetLogger( typeof( RobotScheduler));

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public RobotScheduler( DeviceManager device_manager)
        {
            DeviceManager = device_manager;
            PathPlanner = new PathPlanner( device_manager);
            PendingJobs = new ConcurrentQueue< ActivePlate>();
            RobotSchedulerThreadStopEvent = new ManualResetEvent( false);
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public void StartScheduler()
        {
            PathPlanner.CreateWorld();
            RobotSchedulerThread = new Thread( () => RobotSchedulerThreadRunner()){ Name = GetType().ToString(), IsBackground = true};
            RobotSchedulerThread.Start();
        }
        // ----------------------------------------------------------------------
        public void StopScheduler()
        {
            RobotSchedulerThreadStopEvent.Set();
            if( RobotSchedulerThread == null){
                RobotSchedulerThread.Join();
            }
        }
        // ----------------------------------------------------------------------
        public void AddJob( ActivePlate active_plate)
        {
            PendingJobs.Enqueue( active_plate);
        }
        // ----------------------------------------------------------------------
        protected void RobotSchedulerThreadRunner()
        {
            IEnumerable< RobotInterface> robot_interfaces = DeviceManager.GetRobotInterfaces();

            while( !RobotSchedulerThreadStopEvent.WaitOne( 0)){
                Thread.Sleep( 100);
                ActivePlate active_plate;
                PendingJobs.TryDequeue( out active_plate);
                if( active_plate != null){
                    PathPlanner.CreateWorld();
                    // move_plate
                    Log.DebugFormat( "Need to move plate {0} from {1} to {2}",
                        active_plate, 
                        active_plate.CurrentLocation,
                        active_plate.DestinationLocation);
                    Debug.Assert( active_plate.CurrentLocation != active_plate.DestinationLocation, "Why are we moving the plate to where the plate already is?");
                    IList< Node< PlatePlace>> path = PathPlanner.PlanPath( active_plate.CurrentLocation, active_plate.DestinationLocation);
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
                            PlateLocation current_location = PathPlanner.WorldPlaces[ current_place];
                            AccessibleDeviceInterface current_device = PathPlanner.WorldLocations[ current_location];
                            PlatePlace next_place = next_node.key;
                            PlateLocation next_location = PathPlanner.WorldPlaces[ next_place];
                            AccessibleDeviceInterface next_device = PathPlanner.WorldLocations[ next_location];
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
                                bool active_plate_already_reassigned = DestPlatesAlreadyReassigned.Contains(active_plate.Plate.GetHashCode());
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
    }
}
