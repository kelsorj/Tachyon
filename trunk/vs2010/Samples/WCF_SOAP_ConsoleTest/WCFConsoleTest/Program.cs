using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using BioNex.Shared.Utils;

namespace BioNex.Calculator
{
    // Define a service contract.
    [ServiceContract(Namespace = "http://BioNex.Calculator")]
    public interface ICalculator
    {
        [OperationContract]
        double Add(double n1, double n2);
        [OperationContract]
        double Subtract(double n1, double n2);
        [OperationContract]
        double Multiply(double n1, double n2);
        [OperationContract]
        double Divide(double n1, double n2);
    }

    // Service class that implements the service contract.
    // Added code to write output to the console window.
    public class CalculatorService : ICalculator
    {
        public double Add(double n1, double n2)
        {
            double result = n1 + n2;
            Console.WriteLine("Received Add({0},{1})", n1, n2);
            Console.WriteLine("Return: {0}", result);
            return result;
        }

        public double Subtract(double n1, double n2)
        {
            double result = n1 - n2;
            Console.WriteLine("Received Subtract({0},{1})", n1, n2);
            Console.WriteLine("Return: {0}", result);
            return result;
        }

        public double Multiply(double n1, double n2)
        {
            double result = n1 * n2;
            Console.WriteLine("Received Multiply({0},{1})", n1, n2);
            Console.WriteLine("Return: {0}", result);
            return result;
        }

        public double Divide(double n1, double n2)
        {
            double result = n1 / n2;
            Console.WriteLine("Received Divide({0},{1})", n1, n2);
            Console.WriteLine("Return: {0}", result);
            return result;
        }
    }
    class Program
    {
        static ServiceHost OpenWithElevationTest(Type service, string address_format, string hostname, int port)
        {
            // Step 1 of the address configuration procedure: Create a URI to serve as the base address.
            var base_address = new Uri(string.Format(address_format, hostname, port));

            ServiceHost host = null;
            try
            {
                host = CreateServiceHost(service, base_address);
                host.Open();
            }
            catch (AddressAccessDeniedException)
            {
                var urlacl = string.Format(address_format, "+", port);
                var process_args = string.Format(@"http add urlacl url={0} user=\Everyone", urlacl);
                new ElevatedProcessLauncher("netsh.exe", process_args);

                host.Abort();
                host = CreateServiceHost(service, base_address);
                host.Open();
            }
            return host;
        }

        static ServiceHost CreateServiceHost(Type service, params Uri[] uris)
        {
            // Step 2 of the hosting procedure: Create ServiceHost
            ServiceHost host = new ServiceHost(typeof(CalculatorService), uris);

            // Step 3 of the hosting procedure: Add a service endpoint.
            host.AddServiceEndpoint(typeof(ICalculator), new WSHttpBinding(), "Calculator");

            // Step 4 of the hosting procedure: Enable metadata exchange.
            ServiceMetadataBehavior smb = new ServiceMetadataBehavior() { HttpGetEnabled = true };
            host.Description.Behaviors.Add(smb);

            return host;
        }


        static void Main(string[] args)
        {

            string address_format = "http://{0}:{1}/";
            int port = 6667;
            string hostname = "localhost";

            ServiceHost host = null;
            try
            {
                // Step 5 of the hosting procedure: Start (and then stop) the service.
                host = OpenWithElevationTest(typeof(CalculatorService), address_format, hostname, port);

                Console.WriteLine("The Calculator service is ready.");
                Console.WriteLine("Press <ENTER> to terminate service.");
                Console.WriteLine();
                Console.ReadLine();

                // Close the ServiceHostBase to shutdown the service.
                host.Close();
            }
            catch (CommunicationException ce)
            {
                Console.WriteLine("An exception occurred: {0}", ce.Message);
                if (host != null)
                    host.Abort();
            }
        }
    }
}
