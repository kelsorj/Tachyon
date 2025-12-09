using System;
using System.Threading;
using BioNex.Shared.TeachpointServer;

namespace TeachpointServerTestApp
{
    class Program
    {

        class TeachpointServiceTestImpl : TeachpointService
        {
            public override string[] GetDeviceNames() { return null; }
            public override string[] GetTeachpointNames(string device_name) { return null; }
            public override XmlRpcTeachpoint GetXmlRpcTeachpoint(string device_name, string teachpoint_name) { return new XmlRpcTeachpoint(); }
        }
        
        static void Main(string[] args)
        {
            Console.WriteLine("Serving fake tp server on http://localhost:12345/test_teachpoints\n\nPress any key to exit");
            var server = new TeachpointServer(new TeachpointServiceTestImpl(), "test", 12345);

            while (!Console.KeyAvailable)
                Thread.Sleep(0);

            server.Stop();
        }
    }
}
