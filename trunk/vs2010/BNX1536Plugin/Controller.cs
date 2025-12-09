using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO.Ports;

namespace BioNex.BNX1536Plugin
{
    public abstract class IController
    {
        public abstract void Connect( string com_port);
        public abstract void Close();
        public abstract string StartProgram( int program_number);
        public abstract string StartServiceProgram( int program_number);
        public abstract string QueryStatus();
        public abstract bool Connected { get; protected set; }
        public void Log( string message)
        {
            Debug.WriteLine( message);
        }
    }

    public class SimulationController : IController
    {
        #region Controller Members
        public override bool Connected { get; protected set; }

        public override void Connect(string com_port)
        {
            Log( String.Format( "connected to port {0}", com_port));
            Connected = true;
        }

        public override void Close()
        {
            Log( "closed");
            Connected = false;
        }

        public override string StartProgram(int program_number)
        {
            Log( String.Format( "started program #{0}, sending {1} to device", program_number, Commands.GetProgramNumberCommandString( program_number)));
            Thread.Sleep( 2000);
            Log( String.Format( "program #{0} completed", program_number));
            return ">END";
        }

        public override string StartServiceProgram(int program_number)
        {
            Log( String.Format( "started service program #{0}, sending {1} to device", program_number, Commands.GetServiceProgramCommandString( program_number)));
            Thread.Sleep( 2000);
            Log( String.Format( "service program #{0} completed", program_number));
            return ">END";
        }

        public override string QueryStatus()
        {
            Log( String.Format( "querying status, sending {0} to device", Commands.Status));
            return ">OK1";
        }

        #endregion
    }

    public partial class Controller : IController
    {
        private SerialPort _comport;
        private object _lock;
        public override bool Connected { get; protected set; }

        public Controller()
        {
            InitializeResponses();
            _lock = new object();
        }

        public override void Connect( string com_port)
        {
            _comport = new SerialPort( com_port, 9600, Parity.None, 8, StopBits.One);
            _comport.Open();
            Connected = true;
        }

        public override void Close()
        {
            _comport.Dispose();
            Connected = false;
        }

        public override string StartProgram( int program_number)
        {
            lock( _lock) {
                // select the program number
                string result = SendSerialCommand( Commands.GetProgramNumberCommandString( program_number));
                // send the start program command
                result = SendSerialCommand( Commands.Start);
                Thread.Sleep( 500); // let the device start running first
                // wait for the device to not be busy
                while( QueryStatus() == NormalResponses.Busy)
                    Thread.Sleep( 100);
                result = QueryStatus();
                return result;
            }
        }

        public override string StartServiceProgram( int program_number)
        {
            lock( _lock) {
                // select the program number
                SendSerialCommand( Commands.GetServiceProgramCommandString( program_number));
                // send the start program command
                return SendSerialCommand( Commands.Start);
            }
        }

        public override string QueryStatus()
        {
            if( !Connected)
                return "";
            try {
                lock( _lock) {
                    // send the query status command
                    string response = SendSerialCommand( Commands.Status);
                    return response;
                }
            } catch( Exception) {
                return "";
            }
        }

        /// <summary>
        /// formats the command properly for sending to the device, i.e. appends \n.
        /// do NOT lock the object in this call.
        /// </summary>
        /// <param name="command"></param>
        private string SendSerialCommand( string command)
        {
            _comport.DiscardInBuffer();
            _comport.Write( command + "\r");
            // get the response back from the device and check for errors
            // BNX1536 ends responses with 0x0A 0x0D instead of 0x0D 0x0A.  first time I've seen this!
            int timeout_ms = 1000;
            try {
                // get the response
                _comport.ReadTimeout = timeout_ms;
                string response = _comport.ReadTo( ResponseDelimiter);
                // check for error
                Match m = Regex.Match( response, @">E(\d\d)");
                if( m.Success)
                    throw new BioNex.Exceptions.CommandException( command, String.Format( "Command failed with error code: {0}", m.Groups[0]));
                return response;
            } catch( System.TimeoutException ex) {
                throw new BioNex.Exceptions.ResponseTimeoutException( command, timeout_ms, ex.Message);
            }
        }
    }
}
