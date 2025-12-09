using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;

namespace BioNex.GreenMachine.HardwareInterfaces
{
    public abstract class BaseTecanPump : IPump
    {        
        // this stuff is for command generation
        protected abstract string SpeedCmdChar { get; }
        protected abstract string NonMicroStepCmdChar { get; }
        protected abstract string MicroStepCmdChar { get; }
        protected abstract double SyringeSizeUl { get; }
        protected abstract int MaxMotorSteps { get; }
        protected abstract int MaxMotorMicroSteps { get; }
        protected SerialPort Port { get; set; }

        #region IPump Members

        public abstract void Initialize();
        public abstract void Close();

        private int ConvertUlToSteps(double volume, bool use_microstep)
        {
            return (int)(volume / SyringeSizeUl * (use_microstep ? MaxMotorMicroSteps : MaxMotorSteps));
        }

        public virtual void Aspirate(bool use_output_valve, double volume, int speed, bool use_microstep)
        {
            //! \todo put volume check before actual protocol execution as a compile step!

            // pumpAddressChar-----------------------------------------------vv
            string command = String.Format( "{0}{1}{2}{3}{4}{5}{6}{7}\r\n", "/1",
                                            use_microstep ? MicroStepCmdChar : NonMicroStepCmdChar , 
                                            use_output_valve ? "O" : "I",
                                            SpeedCmdChar, speed, "P", ConvertUlToSteps( volume, use_microstep), "R");
            //                                                    ^-------pumpSpdChar                            ^-------pumpExeChar

            Port.Write( command);
        }

        public virtual void Dispense(bool use_output_valve, double volume, int speed, bool use_microstep)
        {
            //! \todo put volume check before actual protocol execution as a compile step!

            string command = String.Format( "{0}{1}{2}{3}{4}{5}{6}{7}\r\n", "/1", 
                                            use_microstep ? MicroStepCmdChar : NonMicroStepCmdChar, 
                                            use_output_valve ? "O" : "I",
                                            SpeedCmdChar, speed, "D", ConvertUlToSteps( volume, use_microstep), "R");
            //                                                    ^-------pumpSpdChar                            ^-------Port
            Port.Write( command);                        
        }

        public virtual void Transfer(double volume, int speed)
        {
            //! \todo put volume check before actual protocol execution as a compile step!
            
            // I guess you can send two commands together to the pump, and when you execute it
            // with "R", it will do the aspirate followed by the dispense
            string transfer_command = String.Format( "{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}{11}{12}\r\n",
                                                     "/1", NonMicroStepCmdChar, "I", SpeedCmdChar, speed, "A", ConvertUlToSteps( volume, false),
                                                     "O", SpeedCmdChar, speed, "D", ConvertUlToSteps( volume, false), "R");
            Port.Write( transfer_command);

            // I didn't need to use String.Format for this (or any of the other commands), but I
            // am not sure yet whether or not the pump commands need to be configurable via XML like
            // the com_pilot app
            string valve_command = String.Format( "{0}{1}{2}\r\n", "/1", "I", "R");
            Port.Write( valve_command);            
        }

        #endregion
    }
}
