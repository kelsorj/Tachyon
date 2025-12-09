using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.DeviceInterfaces;

namespace BioNex.ComPilotPhantomRobot
{
    public class PhantomRobot : RobotInterface
    {
        #region RobotInterface Members

        public Dictionary<string, List<string>> GetTeachpointNames()
        {
            return new Dictionary<string,List<string>>();
        }

        public void Pick(string from_device_name, string from_teachpoint, string labware_name, bool portrait_orientation, BioNex.Shared.Utils.MutableString expected_barcode)
        {
        }

        public void Place(string to_device_name, string to_teachpoint, string labware_name, bool portrait_orientation, string expected_barcode)
        {
        }

        public void TransferPlate(string from_device, string from_teachpoint, string to_device, string to_teachpoint, string labware_name, bool portrait_orientation, BioNex.Shared.Utils.MutableString expected_barcode, bool no_retract_before_pick = false, bool no_retract_after_place = false)
        {
        }

        public void Delid(string from_device_name, string from_teachpoint, string to_delid_device_name, string to_delid_teachpoint, string labware_name, bool portrait_orientation)
        {
        }

        public void Relid(string from_relid_device_name, string from_relid_teachpoint, string to_device_name, string to_teachpoint, string labware_name, bool portrait_orientation)
        {
        }

        public List<string> ReadBarcodes(AccessibleDeviceInterface target_device, string from_teachpoint, string to_teachpoint, int rack_number, int barcodes_expected, int scan_velocity, int scan_acceleration, List<byte> reread_condition_mask, int barcode_misread_threshold = 0)
        {
            return new List<string>();
        }

        public string GetTrashLocationName()
        {
            return "Trash";
        }

        public void Park()
        {
        }

        public void MoveToDeviceLocation(AccessibleDeviceInterface device_to_move_to, string location_name, bool with_plate)
        {
        }

        public void MoveToDeviceLocationForBCRStrobe(AccessibleDeviceInterface device_to_move_to, string location_name, bool with_plate)
        {
        }

        public string SaveBarcodeImage(string filepath)
        {
            return "";
        }

        public void SafetyEventTriggeredHandler(object sender, EventArgs e)
        {
        }

        public void HandleBarcodeMisreads(List<BarcodeReadErrorInfo> misread_barcode_info, List<string> unbarcoded_plate_locations, Action gui_update_callback, UpdateInventoryLocationDelegate inventory_update_callback)
        {
        }

        public string ReadBarcode(double x, double z, int bcr_config_index)
        {
            return "";
        }

        public void ReloadTeachpoints()
        {
        }

        public bool CanReadBarcode()
        {
            return false;
        }

        public string LastReadBarcode
        {
            get { return ""; }
        }

        public bool BusVoltageOk
        {
            get { return true; }
        }

        public event PickOrPlaceCompleteEventHandler PickComplete;

        public event PickOrPlaceCompleteEventHandler PlaceComplete;

        #endregion
    }
}
