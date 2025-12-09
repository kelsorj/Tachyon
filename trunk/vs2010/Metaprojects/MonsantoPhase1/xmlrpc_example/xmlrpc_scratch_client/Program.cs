using System.Threading;
using System.Net;
using CookComputing.XmlRpc;

namespace xmlrpc_scratch_client
{

    //[XmlRpcUrl("http://127.0.0.1:11000/index/")]
    public interface IStateName1 : IXmlRpcProxy
    {
        [XmlRpcMethod("examples.getStateName1")]
        string GetStateName(int stateNumber);
    }

    //[XmlRpcUrl("http://127.0.0.1:12000/index/")]
    public interface IStateName2 : IXmlRpcProxy
    {
        [XmlRpcMethod("examples.getStateName2")]
        string GetStateName(int stateNumber);
    }

    class Program
    {
        static void Main(string[] args)
        {
            var name = Dns.GetHostName();
            System.Console.WriteLine(string.Format("Client: {0}", name));

            var proxy1 = XmlRpcProxyGen.Create<IStateName1>();
            proxy1.Url = "http://127.0.0.1:11000/";
            var ret1 = proxy1.GetStateName(1);
            System.Console.WriteLine(ret1);

            var proxy2 = XmlRpcProxyGen.Create<IStateName2>();
            proxy2.Url = "http://127.0.0.1:12000/";
            var ret2 = proxy2.GetStateName(1);
            System.Console.WriteLine(ret2);

            while (!System.Console.KeyAvailable)
                Thread.Sleep(0);
        }
    }
}
