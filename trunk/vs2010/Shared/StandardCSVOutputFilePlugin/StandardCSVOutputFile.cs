using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.LibraryInterfaces;
using System.ComponentModel.Composition;
using System.IO;

namespace BioNex.StandardCSVOutputFilePlugin
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ILimsOutputTransferLog))]
    public class StandardCSVOutputFile : ILimsOutputTransferLog
    {
        #region ILimsOutputTransferLog Members

        private TextWriter Writer { get; set; }

        public void Open( string filepath)
        {
            Writer = new StreamWriter( filepath, true);
        }

        public void LogTransfer(string source_barcode, string source_well, string destination_barcode, string destination_well, double volume_uL, DateTime timestamp)
        {
            // see the following link for date time formatting
            // http://msdn.microsoft.com/en-us/library/system.globalization.datetimeformatinfo.aspx
            Writer.WriteLine( String.Format( "{0},{1},{2},{3},{4},{5},{6}", source_barcode, source_well, destination_barcode,
                              destination_well, volume_uL, timestamp.ToString( "yyyyMMddHHmmss"), "OK"));
        }

        public void LogLiquidLevel(string plate_barcode, IDictionary<string, double> well_to_volume_map, DateTime timestamp)
        {
            // do nothing
        }

        public void Close()
        {
            Writer.Close();
        }

        #endregion
    }
}
