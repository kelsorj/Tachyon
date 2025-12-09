using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using CookComputing.XmlRpc;
using BioNex.IWorksCommandServer;


namespace BioNex.IWorksPlugins
{
    interface IHiveUpstackerPingBack : IPingBack, IResetStackHeight
    {
    }

    class PingBackService : SystemMethodsBase, IHiveUpstackerPingBack
    {
        public PingBackService(HiveUpstacker upstacker) { _upstacker = upstacker; }

        public void Ping() { }

        HiveUpstacker _upstacker;
        public void ResetStackHeight() { _upstacker.ResetStackHeight(); }

        public override object InitializeLifetimeService() { return null; } // The lease to this object shall never expire
    }

    public class PingBackServer
    {
        IChannel _channel;
        MarshalByRefObject _service;

        public PingBackServer(int port, HiveUpstacker upstacker)
        {
            try {
                var config = new Dictionary<string, string>() { { "name", "PingBackServer" }, { "ref", "http" }, { "port", port.ToString() }, {"exclusiveAddressUse", false.ToString() } };
                var serverSinkProps = new Dictionary<string, string>() { { "ref", "soap" } };
                var clientSinkProvider = new CookComputing.XmlRpc.XmlRpcClientFormatterSinkProvider();
                var serverSinkProvider = new CookComputing.XmlRpc.XmlRpcServerFormatterSinkProvider( serverSinkProps, null);
                _channel = new HttpChannel( config, null, serverSinkProvider);
                ChannelServices.RegisterChannel(_channel, false); 

                if(RemotingConfiguration.CustomErrorsMode != CustomErrorsModes.Off)
                    RemotingConfiguration.CustomErrorsMode = CustomErrorsModes.Off;

                _service = new PingBackService(upstacker);
                RemotingServices.Marshal(_service, "pingback", typeof(PingBackService));
            } catch( Exception ex) {
                Console.WriteLine( ex.Message);
            }
        }

        public void Stop()
        {
            try
            {
                if (_channel != null)
                {
                    ChannelServices.UnregisterChannel(_channel);
                    _channel = null;
                }
                if(_service != null)
                {
                    RemotingServices.Disconnect(_service);
                    _service = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
