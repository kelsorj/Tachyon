using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.SynapsisPrototype;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.Location;
using Facet.Combinatorics;
using System.Diagnostics;
using log4net;
using PathFinding;

namespace BioNex.PlateScheduler
{
    public class PathPlanner
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        protected DeviceManager DeviceManager { get; set; }
        protected IEnumerable< AccessibleDeviceInterface> AccessibleDevices { get; set; }
        protected IEnumerable< RobotInterface> Robots { get; set; }
        public IDictionary< PlateLocation, AccessibleDeviceInterface> WorldLocations { get; protected set; }
        public IDictionary< PlatePlace, PlateLocation> WorldPlaces { get; protected set; }

        protected IDictionary< PlatePlace, Node< PlatePlace>> WorldNodes { get; set; }

        // ----------------------------------------------------------------------
        // members.
        // ----------------------------------------------------------------------
        protected static readonly ILog Log = LogManager.GetLogger( typeof( PathPlanner));

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public PathPlanner( DeviceManager device_manager)
        {
            DeviceManager = device_manager;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public void CreateWorld()
        {
            AccessibleDevices = DeviceManager.GetAccessibleDeviceInterfaces();
            Robots = DeviceManager.GetRobotInterfaces();
            WorldLocations = AccessibleDevices.SelectMany( device => device.PlateLocationInfo.Select( location => new{ Device = device, Location = location})).ToDictionary( kvp => kvp.Location, kvp => kvp.Device);
            WorldPlaces = WorldLocations.Keys.SelectMany( location => location.Places.Select( place => new{ Location = location, Place = place})).ToDictionary( kvp => kvp.Place, kvp => kvp.Location);

            IEnumerable< PlatePlace> all_places = AccessibleDevices.SelectMany( device => device.PlateLocationInfo).SelectMany( location => location.Places);
            WorldNodes = all_places.ToDictionary( place => place, place => new Node< PlatePlace>{ key = place });
            Combinations< PlatePlace> all_place_combinations = new Combinations< PlatePlace>( all_places.ToList(), 2);
            foreach( IList< PlatePlace> edge in all_place_combinations){
                Debug.Assert( edge.Count == 2);
                foreach( RobotInterface robot in Robots){
                    PlatePlace dst = null;
                    try
                    {
                        PlatePlace src = edge[0];
                        dst = edge[1];
                        PlateLocation src_location = WorldPlaces[src];
                        PlateLocation dst_location = WorldPlaces[dst];
                        AccessibleDeviceInterface src_device = WorldLocations[src_location];
                        AccessibleDeviceInterface dst_device = WorldLocations[dst_location];

                        double weight = robot.GetTransferWeight(src_device,
                                                                 src_location,
                                                                 src,
                                                                 dst_device,
                                                                 dst_location,
                                                                 dst);
                        if ((weight > 0) && (!double.IsPositiveInfinity(weight)))
                        {
                            WorldNodes[src].connections.Add(new Connection<PlatePlace>{ node = WorldNodes[dst], cost = weight, extra_info = robot });
                            WorldNodes[dst].connections.Add(new Connection<PlatePlace>{ node = WorldNodes[src], cost = weight, extra_info = robot });
                        }
                    }
                    catch (Exception)
                    {
                        Log.DebugFormat( "No teachpoint named '{0}' exists", dst == null ? "null" : dst.ToString());
                    }
                }
            }
            CheckNodes();
        }
        // ----------------------------------------------------------------------
        public void CheckNodes()
        {
            IEnumerable< PlatePlace> disconnected_places = from node in WorldNodes
                                                           where node.Value.connections.Count == 0
                                                           select node.Key;
            if( disconnected_places.Count() > 0){
                Log.WarnFormat( "The following places are disconnected from the world, are you sure they have teachpoints: {0}", String.Join( ", ", disconnected_places));
            }
        }
        // ----------------------------------------------------------------------
        public IList< Node< PlatePlace>> PlanPath( PlateLocation src, PlateLocation dst)
        {
            Djikstra< PlatePlace> finder = new Djikstra< PlatePlace>();
            /*
            var x = ( from src_place in src.Places
                      from dst_place in dst.Places
                      select new{ SrcPlace = src_place, DstPlace = dst_place, Distance = finder.FindShortestPath( WorldNodes[ src_place], WorldNodes[ dst_place]).Last().distance }).Aggregate(( closest, iter) => ( closest == null || ( iter.Distance < closest.Distance) ? iter : closest));
                      /*
            Node< PlatePlace> src_node = WorldNodes[ src];
            Node< PlatePlace> dst_node = WorldNodes[ dst];
            List< Node< PlatePlace>> path = finder.FindShortestPath( src_node, dst_node);
                       */
            double shortest_distance = double.PositiveInfinity;
            List< Node< PlatePlace>> shortest_path = null;
            foreach( PlatePlace src_place in src.Places){
                foreach( PlatePlace dst_place in dst.Places){
                    if( !WorldNodes.ContainsKey( src_place)){
                        // Debug.Assert( false, "should only occur during testing");
                        continue;
                    }
                    if( !WorldNodes.ContainsKey( dst_place)){
                        // Debug.Assert( false, "should only occur during testing");
                        continue;
                    }
                    List< Node< PlatePlace>> path = finder.FindShortestPath( WorldNodes[ src_place], WorldNodes[ dst_place]);
                    if( path == null){
                        continue;
                    }
                    if( path.Count == 0){
                        continue;
                    }
                    double distance = path.Last().distance;
                    if( distance < shortest_distance){
                        shortest_distance = distance;
                        shortest_path = path;
                    }
                }
            }
            return shortest_path;
        }
    }
}
