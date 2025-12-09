using System;
using System.Collections.Generic;
using System.Linq;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.TaskListXMLParser;
using BioNex.Shared.Utils.WellMathUtil;

namespace BioNex.Shared.PlateDefs
{
    public enum VolumeUnits
    {
        ul,
        ml
    }

    public class SourceBarcodeComparer : IEqualityComparer<Transfer>
    {
        public bool Equals( Transfer x, Transfer y)
        {
            return x.SrcPlate.Barcode == y.SrcPlate.Barcode;
        }

        public int GetHashCode( Transfer obj)
        {
            return obj.ToString().ToLower().GetHashCode();
        }
    }

    //-------------------------------------------------------------------------
    //-------------------------------------------------------------------------
    // Transfer
    //-------------------------------------------------------------------------
    //-------------------------------------------------------------------------
    public class Transfer : IDisposable
    {
        //! \todo how do we apply the DebuggerBrowsable attributes to a group of members at a time?
        public Well SrcWell { get; private set; }
        public Well DstWell { get; private set; }
        public double TransferVolume { get; private set; }
        public VolumeUnits TransferUnits { get; private set; }
        public double CurrentVolume { get; private set; }
        public VolumeUnits CurrentVolumeUnits { get; private set; }
        public string LiquidProfileName { get; private set; } //! \todo class name or database id?
        public SourcePlate SrcPlate { get; private set; }
        public DestinationPlate DstPlate { get; private set; }
        public string AspirateScriptPath { get; private set; }
        public string DispenseScriptPath { get; private set; }
        public double? AspirateDistanceFromWellBottomMm { get; private set; }
        public double? DispenseDistanceFromWellBottomMm { get; private set; }

        public Transfer( SourcePlate source_plate, Well source_well, double transfer_volume,
                         VolumeUnits transfer_volume_units, double current_volume, VolumeUnits current_volume_units,
                         string liquid_class, DestinationPlate destination_plate, IList< Well> destination_wells,
                         string aspirate_script, string dispense_script)
            : this( source_plate, source_well, transfer_volume, transfer_volume_units, current_volume,
                    current_volume_units, liquid_class, destination_plate, destination_wells, aspirate_script,
                    dispense_script, null, null)
        {
        }

        public Transfer( SourcePlate source_plate, Well source_well, double transfer_volume,
                         VolumeUnits transfer_volume_units, double current_volume, VolumeUnits current_volume_units,
                         string liquid_class, DestinationPlate destination_plate, IList< Well> destination_wells,
                         string aspirate_script, string dispense_script, double? aspirate_distance_from_bottom,
                         double? dispense_distance_from_bottom)
        {
            // first, check to see if the transfer is valid, i.e. that the destination well name
            // is in the range of usable wells for the destination plate
            if( !destination_plate.AreWellsUsable( destination_wells)){
                string error = String.Format( "Could not create transfer because the specified destination well (range) '{0}' was not in the range of usable wells for the destination plate with barcode {1}", destination_wells.ToString(), destination_plate.Barcode.Value);
                throw new TransferMismatchException( error);
            }

            SrcWell = source_well;
            DstWell = destination_wells[ 0];
            TransferVolume = transfer_volume;
            TransferUnits = transfer_volume_units;
            CurrentVolume = current_volume;
            CurrentVolumeUnits = current_volume_units;
            LiquidProfileName = liquid_class;
            SrcPlate = source_plate;
            DstPlate = destination_plate;
            AspirateScriptPath = aspirate_script;
            DispenseScriptPath = dispense_script;
            AspirateDistanceFromWellBottomMm = aspirate_distance_from_bottom;
            DispenseDistanceFromWellBottomMm = dispense_distance_from_bottom;
        }

        public void SetDestinationWell( Well destination_well_name)
        {
            DstWell = destination_well_name;
        }

        public static bool AreEqual( Transfer lt, Transfer rt)
        {
            return lt.SrcPlate.Barcode == rt.SrcPlate.Barcode &&
                   lt.SrcWell == rt.SrcWell &&
                   lt.DstPlate.Barcode == rt.DstPlate.Barcode &&
                   lt.DstWell == rt.DstWell &&
                   lt.TransferVolume == rt.TransferVolume &&
                   lt.TransferUnits == rt.TransferUnits &&
                   lt.AspirateScriptPath == rt.AspirateScriptPath &&
                   lt.DispenseScriptPath == rt.DispenseScriptPath;
        }

        public override string ToString()
        {
            return String.Format( "{0}.[{1}]=>{2}.[{3}]", SrcPlate.Barcode.Value, SrcWell.WellName, DstPlate.Barcode.Value, DstWell.WellName);
        }

        public class TransferMismatchException : ApplicationException
        {
            public TransferMismatchException( string message)
                : base( message)
            {
            }
        }

        #region IDisposable Members
        public void Dispose()
        {
            GC.SuppressFinalize( this);
        }
        #endregion
        /* UNUSED:
        public static string ConvertTransfersToString( List<Transfer> transfers)
        {
            List<string> transfer_strings = new List<string>();
            foreach( Transfer t in transfers)
                transfer_strings.Add( t.ToString());
            return String.Join( ", ", transfer_strings.ToArray());
        }
        public void WriteTransferToFile( string path)
        {
            //! \todo this is not the best approach because it opens, writes, and closes every time.  Will use
            //!       the feature in TransferOverview once I finish moving the XML reader / writer classes
            //!       into a shared library.
            TextWriter writer = new StreamWriter( path, true);
            writer.WriteLine( "{0}\t{1}\t{2}", Source.Barcode, SourceWell.WellName, DestinationWell.WellName);
            writer.Close();
        }
        */
    }
    //-------------------------------------------------------------------------
    //-------------------------------------------------------------------------
    // TransferOverview
    //-------------------------------------------------------------------------
    //-------------------------------------------------------------------------
    public class TransferOverview
    {
        // string default_liquid_class_; //! \todo determine if this should be a class name or database ID

        public PlateCatalog SourcePlates { get; private set; }
        public PlateCatalog DestinationPlates { get; private set; }
        public List<Transfer> Transfers { get; private set; }
        public string DefaultLiquidClass { get; set; }
        public string DefaultAspirateScript { get; set; }
        public string DefaultDispenseScript { get; set; }
        public double? DefaultAspirateDistanceFromWellBottomMm { get; set; }
        public double? DefaultDispenseDistanceFromWellBottomMm { get; set; }
        public TaskListParser.DefaultTaskLists Tasks { get; set; }
        public ILabware DestinationLabware { get; set; }
        public string PlateStorageInterfaceName { get; set; }

        public TransferOverview()
        {
            DefaultLiquidClass = String.Empty;
            DefaultAspirateScript = String.Empty;
            DefaultDispenseScript = String.Empty;
            SourcePlates = new PlateCatalog();
            DestinationPlates = new PlateCatalog();
            Transfers = new List<Transfer>();
            Tasks = new TaskListParser.DefaultTaskLists();
        }

        public IEnumerable<string> GetLiquidProfilesUsed()
        {
            return Transfers.Select( x => x.LiquidProfileName).Distinct();
        }

        //! \todo think about whether or not this should have actually relationships stored between the wells and
        //!       their source plates
        public void AddTransfer( SourcePlate source_plate, Well source_well, DestinationPlate dest_plate, Well dest_well,
                                 double volume, VolumeUnits units, string liquid_class, double aspirate_distance_from_well_bottom_mm,
                                 double dispense_distance_from_well_bottom_mm, string aspirate_script, string dispense_script)
        {
            // use defaults if necessary
            if( aspirate_script == "")
                aspirate_script = DefaultAspirateScript;
            if( dispense_script == "")
                dispense_script = DefaultDispenseScript;

            Transfers.Add( new Transfer( source_plate, source_well, volume, units, 10.0, VolumeUnits.ul,
                                         liquid_class, dest_plate, new List< Well>{ dest_well}, aspirate_script, dispense_script,
                                         aspirate_distance_from_well_bottom_mm, dispense_distance_from_well_bottom_mm));
        }

        public List<Transfer> GetTransfersForDestination( string barcode)
        {
            Plate dest_plate = DestinationPlates[barcode];
            List<Transfer> transfers = new List<Transfer>();
            foreach( Transfer t in Transfers)
                if( t.DstPlate == dest_plate)
                    transfers.Add( t);
            return transfers;
        }

        public static bool AreEqual( TransferOverview lhs, TransferOverview rhs)
        {
            // as a quick test, compare the number of sources, dests, and transfers
            if( lhs.SourcePlates.Count != rhs.SourcePlates.Count ||
                lhs.DestinationPlates.Count != rhs.DestinationPlates.Count ||
                lhs.Transfers.Count != rhs.Transfers.Count)
                return false;

            // otherwise, we have to look at each set of data one by one
            // sources
            foreach( KeyValuePair<string,Plate> kvp in lhs.SourcePlates) {
                try {
                    Plate temp = rhs.SourcePlates[kvp.Value.Barcode];
                } catch( PlateCatalog.PlateDoesNotExistException) {
                    return false;
                }
            }
            // dests
            foreach( KeyValuePair<string,Plate> kvp in lhs.DestinationPlates) {
                try {
                    Plate temp = rhs.DestinationPlates[kvp.Value.Barcode];
                } catch( PlateCatalog.PlateDoesNotExistException) {
                    return false;
                }
            }
            // transfers
            // this sucks -- O(N^2)
            foreach( Transfer lt in lhs.Transfers) {
                bool found_match = false;
                foreach( Transfer rt in rhs.Transfers) {
                    if( Transfer.AreEqual( lt, rt)) {
                        found_match = true;
                        break;
                    }
                }
                if( !found_match)
                    return false;
            }
            return true;
        }
    }
}
