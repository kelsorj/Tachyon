using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using DeviceManagerDatabase;
using log4net;

namespace BioNex.SynapsisPrototype
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(DeviceManager))]
    [Export(typeof(ISystemSetupEditor))]
    [Export(typeof(ExternalDataRequesterInterface))]
    public class DeviceManager : ISystemSetupEditor, ExternalDataRequesterInterface
    {
        // the device manager is in charge of loading all of the device plugins
        // these collections contain the device PLUGINS that are available in
        // the file system and resolvable.  These are not to be used for actual
        // hardware communication.  For that, you want to look at the *Available
        // collections instead.
        [ImportMany(typeof(DeviceInterface))]
        private IEnumerable<DeviceInterface> DevicePlugins { get; set; }

        // these Dictionaries are used for other objects in the system to get access
        // to all of the plugins that the DeviceManager loads
        // Every time you add a new interface to Synapsis, you should add a Dictionary
        // for it here.
        public Dictionary<string, DeviceInterface> DevicePluginsAvailable { get; private set; }
        public Dictionary<string, AccessibleDeviceInterface> AccessibleDevicePluginsAvailable { get; private set; }
        public Dictionary<string, PlateStorageInterface> PlateStoragePluginsAvailable { get; private set; }
        public Dictionary<string, RobotInterface> RobotPluginsAvailable { get; private set; }
        public Dictionary<string, IOInterface> IOPluginsAvailable { get; private set; }
        public Dictionary<string, SystemStartupCheckInterface> SystemStartupCheckPluginsAvailable { get; private set; }
        public Dictionary<string, ProtocolHooksInterface> ProtocolHooksPluginsAvailable { get; private set; }
        public Dictionary<string, SafetyInterface> SafetyPluginsAvailable { get; private set; }
        public Dictionary<string, SystemStatusInterface> SystemStatusPluginsAvailable { get; private set; }
        public Dictionary<string, StackerInterface> StackerPluginsAvailable { get; private set; }
        public Dictionary<string, DockablePlateStorageInterface> DockablePlateStoragePluginsAvailable { get; private set; }

        private static readonly ILog _log = LogManager.GetLogger( typeof( DeviceManager));
        private CompositionContainer _container { get; set; }

        public DeviceManagerDatabase db { get; private set; }

        /// <summary>
        /// This dictionary is used to keep track of which plate locations are currently occupied by
        /// plates.  Prevents robots from crashing plates into each other, and was originally intended
        /// to be used for task execution.
        /// </summary>
        // private Dictionary<AccessibleDeviceInterface,List<PlateLocationInfo>> AllPlateLocations { get; set; }
        
        [ImportingConstructor]
        public DeviceManager( [Import("DeviceManager.filename")] string database_filepath,
                              [Import("MEFContainer")] CompositionContainer c)
        {
            _container = c;
            db = new DeviceManagerDatabase( database_filepath);
            DevicePluginsAvailable = new Dictionary<string, DeviceInterface>();
            AccessibleDevicePluginsAvailable = new Dictionary<string, AccessibleDeviceInterface>();
            PlateStoragePluginsAvailable = new Dictionary<string, PlateStorageInterface>();
            RobotPluginsAvailable = new Dictionary<string,RobotInterface>();
            IOPluginsAvailable = new Dictionary< string, IOInterface>();
            SystemStartupCheckPluginsAvailable = new Dictionary<string,SystemStartupCheckInterface>();
            ProtocolHooksPluginsAvailable = new Dictionary<string,ProtocolHooksInterface>();
            SafetyPluginsAvailable = new Dictionary<string,SafetyInterface>();
            SystemStatusPluginsAvailable = new Dictionary<string, SystemStatusInterface>();
            DockablePlateStoragePluginsAvailable = new Dictionary<string,DockablePlateStorageInterface>();
            StackerPluginsAvailable = new Dictionary<string,StackerInterface>();
            // AllPlateLocations = new Dictionary<AccessibleDeviceInterface,List<PlateLocationInfo>>();
        }

        public void Close()
        {
            if( DevicePluginsAvailable != null) {
                // modified this to deal with ITechnosoftSharers.  Need to close all
                // other devices first, then ITS devices.
                /* remove ITechnosoftConnectionSharer
                var its_devices = from x in DevicePluginsAvailable 
                                  where x.Value is ITechnosoftConnectionSharer
                                  select x.Value;
                */
                foreach( DeviceInterface di in (from kvp in DevicePluginsAvailable select kvp.Value)) {
                    try {
                        // skip if ITS device
                        /* remove ITechnosoftConnectionSharer
                        if( di is ITechnosoftConnectionSharer)
                            continue;
                        */
                        // otherwise, close
                        di.Close();
                    } catch( Exception) {
                        // do nothing
                    }
                }

                // now close ITS devices
                /* remove ITechnosoftConnectionSharer
                foreach( var x in its_devices)
                    (x as DeviceInterface).Close();
                */
            }
        }

        /// <summary>
        /// Grabs all device information in the device manager database, and then tries
        /// to instantiate each of them.  If a device's plugin is not available, then
        /// load as much as possible and inform the user
        /// </summary>
        /// <remarks>
        /// I made LoadDeviceFile a separate, public method (rather than calling directly
        /// from LoadPlugins()) because the application might want to reload plugins
        /// after a user manually copies files into the \plugins folder.
        /// </remarks>
        public void LoadDeviceFile()
        {
            _log.Info( "Loading devices from database");
            ObservableCollection<DeviceInfo> devices = db.GetAllDeviceInfo();

            // if we want to support reloading, we need to clear the existing contents of Available plugins
            AccessibleDevicePluginsAvailable.Clear();
            RobotPluginsAvailable.Clear();
            PlateStoragePluginsAvailable.Clear();
            IOPluginsAvailable.Clear();
            SystemStartupCheckPluginsAvailable.Clear();
            ProtocolHooksPluginsAvailable.Clear();
            SafetyPluginsAvailable.Clear();
            DockablePlateStoragePluginsAvailable.Clear();
            SystemStatusPluginsAvailable.Clear();
            StackerPluginsAvailable.Clear();

            // for each of the devices that got enumerated, check to see if we have a
            // matching device plugin that was loaded by MEF

            // DKM 2012-02-15 the old code here was lame because it was O(n^2).  Use LINQ to make this more efficient?
            var device_matches = from x in devices from y in DevicePlugins
                                    where x.CompanyName.ToLower() == y.Manufacturer.ToLower() && x.ProductName.ToLower() == y.ProductName.ToLower()
                                    select new { InstanceInfo = x, PluginInfo = y };

            foreach( var match in device_matches)
            {
                if( match.InstanceInfo.Disabled) {
                    _log.InfoFormat( "skipped loading of device '{0}' because it is disabled in the device database", match.InstanceInfo.InstanceName);
                    continue;
                }

                _log.InfoFormat( "attempting to load device '{0}' [Manufacturer: {1}]", match.PluginInfo.ProductName, match.PluginInfo.Manufacturer);
                // we have a match, so create the plugin
                try {
                    DeviceInterface new_device = _container.GetExportedValues<DeviceInterface>().FirstOrDefault( x => x.GetType() == match.PluginInfo.GetType());
                    if( new_device == null) {
                        // reuse outer exception for error handling
                        throw new Exception( String.Format( "No device plugin of type '{0}' exists in the plugins folder.", match.PluginInfo.GetType().ToString()));
                    }

                    // first need to set the device parameters so the device can initialize itself
                    new_device.SetProperties( match.InstanceInfo);
                    // DKM 2011-03-22 check for instance name first, just in case I get device reloading working properly
                    //                (i.e. making changes to device manager from within Synapsis w/o a restart)
                    if( DevicePluginsAvailable.ContainsKey( match.InstanceInfo.InstanceName))
                        DevicePluginsAvailable[match.InstanceInfo.InstanceName] = new_device;
                    else
                        DevicePluginsAvailable.Add( match.InstanceInfo.InstanceName, new_device);

                    // see if this device supports any other interfaces
                    if (new_device as AccessibleDeviceInterface != null)
                        AccessibleDevicePluginsAvailable.Add(match.InstanceInfo.InstanceName, new_device as AccessibleDeviceInterface);
                    RobotInterface robot = new_device as RobotInterface;
                    if( robot != null) {
                        // ideally, I want to register the pick and place complete event handlers here, but
                        // need to route it through to Synapsis somehow...
                        //robot.PickComplete += 
                        RobotPluginsAvailable.Add( match.InstanceInfo.InstanceName, new_device as RobotInterface);
                    }
                    if( new_device as PlateStorageInterface != null)
                        PlateStoragePluginsAvailable.Add( match.InstanceInfo.InstanceName, new_device as PlateStorageInterface);
                    if( new_device as IOInterface != null)
                        IOPluginsAvailable.Add( match.InstanceInfo.InstanceName, new_device as IOInterface);
                    if( new_device as SystemStartupCheckInterface != null)
                        SystemStartupCheckPluginsAvailable.Add( match.InstanceInfo.InstanceName, new_device as SystemStartupCheckInterface);
                    if( new_device as ProtocolHooksInterface != null)
                        ProtocolHooksPluginsAvailable.Add( match.InstanceInfo.InstanceName, new_device as ProtocolHooksInterface);
                    if( new_device as SafetyInterface != null)
                        SafetyPluginsAvailable.Add( match.InstanceInfo.InstanceName, new_device as SafetyInterface);
                    if( new_device as SystemStatusInterface != null)
                        SystemStatusPluginsAvailable.Add( match.InstanceInfo.InstanceName, new_device as SystemStatusInterface);
                    if( new_device as DockablePlateStorageInterface != null)
                        DockablePlateStoragePluginsAvailable.Add( match.InstanceInfo.InstanceName, new_device as DockablePlateStorageInterface);
                    if( new_device as StackerInterface != null)
                        StackerPluginsAvailable.Add( match.InstanceInfo.InstanceName, new_device as StackerInterface);
                } catch( Exception ex) {
                    _log.ErrorFormat( "Could not load plugin {0}: {1}", match.InstanceInfo.InstanceName, ex.Message);
                }
                _log.InfoFormat( "loaded device '{0}' [Manufacturer: {1}, Product Name: {2}]", match.InstanceInfo.InstanceName, match.PluginInfo.Manufacturer, match.PluginInfo.ProductName);
            }
        }

        public void ShowEditor()
        {
            DeviceManagerEditor editor = new DeviceManagerEditor( this);
            editor.ShowDialog();
            //editor.Close();

            // DKM 2011-03-22 this is not as trivial as I had originally thought -- the issue being that we have to make the contents of
            //                DiagnosticsPlugins in SynapsisViewModel refresh its contents.
            /*
            // DKM 2011-03-22 experimental -- reload devices
            LoadDeviceFile();
            MessageBox.Show( "Device manager changes now take effect immediately and no longer require you to restart Synapsis.  Please report any bugs to Dave.", "EXPERIMENTAL FEATURE ALERT");
             */
        }

        public PlateStorageInterface GetPlateStorageWithBarcode( string barcode, out string plate_location_name)
        {
            var storage_devices = from p in PlateStoragePluginsAvailable select p.Value;
            foreach( PlateStorageInterface p in storage_devices) {
                if( p.HasPlateWithBarcode( barcode, out plate_location_name)) {
                    return p;
                }
            }
            plate_location_name = "";
            return null;
        }

        public RobotInterface GetRobotThatReachesLocation( AccessibleDeviceInterface caller, string location_name)
        {
            var robots = from r in RobotPluginsAvailable select r.Value;
            foreach( RobotInterface r in robots) {
                if( !r.GetTeachpointNames().ContainsKey( caller.Name)){
                    continue;
                }
                if( r.GetTeachpointNames()[ caller.Name].Contains( location_name)) {
                    return r;
                }
            }
            return null;
        }

        /*
        public void AddPlateLocations( AccessibleDeviceInterface device)
        {
            AllPlateLocations.Add( device, device.PlateLocationInfo.ToList());
        }

        public void ReservePlateLocation( AccessibleDeviceInterface device, string location_name)
        {
            AllPlateLocations[device].Where( x => x.Name == location_name).First().Used.WaitOne();
        }

        public void ReservePlateLocations( AccessibleDeviceInterface device)
        {
            foreach( var x in AllPlateLocations[device])
                x.Used.WaitOne();
        }

        public void FreePlateLocation( AccessibleDeviceInterface device, string location_name)
        {
            AllPlateLocations[device].Where( x => x.Name == location_name).First().Used.Set();
        }

        public void FreePlateLocations( AccessibleDeviceInterface device)
        {
            foreach( var x in AllPlateLocations[device])
                x.Used.Set();
        }
        */ 

        #region ISystemTool Members

        public void ShowTool()
        {
            ShowEditor();
        }

        public string Name
        {
            get
            {
                return "Device manager";
            }
        }

        #endregion

        #region ExternalDataRequesterInterface Members

        public SystemStartupCheckInterface.SafeToMoveDelegate SafeToMove { get; set;}

        public int GetDeviceTypeId( string company_name, string product_name)
        {
            return db.GetDeviceTypeId( company_name, product_name);
        }

        public IEnumerable<DeviceInterface> GetAllDevices()
        {
            return GetDeviceInterfaces();
        }

        public IEnumerable<DeviceInterface> GetDeviceInterfaces()
        {
            return from kvp in DevicePluginsAvailable select kvp.Value;
        }

        public IEnumerable<AccessibleDeviceInterface> GetAccessibleDeviceInterfaces()
        {
            return from kvp in AccessibleDevicePluginsAvailable select kvp.Value;
        }

        public IEnumerable<RobotInterface> GetRobotInterfaces()
        {
            return from kvp in RobotPluginsAvailable select kvp.Value;
        }

        public IEnumerable< PlateStorageInterface> GetPlateStorageInterfaces()
        {
            return from kvp in PlateStoragePluginsAvailable select kvp.Value;
        }

        public IEnumerable<SystemStatusInterface> GetSystemStatusInterfaces()
        {
            return from kvp in SystemStatusPluginsAvailable select kvp.Value;
        }

        public IEnumerable< IOInterface> GetIOInterfaces()
        {
            return from kvp in IOPluginsAvailable select kvp.Value;
        }

        public IEnumerable< SystemStartupCheckInterface> GetSystemStartupCheckInterfaces()
        {
            return from kvp in SystemStartupCheckPluginsAvailable select kvp.Value;
        }

        public IEnumerable< ProtocolHooksInterface> GetProtocolHooksInterfaces()
        {
            return from kvp in ProtocolHooksPluginsAvailable select kvp.Value;
        }

        public IEnumerable<SafetyInterface> GetSafetyInterfaces()
        {
            return from kvp in SafetyPluginsAvailable select kvp.Value;
        }

        public IEnumerable<StackerInterface> GetStackerInterfaces()
        {
            return from kvp in StackerPluginsAvailable select kvp.Value;
        }

        public IEnumerable<DockablePlateStorageInterface> GetDockablePlateStorageInterfaces()
        {
            return from kvp in DockablePlateStoragePluginsAvailable select kvp.Value;
        }

        // DKM 2011-04-05 needed something like this for reading sensors at locations for delidding
        public Func<bool> ReadSensorCallback( string device_name, string location_name)
        {
            var device = DevicePluginsAvailable[ device_name];
            // for now, test by forcing device to be of a new interface so I don't have to propagate
            // changes through every DeviceInterface
            return (device as ISensorQueryable).GetSensorCallback( location_name);
        }

        #endregion
    }
}
