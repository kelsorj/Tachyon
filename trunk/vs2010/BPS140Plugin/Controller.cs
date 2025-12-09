using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Xml.Serialization;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.Utils;
using log4net;

namespace BioNex.BPS140Plugin
{
    /// <summary>
    /// Layer on top of low-level digital I/O module, whatever that ends up being
    /// </summary>
    internal class Controller
    {
        public bool Connected           { get; private set; }
        public int SideFacingRobot      { get; private set; }
        public bool AllowUserOverride   { get; set; }
        public bool UnlockButtonPressed { get; private set; }
        public bool LimitSwitchAOn      { get; private set; }
        public bool LimitSwitchBOn      { get; private set; }
        public bool IsLocked            { get; private set; }
        public bool Simulating          { get; private set; }

        private Thread _update_thread { get; set; }
        private AutoResetEvent _stop_thread_event { get; set; }
        private IOInterface _io_interface { get; set; }
        private SystemStartupCheckInterface.SafeToMoveDelegate _external_conditions_unsafe_for_unlocking { get; set; }
        public BPS140Configuration Config { get; private set; }
        
        // device properties, comes from Device Manager database
        internal Dictionary< string, string> DeviceProperties { get; set; }
        private const string LockMagnetA = "lock magnet a"; // output.
        private const string LockMagnetB = "lock magnet b"; // output.
        private const string UnlockButton = "unlock button"; // input.
        private const string LimitSwitchA = "limit switch a"; // input.
        private const string LimitSwitchB = "limit switch b"; // input.
        // not used for now -- I think the LEDs are controlled by the BPS140 hardware directly
        //private static readonly string Light1 = "light 1"; // output.
        //private static readonly string Light2 = "light 2"; // output.

        private BPS140 _owner { get; set; } // only used to pass unlock requester information back to the app
        private static readonly ILog _log = LogManager.GetLogger( typeof( Controller));

        public Controller( BPS140 owner)
        {
            _owner = owner;
            SideFacingRobot = 1;
            _stop_thread_event = new AutoResetEvent( false);
        }

        /// <summary>
        /// Allows connection to the hardware.  portname is the port assigned to
        /// the SysTec USB->CAN adapter.
        /// </summary>
        /// <remarks>
        /// Simulation is a little counterintuitive right now.  One would think that since the IODevice is the main
        /// communication method for the BPS140, that it would dictate simulation.  However, I want to have the
        /// ability to simulate things separately so I pass it in for now.
        /// </remarks>
        public void Connect( IOInterface io_interface, Dictionary< string, string> device_properties, SystemStartupCheckInterface.SafeToMoveDelegate safe_to_unlock_delegate,
                             bool simulate)
        {
            Simulating = simulate;
            // connect to hardware
            _io_interface = io_interface;
            DeviceProperties = device_properties;
            _external_conditions_unsafe_for_unlocking = safe_to_unlock_delegate;
            // start thread to update public properties
            // DKM 2012-01-16 don't need to allow I/O functions if the BPS140 is "one-sided"
            if( _io_interface != null) {
                _update_thread = new Thread( UpdateThreadRunner);
                _update_thread.Name = "BPS140 lock and state monitor";
                _update_thread.IsBackground = true;
                _update_thread.Start();
            } else {
                SelectSideAInSimulation();
            }
            // load the plate types used by each rack / individual slot
            LoadRackPlateTypes();
            // set connected flag
            Connected = true;
        }

        private void LoadRackPlateTypes()
        {
            try {
                LoadXmlConfiguration();
            } catch (KeyNotFoundException ) {
                throw new Exception( "Could not load plugin because the 'configuration folder' property is missing from the device configuration database");
            } catch( FileNotFoundException ex) {
                // DKM 2011-03-22 not sure why, when this exception gets thrown, that the message changes.
                throw new Exception( String.Format( "Could not load plugin: {0}", ex.Message), ex);
            }
        }

        internal void LoadXmlConfiguration()
        {
            try {
                XmlSerializer serializer = new XmlSerializer(typeof(BPS140Configuration));
                string config_path = DeviceProperties[BPS140.ConfigFolder] + "\\config.xml";
                FileStream reader = new FileStream(config_path.ToAbsoluteAppPath(), FileMode.Open);
                Config = (BPS140Configuration)serializer.Deserialize(reader);
                reader.Close();
            } catch( FileNotFoundException) {
                // no config file, so just use default values
                Config = new BPS140Configuration();
            }
        }

        internal void SaveXmlConfiguration()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(BPS140Configuration));
            // need to erase previous file, because just writing to the same file will leave
            // existing data intact if it doesn't get overwritten
            string filename = DeviceProperties[BPS140.ConfigFolder] +  "\\config.xml";
            string backup = filename + ".backup";
            if( File.Exists( filename)) {
                try {
                    File.Copy( filename, backup, true);
                } catch( Exception ex) {
                    // if we have any sort of error in the backup, bail so we don't delete
                    // the existing config file.  It's better to not save the config than
                    // to crash.
                    string message = String.Format( "Could not backup existing config file for {0}: {1}", _owner.Name, ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                    _log.Info( message);
                    MessageBox.Show( message);
                    return;
                }
                File.Delete( filename);
            }
            try {
                using( FileStream writer = new FileStream( filename , FileMode.Create) ) {
                    serializer.Serialize( writer, Config);
                    writer.Close();
                }
            } catch( InvalidOperationException ex) {
                // this is to catch the case where an out-of-date config.xml is used.  For Igenica and Pioneer 2,
                // the file had an element name called PlateTypeMask for the default plate type, but I changed
                // this text to DefaultPlateType when I had to make improvements for Monsanto 1-4.
                if( ex.InnerException.Message.Contains( "Instance validation error")) {
                    File.Copy( backup, filename, true);
                    MessageBox.Show( String.Format( "The file '{0}' has an incorrect element name.  Please change all occurrences of 'PlateTypeMask' to 'DefaultPlateType' and reload Synapsis.", filename));
                }
            } catch( Exception ex) {
                _log.Info( String.Format( "Could not save {0} configuration: {1}.  Restoring previous config.xml.", _owner.Name, ex.InnerException != null ? ex.InnerException.Message : ex.Message));
                
            }
        }

        public void Close()
        {
            // stop update thread
            _stop_thread_event.Set();
            // reset connected flag
            Connected = false;
        }

        public void Unlock()
        {
            // DKM 2012-01-16 don't need to allow I/O functions if the BPS140 is "one-sided"
            if( _io_interface == null)
                return;

            if( _external_conditions_unsafe_for_unlocking != null)
                if( !_external_conditions_unsafe_for_unlocking( _owner))
                    return;

            // turn off magnets.  wait 3 seconds.  turn magnets back on.  set waiting for lock to true.
            byte[] magnets_bitmask = new byte[]{CreateBitmask( int.Parse( DeviceProperties[ LockMagnetA]), int.Parse( DeviceProperties[ LockMagnetB]))};
            _io_interface.ClearOutputs( magnets_bitmask);
            Thread.Sleep( 1000);
            _io_interface.SetOutputs( magnets_bitmask);
            IsLocked = false;
        }

        private void UpdateThreadRunner()
        {
            while( !_stop_thread_event.WaitOne( 100)){
                try{
                    if( Simulating) {
                        SelectSideAInSimulation();
                    }

                    // DKM 2010-10-05 #220 there's a potential issue here with initialization order.  Since the
                    //                BPS140 could load before the IOPlugin that it relies on, if we don't
                    //                check for it, there is a good chance that the limit switch values
                    //                will be WRONG, and then this locking thread might actually unlock
                    //                the BPS140 when it shouldn't!
                    if( (_io_interface != null) && !(_io_interface as DeviceInterface).Connected)
                        continue;

                    // poll inputs.
                    UnlockButtonPressed = GetInputState( int.Parse( DeviceProperties[ UnlockButton]));
                    LimitSwitchAOn = !GetInputState( int.Parse( DeviceProperties[ LimitSwitchA]));
                    LimitSwitchBOn = !GetInputState( int.Parse( DeviceProperties[ LimitSwitchB]));

                    // #326 added details to debug assertion so it's more obvious what's going on.
                    //Debug.Assert( !(LimitSwitchAOn && LimitSwitchBOn), "Both limit switches on the BPS140 are on.  Please check the contact switches.");
                    if( LimitSwitchAOn && LimitSwitchBOn)
                        continue;
                    // we are guaranteed at this point that a side is locked into position
                    SideFacingRobot = LimitSwitchAOn ? 1 : 2;
                    
                    // if is locked property is false but inputs indicate that device has become locked,
                    // then unlock unnecessary magnet and change is locked property.
                    if( !IsLocked && ( LimitSwitchAOn || LimitSwitchBOn)){
                        OnlyLockNecessaryMagnet();
                        IsLocked = true;
                    } else if( IsLocked && !LimitSwitchAOn && !LimitSwitchBOn) {
                        // someone must have manually forced the racks to unlock
                        IsLocked = false;
                    }
                    if( IsLocked && UnlockButtonPressed){
                        Unlock();
                    }
                } catch( Exception){
                }
            }
        }

        private void SelectSideAInSimulation()
        {
            UnlockButtonPressed = false;
            LimitSwitchAOn = true;
            LimitSwitchBOn = false;
            IsLocked = LimitSwitchAOn ^ LimitSwitchBOn;
        }

#region private methods

        private void OnlyLockNecessaryMagnet()
        {
            if( LimitSwitchAOn){
                _io_interface.ClearOutputs( new byte[]{CreateBitmask( int.Parse( DeviceProperties[ LockMagnetB]))});
                _io_interface.SetOutputs( new byte[]{CreateBitmask( int.Parse( DeviceProperties[ LockMagnetA]))});
            }
            if( LimitSwitchBOn){
                _io_interface.ClearOutputs( new byte[]{CreateBitmask( int.Parse( DeviceProperties[ LockMagnetA]))});
                _io_interface.SetOutputs( new byte[]{CreateBitmask( int.Parse( DeviceProperties[ LockMagnetB]))});
            }
        }

        private bool GetInputState( int bit_number)
        {
            if( Simulating) {
                // this is just to trick the system in simulation mode not to
                // report that both limit switches are on.
                if( bit_number == int.Parse( DeviceProperties[ LimitSwitchB]) )
                    return true;
                return false;
            }
            // DKM 2012-01-16 don't need to check for null _io_interface ref here, because this function is only called from the update thread, and the
            //                update thread only runs if the _io_interface isn't null.
            return _io_interface.GetInput( bit_number);
        }

        private static byte CreateBitmask( params int[] bit_indices)
        {
            byte retval = 0;
            for( int i = 0; i < bit_indices.Length; ++i){
                retval |= (byte)( 1 << ( bit_indices[ i]));
            }
            return retval;
        }

#endregion

    }
}
