using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using BioNex.Shared.IError;
using BioNex.GreenMachine.HardwareInterfaces;

namespace BioNex.GreenMachine
{
    public class GreenMachineSimulationController : IGreenMachineController
    {
        private string _device_instance_name;
        private ILog _log = LogManager.GetLogger( typeof( GreenMachineSimulationController));

        private IXyz _sim_stage;
        private IPump[] _sim_pumps;
        private IError _error_interface;

        public GreenMachineSimulationController( string device_instance_name, int stage_port, int[] pump_ports, IError error_interface)
        {
            _device_instance_name = device_instance_name;
            _error_interface = error_interface;

            _sim_stage = new SimXyz();
            _sim_pumps = new IPump[pump_ports.Count()];
            for( int i=0; i<pump_ports.Count(); i++)
                _sim_pumps[i] = new SimPump();
        }

        #region IGreenMachineController Members

        public void Connect()
        {
            _sim_stage.Initialize();
            foreach( var p in _sim_pumps)
                p.Initialize();

            _log.Info( String.Format( "Connected to '{0}'", _device_instance_name));
            Connected = true;
        }

        public void Close()
        {
            _log.Info( String.Format( "Disconnected from '{0}'", _device_instance_name));
            Connected = false;
        }

        public bool Connected { get; private set; }

        public void Abort()
        {
        }

        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public IXyz Stage
        {
            get { return _sim_stage; }
        }

        public IPump[] Pumps
        {
            get { return _sim_pumps; }
        }

        #endregion
    }
}
