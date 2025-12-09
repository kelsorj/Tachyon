using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.LibraryInterfaces;

namespace BioNex.Shared.LabwareCloudXmlRpcServer
{
    public class LabwareXmlRpcServer : BioNex.Shared.XmlRpcGlue.XmlRpcServer<LabwareXmlRpcService>
    {
        public LabwareXmlRpcServer(ILabwareDatabase labware_db, int port)
            : base(new LabwareXmlRpcService(labware_db), "LabwareCloudServer", "labware", port)
        { }
    }
}
