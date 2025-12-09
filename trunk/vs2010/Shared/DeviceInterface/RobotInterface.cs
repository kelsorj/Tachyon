using System;
using System.Collections.Generic;
using BioNex.Shared.Location;
using BioNex.Shared.Utils;

namespace BioNex.Shared.DeviceInterfaces
{
    public delegate void UpdateInventoryLocationDelegate( string location_name, string updated_barcode);

    public interface RobotInterface
    {
        IDictionary< string, IList< string>> GetTeachpointNames();
        double GetTransferWeight( DeviceInterface src_device, PlateLocation src_location, PlatePlace src_place, DeviceInterface dst_device, PlateLocation dst_location, PlatePlace dst_place);
        void Pick(string from_device_name, string from_teachpoint, string labware_name, MutableString expected_barcode);
        void Place(string to_device_name, string to_teachpoint, string labware_name, string expected_barcode);
        void TransferPlate(string from_device, string from_teachpoint, string to_device, string to_teachpoint, string labware_name, MutableString expected_barcode, bool no_retract_before_pick = false, bool no_retract_after_place = false);
        void Delid( string from_device_name, string from_teachpoint, string to_delid_device_name, string to_delid_teachpoint, string labware_name);
        void Relid(string from_relid_device_name, string from_relid_teachpoint, string to_device_name, string to_teachpoint, string labware_name);
        //! \todo change the last parameter from byte to something else that will allow us to get Intellisense to help out
        List< string> ReadBarcodes( AccessibleDeviceInterface target_device, string from_teachpoint, string to_teachpoint,
                                    int rack_number, int barcodes_expected, int scan_velocity, int scan_acceleration,
                                    List<byte> reread_condition_mask, int barcode_misread_threshold=0);
        void Park();
        void MoveToDeviceLocation( AccessibleDeviceInterface accessible_device, string location_name, bool with_plate);
        void MoveToDeviceLocationForBCRStrobe( AccessibleDeviceInterface device_to_move_to, string location_name, bool with_plate);
        string SaveBarcodeImage( string filepath);
        void SafetyEventTriggeredHandler( object sender, EventArgs e);
        void HandleBarcodeMisreads( List<BarcodeReadErrorInfo> misread_barcode_info, List<string> unbarcoded_plate_locations,
                                    Action gui_update_callback, UpdateInventoryLocationDelegate inventory_update_callback);
            
        /// <summary>
        /// Moves the robot to the specified position and switches the barcode reader to the specified index
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <param name="bcr_config_index">
        /// 0: one size fits all
        /// 1: Hive storage
        /// 2: BPS140 storage
        /// 3: Dock cart ID
        /// 4: Dock storage
        /// 5: Kung Fu Bottom Location
        /// </param>
        /// <returns></returns>
        string ReadBarcode( double x, double z, int bcr_config_index);
        /// <summary>
        /// Allows events from other devices like docks to cause
        /// </summary>
        void ReloadTeachpoints();
        bool CanReadBarcode();
        /// <summary>
        /// allows us to know what barcode was last picked by the robot
        /// </summary>
        string LastReadBarcode { get; }

        bool BusVoltageOk { get; }

        event PickOrPlaceCompleteEventHandler PickComplete;
        event PickOrPlaceCompleteEventHandler PlaceComplete;
    }

    public delegate void PickOrPlaceCompleteEventHandler( object sender, PickOrPlaceCompleteEventArgs e);

    public class PickOrPlaceCompleteEventArgs : EventArgs
    {
        public string Barcode { get; private set; }
        public string DeviceName { get; private set; }
        public string LocationName { get; private set; }

        public PickOrPlaceCompleteEventArgs( string barcode, string device_name, string location_name)
        {
            Barcode = barcode;
            DeviceName = device_name;
            LocationName = location_name;
        }
    }

    public class ScanningParameters
    {
        /// <summary>
        /// indicates that we got a NOREAD from the barcode reader
        /// </summary>
        public const byte RereadNoRead = 0x01;
        /// <summary>
        /// indicates that we got an empty string from the serial port listener thread
        /// </summary>
        public const byte RereadMissedStrobe = 0x02;
        /// <summary>
        /// indicates that we found a barcode, but shouldn't expect one (i.e. should have been a tipbox)
        /// </summary>
        public const byte FoundBarcode = 0x04;

        public int RackNumber { get; set; }
        public double StartingPointOffset { get; set; }
        public int TopShelfNumber { get; set; }
        public string TopShelfTeachpointName { get; set; }
        public string BottomShelfTeachpointName { get; set; }
        public short NumberOfShelves { get; set; }
        public double ScanVelocityMmPerSec { get; set; }
        public double ScanAccelMmPerSec2 { get; set; }
        public List<byte> RereadConditionMasks { get; set; }
        public int BarcodeMisreadThreshold { get; set; }

        public ScanningParameters( int rack_number, double starting_point_offset, int top_shelf_number,
                                   string top_shelf_teachpoint_name, string bottom_shelf_teachpoint_name,
                                   short number_of_shelves, double scan_velocity, double scan_acceleration,
                                   List<byte> reread_condition_masks, int barcode_misread_threshold)
        {
            RackNumber = rack_number;
            StartingPointOffset = starting_point_offset;
            TopShelfNumber = top_shelf_number;
            TopShelfTeachpointName = top_shelf_teachpoint_name;
            BottomShelfTeachpointName = bottom_shelf_teachpoint_name;
            NumberOfShelves = number_of_shelves;
            ScanVelocityMmPerSec = scan_velocity;
            ScanAccelMmPerSec2 = scan_acceleration;
            RereadConditionMasks = reread_condition_masks;
            BarcodeMisreadThreshold = barcode_misread_threshold;
        }
    }

    public class BarcodeReadErrorInfo
    {
        public string TeachpointName { get; private set; }
        /// <summary>
        /// Normally has the filename of the image to display, but could also be null if the image
        /// could not be saved, for whatever reason.
        /// </summary>
        public string ImagePath { get; set; }
        public string CurrentBarcode { get; set; }
        public string NewBarcode { get; set; }
        public bool NoPlatePresent { get; set; }
        public bool UnbarcodedPlatePresent { get; set; }

        public BarcodeReadErrorInfo( string teachpoint_name, string current_barcode, string image_path="")
        {
            TeachpointName = teachpoint_name;
            if( image_path == "")
                ImagePath = System.IO.Path.GetTempPath() + TeachpointName.Replace( ':', '-') + ".jpg";
            else
                ImagePath = image_path;
            CurrentBarcode = current_barcode;
        }
    }
}
