using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using BioNex.Shared.LibraryInterfaces;

namespace BioNex.MonsantoOutputTransferPlugin
{
    public class MonsantoTransferData
    {
        public string SourceBarcode { get; set; }
        public string SourceWell { get; set; }
        public string DestinationBarcode { get; set; }
        public string DestinationWell { get; set; }
        public double TargetVolume { get; set; }
        public DateTime PipetteTimestamp { get; set; }
        public double SensedVolume { get; set; }
        public DateTime SensorTimestamp { get; set; }
    }

    [ PartCreationPolicy( CreationPolicy.Shared)]
    [ Export( typeof( ILimsOutputTransferLog))]
    public class MonsantoOutputTransferPlugin : ILimsOutputTransferLog
    {
        HashSet< MonsantoTransferData> TransferData = new HashSet< MonsantoTransferData>();
        #region ILimsOutputTransferLog Members
        public void Open( string filepath)
        {
        }
        public void LogTransfer( string source_barcode, string source_well, string destination_barcode, string destination_well, double volume_uL, DateTime timestamp)
        {
            lock( TransferData){
                TransferData.Add( new MonsantoTransferData(){ SourceBarcode = source_barcode,
                                                              SourceWell = source_well,
                                                              DestinationBarcode = destination_barcode,
                                                              DestinationWell = destination_well,
                                                              TargetVolume = volume_uL,
                                                              PipetteTimestamp = timestamp,
                                                              SensedVolume = double.NaN });
            }
        }
        public void LogLiquidLevel( string plate_barcode, IDictionary< string, double> well_to_volume_map, DateTime timestamp)
        {
            lock( TransferData){
                foreach( MonsantoTransferData transfer_data in TransferData.Where( transfer => transfer.DestinationBarcode == plate_barcode)){
                    if( well_to_volume_map.Keys.Contains( transfer_data.DestinationWell)){
                        transfer_data.SensedVolume = well_to_volume_map[ transfer_data.DestinationWell];
                        transfer_data.SensorTimestamp = timestamp;
                    }
                }
            }
        }
        public void Close()
        {
        }
        #endregion

        public IList< MonsantoTransferData> ExtractDestinationPlateData( string destination_barcode)
        {
            lock( TransferData){
                // copy references to data on specified destination into a new list.
                IList< MonsantoTransferData> retval = TransferData.Where( transfer => transfer.DestinationBarcode == destination_barcode).ToList();
                // remove references to data on specified destination from master list.
                TransferData.RemoveWhere( transfer => transfer.DestinationBarcode == destination_barcode);
                return retval;
            }
        }
    }
}
