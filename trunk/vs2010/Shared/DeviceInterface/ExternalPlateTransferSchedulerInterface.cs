using System;

namespace BioNex.Shared.DeviceInterfaces
{
    public class PlateNotFoundException : ApplicationException
    {
        public string LabwareName { get; private set; }
        public string Barcode { get; private set; }

        public PlateNotFoundException( string labware_name, string barcode)
            : base( String.Format( "Could not find plate with barcode '{0}' and labware type '{1}'", barcode, labware_name))
        {
            LabwareName = labware_name;
            Barcode = barcode;
        }
    }

    public class PlateNotReachableException : ApplicationException
    {
        public string LocationName { get; private set; }
        public string Barcode { get; private set; }

        public PlateNotReachableException( string labware_name, string barcode, string location_name)
            : base( String.Format( "Plate '{0}' is not reachable by any robot because no teachpoint named '{1}' exists", barcode, location_name))
        {
            LocationName = location_name;
            Barcode = barcode;
        }
    }
    
    public class LocationNotReachableException : ApplicationException
    {
        public string LocationName{ get; private set; }

        public LocationNotReachableException( string location_name)
            : base( String.Format( "The location '{0}' is not reachable by any robot in the system", location_name))
        {
            LocationName = location_name;
        }
    }

    public class LabwareNameNotAvailableException : ApplicationException
    {
        public string LabwareName { get; private set; }

        public LabwareNameNotAvailableException( string labware_name)
            : base( String.Format( "The labware '{0}' is not present in any of the available plate storage devices", labware_name))
        {
            LabwareName = labware_name;
        }
    }
}
