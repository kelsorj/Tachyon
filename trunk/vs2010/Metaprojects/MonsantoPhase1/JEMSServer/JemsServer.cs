using System;
using System.Collections.Generic;

namespace BioNex.GemsRpc
{
    public class JemsServer : BioNex.Shared.XmlRpcGlue.XmlRpcServer<JEMSDataService>
    {
        public delegate void RefreshInventoryViewDelegate(string hive_name);

        public JemsServer(ref Dictionary<string, string> inventory, RefreshInventoryViewDelegate refresh, int port)
            : base(new JEMSDataService(ref inventory, refresh), "JemsServer", "jemsdata", port)
        {}
    }           
}
