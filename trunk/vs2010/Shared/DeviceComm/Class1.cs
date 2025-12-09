using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;

namespace BioNex.DeviceComm
{

    /// <summary>
    /// Represents any physical communication connection, like Ethernet,
    /// serial, USB, etc.
    /// </summary>
    public interface Connection
    {
        void Write( string data);
        string Read();
    }

    /// <summary>
    /// A serial connection
    /// </summary>
    /// <remarks>
    /// MSDN documentation is found here: http://msdn.microsoft.com/en-us/library/system.io.ports.serialport.aspx
    /// </remarks>
    public class SerialConnection : Connection, IDisposable
    {
        private SerialPort _port;

        public enum Parity
        {
            None,
            Odd,
            Even,
            Mark,
            Space
        }

        public enum StopBits
        {
            None,
            One,
            Two,
            OnePointFive
        }

        public enum Handshaking
        {
            None,
            RequestToSend,
            RequestToSendXOnXOff,
            XOnXOff
        }

        /// <summary>
        /// Opens a connection to the port with the specified parameters
        /// </summary>
        /// <remarks>
        /// The .NET SerialPort class expects com ports to be identified as "COM##", instead of "#".
        /// </remarks>
        /// <param name="com_port"></param>
        /// <param name="baud"></param>
        /// <param name="parity"></param>
        /// <param name="databits"></param>
        /// <param name="stopbits"></param>
        public SerialConnection( int com_port, int baud, SerialConnection.Parity parity, int databits, SerialConnection.StopBits stopbits, Handshaking handshaking)
        {
            // convert to the name that the SerialPort class wants
            string com_port_name = String.Format( "COM{0}", com_port);
            System.IO.Ports.Parity com_port_parity = (System.IO.Ports.Parity)Enum.Parse(typeof(Parity), parity.ToString());
            System.IO.Ports.StopBits com_port_stopbits = (System.IO.Ports.StopBits)Enum.Parse(typeof(StopBits), stopbits.ToString());
            _port = new SerialPort( com_port_name, baud, com_port_parity, databits, com_port_stopbits);
            _port.Handshake = (Handshake)Enum.Parse(typeof(Handshaking), handshaking.ToString());
            _port.Open();
        }

        public void Write( string data)
        {
        }

        public string Read()
        {
            return "";
        }

        #region IDisposable Members

        public void Dispose()
        {
            _port.Close();
        }

        #endregion
    }
}
