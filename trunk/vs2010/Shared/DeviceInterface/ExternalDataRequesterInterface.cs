using System;
using System.Collections.Generic;

namespace BioNex.Shared.DeviceInterfaces
{
    public interface ExternalDataRequesterInterface
    {
        SystemStartupCheckInterface.SafeToMoveDelegate SafeToMove { get; set; }

        int GetDeviceTypeId( string company_name, string product_name);
        IEnumerable<DeviceInterface> GetDeviceInterfaces();
        IEnumerable<AccessibleDeviceInterface> GetAccessibleDeviceInterfaces();
        IEnumerable<RobotInterface> GetRobotInterfaces();
        IEnumerable<PlateStorageInterface> GetPlateStorageInterfaces();
        IEnumerable<IOInterface> GetIOInterfaces();
        IEnumerable<SystemStartupCheckInterface> GetSystemStartupCheckInterfaces();
        IEnumerable<SafetyInterface> GetSafetyInterfaces();
        IEnumerable<StackerInterface> GetStackerInterfaces();
        IEnumerable<DockablePlateStorageInterface> GetDockablePlateStorageInterfaces();
        // DKM 2011-04-05 not sure I like this... but just testing for now.
        Func<bool> ReadSensorCallback( string device_name, string location_name);
    }
}
