using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using log4net;

namespace BioNex.Shared.Teachpoints
{
    public class TeachpointNotFoundException : ApplicationException
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public override string Message { get { return String.Format( "Teachpoint '{0}' not found in teachpoint file", TeachpointName); }}
        private string TeachpointName { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public TeachpointNotFoundException( string teachpoint_name)
        {
            TeachpointName = teachpoint_name;
        }

    }

    public class GenericTeachpoint : IXmlSerializable
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public string Name { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        protected GenericTeachpoint()
        {
        }
        // ----------------------------------------------------------------------
        public GenericTeachpoint( String name)
        {
            Name = name;
        }

        // ----------------------------------------------------------------------
        // methods
        // ----------------------------------------------------------------------
        #region IXmlSerializable Implementation
        // ----------------------------------------------------------------------
        public XmlSchema GetSchema()
        {
            return null;
        }
        // ----------------------------------------------------------------------
        public void ReadXml( XmlReader reader)
        {
            // read off the GenericTeachpoint's Name attribute.
            Name = reader.GetAttribute( "Name");
            // read off the GenericTeachpoint start element.
            reader.ReadStartElement();
            // while the next item is a start element...
            while( reader.IsStartElement()){
                // get the current element's name and use it to find the property info associated with it.
                PropertyInfo property_info = GetType().GetProperty( reader.Name);
                // read off the start of the current element.
                reader.ReadStartElement();
                // convert the current element's content into a property value.
                object property_value = property_info.PropertyType.IsEnum ? Enum.Parse( property_info.PropertyType, reader.ReadContentAsString()) : 
                                        property_info.PropertyType == typeof( System.Boolean) ? Boolean.Parse( reader.ReadContentAsString()) : // <-- Microsoft sucks!!
                                            reader.ReadContentAs( property_info.PropertyType, null);
                // set the property named by the current element's name with the value found in the current element's content.
                property_info.SetValue( this, property_value, null);
                // read off the end of the current element.
                reader.ReadEndElement();
            }
            // read off the GenericTeachpoint end element.
            reader.ReadEndElement();
        }
        // ----------------------------------------------------------------------
        public void WriteXml( XmlWriter writer)
        {
            PropertyInfo[] property_infos = GetType().GetProperties();
            // for the Name property, write out an attribute with the property's name and containing the property's value.
            PropertyInfo name_property = property_infos.FirstOrDefault( property_info => property_info.Name == "Name");
            writer.WriteAttributeString( name_property.Name, name_property.GetValue( this, null).ToString());
            // for each property...
            foreach( PropertyInfo property_info in property_infos){
                // if it's not the Name property, then write out an element named with the property's name and containing the property's value.
                if( property_info.Name != "Name"){
                    writer.WriteStartElement( property_info.Name);
                    writer.WriteValue( property_info.GetValue( this, null).ToString());
                    writer.WriteEndElement();
                }
            }
        }
        // ----------------------------------------------------------------------
        #endregion IXmlSerializable Implementation
    }

    [ XmlRootAttribute( "Teachpoints")]
    public class GenericTeachpointCollection< T> : IXmlSerializable where T : GenericTeachpoint
    {
        private static readonly ILog _log = LogManager.GetLogger( "xml");

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        protected HashSet< T> Teachpoints { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public GenericTeachpointCollection()
        {
            Teachpoints = new HashSet< T>();
        }

        // ----------------------------------------------------------------------
        // methods
        // ----------------------------------------------------------------------
        public static GenericTeachpointCollection< T> LoadTeachpointsFromFile( string teachpoint_filepath)
        {
            try{
                using( FileStream reader = new FileStream( teachpoint_filepath, FileMode.Open)){
                    XmlSerializer serializer = new XmlSerializer( typeof( GenericTeachpointCollection< T>));
                    serializer.UnknownAttribute += new XmlAttributeEventHandler(serializer_UnknownAttribute);
                    serializer.UnknownElement += new XmlElementEventHandler(serializer_UnknownElement);
                    serializer.UnknownNode += new XmlNodeEventHandler(serializer_UnknownNode);
                    serializer.UnreferencedObject += new UnreferencedObjectEventHandler(serializer_UnreferencedObject);
                    GenericTeachpointCollection< T> teachpoints = ( GenericTeachpointCollection< T>)serializer.Deserialize( reader);
                    reader.Close();
                    return teachpoints;
                }
            } catch( FileNotFoundException){
                // if file couldn't be found, that's OK, just create a new collection.
                return new GenericTeachpointCollection< T>();
            }
        }

        static void serializer_UnreferencedObject(object sender, UnreferencedObjectEventArgs e)
        {
            _log.ErrorFormat( "Unreferenced object {0} in XML document", e.UnreferencedObject);
        }

        static void serializer_UnknownNode(object sender, XmlNodeEventArgs e)
        {
            _log.ErrorFormat( "Unknown node {0} in object '{1}'", e.NodeType, e.ObjectBeingDeserialized.ToString());
        }

        static void serializer_UnknownElement(object sender, XmlElementEventArgs e)
        {
            _log.ErrorFormat( "Unknown element {0} in object '{1}'", e.Element, e.ObjectBeingDeserialized.ToString());
        }

        static void serializer_UnknownAttribute(object sender, XmlAttributeEventArgs e)
        {
            _log.ErrorFormat( "Unknown attribute {0} in object '{1}'", e.Attr, e.ObjectBeingDeserialized.ToString());
        }
        // ----------------------------------------------------------------------
        public static void SaveTeachpointsToFile( string teachpoint_filepath, GenericTeachpointCollection< T> teachpoints)
        {
            string backup_teachpoint_filepath = teachpoint_filepath + ".backup";
            if( File.Exists( teachpoint_filepath)){
                try{
                    File.Copy( teachpoint_filepath, backup_teachpoint_filepath, true);
                    File.Delete( teachpoint_filepath);
                } catch( Exception){
                }
            }
            using( FileStream writer = new FileStream( teachpoint_filepath, FileMode.CreateNew)){
                XmlSerializer serializer = new XmlSerializer( typeof( GenericTeachpointCollection< T>));
                serializer.Serialize( writer, teachpoints);
                writer.Close();
            }
        }
        // ----------------------------------------------------------------------
        public IList< string> GetTeachpointNames()
        {
            return Teachpoints.Select( tp => tp.Name).ToList();
        }
        // ----------------------------------------------------------------------
        public T GetTeachpoint( string teachpoint_name)
        {
            T retval = Teachpoints.FirstOrDefault( tp => tp.Name == teachpoint_name);
            if( retval == null){
                throw new TeachpointNotFoundException( teachpoint_name);
            }
            return retval;
        }
        // ----------------------------------------------------------------------
        public void SetTeachpoint( T teachpoint)
        {
            Teachpoints.RemoveWhere( tp => tp.Name == teachpoint.Name);
            Teachpoints.Add( teachpoint);
        }
        // ----------------------------------------------------------------------
        #region IXmlSerializable Implementation
        // ----------------------------------------------------------------------
        public XmlSchema GetSchema()
        {
            return null;
        }
        // ----------------------------------------------------------------------
        public void ReadXml( XmlReader reader)
        {
            // read off the GenericTeachpointCollection (-->Teachpoints) start element.
            reader.ReadStartElement();
            // while the next item is a start element...
            while( reader.IsStartElement()){
                // get the current element's name and use it to create the type of teachpoint associated with it.
                T teachpoint = CreateTeachpoint( reader.Name);
                // populate the teachpoint with values found in the xml.
                teachpoint.ReadXml( reader);
                // add the teachpoint to the teachpoint collection.
                Teachpoints.Add( teachpoint);
            }
            // read off the GenericTeachpointCollection (-->Teachpoints) end element.
            reader.ReadEndElement();
        }
        // ----------------------------------------------------------------------
        public void WriteXml( XmlWriter writer)
        {
            // for each teachpoint in the teachpoint collection...
            foreach( T teachpoint in Teachpoints){
                // write out an element representing the teachpoint.
                writer.WriteStartElement( GenerateElementName( teachpoint));
                teachpoint.WriteXml( writer);
                writer.WriteEndElement();
            }
        }
        // ----------------------------------------------------------------------
        #endregion IXmlSerializable Implementation
        // ----------------------------------------------------------------------
        private static string GenerateElementName( T teachpoint)
        {
            Type type = teachpoint.GetType();
            return string.Format( "{0}-{1}", type.Assembly.GetName().Name, type.ToString());
        }
        // ----------------------------------------------------------------------
        private T CreateTeachpoint( string element_name)
        {
            var pieces = element_name.Split( '-').ToList();
            string assembly_name = string.Join( "-", pieces.GetRange( 0, pieces.Count() - 1));
            string type_name = pieces.Last();
            return ( T)( Assembly.Load( assembly_name).CreateInstance( type_name));
        }
    }

    public class Teachpoint
    {
        public string Name { get; private set; }

        private readonly List< TeachpointItem> _teachpoint_items;
        public virtual string ElementName
        {
            get {
                return "";
            }
        }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public Teachpoint( string name = "")
        {
            Name = name;
            _teachpoint_items = new List< TeachpointItem>();
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public virtual void WriteXML( XmlTextWriter writer)
        {
            writer.WriteStartElement( ElementName);
            foreach( TeachpointItem tpi in TeachpointItems) {
                writer.WriteStartElement( tpi.AxisName);
                writer.WriteValue( tpi.Position);
                writer.WriteEndElement();
            }
        }

        public virtual void ReadXML( XmlNode node)
        {
            // loop over the child nodes to get the axis positions
            foreach( XmlNode n in node.ChildNodes) {
                // here, I convert the old teachpoint schema to the newer one that uses "approach_height" instead of "hover_delta"
                string node_name = n.Name;
                if( node_name == "hover_delta")
                    _teachpoint_items.Add( new TeachpointItem( "approach_height", double.Parse( n.InnerText)));
                else
                    _teachpoint_items.Add( new TeachpointItem( n.Name, double.Parse( n.InnerText)));
            }
        }

        public void SetTeachpointItem( TeachpointItem ti)
        {
            // look for an existing teachpoint
            // if it exists, replace existing value, otherwise add it
            TeachpointItem find_item = _teachpoint_items.Find( i=>i.AxisName==ti.AxisName);
            if( find_item == null)
                _teachpoint_items.Add( ti);
            else
                find_item.Position = ti.Position;
        }

        public double GetPosition( string axis_name)
        {
            TeachpointItem find_item = _teachpoint_items.Find( i=>i.AxisName == axis_name);
            //! \todo should I just throw an exception if the axis name isn't found?
            return find_item.Position;
        }

        public int NumTeachpointItems
        {
            get { return _teachpoint_items.Count; }
        }

        //! \todo make the list of teachpoint items return a new list of copies of the items
        public List<TeachpointItem> TeachpointItems
        {
            get { return _teachpoint_items; }
        }

        public double this[string axis_name]
        {
            get { return GetPosition( axis_name); }
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class TeachpointItem
    {
        private readonly string _axis_name;
        private double _position;

        public TeachpointItem( string axis_name, double position)
        {
            _axis_name = axis_name;
            _position = position;
        }

        public string AxisName
        {
            get { return _axis_name; }
        }

        public double Position
        {
            get { return _position; }
            set { _position = value; }
        }
    }
}
