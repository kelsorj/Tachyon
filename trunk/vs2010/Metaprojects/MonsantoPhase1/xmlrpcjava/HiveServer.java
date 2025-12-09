package BioNex;

import java.net.InetAddress;

import org.apache.xmlrpc.common.TypeConverterFactoryImpl;
import org.apache.xmlrpc.server.PropertyHandlerMapping;
import org.apache.xmlrpc.server.XmlRpcServer;
import org.apache.xmlrpc.server.XmlRpcServerConfigImpl;
import org.apache.xmlrpc.webserver.WebServer;

public class HiveServer implements HiveServerInterface
{
    WebServer _server;
    int _port = 6789;

    public void Init(int port, HiveServerCallbackInterface callbacks)
    {
        _port = port;
        JemsImpl.Init(callbacks);
    }

    public void Start() throws BioNex.HiveServerException
    {
        try
        {
            System.out.println("Starting BioNex.HiveServer on port " + _port);
            _server = new WebServer(_port);
            XmlRpcServer xmlRpcServer = _server.getXmlRpcServer();
            PropertyHandlerMapping phm = new PropertyHandlerMapping();
            phm.addHandler("BioNex.GemsRpc.IGemsData", BioNex.JemsImpl.class);
            xmlRpcServer.setHandlerMapping(phm);
            XmlRpcServerConfigImpl serverConfig = (XmlRpcServerConfigImpl) xmlRpcServer.getConfig();
            serverConfig.setEnabledForExtensions(true);
            serverConfig.setContentLengthOptional(false);
            _server.start();
        }
        catch(org.apache.xmlrpc.XmlRpcException e)
        {
            throw new BioNex.HiveServerException(e);
        }
        catch(java.io.IOException e)
        {
            throw new BioNex.HiveServerException(e);
        }
    }

    public void Stop()
    {
        System.out.println("Stopping BioNex.HiveServer");
        _server.shutdown();
    }

    public static void main(String[] args)
    {
        try
        {
            System.out.println("server starting, press a key to exit...");
            HiveServer server = new HiveServer();
            BioNex.HiveServerCallbackInterface callback =
                new BioNex.HiveServerCallbackInterface()
                {
                    public void ReinventoryComplete( String hive_name, String cart_name, Object[] barcodes)
                    {
                        System.out.println("ReinventoryComplete on '" + hive_name + "' cart '" + cart_name + "' barcodes:");
                        for(Object barcode : barcodes)
                        {
                            System.out.println("'" + barcode + "'");
                        }
                    }
                    public void Ping()
                    {
                        System.out.println("Ping");
                    }
                };


            server.Init(6789, callback);
            server.Start();
            System.in.read();
            server.Stop();
        }
        catch(Exception e)
        {
            System.out.println("hive server failed :" + e);
        }
        System.out.println("finished!");
    }
}


