using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FileHelpers;

namespace FileHelpersTestApplication
{
    [DelimitedRecord(",")]
    [IgnoreEmptyLines()]
    public class HitpickInfo
    {
        public string SourceID { get; set; }
        public string SourceWell { get; set; }
        public string DestinationID { get; set; }
        public string DestinationWell { get; set; }
    }
}
