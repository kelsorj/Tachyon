using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FileHelpers;

namespace BioNex.IgenicaCSVConverter
{
    [DelimitedRecord(",")]
    [IgnoreEmptyLines()]
    public class HitpickInfo
    {
        public string SourceID { get; set; }
        public string SourceWell { get; set; }
        // dest barcode and dest well are not specified by the hitpick file.  it's up to
        // us to pick an available dest barcode on the fly, in addition to the well.
    }
}
