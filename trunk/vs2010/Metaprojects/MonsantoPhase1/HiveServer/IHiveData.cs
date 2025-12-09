using System;
using System.Collections.Generic;
using System.Text;
using CookComputing.XmlRpc;

// This is the server side XML-RPC interface definition for the Synapsis side of the GEMS -> Synapsis xml-rpc comms

namespace BioNex.HiveRpc
{
    public interface IHiveData
    {
        [XmlRpcMethod("BioNex.HiveRpc.IHiveData.SetPlateFate", Description="GEMS calls this to set a plate's fate.  Params are barcode and fate")]
        void SetPlateFate( string barcode, string new_fate);

        [XmlRpcMethod("BioNex.HiveRpc.IHiveData.EndBatch", Description="GEMS calls this to signal end of batch if it is desired to undock a cart before completely filling it.  Param is fate")]
        void EndBatch(string fate);

        [XmlRpcMethod("BioNex.HiveRpc.IHiveData.Ping", Description="Ping method is used to verify connection without performing any action")]
        void Ping();
    }
}
