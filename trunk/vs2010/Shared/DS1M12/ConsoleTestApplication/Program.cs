using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DS1M12;
using System.Threading;

namespace LoggerConsoleTestApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            UInt32 result;
            UInt32 num_devices;
            result = Logger.DDL1M12_GetNumDevices(out num_devices);
            if (result != Logger.DDL_SUCCESS)
                return;

            UInt32 serial_number_length = 50;
            StringBuilder serial_number = new StringBuilder((int)serial_number_length);
            result = Logger.DDL1M12_GetDeviceSerialNumber(0, serial_number, serial_number_length);
            if (result != Logger.DDL_SUCCESS)
                return;

            UInt32 handle;
            result = Logger.DDL1M12_OpenSpecifiedDevice(serial_number.ToString(), out handle);
            if (result != Logger.DDL_SUCCESS)
                return;

            /*
            result = Logger.DDL1M12_OpenDevice( ref handle);
            if( result != Logger.DDL_SUCCESS)
                return;
            */

            result = Logger.DDL1M12_InitDevice(handle);
            if (result != Logger.DDL_SUCCESS)
                return;

            Boolean is_programmed;
            result = Logger.DDL1M12_IsDeviceProgrammed(handle, out is_programmed);
            if (result != Logger.DDL_SUCCESS) {
                result = Logger.DDL1M12_ProgramDevice(handle, "C:\\DDL1M12.RBF");
                if (result != Logger.DDL_SUCCESS)
                    return;
            }

            // set up capture
            result = Logger.DDL1M12_SetDeviceChannelState(handle, true, true);
            if (result != Logger.DDL_SUCCESS)
                return;

            Logger.Ddl_Channel_Control_Data DualDataLoggerChannelAControlData = new Logger.Ddl_Channel_Control_Data();
            DualDataLoggerChannelAControlData.bGainTimesTen = false;
            DualDataLoggerChannelAControlData.dwGainuVBit = Logger.GAIN_200UV_PER_BIT;
            DualDataLoggerChannelAControlData.bCouplingState = true;
            DualDataLoggerChannelAControlData.bActiveState = true;

            Logger.Ddl_Channel_Control_Data DualDataLoggerChannelBControlData = new Logger.Ddl_Channel_Control_Data();
            DualDataLoggerChannelBControlData.bGainTimesTen = false;
            DualDataLoggerChannelBControlData.dwGainuVBit = Logger.GAIN_200UV_PER_BIT;
            DualDataLoggerChannelBControlData.bCouplingState = true;
            DualDataLoggerChannelBControlData.bActiveState = true;

            result = Logger.DDL1M12_StartDeviceDataLogging(handle, true, 100, DualDataLoggerChannelAControlData, DualDataLoggerChannelBControlData);
            if (result != Logger.DDL_SUCCESS)
                return;

            // capture a bunch of data
            UInt32 dwChannelANumValuesReturned, dwChannelBNumValuesReturned;
            UInt32[] ChannelADataLoggerBuffer = new UInt32[65536];
            UInt32[] ChannelBDataLoggerBuffer = new UInt32[65536];
            Boolean bChannelAValueMissed = false;
            Boolean bChannelBValueMissed = false;
            UInt32 dwLoopCntr = 0;
            do {
                result = Logger.DDL1M12_GetDeviceChannelData( handle, ChannelADataLoggerBuffer, out dwChannelANumValuesReturned, ChannelBDataLoggerBuffer, out dwChannelBNumValuesReturned);

                for( UInt32 dwBuffIndex = 0; (dwBuffIndex < dwChannelANumValuesReturned); dwBuffIndex++) {
                    if( ChannelADataLoggerBuffer[Logger.MISSING_VALUE_INDEX * dwChannelANumValuesReturned + dwBuffIndex] != 0)
                        bChannelAValueMissed = true;
                }

                for( UInt32 dwBuffIndex = 0; (dwBuffIndex < dwChannelBNumValuesReturned); dwBuffIndex++) {
                    if( ChannelBDataLoggerBuffer[Logger.MISSING_VALUE_INDEX * dwChannelANumValuesReturned + dwBuffIndex] != 0)
                        bChannelBValueMissed = true;
                }

                if( result == Logger.DDL_SUCCESS) {
                    Thread.Sleep(10);
                    // Process Data
                }

                dwLoopCntr++;
            } while( (dwLoopCntr < 100) && (result == Logger.DDL_SUCCESS));

            result = Logger.DDL1M12_StopDeviceDataLogging( handle, Logger.CHANNEL_A | Logger.CHANNEL_B);
            if( result != Logger.DDL_SUCCESS)
                return;

            result = Logger.DDL1M12_SetDeviceChannelState( handle, false, false);
            if( result != Logger.DDL_SUCCESS)
                return;

            // test function generator
            result = Logger.DDL1M12_SetDeviceFunctionGeneratorState( handle, true);
            if( result != Logger.DDL_SUCCESS)
                return;

            UInt32 dwFrequencyHz, dwMaxFreqRangeHz, dwMinFreqRangeHz;
            result = Logger.DDL1M12_GetWaveformFrequencyValues( Logger.WAVEFORM_SAWTOOTH, 100, 128, "C:\\MyWave1.csv", out dwFrequencyHz, out dwMaxFreqRangeHz, out dwMinFreqRangeHz);
            if( result != Logger.DDL_SUCCESS)
                return;

            result = Logger.DDL1M12_SetDeviceFunctionGeneratorWaveform( handle, Logger.WAVEFORM_SAWTOOTH, 100, 5000, 0, 128, 99, false, "C:\\MyWave1.csv");
            if( result != Logger.DDL_SUCCESS)
                return;

            result = Logger.DDL1M12_SetDeviceFunctionGeneratorState( handle, false);
            if( result != Logger.DDL_SUCCESS)
                return;

            StringBuilder error_message = new StringBuilder( 100);
            result = Logger.DDL1M12_GetErrorCodeString( "EN", result, error_message, 100);
            Console.WriteLine( String.Format( "Error string is: {0}", error_message.ToString()));

            StringBuilder version = new StringBuilder( 10);
            result = Logger.DDL1M12_GetDllVersion( version, 10);
            Console.WriteLine( String.Format( "Version number is: {0}", version.ToString()));
            
            // close the device, we're done
            result = Logger.DDL1M12_CloseDevice(handle);
            if (result != Logger.DDL_SUCCESS)
                return;

        }
    }
}
