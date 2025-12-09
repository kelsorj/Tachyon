using System.Net;
using CookComputing.XmlRpc;
using System.Threading;
using System.Net.Sockets;
using System;

namespace xmlrpc_scratch
{
    class ServiceHandler
    {
        HttpListener _listener;
        ListenerService _service;

        public ServiceHandler(int port, ListenerService service)
        {
            _service = service;
            var url1 = string.Format("http://*:{0}/", port);
            System.Console.WriteLine(string.Format("service listening on {0}", url1));

            _listener = new HttpListener();
            _listener.Prefixes.Add(url1);
            _listener.Start();

            var result = _listener.BeginGetContext(new AsyncCallback(ListenerCallback), _listener);
            // listener closed when program exits
        }

        private void ListenerCallback(IAsyncResult result)
        {

            if (_listener == null) return;
            var context = _listener.EndGetContext(result);
            _service.ProcessRequest(context);

            _listener.BeginGetContext(new AsyncCallback(ListenerCallback), _listener);
        }
    }

    class Program
    {
        static string GetLocalIPAddress(string hostname)
        {
            var host = Dns.GetHostEntry(hostname);
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            throw new System.ArgumentOutOfRangeException(string.Format("can't determine ip address of hostname '{0}'", hostname));
        }

        public static void Main(string[] prefixes)
        {
            var name = Dns.GetHostName();
            var ip = GetLocalIPAddress(name);
            System.Console.WriteLine(string.Format("Server '{0}' on '{1}'", name, ip));

            var service1 = new ServiceHandler(11000, new StateNameService1());
            var service2 = new ServiceHandler(12000, new StateNameService2());

            while (!System.Console.KeyAvailable)
            {
                System.Console.Write("."); // show that we're running asynchronously
                Thread.Sleep(1000);
            }
        }        
    }
}