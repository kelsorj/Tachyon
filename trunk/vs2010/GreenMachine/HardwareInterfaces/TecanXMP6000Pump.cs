using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;

namespace BioNex.GreenMachine.HardwareInterfaces
{
    public class TecanXMP6000Pump : BaseTecanPump, IPump
    {
        public TecanXMP6000Pump( int port)
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

        protected override string SpeedCmdChar { get { return "V"; } }
        protected override string NonMicroStepCmdChar { get { return "N0"; } }
        protected override string MicroStepCmdChar { get { return "N1"; } }
        protected override double SyringeSizeUl { get { return 500; } }
        protected override int MaxMotorSteps { get { return 6000; } }
        protected override int MaxMotorMicroSteps { get { return 48000; } }

        #endregion
    }
}
