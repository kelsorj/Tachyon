using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CookComputing.XmlRpc;

// JEMSClient -- Communicates from Synapsis to Monsanto GEMS database server (or our internal JEMSServer for testing)

namespace BioNex.JEMSRpc
{
    public class JEMSClient
    {
        private IJEMSCommandProxy _remote_server { get; set; }
        private string _server_url { get; set; }
        private int _server_port { get; set; }

        public JEMSClient( string server_url, int port)
        {
            _server_url = server_url;
            _server_port = port;

            _remote_server = (IJEMSCommandProxy)XmlRpcProxyGen.Create(typeof(IJEMSCommandProxy));
            _remote_server.Url = String.Format( "http://{0}:{1}/jemsdata", _server_url, _server_port);
            _remote_server.Timeout = 5000;
        }

        public void DestinationPlateComplete(string hive_name, string destination_barcode, BioNex.GemsRpc.TransferMap[] mapping)
        {
            _remote_server.DestinationPlateComplete(hive_name, destination_barcode, mapping);
        }
     

        public void ReinventoryComplete(string hive_name, string cart_name, string[] barcodes)
        {
            _remote_server.ReinventoryComplete( hive_name, cart_name, barcodes);
        }

        public bool Ping()
        {
            try
            {
                _remote_server.Ping();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
