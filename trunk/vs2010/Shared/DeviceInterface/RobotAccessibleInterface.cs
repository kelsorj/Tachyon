using System.Collections.Generic;
using BioNex.Shared.Location;

namespace BioNex.Shared.DeviceInterfaces
{
    public interface RobotAccessibleInterface
    {
        /// <summary>
        /// This method is called by all robots through the application.  For example, if
        /// the Hive has storage as well as a BPS140, it will need to know all of its
        /// plate locations, as well as all of the locations availablein the BPS140.
        /// I think they will need to be repackaged by the application, keyed by
        /// device name (which should be a unique constraint enforced by the DeviceManager).
        /// </summary>
        /// <returns></returns>
        IEnumerable<PlateLocation> PlateLocationInfo { get; }

        /// <summary>
        /// This is how a robot asks a device what location to use for storing a lid.
        /// </summary>
        /// <remarks>
        /// For example, if the Hive is delidding a plate at the Bumblebee's PM1 location,
        /// it would call GetLidLocationInfo( "BB PM 1"), and the Bumblebee should return
        /// a teachpoint name, such as "BB PM 1 lid location".  The robot should call this
        /// method in addition to PlateLocationInfo when populating the list of
        /// plate locations.  I guess for now, we'll have to call it for each plate location
        /// and I'm not sure if this is worse than adding a method that returns all of
        /// the lid locations.
        /// </remarks>
        /// <param name="location_name"></param>
        /// <returns></returns>
        PlateLocation GetLidLocationInfo( string location_name);

        /// <summary>
        /// Gets the string to prepend to "_teachpoints.xml" to generate a teachpoint filename.
        /// </summary>
        /// <remarks>
        /// The current robot teachpoint filename approach is to use [device instance name]_teachpoints.xml.  We would
        /// just use DeviceInterface.Name before, but now that we have docks, we need something that allows us
        /// to use something other than the device instance name.  So now we have GetTeachpointFilenamePrefix.  For
        /// non-dock devices, this will just return DeviceInterface.Name.  For Dock devices, a name can be
        /// returned based on the docked cart ID.
        /// </remarks>
        /// <returns></returns>
        string TeachpointFilenamePrefix { get; }

        /// <summary>
        /// Some devices need a specific WOI loaded so that the barcode reader doesn't pick up extraneous
        /// barcode data.  By passing in a location name, the device driver can return the desired
        /// configuration index to use.
        /// </summary>
        /// <remarks>
        /// Obviously this requires some agreement ahead of time for which devices will use which index.
        /// The dock is interesting because it might actually need two -- one for the cart ID, and another
        /// for flyby barcode reading.  The initial implementation will have pre-determined, hard-coded
        /// configuration indexes until I have proven that it works.  Then we can discuss the details
        /// behind making it configurable.
        /// </remarks>
        /// <returns>
        /// 0 if the device doesn't care (like Bumblebee).  This configuration index's settings
        /// is basically the one-size-fits-all settings that we used before having the ability to switch.
        /// </returns>
        int GetBarcodeReaderConfigurationIndex( string location_name);
    }
}
