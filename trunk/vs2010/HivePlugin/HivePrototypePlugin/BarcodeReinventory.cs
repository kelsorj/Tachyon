using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using BioNex.Shared.BioNexGuiControls;
using BioNex.Shared.DeviceInterfaces;
using log4net;

namespace BioNex.HivePrototypePlugin
{
    public class BarcodeReinventory : IReinventoryStrategy
    {
        private static readonly ILog _log = LogManager.GetLogger( typeof( BarcodeReinventory));
        private HivePlugin _hive { get; set; }

        public BarcodeReinventory( HivePlugin hive)
        {
            _hive = hive;
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
                Debug.WriteLine( "ReinventoryThreadComplete handled in thread " + Thread.CurrentThread.GetHashCode().ToString());
                List<BarcodeReadErrorInfo> misread_barcode_info = caller.EndInvoke( iar);

                // if we are aborting, bail
                if( !_hive.AbortReinventoryEvent.WaitOne( 0)) {
                    // #253 after reinventorying, we want the Hive to go back to its park position
                    try {
                        bool park_robot_after = (bool)iar.AsyncState;
                        if( park_robot_after)
                            _hive.Park();
                    } catch( Exception ex) {
                        // if we can't park, it's not the end of the world
                        _log.InfoFormat( "Could not park robot {0}: {1}", _hive.Name, ex.Message);
                    }
                    // take the misread information and present the user with a GUI to resolve the issues
                    if( misread_barcode_info.Count() > 0)
                        _hive.HandleBarcodeMisreads( misread_barcode_info, _hive.UnbarcodedPlates, _hive.UpdateInventoryView, _hive.UpdateInventoryLocation);
                }

                if( ReinventoryStrategyComplete != null)
                    ReinventoryStrategyComplete( this, new EventArgs());
            } catch( Exception ex) {
                _log.Error( "Hive reinventory error: " + ex.Message);
                if( ReinventoryStrategyError != null)
                    ReinventoryStrategyError( this, new EventArgs());
            }
        }

        public List<Shared.DeviceInterfaces.BarcodeReadErrorInfo> ReinventorySelectedRacksThread(IEnumerable<int> selected_rack_numbers, Action update_callback, bool called_from_diags)
        {
            if( Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "Hive reinventory thread";

            if (ReinventoryStrategyBegin != null)
                ReinventoryStrategyBegin(this, new ReinventoryEventArgs( called_from_diags));

            // here, we can assume that all of the requisite slots have been taught
            // first, get the number of slots in each rack and their pitch, and 
            // cache the information for the next step
            List<ScanningParameters> scanning_parameters = new List<ScanningParameters>();
            // this is used to collect all of the bad barcode information as we're scanning
            List<BarcodeReadErrorInfo> missed_barcodes = new List<BarcodeReadErrorInfo>();

            // DKM 2010-09-17 shouldn't have used the StaticInventoryView to reinventory because if it's called
            //                from a non-GUI thread, it can't use the UserControl.  So now I use the PlateLocationManager
            //                data instead
            // DKM 2010-10-08 now supporting selectable racks for reinventory, so use the new parameter selected_rack_numbers
            var selected_racks = _hive._plate_location_manager.Racks.Where( x => selected_rack_numbers.Contains( x.RackNumber));
            foreach( RackView rack in selected_racks) {
                // new, non-contiguous behavior
                List<List<int>> rack_blocks = ParseContiguousBlocks( rack.SlotIndexes);
                foreach( var x in rack_blocks) {
                    QueueContiguousBlock( rack.RackNumber, x, scanning_parameters);
                }
            }

            _hive.AbortReinventoryEvent.Reset();

            // loop over ScanningParameters, calling the function in the Hive that gives the servo drive
            // 1. the starting point, which should be above the top shelf, in mm (just call MoveAbsolute)
            // 2. TRIG_POS_IU, which is the first (top) shelf position
            // 3. CSPD, CACC
            // 4. Shelf_Delta_IU which is the shelf pitch in encoder counts
            // 5. num_shelves, which is the total number of shelves to scan
            foreach( ScanningParameters sp in scanning_parameters) {
                // ********** GET PLATE TYPE FOR HANDLING MISREADS **********
                // get the rack configuration information so we know how to treat barcode misreads
                // get default plate type for this rack and set all slot plate types to this type
                RackView.PlateTypeT default_plate_type = _hive.Config.GetDefaultPlateTypeForRack( sp.RackNumber);
                List<RackView.PlateTypeT> plate_types = new List<RackView.PlateTypeT>();
                for( int i=0; i<sp.NumberOfShelves; i++) {
                    plate_types.Add( default_plate_type);
                }
                // it's also possible to override an individual slot's expected plate type, so
                // get overridden plate type for these slots, and change their slot plate types
                Dictionary<int,RackView.PlateTypeT> overridden_plate_types = _hive.Config.GetOverriddenPlateTypesInRack( sp.RackNumber);
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
                // ********** END GET PLATE TYPE FOR HANDLING MISREADS **********
                 
                List< string> barcodes = _hive.ReadBarcodes( _hive, sp.TopShelfTeachpointName, sp.BottomShelfTeachpointName, sp.RackNumber,
                                                             sp.NumberOfShelves, (int)sp.ScanVelocityMmPerSec, (int)sp.ScanAccelMmPerSec2,
                                                             reread_condition_masks, _hive.Config.BarcodeMisreadThreshold
                                                             );
                if( _hive.AbortReinventoryEvent.WaitOne( 0))
                    return null;
                // DKM 2011-04-27 need the starting shelf number to support gaps in racks
                HivePlateLocation temp = HivePlateLocation.FromString( sp.TopShelfTeachpointName);
                _hive.RackReinventoryCompleteHandler( new RackReinventoryCompleteEvent( sp.RackNumber, barcodes, temp.SlotNumber));
                if( update_callback != null && _hive._dispatcher != null)
                    _hive._dispatcher.Invoke( update_callback);
                _hive.SaveBarcodeMisreadInfo( sp, barcodes, missed_barcodes, reread_condition_masks);
            }
            
            // after getting the basic misread information, we now need to move to each location and
            // take pictures of what's there
            foreach( BarcodeReadErrorInfo info in missed_barcodes) {
                // move to the location
                try {
                    _hive.MoveToDeviceLocationForBCRStrobe( _hive, info.TeachpointName, false);
                    info.NewBarcode = _hive.SaveBarcodeImage( info.ImagePath);
                } catch( Exception ex) {
                    // if we couldn't move there or capture an image, just continue on and leave
                    // it up to the user to look at the plate
                    _log.DebugFormat( "Could not take image of barcode at location {0}: {1}", info.TeachpointName, ex.Message);
                }
            }

            return missed_barcodes;
        }

        private static void QueueContiguousBlock( int rack_number, List<int> slot_indexes, ICollection<ScanningParameters> scanning_parameters)
        {
            // DKM 2010-10-11 changed this because BPS140 scanning was getting hung up, but you could
            //                get the reinventorying to work if you lightly pressed up on Z
            const double distance_above_top_shelf_mm = 46;
            int top_shelf_number = slot_indexes.Min() + 1;
            int bottom_shelf_number = slot_indexes.Max() + 1;
            // DKM 2011-04-28 I think I made a mistake here.  I should have used something other than ScanningParameters here,
            //                because it's very misleading.  Since SP takes the reread condition masks and barcode error threshold,
            //                and that information is not available here, I should use a different class.  ReadBarcodes() is
            //                responsible for constructing ScanningParameters, which is used by the reinventory thread to
            //                determine its behavior.
            scanning_parameters.Add( new ScanningParameters( rack_number, distance_above_top_shelf_mm, top_shelf_number,
                                                             (new HivePlateLocation( rack_number, top_shelf_number)).ToString(),
                                                             (new HivePlateLocation( rack_number, bottom_shelf_number).ToString()),
                                                             (short)slot_indexes.Count(), 250, 3000, null, 0));
        }

        private static List<List<int>> ParseContiguousBlocks(List<int> list)
        {
            // e.g. if we pass in 0, 1, 2, 5, 6, 7, we should get back two Lists:
            // a. 0, 1, 2
            // b. 5, 6, 7
            List<List<int>> blocks = new List<List<int>>();
            try {
                int last_i = 0;
                List<int> new_block = new List<int>();
                foreach( int i in list) {
                    // if it's the first in the list, always add to new_block
                    if( i == list.First()) {
                        new_block.Add( i);
                    } else {
                        // if this index is not sequential, then create add the previous new_block to blocks,
                        // and then create a new one
                        if( (i - last_i) > 1) {
                            blocks.Add( new_block);
                            new_block = new List<int>();
                        }
                        
                        // otherwise, just add i to new_block
                        new_block.Add( i);
                        last_i = i;
                    }
                }
                // make sure we add the last block!
                blocks.Add( new_block);
            } catch( Exception) {
                
            }
            return blocks;
        }

        #endregion
    }
}
