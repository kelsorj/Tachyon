using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using UcanDotNET;
using CommandLine;


namespace HiveRobotLogger
{
    class Program
    {
        static LogFile logFile;
        static CommandLineParser settings;
        static USBcanServer USBCan;
        static DateTime start_logging; // time that logging started
        static int num_cycles_logging; // number of logging cycles
        static bool quit = false; // quit program
        static CanLogger can_logger;


        static string[] valid_settings = {
                                             "test_cycles"
                                             ,"period"
                                             ,"host_id"
                                             ,"id"
                                             ,"single_axis"
                                             ,"variables"
                                             ,"filename"
                                             ,"outdir"
                                             ,"device_num"
                                             ,"debug"                                             
                                             ,"wait"
                                             ,"convert"
                                             ,"poll"
                                         };
        
        [System.Runtime.InteropServices.DllImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]
        private static extern uint TimeBeginPeriod(uint uMilliseconds);

        [System.Runtime.InteropServices.DllImport("winmm.dll", EntryPoint="timeEndPeriod", SetLastError=true)]
        private static extern uint TimeEndPeriod(uint uMilliseconds);

        static void Main(string[] args)
        {
            TimeBeginPeriod(1); // set timer resolution to 1ms to get max performace from Sleep and USB->CANbus calls

            Debug.Listeners.Add(new TextWriterTraceListener(System.Console.Out));

            Console.TreatControlCAsInput = false;
            Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e)
            {
                // Set the Cancel property to true to prevent the process from terminating.
                e.Cancel = true;
                quit = true;
                //CleanupOnExit(true);
            };

            // get some basic stuff from the command line
            settings = new CommandLineParser(args);

            if (settings["help"] != null || settings["?"] != null)
                ShowUsage();

            foreach (System.Collections.DictionaryEntry key in settings)
                if (!valid_settings.Contains(key.Key))
                {
                    Console.WriteLine("Unknown command line argument: {0}\n\n", key.Key);
                    ShowUsage();
                }

            int cycles = int.Parse(settings["test_cycles"] ?? "0");
            if (cycles <= 0)
            {
                cycles = Int32.MaxValue;
            }
            int period = int.Parse(settings["period"] ?? "25");
            byte host_id = byte.Parse(settings["host_id"] ?? "253");
            byte id = byte.Parse(settings["id"] ?? "23");
            bool single_axis = (settings["single_axis"] ?? "1") == "1";
            string[] variables = (settings["variables"] ?? "APOS,IQ,IQREF,ASPD,POSERR,AD4,AD5,AD7,CACC,CSPD,UQREF,UDREF,ATIME").Split(',');
            string filename = settings["filename"] ?? "BioNexLog";
            string outdir = settings["outdir"] ?? @"C:\Engineering\Logs\";
            byte device_num = byte.Parse(settings["device_num"] ?? "1");
            bool log_debug = (settings["debug"] ?? "0") == "1";
            bool wait = (settings["wait"] ?? "0") == "1";
            bool convert = (settings["convert"] ?? "0") == "1";
            bool poll = (settings["poll"] ?? "1") == "1";

            logFile = new LogFile(filename, outdir, log_debug);
            try
            {
                USBCan = new USBcanServer();
                can_logger = new CanLogger(USBCan);
                can_logger.LogVariables(variables, host_id, id, single_axis, cycles, period, device_num, logFile, out start_logging, out num_cycles_logging, ref quit, convert, poll);
            }
            finally
            {
                CleanupOnExit(wait);

                // just sit here and spin while CleanupOnExit takes care of the final keypress to exit
                while (true)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        static void CleanupOnExit(bool wait)
        {
            quit = true; // set this flag to true to get the logger to stop since this function is spawned as a thread with ctrl-c

            TimeSpan logging_span = DateTime.Now-start_logging;
            double log_cycles_per_sec = num_cycles_logging/logging_span.TotalSeconds;
            Console.WriteLine(String.Format("Total logging runtime: {0} seconds", logging_span.TotalSeconds));
            Console.WriteLine(String.Format("Total number of logging cycles: {0}", num_cycles_logging));
            Console.WriteLine(String.Format("Logging cycles / second: {0:0.000}", log_cycles_per_sec));
            Console.WriteLine(String.Format("Average logging cycle interval: {0:0} msecs", 1000.0/log_cycles_per_sec));

            TimeEndPeriod(1); // reset timer resolution back to default from 1ms to be nice to Windows

            byte ret = USBCan.Shutdown();
            if (ret != USBcanServer.USBCAN_SUCCESSFUL)
            {
                Debug.WriteLine(String.Format("Error: Shutdown returned {0}", ret));
            }

            logFile.CloseLogFile();
            can_logger.KillConsoleThread();

            if (wait)
            {
                Console.WriteLine("\n<press a key to exit>");
                while (Console.KeyAvailable)
                {
                    Console.ReadKey(); // flush ctrl-c character(s), if they exist in the buffer
                }
                while (!Console.KeyAvailable) Thread.Sleep(100);
            }
            Console.WriteLine("Have a super day!");
            Environment.Exit(0);
        }

        static void ShowUsage()
        {
            Console.WriteLine("Quick And Dirty Data Capture Usage:");
            Console.WriteLine("--test_cycles N : number of points to capture. 0 means until ctrl+c (0)");
            Console.WriteLine("--period N : rest period between cycles in milliseconds (25)");
            Console.WriteLine("--variables N : what variable to capture (APOS,IQ,IQREF,ASPD,POSERR,AD4,AD5,AD7,CSPD,CACC,UQREF,UDREF,ATIME)");
            Console.WriteLine("--host_id N : what host id 1-255 (253) ");
            Console.WriteLine("--id N : what axis or group id 1-255 | 1-8 (23) ");
            Console.WriteLine("--single_axis 0/1 : broadcast to group or single axis (1)");
            Console.WriteLine("--filename N : what filename to write data to (BioNexLog)");
            Console.WriteLine("--outdir N : where to write the output file (C:\\Engineering\\Logs\\)");
            Console.WriteLine("--device_num N : what usb can device number (1)");
            Console.WriteLine("--debug 0/1 : whether to log debug messages (0)");
            Console.WriteLine("--wait 0/1 : whether to wait for key to exit console (0)");
            Console.WriteLine("--convert 0/1 : whether to convert iu to eng units (0)");
            Console.WriteLine("--poll 0/1 : whether to ask for data to transmit (1)");
            Console.WriteLine();
            Console.WriteLine("<press a key to exit>");
            while (!Console.KeyAvailable) Thread.Sleep(100);
            Environment.Exit(0);
        }
    }


    class CanLogger
    {
        
        private USBcanServer InitializeHardware(byte device_num)
        {
            int dll_ver = USBCan.GetUserDllVersion();
            Debug.WriteLine(String.Format("User Dll: Ver={0}, Rev={1}, Release={2}", dll_ver & 0xff, (dll_ver & 0xff00) >> 8, (dll_ver & 0xff0000) >> 16));

            byte ret = USBCan.InitHardware(device_num);
            Debug.WriteLine(String.Format("InitHardware returned {0}", ret));
            if (ret != USBcanServer.USBCAN_SUCCESSFUL)
            {
                Debug.WriteLine(String.Format("Error: InitHardware returned {0}", ret));
            }

            ret = USBCan.InitCan(USBcanServer.USBCAN_CHANNEL_CH0, USBcanServer.USBCAN_BAUD_500kBit, USBcanServer.USBCAN_BAUDEX_USE_BTR01,
                    USBcanServer.USBCAN_AMR_ALL, USBcanServer.USBCAN_ACR_ALL, (byte)USBcanServer.tUcanMode.kUcanModeNormal,
                    USBcanServer.USBCAN_OCR_DEFAULT);
            Debug.WriteLine(String.Format("InitCan returned {0}", ret));
            if (ret != USBcanServer.USBCAN_SUCCESSFUL)
            {
                Debug.WriteLine(String.Format("Error: InitCan returned {0}", ret));
            }

            int fw_ver = USBCan.GetFwVersion();
            Debug.WriteLine(String.Format("Firmware: Ver={0}, Rev={1}, Release={2}", fw_ver & 0xff, (fw_ver & 0xff00) >> 8, (fw_ver & 0xff0000) >> 16));

            var HwInfo = new USBcanServer.tUcanHardwareInfoEx();
            var CanInfoCh0 = new USBcanServer.tUcanChannelInfo();
            var CanInfoCh1 = new USBcanServer.tUcanChannelInfo();

            ret = USBCan.GetHardwareInfo(ref HwInfo, ref CanInfoCh0, ref CanInfoCh1);
            if (ret != USBcanServer.USBCAN_SUCCESSFUL)
            {
                Debug.WriteLine(String.Format("Error: GetHardwareInfo returned {0}", ret));
            }
            else
            {
                Debug.WriteLine(String.Format("S/N: {0}, Device# {1}, F/W: {2}, ProdCode: {3}, Size={4}",
                    HwInfo.m_dwSerialNr, HwInfo.m_bDeviceNr, HwInfo.m_dwFwVersionEx, HwInfo.m_dwProductCode, HwInfo.m_dwSize));
                Debug.WriteLine(String.Format("Ch0: Init={0}, CANstatus={1}", CanInfoCh0.m_fCanIsInit, CanInfoCh0.m_wCanStatus));
                Debug.WriteLine(String.Format("Ch1: Init={0}, CANstatus={1}", CanInfoCh1.m_fCanIsInit, CanInfoCh1.m_wCanStatus));
            }
            return USBCan;
        }

        // COMMAND ADDRESSES
        // LONG  APOS -- 0x0228
        // INT     IQ -- 0x0230
        // INT  IQREF -- 0x022F
        // FIXED ASPD -- 0x022C
        // INT POSERR -- 0x022A
        // INT SPDERR -- 0x022E
        // UINT   AD4 -- 0x0240 (AD4 is the VBUS)
        // UINT   AD5 -- 0x0241 (AD5 is the External Reference Analog Input)
        // UINT   AD7 -- 0x0243 (AD7 is the Drive Temperature Analog Input)
        // FIXED CSPD -- 0x02A0
        // FIXED CACC -- 0x02A2
        // e.g. ??APOS from 254 to broadcast ==> 16600005 E0 0F 28 02
        // replies to ??APOS should be something like  1A9FC1nn 28 02 ww xx yy zz (from axis ID nn)

        // TODO -- use a control file for these arrays to expand capabilities

        List<string> command_list;
        string[] commands = 
        {
             "APOS"
            ,"ASPD"
            ,"IQ"
            ,"IQREF"
            ,"POSERR"
            ,"SPDERR"
            ,"AD4"
            ,"AD5"
            ,"AD7"
            ,"CSPD"
            ,"CACC"
            ,"UQREF"
            ,"UDREF"
            ,"ATIME"
            ,"MER"
            ,"SRH"
            ,"SRL"
            ,"CER"
        };


        UInt16[] opcodes =  // opcodes in correct endianness for TML, reverse engineered rather than read from source, so could be wrong?
        {
             0xB205 // APOS -- LONG
            ,0xB205 // ASPD -- FIXED
            ,0xB204 // IQ -- INT
            ,0xB204 // IQREF -- INT
            ,0xB204 // POSERR -- INT
            ,0xB204 // SPDERR -- INT
            ,0xB204 // AD4 -- UINT
            ,0xB204 // AD5 -- UINT
            ,0xB204 // AD7 -- UINT
            ,0xB205 // CSPD -- FIXED
            ,0xB205 // CACC -- FIXED
            ,0xB204 // UQREF -- INT
            ,0xB204 // UDREF -- INT        
            ,0xB205 // ATIME -- LONG
            ,0xB204 // MER -- UINT
            ,0xB204 // SRH -- UINT
            ,0xB204 // SRL -- UINT
            ,0xB204 // CER -- UINT
        };

        System.Type[] types =
        {
             typeof(Int32) // APOS -- LONG
            ,typeof(Int32) // ASPD -- FIXED
            ,typeof(Int16) // IQ -- INT
            ,typeof(Int16) // IQREF -- INT
            ,typeof(Int16) // POSERR -- INT
            ,typeof(Int16) // SPDERR -- INT
            ,typeof(UInt16) // AD4 -- UINT
            ,typeof(UInt16) // AD5 -- UINT
            ,typeof(UInt16) // AD7 -- UINT
            ,typeof(Int32) // CSPD -- FIXED
            ,typeof(Int32) // CACC -- FIXED
            ,typeof(Int16) // UQREF -- INT
            ,typeof(Int16) // UDREF -- INT
            ,typeof(Int32) // ATIME -- LONG
            ,typeof(UInt16) // MER -- UINT
            ,typeof(UInt16) // SRH -- UINT
            ,typeof(UInt16) // SRL -- UINT
            ,typeof(UInt16) // CER -- UINT
        };

        UInt16[] addresses =
        {
             0x0228 // APOS
            ,0x022C // ASPD
            ,0x0230 // IQ
            ,0x022F // IQREF
            ,0x022A // POSERR
            ,0x022E // SPDERR
            ,0x0240 // AD4
            ,0x0241 // AD5
            ,0x0243 // AD7
            ,0x02A0 // CSPD
            ,0x02A2 // CACC
            ,0x0232 // UQREF
            ,0x0235 // UDREF
            ,0x02C0 // ATIME
            ,0x08FC // MER
            ,0x090F // SRH
            ,0x090E // SRL
            ,0x0301 // CER
        };

        USBcanServer USBCan;

        public CanLogger(USBcanServer server)
        {
            USBCan = server;
        }

        private BlockingCollection<string> m_Queue;
        private Thread consoleThread;
        public void KillConsoleThread() {
            if (consoleThread != null)
            {
                consoleThread.Abort();
                while (consoleThread.IsAlive)
                {
                    Thread.Sleep(1);
                }
            }
        }

        private void NonBlockingConsole()
        {
            m_Queue = new BlockingCollection<string>();
            consoleThread = new Thread(
            () =>
            {
                while (true)
                {
                    Console.WriteLine(m_Queue.Take());
                }
            });
            consoleThread.IsBackground = true;
            consoleThread.Start();
        }

        public void LogVariables(string[] variables, byte host_id, byte id, bool single_axis, int num_cycles, int ms_sleep, byte device_num, LogFile logFile,
                                 out DateTime start_logging, out int num_cycles_logging, ref bool quit, bool convert, bool poll)
        {
            start_logging = DateTime.Now; // ignore this
            num_cycles_logging = 0;
            command_list = commands.ToList();
            foreach(var variable in variables)
                if( !command_list.Contains(variable))
                {
                    Debug.WriteLine("Unknown variable : {0} please check and try again", variable);
                    return;
                }

            var USBCan = InitializeHardware(device_num);

            // initialize send message buffer
            var can_msgs_send = new USBcanServer.tCanMsgStruct[1];
            can_msgs_send[0] = USBcanServer.tCanMsgStruct.CreateInstance(0, 0);
          
            // initialize receive message buffers
            const int can_msgs_max = 255;
            var can_msgs_recv = new USBcanServer.tCanMsgStruct[can_msgs_max];
            for (int n = 0; n < can_msgs_max; ++n)
            {
                can_msgs_recv[n] = USBcanServer.tCanMsgStruct.CreateInstance(0, 0);
            }

#if FALSE
            // Discovery :
            FillSendMessageStruct(ref can_msgs_send[0], "APOS", host_id, single_axis, id);
            SendCanMessage(ref can_msgs_send, USBCan);
#endif
            // Discovery :
            // Init can send messages based on variable count
            can_msgs_send = new USBcanServer.tCanMsgStruct[variables.Length];
            for (int i = 0; i < variables.Length; ++i)
            {
                can_msgs_send[i] = USBcanServer.tCanMsgStruct.CreateInstance(0, 0);
                FillSendMessageStruct(ref can_msgs_send[i], variables[i], host_id, single_axis, id);
            }
            SendCanMessage(ref can_msgs_send, USBCan);


            var axis_list = new HashSet<Int32>();
            ReceiveCanMessages(can_msgs_max, ref can_msgs_recv, USBCan, logFile, variables, true, ref axis_list, convert, poll);
            
            if(axis_list.Count == 0)
            {
                Debug.WriteLine("No devices responding on CAN network");
                return;
            }else
                Debug.WriteLine(string.Format("Found {0} devices on CAN", axis_list.Count));

            // LogFile header based on discovery : 
            // -- TODO -- base logfile header off of initial discovery 
            string line = "TIMESTAMP_S,TIMESTAMP_DATE,(DATE) DATE,(TIME) TIME";
            //,(AXIS) X iu,(AXIS) X mm,(AXIS) Z iu,(AXIS) Z mm,(AXIS) T iu,(AXIS) T deg,(CALC) Y mm, (AXIS) G iu,(AXIS) G mm"
            foreach (var axis in axis_list)
                foreach(var variable in variables)
                    line += string.Format(",(Axis {0}) {1}", axis, variable);
            logFile.WriteLine(line, false);

#if FALSE
            // Re-init can send messages based on variable count
            can_msgs_send = new USBcanServer.tCanMsgStruct[variables.Length];
            for (int i = 0; i < variables.Length; ++i )
            {
                can_msgs_send[i] = USBcanServer.tCanMsgStruct.CreateInstance(0, 0);
                FillSendMessageStruct(ref can_msgs_send[i], variables[i], host_id, single_axis, id);
            }
#endif

            NonBlockingConsole(); // get the non-blocking console stuff initialized
            start_logging = DateTime.Now;
            DateTime start_loop;
            DateTime end_loop;
            DateTime last_report = start_logging;
            double avg_rate = 1000.0/ms_sleep; // theoretical rate
            int num_cycles_at_last_second = 0;
            for (int cycle_num = 0; cycle_num < num_cycles && !quit; ++cycle_num)
            {
                start_loop = DateTime.Now;

                if (poll)
                {
                    SendCanMessage(ref can_msgs_send, USBCan);
                }
                ReceiveCanMessages(can_msgs_max, ref can_msgs_recv, USBCan, logFile, variables, false, ref axis_list, convert, poll);
                ++num_cycles_logging;

                end_loop = DateTime.Now;
                if ((end_loop - last_report).TotalSeconds >= 1.0)
                {
                    last_report = end_loop;
                    avg_rate = 0.1*avg_rate + 0.9*(num_cycles_logging-num_cycles_at_last_second);
                    // print out some logging stats every second
                    m_Queue.Add(String.Format("Logged {0} cycles so far ({1} cycles to go). Average rate of {2:0.0} cycles/second recently and {3:0.0} cycles/second since start", 
                        num_cycles_logging, num_cycles-num_cycles_logging, avg_rate, num_cycles_logging/(end_loop-start_logging).TotalSeconds));
                    logFile.Flush(); // now is a good time to flush stream to disk so we can watch it in baretail
                    num_cycles_at_last_second = num_cycles_logging;
                }
                if (poll && (end_loop-start_loop).TotalMilliseconds >= ms_sleep)
                {
                    m_Queue.Add(String.Format("Not sleeping this cycle since this cycle took {0} ms which is more than our interval time of {1} ms", (end_loop - start_loop).TotalMilliseconds, ms_sleep));
                } else {
                    // check to see if we should pinch off the log and increment filename
                    char keypress;
                    if (Console.KeyAvailable)
                    {
                        keypress = Console.ReadKey(true).KeyChar;

                        if (keypress == ' ')
                        {
                            logFile.PinchOffLogAndCreateNewLog();
                            logFile.WriteLine(line, false);
                        }
                        else if (keypress == 'q' || keypress == 'Q')
                        {
                            quit = true;
                        }
                    }
                    if (poll)
                    {
                        // sleep for the remaining time if polling
                        Thread.Sleep((int)(ms_sleep - (end_loop - start_loop).TotalMilliseconds));
                    }
                }
            }
        }

        private string address_to_command(UInt16 address)
        {
            int index = addresses.ToList().IndexOf(address);
            return commands[index];
        }

        private void FillSendMessageStruct(ref USBcanServer.tCanMsgStruct can_msgs_send, string variable, byte host_id, bool single_axis, byte id)
        {
            // send message format:
            // 3 3 2 2 2 2 2 2 | 2 2 2 2 1 1 1 1 | 1 1 1 1 1 1 0 0 | 0 0 0 0 0 0 0 0  
            // 1 0 9 8 7 6 5 4 | 3 2 1 0 9 8 7 6 | 5 4 3 2 1 0 9 8 | 7 6 5 4 3 2 1 0
            // ----------------|-----------------|-----------------|----------------
            // 7 6 5 4 3 2 1 0 | 7 6 5 4 3 2 1 0 | 7 6 5 4 3 2 1 0 | 7 6 5 4 3 2 1 0
            // 0 0 0 A A A A A | A A B C C C C C | C C C 0 0 0 D E | E E E E E E E E 
            // 
            // A: OPCODE Part 1 (7 MSB of OPCODE)
            // B: Group bit
            // C: Axis or Group ID
            // D: Host bit
            // E: OPCODE Part 2 (9 LSB of OPCODE)

            int index = command_list.IndexOf(variable);
            UInt16 opcode = opcodes[index];
            Int32 header = (opcode & 0xFE00) << 13
                | (single_axis ? 0 : 1) << 21
                | (single_axis ? id : (1 << (id - 1))) << 13
                | (0) << 9 // host bit -- always zero i think
                | (opcode & 0x01FF);
            UInt16 address = addresses[index];

            can_msgs_send.m_dwID = header; //0x16600005; // GiveMeData2 Long, axis id or group id encoded here
            can_msgs_send.m_bFF = USBcanServer.USBCAN_MSG_FF_EXT;
            can_msgs_send.m_bDLC = 4;
            can_msgs_send.m_bData[0] = (byte)(host_id << 4);  // host low byte
            can_msgs_send.m_bData[1] = (byte)(host_id >> 4);  // host hi byte (x0fd0 -- 253)
            can_msgs_send.m_bData[2] = (byte)(address & 0x00FF);  // command address low byte
            can_msgs_send.m_bData[3] = (byte)((address & 0xFF00) >> 8);  // command address hi byte

        }

        private byte SendCanMessage(ref USBcanServer.tCanMsgStruct[] can_msgs_sending, USBcanServer USBCan)
        {
            byte ret;
            int dwCount_send = 1;
            ret = USBCan.WriteCanMsg(USBcanServer.USBCAN_CHANNEL_CH0, ref can_msgs_sending, ref dwCount_send);
            //Debug.WriteLine(String.Format("WriteCanMsg returned {0}, dwCount_send={1}", ret, dwCount_send));
            if (ret != USBcanServer.USBCAN_SUCCESSFUL || dwCount_send < can_msgs_sending.Length)
            {
                Debug.WriteLine(String.Format("Error: WriteCanMsg returned {0}, dwCount_send={1}", ret, dwCount_send));
            }
            return ret;
        }

        struct axis_response
        {
            public Int32 axis_id;
            public string command;
            public Int32 data;
        };

        private List<axis_response> last_axis_responses = new List<axis_response>();

        private void ReceiveCanMessages(int can_msgs_max, ref USBcanServer.tCanMsgStruct[] can_msgs, USBcanServer USBCan, LogFile logFile, string[] variables, bool discovery_pass, ref HashSet<Int32> axis_list, bool convert, bool poll)
        {
            int responding_devices = 0;
            int timeout_ms = 500; // milliseconds
            
            var axis_responses = new List<axis_response>();

            DateTime time_start = DateTime.Now;
            while ( true )
            {
                if ((DateTime.Now - time_start).TotalMilliseconds >= timeout_ms)
                {
                    Debug.WriteLine(String.Format("Timeout in ReceiveCanMessages loop after {0} ms.", (DateTime.Now - time_start).TotalMilliseconds));
                    break;
                }

                if (!discovery_pass && (responding_devices >= axis_list.Count))
                {
                    // we received messages from everybody, so break out now
                    break;
                }

                int dwCount = can_msgs_max;
                byte channel = USBcanServer.USBCAN_CHANNEL_CH0;

                // perform non-blocking read. dwCount holds number of messages read in
                int ret = USBCan.ReadCanMsg(ref channel, ref can_msgs, ref dwCount);
                //Debug.WriteLine(String.Format("ReadCanMsg returned {0}, dwCount={1}", ret, dwCount));

                // Check failure -- no data
                if ((ret & USBcanServer.USBCAN_WARN_NODATA) != 0)
                {
                    Thread.Sleep(1);
                    continue;
                }

                // check failure -- not successful, or no msg
                if (ret != USBcanServer.USBCAN_SUCCESSFUL || dwCount < 1)
                {
                    //Debug.WriteLine(String.Format("Error: ReadCanMsg returned {0}, dwCount={1} (expected dwCount=={2})", ret, dwCount, 1));
                    Thread.Sleep(1);
                    continue;
                }

                // loop over all received messages
                for (int n = 0; n < dwCount; ++n)
                {
                    
                     
                    //axis_id = ((can_msgs[0].m_bData.ElementAt(1) & 0x0f) << 4) | ((can_msgs[0].m_bData.ElementAt(0) & 0xf0) >> 4);
                    if ((can_msgs[n].m_dwID & 0xFFFFFF00) == 0x1A9FA100) // TakeData2 Long message from controller to axis 253 (computer)
                    {
                        ++responding_devices;
                        var id = can_msgs[n].m_dwID & 0xFF;
                        UInt16 cmd = (UInt16)(can_msgs[n].m_bData[1] << 8 | can_msgs[n].m_bData[0]);
                        Int32 dat = (Int32)(can_msgs[n].m_bData[5] << 24
                                  |         can_msgs[n].m_bData[4] << 16
                                  |         can_msgs[n].m_bData[3] << 8
                                  |         can_msgs[n].m_bData[2]);

                        axis_responses.Add(new axis_response() { axis_id = id, command = address_to_command(cmd), data = dat });
                        if (discovery_pass)
                        {
                            if (!axis_list.Contains(id))
                            {
                                axis_list.Add(id);
                            }
                            last_axis_responses.Add(new axis_response() { axis_id = id, command = address_to_command(cmd), data = dat });
                        }

#if FALSE
                        Debug.WriteLine(String.Format("Found Axis ID={0}", id));
                        Debug.WriteLine(String.Format("CanMsg: ID={0:x08} DLC={1} {2:x02} {3:x02} {4:x02} {5:x02} {6:x02} {7:x02} {8:x02} {9:x02}",
                            can_msgs[n].m_dwID, can_msgs[n].m_bDLC, can_msgs[n].m_bData[0], can_msgs[n].m_bData[1], can_msgs[n].m_bData[2],
                            can_msgs[n].m_bData[3], can_msgs[n].m_bData[4], can_msgs[n].m_bData[5], can_msgs[n].m_bData[6],
                            can_msgs[n].m_bData[7]));
#endif
                    }
                    else if ((can_msgs[n].m_dwID & 0xFFFFFF00) == 0x1A9FA000) // TakeData2 Int message from controller to axis 253 (computer)
                    {
                        ++responding_devices;
                        var id = can_msgs[n].m_dwID & 0xFF;
                        UInt16 cmd = (UInt16)(can_msgs[n].m_bData[1] << 8 | can_msgs[n].m_bData[0]);
                        Int16 dat = (Int16)(can_msgs[n].m_bData[3] << 8 | can_msgs[n].m_bData[2]);

                        axis_responses.Add(new axis_response() { axis_id = id, command = address_to_command(cmd), data = dat });
                        if (discovery_pass)
                        {
                            if (!axis_list.Contains(id))
                            {
                                axis_list.Add(id);
                            }
                            last_axis_responses.Add(new axis_response() { axis_id = id, command = address_to_command(cmd), data = dat });
                        }


#if FALSE
                        Debug.WriteLine(String.Format("Found Axis ID={0}", id));
                        Debug.WriteLine(String.Format("CanMsg: ID={0:x08} DLC={1} {2:x02} {3:x02} {4:x02} {5:x02} {6:x02} {7:x02} {8:x02} {9:x02}",
                            can_msgs[n].m_dwID, can_msgs[n].m_bDLC, can_msgs[n].m_bData[0], can_msgs[n].m_bData[1], can_msgs[n].m_bData[2],
                            can_msgs[n].m_bData[3], can_msgs[n].m_bData[4], can_msgs[n].m_bData[5], can_msgs[n].m_bData[6],
                            can_msgs[n].m_bData[7]));
#endif
                    }
                    else if (!poll && (can_msgs[n].m_dwID & 0xFFFFFF00) == 0x1A9FA200) // GiveData Int message from controller to axis 253 (computer)
                    {
                        ++responding_devices;
                        var id = can_msgs[n].m_dwID & 0xFF;
                        UInt16 cmd = (UInt16)(can_msgs[n].m_bData[1] << 8 | can_msgs[n].m_bData[0]);
                        Int16 dat = (Int16)(can_msgs[n].m_bData[3] << 8 | can_msgs[n].m_bData[2]);

                        axis_responses.Add(new axis_response() { axis_id = id, command = address_to_command(cmd), data = dat });
                        if (discovery_pass)
                        {
                            if (!axis_list.Contains(id))
                            {
                                axis_list.Add(id);
                            }
                            last_axis_responses.Add(new axis_response() { axis_id = id, command = address_to_command(cmd), data = dat });
                        }


#if FALSE
                        Debug.WriteLine(String.Format("Found Axis ID={0}", id));
                        Debug.WriteLine(String.Format("CanMsg: ID={0:x08} DLC={1} {2:x02} {3:x02} {4:x02} {5:x02} {6:x02} {7:x02} {8:x02} {9:x02}",
                            can_msgs[n].m_dwID, can_msgs[n].m_bDLC, can_msgs[n].m_bData[0], can_msgs[n].m_bData[1], can_msgs[n].m_bData[2],
                            can_msgs[n].m_bData[3], can_msgs[n].m_bData[4], can_msgs[n].m_bData[5], can_msgs[n].m_bData[6],
                            can_msgs[n].m_bData[7]));
#endif
                    }
                    else if (!poll && (can_msgs[n].m_dwID & 0xFFFFFF00) == 0x1A9FA300) // GiveData Long message from controller to axis 253 (computer)
                    {
                        ++responding_devices;
                        var id = can_msgs[n].m_dwID & 0xFF;
                        UInt16 cmd = (UInt16)(can_msgs[n].m_bData[1] << 8 | can_msgs[n].m_bData[0]);
                        Int32 dat = (Int32)(can_msgs[n].m_bData[5] << 24
                                  | can_msgs[n].m_bData[4] << 16
                                  | can_msgs[n].m_bData[3] << 8
                                  | can_msgs[n].m_bData[2]);

                        axis_responses.Add(new axis_response() { axis_id = id, command = address_to_command(cmd), data = dat });
                        if (discovery_pass)
                        {
                            if (!axis_list.Contains(id))
                            {
                                axis_list.Add(id);
                            }
                            last_axis_responses.Add(new axis_response() { axis_id = id, command = address_to_command(cmd), data = dat });
                        }

#if FALSE
                        Debug.WriteLine(String.Format("Found Axis ID={0}", id));
                        Debug.WriteLine(String.Format("CanMsg: ID={0:x08} DLC={1} {2:x02} {3:x02} {4:x02} {5:x02} {6:x02} {7:x02} {8:x02} {9:x02}",
                            can_msgs[n].m_dwID, can_msgs[n].m_bDLC, can_msgs[n].m_bData[0], can_msgs[n].m_bData[1], can_msgs[n].m_bData[2],
                            can_msgs[n].m_bData[3], can_msgs[n].m_bData[4], can_msgs[n].m_bData[5], can_msgs[n].m_bData[6],
                            can_msgs[n].m_bData[7]));
#endif
                    }

                    /*else
                    {
                        //Debug.WriteLine(String.Format("Disregarding unknown CAN message ID={0:x08}", can_msgs[n].m_dwID));
                    }*/
                } // for loop to decode all messages read in
            } // while loop to read in all responses

            if (!discovery_pass && axis_responses.Count > 0)
                LogResponses(axis_list, axis_responses, variables, logFile, convert);
        }

        private void LogResponses(HashSet<Int32> axis_list, List<axis_response> axis_responses, string[] variables, LogFile logfile, bool convert)
        {
            bool need_comma = false;
            string line = "";
            foreach (var axis in axis_list)
            {
                foreach (var variable in variables)
                {
                    var responses_to_check = axis_responses;
                    axis_response response_to_log = new axis_response() { axis_id = axis, command = variable, data = 0 }; // initialize to 0

                    // first find the last good value
                    foreach (var last_response in last_axis_responses)
                    {
                        if (last_response.axis_id != axis)
                            continue;
                        if (last_response.command != variable)
                            continue;

                        response_to_log = last_response; // set response_to_log as last good value
                        break; // get out of here now since we found the match
                    }

                    // now try to find new data
                    bool found_new_response = false;
                    foreach (var response in axis_responses)
                    {
                        if (response.axis_id != axis)
                            continue;
                        if (response.command != variable)
                            continue;

                        // if we timed out on the previous call, we might have grabbed the response as part of this call, 
                        // in which case we could have two or more responses per variable.
                        // Grab only the LAST response from this axis, since any preceding ones were from the timeout period
                        if (!found_new_response)
                        {
                            found_new_response = true;
                            last_axis_responses.Remove(response_to_log); // delete old data
                            last_axis_responses.Add(response); // Copy latest data
                        }
                        response_to_log = response; // update the response_to_log with current data
                    }

                    if (need_comma)
                        line += ',';
                    need_comma = true;

                    if (convert)
                    { // attempt to convert from IU to eng units
                        double double_from_fixedpoint_int32 = (double)((Int16)(((UInt32)response_to_log.data >> 16) & 0xffff)) + (double)(((UInt32)response_to_log.data & 0xffff) / 65536.0);
                        double controller_peak_current = 16.5; // 16.5 Amps peak for IDM640-8EI
                        double vdc_max_measurable = 107.8; // 107.8 Vdc max measurable on IDM640-8EI
                        double slow_loop_servo_time_secs = 0.020; // 50Hz
                        double max_adc_range = 5.0; // 0..5 VDC for default setup
                        double adc_offset = 0.0; // 0.0 VDC for default setup
                        double temp_sensor_gain = 0.01; // Volts/degree C
                        double temp_sensor_output_at_0deg = 0.5; // Volts at 0 degC

                        int num_enc_lines = 4096; // pre-quadrature
                        
                        switch (response_to_log.axis_id % 10)
                        {
                            case 5: // Theta for HG2 (105, 115, ... 195)
                                {
                                    if (response_to_log.axis_id >= 105 || response_to_log.axis_id <= 195) // HiG Spindle axis on ISD860 controller
                                    {
                                        controller_peak_current = 30.9; // 30.9 Amps peak for ISD860
                                        vdc_max_measurable = 108.6; // 108.6 Vdc max measurable on ISD860
                                        //slow_loop_servo_time_secs = 0.020; // 50Hz
                                        //slow_loop_servo_time_secs = 0.01665; // 60Hz
                                        //slow_loop_servo_time_secs = 0.00835; // 120Hz
                                        slow_loop_servo_time_secs = 0.008; // 125Hz
                                        num_enc_lines = 4096; // pre-quadrature
                                        max_adc_range = 20.0; // -10..10 VDC for modified ISD860 controller
                                        adc_offset = 10.0; // 10.0 V offset for modified ISD860 controller
                                        temp_sensor_gain = 0.01; // Volts/degree C
                                        temp_sensor_output_at_0deg = 0.5; // Volts at 0 degC
                                    }
                                }
                                break;
                            case 6: // H1G Spindle (IDM640-8EI)
                                {
                                    controller_peak_current = 16.5; // 16.5 Amps peak for IDM640-8EI
                                    vdc_max_measurable = 107.8; // 107.8 Vdc max measurable on IDM640-8EI
                                    slow_loop_servo_time_secs = 0.020; // 50Hz
                                    num_enc_lines = 4096; // pre-quadrature
                                    max_adc_range = 5.0; // 0..5 VDC for default setup
                                    adc_offset = 0.0; // 0.0 VDC for default setup
                                }
                                break;
                        }

                        double Kif = 65472 / 2 / controller_peak_current; // (bits/Amps) Formula from Page 866 of MackDaddyTechnosoftDoc
                        double Kuf_m = 65472 / vdc_max_measurable; // (bits/Volts) Formula from Page 849 of MackDaddyTechnosoftDoc
                        double Kuf_adc = 65472 / max_adc_range;
                        double Kuf_adc_offset = adc_offset;
                        double KTf = temp_sensor_gain / 3.3 * 65472; // bits / degree C Formula from Page 867 of MackDaddyTechnosoftDoc
                        double temp_offset = (temp_sensor_output_at_0deg * 65472 / 3.3) / KTf; // degrees C from Formula from Page 867 of MackDaddyTechnosoftDoc

                        switch (response_to_log.command)
                        {
                            case "ASPD":
                            case "CSPD":
                            case "TSPD":
                            case "SPDERR":
                            case "SPDREF":
                                double spd = double_from_fixedpoint_int32 / slow_loop_servo_time_secs / num_enc_lines / 4.0 * 60.0; // rpm
                                line += spd;
                                break;
                            case "CACC":
                            case "TACC":
                                double acc = double_from_fixedpoint_int32 / slow_loop_servo_time_secs / slow_loop_servo_time_secs / num_enc_lines / 4.0 * 60.0; // rpm/s
                                line += acc;
                                break;
                            case "IQ":
                            case "IQREF":
                                double IQ = response_to_log.data / Kif;
                                line += IQ;
                                break;
                            case "AD4":
                                double ad = (double)((UInt16)response_to_log.data) / Kuf_m;
                                line += ad;
                                break;
                            case "AD5":
                                double ad5 = (double)(((UInt16)response_to_log.data) / Kuf_adc - Kuf_adc_offset);
                                line += ad5;
                                break;
                            case "AD7":
                                double ad7 = (double)(((UInt16)response_to_log.data) / KTf - temp_offset);
                                line += ad7;
                                break;
                            case "UQREF":
                            case "UDREF":
                                double uref = (double)((Int16)response_to_log.data) / Kuf_m;
                                line += uref;
                                break;
                            case "ATIME":
                                double atime_secs = (double)((Int32)response_to_log.data) * slow_loop_servo_time_secs;
                                line += atime_secs;
                                break;
                            default:
                                line += (types[command_list.IndexOf(variable)] == typeof(UInt16)) ? (line += (UInt16)response_to_log.data) : line += response_to_log.data;
                                break;
                        }
                    }
                    else
                    {
                        // try to handle unsigned type by casting
                        int index = command_list.IndexOf(variable);
                        if (types[index] == typeof(UInt16))
                            line += (UInt16)response_to_log.data;
                        else
                            line += response_to_log.data;
                    }
                }
            }

            logfile.WriteLine(line);
            //Debug.WriteLine(String.Format("{0}", DateTime.Now.ToString("yyyy-MM-dd,HH:mm:ss.fff,"), line));

#if FALSE
                    switch(n)
                    {
                        case 13: // Axis 1 X
                            x_eng = (double)data[n]; / (8000.0 * 4.0) * 24.0; // iu to mm
                            str += "," + x_eng.ToString();
                            break;
                        case 23: // Axis 3 Z
                            z_eng = (double)data[n]; / (5000.0 * 4.0) * 69.959; // iu to mm
                            str += "," + z_eng.ToString();
                            break;
                        case 33: // Axis 5 Theta
                            t_eng = (double)data[n]; / (5000.0 * 4.0) * 48.015463917525773195876288659794; // iu to degrees
                            str += "," + t_eng.ToString();
                            double theta_xform = Math.IEEERemainder(90.0 - t_eng, 360.0); // Get theta_deg in the range [-180.0, 180.0]
                            y_eng = 250.0 * Math.Cos(theta_xform / 180.0 * Math.PI);
                            str += "," + y_eng.ToString();
                            break;
                        case 43: // Axis 6 Gripper
                            g_eng = (double)data[n] / (1024.0 * 4.0) * 2.54; // iu to mm of space between gripper o-rings
                            str += "," + g_eng.ToString();
                            break;
                        default:
                            Debug.WriteLine(String.Format("Don't have eng conversion for this axis #{0}", n));
                            break;
                    }
#endif
        }
#if FALSE
        public void TestDiscovery()
        {
            const byte device_num = 0;
            int num_devices_discovered = 0;
            byte ret;
            byte channel = USBcanServer.USBCAN_CHANNEL_CH0;

            Dictionary<int, string> ts_cntl = new Dictionary<int, string>();

            const int can_msgs_max = 32;
            USBcanServer.tCanMsgStruct[] can_msgs;
            can_msgs = new USBcanServer.tCanMsgStruct[can_msgs_max];
            for (int n = 0; n < can_msgs_max; ++n)
            {
                can_msgs[n] = USBcanServer.tCanMsgStruct.CreateInstance(0, 0);
            }
            USBcanServer.tCanMsgStruct[] can_msgs_sending;
            can_msgs_sending = new USBcanServer.tCanMsgStruct[1];
            can_msgs_sending[0] = USBcanServer.tCanMsgStruct.CreateInstance(0, 0);

            USBcanServer USBCan = new USBcanServer();

            int dll_ver = USBCan.GetUserDllVersion();
            Debug.WriteLine(String.Format("User Dll: Ver={0}, Rev={1}, Release={2}", dll_ver & 0xff, (dll_ver & 0xff00) >> 8, (dll_ver & 0xff0000) >> 16));

            ret = USBCan.InitHardware(device_num);
            Debug.WriteLine(String.Format("InitHardware returned {0}", ret));
            if (ret != USBcanServer.USBCAN_SUCCESSFUL)
            {
                Debug.WriteLine(String.Format("Error: InitHardware returned {0}", ret));
            }

            ret = USBCan.InitCan(USBcanServer.USBCAN_CHANNEL_CH0, USBcanServer.USBCAN_BAUD_500kBit, USBcanServer.USBCAN_BAUDEX_USE_BTR01,
                    USBcanServer.USBCAN_AMR_ALL, USBcanServer.USBCAN_ACR_ALL, (byte)USBcanServer.tUcanMode.kUcanModeNormal,
                    USBcanServer.USBCAN_OCR_DEFAULT);
            Debug.WriteLine(String.Format("InitCan returned {0}", ret));
            if (ret != USBcanServer.USBCAN_SUCCESSFUL)
            {
                Debug.WriteLine(String.Format("Error: InitCan returned {0}", ret));
            }

            int fw_ver = USBCan.GetFwVersion();
            Debug.WriteLine(String.Format("Firmware: Ver={0}, Rev={1}, Release={2}", fw_ver & 0xff, (fw_ver & 0xff00) >> 8, (fw_ver & 0xff0000) >> 16));

            USBcanServer.tUcanHardwareInfoEx HwInfo = new USBcanServer.tUcanHardwareInfoEx();
            USBcanServer.tUcanChannelInfo CanInfoCh0 = new USBcanServer.tUcanChannelInfo();
            USBcanServer.tUcanChannelInfo CanInfoCh1 = new USBcanServer.tUcanChannelInfo();

            ret = USBCan.GetHardwareInfo(ref HwInfo, ref CanInfoCh0, ref CanInfoCh1);
            if (ret != USBcanServer.USBCAN_SUCCESSFUL)
            {
                Debug.WriteLine(String.Format("Error: GetHardwareInfo returned {0}", ret));
            }
            else
            {
                Debug.WriteLine(String.Format("S/N: {0}, Device# {1}, F/W: {2}, ProdCode: {3}, Size={4}",
                    HwInfo.m_dwSerialNr, HwInfo.m_bDeviceNr, HwInfo.m_dwFwVersionEx, HwInfo.m_dwProductCode, HwInfo.m_dwSize));
                Debug.WriteLine(String.Format("Ch0: Init={0}, CANstatus={1}", CanInfoCh0.m_fCanIsInit, CanInfoCh0.m_wCanStatus));
                Debug.WriteLine(String.Format("Ch1: Init={0}, CANstatus={1}", CanInfoCh1.m_fCanIsInit, CanInfoCh1.m_wCanStatus));
            }

            // var_i2 = 0xffe5;       // from 254 to broadcast ==> 04200167 E5 FF 
            // var_i1=(var_i2+),spi;  // from 254 to broadcast ==> 12200108 67 03 66 03
            // ?var_i1;               // from 254 to broadcast ==> 16200004 E0 0F 66 03
            // replies to ?var_i1 should be something like  169FC004 ID 00 66 03 xx yy (from axis ID)
            // ??var_i1;              // from 254 to broadcast ==> 16600004 E0 0F 66 03
            // replies to ??var_i1 should be something like  1A9FC0nn 66 03 xx yy (from axis ID nn)

            // var_i2 = 0xffe5; (from 255 to broadcast)
            can_msgs_sending[0].m_dwID = 0x04200167;
            can_msgs_sending[0].m_bFF = USBcanServer.USBCAN_MSG_FF_EXT;
            can_msgs_sending[0].m_bDLC = 2;
            can_msgs_sending[0].m_bData[0] = 0xE5;
            can_msgs_sending[0].m_bData[1] = 0xFF;
            SendCanMessage(can_msgs_sending, USBCan);

            ////////////////////////
            // First set of chars //
            ////////////////////////


            // var_i1=(var_i2+),spi;  (from 254 to broadcast)
            can_msgs_sending[0].m_dwID = 0x12200108;
            can_msgs_sending[0].m_bFF = USBcanServer.USBCAN_MSG_FF_EXT;
            can_msgs_sending[0].m_bDLC = 4;
            can_msgs_sending[0].m_bData[0] = 0x67;
            can_msgs_sending[0].m_bData[1] = 0x03;
            can_msgs_sending[0].m_bData[2] = 0x66;
            can_msgs_sending[0].m_bData[3] = 0x03;
            SendCanMessage(can_msgs_sending, USBCan);

            // ??var_i1;  (from 254 to broadcast)
            can_msgs_sending[0].m_dwID = 0x16600004; // GiveMeData2
            can_msgs_sending[0].m_bFF = USBcanServer.USBCAN_MSG_FF_EXT;
            can_msgs_sending[0].m_bDLC = 4;
            can_msgs_sending[0].m_bData[0] = 0xE0;
            can_msgs_sending[0].m_bData[1] = 0x0F;
            can_msgs_sending[0].m_bData[2] = 0x66;
            can_msgs_sending[0].m_bData[3] = 0x03;
            SendCanMessage(can_msgs_sending, USBCan);

            num_devices_discovered = TestCANbuildAxisDict(ref channel, ref ts_cntl, can_msgs_max, ref can_msgs, USBCan);

            /////////////////////////
            // Second set of chars //
            /////////////////////////

            // var_i1=(var_i2+),spi;  (from 254 to broadcast)
            can_msgs_sending[0].m_dwID = 0x12200108;
            can_msgs_sending[0].m_bFF = USBcanServer.USBCAN_MSG_FF_EXT;
            can_msgs_sending[0].m_bDLC = 4;
            can_msgs_sending[0].m_bData[0] = 0x67;
            can_msgs_sending[0].m_bData[1] = 0x03;
            can_msgs_sending[0].m_bData[2] = 0x66;
            can_msgs_sending[0].m_bData[3] = 0x03;
            SendCanMessage(can_msgs_sending, USBCan);

            // ??var_i1;  (from 254 to broadcast)
            can_msgs_sending[0].m_dwID = 0x16600004; // GiveMeData2
            can_msgs_sending[0].m_bFF = USBcanServer.USBCAN_MSG_FF_EXT;
            can_msgs_sending[0].m_bDLC = 4;
            can_msgs_sending[0].m_bData[0] = 0xE0;
            can_msgs_sending[0].m_bData[1] = 0x0F;
            can_msgs_sending[0].m_bData[2] = 0x66;
            can_msgs_sending[0].m_bData[3] = 0x03;
            SendCanMessage(can_msgs_sending, USBCan);

            int num_devices_queried = TestCANaddDict(num_devices_discovered, ref channel, ref ts_cntl, can_msgs_max, ref can_msgs, USBCan);

            ///////////////////////// 
            // Third set of chars  //
            /////////////////////////

            // var_i1=(var_i2+),spi;  (from 254 to broadcast)
            can_msgs_sending[0].m_dwID = 0x12200108;
            can_msgs_sending[0].m_bFF = USBcanServer.USBCAN_MSG_FF_EXT;
            can_msgs_sending[0].m_bDLC = 4;
            can_msgs_sending[0].m_bData[0] = 0x67;
            can_msgs_sending[0].m_bData[1] = 0x03;
            can_msgs_sending[0].m_bData[2] = 0x66;
            can_msgs_sending[0].m_bData[3] = 0x03;
            SendCanMessage(can_msgs_sending, USBCan);

            // ??var_i1;  (from 254 to broadcast)
            can_msgs_sending[0].m_dwID = 0x16600004; // GiveMeData2
            can_msgs_sending[0].m_bFF = USBcanServer.USBCAN_MSG_FF_EXT;
            can_msgs_sending[0].m_bDLC = 4;
            can_msgs_sending[0].m_bData[0] = 0xE0;
            can_msgs_sending[0].m_bData[1] = 0x0F;
            can_msgs_sending[0].m_bData[2] = 0x66;
            can_msgs_sending[0].m_bData[3] = 0x03;
            SendCanMessage(can_msgs_sending, USBCan);

            num_devices_queried = TestCANaddDict(num_devices_discovered, ref channel, ref ts_cntl, can_msgs_max, ref can_msgs, USBCan);

            ret = USBCan.Shutdown();
            //_controller._mainDebugFile.WriteLine(@"TestDiscovery", String.Format("Shutdown returned {0}", ret));
            if (ret != USBcanServer.USBCAN_SUCCESSFUL)
            {
                Debug.WriteLine(String.Format("Error: Shutdown returned {0}", ret));
            }

            Debug.WriteLine(String.Format("Found {0} Technosoft controllers on the CANbus", num_devices_discovered));

            foreach (KeyValuePair<int, string> kvp in ts_cntl)
            {
                Debug.WriteLine(String.Format("Axis ID={0} has S/N: {1}", kvp.Key, kvp.Value));
            }
        }

        private int TestCANaddDict(int num_devices_discovered, ref byte channel, ref Dictionary<int, string> ts_cntl, int can_msgs_max, ref USBcanServer.tCanMsgStruct[] can_msgs, USBcanServer USBCan)
        {
            byte ret;
            int dwCount;
            int num_devices_queried = 0;
            DateTime time_start = DateTime.Now;
            const int timeout_ms = 100; // milliseconds

            while ((DateTime.Now - time_start).TotalMilliseconds < timeout_ms && num_devices_queried < num_devices_discovered) // max number of channel ID's on a bus is 255
            {
                int axis_id = 0;
                dwCount = can_msgs_max;
                ret = USBCan.ReadCanMsg(ref channel, ref can_msgs, ref dwCount);
                Debug.WriteLine(String.Format("ReadCanMsg returned {0}, dwCount={1}", ret, dwCount));
                if (ret == USBcanServer.USBCAN_WARN_NODATA)
                {
                    Debug.WriteLine("Sleeping for 1ms");
                    Thread.Sleep(1);
                    continue;
                }
                if (ret != USBcanServer.USBCAN_SUCCESSFUL || dwCount < 1)
                {
                    Debug.WriteLine(String.Format("Error: ReadCanMsg returned {0}, dwCount={1} (expected dwCount=={2})",
                        ret, dwCount, 1));
                    Thread.Sleep(1);
                    continue;
                }
                for (int n = 0; n < dwCount; ++n)
                {
                    Debug.WriteLine(String.Format("CanMsg: ID={0:x08} DLC={1} {2:x02} {3:x02} {4:x02} {5:x02} {6:x02} {7:x02} {8:x02} {9:x02}",
                    can_msgs[n].m_dwID, can_msgs[n].m_bDLC, can_msgs[n].m_bData.ElementAt(0), can_msgs[n].m_bData.ElementAt(1), can_msgs[n].m_bData.ElementAt(2),
                    can_msgs[n].m_bData.ElementAt(3), can_msgs[n].m_bData.ElementAt(4), can_msgs[n].m_bData.ElementAt(5), can_msgs[n].m_bData.ElementAt(6),
                    can_msgs[n].m_bData.ElementAt(7)));
                    //axis_id = ((can_msgs[n].m_bData.ElementAt(1) & 0x0f) << 4) | ((can_msgs[n].m_bData.ElementAt(0) & 0xf0) >> 4);
                    if ((can_msgs[n].m_dwID & 0xFFFFFF00) == 0x1A9FC000) // TakeData2 message from controller to axis 254 (computer)
                    {
                        axis_id = can_msgs[n].m_dwID & 0xFF;
                        Debug.WriteLine(String.Format("Found Axis ID={0}", axis_id));
                        if (!ts_cntl.ContainsKey(axis_id))
                        {
                            Debug.WriteLine(String.Format("Error: Not expecting to hear from AxisID={0}. Ignoring...",
                                axis_id));
                            continue;
                        }
                        num_devices_queried++;
                        ts_cntl[axis_id] += String.Format("{0}{1}",
                            char.ConvertFromUtf32(can_msgs[n].m_bData.ElementAt(2)),
                            char.ConvertFromUtf32(can_msgs[n].m_bData.ElementAt(3)));
                    }
                    else
                    {
                        Debug.WriteLine(String.Format("Disregarding unknown CAN message ID={0:x08}", can_msgs[n].m_dwID));
                    }
                }
            }
            return num_devices_queried;
        }

        private int TestCANbuildAxisDict(ref byte channel, ref Dictionary<int, string> ts_cntl, int can_msgs_max,
                                         ref USBcanServer.tCanMsgStruct[] can_msgs, USBcanServer USBCan)
        {
            int num_devices_discovered = 0;
            int ret;
            int dwCount;
            const int timeout_ms = 100; // milliseconds

            DateTime time_start = DateTime.Now;

            while ((DateTime.Now - time_start).TotalMilliseconds < timeout_ms && num_devices_discovered < 255) // max number of channel ID's on a bus is 255
            {
                int axis_id = 0;
                dwCount = can_msgs_max;
                ret = USBCan.ReadCanMsg(ref channel, ref can_msgs, ref dwCount);
                Debug.WriteLine(String.Format("ReadCanMsg returned {0}, dwCount={1}", ret, dwCount));
                if (ret == USBcanServer.USBCAN_WARN_NODATA)
                {
                    Thread.Sleep(1); // yield the processor if someone else is ready to run;
                    continue;
                }
                if (ret != USBcanServer.USBCAN_SUCCESSFUL || dwCount < 1)
                {
                    Thread.Sleep(1); // yield the processor if someone else is ready to run;
                    Debug.WriteLine(String.Format("Error: ReadCanMsg returned {0}, dwCount={1} (expected dwCount=={2})",
                        ret, dwCount, 1));
                    continue;
                }
                for (int n = 0; n < dwCount; ++n)
                {
                    Debug.WriteLine(String.Format("CanMsg: ID={0:x08} DLC={1} {2:x02} {3:x02} {4:x02} {5:x02} {6:x02} {7:x02} {8:x02} {9:x02}",
                        can_msgs[n].m_dwID, can_msgs[n].m_bDLC, can_msgs[n].m_bData.ElementAt(0), can_msgs[n].m_bData.ElementAt(1), can_msgs[n].m_bData.ElementAt(2),
                        can_msgs[n].m_bData.ElementAt(3), can_msgs[n].m_bData.ElementAt(4), can_msgs[n].m_bData.ElementAt(5), can_msgs[n].m_bData.ElementAt(6),
                        can_msgs[n].m_bData.ElementAt(7)));
                    //axis_id = ((can_msgs[0].m_bData.ElementAt(1) & 0x0f) << 4) | ((can_msgs[0].m_bData.ElementAt(0) & 0xf0) >> 4);
                    if ((can_msgs[n].m_dwID & 0xFFFFFF00) == 0x1A9FC000) // TakeData2 message from controller to axis 254 (computer)
                    {
                        num_devices_discovered++;
                        axis_id = can_msgs[n].m_dwID & 0xFF;
                        Debug.WriteLine(String.Format("Found Axis ID={0}", axis_id));
                        ts_cntl.Add(axis_id, String.Format("{0}{1}",
                            char.ConvertFromUtf32(can_msgs[n].m_bData.ElementAt(2)),
                            char.ConvertFromUtf32(can_msgs[n].m_bData.ElementAt(3))));
                    }
                    else
                    {
                        Debug.WriteLine(String.Format("Disregarding unknown CAN message ID={0:x08}", can_msgs[n].m_dwID));
                    }
                }
            }
            return num_devices_discovered;
        }
#endif
    }
}
