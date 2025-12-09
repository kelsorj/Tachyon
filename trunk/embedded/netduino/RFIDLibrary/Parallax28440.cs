using System;
using Microsoft.SPOT;
using SecretLabs.NETMF.Hardware.Netduino;
using System.IO.Ports;
using System.Threading;

namespace BioNex.NETMF
{
    public class Parallax28440 : IDisposable
    {
        SerialPortWrapper Port { get; set; }
        AutoResetEvent StopUpdating { get; set; }
        Thread UpdateThread { get; set; }

        public Parallax28440( string port)
        {
            StopUpdating = new AutoResetEvent( false);
            Port = new SerialPortWrapper( port, 9600);
            UpdateThread = new Thread( UpdateThreadRunner);
            UpdateThread.Start();
        }

        private void UpdateThreadRunner()
        {
            // exitcontext is ignored: http://www.netmf.com/Discussion/Forums/SingleForum/SingleThread.aspx?mode=singleThread&thread=663efda4-e927-422b-97c4-02ad11c65cde
            while( !StopUpdating.WaitOne( 50, false)) {
                Port.Write( "!RW\x01\x02");
                try {
                    Debug.Print( Port.ReadChars( 5));   // expect 5 characters back from RFID reader
                } catch( Exception ex) {
                    // ignore error, just try again
                }
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            StopUpdating.Set();
            UpdateThread.Join();            
            Port.Dispose();
        }

        #endregion
    }
}
