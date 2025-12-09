using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Client.TestServices;
using System.ServiceModel;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var baseAddress = new Uri("http://localhost:6667/Calculator");
            var ea = new EndpointAddress(baseAddress);
            var wsb = new WSHttpBinding();

            using (var client = new CalculatorClient(wsb, ea))
            {
                var result = client.Add(100.0, 200.0);
                client.Close();
            }
        }
    }
}
