using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using BioNex.Shared.TechnosoftLibrary;

namespace TMLLibCSharpConsoleTestApp
{
    class Program
    {
        static bool g_exit;
        static Random random = new Random();

        static void OpenPlateMover()
        {
            int cycle = 0;
            while (!g_exit)
            {
                int interval = random.Next(10);
                Console.WriteLine("Thread-1 start cycle: {0}", ++cycle);
                var channel = new TMLChannel();
                var result = channel.TS_OpenChannel("1", TMLLibConst.CHANNEL_SYS_TEC_USBCAN, 254, 500000);
                if (!result)
                    throw new Exception("error");
                Thread.Sleep(interval);

                // Y
                int _idxSetup_1 = channel.TS_LoadSetup("C:\\Engineering\\Systems\\Beta\\BB2-BU-10020-01\\12.t.zip");
                if (_idxSetup_1 == -1)
                    throw new Exception("error");
                Thread.Sleep(interval);

                if (!channel.TS_SetupAxis(12, _idxSetup_1))
                    throw new Exception("error");
                Thread.Sleep(interval);

                if (!channel.TS_SelectAxis(12))
                    throw new Exception("error");
                Thread.Sleep(interval);

                if (!channel.TS_SetIntVariable("homing_status", -1))
                    throw new Exception("error");
                Thread.Sleep(interval);

                // Theta
                int _idxSetup_2 = channel.TS_LoadSetup("C:\\Engineering\\Systems\\Beta\\BB2-BU-10020-01\\15.t.zip");
                if (_idxSetup_2 == -1)
                    throw new Exception("error");
                Thread.Sleep(interval);

                if (!channel.TS_SetupAxis(15, _idxSetup_2))
                    throw new Exception("error");
                Thread.Sleep(interval);

                if (!channel.TS_SelectAxis(15))
                    throw new Exception("error");
                Thread.Sleep(interval);

                if (!channel.TS_SetIntVariable("homing_status", -1))
                    throw new Exception("error");
                Thread.Sleep(interval);

                channel.TS_CloseChannel();
                Thread.Sleep(interval);
            }
        }

        static void OpenHive()
        {
            int cycle = 0;
            while (!g_exit)
            {
                int interval = random.Next(50);
                Console.WriteLine("Thread-2 start cycle: {0}", ++cycle);
                var channel = new TMLChannel();
                var result = channel.TS_OpenChannel("0", TMLLibConst.CHANNEL_SYS_TEC_USBCAN, 254, 500000);
                if (!result)
                    throw new Exception("error");
                Thread.Sleep(interval);

                // Robot X
                int _idxSetup_1 = channel.TS_LoadSetup("C:\\Engineering\\Systems\\Beta\\DC1-AU-09316-01\\1.t.zip");
                if (_idxSetup_1 == -1)
                    throw new Exception("error");
                Thread.Sleep(interval);

                if (!channel.TS_SetupAxis(1, _idxSetup_1))
                    throw new Exception("error");
                Thread.Sleep(interval);
                
                if (!channel.TS_SelectAxis(1))
                    throw new Exception("error");
                Thread.Sleep(interval);
                
                if (!channel.TS_SetIntVariable("homing_status", -1))
                    throw new Exception("error");
                Thread.Sleep(interval);

                // Robot Z -- don't move it!
                int _idxSetup_2 = channel.TS_LoadSetup("C:\\Engineering\\Systems\\Beta\\DC1-AU-09316-01\\3.t.zip");
                if (_idxSetup_2 == -1)
                    throw new Exception("error");
                Thread.Sleep(interval);

                if (!channel.TS_SetupAxis(3, _idxSetup_2))
                    throw new Exception("error");
                Thread.Sleep(interval);

                if (!channel.TS_SelectAxis(3))
                    throw new Exception("error");
                Thread.Sleep(interval);

                channel.TS_CloseChannel();
                Thread.Sleep(interval);
            }
        }
     
        static void ExitCheck()
        {
            while (!Console.KeyAvailable)
                Thread.Sleep(100);
            g_exit = true;
        }

        static void Main(string[] args)
        {
            g_exit = false;

            new Thread(ExitCheck).Start();
            Thread[] thread_handles = {
                new Thread(OpenPlateMover),
                new Thread(OpenHive)
            };
            foreach (var t in thread_handles)
                t.Start();
            foreach (var t in thread_handles)
                t.Join();
        }
    }
}
