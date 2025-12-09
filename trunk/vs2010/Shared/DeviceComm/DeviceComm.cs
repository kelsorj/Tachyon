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
        string Read( string delimiter, int timeout_ms);
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
        public SerialConnection( string com_port, int baud, SerialConnection.Parity parity, int databits, SerialConnection.StopBits stopbits, Handshaking handshaking)
        {
            // convert to the name that the SerialPort class wants
            System.IO.Ports.Parity com_port_parity = (System.IO.Ports.Parity)Enum.Parse(typeof(Parity), parity.ToString());
            System.IO.Ports.StopBits com_port_stopbits = (System.IO.Ports.StopBits)Enum.Parse(typeof(StopBits), stopbits.ToString());
            _port = new SerialPort( com_port, baud, com_port_parity, databits, com_port_stopbits);
            _port.Handshake = (Handshake)Enum.Parse(typeof(Handshaking), handshaking.ToString());
            _port.Open();
        }

        public void Write( string data)
        {
            _port.Write( data);
        }

        /// <summary>
        /// Reads data from the serial port until it encounters delimiter.  Also throws
        /// an exception if the delimiter doesn't appear in the buffer before the
        /// timeout period (in seconds) elapses.
        /// </summary>
        /// <remarks>
        /// for SerialPort.ReadTo: This method reads a string up to the specified value.
        ///                        While the returned string does not include the value,
        ///                        the value is removed from the input buffer.
        /// </remarks>
        /// <exception cref="ResponseTimeoutException"
        /// <param name="delimiter"></param>
        /// <param name="timeout_seconds"></param>
        /// <returns></returns>
        public string Read( string delimiter, int timeout_ms)
        {
            DateTime start = DateTime.Now;
            // read on the port until we see the desired delimiter
            _port.ReadTimeout = timeout_ms;
            return _port.ReadTo( delimiter);
        }

        public void FlushInputBuffer()
        {
            _port.DiscardInBuffer();
        }

        #region IDisposable Members

        public void Dispose()
        {
            _port.Close();
        }

        #endregion
    }
}
