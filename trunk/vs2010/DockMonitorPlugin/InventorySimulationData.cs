using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FileHelpers;

namespace BioNex.Plugins.Dock
{
    [DelimitedRecord(",")]
    [IgnoreEmptyLines()]
    internal class InventorySimulationData
    {
        public int RackNumber { get; set; }
        public int SlotNumber { get; set; }
        public string Barcode { get; set; }
    }
}
