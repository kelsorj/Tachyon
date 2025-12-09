using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Location;
using BioNex.Shared.PlateWork;
using BioNex.Shared.Utils.WellMathUtil;
using System.Windows;

namespace BioNex.LiquidLevelDevice
{
    /// <summary>
    /// An LLSensorPlugin is a device consisting of an array of LLSensors, and an x, y, and z motion platform.
    /// </summary>
    public interface ILLSensorPlugin
    {
        event CaptureStartEventHandler CaptureStartEvent;
        event CaptureProgressEventHandler CaptureProgressEvent;
        event CaptureStopEventHandler CaptureStopEvent;
        event SavePropertiesEventHandler SavePropertiesEvent;

        ILLSensorModel Model { get; }
        ILabwareDatabase LabwareDatabase { get; set; }

        IDictionary<string, string> Properties { get; }

        bool IsConnected { get; }

        void MoveToParkPosition(bool portrait);
        void Calibrate();
        List<Averages> Capture(string labware_name);
        IDictionary<Coord, List<Measurement>> HiResScan(bool fast, string labware_name);
        IDictionary<Coord, List<Measurement>>[] LocateTeachpoint();
        IDictionary<Coord, List<Measurement>> XYAlignmentScan(bool reset_slope);
        IDictionary<Coord, List<Measurement>> ZYAlignmentScan(bool reset_slope);
        IList<IDictionary<Coord, List<Measurement>>> XArcCorrectionScan(bool reset_correction);

        Averages CalculateWellAverages(List<Measurement> values);
    }

    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(DeviceInterface))]
    public class LLSensorPlugin : SystemStartupCheckInterface, ILLSensorPlugin, AccessibleDeviceInterface, PlateSchedulerDeviceInterface, IFirmwareUpdateable
    {
        ILLSensorModel _model;
        public ILLSensorModel Model { get { return _model; } }
        PlateLocation _location = new PlateLocation( "LLS location");

        [Import]
        public ILabwareDatabase LabwareDatabase { get; set; }
        [Import(AllowDefault=true)]
        public ILimsOutputTransferLog OutputPlugin { get; set; }

        public LLSensorPlugin()
        {
            _model = new LLSensorModel(this);
        }

        #region ILLSensorPlugin implementation

        public event CaptureStartEventHandler CaptureStartEvent { add { _model.CaptureStartEvent += value; } remove { _model.CaptureStartEvent -= value; } }
        public event CaptureProgressEventHandler CaptureProgressEvent { add { _model.CaptureProgressEvent += value; } remove { _model.CaptureProgressEvent -= value; } }
        public event CaptureStopEventHandler CaptureStopEvent { add { _model.CaptureStopEvent += value; } remove { _model.CaptureStopEvent -= value; } }

        public event SavePropertiesEventHandler SavePropertiesEvent { add { _model.SavePropertiesEvent += value; } remove { _model.SavePropertiesEvent -= value; } }

        public IDictionary<string,string> Properties { get { return _model.Properties; } }

        public bool IsConnected { get { return _model.Connected; } }

        public void MoveToParkPosition(bool portrait)
        {
            _model.MoveToPark(portrait ? TeachpointType.Portrait : TeachpointType.Landscape);
        }

        public void Calibrate()
        {
            _model.Calibrate();
        }

        public List<Averages> Capture(string labware_name)
        {
            return _model.Capture(labware_name);
        }

        public IDictionary<Coord, List<Measurement>> HiResScan(bool fast, string labware_name)
        {
            return _model.HiResScan(fast, labware_name);
        }

        public Averages CalculateWellAverages(List<Measurement> values)
        {
            return _model.CalculateWellAverages(values);
        }

        public IDictionary<Coord, List<Measurement>>[] LocateTeachpoint()
        {
            return _model.LocateTeachpoint();
        }

        public IDictionary<Coord, List<Measurement>> XYAlignmentScan(bool reset_slope)
        {
            return _model.XYAlignmentScan(reset_slope);
        }

        public IDictionary<Coord, List<Measurement>> ZYAlignmentScan(bool reset_slope)
        {
            return _model.ZYAlignmentScan(reset_slope);
        }

        public IList<IDictionary<Coord, List<Measurement>>> XArcCorrectionScan(bool reset_correction)
        {
            return _model.XArcCorrectionScan(reset_correction);
        }

        #endregion

        #region AccessibleDeviceInterface implementation
        public void Connect()
        {
            _model.Connect();
        }

        public bool Connected
        {
            get { return _model.Connected; }
        }

        public void Home()
        {
            _model.Home();
        }

        public bool IsHomed { get { return _model.IsHomed; } }

        public void Close()
        {
            _model.Disconnect();
        }

        public enum Commands { ScanPlate };
        public class CommandParameterNames
        {
            public const string plate_barcode = "plate_barcode";
            public const string labware_name = "labware_name";
        }

        public bool ExecuteCommand(string command, IDictionary<string, object> parameters)
        {
             //public void Capture(int labware_columns, double labware_spacing, double labware_thickness)
            // the only command allowed is ScanPlate
            if (command != Commands.ScanPlate.ToString())
                return false;

            var plate_barcode = (string)parameters[CommandParameterNames.plate_barcode];
            var labware_name = (string)parameters[CommandParameterNames.labware_name];
            List< Averages> scan_results = Capture(labware_name);
            IDictionary< string, double> well_to_volume_map = scan_results.ToDictionary( sr => new Well( sr.Channel, sr.Column).WellName, sr => sr.Average);
            OutputPlugin.LogLiquidLevel( plate_barcode, well_to_volume_map, DateTime.Now);
            return true;
        }

        public IEnumerable<string> GetCommands()
        {
            return new List<string> { Commands.ScanPlate.ToString() };
        }

        public void Abort()
        {
            _model.Abort();
        }

        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public void Reset()
        {            
        }

        /// <summary>
        /// This is required if we want the accessible device to appear correctly (by device instance name) in the robot teaching tab
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }

        public string Name
        {
            get { return _model.InstanceName; }
            set { _model.InstanceName = value; }
        }

        public string ProductName
        {
            get { return BioNexDeviceNames.BeeSure; }
        }

        public string Manufacturer
        {
            get { return "BioNex"; }
        }

        public string Description
        {
            get { return "BeeSure Liquid Level Sensor"; }
        }

        public void SetProperties(DeviceManagerDatabase.DeviceInfo device_info)
        {
            Name = device_info.InstanceName;
            _model.Properties = new LLProperties(device_info.Properties);
        }

        public System.Windows.Controls.UserControl GetDiagnosticsPanel()
        {
            throw new NotImplementedException();
        }

        public void ShowDiagnostics()
        {
            var window = new System.Windows.Window
                             {
                                 Content = new DiagnosticsPanel(this),
                                 Title = Name + "- Diagnostics",
                                 Height = 425,
                                 Width = 640, 
                                 Owner = Application.Current != null ? Application.Current.MainWindow : null
                             };
            window.Show();
        }

        public string TeachpointFilenamePrefix
        {
            get
            {
                return Name;
            }
        }

        public int GetBarcodeReaderConfigurationIndex(string location_name)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<PlateLocation> PlateLocationInfo
        {
            get
            {
                return new List<PlateLocation>() { _location };
            }
        }

        public PlateLocation GetLidLocationInfo(string location_name)
        {
            return null;
        }
        #endregion

        #region PlateSchedulerDeviceInterface implementation
        public event JobCompleteEventHandler JobComplete;
        public PlateLocation GetAvailableLocation( ActivePlate active_plate)
        {
            return _location.Available ? _location : null;
        }

        public bool ReserveLocation( PlateLocation location, ActivePlate active_plate)
        {
            // not my location to reserve.
            if( location != _location){
                return false;
            }
            // reserve location.
            location.Reserved.Set();
            return true;
        }

        public void LockPlace( PlatePlace place)
        {
        }

        public void AddJob( ActivePlate active_plate)
        {
            new Thread( () => JobThread( active_plate)){ Name = GetType().ToString() + " Job Thread", IsBackground = true}.Start();
        }

        protected void JobThread(ActivePlate active_plate)
        {
            active_plate.WaitForPlate();
            try
            {
                IDictionary<string, object> parameters = new Dictionary<string, object>();
                parameters[CommandParameterNames.plate_barcode] = active_plate.Barcode;
                parameters[CommandParameterNames.labware_name] = active_plate.LabwareName;
                DeviceInterface d = (this as DeviceInterface);
                if( d.ExecuteCommand(active_plate.GetCurrentToDo().Command, parameters) && ( JobComplete != null)){
                    JobComplete( this, new JobCompleteEventArguments{ PlateBarcode = active_plate.Barcode});
                }
            }
            catch (Exception)
            {
                // Log.Debug( "Exception occurred");
            }
            active_plate.MarkJobCompleted();
        }

        public void EnqueueWorklist( Worklist worklist)
        {
            throw new NotImplementedException();
        }
        #endregion    

        #region SystemStartupCheckInterface implementation
        public override bool IsReady( out string reason_not_ready)
        {
            reason_not_ready = "";
            return true;
        }
        public override System.Windows.Controls.UserControl GetSystemPanel()
        {
            if (!_model.Properties.GetBool(LLProperties.EnableSystemPanel))
                return null;
            ScanProgressPanel panel = new ScanProgressPanel();
            panel.Plugin = this;
            panel.Width = 60;
            panel.Height = 60;
            return panel;
        }
        #endregion

        #region IFirmwareUpdateable implementation

        public IList<FirmwareVersionInfo> GetCurrentAndCompatibleVersions()
        {
            return ((IFirmwareUpdateable)_model).GetCurrentAndCompatibleVersions();
        }

        public void UpgradeFirmware()
        {
            ((IFirmwareUpdateable)_model).UpgradeFirmware();            
        }

        public FirmwareDeviceInfo GetDeviceInfo()
        {
            return ((IFirmwareUpdateable)_model).GetDeviceInfo();
        }

        #endregion
    }
}
