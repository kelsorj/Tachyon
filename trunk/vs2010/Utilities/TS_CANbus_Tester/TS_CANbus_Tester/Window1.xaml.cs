using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using BioNex.Shared.TechnosoftLibrary;

namespace TS_CANbus_Tester
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]
        private static extern uint TimeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint="timeEndPeriod", SetLastError=true)]
        private static extern uint TimeEndPeriod(uint uMilliseconds);

        [StructLayout(LayoutKind.Sequential)]
        public struct TimeCaps
        {       
            public int periodMin;       
            public int periodMax;
        }

        [DllImport("winmm.dll", SetLastError=true)]
        static extern UInt32 timeGetDevCaps( ref TimeCaps timeCaps, int sizeTimeCaps );

        // uint DesiredResolution: Resolution to set in 100ns units. To receive minimum and maximum resolution values, call NtQueryTimerResolution.
        // bool SetResolution: If set, system Timer's resolution is set to DesiredResolution value. If no, parameter DesiredResolution is ignored. 
        // ref uint CurrentResolution:  Pointer to ULONG value receiving current timer's resolution, in 100-ns units. 
        [DllImport("ntdll.dll", EntryPoint = "NtSetTimerResolution")]
        public static extern void NtSetTimerResolution(uint DesiredResolution, bool SetResolution, ref uint CurrentResolution);

        // ref uint MinimumResolution: Means highest possible delay (in 100-ns units) between timer events.
        // ref uint MaximumResolution: Means lowest possible delay (in 100-ns units) between timer events.
        // ref uint CurrentResolution: Current timer resolution (in 100-ns units) between timer events.
        [DllImport("ntdll.dll", EntryPoint = "NtQueryTimerResolution")]
        public static extern void NtQueryTimerResolution(ref uint MinimumResolution, ref uint MaximumResolution, ref uint CurrentResolution);

        public Window1()
        {
            TimeCaps tc = new TimeCaps();
            UInt32 mm_result = timeGetDevCaps(ref tc, Marshal.SizeOf(tc));
            Console.WriteLine(String.Format("timeGetDevCaps returned {0}, periodMin={1}, periodMax={2}", mm_result, tc.periodMin, tc.periodMax));

            uint MinimumResolution = 0;
            uint MaximumResolution = 0;
            uint CurrentResolution = 0;
            NtQueryTimerResolution(ref MinimumResolution, ref MaximumResolution, ref CurrentResolution);
            Console.WriteLine(String.Format("NtQueryTimerResolution returned: MinRes={0:0.000}ms, MaxRes={1:0.000}ms, CurrentRes={2:0.000}ms",
                (double)MinimumResolution/10000.0, (double)MaximumResolution/10000.0, (double)CurrentResolution/10000.0));

            TimeBeginPeriod(1);  // set timer resolution to 1ms to get max performace from Sleep and USB->CANbus calls

            uint DesiredResolution = MaximumResolution; // requests max resolution, which should set it to 0.5ms on Win7 and 1ms on WinXP
            bool SetResolution = true;
            NtSetTimerResolution(DesiredResolution, SetResolution, ref CurrentResolution);
            Console.WriteLine(String.Format("NtSetTimerResolution({0},...) returned CurrentResolution={1:0.000}ms", DesiredResolution, (double)CurrentResolution/10000.0));


            InitializeComponent();
            MaxResLabel.Content = String.Format("{0:0.000} ms", (double)MaximumResolution / 10000.0);
            CurResLabel.Content = String.Format("{0:0.000} ms", (double)CurrentResolution / 10000.0);
            SystecDeviceTextBox.Text = String.Format("0");
            TSAxisTextBox.Text = String.Format("1");
            NumMessagesTextBox.Text = String.Format("100");
        }

         ~Window1()
        {
            TimeEndPeriod(1);
        }

        private void RunTestButton_Click(object sender, RoutedEventArgs e)
        {
            ElapsedTimeMsTextBox.Text = String.Format("");
            ElapsedTimeMsTextBox.UpdateLayout();

            MsgPerSecTextBox.Text = String.Format("");
            MsgPerSecTextBox.UpdateLayout();

            bool tml_ok = false;
            int axis_idx = -1;
            var channel = new TMLChannel();

            try
            {
                int systec_device = Int32.Parse(SystecDeviceTextBox.Text);
                int ts_axis = Int32.Parse(TSAxisTextBox.Text);
                int num_msgs = Int32.Parse(NumMessagesTextBox.Text);

                tml_ok = channel.TS_OpenChannel(SystecDeviceTextBox.Text, TMLLibConst.CHANNEL_SYS_TEC_USBCAN, 254, 500000);
                if (!tml_ok)
                    throw new Exception();
                axis_idx = channel.TS_LoadSetup("1.t.zip");
                if (axis_idx < 0)
                    throw new Exception();
                if (!channel.TS_SetupAxis((byte)ts_axis, axis_idx))
                {
                    axis_idx = -1; // so we don't try to ask for last error
                    throw new Exception();
                }
                if (!channel.TS_SelectAxis((byte)ts_axis))
                    throw new Exception();

                //TMLComm.MSK_RegisterDebugLogHandler( TMLComm.LOG_TRAFFIC, LogTMLMessage);
                ushort status;
                DateTime start = DateTime.Now;

                for (int n = 0; n < num_msgs; ++n)
                {
                    if (!channel.TS_ReadStatus(TMLLibConst.REG_MER, out status))
                    {
                        Console.WriteLine("Failed to send/receive Status, #{0}", n + 1);
                        throw new Exception();
                    }
                }

                TimeSpan ts = DateTime.Now - start;

                ElapsedTimeMsTextBox.Text = String.Format("{0:0.0000}", ts.TotalMilliseconds);
                MsgPerSecTextBox.Text = String.Format("{0}", (Int32)(num_msgs / (ts.TotalMilliseconds / 1000.0)));

                channel.TS_CloseChannel();
                tml_ok = false;
                axis_idx = -1;
            }
            catch
            {
                String err;
                if (tml_ok && axis_idx >= 0)
                {
                    StringBuilder sb = new StringBuilder(360);
                    TMLChannel.TS_Basic_GetLastErrorText(sb, 360);
                    err = "TML " + sb.ToString();
                }
                else
                {
                    err = "Unknown reason";
                }
                MessageBox.Show(String.Format("Something Failed ({0}). Try again", err));
            }
                finally
            {
                if (tml_ok)
                    channel.TS_CloseChannel();
            }
        }

        private void LogTMLMessage( String message)
        {
            Console.Write( message);
        }
    }
}
