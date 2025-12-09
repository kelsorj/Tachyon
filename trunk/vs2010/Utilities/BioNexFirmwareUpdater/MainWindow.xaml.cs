using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using log4net;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using BioNex.SynapsisPrototype;
using BioNex.Shared.LibraryInterfaces;
using BioNexFirmwareUpdater;
using GalaSoft.MvvmLight.Command;
using System.ComponentModel;
using BioNex.Shared.TechnosoftLibrary;

namespace BioNex.Utilities.BioNexFirmwareUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(MainWindow));
        [Export("MEFContainer")]
        public CompositionContainer Container;
        [Import(typeof(DeviceManager))]
        private DeviceManager _device_manager;
        [Export("DeviceManager.filename")]
        public string DeviceManagerFilename { get; set; }
        [Export("LabwareDatabase.filename")]
        public string LabwareDatabaseFilename { get; set; }

        private bool _updateable_device_selected;
        private List<FirmwareDeviceInfo> _selections;

        private List<BioNex.Shared.LibraryInterfaces.FirmwareDeviceInfo> _updateable_devices;
        public List<BioNex.Shared.LibraryInterfaces.FirmwareDeviceInfo> UpdateableDevices
        {
            get { return _updateable_devices; }
            set
            {
                _updateable_devices = value;
                OnPropertyChanged("UpdateableDevices");
            }
        }
        
        public RelayCommand UpdateFirmwareCommand { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            UpdateFirmwareCommand = new RelayCommand( ExecuteUpdateFirmware, CanExecuteUpdateFirmware);
            _selections = new List<FirmwareDeviceInfo>();

            RefreshDeviceList();
        }

        private void RefreshDeviceList()
        {
            try
            {
                _selections.Clear();
                DeviceManagerFilename = BioNex.Shared.Utils.FileSystem.GetAppPath() + "\\devices.s3db";
                LabwareDatabaseFilename = BioNex.Shared.Utils.FileSystem.GetAppPath() + "\\labware.s3db";
                // load plugins
                BioNex.Shared.PluginManager.PluginLoader.LoadPlugins(this, typeof(App), out Container, new List<string>(), "", _log);
                _device_manager.LoadDeviceFile();
                var device_infos = _device_manager.GetAllDevices();
                var updateable_devices = from x in device_infos where x as IFirmwareUpdateable != null select x as IFirmwareUpdateable;
                List<FirmwareDeviceInfo> updateable_device_info = new List<FirmwareDeviceInfo>();
                // now connect to the updateable devices and get the necessary information from them
                foreach (var ud in updateable_devices)
                {
                    (ud as BioNex.Shared.DeviceInterfaces.DeviceInterface).Connect();
                    FirmwareDeviceInfo info = ud.GetDeviceInfo();
                    updateable_device_info.Add(info);
                }

                // temporary... just to get more items
                updateable_device_info.Add(new FirmwareDeviceInfo("dummy1", "adsf", "asdf", 1, null, new List<FirmwareVersionInfo> { new FirmwareVersionInfo { AxisId = 1, CurrentMajorVersion = 2, CurrentMinorVersion = 3, AxisName = "X", LatestCompatibleMajorVersion = 3, LatestCompatibleMinorVersion = 0 } }, true));
                updateable_device_info.Add(new FirmwareDeviceInfo("dummy2", "adsf", "asdf", 1, null, new List<FirmwareVersionInfo> { new FirmwareVersionInfo { AxisId = 1, CurrentMajorVersion = 2, CurrentMinorVersion = 3, AxisName = "X", LatestCompatibleMajorVersion = 3, LatestCompatibleMinorVersion = 0 } }, true));

                UpdateableDevices = updateable_device_info;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // basic idea: http://www.dotnetscraps.com/dotnetscraps/post/Insert-any-binary-file-in-C-assembly-and-extract-it-at-runtime.aspx
        // addressing embedded path properly: http://www.codeproject.com/KB/dotnet/embeddedresources.aspx
        private bool ExtractFirmware( int axis_id, short major, short minor, out string firmware_path)
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string temp_path = System.IO.Path.GetTempPath();
            firmware_path = String.Format("{0}\\{1}.sw", temp_path, axis_id);
            var input = assembly.GetManifestResourceStream(String.Format("HiGIntegrationTestApp.Firmware.{0}_{1}.{2}.sw", axis_id, major, minor));
            // only overwrite the file if a matching version exists!
            if (input != null)
            {
                if (System.IO.File.Exists(firmware_path))
                    System.IO.File.Delete(firmware_path);
                var output = System.IO.File.Open(firmware_path, System.IO.FileMode.CreateNew);
                CopyStream(input, output);
                input.Dispose();
                output.Dispose();
            }

            return input != null;
        }

        private void CopyStream(System.IO.Stream input, System.IO.Stream output)
        {
            byte[] buffer = new byte[32768];
            while (true)
            {
                int read = input.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                    return;
                output.Write(buffer, 0, read);
            }
        }

        /// <summary>
        /// Compares device's major and minor firmware information to the latest version stored in the assembly
        /// </summary>
        /// <param name="device_major"></param>
        /// <param name="device_minor"></param>
        /// <returns></returns>
        private bool NewerFirmwareAvailable( short current_major, short current_minor, short device_major, short device_minor)
        {
            return current_major > device_major || (current_major == device_major && current_minor > device_minor);
        }

        public void ExecuteUpdateFirmware()
        {
            
            // although the plugin might indicate that there is newer firmware available, it doesn't mean that the
            // utility HAS this firmware image available.  Remember, the plugin knows what the latest version is, but
            // the user could accidentally run an older utility with a newer plugin.  Technically, this wouldn't
            // happen since everything gets installed at the same time, but you never know...

            // for each selected device:
            bool some_errors = false;
            foreach( var selection in _selections) {
                // check that it is actully updateable, since multiselect allows the user to select any combination of devices
                if( !selection.UpdateAvailable)
                    continue;
                // then the utility has to query the plugin for the latest version info.
                List<FirmwareVersionInfo> version_infos = selection.FirmwareVersionInfo;
                // then it has to look for the matching firmware files in its embedded resource manifest
                List<FirmwareDeviceInfo> failures = new List<FirmwareDeviceInfo>();
                foreach( var info in version_infos) {
                    string copy_path;
                    if( !TechnosoftConnection.ExtractFirmware( System.Reflection.Assembly.GetExecutingAssembly(), "BioNex.Utilities.BioNexFirmwareUpdater.Firmware", selection.ProductName, info.AxisId, info.LatestCompatibleMajorVersion, info.LatestCompatibleMinorVersion, out copy_path)) {
                        failures.Add( new FirmwareDeviceInfo( selection.Name, 
                                                              selection.ProductName,
                                                              selection.SerialNumber,
                                                              selection.AdapterId,
                                                              selection.DeviceRef,
                                                              new List<FirmwareVersionInfo> { info },
                                                              false));
                        break;
                    }
                }
                // if it can't find it, display error message and bail
                if( failures.Count != 0) {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine( "Unable to extract firmware updates for the following devices:");
                    foreach( var f in failures) {
                        sb.AppendLine( String.Format( "Device name: {0}, Product: {1}, Axis ID: {2}, Target firmware: {3}.{4}",
                                       f.Name, f.ProductName, f.FirmwareVersionInfo[0].AxisId, f.FirmwareVersionInfo[0].LatestCompatibleMajorVersion, f.FirmwareVersionInfo[0].LatestCompatibleMinorVersion));
                    }
                    MessageBox.Show( sb.ToString());
                    return;
                }
                // otherwise, update.  The device, when called, should assume that its firmware update file is in the user temp folder, using the naming convention
                try {
                    selection.DeviceRef.UpgradeFirmware();
                } catch( Exception ex) {
                    some_errors = true;
                    MessageBox.Show( String.Format( "Unable to update firmware(s) for {0} device '{1}' on adapter {2}: {3}", selection.ProductName, selection.Name, selection.AdapterId, ex.Message));
                }
            }

            if( some_errors) {
                MessageBox.Show( "Not all devices had their firmware updated.");
            } else {
                MessageBox.Show( "All selected devices have had their firmware updated successfully.");
            }

            RefreshDeviceList();
        }

        public bool CanExecuteUpdateFirmware()
        {
            return _updateable_device_selected;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _updateable_device_selected = false;
            // compile a list of selections, based on what's been added and removed
            foreach( FirmwareDeviceInfo info in e.AddedItems) {
                _selections.Add( info);
            }
            foreach( FirmwareDeviceInfo info in e.RemovedItems) {
                _selections.Remove( info);
            }
            foreach( FirmwareDeviceInfo info in _selections) {
                if( info.UpdateAvailable) {
                    _updateable_device_selected = true;
                    return;
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _device_manager.Close();
        }

    }
}
