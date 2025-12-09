using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CookComputing.XmlRpc;

namespace BioNex.IWorksCommandServer
{
    public interface IHiveUpstackerPingBackProxy : IPingBack, IResetStackHeight, IXmlRpcProxy
    {
    }

    public class RemoteStackerPingBack
    {
        private IHiveUpstackerPingBackProxy _remote_server;

        public RemoteStackerPingBack( string server_url, int server_port)
        {
            _remote_server = (IHiveUpstackerPingBackProxy)XmlRpcProxyGen.Create(typeof(IHiveUpstackerPingBackProxy));
            _remote_server.Url = String.Format( "http://{0}:{1}/pingback", server_url, server_port);
            _remote_server.Timeout  = 5000;
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

        public void ResetStackHeight()
        {
            _remote_server.ResetStackHeight();
        }
    }
}
