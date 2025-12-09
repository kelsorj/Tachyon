using System;
using System.Collections.Generic;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using CookComputing.XmlRpc;
using log4net;

namespace BioNex.Shared.XmlRpcGlue
{
    public class XmlRpcServer<server_type> where server_type : MarshalByRefObject
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(server_type));
        IChannel _channel;
        MarshalByRefObject _service;
        readonly string _serverName;

        public XmlRpcServer(server_type service, string serverName, string serverPath, int port)
        {
            _serverName = serverName;
            _service = service;

            try
            {

                var config = new Dictionary<string, string>() { { "name", _serverName }, { "ref", "http" }, { "port", port.ToString() }, {"exclusiveAddressUse", false.ToString() } };
                var serverSinkProps = new Dictionary<string, string>() { { "ref", "soap" } };
                var serverSinkProvider = new XmlRpcServerFormatterSinkProvider( serverSinkProps, null);
                _channel = new HttpChannel( config, null, serverSinkProvider);
                ChannelServices.RegisterChannel(_channel, false);

                if (RemotingConfiguration.CustomErrorsMode != CustomErrorsModes.Off)
                    RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;

                RemotingServices.Marshal( _service, serverPath, typeof(server_type));

                _log.InfoFormat( "Started xmlrpc server {0} at uri http://*:{1}/{2}", _serverName, port, serverPath);

            } catch( Exception ex) {
                Console.WriteLine( ex.Message);
                _log.ErrorFormat( " {0} failed to start: {1}", serverName, ex.Message);
            }
        }


        //Call stop from shutdown methods, or if you need to re-start server make sure to call Stop first
        public void Stop()
        {
            try
            {
                if (_channel != null)
                {
                    ChannelServices.UnregisterChannel(_channel);
                    _channel = null;
                }
                if (_service != null)
                {
                    RemotingServices.Disconnect(_service);
                    _service = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _log.ErrorFormat( "{0} failed to stop: {1}", _serverName, ex.Message);
            }
        }
    }
}
