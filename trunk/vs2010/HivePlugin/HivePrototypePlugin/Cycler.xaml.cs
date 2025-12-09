using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using BioNex.Shared.DeviceInterfaces;
using GalaSoft.MvvmLight.Command;
using log4net;
using BioNex.Shared.ThreadsafeMessenger;

namespace BioNex.HivePrototypePlugin
{
    /// <summary>
    /// The Cycler control owns all of the individual device teachpoint user controls, and keeps track
    /// of how many devices have their teachpoints in use.
    /// </summary>
    public partial class Cycler : UserControl, INotifyPropertyChanged
    {
        private static readonly ILog _log = LogManager.GetLogger( typeof( Cycler));
        public RelayCommand StartCyclerCommand { get; set; }
        public RelayCommand StopCyclerCommand { get; set; }
        public RelayCommand AddCyclerDeviceCommand { get; set; }

        public ICollectionView CyclerDevice { get; set; }
        public ICollectionView CyclerDeviceTeachpoints { get; set; }
        public ICollectionView LabwareNames { get; set; }
        public ICollectionView Orientations { get; set; }
        public ObservableCollection<CyclerDeviceTeachpointSelection> TargetDeviceTeachpoints { get; set; }

        private readonly ThreadsafeMessenger _messenger;
        private CyclerStateMachine _sm;

        private HivePlugin _plugin;
        public HivePlugin Plugin
        { 
            get { return _plugin; }
            set {
                _plugin = value;
                IEnumerable< DeviceInterface> accessible_devices = Plugin.DataRequestInterface.Value.GetAccessibleDeviceInterfaces();
                CyclerDevice = CollectionViewSource.GetDefaultView( accessible_devices);
                CyclerDevice.CurrentChanged += new EventHandler(CyclerDevice_CurrentChanged);
                // populate labware names
                LabwareNames = CollectionViewSource.GetDefaultView( Plugin.LabwareNames);
                // register messenger messages after plugin gets set
                _messenger.Register<CyclerStateMachine.PlateTransferComplete>( this, (message) => { ProgressBarValue++; });
            }
        }

        private int _progressbar_value;
        public int ProgressBarValue
        {
            get { return _progressbar_value; }
            set {
                _progressbar_value = value;
                OnPropertyChanged( "ProgressBarValue");
            }
        }

        private int _progressbar_max;
        public int ProgressBarMaximum
        {
            get { return _progressbar_max; }
            set {
                _progressbar_max = value;
                OnPropertyChanged( "ProgressBarMaximum");
            }
        }

        private bool _enabled;
        public bool Enabled
        {
            get { return _enabled; }
            set {
                _enabled = value;
                OnPropertyChanged( "Enabled");
            }
        }

        private bool _stop_cycler_enabled;
        public bool StopCyclerEnabled
        {
            get { return _stop_cycler_enabled; }
            set {
                _stop_cycler_enabled = value;
                OnPropertyChanged( "StopCyclerEnabled");
            }
        }

        private int _number_of_iterations;
        public int NumberOfIterations
        {
            get { return _number_of_iterations; }
            set
            {
                _number_of_iterations = value;
                Enabled = _number_of_iterations > 0;
                OnPropertyChanged("NumberOfIterations");
            }
        }

        public Cycler()
        {
            InitializeComponent();
            DataContext = this;

            TargetDeviceTeachpoints = new ObservableCollection<CyclerDeviceTeachpointSelection>();
            Orientations = CollectionViewSource.GetDefaultView( new string[] { "Portrait", "Landscape" });

            StartCyclerCommand = new RelayCommand( ExecuteStartCycler, CanExecuteStartCycler);
            StopCyclerCommand = new RelayCommand( ExecuteStopCycler, CanExecuteStopCycler);
            AddCyclerDeviceCommand = new RelayCommand( ExecuteAddCyclerDevice, CanExecuteAddCyclerDevice);

            _messenger = new ThreadsafeMessenger();

            ProgressBarValue = 0;
            ProgressBarMaximum = 0;
            NumberOfIterations = 1;
            Enabled = true;
        }

        void CyclerDevice_CurrentChanged(object sender, EventArgs e)
        {
            // if the device selection changes, then we need to change the teachpoint combobox as well
            ICollectionView view = sender as ICollectionView;
            if (view == null)
                return;

            if( CyclerDevice == null || CyclerDevice.CurrentItem == null)
                return;
            
            CyclerDeviceTeachpoints = CollectionViewSource.GetDefaultView(Plugin.LoadTeachpointNamesForDevice(CyclerDevice.CurrentItem as AccessibleDeviceInterface));
            OnPropertyChanged("CyclerDeviceTeachpoints");
        }

        private void ExecuteStartCycler()
        {
            try {
                // compile list of all target locations
                Dictionary<string,List<string>> device_and_teachpoint_map = new Dictionary<string,List<string>>();
                foreach( var device_selection_control in TargetDeviceTeachpoints) {
                    List<string> selected_locations = device_selection_control.GetSelectedLocations();
                    string device_name = device_selection_control.GetSelectedDevice();
                    if( device_and_teachpoint_map.ContainsKey( device_name)) {
                        device_and_teachpoint_map[device_name].AddRange( selected_locations);
                    } else {
                        device_and_teachpoint_map.Add( device_name, selected_locations);
                    }

                    // disable all controls so their contents can't be messed with
                    device_selection_control.Enabled = false;
                }

                // create the cycler state machine and start it in a thread
                string start_device = CyclerDevice.CurrentItem.ToString();
                string start_location = CyclerDeviceTeachpoints.CurrentItem.ToString();
                string labware = LabwareNames.CurrentItem.ToString();
                bool portrait_orientation = Orientations.CurrentItem.ToString().ToLower() == "portrait";
                CyclerThreadDelegate thread = new CyclerThreadDelegate( CyclerThread);
                // reset progressbar
                ProgressBarValue = 0;
                List<Tuple<string,string>> devices_and_teachpoints = ExpandDevicesAndTeachpoints( device_and_teachpoint_map);
                ProgressBarMaximum = devices_and_teachpoints.Count() * 2 * NumberOfIterations; // there are 2 transfers per teachpoint
                // disable controls as necessary
                StopCyclerEnabled = true;
                Enabled = false;
                // save the start time
                DateTime start_time = DateTime.Now;
                thread.BeginInvoke( start_device, start_location, labware, portrait_orientation, devices_and_teachpoints, CyclerThreadComplete, start_time);
            } catch( Exception) {
                StopCyclerEnabled = false;
                Enabled = true;
            }
        }

        private void ExecuteStopCycler()
        {
            if( _sm != null)
                _sm.Stop();
        }

        private static List<Tuple<string, string>> ExpandDevicesAndTeachpoints(Dictionary<string, List<string>> device_and_teachpoint_map)
        {
            List<Tuple<string,string>> results = new List<Tuple<string,string>>();
            // loop over all of the keys in the map, and expand the list of device->location names
            foreach( var key in device_and_teachpoint_map.Keys) {
                results.AddRange( from x in device_and_teachpoint_map[key] select new Tuple<string,string>(key,x));
            }
            return results;
        }

        public delegate void CyclerThreadDelegate( string start_device, string start_location, string labware, bool portrait, List<Tuple<string,string>> device_and_teachpoint_map);

        public void CyclerThread( string start_device, string start_location, string labware, bool portrait,
                                  List<Tuple<string,string>> devices_and_teachpoints)
        {
            if (Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "Teachpoint Cycler";
            _sm = new CyclerStateMachine(Plugin, _messenger, start_device, start_location, labware, portrait, devices_and_teachpoints, NumberOfIterations);
            _sm.Start();
        }

        public void CyclerThreadComplete( IAsyncResult iar)
        {
            try {
                AsyncResult ar = (AsyncResult)iar;
                CyclerThreadDelegate caller = (CyclerThreadDelegate)ar.AsyncDelegate;
                caller.EndInvoke( iar);
                string time = String.Format("Teachpoint cycling took {0:0.000} minutes", (DateTime.Now - (DateTime)iar.AsyncState).TotalMinutes);
                _log.Info(time);
                MessageBox.Show(time);
            } catch( Exception ex) {
                _log.Error( ex.Message, ex);
            } finally {
                // re-enable the individual device/teachpoint controls
                foreach( var device_selection_control in TargetDeviceTeachpoints) {
                    device_selection_control.Enabled = true;   
                }
                // re-enable the add and start buttons, disable the stop button
                Enabled = true;
                StopCyclerEnabled = false;
            }
        }

        private bool CanExecuteStartCycler()
        {
            return true;
        }

        private bool CanExecuteStopCycler()
        {
            return true;
        }

        private void ExecuteAddCyclerDevice()
        {
            IList< string> device_names = Plugin.DataRequestInterface.Value.GetAccessibleDeviceInterfaces().Select( adi => adi.Name).ToList();
            TargetDeviceTeachpoints.Add( new CyclerDeviceTeachpointSelection( TargetDeviceTeachpoints.Count(), device_names, Plugin.GetTeachpointNames, DeleteDeviceTeachpointsControl));
        }

        private void DeleteDeviceTeachpointsControl( CyclerDeviceTeachpointSelection which)
        {
            TargetDeviceTeachpoints.Remove( which);
        }

        private bool CanExecuteAddCyclerDevice()
        {
            return true;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion
    }
}
