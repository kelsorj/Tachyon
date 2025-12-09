using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;

namespace BioNex.Shared.TaskListXMLParser
{
    public class PlateTask
    {
        public class Parameter
        {
            public string Name { get; private set; }
            public string Value { get; private set; }
            public string Variable { get; private set; }

            public Parameter( string name, string value, string variable)
            {
                Name = name;
                Value = value;
                Variable = variable; 
            }
        }

        public string DeviceType { get; private set; }
        // if a device only has one command, this parameter isn't necessary.  But maybe this is dangerous,
        // since the plugin implementation could change between versions???
        public string Command { get; private set; }
        public IList<Parameter> ParametersAndVariables { get; set; }
        public bool Completed { get; set; }

        public PlateTask( string device_type, string command)
        {
            DeviceType = device_type;
            Command = command;
            ParametersAndVariables = new List< Parameter>();
        }

        public PlateTask( XElement node)
        {
            DeviceType = node.Element( "device_type").Value;
            // for now, command name is optional
            Command = node.Element( "command").Value;
            ParametersAndVariables = new List<Parameter>();
            var parameters_node = node.Element( "parameters");
            if( parameters_node != null){
                var parameters = parameters_node.Elements( "parameter");
                foreach( var x in parameters) {
                    string parameter_name = x.Attribute( "name").Value;
                    string parameter_value = x.Attribute( "value").Value;
                    // parameter_variable is optional, so need to see what happens with parsing when it's not there
                    var parameter_variable = x.Attributes( "variable").FirstOrDefault();
                    ParametersAndVariables.Add( new Parameter( parameter_name, parameter_value, parameter_variable != null ? parameter_variable.Value : ""));
                }
            }
        }

        public Parameter this[string name]
        {
            get { return (from x in ParametersAndVariables where x.Name == name select x).First(); }
            private set {}
        }
    }

    /// <summary>
    /// Takes an XmlNode from the HitpickXMLReader (or perhaps some other assembly) and
    /// parses it after treating it as an XElement.  Most likely as other things start to
    /// use this parser, we'll need to overload the Parse function to take other XML classes.
    /// </summary>
    public class TaskListParser
    {
        public class DefaultTaskLists
        {
            public IList<PlateTask> source_prehitpick_tasks { get; private set; }
            public IList<PlateTask> source_posthitpick_tasks { get; private set; }
            public IList<PlateTask> dest_prehitpick_tasks { get; private set; }
            public IList<PlateTask> dest_posthitpick_tasks { get; private set; }

            public DefaultTaskLists()
            {
                source_prehitpick_tasks = new List<PlateTask>();
                source_posthitpick_tasks = new List<PlateTask>();
                dest_prehitpick_tasks = new List<PlateTask>();
                dest_posthitpick_tasks = new List<PlateTask>();
            }

            public DefaultTaskLists( IList<PlateTask> source_pre, IList<PlateTask> source_post, IList<PlateTask> dest_pre, IList<PlateTask> dest_post)
            {
                source_prehitpick_tasks = source_pre;
                source_posthitpick_tasks = source_post;
                dest_prehitpick_tasks = dest_pre;
                dest_posthitpick_tasks = dest_post;
            }
        }

        static public DefaultTaskLists ParseDefaultTaskLists( XmlNode node)
        {
            // instructions on how to go from XmlNode to XElement or vice versa: http://blogs.msdn.com/b/ericwhite/archive/2008/12/22/convert-xelement-to-xmlnode-and-convert-xmlnode-to-xelement.aspx
            XDocument doc = new XDocument();
            using( XmlWriter writer = doc.CreateWriter())
                node.WriteTo( writer);
            XElement root = doc.Root;
            return ParseDefaultTaskLists( root);
        }

        static public DefaultTaskLists ParseDefaultTaskLists( XElement root)
        {
            Debug.Assert( root.Name == "tasks");

            // find our way to the appropriate nodes
            var source_plate = root.Element( "source_plate_tasks");
            var source_prehitpick = source_plate.Element( "prehitpick_tasks");
            var source_posthitpick = source_plate.Element( "posthitpick_tasks");
            var dest_plate = root.Element( "destination_plate_tasks");
            var dest_prehitpick = dest_plate.Element( "prehitpick_tasks");
            var dest_posthitpick = dest_plate.Element( "posthitpick_tasks");

            // now we can parse the pre and post hitpick task lists
            IList<PlateTask> source_prehitpick_tasks = ParseTaskList( source_prehitpick);
            IList<PlateTask> source_posthitpick_tasks = ParseTaskList( source_posthitpick);
            IList<PlateTask> dest_prehitpick_tasks = ParseTaskList( dest_prehitpick);
            IList<PlateTask> dest_posthitpick_tasks = ParseTaskList( dest_posthitpick);
            return new DefaultTaskLists( source_prehitpick_tasks, source_posthitpick_tasks, dest_prehitpick_tasks, dest_posthitpick_tasks);
        }

        /// <remarks>
        /// returns an empty list if the respective node is either empty or missing
        /// </remarks>
        /// <param name="source_dest_prepost_hitpick"></param>
        /// <returns></returns>
        static public IList<PlateTask> ParseTaskList( XElement source_dest_prepost_hitpick)
        {
            if( source_dest_prepost_hitpick == null)
                return new List<PlateTask>();
            var tasks = from x in source_dest_prepost_hitpick.Elements( "task") select x;
            List<PlateTask> tasklist = new List<PlateTask>();
            foreach( var x in tasks)
                tasklist.Add( new PlateTask( x));
            return tasklist;
        }
    }
}
