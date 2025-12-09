using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CookComputing.XmlRpc;

// This is a SIMULATOR that simulates the Monsanto GEMS XML-RPC client talking to BioNex synapis HiveServer

namespace BioNex.HiveRpc
{
    public class HiveClient
    {
        private IHiveDataProxy _remote_server { get; set; }
        private string _server_url { get; set; }
        private int _server_port { get; set; }
        
        public HiveClient( string server_url, int port)
        {
            _server_url = server_url;
            _server_port = port;            
            _remote_server = (IHiveDataProxy)XmlRpcProxyGen.Create(typeof(IHiveDataProxy));
            _remote_server.Url = String.Format("http://{0}:{1}/hivedata", _server_url, _server_port);
            _remote_server.Timeout  = 10*60000;// 5000;
        }

        public void SetPlateFate( string barcode, string new_fate)
        {
            _remote_server.SetPlateFate( barcode, new_fate);
        }

        public void EndBatch( string fate)
        {
            _remote_server.EndBatch( fate);
        }

        public void Ping()
        {
            _remote_server.Ping();
        }
    }
}
