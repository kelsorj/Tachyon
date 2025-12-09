using System;
using System.Threading;
using System.Windows;
using BioNex.Exceptions;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Utils;
using log4net;

namespace BioNex.Plugins.Dock
{
    public interface IDockController
    {
        bool CartPresent { get;  }
        bool CartDoorClosed { get; }
        bool CartIsDocked { get; }
        bool CartIsReinventoried { get; set; }
        bool Connected { get; }
        bool CanReadBarcode { get; }
        string CartBarcode { get; }
        string CartHumanReadable { get; }
        string CartFilePrefix { get; }
        int NumberOfRacks { get; }
        int NumberOfSlots { get; }
        bool HasDoorSensor { get; }

        void Close();
        void DockCart();
        void UndockCart();
        void ScanCartBarcode();
        // not what I would consider to be an interface method, but I needed it here so I could activate the magnets early
        void EnableDockMagnets(bool enable=true);

        void AbortDockOrUndock();

        void UserEnteredCartBarcode(string cart_barcode);
        // used by teachpoint transformation only
        string GetFilePrefixFromBarcode( string barcode);
    }

    public class SimController : IDockController
    {
        #region IDockController Members

        public void EnableDockMagnets(bool enable=true) { }

        public bool CartPresent
        {
            get { return false; }
        }

        public bool CartDoorClosed
        {
            get { return true; }
        }

        public bool CartIsDocked { get; set; }

        public bool CartIsReinventoried { get; set; }

        public bool Connected { get; set; }

        public void Close()
        {
            Connected = false;
        }

        public string CartBarcode { get; set; }
        public string CartHumanReadable { get; set; }
        public string CartFilePrefix { get; set; }
        public int NumberOfRacks { get; private set; }
        public int NumberOfSlots { get; private set; }
        public bool HasDoorSensor { get { return false; }}

        public SimController()
        {
            Connected = true;
        }

        public void DockCart()
        {
            CartIsDocked = true;
            CartIsReinventoried = false;
        }

        public void UndockCart()
        {
            CartBarcode = "";
            CartHumanReadable = "";
            CartFilePrefix = "";
            CartIsDocked = false;
            CartIsReinventoried = false;
        }

        public void ScanCartBarcode() {}

        public bool CanReadBarcode { get { return true; } }

        public void AbortDockOrUndock() {}

        public void UserEnteredCartBarcode(string cart_barcode) { CartBarcode = CartHumanReadable = cart_barcode; }
        public string GetFilePrefixFromBarcode( string barcode) { return barcode; }
        #endregion
    }

    public class Controller : IDockController
    {
        private DeviceManagerProperties properties { get; set; }
        private IOInterface _io_interface { get; set; }
        private IOInterface _system_io_interface { get; set; }
        private RobotInterface _robot_interface { get; set; }
        private ThreadedUpdates _update_thread { get; set; }
        private static readonly ILog _log = LogManager.GetLogger( typeof( Controller));
        private CartDefinitionFile _cart_lookup { get; set; }
        private string _plugin_instance_name { get; set; }
        public string DockName { get { return _plugin_instance_name; }}
        /// <summary>
        /// used to prevent multiple log messages and excessive I/O commands
        /// </summary>
        private bool _detected_cart { get; set; }

        // IDockController properties
        public bool CartPresent { get; private set; }
        public bool CartDoorClosed { get; private set; }
        public bool CartIsDocked { get; private set; }
        public bool CartIsReinventoried { get; set; }
        public bool Connected { get; private set; }
        public string CartBarcode { get; private set; }
        public string CartHumanReadable { get; private set; }
        public string CartFilePrefix { get; private set; }
        public bool CanReadBarcode {get { return _robot_interface.CanReadBarcode(); } }
        public bool HasDoorSensor
        {
            get {
                return properties.DoorSensorInputIndex != -1;
            }
        }

        private bool _cart_dock_requested { get; set; }
        private bool _abort_requested;

        private int _num_racks;
        public int NumberOfRacks
        {
            get { return _num_racks; }
            set { _num_racks = value; }
        }

        private int _num_slots;
        public int NumberOfSlots
        {
            get { return _num_slots; }
            set { _num_slots = value; }
        }

        public Controller( string plugin_instance_name, DeviceManagerProperties props, IOInterface io_interface, IOInterface system_io_interface, RobotInterface robot_interface, CartDefinitionFile cart_lookup)
        {
            properties = props;
            CartBarcode = "";
            CartHumanReadable = "";
            CartFilePrefix = "";
            _io_interface = io_interface;
            _system_io_interface = system_io_interface;
            _robot_interface = robot_interface;
            _cart_lookup = cart_lookup;
            _plugin_instance_name = plugin_instance_name;
            if( _io_interface == null){
                string message = String.Format( "Could not find IO provider '{0}'", properties.IODevice);
                MessageBox.Show( message);
                throw new DeviceConnectionException( plugin_instance_name, message);
            }

            _update_thread = new ThreadedUpdates( String.Format( "{0} input update thread", plugin_instance_name), Update, 100);
            _update_thread.Start();

            Connected = true;
        }

        public void Close()
        {
            _update_thread.Stop();
            Connected = false;
        }

        private void Update()
        {
            try
            {
                CartPresent = _io_interface.GetInput(properties.PresenceSensorInputIndex);
                CartIsDocked = CartPresent && _cart_dock_requested && CartBarcode != "";
                CartDoorClosed = properties.DoorSensorInputIndex == -1 ? false : _system_io_interface.GetInput(properties.DoorSensorInputIndex);
            }
            catch (Exception)
            {
                // here we don't exit the thread of we have errors.  We need to think about this a little more.  Should we
                // stop updating after X failures?  If so, how do we restart this thread?  Disconnect / reconnect?  I am
                // going to log here, but am worried that this will cause issues with logs getting too large.
                _log.ErrorFormat( "Could not get input states from IO interface");
            }
        }

        public void EnableDockMagnets( bool enable=true)
        {
            foreach( var bit in properties.CartLockOutputIndexes)
                _io_interface.SetOutputState( bit, enable);
        }

        public void DockCart()
        {
            // DKM 2011-05-18 clear CartBarcode because we found an edge case where Reed didn't undock first, but instead
            //                ripped the cart away from the magnets.  When DockCart failed, it kept the previous
            //                CartBarcode in memory.
            CartBarcode = "";
            CartHumanReadable = "";
            CartFilePrefix = "";
            _cart_dock_requested = true;
            CartIsReinventoried = false;
            // turn on the magnets
            EnableDockMagnets();
            // wait to sense the cart
            DateTime start_time = DateTime.Now;
            const double timeout_s = 60.0;
            while( !CartPresent && (DateTime.Now - start_time).TotalSeconds < timeout_s && !_abort_requested) {
                Thread.Sleep( 100);
            }
            if( _abort_requested) {
                _abort_requested = false;
                return;
            }

            if( (DateTime.Now - start_time).TotalSeconds >= timeout_s)
                throw new Exception( "Timed out waiting for cart to arrive at '" + _plugin_instance_name + "'");
            // give cart time to settle after kachunking
            Thread.Sleep( 1000);
            // now scan the cart ID barcode
            // for now, assume a Hive robot -- but we need to figure out a better way to tie a Dock to a specific robot!
            ScanCartBarcode();
        }

        public void UndockCart()
        {
            _log.InfoFormat( "User requested to undock {0}", _plugin_instance_name);

            try {
                EnableDockMagnets( false);
            } catch( Exception ex) {
                _log.InfoFormat( "Could not undock because the magnets were not disabled: {0}", ex.Message);
                return;
            }

            // always force this cart to be invalid if we successfully disable the magnets, so we don't
            // accidentally try to place plates with the cart unavailable.
            _cart_dock_requested = false;
            _detected_cart = false;
            CartIsReinventoried = false;
            CartBarcode = "";
        }
                
        /// <summary>
        /// Scans the cart barcode (position is in device manager properties)
        /// Sets CartBarcode, CartHumanReadable, and CartFilePrefix.
        /// </summary>
        public void ScanCartBarcode()
        {
            // ReadBarcode(x,z) will automatically load the default barcode reader configuration
            string barcode = _robot_interface.ReadBarcode( properties.BarcodeX, properties.BarcodeZ, 3);
            if( Constants.IsNoRead(barcode)) {
                throw new BarcodeException( "Could not read cart ID");
            }

            CartLookupHelper(barcode);
        }

        public void AbortDockOrUndock()
        {
            _abort_requested = true;
        }

        public void UserEnteredCartBarcode(string cart_barcode)
        {
            CartLookupHelper(cart_barcode);
        }

        public string GetFilePrefixFromBarcode( string barcode)
        {
            string human_readable;
            string file_prefix;
            _cart_lookup.GetCartIdentifiersFromBarcode(barcode, out human_readable, out file_prefix);
            return file_prefix;
        }

        private void CartLookupHelper(string barcode)
        {
            string human_readable;
            string file_prefix;

            try
            {
                _cart_lookup.GetNumberOfRacksAndSlotsFromBarcode(barcode, out _num_racks, out _num_slots);
                _cart_lookup.GetCartIdentifiersFromBarcode(barcode, out human_readable, out file_prefix);
            }
            catch (Exception ex)
            {
                string message = String.Format("Could not look up cart definition for cart '{0}': {1}", barcode, ex.Message);
                _log.Info(message);
                throw new Exception(message);
            }

            CartBarcode = barcode;
            CartHumanReadable = human_readable ?? barcode;
            CartFilePrefix = file_prefix ?? barcode;
        }
    }
}
