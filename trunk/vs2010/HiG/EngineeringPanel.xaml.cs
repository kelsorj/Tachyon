using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using BioNex.Shared.SimpleWizard;
using BioNex.Shared.Utils;
using GalaSoft.MvvmLight.Command;
using log4net;
using BioNex.Hig.StateMachines;

namespace BioNex.Hig
{
    /// <summary>
    /// Interaction logic for EngineeringPanel.xaml
    /// </summary>
    public partial class EngineeringPanel : UserControl, INotifyPropertyChanged
    {
        private SynapsisModel _model;
        internal SynapsisModel Model
        {
            get { return _model; }
            set {
                _model = value;
                _dispatcher = _model.MainDispatcher;
                try {
                    DownloadEepromSettings();
                } catch( Exception) {
                    MessageBox.Show( "Could not load EEPROM settings from HiG.  Please reconnect and then reload EEPROM settings from the Engineering Panel.");
                }
            }
        }

        private double _spindle_pos;
        public double SpindlePosition
        {
            get { return _spindle_pos; }
            set {
                _spindle_pos = value;
                OnPropertyChanged( "SpindlePosition");
            }
        }

        private double _shield_pos;
        public double ShieldPosition
        {
            get { return _shield_pos; }
            set {
                _shield_pos = value;
                OnPropertyChanged( "ShieldPosition");
            }
        }

        private Dispatcher _dispatcher;
        private readonly ThreadedUpdates _engineering_position_updater;

        public RelayCommand ApplySettingsCommand { get; set; }
        public RelayCommand ReloadFromDriveToGUICommand { get; set; }
        public RelayCommand LoadDefaultsToGUICommand { get; set; }
        public RelayCommand ExportSettingsCommand { get; set; }

        public RelayCommand SpindleHomeCommand { get; set; }
        public RelayCommand SpindleServoOnCommand { get; set; }
        public RelayCommand SpindleServoOffCommand { get; set; }
        public RelayCommand ShieldHomeCommand { get; set; }
        public RelayCommand ShieldServoOnCommand { get; set; }
        public RelayCommand ShieldServoOffCommand { get; set; }
        public RelayCommand JogCWCommand { get; set; }
        public RelayCommand JogCCWCommand { get; set; }
        public RelayCommand JogShieldCWCommand { get; set; }
        public RelayCommand JogShieldCCWCommand { get; set; }
        public RelayCommand SetupSpindleHomeCommand { get; set; }
        public RelayCommand ImbalanceThresholdCalibrationCommand { get; set; }
        public RelayCommand TeachBucket1Command { get; set; }
        public RelayCommand TeachBucket2Command { get; set; }
        public RelayCommand TeachShieldClosedCommand { get; set; }
        public RelayCommand TeachShieldOpenCommand { get; set; }
        public RelayCommand DefaultThermostatCommand { get; set; }
        public RelayCommand ReprogramSpindleCommand { get; set; }
        public RelayCommand ReprogramShieldCommand { get; set; }

        public List<double> JogIncrements { get; set; }
        public double ShieldIncrement { get; set; }
        public double SpindleIncrement { get; set; }

        private EepromSetting _eeprom_setting_selection;
        private static readonly ILog _log = LogManager.GetLogger( typeof( EngineeringPanel));

        private bool _calibrating;

        private ObservableCollection<EepromSetting> _eeprom_settings;
        public ObservableCollection<EepromSetting> EepromSettings
        {
            get { return _eeprom_settings; }
            set {
                _eeprom_settings = value;
                OnPropertyChanged( "EepromSettings");
            }
        }

        public EngineeringPanel()
        {
            InitializeComponent();
            DataContext = this;

            JogIncrements = new List<double> { 0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1.0, 2.0, 5.0, 10.0, 30.0, 45.0 };
            ShieldIncrement = 1;
            SpindleIncrement = 0.1;

            InitializeCommands();            
            _engineering_position_updater = new ThreadedUpdates( "HiG engineering tab position update thread", UpdatePositionThread, 100);
        }

        private void UpdatePositionThread()
        {
            SpindlePosition = _model.CurrentSpindlePosition;
            ShieldPosition = _model.CurrentShieldPosition;
        }

        private void InitializeCommands()
        {
            Func<bool> CheckModelConnected = new Func<bool>( () => { return _model != null && _model.Connected; } );
            Func<bool> CanExecuteSpindleCommand = new Func<bool>( () => { return CheckModelConnected() && !_model.Spinning; } );

            ApplySettingsCommand = new RelayCommand( ExecuteApplySettings, CheckModelConnected);
            ReloadFromDriveToGUICommand = new RelayCommand( () => ExecuteReloadFromDriveToGUI(), CheckModelConnected);
            LoadDefaultsToGUICommand = new RelayCommand( ExecuteLoadDefaultsToGUI, CheckModelConnected);
            ExportSettingsCommand = new RelayCommand( ExecuteExportSettings, CheckModelConnected);

            SpindleHomeCommand = new RelayCommand(ExecuteSpindleServoHome, CanExecuteSpindleCommand);
            SpindleServoOnCommand = new RelayCommand(() => ExecuteSpindleServoOnOff(true), CanExecuteSpindleCommand);
            SpindleServoOffCommand = new RelayCommand(() => ExecuteSpindleServoOnOff(false), CanExecuteSpindleCommand);
            ShieldHomeCommand = new RelayCommand( ExecuteShieldServoHome, CheckModelConnected);
            ShieldServoOnCommand = new RelayCommand(() => ExecuteShieldServoOnOff(true), CheckModelConnected);
            ShieldServoOffCommand = new RelayCommand(() => ExecuteShieldServoOnOff(false), CheckModelConnected);
            JogCWCommand = new RelayCommand(() => ExecuteJogSpindle(false), CanExecuteSpindleCommand);
            JogCCWCommand = new RelayCommand( () => ExecuteJogSpindle( true), CanExecuteSpindleCommand);
            JogShieldCWCommand = new RelayCommand( () => ExecuteJogShield( false), CheckModelConnected);
            JogShieldCCWCommand = new RelayCommand( () => ExecuteJogShield( true), CheckModelConnected);
            SetupSpindleHomeCommand = new RelayCommand( ExecuteSetupSpindleHome, CheckModelConnected);
            ImbalanceThresholdCalibrationCommand = new RelayCommand( ExecuteImbalanceThresholdCalibration, CheckModelConnected);
            TeachBucket1Command = new RelayCommand( ExecuteTeachBucket1, CanExecuteSpindleCommand);
            TeachBucket2Command = new RelayCommand(ExecuteTeachBucket2, CanExecuteSpindleCommand);
            TeachShieldClosedCommand = new RelayCommand(ExecuteTeachShieldClosed, CheckModelConnected);
            TeachShieldOpenCommand = new RelayCommand( ExecuteTeachShieldOpen, CheckModelConnected);
            ReprogramSpindleCommand = new RelayCommand(ExecuteReprogramSpindle, CheckModelConnected);
            ReprogramShieldCommand = new RelayCommand(ExecuteReprogramShield, CheckModelConnected);
        }

        /// <summary>
        /// Downloads the settings from the HiG for display in the DataGrid.
        /// </summary>
        /// <returns></returns>
        public void DownloadEepromSettings( bool default_values_only=false)
        {
            EepromSettings = new ObservableCollection<EepromSetting>();
            foreach( var setting in Model.GetEepromSettings( default_values_only)) {
                EepromSettings.Add( new EepromSetting( setting));
            }
            OnPropertyChanged( "EepromSettings");
        }

        /// <summary>
        /// ReDownloads the settings from the HiG for display in the DataGrid.
        /// </summary>
        /// <returns></returns>
        public void ExecuteReloadFromDriveToGUI( bool display_prompts=true)
        {
            if( display_prompts && MessageBoxResult.No == MessageBox.Show( "Are you sure you want to load all settings from the device?", "Confirm Load", MessageBoxButton.YesNo))
                return;
            DownloadEepromSettings();
            if( display_prompts)
                MessageBox.Show( "Settings loaded from device successfully");
        }

        /// <summary>
        /// Populates Datagrid with default values
        /// </summary>
        /// <returns></returns>
        public void ExecuteLoadDefaultsToGUI()
        {
            if( MessageBoxResult.No == MessageBox.Show( "Are you sure you want to replace all settings with their default values?", "Confirm Reset", MessageBoxButton.YesNo))
                return;
            DownloadEepromSettings( true);
            MessageBox.Show( "Settings were replaced with default values.  Please click 'Save Settings to Device' to permanently save them in the device.");
        }

        /// <summary>
        /// Takes the values shown in the EEPROM datagrid and writes them to an XML file
        /// </summary>
        private void ExecuteExportSettings()
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = "XML file (*.xml)|*.xml";
            dlg.InitialDirectory = FileSystem.GetAppPath();
            if( dlg.ShowDialog() == true) {
                string filename = dlg.FileName;
                BioNex.Hig.EepromExport export = new BioNex.Hig.EepromExport();
                // loop over all of the EEPROM settings and stick into export for saving
                foreach( var setting in Model.GetEepromSettings()) {
                    export.Settings.Add( new BioNex.Hig.EepromExport.EepromKeyValuePair { Key = setting.HumanReadableName,
                                                                                          Value = setting.Value == null ? "" : setting.Value.ToString() });
                }                
                BioNex.Shared.Utils.FileSystem.SaveXmlConfiguration<BioNex.Hig.EepromExport>( export, filename);
            } else {
                return;
            }
            
        }

        private void ExecuteApplySettings()
        {
            MessageBoxResult response = MessageBox.Show("Are you sure you want to permanently save all of the values in the device?",
                                                         "Confirm Transfer And Store", MessageBoxButton.YesNo);
            if (response == MessageBoxResult.No)
                return;

            try {
                foreach( var setting in EepromSettings) {
                    // this is lame, but serial number is a special case, so we have to check
                    // the EepromSetting to see if it has a setter specified
                    if( setting.SetterFunc == null)
                        Model.WriteEepromSetting( setting);
                    else
                        setting.Save();
                }
                System.Threading.Thread.Sleep(500); // give the drives a little bit of time to digest their new eeprom settings
                Model.SpindleAxis.ResetDrive();
                Model.ShieldAxis.ResetDrive();
                MessageBox.Show( "Values saved successfully and device has been reset.  Please re-home before operation.");
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private void ExecuteSpindleServoOnOff(bool turn_on)
        {
            try {
                Model.RotorServoOnOff( turn_on);
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private void ExecuteSpindleServoHome()
        {
            HourglassWindow hg = HourglassWindow.ShowHourglassWindow(_dispatcher, this.GetParentWindow(), "Closing Shield Door", true, 15);
            try
            {
                // make sure shield is closed before doing this?
                var shield = Model.ShieldAxis;
                shield.CallFunctionAndWaitForDone("func_close_shield", TimeSpan.FromSeconds(15.0));
                HourglassWindow.CloseHourglassWindow(_dispatcher, hg);

                // hourglass window?
                hg = HourglassWindow.ShowHourglassWindow(_dispatcher, this.GetParentWindow(), "Homing Spindle Axis", true, 15);

                Model.RotorServoHome();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                HourglassWindow.CloseHourglassWindow(_dispatcher, hg);
            }
        }

        private void ExecuteShieldServoOnOff(bool turn_on)
        {
            try
            {
                Model.ShieldServoOnOff(turn_on);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ExecuteShieldServoHome()
        {
            HourglassWindow hg = HourglassWindow.ShowHourglassWindow(_dispatcher, this.GetParentWindow(), "Homing Shield Door Axis", true, 15);
            try
            {
                Model.ShieldServoHome();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                HourglassWindow.CloseHourglassWindow(_dispatcher, hg);
            }
        }

        private void ExecuteJogSpindle(bool positive_dir)
        {
            try {
                Model.JogRotor( positive_dir ? -SpindleIncrement : SpindleIncrement);
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private void ExecuteJogShield( bool positive_dir)
        {
            try {
                if( !Model.NoShieldMode) {
                    Model.JogDoor( positive_dir ? ShieldIncrement : -ShieldIncrement);
                }
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        /// <summary>
        /// Steps taken from EMS "Setup and Commissioning" panel made by sib
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Wizard.WizardStep> CreateSetupSpindleHomeSteps()
        {
            Func<bool> HomeShield = new Func<bool>( () => {
                try {
                    if( !Model.NoShieldMode) {
                        Model.ShieldAxis.Home( true);
                    }
                } catch( Exception ex) {
                    _log.Error( String.Format( "Failed to home shield while setting up spindle home position: {0}", ex.Message));
                    return false;
                }
                return true;
            });

            Func<bool> ResetSpindle = new Func<bool>( () => { 
                try {
                    Model.SpindleAxis.ResetDrive();
                    System.Threading.Thread.Sleep(1000); // wait for drive to completely reset
                } catch( Exception ex) {
                    _log.Error( String.Format( "Could not reset drive when setting up spindle home position: {0}", ex.Message));
                    return false;
                }
                return true;
            });

            Func<bool> HomeSpindle = new Func<bool>( () => {
                try {
                    Int16 homing_status = 0;
                    Int32 my_homepos = 0;
                    // store my_homepos locally, because we are going to 0 it on the controller
                    Model.SpindleAxis.GetLongVariable("my_homepos", out my_homepos);

                    // set my_homepos to 0 because this is how it is supposed to work, according to all of the TML code
                    Model.SpindleAxis.SetLongVariable( "my_homepos", 0);

                    Model.SpindleAxis.CallFunction("func_homeaxis");
                    DateTime start = DateTime.Now; // capture time at start
                    while ((DateTime.Now - start).TotalSeconds < 30.0)
                    {
                        Model.SpindleAxis.GetIntVariable("homing_status", out homing_status);
                        _log.Debug(String.Format("time elapsed = {0:0.000} seconds,  homing_status = {1}", (DateTime.Now-start).TotalSeconds, homing_status));
                        if (0 == homing_status)
                            break; // out of while loop because this is successfully done
                        else if (-98 == homing_status)
                            throw new Exception("Spindle axis is not enabled");
                        else if ((-97 == homing_status) || (-96 == homing_status))
                            throw new Exception("Spindle axis is not stopped");

                        System.Threading.Thread.Sleep(200); // sleep a little bit before we query again
                    }

                    if (0 == homing_status)
                    {
                        // Now command the axis to go to the (negative) old home position to get it close, rather than the 0 position
                        double cpos_eng = Model.SpindleAxis.ConvertCountsToEng(-my_homepos);
                        Model.SpindleAxis.MoveAbsolute(cpos_eng, 300.0, 360.0, use_trap: true);
                    }
                } catch( Exception ex) {
                    _log.Error( String.Format( "Could not zero home position or home drive when setting up spindle home position: {0}", ex.Message));
                    return false;
                }
                return true;
            });

            Func<bool> AxisOffSpindle = new Func<bool>( () => {
                try {
                    Model.SpindleAxis.Enable( false, true);
                } catch( Exception ex) {
                    _log.Error( String.Format( "Could not disable spindle axis when setting up spindle home position: {0}", ex.Message));
                    return false;
                }
                return true;
            });

            Func<bool> AxisOnSpindle = new Func<bool>( () => {
                try {
                    Model.SpindleAxis.Enable( true, true);
                } catch( Exception ex) {
                    _log.Error( String.Format( "Could not enable spindle axis when setting up spindle home position: {0}", ex.Message));
                    return false;
                }
                return true;
            });

            Func<bool> SaveHomePositionSpindle = new Func<bool>( () => {
                try {
                    Model.SpindleAxis.CallFunction( "save_homepos");
                } catch( Exception ex) {
                    _log.Error( String.Format( "Could not save bucket #1 home position when setting up spindle home position: {0}", ex.Message));
                    return false;
                }
                return true;
            });

            Func<bool> SaveBucket2OffsetSpindle = new Func<bool>(() =>
            {
                try
                {
                    Model.SpindleAxis.CallFunction("save_bucket2_offset");
                }
                catch (Exception ex)
                {
                    _log.Error(String.Format("Could not save bucket #2 offset from home when setting up spindle home position: {0}", ex.Message));
                    return false;
                }
                return true;
            });

            Func<bool> ResetAndRehomeSpindle = new Func<bool>(() =>
            {
                try {
                    Model.SpindleAxis.Home( true); // The home function resets the drive first
                } catch( Exception ex) {
                    _log.Error( String.Format( "Could not Home spindle axis when setting up spindle home position: {0}", ex.Message));
                    return false;
                }
                return true;
            });

            // Do not move spindle and simply Open shield 
            Func<bool> OpenShieldHere = new Func<bool>( () => {
                try {
                    if( !Model.NoShieldMode) {
                        Model.ShieldAxis.CallFunctionAndWaitForDone("func_open_shield", TimeSpan.FromSeconds(8));
                    }
                } catch( Exception ex) {
                    _log.Error( String.Format( "Could not open shield door after setting up spindle home position: {0}", ex.Message));
                    return false;
                }
                return true;
            });

            // Move spindle to bucket #1 and Open shield 
            Func<bool> OpenShieldAtBucket1 = new Func<bool>(() =>
            {
                try
                {
                    if( !Model.NoShieldMode) {
                        Model.OpenShield(0, false);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(String.Format("Could not open shield door at bucket #1 after setting up spindle home position: {0}", ex.Message));
                    return false;
                }
                return true;
            });

            // Move spindle to bucket #2 and Open shield 
            Func<bool> OpenShieldAtBucket2 = new Func<bool>(() =>
            {
                try
                {
                    if( !Model.NoShieldMode) {
                        Model.OpenShield(1, false);
                    } else {
                        // DKM 2012-04-17 since we can't rely on OpenShield to go to a specific bucket, we need to make the low-level calls instead
                        Model.SpindleAxis.SetIntVariable("bucket_to_goto", 2);
                        int func_done = Model.SpindleAxis.CallFunctionAndWaitForDone("goto_bucket", TimeSpan.FromSeconds(5.0), return_func_done:true);
                        if( func_done != 1) {
                            throw new Exception( "Timed out waiting for bucket move to complete");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(String.Format("Could not open shield door at bucket #2 after setting up spindle home position: {0}", ex.Message));
                    return false;
                }
                return true;
            });

            Func<bool> CloseShieldHere = new Func<bool>(() =>
            {
                try
                {
                    if( !Model.NoShieldMode) {
                        Model.CloseShield(false);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(String.Format("Could not close shield door after setting up spindle home position: {0}", ex.Message));
                    return false;
                }
                return true;
            });

            List<Wizard.WizardStep> steps = new List<Wizard.WizardStep>();
            steps.Add( new Wizard.WizardStep( "Homing shield...", HomeShield, false));
            steps.Add( new Wizard.WizardStep( "Resetting spindle...", ResetSpindle, false));
            steps.Add( new Wizard.WizardStep( "Zeroing home position and homing spindle...", HomeSpindle, false));
            steps.Add( new Wizard.WizardStep( "Opening shield... Bucket lock stomper should not be installed!", OpenShieldHere, false));
            steps.Add( new Wizard.WizardStep( "Turning off spindle servo...", AxisOffSpindle, false));
            steps.Add( new Wizard.WizardStep( "Manually line up bucket 1 with door.  Click Next when ready.", null, true));
            steps.Add( new Wizard.WizardStep( "Turning on spindle...", AxisOnSpindle, false));
            steps.Add( new Wizard.WizardStep( "Fine tune bucket position with jog controls.  Click Next when ready.", null, true));
            steps.Add( new Wizard.WizardStep( "Saving new home position to EEPROM...", SaveHomePositionSpindle, false));
            steps.Add( new Wizard.WizardStep( "Closing shield...", CloseShieldHere, false));
            steps.Add( new Wizard.WizardStep( "Resetting and re-homing Spindle...", ResetAndRehomeSpindle, false));
            steps.Add( new Wizard.WizardStep( "Opening shield... ", OpenShieldAtBucket1, false));
            steps.Add( new Wizard.WizardStep( "If the position is acceptable, click Next, otherwise start this procedure again.", null, true));
            steps.Add( new Wizard.WizardStep( "Closing shield...", CloseShieldHere, false));
            steps.Add( new Wizard.WizardStep( "Going to bucket #2 and opening shield...", OpenShieldAtBucket2, false));
            steps.Add( new Wizard.WizardStep( "Fine tune bucket #2 position with jog controls.  Click Next when ready.", null, true));
            steps.Add( new Wizard.WizardStep( "Saving new home bucket #2 offset to EEPROM...", SaveBucket2OffsetSpindle, false));
            steps.Add( new Wizard.WizardStep( "Done! Close this dialog to exit.", null, false));

            return steps;
        }

        private void ExecuteSetupSpindleHome()
        {
            // use the SimpleWizard to deal with setting up the home position
            IEnumerable<Wizard.WizardStep> steps = CreateSetupSpindleHomeSteps();
            Wizard wiz = new Wizard( "HiG Home Position Configuration", steps);
            Window owner = this.GetParentWindow();
            wiz.Top = owner.Top;
            wiz.Left = owner.Left + owner.Width;
            wiz.Show();
            wiz.Activate();
        }

        private void ExecuteImbalanceThresholdCalibration()
        {
            Window parent = System.Windows.Window.GetWindow( this);

            System.Threading.Tasks.Task task = System.Threading.Tasks.Task.Factory.StartNew( () => {
                _calibrating = true;
                HourglassWindow hg = HourglassWindow.ShowHourglassWindow( Dispatcher, parent, "Imbalance Calibration in Progress", true, 12);

                // we can display a messagebox if imbalance detection isn't supported
                if( !Model.SupportsImbalance) {
                    MessageBox.Show( "Imbalance calibration is only supported by spindle firmware v1.5 or later.");
                    return;
                }
                // since this is not intended for external consumption, I feel comfortable using this cast because
                // I know that _hig is defined as a HiG object, which implements both HigInterface and IHigModel
                var sm = new ImbalanceCalibrationStateMachine( Model as BioNex.Hig.IHigModel, false);
                sm.ExecuteCalibration();

                HourglassWindow.CloseHourglassWindow( Dispatcher, hg);       
                _calibrating = false;
                Dispatcher.Invoke( new Action( () => { ExecuteReloadFromDriveToGUI( false); } ));
            });


        }

        private void ExecuteTeachBucket1()
        {
            MessageBoxResult response = MessageBox.Show("Are you sure you want to teach the spindle position here for bucket #1?", "Confirm teaching", MessageBoxButton.YesNo);
            if (response == MessageBoxResult.No)
                return;

            try
            {
                // Assume that bucket1 was previously set to encoder position 0, so current position of bucket2 is also the offset value
                // Use TML function 'save_bucket2_offset' to compute modulus of APOS and store the appropriate value in both the bucket2_offset RAM variable and save it to EEPROM
                // We want to use the TML function here since other TML functions are used to 'goto_bucket', and if they change, the engineer will be making all of the TML code consistent.
                Model.SpindleAxis.CallFunctionAndWaitForDone("save_bucket1_my_homepos", TimeSpan.FromSeconds(1));
                ExecuteReloadFromDriveToGUI( false);
                MessageBox.Show("Bucket #1 position taught successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ExecuteTeachBucket2()
        {
            MessageBoxResult response = MessageBox.Show( "Are you sure you want to teach the spindle position here for bucket #2?", "Confirm teaching", MessageBoxButton.YesNo);
            if( response == MessageBoxResult.No)
                return;

            try {
                // Assume that bucket1 position is my_homepos, so current position of bucket2 is also the offset value
                // Use TML function 'save_bucket2_offset' to compute modulus of APOS and store the appropriate value in both the bucket2_offset RAM variable and save it to EEPROM
                // We want to use the TML function here since other TML functions are used to 'goto_bucket', and if they change, the engineer will be making all of the TML code consistent.
                Model.SpindleAxis.CallFunctionAndWaitForDone("save_bucket2_offset", TimeSpan.FromSeconds(1));
                ExecuteReloadFromDriveToGUI( false);
                MessageBox.Show( "Bucket #2 position taught successfully");
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private void ExecuteTeachShieldClosed()
        {
            MessageBoxResult response = MessageBox.Show( "Are you sure you want to teach the 'shield closed' position here?", "Confirm teaching", MessageBoxButton.YesNo);
            if( response == MessageBoxResult.No)
                return;

            try {
                int shield_pos = Model.ShieldAxis.GetPositionCounts();
                Model.WriteEepromSetting( new EepromSetting( "not used", "door_close_pos_ptr", EepromSetting.VariableTypeT.Long, shield_pos.ToString(), false));
                MessageBox.Show( "Door closed position taught successfully");
            } catch( Exception ex) {
                _log.Error( String.Format( "Could not teach shield closed position: '{0}'", ex.Message));
                MessageBox.Show( ex.Message);
            }
        }

        private void ExecuteTeachShieldOpen()
        {
            MessageBoxResult response = MessageBox.Show( "Are you sure you want to teach the 'shield open' position here?", "Confirm teaching", MessageBoxButton.YesNo);
            if( response == MessageBoxResult.No)
                return;

            try {
                int shield_pos = Model.ShieldAxis.GetPositionCounts();
                Model.WriteEepromSetting( new EepromSetting( "not used", "door_open_pos_ptr", EepromSetting.VariableTypeT.Long, shield_pos.ToString(), false));
                MessageBox.Show( "Door open position taught successfully");
            } catch( Exception ex) {
                _log.Error( String.Format( "Could not teach shield open position: '{0}'", ex.Message));
                MessageBox.Show( ex.Message);
            }
        }

        private void ExecuteReprogramSpindle()
        {
            MessageBoxResult response = MessageBox.Show("Are you sure you want to reprogram the Motor Settings and the TML code on the Spindle Controller?", "Confirm reprogram", MessageBoxButton.YesNo);
            if (response == MessageBoxResult.No)
                return;

            HourglassWindow hg = HourglassWindow.ShowHourglassWindow(_dispatcher, this.GetParentWindow(), "Reprogramming Spindle Controller", true, 15);
            try
            {
                Model.ReprogramSpindle();
            }
            catch (Exception ex)
            {
                _log.Error(String.Format("Could not reprogram Spindle controller's motor settings and TML code: {0}", ex.Message));
                MessageBox.Show(ex.Message);
            }
            finally
            {
                HourglassWindow.CloseHourglassWindow(_dispatcher, hg);
            }
        }

        private void ExecuteReprogramShield()
        {
            MessageBoxResult response = MessageBox.Show("Are you sure you want to reprogram the Motor Settings and the TML code on the Shield Controller?", "Confirm reprogram", MessageBoxButton.YesNo);
            if (response == MessageBoxResult.No)
                return;

            HourglassWindow hg = HourglassWindow.ShowHourglassWindow(_dispatcher, this.GetParentWindow(), "Reprogramming Shield Controller", true, 15);
            try
            {
                Model.ReprogramShield();
            }
            catch (Exception ex)
            {
                _log.Error(String.Format("Could not reprogram Spindle controller's motor settings and TML code: {0}", ex.Message));
                MessageBox.Show(ex.Message);
            }
            finally
            {
                HourglassWindow.CloseHourglassWindow(_dispatcher, hg);
            }
        }

        private void DataGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            try {
                _eeprom_setting_selection = (EepromSetting)(sender as DataGrid).CurrentItem;
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try {
                EepromSettings.Where( x => x.HumanReadableName == _eeprom_setting_selection.HumanReadableName).First().Value = (e.EditingElement as TextBox).Text;
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _engineering_position_updater.Start();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _engineering_position_updater.Stop();
        }
    }
}
