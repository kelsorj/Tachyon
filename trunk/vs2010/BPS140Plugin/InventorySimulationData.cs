using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FileHelpers;

namespace BioNex.BPS140Plugin
{
    [DelimitedRecord(",")]
    [IgnoreEmptyLines()]
    internal class InventorySimulationData
    {
        public int SideNumber { get; set; }
        public int RackNumber { get; set; }
        public int SlotNumber { get; set; }
        public string Barcode { get; set; }
    }
}
