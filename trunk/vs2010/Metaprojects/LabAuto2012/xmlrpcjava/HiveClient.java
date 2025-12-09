package BioNex;

import org.apache.xmlrpc.client.*;
import java.util.Vector;
import java.net.URL;

public class HiveClient implements HiveClientInterface
{
    private String _host;
    private int _port;
    private XmlRpcClientConfigImpl _config;
    private XmlRpcClient _client;

    public void Init(String host, int port) throws HiveClientException
    {
        try
        {
            _host = host;
            _port = port;

            URL url = new URL("http://" + host + ":" + Integer.toString(port) + "/hivedata");

            _config = new XmlRpcClientConfigImpl();
            _config.setServerURL(url);
            _client = new XmlRpcClient();
            _client.setConfig(_config);
        }
        catch(java.net.MalformedURLException e)
        {
            throw new HiveClientException(e);
        }
    }

    public void AddWorkSet(String workset_xml) throws HiveClientException
    {
        String[] sparams = {workset_xml};
        Vector<String> params = new Vector<String>(java.util.Arrays.asList(sparams));
        ExecuteCall("BioNex.HiveRpc.IHiveData.AddWorkSet", params);
    }

    public void SetPlateFate(String barcode, String fate) throws HiveClientException
    {
        String[] sparams = {barcode, fate};
        Vector<String> params = new Vector<String>(java.util.Arrays.asList(sparams));
        ExecuteCall("BioNex.HiveRpc.IHiveData.SetPlateFate", params);
    }

    public void EndBatch(String fate) throws HiveClientException
    {
        String[] sparams = {fate};
        Vector<String> params = new Vector<String>(java.util.Arrays.asList(sparams));
        ExecuteCall("BioNex.HiveRpc.IHiveData.EndBatch", params);
    }

    public void Ping() throws HiveClientException
    {
        Vector params = new Vector();
        ExecuteCall("BioNex.HiveRpc.IHiveData.Ping", params);
    }

    private void ExecuteCall(String method, Vector params) throws HiveClientException
    {
        if(_client == null)
            throw new HiveClientException("Client object is null, did you call Init?");
        try
        {
            Object result = _client.execute(method, params);
            System.out.println( method + " success, result: '" + result + "'");
        }
        catch(org.apache.xmlrpc.XmlRpcException e)
        {
            throw new HiveClientException(e);
        }
    }

    public static void main(String[] args)
    {
        System.out.println("testing client...");
        try{
            HiveClient client = new HiveClient();
            client.Init("localhost", 5678);
            client.Ping();
            client.SetPlateFate("Source1", "Purge");
            client.SetPlateFate("Source2", "Purge");
            client.EndBatch("Purge");

            client.AddWorkSet("TODO - put a real workset here");
        }
        catch(Exception e){
            System.out.println("xmlrpc call failed :" + e);
        }
        System.out.println("finished!");
    }
}
