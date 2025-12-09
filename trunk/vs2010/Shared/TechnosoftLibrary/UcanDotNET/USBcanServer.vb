'(c) SYSTEC electronic GmbH, D-07973 Greiz, August-Bebel-Str. 29
'    www.systec-electronic.de

'Project:      .NET and COM Interface for USB-CANmodul GW-00x

'Description:  Interface Wrapper Class

'-------------------------------------------------------------------------
'              $RCSfile:$
'              $Author:$
'              $Revision:$  $Date:$
'              $State:$
'              Build Environment:
'                  Microsoft Visual Studio .NET (Visual Basic .NET)
'-------------------------------------------------------------------------

'Revision History:

'  2005/23/11  d.k.:   new (based on Usbcan32.h and UcanIntfEx.cpp from DemoGW006.api)
'  2006/06/06  r.d.: - support of USB-CANmodul1 "Basic" 3204000 and USB-CANmodul2 "Advanced" 3204002
'                    - New member variable m_dwProductCode in struct tUcanHardwareInfoEx to find out the hardware type.
'                    - New member variables m_wNrOfRxBufferEntries and m_wNrOfTxBufferEntries in struct tUcanInitCanParam to change number of buffer entries.

'Schritte zum Zugriff über COM (nicht notwendig bei Zugriff über .NET)
'---------------------------------------------------------------------
'1. COM Klasse registrieren: regasm UcanDotNET.dll /tlb
'2. [Regfile für Setup für anderen Rechner erzeugen: regasm UcanDotNET.dll /regfile]
'3. starken Name erzeugen: sn -k UcanDotNET.snk  [Nur ein einziges Mal nötig, danach noch einmal Code übersetzen]
'4. DLL in Global Assembly Cache (GAC) für systemweiten Zugriff importieren: gacutil /i UcanDotNET.dll
'
' statische Funktionen wie tCanMsgStruct.CreateInstance(), die statischen Get...Message() Funktionen
' und die statischen Events werden nicht in der COM-Klasse abgebildet
'
' ACHTUNG: Bei Änderungen an dem Projekt UcanDotNET muss die AssemblyVersion in AssemblyInfo.vb hochgezählt werden.
'          Ansonsten gibt es Konflikte mit der im GAC registrierten DLL mit selber Versionsnummber.
'          Sinnvoll ist auch die alte UcanDotNET.dll aus dem GAC zu deregistrieren.


Imports System.Runtime.InteropServices
Imports System.Threading


<ClassInterface(ClassInterfaceType.AutoDual), GuidAttribute("E6FEA634-41B5-4004-884C-394CF85F2403")> _
Public Class USBcanServer
    Implements IDisposable

#Region " Variables ####################################################################### "

    ' -------------------------------------------------------------------------------------
    ' Static Variables
    ' -------------------------------------------------------------------------------------
    ' static collection of Ucan handles
    Protected Shared colUcanHandle_l As Collection = New Collection
    ' count running object instances
    Protected Shared dwPlugAndPlayCount_l As Integer = 0
    ' delegate of ConnectControl callback function
    Protected Shared pfnUcanConnectControl_l As New tConnectControlFktEx(AddressOf UcanConnectControl)
    ' protect delegate from being destroyed by the garbage collector
    Protected Shared gchUcanConnectControl_l As GCHandle

    ' -------------------------------------------------------------------------------------
    ' Member variables
    ' -------------------------------------------------------------------------------------
    ' is object instance already disposed?
    Private disposed As Boolean = False
    ' is USB-CANmodul completely initialized?
    Protected m_fIsInitialized As Boolean = False
    ' is hardware initialized?
    Protected m_fHwIsInitialized As Boolean = False
    ' is CAN channel 0 initialized?
    Protected m_fCh0IsInitialized As Boolean = False
    ' is CAN channel 1 initialized?
    Protected m_fCh1IsInitialized As Boolean = False
    ' handle from USBCAN32.DLL
    Protected m_UcanHandle As Byte = 0
    ' delegate of callback function
    Protected m_pfnUcanCallback As New tCallbackFktEx(AddressOf HandleUcanCallback)
    ' protect delegate from being destroyed by garbage collector before called with event DEINITHW
    Protected m_gchUcanCallback As GCHandle

#End Region

#Region " Constants of USBCAN32.DLL ####################################################### "

    ' -------------------------------------------------------------------------------------
    ' Constants copied from Usbcan32.h
    ' -------------------------------------------------------------------------------------
    ' maximum number of modules, that are supported (can not be changed!)
    Public Const USBCAN_MAX_MODULES = 64

    ' maximum number of applications, that can make use of this DLL (can not be changed!)
    Public Const USBCAN_MAX_INSTANCES = 64

    ' With the function UcanInitHardware() the module is used, which is detected first.
    ' This define should only be used, in case only one module is connected to the computer.
    Public Const USBCAN_ANY_MODULE As Byte = 255

    ' no valid USB-CAN Handle
    Public Const USBCAN_INVALID_HANDLE = &HFF

    ' pre-defined baudrate values for GW-001 and GW-002
    ' (use function UcanInitCan or UcanSetBaudrate)
    Public Const USBCAN_BAUD_1MBit As Short = &H14                         ' = 1000 kBit/s
    Public Const USBCAN_BAUD_800kBit As Short = &H16                       ' =  800 kBit/s
    Public Const USBCAN_BAUD_500kBit As Short = &H1C                       ' =  500 kBit/s
    Public Const USBCAN_BAUD_250kBit As Short = &H11C                      ' =  250 kBit/s
    Public Const USBCAN_BAUD_125kBit As Short = &H31C                      ' =  125 kBit/s
    Public Const USBCAN_BAUD_100kBit As Short = &H432F                     ' =  100 kBit/s
    Public Const USBCAN_BAUD_50kBit As Short = &H472F                      ' =   50 kBit/s
    Public Const USBCAN_BAUD_20kBit As Short = &H532F                      ' =   20 kBit/s
    Public Const USBCAN_BAUD_10kBit As Short = &H672F                      ' =   10 kBit/s
    ' special values
    Public Const USBCAN_BAUD_USE_BTREX As Short = &H0                      ' uses predefined extended values of baudrate for
    '                                                             ' Multiport 3004006, USB-CANmodul1 3204000 or USB-CANmodul2 3204002 (do not use for GW-001/002)
    Public Const USBCAN_BAUD_AUTO As Integer = &HFFFF                        ' automatic baudrate detection (not implemented in this version)

    ' pre-defined baudrate values for Multiport 3004006, USB-CANmodul1 3204000, USB-CANmodul2 3204002
    ' (use function UcanInitCanEx or UcanSetBaudrateEx)
    Public Const USBCAN_BAUDEX_1MBit As Integer = &H20354                    ' = 1000 kBit/s    Sample Point: 68,75%
    Public Const USBCAN_BAUDEX_800kBit As Integer = &H30254                  ' =  800 kBit/s    Sample Point: 66,67%
    Public Const USBCAN_BAUDEX_500kBit As Integer = &H50354                  ' =  500 kBit/s    Sample Point: 68,75%
    Public Const USBCAN_BAUDEX_250kBit As Integer = &HB0354                  ' =  250 kBit/s    Sample Point: 68,75%
    Public Const USBCAN_BAUDEX_125kBit As Integer = &H170354                 ' =  125 kBit/s    Sample Point: 68,75%
    Public Const USBCAN_BAUDEX_100kBit As Integer = &H170466                 ' =  100 kBit/s    Sample Point: 65,00%
    Public Const USBCAN_BAUDEX_50kBit As Integer = &H2F0466                  ' =   50 kBit/s    Sample Point: 65,00%
    Public Const USBCAN_BAUDEX_20kBit As Integer = &H770466                  ' =   20 kBit/s    Sample Point: 65,00%
    Public Const USBCAN_BAUDEX_10kBit As Integer = &H80770466                ' =   10 kBit/s    Sample Point: 65,00% (CLK = 1, see L-487 since version 15)
    ' pre-defined baudrate values with corrected sample points
    Public Const USBCAN_BAUDEX_SP2_1MBit As Integer = &H20741                ' = 1000 kBit/s    Sample Point: 87,50%
    Public Const USBCAN_BAUDEX_SP2_800kBit As Integer = &H30731              ' =  800 kBit/s    Sample Point: 86,67%
    Public Const USBCAN_BAUDEX_SP2_500kBit As Integer = &H50741              ' =  500 kBit/s    Sample Point: 87,50%
    Public Const USBCAN_BAUDEX_SP2_250kBit As Integer = &HB0741              ' =  250 kBit/s    Sample Point: 87,50%
    Public Const USBCAN_BAUDEX_SP2_125kBit As Integer = &H170741             ' =  125 kBit/s    Sample Point: 87,50%
    Public Const USBCAN_BAUDEX_SP2_100kBit As Integer = &H1D1741             ' =  100 kBit/s    Sample Point: 87,50%
    Public Const USBCAN_BAUDEX_SP2_50kBit As Integer = &H3B1741              ' =   50 kBit/s    Sample Point: 87,50%
    Public Const USBCAN_BAUDEX_SP2_20kBit As Integer = &H771772              ' =   20 kBit/s    Sample Point: 85,00%
    Public Const USBCAN_BAUDEX_SP2_10kBit As Integer = &H80771772            ' =   10 kBit/s    Sample Point: 85,00% (CLK = 1, see L-487 since version 15)
    ' special values
    Public Const USBCAN_BAUDEX_USE_BTR01 As Integer = &H0                    ' uses predefined values of BTR0/BTR1 for GW-001/002
    Public Const USBCAN_BAUDEX_AUTO As Integer = &HFFFFFFFF                  ' automatic baudrate detection (not implemented in this version)

    ' Frame format for a CAN message (bit oriented)
    Public Const USBCAN_MSG_FF_STD As Byte = &H0                          ' Standard Frame (11-Bit-ID)
    Public Const USBCAN_MSG_FF_ECHO As Byte = &H20                        ' Tx echo (message received from UcanReadCanMsg.. was previously sent by UcanWriteCanMsg..)
    Public Const USBCAN_MSG_FF_RTR As Byte = &H40                         ' Remote Transmition Request Frame
    Public Const USBCAN_MSG_FF_EXT As Byte = &H80                         ' Extended Frame (29-Bit-ID)

    ' Function return codes (encoding)
    Public Const USBCAN_SUCCESSFUL As Byte = &H0                          ' no error
    Public Const USBCAN_ERR As Byte = &H1                                 ' error in DLL; function has not been executed
    Public Const USBCAN_ERRCMD As Byte = &H40                             ' error in module; function has not been executed
    Public Const USBCAN_WARNING As Byte = &H80                            ' Warning; function has been executed anyway
    Public Const USBCAN_RESERVED As Byte = &HC0                           ' reserved return codes (up to 255)

    ' Error messages, that can occur in the DLL
    Public Const USBCAN_ERR_RESOURCE As Byte = &H1                        ' could not created a resource (memory, Handle, ...)
    Public Const USBCAN_ERR_MAXMODULES As Byte = &H2                      ' the maximum number of open modules is exceeded
    Public Const USBCAN_ERR_HWINUSE As Byte = &H3                         ' a module is already in use
    Public Const USBCAN_ERR_ILLVERSION As Byte = &H4                      ' the software versions of the module and DLL are incompatible
    Public Const USBCAN_ERR_ILLHW As Byte = &H5                           ' the module with the corresponding device number is not connected
    Public Const USBCAN_ERR_ILLHANDLE As Byte = &H6                       ' wrong USB-CAN-Handle handed over to the function
    Public Const USBCAN_ERR_ILLPARAM As Byte = &H7                        ' wrong parameter handed over to the function
    Public Const USBCAN_ERR_BUSY As Byte = &H8                            ' instruction can not be processed at this time
    Public Const USBCAN_ERR_TIMEOUT As Byte = &H9                         ' no answer from the module
    Public Const USBCAN_ERR_IOFAILED As Byte = &HA                        ' a request for the driver failed
    Public Const USBCAN_ERR_DLL_TXFULL As Byte = &HB                      ' the message did not fit into the transmission queue
    Public Const USBCAN_ERR_MAXINSTANCES As Byte = &HC                    ' maximum number of applications is reached
    Public Const USBCAN_ERR_CANNOTINIT As Byte = &HD                      ' CAN-interface is not yet initialized
    Public Const USBCAN_ERR_DISCONECT As Byte = &HE                       ' USB-CANmodul was disconnected
    Public Const USBCAN_ERR_NOHWCLASS As Byte = &HF                       ' the needed device class does not exist
    Public Const USBCAN_ERR_ILLCHANNEL As Byte = &H10                     ' illegal CAN channel for GW-001/GW-002
    Public Const USBCAN_ERR_RESERVED1 As Byte = &H11
    Public Const USBCAN_ERR_ILLHWTYPE As Byte = &H12                      ' the API function can not be used with this hardware

    ' Error messages, that the module returns during the command sequence
    Public Const USBCAN_ERRCMD_NOTEQU As Byte = &H40                      ' the received response does not match with the transmitted command
    Public Const USBCAN_ERRCMD_REGTST As Byte = &H41                      ' no access to the CAN controler possible
    Public Const USBCAN_ERRCMD_ILLCMD As Byte = &H42                      ' the module could not interpret the command
    Public Const USBCAN_ERRCMD_EEPROM As Byte = &H43                      ' error while reading the EEPROM occured
    Public Const USBCAN_ERRCMD_RESERVED1 As Byte = &H44
    Public Const USBCAN_ERRCMD_RESERVED2 As Byte = &H45
    Public Const USBCAN_ERRCMD_RESERVED3 As Byte = &H46
    Public Const USBCAN_ERRCMD_ILLBDR As Byte = &H47                      ' illegal baudrate values for Multiport 3004006, USB-CANmodul1 3204000, USB-CANmodul2 3204002 in BTR0/BTR1
    Public Const USBCAN_ERRCMD_NOTINIT As Byte = &H48                     ' CAN channel was not initialized
    Public Const USBCAN_ERRCMD_ALREADYINIT As Byte = &H49                 ' CAN channel was already initialized
    Public Const USBCAN_ERRCMD_ILLSUBCMD As Byte = &H4A                   ' illegal sub-command specified
    Public Const USBCAN_ERRCMD_ILLIDX As Byte = &H4B                      ' illegal index specified (e.g. index for cyclic CAN message)
    Public Const USBCAN_ERRCMD_RUNNING As Byte = &H4C                     ' cyclic CAN message(s) can not be defined because transmission of cyclic CAN messages is already running

    ' Warning messages, that can occur in the DLL
    ' NOTE: These messages are only warnings. The function has been executed anyway.
    Public Const USBCAN_WARN_NODATA As Integer = &H80                        ' no CAN messages received
    Public Const USBCAN_WARN_SYS_RXOVERRUN As Integer = &H81                 ' overrun in the receive queue of the driver
    Public Const USBCAN_WARN_DLL_RXOVERRUN As Integer = &H82                 ' overrun in the receive queue of the DLL
    Public Const USBCAN_WARN_RESERVED1 As Integer = &H83
    Public Const USBCAN_WARN_RESERVED2 As Integer = &H84
    Public Const USBCAN_WARN_FW_TXOVERRUN As Integer = &H85                  ' overrun in the transmit queue of the firmware (but this CAN message was successfully stored in buffer)
    Public Const USBCAN_WARN_FW_RXOVERRUN As Integer = &H86                  ' overrun in the receive queue of the firmware (but this CAN message was successfully read)
    Public Const USBCAN_WARN_FW_TXMSGLOST As Integer = &H87                  ' (not implemented yet)
    Public Const USBCAN_WARN_NULL_PTR As Integer = &H90                      ' pointer to address is NULL (function will not work correctly)
    Public Const USBCAN_WARN_TXLIMIT As Integer = &H91                       ' function UcanWriteCanMsgEx() was called for sending more CAN messages than one
    '                                                             '      But not all of them could be sent because the buffer is full.
    '                                                             '      Variable pointed by pdwCount_p received the number of succeddfully sent CAN messages.
    Public Const USBCAN_WARN_BUSY As Integer = &H92                          ' place holder (only for internaly use)

    ' system errors
    '   Public Const USBCAN_ERR_ABORT = &HC0
    '   Public Const USBCAN_ERR_DATA = &HC1

    ' The Callback function is called, if certain events did occur.
    ' These Defines specify the event.
    Public Const USBCAN_EVENT_INITHW As Integer = 0                          ' the USB-CANmodule has been initialized
    Public Const USBCAN_EVENT_INITCAN As Integer = 1                         ' the CAN interface has been initialized
    Public Const USBCAN_EVENT_RECIEVE As Integer = 2                         ' a new CAN message has been received
    Public Const USBCAN_EVENT_RECEIVE As Integer = 2                         ' a new CAN message has been received
    Public Const USBCAN_EVENT_STATUS As Integer = 3                          ' the error state in the module has changed
    Public Const USBCAN_EVENT_DEINITCAN As Integer = 4                       ' the CAN interface has been deinitialized (UcanDeinitCan() was called)
    Public Const USBCAN_EVENT_DEINITHW As Integer = 5                        ' the USB-CANmodule has been deinitialized (UcanDeinitHardware() was called)
    Public Const USBCAN_EVENT_CONNECT As Integer = 6                         ' a new USB-CANmodule has been connected
    Public Const USBCAN_EVENT_DISCONNECT As Integer = 7                      ' a USB-CANmodule has been disconnected
    Public Const USBCAN_EVENT_FATALDISCON As Integer = 8                     ' a USB-CANmodule has been disconnected during operation
    Public Const USBCAN_EVENT_RESERVED1 As Integer = &H80

    ' CAN Error messages (is returned with UcanGetStatus() )
    Public Const USBCAN_CANERR_OK As Short = &H0                           ' no error
    Public Const USBCAN_CANERR_XMTFULL As Short = &H1                      ' Tx-buffer of the CAN controller is full
    Public Const USBCAN_CANERR_OVERRUN As Short = &H2                      ' Rx-buffer of the CAN controller is full
    Public Const USBCAN_CANERR_BUSLIGHT As Short = &H4                     ' Bus error: Error Limit 1 exceeded (refer to SJA1000 manual)
    Public Const USBCAN_CANERR_BUSHEAVY As Short = &H8                     ' Bus error: Error Limit 2 exceeded (refer to SJA1000 manual)
    Public Const USBCAN_CANERR_BUSOFF As Short = &H10                      ' Bus error: CAN controllerhas gone into Bus-Off state
    Public Const USBCAN_CANERR_QRCVEMPTY As Short = &H20                   ' RcvQueue is empty
    Public Const USBCAN_CANERR_QOVERRUN As Short = &H40                    ' RcvQueue overrun
    Public Const USBCAN_CANERR_QXMTFULL As Short = &H80                    ' transmit queue is full
    Public Const USBCAN_CANERR_REGTEST As Short = &H100                    ' Register test of the SJA1000 failed
    Public Const USBCAN_CANERR_MEMTEST As Short = &H200                    ' Memory test failed
    Public Const USBCAN_CANERR_TXMSGLOST As Short = &H400                  ' transmit CAN message was automatically deleted by firmware
    '                                                             ' (because transmit timeout run over)

    ' USB error messages (is returned with UcanGetStatus() )
    Public Const USBCAN_USBERR_OK As Short = &H0                           ' no error

    ' ABR and ACR for mode "receive all CAN messages"
    Public Const USBCAN_AMR_ALL As Integer = &HFFFFFFFF
    Public Const USBCAN_ACR_ALL As Integer = &H0

    Public Const USBCAN_OCR_DEFAULT As Byte = &H1A                        ' default OCR for standard GW-002
    Public Const USBCAN_OCR_RS485_ISOLATED As Byte = &H1E                 ' OCR for RS485 interface and galvanic isolation
    Public Const USBCAN_OCR_RS485_NOT_ISOLATED As Byte = &HA              ' OCR for RS485 interface without galvanic isolation

    Public Const USBCAN_DEFAULT_BUFFER_ENTRIES As Short = 4096

    Public Const USBCAN_CHANNEL_CH0 As Byte = 0
    Public Const USBCAN_CHANNEL_CH1 As Byte = 1
    Public Const USBCAN_CHANNEL_ANY As Byte = 255                         ' only available for functions UcanCallbackFktEx, UcanReadCanMsgEx
    Public Const USBCAN_CHANNEL_ALL As Byte = 254                         ' only available for methode Shutdown()
    Public Const USBCAN_CHANNEL_CAN1 As Byte = USBCAN_CHANNEL_CH0         ' differences between software and label at hardware
    Public Const USBCAN_CHANNEL_CAN2 As Byte = USBCAN_CHANNEL_CH1         ' differences between software and label at hardware
    Public Const USBCAN_CHANNEL_LIN As Byte = USBCAN_CHANNEL_CH1          ' reserved for future use

    ' definitions for function UcanResetCanEx()
    Public Const USBCAN_RESET_ALL As Integer = &H0                           ' reset everything
    Public Const USBCAN_RESET_NO_STATUS As Integer = &H1                     ' no CAN status reset  (only supported in new devices - not GW-001/002)
    Public Const USBCAN_RESET_NO_CANCTRL As Integer = &H2                    ' no CAN controller reset
    Public Const USBCAN_RESET_NO_TXCOUNTER As Integer = &H4                  ' no TX message counter reset
    Public Const USBCAN_RESET_NO_RXCOUNTER As Integer = &H8                  ' no RX message counter reset
    Public Const USBCAN_RESET_NO_TXBUFFER_CH As Integer = &H10               ' no TX message buffer reset at channel level
    Public Const USBCAN_RESET_NO_TXBUFFER_DLL As Integer = &H20              ' no TX message buffer reset at USBCAN32.DLL level
    Public Const USBCAN_RESET_NO_TXBUFFER_FW As Integer = &H80               ' no TX message buffer reset at firmware level
    Public Const USBCAN_RESET_NO_RXBUFFER_CH As Integer = &H100              ' no RX message buffer reset at channel level
    Public Const USBCAN_RESET_NO_RXBUFFER_DLL As Integer = &H200             ' no RX message buffer reset at USBCAN32.DLL level
    Public Const USBCAN_RESET_NO_RXBUFFER_SYS As Integer = &H400             ' no RX message buffer reset at kernel driver level
    Public Const USBCAN_RESET_NO_RXBUFFER_FW As Integer = &H800              ' no RX message buffer reset at firmware level
    Public Const USBCAN_RESET_FIRMWARE As Integer = &HFFFFFFFF               ' buffers, counters and CANCRTL will be reseted indirectly during firmware reset
    '                                                             '      (means automatically disconnect from USB port in 500ms)

    ' combinations of flags for UcanResetCanEx()
    '      NOTE: for combinations use OR (example: USBCAN_RESET_NO_COUNTER_ALL or USBCAN_RESET_NO_BUFFER_ALL)
    Public Const USBCAN_RESET_NO_COUNTER_ALL As Integer = (USBCAN_RESET_NO_TXCOUNTER Or USBCAN_RESET_NO_RXCOUNTER)
    Public Const USBCAN_RESET_NO_TXBUFFER_COMM As Integer = (USBCAN_RESET_NO_TXBUFFER_DLL Or &H40 Or USBCAN_RESET_NO_TXBUFFER_FW)
    Public Const USBCAN_RESET_NO_RXBUFFER_COMM As Integer = (USBCAN_RESET_NO_RXBUFFER_DLL Or USBCAN_RESET_NO_RXBUFFER_SYS Or USBCAN_RESET_NO_RXBUFFER_FW)
    Public Const USBCAN_RESET_NO_TXBUFFER_ALL As Integer = (USBCAN_RESET_NO_TXBUFFER_CH Or USBCAN_RESET_NO_TXBUFFER_COMM)
    Public Const USBCAN_RESET_NO_RXBUFFER_ALL As Integer = (USBCAN_RESET_NO_RXBUFFER_CH Or USBCAN_RESET_NO_RXBUFFER_COMM)
    Public Const USBCAN_RESET_NO_BUFFER_COMM As Integer = (USBCAN_RESET_NO_TXBUFFER_COMM Or USBCAN_RESET_NO_RXBUFFER_COMM)
    Public Const USBCAN_RESET_NO_BUFFER_ALL As Integer = (USBCAN_RESET_NO_TXBUFFER_ALL Or USBCAN_RESET_NO_RXBUFFER_ALL)
    '      NOTE: for combinations use AND instead of OR (example: USBCAN_RESET_ONLY_RX_BUFF and USBCAN_RESET_ONLY_STATUS)
    Public Const USBCAN_RESET_ONLY_STATUS As Integer = (&HFFFF And Not (USBCAN_RESET_NO_STATUS))
    Public Const USBCAN_RESET_ONLY_CANCTRL As Integer = (&HFFFF And Not (USBCAN_RESET_NO_CANCTRL))
    Public Const USBCAN_RESET_ONLY_TXBUFFER_FW As Integer = (&HFFFF And Not (USBCAN_RESET_NO_TXBUFFER_FW))
    Public Const USBCAN_RESET_ONLY_RXBUFFER_FW As Integer = (&HFFFF And Not (USBCAN_RESET_NO_RXBUFFER_FW))
    Public Const USBCAN_RESET_ONLY_RXCHANNEL_BUFF As Integer = (&HFFFF And Not (USBCAN_RESET_NO_RXBUFFER_CH))
    Public Const USBCAN_RESET_ONLY_TXCHANNEL_BUFF As Integer = (&HFFFF And Not (USBCAN_RESET_NO_TXBUFFER_CH))
    Public Const USBCAN_RESET_ONLY_RX_BUFF As Integer = (&HFFFF And Not (USBCAN_RESET_NO_RXBUFFER_ALL Or USBCAN_RESET_NO_RXCOUNTER))
    Public Const USBCAN_RESET_ONLY_RX_BUFF_GW002 As Integer = (&HFFFF And Not (USBCAN_RESET_NO_RXBUFFER_ALL Or USBCAN_RESET_NO_RXCOUNTER Or USBCAN_RESET_NO_TXBUFFER_FW))
    Public Const USBCAN_RESET_ONLY_TX_BUFF As Integer = (&HFFFF And Not (USBCAN_RESET_NO_TXBUFFER_ALL Or USBCAN_RESET_NO_TXCOUNTER))
    Public Const USBCAN_RESET_ONLY_ALL_BUFF As Integer = (USBCAN_RESET_ONLY_RX_BUFF And USBCAN_RESET_ONLY_TX_BUFF)
    Public Const USBCAN_RESET_ONLY_ALL_COUNTER As Integer = (&HFFFF And Not (USBCAN_RESET_NO_COUNTER_ALL))

    Public Const USBCAN_PRODCODE_MASK_PID As Integer = &HFFFF
    Public Const USBCAN_PRODCODE_MASK_DID As Integer = &HFFFF0000
    Public Const USBCAN_PRODCODE_PID_TWO_CHA As Integer = &H1
    Public Const USBCAN_PRODCODE_PID_TERM As Integer = &H1
    Public Const USBCAN_PRODCODE_PID_RBUSER As Integer = &H1
    Public Const USBCAN_PRODCODE_PID_RBCAN As Integer = &H1

    Public Const USBCAN_PRODCODE_PID_GW001 As Integer = &H1100               ' order code GW-001 "USB-CANmodul" outdated
    Public Const USBCAN_PRODCODE_PID_GW002 As Integer = &H1102               ' order code GW-002 "USB-CANmodul" outdated
    Public Const USBCAN_PRODCODE_PID_MULTIPORT As Integer = &H1103           ' order code 3004006
    Public Const USBCAN_PRODCODE_PID_BASIC As Integer = &H1104               ' order code 3204000 "USB-CANmodul1"
    Public Const USBCAN_PRODCODE_PID_ADVANCED As Integer = &H1105            ' order code 3204002 "USB-CANmodul2"
    Public Const USBCAN_PRODCODE_PID_USBCAN8 As Integer = &H1107             ' order code 3404000 "USB-CANmodul8"
    Public Const USBCAN_PRODCODE_PID_USBCAN16 As Integer = &H1109            ' order code 3404001 "USB-CANmodul16"
    Public Const USBCAN_PRODCODE_PID_RESERVED1 As Integer = &H1144
    Public Const USBCAN_PRODCODE_PID_RESERVED2 As Integer = &H1145

    ' definitions for cyclic CAN messages
    Public Const USBCAN_MAX_CYCLIC_CANMSG As Integer = 16

    ' stopps the transmission of cyclic CAN messages (use instead of USBCAN_CYCLIC_FLAG_START)
    Public Const USBCAN_CYCLIC_FLAG_STOPP As Integer = &H0

    ' the following flags can be cobined
    Public Const USBCAN_CYCLIC_FLAG_START As Integer = &H80000000            ' global enable of transmission of cyclic CAN messages
    Public Const USBCAN_CYCLIC_FLAG_SEQUMODE As Integer = &H40000000         ' list of cyclcic CAN messages will be processed sín
    '                                                             ' sequential mode (otherwise in parallel mode)
    Public Const USBCAN_CYCLIC_FLAG_NOECHO As Integer = &H10000              ' each sent CAN message of the list will be sent back
    '                                                             ' to the host

    ' each CAN message from the list can be enabled or disabled separatly
    Public Const USBCAN_CYCLIC_FLAG_LOCK_0 As Integer = &H1                  ' if some of these bits are set, then the according
    Public Const USBCAN_CYCLIC_FLAG_LOCK_1 As Integer = &H2                  ' CAN message from the list is disabled
    Public Const USBCAN_CYCLIC_FLAG_LOCK_2 As Integer = &H4
    Public Const USBCAN_CYCLIC_FLAG_LOCK_3 As Integer = &H8
    Public Const USBCAN_CYCLIC_FLAG_LOCK_4 As Integer = &H10
    Public Const USBCAN_CYCLIC_FLAG_LOCK_5 As Integer = &H20
    Public Const USBCAN_CYCLIC_FLAG_LOCK_6 As Integer = &H40
    Public Const USBCAN_CYCLIC_FLAG_LOCK_7 As Integer = &H80
    Public Const USBCAN_CYCLIC_FLAG_LOCK_8 As Integer = &H100
    Public Const USBCAN_CYCLIC_FLAG_LOCK_9 As Integer = &H200
    Public Const USBCAN_CYCLIC_FLAG_LOCK_10 As Integer = &H400
    Public Const USBCAN_CYCLIC_FLAG_LOCK_11 As Integer = &H800
    Public Const USBCAN_CYCLIC_FLAG_LOCK_12 As Integer = &H1000
    Public Const USBCAN_CYCLIC_FLAG_LOCK_13 As Integer = &H2000
    Public Const USBCAN_CYCLIC_FLAG_LOCK_14 As Integer = &H4000
    Public Const USBCAN_CYCLIC_FLAG_LOCK_15 As Integer = &H8000

    ' definitions for function UcanGetMsgPending()
    Public Const USBCAN_PENDING_FLAG_RX_DLL As Integer = &H1                 ' returns number of pending RX CAN messages in USBCAN32.DLL
    Public Const USBCAN_PENDING_FLAG_RX_SYS As Integer = &H2                 ' (not implemented yet) returns number of pending RX CAN messages in USBCAN.SYS or UCANNET.SYS
    Public Const USBCAN_PENDING_FLAG_RX_FW As Integer = &H4                  ' returns number of pending RX CAN messages in firmware
    Public Const USBCAN_PENDING_FLAG_TX_DLL As Integer = &H10                ' returns number of pending TX CAN messages in USBCAN32.DLL
    Public Const USBCAN_PENDING_FLAG_TX_SYS As Integer = &H20                ' place holder - there is no TX buffer in USBCAN.SYS or UCANNET.SYS
    Public Const USBCAN_PENDING_FLAG_TX_FW As Integer = &H40                 ' returns number of pending TX CAN messages in firmware
    ' These bits can be combined. In these case the function UcanGetMsgPending() returns the summary of the counters.

    Public Const USBCAN_PENDING_FLAG_RX_ALL As Integer = (USBCAN_PENDING_FLAG_RX_DLL Or USBCAN_PENDING_FLAG_RX_SYS Or USBCAN_PENDING_FLAG_RX_FW)
    Public Const USBCAN_PENDING_FLAG_TX_ALL As Integer = (USBCAN_PENDING_FLAG_TX_DLL Or USBCAN_PENDING_FLAG_TX_SYS Or USBCAN_PENDING_FLAG_TX_FW)
    Public Const USBCAN_PENDING_FLAG_ALL As Integer = (USBCAN_PENDING_FLAG_RX_ALL Or USBCAN_PENDING_FLAG_TX_ALL)

#End Region

#Region " Types of USBCAN32.DLL ########################################################### "

    ' -------------------------------------------------------------------------------------
    ' Types copied from Usbcan32.h
    ' -------------------------------------------------------------------------------------

    ' Structure for the CAN message (suitable for CAN messages according to spec. CAN2.0B)
    <StructLayout(LayoutKind.Sequential, Pack:=1)> _
    Public Structure tCanMsgStruct
        Public m_dwID As Integer   ' CAN Identifier
        Public m_bFF As Byte       ' CAN Frame format (BIT7=1: 29BitID / BIT6=1: RTR-Frame)
        Public m_bDLC As Byte      ' CAN Data Length Code
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=8)> _
        Public m_bData As Byte()   ' CAN Data
        Public m_dwTime As Integer ' Time in ms
        Public Shared Function CreateInstance(Optional ByVal dwID_p As Integer = 0, Optional ByVal bFF_p As Byte = 0) As tCanMsgStruct
            Dim instance As tCanMsgStruct
            instance.m_bData = Array.CreateInstance(GetType(Byte), 8)
            instance.m_bDLC = 8
            instance.m_bFF = bFF_p
            instance.m_dwID = dwID_p
            Return instance
        End Function
    End Structure 'tCanMsgStruct

    ' Structure with the stati (Function: UcanGetStatus())
    <StructLayout(LayoutKind.Sequential, Pack:=1)> _
    Public Structure tStatusStruct
        Public m_wCanStatus As Short   ' current CAN status
        Public m_wUsbStatus As Short   ' current USB status
    End Structure 'tStatusStruct

    ' Structure with init parameters for function UcanInitCanEx() and UcanInitCanEx2()
    '    <StructLayout(LayoutKind.Sequential, Pack:=1, Size:=24)> _
    <StructLayout(LayoutKind.Sequential, Pack:=1)> _
    Protected Structure tUcanInitCanParam
        Public m_dwSize As Integer             ' [IN] size of this structure
        Public m_bMode As tUcanMode            ' [IN] selects the mode of CAN controller (see tUcanMode)
        ' Baudrate Registers for GW-001 or GW-002
        Public m_bBTR0 As Byte                 ' [IN] Bus Timing Register 0 (SJA1000 - use high byte USBCAN_BAUD_...)
        Public m_bBTR1 As Byte                 ' [IN] Bus Timing Register 1 (SJA1000 - use low  byte USBCAN_BAUD_...)
        'Public m_bBTR As Short                ' [IN] Bus Timing Register 0 (SJA1000 - use high byte USBCAN_BAUD_...)
        Public m_bOCR As Byte                  ' [IN] Output Controll Register of SJA1000 (should be 0x1A)
        Public m_dwAMR As Integer              ' [IN] Acceptance Mask Register (SJA1000)
        Public m_dwACR As Integer              ' [IN] Acceptance Code Register (SJA1000)
        ' since version V3.00 - will be ignored from function UcanInitCanEx()
        Public m_dwBaudrate As Integer         ' [IN] Baudrate Register for Multiport 3004006, USB-CANmodul1 3204000, USB-CANmodul2 3204002 (use USBCAN_BAUDEX_...)
        ' since version V3.05
        Public m_wNrOfRxBufferEntries As Short ' [IN] number of receive buffer entries (default is 4096)
        Public m_wNrOfTxBufferEntries As Short ' [IN] number of transmit buffer entries (default is 4096)
    End Structure 'tUcanInitCanParam

    ' Structure with hardware info
    <StructLayout(LayoutKind.Sequential, Pack:=1)> _
    Public Structure tUcanHardwareInfoEx
        Public m_dwSize As Integer     ' [IN] size of this structure
        Public m_UcanHandle As Byte       ' [OUT] USB-CAN-Handle assigned by the DLL
        Public m_bDeviceNr As Byte        ' [OUT] device number of the USB-CANmodule
        Public m_dwSerialNr As Integer    ' [OUT] serial number from USB-CANmodule
        Public m_dwFwVersionEx As Integer ' [OUT] version of firmware
        Public m_dwProductCode As Integer ' [OUT] product code (for differentiate between different hardware modules)
        '                                 '       see constants USBCAN_PRODCODE_...
    End Structure 'tUcanHardwareInfoEx

    ' Structure with info of one CAN channel
    <StructLayout(LayoutKind.Sequential, Pack:=1)> _
    Public Structure tUcanChannelInfo
        Public m_dwSize As Integer       ' [IN] size of this structure
        Public m_bMode As Byte           ' [OUT] slecets the mode of CAN controller (see tUcanMode)
        Public m_bBTR0 As Byte           ' [OUT] Bus Timing Register 0 (SJA1000 - use high byte USBCAN_BAUD_...)
        Public m_bBTR1 As Byte           ' [OUT] Bus Timing Register 1 (SJA1000 - use low  byte USBCAN_BAUD_...)
        Public m_bOCR As Byte            ' [OUT] Output Controll Register of SJA1000 (should be 0x1A)
        Public m_dwAMR As Integer        ' [OUT] Acceptance Mask Register (SJA1000)
        Public m_dwACR As Integer        ' [OUT] Acceptance Code Register (SJA1000)
        Public m_dwBaudrate As Integer   ' [OUT] Baudrate Register for Multiport 3004006, USB-CANmodul1 3204000, USB-CANmodul2 3204002 (use USBCAN_BAUDEX_...)
        <MarshalAs(UnmanagedType.Bool)> _
        Public m_fCanIsInit As Boolean   ' [OUT] is TRUE if CAN interface was initialized, otherwise FALSE
        Public m_wCanStatus As Short     ' [OUT] CAN status (same as received from function UcanGetStatus..())
    End Structure 'tUcanChannelInfo

    Public Enum tUcanMode As Byte
        kUcanModeNormal = 0         ' normal mode (send and reciceive)
        kUcanModeListenOnly = 1     ' listen only mode (only reciceive)
        kUcanModeTxEcho = 2         ' CAN messages which was sent will be received at UcanReadCanMsg..
        kUcanModeRxOrderCh = 4      ' reserved (not implemented in this version)
    End Enum

    ' structure with transfered packet informations
    <StructLayout(LayoutKind.Sequential, Pack:=1)> _
    Protected Structure tUcanMsgCountInfo
        Public m_wSentMsgCount As Short       ' counter of sent CAN messages
        Public m_wRecvdMsgCount As Short      ' counter of received CAN messages
    End Structure 'tUcanMsgCountInfo

    Protected Enum tUcanVersionType As Short
        kVerTypeUserLib = 1         ' version of the library
        kVerTypeUserDll = 1         ' returns the version of USBCAN32.DLL
        kVerTypeSysDrv = 2          ' returns the version of USBCAN.SYS (not supported in this version)
        kVerTypeFirmware = 3        ' returns the version of firmware in hardware (not supported, use function UcanGetFwVersion)
        kVerTypeNetDrv = 4          ' version of UCANNET.SYS  (not supported for WinCE)
        kVerTypeSysLd = 5           ' version of USBCANLD.SYS (not supported for WinCE) (loader for USB-CANmodul GW-001)
        kVerTypeSysL2 = 6           ' version of USBCANL2.SYS                           (loader for USB-CANmodul GW-002)
        kVerTypeSysL3 = 7           ' version of USBCANL3.SYS (not supported for WinCE) (loader for Multiport CAN-to-USB 340400x or 3004006)
        kVerTypeSysL4 = 8           ' version of USBCANL4.SYS                           (loader for USB-CANmodul1 3204000 or 3204001)
        kVerTypeSysL5 = 9           ' version of USBCANL5.SYS                           (loader for USB-CANmodul1 3204002 or 3204003)
        kVerTypeCpl = 10            ' version of USBCANCP.CPL (not supported for WinCE)
    End Enum

#End Region

#Region " Unmanaged API functions of USBCAN32.DLL ######################################### "

    ' -------------------------------------------------------------------------------------
    ' Functions imported from Usbcan32.dll
    ' -------------------------------------------------------------------------------------

    ' BOOL PUBLIC UcanSetDebugMode (DWORD dwDbgLevel_p, _TCHAR* pszFilePathName_p, DWORD dwFlags_p);
    Protected Declare Auto Function UcanSetDebugMode Lib "usbcan32.dll" _
        (ByVal dwDbgLevel_p As Integer, <MarshalAs(UnmanagedType.LPStr)> ByVal pszFilePathName_p As String, _
            ByVal dwFlags_p As Integer) As <MarshalAs(UnmanagedType.Bool)> Boolean

    ' DWORD PUBLIC UcanGetVersionEx (tUcanVersionType VerType_p);
    Protected Declare Auto Function UcanGetVersionEx Lib "usbcan32.dll" _
       (ByVal VerType_p As tUcanVersionType) As Integer

    ' DWORD PUBLIC UcanGetFwVersion (tUcanHandle UcanHandle_p);
    Protected Declare Auto Function UcanGetFwVersion Lib "usbcan32.dll" _
       (ByVal UcanHandle_p As Byte) As Integer

    ' BYTE PUBLIC UcanInitHwConnectControlEx (tConnectControlFktEx fpConnectControlFktEx_p, void* pCallbackArg_p);
    Protected Declare Auto Function UcanInitHwConnectControlEx Lib "usbcan32.dll" _
       (<MarshalAs(UnmanagedType.FunctionPtr)> ByVal fpConnectControlFktEx_p As tConnectControlFktEx, _
        ByVal pCallbackArg_p As IntPtr) As Byte

    ' BYTE PUBLIC UcanDeinitHwConnectControl (void)
    Protected Declare Auto Sub UcanDeinitHwConnectControl Lib "usbcan32.dll" ()

    ' BYTE PUBLIC UcanInitHardwareEx (tUcanHandle* pUcanHandle_p, BYTE bDeviceNr_p,
    '   tCallbackFktEx fpCallbackFktEx_p, void* pCallbackArg_p);
    Protected Declare Auto Function UcanInitHardwareEx Lib "usbcan32.dll" _
       (ByRef pUcanHandle_p As Byte, ByVal bDeviceNr_p As Byte, _
        <MarshalAs(UnmanagedType.FunctionPtr)> ByVal fpCallbackFktEx_p As tCallbackFktEx, _
        ByVal pCallbackArg_p As IntPtr) As Byte

    ' UCANRET PUBLIC UcanGetModuleTime (tUcanHandle UcanHandle_p, DWORD* pdwTime_p);
    Protected Declare Auto Function UcanGetModuleTime Lib "usbcan32.dll" _
       (ByVal UcanHandle_p As Byte, ByRef pdwTime_p As Integer) As Byte

    ' BYTE PUBLIC UcanGetHardwareInfoEx2 (tUcanHandle UcanHandle_p,
    '   tUcanHardwareInfoEx* pHwInfo_p,
    '   tUcanChannelInfo* pCanInfoCh0_p, tUcanChannelInfo* pCanInfoCh1_p);
    Protected Declare Auto Function UcanGetHardwareInfoEx2 Lib "usbcan32.dll" _
       (ByVal UcanHandle_p As Byte, ByRef pHwInfo_p As tUcanHardwareInfoEx, _
        ByRef pCanInfoCh0_p As tUcanChannelInfo, ByRef pCanInfoCh1_p As tUcanChannelInfo) As Byte

    ' BYTE PUBLIC UcanInitCanEx2 (tUcanHandle UcanHandle_p, BYTE bChannel_p, tUcanInitCanParam* pInitCanParam_p);
    Protected Declare Auto Function UcanInitCanEx2 Lib "usbcan32.dll" _
       (ByVal UcanHandle_p As Byte, ByVal bChannel_p As Byte, _
        <[In]()> ByRef pInitCanParam_p As tUcanInitCanParam) As Byte

    ' BYTE PUBLIC UcanSetBaudrateEx (tUcanHandle UcanHandle_p,
    '   BYTE bChannel_p, BYTE bBTR0_p, BYTE bBTR1_p, DWORD dwBaudrate_p);
    Protected Declare Auto Function UcanSetBaudrateEx Lib "usbcan32.dll" _
       (ByVal UcanHandle_p As Byte, ByVal bChannel_p As Byte, _
        ByVal bBTR0_p As Byte, ByVal bBTR1_p As Byte, ByVal dwBaudrate_p As Integer) As Byte

    ' BYTE PUBLIC UcanSetAcceptanceEx (tUcanHandle UcanHandle_p, BYTE bChannel_p,
    '   DWORD dwAMR_p, DWORD dwACR_p);
    Protected Declare Auto Function UcanSetAcceptanceEx Lib "usbcan32.dll" _
       (ByVal UcanHandle_p As Byte, ByVal bChannel_p As Byte, _
        ByVal dwAMR_p As Integer, ByVal dwACR_p As Integer) As Byte

    ' BYTE PUBLIC UcanResetCanEx (tUcanHandle UcanHandle_p, BYTE bChannel_p, DWORD dwResetFlags_p);
    Protected Declare Auto Function UcanResetCanEx Lib "usbcan32.dll" _
       (ByVal UcanHandle_p As Byte, ByVal bChannel_p As Byte, _
        ByVal dwResetFlags_p As Integer) As Byte

    ' BYTE PUBLIC UcanReadCanMsgEx (tUcanHandle UcanHandle_p, BYTE* pbChannel_p,
    '   tCanMsgStruct* pCanMsg_p, DWORD* pdwCount_p);
    Protected Declare Auto Function UcanReadCanMsgEx Lib "usbcan32.dll" _
       (ByVal UcanHandle_p As Byte, ByRef pbChannel_p As Byte, _
        <Out(), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=3)> ByVal pCanMsg_p() As tCanMsgStruct, _
        ByRef pdwCount_p As Integer) As Byte

    ' BYTE PUBLIC UcanWriteCanMsgEx (tUcanHandle UcanHandle_p, BYTE bChannel_p,
    '   tCanMsgStruct* pCanMsg_p, DWORD* pdwCount_p);
    Protected Declare Auto Function UcanWriteCanMsgEx Lib "usbcan32.dll" _
       (ByVal UcanHandle_p As Byte, ByVal bChannel_p As Byte, _
        <[In](), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=3)> ByVal pCanMsg_p() As tCanMsgStruct, _
        ByRef pdwCount_p As Integer) As Byte

    'BYTE PUBLIC UcanGetStatusEx (tUcanHandle UcanHandle_p, BYTE bChannel_p, tStatusStruct* pStatus_p);
    Protected Declare Auto Function UcanGetStatusEx Lib "usbcan32.dll" _
       (ByVal UcanHandle_p As Byte, ByVal bChannel_p As Byte, _
        ByRef pStatus_p As tStatusStruct) As Byte

    ' BYTE PUBLIC UcanGetMsgCountInfoEx (tUcanHandle UcanHandle_p, BYTE bChannel_p,
    '   tUcanMsgCountInfo* pMsgCountInfo_p);
    Protected Declare Auto Function UcanGetMsgCountInfoEx Lib "usbcan32.dll" _
       (ByVal UcanHandle_p As Byte, ByVal bChannel_p As Byte, _
        ByRef pMsgCountInfo_p As tUcanMsgCountInfo) As Byte

    ' UCANRET PUBLIC UcanGetMsgPending (tUcanHandle UcanHandle_p,
    '   BYTE bChannel_p, DWORD dwFlags_p, DWORD* pdwPendingCount_p);
    Protected Declare Auto Function UcanGetMsgPending Lib "usbcan32.dll" _
       (ByVal UcanHandle_p As Byte, ByVal bChannel_p As Byte, ByVal dwFlags_p As Integer, _
       ByRef pdwPendingCount_p As Integer) As Byte

    ' UCANRET PUBLIC UcanGetCanErrorCounter (tUcanHandle UcanHandle_p,
    '   BYTE bChannel_p, DWORD* pdwTxErrorCounter_p, DWORD* pdwRxErrorCounter_p);
    Protected Declare Auto Function UcanGetCanErrorCounter Lib "usbcan32.dll" _
       (ByVal UcanHandle_p As Byte, ByVal bChannel_p As Byte, _
       ByRef pdwTxErrorCounter_p As Integer, ByRef pdwRxErrorCounter_p As Integer) As Byte

    ' UCANRET PUBLIC UcanSetTxTimeout (tUcanHandle UcanHandle_p,
    '   BYTE bChannel_p, DWORD dwTxTimeout_p);
    Protected Declare Auto Function UcanSetTxTimeout Lib "usbcan32.dll" _
       (ByVal UcanHandle_p As Byte, ByVal bChannel_p As Byte, ByVal dwTxTimeout_p As Integer) As Byte

    ' BYTE PUBLIC UcanDeinitCanEx (tUcanHandle UcanHandle_p, BYTE bChannel_p);
    Protected Declare Auto Function UcanDeinitCanEx Lib "usbcan32.dll" _
       (ByVal UcanHandle_p As Byte, ByVal bChannel_p As Byte) As Byte

    ' BYTE PUBLIC UcanDeinitHardware (tUcanHandle UcanHandle_p);
    Protected Declare Auto Function UcanDeinitHardware Lib "usbcan32.dll" _
       (ByVal UcanHandle_p As Byte) As Byte


    ' -------------------------------------------------------------------------------------
    ' Callback Functions (Delegates)
    ' -------------------------------------------------------------------------------------

    ' void PUBLIC UcanCallbackFktEx (tUcanHandle UcanHandle_p, DWORD dwEvent_p,
    '                                BYTE bChannel_p, void* pArg_p);
    Protected Delegate Sub tCallbackFktEx(ByVal tUcanHandle_p As Byte, ByVal dwEvent_p As Integer, _
        ByVal bChannel_p As Byte, ByVal pArg_p As IntPtr)

    ' void (PUBLIC *tConnectControlFktEx) (DWORD dwEvent_p, DWORD dwParam_p, void* pArg_p);
    Protected Delegate Sub tConnectControlFktEx(ByVal dwEvent_p As Integer, _
        ByVal dwParam_p As Integer, ByVal pArg_p As IntPtr)


    ' -------------------------------------------------------------------------------------
    ' Functions for cyclic transmission by the hardware
    ' -------------------------------------------------------------------------------------

    ' UCANRET PUBLIC UcanDefineCyclicCanMsg (tUcanHandle UcanHandle_p,
    '   BYTE bChannel_p, tCanMsgStruct* pCanMsgList_p, DWORD dwCount_p);
    Protected Declare Auto Function UcanDefineCyclicCanMsg Lib "usbcan32.dll" _
       (ByVal UcanHandle_p As Byte, ByVal bChannel_p As Byte, _
        <[In](), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=16)> ByVal pCanMsgList_p() As tCanMsgStruct, _
        ByVal dwCount_p As Integer) As Byte

    ' UCANRET PUBLIC UcanReadCyclicCanMsg (tUcanHandle UcanHandle_p,
    '   BYTE bChannel_p, tCanMsgStruct* pCanMsgList_p, DWORD* pdwCount_p);
    Protected Declare Auto Function UcanReadCyclicCanMsg Lib "usbcan32.dll" _
       (ByVal UcanHandle_p As Byte, ByVal bChannel_p As Byte, _
        <[Out](), MarshalAs(UnmanagedType.LPArray, SizeParamIndex:=16)> ByVal pCanMsgList_p() As tCanMsgStruct, _
        ByRef pdwCount_p As Integer) As Byte

    ' UCANRET PUBLIC UcanEnableCyclicCanMsg (tUcanHandle UcanHandle_p,
    '   BYTE bChannel_p, DWORD dwFlags_p);
    Protected Declare Auto Function UcanEnableCyclicCanMsg Lib "usbcan32.dll" _
       (ByVal UcanHandle_p As Byte, ByVal bChannel_p As Byte, ByVal dwFlags_p As Integer) As Byte

#End Region

#Region " Event declarations ############################################################## "

    ' -------------------------------------------------------------------------------------
    ' Event declarations
    ' -------------------------------------------------------------------------------------

    Public Event CanMsgReceivedEvent(ByVal bChannel_p As Byte) ' a new CAN message has been received
    Public Event InitHwEvent()                                 ' the USB-CANmodule has been initialized
    Public Event InitCanEvent(ByVal bChannel_p As Byte)        ' the CAN interface has been initialized
    Public Event StatusEvent(ByVal bChannel_p As Byte)         ' the error state in the module has changed
    Public Event DeinitCanEvent(ByVal bChannel_p As Byte)      ' the CAN interface has been deinitialized (UcanDeinitCan() was called)
    Public Event DeinitHwEvent()                               ' the USB-CANmodule has been deinitialized (UcanDeinitHardware() was called)
    Public Shared Event ConnectEvent()                         ' a new USB-CANmodule has been connected
    Public Shared Event DisconnectEvent()                      ' a USB-CANmodule has been disconnected
    Public Event FatalDisconnectEvent()                        ' a USB-CANmodule has been disconnected during operation

#End Region

#Region " Public methodes ################################################################# "

    ' -------------------------------------------------------------------------------------
    ' Public methods
    ' -------------------------------------------------------------------------------------

    '---------------------------------------------------------------------------
    '
    ' Constructor: New()
    '
    ' Description: Initializes the class instance
    '
    ' Parameters:  none
    '
    ' Returns:     none
    '
    '---------------------------------------------------------------------------

    Public Sub New()

        MyBase.New()

        ' if first instance register ConnectControll callback function
        If (dwPlugAndPlayCount_l = 0) Then
            gchUcanConnectControl_l = GCHandle.Alloc(pfnUcanConnectControl_l, GCHandleType.Normal)
            UcanInitHwConnectControlEx(pfnUcanConnectControl_l, IntPtr.Zero)
        End If

        dwPlugAndPlayCount_l += 1

    End Sub


    '---------------------------------------------------------------------------
    '
    ' Function:    UcanSetDebugMode()
    '
    ' Description: sets a new debug mode
    '
    ' Parameters:  dwDbgLevel_p        = debug level (bit format)
    '              pszFilePathName_p   = file path to debug log file
    '              dwFlags_p           = 0x00000000 no file append mode
    '                                    0x00000001 file append mode
    '
    ' Return:      BOOL    = FALSE if logfile not created otherwise TRUE
    '
    '---------------------------------------------------------------------------

    Public Shared Function SetDebugMode(ByVal dwDbgLevel_p As Integer, ByVal pszFilePathName_p As String, _
        Optional ByVal dwFlags_p As Integer = 0) As Boolean

        ' call unmanaged function
        Return UcanSetDebugMode(dwDbgLevel_p, pszFilePathName_p, dwFlags_p)

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    InitHardware()
    '
    ' Description: Initializes a USB-CANmodule with the device number x
    '              and registers the internal callback function
    '
    ' Parameters:  bDeviceNr_p     = [IN]  device number of the USB-CANmodule
    '                                      valid values: 0 through 254
    '                                      USBCAN_ANY_MODULE: the first module that is found will be used
    '
    ' Returns:     result of the function
    '
    '---------------------------------------------------------------------------

    Public Function InitHardware(Optional ByVal bDeviceNr_p As Byte = USBCAN_ANY_MODULE) As Byte

        Dim bRet As Byte = USBCAN_SUCCESSFUL

        ' check if already initialized
        If (m_fHwIsInitialized = False) Then

            ' initialize hardware
            m_gchUcanCallback = GCHandle.Alloc(m_pfnUcanCallback, GCHandleType.Normal)

            Try
                bRet = UcanInitHardwareEx(m_UcanHandle, bDeviceNr_p, m_pfnUcanCallback, IntPtr.Zero)
            Catch ex1 As System.Runtime.InteropServices.SEHException
                ' sometimes this exception occures in UcanInitHardwareEx:
                ' System.Runtime.InteropServices.SEHException: Eine externe Komponente hat eine Ausnahme ausgelöst.
                bRet = USBCAN_RESERVED  ' &HC0
            Catch ex2 As System.NullReferenceException
                ' this exception should never occur anymore
                ' bug in USBCAN32.DLL is solved where pUcanEntry->m_fpCallbackFktEx contains an old and wrong value in UcanInitHardware()
                m_fHwIsInitialized = True
                bRet = USBCAN_RESERVED + 1  ' &HC1
            End Try

            If (bRet = USBCAN_SUCCESSFUL) Then
                m_fHwIsInitialized = True
                ' remember this object instance in the static collection
                SyncLock (colUcanHandle_l)
                    colUcanHandle_l.Add(Me)
                End SyncLock
            Else
                m_gchUcanCallback.Free()
            End If

        End If

        Return bRet

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    InitCan()
    '
    ' Description: Initializes the appropriate CAN interface
    '
    ' Parameters:  bChannel_p     = [IN]  CAN channel (USBCAN_CHANNEL_CH0 or USBCAN_CHANNEL_CH1)
    '              wBTR_p         = [IN]  baudrate as 16bit value
    '              dwBaudrate_p   = [IN]  baudrate as 32bit value for Multiport 3004006, USB-CANmodul1 3204000, USB-CANmodul2 3204002
    '              dwAMR_p        = [IN]  Acceptance Mask Register (SJA1000)
    '              dwACR_p        = [IN]  Acceptance Code Register (SJA1000)
    '              bMode_p        = [IN]  selects the mode of the CAN controller
    '              bOCR_p         = [IN]  value of Output Control Register
    '
    ' Returns:     result of the function
    '
    '---------------------------------------------------------------------------

    Public Function InitCan(Optional ByVal bChannel_p As Byte = USBCAN_CHANNEL_CH0, _
                            Optional ByVal wBTR_p As Short = USBCAN_BAUD_1MBit, _
                            Optional ByVal dwBaudrate_p As Integer = USBCAN_BAUDEX_USE_BTR01, _
                            Optional ByVal dwAMR_p As Integer = USBCAN_AMR_ALL, _
                            Optional ByVal dwACR_p As Integer = USBCAN_ACR_ALL, _
                            Optional ByVal bMode_p As Byte = tUcanMode.kUcanModeNormal, _
                            Optional ByVal bOCR_p As Byte = USBCAN_OCR_DEFAULT) As Byte

        Dim bRet As Byte = USBCAN_SUCCESSFUL
        Dim InitParam As tUcanInitCanParam

        ' check if specified CAN channel was already initialized
        If ((bChannel_p = USBCAN_CHANNEL_CH0) AndAlso (m_fCh0IsInitialized = False)) _
        OrElse ((bChannel_p = USBCAN_CHANNEL_CH1) AndAlso (m_fCh1IsInitialized = False)) Then

            ' fill out initialisation struct
            InitParam.m_dwSize = Marshal.SizeOf(InitParam)     ' size of this struct
            InitParam.m_bMode = bMode_p      ' normal operation mode
            InitParam.m_bBTR0 = (wBTR_p >> 8) And &HFF ' baudrate
            InitParam.m_bBTR1 = wBTR_p And &HFF        ' baudrate
            InitParam.m_bOCR = bOCR_p              ' standard output
            InitParam.m_dwAMR = dwAMR_p                 ' CAN message filter
            InitParam.m_dwACR = dwACR_p
            InitParam.m_dwBaudrate = dwBaudrate_p     ' baudrate Ex
            InitParam.m_wNrOfRxBufferEntries = USBCAN_DEFAULT_BUFFER_ENTRIES
            InitParam.m_wNrOfTxBufferEntries = USBCAN_DEFAULT_BUFFER_ENTRIES

            ' initialize CAN interface
            bRet = UcanInitCanEx2(m_UcanHandle, bChannel_p, InitParam)
            If (bRet = USBCAN_SUCCESSFUL) Then

                ' remember the successful init
                If (bChannel_p = USBCAN_CHANNEL_CH0) Then
                    m_fCh0IsInitialized = True
                Else
                    m_fCh1IsInitialized = True
                End If

                ' initialisation complete
                m_fIsInitialized = True
            End If

        End If

        Return bRet

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    ReadCanMsg()
    '
    ' Description: Reads one or more CAN messages
    '
    ' Parameters:  pCanMsg_p       = [OUT] array of several CAN message structure
    '
    '              pbChannel_p     = [IN/OUT] CAN channel (USBCAN_CHANNEL_CH0 or USBCAN_CHANNEL_CH1, USBCAN_CHANNEL_ANY)
    '                                         if INPUT is USBCAN_CHANNEL_ANY the OUTPUT will be the read CAN channel
    '              pdwCount_p      = [OUT] number of received CAN messages
    '
    ' Returns:     result of the function
    '
    '---------------------------------------------------------------------------

    Public Function ReadCanMsg(ByRef pbChannel_p As Byte, ByRef pCanMsgStruct_p() As tCanMsgStruct, Optional ByRef dwCount_p As Integer = 0) As Byte

        Dim bRet As Byte = USBCAN_SUCCESSFUL

        ' set message counter to actual array length
        dwCount_p = pCanMsgStruct_p.Length

        ' call unmanaged function
        bRet = UcanReadCanMsgEx(m_UcanHandle, pbChannel_p, pCanMsgStruct_p, dwCount_p)
        'Debug.WriteLine("ReadCanMsg: count=" + dwCount_p.ToString())

        ' prune the pCanMsgStruct_p Array (not used anymore because of the additional parameter dwCount_p)
        'If dwCount < pCanMsgStruct_p.Length Then
        '    Dim pCanMsgStructNew As Array = Array.CreateInstance(GetType(tCanMsgStruct), dwCount)
        '    Array.Copy(pCanMsgStruct_p, pCanMsgStructNew, dwCount)
        '    pCanMsgStruct_p = pCanMsgStructNew
        'End If
        Return bRet

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    WriteCanMsg()
    '
    ' Description: Sends one or more CAN messages
    '
    ' Parameters:  pCanMsgList_p   = [IN]  array of CAN message structure
    '
    '              bChannel_p      = [IN]  CAN channel (USBCAN_CHANNEL_CH0 or USBCAN_CHANNEL_CH1)
    '
    ' Returns:     result of the function
    '
    '---------------------------------------------------------------------------

    Public Function WriteCanMsg(ByVal bChannel_p As Byte, ByRef pCanMsgList_p() As tCanMsgStruct, Optional ByRef dwCount_p As Integer = 0) As Byte

        Dim bRet As Byte = USBCAN_SUCCESSFUL

        ' set message counter to actual array length
        dwCount_p = pCanMsgList_p.Length

        'Lame check for 0 length RPDO1 from IOX1 driver to IOX1 device on canbus (sib: 2011-06-09)
        ' Don't forget to take this out someday!
        If (pCanMsgList_p(0).m_dwID = &H240) AndAlso (pCanMsgList_p(0).m_bDLC = 0) Then
            Console.WriteLine("NO NO NO: Trying to send 0 length RPDO1 to IOX1 device with can msg ID=0x240")
            Return USBCAN_ERR_ILLPARAM
        End If

        ' call unmanaged function
        bRet = UcanWriteCanMsgEx(m_UcanHandle, bChannel_p, pCanMsgList_p, dwCount_p)
        Return bRet

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    SetBaudrate()
    '
    ' Description: Modifies the baudrate settings of the specified CAN channel of the USB-CANmodule
    '
    ' Parameters:  bBTR0_p         = [IN]  Baudrate Register 0 (GW-001/002 - Multiport 3004006, USB-CANmodul1 3204000, USB-CANmodul2 3204002 only standard values)
    '              bBTR1_p         = [IN]  Baudrate Register 1 (GW-001/002 - Multiport 3004006, USB-CANmodul1 3204000, USB-CANmodul2 3204002 only standard values)
    '
    '              bChannel_p      = [IN]  CAN channel (USBCAN_CHANNEL_CH0 or USBCAN_CHANNEL_CH1)
    '              dwBaudrate_p    = [IN]  Baudrate Register of Multiport 3004006, USB-CANmodul1 3204000, USB-CANmodul2 3204002
    '
    ' Returns:     result of the function
    '
    '---------------------------------------------------------------------------

    Public Function SetBaudrate(Optional ByVal bChannel_p As Byte = USBCAN_CHANNEL_CH0, _
        Optional ByVal wBTR_p As Short = USBCAN_BAUD_1MBit, _
        Optional ByVal dwBaudrate_p As Integer = USBCAN_BAUDEX_USE_BTR01) As Byte

        ' call unmanaged function
        Return UcanSetBaudrateEx(m_UcanHandle, bChannel_p, (wBTR_p >> 8) And &HFF, wBTR_p And &HFF, dwBaudrate_p)

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    SetAcceptance()
    '
    ' Description: Modifies the Acceptance Filter settings of the specified channel of the USB-CANmodule
    '
    ' Parameters:  dwAMR_p         = [IN]  Acceptance Filter Mask (SJA1000)
    '              dwACR_p         = [IN]  Acceptance Filter Code (SJA1000)
    '
    '              bChannel_p      = [IN]  CAN channel (USBCAN_CHANNEL_CH0 or USBCAN_CHANNEL_CH1)
    '
    ' Returns:     result of the function
    '
    '---------------------------------------------------------------------------

    Public Function SetAcceptance(Optional ByVal bChannel_p As Byte = USBCAN_CHANNEL_CH0, _
        Optional ByVal dwAMR_p As Integer = USBCAN_AMR_ALL, _
        Optional ByVal dwACR_p As Integer = USBCAN_ACR_ALL) As Byte

        ' call unmanaged function
        Return UcanSetAcceptanceEx(m_UcanHandle, bChannel_p, dwAMR_p, dwACR_p)

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    GetStatus()
    '
    ' Description: Returns the state of the USB-CANmodule
    '
    ' Parameters:  pStatus_p       = [OUT] pointer to Status structure
    '
    '              bChannel_p      = [IN]  CAN channel (USBCAN_CHANNEL_CH0 or USBCAN_CHANNEL_CH1)
    '
    ' Returns:     result of the function
    '
    '---------------------------------------------------------------------------

    Public Function GetStatus(ByVal pbChannel_p As Byte, ByRef pStatus_p As tStatusStruct) As Byte

        Dim bRet As Byte = USBCAN_SUCCESSFUL

        ' call unmanaged function
        bRet = UcanGetStatusEx(m_UcanHandle, pbChannel_p, pStatus_p)
        Return bRet

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    GetMsgCountInfo()
    '
    ' Description: Reads the packet information from USB-CANmodule (counter of
    '              received and sent CAN messages).
    '
    ' Parameters:  wRecvdMsgCount_p = [OUT] number of CAN messages received
    '
    '              wSentMsgCount_p  = [OUT] number of CAN messages sent
    '
    '              bChannel_p       = [IN]  CAN channel (USBCAN_CHANNEL_CH0 or USBCAN_CHANNEL_CH1)
    '
    ' Returns:     result of the function
    '
    '---------------------------------------------------------------------------

    Public Function GetMsgCountInfo(ByVal bChannel_p As Byte, ByRef wRecvdMsgCount_p As Short, _
        ByRef wSentMsgCount_p As Short) As Byte

        Dim bRet As Byte = USBCAN_SUCCESSFUL
        Dim msgCountInfo As tUcanMsgCountInfo

        ' call unmanaged function
        bRet = UcanGetMsgCountInfoEx(m_UcanHandle, bChannel_p, msgCountInfo)

        ' copy the result to the output parameters
        wRecvdMsgCount_p = msgCountInfo.m_wRecvdMsgCount
        wSentMsgCount_p = msgCountInfo.m_wSentMsgCount

        Return bRet

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    ResetCan()
    '
    ' Description: Resets the CAN interface (Hardware-Reset, empty buffer, ...)
    '
    ' Parameters:  bChannel_p      = [IN]  CAN channel (USBCAN_CHANNEL_CH0 or USBCAN_CHANNEL_CH1)
    '              dwFlags_p       = [IN]  flags defines what should be reseted (see USBCAN_RESET_...)
    '
    ' Returns:     result of the function
    '
    '---------------------------------------------------------------------------
    Public Function ResetCan(ByVal pbChannel_p As Byte, Optional ByVal dwFlags_p As Integer = USBCAN_RESET_ALL) As Byte

        Dim bRet As Byte = USBCAN_SUCCESSFUL

        ' call unmanaged function
        bRet = UcanResetCanEx(m_UcanHandle, pbChannel_p, dwFlags_p)
        Return bRet

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    GetHardwareInfo()
    '
    ' Description: Returns the hardware information of an initialized USB-CANmodule
    '
    ' Parameters:  pHwInfo_p       = [OUT] hardware info structure
    '                                      must not be NULL
    '              pCanInfoCh0_p   = [OUT] CAN channel 0 info structure
    '              pCanInfoCh1_p   = [OUT] CAN channel 1 info structure
    '                                      pCanInfoCh0_p and pCanInfoCh1_p must not be NULL
    '
    ' Returns:     result of the function
    '
    '---------------------------------------------------------------------------

    Public Function GetHardwareInfo(ByRef pHwInfo_p As tUcanHardwareInfoEx, _
                                    ByRef pCanInfoCh0_p As tUcanChannelInfo, _
                                    ByRef pCanInfoCh1_p As tUcanChannelInfo) As Byte

        Dim bRet As Byte = USBCAN_SUCCESSFUL

        ' set the structure sizes to the actual sizes of the unmanaged data
        pHwInfo_p.m_dwSize = Marshal.SizeOf(pHwInfo_p)

        pCanInfoCh0_p.m_dwSize = Marshal.SizeOf(pCanInfoCh0_p)
        pCanInfoCh1_p.m_dwSize = Marshal.SizeOf(pCanInfoCh1_p)

        ' call unmanaged function
        bRet = UcanGetHardwareInfoEx2(m_UcanHandle, pHwInfo_p, pCanInfoCh0_p, pCanInfoCh1_p)
        Return bRet

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    GetUserDllVersion()
    '
    ' Description: returns software version of USBCAN32.DLL
    '
    ' Parameters:  none
    '
    ' Returns:     software version
    '                  format: Bit 0-7:    Version
    '                          Bit 8-15:   Revision
    '                          Bit 16-31:  Release
    '
    '---------------------------------------------------------------------------

    Public Function GetUserDllVersion() As Integer

        ' call unmanaged function
        Return UcanGetVersionEx(tUcanVersionType.kVerTypeUserDll)

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    GetFwVersion()
    '
    ' Description: returns version of the firmware within USB-CANmodul
    '
    ' Parameters:  none
    '
    ' Return:      Integer = version in extended format (see GetUserDllVersion)
    '
    '---------------------------------------------------------------------------

    Public Function GetFwVersion() As Integer

        ' call unmanaged function
        Return UcanGetFwVersion(m_UcanHandle)

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    DefineCyclicCanMsg()
    '
    ' Description: defines a list of CAN messages for automatic transmission
    ' NOTE:        when this function is called an older list will be deleted
    '
    ' Parameters:  bChannel_p      = [IN]  CAN channel (USBCAN_CHANNEL_CH0 or USBCAN_CHANNEL_CH1)
    '              pCanMsgList_p   = [IN]  pointer to the CAN message list (must be an array)
    '                                          if Nothing an older list will only be deleted
    '
    ' Return:      result of the function
    '
    '---------------------------------------------------------------------------

    Public Function DefineCyclicCanMsg(ByVal bChannel_p As Byte, ByRef pCanMsgList_p() As tCanMsgStruct) As Byte

        Dim bRet As Byte = USBCAN_SUCCESSFUL
        Dim dwCount As Integer = 0

        If Not (pCanMsgList_p Is Nothing) Then
            ' set message counter to actual array length
            dwCount = pCanMsgList_p.Length
        End If

        ' call unmanaged function
        bRet = UcanDefineCyclicCanMsg(Me.m_UcanHandle, bChannel_p, pCanMsgList_p, dwCount)
        Return bRet

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    ReadCyclicCanMsg()
    '
    ' Description: reads the list of CAN messages for automatically sending back
    '
    ' Parameters:  bChannel_p      = [IN]  CAN channel (USBCAN_CHANNEL_CH0 or USBCAN_CHANNEL_CH1)
    '              pCanMsgList_p   = [OUT] pointer to receive the CAN message list (must be an array)
    '              pdwCount_p      = [OUT] pointer to a 32 bit variables for receiving the number of
    '                                      CAN messages within the list
    '
    ' Return:      result of the function
    '
    '---------------------------------------------------------------------------

    Public Function ReadCyclicCanMsg(ByVal bChannel_p As Byte, ByRef pCanMsgList_p() As tCanMsgStruct, Optional ByRef dwCount_p As Integer = 0) As Byte

        Dim bRet As Byte = USBCAN_SUCCESSFUL

        ' set message counter to actual array length
        dwCount_p = pCanMsgList_p.Length

        ' call unmanaged function
        bRet = UcanReadCyclicCanMsg(Me.m_UcanHandle, bChannel_p, pCanMsgList_p, dwCount_p)
        'Debug.WriteLine("ReadCyclicCanMsg: count=" + dwCount_p.ToString())
        Return bRet

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    EnableCyclicCanMsg()
    '
    ' Description: enables or disables the automatically sending
    '
    ' Parameters:  bChannel_p      = [IN]  CAN channel (USBCAN_CHANNEL_CH0 or USBCAN_CHANNEL_CH1)
    '              dwFlags_p       = [IN]  this flags specifies which CAN messages should be
    '                                      activated, specifies the processing mode oth the list
    '                                      (sequential or parallel), enables or disables the TxEcho
    '                                      for this CAN messages
    '                                      (see constants USBCAN_CYCLIC_FLAG_...)
    '
    ' Return:      result of the function
    '
    '---------------------------------------------------------------------------

    Public Function EnableCyclicCanMsg(ByVal bChannel_p As Byte, ByVal dwFlags_p As Integer) As Byte

        ' call unmanaged function
        Return UcanEnableCyclicCanMsg(Me.m_UcanHandle, bChannel_p, dwFlags_p)

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    GetMsgPending()
    '
    ' Description: returns the number of pending CAN messages
    '
    ' Parameters:  bChannel_p          = [IN]  CAN channel (USBCAN_CHANNEL_CH0, USBCAN_CHANNEL_CH1 or USBCAN_CHANNEL_ANY)
    '                                          If USBCAN_CHANNEL_ANY is set then the number of borth channels will be
    '                                          added as long as they are initialized.
    '              dwFlags_p           = [IN]  this flags specifies which buffers shoulb be checked
    '                                          (see constants USBCAN_PENDING_FLAG_...)
    '              pdwPendingCount_p   = [OUT] pointer to a 32 bit variable which receives the number of pending messages
    '
    ' Return:      result of the function
    '
    '---------------------------------------------------------------------------

    Public Function GetMsgPending(ByVal bChannel_p As Byte, ByVal dwFlags_p As Integer, ByRef pdwPendingCount_p As Integer) As Byte

        ' call unmanaged function
        Return UcanGetMsgPending(Me.m_UcanHandle, bChannel_p, dwFlags_p, pdwPendingCount_p)

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    UcanGetCanErrorCounter()
    '
    ' Description: reads the current value of the error counters within the CAN controller
    '
    '              Only available for Multiport CAN-to-USB, USB-CANmodulX
    '              (NOT for GW-001 and GW-002 !!!).
    '
    ' Parameters:  bChannel_p          = [IN]  CAN channel (USBCAN_CHANNEL_CH0 or USBCAN_CHANNEL_CH1)
    '              pdwTxErrorCounter_p = [OUT] pointer to a 32 bit variable which receives the TX error counter
    '              pdwRxErrorCounter_p = [OUT] pointer to a 32 bit variable which receives the RX error counter
    '
    ' Return:      result of the function
    '
    '---------------------------------------------------------------------------

    Public Function GetCanErrorCounter(ByVal bChannel_p As Byte, ByRef pdwTxErrorCounter_p As Integer, ByRef pdwRxErrorCounter_p As Integer) As Byte

        ' call unmanaged function
        Return UcanGetCanErrorCounter(Me.m_UcanHandle, bChannel_p, pdwTxErrorCounter_p, pdwRxErrorCounter_p)

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    UcanSetTxTimeout()
    '
    ' Description: sets the transmission timeout
    '
    ' Note: When a transmission timeout is set the firmware tries to send
    ' a message within this timeout. If it could not be sent the firmware sets
    ' the "auto delete" state. Within this state all transmit CAN messages for
    ' this channel will be deleted automatically for not blocking the other
    ' channel. When firmware does delete a transmit CAN message then a new
    ' error status will be set: USBCAN_CANERR_TXMSGLOST (red LED is blinking).
    '
    ' This function can also be used for USB-CANmodul2, 8 or 16 (multiport).
    '
    ' Parameters:  bChannel_p      = [IN]  CAN channel (USBCAN_CHANNEL_CH0 or USBCAN_CHANNEL_CH1)
    '              dwTxTimeout_p   = [IN]  transmit timeout in milleseconds
    '                                      (value 0 swithes off the "auto delete" featuere = default setting)
    '
    ' Return:      result of the function
    '
    '---------------------------------------------------------------------------

    Public Function SetTxTimeout(ByVal bChannel_p As Byte, ByVal dwTxTimeout_p As Integer) As Byte

        ' call unmanaged function
        Return UcanSetTxTimeout(Me.m_UcanHandle, bChannel_p, dwTxTimeout_p)

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    Shutdown()
    '
    ' Description: shuts down all CAN interfaces and the hardware
    '
    ' Parameters:  none
    '
    ' Returns:     result of the function
    '
    '---------------------------------------------------------------------------

    Public Function Shutdown(Optional ByVal dwChannel_p As Integer = USBCAN_CHANNEL_ALL, _
        Optional ByVal fShutDownHardware_p As Boolean = True) As Byte

        Dim bRet As Byte = 0

        SyncLock (Me)
            Debug.WriteLine("Shutdown")

            ' shutdown each channel if it's initialized
            If (m_fCh0IsInitialized = True) AndAlso _
                    ((dwChannel_p = USBCAN_CHANNEL_ALL) OrElse (dwChannel_p = USBCAN_CHANNEL_CH0) OrElse (fShutDownHardware_p = True)) Then
                bRet += UcanDeinitCanEx(m_UcanHandle, USBCAN_CHANNEL_CH0)
                m_fCh0IsInitialized = False
            End If

            If (m_fCh1IsInitialized = True) AndAlso _
                    ((dwChannel_p = USBCAN_CHANNEL_ALL) OrElse (dwChannel_p = USBCAN_CHANNEL_CH1) OrElse (fShutDownHardware_p = True)) Then
                bRet += UcanDeinitCanEx(m_UcanHandle, USBCAN_CHANNEL_CH1)
                m_fCh1IsInitialized = False
            End If

            If (m_fHwIsInitialized = True) AndAlso (fShutDownHardware_p = True) Then

                ' sleep 200ms to prevent calling ReadCanMsg() by a pending callback handler
                Thread.Sleep(200)

                ' shutdown hardware
                Try
                    bRet += UcanDeinitHardware(m_UcanHandle)
                    ' sometimes the following exception occures:
                    'Eine nicht behandelte Ausnahme des Typs 'System.NullReferenceException' ist in UcanDotNET.dll aufgetreten.
                    'Zusätzliche Informationen: Der Objektverweis wurde nicht auf eine Objektinstanz festgelegt.
                Catch npe As NullReferenceException
                    Debug.WriteLine("catched NullReferenceException while executing UcanDeinithardware")
                End Try

                m_fHwIsInitialized = False
                m_UcanHandle = 0
            End If

        End SyncLock

        Return bRet

    End Function

#End Region

#Region " Properties ###################################################################### "

    '---------------------------------------------------------------------------
    '
    ' Property:    IsHardwareInitialized(), IsCan0Initialized(), IsCan1Initialized()
    '
    ' Description: returnes whether hardware and/or CAN interfaces are initialsized
    '
    ' Return:      True if initialized, otherwise False
    '
    '---------------------------------------------------------------------------

    Public ReadOnly Property IsHardwareInitialized() As Boolean

        Get
            Return Me.m_fHwIsInitialized
        End Get

    End Property

    Public ReadOnly Property IsCan0Initialized() As Boolean

        Get
            Return Me.m_fCh0IsInitialized
        End Get

    End Property

    Public ReadOnly Property IsCan1Initialized() As Boolean

        Get
            Return Me.m_fCh1IsInitialized
        End Get

    End Property

#End Region

#Region " Shared helper methodes ########################################################## "

    '---------------------------------------------------------------------------
    '
    ' Function:    GetCanStatusMessage()
    '
    ' Description: Returns the appropriate message string for a given CAN status value
    '
    ' Parameters:  wCanStatus_p = [IN] CAN status value from GetStatus().m_wCanStatus
    '
    ' Returns:     String containing the appropriate status message string.
    '                  When ther are set more bits than one then the appropiate
    '                  status messages are separated by comma.
    '
    '---------------------------------------------------------------------------

    Public Shared Function GetCanStatusMessage(ByVal wCanStatus_p As Short) As String

        Dim strStatus As String = String.Empty

        If (wCanStatus_p = USBCAN_CANERR_OK) Then
            strStatus = "OK"
        Else
            If ((wCanStatus_p And USBCAN_CANERR_TXMSGLOST) <> 0) Then
                strStatus += "Transmit message lost,"
            End If
            If ((wCanStatus_p And USBCAN_CANERR_MEMTEST) <> 0) Then
                strStatus += "Memory test failed,"
            End If
            If ((wCanStatus_p And USBCAN_CANERR_REGTEST) <> 0) Then
                strStatus += "Register test failed,"
            End If
            If ((wCanStatus_p And USBCAN_CANERR_QXMTFULL) <> 0) Then
                strStatus += "Transmit queue is full,"
            End If
            If ((wCanStatus_p And USBCAN_CANERR_QOVERRUN) <> 0) Then
                strStatus += "Receive queue overrun,"
            End If
            If ((wCanStatus_p And USBCAN_CANERR_QRCVEMPTY) <> 0) Then
                strStatus += "Receive queue is empty,"
            End If
            If ((wCanStatus_p And USBCAN_CANERR_BUSOFF) <> 0) Then
                strStatus += "Bus Off,"
            End If
            If ((wCanStatus_p And USBCAN_CANERR_BUSHEAVY) <> 0) Then
                strStatus += "Error Passive,"
            End If
            If ((wCanStatus_p And USBCAN_CANERR_BUSLIGHT) <> 0) Then
                strStatus += "Warning Limit,"
            End If
            If ((wCanStatus_p And USBCAN_CANERR_OVERRUN) <> 0) Then
                strStatus += "Rx-buffer is full,"
            End If
            If ((wCanStatus_p And USBCAN_CANERR_XMTFULL) <> 0) Then
                strStatus += "Tx-buffer is full,"
            End If
        End If

        Return strStatus.TrimEnd(","c)

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    GetBaudrateMessage()
    '
    ' Description: Returns the appropriate message string for a given baudrate value
    '
    ' Parameters:  bBTR0_p = [IN] Bus Timing Register 0 (SJA1000 - use high byte USBCAN_BAUD_...)
    '              bBTR1_p = [IN] Bus Timing Register 1 (SJA1000 - use low byte USBCAN_BAUD_...)
    '
    ' Returns:     String containing the appropriate message text
    '
    '---------------------------------------------------------------------------

    Public Shared Function GetBaudrateMessage(ByVal bBTR0_p As Byte, ByVal bBTR1_p As Byte) As String

        Dim wBTR0 As Short = bBTR0_p
        Dim wBTR1 As Short = bBTR1_p
        Dim wBTR As Short = ((wBTR0 << 8) + wBTR1)
        Return GetBaudrateMessage(wBTR)

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    GetBaudrateMessage()
    '
    ' Description: Returns the appropriate message string for a given baudrate value,
    '              e.g. the constants USBCAN_BAUD_...
    '
    ' Parameters:  wBTR_p = [IN] 16bit baudrate value
    '
    ' Returns:     String containing the appropriate message text
    '
    '---------------------------------------------------------------------------

    Public Shared Function GetBaudrateMessage(ByVal wBTR_p As Short) As String

        If (wBTR_p = USBCAN_BAUD_AUTO) Then
            Return "auto baudrate"
        ElseIf (wBTR_p = USBCAN_BAUD_10kBit) Then
            Return "10 kBit/s"
        ElseIf (wBTR_p = USBCAN_BAUD_20kBit) Then
            Return "20 kBit/s"
        ElseIf (wBTR_p = USBCAN_BAUD_50kBit) Then
            Return "50 kBit/s"
        ElseIf (wBTR_p = USBCAN_BAUD_100kBit) Then
            Return "100 kBit/s"
        ElseIf (wBTR_p = USBCAN_BAUD_125kBit) Then
            Return "125 kBit/s"
        ElseIf (wBTR_p = USBCAN_BAUD_250kBit) Then
            Return "250 kBit/s"
        ElseIf (wBTR_p = USBCAN_BAUD_500kBit) Then
            Return "500 kBit/s"
        ElseIf (wBTR_p = USBCAN_BAUD_800kBit) Then
            Return "800 kBit/s"
        ElseIf (wBTR_p = USBCAN_BAUD_1MBit) Then
            Return "1 MBit/s"
        ElseIf (wBTR_p = USBCAN_BAUD_USE_BTREX) Then
            Return "BTR Ext is used"
        Else
            Return "BTR is unknown (userspecific)"
        End If

    End Function


    '---------------------------------------------------------------------------
    '
    ' Function:    GetBaudrateExMessage()
    '
    ' Description: Returns the appropriate message string for a given baudrate value,
    '              e.g. the constants USBCAN_BAUDEX_...
    '
    ' Parameters:  dwBTR_p = [IN] 32bit baudrate value
    '
    ' Returns:     String containing the appropriate message text
    '
    '---------------------------------------------------------------------------

    Public Shared Function GetBaudrateExMessage(ByVal dwBTR_p As Integer) As String

        If (dwBTR_p = USBCAN_BAUDEX_AUTO) Then
            Return "auto baudrate"
        ElseIf (dwBTR_p = USBCAN_BAUDEX_10kBit) Then
            Return "10 kBit/s"
        ElseIf (dwBTR_p = USBCAN_BAUDEX_SP2_10kBit) Then
            Return "10 kBit/s"
        ElseIf (dwBTR_p = USBCAN_BAUDEX_20kBit) Then
            Return "20 kBit/s"
        ElseIf (dwBTR_p = USBCAN_BAUDEX_SP2_20kBit) Then
            Return "20 kBit/s"
        ElseIf (dwBTR_p = USBCAN_BAUDEX_50kBit) Then
            Return "50 kBit/s"
        ElseIf (dwBTR_p = USBCAN_BAUDEX_SP2_50kBit) Then
            Return "50 kBit/s"
        ElseIf (dwBTR_p = USBCAN_BAUDEX_100kBit) Then
            Return "100 kBit/s"
        ElseIf (dwBTR_p = USBCAN_BAUDEX_SP2_100kBit) Then
            Return "100 kBit/s"
        ElseIf (dwBTR_p = USBCAN_BAUDEX_125kBit) Then
            Return "125 kBit/s"
        ElseIf (dwBTR_p = USBCAN_BAUDEX_SP2_125kBit) Then
            Return "125 kBit/s"
        ElseIf (dwBTR_p = USBCAN_BAUDEX_250kBit) Then
            Return "250 kBit/s"
        ElseIf (dwBTR_p = USBCAN_BAUDEX_SP2_250kBit) Then
            Return "250 kBit/s"
        ElseIf (dwBTR_p = USBCAN_BAUDEX_500kBit) Then
            Return "500 kBit/s"
        ElseIf (dwBTR_p = USBCAN_BAUDEX_SP2_500kBit) Then
            Return "500 kBit/s"
        ElseIf (dwBTR_p = USBCAN_BAUDEX_800kBit) Then
            Return "800 kBit/s"
        ElseIf (dwBTR_p = USBCAN_BAUDEX_SP2_800kBit) Then
            Return "800 kBit/s"
        ElseIf (dwBTR_p = USBCAN_BAUDEX_1MBit) Then
            Return "1000 kBit/s"
        ElseIf (dwBTR_p = USBCAN_BAUDEX_SP2_1MBit) Then
            Return "1000 kBit/s"
        ElseIf (dwBTR_p = USBCAN_BAUDEX_USE_BTR01) Then
            Return "BTR0/BTR1 is used"
        Else
            Return "BTR is unknown (userspecific)"
        End If

    End Function


    '---------------------------------------------------------------------------
    '
    ' Macro:       ConvertToMajorVer(), ConvertToMinorVer(), ConvertToReleaseVer()
    '
    ' Description: macros to convert the version iformation
    '
    ' Parameters:  ver     = [IN] extended version information as unsigned long (32 bit)
    '
    ' Return:      major, minor or release version
    '
    '---------------------------------------------------------------------------

    Public Shared Function ConvertToMajorVer(ByVal dwVersion_p As Integer) As Integer

        Return (dwVersion_p And &HFF)

    End Function

    Public Shared Function ConvertToMinorVer(ByVal dwVersion_p As Integer) As Integer

        Return (dwVersion_p And &HFF00) >> 8

    End Function

    Public Shared Function ConvertToReleaseVer(ByVal dwVersion_p As Integer) As Integer

        Return (dwVersion_p And &HFFFF0000) >> 16

    End Function


    ' checks if the version is equal or higher than a specified value
    Protected Shared Function CheckVersionIsEqualOrHigher(ByVal dwVersion_p As Integer, ByVal dwCmpMajor_p As Integer, ByVal dwCmpMinor_p As Integer) As Boolean

        If ((ConvertToMajorVer(dwVersion_p) > dwCmpMajor_p) OrElse _
            ((ConvertToMajorVer(dwVersion_p) = dwCmpMajor_p) AndAlso ((ConvertToMinorVer(dwVersion_p) >= dwCmpMinor_p)))) Then
            Return True
        Else
            Return False
        End If

    End Function


    '---------------------------------------------------------------------------
    '
    ' Macro:       CheckIs_sysWORXX()
    '              CheckSupportCyclicMsg()
    '              CheckSupportTwoChannel()
    '              CheckSupportTermResistor()
    '              CheckSupportUserPort()
    '              CheckSupportRbUserPort()
    '              CheckSupportRbCanPort()
    '              CheckSupportUcannet()
    '
    ' Description: macros to check if an USB-CANmodul does support different features
    '
    ' Parameters:  pHwInfoEx   = [IN] pointer to the extended hardware information structure
    '                                  (returned by UcanGetHardwareInfoEx2()
    '
    ' Return:      BOOL        = TRUE if hardware does support the feature
    '
    '---------------------------------------------------------------------------

    ' checks if the module is a sysWORXX USB-CANmodul
    ' comparable with macro USBCAN_CHECK_IS_SYSWORXX() in USBCAN32.H
    Public Shared Function CheckIs_sysWORXX(ByVal HwInfoEx_p As tUcanHardwareInfoEx) As Boolean

        If ((HwInfoEx_p.m_dwProductCode And USBCAN_PRODCODE_MASK_PID) >= USBCAN_PRODCODE_PID_MULTIPORT) Then
            Return True
        Else
            Return False
        End If

    End Function

    ' checks if the module supports automatically transmission of cyclic CAN messages
    ' comparable with macro USBCAN_CHECK_SUPPORT_CYCLIC_MSG() in USBCAN32.H
    Public Shared Function CheckSupportCyclicMsg(ByVal HwInfoEx_p As tUcanHardwareInfoEx) As Boolean

        If ((CheckIs_sysWORXX(HwInfoEx_p) = True) AndAlso _
            (CheckVersionIsEqualOrHigher(HwInfoEx_p.m_dwFwVersionEx, 3, 6) = True)) Then
            Return True
        Else
            Return False
        End If

    End Function

    ' checks if the module supports two CAN channels (at logical device)
    ' comparable with macro USBCAN_CHECK_SUPPORT_TWO_CHANNEL() in USBCAN32.H
    Public Shared Function CheckSupportTwoChannel(ByVal HwInfoEx_p As tUcanHardwareInfoEx) As Boolean

        If ((CheckIs_sysWORXX(HwInfoEx_p) = True) AndAlso _
            ((HwInfoEx_p.m_dwProductCode And USBCAN_PRODCODE_PID_TWO_CHA) <> 0)) Then
            Return True
        Else
            Return False
        End If

    End Function

    ' checks if the module supports a termination resistor
    ' comparable with macro USBCAN_CHECK_SUPPORT_TERM_RESISTOR() in USBCAN32.H
    Public Shared Function CheckSupportTermResistor(ByVal HwInfoEx_p As tUcanHardwareInfoEx) As Boolean

        If ((HwInfoEx_p.m_dwProductCode And USBCAN_PRODCODE_PID_TERM) <> 0) Then
            Return True
        Else
            Return False
        End If

    End Function

    ' checks if the module supports a user I/O port
    ' comparable with macro USBCAN_CHECK_SUPPORT_USER_PORT() in USBCAN32.H
    Public Shared Function CheckSupportUserPort(ByVal HwInfoEx_p As tUcanHardwareInfoEx) As Boolean

        If (((HwInfoEx_p.m_dwProductCode And USBCAN_PRODCODE_MASK_PID) <> USBCAN_PRODCODE_PID_BASIC) AndAlso _
            ((HwInfoEx_p.m_dwProductCode And USBCAN_PRODCODE_MASK_PID) <> USBCAN_PRODCODE_PID_RESERVED1) AndAlso _
            (CheckVersionIsEqualOrHigher(HwInfoEx_p.m_dwFwVersionEx, 2, 16) = True)) Then
            Return True
        Else
            Return False
        End If

    End Function

    ' checks if the module supports a user I/O port including read back feature
    ' comparable with macro USBCAN_CHECK_SUPPORT_RBUSER_PORT() in USBCAN32.H
    Public Shared Function CheckSupportRbUserPort(ByVal HwInfoEx_p As tUcanHardwareInfoEx) As Boolean

        If ((HwInfoEx_p.m_dwProductCode And USBCAN_PRODCODE_PID_RBUSER) <> 0) Then
            Return True
        Else
            Return False
        End If

    End Function

    ' checks if the module supports a CAN I/O port including read back feature
    ' comparable with macro USBCAN_CHECK_SUPPORT_RBCAN_PORT() in USBCAN32.H
    Public Shared Function CheckSupportRbCanPort(ByVal HwInfoEx_p As tUcanHardwareInfoEx) As Boolean

        If ((HwInfoEx_p.m_dwProductCode And USBCAN_PRODCODE_PID_RBCAN) <> 0) Then
            Return True
        Else
            Return False
        End If

    End Function

    ' checks if the module supports the usage of USB-CANnetwork driver
    ' comparable with macro USBCAN_CHECK_SUPPORT_UCANNET() in USBCAN32.H
    Public Shared Function CheckSupportUcannet(ByVal HwInfoEx_p As tUcanHardwareInfoEx) As Boolean

        If ((CheckIs_sysWORXX(HwInfoEx_p) = True) AndAlso _
            (CheckVersionIsEqualOrHigher(HwInfoEx_p.m_dwFwVersionEx, 3, 8) = True)) Then
            Return True
        Else
            Return False
        End If

    End Function

#End Region

#Region " Private or protected methodes ################################################### "

    '---------------------------------------------------------------------------
    '
    ' Subprocedure: UcanConnectControl()
    '
    ' Description: is the actual callback function for UcanInitHwConnectControlEx()
    '
    ' Parameters:  bEvent_p    = [IN]  event
    '                  USBCAN_EVENT_CONNECT
    '                  USBCAN_EVENT_DISCONNECT
    '                  USBCAN_EVENT_FATALDISCON
    '              dwParam_p   = [IN]  additional parameter (depends on bEvent_p)
    '                  USBCAN_EVENT_CONNECT:       always 0
    '                  USBCAN_EVENT_DISCONNECT.    always 0
    '                  USBCAN_EVENT_FATALDISCON:   USB-CAN-Handle of the disconnected module
    '              pArg_p      = [IN]  additional parameter
    '                                  Parameter which was defined with UcanInitHardwareEx()
    '                                  not used in this wrapper class
    '
    ' Returns:     none
    '
    '---------------------------------------------------------------------------

    Protected Shared Sub UcanConnectControl(ByVal dwEvent_p As Integer, _
        ByVal dwParam_p As Integer, ByVal pArg_p As IntPtr)

        ' sometimes the console output crashes because the Textwriter object is already disposed
        'Debug.WriteLine("Event: " & dwEvent_p & ", Param: " & dwParam_p)

        ' do for each event type the appropriate action,
        ' e.g. raise the appropriate .NET event
        If (dwEvent_p = USBCAN_EVENT_FATALDISCON) Then

            ' Search for equivalent object in colUcanHandle_l and call object's handle method
            For Each obj As USBcanServer In colUcanHandle_l
                If (obj.m_UcanHandle = dwParam_p) Then
                    obj.HandleFatalDisconnect(dwParam_p, pArg_p)
                    Exit For
                End If
            Next

        ElseIf (dwEvent_p = USBCAN_EVENT_CONNECT) Then
            RaiseEvent ConnectEvent()

        ElseIf (dwEvent_p = USBCAN_EVENT_DISCONNECT) Then
            RaiseEvent DisconnectEvent()

        End If

    End Sub


    '---------------------------------------------------------------------------
    '
    ' Subprocedure: HandleUcanCallback()
    '
    ' Description: Is called from the USBCAN32.DLL if a working event occured.
    '
    ' Parameters:  UcanHandle_p    = [IN]  USB-CAN-Handle
    '                                      Handle, which is returned by the function UcanInitHardware()
    '              bEvent_p        = [IN]  event type
    '                  USBCAN_EVENT_INITHW
    '                  USBCAN_EVENT_INITCAN
    '                  USBCAN_EVENT_RECIEVE
    '                  USBCAN_EVENT_STATUS
    '                  USBCAN_EVENT_DEINITCAN
    '                  USBCAN_EVENT_DEINITHW
    '
    '              bChannel_p      = [IN]  CAN channel (USBCAN_CHANNEL_CH0 or USBCAN_CHANNEL_CH1, USBCAN_CHANNEL_ANY)
    '              pArg_p          = [IN]  additional parameter
    '                                      Parameter which was defined with UcanInitHardwareEx()
    '                                      not used in this wrapper class
    '
    ' Returns:     none
    '
    '---------------------------------------------------------------------------

    Protected Sub HandleUcanCallback(ByVal UcanHandle_p As Byte, _
        ByVal dwEvent_p As Integer, ByVal bChannel_p As Byte, ByVal pArg_p As IntPtr)

        'Debug.WriteLine("im Server: Handle: " & UcanHandle_p & ", Event: " & dwEvent_p & ", Channel: " & bChannel_p)

        ' do for each event type the appropriate action,
        ' e.g. raise the appropriate .NET event
        If (dwEvent_p = USBCAN_EVENT_INITHW) Then
            RaiseEvent InitHwEvent()

        ElseIf (dwEvent_p = USBCAN_EVENT_INITCAN) Then
            RaiseEvent InitCanEvent(bChannel_p)

        ElseIf (dwEvent_p = USBCAN_EVENT_RECEIVE) Then
            RaiseEvent CanMsgReceivedEvent(bChannel_p)

        ElseIf (dwEvent_p = USBCAN_EVENT_STATUS) Then
            RaiseEvent StatusEvent(bChannel_p)

        ElseIf (dwEvent_p = USBCAN_EVENT_DEINITCAN) Then
            RaiseEvent DeinitCanEvent(bChannel_p)

        ElseIf (dwEvent_p = USBCAN_EVENT_DEINITHW) Then

            ' raise event before or after clean up?
            m_gchUcanCallback.Free()

            Dim i As Integer

            ' delete this object instance from static collection
            SyncLock (colUcanHandle_l)
                For i = 1 To colUcanHandle_l.Count
                    If (CType(colUcanHandle_l.Item(i), USBcanServer).m_UcanHandle = UcanHandle_p) Then
                        colUcanHandle_l.Remove(i)
                        Exit For
                    End If
                Next
            End SyncLock

            RaiseEvent DeinitHwEvent()

        End If

    End Sub


    '---------------------------------------------------------------------------
    '
    ' Subprocedure: HandleFatalDisconnect()
    '
    ' Description: is called from UcanConnectControl() and cleans up internal structures
    '              and raises the member event FatalDisconnectEvent()
    '
    ' Parameters:  dwParam_p   = [IN]  additional parameter (depends on bEvent_p)
    '                  USBCAN_EVENT_FATALDISCON:   USB-CAN-Handle of the disconnected module
    '              pArg_p      = [IN]  additional parameter
    '                                  Parameter which was defined with UcanInitHardwareEx()
    '                                  not used in this wrapper class
    '
    ' Returns:     none
    '
    '---------------------------------------------------------------------------

    Protected Sub HandleFatalDisconnect(ByVal dwParam_p As Integer, ByVal pArg_p As IntPtr)

        m_gchUcanCallback.Free()

        ' reset the initialization states
        m_fHwIsInitialized = False
        m_fCh0IsInitialized = False
        m_fCh1IsInitialized = False
        m_fIsInitialized = False

        Dim i As Integer

        ' delete this object instance from static collection
        SyncLock colUcanHandle_l
            For i = 1 To colUcanHandle_l.Count
                'If CType(colUcanHandle_l.Item(i), USBcanServer).m_UcanHandle = dwParam_p Then
                If (colUcanHandle_l.Item(i) Is Me) Then
                    Debug.WriteLine("FatalDisconnect: object removed")
                    colUcanHandle_l.Remove(i)
                    Exit For
                End If
            Next
        End SyncLock

        m_UcanHandle = 0
        RaiseEvent FatalDisconnectEvent()

    End Sub

#End Region

#Region " Disposing and finalizing ######################################################## "

    ' Implement IDisposable.
    ' Do not make this method Overridable.
    ' A derived class should not be able to override this method.
    Public Overloads Sub Dispose() Implements IDisposable.Dispose

        Dispose(True)

        ' Take yourself off of the finalization queue
        ' to prevent finalization code for this object
        ' from executing a second time.
        GC.SuppressFinalize(Me)

    End Sub

    ' Dispose(disposing As Boolean) executes in two distinct scenarios.
    ' If disposing is true, the method has been called directly 
    ' or indirectly by a user's code. Managed and unmanaged resources 
    ' can be disposed.
    ' If disposing equals false, the method has been called by the runtime
    ' from inside the finalizer and you should not reference other    
    ' objects. Only unmanaged resources can be disposed.
    Protected Overridable Overloads Sub Dispose(ByVal disposing As Boolean)

        ' Check to see if Dispose has already been called.
        If Not (Me.disposed) Then

            ' If disposing equals true, dispose all managed 
            ' and unmanaged resources.
            If (disposing) Then
                ' Dispose managed resources.
            End If

            ' Release unmanaged resources. If disposing is false,
            ' only the following code is executed.

            ' if last active instance deregister ConnectControl callback function
            dwPlugAndPlayCount_l -= 1
            If (dwPlugAndPlayCount_l = 0) Then
                UcanDeinitHwConnectControl()
                Try
                    gchUcanConnectControl_l.Free()
                Catch ex As Exception
                    Debug.Assert(False, "Could not free USB-CAN server.  This is safe to ignore.")
                End Try
            End If

            SyncLock (Me)

                ' shutdown each channel if it's initialized
                If (m_fCh0IsInitialized = True) Then
                    UcanDeinitCanEx(m_UcanHandle, USBCAN_CHANNEL_CH0)
                    m_fCh0IsInitialized = False
                End If

                If (m_fCh1IsInitialized = True) Then
                    UcanDeinitCanEx(m_UcanHandle, USBCAN_CHANNEL_CH1)
                    m_fCh1IsInitialized = False
                End If

                If (m_fHwIsInitialized = True) Then

                    ' shutdown hardware
                    Try
                        UcanDeinitHardware(m_UcanHandle)
                        'Eine nicht behandelte Ausnahme des Typs 'System.NullReferenceException' ist in UcanDotNET.dll aufgetreten.
                        'Zusätzliche Informationen: Der Objektverweis wurde nicht auf eine Objektinstanz festgelegt.
                    Catch npe As NullReferenceException
                        Debug.WriteLine("catched NullReferenceException while executing UcanDeinithardware")
                    End Try

                    m_fHwIsInitialized = False
                    m_UcanHandle = 0

                    Dim i As Integer

                    ' test if this object instance is already deleted from the static collection
                    For i = 1 To colUcanHandle_l.Count
                        If (colUcanHandle_l.Item(i) Is Me) Then
                            colUcanHandle_l.Remove(i)
                            Exit For
                        End If
                    Next
                End If

                ' Note that this is not thread safe.
                ' Another thread could start disposing the object
                ' after the managed resources are disposed,
                ' but before the disposed flag is set to true.
                ' If thread safety is necessary, it must be
                ' implemented by the client.
            End SyncLock

        End If

        Me.disposed = True

    End Sub

    ' This Finalize method will run only if the 
    ' Dispose method does not get called.
    ' By default, methods are NotOverridable. 
    ' This prevents a derived class from overriding this method.
    Protected Overrides Sub Finalize()

        ' Do not re-create Dispose clean-up code here.
        ' Calling Dispose(false) is optimal in terms of
        ' readability and maintainability.
        Dispose(False)

        MyBase.Finalize()

    End Sub

#End Region

End Class
