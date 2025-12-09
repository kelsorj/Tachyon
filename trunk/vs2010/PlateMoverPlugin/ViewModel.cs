using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using GalaSoft.MvvmLight.Command;
using System.Windows;
using BioNex.Shared.Utils;
using System.Runtime.Remoting.Messaging;

namespace BioNex.PlateMover
{
    public class ViewModel : INotifyPropertyChanged
    {
        private double _stage_angle;
        public double StageAngle
        {
            get { return _stage_angle; }
            set {
                _stage_angle = value;
                OnPropertyChanged( "StageAngle");
            }
        }

        private double _stage_position;
        public double StagePosition
        {
            get { return _stage_position; }
            set {
                _stage_position = value;
                OnPropertyChanged( "StagePosition");
            }
        }

        private double _r_increment;
        public double RIncrement
        {
            get { return _r_increment; }
            set {
                _r_increment = value;
                OnPropertyChanged( "RIncrement");
            }
        }

        private double _x_increment;
        public double XIncrement
        {
            get { return _x_increment; }
            set {
                _x_increment = value;
                OnPropertyChanged( "XIncrement");
            }
        }

        private Visibility _r_visibility;
        public Visibility RVisibility
        {
            get { return _r_visibility; }
            set {
                _r_visibility = value;
                OnPropertyChanged( "RVisibility");
            }
        }

        private Visibility _y_visibility;
        public Visibility YVisibility
        {
            get { return _y_visibility; }
            set {
                _y_visibility = value;
                OnPropertyChanged( "YVisibility");
            }
        }

        private double _track_length;
        public double TrackLength 
        {
            get { return _track_length; }
            set {
                _track_length = value;
                OnPropertyChanged( "TrackLength");
            }
        }

        private bool _connected; 
        public bool Connected
        {
            get { return _connected; }
            set {
                _connected = value;
                OnPropertyChanged( "Connected");
            }
        }

        // command handlers
        // menu
        public RelayCommand<bool> ConnectCommand { get; set; }
        public RelayCommand HomeAllAxesCommand { get; set; }
        public RelayCommand HomeYCommand { get; set; }
        public RelayCommand HomeRCommand { get; set; }
        public RelayCommand ServosOnCommand { get; set; }
        public RelayCommand ServosOffCommand { get; set; }
        public RelayCommand ResetInterlocksCommand { get; set; }
        public RelayCommand StopAllCommand { get; set; }
        // diags
        public RelayCommand TeachHiveLandscapePickupCommand { get; set; }
        public RelayCommand TeachHivePortraitPickupCommand { get; set; }
        public RelayCommand TeachExternalPickupCommand { get; set; }
        public RelayCommand MoveToHiveLandscapePickupCommand { get; set; }
        public RelayCommand MoveToHivePortraitPickupCommand { get; set; }
        public RelayCommand MoveToExternalPickupCommand { get; set; }
        public RelayCommand JogNegativeCommand { get; set; }
        public RelayCommand RotateCCWCommand { get; set; }
        public RelayCommand RotateCWCommand { get; set; }
        public RelayCommand JogPositiveCommand { get; set; }

        private Model Model { get; set; }

        private ThreadedUpdates Updater { get; set; }

        /// <summary>
        /// Allows the GUI to be grayed out when a button is clicked, and then
        /// re-enabled when the operation is complete or errors out
        /// </summary>
        private bool _gui_busy;

        public ViewModel( Model model)
        {
            InitializeCommands();
            Model = model;
            Updater = new ThreadedUpdates( "PlateMover position updater", PositionReaderCallback);
            RIncrement = 0.01;
            XIncrement = 1;
            RVisibility = Visibility.Visible;
            YVisibility = Visibility.Visible;
        }

        public void InitializeCommands()
        {
            ConnectCommand = new RelayCommand<bool>( ExecuteConnectCommand, (arg) => { return !_gui_busy; });
            HomeAllAxesCommand = new RelayCommand( ExecuteHomeAllAxesCommand, CanExecuteHomeAllAxesCommand);
            HomeYCommand = new RelayCommand( ExecuteHomeYCommand, CanExecuteHomeYCommand);
            HomeRCommand = new RelayCommand( ExecuteHomeRCommand, CanExecuteHomeRCommand);
            ServosOnCommand = new RelayCommand( ExecuteServosOnCommand, () => { return !_gui_busy; });
            ServosOffCommand = new RelayCommand( ExecuteServosOffCommand, () => { return !_gui_busy; });
            ResetInterlocksCommand = new RelayCommand( ExecuteResetInterlocksCommand);
            StopAllCommand = new RelayCommand( ExecuteStopAllCommand);

            TeachHiveLandscapePickupCommand = new RelayCommand( () => ExecuteTeachHivePickup( 0), CanExecuteTeachHivePickup);
            TeachHivePortraitPickupCommand = new RelayCommand( () => ExecuteTeachHivePickup( 1), CanExecuteTeachHivePickup);
            TeachExternalPickupCommand = new RelayCommand( ExecuteTeachExternalPickup, CanExecuteTeachExternalPickup);
            MoveToHiveLandscapePickupCommand = new RelayCommand( () => ExecuteMoveToHivePickup( 0), CanExecuteMoveToHivePickup);
            MoveToHivePortraitPickupCommand = new RelayCommand( () => ExecuteMoveToHivePickup( 1), CanExecuteMoveToHivePickup);
            MoveToExternalPickupCommand = new RelayCommand( ExecuteMoveToExternalPickup, CanExecuteMoveToExternalPickup);
            JogNegativeCommand = new RelayCommand( ExecuteJogNegative, CanExecuteJogNegative);
            RotateCCWCommand = new RelayCommand( ExecuteRotateCCW, CanExecuteRotateCCW);
            RotateCWCommand = new RelayCommand( ExecuteRotateCW, CanExecuteRotateCW);
            JogPositiveCommand = new RelayCommand( ExecuteJogPositive, CanExecuteJogPositive);
        }

        public bool Homed { get { return Model.Homed; } }

        public void StartPositionUpdateThread( bool start)
        {
            if( start && !Updater.Running)
                Updater.Start();
            else if( !start && Updater.Running)
                Updater.Stop();
        }

        private void PositionReaderCallback()
        {
            StageAngle = Model.R;
            StagePosition = Model.Y;
            Connected = Model.Connected;
        }

        private void ExecuteHomeYCommand()
        {
            Model.HomeY();
        }

        private bool CanExecuteHomeYCommand()
        {
            return !_gui_busy && Model.Connected;
        }

        private void ExecuteHomeRCommand()
        {
            Model.HomeR();
        }

        private bool CanExecuteHomeRCommand()
        {
            return !_gui_busy && Model.Connected;
        }

        internal void ExecuteHomeAllAxesCommand()
        {
            Model.HomeAxes( true);
        }

        private bool CanExecuteHomeAllAxesCommand()
        {
            return !_gui_busy && Model.Connected;
        }

        internal void ExecuteConnectCommand( bool connect)
        {
            try {
                _gui_busy = true;
                Connect( connect);
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            } finally {
                _gui_busy = false;
            }
        }

        internal void Connect( bool connect)
        {
            Model.Connect( connect);
            if( connect) {
                // after we've connected to the model, we should know what the axis configuration looks like.
                // set the R and YVisibility accordingly
                RVisibility = Model.Stage.HasR ? Visibility.Visible : Visibility.Collapsed;
                YVisibility = Model.Stage.HasY ? Visibility.Visible : Visibility.Collapsed;
                TrackLength = Model.TrackLength;
            }
            StartPositionUpdateThread( connect);
            // I had to add this because in the PlateMover plugin, the update thread only runs as long as
            // it is connected, so when you disconnect, the Connected property is no longer updated.
            Connected = Model.Connected;
        }

        private void ExecuteServosOnCommand()
        {
            try {
                _gui_busy = true;
                Model.ServoOn();
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            } finally {
                _gui_busy = false;
            }
        }

        private void ExecuteServosOffCommand()
        {
            try {
                _gui_busy = true;
                Model.ServoOff();
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            } finally {
                _gui_busy = false;
            }
        }

        private void ExecuteResetInterlocksCommand()
        {
        }

        private void ExecuteStopAllCommand()
        {
            try {
                _gui_busy = true;
                Model.StopAll();
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
                _gui_busy = false;
            }
        }

        private void ExecuteTeachHivePickup( int orientation)
        {
            MessageBoxResult result = MessageBox.Show( "Are you sure you want to teach the Hive teachpoint here?", "Confirm teachpoint", MessageBoxButton.YesNo);
            if( result == MessageBoxResult.No)
                return;

            try {
                Model.SaveHiveTeachpoint( orientation);
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private bool CanExecuteTeachHivePickup()
        {
            return Model.Connected && Model.Homed && !_gui_busy;
        }

        private void ExecuteTeachExternalPickup()
        {
            MessageBoxResult result = MessageBox.Show( "Are you sure you want to teach the External teachpoint here?", "Confirm teachpoint", MessageBoxButton.YesNo);
            if( result == MessageBoxResult.No)
                return;
            try {
                Model.SaveExternalTeachpoint();
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private bool CanExecuteTeachExternalPickup()
        {
            return Model.Connected && Model.Homed && !_gui_busy;
        }

        private void ExecuteMoveToHivePickup( int orientation)
        {
            _gui_busy = true;
            new Action( () => Model.MoveToHiveTeachpoint( orientation)).BeginInvoke( MoveComplete, null);
        }

        private bool CanExecuteMoveToHivePickup()
        {
            return Model.Connected && Model.Homed && !_gui_busy;
        }

        private void ExecuteMoveToExternalPickup()
        {
            _gui_busy = true;
            new Action( Model.MoveToExternalTeachpoint).BeginInvoke( MoveComplete, null);
        }

        private void MoveComplete( IAsyncResult iar)
        {
            try {
                AsyncResult ar = (AsyncResult)iar;
                Action callback = (Action)ar.AsyncDelegate;
                callback.EndInvoke( iar);
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);  
            } finally {
                _gui_busy = false;
            }
        }

        private bool CanExecuteMoveToExternalPickup()
        {
            return Model.Connected && Model.Homed && !_gui_busy;
        }

        private void ExecuteJogNegative()
        {
            Model.Stage.JogNegative( XIncrement);
        }

        private bool CanExecuteJogNegative()
        {
            return Model.Connected && Model.Homed && !_gui_busy;
        }

        private void ExecuteRotateCW()
        {
            Model.Stage.RotateCW( RIncrement);
        }

        private bool CanExecuteRotateCW()
        {
            return Model.Connected && Model.Homed && !_gui_busy;
        }

        private void ExecuteRotateCCW()
        {
            Model.Stage.RotateCCW( RIncrement);
        }

        private bool CanExecuteRotateCCW()
        {
            return Model.Connected && Model.Homed && !_gui_busy;
        }

        private void ExecuteJogPositive()
        {
            Model.Stage.JogPositive( XIncrement);
        }

        private bool CanExecuteJogPositive()
        {
            return Model.Connected && Model.Homed && !_gui_busy;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for
        /// a given property.
        /// </summary>
        /// <param name="propertyName">The name of the changed property.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
