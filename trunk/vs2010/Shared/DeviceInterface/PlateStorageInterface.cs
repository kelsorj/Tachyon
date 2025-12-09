using System;
using System.Collections.Generic;

namespace BioNex.Shared.DeviceInterfaces
{
    public interface PlateStorageInterface
    {
        event EventHandler ReinventoryBegin;
        event EventHandler ReinventoryComplete;
        event EventHandler ReinventoryError;

        void Unload( string labware_name, string barcode, string from_location_name);
        void Load( string labware_name, string barcode, string to_location_name);
        bool HasPlateWithBarcode( string barcode, out string location_name);
        /// <summary>
        /// This method is used to request all locations for a labware of a specific type.  The
        /// idea is that the application will call this to find out where all of the tip boxes
        /// or destination labware is stored in a device.
        /// </summary>
        /// <param name="labware_name"></param>
        /// <returns></returns>
        IEnumerable<string> GetLocationsForLabware( string labware_name);
        /// <summary>
        /// This returns all plates present in the storage device.  The Key is the barcode,
        /// and the Value is the location name.
        /// </summary>
        /// <remarks>
        /// For the BPS140, this will only return the plates that are FACING THE ROBOT.  Note that
        /// this implementation will eventually need to change if storage devices are ever going
        /// to be accessible by multiple robots!
        /// </remarks>
        /// <returns></returns>
        IEnumerable<KeyValuePair<string,string>> GetInventory( string robot_name);
        bool Reinventory( bool park_robot_after);
        void DisplayInventoryDialog();
        /// <summary>
        /// Returns all of the storage location names in this device.
        /// For example, Hive would return "Rack 1, Slot 1", "Rack 1, Slot 2", etc.
        /// BPS140 would return "Side 1: Rack 1, Slot 1", "Side 1: Rack 1, Slot 2", etc.
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetStorageLocationNames();
    }

    public delegate void DockEventHandler( object sender, DockEventArgs e);

    public class DockEventArgs : EventArgs
    {
        public string DockName { get; private set; }
        public string CartBarcode { get; private set; }
        public string CartHumanReadable { get; private set; }

        public DockEventArgs( string dock_name, string cart_barcode, string cart_human_readable)
        {
            DockName = dock_name;
            CartBarcode = cart_barcode;
            CartHumanReadable = cart_human_readable;
        }
    }

    public interface DockablePlateStorageInterface : PlateStorageInterface
    {
        string SubStorageName { get; set; }
        bool SubStoragePresent { get; }
        bool SubStorageDocked { get; }

        IEnumerable<string> GetAvailableLocationNames();
        // this is only used by teachpoint transformation -- we wanted to be able to get teachpoints for
        // undocked carts without requiring us to change all of the other device interfaces.
        string GetTeachpointFilenamePrefix( string barcode);

        void Dock();
        void Undock();
        /// <summary>
        /// This event get raised when the storage component (e.g. cart) is docked
        /// </summary>
        /// <remarks>
        /// At some point, I think we'll want to create our own EventArgs so we can specify which
        /// cart was docked at which dock, but for now I think this is good enough.
        /// </remarks>
        event DockEventHandler Docked;
        event DockEventHandler DockingError;
        /// <summary>
        /// This event get raised when the storage component (e.g. cart) is undocked
        /// </summary>
        /// <remarks>
        /// At some point, I think we'll want to create our own EventArgs so we can specify which
        /// cart was undocked from which dock, but for now I think this is good enough.
        /// </remarks>
        event DockEventHandler Undocked;
        event DockEventHandler UndockingError;
    }
}
