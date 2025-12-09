using System;
using Microsoft.SPOT;
using SecretLabs.NETMF.Hardware.Netduino;
using System.IO.Ports;

namespace BioNex.NETMF
{
    public class Amulet : IDisposable
    {
        private SerialPortWrapper Port { get; set; }

        public Amulet( string port, BaudRate baudrate)
        {
            Port = new SerialPortWrapper( port, (int)baudrate);
            Port.Write( "test");
        }

        #region IDisposable Members

        public void Dispose()
        {
            Port.Dispose();
        }

        #endregion
    }
}
