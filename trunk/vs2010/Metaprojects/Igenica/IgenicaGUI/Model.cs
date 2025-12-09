using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Windows;
using BioNex.BumblebeePlugin.Scheduler;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Utils;
using BioNex.SynapsisPrototype;
using log4net;
using System.Threading;

namespace BioNex.IgenicaGUI
{
    [Export("IgenicaModel")]
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

#warning Felix, please convert to new scheduler if necessary
        /*
        [ImportMany]
        private IEnumerable<ExternalPlateTransferSchedulerInterface> PlateTransferServices { get; set; }
         */

        // used as a pass-through event for making homing and reinventorying possible
        internal event EventHandler ModelAbortableProcessStarted;
        internal event EventHandler ModelAbortableProcessComplete;

        /// <summary>
        /// needed so that the plugin code (checklist.xaml.cs) can forward the event to the model for handling
        /// </summary>
        internal event EventHandler ProtocolComplete;
        
        private static readonly ILog _log = LogManager.GetLogger( typeof( Model));
        [Import]
        internal DeviceManager _device_manager { get; set; }

        public string DisplayTipboxesToolTip { get; set; }
        public string DisplayPlatesToolTip { get; set; }

        public Model()
        {
            // just to see what the xml looks like when we serialize
            /*
            IgenicaGuiConfig config = new IgenicaGuiConfig();
            config.destination_barcode_ranges.Add( new IgenicaGuiConfig.range { start_barcode = 1000, end_barcode = 2000 } );
            config.destination_barcode_ranges.Add( new IgenicaGuiConfig.range { start_barcode = 3000, end_barcode = 4000 } );
            config.source_barcode_ranges.Add( new IgenicaGuiConfig.range { start_barcode = 10000, end_barcode = 20000 } );
            config.source_barcode_ranges.Add( new IgenicaGuiConfig.range { start_barcode = 30000, end_barcode = 40000 } );
            FileSystem.SaveXmlConfiguration<IgenicaGuiConfig>( config, "igenica_config.xml");
             */
        }

        // these things need to get factored out somehow -- they are CUSTOMER-SPECIFIC
        public bool Reinventorying { get; private set; }
        public bool PlatesReinventoried { get; private set; }

        /// <summary>
        /// This starts the ball rolling and executes the hitpick file
        /// </summary>
        /// <param name="hitpick_filepath"></param>
        /// <param name="converter"></param>
        /// <param name="tip_handling_method"></param>
        public bool ExecuteHitpick( string hitpick_filepath, string tip_handling_method)
        {
            if( Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "Igenica GUI hitpick execution";

            // set all of the parameters for the hitpick
            Dictionary<string,object> parameters = new Dictionary<string,object>();

#warning Felix, please convert to new scheduler if necessary
            //parameters.Add( "PlateHandler", PlateTransferServices.FirstOrDefault( x => x.GetPlateTransferStrategyName() == "Simple" ));

            // tip handling, either tip change or tip wash
            parameters.Add( "TipHandlingMethodName", tip_handling_method);
            parameters.Add( "HitpickFilepath", hitpick_filepath);
            parameters.Add( "Callback", new AsyncCallback( ProcessComplete));
           
            // call into the right plugin
            IEnumerable<DeviceInterface> bumblebees = GetAllDevices().Where( (device) => device.ProductName == "Bumblebee");
            Debug.Assert( bumblebees.Count() == 1, "There must be one and only one Bumblebee device plugin defined for now.");
            // there's only one command in the bumblebee right now
            IEnumerable<string> commands = bumblebees.First().GetCommands();
            return bumblebees.First().ExecuteCommand( "ExecuteHitpickFile", parameters);
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

            // signal that the protocol is complete
            if( ProtocolComplete != null)
                ProtocolComplete( this, new EventArgs());

            //! \todo not sure if this is the best way to clear the event handler
            ProtocolComplete = null;
            // all of the cleanup and notifications are now handled by the Synapsis app via the ProtocolComplete event handler
        }

        public void ReinventoryTipboxes( bool park_robot_after)
        {
            PlatesReinventoried = false;
            var devices = _device_manager.DevicePluginsAvailable;

            var hive = GetHivePlugin();
            if( hive == null) {
                var speedy_robots = from p in devices where p.Value.ProductName == "Speedy Robot" select p.Value;
                if( speedy_robots == null || speedy_robots.Count() == 0)
                    return;
                PlatesReinventoried = true;
                return;
            }

            hive.ReinventoryComplete += new EventHandler(hive_ReinventoryComplete);
            hive.ReinventoryError += new EventHandler(hive_ReinventoryError);
            //! \todo if we have a variable on the stack to do reinventorying, how do we unregister the event handlers???
            Reinventorying = true;

            // make reinventory abortable
            if( ModelAbortableProcessStarted != null)
                ModelAbortableProcessStarted( this, new EventArgs());
            hive.ReinventoryComplete += ModelAbortableProcessComplete;
            hive.ReinventoryError += ModelAbortableProcessComplete;

            hive.Reinventory( park_robot_after);
        }

        PlateStorageInterface GetHivePlugin()
        {
            var devices = _device_manager.DevicePluginsAvailable;
            var hives = from p in devices where p.Value.ProductName == "Hive" select p.Value;
            if( hives == null || hives.Count() == 0)
                return null;
            return hives.First() as PlateStorageInterface;
        }

        void hive_ReinventoryComplete(object sender, EventArgs e)
        {
            PlatesReinventoried = true;
            Reinventorying = false;
            PlateStorageInterface hive = GetHivePlugin();
            hive.ReinventoryComplete -= hive_ReinventoryComplete;
            hive.ReinventoryComplete -= ModelAbortableProcessComplete;
        }

        void hive_ReinventoryError(object sender, EventArgs e)
        {
            PlatesReinventoried = false;
            Reinventorying = false;
            PlateStorageInterface hive = GetHivePlugin();
            hive.ReinventoryError -= hive_ReinventoryError;
            hive.ReinventoryError -= ModelAbortableProcessComplete;
        }

        internal void DisplayTipboxes()
        {
            var devices = _device_manager.DevicePluginsAvailable;
            PlateStorageInterface hive = ( from p in devices where p.Value.ProductName == "Hive" select p.Value).First() as PlateStorageInterface;
            hive.DisplayInventoryDialog();
        }

        internal bool CanExecuteDisplayTipboxes()
        {
            var devices = _device_manager.DevicePluginsAvailable;
            PlateStorageInterface hive = ( from p in devices where p.Value.ProductName == "Hive" select p.Value).FirstOrDefault() as PlateStorageInterface;
            if( hive == null)
                DisplayTipboxesToolTip = "No Hive robot device present in the device manager";
            else
                DisplayTipboxesToolTip = "Show tipboxes in storage";
            return hive != null;
        }
    }
}