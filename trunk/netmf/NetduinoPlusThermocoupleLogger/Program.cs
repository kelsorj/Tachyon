using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace NetduinoPlusThermocoupleLogger
{
    public class Program
    {
        public static void Main()
        {
            // write your code here
            McLaughlinQuadThermocoupleShield.QuadThermocouple thermocouples = new McLaughlinQuadThermocoupleShield.QuadThermocouple( );
            while( true) {
                System.Collections.ArrayList readings = thermocouples.GetReadings();
                for( int i=0; i<readings.Count; i++) {
                    McLaughlinQuadThermocoupleShield.QuadThermocouple.ThermocoupleReading reading = (McLaughlinQuadThermocoupleShield.QuadThermocouple.ThermocoupleReading)readings[i];
                    Debug.Print( "Thermocouple #" + reading.Index.ToString() + ": " + reading.Temperature.ToString() + "[" + (reading.Open ? " open " : "") +
                                 (reading.ShortedToGnd ? " shorted to gnd " : "") + (reading.ShortedToVcc ? " shorted to vcc " : "") + "]");
                }
            }
        }
    }
}
