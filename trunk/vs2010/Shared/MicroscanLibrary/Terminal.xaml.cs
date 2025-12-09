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
using System.ComponentModel;
using System.Diagnostics;
using GalaSoft.MvvmLight.Command;

namespace BioNex.Shared.Microscan
{
    /// <summary>
    /// Interaction logic for Terminal.xaml
    /// </summary>
    public partial class Terminal : UserControl, INotifyPropertyChanged
    {
        private MicroscanReader _reader;
        internal MicroscanReader Reader
        {
            set {
                _reader = value;
                _reader.SerialDataReceived += new IncomingSerialDataEventHandler(_reader_SerialDataReceived);
            }
        }

        public RelayCommand ClearCommand { get; set; }

        void _reader_SerialDataReceived(object sender, IncomingSerialDataEventArgs e)
        {
            IncomingData += e.Data + "\r\n\r\n";
        }

        private string _incomingdata;
        public string IncomingData
        {
            get { return _incomingdata; }
            set {
                _incomingdata = value;
                OnPropertyChanged( "IncomingData");
            }
        }

        /// <summary>
        /// this is one-way data from gui to code.  We need to keep track of what was sent from the textbox so we don't resend everything on the screen.
        /// we'll see how this works...
        /// </summary>
        private string _outgoing_data_sent = ""; // stuff sent already
        public string OutgoingData
        {
            get { return _outgoing_data_sent; }
            set {
                Debug.WriteLine( String.Format( "textbox has text: {0}, already sent: {1}", value, _outgoing_data_sent));
                // take substring of textbox text -- don't want to send the same data over and over again
                string command_fragment = value.Substring( _outgoing_data_sent.Length);
                if( command_fragment.EndsWith( ">")) {
                    _outgoing_data_sent += command_fragment;
                    Debug.WriteLine( String.Format( "Sending command: {0}, now already sent: {1}", command_fragment, _outgoing_data_sent));
                    if( command_fragment.Contains( '?'))
                        _reader.SendCommandAndGetResponse( command_fragment);
                    else
                        _reader.SendCommandNoResponse( command_fragment);
                } 
            }
        }

        public Terminal()
        {
            InitializeComponent();
            DataContext = this;

            ClearCommand = new RelayCommand( () => { IncomingData = ""; });
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion
    }
}
