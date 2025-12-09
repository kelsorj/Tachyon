using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.HiveIntegration
{
    /// <summary>
    /// HiveClient is the entity that wants to control the Hive by sending the remote server (Synapsis) RPC commands
    /// </summary>
    public interface HiveClient : HiveCommands, CookComputing.XmlRpc.IXmlRpcProxy
    {
    }

    /// <summary>
    /// HiveServer accepts the RPC commands on the Synapsis end, so customer GUI plugin should inherit from this class
    /// </summary>
    public abstract class HiveRpcInterface : CookComputing.XmlRpc.SystemMethodsBase, HiveCommands
    {
        public abstract void Ping();
        public abstract void Initialize( string xml_parameters);
        public abstract void Close();
        public abstract void UnloadPlate( string expected_barcode, string labware_name);
        public abstract void LoadPlate( string expected_barcode, string labware_name);
        public abstract bool HasBarcode( string barcode);
        public abstract string GetInventory();
        public abstract void ScanInventory();
        public abstract void MovePlate( string barcode, string labware_name, string destination_group);
        public abstract int GetStatus();
        public abstract void PresentStage();
        // http://www.codeproject.com/Articles/14791/NET-Remoting-with-an-easy-example
        // http://www.thinktecture.com/resourcearchive/net-remoting-faq/singletonisdying
        public override object InitializeLifetimeService() { return null; } // The lease to this object shall never expire
    }

    /// <summary>
    /// This is what sits and listens for incoming RPC commands.  You must create a class that
    /// inherits from HiveRpcInterface and implements all of its methods, and pass an instance
    /// of that class to the HiveServer constructor.
    /// </summary>
    public class HiveServer : BioNex.Shared.XmlRpcGlue.XmlRpcServer<HiveRpcInterface>
    {
        public HiveServer( HiveRpcInterface service, int port)
            : base( service, "HiveServer", "hiverpc", port)
        {}
    }
}
