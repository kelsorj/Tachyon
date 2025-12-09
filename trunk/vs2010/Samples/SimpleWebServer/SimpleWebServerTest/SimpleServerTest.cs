using System;
using System.Text;
using System.IO;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BioNex.Shared.Utils;

namespace SimpleWebServerTest
{
    [TestClass]
    public class SimpleServerTest
    {
        static string source = "<HTML><BODY> Hello world!</BODY></HTML>";
        static SimpleHttpServer server;

        [ClassInitialize]
        public static void setUp(TestContext context)
        {
            var bytes = Encoding.UTF8.GetBytes(source);

            var uri = "http://+:80/tests/";
            server = new SimpleHttpServer(uri, (Stream outputStream) =>
                {
                    new MemoryStream(bytes).CopyTo(outputStream);
                },
                true
            );
        }

        [ClassCleanup]
        public static void tearDown()
        {
            server.Stop();
        }

        [TestMethod]
        public void TestServer()
        {
            // Now request the page via http
            var url = "http://localhost/tests/";
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            var response = request.GetResponse();
            var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            var result = reader.ReadToEnd();
            reader.Close();
            response.Close();


            StringAssert.Equals(result, source);
        }

    }
}
