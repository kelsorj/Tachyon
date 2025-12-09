using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Collections.Generic;

#if !TML_SINGLETHREADED
namespace BioNex.Shared.TechnosoftLibrary
#else
namespace TML
#endif
{
#if !TML_SINGLETHREADED
    public interface ITMLChannel
    {
        // channel management
        Boolean TS_OpenChannel(String pszDevName, Byte btType, Byte nHostID, UInt32 baudrate); 
        void TS_CloseChannel();

        Int32 TS_LoadSetup(String setupPath);
        Boolean TS_SetupAxis(Byte axisID, Int32 idxSetup);
        Boolean TS_SelectAxis(Byte axisID);
        Boolean TS_SetupGroup(Byte groupID, Int32 idxSetup);
        Boolean TS_SelectGroup(Byte groupID);
        Boolean TS_SetupBroadcast(Int32 idxSetup);
        Boolean TS_SelectBroadcast();
        Boolean TS_Reset();
        Boolean TS_ResetFault();
        Boolean TS_Power(Boolean Enable);
        Boolean TS_ReadStatus(Int16 SelIndex, out UInt16 Status);
        Boolean TS_Save();
        Boolean TS_UpdateImmediate();
        Boolean TS_UpdateOnEvent();
        Boolean TS_SetPosition(Int32 PosValue);
        Boolean TS_SetCurrent(Int16 CrtValue);
        Boolean TS_SetTargetPositionToActual();
        Boolean TS_GetVariableAddress(String pszName, out Int16 address);
        Boolean TS_SetIntVariable(String pszName, Int16 value);
        Boolean TS_GetIntVariable(String pszName, out Int16 value);
        Boolean TS_SetLongVariable(String pszName, Int32 value);
        Boolean TS_GetLongVariable(String pszName, out Int32 value);
        Boolean TS_SetFixedVariable(String pszName, Double value);
        Boolean TS_GetFixedVariable(String pszName, out Double value);
        Boolean TS_SetBuffer(UInt16 address, UInt16[] arrayValues, UInt16 nSize);
        Boolean TS_GetBuffer(UInt16 address, UInt16[] arrayValues, UInt16 nSize);
        Boolean TS_MoveAbsolute(Int32 AbsPosition, Double Speed, Double Acceleration, Int16 MoveMoment, Int16 ReferenceBase);
        Boolean TS_MoveRelative(Int32 RelPosition, Double Speed, Double Acceleration, Boolean IsAdditive, Int16 MoveMoment, Int16 ReferenceBase);
        Boolean TS_MoveVelocity(Double Speed, Double Acceleration, Int16 MoveMoment, Int16 ReferenceBase);
        Boolean TS_SetAnalogueMoveExternal(Int16 ReferenceType, Boolean UpdateFast, Double LimitVariation, Int16 MoveMoment);
        Boolean TS_SetDigitalMoveExternal(Boolean SetGearRatio, Int16 Denominator, Int16 Numerator, Double LimitVariation, Int16 MoveMoment);
        Boolean TS_SetOnlineMoveExternal(Int16 ReferenceType, Double LimitVariation, Double InitialValue, Int16 MoveMoment);
        Boolean TS_VoltageTestMode(Int16 MaxVoltage, Int16 IncrVoltage, Int16 Theta0, Int16 Dtheta, Int16 MoveMoment);
        Boolean TS_TorqueTestMode(Int16 MaxTorque, Int16 IncrTorque, Int16 Theta0, Int16 Dtheta, Int16 MoveMoment);
        Boolean TS_SetGearingMaster(Boolean Group, Byte SlaveID, Int16 ReferenceBase, Boolean Enable, Boolean SetSlavePos, Int16 MoveMoment);
        Boolean TS_SetGearingSlave(Int16 Denominator, Int16 Numerator, Int16 ReferenceBase, Int16 EnableSlave, Double LimitVariation, Int16 MoveMoment);
        Boolean TS_MotionSuperposition(Int16 Enable, Int16 Update);
        Boolean TS_SetCammingMaster(Boolean Group, Byte SlaveID, Int16 ReferenceBase, Boolean Enable, Int16 MoveMoment);
        Boolean TS_CamDownload(String pszCamFile, UInt16 wLoadAddress, UInt16 wRunAddress, out UInt16 wNextLoadAddr, out UInt16 wNexRunAddr);
        Boolean TS_CamInitialization(UInt16 LoadAddress, UInt16 RunAddress);
        Boolean TS_SetCammingSlaveRelative(UInt16 RunAddress, Int16 ReferenceBase, Int16 EnableSlave, Int16 MoveMoment, Int32 OffsetFromMaster, Double MultInputFactor, Double MultOutputFactor);
        Boolean TS_SetCammingSlaveAbsolute(UInt16 RunAddress, Double LimitVariation, Int16 ReferenceBase, Int16 EnableSlave, Int16 MoveMoment, Int32 OffsetFromMaster, Double MultInputFactor, Double MultOutputFactor);
        Boolean TS_SetMasterResolution(Int32 MasterResolution);
        Boolean TS_SendSynchronization(Int32 Period);
        Boolean TS_Stop();
        Boolean TS_QuickStopDecelerationRate(Double Deceleration);
        Boolean TS_SendPVTPoint(Int32 Position, Double Velocity, UInt32 Time, Int16 PVTCounter);
        Boolean TS_SendPVTFirstPoint(Int32 Position, Double Velocity, Int32 Time, Int16 PVTCounter, Int16 PositionType, Int32 InitialPosition, Int16 MoveMoment, Int16 ReferenceBase);
        Boolean TS_PVTSetup(Int16 ClearBuffer, Int16 IntegrityChecking, Int16 ChangePVTCounter, Int16 AbsolutePositionSource, Int16 ChangeLowLevel, Int16 PVTCounterValue, Int16 LowLevelValue);
        Boolean TS_SendPTPoint(Int32 Position, Int32 Time, Int16 PTCounter);
        Boolean TS_SendPTFirstPoint(Int32 Position, Int32 Time, Int16 PTCounter, Int16 PositionType, Int32 InitialPosition, Int16 MoveMoment, Int16 ReferenceBase);
        Boolean TS_PTSetup(Int16 ClearBuffer, Int16 IntegrityChecking, Int16 ChangePTCounter, Int16 AbsolutePositionSource, Int16 ChangeLowLevel, Int16 PTCounterValue, Int16 LowLevelValue);
        Boolean TS_MoveSCurveRelative(Int32 RelPosition, Double Speed, Double Acceleration, Int32 JerkTime, Int16 MoveMoment, Int16 DecelerationType);
        Boolean TS_MoveSCurveAbsolute(Int32 AbsPosition, Double Speed, Double Acceleration, Int32 JerkTime, Int16 MoveMoment, Int16 DecelerationType);
        Boolean TS_CheckEvent(out Boolean eventDetected);
        Boolean TS_SetEventOnMotionComplete(Boolean WaitEvent, Boolean EnableStop);
        Boolean TS_SetEventOnMotorPosition(Int16 PositionType, Int32 Position, Boolean Over, Boolean WaitEvent, Boolean EnableStop);
        Boolean TS_SetEventOnLoadPosition(Int16 PositionType, Int32 Position, Boolean Over, Boolean WaitEvent, Boolean EnableStop);
        Boolean TS_SetEventOnMotorSpeed(Double Speed, Boolean Over, Boolean WaitEvent, Boolean EnableStop);
        Boolean TS_SetEventOnLoadSpeed(Double Speed, Boolean Over, Boolean WaitEvent, Boolean EnableStop);
        Boolean TS_SetEventOnTime(UInt16 Time, Boolean WaitEvent, Boolean EnableStop);
        Boolean TS_SetEventOnPositionRef(Int32 Position, Boolean Over, Boolean WaitEvent, Boolean EnableStop);
        Boolean TS_SetEventOnSpeedRef(Double Speed, Boolean Over, Boolean WaitEvent, Boolean EnableStop);
        Boolean TS_SetEventOnTorqueRef(int Torque, Boolean Over, Boolean WaitEvent, Boolean EnableStop);
        Boolean TS_SetEventOnEncoderIndex(Int16 IndexType, Int16 TransitionType, Boolean WaitEvent, Boolean EnableStop);
        Boolean TS_SetEventOnLimitSwitch(Int16 LSWType, Int16 TransitionType, Boolean WaitEvent, Boolean EnableStop);
        Boolean TS_SetEventOnDigitalInput(Byte InputPort, Int16 IOState, Boolean WaitEvent, Boolean EnableStop);
        Boolean TS_SetEventOnHomeInput(Int16 IOState, Boolean WaitEvent, Boolean EnableStop);
        Boolean TS_SetupInput(Byte nIO);
        Boolean TS_GetInput(Byte nIO, out Byte InValue);
        Boolean TS_GetHomeInput(out Byte InValue);
        Boolean TS_SetupOutput(Byte nIO);
        Boolean TS_SetOutput(Byte nIO, Byte OutValue);
        Boolean TS_GetMultipleInputs(String pszVarName, out Int16 Status);
        Boolean TS_SetMultipleOutputs(String pszVarName, Int16 Status);
        Boolean TS_SetMultipleOutputs2(Int16 SelectedPorts, Int16 Status);
        Boolean TS_SendDataToHost(Byte HostAddress, UInt32 StatusRegMask, UInt16 ErrorRegMask);
        Boolean TS_OnlineChecksum(UInt16 startAddress, UInt16 endAddress, out UInt16 checksum);
        Boolean TS_DownloadProgram(String pszOutFile, out UInt16 wEntryPoint);
        Boolean TS_DownloadSwFile(String pszSwFile);
        Boolean TS_GOTO(UInt16 address);
        Boolean TS_GOTO_Label(String pszLabel);
        Boolean TS_CALL(UInt16 address);
        Boolean TS_CALL_Label(String pszFunctionName);
        Boolean TS_CancelableCALL(UInt16 address);
        Boolean TS_CancelableCALL_Label(String pszFunctionName);
        Boolean TS_ABORT();
        Boolean TS_Execute(String pszCommands);
        Boolean TS_ExecuteScript(String pszFileName);
        Boolean TS_GetOutputOfExecute(StringBuilder pszOutput, int nMaxChars);
        Boolean TS_SetupLogger(UInt16 wLogBufferAddr, UInt16 wLogBufferLen, UInt16[] arrayAddresses, UInt16 countAddr, UInt16 period);
        Boolean TS_StartLogger(UInt16 wLogBufferAddr, Byte LogType);
        Boolean TS_CheckLoggerStatus(UInt16 wLogBufferAddr, out UInt16 status);
        Boolean TS_UploadLoggerResults(UInt16 wLogBufferAddr, UInt16[] arrayValues, ref UInt16 countValues);
        void TS_RegisterHandlerForUnrequestedDriveMessages(TMLLibConst.pfnCallbackRecvDriveMsg handler);
        Boolean TS_CheckForUnrequestedDriveMessages();
        Boolean TS_DriveInitialisation();
    }
#endif

#if !TML_SINGLETHREADED
    public class TMLLibConst
#else
    public class TMLLib
#endif
    {
        //supported CAN protocols
        public const Byte PROTOCOL_TMLCAN = 0x00;    //use TMLCAN protocol (default, 29-bit identifiers)
        public const Byte PROTOCOL_TECHNOCAN = 0x80; //use TechnoCAN protocol (11-bit identifiers)
        public const Byte PROTOCOL_MASK = 0x80;      //this bits are used for specifying CAN protocol through nChannelType param of MSK_OpenComm function

        // ***** supported CAN devices *****************************
        // CHANNEL_IXXAT_CAN - see http://www.ixxat.com
        // CHANNEL_SYS_TEC_USBCAN - see www.systec-electronic.com
        // CHANNEL_ESD_CAN - see http://www.esd-electronics.com
        // CHANNEL_PEAK_SYS_PCAN_* - see http://www.peak-system.com
        // **********************************************************
        public const Byte CHANNEL_RS232 = 0;
        public const Byte CHANNEL_RS485 = 1;
        public const Byte CHANNEL_IXXAT_CAN = 2;
        public const Byte CHANNEL_SYS_TEC_USBCAN = 3;
        public const Byte CHANNEL_PEAK_SYS_PCAN_PCI = 4;
        public const Byte CHANNEL_ESD_CAN = 5;
        public const Byte CHANNEL_PEAK_SYS_PCAN_ISA = 6;
        public const Byte CHANNEL_PEAK_SYS_PCAN_PC104 = CHANNEL_PEAK_SYS_PCAN_ISA; //Same with PCAN_ISA
        public const Byte CHANNEL_PEAK_SYS_PCAN_USB = 7;
        public const Byte CHANNEL_PEAK_SYS_PCAN_DONGLE = 8;
        public const Byte CHANNEL_VIRTUAL_SERIAL = 15;
        public const Byte CHANNEL_XPORT_IP = 16;

        // Constant used for host ID
        public const Byte HOST_ID = 0;

        // Constants used as values for 'Logger' parameters
        public const Byte LOGGER_SLOW = 1;
        public const Byte LOGGER_FAST = 2;

        // Constants used as values for 'MoveMoment' parameters
        public const Int16 UPDATE_NONE = -1;
        public const Int16 UPDATE_ON_EVENT = 0;
        public const Int16 UPDATE_IMMEDIATE = 1;

        // Constants used for 'ReferenceType' parameters
        public const Int16 REFERENCE_POSITION = 0;
        public const Int16 REFERENCE_SPEED = 1;
        public const Int16 REFERENCE_TORQUE = 2;
        public const Int16 REFERENCE_VOLTAGE = 3;

        // Constants used for EnableSuperposition
        public const Int16 SUPERPOS_DISABLE = -1;
        public const Int16 SUPERPOS_NONE = 0;
        public const Int16 SUPERPOS_ENABLE = 1;

        // Constants used for PositionType
        public const Int16 ABSOLUTE_POSITION = 0;
        public const Int16 RELATIVE_POSITION = 1;

        // Constants used for EnableSlave
        public const Int16 SLAVE_NONE = 0;
        public const Int16 SLAVE_COMMUNICATION_CHANNEL = 1;
        public const Int16 SLAVE_2ND_ENCODER = 2;

        // Constants used for ReferenceBase
        public const Int16 FROM_MEASURE = 0;
        public const Int16 FROM_REFERENCE = 1;

        // Constants used for DecelerationType
        public const Int16 S_CURVE_SPEED_PROFILE = 0;
        public const Int16 TRAPEZOIDAL_SPEED_PROFILE = 1;

        // Constants used for IOState
        public const byte IO_HIGH = 1;
        public const byte IO_LOW = 0;

        // Constants used for TransitionType
        public const Int16 TRANSITION_HIGH_TO_LOW = -1;
        public const Int16 TRANSITION_DISABLE = 0;
        public const Int16 TRANSITION_LOW_TO_HIGH = 1;

        // Constants used for IndexType
        public const Int16 INDEX_1 = 1;
        public const Int16 INDEX_2 = 2;

        // Constants used for LSWType
        public const Int16 LSW_NEGATIVE = -1;
        public const Int16 LSW_POSITIVE = 1;

        // Constants used for TS_Power; to activate/deactivate teh PWM commands
        public const Boolean POWER_ON = true;
        public const Boolean POWER_OFF = false;

        // Constants used as inputs parameters of the I/O functions
        public const Byte INPUT_0 = 0;
        public const Byte INPUT_1 = 1;
        public const Byte INPUT_2 = 2;
        public const Byte INPUT_3 = 3;
        public const Byte INPUT_4 = 4;
        public const Byte INPUT_5 = 5;
        public const Byte INPUT_6 = 6;
        public const Byte INPUT_7 = 7;
        public const Byte INPUT_8 = 8;
        public const Byte INPUT_9 = 9;
        public const Byte INPUT_10 = 10;
        public const Byte INPUT_11 = 11;
        public const Byte INPUT_12 = 12;
        public const Byte INPUT_13 = 13;
        public const Byte INPUT_14 = 14;
        public const Byte INPUT_15 = 15;
        public const Byte INPUT_16 = 16;
        public const Byte INPUT_17 = 17;
        public const Byte INPUT_18 = 18;
        public const Byte INPUT_19 = 19;
        public const Byte INPUT_20 = 20;
        public const Byte INPUT_21 = 21;
        public const Byte INPUT_22 = 22;
        public const Byte INPUT_23 = 23;
        public const Byte INPUT_24 = 24;
        public const Byte INPUT_25 = 25;
        public const Byte INPUT_26 = 26;
        public const Byte INPUT_27 = 27;
        public const Byte INPUT_28 = 28;
        public const Byte INPUT_29 = 29;
        public const Byte INPUT_30 = 30;
        public const Byte INPUT_31 = 31;
        public const Byte INPUT_32 = 32;
        public const Byte INPUT_33 = 33;
        public const Byte INPUT_34 = 34;
        public const Byte INPUT_35 = 35;
        public const Byte INPUT_36 = 36;
        public const Byte INPUT_37 = 37;
        public const Byte INPUT_38 = 38;
        public const Byte INPUT_39 = 39;

        public const Byte OUTPUT_0 = 0;
        public const Byte OUTPUT_1 = 1;
        public const Byte OUTPUT_2 = 2;
        public const Byte OUTPUT_3 = 3;
        public const Byte OUTPUT_4 = 4;
        public const Byte OUTPUT_5 = 5;
        public const Byte OUTPUT_6 = 6;
        public const Byte OUTPUT_7 = 7;
        public const Byte OUTPUT_8 = 8;
        public const Byte OUTPUT_9 = 9;
        public const Byte OUTPUT_10 = 10;
        public const Byte OUTPUT_11 = 11;
        public const Byte OUTPUT_12 = 12;
        public const Byte OUTPUT_13 = 13;
        public const Byte OUTPUT_14 = 14;
        public const Byte OUTPUT_15 = 15;
        public const Byte OUTPUT_16 = 16;
        public const Byte OUTPUT_17 = 17;
        public const Byte OUTPUT_18 = 18;
        public const Byte OUTPUT_19 = 19;
        public const Byte OUTPUT_20 = 20;
        public const Byte OUTPUT_21 = 21;
        public const Byte OUTPUT_22 = 22;
        public const Byte OUTPUT_23 = 23;
        public const Byte OUTPUT_24 = 24;
        public const Byte OUTPUT_25 = 25;
        public const Byte OUTPUT_26 = 26;
        public const Byte OUTPUT_27 = 27;
        public const Byte OUTPUT_28 = 28;
        public const Byte OUTPUT_29 = 29;
        public const Byte OUTPUT_30 = 30;
        public const Byte OUTPUT_31 = 31;
        public const Byte OUTPUT_32 = 32;
        public const Byte OUTPUT_33 = 33;
        public const Byte OUTPUT_34 = 34;
        public const Byte OUTPUT_35 = 35;
        public const Byte OUTPUT_36 = 36;
        public const Byte OUTPUT_37 = 37;
        public const Byte OUTPUT_38 = 38;
        public const Byte OUTPUT_39 = 39;

        // Constants used for the register for function TS_ReadStatus
        public const Int16 REG_MCR = 0;
        public const Int16 REG_MSR = 1;
        public const Int16 REG_ISR = 2;
        public const Int16 REG_SRL = 3;
        public const Int16 REG_SRH = 4;
        public const Int16 REG_MER = 5;

        // Constants used to select or set the group
        public const Byte GROUP_0 = 0;
        public const Byte GROUP_1 = 1;
        public const Byte GROUP_2 = 2;
        public const Byte GROUP_3 = 3;
        public const Byte GROUP_4 = 4;
        public const Byte GROUP_5 = 5;
        public const Byte GROUP_6 = 6;
        public const Byte GROUP_7 = 7;
        public const Byte GROUP_8 = 8;

        // Special parameter values
        public const Int32 FULL_RANGE = 0;
        public const Int32 NO_VARIATION = 0;

        // ***************************************************************************
        // Callback function used by client application for handling unsolicited
        // messages which this driver receives in unexpected places
        // ***************************************************************************
        public delegate void pfnCallbackRecvDriveMsg(UInt16 wAxisID, UInt16 wAddress, Int32 Value);
#if !TML_SINGLETHREADED
    }

    class TMLLibPInvokes
    {
#endif
        /// <summary>
        /// Function: Returns a text related to the last occurred error when one of the library functions was called.
        /// </summary>
        /// <returns>A text related to the last occurred error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern String TS_GetLastErrorText();

        /// <summary>
        /// Updates strError with a text related to the last occurred error when one of the library functions was called.
        /// </summary>
        /// <param name="strError">string with error text</param>
        /// <param name="nBuffSize">number of chars in strError</param>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern void TS_Basic_GetLastErrorText(StringBuilder strError, Int32 nBuffSize);

        // *******************************************************************
        // *******************Parametrization*********************************
        // *******************************************************************

        /// <summary>
        /// Load setup information from a zip archive or a directory containing setup.cfg and variables.cfg files.
        /// </summary>
        /// <param name="setupPath">path to the zip archive or directory that contains setup.cfg and variables.cfg of the given setup </param>
        /// <returns>>=0 index of the loaded setup; -1 if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Int32 TS_LoadSetup(String setupPath);

        // *******************************************************************
        // ******************* Communication channels ************************
        // *******************************************************************

        /// <summary>
        /// Open a communication channel: NOTE -- owned thread, must use TS_SelectChannel if multiple channels are used in a thread, and Select must be mutexed
        /// </summary>
        /// <param name="pszDevName">Number of the serial channel to be open (for serial ports: "COM1", "COM2", ...; for CAN devices: "1", "2", ..)</param>
        /// <param name="btType">channel type (CHANNEL_*) with an optional protocol (PROTOCOL_*, default is PROTOCOL_TMLCAN)</param>
        /// <param name="nHostID">Is the address of your PC computer. A value between 1 and 255
        ///                         For RS232: axis ID of the drive connected to the PC serial port (usually 255)
        ///     			        For RS485 or CAN devices: must be an unused axis ID! It is the address of your PC computer on the RS485 network.
        ///     			        For XPORT: "IP:port"</param>
        /// <param name="baudrate">serial ports: 9600, 19200, 38400, 56000 or 115200
        ///     			       CAN devices: 125000, 250000, 500000, 1000000</param>
        /// <returns>channel's file descriptor or -1 if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Int32 TS_OpenChannel(String pszDevName, Byte btType, Byte nHostID, UInt32 baudrate);

        /// <summary>
        /// Select active communication channel. If you use only one channel there is no need to call this function.
        /// </summary>
        /// <param name="fd">channel file descriptor (-1 means selected communication channel)</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SelectChannel(Int32 fd);

        /// <summary>
        /// Close the communication channel.
        /// </summary>
        /// <param name="fd">channel file descriptor (-1 means selected communication channel)</param>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern void TS_CloseChannel(Int32 fd);

        // *******************************************************************
        // *******************Drive Administration ***************************
        // *******************************************************************

        /// <summary>
        /// Select setup configuration for the drive with axis ID.
        /// </summary>
        /// <param name="axisID">axis ID. It must be a value between 1 and 255; </param>
        /// <param name="idxSetup">Index of previously loaded setup, returned by TS_LoadSetup </param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetupAxis(Byte axisID, Int32 idxSetup);

        /// <summary>
        /// Selects the active axis. Must be mutexed with TS_SelectAxis/TS_SelectGroup/TS_SelectBroadcast
        /// </summary>
        /// <param name="axisID">The ID of the axis to become the active one. It must be a value between 1 and 255; 
        ///			             For RS485/CAN communication, this value must be different than nHostID parameter 
        ///					     defined at TS_OpenChannel function call.</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SelectAxis(Byte axisID);

        /// <summary>
        /// Select setup configuration for the drives within group.
        /// </summary>
        /// <param name="groupID">group ID. It must be a value between 1 and 8</param>
        /// <param name="idxSetup">Index of previously loaded setup, returned from TS_LoadSetup</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetupGroup(Byte groupID, Int32 idxSetup);

        /// <summary>
        /// Selects the active group. Must be mutexed with TS_SelectAxis/TS_SelectGroup/TS_SelectBroadcast
        /// </summary>
        /// <param name="groupID">The ID of the group of axes to become the active ones. It must be a value between 1 and 8.</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SelectGroup(Byte groupID);

        /// <summary>
        /// Select setup configuration for all drives on the active channel.
        /// </summary>
        /// <param name="idxSetup">Index of previously loaded setup, returned by TS_LoadSetup</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetupBroadcast(Int32 idxSetup);

        /// <summary>
        /// Selects all axis on the active channel. Must be mutexed with TS_SelectAxis/TS_SelectGroup/TS_SelectBroadcast
        /// </summary>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SelectBroadcast();

        /// <summary>
        /// Resets selected drives.
        /// </summary>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_Reset();

        /// <summary>
        /// This function clears most of the errors bits from Motion Error Register (MER).
        /// </summary>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_ResetFault();

        /// <summary>
        /// Controls the power stage (ON/OFF).
        /// </summary>
        /// <param name="Enable">TRUE -> Power ON the drive; FALSE -> Power OFF the drive</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_Power(Boolean Enable);

        /// <summary>
        /// Returns drive status information.
        /// </summary>
        /// <param name="SelIndex">		        
        ///     REG_MCR -> read MCR register
        ///     REG_MSR -> read MSR register
        ///		REG_ISR -> read ISR register 
        ///     REG_SRL -> read SRL register 
        ///     REG_SRH -> read SRH register 
        ///     REG_MER -> read MER register</param>
        /// <param name="Status">drive status information (value of the selected register)</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_ReadStatus(Int16 SelIndex, out UInt16 Status);

        /// <summary>
        /// Saves actual values of all the parameters from the drive/motor working memory into the EEPROM setup table.
        /// </summary>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_Save();

        /// <summary>
        /// Update the motion mode immediately.
        /// </summary>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_UpdateImmediate();

        /// <summary>
        /// Update the motion mode on next event occurence.
        /// </summary>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_UpdateOnEvent();

        /// <summary>
        /// Set actual position value.
        /// </summary>
        /// <param name="PosValue">Value at which the position is set</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetPosition(Int32 PosValue);

        /// <summary>
        /// Set actual current value. REMARK: this command can be used only for step motor drives
        /// </summary>
        /// <param name="CrtValue">Value at which the motor current is set</param>					        
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetCurrent(Int16 CrtValue);

        /// <summary>
        /// Set the target position value equal to the actual position value.
        /// </summary>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetTargetPositionToActual();

        /// <summary>
        /// Returns the variable address. The address is read from setup file
        /// </summary>
        /// <param name="pszName">Variable name</param>
        /// <param name="address">Variable address</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_GetVariableAddress(String pszName, out Int16 address);

        /// <summary>
        /// Writes an integer type variable to the drive.
        /// </summary>
        /// <param name="pszName">Name of the variable</param>
        /// <param name="value">Variable value</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetIntVariable(String pszName, Int16 value);

        /// <summary>
        /// Reads an integer type variable from the drive.
        /// </summary>
        /// <param name="pszName">Name of the variable</param>
        /// <param name="value">Variable value</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_GetIntVariable(String pszName, out Int16 value);

        /// <summary>
        /// Writes a Int32 integer type variable to the drive.
        /// </summary>
        /// <param name="pszName">Name of the variable</param>
        /// <param name="value">Variable value</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetLongVariable(String pszName, Int32 value);

        /// <summary>
        /// Reads a Int32 integer type variable from the drive.
        /// </summary>
        /// <param name="pszName">Name of the variable</param>
        /// <param name="value">Variable value</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_GetLongVariable(String pszName, out Int32 value);

        /// <summary>
        /// Writes a fixed point type variable to the drive.
        /// </summary>
        /// <param name="pszName">Name of the variable</param>
        /// <param name="value">Variable value</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetFixedVariable(String pszName, Double value);

        /// <summary>
        /// Reads a fixed point type variable from the drive.
        /// </summary>
        /// <param name="pszName">Name of the variable</param>
        /// <param name="value">Variable value</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_GetFixedVariable(String pszName, out Double value);

        /// <summary>
        /// Download a data buffer to the drive's memory. 
        /// </summary>
        /// <param name="address">Start address where to download the data buffer</param>
        /// <param name="arrayValues">Buffer containing the data to be downloaded</param>
        /// <param name="nSize">the number of words to download</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetBuffer(UInt16 address, UInt16[] arrayValues, UInt16 nSize);

        /// <summary>
        /// Upload a data buffer from the drive (get it from motion chip's memory). 
        /// </summary>
        /// <param name="address">Start address where from to upload the data buffer</param>
        /// <param name="arrayValues">output Buffer address where the uploaded data will be stored</param>
        /// <param name="nSize">the number of words to upload</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_GetBuffer(UInt16 address, UInt16[] arrayValues, UInt16 nSize);

        // *******************************************************************
        // *******************MOTION functions********************************
        // *******************************************************************

        /// <summary>
        /// Move Absolute with trapezoidal speed profile. 
        ///     This function allows you to program a position profile 
        ///     with a trapezoidal shape of the speed.
        /// </summary>
        /// <param name="AbsPosition">Absolute position target value</param>
        /// <param name="Speed">Slew speed; if 0, use previously defined value</param>
        /// <param name="Acceleration">Acceleration  decceleration; if 0, use previously defined value</param>
        /// <param name="MoveMoment">UPDATE_NONE -> setup motion parameters, movement will start later (on an Update command)
        ///             	         UPDATE_IMMEDIATE -> start moving immediate
        ///		                     UPDATE_ON_EVENT -> start moving on event</param>
        /// <param name="ReferenceBase">FROM_MEASURE -> the position reference starts from the actual measured position value
        ///             		        FROM_REFERENCE -> the position reference starts from the actual reference position value</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_MoveAbsolute(Int32 AbsPosition, Double Speed, Double Acceleration, Int16 MoveMoment, Int16 ReferenceBase);

        /// <summary>
        /// Move Relative with trapezoidal speed profile. 
        ///     This function allows you to program a position profile 
        ///     with a trapezoidal shape of the speed.
        /// </summary>
        /// <param name="RelPosition">Relative position target value</param>
        /// <param name="Speed">Slew speed; if 0, use previously defined value</param>
        /// <param name="Acceleration">Acceleration  decceleration; if 0, use previously defined value</param>
        /// <param name="IsAdditive">TRUE -> Add the position increment to the position to reach set by the previous motion command
        ///          		         FALSE -> No position increment is added to the target position</param>
        /// <param name="MoveMoment">UPDATE_NONE -> setup motion parameters, movement will start latter (on an Update command)
        ///         		         UPDATE_IMMEDIATE -> start moving immediate
        ///		                     UPDATE_ON_EVENT -> start moving on event</param>
        /// <param name="ReferenceBase">FROM_MEASURE -> the position reference starts from the actual measured position value
        ///             		        FROM_REFERENCE -> the position reference starts from the actual reference position value</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_MoveRelative(Int32 RelPosition, Double Speed, Double Acceleration, Boolean IsAdditive, Int16 MoveMoment, Int16 ReferenceBase);

        /// <summary>
        /// Move at a given speed, with acceleration profile.
        /// </summary>
        /// <param name="Speed">Jogging speed</param>
        /// <param name="Acceleration">Acceleration  decceleration; if 0, use previously defined value</param>
        /// <param name="MoveMoment">UPDATE_NONE -> setup motion parameters, movement will start latter (on an Update command)
        ///         		         UPDATE_IMMEDIATE -> start moving immediate
        ///     		             UPDATE_ON_EVENT -> start moving on event</param>
        /// <param name="ReferenceBase">FROM_MEASURE -> the position reference starts from the actual measured position value
        ///             		        FROM_REFERENCE -> the position reference starts from the actual reference position value</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_MoveVelocity(Double Speed, Double Acceleration, Int16 MoveMoment, Int16 ReferenceBase);

        /// <summary>
        /// Set Motion type as using an analogue external reference. 
        /// </summary>
        /// <param name="ReferenceType">REFERENCE_POSITION -> external position reference
        ///		                        REFERENCE_SPEED -> external speed reference
        ///		                        REFERENCE_TORQUE -> external torque reference
        ///     		                REFERENCE_VOLTAGE -> external voltage reference</param>
        /// <param name="UpdateFast">TRUE -> generate the torque reference in the fast control loop
        ///         		         FALSE -> generate the torque reference in the slow control loop</param>
        /// <param name="LimitVariation">NO_VARIATION (0) -> the external reference is limited at the value set in the Drive Setup
        ///         		             A value which can be an acceleration or speed in function of the reference type.</param>
        /// <param name="MoveMoment">UPDATE_NONE -> setup motion parameters, movement will start latter (on an Update command)
        ///                          UPDATE_IMMEDIATE -> start moving immediate
        ///     		             UPDATE_ON_EVENT -> start moving on event</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetAnalogueMoveExternal (Int16 ReferenceType, Boolean UpdateFast, Double LimitVariation, Int16 MoveMoment);

        /// <summary>
        /// Set Motion type as using a digital external reference. This function is used only for Positioning.
        /// </summary>
        /// <param name="SetGearRatio">Set the gear parameters; if TRUE, following parameters are needed</param>
        /// <param name="Denominator">Gear master ratio</param>
        /// <param name="Numerator">Gear slave ratio</param>
        /// <param name="LimitVariation">NO_VARIATION (0) -> the external reference is limited at the value set in the Drive Setup
        ///         		             A value which can be an acceleration or speed in function of the reference type.</param>
        /// <param name="MoveMoment">UPDATE_NONE -> setup motion parameters, movement will start latter (on an Update command)
        ///                          UPDATE_IMMEDIATE -> start moving immediate
        ///     		             UPDATE_ON_EVENT -> start moving on event</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetDigitalMoveExternal (Boolean SetGearRatio, Int16 Denominator, Int16 Numerator, Double LimitVariation, Int16 MoveMoment);

        /// <summary>
        /// Set Motion type as using an analogue external reference.
        /// </summary>
        /// <param name="ReferenceType">REFERENCE_POSITION -> external position reference
        ///		                        REFERENCE_SPEED -> external speed reference
        ///		                        REFERENCE_TORQUE -> external torque reference
        ///     		                REFERENCE_VOLTAGE -> external voltage reference</param>
        /// <param name="LimitVariation">NO_VARIATION (0) -> the external reference is limited at the value set in the Drive Setup
        ///         		             A value which can be an acceleration or speed in function of the reference type.</param>
        /// <param name="InitialValue">If non zero, set initial value of EREF</param>
        /// <param name="MoveMoment">UPDATE_NONE -> setup motion parameters, movement will start latter (on an Update command)
        ///                          UPDATE_IMMEDIATE -> start moving immediate
        ///     		             UPDATE_ON_EVENT -> start moving on event</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetOnlineMoveExternal (Int16 ReferenceType, Double LimitVariation, Double InitialValue,  Int16 MoveMoment);

        /// <summary>
        /// Use voltage test mode. 
        /// </summary>
        /// <param name="MaxVoltage">Maximum test voltage value</param>
        /// <param name="IncrVoltage">Voltage increment on each slow sampling period</param>
        /// <param name="Theta0">Initial value of electrical angle value
        ///     	             Remark: used only for AC motors; set to 0 otherwise</param>
        /// <param name="Dtheta"></param>
        /// <param name="MoveMoment">UPDATE_NONE -> setup motion parameters, movement will start latter (on an Update command)
        ///                          UPDATE_IMMEDIATE -> start moving immediate
        ///     		             UPDATE_ON_EVENT -> start moving on event</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_VoltageTestMode(Int16 MaxVoltage, Int16 IncrVoltage, Int16 Theta0, Int16 Dtheta, Int16 MoveMoment);

        /// <summary>
        /// Use torque test mode.
        /// </summary>
        /// <param name="MaxTorque">Maximum test torque value</param>
        /// <param name="IncrTorque">Torque increment on each slow sampling period</param>
        /// <param name="Theta0">Initial value of electrical angle value
        ///     	             Remark: used only for AC motors; set to 0 otherwise</param>
        /// <param name="Dtheta">Electric angle increment on each slow sampling period</param>
        /// <param name="MoveMoment">UPDATE_NONE -> setup motion parameters, movement will start latter (on an Update command)
        ///                          UPDATE_IMMEDIATE -> start moving immediate
        ///     		             UPDATE_ON_EVENT -> start moving on event</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_TorqueTestMode(Int16 MaxTorque, Int16 IncrTorque, Int16 Theta0, Int16 Dtheta, Int16 MoveMoment);

        /// <summary>
        /// Setup master parameters in gearing mode.
        /// </summary>
        /// <param name="Group">TRUE -> set slave group ID with value; 
        ///     		        FALSE-> set slave axis ID with SlaveID value;</param>
        /// <param name="SlaveID">Axis ID in the case that Group is FALSE or a Group ID when Group is TRUE</param>
        /// <param name="ReferenceBase">FROM_MEASURE -> send position feedback
        ///         		            FROM_REFERENCE -> send position reference</param>
        /// <param name="Enable">TRUE -> enable gearing operation; FALSE -> disable gearing operation</param>
        /// <param name="SetSlavePos">TRUE -> initialize slave(s) with master position</param>
        /// <param name="MoveMoment">UPDATE_NONE -> setup motion parameters, movement will start latter (on an Update command)
        ///		                     UPDATE_IMMEDIATE -> start moving immediate
        ///		                     UPDATE_ON_EVENT -> start moving on event</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetGearingMaster(Boolean Group, Byte SlaveID, Int16 ReferenceBase, Boolean Enable, 
                        Boolean SetSlavePos, Int16 MoveMoment);

        /// <summary>
        /// Setup slave parameters in gearing mode.
        /// </summary>
        /// <param name="Denominator">Master gear ratio value</param>
        /// <param name="Numerator">Slave gear ratio value</param>
        /// <param name="ReferenceBase">FROM_MEASURE -> the position reference starts from the actual measured position value;
        ///     		                FROM_REFERENCE -> the position reference starts from the actual reference position value</param>
        /// <param name="EnableSlave">SLAVE_NONE -> do not enable slave operation;
        ///		                      SLAVE_COMMUNICATION_CHANNEL -> enable operation got via a communication channel;
        ///		                      SLAVE_2ND_ENCODER -> enable operation read from 2nd encoder or P&amp;D inputs</param>
        /// <param name="LimitVariation">NO_VARIATION (0) -> the external reference is limited at the value set in the Drive Setup
        /// 		                     A value which can be an acceleration or speed in function of the reference type.</param>
        /// <param name="MoveMoment">UPDATE_NONE -> setup motion parameters, movement will start latter (on an Update command)
        ///		                     UPDATE_IMMEDIATE -> start moving immediate
        ///		                     UPDATE_ON_EVENT -> start moving on event</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetGearingSlave(Int16 Denominator, Int16 Numerator, Int16 ReferenceBase, 
                                           Int16 EnableSlave, Double LimitVariation, Int16 MoveMoment);

        /// <summary>
        /// enable or disable the superposition of the electronic gearing mode with a second motion mode
        /// </summary>
        /// <param name="Enable">if 0, disable the Superposition mode
        ///     			     if 1, enable the Superposition mode</param>
        /// <param name="Update">if 0, doesn't send UPD command to the drive, in order to take into account the Superposition mode
        /// 			         if 1, sends UPD command to the drive, in order to take into account the Superposition mode</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_MotionSuperposition(Int16 Enable, Int16 Update);

        /// <summary>
        /// Setup master parameters in camming mode.
        /// </summary>
        /// <param name="Group">TRUE -> set slave group ID with (SlaveID + 256) value; 
        /// 		            FALSE-> set slave axis ID with SlaveID value;</param>
        /// <param name="SlaveID">Axis ID in case Group is FALSE, or group mask otherwise (0 means broadcast)</param>
        /// <param name="ReferenceBase">FROM_MEASURE -> send position feedback
        /// 		                    FROM_REFERENCE -> send position reference</param>
        /// <param name="Enable">TRUE -> enable camming operation; FALSE -> disable camming operation</param>
        /// <param name="MoveMoment">UPDATE_NONE -> setup motion parameters, movement will start latter (on an Update command)
        ///         		         UPDATE_IMMEDIATE -> start moving immediate
        ///		                     UPDATE_ON_EVENT -> start moving on event</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetCammingMaster(Boolean Group, Byte SlaveID, Int16 ReferenceBase, Boolean Enable, Int16 MoveMoment);

        /// <summary>
        /// Download a CAM file to the drive, at a specified address.
        /// </summary>
        /// <param name="pszCamFile">the name of the file containing the CAM information</param>
        /// <param name="wLoadAddress">memory address where the CAM is loaded </param>
        /// <param name="wRunAddress">memory where the actual CAM table is transfered and executed at run time</param>
        /// <param name="wNextLoadAddr">memory address available for the next CAM file; if 0 there is no memory left</param>
        /// <param name="wNexRunAddr">memory where the next CAM table is transfered and executed at run time;</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_CamDownload(String pszCamFile, UInt16 wLoadAddress, UInt16 wRunAddress, out UInt16 wNextLoadAddr, out UInt16 wNexRunAddr);

        /// <summary>
        /// Copies a CAM file from E2ROM to RAM memory. You should not use this if you download CAMs directly to RAM memory (load address == run address)
        /// </summary>
        /// <param name="LoadAddress">memory address in E2ROM where the CAM is already loaded</param>
        /// <param name="RunAddress">memory address in RAM where the CAM is copied.</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_CamInitialization(UInt16 LoadAddress, UInt16 RunAddress);

        /// <summary>
        /// Setup slave parameters in relative camming mode.
        /// </summary>
        /// <param name="RunAddress">memory addresses where the CAM is executed at run time. If any of them is 0 it means that no start address is set</param>
        /// <param name="ReferenceBase">FROM_MEASURE -> the position reference starts from the actual measured position value
        ///                             FROM_REFERENCE -> the position reference starts from the actual reference position value</param>
        /// <param name="EnableSlave">SLAVE_NONE -> do not enable slave operation
        ///		                      SLAVE_COMMUNICATION_CHANNEL -> enable operation got via a communication channel
        ///     		              SLAVE_2ND_ENCODER -> enable operation read from 2nd encoder or P&amp;D inputs</param>
        /// <param name="MoveMoment">UPDATE_NONE -> setup motion parameters, movement will start latter (on an Update command)
        ///		                     UPDATE_IMMEDIATE -> start moving immediate
        ///		                     UPDATE_ON_EVENT -> start moving on event</param>
        /// <param name="OffsetFromMaster">if non-zero, set the correspondent parameter</param>
        /// <param name="MultInputFactor">if non-zero, set the correspondent parameter</param>
        /// <param name="MultOutputFactor">if non-zero, set the correspondent parameter</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetCammingSlaveRelative(UInt16 RunAddress, Int16 ReferenceBase, Int16 EnableSlave, Int16 MoveMoment,
                                                      Int32 OffsetFromMaster, Double MultInputFactor, Double MultOutputFactor);

        /// <summary>
        /// Setup slave parameters in absolute camming mode.
        /// </summary>
        /// <param name="RunAddress">memory addresses where the CAM is executed at run time. If any of them is 0 it means that no start address is set</param>
        /// <param name="LimitVariation">NO_VARIATION (0) -> no limitation on speed value at the value set in the Drive Setup
        ///		                         A value which can be an acceleration or speed in function of the reference type.</param>
        /// <param name="ReferenceBase">FROM_MEASURE -> the position reference starts from the actual measured position value
        ///		                        FROM_REFERENCE -> the position reference starts from the actual reference position value</param>
        /// <param name="EnableSlave">SLAVE_NONE -> do not enable slave operation
        ///		                      SLAVE_COMMUNICATION_CHANNEL -> enable operation got via a communication channel
        ///		                      SLAVE_2ND_ENCODER -> enable operation read from 2nd encoder or P&amp;D inputs</param>
        /// <param name="MoveMoment">UPDATE_NONE -> setup motion parameters, movement will start latter (on an Update command)
        ///		                     UPDATE_IMMEDIATE -> start moving immediate
        ///		                     UPDATE_ON_EVENT -> start moving on event</param>
        /// <param name="OffsetFromMaster">if non-zero, set the correspondent parameter</param>
        /// <param name="MultInputFactor">if non-zero, set the correspondent parameter</param>
        /// <param name="MultOutputFactor">if non-zero, set the correspondent parameter</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetCammingSlaveAbsolute(UInt16 RunAddress, Double LimitVariation, Int16 ReferenceBase, Int16 EnableSlave, Int16 MoveMoment,
                                                      Int32 OffsetFromMaster, Double MultInputFactor, Double MultOutputFactor);

        /// <summary>
        /// Setup the resolution for the master encoder connected on the second encoder input of the drive.
        /// </summary>
        /// <param name="MasterResolution">FULL_RANGE (0) -> select this option if the master position is not cyclic. 
        ///                                (e.g. the resolution is equal with the whole  32-bit range of position)
        ///		                           Value that reprezents the number of lines of the 2nd master encoder</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetMasterResolution(Int32 MasterResolution);

        /// <summary>
        /// Setup drives to send synchronization messages.
        /// </summary>
        /// <param name="Period">the time period between 2 sends</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SendSynchronization (Int32 Period);

        /// <summary>
        /// Stop the motion.
        /// </summary>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_Stop();

        /// <summary>
        /// Set the deceleration rate used for QuickStop or SCurve positioning profile.
        /// </summary>
        /// <param name="Deceleration">the value of the deceleration rate</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_QuickStopDecelerationRate(Double Deceleration);

        /// <summary>
        /// Sends a PVT point to the drive.
        /// </summary>
        /// <param name="Position">drive position for the desired point</param>
        /// <param name="Velocity">desired velocity of the drive at the point</param>
        /// <param name="Time">amount of time for the segment</param>
        /// <param name="PVTCounter">integrity counter for current PVT point</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SendPVTPoint(Int32 Position, Double Velocity, UInt32 Time, Int16 PVTCounter);

        /// <summary>
        /// Sends the first point from a series of PVT points and sets the PVT motion mode.
        /// </summary>
        /// <param name="Position">drive position for the desired point</param>
        /// <param name="Velocity">desired velocity of the drive at the point</param>
        /// <param name="Time">amount of time for the segment</param>
        /// <param name="PVTCounter">integrity counter for current PVT point</param>
        /// <param name="PositionType">ABSOLUTE_POSITION or RELATIVE_POSITION</param>
        /// <param name="InitialPosition">drive initial position at the start of an absolute PVT movement.
        ///							      It is taken into consideration only if an absolute movement is requested</param>
        /// <param name="MoveMoment">UPDATE_NONE -> setup motion parameters, movement will start latter (on an Update command)
        ///		                     UPDATE_IMMEDIATE -> start moving immediate
        ///		                     UPDATE_ON_EVENT -> start moving on event</param>
        /// <param name="ReferenceBase">FROM_MEASURE -> the position reference starts from the actual measured position value
        ///                             FROM_REFERENCE -> the position reference starts from the actual reference position value</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SendPVTFirstPoint(Int32 Position,Double Velocity, Int32 Time, Int16 PVTCounter,
                                         Int16 PositionType, Int32 InitialPosition, Int16 MoveMoment, Int16 ReferenceBase);

        /// <summary>
        /// For PVT motion mode parametrization and setup.
        /// </summary>
        /// <param name="ClearBuffer">0 -> nothing
        ///						      1 -> clears the PVT buffer</param>
        /// <param name="IntegrityChecking">0 -> PVT integrity counter checking is active (default)
        ///								    1 -> PVT integrity counter checking is inactive</param>
        /// <param name="ChangePVTCounter">0 -> nothing
        ///							       1 -> drive internal PVT integrity counter is changed with the value specified PVTCounterValue</param>
        /// <param name="AbsolutePositionSource">specifies the source for the initial position in case the PVT motion mode will be absolute
        ///							             0 -> initial position read from PVTPOS0
        ///							             1 -> initial position read from current value of target positio (TPOS)</param>
        /// <param name="ChangeLowLevel">0 -> nothing
        ///								 1 -> the parameter for BufferLow signaling is changed with the value specified LowLevelValue</param>
        /// <param name="PVTCounterValue">New value for the drive internal PVT integrity counter</param>
        /// <param name="LowLevelValue">New value for the level of the BufferLow signal</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_PVTSetup(Int16 ClearBuffer, Int16 IntegrityChecking, Int16 ChangePVTCounter,
                                Int16 AbsolutePositionSource, Int16 ChangeLowLevel, Int16 PVTCounterValue, Int16 LowLevelValue);

        /// <summary>
        /// Sends a PT point to the drive.
        /// </summary>
        /// <param name="Position">drive position for the desired point</param>
        /// <param name="Time">amount of time for the segment</param>
        /// <param name="PTCounter">integrity counter for current PT point</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SendPTPoint(Int32 Position, Int32 Time, Int16 PTCounter);

        /// <summary>
        /// Sends the first point from a series of PT points and sets the PT motion mode.
        /// </summary>
        /// <param name="Position">drive position for the desired point</param>
        /// <param name="Time">amount of time for the segment</param>
        /// <param name="PTCounter">integrity counter for current PT point</param>
        /// <param name="PositionType">ABSOLUTE_POSITION or RELATIVE_POSITION</param>
        /// <param name="InitialPosition">drive initial position at the start of an absolute PT movement.
        ///							      It is taken into consideration only if an absolute movement is requested</param>
        /// <param name="MoveMoment">UPDATE_NONE -> setup motion parameters, movement will start latter (on an Update command)
        ///		                     UPDATE_IMMEDIATE -> start moving immediate
        ///		                     UPDATE_ON_EVENT -> start moving on event</param>
        /// <param name="ReferenceBase">FROM_MEASURE -> the position reference starts from the actual measured position value
        ///		                        FROM_REFERENCE -> the position reference starts from the actual reference position value</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SendPTFirstPoint(Int32 Position, Int32 Time, Int16 PTCounter,
                                         Int16 PositionType, Int32 InitialPosition, Int16 MoveMoment, Int16 ReferenceBase);

        /// <summary>
        /// For PT motion mode parametrization and setup.
        /// </summary>
        /// <param name="ClearBuffer">0 -> nothing
        ///						      1 -> clears the PT buffer</param>
        /// <param name="IntegrityChecking">0 -> PT integrity counter checking is active (default)
        ///                                 1 -> PT integrity counter checking is inactive</param>
        /// <param name="ChangePTCounter">0 -> nothing
        ///                               1 -> drive internal PT integrity counter is changed with the value specified PTCounterValue</param>
        /// <param name="AbsolutePositionSource">specifies the source for the initial position in case the PT motion mode will be absolute
        ///                                      0 -> initial position read from PVTPOS0
        ///                                      1 -> initial position read from current value of target positio (TPOS)</param>
        /// <param name="ChangeLowLevel">0 -> nothing
        ///                              1 -> the parameter for BufferLow signaling is changed with the value specified LowLevelValue</param>
        /// <param name="PTCounterValue">New value for the drive internal PT integrity counter</param>
        /// <param name="LowLevelValue">New value for the level of the BufferLow signal</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_PTSetup(Int16 ClearBuffer, Int16 IntegrityChecking, Int16 ChangePTCounter,
                                Int16 AbsolutePositionSource, Int16 ChangeLowLevel, Int16 PTCounterValue, Int16 LowLevelValue);

        /// <summary>
        /// For relative S-Curve motion mode.
        /// </summary>
        /// <param name="RelPosition">Relative position reference value</param>
        /// <param name="Speed">Slew speed</param>
        /// <param name="Acceleration">Acceleration  decceleration</param>
        /// <param name="JerkTime">The time after the acceleration reaches the desired value.</param>
        /// <param name="MoveMoment">UPDATE_NONE -> setup motion parameters, movement will start latter (on an Update command)
        ///            		         UPDATE_IMMEDIATE -> start moving immediate
        ///		                     UPDATE_ON_EVENT -> start moving on event</param>
        /// <param name="DecelerationType">S_CURVE_SPEED_PROFILE -> s-curve speed profile
        ///                 		       TRAPEZOIDAL_SPEED_PROFILE -> trapezoidal speed profile</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_MoveSCurveRelative(Int32 RelPosition, Double Speed, Double Acceleration, Int32 JerkTime, Int16 MoveMoment, Int16 DecelerationType);

        /// <summary>
        /// For absolute S-Curve motion mode.
        /// </summary>
        /// <param name="AbsPosition">Absolute position reference value</param>
        /// <param name="Speed">Slew speed</param>
        /// <param name="Acceleration">Acceleration  decceleration</param>
        /// <param name="JerkTime">The time after wich the acceleration reaches the desired value.</param>
        /// <param name="MoveMoment">UPDATE_NONE -> setup motion parameters, movement will start latter (on an Update command)
        ///		                     UPDATE_IMMEDIATE -> start moving immediate
        ///		                     UPDATE_ON_EVENT -> start moving on event</param>
        /// <param name="DecelerationType">S_CURVE_SPEED_PROFILE -> s-curve speed profile
        ///		                           TRAPEZOIDAL_SPEED_PROFILE -> trapezoidal speed profile</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_MoveSCurveAbsolute(Int32 AbsPosition, Double Speed, Double Acceleration, Int32 JerkTime, Int16 MoveMoment, Int16 DecelerationType);

        // *******************************************************************
        // *******************EVENT-RELATED functions*************************
        // *******************************************************************

        /// <summary>
        /// Check if the actually active event occured.
        /// </summary>
        /// <param name="eventDetected">TRUE on event detected</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_CheckEvent(out Boolean eventDetected);

        /// <summary>
        /// Setup event when the motion is complete.
        /// </summary>
        /// <param name="WaitEvent">TRUE -> Wait until event occurs; FALSE -> Continue</param>
        /// <param name="EnableStop">TRUE -> On motion complete, stop the motion, FALSE -> Don't stop the motion</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetEventOnMotionComplete(Boolean WaitEvent, Boolean EnableStop);

        /// <summary>
        /// Setup event when motor position is over/under imposed value.
        /// </summary>
        /// <param name="PositionType">ABSOLUTE_POSITION or RELATIVE_POSITION</param>
        /// <param name="Position">Position value to be reached</param>
        /// <param name="Over">TRUE -> Look for position over; FALSE -> Look for position below</param>
        /// <param name="WaitEvent">TRUE -> Wait until event occurs; FALSE -> Continue</param>
        /// <param name="EnableStop">TRUE -> On event, stop the motion, FALSE -> Don't stop the motion</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetEventOnMotorPosition(Int16 PositionType, Int32 Position, Boolean Over, Boolean WaitEvent, Boolean EnableStop);

        /// <summary>
        /// Setup event when load position is over/under imposed value.
        /// </summary>
        /// <param name="PositionType">ABSOLUTE_POSITION or RELATIVE_POSITION</param>
        /// <param name="Position">Position value to be reached</param>
        /// <param name="Over">TRUE -> Look for position over; FALSE -> Look for position below</param>
        /// <param name="WaitEvent">TRUE -> Wait until event occurs; FALSE -> Continue</param>
        /// <param name="EnableStop">TRUE -> On event, stop the motion, FALSE -> Don't stop the motion</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetEventOnLoadPosition(Int16 PositionType, Int32 Position, Boolean Over, Boolean WaitEvent, Boolean EnableStop);

        /// <summary>
        /// Setup event when motor speed is over/under imposed value.
        /// </summary>
        /// <param name="Speed">Speed value to be reached</param>
        /// <param name="Over">TRUE -> Look for speed over; FALSE -> Look for speed below</param>
        /// <param name="WaitEvent">TRUE -> Wait until event occurs; FALSE -> Continue</param>
        /// <param name="EnableStop">TRUE -> On event, stop the motion, FALSE -> Don't stop the motion</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetEventOnMotorSpeed(Double Speed, Boolean Over, Boolean WaitEvent, Boolean EnableStop);

        /// <summary>
        /// Setup event when load speed is over/under imposed value.
        /// </summary>
        /// <param name="Speed">Speed value to be reached</param>
        /// <param name="Over">TRUE -> Look for speed over; FALSE -> Look for speed below</param>
        /// <param name="WaitEvent">TRUE -> Wait until event occurs; FALSE -> Continue</param>
        /// <param name="EnableStop">TRUE -> On event, stop the motion, FALSE -> Don't stop the motion</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetEventOnLoadSpeed(Double Speed, Boolean Over, Boolean WaitEvent, Boolean EnableStop);

        /// <summary>
        /// Setup event after a time interval.
        /// </summary>
        /// <param name="Time">Time after which the event will be set</param>
        /// <param name="WaitEvent">TRUE -> Wait until event occurs; FALSE -> Continue</param>
        /// <param name="EnableStop">TRUE -> On event, stop the motion, FALSE -> Don't stop the motion</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetEventOnTime(UInt16 Time, Boolean WaitEvent, Boolean EnableStop);

        /// <summary>
        /// Setup event when position reference is over/under imposed value.
        /// </summary>
        /// <param name="Position">Position value to be reached</param>
        /// <param name="Over">TRUE -> Look for speed over; FALSE -> Look for speed below</param>
        /// <param name="WaitEvent">TRUE -> Wait until event occurs; FALSE -> Continue</param>
        /// <param name="EnableStop">TRUE -> On event, stop the motion, FALSE -> Don't stop the motion</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetEventOnPositionRef(Int32 Position, Boolean Over, Boolean WaitEvent, Boolean EnableStop);

        /// <summary>
        /// Setup event when speed reference is over/under imposed value.
        /// </summary>
        /// <param name="Speed">Speed value to be reached</param>
        /// <param name="Over">TRUE -> Look for speed over; FALSE -> Look for speed below</param>
        /// <param name="WaitEvent">TRUE -> Wait until event occurs; FALSE -> Continue</param>
        /// <param name="EnableStop">TRUE -> On event, stop the motion, FALSE -> Don't stop the motion</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetEventOnSpeedRef(Double Speed, Boolean Over, Boolean WaitEvent, Boolean EnableStop);

        /// <summary>
        /// Setup event when torque reference is over/under imposed value.
        /// </summary>
        /// <param name="Torque">Torque value to be reached</param>
        /// <param name="Over">TRUE -> Look for speed over; FALSE -> Look for speed below</param>
        /// <param name="WaitEvent">TRUE -> Wait until event occurs; FALSE -> Continue</param>
        /// <param name="EnableStop">TRUE -> On event, stop the motion, FALSE -> Don't stop the motion</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetEventOnTorqueRef(int Torque, Boolean Over, Boolean WaitEvent, Boolean EnableStop);

        /// <summary>
        /// Setup event when encoder index is triggered.
        /// </summary>
        /// <param name="IndexType">INDEX_1 or INDEX_2</param>
        /// <param name="TransitionType">TRANSITION_HIGH_TO_LOW or TRANSITION_LOW_TO_HIGH</param>
        /// <param name="WaitEvent">TRUE -> Wait until event occurs; FALSE -> Continue</param>
        /// <param name="EnableStop">TRUE -> On event, stop the motion, FALSE -> Don't stop the motion</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetEventOnEncoderIndex(Int16 IndexType, Int16 TransitionType, Boolean WaitEvent, Boolean EnableStop);

        /// <summary>
        /// Setup event when selected limit switch is triggered.
        /// </summary>
        /// <param name="LSWType">LSW_NEGATIVE or LSW_POSITIVE</param>
        /// <param name="TransitionType">TRANSITION_HIGH_TO_LOW or TRANSITION_LOW_TO_HIGH</param>
        /// <param name="WaitEvent">TRUE -> Wait until event occurs; FALSE -> Continue</param>
        /// <param name="EnableStop">TRUE -> On event, stop the motion, FALSE -> Don't stop the motion</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetEventOnLimitSwitch(Int16 LSWType, Int16 TransitionType, Boolean WaitEvent, Boolean EnableStop);

        /// <summary>
        /// Setup event when selected input port status is IOState.
        /// </summary>
        /// <param name="InputPort">Input port number</param>
        /// <param name="IOState">IO_LOW or IO_HIGH</param>
        /// <param name="WaitEvent">TRUE -> Wait until event occurs; FALSE -> Continue</param>
        /// <param name="EnableStop">TRUE -> On event, stop the motion, FALSE -> Don't stop the motion</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetEventOnDigitalInput(Byte InputPort, Int16 IOState, Boolean WaitEvent, Boolean EnableStop);

        /// <summary>
        /// Setup event when selected input port status is IOState.
        /// </summary>
        /// <param name="IOState">IO_LOW or IO_HIGH</param>
        /// <param name="WaitEvent">TRUE -> Wait until event occurs; FALSE -> Continue</param>
        /// <param name="EnableStop">TRUE -> On event, stop the motion, FALSE -> Don't stop the motion</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetEventOnHomeInput(Int16 IOState, Boolean WaitEvent, Boolean EnableStop);

        // *******************************************************************
        // *******************INPUT / OUTPUT functions************************
        // *******************************************************************

        /// <summary>
        /// Setup IO port as input.
        /// </summary>
        /// <param name="nIO">Port number to be set as input</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetupInput(Byte nIO);

        /// <summary>
        /// Get input port status.
        /// </summary>
        /// <param name="nIO">Input port number to be read</param>
        /// <param name="InValue">the input port status value (0 or 1)</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_GetInput(Byte nIO, out Byte InValue);

        /// <summary>
        /// Get home input port status. 
        /// </summary>
        /// <param name="InValue">the input port status value (0 or 1)</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_GetHomeInput(out Byte InValue);

        /// <summary>
        /// Setup IO port as output.
        /// </summary>
        /// <param name="nIO">Port number to be set as output</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetupOutput(Byte nIO);

        /// <summary>
        /// Set output port status.
        /// </summary>
        /// <param name="nIO">Output port number to be written</param>
        /// <param name="OutValue">Output port status value to be set (0 or 1)</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetOutput(Byte nIO, Byte OutValue);

        /// <summary>
        /// Read multiple inputs.
        /// </summary>
        /// <param name="pszVarName">temporary variable name used to read input status</param>
        /// <param name="Status">value of multiple input status.</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_GetMultipleInputs(String pszVarName, out Int16 Status);

        /// <summary>
        /// Set multiple outputs (for firmware versions FAxx).
        /// </summary>
        /// <param name="pszVarName">temporary variable name used to set output status</param>
        /// <param name="Status">value to be set</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetMultipleOutputs(String pszVarName, Int16 Status);

        /// <summary>
        /// Set multiple outputs (for firmware versions FBxx).
        /// </summary>
        /// <param name="SelectedPorts">port mask. Set bit n to 1 if you want to update the status of port n.</param>
        /// <param name="Status">value to be set</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetMultipleOutputs2(Int16 SelectedPorts, Int16 Status);

        // *******************************************************************
        // *******************General use*************************************
        // *******************************************************************

        /// <summary>
        /// Send status and error registers to host.
        /// </summary>
        /// <param name="HostAddress">axis ID of host</param>
        /// <param name="StatusRegMask">bit mask for status register </param>
        /// <param name="ErrorRegMask">bit mask for error register</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SendDataToHost(Byte HostAddress, UInt32 StatusRegMask, UInt16 ErrorRegMask);

        /// <summary>
        /// Get checksum of a memory range. 
        /// </summary>
        /// <param name="startAddress">start memory address</param>
        /// <param name="endAddress">end memory address</param>
        /// <param name="checksum">checksum (sum modulo 0xFFFF) of a memory range returned by the active drive/motor  </param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_OnlineChecksum(UInt16 startAddress, UInt16 endAddress, out UInt16 checksum);

        /// <summary>
        /// Download a COFF formatted file to the drive, and return the entry point of that file.
        /// </summary>
        /// <param name="pszOutFile">Path to the output TML object file</param>
        /// <param name="wEntryPoint">the entry point (start address) of the downloaded file</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_DownloadProgram(String pszOutFile, out UInt16 wEntryPoint);

        /// <summary>
        /// Download a .sw file to the drive's E2PROM.
        /// </summary>
        /// <param name="pszSwFile">Path to the SW file generated from EasyMotion Studio/EasySetUp</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_DownloadSwFile(String pszSwFile);

        /// <summary>
        /// Execute a GOTO instruction on the drive.
        /// </summary>
        /// <param name="address">program memory address of the instruction</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_GOTO(UInt16 address);

        /// <summary>
        /// Execute a GOTO instruction on the drive.
        /// </summary>
        /// <param name="pszLabel">label of the instruction</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_GOTO_Label(String pszLabel);

        /// <summary>
        /// Execute a CALL instruction on the drive.
        /// </summary>
        /// <param name="address">address of the procedure</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_CALL(UInt16 address);

        /// <summary>
        /// Execute a CALL instruction on the drive.
        /// </summary>
        /// <param name="pszFunctionName">name of the procedure to be executed</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_CALL_Label(String pszFunctionName);

        /// <summary>
        /// Execute a cancelable call (CALLS) instruction on the drive.
        /// </summary>
        /// <param name="address">address of the procedure</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_CancelableCALL(UInt16 address);

        /// <summary>
        /// Execute a cancelable call (CALLS) instruction on the drive.
        /// </summary>
        /// <param name="pszFunctionName">name of the procedure to be executed</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_CancelableCALL_Label(String pszFunctionName);

        /// <summary>
        /// Execute ABORT instruction on the drive (aborts execution of a procedure called with cancelable call instruction).
        /// </summary>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_ABORT();

        /// <summary>
        /// Execute TML commands entered in TML source code format (as is entered in Command Interpreter).
        /// </summary>
        /// <param name="pszCommands">String containing the TML source code to be executed. Multiple lines are allowed.</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_Execute(String pszCommands);

        /// <summary>
        /// Execute TML commands in TML source code, from a script file (as is entered in Command Interpreter).
        /// </summary>
        /// <param name="pszFileName">The name of the file containing the TML source code to be executed.</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_ExecuteScript(String pszFileName);

        /// <summary>
        /// Return the TML output code of the last previously executed library function call.
        /// </summary>
        /// <param name="pszOutput">String containing the TML source code generated at the last library function call.</param>
        /// <param name="nMaxChars">maximum number of characters to return in the string</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_GetOutputOfExecute(StringBuilder pszOutput, int nMaxChars);

        /// <summary>
        /// Setup logger parameters (could be set up on a group/broadcast destination).
        /// </summary>
        /// <param name="wLogBufferAddr">The address of logger buffer in drive memory, where data will be stored during logging</param>
        /// <param name="wLogBufferLen">The length in WORDs of the logger buffer</param>
        /// <param name="arrayAddresses">An array containing the drive memory addresses to be logged</param>
        /// <param name="countAddr">The number of memory addresses to be logged</param>
        /// <param name="period">How offen to log the data: a value between 1 and 7FFF (useful only for new generation drives).
        ///		                 If it is different than 1, one set of data will be stored at every "period" samplings.</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_SetupLogger(UInt16 wLogBufferAddr, UInt16 wLogBufferLen, UInt16[] arrayAddresses, UInt16 countAddr, UInt16 period);

        /// <summary>
        /// Start the logger on a drive (could be started on a group/broadcast destination).
        /// </summary>
        /// <param name="wLogBufferAddr">address of logger buffer (previously set by TS_SetupLogger)</param>
        /// <param name="LogType">LOGGER_FAST: logging occurs in fast sampling control loop (current loop)
        ///			              LOGGER_SLOW: logging occurs in slow sampling control loop (position/speed loop)</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_StartLogger(UInt16 wLogBufferAddr, Byte LogType);

        /// <summary>
        /// Check logger status. (destination must be a single axis).
        /// </summary>
        /// <param name="wLogBufferAddr">address of logger buffer (previously set by TS_SetupLogger)</param>
        /// <param name="status">Number of points still remaining to capture; if it is 0, the logging is completed</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_CheckLoggerStatus(UInt16 wLogBufferAddr, out UInt16 status);

        /// <summary>
        /// Upload logged data from the drive (destination must be a single axis).
        /// </summary>
        /// <param name="wLogBufferAddr">address of logger buffer (previously set by TS_SetupLogger)</param>
        /// <param name="arrayValues">Pointer to the array where the uploaded data is stored on the PC (output)</param>
        /// <param name="countValues">The size of arrayValues (input/output)</param>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_UploadLoggerResults(UInt16 wLogBufferAddr, UInt16[] arrayValues, ref UInt16 countValues);

        /// <summary>
        /// Register application's handler for unrequested drive messages.
        /// </summary>
        /// <param name="handler">pointer to handler</param>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void TS_RegisterHandlerForUnrequestedDriveMessages(TMLLibConst.pfnCallbackRecvDriveMsg handler);
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void TS_RegisterHandlerForUnrequestedDriveMessages(pfnCallbackRecvDriveMsg handler);
#endif

        /// <summary>
        /// Check if there are new unrequested drive messages and call handler for every message received.
        /// </summary>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_CheckForUnrequestedDriveMessages();

        /// <summary>
        /// Execute ENDINIT command and verify if the setup table is valid. This function 
        ///     must be called only after TS_LoadSetup & TS_SetupAxis & TS_SelectAxis are called.
        ///     If the setup table is invalid then use the EasySetUp or EasyMotion Studio to download
        ///     a valid setup table. Please note that after the setup table download, the drive must
        ///     be reset in order to activate the new setup data.
        /// </summary>
        /// <returns>TRUE if no error; FALSE if error</returns>
#if !TML_SINGLETHREADED
        [DllImport("TML_lib-mt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#else
        [DllImport("TML_lib.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
#endif
        public static extern Boolean TS_DriveInitialisation();
    }

#if !TML_SINGLETHREADED
    public class TMLChannel : ITMLChannel
    {
        // unfortunately, the error functions are library global.  We may have to do something to guarantee that we prevent error retrieval race conditions
        public static String TS_GetLastErrorText() { return TMLLibPInvokes.TS_GetLastErrorText(); }
        public static void TS_Basic_GetLastErrorText(StringBuilder strError, Int32 nBuffSize) { TMLLibPInvokes.TS_Basic_GetLastErrorText(strError, nBuffSize); }

        class TS_OpenChannelWork : BooleanChannelWork
        {
            public String name;
            public Byte type;
            public Byte id;
            public UInt32 baud;
            public override void DoWork() { result = TMLLibPInvokes.TS_OpenChannel(name, type, id, baud) != -1; }
        }
        public Boolean TS_OpenChannel(String pszDevName, Byte btType, Byte nHostID, UInt32 baudrate)
        {
            // close any previous connection on this channel
            if (_thread != null)
                TS_CloseChannel();
                
            // allocate thread exit event
            _exit_event = new ManualResetEvent(false);
            _shutting_down_event = new ManualResetEvent(false);
            _work_ready_event = new ManualResetEvent(false);

            // allocate channel queue
            _work_queue = new Queue<ChannelWork>();
            _work_queue_lock = new Object();

            // launch a thread. This thread will be the marshall thread for all calls across this channel.
            _thread = new Thread(TMLChannelMarshallingThread);
            _thread.IsBackground = true; // allow app to exit without waiting to join thread, although we will join it on calls to TS_CloseChannel
            _thread.Name = "TMLChannel-" + pszDevName + "-" + nHostID;
            _thread.Start();

            var work = new TS_OpenChannelWork{ name = pszDevName, type = btType, id = nHostID, baud = baudrate };
            return WaitForBooleanWorkCompletion(work);
        }
     
        class TS_CloseChannelWork : ChannelWork
        {
            public ManualResetEvent exit_event;
            public ManualResetEvent work_ready_event;
            public override void DoWork() { TMLLibPInvokes.TS_CloseChannel(-1); exit_event.Set(); work_ready_event.Set(); }
        }
        public void TS_CloseChannel()
        {
            if (_thread == null)
                return;

            var work = new TS_CloseChannelWork{ exit_event = _exit_event, work_ready_event = _work_ready_event };
            WaitForWorkCompletion(work, true);

            // this command will signal the thread to exit, so we should Join on it to verify completion
            _thread.Join();
        }

        class TS_LoadSetupWork : ChannelWork
        {
            public String path;
            public Int32 result;
            public override void DoWork() { result = TMLLibPInvokes.TS_LoadSetup(path); }
        }
        public Int32 TS_LoadSetup(String setupPath) { var work = new TS_LoadSetupWork{ path = setupPath }; WaitForWorkCompletion(work); return work.result; }

        class TS_SetupAxisWork : BooleanChannelWork
        {
            public Byte axisId;
            public Int32 idxSetup;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetupAxis(axisId, idxSetup); }
        }
        public Boolean TS_SetupAxis(Byte axisID, Int32 idxSetup) { return WaitForBooleanWorkCompletion(new TS_SetupAxisWork{ axisId = axisID, idxSetup = idxSetup }); }

        class TS_SelectAxisWork : BooleanChannelWork
        {
            public Byte axisId;
            public override void DoWork() { result = TMLLibPInvokes.TS_SelectAxis(axisId); }
        }
        public Boolean TS_SelectAxis(Byte axisID) { return WaitForBooleanWorkCompletion(new TS_SelectAxisWork{ axisId = axisID }); }

        class TS_SetupGroupWork : BooleanChannelWork
        {
            public Byte groupId;
            public Int32 idxSetup;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetupGroup(groupId, idxSetup); }
        }
        public Boolean TS_SetupGroup(Byte groupID, Int32 idxSetup) { return WaitForBooleanWorkCompletion(new TS_SetupGroupWork{ groupId = groupID, idxSetup = idxSetup }); }

        class TS_SelectGroupWork : BooleanChannelWork
        {
            public Byte groupId;
            public override void DoWork() { result = TMLLibPInvokes.TS_SelectGroup(groupId); }
        }
        public Boolean TS_SelectGroup(Byte groupID) { return WaitForBooleanWorkCompletion(new TS_SelectGroupWork{ groupId = groupID }); }

        class TS_SetupBroadcastWork : BooleanChannelWork
        {
            public Int32 idxSetup;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetupBroadcast(idxSetup); }
        }
        public Boolean TS_SetupBroadcast(Int32 idxSetup) { return WaitForBooleanWorkCompletion(new TS_SetupBroadcastWork{ idxSetup = idxSetup }); }

        class TS_SelectBroadcastWork : BooleanChannelWork
        {
            public override void DoWork() { result = TMLLibPInvokes.TS_SelectBroadcast(); }
        }
        public Boolean TS_SelectBroadcast() { return WaitForBooleanWorkCompletion(new TS_SelectBroadcastWork()); }

        class TS_ResetWork : BooleanChannelWork
        {
            public override void DoWork() { result = TMLLibPInvokes.TS_Reset(); }
        }
        public Boolean TS_Reset() { return WaitForBooleanWorkCompletion(new TS_ResetWork()); }

        class TS_ResetFaultWork : BooleanChannelWork
        {
            public override void DoWork() { result = TMLLibPInvokes.TS_ResetFault(); }
        }
        public Boolean TS_ResetFault() { return WaitForBooleanWorkCompletion(new TS_ResetFaultWork()); }

        class TS_PowerWork : BooleanChannelWork
        {
            public Boolean enable;
            public override void DoWork() { result = TMLLibPInvokes.TS_Power(enable); }
        }
        public Boolean TS_Power(Boolean Enable) { return WaitForBooleanWorkCompletion(new TS_PowerWork{ enable = Enable }); }

        class TS_ReadStatusWork : BooleanChannelWork
        {
            public Int16 selIndex;
            public UInt16 status;
            public override void DoWork() { result = TMLLibPInvokes.TS_ReadStatus(selIndex, out status); }
        }
        public Boolean TS_ReadStatus(Int16 SelIndex, out UInt16 Status) { var work = new TS_ReadStatusWork{ selIndex = SelIndex }; var result = WaitForBooleanWorkCompletion(work); Status = work.status; return result; }

        class TS_SaveWork : BooleanChannelWork
        {
            public override void DoWork() { result = TMLLibPInvokes.TS_Save(); }
        }
        public Boolean TS_Save() { return WaitForBooleanWorkCompletion(new TS_SaveWork()); }

        class TS_UpdateImmediateWork : BooleanChannelWork
        {
            public override void DoWork() { result = TMLLibPInvokes.TS_UpdateImmediate(); }
        }
        public Boolean TS_UpdateImmediate() { return WaitForBooleanWorkCompletion(new TS_UpdateImmediateWork()); }

        class TS_UpdateOnEventWork : BooleanChannelWork
        {
            public override void DoWork() { result = TMLLibPInvokes.TS_UpdateOnEvent(); }
        }
        public Boolean TS_UpdateOnEvent() { return WaitForBooleanWorkCompletion(new TS_UpdateOnEventWork()); }

        class TS_SetPositionWork : BooleanChannelWork
        {
            public Int32 position;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetPosition(position); }
        }
        public Boolean TS_SetPosition(Int32 PosValue) { return WaitForBooleanWorkCompletion(new TS_SetPositionWork{ position = PosValue }); }

        class TS_SetCurrentWork : BooleanChannelWork
        {
            public Int16 current;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetCurrent(current); }
        }
        public Boolean TS_SetCurrent(Int16 CrtValue) { return WaitForBooleanWorkCompletion(new TS_SetCurrentWork{ current = CrtValue }); }

        class TS_SetTargetPositionToActualWork : BooleanChannelWork
        {
            public override void DoWork() { result = TMLLibPInvokes.TS_SetTargetPositionToActual(); }
        }
        public Boolean TS_SetTargetPositionToActual() { return WaitForBooleanWorkCompletion(new TS_SetTargetPositionToActualWork()); }

        class TS_GetVariableAddressWork : BooleanChannelWork
        {
            public String name;
            public Int16 address;
            public override void DoWork() { result = TMLLibPInvokes.TS_GetVariableAddress(name, out address); }
        }
        public Boolean TS_GetVariableAddress(String pszName, out Int16 address) { var work = new TS_GetVariableAddressWork{ name = pszName }; var result = WaitForBooleanWorkCompletion(work); address = work.address; return result; }

        class TS_SetIntVariableWork : BooleanChannelWork
        {
            public String name;
            public Int16 value;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetIntVariable(name, value); }
        }
        public Boolean TS_SetIntVariable(String pszName, Int16 value) { return WaitForBooleanWorkCompletion(new TS_SetIntVariableWork{ name = pszName, value = value }); }

        class TS_GetIntVariableWork : BooleanChannelWork
        {
            public String name;
            public Int16 value;
            public override void DoWork() { result = TMLLibPInvokes.TS_GetIntVariable(name, out value); }
        }
        public Boolean TS_GetIntVariable(String pszName, out Int16 value) { var work = new TS_GetIntVariableWork{ name = pszName }; var result = WaitForBooleanWorkCompletion(work); value = work.value; return result; }

        class TS_SetLongVariableWork : BooleanChannelWork
        {
            public String name;
            public Int32 value;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetLongVariable(name, value); }
        }
        public Boolean TS_SetLongVariable(String pszName, Int32 value) { return WaitForBooleanWorkCompletion(new TS_SetLongVariableWork{ name = pszName, value = value }); }

        class TS_GetLongVariableWork : BooleanChannelWork
        {
            public String name;
            public Int32 value;
            public override void DoWork() { result = TMLLibPInvokes.TS_GetLongVariable(name, out value); }
        }
        public Boolean TS_GetLongVariable(String pszName, out Int32 value) { var work = new TS_GetLongVariableWork{ name = pszName }; var result = WaitForBooleanWorkCompletion(work); value = work.value; return result; }

        class TS_SetFixedVariableWork : BooleanChannelWork
        {
            public String name;
            public Double value;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetFixedVariable(name, value); }
        }
        public Boolean TS_SetFixedVariable(String pszName, Double value) { return WaitForBooleanWorkCompletion(new TS_SetFixedVariableWork{ name = pszName, value = value }); }

        class TS_GetFixedVariableWork : BooleanChannelWork
        {
            public String name;
            public Double value;
            public override void DoWork() { result = TMLLibPInvokes.TS_GetFixedVariable(name, out value); }
        }
        public Boolean TS_GetFixedVariable(String pszName, out Double value) { var work = new TS_GetFixedVariableWork{ name = pszName }; var result = WaitForBooleanWorkCompletion(work); value = work.value; return result; }

        class TS_SetBufferWork : BooleanChannelWork
        {
            public UInt16 address;
            public UInt16[] values;
            public UInt16 size;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetBuffer(address, values, size); }
        }
        public Boolean TS_SetBuffer(UInt16 address, UInt16[] arrayValues, UInt16 nSize) { return WaitForBooleanWorkCompletion(new TS_SetBufferWork{ address = address, values = arrayValues, size = nSize }); }

        class TS_GetBufferWork : BooleanChannelWork
        {
            public UInt16 address;
            public UInt16[] values;
            public UInt16 size;
            public override void DoWork() { result = TMLLibPInvokes.TS_GetBuffer(address, values, size); }
        }
        public Boolean TS_GetBuffer(UInt16 address, UInt16[] arrayValues, UInt16 nSize) { return WaitForBooleanWorkCompletion(new TS_GetBufferWork{ address = address, values = arrayValues, size = nSize }); }

        class TS_MoveAbsoluteWork : BooleanChannelWork
        {
            public Int32 position;
            public Double speed;
            public Double accel;
            public Int16 moment;
            public Int16 reference;
            public override void DoWork() { result = TMLLibPInvokes.TS_MoveAbsolute(position, speed, accel, moment, reference); }
        }
        public Boolean TS_MoveAbsolute(Int32 AbsPosition, Double Speed, Double Acceleration, Int16 MoveMoment, Int16 ReferenceBase) { return WaitForBooleanWorkCompletion(new TS_MoveAbsoluteWork{ position = AbsPosition, speed = Speed, accel = Acceleration, moment = MoveMoment, reference = ReferenceBase }); }

        class TS_MoveRelativeWork : BooleanChannelWork
        {
            public Int32 position;
            public Double speed;
            public Double accel;
            public Boolean additive;
            public Int16 moment;
            public Int16 reference;
            public override void DoWork() { result = TMLLibPInvokes.TS_MoveRelative(position, speed, accel, additive, moment, reference); }
        }
        public Boolean TS_MoveRelative(Int32 RelPosition, Double Speed, Double Acceleration, Boolean IsAdditive, Int16 MoveMoment, Int16 ReferenceBase) { return WaitForBooleanWorkCompletion(new TS_MoveRelativeWork{ position = RelPosition, speed = Speed, accel = Acceleration, additive = IsAdditive, moment = MoveMoment, reference = ReferenceBase }); }

        class TS_MoveVelocityWork : BooleanChannelWork
        {
            public Double speed;
            public Double accel;
            public Int16 moment;
            public Int16 reference;
            public override void DoWork() { result = TMLLibPInvokes.TS_MoveVelocity(speed, accel, moment, reference); }
        }
        public Boolean TS_MoveVelocity(Double Speed, Double Acceleration, Int16 MoveMoment, Int16 ReferenceBase) { return WaitForBooleanWorkCompletion(new TS_MoveVelocityWork{ speed = Speed, accel = Acceleration, moment = MoveMoment, reference = ReferenceBase }); }

        class TS_SetAnalogueMoveExternalWork : BooleanChannelWork
        {
            public Int16 reference_type;
            public Boolean update_fast;
            public Double limit_variation;
            public Int16 moment;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetAnalogueMoveExternal(reference_type, update_fast, limit_variation, moment); }
        }
        public Boolean TS_SetAnalogueMoveExternal(Int16 ReferenceType, Boolean UpdateFast, Double LimitVariation, Int16 MoveMoment) { return WaitForBooleanWorkCompletion(new TS_SetAnalogueMoveExternalWork{ reference_type = ReferenceType, update_fast = UpdateFast, limit_variation = LimitVariation, moment = MoveMoment }); }

        class TS_SetDigitalMoveExternalWork : BooleanChannelWork
        {
            public Boolean set_gear_ratio;
            public Int16 denominator;
            public Int16 numerator;
            public Double limit_variation;
            public Int16 moment;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetDigitalMoveExternal(set_gear_ratio, denominator, numerator, limit_variation, moment); }
        }
        public Boolean TS_SetDigitalMoveExternal(Boolean SetGearRatio, Int16 Denominator, Int16 Numerator, Double LimitVariation, Int16 MoveMoment) { return WaitForBooleanWorkCompletion(new TS_SetDigitalMoveExternalWork{ set_gear_ratio = SetGearRatio, denominator = Denominator, numerator = Numerator, limit_variation = LimitVariation, moment = MoveMoment }); }

        class TS_SetOnlineMoveExternalWork : BooleanChannelWork
        {
            public Int16 reference_type;
            public Double limit_variation;
            public Double initial_value;
            public Int16 moment;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetOnlineMoveExternal(reference_type, limit_variation, initial_value, moment); }
        }
        public Boolean TS_SetOnlineMoveExternal(Int16 ReferenceType, Double LimitVariation, Double InitialValue, Int16 MoveMoment) { return WaitForBooleanWorkCompletion(new TS_SetOnlineMoveExternalWork{ reference_type = ReferenceType, limit_variation = LimitVariation, initial_value = InitialValue, moment = MoveMoment }); }

        class TS_VoltageTestModeWork : BooleanChannelWork
        {
            public Int16 max_volts;
            public Int16 inc_volts;
            public Int16 theta_0;
            public Int16 d_theta;
            public Int16 moment;
            public override void DoWork() { result = TMLLibPInvokes.TS_VoltageTestMode(max_volts, inc_volts, theta_0, d_theta, moment); }
        }
        public Boolean TS_VoltageTestMode(Int16 MaxVoltage, Int16 IncrVoltage, Int16 Theta0, Int16 Dtheta, Int16 MoveMoment) { return WaitForBooleanWorkCompletion(new TS_VoltageTestModeWork{ max_volts = MaxVoltage, inc_volts = IncrVoltage, theta_0 = Theta0, d_theta = Dtheta, moment = MoveMoment }); }

        class TS_TorqueTestModeWork : BooleanChannelWork
        {
            public Int16 max_torque;
            public Int16 inc_torque;
            public Int16 theta_0;
            public Int16 d_theta;
            public Int16 moment;
            public override void DoWork() { result = TMLLibPInvokes.TS_TorqueTestMode(max_torque, inc_torque, theta_0, d_theta, moment); }
        }
        public Boolean TS_TorqueTestMode(Int16 MaxTorque, Int16 IncrTorque, Int16 Theta0, Int16 Dtheta, Int16 MoveMoment) { return WaitForBooleanWorkCompletion(new TS_TorqueTestModeWork{ max_torque = MaxTorque, inc_torque = IncrTorque, theta_0 = Theta0, d_theta = Dtheta, moment = MoveMoment }); }

        class TS_SetGearingMasterWork : BooleanChannelWork
        {
            public Boolean group;
            public Byte slave_id;
            public Int16 reference;
            public Boolean enable;
            public Boolean set_slave_pos;
            public Int16 moment;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetGearingMaster(group, slave_id, reference, enable, set_slave_pos, moment); }
        }
        public Boolean TS_SetGearingMaster(Boolean Group, Byte SlaveID, Int16 ReferenceBase, Boolean Enable, Boolean SetSlavePos, Int16 MoveMoment) { return WaitForBooleanWorkCompletion(new TS_SetGearingMasterWork{ group = Group, slave_id = SlaveID, reference = ReferenceBase, enable = Enable, set_slave_pos = SetSlavePos, moment = MoveMoment }); }

        class TS_SetGearingSlaveWork : BooleanChannelWork
        {
            public Int16 denominator;
            public Int16 numerator;
            public Int16 reference;
            public Int16 enable_slave;
            public Double limit_variation;
            public Int16 moment;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetGearingSlave(denominator, numerator, reference, enable_slave, limit_variation, moment); }
        }
        public Boolean TS_SetGearingSlave(Int16 Denominator, Int16 Numerator, Int16 ReferenceBase, Int16 EnableSlave, Double LimitVariation, Int16 MoveMoment) { return WaitForBooleanWorkCompletion(new TS_SetGearingSlaveWork{ denominator = Denominator, numerator = Numerator, reference = ReferenceBase, enable_slave = EnableSlave, limit_variation = LimitVariation, moment = MoveMoment }); }

        class TS_MotionSuperpositionWork : BooleanChannelWork
        {
            public Int16 enable;
            public Int16 update;
            public override void DoWork() { result = TMLLibPInvokes.TS_MotionSuperposition(enable, update); }
        }
        public Boolean TS_MotionSuperposition(Int16 Enable, Int16 Update) { return WaitForBooleanWorkCompletion(new TS_MotionSuperpositionWork{ enable = Enable, update = Update }); }

        class TS_SetCammingMasterWork : BooleanChannelWork
        {
            public Boolean group;
            public Byte slave_id;
            public Int16 reference;
            public Boolean enable;
            public Int16 moment;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetCammingMaster(group, slave_id, reference, enable, moment); }
        }
        public Boolean TS_SetCammingMaster(Boolean Group, Byte SlaveID, Int16 ReferenceBase, Boolean Enable, Int16 MoveMoment) { return WaitForBooleanWorkCompletion(new TS_SetCammingMasterWork{ group = Group, slave_id = SlaveID, reference = ReferenceBase, enable = Enable, moment = MoveMoment }); }

        class TS_CamDownloadWork : BooleanChannelWork
        {
            public String cam_file;
            public UInt16 load_address;
            public UInt16 run_address;
            public UInt16 next_load_address;
            public UInt16 next_run_address;
            public override void DoWork() { result = TMLLibPInvokes.TS_CamDownload(cam_file, load_address, run_address, out next_load_address, out next_run_address); }
        }
        public Boolean TS_CamDownload(String pszCamFile, UInt16 wLoadAddress, UInt16 wRunAddress, out UInt16 wNextLoadAddr, out UInt16 wNexRunAddr) { var work = new TS_CamDownloadWork{ cam_file = pszCamFile, load_address = wLoadAddress, run_address = wRunAddress }; var result = WaitForBooleanWorkCompletion(work); wNextLoadAddr = work.next_load_address; wNexRunAddr = work.next_run_address; return result; }

        class TS_CamInitializationWork : BooleanChannelWork
        {
            public UInt16 load_address;
            public UInt16 run_address;
            public override void DoWork() { result = TMLLibPInvokes.TS_CamInitialization(load_address, run_address); }
        }
        public Boolean TS_CamInitialization(UInt16 LoadAddress, UInt16 RunAddress) { return WaitForBooleanWorkCompletion(new TS_CamInitializationWork{ load_address = LoadAddress, run_address = RunAddress }); }

        class TS_SetCammingSlaveRelativeWork : BooleanChannelWork
        {
            public UInt16 run_address;
            public Int16 reference;
            public Int16 enable_slave;
            public Int16 moment;
            public Int32 offset;
            public Double input_factor;
            public Double output_factor;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetCammingSlaveRelative(run_address, reference, enable_slave, moment, offset, input_factor, output_factor); }
        }
        public Boolean TS_SetCammingSlaveRelative(UInt16 RunAddress, Int16 ReferenceBase, Int16 EnableSlave, Int16 MoveMoment, Int32 OffsetFromMaster, Double MultInputFactor, Double MultOutputFactor) { return WaitForBooleanWorkCompletion(new TS_SetCammingSlaveRelativeWork{ run_address = RunAddress, reference = ReferenceBase, enable_slave = EnableSlave, moment = MoveMoment, offset = OffsetFromMaster, input_factor = MultInputFactor, output_factor = MultOutputFactor }); }

        class TS_SetCammingSlaveAbsoluteWork : BooleanChannelWork
        {
            public UInt16 run_address;
            public Double limit_variation;
            public Int16 reference;
            public Int16 enable_slave;
            public Int16 moment;
            public Int32 offset;
            public Double input_factor;
            public Double output_factor;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetCammingSlaveAbsolute(run_address, limit_variation, reference, enable_slave, moment, offset, input_factor, output_factor); }
        }
        public Boolean TS_SetCammingSlaveAbsolute(UInt16 RunAddress, Double LimitVariation, Int16 ReferenceBase, Int16 EnableSlave, Int16 MoveMoment, Int32 OffsetFromMaster, Double MultInputFactor, Double MultOutputFactor) { return WaitForBooleanWorkCompletion(new TS_SetCammingSlaveAbsoluteWork{ run_address = RunAddress, limit_variation=LimitVariation, reference = ReferenceBase, enable_slave = EnableSlave, moment = MoveMoment, offset = OffsetFromMaster, input_factor = MultInputFactor, output_factor = MultOutputFactor }); }

        class TS_SetMasterResolutionWork : BooleanChannelWork
        {
            public Int32 resolution;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetMasterResolution(resolution); }
        }
        public Boolean TS_SetMasterResolution(Int32 MasterResolution) { return WaitForBooleanWorkCompletion(new TS_SetMasterResolutionWork{ resolution = MasterResolution }); }

        class TS_SendSynchronizationWork : BooleanChannelWork
        {
            public Int32 period;
            public override void DoWork() { result = TMLLibPInvokes.TS_SendSynchronization(period); }
        }
        public Boolean TS_SendSynchronization(Int32 Period) { return WaitForBooleanWorkCompletion(new TS_SendSynchronizationWork{ period = Period }); }

        class TS_StopWork : BooleanChannelWork
        {
            public override void DoWork() { result = TMLLibPInvokes.TS_Stop(); }
        }
        public Boolean TS_Stop() { return WaitForBooleanWorkCompletion(new TS_StopWork()); }

        class TS_QuickStopDecelerationRateWork : BooleanChannelWork
        {
            public Double deceleration;
            public override void DoWork() { result = TMLLibPInvokes.TS_QuickStopDecelerationRate(deceleration); }
        }
        public Boolean TS_QuickStopDecelerationRate(Double Deceleration) { return WaitForBooleanWorkCompletion(new TS_QuickStopDecelerationRateWork{ deceleration = Deceleration }); }

        class TS_SendPVTPointWork : BooleanChannelWork
        {
            public Int32 position;
            public Double velocity;
            public UInt32 time;
            public Int16 counter;
            public override void DoWork() { result = TMLLibPInvokes.TS_SendPVTPoint(position, velocity, time, counter); }
        }
        public Boolean TS_SendPVTPoint(Int32 Position, Double Velocity, UInt32 Time, Int16 PVTCounter) { return WaitForBooleanWorkCompletion(new TS_SendPVTPointWork{ position = Position, velocity = Velocity, time = Time, counter = PVTCounter }); }

        class TS_SendPVTFirstPointWork : BooleanChannelWork
        {
            public Int32 position;
            public Double velocity;
            public Int32 time;
            public Int16 counter;
            public Int16 position_type;
            public Int32 initial_position;
            public Int16 moment;
            public Int16 reference;
            public override void DoWork() { result = TMLLibPInvokes.TS_SendPVTFirstPoint(position, velocity, time, counter, position_type, initial_position, moment, reference); }
        }
        public Boolean TS_SendPVTFirstPoint(Int32 Position, Double Velocity, Int32 Time, Int16 PVTCounter, Int16 PositionType, Int32 InitialPosition, Int16 MoveMoment, Int16 ReferenceBase) { return WaitForBooleanWorkCompletion(new TS_SendPVTFirstPointWork{ position = Position, velocity = Velocity, time = Time, counter = PVTCounter, position_type = PositionType, initial_position = InitialPosition, moment = MoveMoment, reference = ReferenceBase }); }

        class TS_PVTSetupWork : BooleanChannelWork
        {
            public Int16 clear;
            public Int16 integrity_check;
            public Int16 change_counter;
            public Int16 position_source;
            public Int16 change_low_level;
            public Int16 counter_value;
            public Int16 low_level_value;
            public override void DoWork() { result = TMLLibPInvokes.TS_PVTSetup(clear, integrity_check, change_counter, position_source, change_low_level, counter_value, low_level_value); }
        }
        public Boolean TS_PVTSetup(Int16 ClearBuffer, Int16 IntegrityChecking, Int16 ChangePVTCounter, Int16 AbsolutePositionSource, Int16 ChangeLowLevel, Int16 PVTCounterValue, Int16 LowLevelValue) { return WaitForBooleanWorkCompletion(new TS_PVTSetupWork{ clear = ClearBuffer, integrity_check = IntegrityChecking, change_counter = ChangePVTCounter, position_source = AbsolutePositionSource, change_low_level = ChangeLowLevel, counter_value = PVTCounterValue, low_level_value = LowLevelValue }); }

        class TS_SendPTPointWork : BooleanChannelWork
        {
            public Int32 position;
            public Int32 time;
            public Int16 counter;
            public override void DoWork() { result = TMLLibPInvokes.TS_SendPTPoint(position, time, counter); }
        }
        public Boolean TS_SendPTPoint(Int32 Position, Int32 Time, Int16 PTCounter) { return WaitForBooleanWorkCompletion(new TS_SendPTPointWork{ position = Position, time = Time, counter = PTCounter }); }

        class TS_SendPTFirstPointWork : BooleanChannelWork
        {
            public Int32 position;
            public Int32 time;
            public Int16 counter;
            public Int16 position_type;
            public Int32 initial_position;
            public Int16 moment;
            public Int16 reference;
            public override void DoWork() { result = TMLLibPInvokes.TS_SendPTFirstPoint(position, time, counter, position_type, initial_position, moment, reference); }
        }
        public Boolean TS_SendPTFirstPoint(Int32 Position, Int32 Time, Int16 PTCounter, Int16 PositionType, Int32 InitialPosition, Int16 MoveMoment, Int16 ReferenceBase) { return WaitForBooleanWorkCompletion(new TS_SendPTFirstPointWork{ position = Position, time = Time, counter = PTCounter, position_type = PositionType, initial_position = InitialPosition, moment = MoveMoment, reference = ReferenceBase }); }

        class TS_PTSetupWork : BooleanChannelWork
        {
            public Int16 clear_buffer;
            public Int16 integrity_check;
            public Int16 change_counter;
            public Int16 position_source;
            public Int16 change_low_level;
            public Int16 counter_value;
            public Int16 low_level_value;
            public override void DoWork() { result = TMLLibPInvokes.TS_PTSetup(clear_buffer, integrity_check, change_counter, position_source, change_low_level, counter_value, low_level_value); }
        }
        public Boolean TS_PTSetup(Int16 ClearBuffer, Int16 IntegrityChecking, Int16 ChangePTCounter, Int16 AbsolutePositionSource, Int16 ChangeLowLevel, Int16 PTCounterValue, Int16 LowLevelValue) { return WaitForBooleanWorkCompletion(new TS_PTSetupWork{ clear_buffer = ClearBuffer, integrity_check = IntegrityChecking, change_counter = ChangePTCounter, position_source = AbsolutePositionSource, change_low_level = ChangeLowLevel, counter_value = PTCounterValue, low_level_value = LowLevelValue }); }

        class TS_MoveSCurveRelativeWork : BooleanChannelWork
        {
            public Int32 position;
            public Double speed;
            public Double accel;
            public Int32 jerk_time;
            public Int16 moment;
            public Int16 decel_type;
            public override void DoWork() { result = TMLLibPInvokes.TS_MoveSCurveRelative(position, speed, accel, jerk_time, moment, decel_type); }
        }
        public Boolean TS_MoveSCurveRelative(Int32 RelPosition, Double Speed, Double Acceleration, Int32 JerkTime, Int16 MoveMoment, Int16 DecelerationType) { return WaitForBooleanWorkCompletion(new TS_MoveSCurveRelativeWork{ position = RelPosition, speed = Speed, accel = Acceleration, jerk_time = JerkTime, moment = MoveMoment, decel_type = DecelerationType }); }

        class TS_MoveSCurveAbsoluteWork : BooleanChannelWork
        {
            public Int32 position;
            public Double speed;
            public Double accel;
            public Int32 jerk_time;
            public Int16 moment;
            public Int16 decel_type;
            public override void DoWork() { result = TMLLibPInvokes.TS_MoveSCurveAbsolute(position, speed, accel, jerk_time, moment, decel_type); }
        }
        public Boolean TS_MoveSCurveAbsolute(Int32 AbsPosition, Double Speed, Double Acceleration, Int32 JerkTime, Int16 MoveMoment, Int16 DecelerationType) { return WaitForBooleanWorkCompletion(new TS_MoveSCurveAbsoluteWork{ position = AbsPosition, speed = Speed, accel = Acceleration, jerk_time = JerkTime, moment = MoveMoment, decel_type = DecelerationType }); }

        class TS_CheckEventWork : BooleanChannelWork
        {
            public Boolean event_detected;
            public override void DoWork() { result = TMLLibPInvokes.TS_CheckEvent(out event_detected); }
        }
        public Boolean TS_CheckEvent(out Boolean eventDetected) { var work = new TS_CheckEventWork(); var result = WaitForBooleanWorkCompletion(work); eventDetected = work.event_detected; return result; }

        class TS_SetEventOnMotionCompleteWork : BooleanChannelWork
        {
            public Boolean wait;
            public Boolean enable_stop;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetEventOnMotionComplete(wait, enable_stop); }
        }
        public Boolean TS_SetEventOnMotionComplete(Boolean WaitEvent, Boolean EnableStop) { return WaitForBooleanWorkCompletion(new TS_SetEventOnMotionCompleteWork{ wait = WaitEvent, enable_stop = EnableStop }); }

        class TS_SetEventOnMotorPositionWork : BooleanChannelWork
        {
            public Int16 position_type;
            public Int32 position;
            public Boolean over;
            public Boolean wait;
            public Boolean enable_stop;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetEventOnMotorPosition(position_type, position, over, wait, enable_stop); }
        }
        public Boolean TS_SetEventOnMotorPosition(Int16 PositionType, Int32 Position, Boolean Over, Boolean WaitEvent, Boolean EnableStop) { return WaitForBooleanWorkCompletion(new TS_SetEventOnMotorPositionWork{ position_type = PositionType, position = Position, over = Over, wait = WaitEvent, enable_stop = EnableStop }); }

        class TS_SetEventOnLoadPositionWork : BooleanChannelWork
        {
            public Int16 position_type;
            public Int32 position;
            public Boolean over;
            public Boolean wait;
            public Boolean enable_stop;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetEventOnLoadPosition(position_type, position, over, wait, enable_stop); }
        }
        public Boolean TS_SetEventOnLoadPosition(Int16 PositionType, Int32 Position, Boolean Over, Boolean WaitEvent, Boolean EnableStop) { return WaitForBooleanWorkCompletion(new TS_SetEventOnLoadPositionWork{ position_type = PositionType, position = Position, over = Over, wait = WaitEvent, enable_stop = EnableStop }); }

        class TS_SetEventOnMotorSpeedWork : BooleanChannelWork
        {
            public Double speed;
            public Boolean over;
            public Boolean wait;
            public Boolean enable_stop;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetEventOnMotorSpeed(speed, over, wait, enable_stop); }
        }
        public Boolean TS_SetEventOnMotorSpeed(Double Speed, Boolean Over, Boolean WaitEvent, Boolean EnableStop) { return WaitForBooleanWorkCompletion(new TS_SetEventOnMotorSpeedWork{ speed = Speed, over = Over, wait = WaitEvent, enable_stop = EnableStop }); }

        class TS_SetEventOnLoadSpeedWork : BooleanChannelWork
        {
            public Double speed;
            public Boolean over;
            public Boolean wait;
            public Boolean enable_stop;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetEventOnLoadSpeed(speed, over, wait, enable_stop); }
        }
        public Boolean TS_SetEventOnLoadSpeed(Double Speed, Boolean Over, Boolean WaitEvent, Boolean EnableStop) { return WaitForBooleanWorkCompletion(new TS_SetEventOnLoadSpeedWork{ speed = Speed, over = Over, wait = WaitEvent, enable_stop = EnableStop }); }

        class TS_SetEventOnTimeWork : BooleanChannelWork
        {
            public UInt16 time;
            public Boolean wait;
            public Boolean enable_stop;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetEventOnTime(time, wait, enable_stop); }
        }
        public Boolean TS_SetEventOnTime(UInt16 Time, Boolean WaitEvent, Boolean EnableStop) { return WaitForBooleanWorkCompletion(new TS_SetEventOnTimeWork{ time = Time, wait = WaitEvent, enable_stop = EnableStop }); }

        class TS_SetEventOnPositionRefWork : BooleanChannelWork
        {
            public Int32 position;
            public Boolean over;
            public Boolean wait;
            public Boolean enable_stop;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetEventOnPositionRef(position, over, wait, enable_stop); }
        }
        public Boolean TS_SetEventOnPositionRef(Int32 Position, Boolean Over, Boolean WaitEvent, Boolean EnableStop) { return WaitForBooleanWorkCompletion(new TS_SetEventOnPositionRefWork{ position=Position, over = Over, wait = WaitEvent, enable_stop = EnableStop }); }

        class TS_SetEventOnSpeedRefWork : BooleanChannelWork
        {
            public Double speed;
            public Boolean over;
            public Boolean wait;
            public Boolean enable_stop;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetEventOnSpeedRef(speed, over, wait, enable_stop); }
        }
        public Boolean TS_SetEventOnSpeedRef(Double Speed, Boolean Over, Boolean WaitEvent, Boolean EnableStop) { return WaitForBooleanWorkCompletion(new TS_SetEventOnSpeedRefWork{ speed = Speed, over = Over, wait = WaitEvent, enable_stop = EnableStop }); }

        class TS_SetEventOnTorqueRefWork : BooleanChannelWork
        {
            public int torque;
            public Boolean over;
            public Boolean wait;
            public Boolean enable_stop;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetEventOnTorqueRef(torque, over, wait, enable_stop); }
        }
        public Boolean TS_SetEventOnTorqueRef(int Torque, Boolean Over, Boolean WaitEvent, Boolean EnableStop) { return WaitForBooleanWorkCompletion(new TS_SetEventOnTorqueRefWork{ torque = Torque, over = Over, wait = WaitEvent, enable_stop = EnableStop }); }

        class TS_SetEventOnEncoderIndexWork : BooleanChannelWork
        {
            public Int16 index_type;
            public Int16 transition_type;
            public Boolean wait;
            public Boolean enable_stop;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetEventOnEncoderIndex(index_type, transition_type, wait, enable_stop); }
        }
        public Boolean TS_SetEventOnEncoderIndex(Int16 IndexType, Int16 TransitionType, Boolean WaitEvent, Boolean EnableStop) { return WaitForBooleanWorkCompletion(new TS_SetEventOnEncoderIndexWork{ index_type = IndexType, transition_type = TransitionType, wait = WaitEvent, enable_stop = EnableStop }); }

        class TS_SetEventOnLimitSwitchWork : BooleanChannelWork
        {
            public Int16 LSWType;
            public Int16 TransitionType;
            public Boolean WaitEvent;
            public Boolean EnableStop;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetEventOnLimitSwitch(LSWType, TransitionType, WaitEvent, EnableStop); }
        }
        public Boolean TS_SetEventOnLimitSwitch(Int16 LSWType, Int16 TransitionType, Boolean WaitEvent, Boolean EnableStop) { return WaitForBooleanWorkCompletion(new TS_SetEventOnLimitSwitchWork{ LSWType = LSWType, TransitionType = TransitionType, WaitEvent = WaitEvent, EnableStop = EnableStop }); }

        class TS_SetEventOnDigitalInputWork : BooleanChannelWork
        {
            public Byte InputPort;
            public Int16 IOState;
            public Boolean WaitEvent;
            public Boolean EnableStop;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetEventOnDigitalInput(InputPort, IOState, WaitEvent, EnableStop); }
        }
        public Boolean TS_SetEventOnDigitalInput(Byte InputPort, Int16 IOState, Boolean WaitEvent, Boolean EnableStop) { return WaitForBooleanWorkCompletion(new TS_SetEventOnDigitalInputWork{ InputPort = InputPort, IOState = IOState, WaitEvent = WaitEvent, EnableStop = EnableStop }); }


        class TS_SetEventOnHomeInputWork : BooleanChannelWork
        {
            public Int16 IOState; 
            public Boolean WaitEvent;
            public Boolean EnableStop;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetEventOnHomeInput(IOState, WaitEvent, EnableStop); }
        }
        public Boolean TS_SetEventOnHomeInput(Int16 IOState, Boolean WaitEvent, Boolean EnableStop) { return WaitForBooleanWorkCompletion(new TS_SetEventOnHomeInputWork{ IOState = IOState, WaitEvent = WaitEvent, EnableStop = EnableStop }); }

        class TS_SetupInputWork : BooleanChannelWork
        {
            public Byte nIO;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetupInput(nIO); }
        }
        public Boolean TS_SetupInput(Byte nIO) { return WaitForBooleanWorkCompletion(new TS_SetupInputWork{ nIO = nIO }); }

        class TS_GetInputWork : BooleanChannelWork
        {
            public Byte nIO;
            public Byte InValue;
            public override void DoWork() { result = TMLLibPInvokes.TS_GetInput(nIO, out InValue); }
        }
        public Boolean TS_GetInput(Byte nIO, out Byte InValue) { var work = new TS_GetInputWork{ nIO = nIO }; var result = WaitForBooleanWorkCompletion(work); InValue = work.InValue; return result; }

        class TS_GetHomeInputWork : BooleanChannelWork
        {
            public Byte InValue;
            public override void DoWork() { result = TMLLibPInvokes.TS_GetHomeInput(out InValue); }
        }
        public Boolean TS_GetHomeInput(out Byte InValue) { var work = new TS_GetHomeInputWork(); var result = WaitForBooleanWorkCompletion(work); InValue = work.InValue; return result; }

        class TS_SetupOutputWork : BooleanChannelWork
        {
            public Byte nIO;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetupOutput(nIO); }
        }
        public Boolean TS_SetupOutput(Byte nIO) { return WaitForBooleanWorkCompletion(new TS_SetupOutputWork{ nIO = nIO }); }

        class TS_SetOutputWork : BooleanChannelWork
        {
            public Byte nIO;
            public Byte OutValue;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetOutput(nIO, OutValue); }
        }
        public Boolean TS_SetOutput(Byte nIO, Byte OutValue) { return WaitForBooleanWorkCompletion(new TS_SetOutputWork{ nIO = nIO, OutValue = OutValue }); }

        class TS_GetMultipleInputsWork : BooleanChannelWork
        {
            public String pszVarName;
            public Int16 Status;
            public override void DoWork() { result = TMLLibPInvokes.TS_GetMultipleInputs(pszVarName, out Status); }
        }
        public Boolean TS_GetMultipleInputs(String pszVarName, out Int16 Status) { var work = new TS_GetMultipleInputsWork{ pszVarName = pszVarName }; var result = WaitForBooleanWorkCompletion(work); Status = work.Status; return result; }

        class TS_SetMultipleOutputsWork : BooleanChannelWork
        {
            public String pszVarName;
            public Int16 Status;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetMultipleOutputs(pszVarName, Status); }
        }
        public Boolean TS_SetMultipleOutputs(String pszVarName, Int16 Status) { return WaitForBooleanWorkCompletion(new TS_SetMultipleOutputsWork{ pszVarName = pszVarName, Status = Status }); }

        class TS_SetMultipleOutputs2Work : BooleanChannelWork
        {
            public Int16 SelectedPorts;
            public Int16 Status;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetMultipleOutputs2(SelectedPorts, Status); }
        }
        public Boolean TS_SetMultipleOutputs2(Int16 SelectedPorts, Int16 Status) { return WaitForBooleanWorkCompletion(new TS_SetMultipleOutputs2Work{ SelectedPorts = SelectedPorts, Status = Status }); }

        class TS_SendDataToHostWork : BooleanChannelWork
        {
            public Byte HostAddress;
            public UInt32 StatusRegMask;
            public UInt16 ErrorRegMask;
            public override void DoWork() { result = TMLLibPInvokes.TS_SendDataToHost(HostAddress, StatusRegMask, ErrorRegMask); }
        }
        public Boolean TS_SendDataToHost(Byte HostAddress, UInt32 StatusRegMask, UInt16 ErrorRegMask) { return WaitForBooleanWorkCompletion(new TS_SendDataToHostWork{ HostAddress = HostAddress, StatusRegMask = StatusRegMask, ErrorRegMask = ErrorRegMask }); }

        class TS_OnlineChecksumWork: BooleanChannelWork
        {
            public UInt16 startAddress;
            public UInt16 endAddress;
            public UInt16 checksum;
            public override void DoWork() { result = TMLLibPInvokes.TS_OnlineChecksum(startAddress, endAddress, out checksum); }
        }
        public Boolean TS_OnlineChecksum(UInt16 startAddress, UInt16 endAddress, out UInt16 checksum) { var work = new TS_OnlineChecksumWork{ startAddress = startAddress, endAddress = endAddress }; var result = WaitForBooleanWorkCompletion(work); checksum = work.checksum; return result; }

        class TS_DownloadProgramWork : BooleanChannelWork
        {
            public String pszOutFile;
            public UInt16 wEntryPoint;
            public override void DoWork() { result = TMLLibPInvokes.TS_DownloadProgram(pszOutFile, out wEntryPoint); }
        }
        public Boolean TS_DownloadProgram(String pszOutFile, out UInt16 wEntryPoint) { var work = new TS_DownloadProgramWork{ pszOutFile = pszOutFile }; var result = WaitForBooleanWorkCompletion(work); wEntryPoint = work.wEntryPoint; return result; }

        class TS_DownloadSwFileWork : BooleanChannelWork
        {
            public string pszSwFile;
            public override void DoWork() { result = TMLLibPInvokes.TS_DownloadSwFile(pszSwFile); }
        }
        public Boolean TS_DownloadSwFile(String pszSwFile) { return WaitForBooleanWorkCompletion(new TS_DownloadSwFileWork{ pszSwFile = pszSwFile }); }

        class TS_GOTOWork : BooleanChannelWork
        {
            public UInt16 address;
            public override void DoWork() { result = TMLLibPInvokes.TS_GOTO(address); }
        }
        public Boolean TS_GOTO(UInt16 address) { return WaitForBooleanWorkCompletion(new TS_GOTOWork{ address = address }); }

        class TS_GOTO_LabelWork : BooleanChannelWork
        {
            public String pszLabel;
            public override void DoWork() { result = TMLLibPInvokes.TS_GOTO_Label(pszLabel); }
        }
        public Boolean TS_GOTO_Label(String pszLabel) { return WaitForBooleanWorkCompletion(new TS_GOTO_LabelWork{ pszLabel = pszLabel }); }

        class TS_CALLWork : BooleanChannelWork
        {
            public UInt16 address;
            public override void DoWork() { result = TMLLibPInvokes.TS_CALL(address); }
        }
        public Boolean TS_CALL(UInt16 address) { return WaitForBooleanWorkCompletion(new TS_CALLWork{ address = address }); }

        class TS_CALL_LabelWork : BooleanChannelWork
        {
            public String pszFunctionName;
            public override void DoWork() { result = TMLLibPInvokes.TS_CALL_Label(pszFunctionName); }
        }
        public Boolean TS_CALL_Label(String pszFunctionName) { return WaitForBooleanWorkCompletion(new TS_CALL_LabelWork{ pszFunctionName = pszFunctionName }); }

        class TS_CancelableCALLWork : BooleanChannelWork
        {
            public UInt16 address;
            public override void DoWork() { result = TMLLibPInvokes.TS_CancelableCALL(address); }
        }
        public Boolean TS_CancelableCALL(UInt16 address) { return WaitForBooleanWorkCompletion(new TS_CancelableCALLWork{ address = address }); }

        class TS_CancelableCALL_LabelWork : BooleanChannelWork
        {
            public String pszFunctionName;
            public override void DoWork() { result = TMLLibPInvokes.TS_CancelableCALL_Label(pszFunctionName); }
        }
        public Boolean TS_CancelableCALL_Label(String pszFunctionName) { return WaitForBooleanWorkCompletion(new TS_CancelableCALL_LabelWork{ pszFunctionName = pszFunctionName }); }

        class TS_ABORTWork : BooleanChannelWork
        {
            public override void DoWork() { result = TMLLibPInvokes.TS_ABORT(); }
        }
        public Boolean TS_ABORT() { return WaitForBooleanWorkCompletion(new TS_ABORTWork()); }

        class TS_ExecuteWork : BooleanChannelWork
        {
            public String pszCommands;
            public override void DoWork() { result = TMLLibPInvokes.TS_Execute(pszCommands); }
        }
        public Boolean TS_Execute(String pszCommands) { return WaitForBooleanWorkCompletion(new TS_ExecuteWork{ pszCommands = pszCommands }); }

        class TS_ExecuteScriptWork : BooleanChannelWork
        {
            public String pszFileName;
            public override void DoWork() { result = TMLLibPInvokes.TS_ExecuteScript(pszFileName); }
        }
        public Boolean TS_ExecuteScript(String pszFileName) { return WaitForBooleanWorkCompletion(new TS_ExecuteScriptWork{ pszFileName = pszFileName }); }

        class TS_GetOutputOfExecuteWork : BooleanChannelWork
        {
            public StringBuilder pszOutput;
            public int nMaxChars;
            public override void DoWork() { result = TMLLibPInvokes.TS_GetOutputOfExecute(pszOutput, nMaxChars); }
        }
        public Boolean TS_GetOutputOfExecute(StringBuilder pszOutput, int nMaxChars) { return WaitForBooleanWorkCompletion(new TS_GetOutputOfExecuteWork{ pszOutput = pszOutput, nMaxChars = nMaxChars }); }

        class TS_SetupLoggerWork : BooleanChannelWork
        {
            public UInt16 wLogBufferAddr;
            public UInt16 wLogBufferLen;
            public UInt16[] arrayAddresses;
            public UInt16 countAddr;
            public UInt16 period;
            public override void DoWork() { result = TMLLibPInvokes.TS_SetupLogger(wLogBufferAddr, wLogBufferLen, arrayAddresses, countAddr, period); }
        }
        public Boolean TS_SetupLogger(UInt16 wLogBufferAddr, UInt16 wLogBufferLen, UInt16[] arrayAddresses, UInt16 countAddr, UInt16 period) { return WaitForBooleanWorkCompletion(new TS_SetupLoggerWork{ wLogBufferAddr = wLogBufferAddr, wLogBufferLen = wLogBufferLen, arrayAddresses = arrayAddresses, countAddr = countAddr, period = period }); }

        class TS_StartLoggerWork : BooleanChannelWork
        {
            public UInt16 wLogBufferAddr;
            public Byte LogType;
            public override void DoWork() { result = TMLLibPInvokes.TS_StartLogger(wLogBufferAddr, LogType); }
        }
        public Boolean TS_StartLogger(UInt16 wLogBufferAddr, Byte LogType) { return WaitForBooleanWorkCompletion(new TS_StartLoggerWork{ wLogBufferAddr = wLogBufferAddr, LogType = LogType }); }

        class TS_CheckLoggerStatusWork : BooleanChannelWork
        {
            public UInt16 wLogBufferAddr;
            public UInt16 status;
            public override void DoWork() { result = TMLLibPInvokes.TS_CheckLoggerStatus(wLogBufferAddr, out status); }
        }
        public Boolean TS_CheckLoggerStatus(UInt16 wLogBufferAddr, out UInt16 status) { var work = new TS_CheckLoggerStatusWork{ wLogBufferAddr = wLogBufferAddr }; var result = WaitForBooleanWorkCompletion(work); status = work.status; return result; }

        class TS_UploadLoggerResultsWork : BooleanChannelWork
        {
            public UInt16 wLogBufferAddr;
            public UInt16[] arrayValues;
            public UInt16 countValues;
            public override void DoWork() { result = TMLLibPInvokes.TS_UploadLoggerResults(wLogBufferAddr, arrayValues, ref countValues); }
        }
        public Boolean TS_UploadLoggerResults(UInt16 wLogBufferAddr, UInt16[] arrayValues, ref UInt16 countValues) { var work = new TS_UploadLoggerResultsWork{ wLogBufferAddr = wLogBufferAddr, arrayValues = arrayValues, countValues = countValues }; var result = WaitForBooleanWorkCompletion(work); countValues = work.countValues; return result; }

        class TS_RegisterHandlerForUnrequestedDriveMessagesWork : ChannelWork
        {
            public TMLLibConst.pfnCallbackRecvDriveMsg handler;
            public override void DoWork() { TMLLibPInvokes.TS_RegisterHandlerForUnrequestedDriveMessages(handler); }
        }
        public void TS_RegisterHandlerForUnrequestedDriveMessages(TMLLibConst.pfnCallbackRecvDriveMsg handler) { WaitForWorkCompletion(new TS_RegisterHandlerForUnrequestedDriveMessagesWork{ handler = handler }); }

        class TS_CheckForUnrequestedDriveMessagesWork : BooleanChannelWork
        {
            public override void DoWork() { result = TMLLibPInvokes.TS_CheckForUnrequestedDriveMessages(); }
        }
        public Boolean TS_CheckForUnrequestedDriveMessages() { return WaitForBooleanWorkCompletion(new TS_CheckForUnrequestedDriveMessagesWork()); }

        class TS_DriveInitialisationWork : BooleanChannelWork
        {
            public override void DoWork() { result = TMLLibPInvokes.TS_DriveInitialisation(); }
        }
        public Boolean TS_DriveInitialisation() { return WaitForBooleanWorkCompletion(new TS_DriveInitialisationWork()); }

        // ------------------------------------------------------------------------- //
        // -                   Implementation Details                              - //
        // ------------------------------------------------------------------------- //
        abstract class ChannelWork
        {
            public readonly ManualResetEvent work_complete;
            protected ChannelWork() { work_complete = new ManualResetEvent(false); }
            public abstract void DoWork();
        }
        abstract class BooleanChannelWork : ChannelWork
        {
            public Boolean result;
        }

        DateTime _last_queue_time;
        Thread _thread;
        Queue<ChannelWork> _work_queue;
        Object _work_queue_lock;
        ManualResetEvent _exit_event;
        ManualResetEvent _shutting_down_event;
        ManualResetEvent _work_ready_event;

        void WaitForWorkCompletion(ChannelWork work, bool shutting_down=false)
        {
            if( shutting_down)
                _shutting_down_event.Set();
            else if( _shutting_down_event.WaitOne(0))
                return;

            if( _thread == null)
                return;

            lock(_work_queue_lock)
            {
                _work_queue.Enqueue(work);
                _work_ready_event.Set();
            }
            work.work_complete.WaitOne();
        }

        Boolean WaitForBooleanWorkCompletion(BooleanChannelWork work)
        {
            WaitForWorkCompletion(work);
            return work.result;
        }

        void TMLChannelMarshallingThread()
        {
            _last_queue_time = DateTime.Now;
            while (!_exit_event.WaitOne(0))
            {
                var now = DateTime.Now;
                bool empty;
                lock (_work_queue_lock)
                {
                    empty = _work_queue.Count == 0;
                }
                if (empty)
                {
                    _work_ready_event.WaitOne();
                    continue;
                }
                ChannelWork next_work;
                lock (_work_queue_lock)
                {
                    _work_ready_event.Reset();
                    next_work = _work_queue.Dequeue();
                }
                try
                {
                    next_work.DoWork();
                }
                finally
                {
                    _last_queue_time = DateTime.Now;
                    next_work.work_complete.Set();
                }
            }
        }
    }
#endif
}
