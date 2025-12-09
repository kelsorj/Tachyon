using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using BioNex.Shared.BioNexGuiControls;
using BioNex.Shared.DeviceInterfaces;
using log4net;

namespace BioNex.BPS140Plugin
{
    public class BarcodeReinventory : IReinventoryStrategy
    {
        private BPS140 _bps { get; set; }
        private static readonly ILog _log = LogManager.GetLogger( typeof( BarcodeReinventory));

        public BarcodeReinventory( BPS140 bps)
        {
            _bps = bps;
        }

        #region IReinventoryStrategy Members

        public event EventHandler ReinventoryStrategyBegin;
        public event EventHandler ReinventoryStrategyComplete;
        public event EventHandler ReinventoryStrategyError;

        public void ReinventoryThreadComplete(IAsyncResult iar)
        {
            AsyncResult ar = (AsyncResult)iar;
            ReinventoryDelegate caller = (ReinventoryDelegate)ar.AsyncDelegate;
            try {
                List<BarcodeReadErrorInfo> misreads = caller.EndInvoke( iar);

                if( misreads != null && misreads.Count() > 0) {
                    _bps.HandleBarcodeMisreads( misreads);
                }

                if( ReinventoryStrategyComplete != null)
                    ReinventoryStrategyComplete( this, new EventArgs());
            } catch( Exception ex) {
                _log.Error( "BPS140 reinventory error: " + ex.Message);
                if( ReinventoryStrategyError != null)
                    ReinventoryStrategyError( this, new EventArgs());
            }
        }

        public List<BarcodeReadErrorInfo> ReinventorySelectedRacksThread(IEnumerable<int> selected_rack_numbers, Action update_callback, bool called_from_diags)
        {
            if( Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "BPS140 reinventory thread";

            if (ReinventoryStrategyBegin != null)
                ReinventoryStrategyBegin(this, new ReinventoryEventArgs( called_from_diags));

            // immediately fail if we aren't Locked, which means both side sensors should be off
            if( _bps.IsInUnsafeState())
                return new List<BarcodeReadErrorInfo>();

            // get all the available robot interfaces.
            IEnumerable< RobotInterface> robot_interfaces = _bps.DataRequestInterface.Value.GetRobotInterfaces();

            // get all the information a robot needs to know in order to support reinventory.
            int current_side = _bps.Controller.SideFacingRobot;
            List<string> teachpoints_needed = new List<string>();

            // DKM 2011-03-16 need to move Rack class into DeviceInterfaces?
            IEnumerable<RackView> racks_to_reinventory = ( _bps.Controller.SideFacingRobot == 1)
                ? _bps.PlateLocationManager.Side1Racks.Where( x => selected_rack_numbers.Contains( x.RackNumber))
                : _bps.PlateLocationManager.Side2Racks.Where( x => selected_rack_numbers.Contains( x.RackNumber));
                
            foreach( RackView rack in racks_to_reinventory) {
                if( rack.SlotIndexes.Count() == 0)
                    continue;
                BPS140PlateLocation top_location = new BPS140PlateLocation( current_side, rack.RackNumber, rack.SlotIndexes.Min() + 1);
                BPS140PlateLocation bottom_location = new BPS140PlateLocation( current_side, rack.RackNumber, rack.SlotIndexes.Max() + 1);
                teachpoints_needed.Add( top_location.ToString());
                teachpoints_needed.Add( bottom_location.ToString());
            }

            // declare a chosen robot and initialize it to null.
            RobotInterface chosen_robot = null;

            // loop through all the robots.
            // if we find a robot that hits all the teachpoints needed to support reinventory, then select that robot.
            foreach( RobotInterface robot in robot_interfaces){
                IDictionary< string, IList< string>> all_teachpoints = robot.GetTeachpointNames();
                if( !all_teachpoints.ContainsKey( _bps.Name)){
                    continue;
                }

                // DKM 2010-09-20 SequenceEqual expects the two collections to be sorted first!
                if( all_teachpoints[ _bps.Name].Intersect( teachpoints_needed).OrderBy( x => x, StringComparer.Ordinal).SequenceEqual( teachpoints_needed.OrderBy( x => x, StringComparer.Ordinal))) {
                    chosen_robot = robot;
                    break;
                }
            }

            // if we can't find a robot to support reinventory, then fail.
            if( chosen_robot == null){
                return new List<BarcodeReadErrorInfo>();
            }

            // make sure teachpoints needed is an even number.
            Debug.Assert( teachpoints_needed.Count % 2 == 0);

            List<BarcodeReadErrorInfo> missed_barcodes = new List<BarcodeReadErrorInfo>();
            // loop through teachpoints -- two at a time -- calling read barcode.
            foreach( RackView rack in racks_to_reinventory.ToList()) {
                if( rack.SlotIndexes.Count() == 0)
                    continue;
                BPS140PlateLocation top_location = new BPS140PlateLocation( current_side, rack.RackNumber, rack.SlotIndexes.Min() + 1);
                BPS140PlateLocation bottom_location = new BPS140PlateLocation( current_side, rack.RackNumber, rack.SlotIndexes.Max() + 1);

                // get the rack configuration information so we know how to treat barcode misreads
                // get default plate type for this rack and set all slot plate types to this type
                RackView.PlateTypeT default_plate_type = _bps.Controller.Config.GetDefaultPlateTypeForRack( _bps.Controller.SideFacingRobot, rack.RackNumber);
                List<RackView.PlateTypeT> plate_types = new List<RackView.PlateTypeT>();
                for( int i=0; i<rack.SlotIndexes.Count(); i++) {
                    plate_types.Add( default_plate_type);
                }
                // get overridden plate type for specific slots, and overwrite the slot plate types
                Dictionary<int,RackView.PlateTypeT> overridden_plate_types = _bps.Controller.Config.GetOverriddenPlateTypesInRack( _bps.Controller.SideFacingRobot, rack.RackNumber);
                foreach( var x in overridden_plate_types) {
                    plate_types[x.Key - 1] = x.Value;
                }

                List<byte> reread_condition_masks = new List<byte>();
                foreach( var plate_type in plate_types) {
                    if( plate_type == RackView.PlateTypeT.Tipbox) {
                        reread_condition_masks.Add( ScanningParameters.RereadMissedStrobe);
                    } else if( plate_type == RackView.PlateTypeT.Barcode) {
                        reread_condition_masks.Add( (byte)(ScanningParameters.RereadMissedStrobe | ScanningParameters.RereadNoRead));
                    }
                }

                List< string> barcodes = chosen_robot.ReadBarcodes( _bps, top_location.ToString(), bottom_location.ToString(),
                                                                    rack.RackNumber, rack.SlotIndexes.Count(), 250, 3000,
                                                                    reread_condition_masks, _bps.Controller.Config.BarcodeMisreadThreshold );

                // if barcodes is null, the reinventorying process was aborted
                if( barcodes == null)
                    return null;

                // save misread information for another inventory step
                _bps.SaveBarcodeMisreadInfo( _bps.Controller.SideFacingRobot, rack.RackNumber, barcodes, missed_barcodes, reread_condition_masks);
                
                _bps.EnterPlatesIntoInventory( barcodes, current_side, rack.RackNumber);
                if( update_callback != null && _bps.Dispatcher != null)
                    _bps.Dispatcher.Invoke( update_callback);
            }

            // after getting the basic misread information, we now need to move to each location and
            // take pictures of what's there
            foreach( BarcodeReadErrorInfo info in missed_barcodes) {
                // move to the location
                try {
                    chosen_robot.MoveToDeviceLocationForBCRStrobe( _bps, info.TeachpointName, false);
                    info.NewBarcode = chosen_robot.SaveBarcodeImage( info.ImagePath);
                } catch( Exception ex) {
                    // if we couldn't move there or capture an image, just continue on and leave
                    // it up to the user to look at the plate
                    _log.Debug(String.Format("Could not take image of barcode at location {0}: {1}", info.TeachpointName, ex.Message));
                    info.ImagePath = null; // flag as bad file
                }
            }

            // if we can't park, it's okay, just log it
            try {
                chosen_robot.Park();
            } catch( Exception ex) {
                _log.Info( String.Format( "Could not park robot {0}: {1}", (chosen_robot as DeviceInterface).Name, ex.Message));
            }

            return missed_barcodes;
        }

        #endregion
    }
}
