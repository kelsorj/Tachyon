using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BioNex.Shared.TechnosoftLibrary
{
    // TMLComm low-level comm library is currently unused, but preserved here for reference. 

    // private for now so that it's not used by mistake

    class TMLComm
    {
        public const Int32 LOG_NONE = 0;
        public const Int32 LOG_ERROR = 1;
        public const Int32 LOG_WARNING = 2;
        public const Int32 LOG_TRAFFIC = 3;

        //supported CAN protocols
        public const Byte PROTOCOL_TMLCAN = 0x00;    //use TMLCAN protocol (default, 29-bit identifiers)
        public const Byte PROTOCOL_TECHNOCAN = 0x80; //use TechnoCAN protocol (11-bit identifiers)
        public const Byte PROTOCOL_MASK = 0x80;      //this bits are used for specifying CAN protocol through nChannelType param of MSK_OpenComm function

        // **** supported CAN devices *****************************
        // CHANNEL_IXXAT_CAN - see http://www.ixxat.com
        // CHANNEL_SYS_TEC_USBCAN - see www.systec-electronic.com
        // CHANNEL_ESD_CAN - see http://www.esd-electronics.com
        // CHANNEL_PEAK_SYS_PCAN_* - see http://www.peak-system.com
        // ********************************************************
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

        //MSK Message structure
        public const UInt16 DSP_PC_FLAG = 0x1;
        public const UInt16 DSP_GROUP_FLAG = 0x1000;
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MSK_MSG {
            public UInt16 m_Len;
            public UInt16 m_Addr;
            public UInt16 m_OpCode;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
                public UInt16 [] m_Data;
        }

        // ***************************************************************************
        // Callback function used by client application for handling debug messages
        // ***************************************************************************
        public delegate void MSK_CallbackDebugLog(String pszMsg);

        // ***************************************************************************
        // Callback function used by client application for handling unsolicited
        // messages which this driver receives in unexpected places
        // ***************************************************************************
        public delegate void MSK_CallbackRecvMsg(ref MSK_MSG pMsg);

        // ***************************************************************************
        // Callback function used by client application to manage complex 
        // MSK functions, like downloading or flash burning/
        // ***************************************************************************
        public delegate bool MSK_Callback(Int32 nMSGType, String pcszMsg,
                                        Single nElpsTime, String pUsrData);
        public const Int32 MSK_CALLBACK_APPEND_MSG = 1;
        public const Int32 MSK_CALLBACK_YESNO_MSG = 2;
        public const Int32 MSG_CALLBACK_REPLACE_LASTLINE = 3;
        public const Int32 MSG_CALLBACK_APPEND_NEWLINE_MSG = 4;
        //	Parameters:
        //  nMSGType:	message type;
        //  pcszMsg:	message to be displayed by client application;
        //              the meaning of this message depends by the "nMSGType" parameter:
        //                  "MSK_CALLBACK_APPEND_MSG" - append the message to the 
        //                      client application output;
        //                  "MSK_CALLBACK_YESNO_MSG" - prompts user for a question;
        //                  "MSG_CALLBACK_REPLACE_LASTLINE" - replace last line of the
        //                      client application output with the provided message;
        //                  "MSG_CALLBACK_APPEND_NEWLINE_MSG" - append the message at
        //                      the beginning of a new line;
        //  nElpsTime:	relative elapsed time (percent value);
        //  pUsrData:	pointer to user data provided by client application;
        //              usually used to identify the output device.
        //  Return value:
        //              TRUE:	continue to execute MSK function;
        //              FALSE:	exit from MSK function with user abort error code; 

        // DSP memory space 
        public const Byte MSK_PM  = 0;	// Program memory space 
        public const Byte MSK_DM  = 1;	// Data memory space 
        public const Byte MSK_SPI = 2;	// SPI memory space 

        // ****************************************************************************
        // All MSK exported function use the same meaning of the return value:
        //  TRUE:	if the function is successful;
        //  FALSE:	error; Use MSK_GetLastError for more details
        // ****************************************************************************
        [DllImport("TMLcomm.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]
        public static extern String  MSK_GetLastError();

        // ****************************************************************************
        // MSK_RegisterDebugLogHandler: set destination of debugging messages and level of debugging
        // ****************************************************************************
        [DllImport("TMLcomm.dll", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.StdCall)]
        public static extern void MSK_RegisterDebugLogHandler(Int32 nLevel, MSK_CallbackDebugLog pfnCallback);
        // ****************************************************************************
        //   Parameters:
        //   pfnCallback - pointer to callback function who receive debug messages
        //   nLevel - level of logging (1 = error, 2 = error and warning, 3 = traffic, error and warning)
        //   if pfnCallback == NULL or nLevel <=0, logging is disabled
        // ****************************************************************************

        // ****************************************************************************
        // MSK_RegisterReceiveUnsolicitedMsgHandler: 
        //    register a callback called when unexpected messages are received instead acknowledge
        // ****************************************************************************
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern void MSK_RegisterReceiveUnsolicitedMsgHandler(MSK_CallbackRecvMsg pfnCallback);
        // 
        //    pfnCallback: pointer to the callback function
        //

        // ****************************************************************************
        //   MSK_OpenComm: function that opens a communication channel
        // ****************************************************************************
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 MSK_OpenComm(String pszComm, UInt16 nHostAddress, bool bExclusive, Byte nChannelType);
        // ****************************************************************************
        //   Parameters:
        //      pszComm:   identifier for the communication port
        //            RS232 and RS485:
        //                    "1","2","3"... -> COM1, COM2, COM3...
        //            CHANNEL_IXXAT_CAN: "1" .. "4"
        //            CHANNEL_SYS_TEC_USBCAN and CHANNEL_ESD_CAN: "0" .. "10"
        //            CHANNEL_PEAK_SYS_PCAN_PCI: "1" or "2"
        //            CHANNEL_XPORT_IP: "IP" or "hostname"
        //      nHostAddress: host board address
        //      nChannelType: Type of communcation (CHANNEL_* defines) with an optional protocol mask (PROTOCOL_* defines)
        //        bExclusive:	TRUE value for exclusive using.
        //   Return:
        //      -1 means error
        //      otherwise is the port file descriptor. 
        // ****************************************************************************

        // ****************************************************************************
        //   MSK_SelectComm: function that select a communication channel.
        //      Note: MSK_OpenComm automatically selects the communication port. If you work only
        //         with one communication port there is no need to call this function
        // ****************************************************************************
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 MSK_SelectComm(Int32 fdComm);
        // ****************************************************************************
        //   Parameters:
        //      fdComm: the communication port file descriptor.
        //   Return:
        //      if error it returns -1
        //        else returns previous selected fdComm
        // ****************************************************************************

        //****************************************************************************
        //   MSK_CloseComm: function that closes the communication channel 
        //****************************************************************************
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool MSK_CloseComm(Int32 fdComm);
        // ****************************************************************************
        //   Parameters:
        //      fdComm: the communication port file descriptor. -1 means current selected.
        // ****************************************************************************
 
        // ***************************************************************************
        // MSK_UpdateSettings: function that reloads settings from  Windows registry  path HKEY_CURRENT_USER\Software\TechnoSoft\TMLCOMM
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool MSK_UpdateSettings();

        // ****************************************************************************
        // MSK_SetBaudRate: set device baud rate.
        // ****************************************************************************
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool MSK_SetBaudRate(UInt32 nBaud);
        // ****************************************************************************
        //    Parameters:
        //    nBaud:		new baud rate (serial lines have baud rates of 9600, 19200, 38400, 57600 or 115200)
        // ****************************************************************************

        // ****************************************************************************
        //   MSK_GetBaudRate: get device baud rate.
        // ****************************************************************************
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern UInt32 MSK_GetBaudRate();
        // ****************************************************************************
        //   Return: 0 on error or baudrate on success
        // ****************************************************************************

        // ****************************************************************************
        //   MSK_GetBytesCountInQueue: Test if any character is available on the communication buffer device 
        // ****************************************************************************
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern Int32 MSK_GetBytesCountInQueue();
        // ****************************************************************************
        //    Returns:
        //    if < 0		erorr
        //    else number of bytes in serial queue
        // ****************************************************************************

        // ****************************************************************************
        //   MSK_SetActiveDrive: set active drive (messages are sent/received to/from this axis)
        // ****************************************************************************
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool MSK_SetActiveDrive(UInt16 AxisID);
        // ****************************************************************************
        //   Parameters:
        //      AxisID: the address of the drive.
        // ****************************************************************************

        // ****************************************************************************
        //   MSK_GetActiveDrive: retrieve the current active drive
        // ****************************************************************************
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool MSK_GetActiveDrive(out UInt16 pAxisID);
        // ****************************************************************************
        //   Parameters:
        //      pAxisID: pointer to a buffer which will contain the address of the drive.
        // ****************************************************************************

        // ****************************************************************************
        //   MSK_SetHostDrive: Set the address of the host drive
        // ****************************************************************************
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool MSK_SetHostDrive(UInt16 AxisID);
        // ****************************************************************************
        //   Parameters:
        //      AxisID: the address of the drive.
        // ****************************************************************************

        // ****************************************************************************
        //   MSK_GetHostDrive: Get the address of the host drive
        // ****************************************************************************
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool MSK_GetHostDrive(out UInt16 pAxisID);
        // ****************************************************************************
        //   Parameters:
        //      pAxisID: pointer to a buffer which will contain the address of the drive.
        // ****************************************************************************

        // ****************************************************************************
        //   MSK_GetDriveVersion: retrieve firmware version of the active drive
        // ****************************************************************************
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool MSK_GetDriveVersion(StringBuilder szDriveVersion);
        // ****************************************************************************
        //   Parameters:
        //      szDriveVersion: pointer to a buffer of 5 characters (not including null terminator)
        //         which will contain the firmware version.
        // ****************************************************************************

        // ****************************************************************************
        //   MSK_ResetDrive: reset the active drive.
        // ****************************************************************************
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool MSK_ResetDrive();
    
        // ****************************************************************************
        //   MSK_SendMessage: send a message. 
        //        Note: The active drive has no effect on this function.
        // ****************************************************************************
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool MSK_SendMessage(ref MSK_MSG pMsg);
        // ****************************************************************************
        //   Parameters:
        //      pMsg: pointer to a message structure which will be sent
        //   CAUTION: pMsg->wAddr must be equal with (axisID << 4) or ((1 << (4+groupID-1)) | DSP_GROUP_FLAG)
        // ****************************************************************************

        // ****************************************************************************
        //   MSK_ReceiveMessage: receive a message. The active drive has no effect on this function.
        //        Note: The active drive has no effect on this function.
        // ****************************************************************************
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool MSK_ReceiveMessage(out MSK_MSG pMsg);
        // ****************************************************************************
        //   Parameters:
        //      pMsg: pointer to a message structure which will contain the received message
        // ****************************************************************************

        // ****************************************************************************
        //    MSK_CheckSum: Ask for checksum of active drive's memory.
        // ****************************************************************************
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool MSK_CheckSum(UInt16 nStartAddr, UInt16 nEndAddr, out UInt16 pCheckSum);
        // ****************************************************************************
        //   Parameters:
        //        nStartAddr:	start memory address
        //        nEndAddr: end memory address
        //        pCheckSum: received CheckSum from the drive
        // ****************************************************************************

        // ****************************************************************************
        //   MSK_SendData: This function transfers a block of data into active drive's memory 
        // ****************************************************************************
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool MSK_SendData(UInt16[] pData, UInt16 nAddr, Byte memType, UInt16 nLength);
        // ****************************************************************************
        //   Parameters:
        //        pData:      pointer to data buffer;
        //        nAddr:      memory address where the data will be stored
        //        memType:    memory space where the data will be stored (MSK_DM, MSK_PM OR MSK_SPI)
        //        nLength:    number of words to be stored
        // ****************************************************************************

        // ****************************************************************************
        //   MSK_ReceiveData: This function read a block of data from active drive's memory 
        // ****************************************************************************
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool MSK_ReceiveData(UInt16[] pData, UInt16 nAddr, Byte memType, UInt16 nLength);
        // *****************************************************************************
        //   Parameters:
        //      pData:      pointer to data buffer;
        //      nAddr:      memory address from where the data will be readed
        //      memType:      memory space from where the data will be readed (MSK_DM, MSK_PM OR MSK_SPI)
        //      nLength:      number of words to be readed
        // ****************************************************************************

        // ****************************************************************************
        //   MSK_SendBigData: This function transfers a block of data into active drive's memory, 
        //        Note: the only difference from MSK_SendData is support for completion measurement
        // ****************************************************************************
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool MSK_SendBigData(UInt16[] pData, UInt16 nAddr, Byte memType, UInt16 nLength, MSK_Callback pf, String pUsrData);
        // ****************************************************************************
        //   Parameters:
        //      pData:      pointer to data buffer;
        //      nAddr:      memory address where the data will be stored
        //      memType:    memory space where the data will be stored (MSK_DM, MSK_PM OR MSK_SPI)
        //      nLength:    number of words to be stored
        //      pf:         pointer to a callback function used to display progress status of this operation
        //      pUsrData:   pointer to user data passed to callback function
        // ****************************************************************************

        // ****************************************************************************
        //   MSK_ReceiveBigData: This function read a block of data from active drive's memory. 
        //        Note: the only difference from MSK_ReceiveData is support for completion measurement
        // ****************************************************************************
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool MSK_ReceiveBigData(UInt16[] pData, UInt16 nAddr, Byte memType, UInt16 nLength, MSK_Callback pf, String pUsrData);
        // ****************************************************************************
        //   Parameters:
        //      pData:      pointer to data buffer;
        //      nAddr:      memory address from where the data will be readed
        //      memType:    memory space from where the data will be readed (MSK_DM, MSK_PM OR MSK_SPI)
        //      nLength:    number of words to be read
        //      pf:         pointer to a callback function used to display progress status of this operation
        //      pUsrData:   pointer to user data passed to callback function
        // ****************************************************************************

        // *****************************************************************************
        //   MSK_COFFDownload: download a COFF formatted file into drive's memory (PM or SPI)
        // *****************************************************************************
        [DllImport("TMLcomm.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern bool MSK_COFFDownload(String file_name, out UInt16 pEntry, out UInt16 pBeginAddr, out UInt16 pEndAddr,
        MSK_Callback pf, String pUsrData, UInt16 wSPISize);
        // ****************************************************************************
        //   Parameters:
        //      file_name:            name of the COFF file
        //      pEntry(out param):   will contain the entry point of the downloaded OUT (can be NULL)
        //      pBeginAddr(out param):   will contain the smallest location address written by download (can be NULL)
        //      pEndAddr(out param):   will contain the biggest location address written by download (can be NULL)
        //      pf:                  pointer to a callback function used to control the download process (can be NULL)
        //      pUsrData:            user data passed to callback function (can be NULL)
        //      wSPISize:            size (in WORDS) of the SPI memory. Used only for testing if a section is out of memory.
        //                              The range is between 0 and 0x4000.
        // ****************************************************************************
    }
}
