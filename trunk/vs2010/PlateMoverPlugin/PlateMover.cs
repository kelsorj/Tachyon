using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.Location;
using BioNex.Shared.PlateWork;
using BioNex.Shared.Utils;
using log4net;

namespace BioNex.PlateMover
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(DeviceInterface))]
    [Export(typeof(RobotInterface))]
    public class PlateMoverPlugin : AccessibleDeviceInterface, RobotInterface, PlateSchedulerDeviceInterface
    {       
        public static class PlateMoverCommands
        {
            public static readonly string MoveToInternalLandscapeTeachpoint = "movetointernallandscapeteachpoint";
            public static readonly string MoveToInternalPortraitTeachpoint = "movetointernalportraitteachpoint";
            public static readonly string MoveToExternalTeachpoint = "movetoexternalteachpoint";
        }

        private bool Simulating { get; set; }
        public ViewModel ViewModel { get; private set; }
        private Model Model { get; set; }
        
        private System.Windows.Window _diagnostics_window { get; set; }

        protected PlateLocation _location { get; set; }
        private static readonly ILog Log = LogManager.GetLogger( typeof( PlateMoverPlugin));

        [ImportingConstructor]
        public PlateMoverPlugin( [Import] Model model)
        {
            Model = model;
            ViewModel = new ViewModel( Model);
            IList< PlatePlace> places = new List< PlatePlace>();
            places.Add( new PlatePlace( "PlateMover (landscape)"));
            places.Add( new PlatePlace( "PlateMover (portrait)"));
            _location = new PlateLocation( "PlateMover stage", places);
        }

        #region DeviceInterface Members

        public string Manufacturer
        {
            get
            {
                return "BioNex";
            }
        }

        public string ProductName
        {
            get
            {
                return BioNexDeviceNames.PlateMover;
            }
        }

        public string Name { get; private set; }

        /// <summary>
        /// This is required if we want the accessible device to appear correctly (by device instance name) in the robot teaching tab
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }

        public string Description
        {
            get
            {
                return "Plate shuttle device";
            }
        }

        public System.Windows.Controls.UserControl GetDiagnosticsPanel()
        {
            DiagnosticsPanel diags = new DiagnosticsPanel();
            diags.DataContext = ViewModel;
            return diags;
        }

        public void  SetProperties(DeviceManagerDatabase.DeviceInfo device_info)
        {
            Name = device_info.InstanceName;
            Model.DeviceInstanceName = Name;
            Model.SetDeviceProperties( device_info.Properties);
        }

        public void  ShowDiagnostics()
        {
            if( _diagnostics_window == null) {
                _diagnostics_window = new System.Windows.Window();
                DiagnosticsPanel diags = new DiagnosticsPanel();
                diags.DataContext = ViewModel;
                _diagnostics_window.Content = diags;
                _diagnostics_window.Title =  Name + "- Diagnostics" + (Simulating ? " (Simulating)" : "");
                _diagnostics_window.Closed += new EventHandler(_diagnostics_window_Closed);
                _diagnostics_window.Height = 600;
                _diagnostics_window.Width = 850;
            }

            _diagnostics_window.Show();
            _diagnostics_window.Activate();
        }

        void _diagnostics_window_Closed(object sender, EventArgs e)
        {
            _diagnostics_window = null;
        }

        public void  Connect()
        {
            ViewModel.Connect( true);
        }

        public bool  Connected
        {
            get { return ViewModel.Connected; }
        }

        public void Home()
        {
            Model.HomeAxes(false);
        }

        public bool IsHomed { get { return ViewModel.Homed; } }

        public void  Close()
        {
            ViewModel.Connect( false);
        }

        public bool ExecuteCommand(string command, IDictionary<string,object> parameters)
        {
            try {
                if( command.ToLower() == PlateMoverCommands.MoveToInternalLandscapeTeachpoint) {
                    Model.MoveToHiveTeachpoint( 0);
                } else if( command.ToLower() == PlateMoverCommands.MoveToInternalPortraitTeachpoint) {
                    Model.MoveToHiveTeachpoint( 1);
                } else if( command.ToLower() == PlateMoverCommands.MoveToExternalTeachpoint) {
                    Model.MoveToExternalTeachpoint();
                }
            } catch( Exception) {
                return false;
            }

            return true;
        }

        public IEnumerable<string>  GetCommands()
        {
            return new List<string>();
        }

        public void  Abort() {}
        public void  Pause() {}
        public void  Resume() {}
        public void Reset()
        {
            Model.ResetPauseAbort();
        }

        public IEnumerable<PlateLocation> PlateLocationInfo
        {
            get
            {
                return new List<PlateLocation> { _location };
            }
        }

        public string GetLidLocationInfo( string location_name)
        {
            return null;
        }

        #endregion


        PlateLocation RobotAccessibleInterface.GetLidLocationInfo(string location_name)
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

        public int GetBarcodeReaderConfigurationIndex( string location_name)
        {
            return 0;
        }

        #region RobotInterface Members
        public IDictionary< string, IList< string>> GetTeachpointNames()
        {
            // throw new NotImplementedException();
            IDictionary< string, IList< string>> retval = new Dictionary< string, IList< string>>();
            return retval;
        }

        public double GetTransferWeight( DeviceInterface src_device, PlateLocation src_location, PlatePlace src_place, DeviceInterface dst_device, PlateLocation dst_location, PlatePlace dst_place)
        {
            return (( _location.Places.Contains( src_place)) && ( _location.Places.Contains( dst_place))) ? 1.0 : double.PositiveInfinity;
        }

        public void Pick(string from_device_name, string from_teachpoint, string labware_name, MutableString expected_barcode)
        {
            throw new NotImplementedException();
        }

        public void Place(string to_device_name, string to_teachpoint, string labware_name, string expected_barcode)
        {
            throw new NotImplementedException();
        }

        public void TransferPlate(string from_device, string from_teachpoint, string to_device, string to_teachpoint, string labware_name, Shared.Utils.MutableString expected_barcode, bool no_retract_before_pick = false, bool no_retract_after_place = false)
        {
            throw new NotImplementedException();
        }

        public void Delid(string from_device_name, string from_teachpoint, string to_delid_device_name, string to_delid_teachpoint, string labware_name)
        {
            throw new NotImplementedException();
        }

        public void Relid(string from_relid_device_name, string from_relid_teachpoint, string to_device_name, string to_teachpoint, string labware_name)
        {
            throw new NotImplementedException();
        }

        public List< string> ReadBarcodes( AccessibleDeviceInterface target_device, string from_teachpoint, string to_teachpoint, int rack_number, int barcodes_expected, int scan_velocity, int scan_acceleration, List< byte> reread_condition_mask, int barcode_misread_threshold = 0)
        {
            throw new NotImplementedException();
        }

        public void Park()
        {
            throw new NotImplementedException();
        }

        public void MoveToDeviceLocation( AccessibleDeviceInterface device_to_move_to, string location_name, bool with_plate)
        {
            throw new NotImplementedException();
        }

        public void MoveToDeviceLocationForBCRStrobe( AccessibleDeviceInterface device_to_move_to, string location_name, bool with_plate)
        {
            throw new NotImplementedException();
        }

        public string SaveBarcodeImage( string filepath)
        {
            throw new NotImplementedException();
        }

        public void SafetyEventTriggeredHandler( object sender, EventArgs e)
        {
            
        }

        public void HandleBarcodeMisreads( List< BarcodeReadErrorInfo> misread_barcode_info, List< string> unbarcoded_plate_locations, Action gui_update_callback, UpdateInventoryLocationDelegate inventory_update_callback)
        {
            throw new NotImplementedException();
        }

        public string ReadBarcode( double x, double z, int bcr_config_index)
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

        public bool BusVoltageOk
        {
            get { throw new NotImplementedException(); }
        }

        public event PickOrPlaceCompleteEventHandler PickComplete { add {} remove {} }

        public event PickOrPlaceCompleteEventHandler PlaceComplete { add {} remove {} }
        #endregion

        #region PlateSchedulerDeviceInterface Members
        public event JobCompleteEventHandler JobComplete { add {} remove {} }
        public PlateLocation GetAvailableLocation( ActivePlate active_plate)
        {
            throw new NotImplementedException();
        }

        public bool ReserveLocation( PlateLocation location, ActivePlate active_plate)
        {
            throw new NotImplementedException();
        }

        public void LockPlace( PlatePlace place)
        {
            int orientation = place.Name.Contains( "(landscape)") ? 0 : 1;
            Model.MoveToHiveTeachpoint( orientation);
        }

        public void AddJob( ActivePlate active_plate)
        {
            throw new NotImplementedException();
        }

        public void EnqueueWorklist( Worklist worklist)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
