using System;
using System.Linq;
using BioNex.Hive.Hardware;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.TeachpointServer;

namespace BioNex.HivePrototypePlugin
{
    public class TeachpointServiceImplementation : TeachpointService
    {
        readonly HivePlugin _plugin;

        public TeachpointServiceImplementation(HivePlugin plugin)
        {
            _plugin = plugin;
        }

        public override string[] GetDeviceNames()
        {
            return _plugin.DataRequestInterface.Value.GetAccessibleDeviceInterfaces().Select( adi => adi.Name).ToArray();
        }

        public override string[] GetTeachpointNames(string device_name, string dockable_barcode)
        {
            if( dockable_barcode != null && IsDock(device_name) ) 
            {
                _plugin.Hardware.LoadTeachpoints( _plugin.DataRequestInterface.Value.GetAccessibleDeviceInterfaces().FirstOrDefault( adi => adi.Name == device_name));
                return _plugin.Hardware.GetTeachpointNames( device_name).ToArray();
            } 
            
            return _plugin.GetTeachpointNames()[device_name].ToArray();
        }

        public override XmlRpcTeachpoint GetXmlRpcTeachpoint(string device_name, string dockable_barcode, string teachpoint_name)
        {
            if( dockable_barcode != null && IsDock(device_name))
            {
                _plugin.Hardware.LoadTeachpoints( _plugin.DataRequestInterface.Value.GetAccessibleDeviceInterfaces().FirstOrDefault( adi => adi.Name == device_name));
                HiveTeachpoint teachpoint = _plugin.Hardware.GetTeachpoint( device_name, teachpoint_name);
                if( teachpoint != null){
                    try{
                        return new XmlRpcTeachpoint( teachpoint);
                    } catch ( Exception){
                        return new XmlRpcTeachpoint();
                    }
                }
            }
            return new XmlRpcTeachpoint( _plugin.Hardware.GetTeachpoint( device_name, teachpoint_name));
        }

        public override bool IsDock(string device)
        {
            var available_docks = _plugin.DataRequestInterface.Value.GetDockablePlateStorageInterfaces();
            return available_docks.Where( (x) => (x as DeviceInterface).Name == device).FirstOrDefault() != null;            
        }
    }
}
