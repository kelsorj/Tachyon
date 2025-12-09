using System;
using System.Text;
using System.Threading;
using System.IO;
using BioNex.Shared.Utils;

namespace SimpleConsoleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            int i = 0;
            // note -- '*' in prefix means match only if another prefix doesn't match (e.g. http://*:80/tests/ will fail if http://*:80/ has a reservation)
            //         '+' in prefix means match always
            var uri = "http://+:80/tests/";
            var server = new SimpleHttpServer(uri, (Stream outputStream) =>
                {
                    var source = string.Format("<HTML><BODY> Hello world! {0}</BODY></HTML>", ++i);
                    var bytes = Encoding.UTF8.GetBytes(source);
                    var stream = new MemoryStream(bytes);
                    stream.CopyTo(outputStream);
                }
            );

            Console.WriteLine("Simple web server listening on '{0}'", uri);
            Console.WriteLine("Press a key to exit");
            while (!Console.KeyAvailable)
                Thread.Sleep(0);

            server.Stop();
        }
    }
}
