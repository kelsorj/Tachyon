using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using System.Windows;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.Location;
using BioNex.Shared.Utils;
using log4net;
using BioNex.Shared.TaskListXMLParser;
using System.Threading;
using BioNex.Shared.PlateWork;

namespace BioNex.Hig
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(DeviceInterface))]
    public class HigPlugin : AccessibleDeviceInterface, IHasSystemPanel, PlateSchedulerDeviceInterface
    {
        private readonly ViewModel _viewModel;
        public ViewModel viewModel { get { return _viewModel; } }
        private SynapsisModel _model { get; set; }
        private static readonly ILog _log = LogManager.GetLogger(typeof(HigPlugin));
        private Window _diagnostics_window;
        private SystemPanel _system_panel;

        /// <summary>
        /// whether or not Home was commanded.  If true, then IsHomed can't return true until _home_requested is false.
        /// </summary>
        private bool _home_requested;

        private PlateLocation _bucket1_location = new PlateLocation( "Bucket 1");
        private PlateLocation _bucket2_location = new PlateLocation( "Bucket 2");
        
        public static class Commands
        {
            public const string Spin = "Spin";
            public const string OpenShield = "OpenShield";
        }

        public static class CommandParameters
        {
            public const string Accel = "accel";
            public const string Decel = "decel";
            public const string G = "g";
            public const string TimeInSeconds = "time_s";
            public const string BucketNumber = "bucket_number";
        }

        public double EstimatedTimeRemainingInSeconds { get { return _model == null ? 0 : _model.SpinSecRemaining; } }
        
        public double GetEstimatedSpinTime( double gs, double accel_percent, double decel_percent, double time_seconds)
        {
            try {
                return _model == null ? 0 : HigUtils.GetEstimatedCycleTime(gs, _model.RotationalRadiusMm, _model.SpindleAxis.Settings.Acceleration, accel_percent,
                                                                            _model.SpindleAxis.Settings.Acceleration, decel_percent, time_seconds);
            } catch( Exception) {
                return 0;
            }
        }

        #region DeviceInterface Members

        [ImportingConstructor]
        public HigPlugin([Import] SynapsisModel model)
        {
            _model = model;
            _viewModel = new ViewModel( _model);
            _system_panel = new SystemPanel();
            _system_panel.DataContext = _viewModel;
        }

        public string Manufacturer
        {
            get
            {
                return "BioNex";
            }
        }

        public string ProductName
        {
            get
            {
                return "HiG";
            }
        }

        public string Name
        {
            get { return _model.DeviceInstanceName; }
            private set { _model.DeviceInstanceName = value; }
        }

        /// <summary>
        /// This is required if we want the accessible device to appear correctly (by device instance name) in the robot teaching tab
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }

        public string Description
        {
            get
            {
                return "HiG microplate centrifuge";
            }
        }

        public void SetProperties(DeviceManagerDatabase.DeviceInfo deviceInfo)
        {
            Name = deviceInfo.InstanceName;
            // if Model.SetDeviceProperties took device_info, we wouldn't have to split it up like this
            _model.DeviceProperties = deviceInfo.Properties;
        }

        public System.Windows.Controls.UserControl GetDiagnosticsPanel()
        {
            var panel = new DiagnosticsPanel( _viewModel) {DataContext = this};
            return panel;
        }

        public void ShowDiagnostics()
        {
            if( _diagnostics_window == null) {
                _diagnostics_window = new System.Windows.Window();
                _diagnostics_window.Content = new BioNex.Hig.DiagnosticsPanel(_viewModel);
                _diagnostics_window.Title = Name + "- Diagnostics" + (_model.Simulating ? " (Simulating)" : "");
                _diagnostics_window.Closed += new EventHandler(_diagnostics_window_Closed);
                _diagnostics_window.Height = 630;
                _diagnostics_window.Width = 660;
            }
            _diagnostics_window.Show();
            _diagnostics_window.Activate();
        }

        void _diagnostics_window_Closed(object sender, EventArgs e)
        {
            _diagnostics_window.Content = null;
            _diagnostics_window = null;
        }

        public void Connect()
        {
            _model.Connected = true;
        }

        public bool Connected { get { return _model.Connected; } }

        public void Home()
        {
            Home(false);
        }

        public void Home(bool allow_abort, bool blocking=false)
        {
            _home_requested = true;
            Action home = new Action(() => { _model.Home(allow_abort); });
            if( !blocking)
                home.BeginInvoke(HomeComplete, null);
            else {
                try {
                    home.Invoke();
                } finally {
                    _home_requested = false;
                }
            }
        }

        private void HomeComplete(IAsyncResult iar)
        {
            try
            {
                AsyncResult ar = (AsyncResult)iar;
                Action caller = (Action)ar.AsyncDelegate;
                caller.EndInvoke(iar);
            }
            finally
            {
                _home_requested = false;
            }
        }

        public bool IsHomed
        {
            get
            {
                return _model.Homed && !_home_requested;
            }
        }

        public void Close()
        {
            _model.Connected = false;
        }

        public bool ExecuteCommand(string command, IDictionary<string, object> parameters)
        {
            if( command != Commands.Spin && command != Commands.OpenShield)
                return false;

            if( command == Commands.Spin) {
                // verify that the parameters we need are present
                // DKM this is a pretty stupid oversight -- no error message reporting via DeviceInterface???
                if( !parameters.ContainsKey( CommandParameters.Accel) || !parameters.ContainsKey( CommandParameters.Decel) ||
                    !parameters.ContainsKey( CommandParameters.G) || !parameters.ContainsKey( CommandParameters.TimeInSeconds))
                    return false;

                // if we got here we're all good to spin
                try {
                    double g = parameters[CommandParameters.G].ToDouble();
                    double accel = parameters[CommandParameters.Accel].ToDouble();
                    double decel = parameters[CommandParameters.Decel].ToDouble();
                    double time_s = parameters[CommandParameters.TimeInSeconds].ToDouble();
                    _log.Info( String.Format( "Spinning plate at {0}G, {1}% accel, {2}% decel, for {3}s", g, accel, decel, time_s));
                    _model.Spin( accel, decel, g, time_s, false);
                } catch( Exception ex) {
                    // DKM todo for Giles -- add log4net logging
                    Debug.WriteLine( ex.Message);
                    return false;
                }
            } else if( command == Commands.OpenShield) {
                if( !parameters.ContainsKey( CommandParameters.BucketNumber))
                    return false;
                try {
                    int bucket_number = parameters[CommandParameters.BucketNumber].ToInt();
                    _log.Info( String.Format( "Opening shield to bucket {0}", bucket_number));
                    // DKM 2011-08-24 this is temporary messed up because we're using number instead of index, but I guess as long as the semantics are clear it's okay
                    _model.OpenShield( bucket_number - 1, false);
                } catch( Exception ex) {
                    // DKM todo for Giles -- add log4net logging
                    Debug.WriteLine( ex.Message);
                    return false;
                }
            }

            return true;
        }

        public IEnumerable<string> GetCommands()
        {
            return new List<string> { Commands.Spin };
        }

        public void Abort()
        {
            _model.Abort();
        }

        public void Pause() {} 
        public void Resume() {}

        public void Reset()
        {
            _model.ResetPauseAbort();
        }

        public IEnumerable<PlateLocation> PlateLocationInfo
        {
            get
            {
                return new List<PlateLocation> { _bucket1_location, _bucket2_location };
            }
        }

        public PlateLocation GetLidLocationInfo( string location_name)
        {
            return null;
        }

        public int GetBarcodeReaderConfigurationIndex(string location_name)
        {
            return 0;
        }

        #endregion

        #region RobotAccessibleInterface Members


        public string TeachpointFilenamePrefix
        {
            get
            {
                return Name;
            }
        }

        #endregion

        #region IHasSystemPanel Members

        public System.Windows.Controls.UserControl GetSystemPanel()
        {
            return _system_panel;
        }

        #endregion

        #region PlateSchedulerDeviceInterface Members

        public event JobCompleteEventHandler JobComplete;

        public PlateLocation GetAvailableLocation(BioNex.Shared.PlateWork.ActivePlate active_plate)
        {
            // get the bucket that the scheduler wants to use
            PlateTask current_task = active_plate.GetCurrentToDo();
            // DKM 2012-01-23 not sure why .First isn't working here, but is with the BNX1536?  Couldn't copy with ToList, so have to loop over collection???
            int bucket_number = 0;
            foreach( var x in current_task.ParametersAndVariables) {
                if( x.Name == CommandParameters.BucketNumber)
                    bucket_number = int.Parse( x.Value);
            }
            Debug.Assert( bucket_number != 0);
            if( bucket_number == 2)
                return _bucket2_location.Available ? _bucket2_location : null;
            else
                return _bucket1_location.Available ? _bucket1_location : null;
        }

        public bool ReserveLocation(PlateLocation location, BioNex.Shared.PlateWork.ActivePlate active_plate)
        {
            // not my location to reserve.
            if( location != _bucket1_location && location != _bucket2_location){
                return false;
            }
            if( location == _bucket1_location)
                _bucket1_location.Reserved.Set();
            else if( location == _bucket2_location)
                _bucket2_location.Reserved.Set();
            return true;
        }

        public void LockPlace(PlatePlace place)
        {            
        }

        public void AddJob(BioNex.Shared.PlateWork.ActivePlate active_plate)
        {
            new Thread( () => JobThread( active_plate)){ Name = GetType().ToString() + " Job Thread", IsBackground = true}.Start();
        }

        public void EnqueueWorklist(BioNex.Shared.PlateWork.Worklist worklist)
        {
            throw new NotImplementedException();
        }

        protected void JobThread(ActivePlate active_plate)
        {
            active_plate.WaitForPlate();
            try
            {
                IDictionary<string, object> parameters = new Dictionary<string, object>();
                PlateTask current_task = active_plate.GetCurrentToDo();
                // DKM 2012-01-23 WTF why won't it let me use First???
                foreach( var x in current_task.ParametersAndVariables) {
                    if( x.Name == CommandParameters.Accel)
                        parameters[CommandParameters.Accel] = int.Parse( x.Value);
                    else if( x.Name == CommandParameters.Decel)
                        parameters[CommandParameters.Decel] = int.Parse( x.Value);
                    else if( x.Name == CommandParameters.G)
                        parameters[CommandParameters.G] = int.Parse( x.Value);
                    else if( x.Name == CommandParameters.BucketNumber)
                        parameters[CommandParameters.BucketNumber] = int.Parse( x.Value);
                    else if( x.Name == CommandParameters.TimeInSeconds)
                        parameters[CommandParameters.TimeInSeconds] = double.Parse( x.Value);
                }
                Debug.Assert( parameters.ContainsKey( CommandParameters.Accel) && parameters.ContainsKey( CommandParameters.BucketNumber) && parameters.ContainsKey( CommandParameters.Decel) &&
                              parameters.ContainsKey( CommandParameters.G) && parameters.ContainsKey( CommandParameters.TimeInSeconds), "HiG task in hitpick XML file is missing one or more parameters");
                DeviceInterface d = (this as DeviceInterface);
                d.ExecuteCommand(active_plate.GetCurrentToDo().Command, parameters);
            }
            catch (Exception)
            {
                // Log.Debug( "Exception occurred");
            }
            active_plate.MarkJobCompleted();
        }

        #endregion
    }
}
