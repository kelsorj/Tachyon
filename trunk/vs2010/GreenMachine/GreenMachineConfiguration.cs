using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.GreenMachine
{
    public class GreenMachineConfiguration
    {
        public class TTRobotSettings
        {
            public string XDefaultSpeed { get; set; }
            public string YDefaultSpeed { get; set; }
            public string ZDefaultSpeed { get; set; }
            public string XDefaultAccel { get; set; }
            public string YDefaultAccel { get; set; }
            public string ZDefaultAccel { get; set; }
        }
    }
}
