using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BioNex.Shared.Utils;
using log4net;
using System.Reflection;

namespace BioNex.Shared.TechnosoftLibrary
{
    public class TechnosoftException : ApplicationException
    {
        public TechnosoftException( string msg) : base( msg)
        {
        }
    }

    /// <summary>
    /// need a MotorController class to deal with communication to the individual drives
    /// </summary>
    public class TechnosoftConnection
    {
        // DKM 2011-10-21 we set func_done before calling functions everywhere, so we should have a consistent starting value.
        //                We used to use 0, but that's a bad choice because we also set func_done to 0 when the function
        //                starts executing in the controller.  If we used a different number, we'd know if functions actually
        //                started to execute on the controller by querying this value.
        public static int InitialFuncDone { get { return -99; }}

        private readonly Dictionary<byte,IAxis> _axes = new Dictionary<byte,IAxis>();
        public bool Simulating { get; private set; }
        public bool Connected { get; set; }
        private readonly object _tml_lock = new object();

        public const string FirmwareMajor = "firmware_major";
        public const string FirmwareMinor = "firmware_minor";
        /// <summary>
        /// Only write to this variable address via IAxis.WriteSerialNumber
        /// </summary>
        public const string SerialNumber = "serial_number_ptr";

#if !TML_SINGLETHREADED
        private readonly ITMLChannel _channel;
#else
        readonly int _channel_id;
#endif
        private readonly GroupManager _group_manager;
        private readonly Dictionary<string,List<IAxis>> ConnectedBuddies = new Dictionary<string,List<IAxis>>();

        private static readonly ILog _log = LogManager.GetLogger(typeof(TechnosoftConnection));

        /* For unrequested CANbus messages */
        /*
#if !TML_SINGLETHREADED
        TMLLibConst.pfnCallbackRecvDriveMsg _TMLLibCBD;
#else
        TML.TMLLib.pfnCallbackRecvDriveMsg _TMLLibCBD;
#endif
        */

        public Dictionary<byte,IAxis> GetAxes()
        {
            // DKM 2011-07-22 well this was a nice bug.  When I added the buddy axis concept, I was able to get just
            //                the buddy axes, but if any code relied on iterating over axes with GetAxes(), it broke
            //                that functionality because I returned buddy axes as well!
            Dictionary<byte,IAxis> axes_without_buddies = new Dictionary<byte,IAxis>();
            // if no connected buddies, then return axes
            if( ConnectedBuddies.Count() == 0)
                return _axes;
            // otherwise, only add non-buddies
            foreach( var kvp in _axes) {
                foreach( var buddies in ConnectedBuddies) {
                    if( buddies.Value.Contains( kvp.Value))
                        continue;
                    axes_without_buddies.Add( kvp.Value.GetID(), kvp.Value);
                }
            }
            return axes_without_buddies;
        }

        public void Abort()
        {
            foreach( KeyValuePair<byte,IAxis> kvp in _axes) {
                kvp.Value.Abort();
            }
        }

        public void EnableAllAxes()
        {
            if( Simulating)
                return;

            lock( _tml_lock){
                // determine the disabled axes.
                IEnumerable< IAxis> disabled_axes = from axis in _axes.Values
                                                    where !axis.IsAxisOnFlag
                                                    select axis;
                // acquire a group to enable disabled axes in a group.
                Group group = null;
                try{
                    group = _group_manager.AcquireGroup( disabled_axes);
                    group.EnableGroup();
                } finally{
                    _group_manager.ReleaseGroup( group);
                }
                // allow five seconds for all disabled axes to successfully enable.
                DateTime start_time = DateTime.Now;
                foreach( IAxis axis in disabled_axes){
                    int func_done = InitialFuncDone;
                    while( func_done != 1){
                        TimeSpan time_elapsed = DateTime.Now - start_time;
                        if( time_elapsed.TotalSeconds > 5.0){
                            throw new TechnosoftException( "Some axes failed to enable");
                        }
                        axis.GetLongVariable( "func_done", out func_done);
                    }
                    _log.DebugFormat( "Axis {0} enabled through broadcast enable within {1} seconds", axis.Name, ( DateTime.Now - start_time).TotalSeconds);
                }
            }
        }

        /// <summary>
        /// Broadcasts the STOP command to all axes.
        /// </summary>
        /// <remarks>
        /// Mark needs to decide whether it's better to use TS_Stop or use his own stop command
        /// </remarks>
        public void StopAllAxes()
        {
            if( Simulating)
                return;

            lock (_tml_lock)
            {
#if !TML_SINGLETHREADED
                if (!_channel.TS_SelectBroadcast())
                    throw new TechnosoftException( "Could not select broadcast");
                if (!_channel.TS_CALL_Label( "func_my_stop"))
                    throw new TechnosoftException( "Could not stop");
#else
                if (!TML.TMLLib.TS_SelectBroadcast())
                    throw new TechnosoftException( "Could not select broadcast");
                if (!TML.TMLLib.TS_CALL_Label( "func_my_stop"))
                    throw new TechnosoftException( "Could not stop");
#endif
            }
        }

        /// <summary>
        /// The problem is that the axes will auto-retry MoveAbsoluteHelper when a move doesn't complete,
        /// and if we try to pause the protocol, the axis will recommand the previous move automatically.
        /// This function should be called any time we need to pause the protocol -- it will prevent
        /// the call to MoveAbsoluteHelper from re-entering.
        /// </summary>
        public void Pause()
        {
            foreach( KeyValuePair<byte,IAxis> kvp in GetAxes())
                kvp.Value.Pause();
        }

        public void Resume()
        {
            foreach( KeyValuePair<byte,IAxis> kvp in GetAxes())
                kvp.Value.Resume();
        }

        /// <summary>
        /// This constructor is ONLY used for when you want to simulate hardware.  I added
        /// it so users of the Technosoft library have the option of reading the property
        /// to know which kind of IAxis derived class to instantiate.  Perhaps the next
        /// version should have all of the IAxis members inside of this class instead of
        /// in the application itself.
        /// </summary>
        public TechnosoftConnection()
        {
            Simulating = true;
            Connected = true;
#if !TML_SINGLETHREADED
            _channel = null;
#endif
            // DKM 2010-06-14 don't probe in simulation mode
            //ProbeSystec(); // Probe systec devices on the CANbus and print some stuff in the logfile for logging reasons
#if !TML_SINGLETHREADED
            _group_manager = GroupManager.GetInstance( _channel, _tml_lock);
#else
            _group_manager = GroupManager.GetInstance( null, _tml_lock);
#endif
        }

        //[Conditional("TMLDriveMessages")]  // This is enabled currently sicne there is a comment at the beginning of this line :P
        /*
        private void MarksWeirdTMLCallback()
        {
#if !TML_SINGLETHREADED
            _TMLLibCBD = TMLLibCallback;
            _channel.TS_RegisterHandlerForUnrequestedDriveMessages(_TMLLibCBD);
#else
            _TMLLibCBD = new TML.TMLLib.pfnCallbackRecvDriveMsg(TMLLibCallback);
            TML.TMLLib.TS_RegisterHandlerForUnrequestedDriveMessages(_TMLLibCBD);
#endif
        }
        */

        /// <summary>
        /// opens a connection via CAN
        /// throws MotorException
        /// </summary>
        public TechnosoftConnection( string channel_name, byte can_mfg_id, uint baudrate)
        {
            Simulating = false;
            const byte host_id = 254;
            // open the CAN connection
#if !TML_SINGLETHREADED
            _channel = new TMLChannel();
            if ( !_channel.TS_OpenChannel(channel_name, can_mfg_id, host_id, baudrate))
#else
            if ( (_channel_id = TML.TMLLib.TS_OpenChannel(channel_name, can_mfg_id, host_id, baudrate)) == -1)
#endif
                throw new TechnosoftException( String.Format( "Failed to open connection to device on channel {0}", channel_name));
            Connected = true;
            // 2010-09-08 sib: Commenting out the TMLCallback because EasyMotionStudio generates a million of these things and basically renders this plugin useless
            //MarksWeirdTMLCallback();
#if !TML_SINGLETHREADED
            _group_manager = GroupManager.GetInstance( _channel, _tml_lock);
#else
            _group_manager = GroupManager.GetInstance( null, _tml_lock);
#endif
        }

        public void TMLLibCallback ( UInt16 wAxisID, UInt16 wAddress, Int32 Value )
        {
            _log.DebugFormat( "TMLLibCallback wAxidID={0}, wAddress={1}, Value={2}", wAxisID, wAddress, Value);
        }

        public void TMLLibCheckUnrequestedMessages()
        {
#if !TML_SINGLETHREADED
            if(_channel == null)
                return;
            _channel.TS_CheckForUnrequestedDriveMessages();
#else
            TML.TMLLib.TS_CheckForUnrequestedDriveMessages();
#endif
        }

        public TechnosoftConnection( uint com_port)
        {
            Simulating = false;
            const byte host_id = 255;
            const uint baudrate = 115200;
            string channel_name = String.Format( "COM{0}", com_port);
#if !TML_SINGLETHREADED
            _channel = new TMLChannel();
            if (!_channel.TS_OpenChannel(channel_name, TMLLibConst.CHANNEL_RS232, host_id, baudrate))
#else
            if ((_channel_id = TML.TMLLib.TS_OpenChannel(channel_name, TML.TMLLib.CHANNEL_RS232, host_id, baudrate)) == -1)
#endif
                throw new TechnosoftException( "Failed to open RS232 connection to device");
            Connected = true;
#if !TML_SINGLETHREADED
            _group_manager = GroupManager.GetInstance( _channel, _tml_lock);
#else
            _group_manager = GroupManager.GetInstance( null, _tml_lock);
#endif
        }

        /// <summary>
        /// Converts position [mm] to IU [ticks]
        /// </summary>
        /// <param name="position"></param>
        /// <param name="num_encoder_lines"></param>
        /// <param name="gear_ratio">The gear ratio or pitch [rot/mm].  For 1rot / 20mm, use 20.  For 77.36 motor rot / 10.35 load rot, use 10.35 * 360 / 77.36</param>
        /// <returns></returns>
        public static int ConvertPositionToIU( double position, int num_encoder_lines, double gear_ratio)
        {
            // position,iu = position,rad * 4 * num_encoder_lines * gear_ratio / 2PI
            // note that gear_ratio needs to be inverted and 2PI cancels out
            int position_in_iu = (int)(position * 4 * num_encoder_lines / gear_ratio);
            return position_in_iu;
        }

        /// <summary>
        /// Converts velocity [mm/s] to IU [ticks/slow_loop_servo_period]
        /// </summary>
        /// <param name="velocity">in mm/s</param>
        /// <param name="slow_loop_servo_period_s"></param>
        /// <param name="num_encoder_lines"></param>
        /// <param name="gear_ratio">The gear ratio or pitch [rot/mm].  For 1rot / 20mm, use 20.  For 77.36 motor rot / 10.35 load rot, use 10.35 * 360 / 77.36</param>
        /// <returns></returns>
        public static double ConvertVelocityToIU( double velocity, double slow_loop_servo_period_s, int num_encoder_lines, double gear_ratio)
        {
            // velocity,iu = velocity,rad/s * 4 * num_encoder_lines * slow_loop_servo_period_ms * gear_ratio / 2PI
            // note that gear_ratio needs to be inverted, and 2PI cancels out
            // divide gear_ratio by 1000 as well since we're going to use m/s
            double velocity_in_iu = velocity * 4 * num_encoder_lines * slow_loop_servo_period_s / gear_ratio;
            return velocity_in_iu;
        }

        /// <summary>
        /// Converts acceleration [mm/s^2] to IU [ticks/slow_loop_servo_period^2]
        /// </summary>
        /// <param name="acceleration">int mm/s^2</param>
        /// <param name="slow_loop_servo_period_s"></param>
        /// <param name="num_encoder_lines"></param>
        /// <param name="gear_ratio">The gear ratio or pitch [rot/mm].  For 1rot / 20mm, use 20.  For 77.36 motor rot / 10.35 load rot, use 10.35 * 360 / 77.36</param>
        /// <returns></returns>
        public static double ConvertAccelerationToIU( double acceleration, double slow_loop_servo_period_s, int num_encoder_lines, double gear_ratio)
        {
            // accel,iu = accel,rad/s2 * 4 * num_encoder_lines * slow_loop_servo_period_ms^2 * gear_ratio / 2PI
            double acceleration_in_iu = acceleration * 4 * num_encoder_lines * slow_loop_servo_period_s * slow_loop_servo_period_s / gear_ratio;
            return acceleration_in_iu;
        }

        /// <summary>
        /// Converts from time (for things like settling time) to IU
        /// </summary>
        /// <param name="time_ms">Time [ms]</param>
        /// <param name="slow_loop_servo_period_s"></param>
        /// <returns></returns>
        public static short ConvertTimeToIU( int time_ms, double slow_loop_servo_period_s)
        {
            double time_s = time_ms / 1000.0;
            return (short)Math.Round( time_s / slow_loop_servo_period_s);
        }

        /// <summary>
        /// loads an XML file that contains information about the controller IDs and their motor settings,
        /// and creates the necessary IAxis-based objects
        /// </summary>
        /// <param name="motor_settings_path"></param>
        /// <param name="tsm_setup_folder"></param>
        public void LoadConfiguration( string motor_settings_path, string tsm_setup_folder)
        {
            // first, load all of the axes from the file into the map
            var settings = MotorSettings.LoadMotorSettings( motor_settings_path);
            LoadConfiguration(settings, tsm_setup_folder);
        }

        /// <summary>
        /// creates IAxis objects based on pre-loaded motor settings data, allowing clients to pre-edit the motor settings
        /// for example, BeeSure uses this to selectively remove the Theta Axis from the settings if it's not part of the build
        /// </summary>
        /// <param name="settings"></param>
        public void LoadConfiguration(IDictionary<byte, MotorSettings> settings, string tsm_setup_folder)
        {
            foreach( KeyValuePair<byte,MotorSettings> kvp in settings) {
                byte axis_id = kvp.Key;
                MotorSettings setting = kvp.Value;
                //! \todo this is temporary so I only have to mess with the Y axes
                string setup_file = (tsm_setup_folder + String.Format("\\{0}.t.zip", axis_id)).ToAbsoluteAppPath();
                if( !Simulating && !setting.Simulate) {
                    // inside the constructor, we will need to modify the motor settings values!
                    try {
#if !TML_SINGLETHREADED
                        _axes[axis_id] = new TSAxis( _channel, axis_id, setting, setup_file, _tml_lock);
#else
                        _axes[axis_id] = new TSAxis( axis_id, setting, setup_file, _tml_lock);
#endif
                    } catch( AxisException ex) {
                        _axes[axis_id] = null;
                        _log.Warn( String.Format( "Could not initialize axis {0}: {1}", axis_id, ex.Message), ex);
                    }
                } else
                    _axes[axis_id] = new SimAxis( axis_id, kvp.Value);
                
                
                // temporarily set up trapezoidal moves for the W axes -- need to make this a motor setting!
                if (axis_id % 10 == 4)
                {
                    if( _axes[axis_id] != null)
                        _axes[axis_id].UseTrapezoidalProfileByDefault = true;
                }
            }
        }

        /// <summary>
        /// the device "buddy_name" is requesting axes from the TechnosoftConnection sharer
        /// </summary>
        /// <param name="buddy_name_or_port"></param>
        /// <param name="simulate"></param>
        /// <param name="motor_settings_path"></param>
        /// <param name="tsm_setup_folder"></param>
        /// <returns></returns>
        [Obsolete]
        public List<IAxis> LoadBuddyConfiguration( string buddy_name_or_port, bool simulate, string motor_settings_path, string tsm_setup_folder)
        {
            Debug.Assert( !ConnectedBuddies.ContainsKey( buddy_name_or_port), String.Format( "The device {0} is already borrowing this connection", buddy_name_or_port));
            // first, load all of the axes from the file into the map
            Dictionary<byte,MotorSettings> settings = MotorSettings.LoadMotorSettings( motor_settings_path);
            // create the list of axes to eventually return
            List<IAxis> buddy_axes = new List<IAxis>();
            foreach( KeyValuePair<byte,MotorSettings> kvp in settings) {
                byte axis_id = kvp.Key;
                // make sure that this axis id isn't in use by another device
                if( _axes.ContainsKey( axis_id))
                    throw new TechnosoftException( String.Format( "The axis ID {0} is already in use by another device", axis_id));
                MotorSettings setting = kvp.Value;
                //! \todo this is temporary so I only have to mess with the Y axes
                string setup_file = (tsm_setup_folder + String.Format("\\{0}.t.zip", axis_id)).ToAbsoluteAppPath();
                if( !Simulating) {
                    // inside the constructor, we will need to modify the motor settings values!
                    try {
#if !TML_SINGLETHREADED
                        _axes[axis_id] = new TSAxis( _channel, axis_id, setting, setup_file, _tml_lock);
#else
                        _axes[axis_id] = new TSAxis( axis_id, setting, setup_file, _tml_lock);
#endif
                    } catch( AxisException ex) {
                        _axes[axis_id] = null;
                        _log.Warn( String.Format( "Could not initialize axis {0}: {1}", axis_id, ex.Message), ex);
                    }
                } else
                    _axes[axis_id] = new SimAxis( axis_id, kvp.Value);
                // add the axes to the list to return from outside
                buddy_axes.Add( _axes[axis_id]);
            }

            ConnectedBuddies.Add( buddy_name_or_port, buddy_axes);
            return buddy_axes;
        }

        public void CloseBuddyConnection( string buddy_name)
        {
            foreach( IAxis axis in ConnectedBuddies[buddy_name])
                _axes.Remove( axis.GetID());
            ConnectedBuddies.Remove( buddy_name);
        }

        public void SetBroadcastMasterAxisID( byte axis_id)
        {
            if( Simulating)
                return;
            lock( _tml_lock) {
#if !TML_SINGLETHREADED
                if (!_channel.TS_SetupBroadcast( GetAxes()[axis_id].IdxSetup))
#else
                if (!TML.TMLLib.TS_SetupBroadcast( GetAxes()[axis_id].IdxSetup))
#endif
                    throw new AxisException( GetAxes()[axis_id], String.Format( "Could not set up axis {0} for Broadcast master", axis_id));
            }
        }

        public void SetGroupMasterAxisID( byte group_id, byte axis_id)
        {
            if( Simulating)
                return;
            lock( _tml_lock) {
#if !TML_SINGLETHREADED
                if (!_channel.TS_SetupGroup( group_id, GetAxes()[axis_id].IdxSetup))
#else
                if (!TML.TMLLib.TS_SetupGroup( group_id, GetAxes()[axis_id].IdxSetup))
#endif
                    throw new AxisException( GetAxes()[axis_id], String.Format( "Could not set up group {0} for Group master from axis {1}", group_id, axis_id));
            }
        }

        public void Close()
        {
            if( ConnectedBuddies.Count() > 0) {
                string error = String.Format( "The connection cannot be closed because the following devices are still connected: {0}",
                                              (from x in ConnectedBuddies select x.Key).ToCommaSeparatedString());
                throw new TechnosoftException( error);
            }

            Connected = false;
            if( !Simulating) {
                lock( _tml_lock) {
                    // _can_file_descriptor is set when we call TS_OpenChannel
#if !TML_SINGLETHREADED
                    _channel.TS_CloseChannel();
#else
                    TML.TMLLib.TS_CloseChannel(_channel_id);
#endif
                }
            }
        }

        /// <summary>
        /// Probes for attached Systec USB->CAN dongles and logs info about them
        /// </summary>
        public void ProbeSystec(int first, int last)
        {
            // DKM 2011-09-21 this function has been removed until it's necessary and is compliant with the CanDongleWrapper assembly

            /*
            DateTime datetime_start_func = DateTime.Now;

            if ((first < 0) || (first > last) || (last > 255))
                return;

            UcanDotNET.USBcanServer USBcanServer = new UcanDotNET.USBcanServer();
            byte ret;

            int dll_ver = USBcanServer.GetUserDllVersion();
            _log.DebugFormat( "Systec USBcan32.dll: Ver={0}, Rev={1}, Release={2}", dll_ver & 0xff, (dll_ver & 0xff00) >> 8, (dll_ver & 0xff0000) >> 16);

            for (int n = first; n <= last; ++n)
            {
                String ret_str;
                ret = USBcanServer.InitHardware((byte)(n));
                switch (ret)
                {
                    case UcanDotNET.USBcanServer.USBCAN_SUCCESSFUL:
                        ret_str = "USBCAN_SUCCESSFUL";
                        break;
                    case UcanDotNET.USBcanServer.USBCAN_ERR_MAXINSTANCES:
                        ret_str = "USBCAN_ERR_MAXINSTANCES";
                        break;
                    case UcanDotNET.USBcanServer.USBCAN_ERR_HWINUSE:
                        ret_str = "USBCAN_ERR_HWINUSE";
                        break;
                    case UcanDotNET.USBcanServer.USBCAN_ERR_ILLHW:
                        ret_str = "USBCAN_ERR_ILLHW";
                        break;
                    case UcanDotNET.USBcanServer.USBCAN_ERR_RESOURCE:
                        ret_str = "USBCAN_ERR_RESOURCE";
                        break;
                    case UcanDotNET.USBcanServer.USBCAN_ERR_ILLVERSION:
                        ret_str = "USBCAN_ERR_ILLVERSION";
                        break;
                    case UcanDotNET.USBcanServer.USBCAN_ERR_MAXMODULES:
                        ret_str = "USBCAN_ERR_MAXMODULES";
                        break;
                    default:
                        ret_str = "UNKNOWN";
                        break;
                }

                _log.DebugFormat( "Systec InitHardware({0}) returned {1} ==> {2}", n, ret, ret_str);
                
                if (ret != UcanDotNET.USBcanServer.USBCAN_SUCCESSFUL)
                    continue; // go back to the top of the loop

                ret = USBcanServer.InitCan(UcanDotNET.USBcanServer.USBCANTML.TMLLib_CH0,
                    UcanDotNET.USBcanServer.USBCAN_BAUDEX_USE_BTR01,
                    UcanDotNET.USBcanServer.USBCAN_BAUDEX_500kBit,
                    UcanDotNET.USBcanServer.USBCAN_AMR_ALL,
                    UcanDotNET.USBcanServer.USBCAN_ACR_ALL,
                    (byte)UcanDotNET.USBcanServer.tUcanMode.kUcanModeNormal,
                    UcanDotNET.USBcanServer.USBCAN_OCR_DEFAULT);
                switch (ret)
                {
                    case UcanDotNET.USBcanServer.USBCAN_SUCCESSFUL:
                        ret_str = "USBCAN_SUCCESSFUL";
                        break;
                    case UcanDotNET.USBcanServer.USBCAN_ERR_MAXINSTANCES:
                        ret_str = "USBCAN_ERR_MAXINSTANCES";
                        break;
                    case UcanDotNET.USBcanServer.USBCAN_ERR_ILLHANDLE:
                        ret_str = "USBCAN_ERR_ILLHANDLE";
                        break;
                    case UcanDotNET.USBcanServer.USBCAN_ERR_RESOURCE:
                        ret_str = "USBCAN_ERR_RESOURCE";
                        break;
                    case UcanDotNET.USBcanServer.USBCAN_ERR_BUSY:
                        ret_str = "USBCAN_ERR_BUSY";
                        break;
                    case UcanDotNET.USBcanServer.USBCAN_ERR_IOFAILED:
                        ret_str = "USBCAN_ERR_IOFAILED";
                        break;
                    case UcanDotNET.USBcanServer.USBCAN_ERRCMD_NOTEQU:
                        ret_str = "USBCAN_ERR_NOTEQU";
                        break;
                    case UcanDotNET.USBcanServer.USBCAN_ERRCMD_REGTST:
                        ret_str = "USBCAN_ERR_REGTST";
                        break;
                    case UcanDotNET.USBcanServer.USBCAN_ERRCMD_ILLCMD:
                        ret_str = "USBCAN_ERR_ILLCMD";
                        break;
                    default:
                        ret_str = "UNKNOWN";
                        break;
                }

                _log.DebugFormat( "Systec InitCAN() returned {0} ==> {1}", ret, ret_str);

                if (ret != UcanDotNET.USBcanServer.USBCAN_SUCCESSFUL)
                    continue; // go back to the top of the loop

                int fw_ver = USBcanServer.GetFwVersion();
                _log.DebugFormat( "Firmware: Ver={0}, Rev={1}, Release={2}", fw_ver & 0xff, (fw_ver & 0xff00) >> 8, (fw_ver & 0xff0000) >> 16);

                UcanDotNET.USBcanServer.tUcanHardwareInfoEx HwInfo = new UcanDotNET.USBcanServer.tUcanHardwareInfoEx();
                UcanDotNET.USBcanServer.tUcanChannelInfo CanInfoCh0 = new UcanDotNET.USBcanServer.tUcanChannelInfo();
                UcanDotNET.USBcanServer.tUcanChannelInfo CanInfoCh1 = new UcanDotNET.USBcanServer.tUcanChannelInfo();

                ret = USBcanServer.GetHardwareInfo(ref HwInfo, ref CanInfoCh0, ref CanInfoCh1);
                switch (ret)
                {
                    case UcanDotNET.USBcanServer.USBCAN_SUCCESSFUL:
                        ret_str = "USBCAN_SUCCESSFUL";
                        break;
                    case UcanDotNET.USBcanServer.USBCAN_ERR_MAXINSTANCES:
                        ret_str = "USBCAN_ERR_MAXINSTANCES";
                        break;
                    case UcanDotNET.USBcanServer.USBCAN_ERR_ILLHANDLE:
                        ret_str = "USBCAN_ERR_ILLHANDLE";
                        break;
                    case UcanDotNET.USBcanServer.USBCAN_ERR_ILLPARAM:
                        ret_str = "USBCAN_ERR_ILLPARAM";
                        break;
                    default:
                        ret_str = "UNKNOWN";
                        break;
                }

                _log.DebugFormat( "Systec GetHardwareInfo() returned {0} ==> {1}", ret, ret_str);

                if (ret != UcanDotNET.USBcanServer.USBCAN_SUCCESSFUL)
                    continue; // go back to the top of the loop
                String fw_str = String.Format("{0}.{1}.{2}", HwInfo.m_dwFwVersionEx & 0xff, (HwInfo.m_dwFwVersionEx & 0xff00) >> 8, (HwInfo.m_dwFwVersionEx & 0xff0000) >> 16);
                String prodcode_str;
                switch (HwInfo.m_dwProductCode & UcanDotNET.USBcanServer.USBCAN_PRODCODE_MASK_PID)
                {
                    case UcanDotNET.USBcanServer.USBCAN_PRODCODE_PID_GW001:
                        prodcode_str = "GW001";
                        break;
                    case UcanDotNET.USBcanServer.USBCAN_PRODCODE_PID_GW002:
                        prodcode_str = "GW002";
                        break;
                    case UcanDotNET.USBcanServer.USBCAN_PRODCODE_PID_MULTIPORT:
                        prodcode_str = "Multiport";
                        break;
                    case UcanDotNET.USBcanServer.USBCAN_PRODCODE_PID_BASIC:
                        prodcode_str = "USB_CANmodul1";
                        break;
                    case UcanDotNET.USBcanServer.USBCAN_PRODCODE_PID_ADVANCED:
                        prodcode_str = "USB_CANmodul2";
                        break;
                    default:
                        prodcode_str = "Unknown";
                        break;
                }
                _log.InfoFormat( "Systec USB->CAN S/N: {0}, Device# {1}, F/W: {2}, ProdCode: {3}",
                    HwInfo.m_dwSerialNr, HwInfo.m_bDeviceNr, fw_str, prodcode_str);
                _log.DebugFormat( "Ch0: Init={0}, CANstatus={1}", CanInfoCh0.m_fCanIsInit, CanInfoCh0.m_wCanStatus);
                if ((HwInfo.m_dwProductCode & UcanDotNET.USBcanServer.USBCAN_PRODCODE_PID_TWO_CHA) == UcanDotNET.USBcanServer.USBCAN_PRODCODE_PID_TWO_CHA)
                    _log.DebugFormat( "Ch1: Init={0}, CANstatus={1}", CanInfoCh1.m_fCanIsInit, CanInfoCh1.m_wCanStatus);

                USBcanServer.Shutdown();
            } // for loop for hw usb modules

            TimeSpan func_ts = DateTime.Now - datetime_start_func;
            _log.DebugFormat( "ProbeSystec() Done in {0:0}ms.", func_ts.TotalMilliseconds);
            */
        } // ProbeSystec

        /// <summary>
        /// Clears everything related to pausing and aborting
        /// </summary>
        public void ResetPauseAbort()
        {
            foreach( KeyValuePair<byte,IAxis> kvp in GetAxes()) {
                kvp.Value.ResetPause();
                kvp.Value.ResetAbort();
            }
        }

        // basic idea: http://www.dotnetscraps.com/dotnetscraps/post/Insert-any-binary-file-in-C-assembly-and-extract-it-at-runtime.aspx
        // addressing embedded path properly: http://www.codeproject.com/KB/dotnet/embeddedresources.aspx
        /// <summary>
        /// 
        /// </summary>
        /// <param name="destination_folder"></param>
        /// <param name="axis"></param>
        /// <param name="resource_prefix">e.g. BioNex.HiGIntegration.TML</param>
        /// <param name="major"></param>
        /// <param name="minor"></param>
        public static void ExtractTmlFiles( Assembly source_assembly, string destination_folder, byte axis, string resource_prefix, int major, int minor, bool overwrite_if_existing=false, bool use_version=true)
        {            
            // DKM 2011-12-06 originally, I passed in a list of axes, but since this is only for HiG, and since we want to support
            //                different versions for the shield and spindle, I treat each axis separately here.
            // SHIELD AXIS
            string path = String.Format( "{0}\\{1}.t.zip", destination_folder, axis);
            //In the next line you should provide NameSpace.FileName.Extension that you have embedded
            var resource_string = use_version ? String.Format("{0}.{1}_{2}.{3}.t.zip", resource_prefix, axis, major, minor) : string.Format("{0}.{1}.t.zip", resource_prefix, axis);
            var input = source_assembly.GetManifestResourceStream( resource_string);
            // only overwrite the file if a matching version exists!
            if( input != null) {
                if( System.IO.File.Exists( path)) {
                    if( overwrite_if_existing)
                        System.IO.File.Delete( path);
                    else
                        return;
                }
                // DKM 2012-01-18 create the folder if necessary
                if( !System.IO.Directory.Exists( destination_folder)) {
                    System.IO.Directory.CreateDirectory( destination_folder);
                }
                var output = System.IO.File.Open( path, System.IO.FileMode.CreateNew);
                CopyStream( input, output);
                input.Dispose();
                output.Dispose();
            }
        }

        /// <summary>
        /// Pulls the motor_settings.xml file from the assembly.  Keep in mind that each controller could have a different version of firmware,
        /// so the extracted file might not be the best one.  Future plugins will likely only do this if requested (via Initialize???)
        /// </summary>
        /// <param name="destination_folder"></param>
        /// <param name="resource_prefix"></param>
        public static void ExtractMotorSettingsFile( Assembly source_assembly, string destination_folder, string resource_prefix, bool overwrite_if_existing=false, string embedded_settings_name="motor_settings.xml", string output_settings_name="motor_settings.xml")
        {
            // MOTOR SETTINGS XML
            string output_path = String.Format( "{0}\\{1}", destination_folder, output_settings_name);
            //In the next line you should provide NameSpace.FileName.Extension that you have embedded
            var input = source_assembly.GetManifestResourceStream( resource_prefix + string.Format(".{0}", embedded_settings_name));
            if( input != null) {
                if( System.IO.File.Exists( output_path)) {
                    if( overwrite_if_existing)
                        System.IO.File.Delete( output_path);
                    else
                        return;
                }
                // DKM 2012-01-18 create the folder if necessary
                if( !System.IO.Directory.Exists( destination_folder)) {
                    System.IO.Directory.CreateDirectory( destination_folder);
                }
                var output = System.IO.File.Open( output_path, System.IO.FileMode.CreateNew);
                CopyStream( input, output);
                input.Dispose();
                output.Dispose();
            }
        }

        public static bool ExtractFirmware( Assembly source_assembly, string resource_prefix, string product_name, int axis_id, int major, int minor, out string firmware_path)
        {
            string temp_path = System.IO.Path.GetTempPath();
            firmware_path = String.Format("{0}\\{1}_{2}_{3}.{4}.sw", temp_path, product_name, axis_id, major, minor);
            var input = source_assembly.GetManifestResourceStream(String.Format("{0}.{1}.{2}_{3}.{4}.sw", resource_prefix, product_name, axis_id, major, minor));
            // only overwrite the file if a matching version exists!
            if (input != null)
            {
                if (System.IO.File.Exists(firmware_path))
                    System.IO.File.Delete(firmware_path);
                var output = System.IO.File.Open(firmware_path, System.IO.FileMode.CreateNew);
                CopyStream(input, output);
                input.Dispose();
                output.Dispose();
            }

            return input != null;
        }

        private static void CopyStream( System.IO.Stream input, System.IO.Stream output)
        {
            byte[] buffer = new byte[32768];
            while (true)
            {
                int read = input.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                    return;
                output.Write(buffer, 0, read);
            }
        }

        public static void ExtractTemporaryTmlFiles(Assembly assembly, string path, List<byte> axis_ids, string resource_prefix, int major, int minor)
        {
            TechnosoftConnection.ExtractMotorSettingsFile( assembly, path, resource_prefix, true);
            foreach( var id in axis_ids) {
                TechnosoftConnection.ExtractTmlFiles( assembly, path, id, "BioNex.LiquidLevelDevice.TML", major, minor, true);
            }
        }
    }
}
