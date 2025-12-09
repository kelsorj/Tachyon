using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CookComputing.XmlRpc;

// IGemsData interface served by JemsServer, BioNex test server (this interface will be served by Monsanto GEMS in final version)

namespace BioNex.GemsRpc
{
    public interface IGemsData
    {
        [XmlRpcMethod("BioNex.GemsRpc.IGemsData.ReinventoryComplete", Description="ReinventoryComplete method is called by Synapsis after an inventory is completed.  Parameters are hive_name, cart_name, and an array of barcodes")]
        void ReinventoryComplete( string hive_name, string cart_name, string[] barcodes);

        [XmlRpcMethod("BioNex.GemsRpc.IGemsData.Ping", Description="Ping method is used to verify connection without performing any action")]
        void Ping();
    }
}
