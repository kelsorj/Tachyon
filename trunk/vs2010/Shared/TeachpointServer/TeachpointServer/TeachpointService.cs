using System;
using BioNex.Shared.Teachpoints;
using CookComputing.XmlRpc;

namespace BioNex.Shared.TeachpointServer
{
    public abstract class TeachpointService : SystemMethodsBase, ITeachpointService
    {
        public abstract string[] GetDeviceNames();
        public abstract string[] GetTeachpointNames(string device_name, string dockable_barcode);
        public abstract XmlRpcTeachpoint GetXmlRpcTeachpoint(string device_name, string dockable_barcode, string teachpoint_name);
        public abstract bool IsDock(string device);

        public override object InitializeLifetimeService() { return null; } // The lease to this object shall never expire
    }
}
