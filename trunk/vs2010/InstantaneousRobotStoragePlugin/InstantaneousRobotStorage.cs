using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
//RJK 2-23-10 added to fake hive timings
using System.Threading;
using System.Windows.Forms;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.Location;
using BioNex.Shared.Utils;
using DeviceManagerDatabase;
using log4net;

namespace InstantaneousRobotStoragePlugin
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof( DeviceInterface))]
    [Export( typeof( RobotInterface))]
    [Export( typeof( PlateStorageInterface))]
    public class InstantaneousRobotStorageDevice : RobotInterface, PlateStorageInterface, AccessibleDeviceInterface
    {
        public event EventHandler ReinventoryBegin;
        public event EventHandler ReinventoryComplete;
        public event EventHandler ReinventoryError;
        public event PickOrPlaceCompleteEventHandler PickComplete;
        public event PickOrPlaceCompleteEventHandler PlaceComplete;
        private static readonly ILog _log = LogManager.GetLogger(typeof(InstantaneousRobotStorageDevice));
        private Dictionary<string,string> Properties { get; set; }

        // Device
        public string Manufacturer { get { return "BioNex"; } }
        public string ProductName { get { return "Speedy Robot"; } }
        public string Name { get; private set;}
        
        /// <summary>
        /// This is required if we want the accessible device to appear correctly (by device instance name) in the robot teaching tab
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }

        public string Description { get { return "handles all plate instantaneously without prompting"; } }
        public System.Windows.Controls.UserControl GetDiagnosticsPanel() { return new SpeedyRobotDiagnosticsPanel(); }
        public void ShowDiagnostics()
        {
            _log.Debug( "Displaying diagnostics");
        }
        public void Connect()
        {
            string message = "Connecting to Speedy Robot named '" + Name + "'";
            _log.Debug( message);
            Debug.WriteLine( message);
        }

        public bool Connected { get { return true; }}
        
        public void Home()
        {
        }

        public bool IsHomed
        {
            get
            {
                // always return true now.
                return true;
            }
        }

        public void SetProperties( DeviceInfo device_info)
        {
            Name = device_info.InstanceName;
            Properties = new Dictionary<string,string>( device_info.Properties);
            Debug.WriteLine( "Set properties for '" + Name + "'");
        }

        public void Close() {}

        public bool ExecuteCommand( string command, IDictionary<string,object> parameters) { throw new NotImplementedException(); }

        public IEnumerable<string> GetCommands() { return new List<string>(); }

        public IEnumerable<PlateLocation> PlateLocationInfo
        {
            get
            {
                return new List<PlateLocation> { new PlateLocation("Shelf 1"), new PlateLocation("Shelf 2") };
            }
        }

        public PlateLocation GetLidLocationInfo( string location_name)
        {
            return null;
        }

        public string TeachpointFilenamePrefix
        {
            get
            {
                return Name;
            }
        }

        public void Abort() {}
        public void Pause() {}
        public void Resume() {}
        public void Reset() {}

        // Robot
        public IDictionary< string, IList< string>> GetTeachpointNames()
        {
            _log.Debug( "Retrieving teachpoints");
            IDictionary< string, IList< string>> retval = new Dictionary< string, IList< string>>();
            retval.Add( "Device", new List< string>{ "Stage 1", "Stage 2", "Stage 3" });
            retval.Add( "BB Beta", new List< string>{ "BB PM 1", "BB PM 2", "BB PM 3"});
            retval.Add( "BB Pilot", new List< string>{ "BB PM 1", "BB PM 2", "BB PM 3", "BB PM 4"});
            retval.Add( BioNexDeviceNames.Bumblebee, new List< string>{ "BB PM 1", "BB PM 2", "BB PM 3", "BB PM 4"});
            retval.Add( "BNX1536", new List<string> { "Stage" } );
            retval.Add( "Plate Mover", new List<string> { "Stage, Landscape", "Stage, Portrait" } );
            retval.Add( "HiG", new List<string> { "Bucket 1", "Bucket 2" } );
            List< string> all = Get99Teachpoints();
            all.Add( "Trash");
            retval.Add( "Simulated Robot", all);
            return retval;
        }

        public double GetTransferWeight( DeviceInterface src_device, PlateLocation src_location, PlatePlace src_place, DeviceInterface dst_device, PlateLocation dst_location, PlatePlace dst_place)
        {
            return 1.0;
        }

        public void Pick(string from_device_name, string from_teachpoint, string labware_name, MutableString expected_barcode)
        {
            _log.DebugFormat( "Picking plate '{0}' from {1}", expected_barcode, from_teachpoint);
            if( PickComplete != null)
                PickComplete( this, null);
        }

        public void Place(string to_device_name, string to_teachpoint, string labware_name, string expected_barcode)
        {
            _log.DebugFormat( "Placing plate '{0}' at {1}", expected_barcode, to_teachpoint);
            if( PlaceComplete != null)
                PlaceComplete( this, null);
        }

        public void TransferPlate(string from_device, string from_teachpoint, string to_device, string to_teachpoint, string labware_name, MutableString expected_barcode, bool no_retract_before_pick = false, bool no_retract_after_place = false)
        {
            _log.DebugFormat( "Picking plate '{0}' from {1} and placing at {2}", expected_barcode, from_teachpoint, to_teachpoint);
            TimeSpan interval = new TimeSpan(0, 0, 3);
            _log.DebugFormat( "Picking and placing Sleep for {0} seconds", interval);
            // Reed, you can also just do Thread.Sleep( 10000), since Sleep can take an arg that's in ms.
            Thread.Sleep( interval);
            if( PickComplete != null)
                PickComplete( this, null);
            if( PlaceComplete != null)
                PlaceComplete( this, null);
        }

        public void Delid(string from_device_name, string from_teachpoint, string to_delid_device_name, string to_delid_teachpoint, string labware_name)
        {
            TransferPlate(from_device_name, from_teachpoint, to_delid_device_name, to_delid_teachpoint, labware_name, new MutableString(), true, false);
        }

        public void Relid(string from_delid_device_name, string from_delid_teachpoint, string to_device_name, string to_teachpoint, string labware_name)
        {
            TransferPlate(from_delid_device_name, from_delid_teachpoint, to_device_name, to_teachpoint, labware_name, new MutableString(), false, true);
        }

        public void MoveToDeviceLocation( AccessibleDeviceInterface device_to_move_to, string location_name, bool with_plate)
        {
        }

        public void MoveToDeviceLocationForBCRStrobe( AccessibleDeviceInterface device_to_move_to, string location_name, bool with_plate)
        {
        }

        public string SaveBarcodeImage( string filepath)
        {
            return "";
        }

        // Plate storage
        public void Unload( string labware_name, string barcode, string to_teachpoint)
        {
            string message = String.Format( "Unloading plate with barcode '{0}' to teachpoint '{1}", barcode, to_teachpoint);
            _log.Debug( message);
            //! \bug this is evil, and I did this knowingly because I didn't know how to deal with it yet.  You can't do GUI stuff
            //!      from a worker thread, and this will get called by a scheduler, which is running in a thread.  Need to utilize
            //!      future error reporting framework to deal with something like this.
            //RJK 2-23-10 added to fake hive timings
            //System.Windows.Forms.MessageBox.Show( message);
            TimeSpan interval = new TimeSpan(0, 0, 0);
            _log.DebugFormat( "Unloading sleep for {0} seconds", interval);
            // Reed, you can also just do Thread.Sleep( 7000), since Sleep can take an arg that's in ms.
            Thread.Sleep( interval);
            // find the location of the plate with barcode XXXX

            // pick the plate from this location

            // now place at unload teachpoint
        }

        public void Load( string labware_name, string barcode, string from_teachpoint)
        {
            string message = String.Format( "Loading plate with barcode '{0}' from teachpoint '{1}'", barcode, from_teachpoint);
            _log.Debug( message);
            //! \bug this is evil, and I did this knowingly because I didn't know how to deal with it yet.  You can't do GUI stuff
            //!      from a worker thread, and this will get called by a scheduler, which is running in a thread.  Need to utilize
            //!      future error reporting framework to deal with something like this.
            //RJK 2-23-10 added to fake hive timings
            //System.Windows.Forms.MessageBox.Show( message);
            TimeSpan interval = new TimeSpan(0, 0, 0);
            _log.DebugFormat( "Loading sleep for {0} seconds", interval);
            // Reed, you can also just do Thread.Sleep( 7000), since Sleep can take an arg that's in ms.
            Thread.Sleep( interval);
            //! \todo passing in a barcode doesn't make sense.  it's just temporary.
            //!       I just don't know what the inside of an incubator is going
            //!       to look like.  Is it rows / columns?  Or is it a hetergeneous
            //!       mixture of devices, like on the Hive?
            
            // pick up plate from teachpoint

            // load plate into specified location

        }

        public bool HasPlateWithBarcode( string barcode, out string location_name)
        {
            location_name = "Shelf 1";
            return true;
        }

        public IEnumerable<KeyValuePair<string,string>> GetInventory( string robot_name)
        {
            return new List<KeyValuePair<string,string>>();
        }

        public List< string> ReadBarcodes( AccessibleDeviceInterface target_device, string from_teachpoint, string to_teachpoint, int rack_number,
                                           int barcodes_expected, int scan_velocity, int scan_acceleration,
                                           List<byte> reread_condition_mask, int barcode_misread_threshold=0)
        {
            List< string> retval = new List< string>();
            for( int loop = 0; loop < barcodes_expected; ++loop){
                retval.Add( string.Format( "barcode {0}", loop + 1));
            }
            return retval;
        }

        public string ReadBarcode( double x, double z, int bcr_config_index=0) { return ""; }

        public bool CanReadBarcode() {return true;}

        public void Park() {}

        public void SafetyEventTriggeredHandler( object sender, EventArgs e) {}

        public IEnumerable< string> GetLocationsForLabware( string labware_name)
        {
            // for simulation purposes
            if( labware_name == "tipbox")
                return Get99Teachpoints();
            List< string> retval = new List< string> { "Shelf 1", "Shelf 2" };
            return retval;
        }

        public IEnumerable<string> GetStorageLocationNames()
        {
            return new List<string>();
        }

        private static List<string> Get99Teachpoints()
        {
            List<string> tipbox_locations = new List<string>();
            for( int i=1; i<=99; i++)
                tipbox_locations.Add( String.Format( "Shelf {0}", i));
            return tipbox_locations;
        }

        public bool Reinventory( bool park_robot_after)
        {
            try {
                if (ReinventoryBegin != null)
                    ReinventoryBegin(this, null);
                if( ReinventoryComplete != null)
                    ReinventoryComplete( this, null);
                return true;
            } catch( Exception) {
                if( ReinventoryError != null)
                    ReinventoryError( this, null);
                return false;
            }
            
        }

        public void DisplayInventoryDialog()
        {
            MessageBox.Show( "The instantaneous robot plugin does not support inventory viewing");
        }

        public void ReloadTeachpoints() {}

        public string LastReadBarcode { get { return "simulation";}}

        public int GetBarcodeReaderConfigurationIndex( string location_name)
        {
            return 0;
        }

        public void HandleBarcodeMisreads( List<BarcodeReadErrorInfo> misread_barcode_info, List<string> unbarcoded_plate_locations,
                                    Action gui_update_callback, UpdateInventoryLocationDelegate inventory_update_callback)
        {
        }

        public bool BusVoltageOk { get { return true; } }
    }
}
