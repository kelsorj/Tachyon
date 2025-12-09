using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.CustomerGUIPlugins
{
    /*
    /// <summary>
    /// Tells the system which dock's cart to reinventory, and possibly other information
    /// </summary>
    public class ReinventoryCartMessage
    {
        public string DockName { get; private set; }

        /// <summary>
        /// Tells the system to reinventory a cart, and hands over the coordinate of the barcode so it knows where to scan for the dock ID.
        /// </summary>
        public ReinventoryCartMessage( string dock_name)
        {
            DockName = dock_name;
        }
    }

    /// <summary>
    /// Tells the system which location in which cart to move a plate to
    /// </summary>
    public class MovePlateMessage
    {
        /// <summary>
        /// The barcode of the plate that LIMS wants moved to a cart.  Based on the barcode, the system should know
        /// where the plate is located in Hive storage.
        /// </summary>
        public string Barcode { get; set; }

        public string Device {get;set;}
        /// <summary>
        /// The destination determines where the plate is supposed to go to.
        /// Destination is a location in the device to put the plate
        /// </summary>
        public string Destination { get; set; }

        public string Labware { get; set; }

        public MovePlateMessage( string barcode, string device, string destination, string labware)
        {
            Barcode = barcode;
            Device = device;
            Destination = destination;
            Labware = labware;
        }
    }

    /// <summary>
    /// Tells the system to load a plate.  Hive will decide which location is optimal, or at least available.
    /// </summary>
    public class LoadPlateMessage
    {
        /// <summary>
        /// Hopefully, we'll be able to transmit the barcode along with the plate when we Upstack it.
        /// </summary>
        public string Barcode { get; private set; }
        /// <summary>
        /// for Phase 1, this is technically always going to be HOLD, but we'll use it anyway.
        /// </summary>
        public string Fate { get; private set; }

        public string Labware { get; private set; }

        public LoadPlateMessage( string labware, string barcode, string fate)
        {
            Labware = labware;
            Barcode = barcode;
            Fate = fate;
        }
    }

    /// <summary>
    /// Tells the system to unload a plate.  Hive will decide which plate to unload.
    /// </summary>
    public class UnLoadPlateMessage
    {
        public string Labware { get; private set; }

        public UnLoadPlateMessage( string labware)
        {
            Labware = labware;
        }
    }

    public class DockCartRequestMessage
    {
        public string DockName { get; private set; }

        public DockCartRequestMessage( string dock_name)
        {
            DockName = dock_name;
        }
    }

    public class DockCartCompleteMessage
    {
        public string DockName { get; private set; }

        public DockCartCompleteMessage( string dock_name)
        {
            DockName = dock_name;
        }
    }

    public class UndockCartRequestMessage
    {
        public string DockName { get; private set; }

        public UndockCartRequestMessage( string dock_name)
        {
            DockName = dock_name;
        }
    }

    public class UndockCartCompleteMessage
    {
        public string DockName { get; private set; }

        public UndockCartCompleteMessage( string dock_name)
        {
            DockName = dock_name;
        }
    }
     */

    public class MakeLocationAvailableMessage
    {
        private string _location_xml { get; set; }

        public MakeLocationAvailableMessage( string location_xml)
        {
            _location_xml = location_xml;
        }
    }
}
