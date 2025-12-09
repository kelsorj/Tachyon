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
using System.IO.Ports;
using System.Threading;
using System.ComponentModel;
using GalaSoft.MvvmLight.Command;
using BioNex.Shared.Amulet;

namespace AmuletTestApp
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class GUI : Window, INotifyPropertyChanged
    {
        private Amulet _amulet { get; set; }

        public RelayCommand ConnectCommand { get; set; }
        public RelayCommand PcFromAmuletRamByteCommand { get; set; }
        public RelayCommand PcFromAmuletRamWordCommand { get; set; }
        public RelayCommand PcFromAmuletRamStringCommand { get; set; }

        private List<string> _available_com_ports;
        public List<string> AvailableComPorts
        {
            get { return _available_com_ports; }
            set {
                _available_com_ports = value;
                OnPropertyChanged( "AvailableComPorts");
            }
        }

        public string SelectedComPort { get; set; }

        // Amulet communication via databinding here
        private byte _pc_to_amulet_byte;
        public byte PcToAmuletByte
        {
            get { return _pc_to_amulet_byte; }
            set {
                _pc_to_amulet_byte = value;
                _amulet.SetByte( 0, value);
            }
        }

        private ushort _pc_to_amulet_word;
        public ushort PcToAmuletWord
        {
            get { return _pc_to_amulet_word; }
            set {
                _pc_to_amulet_word = value;
                _amulet.SetWord( 0, value);
            }
        }

        private string _pc_to_amulet_string;
        public string PcToAmuletString
        {
            get { return _pc_to_amulet_string; }
            set {
                _pc_to_amulet_string = value;
                _amulet.SetString( 0, value);
            }
        }

        private Brush _amulet_to_pc_rpc_text_color;
        public Brush AmuletToPcRpcTextColor
        {
            get { return _amulet_to_pc_rpc_text_color; }
            set {
                _amulet_to_pc_rpc_text_color = value;
                OnPropertyChanged( "AmuletToPcRpcTextColor");
            }
        }

        private byte _amulet_to_pc_byte;
        public byte AmuletToPcByte
        {
            get { return _amulet_to_pc_byte; }
            set {
                _amulet_to_pc_byte = value;
                OnPropertyChanged( "AmuletToPcByte");
            }
        }

        private ushort _amulet_to_pc_word;
        public ushort AmuletToPcWord
        {
            get { return _amulet_to_pc_word; }
            set {
                _amulet_to_pc_word = value;
                OnPropertyChanged( "AmuletToPcWord");
            }
        }

        private string _amulet_to_pc_string;
        public string AmuletToPcString
        {
            get { return _amulet_to_pc_string; }
            set {
                _amulet_to_pc_string = value;
                OnPropertyChanged( "AmuletToPcString");
            }
        }

        // values here are updated in the GUI and go only one way
        public byte AmuletFromPcUartByte { get; set; }
        public ushort AmuletFromPcUartWord { get; set; }
        public string AmuletFromPcUartString { get; set; }

        // values here are requested by the PC from Amulet RAM
        private byte _pc_from_amulet_ram_byte;
        public byte PcFromAmuletRamByte
        {
            get { return _pc_from_amulet_ram_byte; }
            set {
                _pc_from_amulet_ram_byte = value;
                OnPropertyChanged( "PcFromAmuletRamByte");
            }
        }

        private ushort _pc_from_amulet_ram_word;
        public ushort PcFromAmuletRamWord
        {
            get { return _pc_from_amulet_ram_word; }
            set {
                _pc_from_amulet_ram_word = value;
                OnPropertyChanged( "PcFromAmuletRamWord");
            }
        }

        private string _pc_from_amulet_ram_string;
        public string PcFromAmuletRamString
        {
            get { return _pc_from_amulet_ram_string; }
            set {
                _pc_from_amulet_ram_string = value;
                OnPropertyChanged( "PcFromAmuletRamString");
            }
        }

        public GUI()
        {
            InitializeComponent();
            this.DataContext = this;
            InitializeCommands();
            AvailableComPorts = new List<string>( SerialPort.GetPortNames());

            AmuletToPcRpcTextColor = Brushes.Gray;
        }

        private void InitializeCommands()
        {
            ConnectCommand = new RelayCommand( ExecuteConnectCommand);
            PcFromAmuletRamByteCommand = new RelayCommand( ExecutePcFromAmuletRamByte);
            PcFromAmuletRamWordCommand = new RelayCommand( ExecutePcFromAmuletRamWord);
            PcFromAmuletRamStringCommand = new RelayCommand( ExecutePcFromAmuletRamString);
        }

        private void ExecuteConnectCommand()
        {
            // lazy for now -- just simple ack with 0xF0
            _amulet = new Amulet( SelectedComPort);
            _amulet.Connect();

            // amulet sets stuff in PC
            _amulet.InvokeRpcRequested += new EventHandler(_amulet_InvokeRpcRequested);
            _amulet.SetByteRequested += new EventHandler(_amulet_SetByteRequested);
            _amulet.SetWordRequested += new EventHandler(_amulet_SetWordRequested);
            _amulet.SetStringRequested += new EventHandler(_amulet_SetStringRequested);

            // amulet requests values from PC
            _amulet.GetByteRequested += new EventHandler(_amulet_GetByteRequested);
            //_amulet.GetWordRequested += new EventHandler(_amulet_GetWordRequested);
        }

        void _amulet_GetWordRequested(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        void _amulet_GetByteRequested(object sender, EventArgs e)
        {
            AmuletCommandEventArgs args = (AmuletCommandEventArgs)e;
            _amulet.RespondWith( args.Command, args.Variable, AmuletFromPcUartByte.ByteToByteArray());
        }

        void _amulet_SetStringRequested(object sender, EventArgs e)
        {
            AmuletCommandEventArgs args = (AmuletCommandEventArgs)e;
            _amulet.RespondWith( args.Command, args.Variable, args.Value);
            AmuletToPcString = args.Value.ByteArrayToString();
        }

        void _amulet_SetWordRequested(object sender, EventArgs e)
        {
            AmuletCommandEventArgs args = (AmuletCommandEventArgs)e;
            _amulet.RespondWith( args.Command, args.Variable, args.Value);
            AmuletToPcWord = args.Value.ByteArrayToWord();
        }

        void _amulet_SetByteRequested(object sender, EventArgs e)
        {
            AmuletCommandEventArgs args = (AmuletCommandEventArgs)e;
            _amulet.RespondWith( args.Command, args.Variable, args.Value);
            AmuletToPcByte = args.Value.ByteArrayToByte();
        }

        void _amulet_InvokeRpcRequested(object sender, EventArgs e)
        {
            AmuletCommandEventArgs args = (AmuletCommandEventArgs)e;
            _amulet.RespondWith( args.Command, args.Variable, null);
            AmuletToPcRpcTextColor = Brushes.Green;
            Thread.Sleep( 100);
            AmuletToPcRpcTextColor = Brushes.Gray;
            // TODO now invoke the RPC method
        }

        private void ExecutePcFromAmuletRamByte()
        {
            PcFromAmuletRamByte = _amulet.GetByte( 0);
        }

        private void ExecutePcFromAmuletRamWord()
        {
            PcFromAmuletRamWord = _amulet.GetWord( 0);
        }

        private void ExecutePcFromAmuletRamString()
        {

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _amulet.Close();
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
