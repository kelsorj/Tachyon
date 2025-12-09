using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using BioNex.Plugins.Dock;
using BioNex.Shared.BioNexGuiControls;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using GalaSoft.MvvmLight.Command;
using log4net;

namespace BioNex.Plugins
{
    /// <summary>
    /// Interaction logic for DockGUI.xaml
    /// </summary>
    public partial class DockGUI : UserControl, INotifyPropertyChanged
    {
        private DispatcherTimer _update_timer { get; set; }
        private DockMonitorPlugin _dock_plugin { get; set; }
        private Dispatcher _dispatcher { get; set; }
        private static readonly ILog _log = LogManager.GetLogger( typeof( DockGUI));

        private bool _app_closing;

        private string _dock_name;
        public string DockName 
        {
            get { return _dock_name; }
            private set { _dock_name = value; }
        }

        private Brush _cart_docked_color;
        public Brush CartDockedColor
        {
            get { return _cart_docked_color; }
            set {
                _cart_docked_color = value;
                OnPropertyChanged( "CartDockedColor");
            }
        }

        private Brush _cart_present_color;
        public Brush CartPresentColor
        {
            get { return _cart_present_color; }
            set {
                _cart_present_color = value;
                OnPropertyChanged( "CartPresentColor");
            }
        }

        private Brush _cart_reinventorying_color;
        public Brush CartReinventoryingColor
        {
            get { return _cart_reinventorying_color; }
            set {
                _cart_reinventorying_color = value;
                OnPropertyChanged( "CartReinventoryingColor");
            }
        }

        private string _cart_barcode;

        private string _cart_human_readable;
        public string CartHumanReadable
        {
            get { return _cart_human_readable; }
            set { 
                _cart_human_readable = value;
                OnPropertyChanged( "CartHumanReadable");
            }
        }

        private Visibility _undock_overlay_visible;
        public Visibility UndockOverlayVisible
        {
            get { return _undock_overlay_visible; }
            set {
                _undock_overlay_visible = value;
                OnPropertyChanged( "UndockOverlayVisible");
            }
        }

        // NOT WORKING
        private string _cart_docked_tooltip;
        public string CartDockedToolTip
        {
            get { return _cart_docked_tooltip; }
            set {
                _cart_docked_tooltip = value;
                OnPropertyChanged( "CartDockedToolTip");
            }
        }

        private string _request_to_dock_tooltip;
        public string RequestToDockToolTip
        {
            get { return _request_to_dock_tooltip; }
            set {
                _request_to_dock_tooltip = value;
                OnPropertyChanged( "RequestToDockToolTip");
            }
        }

        private string _reinventory_tooltip;
        public string ReinventoryToolTip
        {
            get { return _reinventory_tooltip; }
            set {
                _reinventory_tooltip = value;
                OnPropertyChanged( "ReinventoryToolTip");
            }
        }

        private string _request_to_undock_tooltip;
        public string RequestToUndockToolTip
        {
            get { return _request_to_undock_tooltip; }
            set {
                _request_to_undock_tooltip = value;
                OnPropertyChanged( "RequestToUndockToolTip");
            }
        }

        private bool _cart_reinventoried;

        public RelayCommand RequestToDockCommand { get; set; }
        public RelayCommand ReinventoryCommand { get; set; }
        public RelayCommand RequestToUndockCommand { get; set; }

        private bool _dock_pending = false;
        private bool _undock_pending = false;
        private bool _reinventory_pending = false;

        public DockGUI( DockMonitorPlugin plugin)
        {
            InitializeComponent();
            InitializeCommands();

            CartDockedColor = Brushes.LightGray;
            CartPresentColor = Brushes.LightGray;
            CartReinventoryingColor = Brushes.LightGray;
            _dock_plugin = plugin;
            // only use this to set the CartBarcode for the GUI
            _dock_plugin.Undocked += new DockEventHandler(_dock_plugin_Undocked);
            // need to handle UndockingError, or if we get an error the cart information could remain on the screen
            _dock_plugin.UndockingError += new DockEventHandler(_dock_plugin_Undocked);
            _dock_plugin.Docked += new DockEventHandler(_dock_plugin_Docked);
            _dock_plugin.ReinventoryComplete += new EventHandler(_dock_plugin_ReinventoryComplete);
            // this is used to update the inventory GUI when plates get loaded and unloaded
            _dock_plugin.PlateLoaded += new DockMonitorPlugin.PlateLoadUnloadEventHandler(_dock_plugin_PlateLoaded);
            _dock_plugin.PlateUnloaded += new DockMonitorPlugin.PlateLoadUnloadEventHandler(_dock_plugin_PlateUnloaded);

            Application.Current.MainWindow.Closing += new CancelEventHandler(MainWindow_Closing);

            DataContext = this;
            _dispatcher = _dock_plugin._dispatcher;

            InitializeInventoryControl();

            UndockOverlayVisible = Visibility.Hidden;
            
            _update_timer = new DispatcherTimer();
            _update_timer.Tick += new EventHandler(_update_timer_Tick);
            _update_timer.Interval = new TimeSpan( 0, 0, 0, 0, 100);
            _update_timer.Start();
        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            _app_closing = true;
        }

        void _dock_plugin_PlateUnloaded(object sender, DockMonitorPlugin.PlateLoadUnloadEventArgs e)
        {
            if (_app_closing)
                return;
            _dispatcher.Invoke( new Action<int,int,string,SlotView.SlotStatus>( inventory_control.SetSlotLoadedOrUnloaded), e.RackIndex, e.SlotIndex, e.Barcode, SlotView.SlotStatus.Empty);
        }

        void _dock_plugin_PlateLoaded(object sender, DockMonitorPlugin.PlateLoadUnloadEventArgs e)
        {
            if (_app_closing)
                return;

            if( Constants.IsNoRead( e.Barcode))
                _dispatcher.Invoke( new Action<int,int,string,SlotView.SlotStatus>( inventory_control.SetSlotLoadedOrUnloaded), e.RackIndex, e.SlotIndex, e.Barcode, SlotView.SlotStatus.Unknown);
            else if( Constants.IsEmpty( e.Barcode))
                _dispatcher.Invoke( new Action<int,int,string,SlotView.SlotStatus>( inventory_control.SetSlotLoadedOrUnloaded), e.RackIndex, e.SlotIndex, e.Barcode, SlotView.SlotStatus.Empty);
            else
                _dispatcher.Invoke( new Action<int,int,string,SlotView.SlotStatus>( inventory_control.SetSlotLoadedOrUnloaded), e.RackIndex, e.SlotIndex, e.Barcode, SlotView.SlotStatus.Loaded);
        }

        private void InitializeInventoryControl()
        {
            DockName = _dock_plugin.Name;
            inventory_control.RackColumnName = "rack";
            inventory_control.SlotColumnName = "slot";
            inventory_control.LoadedColumnName = "loaded";
            // DKM 2011-06-03 don't need this because we don't have a cart docked upon startup anyway
            //SetInventoryControlRackConfiguration();
        }

        private void SetInventoryControlRackConfiguration()
        {
            int num_racks;
            int num_slots;
            _dock_plugin.GetCurrentCartConfiguration( out num_racks, out num_slots);
            // setup new InventoryControl
            List<InventoryControl.RackDefinition> rack_definitions = new List<InventoryControl.RackDefinition>();
            for( int i=0; i<num_racks; i++) {
                rack_definitions.Add( new InventoryControl.RackDefinition( num_slots));
            }

            // need to use Dispatcher because we have to modify the GUI in response to an event that gets set asynchronously
            if (_app_closing)
                return;
            _dispatcher.Invoke(new AsynchronousRackUpdateDelegate(inventory_control.SetRackConfigurations), rack_definitions);
        }

        private delegate void AsynchronousRackUpdateDelegate(List<InventoryControl.RackDefinition> rack_definitions);

        void _dock_plugin_ReinventoryComplete(object sender, EventArgs e)
        {
            _cart_reinventoried = true;
            UpdateInventoryControl();
        }

        void _dock_plugin_Undocked(object sender, DockEventArgs e)
        {
            _cart_reinventoried = false;
            _cart_barcode = "";
            CartHumanReadable = "";
            SetInventoryControlRackConfiguration();
        }

        void _dock_plugin_Docked(object sender, DockEventArgs e)
        {
            _cart_reinventoried = false;
            _cart_barcode = e.CartBarcode;
            CartHumanReadable = e.CartHumanReadable;
            SetInventoryControlRackConfiguration();
        }

        ~DockGUI()
        {
            _update_timer.Stop();
        }

        private void InitializeCommands()
        {
            RequestToDockCommand = new RelayCommand( ExecuteRequestToDock, CanExecuteRequestToDock);
            ReinventoryCommand = new RelayCommand( ExecuteReinventory, CanExecuteReinventory);
            RequestToUndockCommand = new RelayCommand( ExecuteRequestToUndock, CanExecuteRequestToUndock);
        }

        //****************************** DOCKING HAPPENS HERE ***********************************
        private bool CanExecuteRequestToDock()
        {
            if( _dock_pending)
            {
                RequestToDockToolTip = "Dock operation in progress - please dock the cart";
                return false;
            }
            else if (_undock_pending)
            {
                RequestToDockToolTip = "Undock operation in progress - undock must complete before requesting a dock";
                return false;
            }

            string reason_not_allowed;
            bool result = _dock_plugin._external_communications.DockAllowed( _dock_plugin.Name, out reason_not_allowed);

            RequestToDockToolTip = result ? "Turn on magnets and allow cart to be docked" : reason_not_allowed;
            return result;
        }

        private void ExecuteRequestToDock()
        {
            _dock_pending = true;
            Action dock_thread = new Action( RequestDockThread);
            dock_thread.BeginInvoke( RequestDockComplete, null);
        }

        private void RequestDockThread()
        {
            _dock_plugin.Dock();
        }

        private void RequestDockComplete( IAsyncResult iar)
        {
            try {
                AsyncResult ar = (AsyncResult)iar;
                Action caller = (Action)ar.AsyncDelegate;
                caller.EndInvoke( iar);
                _cart_barcode = _dock_plugin.CartBarcode;
                CartHumanReadable = _dock_plugin.CartHumanReadable;
            } catch( Exception ex) {
                string message= String.Format( "Could not dock cart at '{0}': {1}", _dock_plugin.Name, ex.Message);
                MessageBox.Show( message);
                _log.Info( message);
                _cart_barcode = "";
                CartHumanReadable = "";
            }
            _dock_pending = false;
        }
        //****************************************************************************************

        private void ExecuteReinventory()
        {
            _reinventory_pending = true;
            Action reinventory_thread = new Action( ReinventoryThread);
            reinventory_thread.BeginInvoke( ReinventoryThreadComplete, null);
        }

        private void ReinventoryThread()
        {
            // DKM 2011-05-31 this isn't working because a different thread is the owner???
            /*
            if( Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = String.Format( "{0} reinventory thread", Name);
             */

            _dispatcher.Invoke( new Action( inventory_control.Clear));
            if( !_dock_plugin.Reinventory(true)) {
                throw new Exception( "Failed to reinventory");
            }
        }

        private void ReinventoryThreadComplete( IAsyncResult iar)
        {
            try {
                AsyncResult ar = (AsyncResult)iar;
                Action caller = (Action)ar.AsyncDelegate;
                caller.EndInvoke( iar);
            } catch( Exception ex) {
                string message= String.Format( "Could not reinventory cart at '{0}': {1}", _dock_plugin.Name, ex.Message);
                MessageBox.Show( message);
                _log.Info( message);
            }
            _reinventory_pending = false;
        }

        /// <summary>
        /// Updates the GUI that shows all of the RackViews
        /// </summary>
        /// <remarks>
        /// Because I have various ways of representing storage, i.e. map of barcode to location name, as
        /// well as map of barcode to map of database column to values like "rack" and "slot", I need to
        /// deconvolve (if that's a real word) the data
        /// </remarks>
        private void UpdateInventoryControl()
        {
            IEnumerable<KeyValuePair<string,string>> inventory = _dock_plugin.GetInventory( "not currently used");
            Dictionary<string,Dictionary<string,string>> deconvoluted_inventory = new Dictionary<string,Dictionary<string,string>>();
            foreach( var x in inventory) {
                string barcode = x.Key;
                string location_name = x.Value;
                DockMonitorPlateLocation location = DockMonitorPlateLocation.FromString( location_name);
                Dictionary<string,string> db_location = new Dictionary<string,string> { 
                    { inventory_control.RackColumnName, location.RackNumber.ToString() },
                    { inventory_control.SlotColumnName, location.SlotNumber.ToString() }
                };
                deconvoluted_inventory.Add( barcode, db_location);
            }

            _dispatcher.Invoke(new AsynchronousInventoryUpdateDelegate(inventory_control.UpdateFromInventory), deconvoluted_inventory);
        }

        private delegate void AsynchronousInventoryUpdateDelegate( Dictionary<string,Dictionary<string,string>> inventory);

        private bool CanExecuteReinventory()
        {
            if(_reinventory_pending)
            {
                ReinventoryToolTip = "Reinventory operation in progress - please wait for it to complete";
                return false;
            }
            else if (_undock_pending)
            {
                ReinventoryToolTip = "Undock operation in progress - cart must be docked before requesting an inventory";
                return false;
            }
            else if (!_dock_plugin.SubStorageDocked)
            {
                ReinventoryToolTip = "Cart is not docked";
                return false;
            }
            else if (!_dock_plugin._external_communications.BarcodeReaderAvailable)
            {
                ReinventoryToolTip = "Bar code reader isn't available to read cart id";
                return false;
            }
            // make sure the dock plugin knows about all of the teachpoints needed to reinventory
            else if (!_dock_plugin.CartDefinitionAvailable())
            {
                ReinventoryToolTip = String.Format("Cannot reinventory cart '{0}' because the cart definition is missing", _cart_barcode);
                return false;
            }
            else if (_cart_barcode == "")
            {
                ReinventoryToolTip = "Cart id not scanned successfully";
                return false;
            }

            string reason;
            var ext_ok = _dock_plugin._external_communications.ReinventoryAllowed( _dock_plugin.Name, out reason);
            if( !ext_ok) {
                ReinventoryToolTip = reason;
                return false;
            }

            ReinventoryToolTip = "Reinventory cart";
            return true;
        }

        private void ExecuteRequestToUndock()
        {
            // This currently turns the magnets off for either 60 seconds, or until the cart-present sensor turns off whichever comes first
            // 
            // TODO:
            // 1) This should PARK THE ROBOT FIRST
            // 2) This should put the behavior engine in WAIT_FOR_UNDOCK state so that no other robot action can happen
            // 3) When this completes, it should exit the robot back to IDLE state
            //
            // This should be CANCELLABLE:
            // 1) Get rid of the 60 second timeout (only allow exit via Cancel)
            // 2) Change button text 
            // 3) if user presses button again before cart-present sensor turns off, turn the magnets back on and transition back to IDLE state (also restore button text)
            //

            _undock_pending = true;
            Action undock_thread = new Action( UndockThread);
            undock_thread.BeginInvoke( UndockThreadComplete, null);
        }

        private void UndockThread()
        {
            _dock_plugin.Undock();
        }

        private void UndockThreadComplete( IAsyncResult iar)
        {
            try {
                AsyncResult ar = (AsyncResult)iar;
                Action caller = (Action)ar.AsyncDelegate;
                caller.EndInvoke( iar);
            } catch( Exception ex) {
                string message= String.Format( "Could not undock cart at '{0}': {1}", _dock_plugin.Name, ex.Message);
                MessageBox.Show( message);
                _log.Info( message);
            }
            _undock_pending = false;
        }

        private bool CanExecuteRequestToUndock()
        {
            // DKM 2011-10-13 TODO shouldn't we prevent a cart from being undocked if it's currently in use by
            //                worklist that is being processed?

            if( _undock_pending)
            {
                RequestToUndockToolTip = "Undock operation in progress - please move the cart out of the dock";
                return false;
            }

            string reason;
            bool ext_ok = _dock_plugin._external_communications.UndockAllowed( _dock_plugin.Name, out reason);
            if( !ext_ok) {
                RequestToUndockToolTip = reason;
                return false;
            }
            // ok to undock
            RequestToUndockToolTip = _dock_plugin.SubStoragePresent ? "Undock cart" : "Cart not present";
            return _dock_plugin.SubStoragePresent;
        }

        private void _update_timer_Tick(object sender, EventArgs e)
        {
            try
            {
                // cart present
                if( _dock_plugin.SubStoragePresent)
                    CartPresentColor = Brushes.DarkGreen;
                else
                    CartPresentColor = Brushes.LightGray;

                // cart docked
                if (_undock_pending) {
                    CartDockedColor = Brushes.Goldenrod;
                    CartDockedToolTip = "Undock operation in progress, magnets de-energized";
                } else if( _dock_plugin.SubStorageDocked) {
                    CartDockedColor = Brushes.DarkGreen;
                    CartDockedToolTip = "Cart is present and magnets are on";
                } else {
                    CartDockedColor = Brushes.LightGray;
                    CartDockedToolTip = "Cart is not present and magnets are off";
                }

                // cart reinventorying
                if (_dock_plugin._external_communications.Reinventorying)
                    CartReinventoryingColor = Brushes.DarkGoldenrod;
                else if ( _cart_reinventoried)
                    CartReinventoryingColor = Brushes.DarkGreen;
                else
                    CartReinventoryingColor = Brushes.LightGray;
            }
            catch (Exception)
            {
                // do nothing for now
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged == null)
                return;
            PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion
    }
}
