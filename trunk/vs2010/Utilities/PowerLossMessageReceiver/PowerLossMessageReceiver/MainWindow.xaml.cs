using System;
using System.Collections.Generic;
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
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace PowerLossMessageReceiver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static string msgstr = "SynapsisUtilityPowerLossDetected";
        static uint msg_id;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint RegisterWindowMessage(string lpString);

        public MainWindow()
        {
            Console.WriteLine("Registering '{0}' with app", msgstr);
            msg_id = RegisterWindowMessage(msgstr);
            if (0 == msg_id)
            {
                Console.WriteLine("Registering '{0}' failed with: {1}", msgstr, Marshal.GetLastWin32Error().ToString());
            }

            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        static int c = 0;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Handle messages...
            if (msg == msg_id)
            {
                Console.WriteLine("Hooray! Received {0} message: msg={1}, wParam={2}, lParam={3}", ++c, msg, wParam, lParam);
                textBlock1.Text = String.Format("{0} count: {1}\nLast event timestamp: {2}", msgstr, c, DateTime.Now.ToString());
            }

            return IntPtr.Zero;
        }
    }
}
