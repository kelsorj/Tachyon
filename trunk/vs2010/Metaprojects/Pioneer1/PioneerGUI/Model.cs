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
using System.Windows.Threading;

namespace BioNex.PioneerGUI
{
    [Export("PioneerModel")]
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

#warning Felix, how is the plate transfer service replaced in the new scheduler?
        /*
        [ImportMany]
        private IEnumerable<ExternalPlateTransferSchedulerInterface> PlateTransferServices { get; set; }
         */

        [Import("MainDispatcher")]
        private Dispatcher _dispatcher;

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

        // used to show hourglass window during reinventory
        private HourglassWindow _hg;

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
            if( Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "Pioneer GUI hitpick execution";

            // call into the right plugin
            IEnumerable<DeviceInterface> bumblebees = GetAllDevices().Where( (device) => device.ProductName.ToLower() == "bumblebee");
            string error_msg = "There must be one and only one Bumblebee device plugin defined in the device manager.";
            Debug.Assert( bumblebees.Count() == 1, error_msg);
            if( bumblebees.Count() != 1) {
                MessageBox.Show( error_msg);
                return false;
            }

            // set all of the parameters for the hitpick
            Dictionary<string,object> parameters = new Dictionary<string,object>();

#warning Felix, how is the plate transfer service replaced in the new scheduler?
            //parameters.Add( "PlateHandler", PlateTransferServices.FirstOrDefault( x => x.GetPlateTransferStrategyName() == "Simple" ));

            // tip handling, either tip change or tip wash
            parameters.Add( "TipHandlingMethodName", tip_handling_method);
            parameters.Add( "HitpickFilepath", hitpick_filepath);
            parameters.Add( "Callback", new AsyncCallback( ProcessComplete));
           
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
            TipboxesReinventoried = false;
            var devices = _device_manager.DevicePluginsAvailable;

            var hive = GetHivePlugin();
            if( hive == null) {
                var speedy_robots = from p in devices where p.Value.ProductName.ToLower() == "speedy robot" select p.Value;
                if( speedy_robots == null || speedy_robots.Count() == 0)
                    return;
                TipboxesReinventoried = true;
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

            ShowReinventoryHourglassWindow();
            hive.Reinventory( park_robot_after);
        }

        private void ShowReinventoryHourglassWindow()
        {
            _dispatcher.Invoke( new Action( () => {
                _hg = new HourglassWindow();
                _hg.Title = "Waiting for Completion or Timeout";
                _hg.ShowInTaskbar = false;
                _hg.Owner = Application.Current.MainWindow;
                _hg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                _hg.Show();
            } ));
        }

        PlateStorageInterface GetHivePlugin()
        {
            var devices = _device_manager.DevicePluginsAvailable;
            var hives = from p in devices where p.Value.ProductName.ToLower() == "hive" select p.Value;
            if( hives == null || hives.Count() == 0)
                return null;
            return hives.First() as PlateStorageInterface;
        }

        void hive_ReinventoryComplete(object sender, EventArgs e)
        {
            TipboxesReinventoried = true;
            Reinventorying = false;
            PlateStorageInterface hive = GetHivePlugin();
            hive.ReinventoryComplete -= hive_ReinventoryComplete;
            hive.ReinventoryComplete -= ModelAbortableProcessComplete;

            CloseReinventoryHourglassWindow();
        }

        private void CloseReinventoryHourglassWindow()
        {
            _dispatcher.Invoke(new Action(() => { _hg.Close(); }));
        }

        void hive_ReinventoryError(object sender, EventArgs e)
        {
            TipboxesReinventoried = false;
            Reinventorying = false;
            PlateStorageInterface hive = GetHivePlugin();
            hive.ReinventoryError -= hive_ReinventoryError;
            hive.ReinventoryError -= ModelAbortableProcessComplete;

            CloseReinventoryHourglassWindow();
        }

        public void ReinventoryPlates()
        {
            PlatesReinventoried = false;
            var devices = _device_manager.DevicePluginsAvailable;
            var bps140 = GetBPS140Plugin();
            if( bps140 == null) {
                var speedy_robots = from p in devices where p.Value.ProductName.ToLower() == "speedy robot" select p.Value;
                if( speedy_robots == null || speedy_robots.Count() == 0)
                    return;
                PlatesReinventoried = true;
                return;
            }
            bps140.ReinventoryComplete += new EventHandler(bps140_ReinventoryComplete);
            bps140.ReinventoryError += new EventHandler(bps140_ReinventoryError);
            Reinventorying = true;

            if( ModelAbortableProcessStarted != null)
                ModelAbortableProcessStarted( this, new EventArgs());
            bps140.ReinventoryComplete += ModelAbortableProcessComplete;
            bps140.ReinventoryError += ModelAbortableProcessComplete;

            ShowReinventoryHourglassWindow();
            bps140.Reinventory( true);
        }

        public PlateStorageInterface GetBPS140Plugin()
        {
            var devices = _device_manager.DevicePluginsAvailable;
            var bps140s = from p in devices where p.Value.ProductName.ToUpper() == "BPS140" select p.Value;
            if( bps140s == null || bps140s.Count() == 0)
                return null;
            return bps140s.First() as PlateStorageInterface;
        }

        public void ReinventoryAllStorage()
        {
            // start thread that call ReinventoryTipboxes, then ReinventoryPlates
            Action reinventory_thread = new Action( ReinventoryAllStorageThread);
            reinventory_thread.BeginInvoke( ReinventoryAllStorageComplete, null);
        }

        private void ReinventoryAllStorageThread()
        {
            ReinventoryTipboxes( park_robot_after:false);
            while( Reinventorying) {
                Thread.Sleep( 100);
            }

            // #484: prevent reinventorying of plates if reinventorying tipboxes failed
            if( !TipboxesReinventoried)
                return;

            ReinventoryPlates();
            while( Reinventorying) {
                Thread.Sleep( 100);
            }
        }

        private void ReinventoryAllStorageComplete( IAsyncResult iar)
        {
            try {
                AsyncResult ar = (AsyncResult)iar;
                Action callback = (Action)ar.AsyncDelegate;
                callback.EndInvoke( iar);
            } catch( Exception) {
            }
        }

        void bps140_ReinventoryError(object sender, EventArgs e)
        {
            PlatesReinventoried = false;
            Reinventorying = false;
            var bps140 = GetBPS140Plugin();
            bps140.ReinventoryError -= bps140_ReinventoryError;
            bps140.ReinventoryError -= ModelAbortableProcessComplete;

            CloseReinventoryHourglassWindow();
        }

        void bps140_ReinventoryComplete(object sender, EventArgs e)
        {
            PlatesReinventoried = true;
            Reinventorying = false;
            var bps140 = GetBPS140Plugin();
            bps140.ReinventoryComplete -= bps140_ReinventoryComplete;
            bps140.ReinventoryComplete -= ModelAbortableProcessComplete;

            CloseReinventoryHourglassWindow();
        }

        internal void DisplayTipboxes()
        {
            var devices = _device_manager.DevicePluginsAvailable;
            PlateStorageInterface hive = ( from p in devices where p.Value.ProductName.ToLower() == "hive" select p.Value).First() as PlateStorageInterface;
            hive.DisplayInventoryDialog();
        }

        internal bool CanExecuteDisplayTipboxes()
        {
            var devices = _device_manager.DevicePluginsAvailable;
            PlateStorageInterface hive = ( from p in devices where p.Value.ProductName.ToLower() == "hive" select p.Value).FirstOrDefault() as PlateStorageInterface;
            if( hive == null)
                DisplayTipboxesToolTip = "No Hive robot device present in the device manager";
            else
                DisplayTipboxesToolTip = "Show tipboxes in storage";
            return hive != null;
        }

        internal void DisplayPlates()
        {
            var devices = _device_manager.DevicePluginsAvailable;
            PlateStorageInterface bps140 = ( from p in devices where p.Value.ProductName.ToUpper() == "BPS140" select p.Value).First() as PlateStorageInterface;
            bps140.DisplayInventoryDialog();
        }

        internal bool CanExecuteDisplayPlates()
        {
            var devices = _device_manager.DevicePluginsAvailable;
            PlateStorageInterface bps140 = (from p in devices where p.Value.ProductName.ToUpper() == "BPS140" select p.Value).FirstOrDefault() as PlateStorageInterface;
            if( bps140 == null)
                DisplayPlatesToolTip = "No BPS140 storage device present in the device manager";
            else
                DisplayPlatesToolTip = "Show plates in storage";
            return bps140 != null;
        }
    }
}