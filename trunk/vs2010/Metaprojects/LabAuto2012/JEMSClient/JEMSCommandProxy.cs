using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CookComputing.XmlRpc;

// Proxy interface used by JEMSClient to talk to GEMS or JEMSServer

namespace BioNex.JEMSRpc
{
    public interface IJEMSCommandProxy : BioNex.GemsRpc.IGemsData, IXmlRpcProxy 
    {
    }
}
