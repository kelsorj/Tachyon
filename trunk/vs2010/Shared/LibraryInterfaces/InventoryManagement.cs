using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Shared.LibraryInterfaces
{
    public class InventoryPlateAlreadyUnloadedException : ApplicationException
    {
        public string Barcode { get; private set; }
        public InventoryPlateAlreadyUnloadedException( string barcode)
            : base( String.Format( "The plate with barcode '{0}' was already unloaded and is not currently in storage.", barcode))
        {
            Barcode = barcode;
        }
    }

    public class InventorySchemaMismatchException : ApplicationException
    {
        public IEnumerable<string> MismatchedFields { get; private set; }

        public InventorySchemaMismatchException( IEnumerable<string> mismatched_fields)
        {
            MismatchedFields = mismatched_fields;
        }
    }

    public class InventoryBarcodeNotFoundException : ApplicationException
    {
        public string Barcode { get; private set; }

        public InventoryBarcodeNotFoundException( string barcode)
            : base( String.Format( "Barcode '{0}' not found in inventory", barcode))
        {
            Barcode = barcode;
        }
    }

    public class InventoryFileDoesNotExistException : ApplicationException
    {
        public string FilePath { get; private set; }

        public InventoryFileDoesNotExistException( string filepath)
            : base( String.Format( "The inventory file '{0}' does not exist.", filepath))
        {
            FilePath = filepath;
        }
    }

    public interface IInventoryManagement
    {
        /// <summary>
        /// Creates a device-compatible inventory database
        /// </summary>
        /// <param name="db_filename">path to the database file that you want to create</param>
        /// <param name="storage_locations_schema">the device-specific field names in the storage_locations table</param>
        void CreateDatabase( string db_filename, List<string> storage_locations_schema);
        /// <summary>
        /// Loads the specified database file and confirms that the storage_locations table is compatible
        /// </summary>
        /// <param name="db_filename">path to the database file that you want to create</param>
        /// <param name="storage_locations_schema">the device-specific field names in the storage_locations table</param>
        void LoadDatabase( string db_filename, List<string> storage_locations_schema);
        /// <summary>
        /// Retrieves the device-specific location information for a plate with the give barcode
        /// </summary>
        /// <param name="barcode"></param>
        /// <returns></returns>
        Dictionary<string,string> GetLocation( string barcode);
        /// <summary>
        /// Returns the entire plate inventory in XML format
        /// </summary>
        /// <returns></returns>
        string GetInventoryXml();
        /// <summary>
        /// Returns the entire plate inventory as a map of barcodes to device-specific location information
        /// </summary>
        /// <returns></returns>
        Dictionary<string, Dictionary<string,string>> GetInventoryData();
        /// <summary>
        /// Adds a plate with the specified barcode to the specified location
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="location">The keys should include all of the device-specific fields passed upon database creation/loading</param>
        void Load( string barcode, Dictionary<string,string> location);
        /// <summary>
        /// Removes the plate with the specified barcode from inventory, but preserves its volume records
        /// </summary>
        /// <param name="barcode"></param>
        void Unload( string barcode);
        /// <summary>
        /// Deletes the plate with the specified barcode from inventory, along with all of its volume records
        /// </summary>
        /// <param name="barcode"></param>
        void Delete( string barcode);
        /// <summary>
        /// Gets the volume in a specific named reservoir in a plate with the specified barcode
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="reservoir_name">e.g. "A1", "C5", etc.</param>
        /// <returns></returns>
        double GetVolume( string barcode, string reservoir_name);
        /// <summary>
        /// Gets the volumes in all of the wells / reservoirs in the plate / barcoded vessel
        /// </summary>
        /// <param name="barcode"></param>
        /// <returns></returns>
        Dictionary<string,double> GetAllVolumes( string barcode);
        /// <summary>
        /// Sets the initial volume for a given reservoir in a specific plate to the specified amount, in uL
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="reservoir_names">all of the reservoir names whose initial volumes need to be set</param>
        /// <param name="volume_uL"></param>
        void SetInitialVolume( string barcode, List<string> reservoir_names, double volume_uL);
        /// <summary>
        /// Adjusts the volume of a given reservoir up or down by the volume delta specified
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="reservoir_names">all of the reservoir names whose volume deltas need to be set</param>
        /// <param name="volume_delta_uL">a positive value means fluid was dispensed into this well, a negative value means fluid was aspirated from this well</param>
        void AdjustVolume( string barcode, List<string> reservoir_names, double volume_delta_uL);
    }
}
