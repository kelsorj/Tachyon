using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using BioNex.Shared.BioNexGuiControls;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using FileHelpers;
using log4net;

namespace BioNex.HivePrototypePlugin
{
    public class SimulatedReinventory : IReinventoryStrategy
    {
        private string _simulation_filepath { get; set; }

        private HivePlugin _hive { get; set; }
        private static readonly ILog _log = LogManager.GetLogger( typeof( SimulatedReinventory));

        public SimulatedReinventory( HivePlugin hive)
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
                if( ReinventoryStrategyComplete != null)
                    ReinventoryStrategyComplete( this, new EventArgs());
            } catch( Exception ex) {
                _log.Error( "Hive reinventory error: " + ex.Message);
                if( ReinventoryStrategyError != null)
                    ReinventoryStrategyError( this, new EventArgs());
            }
        }

        public void GetInventorySimulationFilename()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "CSV files (*.csv)|*.csv";
            if( dlg.ShowDialog() == true) {
                _simulation_filepath = dlg.FileName;
            }
        }

        public List<BarcodeReadErrorInfo> ReinventorySelectedRacksThread(IEnumerable<int> selected_rack_numbers, Action update_callback, bool called_from_diags)
        {
            try {
                // allow the user to select a file for reinventory simulation, but
                // need to marshall to main thread
                // commenting out for now because some XP systems crash when opening openfiledialog
                //_hive._dispatcher.Invoke( new Action(GetInventorySimulationFilename));

                if (ReinventoryStrategyBegin != null)
                    ReinventoryStrategyBegin(this, new ReinventoryEventArgs(called_from_diags));

                List<ScanningParameters> scanning_parameters = new List<ScanningParameters>();
                var selected_racks = _hive._plate_location_manager.Racks.Where( x => selected_rack_numbers.Contains( x.RackNumber));

                // load the simulation inventory file
                FileHelperEngine engine = new FileHelperEngine(typeof(InventorySimulationData));
                engine.Options.IgnoreFirstLines = 1;

                // hardcoding for now because some XP systems crash when opening openfiledialog
                _simulation_filepath = _hive.DeviceProperties[HivePlugin.ConfigFolder] + "\\inventory_simulation_data.csv";
                InventorySimulationData[] inventory = engine.ReadFile( _simulation_filepath) as InventorySimulationData[];

                foreach( RackView rack in selected_racks) {
                    var plates = (from plate in inventory where plate.RackNumber == rack.RackNumber select plate);
                    List<string> barcodes = new List<string>();
                    for( int i=1; i<=rack.SlotIndexes.Count(); i++) {
                        var plate_in_slot = plates.Where( x => x.SlotNumber == i).FirstOrDefault();
                        if( plate_in_slot == null)
                            continue;
                        if( plates.Where( x => x.SlotNumber == i).First().Barcode == "")
                            barcodes.Add( Constants.NoRead);
                        else
                            barcodes.Add( plates.Where( x => x.SlotNumber == i).First().Barcode);
                    }

                    if( _hive.AbortReinventoryEvent.WaitOne( 0))
                        return null;
                    // DKM 2011-04-27 passed 1 for starting shelf number, but later it should recognize gaps in racks
                    _hive.RackReinventoryCompleteHandler( new RackReinventoryCompleteEvent( rack.RackNumber, barcodes, 1));
                    if( update_callback != null && _hive._dispatcher != null)
                        _hive._dispatcher.Invoke( update_callback);
                }
            } catch( Exception ex) {
                _log.InfoFormat( "Could not simulate inventory for {0}: {1}", _hive.Name, ex.Message);
            }

            // there aren't any misreads in simulation, so just return empty collection
            return new List<Shared.DeviceInterfaces.BarcodeReadErrorInfo>();
        }

        #endregion
    }
}
