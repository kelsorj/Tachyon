using System;
using System.Collections.Generic;
using System.Linq;

/*
Location to Location pathing, where locations can be represented as a 
graph connected by robots.

I.E. motion always goes from device location to robot location to device 
location or robot location to robot location if both devices are robots (or the 
same robot).

input :  connected graph with weighted connections between vertices.

Weight is 0, 1, or INFINITY
Where:
   0 represents a required move from this position (e.g. plate 
rotation)  -- better not have two zero weighted connections from a location
   1 represents a standard move from this position -- if two equal 
weighted connections are available, the first one gets picked.
   INFINITY represents a move to an occupied location

INFINITY weight is equivalent to no connection, but can be used if you want
     to keep a fixed graph structure and just adjust the weights,
     which could save some time in larger systems.  Otherwise, just re-build the
     graph and use only 0 or 1 for wieghts.

pseudocode for graph creation from robot / location / teachpoints:

// I'm assuming something like this:
list<Device> devices;
struct Device
{
    string name
    list<Location> locations
}
struct Location
{
   string name
   list<Teachpoint> teachpoints
}
struct Teachpoint
{
   string name
   Device robot
   Location robot_location
}

So we have N teachpoints per location and N locations per device.  Some 
devices are robots, and robots use a location to move plates (e.g. 
gripper or plate pad)

map<mapping, Node> all_nodes;
foreach (device in devices)
   foreach (location in device.locations)
     string mapping = device.name + location.name
     Node node = new Node(mapping) // i.e. establish node to device 
reference
     all_nodes[mapping] = node

foreach (device in devices)
   foreach (location in device.locations)
     string device_mapping = device.name + location.name
     Node node = all_nodes[device_mapping]

     foreach (teachpoint in location.teachpoints)
       string robot_mapping = teachpoint.robot.name + 
teachpoint.robot_location.name
       Node robot_node = all_nodes.all_nodes[robot_mapping]
       Connection connection = new Connection(){node = robot_node, 
weight = 1}
       node.connections.push(connection)
       robot_node.push(connection)


An alternative data layout is to assume all graph nodes are connected, and
have have the connection weight be a function that takes two nodes 
(current_node, test_node)

The advantage is that there's no data preparation necessary (no need to 
come up with a connection structure).
The disadvantage is that you end up with a lot more node tests (but in 
our system size this probably isn't an issue).

I looked into A* as well to see if this could be improved, short answer 
is that A* doesn't really apply in our case, since all moves cost the same.
A* would have an advantage if there was some inequality in our pathing 
(like going up in Z was more expensive than down or something) so we 
could write a path weight heuristic.

*/


namespace PathFinding
{
    /// <summary>
    /// Node represents a vertex in our connection graph
    ///     -- key is User defined data e.g. a teachpoint identifier for this node
    ///     -- connection is a user provided list of path's leaving this node
    /// </summary>
    /// <typeparam name="KeyType"></typeparam>
    public class Node<KeyType>
    {
        public KeyType key;             
        public List<Connection<KeyType>> connections = new List<Connection<KeyType>>();  // connection is a user provided list of path's leaving this node

        public double distance;                    // Algorthim assigns distance
        public Node<KeyType> previous = null;   // Algorithm assigns this so that we can generate a path
    }

    /// <summary>
    /// Connection represents a directed edge in our connection graph
    ///     -- node is the node that this connection points towards
    ///     -- cost is the distance between the two nodes (we will probably only use cost of 1)
    /// </summary>
    /// <typeparam name="KeyType"></typeparam>
    public class Connection<KeyType>
    {
        public Node<KeyType> node;
        public double cost;
        public Object extra_info = null;
    }

    /// <summary>
    /// Stateless Djikstra class -- can be made a singleton
    /// </summary>
    /// <typeparam name="KeyType"></typeparam>
    public class Djikstra<KeyType>
    {
        const double INFINITY = double.PositiveInfinity;

        /// <summary>
        /// FindShortestPath:
        ///     Assumes input of a connected graph contained in the "start" node.  
        ///     Runs Djikstra algo until shortest path to "end" node is found (which must refer to a node in the graph)
        ///     If a path is found, it returns an ordered list of all nodes in the path
        ///     If no path is found, it returns an empty list
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public List<Node<KeyType>> FindShortestPath(Node<KeyType> start, Node<KeyType> end)
        {
            #region test arguments
            if (start == null)
                throw new ArgumentNullException("1st argument 'start' cannot be null, this must be a reference to the node from which pathing begins");
            if (end == null)
                throw new ArgumentNullException("2nd argument 'end' cannot be null, this must be a reference to the node on which pathing will finish");
            #endregion
            

            // First, the trivial case
            if(start == end)
            {
                var short_path = new List<Node<KeyType>>(); 
                short_path.Add(end);
                return short_path;
            }

            // Prepare / reset -- generate world recursively from nodes connected to start
            var world = new List<Node<KeyType>>();
            GenerateWorld(start, world);

            // if we didn't find the end node in the connected graph, we're not going to be able to find a path to it, so exit early with empty path
            if (!world.Contains(end))
            {
                return new List<Node<KeyType>>(); // return no path in this case
            }

            // reset distance of all nodes in the world
            foreach (var node in world)
            {
                node.distance = INFINITY;
                node.previous = null;
            }

            // start node distance is special -- it's the closest node to where we start!
            start.distance = 0;
           
            // + 
            // scan the graph for shortest path
            // +

            // Loop terminates 
            //   - when we run out of nodes to process (current == null)
            //   - when we reach the "end" node.  Since we follow the shortest branches first, this will be the shortest path
            Node<KeyType> current = start;
            while(current != null && current != end)
            {
                foreach(var connection in current.connections)
                {
                    var test_node = connection.node;

                    // "Relax"

                    // distance to the test_node is the distance to the node it's connected from (current.node) 
                    // plus its connection weight
                    // if the calculated distance is less than any distance we've already assigned it 
                    // (from a previous test, or INFINITY to start)
                    // then we save the calculated distance in the node 
                    // -- this is the shortest distance to that node from our start point found so far
                    double distance = current.distance + connection.cost;
                    if(distance < test_node.distance)
                    {
                        test_node.distance = distance;
                        test_node.previous = current;  // save a path backwards to current node
                    }

                }

                // remove the current node from the list of all nodes, so we don't test cycles
                world.Remove(current);

                current = world.Aggregate((closest, test_node) => (closest == null || test_node.distance < closest.distance ? test_node : closest));
                if (current.distance == INFINITY)
                    current = null; // equivalent to "break;" but I wanted exit conditions to be documented in while() test
            }

            // return list from start to end, including start
            var path = new List<Node<KeyType>>();
            Node<KeyType> next_node = current;// end; --> point to current not end, otherwise we would stick end in results even when we didn't find a path
            while (next_node != null) // && next_node != start)    // ---> Uncomment to exclude start node from path
            {
                path.Insert(0, next_node);
                next_node = next_node.previous;
            }
            
            return path;    // returns empty list, or list of nodes from start to end,
                            // including start end end.  You can either run this once per path
                            // and reserve the entire path (safest),
                            // or reserve the first node in the path, and run the algorithm
                            // again after every move (in which case you'll need to shuffle plates around if you reach a deadlock)
        }

        private static void GenerateWorld(Node<KeyType> start, ICollection<Node<KeyType>> world)
        {
            world.Add(start);
            foreach (var connection in start.connections)
                if (!world.Contains(connection.node))
                {
                    if (connection.cost < 0)
                        throw new ArgumentException(string.Format("Connection from {0} to {1} has negative weight", start.key.ToString(), connection.node.key.ToString()));
                    GenerateWorld(connection.node, world);
                }
        }
    }
}
