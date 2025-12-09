using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.LiquidLevelDevice
{
    class BeeSureIntegrationSamples
    {
        void main()
        {
            BeeSureIntegration bsi = new BeeSureIntegration();
            
            // use the Synapsis device manager to get device properties
            // only need to do this when your app launches
            var db = new BioNex.SynapsisPrototype.DeviceManagerDatabase();
            var device_properties = db.GetProperties("BioNex", BioNex.Shared.DeviceInterfaces.BioNexDeviceNames.BeeSure, "Liquid Level Sensor");
            device_properties["company"] = "BioNex";
            device_properties["product"] = BioNex.Shared.DeviceInterfaces.BioNexDeviceNames.BeeSure;
            device_properties["name"] = "My BeeSure";
            // 0 = don't simulate, 1 = simulate
            device_properties["simulate"] = "0";
            // set the motor_adapter value to be the lowest of the IDs present.  If you open the USB-CANmodul Utility,
            // you will see the device IDs enumerated.
            int motor_adapter = 0;
            int sensor_adapter = motor_adapter + 1;
            device_properties["motor CAN device id"] = motor_adapter.ToString();
            device_properties["sensor CAN device id"] = sensor_adapter.ToString();

            // you can create your own labware "database", which is just a lookup table of strings to
            // a class that implements the IBeeSureLabwareProperties interface
            Dictionary<string, IBeeSureLabwareProperties> labware_database = new Dictionary<string, IBeeSureLabwareProperties>();
            // add your plate definitions like this
            string selected_labware = "96 well plate";
            labware_database.Add( selected_labware, new BeeSureLabware(selected_labware, (short)num_rows, (short)num_columns, row_spacing, column_spacing, thickness, well_radius));

            // all BeeSure methods block
            bsi.Initialize(device_properties);
            // must home the device after power on
            bsi.Home();
            // calibrate the device for the selected labware
            
            IBeeSureLabwareProperties labware_properties = GetLabwareProperties(selected_labware);
            bsi.Calibrate(labware_properties);
            // scan the plate
            bsi.Capture(labware_properties);
        }
    }
}
