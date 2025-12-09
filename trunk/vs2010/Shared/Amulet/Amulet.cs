using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;

namespace BioNex.Shared.Amulet
{
    public class Amulet
    {
        private SerialPort _port { get; set; }
        private Object _lock = new Object();
        private string _buffer = "";
        private Thread _command_thread { get; set; }
        private AutoResetEvent _stop_command_parsing { get; set; }
        private Encoding _extended_ascii { get; set; }

        public class RamArray<Type>
        {
            private List<Type> _values { get; set; }

            public RamArray( Type default_value)
            {
                _values = Enumerable.Repeat( (Type)default_value, 256).ToList();
            }

            public Type this[ byte index]
            {
                get {
                    Debug.Assert( index >= 0 && index <= 255);
                    return _values[index];
                }
                set {
                    _values[index] = value;
                }
            }
        }

        public RamArray<byte> RamBytes { get; set; }
        public RamArray<short> RamWords { get; set; }
        public RamArray<string> RamStrings { get; set; }

        public static class MasterCommands
        {
            public const byte GetByte = 0xD0;
            public const byte GetWord = 0xD1;
            public const byte GetString = 0xD2;
            public const byte GetLabel = 0xD3;
            public const byte GetRpc = 0xD4;
            public const byte SetByte = 0xD5;
            public const byte SetWord = 0xD6;
            public const byte SetString = 0xD7;
            public const byte InvokeRpc = 0xD8;
            public const byte GetByteArray = 0xDD;
            public const byte GetWordArray = 0xDE;
            public const byte SetByteArray = 0xDF;
            public const byte SetWordArray = 0xF2;
        }

        // the PC must respond to these events when it is acting as the slave
        public event EventHandler GetByteRequested;
        public event EventHandler GetWordRequested;
        public event EventHandler GetStringRequested;
        public event EventHandler GetLabelRequested;
        public event EventHandler GetRpcRequested;
        public event EventHandler SetByteRequested;
        public event EventHandler SetWordRequested;
        public event EventHandler SetStringRequested;
        public event EventHandler InvokeRpcRequested;
        public event EventHandler GetByteArrayRequested;
        public event EventHandler GetWordArrayRequested;
        public event EventHandler SetByteArrayRequested;
        public event EventHandler SetWordArrayRequested;

        public byte[] AllMasterCommands = new byte[] { MasterCommands.GetByte, MasterCommands.GetWord, MasterCommands.GetString,
                                                     MasterCommands.GetLabel, MasterCommands.GetRpc, MasterCommands.SetByte,
                                                     MasterCommands.SetWord, MasterCommands.SetString, MasterCommands.InvokeRpc,
                                                     MasterCommands.GetByteArray, MasterCommands.GetWordArray,
                                                     MasterCommands.SetByteArray, MasterCommands.SetWordArray };

        /// <summary>
        /// These are the number of bytes expected if we are acting as the slave and getting a command from the master.
        /// 0 means that we should look for a null terminator.
        /// </summary>
        public Dictionary<byte,int> MasterCommandBytes = new Dictionary<byte,int> { { MasterCommands.GetByte, 2 },
                                                                                      { MasterCommands.GetWord, 2 },
                                                                                      { MasterCommands.GetString, 2 },
                                                                                      { MasterCommands.GetLabel, 2 },
                                                                                      { MasterCommands.GetRpc, 2 },
                                                                                      { MasterCommands.SetByte, 4 },
                                                                                      { MasterCommands.SetWord, 6 },
                                                                                      { MasterCommands.SetString, 0 },
                                                                                      { MasterCommands.InvokeRpc, 2 },
                                                                                      { MasterCommands.GetByteArray, 2 },
                                                                                      { MasterCommands.GetWordArray, 2 },
                                                                                      { MasterCommands.SetByteArray, 0 },
                                                                                      { MasterCommands.SetWordArray, 0 } };

        public Dictionary<byte,byte[]> CommandResponseCache { get; set; }

        public static class SlaveResponses
        {
            public const byte GetByte = 0xE0;
            public const byte GetWord = 0xE1;
            public const byte GetString = 0xE2;
            public const byte GetLabel = 0xE3;
            public const byte GetRpc = 0xE4;
            public const byte SetByte = 0xE5;
            public const byte SetWord = 0xE6;
            public const byte SetString = 0xE7;
            public const byte InvokeRpc = 0xE8;
            public const byte GetByteArray = 0xED;
            public const byte GetWordArray = 0xEE;
            public const byte SetByteArray = 0xEF;
            public const byte SetWordArray = 0xF3;    // yes, this is supposed to be F3, not E2 like you might think!
        }

        public byte[] AllSlaveResponses = new byte[] { SlaveResponses.GetByte, SlaveResponses.GetWord, SlaveResponses.GetString,
                                                      SlaveResponses.GetLabel, SlaveResponses.GetRpc, SlaveResponses.SetByte,
                                                      SlaveResponses.SetWord, SlaveResponses.SetString, SlaveResponses.InvokeRpc,
                                                      SlaveResponses.GetByteArray, SlaveResponses.GetWordArray,
                                                      SlaveResponses.SetByteArray, SlaveResponses.SetWordArray };

        /// <summary>
        /// These are the number of bytes expected if we are getting a response back from the slave.
        /// 0 means that we need to look for a null terminator.
        /// </summary>
        public Dictionary<byte,int> SlaveResponseBytes = new Dictionary<byte,int> { { SlaveResponses.GetByte, 5 },
                                                                                      { SlaveResponses.GetWord, 7 },
                                                                                      { SlaveResponses.GetString, 0 },
                                                                                      { SlaveResponses.GetLabel, 0 },
                                                                                      { SlaveResponses.GetRpc, 0 },
                                                                                      { SlaveResponses.SetByte, 5 },
                                                                                      { SlaveResponses.SetWord, 7 },
                                                                                      { SlaveResponses.SetString, 0 },
                                                                                      { SlaveResponses.InvokeRpc, 3 },
                                                                                      { SlaveResponses.GetByteArray, 0 },
                                                                                      { SlaveResponses.GetWordArray, 0 },
                                                                                      { SlaveResponses.SetByteArray, 0 },
                                                                                      { SlaveResponses.SetWordArray, 0 } };

        public class AmuletException : Exception
        {
            public AmuletException( string message) : base(message)
            {
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="portname"></param>
        /// <param name="baud"></param>
        /// <param name="ack_all_bytes">Whether or not the PC should respond with all bytes to incoming commands, or expect to receive all bytes when sending a command.</param>
        public Amulet( string portname, int baud=115200)
        {
            _stop_command_parsing = new AutoResetEvent( false);
            _port = new SerialPort( portname, baud, Parity.None, 8, StopBits.One);
            _port.Encoding = _extended_ascii = Encoding.GetEncoding("iso-8859-1");
            _port.DataReceived += new SerialDataReceivedEventHandler(_port_DataReceived);
            RamBytes = new RamArray<byte>( 0);
            RamWords = new RamArray<short>( 0);
            RamStrings = new RamArray<string>( "");
            CommandResponseCache = new Dictionary<byte,byte[]>();
        }

        private void _port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try {
                SerialPort port = sender as SerialPort;
                lock( _lock) {
                    _buffer += port.ReadExisting();
                }
            } catch( Exception ex) {
                Debug.WriteLine( "Error in serial data received event handler: " + ex.Message);
            }
        }

        private enum Commands { Initialize=0xB5, }

        private void SerialCommandParserThreadHandler()
        {
            while( !_stop_command_parsing.WaitOne( 10)) {
                if( _buffer.Length == 0)
                    continue;

                // look for the first command byte, either a send or receive command
                bool amulet_as_master = (_buffer[0] & 0xD0) != 0;

                int pos = -1;
                if( amulet_as_master)
                    pos = _buffer.IndexOfAny( _extended_ascii.GetChars( AllMasterCommands));
                else
                    pos = _buffer.IndexOfAny( _extended_ascii.GetChars( AllSlaveResponses));
                // if the first position isn't a response byte, or if the response byte isn't even present,
                // then we assume this is garbage.
                if( pos == -1) {
                    _buffer = "";
                    continue;
                }

                // clear all bytes before pos, since it's "garbage"
                _buffer = _buffer.Remove( 0, pos);
                byte response_byte = (byte)_buffer[0];
                // 0 means we have to look for a null terminator.  Otherwise, we have to wait
                // for num_expected_bytes number of bytes in the buffer
                int num_expected_bytes = 0;
                if( amulet_as_master)
                    num_expected_bytes = MasterCommandBytes[response_byte];
                else
                    num_expected_bytes = SlaveResponseBytes[response_byte];
                // data is as expected, so wait until we get the desired number of bytes
                if( num_expected_bytes == 0) {
                    // string or array -- wait for a null character
                    while( (pos = _buffer.IndexOf( (char)0)) == -1)
                        Thread.Sleep( 10);
                    // setting num_expected_bytes to account for null character now, so following
                    // parsing works like it does for fixed-length command responses.
                    num_expected_bytes = pos;
                } else {
                    // wait for the correct number of bytes
                    while( _buffer.Length < num_expected_bytes)
                        Thread.Sleep( 10);
                }
                
                // if we are acting as a slave (Amulet is master and sending commands, then we need to
                // send a command response back to the Amulet so it doesn't keep resending the command
                if( amulet_as_master)
                    SendResponseToMaster( response_byte, _extended_ascii.GetBytes( _buffer.Substring( 1, num_expected_bytes)));
                else
                    CacheResponseAndFireEvent( response_byte, _extended_ascii.GetBytes( _buffer.Substring( 1, num_expected_bytes)));
                // strip off everything before it -- remember to include the command, as well as the command variable
                _buffer = _buffer.Remove( 0, num_expected_bytes + 1);
                Debug.WriteLine( "Left in buffer: " + _buffer);
            }
        }

        private void CacheResponseAndFireEvent( byte command, byte[] command_response)
        {
            CommandResponseCache[command] = command_response;
        }

        private void SendResponseToMaster( byte command_id, byte[] command_response)
        {
            byte response_id = (byte)(command_id + 0x10); // response byte is 0xE?, where command byte is 0xD?
            // it's up to the client application to handle these requests from the Amulet via events
            switch( command_id) {
                case MasterCommands.GetByte:
                    if( GetByteRequested != null)
                        GetByteRequested( this, new AmuletCommandEventArgs( response_id, command_response));
                    else
                        UnhandledRequest( response_id, command_response);
                    break;
                case MasterCommands.GetWord:
                    if( GetWordRequested != null)
                        GetWordRequested( this, new AmuletCommandEventArgs( response_id, command_response));
                    else
                        UnhandledRequest( response_id, command_response);
                    break;
                case MasterCommands.GetString:
                    if( GetStringRequested != null)
                        GetStringRequested( this, new AmuletCommandEventArgs( response_id, command_response));
                    else
                        UnhandledRequest( response_id, command_response);
                    break;
                case MasterCommands.GetLabel:
                    if( GetLabelRequested != null)
                        GetLabelRequested( this, new AmuletCommandEventArgs( response_id, command_response));
                    else
                        UnhandledRequest( response_id, command_response);
                    break;
                case MasterCommands.GetRpc:
                    if( GetRpcRequested != null)
                        GetRpcRequested( this, new AmuletCommandEventArgs( response_id, command_response));
                    else
                        UnhandledRequest( response_id, command_response);
                    break;
                case MasterCommands.SetByte:
                    if( SetByteRequested != null)
                        SetByteRequested( this, new AmuletCommandEventArgs( response_id, command_response));
                    else
                        UnhandledRequest( response_id, command_response);
                    break;
                case MasterCommands.SetWord:
                    if( SetWordRequested != null)
                        SetWordRequested( this, new AmuletCommandEventArgs( response_id, command_response));
                    else
                        UnhandledRequest( response_id, command_response);
                    break;
                case MasterCommands.SetString:
                    if( SetStringRequested != null)
                        SetStringRequested( this, new AmuletCommandEventArgs( response_id, command_response));
                    else
                        UnhandledRequest( response_id, command_response);
                    break;
                case MasterCommands.InvokeRpc:
                    if( InvokeRpcRequested != null)
                        InvokeRpcRequested( this, new AmuletCommandEventArgs( response_id, command_response));
                    else
                        UnhandledRequest( response_id, command_response);
                    break;
                case MasterCommands.GetByteArray:
                    if( GetByteArrayRequested != null)
                        GetByteArrayRequested( this, new AmuletCommandEventArgs( response_id, command_response));
                    else
                        UnhandledRequest( response_id, command_response);
                    break;
                case MasterCommands.GetWordArray:
                    if( GetWordArrayRequested != null)
                        GetWordArrayRequested( this, new AmuletCommandEventArgs( response_id, command_response));
                    else
                        UnhandledRequest( response_id, command_response);
                    break;
                case MasterCommands.SetByteArray:
                    if( SetByteArrayRequested != null)
                        SetByteArrayRequested( this, new AmuletCommandEventArgs( response_id, command_response));
                    else
                        UnhandledRequest( response_id, command_response);
                    break;
                case MasterCommands.SetWordArray:
                    if( SetWordArrayRequested != null)
                        SetWordArrayRequested( this, new AmuletCommandEventArgs( response_id, command_response));
                    else
                        UnhandledRequest( response_id, command_response);
                    break;
            }
        }
        
        private void UnhandledRequest( byte response_byte, byte[] variable)
        {
            /*
            // copy over response ID and the variable ID
            byte[] response = new byte[3];
            response[0] = response_byte;
            System.Buffer.BlockCopy( variable, 0, response, 1, 2);
            // now based on the response ID, we need to decide what else to send along with the data
            switch( response_byte) {
                 // first handle the commands that don't have any values
                case SlaveResponses.InvokeRpc:
                    break;
                // byte returns
                case SlaveResponses.GetByte:
                case SlaveResponses.SetByte:
                    break;
                // word returns
                case SlaveResponses.GetWord:
                case SlaveResponses.SetWord:
                    break;
                // null-terminated returns
                case SlaveResponses.GetString:
                case SlaveResponses.GetLabel:
                case SlaveResponses.GetRpc:
                case SlaveResponses.SetString:
                case SlaveResponses.GetByteArray:
                case SlaveResponses.GetWordArray:
                    break;
            }
             */

            // since the command isn't implemented, just ACK so the Amulet doesn't keep sending data
            _port.Write( new byte[] { (byte)0xF0 }, 0, 1);
        }

        public void Connect()
        {
            _port.Open();
            _port.DiscardInBuffer();
            _command_thread = new Thread( SerialCommandParserThreadHandler);
            _command_thread.Name = "Amulet serial command parser";
            _command_thread.IsBackground = true;
            _command_thread.Start();
        }

        public void Close()
        {
            _stop_command_parsing.Set();
            _command_thread.Join();
            lock( _lock) {
                _port.Close();
            }
        }

        public void SetByte( byte byte_index, byte byte_value)
        {
            string index = Utils.ByteToHexString( byte_index);
            string value = Utils.ByteToHexString( byte_value);
            byte[] command = { (byte)MasterCommands.SetByte, (byte)index[0], (byte)index[1], (byte)value[0], (byte)value[1] };
            lock( _lock) {
                _port.DiscardInBuffer();
                _port.Write( command, 0, 5);
            }
            // look for response
        }

        public void SetWord( byte word_index, ushort word_value)
        {
            string index = Utils.ByteToHexString( word_index);
            string value = Utils.WordToHexString( word_value);
            byte[] command = { MasterCommands.SetWord, (byte)index[0], (byte)index[1], (byte)value[0], (byte)value[1], (byte)value[2], (byte)value[3] };
            lock( _lock) {
                _port.Write( command, 0, 7);
            }
            // look for response
        }

        public void SetString( byte string_index, string string_value)
        {
            string index = Utils.ByteToHexString( string_index);
            byte[] command = { (byte)MasterCommands.SetString, (byte)index[0], (byte)index[1] };
            byte[] string_bytes = UTF8Encoding.UTF8.GetBytes( string_value.ToCharArray(), 0, string_value.Length);
            lock( _lock) {
                _port.Write( command, 0, 3);
                _port.Write( string_bytes, 0, string_value.Length);
                _port.Write( new byte[] {0}, 0, 1);
            }
        }

        public byte GetByte( byte var_index_0_based)
        {
            // need to convert var_index_0_based into a hex string
            // then we want to pass the first character, then the second after the GetByte command 
            string command_bytes = Utils.ByteToHexString( var_index_0_based);
            // here we will need to clear the receive buffer before sending the command
            //! \todo need to implement a response map that is updated by the listener thread
            lock( _lock) {
                _port.DiscardInBuffer();
                byte[] command = { (byte)MasterCommands.GetByte, (byte)command_bytes[0], (byte)command_bytes[1] };
                _port.Write( command, 0, 3);
                byte[] value;
                while( !CommandResponseCache.TryGetValue( (byte)MasterCommands.GetByte, out value))
                    Thread.Sleep( 0);
                return value.ByteArrayToByte();
            }
        }

        public ushort GetWord( byte var_index_0_based)
        {
            return 0;
        }

        /// <summary>
        /// When the Amulet sends a command request to the PC, it's acting as the master, and expects a response
        /// from the PC.  When the Amulet library sees the incoming request, it fires an event, and it is up to
        /// the client application to handle this event by calling RespondWith.
        /// </summary>
        /// <param name="response_byte"></param>
        /// <param name="response_variable"></param>
        /// <param name="response_value"></param>
        public void RespondWith( byte response_byte, byte[] response_variable, byte[] response_value)
        {
            // http://stackoverflow.com/questions/415291/best-way-to-combine-two-or-more-byte-arrays-in-c
            int response_length = 1 + response_variable.Length + (response_value != null ? response_value.Length : 0);
            byte[] response = new byte[response_length];
            response[0] = response_byte;
            System.Buffer.BlockCopy( response_variable, 0, response, 1, response_variable.Length);
            if( response_value != null)
                System.Buffer.BlockCopy( response_value, 0, response, 1 + response_variable.Length, response_value.Length);
            lock( _lock) {
                _port.Write( response, 0, response_length);
            }
        }
    }
}
