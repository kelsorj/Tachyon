using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.PlateDefs;

namespace BioNex.PlateStorageStackerMockPlugin
{
    public class PlateStorageStacker : StackerInterface, DeviceInterface, RobotInterface, PlateStorageInterface
    {
        private Controller _controller;

        public PlateStorageStacker()
        {
            _controller = new Controller();
        }

        public void Upstack( Plate plate)
        {

        }

        public void Downstack( Plate plate)
        {

        }

        #region DeviceInterface Members

        public string Manufacturer
        {
            get
            {
                return BioNex.Shared.DeviceInterfaces.BioNexAttributes.CompanyName;
            }
        }

        public string ProductName
        {
            get
            {
                return "PlateStorageStacker";
            }
        }

        public string Name
        {
            get
            {
                return "PlateStorageStacker";
            }
        }

        public string Description
        {
            get
            {
                return "Storage device that just looks like a stacker to the client";
            }
        }

        public System.Windows.Controls.UserControl GetDiagnosticsPanel()
        {
            throw new NotImplementedException();
        }

        public void ShowDiagnostics()
        {
            throw new NotImplementedException();
        }

        public void Initialize( int device_id)
        {
            // initialize the controller
            _controller.Initialize();
        }

        public void Close()
        {
            _controller.Close();
        }

        public bool Connected
        {
            get { throw new NotImplementedException(); }
        }

        public void Pause()
        {
            throw new NotImplementedException();
        }

        public void Resume()
        {
            throw new NotImplementedException();
        }

        public void SetProperties(DeviceManagerDatabase.DeviceInfo device_info)
        {
            throw new NotImplementedException();
        }

        public void Connect()
        {
            throw new NotImplementedException();
        }

        public void Home()
        {
            throw new NotImplementedException();
        }

        public bool IsHomed { get { throw new NotImplementedException(); } }

        public bool ExecuteCommand(string command, Dictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetCommands()
        {
            throw new NotImplementedException();
        }

        public void Abort()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetPlateLocationNames()
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region PlateStorageInterface Members

        public void Unload(string labware_name, string barcode, string to_teachpoint)
        {
            throw new NotImplementedException();
        }

        public void Load(string labware_name, string barcode, string from_teachpoint)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region RobotInterface Members

        public List<string> GetTeachpointNames()
        {
            throw new NotImplementedException();
        }

        public void Pick(string from_teachpoint, double approach_height, double gripper_offset)
        {
            throw new NotImplementedException();
        }

        public void Place(string to_teachpoint, double approach_height, double gripper_offset)
        {
            throw new NotImplementedException();
        }

        public void TransferPlate(string from_teachpoint, double from_approach_height, double from_gripper_offset, string to_teachpoint, double to_approach_height, double to_gripper_offset)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region RobotInterface Members

        Dictionary<string, List<string>> RobotInterface.GetTeachpointNames()
        {
            throw new NotImplementedException();
        }

        public void Pick(string from_device_name, string from_teachpoint, string labware_name, bool portrait_orientation, string expected_barcode)
        {
            throw new NotImplementedException();
        }

        public void Place(string to_device_name, string to_teachpoint, string labware_name, bool portrait_orientation, string expected_barcode)
        {
            throw new NotImplementedException();
        }

        public void TransferPlate(string from_device, string from_teachpoint, string to_device, string to_teachpoint, string labware_name, bool portrait_orientation, string expected_barcode, bool no_retract_before_pick = false, bool no_retract_after_place = false)
        {
            throw new NotImplementedException();
        }

        public void Delid(string from_device_name, string from_teachpoint, string to_delid_device_name, string to_delid_teachpoint, string labware_name, bool portrait_orientation)
        {
            throw new NotImplementedException();
        }

        public void Relid(string from_relid_device_name, string from_relid_teachpoint, string to_device_name, string to_teachpoint, string labware_name, bool portrait_orientation)
        {
            throw new NotImplementedException();
        }

        public List<string> ReadBarcodes(AccessibleDeviceInterface target_device, string from_teachpoint, string to_teachpoint, int rack_number, int barcodes_expected, List<byte> reread_condition_mask, int barcode_misread_threshold = 0)
        {
            throw new NotImplementedException();
        }

        public string GetTrashLocationName()
        {
            throw new NotImplementedException();
        }

        public void Park()
        {
            throw new NotImplementedException();
        }

        public void MoveToDeviceLocation(AccessibleDeviceInterface device_to_move_to, string location_name, bool with_plate)
        {
            throw new NotImplementedException();
        }

        public void MoveToDeviceLocationForBCRStrobe(AccessibleDeviceInterface device_to_move_to, string location_name, bool with_plate)
        {
            throw new NotImplementedException();
        }

        public string SaveBarcodeImage(string filepath)
        {
            throw new NotImplementedException();
        }

        public void SafetyEventTriggeredHandler(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public string ReadBarcode(double x, double z)
        {
            throw new NotImplementedException();
        }

        public void ReloadTeachpoints()
        {
            throw new NotImplementedException();
        }

        public bool CanReadBarcode()
        {
            throw new NotImplementedException();
        }

        public string LastReadBarcode
        {
            get { throw new NotImplementedException(); }
        }

        public event PickOrPlaceCompleteEventHandler PickComplete;

        public event PickOrPlaceCompleteEventHandler PlaceComplete;

        #endregion

        #region PlateStorageInterface Members

        public bool HasPlateWithBarcode(string barcode, out string location_name)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetLocationsForLabware(string labware_name)
        {
            throw new NotImplementedException();
        }

        public bool Reinventory()
        {
            throw new NotImplementedException();
        }

        public event EventHandler ReinventoryComplete;

        public event EventHandler ReinventoryError;

        public IEnumerable<KeyValuePair<string, string>> GetInventory(string robot_name)
        {
            throw new NotImplementedException();
        }

        public void DisplayInventoryDialog()
        {
            throw new NotImplementedException();
        }

        public event EventHandler ReinventoryBegin;

        public bool Reinventory(bool park_robot_after)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetStorageLocationNames()
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
