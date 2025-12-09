using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace FezConnectPcServer
{
    class Program
    {
        static void Main(string[] args)
        {
            IPAddress ip = IPAddress.Parse( "192.168.2.100");
            IPEndPoint endpoint = new IPEndPoint( ip, 12345);
            Socket socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind( endpoint);
            IPEndPoint local_endpoint = (IPEndPoint)socket.LocalEndPoint;

            socket.Listen( 1);
            AutoResetEvent done = new AutoResetEvent( false);

            while( true) {
                Console.WriteLine( "Waiting for connection");
                socket.BeginAccept( 4, Connect, socket);
                done.WaitOne();
            }
        }

        private static void Connect( IAsyncResult iar)
        {
            Console.WriteLine( "Connected!");
            Socket temp_socket = (Socket)iar.AsyncState;
            Socket socket = temp_socket.EndAccept( iar);

        }
    }
}
