using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.Location;
using BioNex.Shared.PlateWork;
using DeviceManagerDatabase;

namespace LLSPlaceholder
{
    [ PartCreationPolicy( CreationPolicy.NonShared)]
    [ Export( typeof( DeviceInterface))]
    public class LLSPlaceholder : AccessibleDeviceInterface, PlateSchedulerDeviceInterface
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        protected PlateLocation _location { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public LLSPlaceholder()
        {
            IList< PlatePlace> places = new List< PlatePlace>();
            _location = new PlateLocation( "LLS location");
        }

        #region AccessibleDeviceInterface implementation
        public void Connect()
        {
            // LLSPlaceholder requires no connection (for now).
            Connected = true;
        }

        public bool Connected
        {
            get;
            private set;
        }

        public void Home()
        {
            // LLSPlaceholder requires no homing.
        }

        public bool IsHomed
        {
            get { return true; }
        }

        public void Close()
        {
            // LLSPlaceholder requires no connection (for now).
        }

        public bool ExecuteCommand( string command, IDictionary< string, object> parameters)
        {
            throw new NotImplementedException();
        }

        public IEnumerable< string> GetCommands()
        {
            throw new NotImplementedException();
        }

        public void Abort()
        {
        }

        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public string Name
        {
            get;
            private set;
        }

        public string ProductName
        {
            get { return "LLS"; }
        }

        public string Manufacturer
        {
            get { return "BioNex"; }
        }

        public string Description
        {
            get { return "LLS"; }
        }

        public void SetProperties( DeviceInfo device_info)
        {
            Name = device_info.InstanceName;
            // LLSPlaceholder needs no properties (for now).
        }

        public UserControl GetDiagnosticsPanel()
        {
            MessageBox.Show( "LLSPlaceholder does not have a diagnostics panel available");
            return null;
        }

        public void ShowDiagnostics()
        {
            MessageBox.Show( "LLSPlaceholder does not have a diagnostics panel available");
        }

        public IEnumerable< PlateLocation> PlateLocationInfo
        {
            get
            {
                return new List<PlateLocation>() { _location };
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

        public int GetBarcodeReaderConfigurationIndex( string location_name)
        {
            throw new NotImplementedException();
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

        protected void JobThread( ActivePlate active_plate)
        {
            active_plate.WaitForPlate();
            if( JobComplete != null){
                JobComplete( this, new JobCompleteEventArguments(){ PlateBarcode = active_plate.Barcode});
            }
            active_plate.MarkJobCompleted();
        }

        public void EnqueueWorklist( Worklist worklist)
        {
            throw new NotImplementedException();
        }
        #endregion

        public override string ToString()
        {
            return Name;
        }
    }
}
