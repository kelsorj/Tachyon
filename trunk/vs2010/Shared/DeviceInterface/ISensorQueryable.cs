using System;

namespace BioNex.Shared.DeviceInterfaces
{
    public interface ISensorQueryable
    {
        Func<bool> GetSensorCallback( string location_name);
    }
}
