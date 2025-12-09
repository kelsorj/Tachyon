using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Diagnostics;

namespace BioNex.BNX1536Plugin
{
    public partial class ViewModel : BaseViewModel
    {
        BNX1536Device _model;
        private string _status;
        public string Status
        {
            get { return _status; }
            set {
                _status = value;
                RaisePropertyChanged( "Status");
            }
        }
        public string SelectedPort { get; set; }
        public string SelectedSimPort { get; set; }
        public int SelectedProgram { get; set; }
        public int SelectedServiceProgram { get; set; }
        public List<int> ProgramItems { get; set; }
        public List<int> ServiceProgramItems { get; set; }

        public ViewModel( BNX1536Device model)
        {
            InitializeCommands();
            InitializePorts();
            InitializeProgramLists();
            _model = model;
        }

        private void InitializePorts()
        {
            string[] ports = SerialPort.GetPortNames();
            if( ports.Count() > 0) {
                SelectedPort = SerialPort.GetPortNames()[0];
                SelectedSimPort = SelectedPort;
            }
        }

        private void InitializeProgramLists()
        {
            ProgramItems = new List<int>();
            for( int i=1; i<=99; i++)
                ProgramItems.Add( i);
            ServiceProgramItems = new List<int>();
            for( int i=1; i<=4; i++)
                ServiceProgramItems.Add( i);
            SelectedProgram = 1;
            SelectedServiceProgram = 1;
        }

        public void Connect()
        {
            _model.Connect( SelectedPort);
        }

        public void RunSelectedProgram()
        {
            _model.StartProgram( SelectedProgram);
        }

        public void RunSelectedServiceProgram()
        {
            _model.StartServiceProgram( SelectedServiceProgram);
        }

        public void QueryStatus()
        {
            Status = _model.QueryStatus();
        }
    }
}
