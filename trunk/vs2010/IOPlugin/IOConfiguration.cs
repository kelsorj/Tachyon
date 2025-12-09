using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.DeviceInterfaces;

namespace BioNex.IOPlugin
{
    public class IOConfiguration
    {
        public class HazardousBit
        {
            public string BitName { get; set; }
            public int BitNumber { get; set; }
            public int HazardousLogicLevel { get; set; }
            public string NotificationMessage { get; set; }
        }

        public List<HazardousBit> HazardousBits { get; set; }
        public List<BitNameMapping> InputNames { get; set; }
        public List<BitNameMapping> OutputNames { get; set; }

        public IOConfiguration()
        {
            HazardousBits = new List<HazardousBit>();
            InputNames = new List<BitNameMapping>();
            OutputNames = new List<BitNameMapping>();
        }
    }
}
