using System.Collections.Generic;

namespace DeviceManagerDatabase
{
    /// <summary>
    /// Contains information about the device, like product name, company, and instance name,
    /// but also the properties specific to that device, like serial number, connection type, etc.
    /// </summary>
    public class DeviceInfo
    {
        public string CompanyName { get; set; }
        /// <summary>
        /// The name of the product, like "Hive", or "Bumblebee"
        /// </summary>
        public string ProductName { get; set; }
        /// <summary>
        /// A specific instance of the product in a system, like "Bumblebee #1"
        /// </summary>
        public string InstanceName { get; set; }
        /// <summary>
        /// Whether or not this device is disabled (i.e. cannot be used by the application)
        /// </summary>
        public bool Disabled { get; set; }
        public Dictionary<string,string> Properties { get; set; }

        public DeviceInfo( string company, string product, string name, bool disabled, IDictionary<string,string> properties)
        {
            CompanyName = company;
            ProductName = product;
            InstanceName = name;
            Disabled = disabled;
            Properties = new Dictionary<string,string>( properties);
        }
    }
}
