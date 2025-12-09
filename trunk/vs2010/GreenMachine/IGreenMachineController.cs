using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.GreenMachine.HardwareInterfaces;

namespace BioNex.GreenMachine
{
    public interface IGreenMachineController
    {
        void Connect();
        void Close();
        bool Connected { get; }
        void Abort();
        void Pause();
        void Resume();

        IXyz Stage { get; }
        IPump[] Pumps { get; }
    }
}
