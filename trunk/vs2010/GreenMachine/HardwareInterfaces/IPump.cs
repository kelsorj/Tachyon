using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.GreenMachine.HardwareInterfaces
{
    public interface IPump
    {
        void Initialize();
        void Close();

        void Aspirate( bool use_output_valve, double volume, int speed, bool use_microstep);
        void Dispense( bool use_output_valve, double volume, int speed, bool use_microstep);
        /// <summary>
        /// For the compilot case, Transfer seems to assume a valve ID of 1.
        /// </summary>
        /// <remarks>
        /// Compare TransferCommand in transfer Transfer to Command in Dispense -- Dispense
        /// uses ValveId, which is pumpInputChar if using a valve != 2.  TransferCommand
        /// always uses pumpInputChar, and never pumpOutputChar.
        /// </remarks>
        /// <param name="valve_id"></param>
        /// <param name="volume"></param>
        /// <param name="speed"></param>
        /// <param name="use_microstep"></param>
        void Transfer( double volume, int speed);
    }
}
