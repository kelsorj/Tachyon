using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.GreenMachine.StateMachines;
using BioNex.GreenMachine.HardwareInterfaces;
using BioNex.Shared.IError;

namespace BioNex.GreenMachine
{
    public class GreenMachineController : IGreenMachineController
    {
        private string _device_instance_name;
        // hardware classes
        private IXyz _syringe_robot;
        private IPump[] _pumps;
        private IError _error_interface;

        public GreenMachineController( string device_instance_name, int xyz_port, int[] pump_ports, IError error_interface)
        {
            _device_instance_name = device_instance_name;
            // connect to the individual components, then pass into the controller
            _syringe_robot = new TTSyringeRobot( xyz_port);
            int num_pumps = pump_ports.Count();
            _pumps = new TecanXMP6000Pump[num_pumps];
            for( int i=0; i<num_pumps; i++) {
                _pumps[i] = new TecanXMP6000Pump( pump_ports[i]);
            }

            _error_interface = error_interface;
        }

        public void Connect()
        {
            _syringe_robot.Initialize();
            foreach( IPump pump in Pumps)
                pump.Initialize();
            Connected = true;
        }

        public void Close()
        {}
    
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
            get { return _syringe_robot; }
        }

        public IPump[] Pumps
        {
            get { return _pumps; }
        }
    }
}
