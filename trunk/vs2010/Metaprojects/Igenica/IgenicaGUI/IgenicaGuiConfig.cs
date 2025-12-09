using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.IgenicaGUI
{
    public class IgenicaGuiConfig
    {
        public class range
        {
            public string labware_name { get; set; }
            public uint start_barcode { get; set; }
            public uint end_barcode { get; set; }
        }

        public List<range> source_barcode_ranges { get; set; }
        public List<range> destination_barcode_ranges { get; set; }

        public IgenicaGuiConfig()
        {
            source_barcode_ranges = new List<range>();
            destination_barcode_ranges = new List<range>();
        }

        private bool IsBarcodeInRanges( string barcode, List<range> ranges)
        {
            // fail if the barcode isn't numeric
            uint result;
            if( !uint.TryParse( barcode, out result))
                return false;
            foreach( range range in ranges)
                if( range.start_barcode <= result && range.end_barcode >= result)
                    return true;
            return false;
        }

        public bool IsDestinationBarcode( string barcode)
        {
            return IsBarcodeInRanges( barcode, destination_barcode_ranges);
        }

        public bool IsSourceBarcode( string barcode)
        {
            return IsBarcodeInRanges( barcode, source_barcode_ranges);
        }

        public string GetLabwareNameForBarcode( string barcode)
        {
            uint result;
            if( !uint.TryParse( barcode, out result))
                return "";

            // check source plates first
            var labware_name = (from x in source_barcode_ranges where (x.start_barcode <= result  && x.end_barcode >= result) select x.labware_name).FirstOrDefault();
            if( labware_name != null)
                return labware_name;
            labware_name = (from x in destination_barcode_ranges where (x.start_barcode <= result  && x.end_barcode >= result) select x.labware_name).FirstOrDefault();
            if( labware_name != null)
                return labware_name;
            return "";
        }
    }
}
