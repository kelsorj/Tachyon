using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Diagnostics;
//using SecretLabs.NETMF.Hardware;
//using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace Test
{
    public class Program
    {
        public static void Main()
        {
            bool use_netduino = false;

            // write your code here
            OutputPort valve = null;
            OutputPort probe = null;
            //SecretLabs.NETMF.Hardware.AnalogInput pot1 = null;
            GHIElectronics.NETMF.Hardware.AnalogIn pot2 = null;

            // set default output value to true since the brake board used for the valve is active low
            if( use_netduino) {
                valve = new OutputPort( SecretLabs.NETMF.Hardware.NetduinoPlus.Pins.GPIO_PIN_D0, true);
                probe = new OutputPort( SecretLabs.NETMF.Hardware.NetduinoPlus.Pins.GPIO_PIN_D1, true);
                //pot1 = new SecretLabs.NETMF.Hardware.AnalogInput( SecretLabs.NETMF.Hardware.NetduinoPlus.Pins.GPIO_PIN_A0);
            } else {
                valve = new OutputPort( (Cpu.Pin)GHIElectronics.NETMF.FEZ.FEZ_Pin.Digital.Di20, true);
                probe = new OutputPort( (Cpu.Pin)GHIElectronics.NETMF.FEZ.FEZ_Pin.Digital.Di21, true);
                pot2 = new GHIElectronics.NETMF.Hardware.AnalogIn( GHIElectronics.NETMF.Hardware.AnalogIn.Pin.Ain0);
            }

            // check output timing
            DigitalOutputWriteTiming( probe);

            bool stress_test = false;
            bool microsecond_test = true;

            if( microsecond_test) {
                Thread microsecond_thread = new Thread( () => MicrosecondThread(probe));
                microsecond_thread.Priority = ThreadPriority.Highest;
                microsecond_thread.Start();
            }

            if( stress_test) { // have main toggle a couple of DIOs, and read ADC
                while (true) {
                    int reading = 0;
                    if( use_netduino) {
                        //reading = pot1.Read();
                    } else {
                        reading = pot2.Read() / 10 + 1;
                    }

                    valve.Write(false);
                    if( !microsecond_test)
                        probe.Write(false);
                    Thread.Sleep( reading);
                    valve.Write(true);
                    if( !microsecond_test)
                        probe.Write(true);
                    Thread.Sleep( reading);

                    //Debug.Print( reading.ToString());
                }
            } else { // don't do anything in main but sleep
                while( true) {
                    Thread.Sleep( 0);
                }
            }
        }

        private static bool _output_state;

        public static void ToggleOutput( object output)
        {
            ((OutputPort)output).Write( _output_state);
            _output_state = !_output_state;
        }

        private static void MicrosecondThread( OutputPort probe)
        {
            bool state = true;
            while( true) {
                int delay_us = 1;
                int delay_ticks = delay_us * 10;
                DateTime start = DateTime.Now;
                probe.Write( state);
                state = !state;
                while( (DateTime.Now - start).Ticks < delay_ticks) {
                    Thread.Sleep( 1);
                }
            }
        }

        private static void DigitalOutputWriteTiming( OutputPort port)
        {
            int num_tries = 10000;
            DateTime start = DateTime.Now;
            for( int i=0; i<num_tries; i++) {
                port.Write( true);
            }
            TimeSpan duration = DateTime.Now - start;
            Debug.Print( "Setting digital output once takes " + (duration.Ticks) / num_tries + " ticks");
        }
    }
}
