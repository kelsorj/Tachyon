using FileHelpers;

namespace BioNex.HivePrototypePlugin
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
