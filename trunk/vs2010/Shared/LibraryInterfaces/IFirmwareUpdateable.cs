using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Shared.LibraryInterfaces
{
    public struct FirmwareVersionInfo
    {
        public string AxisName;
        public byte AxisId;
        public int CurrentMajorVersion;
        public int CurrentMinorVersion;
        public int LatestCompatibleMajorVersion;
        public int LatestCompatibleMinorVersion;
    }

    public class FirmwareDeviceInfo
    {
        public string Name { get; private set; }
        public string ProductName { get; private set; }
        public string SerialNumber { get; private set; }
        public int AdapterId { get; private set; }
        public IFirmwareUpdateable DeviceRef { get; private set; }
        public List<FirmwareVersionInfo> FirmwareVersionInfo { get; private set; }
        public bool UpdateAvailable { get; private set; }

        public FirmwareDeviceInfo( string name, string product_name, string serial_number, int adapter_id, IFirmwareUpdateable device_ref,
                                   List<FirmwareVersionInfo> firmware_version_info, bool update_available)
        {
            Name = name;
            ProductName = product_name;
            SerialNumber = serial_number;
            AdapterId = adapter_id;
            DeviceRef = device_ref;
            FirmwareVersionInfo = firmware_version_info;
            UpdateAvailable = update_available;
        }
    }

    public interface IFirmwareUpdateable
    {
        IList<FirmwareVersionInfo> GetCurrentAndCompatibleVersions();
        void UpgradeFirmware();
        FirmwareDeviceInfo GetDeviceInfo();
    }
}
