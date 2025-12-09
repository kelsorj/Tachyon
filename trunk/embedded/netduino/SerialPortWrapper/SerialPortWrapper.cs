using System;
using Microsoft.SPOT;
using System.IO.Ports;
using System.Threading;
using System.Text;

namespace BioNex.NETMF
{
    public class SerialPortWrapper : IDisposable
    {
        private SerialPort port { get; set; }

        private const int bufferMax = 1024;
        private byte[] buffer = new Byte[bufferMax];
        private int bufferLength = 0;
        private const int ResponseTimeoutSeconds = 10;

        public SerialPortWrapper(string portName, int baudRate, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            port = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            port.ReadTimeout = 100;
            port.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);
            port.Open();
        }

        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (buffer)
            {
                int bytesReceived = port.Read(buffer, bufferLength, bufferMax - bufferLength);
                if (bytesReceived > 0)
                {
                    bufferLength += bytesReceived;
                    if (bufferLength >= bufferMax)
                        throw new ApplicationException("Buffer Overflow.  Send shorter lines, or increase lineBufferMax.");
                }
            }
        }

        public string ReadLine()
        {
            string line = "";

            lock (buffer)
            {
                //-- Look for Return char in buffer --
                for (int i = 0; i < bufferLength; i++)
                {
                    //-- Consider EITHER CR or LF as end of line, so if both were received it would register as an extra blank line. --
                    if (buffer[i] == '\r' || buffer[i] == '\n')
                    {
                        buffer[i] = 0; // Turn NewLine into string terminator
                        line = "" + new string(Encoding.UTF8.GetChars(buffer)); // The "" ensures that if we end up copying zero characters, we'd end up with blank string instead of null string.
                        //Debug.Print("LINE: <" + line + ">");
                        bufferLength = bufferLength - i - 1;
                        Array.Copy(buffer, i + 1, buffer, 0, bufferLength); // Shift everything past NewLine to beginning of buffer
                        break;
                    }
                }
            }

            return line;
        }

        public string ReadChars( int num_chars)
        {
            DateTime start = DateTime.Now;
            lock( buffer) {
                while( bufferLength < num_chars && (DateTime.Now - start).Seconds < ResponseTimeoutSeconds ) {
                    Thread.Sleep( 100);
                }
                if( (DateTime.Now - start).Seconds >= ResponseTimeoutSeconds)
                    throw new Exception( "Timed out while waiting for response");
                byte[] result = new byte[num_chars];
                Array.Copy( buffer, result, num_chars);
                Array.Copy( buffer, num_chars, buffer, 0, bufferLength);
                bufferLength -= 5;
                char[] char_result = new char[num_chars];
                Decoder decoder = Encoding.UTF8.GetDecoder();
                int bytes_used;
                int chars_used;
                bool completed;
                decoder.Convert( result, 0, num_chars, char_result, 0, num_chars, false, out bytes_used, out chars_used, out completed);
                return new String( char_result);
            }
        }

        public void Write( string line )
        {
            System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
            byte[] bytesToSend = encoder.GetBytes(line);
            port.Write(bytesToSend, 0, bytesToSend.Length);
        }

        public void WriteLine(string line)
        {
            Write( line + "\r");
        }

        #region IDisposable Members

        public void Dispose()
        {
            port.Close();
        }

        #endregion
    }
}
