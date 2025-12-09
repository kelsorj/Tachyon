using System;
using BioNex.Hive.Hardware;
using BioNex.Shared.TeachpointServer;
using CookComputing.XmlRpc;

namespace BioNex.HivePrototypePlugin
{
    public interface TeachpointServiceProxy : ITeachpointService, IXmlRpcProxy
    { }

    public class TeachpointXmlRpcClient : ITeachpointService
    {
        private TeachpointServiceProxy _remote_server { get; set; }

        public TeachpointXmlRpcClient(string device_name, string url, int port)
        {
            var path = string.Format("{0}_teachpoints", device_name);
            _remote_server = (TeachpointServiceProxy)XmlRpcProxyGen.Create(typeof(TeachpointServiceProxy));
            _remote_server.Url = String.Format( "http://{0}:{1}/{2}", url, port, path);
            _remote_server.Timeout = 5000;
        }

        public string[] GetDeviceNames()
        {
            return _remote_server.GetDeviceNames();
        }

        public string[] GetTeachpointNames(string device, string dockable_id)
        {
            return _remote_server.GetTeachpointNames(device, dockable_id);
        }

        public XmlRpcTeachpoint GetXmlRpcTeachpoint(string device, string dockable_barcode, string teachpoint)
        {
            return _remote_server.GetXmlRpcTeachpoint(device, dockable_barcode, teachpoint);
        }

        public HiveTeachpoint GetTeachpoint(string device, string dockable_barcode, string teachpoint)
        {
            return (HiveTeachpoint)GetXmlRpcTeachpoint(device, dockable_barcode, teachpoint);
        }

        public bool IsDock(string device)
        {
            return _remote_server.IsDock(device);
        }
    }
}
