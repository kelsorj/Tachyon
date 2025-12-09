using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using TML;

namespace PX1_Theta_Tester
{
    class Program
    {
        public static Boolean done = false;
        
        public static void Main(string[] args)
        {
            byte axisID = 5;

            if (1 != args.Length)
            {
                System.Console.WriteLine("Defaulting to AxisID = " + axisID.ToString());
            } else {
                axisID = byte.Parse(args[0]);
                System.Console.WriteLine("Using AxisID = " + axisID.ToString());
            }

            if (axisID < 1 || axisID > 255)
            {
                System.Console.WriteLine("Please enter an axisID between 1 and 255 as the first argument.");
                return;
            }

            // Create and open logfile first
            StreamWriter logFile;
            String directory = @"C:\Engineering\logs";
            String pathname;

            if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
            pathname = String.Format(@"{0}\{1}-px1_theta_revs_axis{2}.txt", directory, DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss"), axisID);
            logFile = File.CreateText(pathname);
            logFile.AutoFlush = true;
            Console.WriteLine(String.Format("Created logfile {0}", pathname));

            Console.TreatControlCAsInput = false;
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CtrlCHandler);

            //int fd = TMLLib.TS_OpenChannel("1", TMLLib.CHANNEL_IXXAT_CAN, 254, 500000);
            int fd = TMLLib.TS_OpenChannel("1", TMLLib.CHANNEL_SYS_TEC_USBCAN, 254, 500000);
            if (fd <= 0) {
                Console.WriteLine(String.Format("TS_OpenChannel returned {0}", fd));
                goto cleanup;
            }
            Console.WriteLine("OpenChannel Success");

            int ret = TMLLib.TS_LoadSetup(axisID.ToString() + ".t.zip");
            if (ret < 0) {
                Console.WriteLine(String.Format("TS_LoadSetup returned {0}", ret));
                goto cleanup;
            }
            Console.WriteLine("Loaded Setup " + axisID.ToString() + ".t.zip Success");

            if (!TMLLib.TS_SetupAxis(axisID, ret)) {
                Console.WriteLine(String.Format("TS_SetupAxis({0}) returned false", axisID));
                goto cleanup;
            }
            Console.WriteLine("SetupAxis " + axisID.ToString() + " Success");

            TMLLib.TS_SelectAxis(axisID);
            if (!TMLLib.TS_Reset()) // reset axis
            {
                Console.WriteLine(String.Format("TS_Reset() returned false", axisID));
                goto cleanup;
            }

            Thread.Sleep(2000);

            short homing_status = -1;
            Console.WriteLine("Calling homeaxis");
            if (!TMLLib.TS_CALL_Label("func_homeaxis"))
            {
                Console.WriteLine("TS_CALL_Label(\"func_homeaxis\") returned false");
                goto cleanup;
            }

            if (!TMLLib.TS_GetIntVariable("homing_status", out homing_status))
            {
                Console.WriteLine("TS_GetIntVariable(\"homing_status\") returned false");
                goto cleanup;
            }

            while (!done)
            {
                int APOS;
                TMLLib.TS_GetIntVariable("homing_status", out homing_status);
                TMLLib.TS_GetLongVariable("APOS", out APOS);
                Console.WriteLine(String.Format("homing_status={0}, APOS={1}", homing_status, APOS));
                if (0 == homing_status)
                    break;
                Thread.Sleep(250);
            }

            DateTime t1 = DateTime.Now;
            TimeSpan ts;
            int cycle = 0;
            int func_done = 0;
            TMLLib.TS_CALL_Label("spin_1rev");
            while (!done)
            {
                String str;
                ts = DateTime.Now - t1;
                if (ts.Seconds >= 10) {
                    t1 = DateTime.Now;
                    Console.WriteLine(t1.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                }
                TMLLib.TS_GetLongVariable("func_done", out func_done);
                if (1 == func_done)
                {
                    cycle++;
                    int CAPPOS = 0;
                    TMLLib.TS_GetLongVariable("CAPPOS", out CAPPOS);
                    str = String.Format("{0}, {1}", cycle, CAPPOS);
                    Console.WriteLine(str);
                    logFile.WriteLine(str);
                    func_done = 0;
                    TMLLib.TS_CALL_Label("spin_1rev");
                }

                Thread.Sleep(100);
            }

          cleanup:
            Console.WriteLine("\nCleaning up...");
            TMLLib.TS_CloseChannel(fd);
            logFile.Close();
            Thread.Sleep(1000);
        } // Main

        protected static void CtrlCHandler(object sender, ConsoleCancelEventArgs args)
        {
            // Announce that the event handler has been invoked.
            Console.WriteLine("\nThe operation has been interrupted.");

            // Set the Cancel property to true to prevent the process from terminating.
            args.Cancel = true;

            done = true;
        } // CtrlCHandler

    }
}
