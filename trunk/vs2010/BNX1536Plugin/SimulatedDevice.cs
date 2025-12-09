using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;

namespace BioNex.BNX1536Plugin
{
    internal class SimulatedDevice
    {
        private SerialPort _port;
        private Thread _listener_thread;
        private ManualResetEvent _stop_listening_event;
        private ManualResetEvent _listener_exited;

        public void Connect( string port_name)
        {
            // we could have called Connect twice without calling Close first
            if( _port != null)
                _port.Close();
            _port = new SerialPort( port_name, 9600, Parity.None, 8, StopBits.One);
            _port.Handshake = Handshake.None;
            _port.Open();
            // set up the listener thread
            _listener_thread = new Thread( ListenerThread);
            _stop_listening_event = new ManualResetEvent( false);
            _listener_exited = new ManualResetEvent( false);
            _listener_thread.Start();
        }

        public void Close()
        {
            // set the stop listening event and then wait until the thread exits
            _stop_listening_event.Set();
            _listener_exited.WaitOne( -1);
            _port.Close();
        }

        public void ListenerThread()
        {
            bool done_processing = false;
            string buffer = String.Empty;
            do {
                // read data coming in on the serial port and append to the buffer
                buffer += _port.ReadExisting();
                if( buffer.Length >= 4 && buffer.IndexOfAny( new char[] { '\r', '\n' }) != -1) {
                    string[] commands = ParseCommandsInBuffer( ref buffer);
                    foreach( string command in commands)
                        HandleCommand( command);
                }
                Thread.Sleep( 100);
            } while( !_stop_listening_event.WaitOne( 0) || !done_processing);
            _listener_exited.Set();
        }

        private void HandleCommand( string command)
        {
            // try to handle the simple commands first
            switch( command) {
                case Commands.Start:
                    HandleStartCommand();
                    break;
                case Commands.Status:
                    HandleStatusCommand();
                    break;
                case Commands.ToggleStop:
                    HandleStopCommand();
                    break;
                default:
                    break;
            }
            // handle select program and select service program separately
            if( command.Length == 4 && command.Substring( 0, 3) == "#PA") {
                // #PAn, so n = program number
                HandleSelectServiceProgram( int.Parse(command.Substring( 3, 1)));
            } else if( command.Length == 4 && command.Substring( 0, 2) == "#P") {
                // #Pnn, so nn = program number
                HandleSelectProgram( int.Parse( command.Substring( 2, 2)));
            }
        }

        public static string[] ParseCommandsInBuffer( ref string buffer)
        {
            // commands should have a /r, /n, or /r/n at the end
            // do some trickery to make parsing a little simpler -- replace
            // \r\n with \n, then replace \n with \r, and now all commands
            // should be separated only by \r
            buffer = buffer.Replace( "\r\n", "\n");
            buffer = buffer.Replace( '\n', '\r');
            // where's the last \r?  strip off the contents of buffer up to
            // and including that \r, and leave the rest of buffer alone
            // so that the thread can keep appending to the apparently
            // incomplete command that was in the buffer when it was last read
            int pos = buffer.LastIndexOf( '\r');
            // note that I don't use a pos + 1, because I don't want an extra
            // "command" (which would be blank) in the resultant array of commands
            string command_string = buffer.Substring( 0, pos);
            // overwrite buffer with remaining contents
            buffer = buffer.Remove( 0, pos + 1);
            return command_string.Split( '\r');
        }

        private void HandleStartCommand()
        {
            Debug.WriteLine( "handling start command");
            _port.Write( ">OK2\r\n");
            Thread.Sleep( 1000);
            _port.Write( ">END\r\n");
        }

        private void HandleStatusCommand()
        {
            Debug.WriteLine( "handling status command");
            _port.Write( ">OK1\r\n");
        }

        private void HandleStopCommand()
        {
            Debug.WriteLine( "handling stop command");
            _port.Write( ">OK2\r\n");
        }

        private void HandleSelectServiceProgram( int program_number)
        {
            Debug.WriteLine( "handling select service program command");
            //! \todo simulator really needs to use the state machine to keep track of valid responses
            if( program_number < 1 || program_number > 4)
                _port.Write( ">E02\r\n");
            else
                _port.Write( ">OK2");
        }

        private void HandleSelectProgram( int program_number)
        {
            Debug.WriteLine( "handling select program command");
            //! \todo simulator really needs to use the state machine to keep track of valid responses
            if( program_number < 1 || program_number > 99)
                _port.Write( ">E02\r\n");
            else
                _port.Write( ">OK2");
        }
    }
}
