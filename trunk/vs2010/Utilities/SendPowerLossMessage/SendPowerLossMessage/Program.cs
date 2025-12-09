using System;
using System.Runtime.InteropServices;

namespace SendPowerLossMessage
{
    class Program
    {
        private const string msgstr = "SynapsisUtilityPowerLossDetected";
        static uint msg_id;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern uint FindWindow(String sClassName, String sAppName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool PostMessage(int hhwnd, uint msg, IntPtr wparam, IntPtr lparam);

        private const int HWND_BROADCAST = 0xffff;

        static void Main(string[] args)
        {
            msg_id = RegisterWindowMessage(msgstr);
            if (0 == msg_id)
            {
                Console.WriteLine("Registering '{0}' failed with: {1}", msgstr, Marshal.GetLastWin32Error().ToString());
            }
            else
            {
                Console.WriteLine("Registered '{0}' with id: {1}", msgstr, msg_id);
            }

            //const string AppName_str = "Power Loss Message Receiver";
            const string AppName_str = "Synapsis";

            uint hwnd = 0;

            if ((hwnd = FindWindow(null, AppName_str)) == 0)
            {
                hwnd = HWND_BROADCAST;
                Console.WriteLine("Could not find '{0}'", AppName_str);
                Console.WriteLine("Posting '{0}' message to local BROADCAST", msgstr);
            }
            else
            {
                Console.WriteLine("Found '{0}' with hwnd={1}", AppName_str, hwnd);
                Console.WriteLine("Posting '{0}' message to '{1}'", msgstr, AppName_str);
            }

            PostMessage((int)hwnd, msg_id, IntPtr.Zero, IntPtr.Zero);
            System.Threading.Thread.Sleep(1000);
        }
    }
}
