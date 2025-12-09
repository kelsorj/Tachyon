using System;

// This is the server class that actually serves the remote interface over xml-rpc.  Synapsis instantiates this so that the BioNex Stacker Plugin can connect from the VWorks machine

namespace BioNex.IWorksCommandServer
{
    public class RemoteStackerServer : BioNex.Shared.XmlRpcGlue.XmlRpcServer<StackerRpcInterface>
    {
        public RemoteStackerServer(StackerRpcInterface service, int port)
            : base(service, "RemoteStackerServer", "stackerdata", port)
        {}
    }
}
