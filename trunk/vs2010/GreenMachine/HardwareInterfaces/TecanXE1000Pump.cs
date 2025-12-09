using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;

namespace BioNex.GreenMachine.HardwareInterfaces
{
    class TecanXE1000Pump : BaseTecanPump, IPump
    {
        public TecanXE1000Pump( int port)
        {
            Port = new SerialPort( "COM" + port.ToString(), 9600, Parity.None, 8, StopBits.One);            
        }

        #region IPump Members

        public override void Initialize()
        {   
            Port.Open();
        }

        public override void Close()
        {
        }

        protected override string SpeedCmdChar { get { return "S"; } }
        protected override string NonMicroStepCmdChar { get { return ""; } }
        protected override string MicroStepCmdChar { get { return "N1"; } }
        protected override double SyringeSizeUl { get { return 5000; } }
        protected override int MaxMotorSteps { get { return 1000; } }
        protected override int MaxMotorMicroSteps { get { return 1000; } }

        #endregion
    }
}
