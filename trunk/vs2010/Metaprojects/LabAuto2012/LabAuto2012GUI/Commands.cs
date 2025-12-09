using System;
using System.ComponentModel.Composition;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Windows;
using BioNex.Shared.LibraryInterfaces;
using GalaSoft.MvvmLight.Command;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.PlateDefs;

namespace BioNex.CustomerGUIPlugins
{
    partial class LabAuto2012GUI
    {
        public RelayCommand SelectHitpickFileCommand { get; set; }
        public RelayCommand ReinventoryStaticStorageCommand { get; set; }
        public RelayCommand HomeAllCommand { get; set; }
        public RelayCommand DisplayStaticStorageCommand { get; set; }
        public RelayCommand EnqueueHitpickCommand { get; set; }

        // DKM 2011-06-08 added pause / resume / abort here
        public RelayCommand CustomerGuiPauseCommand { get; set; }
        public RelayCommand CustomerGuiResumeCommand { get; set; }
        public RelayCommand CustomerGuiAbortCommand { get; set; }
        public RelayCommand CustomerGuiShowTipBoxManager { get; set; }

        // DKM 2011-10-13 prevent user from clicking Pause and Resume multiple times
        private bool _already_clicked_pause { get; set; }
        private bool _already_clicked_resume { get; set; }

        private string _reinventory_static_storage_tooltip;
        public string ReinventoryStaticStorageToolTip
        {
            get { return _reinventory_static_storage_tooltip; }
            set {
                _reinventory_static_storage_tooltip = value;
                OnPropertyChanged( "ReinventoryStaticStorageToolTip");
            }
        }

        private void InitializeCommands()
        {
            HomeAllCommand = new RelayCommand(ExecuteHomeAllCommand, CanExecuteHomeAllCommand);
            SelectHitpickFileCommand = new RelayCommand( ExecuteSelectHitpickFile);
            ReinventoryStaticStorageCommand = new RelayCommand( ExecuteReinventoryStaticStorage, CanExecuteReinventoryStaticStorage);
            DisplayStaticStorageCommand = new RelayCommand( ExecuteDisplayStaticStorage);
            EnqueueHitpickCommand = new RelayCommand( ExecuteEnqueueHitpick, CanExecuteEnqueueHitpick);

            CustomerGuiPauseCommand = new RelayCommand( ExecutePauseCommand, CanExecutePauseCommand);
            CustomerGuiResumeCommand = new RelayCommand( ExecuteResumeCommand, CanExecuteResumeCommand); // this is for Go Live
            CustomerGuiAbortCommand = new RelayCommand( ExecuteAbortCommand, CanExecuteAbortCommand);
            CustomerGuiShowTipBoxManager = new RelayCommand( ExecuteShowTipBoxManager, CanExecuteShowTipBoxManager);
        }

        private void ExecuteEnqueueHitpick()
        {
            BioNex.Shared.HitpickXMLReader.Reader reader = new Shared.HitpickXMLReader.Reader( _labware_database);
            TransferOverview to = reader.Read( SelectedHitpickFile, null);
            _synapsis_view_model.EnqueueTransferOverview( to, "not needed?", false);
            _log.InfoFormat( "Added hitpick file '{0}' to work queue", SelectedHitpickFile);
        }

        private bool CanExecuteEnqueueHitpick()
        {
            return SelectedHitpickFile != null && SelectedHitpickFile != "";
        }

        private void ExecuteReinventoryStaticStorage()
        {
            Thread reinventory_thread = new Thread( () => {
                // returns false if we didn't transition for some reason
                if( !_engine.RequestReinventory())
                    return;
                var hive = _robot as HivePrototypePlugin.HivePlugin;
                if( hive == null) {
                    _log.Info( String.Format( "Cannot reinventory static storage with robot '{0}'", _robot));
                    return;
                }
                hive.Reinventory( true);
            });
            reinventory_thread.Name = "Hive reinventory from LabAuto GUI";
            reinventory_thread.IsBackground = true;
            reinventory_thread.Start();
        }

        private void ExecuteSelectHitpickFile()
        {
            string selected_hitpick_file;
            ILimsTextConverter selected_converter;
            bool user_plugin_selected;
            PluginUtilities.SelectHitpickFile( LimsTextConverters, out selected_hitpick_file, out selected_converter, out user_plugin_selected);
            _log.Info( "user selected hitpick file '" + selected_hitpick_file + "'");
            SelectedHitpickFile = selected_hitpick_file;
            SelectedLimsTextConverter = selected_converter;
            UserPluginSelected = user_plugin_selected;
        }

        private bool CanExecuteReinventoryStaticStorage()
        {
            if( _engine == null)
                return false;

            if (_engine.IsInState(BehaviorEngine.State.NotHomed)) {
                ReinventoryStaticStorageToolTip = "Must home first";
                return false;
            }

            if( Reinventorying) {
                ReinventoryStaticStorageToolTip = "Currently reinventorying storage";
                return false;
            } else if( Homing) {
                ReinventoryStaticStorageToolTip = "Currently homing";
                return false;
            } else {
                ReinventoryStaticStorageToolTip = "Reinventory static storage";
                return true;
            }
        }

        private void ExecuteDisplayStaticStorage()
        {
            var hive = _robot as PlateStorageInterface;
            hive.DisplayInventoryDialog();
        }
        
        [Import]
        private Lazy<ICustomSynapsisQuery> _synapsisQuery { get; set; }

        private void ExecuteHomeAllCommand()
        {           
            // fire this in a worker thread so that pressing the home button doesn't block the main thread while we wait for the stars to align
            new Thread(() =>
            {
                // enter NotHomed state - lock it down so nothing else can happen, then launch the homing op
                _engine.FireHome();

                bool skip = false;
                if (_synapsisQuery.Value.AllDevicesHomed)
                {
                    _dispatcher.Invoke( new Action( () => {
                        var answer = MessageBox.Show( Application.Current.MainWindow, "All devices are already homed.  Are you sure you want to rehome them?", "Rehome all devices?", MessageBoxButton.YesNo);
                        if (answer == MessageBoxResult.No)
                        {
                            _log.Info("user does not want to rehome all devices");
                            _engine.FireHomeComplete();
                            skip = true;
                            return;
                        }
                        _log.Info("user requested to rehome all devices");
                    }));
                }

                if( skip)
                    return;

                // false return means the user aborted the request to home
                if (!_synapsisQuery.Value.HomeAllDevices())
                {
                    // DKM 2011-06-10 this is the most likely cause of the home failure, since HomeAllDevices returns false
                    //                if there is a problem BEFORE the home state machines get started, and the only thing
                    //                does right now is call SafeToMove.
                    MessageBox.Show("Could not home.  Please check that the doors are closed, reset the interlocks, and try again.");
                    _engine.FireHomeFailed();
                    return;
                }

                /*
                // there is currently no way to wait for home to complete, and home is non blocking.  Sweet.  All we can do is say that homing is complete at this point.
                // but this is almost guaranteed to cause a problem.
                _engine.FireHomeComplete();
                 */

                // if we were already homed, we need to wait until we're not homed or the waiting code will fall through
                while( !_synapsisQuery.Value.AllDevicesHomed)
                    Thread.Sleep( 100);


                double timeout_sec = 60;
                DateTime start = DateTime.Now;
                while( !_synapsisQuery.Value.AllDevicesHomed && (DateTime.Now - start).TotalSeconds < timeout_sec) {
                    Thread.Sleep( 100);
                }
                if( (DateTime.Now - start).TotalSeconds >= timeout_sec) {
                    _dispatcher.Invoke( new Action( () => { MessageBox.Show( Application.Current.MainWindow, "Homing timed out.  Please try again."); } ));
                    _engine.FireHomeFailed();
                } else {
                    _engine.FireHomeComplete();
                }
            }).Start();
        }

        private bool CanExecuteHomeAllCommand()
        {
            if( _engine == null)
                return false;

            // tell users that we're already homed
            if( _engine.NotHomed) {
                HomeAllCommandToolTip = "Home all devices";
                return true;
            }
            
            if( _engine.Paused) {
                HomeAllCommandToolTip = "After initial homing, you must click Start before homing again";
                return false;
            } else if( _engine.Homing) {
                HomeAllCommandToolTip = "Currently homing all devices";
                return false;
            } else {
                HomeAllCommandToolTip = "Home all devices";
                return true;
            }
        }

        private void CheckInitialHomeState()
        {
            if( _synapsisQuery.Value.AllDevicesHomed)
                _engine.FireHomedAtInit();
        }

        private void ExecutePauseCommand()
        {
            // DKM 2011-05-27 do this to prevent potential main threadlock when pausing and doing other actions
            new Action( () => { 
                if( Thread.CurrentThread.Name == null)
                    Thread.CurrentThread.Name = "GUI pause button event thread";

                _already_clicked_pause = true;
                _engine.FirePause();

            } ).BeginInvoke( PauseRequestComplete, null);
        }

        private bool CanExecutePauseCommand()
        {
            if( _engine == null)
                return false;
            return !_already_clicked_pause && !_engine.Paused && !_engine.Homing && !_engine.Reinventorying && !_engine.PausedWhileMovingPlates;
        }

        /// <summary>
        /// This is the Go Live button handler
        /// </summary>
        private void ExecuteResumeCommand()
        {
            // DKM 2011-05-27 do this to prevent potential main threadlock when pausing and doing other actions
            new Action( () => { 
                if( Thread.CurrentThread.Name == null)
                    Thread.CurrentThread.Name = "GUI resume button event thread";

                _already_clicked_resume = true;
                _engine.FireResume();
                // force the Go Live text to change to Resume
                OnPropertyChanged( "ResumeButtonText");
            } ).BeginInvoke( ResumeRequestComplete, null);
        }

        private bool CanExecuteResumeCommand()
        {
            if( _engine == null)
                return false;
            return !_already_clicked_resume && (_engine.IsInState( BehaviorEngine.State.Paused) || _engine.IsInState(BehaviorEngine.State.PausedWhileMovingPlates));
        }

        private void ExecuteAbortCommand()
        {
        }

        private bool CanExecuteAbortCommand()
        {
            return true;
        }

        [ Import]
        ITipBoxManager TipBoxManager { get; set; }

        private void ExecuteShowTipBoxManager()
        {
            ( TipBoxManager as ISystemSetupEditor).ShowTool();
        }

        private bool CanExecuteShowTipBoxManager()
        {
            return true;
        }

        private void PauseRequestComplete( IAsyncResult iar)
        {
            AsyncResult ar = (AsyncResult)iar;
            Action callback = (Action)ar.AsyncDelegate;
            _already_clicked_pause = false;
            callback.EndInvoke(iar);
        }

        private void ResumeRequestComplete( IAsyncResult iar)
        {
            AsyncResult ar = (AsyncResult)iar;
            Action callback = (Action)ar.AsyncDelegate;
            _already_clicked_resume = false;
            callback.EndInvoke(iar);
        }
    }
}
