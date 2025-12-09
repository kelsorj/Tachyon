using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using System.Threading;

namespace BioNex.HiveIntegration
{
    public class HiveServerImpl : HiveIntegration.HiveRpcInterface
    {
        private static readonly ILog _log = LogManager.GetLogger( typeof( HiveServerImpl));
        private IntegrationGui _gui;
        public bool Connected { get; private set; }

        public event EventHandler ClientConnected;
        public event EventHandler ClientDisconnected;

        public HiveServerImpl( IntegrationGui gui)
        {
            _gui = gui;
        }

        public override void Ping()
        {            
            _log.Debug( "Ping() called");
        }

        public override void Initialize(string xml_parameters)
        {
            _log.DebugFormat( "Initialize(\"{0}\", \"{1}\") called", xml_parameters);
            if( !_gui.DevicesAreHomed() && !_gui.ExecuteHomeAllCommandHelper( prompt_user:false)) { 
                string error = "Hive was unable to home.  Please try homing from the Hive system to diagnose any problems, and then try again.";
                _log.Error( error);
                throw new Exception( error);
            }
            // since ExecuteHomeAllCommand runs in a thread, we need to block here until we've determined that all devices are homed.
            // if we were already homed, then the following check will just fall through, but we need to do this because it takes
            // time for the homing statuses to get reset on all of the devices
            while( !_gui._synapsisQuery.Value.AllDevicesHomed)
                Thread.Sleep( 100);
            // now wait for homing to complete
            HiveIntegration.HiveXmlHelper.InitializeParams init_params = HiveIntegration.HiveXmlHelper.XmlToInitializeParams( xml_parameters);
            DateTime start = DateTime.Now;
            int allowable_timeout = init_params.TimeoutSec < 30 ? 30 : init_params.TimeoutSec;
            while( !_gui._synapsisQuery.Value.AllDevicesHomed && (DateTime.Now - start).TotalSeconds < allowable_timeout)
                Thread.Sleep( 100);
            // check for timeout
            double time_to_home = (DateTime.Now - start).TotalSeconds;
            if( time_to_home > (double)init_params.TimeoutSec) {
                string error = String.Format( "Initialize() timed out after {0:0.000}s", time_to_home);
                _log.Error( error);
                throw new Exception( error);
            } else {
                string msg = String.Format( "Initialize() completed after {0:0.000}s", time_to_home);
                _log.Debug( msg);
            }
            // Initialize will put the Hive into running mode automatically
            _gui.ExecuteResumeCommand();
            Connected = true;
            if( ClientConnected != null)
                ClientConnected( this, null);
        }

        public override void Close()
        {            
            Connected = false;
            if( ClientDisconnected != null)
                ClientDisconnected( this, null);
        }

        public override void UnloadPlate(string expected_barcode, string labware_name)
        {
            _log.DebugFormat( "UnloadPlate(\"{0}\", \"{1}\") called", expected_barcode, labware_name);
            // check that Hive is homed
            if( !_gui.DevicesAreHomed()) {
                string reason = "Hive is not homed";
                _log.ErrorFormat( "UnloadPlate(\"{0}\", \"{1}\") failed: {2}", expected_barcode, labware_name, reason);
                throw new Exception( reason);
            }
            // ensure that the specified labware exists
            // DKM 2012-05-14 allow LabwareNotFoundException to bubble up if the labware isn't available
            ILabware labware = _gui._labware_db.GetLabware( labware_name);
            // ensure that the barcode exists
            if( !HasBarcode( expected_barcode)) {
                string reason = String.Format( "The barcode '{0}' does not exist in the Hive device '{1}'", expected_barcode, (_gui._robot as DeviceInterface).Name);
                _log.ErrorFormat( "UnloadPlate(\"{0}\", \"{1}\") failed: {2}", expected_barcode, labware_name, reason);
                throw new Exception( reason);
            }
            // unload the plate
            var sm = new DownstackStateMachine( _gui.ErrorInterface, _gui, labware_name, false, expected_barcode, allow_unload_from_carts:true);
            sm.Start();
            // DKM 2012-05-17 when we're done, update the full/empty status
            _gui.UpdateFullEmptyStatus();
        }

        public override void LoadPlate(string expected_barcode, string labware_name)
        {
            _log.DebugFormat( "LoadPlate(\"{0}\", \"{1}\") called", expected_barcode, labware_name);
            // check that Hive is homed
            if( !_gui.DevicesAreHomed()) {
                string reason = "Hive is not homed";
                _log.ErrorFormat( "LoadPlate(\"{0}\", \"{1}\") failed: {2}", expected_barcode, labware_name, reason);
                throw new Exception( reason);
            }
            // ensure that the specified labware exists
            // DKM 2012-05-14 allow LabwareNotFoundException to bubble up if the labware isn't available
            ILabware labware = _gui._labware_db.GetLabware( labware_name);
            // load the plate
            var sm = new UpstackStateMachine( _gui.ErrorInterface, _gui, labware_name, expected_barcode, false);
            sm.Start();
            // DKM 2012-05-17 when we're done, update the full/empty status
            _gui.UpdateFullEmptyStatus();
        }

        public override bool HasBarcode(string barcode)
        {
            _log.DebugFormat( "HasBarcode(\"{0}\") called", barcode);
            if( _gui.PlateIsInAnyCart( barcode)) {
                _log.DebugFormat( "barcode '{0}' was found in a cart", barcode);
                return true;
            }
            if( _gui.PlateIsInStaticStorage( barcode)) {
                _log.DebugFormat( "barcode '{0}' was found in static storage", barcode);
                return true;
            }
            return false;
        }

        public override string GetInventory()
        {
            _log.Debug( "GetInventory() called");
            Dictionary<string,List<string>> inventory = _gui.GetInventory();
            return HiveXmlHelper.InventoryToXml( inventory);
        }

        public override void ScanInventory()
        {
            _log.Debug( "ScanInventory() called");
            if( !_gui.CanExecuteReinventoryStaticStorage()) {
                string reason = _gui.ReinventoryStaticStorageToolTip;
                _log.Error( "ScanInventory() failed: " + reason);
                throw new Exception( reason);
            }
        
            _gui.ExecuteReinventoryStaticStorage( blocking:true);
        }

        public override void MovePlate(string barcode, string labware_name, string destination_group)
        {
            _log.DebugFormat( "MovePlate(\"{0}\", \"{1}\", \"{2}\") called", barcode, labware_name, destination_group);
            // check that Hive is homed
            if( !_gui.DevicesAreHomed()) {
                string reason = "Hive is not homed";
                _log.ErrorFormat( "MovePlate(\"{0}\", \"{1}\", \"{2}\") failed: {2}", barcode, labware_name, destination_group, reason);
                throw new Exception( reason);
            }
            // first make sure that the barcode requested exists somewhere
            if( !HasBarcode( barcode)) {
                string reason = String.Format( "The barcode '{0}' does not exist in the Hive device '{1}'", barcode, (_gui._robot as DeviceInterface).Name);
                _log.ErrorFormat( "MovePlate(\"{0}\", \"{1}\", \"{2}\") failed: {2}", barcode, labware_name, destination_group, reason);
                throw new Exception( reason);
            }
            // move the plate with the specified barcode
            // DKM 2012-05-14 need to verify this with Giles -- make sure OnUpdatePlateFate called directly, as it was originally
            //                called based on the return value of a delegate argument in MovePlate().  No failure from OnUpdatePlateFate(),
            //                but I think it should return an exception if the sql query fails.
            _gui.OnUpdatePlateFate( barcode, destination_group);
        }

        public override int GetStatus()
        {
            _log.Debug( "GetStatus() called");
            return _gui.Status.ToInt();
        }

        public override void PresentStage()
        {
            _log.Debug( "PresentStage() called");
            // check that Hive is homed
            if( !_gui.DevicesAreHomed()) {
                string reason = "Hive is not homed";
                _log.ErrorFormat( "PresentStage() failed: {0}", reason);
                throw new Exception( reason);
            }
            if( !_gui._platemover.ExecuteCommand( PlateMover.PlateMoverPlugin.PlateMoverCommands.MoveToExternalTeachpoint, null)) {
                string error = "PresentStage() failed when commanding the plate mover to move to its external teachpoint";
                _log.Error( error);
                throw new Exception( error);
            }
        }
    }
}
