using System;

namespace BioNex.Shared.DeviceInterfaces
{
    public interface IPlateLocationTracker
    {
        /// <summary>
        /// Sets the location of a barcoded plate.  Call this when placing at a location.
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="device"></param>
        /// <param name="location_name"></param>
        void RecordPlateLocation( string barcode, DeviceInterface device, string location_name);
        /// <summary>
        /// Clears the location of a barcoded plate.  Call this when picking from a location.
        /// </summary>
        /// <param name="barcode"></param>
        void ClearPlateLocation( string barcode);
        /// <summary>
        /// Gets the device's location for a given plate barcode.
        /// </summary>
        /// <param name="barcode"></param>
        /// <returns></returns>
        Tuple<DeviceInterface,string> GetPlateLocation( string barcode);
        /// <summary>
        /// Get the plate barcode that is currently at a given device's location.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="location_name"></param>
        /// <returns></returns>
        string GetPlateAtLocation( DeviceInterface device, string location_name);
    }
}
