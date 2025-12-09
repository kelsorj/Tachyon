using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace BioNex.AmgenProtocolXmlParser
{
    public class AmgenParser
    {
        public static readonly string RootName = "protocol";
        public static readonly string PlateAttributeName = "plate";

        public string LabwareName { get; private set; }
        public IList<Tuple<string,Dictionary<string,object>>> Tasks { get; private set; }

        public enum ProtocolCommands { Aspirate, Dispense, Transfer, Mix, Prime, LoadSyringe };

        public void LoadProtocolFile( string path)
        {
            XDocument doc = XDocument.Load( path);
            // check the root element
            XElement root = doc.Root;
            if( root.Name != RootName)
                throw new Exception( String.Format( "The root element should be named '{0}', but it was '{1}'", RootName, root.Name));
            if( root.Attributes( PlateAttributeName).Count() != 1)
                throw new Exception( String.Format( "The root element '{0}' must define the required attribute '{1}' once", RootName, PlateAttributeName));
            LabwareName = root.Attribute( PlateAttributeName).Value;
            // now parse the descendants, which each represent some kind of task
            var task_elements = root.Elements();
            Tasks = new List<Tuple<string,Dictionary<string,object>>>();
            foreach( XElement task in task_elements) {
                string task_name = task.Name.ToString();
                var property_elements = task.Elements();
                Dictionary<string,object> properties = new Dictionary<string,object>();
                foreach( XElement pe in property_elements) {
                    properties.Add( pe.Name.ToString(), pe.Value);
                }

                Tasks.Add( new Tuple<string,Dictionary<string,object>>( task_name, properties));
            }
        }
    }
}
