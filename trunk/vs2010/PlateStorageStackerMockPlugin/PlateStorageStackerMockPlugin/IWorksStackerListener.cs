using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using GalaSoft.MvvmLight.Messaging;
using System.Diagnostics;

namespace BioNex.PlateStorageStackerMockPlugin
{
    public class AlreadyListeningException : ApplicationException
    {
        public System.Net.IPAddress IPAddress { get; private set; }
        public int Port { get; private set; }

        public AlreadyListeningException( System.Net.IPAddress ip_address, int port)
            : base( "Already listening on this IP address and port number")
        {
            IPAddress = ip_address;
            Port = port;
        }
    }

    /// <summary>
    /// This is the TCP server used by the plugin controller to get commands from an IWorks driver.
    /// It also sends back error messages to the client, like when an invalid command is received,
    /// or if there is an error during command execution.
    /// </summary>
    /// <remarks>
    /// This post was helpful: http://www.switchonthecode.com/tutorials/csharp-tutorial-simple-threaded-tcp-server
    /// </remarks>
    public class IWorksStackerListener
    {
        private TcpListener _listener;
        private TcpClient _client;
        private Thread _listener_thread;
        private ManualResetEvent _stop_server_event;
        private bool AlreadyListening { get; set; }

        public void StartListening( System.Net.IPAddress ip_address, int port)
        {
            if( AlreadyListening)
                throw new AlreadyListeningException( ip_address, port);
            _stop_server_event = new ManualResetEvent( false);
            _listener = new TcpListener( ip_address, port);
            _listener_thread = new Thread( new ThreadStart( ListenForConnectionRequestThread));
            _listener_thread.Start();
            Messenger.Default.Register<CommandError>( this, HandleCommandError);
            AlreadyListening = true;
        }

        public void StopListening()
        {
            // setting the event will prevent the server from connecting to any more clients
            _stop_server_event.Set();
            _listener.Stop();
            Messenger.Default.Unregister<CommandError>( this, HandleCommandError);
            AlreadyListening = false;
        }

        private void ListenForConnectionRequestThread()
        {
            // start listening
            _listener.Start();
            // once connected, handle all of the commands coming in from the client in another thread
            // but keep listening.  this allows us to recover if the previous client loses its connection.
            ManualResetEvent[] wait_handles = new ManualResetEvent[1] { _stop_server_event };
            while( WaitHandle.WaitAny( wait_handles, 500) == WaitHandle.WaitTimeout) {
                try {
                    // block until a client connects
                    _client = _listener.AcceptTcpClient();
                    // create the command interpreter we're going to use
                    CommandInterpreter interpreter = new CommandInterpreter();
                    RunCommandInterpreterThread( _client, interpreter);
                } catch( SocketException ex) {
                    // client disconnected?
                }
            }
            AlreadyListening = false;
        }

        private void RunCommandInterpreterThread( TcpClient client, CommandInterpreter interpreter)
        {
            NetworkStream stream = client.GetStream();
            stream.ReadTimeout = System.Threading.Timeout.Infinite; // infinite wait
            const int max_retries = 10;
            int retry_count = 0;
            // a 1k buffer seems like more than enough
            const int buffer_size = 1024;
            byte[] buffer = new byte[buffer_size];
            int bytes_read;

            ManualResetEvent[] wait_handles = new ManualResetEvent[1] { _stop_server_event };
            while( WaitHandle.WaitAny( wait_handles, 500) == WaitHandle.WaitTimeout) {
                try {
                    //if( !client.GetStream().DataAvailable)
                    //  continue;
                    bytes_read = stream.Read( buffer, 0, buffer_size);
                    if( bytes_read > 0) {
                        interpreter.AddToQueue( System.Text.Encoding.ASCII.GetString( buffer, 0, bytes_read));
                        try {
                            Command c = interpreter.GetNextCommand();
                            Messenger.Default.Send<Command>( c);
                        } catch( InvalidOperationException) {
                            // do nothing since we don't have a valid command
                        }
                    } else {
                        // this means that the client disconnected!
                        break;
                    }
                } catch( System.IO.IOException ex) {
                    // stop listening and just let the client reconnect
                    break;
                } catch( ObjectDisposedException) {
                    // failure to read from the network.  try again
                    // only as long as we have retries remaining
                    if( retry_count++ >= max_retries)
                        break;
                }
            }
        }

        private void HandleCommandError( CommandError error)
        {
            Debug.WriteLine( "sending error back to client: " + error.ToString());
            // notify the client of a command error
            NetworkStream stream = _client.GetStream();
            byte[] data = System.Text.Encoding.ASCII.GetBytes( error.ToString());
            stream.Write( data, 0, data.Length);
        }
    }
}
