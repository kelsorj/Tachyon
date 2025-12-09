using System;
using System.Collections.Generic;

// This is the server class that actually serves the remote interface over xml-rpc.  Synapsis instantiates this so that Monsanto GEMS can connect.

namespace BioNex.HiveRpc
{
    public class HiveServer : BioNex.Shared.XmlRpcGlue.XmlRpcServer<HiveRemoteInterface>
    {
        public delegate void UpdatePlateFateDelegate(string barcode, string fate);
        public delegate void EndBatchDelegate(string batch);
        public delegate void AddWorkSetDelegate(string workset_xml);

        public HiveServer(int port, UpdatePlateFateDelegate update=null, EndBatchDelegate end_batch=null, AddWorkSetDelegate add_workset=null) 
            : base(new HiveRemoteInterface(update, end_batch, add_workset), "HiveServer", "hivedata", port)
        { }
    }
}
