using System;
using System.Collections.Generic;

// JemsServer is our simulated GEMS Server.  
// This project has its own Solution for generating a simulation executable
// Synapsis does not use a JemsServer object (it uses JemsClient)
// But this project is included in Synapsis because the IGEMSData interface is defined here

namespace BioNex.GemsRpc
{
    public class JemsServer : BioNex.Shared.XmlRpcGlue.XmlRpcServer<JEMSDataService>
    {
        public delegate void RefreshInventoryViewDelegate(string hive_name);

        public JemsServer(ref Dictionary<string, TransferMap[]> transfer_map, ref Dictionary<string, string> inventory, RefreshInventoryViewDelegate refresh, int port)
            : base(new JEMSDataService(ref transfer_map, ref inventory, refresh), "JemsServer", "jemsdata", port)
        {}
    }           
}
