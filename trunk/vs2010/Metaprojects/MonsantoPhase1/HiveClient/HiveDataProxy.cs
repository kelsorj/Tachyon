using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CookComputing.XmlRpc;

namespace BioNex.HiveRpc
{
    public interface IHiveDataProxy : IXmlRpcProxy
    {
        [XmlRpcMethod("BioNex.HiveRpc.IHiveData.SetPlateFate")]
        void SetPlateFate(string barcode, string new_fate);
        [XmlRpcMethod("BioNex.HiveRpc.IHiveData.EndBatch")]
        void EndBatch(string fate);
        [XmlRpcMethod("BioNex.HiveRpc.IHiveData.Ping")]
        void Ping();
    }
}
