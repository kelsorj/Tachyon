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

namespace TrashPlugin
{
    [ PartCreationPolicy( CreationPolicy.NonShared)]
    [ Export( typeof( DeviceInterface))]
    public class TrashPlugin : AccessibleDeviceInterface, PlateSchedulerDeviceInterface
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        protected PlateLocation _location { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public TrashPlugin()
        {
            IList< PlatePlace> places = new List< PlatePlace>();
            places.Add( new PlatePlace( "Trash (landscape)"));
            places.Add( new PlatePlace( "Trash (portrait)"));
            _location = new PlateLocation( "Trash location", places);
        }

        #region AccessibleDeviceInterface implementation
        public void Connect()
        {
            // Trash requires no connection (for now).
            Connected = true;
        }

        public bool Connected
        {
            get;
            private set;
        }

        public void Home()
        {
            // Trash requires no homing.
        }

        public bool IsHomed
        {
            get { return true; }
        }

        public void Close()
        {
            // Trash requires no connection (for now).
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
            get { return "Trash"; }
        }

        public string Manufacturer
        {
            get { return "BioNex"; }
        }

        public string Description
        {
            get { return "Trash"; }
        }

        public void SetProperties( DeviceInfo device_info)
        {
            Name = device_info.InstanceName;
            // Trash needs no properties (for now).
        }

        public UserControl GetDiagnosticsPanel()
        {
            MessageBox.Show( "Trash does not have a diagnostics panel available");
            return null;
        }

        public void ShowDiagnostics()
        {
            MessageBox.Show( "Trash does not have a diagnostics panel available");
        }

        public IEnumerable< PlateLocation> PlateLocationInfo
        {
            get
            {
                return new List<PlateLocation> { _location };
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
        public event JobCompleteEventHandler JobComplete { add {} remove {} }
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
            active_plate.MarkJobCompleted();
            _location.Occupied.Reset();
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
