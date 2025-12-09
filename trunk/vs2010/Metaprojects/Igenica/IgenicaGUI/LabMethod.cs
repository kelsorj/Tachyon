using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.IgenicaGUI
{
    public class LabMethodConfiguration
    {
        public string PreProtocolMessageFilename { get; set; }
        public List<LabMethod> LabMethods = new List<LabMethod>();
    }

    public class LabMethod
    {
        public string Name { get; set; }
        public string SourceLabware { get; set; }
        public string DestinationLabware { get; set; }
        public string LiquidProfile { get; set; }
        public double VolumeUl { get; set; }
        public double AspirateDistanceFromBottomMm { get; set; }
        public double DispenseDistanceFromBottomMm { get; set; }
    }
}
