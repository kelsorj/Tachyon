using System.Collections.Generic;
using System.Linq;
using BioNex.Shared.Utils;

namespace BioNex.Plugins.Dock
{
    public class DeviceManagerProperties
    {
        public string IODevice { get; private set; }
        public string SystemIODevice { get; private set; }
        public string RobotDevice { get; private set; }
        public int PresenceSensorInputIndex { get; private set; }
        public List<int> CartLockOutputIndexes { get; private set; }
        public bool Simulating { get; private set; }
        public int DoorSensorInputIndex { get; private set; }
        public string ConfigFolder { get; private set; }
        public double BarcodeX { get; private set; }
        public double BarcodeZ { get; private set; }

        public DeviceManagerProperties( IDictionary<string,string> properties)
        {
            IODevice = properties["dock i/o device name"];
            SystemIODevice = properties["system i/o device name"];
            RobotDevice = properties["barcoding robot device name"];
            PresenceSensorInputIndex = properties["cart presence sensor input index"].ToInt();
            string value;
            if( properties.TryGetValue( "door sensor input index", out value))
                DoorSensorInputIndex = value.ToInt();
            else
                DoorSensorInputIndex = -1; // default value
            CartLockOutputIndexes = (from x in properties["cart lock output index(s)"].Split(',') select int.Parse(x)).ToList();
            Simulating = properties["simulate dock"] != "0";
            if( properties.TryGetValue( "dock configuration folder", out value))
                ConfigFolder = value;
            else
                ConfigFolder = "";
            BarcodeX = properties["cart ID X position"].ToDouble();
            BarcodeZ = properties["cart ID Z position"].ToDouble();
        }
    }
}
