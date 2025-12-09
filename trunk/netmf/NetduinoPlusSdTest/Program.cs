using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using Microsoft.SPOT.IO;

namespace NetduinoPlusSdTest
{
    public class Program
    {
        public static void Main()
        {
            // write your code here
            while( true) {
                try {

                    VolumeInfo volume = VolumeInfo.GetVolumes()[0];
                    Debug.Print( "Volume Name: " + volume.Name);
                } catch( Exception ex) {
                    Debug.Print( ex.Message);
                    Thread.Sleep( 5000);
                }
                Thread.Sleep( 500);
            }
        }

    }
}
