using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using Systec_IO;

namespace IO_X1_low_level_example
{
    class IO_X1_example
    {
        public static Boolean done = false;

        public static void help()
        {
            Console.WriteLine("");
            Console.WriteLine("Press 0-7 to toggle individual output channels.");
            Console.WriteLine("Press SHIFT 0-7 to set individual output channels.");
            Console.WriteLine("Press CTRL 0-7 to clear individual output channels.");
            Console.WriteLine("Press 8 to turn all channels ON.");
            Console.WriteLine("Press 9 to turn all channels OFF.");
            Console.WriteLine("Press SPACEBAR to toggle all channels.");
            Console.WriteLine("Press I to refresh and report Inputs.");
            Console.WriteLine("Press O to report Outputs.");
            Console.WriteLine("Press D to make each output come on in a dance.");
            Console.WriteLine("Press Escape, Enter, Q, X, or Ctrl-C to quit.");
            Console.WriteLine("Press ? for help.");
        }

        [System.Runtime.InteropServices.DllImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]
        private static extern uint TimeBeginPeriod(uint uMilliseconds);

        [System.Runtime.InteropServices.DllImport("winmm.dll", EntryPoint = "timeEndPeriod", SetLastError = true)]
        private static extern uint TimeEndPeriod(uint uMilliseconds);

        static void Main(string[] args)
        {
            TimeBeginPeriod(1); // set timer resolution to 1ms to get max performace from Sleep and USB->CANbus calls

            IOX1 m_IOX1 = new IOX1();

            if (0 == args.Length)
            {
                // Device address = 0x40 for the CANid of the device set by circular hex switches
                // CAN Device = 2 for the CAN adapter. Use 2 for Pioneer1 system
                // Channel = 0. Use 0 for Pioneer1 system
                // Baudrate = 500kbps for our CANbus baud rate  set by circular hex switches
                if (0 != m_IOX1.Initialize(0x40, 2, 0, (int)IOX1.baud_rates.baud_500kbit))
                {
                    Console.WriteLine("Init failed...\n");
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadKey(true);
                    return;
                }
            }
            else if (2 == args.Length)
            {
                // Note that arg0 is a hex number, and no 0x is allowed to be in front of it.
                if (0 != m_IOX1.Initialize(Int16.Parse(args[0], System.Globalization.NumberStyles.HexNumber), Int32.Parse(args[1]), 0, (int)IOX1.baud_rates.baud_500kbit))
                {
                    Console.WriteLine("Init failed for CANid={0:x02} on CANbus device#{1}, channel 0 at 500kbit/s...\n", 
                                      Int16.Parse(args[0], System.Globalization.NumberStyles.HexNumber), Int16.Parse(args[1]));
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadKey(true);
                    return;
                }
            }

            Console.TreatControlCAsInput = false;
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CtrlCHandler);

            help();

            UInt16 last_inputs = 0;
            Byte last_outputs = 0;
            Boolean first_time = true;
            
            while (!done)
            {
                UInt16 inputs = 0;
                Byte outputs = 0;

                if (last_inputs != (inputs = m_IOX1.ReadInputs()) || first_time)
                {
                    Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + " -- Inputs  = {0:x04}", inputs);
                    last_inputs = inputs;
                }
                
                if (last_outputs != (outputs = m_IOX1.GetOutputState()) || first_time)
                {
                    Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + "-- Outputs =   {0:x02}", outputs);
                    last_outputs = outputs;
                }

                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.KeyChar >= '0' && key.KeyChar < '8')
                        m_IOX1.ToggleOutput(key.KeyChar - '0');
                    else if (key.KeyChar == ')')
                        m_IOX1.SetOutputs((byte)(0x01));
                    else if (key.KeyChar == '!')
                        m_IOX1.SetOutputs((byte)(0x02));
                    else if (key.KeyChar == '@')
                        m_IOX1.SetOutputs((byte)(0x04));
                    else if (key.KeyChar == '#')
                        m_IOX1.SetOutputs((byte)(0x08));
                    else if (key.KeyChar == '$')
                        m_IOX1.SetOutputs((byte)(0x10));
                    else if (key.KeyChar == '%')
                        m_IOX1.SetOutputs((byte)(0x20));
                    else if (key.KeyChar == '^')
                        m_IOX1.SetOutputs((byte)(0x40));
                    else if (key.KeyChar == '&')
                        m_IOX1.SetOutputs((byte)(0x80));
                    else if (key.KeyChar == '8')
                        m_IOX1.WriteOutputs(0xff);
                    else if (key.KeyChar == '9')
                        m_IOX1.WriteOutputs(0x00);
                    else if (key.KeyChar == ' ')
                        m_IOX1.WriteOutputs((byte)~m_IOX1.GetOutputState());
                    else if (key.KeyChar == 'i' || key.KeyChar == 'I')
                    {
                        inputs = m_IOX1.ReadInputs();
                        Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + " -- Inputs  = {0:x04}", inputs);
                        last_inputs = inputs;
                    }
                    else if (key.KeyChar == 'o' || key.KeyChar == 'O')
                    {
                        Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + " -- Outputs  = {0:x02}", m_IOX1.GetOutputState());
                    }
                    else if (key.KeyChar == 27 || key.KeyChar == '\n' || key.KeyChar == '\r' || key.KeyChar == 'X' || key.KeyChar == 'x' || key.KeyChar == 'Q' || key.KeyChar == 'q')
                        done = true;
                    else if (key.KeyChar == '?')
                        help();
                    else if (key.Modifiers == ConsoleModifiers.Control)
                    {
                        byte bitmask = 0x00;
                        if (key.Key == ConsoleKey.D0) bitmask = 0x01;
                        else if (key.Key == ConsoleKey.D1) bitmask = 0x02;
                        else if (key.Key == ConsoleKey.D2) bitmask = 0x04;
                        else if (key.Key == ConsoleKey.D3) bitmask = 0x08;
                        else if (key.Key == ConsoleKey.D4) bitmask = 0x10;
                        else if (key.Key == ConsoleKey.D5) bitmask = 0x20;
                        else if (key.Key == ConsoleKey.D6) bitmask = 0x40;
                        else if (key.Key == ConsoleKey.D7) bitmask = 0x80;
                        m_IOX1.ClearOutputs(bitmask);
                    }
                    else if (key.KeyChar == 'd' || key.KeyChar == 'D')
                    {
                        Console.WriteLine("\nPress any key to stop this madness.");
                        byte bits = 0x00;
                        Random sleep_time_ms = new Random();
                        while (!Console.KeyAvailable)
                        {
                            m_IOX1.WriteOutputs(bits);
                            if (bits == 0)
                                bits = 0x01;
                            else
                                bits = (byte)(bits << 1);
                            Thread.Sleep(sleep_time_ms.Next(100)+1);
                        }
                        ConsoleKeyInfo dummy = Console.ReadKey(true);
                    }
                    else
                        Console.WriteLine("Did not understand keypress: {0} with modifiers {1} which is {2}", key.KeyChar, key.Modifiers.ToString(), key.Key.ToString());
                } // if

                Thread.Sleep(20);

                first_time = false;
            } // for

            Console.WriteLine("Bye bye...");
            m_IOX1.Close();

            TimeEndPeriod(1); // reset timer resolution back to default from 1ms to be nice to Windows

            return;
        }


        protected static int times_called = 0;

        protected static void CtrlCHandler(object sender, ConsoleCancelEventArgs args)
        {

            if (++times_called > 2)
            {
                Console.WriteLine("okay... okay... I'm exiting now.");

                args.Cancel = false;
                return;
            }

            // Set the Cancel property to true to prevent the process from terminating.
            args.Cancel = true;

            done = true;
        } // CtrlCHandler

    } // class IO_X1_example
} // namespace IO_X1_low_level_example
