using System;

namespace BioNex.Shared.TeachpointServer
{

    // This is a server class that remotes teachpoints 

    public class TeachpointServer : BioNex.Shared.XmlRpcGlue.XmlRpcServer<TeachpointService>
    {
        public TeachpointServer(TeachpointService service_impl, string device_name, int port)
            : base(service_impl, string.Format("{0}_teachpoints_service", device_name), string.Format("{0}_teachpoints", device_name), port)
        {
        }
    }
}
