using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CookComputing.XmlRpc;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.LabwareDatabase;

namespace BioNex.Shared.LabwareCloudXmlRpcServer
{
    public interface ILabwareCloudXmlRpcService
    {
        [XmlRpcMethod("BioNex.Labware.Sync", Description = "If the master's labware db has changed since lastSyncTime, master returns the complete labware db as an array of XmlRpcLabware entries.")]
        XmlRpcLabware[] Sync(DateTime lastSyncTime);

        [XmlRpcMethod("BioNex.Labware.BeginPublish", Description = "The client uses this method to begin the Publish transaction.  The master returns the transaction guid as a string. Transaction will timeout after 30 seconds of inactivity.")]
        string BeginPublish();

        [XmlRpcMethod("BioNex.Labware.EndPublish", Description = "The client uses this method to flush the publish transaction.  After this call, the transaction is complete and the transaction guid is no longer valid.")]
        void EndPublish(string guid);
        
        [XmlRpcMethod("BioNex.Labware.Publish", Description = "The client uses this method to transmit all or part of the local labware changes to the master.  The guid must be a valid transaction guid returned by BeginPublish.")]
        void Publish(string guid, XmlRpcLabware[] labware);
    }
  
    /// <summary>
    ///  Labware wrapper classes for xmlrpc serialization.  Needed since xmlrpc can't deal with the associative property container
    /// </summary>
    /// 
    public struct XmlRpcLabware
    {
        // XmlRpc Serializer only serializes PUBLIC properties and fields
        public string name;
        public string notes;
        public string tags;
        public XmlRpcLabwareProperty[] properties;


        public XmlRpcLabware(ILabware labware)
        {
            name = labware.Name;
            notes = labware.Notes ?? ""; // no nulls allowed over xml-rpc channel for now
            tags = labware.Tags ?? ""; // no nulls allowed over xml-rpc channel for now

            properties = new XmlRpcLabwareProperty[labware.Properties.Count];
            for(int i=0; i< labware.Properties.Count; ++i)
                properties[i] = new XmlRpcLabwareProperty(labware.Properties.ElementAt(i));
        }

        public static explicit operator Labware(XmlRpcLabware x)
        {
            var labware = new Labware();
            labware.Name = x.name;
            labware.Notes = x.notes;
            labware.Tags = x.tags;

            if( x.properties != null)
                foreach (var xlp in x.properties)
                    labware.Properties[xlp.name] = xlp.GetTypedValue();
            return labware;
        }
    }

    public struct XmlRpcLabwareProperty
    {
        // XmlRpc Serializer only serializes PUBLIC properties and fields
        public string name;
        public string type;  // LabwarePropertyType Enum --> XmlRpc.Net chokes on Enum types !?!
        public string value;

        public XmlRpcLabwareProperty(KeyValuePair<string, object> kvp)
        {
            var obj = kvp.Value;
            var obj_type = obj.GetType();

            if (obj_type == typeof(int)) type = LabwarePropertyType.INTEGER.ToString();
            else if (obj_type == typeof(string)) type = LabwarePropertyType.STRING.ToString();
            else if (obj_type == typeof(double)) type = LabwarePropertyType.DOUBLE.ToString();
            else if (obj_type == typeof(bool)) type = LabwarePropertyType.BOOL.ToString();
            else throw new ArgumentException("Invalid labware property type");

            name = kvp.Key;
            value = obj.ToString();
        }

        public object GetTypedValue()
        {
            switch ((LabwarePropertyType)Enum.Parse(typeof(LabwarePropertyType), type))
            {
                case LabwarePropertyType.INTEGER:
                    return int.Parse(value);
                case LabwarePropertyType.STRING:
                    return value;
                case LabwarePropertyType.DOUBLE:
                    return double.Parse(value);
                case LabwarePropertyType.BOOL:
                    return bool.Parse(value);
            }
            throw new ArgumentException("Invalid labware property type");
        }
    }
}
