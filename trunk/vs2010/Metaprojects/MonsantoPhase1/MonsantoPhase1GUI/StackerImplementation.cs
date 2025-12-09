using System;
using System.Linq;
using BioNex.IWorksCommandServer;
using log4net;
using System.Windows.Media;
using BioNex.Shared.LibraryInterfaces;
using System.Threading;
using BioNex.Shared.Utils;

namespace BioNex.CustomerGUIPlugins
{
    public enum ReturnCode
    {
        RETURN_SUCCESS = 0,
        RETURN_BAD_ARGS = 1,
        RETURN_FAIL = 2,
    }

    public enum PlateFlagsType
    {
        STACK_NORMAL_PLATES = 0,
        STACK_LIDDED_PLATES = 1,
        STACK_SEALED_PLATES = 2,
    }

    public class StackerImplementation : StackerRpcInterface
    {
        private MonsantoPhase1GUI _plugin { get; set; }
        private ILog _log = LogManager.GetLogger( typeof( StackerImplementation));

        public StackerImplementation( MonsantoPhase1GUI plugin)
        {
            _plugin = plugin;
        }

        #region PingBack 
        public override void Ping() { } // do nothing method to verify connection
        #endregion

        #region IWorksDeviceCommands

        public override bool IsLocationAvailable(string location_xml)
        {
            // are we already upstacking?  if so, bail.

            // GB - May-6-11 we don't need an idle check anymore, the upstack action will just be block until the hive enters idle state (provided that inventory checking is thread safe)
            //if( !_plugin._engine.IsIdle)
            //    return false;

            // check to see how much static storage is available
            //if(_plugin.GetAvailableStaticLocations().Count() == 0)
            //    return false;

            // do we need to reserve a spot?  Be careful here, don't reserve a new one every time because this method
            // gets called very often.  Maybe reserve the same location each time?

            return true;           
        }

        private bool _make_location_available_in_progress;
        public override int MakeLocationAvailable(string location_xml)
        {
            _make_location_available_in_progress = true;

            while (_plugin._engine.NotHomed || _plugin._engine.Paused)
                Thread.Sleep(100);
            _log.Info("VWorks is asking to make the PlateMover stage accessible to its robot");

            var sm = new PlateMoverStateMachine(_plugin.ErrorInterface, _plugin._platemover);
            var starter = new System.Threading.ParameterizedThreadStart(PlateMoverWorkerThread);
            var thread = new System.Threading.Thread(starter);
            thread.Start(sm);

            return (int)ReturnCode.RETURN_SUCCESS;
        }

        private void PlateMoverWorkerThread(object param)
        {
            try
            {
                // this doesn't need to block the behavior engine for now, since we want the plate mover motion to be independent of shuffling plates 
                // in and out of slots
                var sm = (PlateMoverStateMachine)param;
                sm.Start();
            }
            finally
            {
                _make_location_available_in_progress = false;
            }
        }

        public override bool IsMLAComplete()
        {
            // DKM 2011-06-16 added this to prevent VWorks from dropping off a plate when Synapsis first starts up and is not homed
            while (_plugin._engine.NotHomed || _plugin._engine.Paused)
                Thread.Sleep(100);

            return !_make_location_available_in_progress;
        }

        #endregion
        
        #region IWorksStackerCommands

        private bool _sink_plate_in_progress;
        // this call should return immediately, and the caller should poll for completion using IsSinkPlateComplete
        public override int SinkPlate(string labware, int PlateFlags, string SinkToLocation)
        {
            _sink_plate_in_progress = true;

            _log.Info("Synapsis has been requested to upstack the plate");
           
            // package the params in a state machine object
            // need to use "!STROBE!" expected barcode to make robot strobe, but not report errors
            var sm = new UpstackStateMachine(_plugin.ErrorInterface, _plugin, labware, Constants.Strobe , false);

            // launch a worker thread to do the Upstack op, pass the state machine
            var starter = new System.Threading.ParameterizedThreadStart(UpstackWorkerThread);
            var thread = new System.Threading.Thread(starter);
            thread.Start(sm);
          
            return (int)ReturnCode.RETURN_SUCCESS;
        }

        private void UpstackWorkerThread(object param)
        {
            if (!_plugin._engine.FireLoadPlate())
                return;
            var sm = (UpstackStateMachine)param;
            sm.Start();
            // make sure you call UpstackComplete FIRST because this changes inventory!
            _plugin.UpstackComplete(_plugin._robot.LastReadBarcode);
            // we have to do this stuff after UpstackComplete because this causes the behavior engine
            // to transition states, and it's possible to crash plates if another task takes up a slot
            // that was just used.
            _plugin._engine.FireDoneLoadingPlate();
            _sink_plate_in_progress = false;
        }

        public override bool IsSinkPlateComplete()
        {
            return !_sink_plate_in_progress;
        }


        private bool _source_plate_in_progress;
        public override int SourcePlate(string labware, int PlateFlags, string SinkToLocation)
        {
            _source_plate_in_progress = true;

            _log.Info("Synapsis has been requested to downstack a plate");

            // package the params in a state machine object
            var sm = new DownstackStateMachine(_plugin.ErrorInterface, _plugin, labware, false);

            // launch a worker thread to do the Upstack op, pass the state machine
            var starter = new System.Threading.ParameterizedThreadStart(DownstackWorkerThread);
            var thread = new System.Threading.Thread(starter);
            thread.Start(sm);

            return (int)ReturnCode.RETURN_SUCCESS;
        }

        private void DownstackWorkerThread(object param)
        {
            if( !_plugin._engine.FireUnloadPlate())
                return;
            var sm = (DownstackStateMachine)param;
            sm.Start();
            _plugin._engine.FireDoneUnloadingPlate();
            _source_plate_in_progress = false;
        }

        public override bool IsSourcePlateComplete()
        {
            return !_source_plate_in_progress;
        }

        
        public override int IsStackEmpty(string location)
        {
            int num_empty_locations = _plugin.GetAvailableStaticLocations().Count();
            int num_total_locations = _plugin.GetStaticStorageLocationNames().Count();
            bool stack_is_empty = num_empty_locations == num_total_locations;
            _plugin.StorageEmptyStatusColor = stack_is_empty ? Brushes.DarkRed : Brushes.LightGray;
            return stack_is_empty ? 1 : 0;
        }

        public override int IsStackFull( string location)
        {
            int num_empty_locations = _plugin.GetAvailableStaticLocations().Count();
            bool stack_is_full = num_empty_locations == 0;
            _plugin.StorageFullStatusColor = stack_is_full ? Brushes.DarkRed : Brushes.LightGray;
            return stack_is_full ? 1 : 0;
        }

        public override void Abort()
        {
            _plugin._engine.AbortUpstackOrDownstack();
        }

        public override void PrepareForRun()
        {
            _plugin._engine.ResetAbortUpstackOrDownstack();
        }

        #endregion
    }
}
