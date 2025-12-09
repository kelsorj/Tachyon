using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using BioNex.Shared.Teachpoints;
using log4net;

namespace BioNex.BumblebeeGUI
{
    public class ShuttleTeachpoint : GenericTeachpoint
    {
        // ----------------------------------------------------------------------
        // enumerations.
        // ----------------------------------------------------------------------
        public enum TeachpointType {
            Center,
        }

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public byte ShuttleId { get; set; }
        public byte ChannelId { get; set; }
        public TeachpointType TeachpointId { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public ShuttleTeachpoint()
        {
        }
        // ----------------------------------------------------------------------
        public ShuttleTeachpoint( byte shuttle_id, byte channel_id, TeachpointType teachpoint_id, double x, double y, double z)
        {
            Name = teachpoint_id.ToString();
            ShuttleId = shuttle_id;
            ChannelId = channel_id;
            X = x;
            Y = y;
            Z = z;
        }
    }

    public class NewStageTeachpoint : GenericTeachpoint
    {
        // ----------------------------------------------------------------------
        // enumerations.
        // ----------------------------------------------------------------------
        public enum TeachpointType {
            UpperLeft,
            LowerRight,
        }

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public byte StageId { get; set; }
        public byte ChannelId { get; set; }
        public TeachpointType TeachpointId { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double R { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public NewStageTeachpoint()
        {
        }
        // ----------------------------------------------------------------------
        public NewStageTeachpoint( byte stage_id, byte channel_id, TeachpointType teachpoint_id, double x, double y, double z, double r)
        {
            StageId = stage_id;
            ChannelId = channel_id;
            TeachpointId = teachpoint_id;
            X = x;
            Y = y;
            Z = z;
            R = r;
        }
    }

    public class StageTeachpoint : Teachpoint
    {
        private readonly byte _id;
        private Teachpoint _upper_left = new Teachpoint();
        private Teachpoint _lower_right = new Teachpoint();
        public string TipUsedForTeaching { get; set; }

        public override string ElementName
        {
            get { return "stage"; }
        }

        public StageTeachpoint( byte id)
        {
            _id = id;
            //! \todo add to XML
            TipUsedForTeaching = "tipbox";
        }

        public Teachpoint UpperLeft
        {
            get { return _upper_left; }
            set { _upper_left = value; }
        }

        public Teachpoint LowerRight
        {
            get { return _lower_right; }
            set { _lower_right = value; }
        }

        public override void WriteXML( XmlTextWriter writer)
        {
            // need to write <upper_left> and <lower_right> teachpoints
            writer.WriteStartElement( ElementName);
            writer.WriteAttributeString( "id", _id.ToString());
                // upper_left
                writer.WriteStartElement( "upper_left");
                foreach( TeachpointItem tpi in _upper_left.TeachpointItems) {
                    writer.WriteStartElement( tpi.AxisName);
                    writer.WriteValue( tpi.Position);
                    writer.WriteEndElement();
                }
                // lower_right
                writer.WriteStartElement( "lower_right");
                foreach( TeachpointItem tpi in _lower_right.TeachpointItems) {
                    writer.WriteStartElement( tpi.AxisName);
                    writer.WriteValue( tpi.Position);
                    writer.WriteEndElement();
                }
            writer.WriteEndElement();
        }

        public override void ReadXML( XmlNode node)
        {
            foreach( XmlNode n in node.ChildNodes) {
                if( n.Name == "upper_left") {
                    // read in upper left
                    _upper_left.ReadXML( n);
                } else if( n.Name == "lower_right") {
                    // read in lower right
                    _lower_right.ReadXML( n);
                }
            }
        }
    }

    public class WashTeachpoint : Teachpoint
    {
        private Teachpoint _tp = new Teachpoint();

        public Teachpoint teachpoint
        {
            get { return _tp; }
            set { _tp = value; }
        }

        public override string ElementName
        {
            get { return "wash"; }
        }

        public WashTeachpoint()
        {
        }

        public override void WriteXML( XmlTextWriter writer)
        {
            _tp.WriteXML( writer);
        }

        public override void ReadXML( XmlNode node)
        {
            _tp.ReadXML( node);
        }
    }

    public class RobotTeachpoint : Teachpoint
    {
        private Teachpoint _tp = new Teachpoint();

        public Teachpoint teachpoint
        {
            get { return _tp; }
            set { _tp = value; }
        }

        public override string ElementName
        {
            get { return "robot"; }
        }

        public RobotTeachpoint()
        {
        }

        public override void WriteXML( XmlTextWriter writer)
        {
            _tp.WriteXML( writer);
        }

        public override void ReadXML( XmlNode node)
        {
            _tp.ReadXML( node);
        }
    }

    public class WasherTeachpoint : Teachpoint
    {
        private Teachpoint _tp = new Teachpoint();

        public Teachpoint teachpoint
        {
            get { return _tp; }
            set { _tp = value; }
        }

        public override string ElementName
        {
            get { return "washer"; }
        }

        public WasherTeachpoint()
        {
        }

        public override void WriteXML( XmlTextWriter writer)
        {
            _tp.WriteXML( writer);
        }

        public override void ReadXML( XmlNode node)
        {
            _tp.ReadXML( node);
        }
    }

    public class Teachpoints
    {
        /// <summary>
        /// _teachpoints maps a channel to one or more stage teachpoints and a wash teachpoint
        /// </summary>
        private readonly Dictionary< byte, Dictionary< byte, StageTeachpoint>> _stage_teachpoints = new Dictionary< byte, Dictionary< byte, StageTeachpoint>>();
        private readonly Dictionary< byte, WashTeachpoint> _wash_teachpoints = new Dictionary< byte,WashTeachpoint>();
        private readonly Dictionary< byte, RobotTeachpoint> _landscape_teachpoints = new Dictionary< byte, RobotTeachpoint>();
        private readonly Dictionary< byte, RobotTeachpoint> _portrait_teachpoints = new Dictionary< byte, RobotTeachpoint>();
        private readonly Dictionary< byte, WasherTeachpoint> _washer_teachpoints = new Dictionary< byte, WasherTeachpoint>();
        private string teachpoint_filepath = null;
        private static readonly ILog Log = LogManager.GetLogger( typeof(Teachpoints));

        // private string shuttle_teachpoint_filepath = null;
        // private GenericTeachpointCollection< ShuttleTeachpoint> ShuttleTeachpoints = null;

        // ----------------------------------------------------------------------
        public Teachpoints()
        {
        }
        // ----------------------------------------------------------------------
        public void AddUpperLeftStageTeachpoint( byte channel_id, byte stage_id, double x, double z, double y, double r)
        {
            Teachpoint tp = new Teachpoint();
            tp.SetTeachpointItem( new TeachpointItem( "x", x));
            tp.SetTeachpointItem( new TeachpointItem( "z", z));
            tp.SetTeachpointItem( new TeachpointItem( "y", y));
            tp.SetTeachpointItem( new TeachpointItem( "r", r));
            if( !_stage_teachpoints.ContainsKey( channel_id)) {
                _stage_teachpoints.Add( channel_id, new Dictionary<byte,StageTeachpoint>());
                _stage_teachpoints[channel_id].Add( stage_id, new StageTeachpoint( stage_id));
                _stage_teachpoints[channel_id][stage_id].UpperLeft = tp;
            } else if( !_stage_teachpoints[channel_id].ContainsKey( stage_id)) {
                _stage_teachpoints[channel_id].Add( stage_id, new StageTeachpoint( stage_id));
                _stage_teachpoints[channel_id][stage_id].UpperLeft = tp;
            } else {
                _stage_teachpoints[channel_id][stage_id].UpperLeft = tp;
            }
            Log.InfoFormat( "updated upper left teachpoint for channel {0}, stage {1} at x={2}, y={3}, z={4}, r={5}", channel_id, stage_id, x, y, z, r);
        }
        // ----------------------------------------------------------------------
        public void AddLowerRightStageTeachpoint( byte channel_id, byte stage_id, double x, double z, double y, double r)
        {
            //! \todo refactor
            Teachpoint tp = new Teachpoint();
            tp.SetTeachpointItem( new TeachpointItem( "x", x));
            tp.SetTeachpointItem( new TeachpointItem( "z", z));
            tp.SetTeachpointItem( new TeachpointItem( "y", y));
            tp.SetTeachpointItem( new TeachpointItem( "r", r));
            if( !_stage_teachpoints.ContainsKey( channel_id)) {
                _stage_teachpoints.Add( channel_id, new Dictionary<byte,StageTeachpoint>());
                _stage_teachpoints[channel_id].Add( stage_id, new StageTeachpoint( stage_id));
                _stage_teachpoints[channel_id][stage_id].LowerRight = tp;
            } else if( !_stage_teachpoints[channel_id].ContainsKey( stage_id)) {
                _stage_teachpoints[channel_id].Add( stage_id, new StageTeachpoint( stage_id));
                _stage_teachpoints[channel_id][stage_id].LowerRight = tp;
            } else {
                _stage_teachpoints[channel_id][stage_id].LowerRight = tp;
            }
            Log.InfoFormat( "updated lower right teachpoint for channel {0}, stage {1} at x={2}, y={3}, z={4}, r={5}", channel_id, stage_id, x, y, z, r);
        }
        // ----------------------------------------------------------------------
        public void AddWashTeachpoint( byte channel_id, double x, double z)
        {
            Teachpoint tp = new Teachpoint();
            tp.SetTeachpointItem( new TeachpointItem( "x", x));
            tp.SetTeachpointItem( new TeachpointItem( "z", z));
            if( !_wash_teachpoints.ContainsKey( channel_id)) {
                _wash_teachpoints.Add( channel_id, new WashTeachpoint());
                _wash_teachpoints[channel_id].teachpoint = tp;
            } else {
                _wash_teachpoints[channel_id].teachpoint = tp;
            }
            Log.InfoFormat( "updated wash teachpoint for channel {0} at x={1}, z={2}", channel_id, x, z);
        }
        // ----------------------------------------------------------------------
        public void AddRobotTeachpoint( byte stage_id, double y, double r, int orientation)
        {
            IDictionary< byte, RobotTeachpoint> robot_teachpoints = orientation == 0 ? _landscape_teachpoints : _portrait_teachpoints;
            Teachpoint tp = new Teachpoint();
            tp.SetTeachpointItem( new TeachpointItem( "y", y));
            tp.SetTeachpointItem( new TeachpointItem( "r", r));
            if( !robot_teachpoints.ContainsKey( stage_id)) {
                robot_teachpoints.Add( stage_id, new RobotTeachpoint());
                robot_teachpoints[stage_id].teachpoint = tp;
            } else {
                robot_teachpoints[stage_id].teachpoint = tp;
            }
            Log.InfoFormat( "updated stage robot teachpoint for stage {0} at y={1}, r={2}", stage_id, y, r);
        }
        // ----------------------------------------------------------------------
        public void AddWasherTeachpoint( byte stage_id, string tp_key, double tp_value)
        {
            if( !_washer_teachpoints.ContainsKey( stage_id)){
                _washer_teachpoints.Add( stage_id, new WasherTeachpoint());
            }
            WasherTeachpoint washer_tp = _washer_teachpoints[ stage_id];
            washer_tp.teachpoint.SetTeachpointItem( new TeachpointItem( tp_key, tp_value));
        }
        // ----------------------------------------------------------------------
        public StageTeachpoint GetStageTeachpoint( byte channel_id, byte stage_id)
        {
            return _stage_teachpoints[channel_id][stage_id];
        }
        // ----------------------------------------------------------------------
        public List<Teachpoint> GetAllStageTeachpoints()
        {
            // not sure how to do this in LINQ
            List<Teachpoint> stage_teachpoints = new List<Teachpoint>();
            foreach( KeyValuePair<byte,Dictionary<byte,StageTeachpoint>> dic in _stage_teachpoints) {
                foreach( KeyValuePair<byte,StageTeachpoint> kvp in dic.Value) {
                    stage_teachpoints.Add( kvp.Value.UpperLeft);
                    stage_teachpoints.Add( kvp.Value.LowerRight);
                }
            }
            return stage_teachpoints;
        }
        // ----------------------------------------------------------------------
        public Teachpoint GetWashTeachpoint( byte channel_id)
        {
            return _wash_teachpoints[channel_id].teachpoint;
        }
        // ----------------------------------------------------------------------
        public Teachpoint GetRobotTeachpoint( byte stage_id, int orientation)
        {
            return orientation == 0 ? _landscape_teachpoints[stage_id].teachpoint : _portrait_teachpoints[stage_id].teachpoint;
        }
        // ----------------------------------------------------------------------
        public Teachpoint GetWasherTeachpoint( byte stage_id)
        {
            return _washer_teachpoints[ stage_id].teachpoint;
        }
        // ----------------------------------------------------------------------
        public void LoadTeachpointFile( string path, string schemapath)
        {
            // use the XML DOM to load the teachpoints
            XmlDocument doc = new XmlDocument();
            doc.Load( path);
            if( !String.IsNullOrEmpty( schemapath)) {
                XmlTextReader xtr = new XmlTextReader( path);
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.Schemas.Add( null, schemapath);
                settings.ValidationType = ValidationType.Schema;
                XmlReader reader = XmlReader.Create( xtr, settings);
                doc.Load( reader);
            } else {
                doc.Load( path);
            }
            Debug.Assert( doc.DocumentElement.Name == "Teachpoints");
            _stage_teachpoints.Clear();
            _wash_teachpoints.Clear();
            _landscape_teachpoints.Clear();
            _portrait_teachpoints.Clear();
            // parse all <StageTeachpoints> and <WashTeachpoints> nodes under <Teachpoints>
            foreach( XmlNode node in doc.DocumentElement.ChildNodes) {
                if( node.Name == "StageTeachpoints") {
                    // this will get all TP info and add it to the teachpoints list / dictionary
                    ParseStageTeachpoints( node, _stage_teachpoints);
                } else if( node.Name == "WashTeachpoints") {
                    ParseWashTeachpoints( node, _wash_teachpoints);
                } else if( node.Name == "LandscapeTeachpoints") {
                    ParseRobotTeachpoints( node, _landscape_teachpoints);
                } else if( node.Name == "PortraitTeachpoints") {
                    ParseRobotTeachpoints( node, _portrait_teachpoints);
                } else if( node.Name == "WasherTeachpoints") {
                    ParseWasherTeachpoints( node, _washer_teachpoints);
                }
            }
            teachpoint_filepath = path;

            // shuttle_teachpoint_filepath = path + ".shuttle";
            // ShuttleTeachpoints = GenericTeachpointCollection< ShuttleTeachpoint>.LoadTeachpointsFromFile( shuttle_teachpoint_filepath);
        }
        // ----------------------------------------------------------------------
        public void SaveTeachpointFile()
        {
            SaveTeachpointFile( teachpoint_filepath);
            // GenericTeachpointCollection< ShuttleTeachpoint>.SaveTeachpointsToFile( shuttle_teachpoint_filepath, ShuttleTeachpoints);
        }
        // ----------------------------------------------------------------------
        public void SaveTeachpointFile( string path)
        {
            XmlTextWriter writer = new XmlTextWriter( path, null);
            writer.Formatting = Formatting.Indented;
            writer.WriteStartDocument();
            writer.WriteStartElement( "Teachpoints");
                // save all stage teachpoints
                writer.WriteStartElement( "StageTeachpoints");
                foreach( KeyValuePair<byte,Dictionary<byte,StageTeachpoint>> kvp in _stage_teachpoints)
                    SaveStageTeachpoint( kvp, writer);
                writer.WriteEndElement();
                // save all wash teachpoints
                writer.WriteStartElement( "WashTeachpoints");
                foreach( KeyValuePair<byte,WashTeachpoint> kvp in _wash_teachpoints)
                    SaveWashTeachpoint( kvp, writer);
                writer.WriteEndElement();
                // save landscape teachpoints
                writer.WriteStartElement( "LandscapeTeachpoints");
                foreach( KeyValuePair<byte,RobotTeachpoint> kvp in _landscape_teachpoints)
                    SaveRobotTeachpoint( kvp, writer);
                writer.WriteEndElement();
                // save portrait teachpoints
                writer.WriteStartElement( "PortraitTeachpoints");
                foreach( KeyValuePair<byte,RobotTeachpoint> kvp in _portrait_teachpoints)
                    SaveRobotTeachpoint( kvp, writer);
                writer.WriteEndElement();
                // save washer teachpoints
                writer.WriteStartElement( "WasherTeachpoints");
                foreach( KeyValuePair<byte,WasherTeachpoint> kvp in _washer_teachpoints)
                    SaveWasherTeachpoint( kvp, writer);
                writer.WriteEndElement();
            writer.WriteEndElement();
            writer.Close();
            Log.InfoFormat( "Saved teachpoint file '{0}'", path);
        }
        // ----------------------------------------------------------------------
        private static void SaveStageTeachpoint( KeyValuePair<byte,Dictionary<byte,StageTeachpoint>> channel_teachpoint_info, XmlTextWriter writer)
        {
            byte channel_id = channel_teachpoint_info.Key;
            Dictionary<byte,StageTeachpoint> ct = channel_teachpoint_info.Value;
            writer.WriteStartElement( "channel");
            writer.WriteAttributeString( "id", channel_id.ToString());
            foreach( KeyValuePair<byte,StageTeachpoint> kvp in ct) {
                byte stage_id = kvp.Key;
                StageTeachpoint tp = kvp.Value;
                writer.WriteStartElement( "stage");
                writer.WriteAttributeString( "id", stage_id.ToString());
                    // upper left
                    writer.WriteStartElement( "upper_left");
                    if( tp.UpperLeft != null) {
                        foreach( TeachpointItem tpi in tp.UpperLeft.TeachpointItems) {
                            writer.WriteStartElement( tpi.AxisName);
                            writer.WriteValue( Convert.ToString( tpi.Position));
                            writer.WriteEndElement();
                        }
                    }
                    writer.WriteEndElement();
                    // lower right
                    writer.WriteStartElement( "lower_right");
                    if( tp.LowerRight != null) {
                        foreach( TeachpointItem tpi in tp.LowerRight.TeachpointItems) {
                            writer.WriteStartElement( tpi.AxisName);
                            writer.WriteValue( Convert.ToString( tpi.Position));
                            writer.WriteEndElement();
                        }
                    }
                    writer.WriteEndElement();
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        // ----------------------------------------------------------------------
        private static void SaveWashTeachpoint( KeyValuePair<byte,WashTeachpoint> wash_teachpoint_info, XmlTextWriter writer)
        {
            byte channel_id = wash_teachpoint_info.Key;
            WashTeachpoint tp = wash_teachpoint_info.Value;
            writer.WriteStartElement( "channel");
            writer.WriteAttributeString( "id", channel_id.ToString());
                foreach( TeachpointItem tpi in tp.teachpoint.TeachpointItems) {
                    writer.WriteStartElement( tpi.AxisName);
                    writer.WriteValue( Convert.ToString( tpi.Position));
                    writer.WriteEndElement();
                }
            writer.WriteEndElement();
        }
        // ----------------------------------------------------------------------
        private static void SaveRobotTeachpoint( KeyValuePair< byte, RobotTeachpoint> robot_teachpoint_info, XmlTextWriter writer)
        {
            byte stage_id = robot_teachpoint_info.Key;
            RobotTeachpoint tp = robot_teachpoint_info.Value;
            writer.WriteStartElement( "stage");
            writer.WriteAttributeString( "id", stage_id.ToString());
                foreach( TeachpointItem tpi in tp.teachpoint.TeachpointItems) {
                    writer.WriteStartElement( tpi.AxisName);
                    writer.WriteValue( Convert.ToString( tpi.Position));
                    writer.WriteEndElement();
                }
            writer.WriteEndElement();
        }
        // ----------------------------------------------------------------------
        private static void SaveWasherTeachpoint( KeyValuePair< byte, WasherTeachpoint> washer_teachpoint_info, XmlTextWriter writer)
        {
            byte tip_washer_id = washer_teachpoint_info.Key;
            WasherTeachpoint tp = washer_teachpoint_info.Value;
            writer.WriteStartElement( "tip_washer");
            writer.WriteAttributeString( "id", tip_washer_id.ToString());
                foreach( TeachpointItem tpi in tp.teachpoint.TeachpointItems) {
                    writer.WriteStartElement( tpi.AxisName);
                    writer.WriteValue( Convert.ToString( tpi.Position));
                    writer.WriteEndElement();
                }
            writer.WriteEndElement();
        }
        // ----------------------------------------------------------------------
        private static void ParseStageTeachpoints( XmlNode nodes, IDictionary< byte, Dictionary< byte, StageTeachpoint>> tps)
        {
            foreach( XmlNode node in nodes) {
                if( node.Name == "channel") {
                    byte channel_id = byte.Parse( node.Attributes["id"].Value);
                    Dictionary<byte,StageTeachpoint> parsed_tps = new Dictionary<byte,StageTeachpoint>();
                    foreach( XmlNode n in node) {
                        // here, we either have a stage or wash teachpoint, and we
                        // unfortunately need to treat each differently
                        if( n.Name == "stage") {
                            byte stage_id = byte.Parse( n.Attributes["id"].InnerText);
                            parsed_tps[stage_id] =  ParseStageTeachpoint( n);
                        }
                    }
                    tps.Add( channel_id, parsed_tps);                    
                }
            }
        }
        // ----------------------------------------------------------------------
        private static void ParseWashTeachpoints( XmlNode nodes, IDictionary< byte, WashTeachpoint> tps)
        {
            foreach( XmlNode node in nodes) {
                if( node.Name == "channel") {
                    byte channel_id = byte.Parse( node.Attributes["id"].Value);
                    WashTeachpoint tp = ParseWashTeachpoint( node);
                    tps[channel_id] = tp;
                }
            }
        }
        // ----------------------------------------------------------------------
        private static void ParseRobotTeachpoints( XmlNode nodes, IDictionary< byte, RobotTeachpoint> tps)
        {
            foreach( XmlNode node in nodes) {
                if( node.Name == "stage") {
                    byte stage_id = byte.Parse( node.Attributes["id"].Value);
                    RobotTeachpoint tp = ParseRobotTeachpoint( node);
                    tps[stage_id] = tp;
                }
            }
        }
        // ----------------------------------------------------------------------
        private static void ParseWasherTeachpoints( XmlNode nodes, IDictionary< byte, WasherTeachpoint> tps)
        {
            foreach( XmlNode node in nodes) {
                if( node.Name == "tip_washer") {
                    byte tip_washer_id = byte.Parse( node.Attributes["id"].Value);
                    WasherTeachpoint tp = ParseWasherTeachpoint( node);
                    tps[tip_washer_id] = tp;
                }
            }
        }
        // ----------------------------------------------------------------------
        private static StageTeachpoint ParseStageTeachpoint( XmlNode node)
        {
            // get the stage ID from the node
            byte id = byte.Parse( node.Attributes["id"].Value);
            StageTeachpoint tp = new StageTeachpoint( id);
            tp.ReadXML( node);
            return tp;
        }
        // ----------------------------------------------------------------------
        private static WashTeachpoint ParseWashTeachpoint( XmlNode node)
        {
            WashTeachpoint tp = new WashTeachpoint();
            tp.ReadXML( node);
            return tp;
        }
        // ----------------------------------------------------------------------
        private static RobotTeachpoint ParseRobotTeachpoint( XmlNode node)
        {
            RobotTeachpoint tp = new RobotTeachpoint();
            tp.ReadXML( node);
            return tp;
        }
        // ----------------------------------------------------------------------
        private static WasherTeachpoint ParseWasherTeachpoint( XmlNode node)
        {
            WasherTeachpoint tp = new WasherTeachpoint();
            tp.ReadXML( node);
            return tp;
        }
    }
}
