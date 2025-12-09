using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using log4net;
using System.Runtime.Remoting.Messaging;
using BioNex.Shared.Utils;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Diagnostics;

[assembly:InternalsVisibleTo("GreenMachineUnitTests")]

namespace BioNex.GreenMachine.HardwareInterfaces
{
    public class TTSyringeRobot : IXyz
    {
        private ILog _log = LogManager.GetLogger( typeof( TTSyringeRobot));
        private ThreadedUpdates _updater;
        private IXyz.AxisInfo[] _axis_info;
        private object _serial_lock = new Object();

        public static class Commands
        {
            public static readonly string Initialize = "!99232071@@";
            public static readonly string HomeAxis = "!992330{0}00000000@@\r\n";
            public static readonly string QueryAxis = "!992120{0}@@\r\n"; // just add single character for axis ID -- X=2, Y=1, Z=4
            public static readonly string MoveAbsolute = "!992340{0}{1:0000}{2:0000}{3:00000000}@@\r\n";
            public static readonly string MoveRelativeX = "!9923502";
            public static readonly string MoveRelativeY = "!9923501";
            public static readonly string MoveRelativeZ = "!9923504";
            public static readonly string SetOutput = "!9924";
            public static readonly string QueryInput = "!9920B00100001@@";
            public static readonly string Terminate = "!9923807@@";
            public static readonly string EnableAxis = "!992320{0}{1}@@\r\n"; // just add single character for axis ID -- X=2, Y=1, Z=4
        }

        private SerialPort _port;

        public TTSyringeRobot( int port)
        {
            _port = new SerialPort( "COM" + port.ToString(), 9600, Parity.None, 8, StopBits.One);
            _updater = new ThreadedUpdates( "TTRobotUpdateThread", UpdateThread, 250);
            _axis_info = new IXyz.AxisInfo[3];
            for( int i=0; i<3; i++)
                _axis_info[i] = new IXyz.AxisInfo();
        }

        #region IXyz Members

        /// <summary>
        /// Opens the specified serial port
        /// </summary>
        /// <param name="port"></param>
        public override void Initialize()
        {
            lock( _serial_lock) {
                _port.Open();
            }
            _updater.Start();
        }

        public override void Close()
        {
            _updater.Stop();
            lock( _serial_lock) {
                _port.Close();
            }
        }

        private void UpdateThread()
        {
            // send axis query command to X, Y, and Z
            string x_response;
            string y_response;
            string z_response;
            lock( _serial_lock) {
                x_response = GetAxisResponse( Axes.X);
                y_response = GetAxisResponse( Axes.Y);
                z_response = GetAxisResponse( Axes.Z);
            }

            // parse response to get positions
            ParseAxisResponses( x_response, y_response, z_response);
            
            // query inputs

        }

        /// <summary>
        /// Gets the response string from the specific axis, for position and homing info
        /// </summary>
        /// <remarks>
        /// Remember to LOCK outside of this call!
        /// </remarks>
        /// <param name="axis"></param>
        /// <returns></returns>
        private string GetAxisResponse( Axes axis)
        {
            string command = String.Format( Commands.QueryAxis, axis == Axes.X ? "2" : axis == Axes.Y ? "1" : "4");
            _log.Debug( "GetAxisResponse: flushing serial port");
            _port.DiscardInBuffer();
            _log.Debug( "GetAxisResponse: writing " + command);
            _port.Write( command);
            _log.Debug( "GetAxisResponse: reading...");
            return _port.ReadTo( "\r\n");
        }

        private void ParseAxisResponses( string x, string y, string z)
        {
            double position_mm;
            bool enabled;
            bool homed;

            // only update if no parsing errors

            if( ParseAxisResponseHelper( x, out position_mm, out enabled, out homed)) {
                _axis_info[(int)Axes.X].PositionMM = position_mm;
                _axis_info[(int)Axes.X].Enabled = enabled;
                _axis_info[(int)Axes.X].Homed = homed;
            }

            if( ParseAxisResponseHelper( y, out position_mm, out enabled, out homed)) {
                _axis_info[(int)Axes.Y].PositionMM = position_mm;
                _axis_info[(int)Axes.Y].Enabled = enabled;
                _axis_info[(int)Axes.Y].Homed = homed;
            }

            if( ParseAxisResponseHelper( z, out position_mm, out enabled, out homed)) {
                _axis_info[(int)Axes.Z].PositionMM = position_mm;
                _axis_info[(int)Axes.Z].Enabled = enabled;
                _axis_info[(int)Axes.Z].Homed = homed;
            }
        }

        internal bool ParseAxisResponseHelper( string response, out double position_mm, out bool enabled, out bool homed)
        {
            position_mm = 0;
            enabled = false;
            homed = false;
            // e.g. #99212021C00000000002715AF\r\n
            Regex re = new Regex( @"#992120\d([0-9a-fA-F][0-9a-fA-F])[0-9a-fA-F]{6}([0-9a-fA-F]{8})([0-9a-fA-F]{2})");
            MatchCollection matches = re.Matches( response);
            if( matches.Count != 1) {
                _log.Info( "Failed overall regex match for axis response " + response);
                return false;
            }
            if( matches[0].Groups.Count != 4) {
                _log.Info( "Failed group matches for axis response " + response);
                return false;
            }

            Debug.Assert( matches[0].Groups.Count == 4, "Could not parse individual items from axis response");
            string status_string = matches[0].Groups[1].ToString();
            string position_string = matches[0].Groups[2].ToString();
            string checksum_string = matches[0].Groups[3].ToString();
            // convert status string into byte
            byte status = Convert.ToByte( status_string, 16);
            enabled = (status & 0x08) != 0;
            homed = (status & 0x04) != 0;
            position_mm = Convert.ToInt32( position_string, 16) / 1000.0;

            return true;
        }

        public override void MoveAbsolute( Axes axis, double position_mm, int velocity, int accel, int decel, bool wait_for_complete)
        {
            MoveHelper( axis, position_mm, velocity, accel, decel, wait_for_complete, true);
        }

        public override void MoveRelative( Axes axis, double amount_mm, int velocity, int accel, int decel, bool wait_for_complete)
        {
            MoveHelper( axis, amount_mm, velocity, accel, decel, wait_for_complete, false);
        }

        public void MoveHelper( Axes axis, double position_mm, int velocity, int accel, int decel, bool wait_for_complete, bool is_absolute_move)
        {
            string command = String.Format( Commands.MoveAbsolute, axis == Axes.X ? "2" : axis == Axes.Y ? "1" : "4",
                                            accel.ToString("X"), decel.ToString("X"), velocity.ToString("X"), ((int)(position_mm * 1000)).ToString("X"));

            Action send_command = new Action( () => {
                lock( _serial_lock) {
                    _port.Write( command);
                }
            });

            if( wait_for_complete)
                send_command.Invoke();
            else
                send_command.BeginInvoke( MoveHelperComplete, null);
        }

        private void MoveHelperComplete( IAsyncResult iar)
        {
            try {
                AsyncResult ar = (AsyncResult)iar;
                Action caller = (Action)ar.AsyncDelegate;
                caller.EndInvoke( iar);
            } catch( Exception ex) {
                _log.Error( ex.Message);
            }
        }

        public override void HomeAllAxes()
        {
            HomeAxis( Axes.Z, true);
            HomeAxis( Axes.X, false);
            HomeAxis( Axes.Y, false);
        }

        public override void HomeAxis( IXyz.Axes axis, bool blocking)
        {
            string command = String.Format( Commands.HomeAxis, axis == Axes.X ? "2" : axis == Axes.Y ? "1" : "4");

            lock( _serial_lock) {
                _port.Write( command);
            }
        }

        public override double GetPositionMM( Axes axis)
        {
            return _axis_info[(int)axis].PositionMM;
        }

        public override void EnableAxis( Axes axis, bool enable)
        {
            string command = String.Format( Commands.EnableAxis, axis == Axes.X ? "2" : axis == Axes.Y ? "1" : "4", enable ? "1" : "0");

            lock( _serial_lock) {
                _log.Info( "EnableAxis: writing " + command);
                _port.Write( command);
            }
        }

        public override bool IsAxisEnabled( Axes axis)
        {
            return _axis_info[(int)axis].Enabled;
        }
        
        public override void Stop()
        {
            EnableAxis( Axes.X, false);
            EnableAxis( Axes.Y, false);
            EnableAxis( Axes.Z, false);
        }

        public override bool Homed()
        {
            return true;
        }

        #endregion
    }
}
