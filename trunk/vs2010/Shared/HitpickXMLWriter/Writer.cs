using System.Collections.Generic;
using System.Xml;
using BioNex.Shared.PlateDefs;

namespace BioNex.Shared.HitpickXMLWriter
{
    public class Writer
    {
        public static void Write( TransferOverview to, string filepath)
        {
            // using XmlTextWriter to get some experience with it
            XmlTextWriter writer = new XmlTextWriter( filepath, null);
            writer.Formatting = Formatting.Indented;
            writer.WriteStartDocument();
            writer.WriteStartElement( "transfer_overview");
            // write the default parameters, like liquid class ID, asp/disp script,
            // asp/disp distance from well bottom
                writer.WriteStartElement( "default_liquid_class_id");
                writer.WriteValue( to.DefaultLiquidClass);
                writer.WriteEndElement();
                writer.WriteStartElement( "aspirate_script");
                writer.WriteValue( to.DefaultAspirateScript ?? "");
                writer.WriteEndElement();
                writer.WriteStartElement( "dispense_script");
                writer.WriteValue( to.DefaultDispenseScript ?? "");
                writer.WriteEndElement();
                writer.WriteStartElement( "aspirate_distance_from_well_bottom_mm");
                writer.WriteValue( to.DefaultAspirateDistanceFromWellBottomMm == null ? 0 : to.DefaultAspirateDistanceFromWellBottomMm.Value);
                writer.WriteEndElement();
                writer.WriteStartElement( "dispense_distance_from_well_bottom_mm");
                writer.WriteValue( to.DefaultDispenseDistanceFromWellBottomMm == null ? 0 : to.DefaultDispenseDistanceFromWellBottomMm.Value);
                writer.WriteEndElement();
                // loop over the transfer overview objects
                // --- sources ---
                PlateCatalog source_plates = to.SourcePlates;
                writer.WriteStartElement( "sources");
                foreach( KeyValuePair<string,Plate> kvp in source_plates) {
                    writer.WriteStartElement( "source");
                        writer.WriteStartElement( "labware_id");
                        writer.WriteValue( kvp.Value.LabwareName);
                        writer.WriteEndElement();
                        writer.WriteStartElement( "barcode");
                        writer.WriteValue( kvp.Value.Barcode);
                        writer.WriteEndElement();
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                // --- destinations ---
                PlateCatalog dest_plates = to.DestinationPlates;
                writer.WriteStartElement( "destinations");
                foreach( KeyValuePair<string,Plate> kvp in dest_plates) {
                    // cast to DestinationPlate because we need the usable wells
                    DestinationPlate dest_plate = (DestinationPlate)kvp.Value;
                    writer.WriteStartElement( "destination");
                        writer.WriteStartElement( "labware_id");
                        writer.WriteValue( dest_plate.LabwareName);
                        writer.WriteEndElement();
                        writer.WriteStartElement( "barcode");
                        writer.WriteValue( dest_plate.Barcode);
                        writer.WriteEndElement();
                        writer.WriteStartElement( "usable_wells");
                        writer.WriteValue( dest_plate.UsableWellsString);
                        writer.WriteEndElement();
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                // --- transfers ---
                writer.WriteStartElement( "transfers");
                foreach( Transfer t in to.Transfers) {
                    writer.WriteStartElement( "transfer");
                        if( t.LiquidProfileName != null) {
                            writer.WriteStartElement( "liquid_class_id");
                            writer.WriteValue( t.LiquidProfileName);
                            writer.WriteEndElement();
                        }
                        // source
                        writer.WriteStartElement( "source");
                            writer.WriteStartElement( "barcode");
                            writer.WriteValue( t.SrcPlate.Barcode);
                            writer.WriteEndElement();
                            writer.WriteStartElement( "well");
                            writer.WriteValue( t.SrcWell.WellName);
                            writer.WriteEndElement();
                        writer.WriteEndElement();
                        // dest
                        writer.WriteStartElement( "destination");
                            writer.WriteStartElement( "barcode");
                            writer.WriteValue( t.DstPlate.Barcode);
                            writer.WriteEndElement();
                            writer.WriteStartElement( "well");
                            writer.WriteValue( t.DstWell.WellName);
                            writer.WriteEndElement();
                        writer.WriteEndElement();
                        // transfer volume
                        writer.WriteStartElement( "transfer_volume");
                        writer.WriteAttributeString( "units", t.TransferUnits.ToString());
                        writer.WriteValue( t.TransferVolume.ToString());
                        writer.WriteEndElement();
                        // current volume
                        writer.WriteStartElement( "current_volume");
                        writer.WriteAttributeString( "units", t.TransferUnits.ToString());
                        writer.WriteValue( t.CurrentVolume.ToString());
                        writer.WriteEndElement();
                    writer.WriteEndElement();
                }
            writer.WriteEndElement();
            // --- done ---
            writer.WriteEndElement();
            writer.Close();
        }
    }
}
