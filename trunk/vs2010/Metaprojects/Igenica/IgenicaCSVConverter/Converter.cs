using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.LibraryInterfaces;
using FileHelpers;
using BioNex.Shared.PlateDefs;
using System.ComponentModel.Composition;
using log4net;
using BioNex.Shared.Utils.WellMathUtil;

namespace BioNex.IgenicaCSVConverter
{
    [Export(typeof(ILimsTextConverter))]
    public class Converter : ILimsTextConverter
    {
        private ILog _log = LogManager.GetLogger( typeof( ILimsTextConverter));

        public override string Name
        {
            get { return "Igenica CSV Converter"; }
        }

        public override string Filter
        {
            get { return "Igenica CSV hitpick file (*.csv)|*.csv"; }
        }

        public override string FileExtension
        {
            get { return "csv"; }
        }

        public override string GetConvertedHitpickFile(string customer_filepath, IEnumerable<string> available_destination_barcodes,
                                                       ILabware destination_labware)
        {
            _log.Info( String.Format( "converting customer hitpick file '{0}'", customer_filepath));
            FileHelperEngine engine = new FileHelperEngine(typeof(HitpickInfo));
            engine.Options.IgnoreFirstLines = 1;
            HitpickInfo[] info = engine.ReadFile( customer_filepath) as HitpickInfo[];
            // get only the unique source barcodes
            var source_barcodes = (from hpi in info select hpi.SourceID).ToArray().Distinct();

            TransferOverview transfers = new TransferOverview();
            // add the source plates
            foreach( string barcode in source_barcodes) {
                transfers.SourcePlates.Add( new SourcePlate( DefaultSourceLabware, barcode));
            }

            // don't need to add destination plates, since the DestinationWellFormatter will
            // tell us where to put everything
            List<uint> destination_barcodes = (from x in available_destination_barcodes select uint.Parse( x)).ToList();
            destination_barcodes.Sort();
            int num_dest_wells = int.Parse(destination_labware.Properties[LabwarePropertyNames.NumberOfWells].ToString());
            DestinationWellFormatter formatter = new DestinationWellFormatter( destination_barcodes, num_dest_wells);
            // add the transfers
            foreach( HitpickInfo hpi in info) {
                SourcePlate source_plate = transfers.SourcePlates[hpi.SourceID] as SourcePlate;
                uint dest_plate_barcode;
                string dest_wellname;
                formatter.GetNextTransferLocation( out dest_plate_barcode, out dest_wellname);
                DestinationPlate dest_plate = new DestinationPlate( destination_labware, dest_plate_barcode.ToString(), "any");
                transfers.DestinationPlates.Add( dest_plate);
                transfers.AddTransfer( source_plate, new Well(hpi.SourceWell), dest_plate, new Well(dest_wellname), DefaultTransferVolume, VolumeUnits.ul,
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
