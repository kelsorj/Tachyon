using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using BioNex.Shared.DeviceInterfaces;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using System.Windows;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Utils;
using log4net;
using BioNex.SynapsisPrototype;
using BioNex.BumblebeePlugin.Scheduler;

namespace BioNex.LabAutoGUI
{
    [Export("LabAutoModel")]
    public class Model
    {
        [Import]
        public BioNex.Shared.LibraryInterfaces.ILabwareDatabase LabwareDatabase { get; set; }
        [Import]
        public BioNex.Shared.LibraryInterfaces.ILiquidProfileLibrary LiquidProfileLibrary { get; set; }
        [ImportMany]
        public IEnumerable<ILimsTextConverter> LimsTextConverters { get; private set; }
        [Import(AllowDefault=true)]
        public ILimsOutputTransferLog OutputPlugin { get; set; }
        [ImportMany]
        private IEnumerable<ExternalPlateTransferSchedulerInterface> PlateTransferServiceStrategies { get; set; }

        /// <summary>
        /// needed so that the plugin code (checklist.xaml.cs) can forward the event to the model for handling
        /// </summary>
        internal event EventHandler ProtocolComplete;
        
        private static readonly ILog _log = LogManager.GetLogger( typeof( Model));
        [Import]
        internal DeviceManager _device_manager { get; set; }

        public Model()
        {
        }

        // these things need to get factored out somehow -- they are CUSTOMER-SPECIFIC
        public bool Reinventorying { get; private set; }
        public bool PlatesReinventoried { get; private set; }
        public bool TipboxesReinventoried { get; private set; }

        /// <summary>
        /// This starts the ball rolling and executes the hitpick file
        /// </summary>
        /// <param name="hitpick_filepath"></param>
        /// <param name="converter"></param>
        /// <param name="tip_handling_method"></param>
        public bool ExecuteHitpick( string hitpick_filepath, string tip_handling_method)
        {
            // set all of the parameters for the hitpick
            Dictionary<string,object> parameters = new Dictionary<string,object>();
            // DKM 2011-08-09 changed the strategy name from "LabAuto" to "Simple" for Hamilton/Qiagen demo
            parameters.Add( "PlateHandler", (from x in PlateTransferServiceStrategies where x.GetPlateTransferStrategyName() == "Simple" select x).First());
            // tip handling, either tip change or tip wash
            parameters.Add( "TipHandlingMethodName", tip_handling_method);
            parameters.Add( "HitpickFilepath", hitpick_filepath);
            parameters.Add( "Callback", new AsyncCallback( ProcessComplete));
           
            // call into the right plugin
            IEnumerable<DeviceInterface> bumblebees = GetAllDevices().Where( (device) => device.ProductName == "Bumblebee");
            Debug.Assert( bumblebees.Count() == 1, "There must be one and only one Bumblebee device plugin defined for now.");
            // there's only one command in the bumblebee right now
            IEnumerable<string> commands = bumblebees.First().GetCommands();
            bool result = bumblebees.First().ExecuteCommand( "ExecuteHitpickFile", parameters);
            return result;
        }

        /// <summary>
        /// Returns all of the plugins that were successfully loaded and present in the device manager
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DeviceInterface> GetAllDevices()
        {
            return _device_manager.GetAllDevices();
        }

        /// <summary>
        /// This executes when the scheduler is done processing the hitpick list
        /// </summary>
        /// <param name="iar"></param>
        private void ProcessComplete( IAsyncResult iar)
        {
            AsyncResult ar = (AsyncResult)iar;
            DateTime start = (DateTime)iar.AsyncState;

            // CUSTOMER-SPECIFIC -- need to figure out how to deal with executing units of work
            TransferProcess process = (TransferProcess)ar.AsyncDelegate;
            try {
                process.EndInvoke( iar);
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }

            //! \todo this is specific to aborting, and really should go into the SynapsisModel instead
            /*
            // call ProtocolComplete in all of the plugins that implement ProtocolHooks
            // only do this if we're NOT ABORTING
            if( !Aborting) {
                foreach( ProtocolHooksInterface i in _device_manager.GetProtocolHooksInterfaces())
                    i.ProtocolComplete();
            }
             */

            if( OutputPlugin != null)
                OutputPlugin.Close();

            // block here until all of our separate tasks have completed
            // DKM 2011-08-09 changed from "LabAuto" to "Simple" for Hamilton/Qiagen demo
            ExternalPlateTransferSchedulerInterface labauto_transfer_scheduler = (from x in PlateTransferServiceStrategies where x.GetPlateTransferStrategyName() == "Simple" select x).First();
            labauto_transfer_scheduler.WaitForDestinationPostHitpickTasks();

            // signal that the protocol is complete
            if( ProtocolComplete != null)
                ProtocolComplete( this, new EventArgs());

            //! \todo not sure if this is the best way to clear the event handler
            ProtocolComplete = null;
            // all of the cleanup and notifications are now handled by the Synapsis app via the ProtocolComplete event handler
        }

        public void ReinventoryTipboxes()
        {
            TipboxesReinventoried = false;
            var devices = _device_manager.DevicePluginsAvailable;
            var hives = from p in devices where p.Value.ProductName == "Hive" select p.Value;
            // we could be running with a speedy robot
            if( hives == null || hives.Count() == 0) {
                var speedy_robots = from p in devices where p.Value.ProductName == "Speedy Robot" select p.Value;
                if( speedy_robots == null || speedy_robots.Count() == 0)
                    return;
                TipboxesReinventoried = true;
                return;
            }
            PlateStorageInterface hive = hives.First() as PlateStorageInterface;
            hive.ReinventoryComplete += new EventHandler(hive_ReinventoryComplete);
            hive.ReinventoryError += new EventHandler(hive_ReinventoryError);
            //! \todo if we have a variable on the stack to do reinventorying, how do we unregister the event handlers???
            Reinventorying = true;
            hive.Reinventory(true);
        }

        void hive_ReinventoryComplete(object sender, EventArgs e)
        {
            TipboxesReinventoried = true;
            Reinventorying = false;
        }

        void hive_ReinventoryError(object sender, EventArgs e)
        {
            TipboxesReinventoried = false;
            Reinventorying = false;
        }

        public void ReinventoryPlates()
        {
            PlatesReinventoried = false;
            var devices = _device_manager.DevicePluginsAvailable;
            var bps140s = from p in devices where p.Value.ProductName == "BPS140" select p.Value;
            if( bps140s == null || bps140s.Count() == 0) {
                var speedy_robots = from p in devices where p.Value.ProductName == "Speedy Robot" select p.Value;
                if( speedy_robots == null || speedy_robots.Count() == 0)
                    return;
                PlatesReinventoried = true;
                return;
            }
            PlateStorageInterface bps140 = bps140s.First() as PlateStorageInterface;
            bps140.ReinventoryComplete += new EventHandler(bps140_ReinventoryComplete);
            bps140.ReinventoryError += new EventHandler(bps140_ReinventoryError);
            Reinventorying = true;
            bps140.Reinventory(true);
        }

        void bps140_ReinventoryError(object sender, EventArgs e)
        {
            PlatesReinventoried = false;
            Reinventorying = false;
        }

        void bps140_ReinventoryComplete(object sender, EventArgs e)
        {
            PlatesReinventoried = true;
            Reinventorying = false;
        }

        internal void DisplayTipboxes()
        {
            var devices = _device_manager.DevicePluginsAvailable;
            PlateStorageInterface hive = ( from p in devices where p.Value.ProductName == "Hive" select p.Value).First() as PlateStorageInterface;
            hive.DisplayInventoryDialog();
        }

        internal void DisplayPlates()
        {
            var devices = _device_manager.DevicePluginsAvailable;
            PlateStorageInterface bps140 = ( from p in devices where p.Value.ProductName == "BPS140" select p.Value).First() as PlateStorageInterface;
            bps140.DisplayInventoryDialog();
        }
    }
}