using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DS1M12;
using System.Threading;

namespace ScopeConsoleTestApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            UInt32 result;
            UInt32 num_devices;
            result = Scope.DS1M12_GetNumDevices(out num_devices);
            if (result != Scope.DS_SUCCESS)
                return;

            UInt32 serial_number_length = 50;
            StringBuilder serial_number = new StringBuilder((int)serial_number_length);
            result = Scope.DS1M12_GetDeviceSerialNumber(0, serial_number, serial_number_length);
            if (result != Scope.DS_SUCCESS)
                return;

            UInt32 handle;
            result = Scope.DS1M12_OpenSpecifiedDevice(serial_number.ToString(), out handle);
            if (result != Scope.DS_SUCCESS)
                return;

            /*
            result = Logger.DDL1M12_OpenDevice( ref handle);
            if( result != Scope.DDL_SUCCESS)
                return;
            */

            result = Scope.DS1M12_InitDevice(handle);
            if (result != Scope.DS_SUCCESS)
                return;

            Boolean is_programmed;
            result = Scope.DS1M12_IsDeviceProgrammed(handle, out is_programmed);
            if (result != Scope.DS_SUCCESS) {
                result = Logger.DDL1M12_ProgramDevice(handle, "C:\\DDL1M12.RBF");
                if (result != Scope.DS_SUCCESS)
                    return;
            }

            result = Scope.DS1M12_SetDeviceChannelState( handle, true, false);
            if( result != Scope.DS_SUCCESS)
                return;

            Scope.Ds_Trigger_Data DualScopeTriggerData = new Scope.Ds_Trigger_Data();
            DualScopeTriggerData.bAutoTriggerMode = true;
            DualScopeTriggerData.bEdgePulseTrigger = false;
            DualScopeTriggerData.bEdgeTriggerPositiveNegative = false;
            DualScopeTriggerData.iTriggerLevelmVolts = 3;
            DualScopeTriggerData.bPulseWidthTriggerPositiveNegative = false;
            DualScopeTriggerData.bPulseWidthTriggerLessThanGreaterThan = false;
            DualScopeTriggerData.dwTriggerPulseWidth = 3; //65536
            DualScopeTriggerData.dwDelayScanAfterTriggerNumSampleRateIntervals = 0; //65536;

            Scope.Ds_Channel_Control_Data DualScopeChannelAControlData = new Scope.Ds_Channel_Control_Data();
            DualScopeChannelAControlData.bGainTimesTen = true;
            DualScopeChannelAControlData.dwGainuVBit = Scope.GAIN_1MV_PER_BIT;
            DualScopeChannelAControlData.bCouplingState = false;
            DualScopeChannelAControlData.bActiveState = true;

            Scope.Ds_Channel_Control_Data DualScopeChannelBControlData = new Scope.Ds_Channel_Control_Data();
            DualScopeChannelBControlData.bGainTimesTen = true;
            DualScopeChannelBControlData.dwGainuVBit = Scope.GAIN_1MV_PER_BIT;
            DualScopeChannelBControlData.bCouplingState = false;
            DualScopeChannelBControlData.bActiveState = false;

            Boolean bDualScopeTriggered = false;
            UInt32[] ChannelADataBuffer = new UInt32[Scope.CHANNEL_DATA_VALUES_BUFFER_SIZE];
            UInt32[] ChannelBDataBuffer = new UInt32[Scope.CHANNEL_DATA_VALUES_BUFFER_SIZE];
            Scope.Ds_Channel_Data ChannelAData = new Scope.Ds_Channel_Data();
            Scope.Ds_Channel_Data ChannelBData = new Scope.Ds_Channel_Data();
            UInt32 dwDelayCntr = 0;
            UInt32 dwLoopCntr = 0;

            do {
                result = Scope.DS1M12_StartDeviceChannelScan( handle, false, Scope.MAXIMUM_NUMBER_SAMPLES_1K, 
                                                              Scope.CHANNEL_A, DualScopeTriggerData, Scope.SAMPLE_INTERVAL_1MS,
                                                              DualScopeChannelAControlData, DualScopeChannelBControlData);
                if( result != Scope.DS_SUCCESS)
                    return;

                do {
                    result = Scope.DS1M12_GetDeviceChannelData( handle, ref bDualScopeTriggered, ChannelADataBuffer,
                                                                ChannelAData, ChannelBDataBuffer, ChannelBData);
                    if( result != Scope.DS_DEVICE_NOT_READY)
                        Thread.Sleep(10);

                    dwDelayCntr++;
                }
                while (((dwDelayCntr < 500) && (bDualScopeTriggered == false)) &&
                       ((result == Scope.DS_SUCCESS) || (result == Scope.DS_DEVICE_NOT_READY)));

                if ((result == Scope.DS_SUCCESS) && (bDualScopeTriggered == true))
                {
                    // Process Data
                }

                dwLoopCntr++;
            } while ((dwLoopCntr < 100) && ((result == Scope.DS_SUCCESS) || (result == Scope.DS_DEVICE_NOT_READY)));

            if( (result != Scope.DS_SUCCESS) && (result != Scope.DS_DEVICE_NOT_READY))
                return;

            result = Scope.DS1M12_SetDeviceChannelState( handle, false, false);
            if( result != Scope.DS_SUCCESS)
                return;

            result = Scope.DS1M12_SetDeviceFunctionGeneratorState( handle, true);
            if( result != Scope.DS_SUCCESS)
                return;

            UInt32 dwFrequencyHz, dwMaxFreqRangeHz, dwMinFreqRangeHz;
            result = Scope.DS1M12_GetWaveformFrequencyValues( Scope.WAVEFORM_SAWTOOTH, 100, 128, "C:\\MyWave1.csv", out dwFrequencyHz, out dwMaxFreqRangeHz, out dwMinFreqRangeHz);
            if( result != Scope.DS_SUCCESS)
                return;

            result = Scope.DS1M12_SetDeviceFunctionGeneratorWaveform( handle, Scope.WAVEFORM_SAWTOOTH, 100, 5000, 0, 128, 99, false, "C:\\MyWave1.csv");
            if( result != Scope.DS_SUCCESS)
                return;

            Thread.Sleep(1000);

            result = Scope.DS1M12_SetDeviceFunctionGeneratorState( handle, false);
            if( result != Scope.DS_SUCCESS)
                return;
            
            result = Scope.DS1M12_CloseDevice( handle);
            if( result != Scope.DS_SUCCESS)
                return;
        }
    }
}