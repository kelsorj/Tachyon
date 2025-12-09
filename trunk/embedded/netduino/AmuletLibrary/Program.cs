using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using System.IO.Ports;
using RFIDLibrary;
using BioNex.NETMF;

namespace AmuletLibrary
{
    public class Program
    {
        private static SerialPort Port { get; set; }

        public static void Main()
        {
            Amulet amulet = new Amulet( SerialPorts.COM2, BaudRates.Baud115200);
            Parallax28440 rfid = new Parallax28440( SerialPorts.COM1);
            amulet.Dispose();
            while( true) {
                Thread.Sleep( 10000);
            }
            rfid.Dispose();
        }
    }
}
