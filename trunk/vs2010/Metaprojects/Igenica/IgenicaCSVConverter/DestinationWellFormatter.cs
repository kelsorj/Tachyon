using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("IgenicaTests")]
namespace BioNex.IgenicaCSVConverter
{
    public class DestinationWellFormatter
    {
        private List<uint> _dest_plates { get; set; }
        private HashSet<uint> _used_dest_plates { get; set; }
        private int _current_plate_index { get; set; }
        private int _current_plate_well_index { get; set; }
        private int _number_of_wells { get; set; }

        public DestinationWellFormatter( List<uint> destination_plate_barcodes, int number_of_wells)
        {
            _dest_plates = destination_plate_barcodes;
            _used_dest_plates = new HashSet<uint>();
            _number_of_wells = number_of_wells;
        }

        public void GetNextTransferLocation( out uint barcode, out string wellname)
        {
            if( _current_plate_index >= _dest_plates.Count()) {
                // reset counters so we can run again if the user corrects the issue and then reinventories
                _current_plate_index = 0;
                _current_plate_well_index = 0;
                // error
                throw new Exception( "There are not enough destination plates to process this hitpick file.");
            }
            
            // set the current plate and well name
            barcode = _dest_plates[_current_plate_index];
            wellname = IgenicaIndexToWellName( _current_plate_well_index++, _number_of_wells);
            // reset to new plate if we are on the last well of this plate
            if( _current_plate_well_index >= _number_of_wells) {
                _current_plate_index++;
                _current_plate_well_index = 0;
            }

            return;
        }

        /// <summary>
        /// Igenica conveniently goes top-to-bottom, left-to-right, so I can't use my existing Util methods.
        /// </summary>
        /// <param name="well_index"></param>
        /// <param name="number_of_wells"></param>
        /// <returns></returns>
        static internal string IgenicaIndexToWellName( int well_index, int number_of_wells)
        {
            // default to 96 well plate
            // need number of rows and columns first
            int num_rows = 8;
            if( number_of_wells == 48) {
                num_rows = 6;
            } else if( number_of_wells == 384) {
                num_rows = 16;
            }

            int column_index = well_index / num_rows;
            int row_index = well_index % num_rows;
            return new BioNex.Shared.Utils.WellMathUtil.Well( row_index, column_index).WellName;
        }

    }
}
