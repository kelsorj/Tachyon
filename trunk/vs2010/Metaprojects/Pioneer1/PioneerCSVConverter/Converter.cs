using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.LibraryInterfaces;
using System.ComponentModel.Composition;
using BioNex.Shared.LabwareDatabase;
using FileHelpers;
using FileHelpersTestApplication;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.Utils.WellMathUtil;

namespace PioneerCSVConverter
{
    [Export(typeof(ILimsTextConverter))]
    public class Converter : ILimsTextConverter
    {
        public override string Name
        {
            get { return "Pioneer CSV Converter"; }
        }

        public override string Filter
        {
            get { return "Pioneer CSV hitpick file (*.csv)|*.csv"; }
        }

        public override string FileExtension
        {
            get { return "csv"; }
        }

        public override string GetConvertedHitpickFile( string customer_filepath)
        {
            FileHelperEngine engine = new FileHelperEngine(typeof(HitpickInfo));
            engine.Options.IgnoreFirstLines = 1;
            HitpickInfo[] info = engine.ReadFile( customer_filepath) as HitpickInfo[];
            var source_barcodes = (from hpi in info select hpi.SourceID).ToArray();
            var dest_barcodes = (from hpi in info select hpi.DestinationID).ToArray();
            TransferOverview transfers = new TransferOverview();
            // add the source plates
            foreach( string barcode in source_barcodes) {
                transfers.SourcePlates.Add( new SourcePlate( DefaultSourceLabware, barcode));
            }
            // add the dest plates
            foreach( string barcode in dest_barcodes) {
                transfers.DestinationPlates.Add( new DestinationPlate( DefaultDestinationLabware, barcode, "any"));
            }
            // add the transfers
            foreach( HitpickInfo hpi in info) {
                SourcePlate source_plate = new SourcePlate( DefaultSourceLabware, hpi.SourceID);
                DestinationPlate dest_plate = new DestinationPlate( DefaultDestinationLabware, hpi.DestinationID, "any");
                transfers.AddTransfer( source_plate, new Well(hpi.SourceWell), dest_plate, new Well(hpi.DestinationWell), DefaultTransferVolume, VolumeUnits.ul,
                                       DefaultLiquidProfile, DefaultAspirateDistanceFromWellBottom, DefaultDispenseDistanceFromWellBottom, "", "");
            }
            // write the main transfer elements
            transfers.DefaultLiquidClass = DefaultLiquidProfile;
            transfers.DefaultAspirateDistanceFromWellBottomMm = DefaultAspirateDistanceFromWellBottom;
            transfers.DefaultDispenseDistanceFromWellBottomMm = DefaultDispenseDistanceFromWellBottom;
            
            string output_filename = customer_filepath + ".xml";
            BioNex.Shared.HitpickXMLWriter.Writer.Write( transfers, output_filename);
            return output_filename;
        }
    }
}
