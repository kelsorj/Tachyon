using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace DS1M12
{
    /// <summary>
    /// this class wraps the DS1M12 C++ library for the Scope feature
    /// </summary>
    public class Scope
    {
        public const UInt16 CHANNEL_A = 1;
        public const UInt16 CHANNEL_B = 2;
        public const UInt16 EXTERNAL_TRIGGER = 3;

        public const UInt16 GAIN_100UV_PER_BIT     = 1;
        public const UInt16 GAIN_200UV_PER_BIT     = 2;
        public const UInt16 GAIN_500UV_PER_BIT     = 3;
        public const UInt16 GAIN_1MV_PER_BIT       = 4;
        public const UInt16 GAIN_2MV_PER_BIT       = 5;
        public const UInt16 GAIN_5MV_PER_BIT       = 6;
        public const UInt16 GAIN_10MV_PER_BIT      = 7;
        public const UInt16 GAIN_20MV_PER_BIT      = 8;
        public const UInt16 GAIN_50MV_PER_BIT      = 9;

        public const UInt16 WAVEFORM_DC           = 1;
        public const UInt16 WAVEFORM_SQUARE       = 2;
        public const UInt16 WAVEFORM_SAWTOOTH     = 3;
        public const UInt16 WAVEFORM_SIN_COS      = 4;
        public const UInt16 WAVEFORM_TRIANGULAR   = 5;
        public const UInt16 WAVEFORM_PULSE        = 6;
        public const UInt16 WAVEFORM_CUSTOM       = 7;

        public const UInt16 DS_SUCCESS                 = 0; // FT_OK
        public const UInt16 DS_INVALID_HANDLE          = 1; // FT_INVALID_HANDLE
        public const UInt16 DS_DEVICE_NOT_FOUND        = 2; //FT_DEVICE_NOT_FOUND
        public const UInt16 DS_DEVICE_NOT_OPENED       = 3; //FT_DEVICE_NOT_OPENED
        public const UInt16 DS_IO_ERROR                = 4; //FT_IO_ERROR
        public const UInt16 DS_INSUFFICIENT_RESOURCES  = 5; // FT_INSUFFICIENT_RESOURCES

        public const UInt16 SAMPLE_INTERVAL_1US     = 1;      // 1 micro-seconds sample interval
        public const UInt16 SAMPLE_INTERVAL_2US     = 2;      // 2 micro-seconds sample interval
        public const UInt16 SAMPLE_INTERVAL_4US     = 3;      // 5 micro-seconds sample interval4
        public const UInt16 SAMPLE_INTERVAL_10US    = 4;      // 10 micro-seconds sample interval
        public const UInt16 SAMPLE_INTERVAL_20US    = 5;      // 20 micro-seconds sample interval
        public const UInt16 SAMPLE_INTERVAL_40US    = 6;      // 40 micro-seconds sample interval
        public const UInt16 SAMPLE_INTERVAL_100US   = 7;      // 100 micro-seconds sample interval
        public const UInt16 SAMPLE_INTERVAL_200US   = 8;      // 200 micro-seconds sample interval
        public const UInt16 SAMPLE_INTERVAL_400US   = 9;      // 400 micro-seconds sample interval
        public const UInt16 SAMPLE_INTERVAL_1MS     = 10;     // 1 milli-seconds sample interval
        public const UInt16 SAMPLE_INTERVAL_2MS     = 11;     // 2 milli-seconds sample interval
        public const UInt16 SAMPLE_INTERVAL_4MS     = 12;     // 4 milli-seconds sample interval
        public const UInt16 SAMPLE_INTERVAL_10MS    = 13;     // 10 milli-seconds sample interval
        public const UInt16 SAMPLE_INTERVAL_20MS    = 14;    // 20 milli-seconds sample interval
        public const UInt16 SAMPLE_INTERVAL_40MS    = 15;     // 40 milli-seconds sample interval

        public const UInt16 MAXIMUM_NUMBER_SAMPLES_1K   = 1;
        public const UInt16 MAXIMUM_NUMBER_SAMPLES_2K   = 2;
        public const UInt16 MAXIMUM_NUMBER_SAMPLES_4K   = 3;
        public const UInt16 MAXIMUM_NUMBER_SAMPLES_8K   = 4;

        public const UInt16 DS_FAILED_TO_COMPLETE_COMMAND           = 20;    // cannot change, error code mapped from Scopes and MorphIC classes
        public const UInt16 DS_FAILED_TO_SYNCHRONIZE_DEVICE         = 21;  // cannot change, error code mapped from Scopes and MorphIC classes
        public const UInt16 DS_FPGA_FILE_NOT_FOUND                  = 22;           // cannot change, error code mapped from MorphIC class
        public const UInt16 DS_INVALID_FPGA_FILE_NAME               = 23;        // cannot change, error code mapped from MorphIC class
        public const UInt16 DS_INVALID_FPGA_FILE_EXTENSION          = 24;   // cannot change, error code mapped from MorphIC class
        public const UInt16 DS_FAILED_TO_OPEN_FPGA_FILE             = 25;      // cannot change, error code mapped from MorphIC class
        public const UInt16 DS_FPGA_FILE_READ_ERROR                 = 26;          // cannot change, error code mapped from MorphIC class
        public const UInt16 DS_FPGA_FILE_CORRUPT                    = 27;             // cannot change, error code mapped from MorphIC class
        public const UInt16 DS_FAILED_TO_CLEAR_DEVICE               = 28;        // cannot change, error code mapped from MorphIC class
        public const UInt16 DS_FAILED_TO_PROGRAM_DEVICE             = 29;      // cannot change, error code mapped from MorphIC class
        public const UInt16 DS_DEVICE_PROGRAM_CHANNEL_IN_USE        = 30; // cannot change, error code mapped from MorphIC class
        public const UInt16 DS_CALIB_DATA_READ_FAILURE              = 31;
        public const UInt16 DS_TOO_MANY_DEVICES                     = 32;
        public const UInt16 DS_INVALID_SERIAL_NUMBER_INDEX          = 33;
        public const UInt16 DS_NULL_SERIAL_NUMBER_BUFFER_POINTER    = 34;
        public const UInt16 DS_SERIAL_NUMBER_BUFFER_TOO_SMALL       = 35;
        public const UInt16 DS_INVALID_SERIAL_NUMBER                = 36;
        public const UInt16 DS_DEVICE_IN_USE                        = 37;
        public const UInt16 DS_DEVICE_IN_USE_DEVICE_NOT_FOUND       = 38;
        public const UInt16 DS_FAILED_TO_SYNCHRONIZE_DEVICE_MPSSE   = 39;
        public const UInt16 DS_INVALID_CHANNEL                      = 40;
        public const UInt16 DS_INVALID_MAX_NUMBER_SAMPLES           = 41;
        public const UInt16 DS_NULL_TRIGGER_DATA_POINTER            = 42;
        public const UInt16 DS_CHANNEL_TRIGGER_LEVEL_OUT_OF_RANGE   = 43;
        public const UInt16 DS_EXTERNAL_TRIGGER_LEVEL_OUT_OF_RANGE  = 44;
        public const UInt16 DS_PULSE_WIDTH_OUT_OF_RANGE             = 45;
        public const UInt16 DS_MAX_DELAY_SCAN_EXCEEDED              = 46;
        public const UInt16 DS_INVALID_GAIN                         = 47;
        public const UInt16 DS_INVALID_SAMPLE_RATE                  = 48;
        public const UInt16 DS_NULL_CHANNEL_CONTROL_DATA_POINTER    = 49;
        public const UInt16 DS_NULL_CHANNEL_DATA_BUFFER_POINTER     = 50;
        public const UInt16 DS_NULL_CHANNEL_DATA_POINTER            = 51;
        public const UInt16 DS_DEVICE_NOT_READY                     = 52;
        public const UInt16 DS_CHANNEL_A_FUSE_FAILED                = 53;
        public const UInt16 DS_CHANNEL_B_FUSE_FAILED                = 54;
        public const UInt16 DS_EXTERNAL_TRIGGER_FUSE_FAILED         = 55;
        public const UInt16 DS_INVALID_WAVEFORM                     = 56;
        public const UInt16 DS_INVALID_FREQUENCY_VALUE              = 57;
        public const UInt16 DS_INVALID_PEAK_TO_PEAK_VOLTAGE_LEVEL   = 58;
        public const UInt16 DS_INVALID_DC_VOLTAGE_LEVEL             = 59;
        public const UInt16 DS_INVALID_OFFSET_VOLTAGE_LEVEL         = 60;
        public const UInt16 DS_INVALID_NUM_SAMPLES                  = 61;
        public const UInt16 DS_INVALID_PULSE_DUTY_RATIO             = 62;
        public const UInt16 DS_INVALID_CSV_FILE_NAME                = 63;
        public const UInt16 DS_INVALID_CSV_FILE_EXTENSION           = 64;
        public const UInt16 DS_CSV_FILE_NOT_FOUND                   = 65;
        public const UInt16 DS_CSV_FILE_CORRUPT                     = 66;
        public const UInt16 DS_FAILED_TO_OPEN_CSV_FILE              = 67;
        public const UInt16 DS_CSV_FILE_READ_ERROR                  = 68;
        public const UInt16 DS_FUNCTION_GENERATOR_FUSE_FAILED       = 69;
        public const UInt16 DS_NULL_DLL_VERSION_BUFFER_POINTER      = 70;
        public const UInt16 DS_DLL_VERSION_BUFFER_TOO_SMALL         = 71;
        public const UInt16 DS_NULL_LANGUAGE_CODE_BUFFER_POINTER    = 72;
        public const UInt16 DS_NULL_ERROR_MESSAGE_BUFFER_POINTER    = 73;
        public const UInt16 DS_ERROR_MESSAGE_BUFFER_TOO_SMALL       = 74;
        public const UInt16 DS_INVALID_LANGUAGE_CODE                = 75;
        public const UInt16 DS_INVALID_STATUS_CODE                  = 76;

        public const Int32 CHANNEL_DATA_VALUES_BUFFER_SIZE = 8192;    // 8k integers

        [StructLayout(LayoutKind.Sequential)]
        public class Ds_Trigger_Data
        {
            public Boolean bAutoTriggerMode;
            public Boolean bEdgePulseTrigger;
            public Boolean bEdgeTriggerPositiveNegative;
            public UInt32 iTriggerLevelmVolts;
            public Boolean bPulseWidthTriggerPositiveNegative;
            public Boolean bPulseWidthTriggerLessThanGreaterThan;
            public UInt32 dwTriggerPulseWidth;
            public UInt32 dwDelayScanAfterTriggerNumSampleRateIntervals;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class Ds_Channel_Control_Data
        {
            public Boolean bGainTimesTen;
            public UInt32 dwGainuVBit;
            public Boolean bCouplingState;
            public Boolean bActiveState;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class Ds_Channel_Data
        {
            public UInt32 dwMaxNumDataValues;
            public UInt32 dwNumValuesReturned;
            public Boolean bMaxValuesClipped;
            public Boolean bMinValuesClipped;
        }

        [DllImport("DS1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DS1M12_GetNumDevices(out UInt32 lpdwNumDevices);

        [DllImport("DS1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DS1M12_GetDeviceSerialNumber( UInt32 dwSerialNumberIndex, [MarshalAs(UnmanagedType.LPStr)] StringBuilder lpSerialNumberBuffer, UInt32 dwBufferSize);

        [DllImport("DS1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DS1M12_OpenSpecifiedDevice( String lpSerialNumber, out UInt32 pddlHandle);

        [DllImport("DS1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DS1M12_OpenDevice( ref UInt32 pddlHandle);

        [DllImport("DS1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DS1M12_CloseDevice( UInt32 ddlHandle);

        [DllImport("DS1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DS1M12_InitDevice( UInt32 ddlHandle);

        [DllImport("DS1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DS1M12_IsDeviceProgrammed( UInt32 ddlHandle, out Boolean lpbProgramState);

        [DllImport("DS1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DS1M12_ProgramDevice( UInt32 ddlHandle, String lpFileName);

        [DllImport("DS1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DS1M12_SetDeviceChannelState( UInt32 ddlHandle, Boolean bChannelAActiveState, Boolean bChannelBActiveState);

        [DllImport("DS1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DS1M12_StartDeviceChannelScan( UInt32 dsHandle, Boolean bTestMode, UInt32 dwMaxNumSamples,
                                                                   UInt32 dwTriggerChannel, [In, MarshalAs(UnmanagedType.LPStruct)] Ds_Trigger_Data pTriggerData,
                                                                   UInt32 dwSampleInterval, [In, MarshalAs(UnmanagedType.LPStruct)] Ds_Channel_Control_Data pChannelAControlData,
                                                                   [In, MarshalAs(UnmanagedType.LPStruct)] Ds_Channel_Control_Data pChannelBControlData);

        [DllImport("DS1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DS1M12_GetDeviceChannelData( UInt32 ddlHandle, ref Boolean lpbDualScopeTriggered,
                                                                  [MarshalAs(UnmanagedType.LPArray, SizeConst=CHANNEL_DATA_VALUES_BUFFER_SIZE)] UInt32[] pChannelADataBuffer,
                                                                  [Out, MarshalAs(UnmanagedType.LPStruct)] Ds_Channel_Data pChannelAData,
                                                                  [MarshalAs(UnmanagedType.LPArray, SizeConst=CHANNEL_DATA_VALUES_BUFFER_SIZE)] UInt32[] pChannelBDataBuffer,
                                                                  [Out, MarshalAs(UnmanagedType.LPStruct)] Ds_Channel_Data pChannelBData);

        [DllImport("DS1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DS1M12_SetDeviceFunctionGeneratorState( UInt32 ddlHandle, Boolean bActiveState);

        [DllImport("DS1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DS1M12_GetWaveformFrequencyValues( UInt32 dwOutputWaveform, UInt32 dwFrequencyValue, UInt32 dwNumSamples,
                                                                        String lpFileName, out UInt32 lpdwFrequencyHz, out UInt32 lpdwMaxFreqRangeHz, out UInt32 lpdwMinFreqRangeHz);

        [DllImport("DS1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DS1M12_SetDeviceFunctionGeneratorWaveform( UInt32 ddlHandle, UInt32 dwOutputWaveform, UInt32 dwFrequencyValue,
                                                                                UInt32 iPeakToPeakmVolts, UInt32 iOffsetmVolts, UInt32 dwNumSamples,
                                                                                UInt32 dwPulseDutyRatio, Boolean bInvertWaveform, string lpFileName);

        [DllImport("DS1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DS1M12_GetDllVersion( [MarshalAs(UnmanagedType.LPStr)] StringBuilder lpDllVersionBuffer, UInt32 dwBufferSize);

        [DllImport("DS1M12.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Winapi)]
        public static extern UInt32 DS1M12_GetErrorCodeString( String lpLanguage, UInt32 StatusCode,
                                                                [MarshalAs(UnmanagedType.LPStr)] StringBuilder lpErrorMessageBuffer, UInt32 dwBufferSize);

    }
}
