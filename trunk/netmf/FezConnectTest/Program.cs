using System;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using GHIElectronics.NETMF.FEZ;
using GHIElectronics.NETMF.Net;
using GHIElectronics.NETMF.Net.NetworkInformation;
using GHIElectronics.NETMF.Net.Sockets;

namespace FezConnectTest
{
    public class Program
    {
        public static void Main()
        {
            byte[] ip = { 192, 168, 2, 20 };
            byte[] subnet = { 255, 255, 255, 0 };
            byte[] gateway = { 192, 168, 2, 2 };
            // 00261C is the Vendor ID
            // got this mac from http://www.macvendorlookup.com
            byte[] mac = { 0x00, 0x26, 0x1C, 0xF9, 0xA5, 0x1D };

            WIZnet_W5100.Enable( SPI.SPI_module.SPI1, (Cpu.Pin)FEZ_Pin.Digital.Di10, (Cpu.Pin)FEZ_Pin.Digital.Di7, true);
            NetworkInterface.EnableStaticIP( ip, subnet, gateway, mac);
            //NetworkInterface.EnableStaticDns( new byte[] { 192, 168, 2, 10 });

            Socket socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress destination = new IPAddress( new byte[] { 192, 168, 2, 100 });
            IPEndPoint destination_endpoint = new IPEndPoint( destination, 12345);

            byte[] bytes_to_send = new byte[] { 0x01, 0x44, 0x22, (byte)'R' };
            socket.SendTo( bytes_to_send, bytes_to_send.Length, SocketFlags.None, destination_endpoint);

            while (true)
            {
                // Sleep for 500 milliseconds
                Thread.Sleep(500);
            }
        }

    }
}
