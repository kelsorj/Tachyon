using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using BioNex.Shared.Utils;
using BioNex.Shared.LabwareDatabase;
using BioNex.Shared.LabwareCloudXmlRpcServer;


namespace LabwareCloudService
{
    public class LabwareCloudApp
    {
        const int http_port = 7676;
        const int xmlrpc_port = 7677;
        string labware_db_location = @"config\labware.s3db";

        LabwareDatabase _labware_db;
        LabwareHttpServer _http_server;
        LabwareXmlRpcServer _xmlrpc_server;

        public void Start(bool isService)
        {
            _labware_db = new LabwareDatabase(labware_db_location.ToAbsoluteAppPath());
            _http_server = new LabwareHttpServer(_labware_db, isService, http_port);
            _xmlrpc_server = new LabwareXmlRpcServer(_labware_db, xmlrpc_port);            

            // serve xml-rpc server that provides the labware cloud service
        }

        public void Stop()
        {
            // ports are released when the process terminates, but a service can be stopped without terminating the process
            // its nice to release in that case as well.
            _http_server.Stop(); 
            _xmlrpc_server.Stop();
        }
    }
}
