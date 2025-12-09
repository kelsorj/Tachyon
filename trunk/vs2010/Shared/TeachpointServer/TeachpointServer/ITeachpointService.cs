using System;
using System.Linq;
using System.Reflection;
using BioNex.Shared.Teachpoints;
using CookComputing.XmlRpc;

namespace BioNex.Shared.TeachpointServer
{
    public interface ITeachpointService
    {
        [XmlRpcMethod("BioNex.Teachpoints.GetDeviceNames", Description = "Returns the list of devices that this robot has access to")]
        string[] GetDeviceNames();

        [XmlRpcMethod("BioNex.Teachpoints.GetTeachpointNames", Description="Returns the list of teachpoints that this robot uses to access the named device")]
        string[] GetTeachpointNames(string device_name, string dockable_barcode);

        [XmlRpcMethod("BioNex.Teachpoints.GetTeachpoint", Description = "Returns a Teachpoint structure for the specified device name and teachpoint name")]
        XmlRpcTeachpoint GetXmlRpcTeachpoint(string device_name, string dockable_barcode, string teachpoint_name);

        [XmlRpcMethod("BioNex.Teachpoints.IsDock", Description = "Returns true if the named device is a dock device")]
        bool IsDock(string device_name);
    }


    /// <summary>
    /// Teachpoint wrapper classes for xmlrpc serialization
    /// </summary>
    public struct XmlRpcTeachpoint
    {
        // XmlRpc Serializer only serializes PUBLIC properties and fields
        public string name;
        public XmlRpcTeachpointItem[] items;

        public XmlRpcTeachpoint( GenericTeachpoint tp)
        {
            name = tp.Name;
            var property_infos = tp.GetType().GetProperties().Where( property_info => property_info.Name != "Name");
            items = property_infos.Select( property_info => new XmlRpcTeachpointItem( property_info.Name, property_info.GetValue( tp, null).ToString())).ToArray();
        }

        public static explicit operator GenericTeachpoint( XmlRpcTeachpoint xtp)
        {
            var tp = new GenericTeachpoint( xtp.name);
            foreach( var xtpi in xtp.items){
                PropertyInfo property_info = tp.GetType().GetProperty( xtpi.axis);
                object property_value = property_info.PropertyType.IsEnum ? Enum.Parse( property_info.PropertyType, xtpi.position) : null;
                if( property_value == null){
                    throw new Exception( "need to do more parsing");
                }
                property_info.SetValue( tp, property_value, null);
            }
            return tp;
        }
    }

    public struct XmlRpcTeachpointItem
    {
        // XmlRpc Serializer only serializes PUBLIC properties and fields
        public string axis;
        public string position;

        public XmlRpcTeachpointItem( string name, string value)
        {
            axis = name;
            position = value;
        }

        /* not needed.
        public static explicit operator TeachpointItem(XmlRpcTeachpointItem xtpi)
        {
            return new TeachpointItem(xtpi.axis, xtpi.position);
        }
        */
    }
}
