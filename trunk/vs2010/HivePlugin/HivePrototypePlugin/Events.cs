using System.Collections.Generic;

namespace BioNex.HivePrototypePlugin
{
    public class RackReinventoryCompleteEvent
    {
        public int RackNumber { get; private set; }
        public List<string> Barcodes { get; private set; }
        public int StartingSlotNumber { get; private set; }

        public RackReinventoryCompleteEvent( int rack_number, List<string> barcodes, int starting_slot_number)
        {
            RackNumber = rack_number;
            Barcodes = barcodes;
            StartingSlotNumber = starting_slot_number;
        }
    }

}
