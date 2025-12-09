using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using BioNex.Shared.TechnosoftLibrary;
using System.Diagnostics;
using System.Collections.ObjectModel; // requires WindowsBase reference
using BioNex.BumblebeeAlphaGUI.SchedulerInterface;
using System.Windows.Input;
using System.Windows;
using BioNex.Shared.DeviceInterfaces;
using System.Windows.Controls; // required by ICommand, requires PresentationCore and PresentationFramework references
using BioNex.BumblebeeAlphaGUI;
using BioNex.Shared.ErrorHandling;
using System.Windows.Threading;
using BioNex.BumblebeeAlphaGUI.Model;
using BioNex.Shared.LabwareDatabase;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using BioNex.Shared.LibraryInterfaces;
using log4net;
using GalaSoft.MvvmLight.Messaging;
using BioNex.BumblebeeAlphaGUI.TipOperations;

namespace BioNex.BumblebeeAlphaGUI.ViewModel
{
    [Export("ViewModel")]
    [Export(typeof(IError))]
    public partial class MainViewModel : BaseViewModel, IError
    {
        private ILog _log = LogManager.GetLogger(typeof(MainViewModel));


        #region Main GUI properties
        //---------------------------------------------------------------------
        public ObservableCollection<string> SchedulerPluginNames
        {
            get {
                return _model.GetSchedulerPluginNames();
            }
            
            private set {}
        }
        public ObservableCollection<string> RobotStoragePluginNames
        {
            get {
                return _model.GetRobotStoragePluginNames();
            }
            private set {}
        }
        public ObservableCollection<string> TipHandlingMethodNames { get; private set; }
        public ObservableCollection<string> AllDevicePluginNames
        {
            get {
                return _model.GetAllDevicePluginNames();
            }
            private set {}
        }

        private string _tip_handling_method;
        public string SelectedTipHandlingMethod
        {
            get { return _tip_handling_method; }
            set {
                _tip_handling_method = value;
                RaisePropertyChanged( "SelectedTipHandlingMethod");
            }
        }

        // properties
        private string _hitpick_filepath;
        public string HitpickFilepath
        {
            get { return _hitpick_filepath; }
            set {
                _hitpick_filepath = value;
                RaisePropertyChanged( "HitpickFilepath");
            }
        }
        public bool AxesHomed
        { 
            get {
                List<IAxis> axes_not_homed = new List<IAxis>();
                return _model.IsHomed( axes_not_homed);
            }
        }

        private int _selected_tab_index;
        public int SelectedTabIndex
        {
            get { return _selected_tab_index; }
            set {
                _selected_tab_index = value;
                RaisePropertyChanged( "SelectedTabIndex");
            }
        }

        private int _progress_bar_maximum;
        public int ProgressBarMaximum
        {
            get { return _progress_bar_maximum; }
            set {
                _progress_bar_maximum = value;
                RaisePropertyChanged( "ProgressBarMaximum");
            }
        }

        private int _progress_bar_value;
        public int ProgressBarValue
        {
            get { return _progress_bar_value; }
            set {
                _progress_bar_value = value;
                RaisePropertyChanged( "ProgressBarValue");
            }
        }

        public BumblebeeAlphaGUI.Model.Model.ProtocolState ProtocolState { get { return _model.ProtocolExecutionState; } }
        
        public string SelectedSchedulerPlugin{ private get; set; }
        public string SelectedDevicePlugin { private get; set; }

        /// <summary>
        /// for error handling, we've got an ObservableCollection<> that handles the automatic
        /// updates to the GUI via databinding.  The List<> is what's modified via the
        /// ErrorInterface.  We then use the DispatcherTimer to deal with moving errors from
        /// the List to the ObservableCollection, to prevent the GUI from updating via
        /// a worker thread.
        /// </summary>
        public ObservableCollection<ErrorPanel> Errors { get; set; }
        private List<ErrorData> _errors;
        private DispatcherTimer _timer;
        //---------------------------------------------------------------------
        #endregion

        #region Diagnostics properties
        //---------------------------------------------------------------------
        // diagnostics properties
        // the C# property is used as a string->double converter for the private properties.
        // the private properties will be used internally to move the correct stage and channel axes.
        private byte _selected_channel;
        public byte SelectedChannel 
        { 
            get {
                return _selected_channel;
            }
            set {
                _selected_channel = value;
                // whenever the channel is selected, we need to get an updated
                // UL and LR teachpoint for the stage
                // get the teachpoint from the model
                UpdateTeachpointPositionsForTooltips();
                _log.Debug( String.Format( "Selected channel {0}", _selected_channel));
            }
        }
        private byte _selected_stage;
        public byte SelectedStage
        {
            get {
                return _selected_stage;
            }
            set {
                _selected_stage = value;
                // whenever the stage is selected, we need to get an updated
                // UL and LR teachpoint for the stage
                UpdateTeachpointPositionsForTooltips();
                _log.Debug( String.Format( "Selected stage {0}", _selected_stage));
            }
        }

        private string _selected_tipbox;
        public string SelectedTipBox
        { 
            get { return _selected_tipbox; }
            set {
                _selected_tipbox = value;
                UpdateTipNames();
            }
        }

        // for tip press test
        public string SelectedTip { get; set; } 
        // tooltip to make tip testing clearer for the user
        //private string _tipon_tooltip;
        public string TestTipOnToolTip { get; set; }

        private void UpdateTeachpointPositionsForTooltips()
        {
            _current_lr_teachpoint = _model.GetLRTeachpoint( _selected_channel, _selected_stage);
            _current_ul_teachpoint = _model.GetULTeachpoint( _selected_channel, _selected_stage);
            _current_wash_teachpoint = _model.GetWashTeachpoint( _selected_channel);
        }

        private void UpdateTipNames()
        {
            TipNames.Clear();
            // get the labware info
            //! \todo get rid of this direct instantiation of the labware database -- needs to be replaced by MEF
            TipBox tipbox = _model.GetLabware(SelectedTipBox) as TipBox;
            Debug.Assert( tipbox != null);
            int number_of_tips = tipbox.NumberOfTips;
            for( int i=0; i<number_of_tips; i++)
                TipNames.Add( BioNex.Shared.Utils.Wells.IndexToWellName( i, number_of_tips));
        }

        private ObservableCollection<int> _channel_list;
        public ObservableCollection<int> ChannelList
        {
            get {
                return _channel_list;
            }
        }
        private ObservableCollection<int> _stage_list;
        public ObservableCollection<int> StageList
        {
            get {
                return _stage_list;
            }
        }

        public ObservableCollection<string> TipBoxNames { get; private set; }

        // for jogging -- this is how the View gets the increment value
        // into the ViewModel.  The ViewModel then uses these values
        // to pass to the Move() calls into the Model.
        public double XIncrement { get; set; }
        public double YIncrement { get; set; }
        public double ZIncrement { get; set; }
        public double WIncrement { get; set; }
        public double RIncrement { get; set; }
        // for position readout
        public void UpdatePositions()
        {
            double x, y, z, w, r;
            _model.UpdatePositions( SelectedChannel, SelectedStage, out x, out y, out z, out w, out r);
            XPosition = x;
            YPosition = y;
            ZPosition = z;
            WPosition = w;
            RPosition = r;
        }
        private double _x_position;
        public double XPosition
        {
            get {
                return _x_position;
            }
            set {
                _x_position = value;
                RaisePropertyChanged( "XPosition");
            }

        }
        private double _y_position;
        public double YPosition
        {
            get {
                return _y_position;
            }
            set {
                _y_position = value;
                RaisePropertyChanged( "YPosition");
            }

        }
        private double _z_position;
        public double ZPosition
        {
            get {
                return _z_position;
            }
            set {
                _z_position = value;
                RaisePropertyChanged( "ZPosition");
            }

        }
        private double _w_position;
        public double WPosition
        {
            get {
                return _w_position;
            }
            set {
                _w_position = value;
                RaisePropertyChanged( "WPosition");
            }

        }
        private double _r_position;
        public double RPosition
        {
            get {
                return _r_position;
            }
            set {
                _r_position = value;
                RaisePropertyChanged( "RPosition");
            }

        }

        private Positions _current_ul_teachpoint;
        public Positions CurrentULTeachpoint
        {
            get {
                // updated in UpdateTeachpointPositionsForTooltips, every time a new stage / channel is selected
                return _current_ul_teachpoint;
            }
        }

        private Positions _current_lr_teachpoint;
        public Positions CurrentLRTeachpoint
        {
            get {
                // updated in UpdateTeachpointPositionsForTooltips, every time a new stage / channel is selected
                return _current_lr_teachpoint;
            }
        }

        private Positions _current_wash_teachpoint;
        public Positions CurrentWashTeachpoint
        {
            get {
                // updated in UpdateTeachpointPositionsForTooltips, every time a new stage / channel is selected
                return _current_wash_teachpoint;
            }
        }

        public ObservableCollection<string> TipNames { get; set; }

        public bool DisableXYAxesChecked
        {
            get {
                return _model.IsChannelXEnabled( SelectedChannel) || _model.IsStageYEnabled( SelectedStage);
            }
        }

        /// <summary>
        /// homes all axes on the device in the proper order
        /// </summary>
        public void Home()
        {
            _model.Home();
        }

        private bool IsChannelHomed( List<IAxis> axes_not_homed)
        {
            return _model.IsChannelHomed( SelectedChannel, axes_not_homed);
        }

        private bool IsStageHomed( List<IAxis> axes_not_homed)
        {
            return _model.IsStageHomed( SelectedStage, axes_not_homed);
        }

        public void HomeX( byte channel_id_0_based)
        {
            _model.HomeX( (byte)(channel_id_0_based + 1));
        }

        public void HomeY( byte stage_id_0_based)
        {
            _model.HomeY( (byte)(stage_id_0_based + 1));
        }

        public void HomeZ( byte channel_id_0_based)
        {
            _model.HomeZ( (byte)(channel_id_0_based + 1));
        }

        public void HomeW( byte channel_id_0_based)
        {
            _model.HomeW( (byte)(channel_id_0_based + 1));
        }

        public void HomeR( byte stage_id_0_based)
        {
            _model.HomeR( (byte)(stage_id_0_based + 1));
        }

        //---------------------------------------------------------------------
        #endregion

        #region Setup properties
        public string MotorSettingsPath { get; set; }
        public string HardwareConfigurationPath { get; set; }
        public string TSMSetupFolder { get; set; }
        public string TeachpointPath { get; set; }
        #endregion

        [Import("Model")]
        public BumblebeeAlphaGUI.Model.Model _model { get; set; }
                
        public MainViewModel()
        {
            //SchedulerPluginNames = new ObservableCollection<string>();
            //RobotStoragePluginNames = new ObservableCollection<string>();
            TipHandlingMethodNames = new ObservableCollection<string>();
            TipNames = new ObservableCollection<string>();

            InitializeCommands();

            // initialize the increment properties
            XIncrement = 1;
            YIncrement = 1;
            ZIncrement = 1;
            WIncrement = 1;
            RIncrement = 1;

            HitpickFilepath = @"c:\hitpickgui.xml";

            // for error handling
            Errors = new ObservableCollection<ErrorPanel>();
            _errors = new List<ErrorData>();
            _timer = new DispatcherTimer();
            _timer.Tick += new EventHandler(_timer_Tick);
            _timer.Interval = new TimeSpan( 0, 0, 0, 0, 250);
            _timer.Start();

            Messenger.Default.Register<ProgressValue>( this, HandleProgressValueUpdate);
            Messenger.Default.Register<ProgressMax>( this, HandleProgressMaxUpdate);
        }

        private void HandleProgressValueUpdate( ProgressValue value)
        {
            ProgressBarValue += value.Value;
        }

        private void HandleProgressMaxUpdate( ProgressMax value)
        {
            ProgressBarMaximum = value.Maximum;
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            // prevent interleaved events.  at least, this is what we had to do in MFC...
            _timer.Stop();
            for( int i=_errors.Count - 1; i>=0; i--) {
                Errors.Add( new ErrorPanel( _errors[i]));
                _errors.RemoveAt( i);
            }
            CommandManager.InvalidateRequerySuggested();
            _timer.Start();
        }

        public void ResetServos()
        {
            _model.ResetServos();
        }

        public void TestTipOn()
        {
            _model.TestTipOn( SelectedTipBox, SelectedChannel, SelectedStage, SelectedTip);
        }

        public void ServoOn()
        {
            _model.ServoOn();
        }

        public void StopAllMotors()
        {
            _model.StopAllMotors();
        }

        public void SetSystemSpeed( int percent)
        {
            _model.SetSystemSpeed( percent);
        }

        public void DisableXYAxes( bool disable)
        {
            _model.DisableXYAxes( SelectedChannel, SelectedStage, disable);
        }

        public byte GetNumberOfChannels()
        {
            return _model.GetNumberOfChannels();
        }

        public byte GetNumberOfStages()
        {
            return _model.GetNumberOfStages();
        }

        public void ReturnChannelsHome()
        {
            _model.ReturnChannelsHome();
        }

        public void ReturnStagesHome()
        {
            _model.ReturnStagesHome();
        }

        public void HomeAllDevices()
        {
            _model.HomeAllDevices();
        }

        public void Initialize()
        {
            try {
                _model.Initialize();
                // set property values that need to wait until hardware gets initialized
                // these are used by diagnostics
                _channel_list = new ObservableCollection<int>();
                // set the values in the comboboxes
                for( byte i=1; i<=GetNumberOfChannels(); i++)
                    _channel_list.Add( i);
                _stage_list = new ObservableCollection<int>();
                for( byte i=1; i<=GetNumberOfStages(); i++)
                    _stage_list.Add( i);
                // tipbox names
                TipBoxNames = new ObservableCollection<string>();
                LoadTipBoxNames();
            } catch( TechnosoftException ex) {
                MessageBox.Show("Could not connect to Bumblebee device.  If you were expecting this to run without hardware, then you need a version with simulation enabled.  " + ex.Message);
            } catch( KeyNotFoundException) {
                // here, we want to bail so the user can change the settings as requested in Initialize()
                return;
            }
            // load preferences
            //! \todo use constants for these...
            try {
                HitpickFilepath = _model.GetPreference( "last hitpick file");
            } catch( KeyNotFoundException) {
            }
            try {
                MotorSettingsPath = _model.GetPreference( "motor settings file");
            } catch( KeyNotFoundException) {
            }
            try {
                HardwareConfigurationPath = _model.GetPreference( "hardware configuration file");
            } catch( KeyNotFoundException) {
            }
            try {
                TSMSetupFolder = _model.GetPreference( "TSM setup folder");
            } catch( KeyNotFoundException) {
            }
            try {
                TeachpointPath = _model.GetPreference( "teachpoint file");
            } catch( KeyNotFoundException) {
            }
            // populate tip handling behaviors
            LoadTipHandlingMethodNames();
        }

        private void LoadTipBoxNames()
        {
            TipBoxNames = new ObservableCollection<string>( _model.GetTipBoxNames());
        }

        private void LoadTipHandlingMethodNames()
        {
            TipHandlingMethodNames.Add( TipHandlingStrings.None);
            TipHandlingMethodNames.Add( TipHandlingStrings.ChangeTip);
            TipHandlingMethodNames.Add( TipHandlingStrings.WashTip);
        }

        public void TeachUL()
        {
            List<IAxis> axes_not_homed = new List<IAxis>();
            bool channel_homed = IsChannelHomed( axes_not_homed);
            bool stage_homed = IsStageHomed( axes_not_homed);
            if( !channel_homed || !stage_homed) {
                MessageBox.Show(String.Format("You may not teach this point because the following axes are not homed: {0}", AlphaHardware.GetCommaDelimitedAxisIDs(axes_not_homed)));
                return;
            }
            Teach( true);
        }

        public void TeachLR()
        {
            List<IAxis> axes_not_homed = new List<IAxis>();
            bool channel_homed = IsChannelHomed( axes_not_homed);
            bool stage_homed = IsStageHomed( axes_not_homed);
            if( !channel_homed || !stage_homed) {
                MessageBox.Show(String.Format("You may not teach this point because the following axes are not homed: {0}", AlphaHardware.GetCommaDelimitedAxisIDs(axes_not_homed)));
                return;
            }
            Teach( false);
        }

        public void TeachWash()
        {
            byte channel_id = SelectedChannel;
            _model.TeachWash( channel_id);
        }

        public void TeachRobotPosition()
        {
            byte stage_id = SelectedStage;
            _model.TeachRobotPosition( stage_id);
        }

        public void TeachTipPosition()
        {
            byte channel_id = SelectedChannel;
            byte stage_id = SelectedStage;
            string tipbox = SelectedTipBox;
            _model.TeachTipPosition( channel_id, stage_id, tipbox);
        }

        public void Teach( bool upper_left)
        {
            string message = String.Format("Are you sure you want to teach the {0} point for channel {1}, stage {2} here?", (upper_left ? "upper left" : "lower right"), SelectedChannel, SelectedStage);
            if (MessageBox.Show(message, "Confirm Teachpoint", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;

            byte channel_id = SelectedChannel;
            byte stage_id = SelectedStage;

            _model.Teach( upper_left, channel_id, stage_id);
        }

        public void Execute()
        {
            // make sure the hardware is homed
            List<IAxis> axes_not_homed = new List<IAxis>();
            if( !_model.IsHomed( axes_not_homed)) {
                MessageBox.Show( String.Format( "You may not start a run until the following axes are homed: {0}", AlphaHardware.GetCommaDelimitedAxisIDs( axes_not_homed)));
                return;
            }

            // prompt the user to servo on all axes if necessary
            if( !_model.IsOn()) {
                MessageBoxResult result = MessageBox.Show( "Is it okay to servo on all axes now?", "Servo on?", MessageBoxButton.YesNo);
                if( result == MessageBoxResult.No)
                    return;
                _model.On();
            }
            ProgressBarValue = 0;
            _model.Execute( SelectedSchedulerPlugin, SelectedDevicePlugin, HitpickFilepath, SelectedTipHandlingMethod);
        }

        public void PauseScheduler()
        {
            _model.Pause();
        }

        public void ResumeScheduler()
        {
            _model.Resume();
        }

        public void AbortScheduler()
        {
            _model.Abort();
        }

        public UserControl GetDeviceDiagnosticsPanel( string plugin_name)
        {
            return _model.GetDeviceDiagnosticsPanel( plugin_name);
        }

        public void ShowDiagnostics( string plugin_name)
        {
            _model.ShowDiagnostics( plugin_name);
        }

        public void SavePreferences()
        {
            _model.SavePreferences();
        }

        public void Close()
        {
            _model.Close();
        }

        public void SaveSettingsFiles()
        {
            _model.SetPreference( "motor settings file", MotorSettingsPath);
            _model.SetPreference( "hardware configuration file", HardwareConfigurationPath);
            _model.SetPreference( "TSM setup folder", TSMSetupFolder);
            _model.SetPreference( "teachpoint file", TeachpointPath);
        }

        public void SaveLastHitpickFile()
        {
            _model.SetPreference( "last hitpick file", HitpickFilepath);
        }

        #region Move button commands
        public void JogX( bool positive_direction)
        {
            // we didn't need to pass in any increment or channel info because
            // the model should already know based on the databindings
            _model.JogX( SelectedChannel, positive_direction ? XIncrement : -XIncrement);
        }
        public void JogY( bool positive_direction)
        {
            // we didn't need to pass in any increment or channel info because
            // the model should already know based on the databindings
            _model.JogY( SelectedStage, positive_direction ? YIncrement : -YIncrement);
        }
        public void JogZ( bool positive_direction)
        {
            // we didn't need to pass in any increment or channel info because
            // the model should already know based on the databindings
            _model.JogZ( SelectedChannel, positive_direction ? ZIncrement : -ZIncrement);
        }
        public void JogW( bool positive_direction)
        {
            // we didn't need to pass in any increment or channel info because
            // the model should already know based on the databindings
            _model.JogW( SelectedChannel, positive_direction ? WIncrement : -WIncrement);
        }
        public void JogR( bool positive_direction)
        {
            // we didn't need to pass in any increment or channel info because
            // the model should already know based on the databindings
            _model.JogR( SelectedStage, positive_direction ? RIncrement : -RIncrement);
        }
        public void MoveAboveUL( double distance_above_mm)
        {
            byte channel_id = SelectedChannel;
            byte stage_id = SelectedStage;
            _model.MoveAboveUL( channel_id, stage_id, distance_above_mm);
        }
        public void MoveToUL()
        {
            byte channel_id = SelectedChannel;
            byte stage_id = SelectedStage;
            _model.MoveToUL( channel_id, stage_id);
        }
        public void MoveAboveLR( double distance_above_mm)
        {
            byte channel_id = SelectedChannel;
            byte stage_id = SelectedStage;
            _model.MoveAboveLR( channel_id, stage_id, distance_above_mm);
        }
        public void MoveToLR()
        {
            byte channel_id = SelectedChannel;
            byte stage_id = SelectedStage;
            _model.MoveToLR( channel_id, stage_id);
        }
        public void MoveAboveWash()
        {
            byte channel_id = SelectedChannel;
            _model.MoveAboveWash( channel_id);
        }
        public void MoveToWash()
        {
            byte channel_id = SelectedChannel;
            _model.MoveToWash( channel_id);
        }
        #endregion

        #region ErrorInterface Members

        public void AddError(ErrorData error)
        {
            SelectedTabIndex = 1; // select the error tab
            _errors.Add( error);
        }

        #endregion
    }
}
