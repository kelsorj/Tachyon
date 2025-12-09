using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using BioNex.BumblebeePlugin.Hardware;
using BioNex.BumblebeePlugin.Model;
using BioNex.Shared.IError;
using GalaSoft.MvvmLight;

namespace BioNex.BumblebeePlugin.ViewModel
{
    public class Increments
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double W { get; set; }
        public double R { get; set; }
        public double A { get; set; }
        public double B { get; set; }

        public Increments()
        {
            X = Y = Z = W = R = A = B = 1.0;
        }

        public double this[ string axis_name]
        {
            get{
                switch( axis_name){
                    case "X":   return X;
                    case "Y":   return Y;
                    case "Z":   return Z;
                    case "W":   return W;
                    case "R":   return R;
                    case "A":   return A;
                    case "B":   return B;
                    default:    return 0.0;
                }
            }
        }
    }

    public partial class MainViewModel : ViewModelBase, IError
    {
        private MainModel Model { get; set; }
        private System.Windows.Threading.Dispatcher Dispatcher { get; set; }

        // increment values for all axes
        public Increments JogIncrements { get; set; }

        private DispatcherTimer Timer { get; set; }

        // stage and channel IDs
        // these are the ones that are selected
        private byte _selected_channel_id;
        public byte SelectedChannelID
        {
            get { return _selected_channel_id; }
            set {
                _selected_channel_id = value;
                UpdateTeachpointPositionsForTooltips();
                //Log.InfoFormat( "Selected channel {0}", _selected_channel);
            }
        }

        private byte _selected_stage_id;
        public byte SelectedStageID
        {
            get { return _selected_stage_id; }
            set {
                _selected_stage_id = value;
                UpdateTeachpointPositionsForTooltips();
                UpdateShuttleRowAndColumnLists();
                RaisePropertyChanged( "StageTypeString");
                RaisePropertyChanged( "HideIfTipShuttle");
                RaisePropertyChanged( "HideIfNotTipShuttle");
                RaisePropertyChanged( "HasRAxisGridLength12");
                RaisePropertyChanged( "HasABAxisGridLength06");
                RaisePropertyChanged( "HasABAxisGridLength12");
            }
        }

        public bool SelectedStageIsTipShuttle { get { return Model.Hardware.GetStage( _selected_stage_id) is TipShuttle; }}
        public string StageTypeString { get { return SelectedStageIsTipShuttle ? "Tip Shuttle" : "Stage"; }}
        public Visibility HideIfTipShuttle { get { return SelectedStageIsTipShuttle ? Visibility.Collapsed : Visibility.Visible; }}
        public Visibility HideIfNotTipShuttle { get { return SelectedStageIsTipShuttle ? Visibility.Visible : Visibility.Collapsed; }}
        public GridLength HasRAxisGridLength12 { get { return SelectedStageIsTipShuttle ? new GridLength( 0) : new GridLength( 12, GridUnitType.Star); }}
        public GridLength HasABAxisGridLength06 { get { return !SelectedStageIsTipShuttle ? new GridLength( 0) : new GridLength( 6, GridUnitType.Star); }}
        public GridLength HasABAxisGridLength12 { get { return !SelectedStageIsTipShuttle ? new GridLength( 0) : new GridLength( 12, GridUnitType.Star); }}
        // public Visibility CustomerGUITabVisibility { get { return _customer_gui_tab_visibility; } set { _customer_gui_tab_visibility = value; RaisePropertyChanged("CustomerGUITabVisibility"); }}

        public Positions SelectedLRTeachpoint { get; private set; }
        public Positions SelectedULTeachpoint { get; private set; }
        public Positions SelectedWashTeachpoint { get; private set; }

        public string AutoTeachEverythingToolTip { get; private set; }

        // these allow the droplists to show the available stages and channels
        public ObservableCollection<byte> ChannelList { get; private set; }
        public ObservableCollection<byte> StageList { get; private set; }

        // droplists to show shuttle rows and columns.
        public ObservableCollection< int> ShuttleRowList { get; private set; }
        public ObservableCollection< int> ShuttleColumnList { get; private set; }

        // position readout on jog tab, and home status
        private Positions _current_channel_and_stage_positions;
        public Positions CurrentChannelAndStagePositions
        {
            get { return _current_channel_and_stage_positions; }
            set {
                _current_channel_and_stage_positions = value;
                RaisePropertyChanged( "CurrentChannelAndStagePositions");
            }
        }
        private AxisBoolStatus _axis_home_status;
        public AxisBoolStatus AxisHomeStatus
        {
            get { return _axis_home_status; }
            set {
                _axis_home_status = value;
                RaisePropertyChanged( "AxisHomeStatus");
            }
        }
        private AxisBoolStatus _servo_on_status;
        public AxisBoolStatus ServoOnStatus
        {
            get { return _servo_on_status; }
            set {
                _servo_on_status = value;
                RaisePropertyChanged( "ServoOnStatus");
            }
        }

        // status
        private bool _connected;
        public bool Connected
        {
            get { return _connected; }
            set {
                _connected = value;
                RaisePropertyChanged( "Connected");
            }
        }

        private Visibility maintenance_tab_visibility_;
        public Visibility MaintenanceTabVisibility
        {
            get { return maintenance_tab_visibility_; }
            set {
                maintenance_tab_visibility_ = value;
                RaisePropertyChanged( "MaintenanceTabVisibility");
            }
        }

        // Tooltips
        private string TeachRobotPickupTooltip { get; set; }
        
        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public MainViewModel( Model.MainModel model, System.Windows.Threading.Dispatcher dispatcher)
        {
            Model = model;
            Dispatcher = dispatcher;

            MaintenanceTabVisibility = Visibility.Hidden;
            JogIncrements = new Increments();
            ChannelList = new ObservableCollection<byte>();
            StageList = new ObservableCollection<byte>();
            _selected_channel_id = 1;
            _selected_stage_id = 1;
            TipName = "A1";
            ShuttleRowList = new ObservableCollection<int>();
            ShuttleColumnList = new ObservableCollection<int>();

            InitializeRelayCommands();

            SelectedCyclerAxes = new ObservableCollection< string>{ "X", "Z", "W", "Y", "R" };

            Timer = new DispatcherTimer();
            Timer.Tick += new EventHandler(Timer_Tick);
            Timer.Interval = new TimeSpan( 0, 0, 0, 0, 100);
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        private void Timer_Tick(object sender, EventArgs event_args)
        {
            // note that we're getting data out of an array, so subtract 1 from the channel and stage IDs
            Connected = Model.Connected;

            Channel channel = Model.Hardware.GetChannel( SelectedChannelID);
            Stage stage = Model.Hardware.GetStage( SelectedStageID);

            AxisBoolStatus h = new AxisBoolStatus();
            AxisBoolStatus e = new AxisBoolStatus();
            Positions p = new Positions();

            h.X = channel.AxisStatuses[ "X"].IsHomed;
            e.X = channel.AxisStatuses[ "X"].IsEnabled;
            p.X = channel.AxisStatuses[ "X"].PositionMM;
            h.Z = channel.AxisStatuses[ "Z"].IsHomed;
            e.Z = channel.AxisStatuses[ "Z"].IsEnabled;
            p.Z = channel.AxisStatuses[ "Z"].PositionMM;
            h.W = channel.AxisStatuses[ "W"].IsHomed;
            e.W = channel.AxisStatuses[ "W"].IsEnabled;
            p.W = channel.AxisStatuses[ "W"].PositionMM;

            h.Y = stage.AxisStatuses[ "Y"].IsHomed;
            e.Y = stage.AxisStatuses[ "Y"].IsEnabled;
            p.Y = stage.AxisStatuses[ "Y"].PositionMM;
            h.R = stage.AxisStatuses[ "R"].IsHomed;
            e.R = stage.AxisStatuses[ "R"].IsEnabled;
            p.R = stage.AxisStatuses[ "R"].PositionMM;
            if( stage is TipShuttle){
                h.A = stage.AxisStatuses[ "A"].IsHomed;
                e.A = stage.AxisStatuses[ "A"].IsEnabled;
                p.A = stage.AxisStatuses[ "A"].PositionMM;
                h.B = stage.AxisStatuses[ "B"].IsHomed;
                e.B = stage.AxisStatuses[ "B"].IsEnabled;
                p.B = stage.AxisStatuses[ "B"].PositionMM;
            }

            AxisHomeStatus = h;
            ServoOnStatus = e;
            CurrentChannelAndStagePositions = p;
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Allows ViewModel to be initialized after it's constructed when the GUI (diagnostics) is displayed
        /// </summary>
        public void Initialize()
        {
            ChannelList.Clear();
            foreach( Channel channel in Model.Hardware.AvailableChannels.OrderBy( c => c.ID)){
                ChannelList.Add( channel.ID);
            }
            StageList.Clear();
            foreach( Stage stage in Model.Hardware.Stages.OrderBy( s => s.ID)){
                StageList.Add( stage.ID);
            }
        }
        // ----------------------------------------------------------------------
        private void UpdateTeachpointPositionsForTooltips()
        {
            SelectedLRTeachpoint = Model.GetLRTeachpoint( SelectedChannelID, SelectedStageID);
            SelectedULTeachpoint = Model.GetULTeachpoint( SelectedChannelID, SelectedStageID);
            SelectedWashTeachpoint = Model.GetWashTeachpoint( SelectedChannelID);
        }
        // ----------------------------------------------------------------------
        private void UpdateShuttleRowAndColumnLists()
        {
            TipShuttle tip_shuttle = Model.Hardware.GetTipShuttle( SelectedStageID);
            if( tip_shuttle != null){
                ShuttleRowList.Clear();
                foreach( int i in Enumerable.Range( 1, tip_shuttle.TipCarrier.NumRows)){
                    ShuttleRowList.Add( i);
                }
                ShuttleColumnList.Clear();
                foreach( int i in Enumerable.Range( 1, tip_shuttle.TipCarrier.NumCols)){
                    ShuttleColumnList.Add( i);
                }

            }
        }
        // ----------------------------------------------------------------------
        public void StartBackgroundUpdates()
        {
            Timer.Start();
        }
        // ----------------------------------------------------------------------
        public void StopBackgroundUpdates()
        {
            Timer.Stop();
        }
        // ----------------------------------------------------------------------
        #region IError Members
        // ----------------------------------------------------------------------
        private delegate void AddErrorDelegate( ErrorData error);
        // ----------------------------------------------------------------------
        public event ErrorEventHandler ErrorEvent;
        // ----------------------------------------------------------------------
        public IEnumerable< ErrorData> PendingErrors { get { return new List< ErrorData>(); }}
        // ----------------------------------------------------------------------
        public bool WaitForUserToHandleError { get { return true; }}
        // ----------------------------------------------------------------------
        public void AddError( ErrorData error)
        {
            // when we add the error, we need to use the Dispatcher to do it
            try{
                Dispatcher.BeginInvoke( new AddErrorDelegate( AddErrorSTA), error);
            } catch( Exception){
            }
        }
        // ----------------------------------------------------------------------
        public void Clear() {}
        // ----------------------------------------------------------------------
        private void AddErrorSTA( ErrorData error)
        {
            if( Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "Add Bumblebee GUI error";

            // fire the ErrorEvent before we show the dialog, since the dialog is going to block
            if (ErrorEvent != null)
                ErrorEvent(this, error);

            BioNex.Shared.ErrorHandling.ErrorDialog dlg = new BioNex.Shared.ErrorHandling.ErrorDialog( error);
            // #179 need to use modeless dialogs, because modal ones prevent us from being able to reset the interlocks via software
            dlg.Show();
        }
        // ----------------------------------------------------------------------
        #endregion
        // ----------------------------------------------------------------------
        // stuff for cycler down here -- testing to see if the datacontext is inherited, which I believe it is
        public ObservableCollection<string> SelectedCyclerAxes { get; set; }
    }
}
