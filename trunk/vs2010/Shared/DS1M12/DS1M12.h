/*++

Copyright (c) 2004  Future Technology Devices International Ltd.

Module Name:

    ds1m12.h

Abstract:

    API DLL for Dual Scope Device(DS1M12).
    DS1M12 library definitions

Environment:

    kernel & user mode

Revision History:

    07/07/04    kra     Created.
	
--*/


#ifndef DS1M12_H
#define DS1M12_H


// The following ifdef block is the standard way of creating macros
// which make exporting from a DLL simpler.  All files within this DLL
// are compiled with the DS1M12_EXPORTS symbol defined on the command line.
// This symbol should not be defined on any project that uses this DLL.
// This way any other project whose source files include this file see
// DS2200_API functions as being imported from a DLL, whereas this DLL
// sees symbols defined with this macro as being exported.

#ifdef DS1M12_EXPORTS
#define DS1M12_API __declspec(dllexport)
#else
#define DS1M12_API __declspec(dllimport)
#endif

typedef DWORD DS_HANDLE;
typedef ULONG DS_STATUS;

const CHANNEL_A = 1;
const CHANNEL_B = 2;
const EXTERNAL_TRIGGER = 3;

#define MAXIMUM_NUMBER_SAMPLES_1K 1
#define MAXIMUM_NUMBER_SAMPLES_2K 2
#define MAXIMUM_NUMBER_SAMPLES_4K 3
#define MAXIMUM_NUMBER_SAMPLES_8K 4

#define GAIN_100UV_PER_BIT     1
#define GAIN_200UV_PER_BIT     2
#define GAIN_500UV_PER_BIT     3
#define GAIN_1MV_PER_BIT       4
#define GAIN_2MV_PER_BIT       5
#define GAIN_5MV_PER_BIT       6
#define GAIN_10MV_PER_BIT      7
#define GAIN_20MV_PER_BIT      8
#define GAIN_50MV_PER_BIT      9

#define SAMPLE_INTERVAL_1US     1      // 1 micro-seconds sample interval
#define SAMPLE_INTERVAL_2US     2      // 2 micro-seconds sample interval
#define SAMPLE_INTERVAL_4US     3      // 5 micro-seconds sample interval4
#define SAMPLE_INTERVAL_10US    4      // 10 micro-seconds sample interval
#define SAMPLE_INTERVAL_20US    5      // 20 micro-seconds sample interval
#define SAMPLE_INTERVAL_40US    6      // 40 micro-seconds sample interval
#define SAMPLE_INTERVAL_100US   7      // 100 micro-seconds sample interval
#define SAMPLE_INTERVAL_200US   8      // 200 micro-seconds sample interval
#define SAMPLE_INTERVAL_400US   9      // 400 micro-seconds sample interval
#define SAMPLE_INTERVAL_1MS     10     // 1 milli-seconds sample interval
#define SAMPLE_INTERVAL_2MS     11     // 2 milli-seconds sample interval
#define SAMPLE_INTERVAL_4MS     12     // 4 milli-seconds sample interval
#define SAMPLE_INTERVAL_10MS    13     // 10 milli-seconds sample interval
#define SAMPLE_INTERVAL_20MS    14     // 20 milli-seconds sample interval
#define SAMPLE_INTERVAL_40MS    15     // 40 milli-seconds sample interval

#define WAVEFORM_DC           1
#define WAVEFORM_SQUARE       2
#define WAVEFORM_SAWTOOTH     3
#define WAVEFORM_SIN_COS      4
#define WAVEFORM_TRIANGULAR   5
#define WAVEFORM_PULSE        6
#define WAVEFORM_CUSTOM       7


#define DS_SUCCESS 0 // FT_OK
#define DS_INVALID_HANDLE 1 // FT_INVALID_HANDLE
#define DS_DEVICE_NOT_FOUND 2 //FT_DEVICE_NOT_FOUND
#define DS_DEVICE_NOT_OPENED 3 //FT_DEVICE_NOT_OPENED
#define DS_IO_ERROR 4 //FT_IO_ERROR
#define DS_INSUFFICIENT_RESOURCES 5 // FT_INSUFFICIENT_RESOURCES

#define DS_FAILED_TO_COMPLETE_COMMAND 20    // cannot change, error code mapped from Scopes and MorphIC classes
#define DS_FAILED_TO_SYNCHRONIZE_DEVICE 21  // cannot change, error code mapped from Scopes and MorphIC classes
#define DS_FPGA_FILE_NOT_FOUND 22           // cannot change, error code mapped from MorphIC class
#define DS_INVALID_FPGA_FILE_NAME 23        // cannot change, error code mapped from MorphIC class
#define DS_INVALID_FPGA_FILE_EXTENSION 24   // cannot change, error code mapped from MorphIC class
#define DS_FAILED_TO_OPEN_FPGA_FILE 25      // cannot change, error code mapped from MorphIC class
#define DS_FPGA_FILE_READ_ERROR 26          // cannot change, error code mapped from MorphIC class
#define DS_FPGA_FILE_CORRUPT 27             // cannot change, error code mapped from MorphIC class
#define DS_FAILED_TO_CLEAR_DEVICE 28        // cannot change, error code mapped from MorphIC class
#define DS_FAILED_TO_PROGRAM_DEVICE 29      // cannot change, error code mapped from MorphIC class
#define DS_DEVICE_PROGRAM_CHANNEL_IN_USE 30 // cannot change, error code mapped from MorphIC class
#define DS_CALIB_DATA_READ_FAILURE 31
#define DS_TOO_MANY_DEVICES 32
#define DS_INVALID_SERIAL_NUMBER_INDEX 33
#define DS_NULL_SERIAL_NUMBER_BUFFER_POINTER 34
#define DS_SERIAL_NUMBER_BUFFER_TOO_SMALL 35
#define DS_INVALID_SERIAL_NUMBER 36
#define DS_DEVICE_IN_USE 37
#define DS_DEVICE_IN_USE_DEVICE_NOT_FOUND 38
#define DS_FAILED_TO_SYNCHRONIZE_DEVICE_MPSSE 39
#define DS_INVALID_CHANNEL 40
#define DS_INVALID_MAX_NUMBER_SAMPLES 41
#define DS_NULL_TRIGGER_DATA_POINTER 42
#define DS_CHANNEL_TRIGGER_LEVEL_OUT_OF_RANGE 43
#define DS_EXTERNAL_TRIGGER_LEVEL_OUT_OF_RANGE 44
#define DS_PULSE_WIDTH_OUT_OF_RANGE 45
#define DS_MAX_DELAY_SCAN_EXCEEDED 46
#define DS_INVALID_GAIN 47
#define DS_INVALID_SAMPLE_RATE 48
#define DS_NULL_CHANNEL_CONTROL_DATA_POINTER 49
#define DS_NULL_CHANNEL_DATA_BUFFER_POINTER 50
#define DS_NULL_CHANNEL_DATA_POINTER 51
#define DS_DEVICE_NOT_READY 52
#define DS_CHANNEL_A_FUSE_FAILED 53
#define DS_CHANNEL_B_FUSE_FAILED 54
#define DS_EXTERNAL_TRIGGER_FUSE_FAILED 55
#define DS_INVALID_WAVEFORM 56
#define DS_INVALID_FREQUENCY_VALUE 57
#define DS_INVALID_PEAK_TO_PEAK_VOLTAGE_LEVEL 58
#define DS_INVALID_DC_VOLTAGE_LEVEL 59
#define DS_INVALID_OFFSET_VOLTAGE_LEVEL 60
#define DS_INVALID_NUM_SAMPLES 61
#define DS_INVALID_PULSE_DUTY_RATIO 62
#define DS_INVALID_CSV_FILE_NAME 63
#define DS_INVALID_CSV_FILE_EXTENSION 64
#define DS_CSV_FILE_NOT_FOUND 65
#define DS_CSV_FILE_CORRUPT 66
#define DS_FAILED_TO_OPEN_CSV_FILE 67
#define DS_CSV_FILE_READ_ERROR 68
#define DS_FUNCTION_GENERATOR_FUSE_FAILED 69
#define DS_NULL_DLL_VERSION_BUFFER_POINTER 70
#define DS_DLL_VERSION_BUFFER_TOO_SMALL 71
#define DS_NULL_LANGUAGE_CODE_BUFFER_POINTER 72
#define DS_NULL_ERROR_MESSAGE_BUFFER_POINTER 73
#define DS_ERROR_MESSAGE_BUFFER_TOO_SMALL 74
#define DS_INVALID_LANGUAGE_CODE 75
#define DS_INVALID_STATUS_CODE 76

#ifdef __cplusplus
extern "C" {
#endif

DS1M12_API
DS_STATUS WINAPI DS1M12_GetNumDevices(LPDWORD lpdwNumDevices);

DS1M12_API
DS_STATUS WINAPI DS1M12_GetDeviceSerialNumber(DWORD dwSerialNumberIndex, LPSTR lpSerialNumberBuffer, DWORD dwBufferSize);

DS1M12_API
DS_STATUS WINAPI DS1M12_OpenSpecifiedDevice(LPSTR lpSerialNumber, DS_HANDLE *pdsHandle);

DS1M12_API
DS_STATUS WINAPI DS1M12_OpenDevice(DS_HANDLE *pdsHandle);

DS1M12_API
DS_STATUS WINAPI DS1M12_CloseDevice(DS_HANDLE dsHandle);

DS1M12_API
DS_STATUS WINAPI DS1M12_InitDevice(DS_HANDLE dsHandle);

DS1M12_API
DS_STATUS WINAPI DS1M12_ProgramDevice(DS_HANDLE dsHandle, LPSTR lpFileName);

DS1M12_API
DS_STATUS WINAPI DS1M12_IsDeviceProgrammed(DS_HANDLE dsHandle, LPBOOL lpbProgramState);

DS1M12_API
DS_STATUS WINAPI DS1M12_SetDeviceChannelState(DS_HANDLE dsHandle, BOOL bChannelAActiveState, BOOL bChannelBActiveState);

typedef struct Ds_Trigger_Data{
  BOOL  bAutoTriggerMode;
  BOOL  bEdgePulseTrigger;
  BOOL  bEdgeTriggerPositiveNegative;
  INT   iTriggerLevelmVolts;
  BOOL  bPulseWidthTriggerPositiveNegative;
  BOOL  bPulseWidthTriggerLessThanGreaterThan;
  DWORD dwTriggerPulseWidth;
  DWORD dwDelayScanAfterTriggerNumSampleRateIntervals;
}DS_TRIGGER_DATA, *PDS_TRIGGER_DATA;

typedef struct Ds_Channel_Control_Data{
  BOOL  bGainTimesTen;
  DWORD dwGainuVBit;
  BOOL  bCouplingState;
  BOOL  bActiveState;
}DS_CHANNEL_CONTROL_DATA, *PDS_CHANNEL_CONTROL_DATA;

DS1M12_API
DS_STATUS WINAPI DS1M12_StartDeviceChannelScan(DS_HANDLE dsHandle, BOOL bTestMode, DWORD dwMaxNumSamples,
                                               DWORD dwTriggerChannel, PDS_TRIGGER_DATA pTriggerData, DWORD dwSampleInterval,
                                               PDS_CHANNEL_CONTROL_DATA pChannelAControlData, PDS_CHANNEL_CONTROL_DATA pChannelBControlData);

#define CHANNEL_DATA_VALUES_BUFFER_SIZE 8192    // 8k integers

typedef INT ChannelDataValuesBuffer[CHANNEL_DATA_VALUES_BUFFER_SIZE];
typedef ChannelDataValuesBuffer *PChannelDataValuesBuffer;

typedef struct Ds_Channel_Data{
  DWORD dwMaxNumDataValues;
  DWORD dwNumValuesReturned;
  BOOL  bMaxValuesClipped;
  BOOL  bMinValuesClipped;
}DS_CHANNEL_DATA, *PDS_CHANNEL_DATA;

DS1M12_API
DS_STATUS WINAPI DS1M12_GetDeviceChannelData(DS_HANDLE dsHandle, LPBOOL lpbDualScopeTriggered,
                                             PChannelDataValuesBuffer pChannelADataBuffer, PDS_CHANNEL_DATA pChannelAData,
                                             PChannelDataValuesBuffer pChannelBDataBuffer, PDS_CHANNEL_DATA pChannelBAData);

DS1M12_API
DS_STATUS WINAPI DS1M12_SetDeviceFunctionGeneratorState(DS_HANDLE dsHandle, BOOL bActiveState);

DS1M12_API
DS_STATUS WINAPI DS1M12_GetWaveformFrequencyValues(DWORD dwOutputWaveform, DWORD dwFrequencyValue,
                                                   DWORD dwNumSamples, LPSTR lpFileName, LPDWORD lpdwFrequencyHz,
                                                   LPDWORD lpdwMaxFreqRangeHz, LPDWORD lpdwMinFreqRangeHz);

DS1M12_API
DS_STATUS WINAPI DS1M12_SetDeviceFunctionGeneratorWaveform(DS_HANDLE dsHandle, DWORD dwOutputWaveform,
                                                           DWORD dwFrequencyValue, INT iPeakToPeakmVolts,
                                                           INT iOffsetmVolts, DWORD dwNumSamples,
                                                           DWORD dwPulseDutyRatio, BOOL bInvertWaveform,
                                                           LPSTR lpFileName);

DS1M12_API
DS_STATUS WINAPI DS1M12_GetDllVersion(LPSTR lpDllVersionBuffer, DWORD dwBufferSize);

DS1M12_API
DS_STATUS WINAPI DS1M12_GetErrorCodeString(LPSTR lpLanguage, DS_STATUS StatusCode,
                                           LPSTR lpErrorMessageBuffer, DWORD dwBufferSize);


#ifdef __cplusplus
}
#endif


#endif  /* DS1M12_H */