using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.DeviceInterfaces;
using FileHelpers;
using BioNex.Shared.BioNexGuiControls;
using log4net;
using BioNex.Shared.LibraryInterfaces;

namespace BioNex.BPS140Plugin
{
    public class SimulatedReinventory : IReinventoryStrategy
    {
        private BPS140 _bps { get; set; }
        private static readonly ILog _log = LogManager.GetLogger( typeof( SimulatedReinventory));

        public SimulatedReinventory( BPS140 bps)
        {
            _bps = bps;
        }

        #region IReinventoryStrategy Members

        public event EventHandler ReinventoryStrategyBegin;
        public event EventHandler ReinventoryStrategyComplete;
        public event EventHandler ReinventoryStrategyError { add {} remove {} }

        public void ReinventoryThreadComplete(IAsyncResult iar)
        {
            if( ReinventoryStrategyComplete != null)
                ReinventoryStrategyComplete( this, new EventArgs());
        }

        public List<BarcodeReadErrorInfo> ReinventorySelectedRacksThread(IEnumerable<int> selected_rack_numbers, Action update_callback, bool called_from_diags)
        {
            try {
                // immediately fail if we aren't Locked, which means both side sensors should be off
                if( _bps.IsInUnsafeState())
                    return new List<BarcodeReadErrorInfo>();

                if (ReinventoryStrategyBegin != null)
                    ReinventoryStrategyBegin(this, new ReinventoryEventArgs( called_from_diags));

                // get all the information a robot needs to know in order to support reinventory.
                int current_side = _bps.Controller.SideFacingRobot;
                List<string> teachpoints_needed = new List<string>();

                // DKM 2011-03-16 need to move Rack class into DeviceInterfaces?
                IEnumerable<RackView> racks_to_reinventory = null;
                if( _bps.Controller.SideFacingRobot == 1)
                    racks_to_reinventory = _bps.PlateLocationManager.Side1Racks.Where( x => selected_rack_numbers.Contains( x.RackNumber));
                else
                    racks_to_reinventory = _bps.PlateLocationManager.Side2Racks.Where( x => selected_rack_numbers.Contains( x.RackNumber));
                
                List<BarcodeReadErrorInfo> missed_barcodes = new List<BarcodeReadErrorInfo>();

                // load the simulation inventory file
                FileHelperEngine engine = new FileHelperEngine(typeof(InventorySimulationData));
                engine.Options.IgnoreFirstLines = 1;
                string simulation_filepath = _bps.Controller.DeviceProperties[BPS140Plugin.BPS140.ConfigFolder] + "\\inventory_simulation_data.csv";
                InventorySimulationData[] inventory = engine.ReadFile( simulation_filepath) as InventorySimulationData[];

                // loop through teachpoints -- two at a time -- calling read barcode.
                foreach( RackView rack in racks_to_reinventory.ToList()) {
                    var plates = (from plate in inventory where plate.SideNumber == _bps.Controller.SideFacingRobot && plate.RackNumber == rack.RackNumber select plate);
                    List<string> barcodes = new List<string>();
                    for( int i=1; i<=rack.SlotIndexes.Count(); i++) {
                        var plate_in_slot = plates.Where( x => x.SlotNumber == i).FirstOrDefault();
                        if( plate_in_slot == null)
                            continue;
                        if( plate_in_slot.Barcode == "")
                            barcodes.Add( Constants.NoRead);
                        else
                            barcodes.Add( plates.Where( x => x.RackNumber == rack.RackNumber && x.SlotNumber == i).First().Barcode);
                    }
                
                    _bps.EnterPlatesIntoInventory( barcodes, current_side, rack.RackNumber);
                    if( update_callback != null && _bps.Dispatcher != null)
                        _bps.Dispatcher.Invoke( update_callback);
                }
            } catch( Exception ex) {
                _log.Info( String.Format( "Could not simulate inventory on {0}: {1}", _bps.Name, ex.Message));
            }

            return new List<BarcodeReadErrorInfo>();
        }

        #endregion
    }
}
