/*++

Copyright (c) 2004  Future Technology Devices International Ltd.

Module Name:

    ds1m12.h

Abstract:

    API DLL for Dual Data Logger Device(DDL1M12).
    DDL1M12 library definitions

Environment:

    kernel & user mode

Revision History:

    24/09/04    kra     Created.
	
--*/


#ifndef DDL1M12_H
#define DDL1M12_H


// The following ifdef block is the standard way of creating macros
// which make exporting from a DLL simpler.  All files within this DLL
// are compiled with the DDL1M12_EXPORTS symbol defined on the command line.
// This symbol should not be defined on any project that uses this DLL.
// This way any other project whose source files include this file see
// DDL2200_API functions as being imported from a DLL, whereas this DLL
// sees symbols defined with this macro as being exported.

#ifdef DDL1M12_EXPORTS
#define DDL1M12_API __declspec(dllexport)
#else
#define DDL1M12_API __declspec(dllimport)
#endif

typedef DWORD DDL_HANDLE;
typedef ULONG DDL_STATUS;

#define CHANNEL_A 1
#define CHANNEL_B 2

#define GAIN_100UV_PER_BIT     1
#define GAIN_200UV_PER_BIT     2
#define GAIN_500UV_PER_BIT     3
#define GAIN_1MV_PER_BIT       4
#define GAIN_2MV_PER_BIT       5
#define GAIN_5MV_PER_BIT       6
#define GAIN_10MV_PER_BIT      7

#define WAVEFORM_DC           1
#define WAVEFORM_SQUARE       2
#define WAVEFORM_SAWTOOTH     3
#define WAVEFORM_SIN_COS      4
#define WAVEFORM_TRIANGULAR   5
#define WAVEFORM_PULSE        6
#define WAVEFORM_CUSTOM       7


#define DDL_SUCCESS 0 // FT_OK
#define DDL_INVALID_HANDLE 1 // FT_INVALID_HANDLE
#define DDL_DEVICE_NOT_FOUND 2 //FT_DEVICE_NOT_FOUND
#define DDL_DEVICE_NOT_OPENED 3 //FT_DEVICE_NOT_OPENED
#define DDL_IO_ERROR 4 //FT_IO_ERROR
#define DDL_INSUFFICIENT_RESOURCES 5 // FT_INSUFFICIENT_RESOURCES

#define DDL_FAILED_TO_COMPLETE_COMMAND 20    // cannot change, error code mapped from Scopes and MorphIC classes
#define DDL_FAILED_TO_SYNCHRONIZE_DEVICE 21  // cannot change, error code mapped from Scopes and MorphIC classes
#define DDL_FPGA_FILE_NOT_FOUND 22           // cannot change, error code mapped from MorphIC class
#define DDL_INVALID_FPGA_FILE_NAME 23        // cannot change, error code mapped from MorphIC class
#define DDL_INVALID_FPGA_FILE_EXTENSION 24   // cannot change, error code mapped from MorphIC class
#define DDL_FAILED_TO_OPEN_FPGA_FILE 25      // cannot change, error code mapped from MorphIC class
#define DDL_FPGA_FILE_READ_ERROR 26          // cannot change, error code mapped from MorphIC class
#define DDL_FPGA_FILE_CORRUPT 27             // cannot change, error code mapped from MorphIC class
#define DDL_FAILED_TO_CLEAR_DEVICE 28        // cannot change, error code mapped from MorphIC class
#define DDL_FAILED_TO_PROGRAM_DEVICE 29      // cannot change, error code mapped from MorphIC class
#define DDL_DEVICE_PROGRAM_CHANNEL_IN_USE 30 // cannot change, error code mapped from MorphIC class
#define DDL_CALIB_DATA_READ_FAILURE 31
#define DDL_TOO_MANY_DEVICES 32
#define DDL_INVALID_SERIAL_NUMBER_INDEX 33
#define DDL_NULL_SERIAL_NUMBER_BUFFER_POINTER 34
#define DDL_SERIAL_NUMBER_BUFFER_TOO_SMALL 35
#define DDL_INVALID_SERIAL_NUMBER 36
#define DDL_DEVICE_IN_USE 37
#define DDL_DEVICE_IN_USE_DEVICE_NOT_FOUND 38
#define DDL_FAILED_TO_SYNCHRONIZE_DEVICE_MPSSE 39
#define DDL_INVALID_CHANNEL 40
#define DDL_INVALID_GAIN 41
#define DDL_INVALID_SAMPLE_RATE 42
#define DDL_NULL_CHANNEL_CONTROL_DATA_POINTER 43
#define DDL_NULL_CHANNEL_DATA_BUFFER_POINTER 44
#define DDL_CHANNEL_A_FUSE_FAILED 45
#define DDL_CHANNEL_B_FUSE_FAILED 46
#define DDL_INVALID_WAVEFORM 47
#define DDL_INVALID_FREQUENCY_VALUE 48
#define DDL_INVALID_PEAK_TO_PEAK_VOLTAGE_LEVEL 49
#define DDL_INVALID_DC_VOLTAGE_LEVEL 50
#define DDL_INVALID_OFFSET_VOLTAGE_LEVEL 51
#define DDL_INVALID_NUM_SAMPLES 52
#define DDL_INVALID_PULSE_DUTY_RATIO 53
#define DDL_INVALID_CSV_FILE_NAME 54
#define DDL_INVALID_CSV_FILE_EXTENSION 55
#define DDL_CSV_FILE_NOT_FOUND 56
#define DDL_CSV_FILE_CORRUPT 57
#define DDL_FAILED_TO_OPEN_CSV_FILE 58
#define DDL_CSV_FILE_READ_ERROR 59
#define DDL_FUNCTION_GENERATOR_FUSE_FAILED 60
#define DDL_NULL_DLL_VERSION_BUFFER_POINTER 61
#define DDL_DLL_VERSION_BUFFER_TOO_SMALL 62
#define DDL_NULL_LANGUAGE_CODE_BUFFER_POINTER 63
#define DDL_NULL_ERROR_MESSAGE_BUFFER_POINTER 64
#define DDL_ERROR_MESSAGE_BUFFER_TOO_SMALL 65
#define DDL_INVALID_LANGUAGE_CODE 66
#define DDL_INVALID_STATUS_CODE 67

#ifdef __cplusplus
extern "C" {
#endif

DDL1M12_API
DDL_STATUS WINAPI DDL1M12_GetNumDevices(LPDWORD lpdwNumDevices);

DDL1M12_API
DDL_STATUS WINAPI DDL1M12_GetDeviceSerialNumber(DWORD dwSerialNumberIndex, LPSTR lpSerialNumberBuffer, DWORD dwBufferSize);

DDL1M12_API
DDL_STATUS WINAPI DDL1M12_OpenSpecifiedDevice(LPSTR lpSerialNumber, DDL_HANDLE *pddlHandle);

DDL1M12_API
DDL_STATUS WINAPI DDL1M12_OpenDevice(DDL_HANDLE *pddlHandle);

DDL1M12_API
DDL_STATUS WINAPI DDL1M12_CloseDevice(DDL_HANDLE ddlHandle);

DDL1M12_API
DDL_STATUS WINAPI DDL1M12_InitDevice(DDL_HANDLE ddlHandle);

DDL1M12_API
DDL_STATUS WINAPI DDL1M12_ProgramDevice(DDL_HANDLE ddlHandle, LPSTR lpFileName);

DDL1M12_API
DDL_STATUS WINAPI DDL1M12_IsDeviceProgrammed(DDL_HANDLE ddlHandle, LPBOOL lpbProgramState);

DDL1M12_API
DDL_STATUS WINAPI DDL1M12_SetDeviceChannelState(DDL_HANDLE ddlHandle, BOOL bChannelAActiveState, BOOL bChannelBActiveState);

typedef struct Ddl_Channel_Control_Data{
  BOOL  bGainTimesTen;
  DWORD dwGainuVBit;
  BOOL  bCouplingState;
  BOOL  bActiveState;
}DDL_CHANNEL_CONTROL_DATA, *PDDL_CHANNEL_CONTROL_DATA;

DDL1M12_API
DDL_STATUS WINAPI DDL1M12_StartDeviceDataLogging(DDL_HANDLE ddlHandle, BOOL bTestMode, DWORD dwSampleIntervaluSecs,
                                                 PDDL_CHANNEL_CONTROL_DATA pChannelAControlData, PDDL_CHANNEL_CONTROL_DATA pChannelBControlData);

DDL1M12_API
DDL_STATUS WINAPI DDL1M12_StopDeviceDataLogging(DDL_HANDLE ddlHandle, DWORD dwChannel);

#define DATA_LOGGER_BUFFER_SIZE   16384   // 16k bytes

#define NUM_DATA_LOG_ELEMENTS       4

#define DATA_VALUE_INDEX            0
#define MISSING_VALUE_INDEX         1
#define MAX_VALUE_CLIPPED_INDEX     2
#define MIN_VALUE_CLIPPED_INDEX     3

typedef INT DataLoggerBuffer[DATA_LOGGER_BUFFER_SIZE][NUM_DATA_LOG_ELEMENTS];
typedef DataLoggerBuffer *PDataLoggerBuffer;

DDL1M12_API
DDL_STATUS WINAPI DDL1M12_GetDeviceChannelData(DDL_HANDLE ddlHandle, PDataLoggerBuffer pChannelADataLoggerBuffer, LPDWORD lpdwChannelANumValuesReturned,
                                               PDataLoggerBuffer pChannelBDataLoggerBuffer, LPDWORD lpdwChannelBNumValuesReturned);

DDL1M12_API
DDL_STATUS WINAPI DDL1M12_SetDeviceFunctionGeneratorState(DDL_HANDLE ddlHandle, BOOL bActiveState);

DDL1M12_API
DDL_STATUS WINAPI DDL1M12_GetWaveformFrequencyValues(DWORD dwOutputWaveform, DWORD dwFrequencyValue,
                                                   DWORD dwNumSamples, LPSTR lpFileName, LPDWORD lpdwFrequencyHz,
                                                   LPDWORD lpdwMaxFreqRangeHz, LPDWORD lpdwMinFreqRangeHz);

DDL1M12_API
DDL_STATUS WINAPI DDL1M12_SetDeviceFunctionGeneratorWaveform(DDL_HANDLE ddlHandle, DWORD dwOutputWaveform,
                                                           DWORD dwFrequencyValue, INT iPeakToPeakmVolts,
                                                           INT iOffsetmVolts, DWORD dwNumSamples,
                                                           DWORD dwPulseDutyRatio, BOOL bInvertWaveform,
                                                           LPSTR lpFileName);

DDL1M12_API
DDL_STATUS WINAPI DDL1M12_GetDllVersion(LPSTR lpDllVersionBuffer, DWORD dwBufferSize);

DDL1M12_API
DDL_STATUS WINAPI DDL1M12_GetErrorCodeString(LPSTR lpLanguage, DDL_STATUS StatusCode,
                                           LPSTR lpErrorMessageBuffer, DWORD dwBufferSize);


#ifdef __cplusplus
}
#endif


#endif  /* DDL1M12_H */