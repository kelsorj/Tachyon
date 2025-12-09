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
using BioNex.PlateStorageStackerMockPlugin;
using System.Net.Sockets;
using System.Diagnostics;

namespace PlateStorageStackerTestApplication
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private PlateStorageStacker _stacker;
        private TcpClient _client;
        private NetworkStream _stream;

        public Window1()
        {
            InitializeComponent();
            _stacker = new PlateStorageStacker();
            // initialize stacker so it starts to listen
            // exception obviously shouldn't get thrown since this is now in the constructor,
            // but FYI it will throw that exception if you try to call Initialize twice without
            // closing first.
            try {
                _stacker.Initialize( 0);
            } catch( AlreadyListeningException ex) {
                MessageBox.Show( String.Format( "Listener is already listening at IP address {0} on port {1}", ex.IPAddress.ToString(), ex.Port));
                return;
            }
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            if( _client != null && _client.Connected) {
                Debug.WriteLine( "Already connected");
                return;
            }
            // create connection to listener (currently on 127.0.01:7890)
            try {
                _client = new TcpClient();
                _client.Connect( "127.0.0.1", 7890);
                _stream = _client.GetStream();
                Debug.WriteLine( "Connected!");
            } catch( SocketException ex) {
                MessageBox.Show( "Could not connect to stacker listener: " + ex.Message);
            }
        }

        private void Upstack_Click(object sender, RoutedEventArgs e)
        {
            string command = "upstack --arga=\"arg1\"\r\n";
            SendCommand( command);
        }

        private void Downstack_Click(object sender, RoutedEventArgs e)
        {
            string command = "downstack --arga=\"arg1\"\r\n";
            SendCommand( command);
        }

        private void IllegalCommand_Click(object sender, RoutedEventArgs e)
        {
            string command = "oops --arga=\"arg1\"\r\n";
            SendCommand( command);
        }

        private void SendCommand( string command)
        {
            byte[] data = System.Text.Encoding.ASCII.GetBytes( command);
            try {
                _stream.Write( data, 0, data.Length); 
            } catch( SocketException ex) {
                MessageBox.Show( "Could not send command: " + ex.Message);
            } catch( ObjectDisposedException ex) {
                MessageBox.Show( "Could not send command because the connection was closed already");
            } catch( NullReferenceException) {
                MessageBox.Show( "Could not send command because there isn't a connection to the device");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DisconnectClient();
            _stacker.Close();
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            DisconnectClient();
        }

        private void DisconnectClient()
        {
            if( _client == null || _client.Client == null)
                return;
            try {
                _client.Client.Disconnect( false);
                // these two lines aren't necessary, I think.
                //_client.GetStream().Close();
                _client.Client.Close();
                _client.Close();
                Debug.WriteLine( "Disconnected");
            } catch( ObjectDisposedException) {
                // do nothing, the TcpClient was created, then closed twice in a row
            }
        }
    }
}
