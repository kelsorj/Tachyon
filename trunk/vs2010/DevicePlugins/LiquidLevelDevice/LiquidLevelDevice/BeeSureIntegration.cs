using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.DeviceInterfaces;

namespace BioNex.LiquidLevelDevice
{
    /// <summary>
    /// Integrators should only pay attention to the details contained within this interface.  It is intended to make
    /// usage of the BeeSure integration driver simple and straightforward, as it eliminates all of unnecessary details
    /// in the BeeSureIntegration class.
    /// </summary>
    public interface IBeeSureIntegration
    {
        /// <summary>
        /// Establishes a connection to the BeeSure hardware, but does not home the axes.
        /// </summary>
        /// <param name="device_properties">The device-specific properties</param>
        /// <example>
        /// This sample demonstrates how to initialize a BeeSure device.  Please note that everything up to,
        /// and including the call to Initialize is required before calling any of the other methods in the
        /// API.  Subsequent code examples will assume that the requisite functions have been called
        /// beforehand.
        /// <code>
        /// BeeSureIntegration bsi = new BeeSureIntegration();
        ///            
        ///  // use the Synapsis device manager to get device properties
        ///  // only need to do this when your app launches
        ///  var db = new BioNex.SynapsisPrototype.DeviceManagerDatabase();
        ///  var device_properties = db.GetProperties("BioNex", BioNex.Shared.DeviceInterfaces.BioNexDeviceNames.BeeSure, "Liquid Level Sensor");
        ///  device_properties["company"] = "BioNex";
        ///  device_properties["product"] = BioNex.Shared.DeviceInterfaces.BioNexDeviceNames.BeeSure;
        ///  device_properties["name"] = "My BeeSure";
        ///  // 0 = don't simulate, 1 = simulate
        ///  device_properties["simulate"] = "0";
        ///  // set the motor_adapter value to be the lowest of the IDs present.  If you open the USB-CANmodul Utility,
        ///  // you will see the device IDs enumerated.
        ///  int motor_adapter = 0;
        ///  int sensor_adapter = motor_adapter + 1;
        ///  device_properties["motor CAN device id"] = motor_adapter.ToString();
        ///  device_properties["sensor CAN device id"] = sensor_adapter.ToString();
        ///
        ///  // you can create your own labware "database", which is just a lookup table of strings to
        ///  // a class that implements the IBeeSureLabwareProperties interface
        ///  Dictionary<string, IBeeSureLabwareProperties> labware_database = new Dictionary<string, IBeeSureLabwareProperties>();
        ///  // add your plate definitions like this
        ///  string selected_labware = "96 well plate";
        ///  labware_database.Add( selected_labware, new BeeSureLabware(selected_labware, (short)num_rows, (short)num_columns, row_spacing, column_spacing, thickness, well_radius));
        ///
        ///  // all BeeSure methods block
        ///  bsi.Initialize(device_properties);
        /// </code>
        /// </example>
        void Initialize( IDictionary<String,String> device_properties);
        /// <summary>
        /// Attach an instance of a ScanProgressPanel to this BeeSure instance so that the progress panel receives updates during a scan
        /// </summary>
        void AttachScanProgressPanel(ScanProgressPanel panel);
        /// <summary>
        /// Homes all of the axes.
        /// </summary>
        /// <example>
        /// This is an example of how to home the BeeSure.
        /// <code>
        /// BeeSureIntegration bsi = new BeeSureIntegration();
        /// // ... see code example for Initialize method to see what goes here
        /// bsi.Home();
        /// </code>
        /// </example>
        void Home();
        /// <summary>
        /// Runs the BeeSure Calibration routine, assuming an EMPTY STAGE.  Calibration will be labware dependent.
        /// </summary>
        /// <param name="properties">The relevant labware properties as defined in the IBeeSureLabwareProperties interface</param>
        /// <example>
        /// This is an example of how to calibrate the BeeSure.
        /// <code>
        /// BeeSureIntegration bsi = new BeeSureIntegration();
        /// // ... see code example for Initialize method to see what goes here
        /// bsi.Home();
        /// IBeeSureLabwareProperties labware_properties = GetLabwareProperties(selected_labware);
        /// bsi.Calibrate(labware_properties);
        /// </code>
        /// </example>
        void Calibrate(IBeeSureLabwareProperties properties);
        /// <summary>
        /// Scans the plate based on the provided labware properties
        /// Returns null if there was an error during the scan
        /// </summary>
        /// <param name="properties">The relevant labware properties as defined in the IBeeSureLabwareProperties interface</param>
        /// <example>
        /// This is an example of how to scan a plate.
        /// <code>
        /// BeeSureIntegration bsi = new BeeSureIntegration();
        /// // ... see code example for Initialize method to see what goes here
        /// bsi.Home();
        /// IBeeSureLabwareProperties labware_properties = GetLabwareProperties(selected_labware);
        /// bsi.Calibrate(labware_properties);
        /// bsi.Capture(labware_properties);
        /// </code>
        /// </example>
        IEnumerable<IBeeSureCaptureResults> Capture( IBeeSureLabwareProperties properties);
        /// <summary>
        /// Displays the diagnostics dialog.
        /// </summary>
        /// <param name="modal"></param>
        /// <param name="properties">The relevant labware properties as defined in the IBeeSureLabwareProperties interface</param>
        void ShowDiagnostics(Boolean modal, IEnumerable<IBeeSureLabwareProperties> properties);
        /// <summary>
        /// Closes the connection to the BeeSure hardware.
        /// </summary>
        void Close();
        /// <summary>
        /// Retrieves the last error experienced by the BeeSure hardware, whether reported or not.
        /// Empty string if last operation completed successfully
        /// </summary>
        String LastError { get; }
        /// <summary>
        /// Whether or not the device is homed.
        /// </summary>
        Boolean IsHomed { get; }
        /// <summary>
        /// Whether or not a communications connection has been made with the device.  The device must
        /// be connected before it can accept any commands.
        /// </summary>
        Boolean IsConnected { get; }
        /// <summary>
        /// For BeeSure equipped with Rotating stage, move stage to portrait or landscape park position for robot access
        /// </summary>
        /// <param name="portrait">True if portrait, False if landscape</param>
        void MoveToParkPosition(Boolean portrait);
    }

    /// <summary>
    ///  Results structure returned by Capture
    /// </summary>
    public interface IBeeSureCaptureResults
    {
        /// <summary>
        /// 0 based sensor channel index
        /// </summary>
        int Channel { get; }
        /// <summary>
        /// 0 based labware column index
        /// </summary>
        int Column { get; }
        /// <summary>
        /// 0 based scan row -- actual labware row is a function of this, the channel index, and the number of rows in the labware:
        ///     labware_row == row + (channel * labware_rows / 8)
        /// </summary>
        int Row { get; }
        /// <summary>
        /// averaged x axis position (absolute) for this sample
        /// </summary>
        double XAverage { get; }
        /// <summary>
        /// averaged y axis position (absolute) for this sample
        /// </summary>
        double YAverage { get; }
        /// <summary>
        /// The averaged measurement value, including all outliers
        /// </summary>
        double PopulationAverage { get; }
        // DKM 2012-03-23 the unlatexed formula below is Sqrt( Sum( (measurement - population_average)^2) / num_measurements)
        //\f$ \sqrt{ \frac{ \sum_{n=0}^num_measurements{(measurement - population_average)^2}/{num_measurements} } }\f$
        // \f$(x_1,y_1)\f$
        /// <summary>
        /// The standard deviation of the average according to the formula:
        /// Sqrt( Sum( (measurement - population_average)^2) / num_measurements)
        /// </summary>
        double StandardDeviation { get; }
        /// <summary>
        /// The averaged measurement after discarding any measurements whose absolute distance from the poulation average is greater than one standard deviation
        /// </summary>
        double Average { get; }
    }

    /// <summary>
    /// Provides only the relevant subset of labware properties to the BeeSure
    /// </summary>
    public interface IBeeSureLabwareProperties
    {
        /// <summary>
        /// The name of the labware, used internally to pull volume fit data.
        /// </summary>
        String Name { get; }
        /// <summary>
        /// The number of rows in the plate to be scanned.
        /// </summary>
        Int16 NumberOfRows { get; }
        /// <summary>
        /// The number of columns in the plate to be scanned.
        /// </summary>
        Int16 NumberOfColumns { get; }
        /// <summary>
        /// The center-to-center spacing between well rows.
        /// </summary>
        Double RowSpacing { get; }
        /// <summary>
        /// The center-to-center spacing between well columns.
        /// </summary>
        Double ColumnSpacing { get; }
        /// <summary>
        /// The thickness of the plate.
        /// </summary>
        Double Thickness { get; }
        /// <summary>
        /// The radius of the well.
        /// </summary>
        Double WellRadius { get; }
    }

    /// <summary>
    /// This is the class that integrators will instantiate to control the BeeSure hardware
    /// </summary>
    public sealed class BeeSureIntegration : IBeeSureIntegration
    {
        private ILLSensorPlugin _sensor_plugin;
        private DeviceInterface _device;
        private string _last_error;

        public BeeSureIntegration()
        {
            _sensor_plugin = new LLSensorPlugin();
            _device = (DeviceInterface)_sensor_plugin;
        }

        public bool IsHomed { get { return _device.IsHomed; } }
        public bool IsConnected { get { return _sensor_plugin.IsConnected; } }

        public void Initialize( IDictionary<string,string> properties )
        {
            try
            {
                _last_error = "";
                var device_info = new DeviceManagerDatabase.DeviceInfo(properties["company"], properties["product"], properties["name"], false, properties);
                _device.SetProperties(device_info);
                _device.Connect();
            }
            catch (Exception e)
            {
                _last_error = e.Message;
            }
        }

        public void AttachScanProgressPanel(ScanProgressPanel panel)
        {
            panel.Plugin = _sensor_plugin;
        }

        public void Home()
        {
            try
            {
                _last_error = "";
                _device.Home();
            }
            catch (Exception e)
            {
                _last_error = e.Message;
            }
        }

        public void Calibrate(IBeeSureLabwareProperties properties)
        {
            try
            {
                _last_error = "";
                // create a temporary single-plate labware "database" here
                LightweightLabwareDatabase db = new LightweightLabwareDatabase();
                db.AddLabware(new LightweightLabware(properties.Name, properties)); // add a plate with the given properties            
                _sensor_plugin.Model.LabwareDatabase = db;                                 // set the labware database for the plugin's model            
                _sensor_plugin.Calibrate();
            }
            catch (Exception e)
            {
                _last_error = e.Message;
            }
        }

        public IEnumerable<IBeeSureCaptureResults> Capture(IBeeSureLabwareProperties properties)
        {
            try
            {
                _last_error = "";
                // create a temporary single-plate labware "database" here
                LightweightLabwareDatabase db = new LightweightLabwareDatabase();
                db.AddLabware(new LightweightLabware(properties.Name, properties)); // add a plate with the given properties            
                _sensor_plugin.Model.LabwareDatabase = db;                                 // set the labware database for the plugin's model            
                return (IEnumerable<IBeeSureCaptureResults>)_sensor_plugin.Capture(properties.Name);                            // now scan the plate using this newly-created labware database
            }
            catch (Exception e)
            {
                _last_error = e.Message;
                return null;
            }
        }

        public void ShowDiagnostics(bool modal, IEnumerable<IBeeSureLabwareProperties> properties)
        {
            try
            {
                _last_error = "";
                // create a temporary single-plate labware "database" here
                LightweightLabwareDatabase db = new LightweightLabwareDatabase();
                foreach (var property in properties)
                    db.AddLabware(new LightweightLabware(property.Name, property)); // add a plate with the given properties            
                _sensor_plugin.Model.LabwareDatabase = db;                                 // set the labware database for the plugin's model            
                _device.ShowDiagnostics();
            }
            catch (Exception e)
            {
                _last_error = e.Message;
            }
        }

        public void Close()
        {
            try
            {
                _last_error = "";
                _device.Close();
            }
            catch (Exception e)
            {
                _last_error = e.Message;
            }
        }

        public string LastError
        {
            get { return _last_error; }
        }

        public void MoveToParkPosition(bool portrait)
        {
            try
            {
                _last_error = "";
                _sensor_plugin.MoveToParkPosition(portrait);
            }
            catch (Exception e)
            {
                _last_error = e.Message;
            }
        }
    }
}

/* \example BeeSureIntegrationSamples.cs
 * This is an example of how to use the BeeSureIntegration class
 */
