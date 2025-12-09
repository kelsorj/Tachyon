using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace DS1M12
{
    /// <summary>
    /// this class wraps the DS1M12 C++ library for the Logger features
    /// </summary>
    public class Logger
    {
        public const UInt16 CHANNEL_A = 1;
        public const UInt16 CHANNEL_B = 2;

        public const UInt16 GAIN_100UV_PER_BIT     = 1;
        public const UInt16 GAIN_200UV_PER_BIT     = 2;
        public const UInt16 GAIN_500UV_PER_BIT     = 3;
        public const UInt16 GAIN_1MV_PER_BIT       = 4;
        public const UInt16 GAIN_2MV_PER_BIT       = 5;
        public const UInt16 GAIN_5MV_PER_BIT       = 6;
        public const UInt16 GAIN_10MV_PER_BIT      = 7;

        public const UInt16 WAVEFORM_DC           = 1;
        public const UInt16 WAVEFORM_SQUARE       = 2;
        public const UInt16 WAVEFORM_SAWTOOTH     = 3;
        public const UInt16 WAVEFORM_SIN_COS      = 4;
        public const UInt16 WAVEFORM_TRIANGULAR   = 5;
        public const UInt16 WAVEFORM_PULSE        = 6;
        public const UInt16 WAVEFORM_CUSTOM       = 7;

        public const UInt16 DDL_SUCCESS                 = 0; // FT_OK
        public const UInt16 DDL_INVALID_HANDLE          = 1; // FT_INVALID_HANDLE
        public const UInt16 DDL_DEVICE_NOT_FOUND        = 2; //FT_DEVICE_NOT_FOUND
        public const UInt16 DDL_DEVICE_NOT_OPENED       = 3; //FT_DEVICE_NOT_OPENED
        public const UInt16 DDL_IO_ERROR                = 4; //FT_IO_ERROR
        public const UInt16 DDL_INSUFFICIENT_RESOURCES  = 5; // FT_INSUFFICIENT_RESOURCES

        public const UInt16 DDL_FAILED_TO_COMPLETE_COMMAND          = 20;    // cannot change, error code mapped from Scopes and MorphIC classes
        public const UInt16 DDL_FAILED_TO_SYNCHRONIZE_DEVICE        = 21;  // cannot change, error code mapped from Scopes and MorphIC classes
        public const UInt16 DDL_FPGA_FILE_NOT_FOUND                 = 22;           // cannot change, error code mapped from MorphIC class
        public const UInt16 DDL_INVALID_FPGA_FILE_NAME              = 23;        // cannot change, error code mapped from MorphIC class
        public const UInt16 DDL_INVALID_FPGA_FILE_EXTENSION         = 24;   // cannot change, error code mapped from MorphIC class
        public const UInt16 DDL_FAILED_TO_OPEN_FPGA_FILE            = 25;      // cannot change, error code mapped from MorphIC class
        public const UInt16 DDL_FPGA_FILE_READ_ERROR                = 26;          // cannot change, error code mapped from MorphIC class
        public const UInt16 DDL_FPGA_FILE_CORRUPT                   = 27;             // cannot change, error code mapped from MorphIC class
        public const UInt16 DDL_FAILED_TO_CLEAR_DEVICE              = 28;        // cannot change, error code mapped from MorphIC class
        public const UInt16 DDL_FAILED_TO_PROGRAM_DEVICE            = 29;      // cannot change, error code mapped from MorphIC class
        public const UInt16 DDL_DEVICE_PROGRAM_CHANNEL_IN_USE       = 30; // cannot change, error code mapped from MorphIC class
        public const UInt16 DDL_CALIB_DATA_READ_FAILURE             = 31;
        public const UInt16 DDL_TOO_MANY_DEVICES                    = 32;
        public const UInt16 DDL_INVALID_SERIAL_NUMBER_INDEX         = 33;
        public const UInt16 DDL_NULL_SERIAL_NUMBER_BUFFER_POINTER   = 34;
        public const UInt16 DDL_SERIAL_NUMBER_BUFFER_TOO_SMALL      = 35;
        public const UInt16 DDL_INVALID_SERIAL_NUMBER               = 36;
        public const UInt16 DDL_DEVICE_IN_USE                       = 37;
        public const UInt16 DDL_DEVICE_IN_USE_DEVICE_NOT_FOUND      = 38;
        public const UInt16 DDL_FAILED_TO_SYNCHRONIZE_DEVICE_MPSSE  = 39;
        public const UInt16 DDL_INVALID_CHANNEL                     = 40;
        public const UInt16 DDL_INVALID_GAIN                        = 41;
        public const UInt16 DDL_INVALID_SAMPLE_RATE                 = 42;
        public const UInt16 DDL_NULL_CHANNEL_CONTROL_DATA_POINTER   = 43;
        public const UInt16 DDL_NULL_CHANNEL_DATA_BUFFER_POINTER    = 44;
        public const UInt16 DDL_CHANNEL_A_FUSE_FAILED               = 45;
        public const UInt16 DDL_CHANNEL_B_FUSE_FAILED               = 46;
        public const UInt16 DDL_INVALID_WAVEFORM                    = 47;
        public const UInt16 DDL_INVALID_FREQUENCY_VALUE             = 48;
        public const UInt16 DDL_INVALID_PEAK_TO_PEAK_VOLTAGE_LEVEL  = 49;
        public const UInt16 DDL_INVALID_DC_VOLTAGE_LEVEL            = 50;
        public const UInt16 DDL_INVALID_OFFSET_VOLTAGE_LEVEL        = 51;
        public const UInt16 DDL_INVALID_NUM_SAMPLES                 = 52;
        public const UInt16 DDL_INVALID_PULSE_DUTY_RATIO            = 53;
        public const UInt16 DDL_INVALID_CSV_FILE_NAME               = 54;
        public const UInt16 DDL_INVALID_CSV_FILE_EXTENSION          = 55;
        public const UInt16 DDL_CSV_FILE_NOT_FOUND                  = 56;
        public const UInt16 DDL_CSV_FILE_CORRUPT                    = 57;
        public const UInt16 DDL_FAILED_TO_OPEN_CSV_FILE             = 58;
        public const UInt16 DDL_CSV_FILE_READ_ERROR                 = 59;
        public const UInt16 DDL_FUNCTION_GENERATOR_FUSE_FAILED      = 60;
        public const UInt16 DDL_NULL_DLL_VERSION_BUFFER_POINTER     = 61;
        public const UInt16 DDL_DLL_VERSION_BUFFER_TOO_SMALL        = 62;
        public const UInt16 DDL_NULL_LANGUAGE_CODE_BUFFER_POINTER   = 63;
        public const UInt16 DDL_NULL_ERROR_MESSAGE_BUFFER_POINTER   = 64;
        public const UInt16 DDL_ERROR_MESSAGE_BUFFER_TOO_SMALL      = 65;
        public const UInt16 DDL_INVALID_LANGUAGE_CODE               = 66;
        public const UInt16 DDL_INVALID_STATUS_CODE                 = 67;

        public const UInt16 NUM_DATA_LOG_ELEMENTS       = 4;
        public const UInt16 DATA_VALUE_INDEX            = 0;
        public const UInt16 MISSING_VALUE_INDEX         = 1;
        public const UInt16 MAX_VALUE_CLIPPED_INDEX     = 2;
        public const UInt16 MIN_VALUE_CLIPPED_INDEX     = 3;

        [StructLayout(LayoutKind.Sequential)]
        public class Ddl_Channel_Control_Data {
            public Boolean bGainTimesTen;
            public UInt32 dwGainuVBit;
            public Boolean bCouplingState;
            public Boolean bActiveState;
        }

        [DllImport("DDL1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DDL1M12_GetNumDevices( out UInt32 lpdwNumDevices);

        [DllImport("DDL1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DDL1M12_GetDeviceSerialNumber( UInt32 dwSerialNumberIndex, [MarshalAs(UnmanagedType.LPStr)] StringBuilder lpSerialNumberBuffer, UInt32 dwBufferSize);

        [DllImport("DDL1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DDL1M12_OpenSpecifiedDevice( String lpSerialNumber, out UInt32 pddlHandle);

        [DllImport("DDL1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DDL1M12_OpenDevice( ref UInt32 pddlHandle);

        [DllImport("DDL1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DDL1M12_CloseDevice( UInt32 ddlHandle);

        [DllImport("DDL1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DDL1M12_InitDevice( UInt32 ddlHandle);

        [DllImport("DDL1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DDL1M12_IsDeviceProgrammed( UInt32 ddlHandle, out Boolean lpbProgramState);

        [DllImport("DDL1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DDL1M12_ProgramDevice( UInt32 ddlHandle, String lpFileName);

        [DllImport("DDL1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DDL1M12_SetDeviceChannelState( UInt32 ddlHandle, Boolean bChannelAActiveState, Boolean bChannelBActiveState);

        [DllImport("DDL1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DDL1M12_StartDeviceDataLogging( UInt32 ddlHandle, Boolean bTestMode, UInt32 dwSampleIntervaluSecs,
                                                                    [In, MarshalAs(UnmanagedType.LPStruct)] Ddl_Channel_Control_Data pChannelAControlData,
                                                                    [In, MarshalAs(UnmanagedType.LPStruct)] Ddl_Channel_Control_Data pChannelBControlData);

        [DllImport("DDL1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DDL1M12_StopDeviceDataLogging( UInt32 ddlHandle, UInt32 dwChannel);

        [DllImport("DDL1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DDL1M12_GetDeviceChannelData( UInt32 ddlHandle, [MarshalAs(UnmanagedType.LPArray, SizeConst=65536)] UInt32[] pChannelADataLoggerBuffer, out UInt32 lpdwChannelANumValuesReturned,
                                                                  [MarshalAs(UnmanagedType.LPArray, SizeConst=65536)] UInt32[] pChannelBDataLoggerBuffer, out UInt32 lpdwChannelBNumValuesReturned);

        [DllImport("DDL1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DDL1M12_SetDeviceFunctionGeneratorState( UInt32 ddlHandle, Boolean bActiveState);

        [DllImport("DDL1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DDL1M12_GetWaveformFrequencyValues( UInt32 dwOutputWaveform, UInt32 dwFrequencyValue, UInt32 dwNumSamples,
                                                                        String lpFileName, out UInt32 lpdwFrequencyHz, out UInt32 lpdwMaxFreqRangeHz, out UInt32 lpdwMinFreqRangeHz);

        [DllImport("DDL1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DDL1M12_SetDeviceFunctionGeneratorWaveform( UInt32 ddlHandle, UInt32 dwOutputWaveform, UInt32 dwFrequencyValue,
                                                                                UInt32 iPeakToPeakmVolts, UInt32 iOffsetmVolts, UInt32 dwNumSamples,
                                                                                UInt32 dwPulseDutyRatio, Boolean bInvertWaveform, string lpFileName);

        [DllImport("DDL1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DDL1M12_GetDllVersion( [MarshalAs(UnmanagedType.LPStr)] StringBuilder lpDllVersionBuffer, UInt32 dwBufferSize);

        [DllImport("DDL1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DDL1M12_GetErrorCodeString( String lpLanguage, UInt32 StatusCode,
                                                                [MarshalAs(UnmanagedType.LPStr)] StringBuilder lpErrorMessageBuffer, UInt32 dwBufferSize);
    }
}
