using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Shared.SimpleInventory
{
    public class InventoryXMLFileException : ApplicationException
    {
        public InventoryXMLFileException( string msg) : base(msg)
        {
        }
    }

    public class InventoryBarcodeNotFoundException : ApplicationException
    {
        public string Barcode { get; private set; }

        public InventoryBarcodeNotFoundException( string barcode_or_barcodes)
            : base(String.Format( "Could not find the barcode(s) {0} in the inventory system", barcode_or_barcodes))
        {
            Barcode = barcode_or_barcodes;
        }
    }

    public class InventoryDuplicateBarcodeException : ApplicationException
    {
        public InventoryDuplicateBarcodeException( string msg) : base(msg)
        {
        }
    }
}
