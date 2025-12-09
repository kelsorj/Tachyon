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
using BioNex.Shared.LibraryInterfaces;
using System.ComponentModel.Composition;
using GalaSoft.MvvmLight.Command;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using BioNex.Shared.Utils;
using System.IO;
using System.Xml.Serialization;
using log4net;
using System.Threading;
using System.Windows.Threading;

namespace BioNex.PioneerGUI
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    [Export(typeof(ICustomerGUI))]
    public partial class Checklist : UserControl, ICustomerGUI, INotifyPropertyChanged
    {
        [Import]
        private Lazy<ICustomSynapsisQuery> SynapsisQuery { get; set; }
        [Import("MainDispatcher")]
        private Dispatcher _dispatcher;
        private HourglassWindow _hg;

        public Model _model { get; private set; }
        public event EventHandler ProtocolComplete;
        public event EventHandler AbortableTaskStarted;
        public event EventHandler AbortableTaskComplete;

        // logging to _log goes into all of the main log's appenders
        private static readonly ILog _log = LogManager.GetLogger( "PioneerGUI");
        private AutoResetEvent CloseHourglassWindowEvent = new AutoResetEvent( false);

        private bool _already_homing;

        [ImportingConstructor]
        public Checklist( [Import("PioneerModel")] Model model)
        {
            InitializeComponent();
            _model = model;
            this.DataContext =  this;
            InitializeCommands();
            _model.ModelAbortableProcessStarted += AbortableTaskStarted;
            _model.ModelAbortableProcessComplete += AbortableTaskComplete;
        }

        ~Checklist()
        {
            _model.ProtocolComplete -= _model_ProtocolComplete;
        }

        private void InitializeCommands()
        {
            SelectHitpickCommand = new RelayCommand( SelectHitpickFile, CanExecuteSelectHitpickCommand);
            HomeAllDevicesCommand = new RelayCommand( HomeAllDevices, CanExecuteHomeAllDevicesCommand );
            ReinventoryTipboxesCommand = new RelayCommand( ReinventoryTipboxes, CanExecuteReinventoryTipboxesCommand);
            ReinventoryPlatesCommand = new RelayCommand( ReinventoryPlates, CanExecuteReinventoryPlatesCommand);
            ReinventoryAllStorageCommand = new RelayCommand( ReinventoryAllStorage, CanExecuteReinventoryAllStorageCommand);
            DisplayTipboxesCommand = new RelayCommand( _model.DisplayTipboxes, _model.CanExecuteDisplayTipboxes);
            DisplayPlatesCommand = new RelayCommand( _model.DisplayPlates, _model.CanExecuteDisplayPlates);
        }

        public RelayCommand SelectHitpickCommand { get; set; }
        public RelayCommand HomeAllDevicesCommand { get; set; }
        public RelayCommand ReinventoryTipboxesCommand { get; set; }
        public RelayCommand ReinventoryPlatesCommand { get; set; }
        public RelayCommand ReinventoryAllStorageCommand { get; set; }
        public RelayCommand DisplayTipboxesCommand { get; set; }
        public RelayCommand DisplayPlatesCommand { get; set; }

        private ILimsTextConverter SelectedLimsTextConverter { get; set; }

        private string _selected_hitpick_file;
        /// <summary>
        /// The name of the hitpick file the customer wants to run.  By setting this
        /// property, the notification will automatically make the GUI update, and
        /// it will set the HitpickFileSelected property, which then makes the GUI
        /// update the state of the associated checkbox.
        /// </summary>
        public string SelectedHitpickFile
        {
            get { return _selected_hitpick_file; }
            set {
                _selected_hitpick_file = value;
                OnPropertyChanged( "SelectedHitpickFile");
                //! \todo check to see if the hitpick file is valid
                HitpickFileSelected = true;
            }
        }

        private bool _hitpick_file_selected;
        /// <summary>
        /// Whether or not a valid hitpick file was selected by the user
        /// </summary>
        public bool HitpickFileSelected
        {
            get { return _hitpick_file_selected; }
            set {
                _hitpick_file_selected = value;
                OnPropertyChanged( "HitpickFileSelected");
            }
        }

        private string reinventory_tipboxes_tooltip_;
        public string ReinventoryTipboxesToolTip
        {
            get { return reinventory_tipboxes_tooltip_; }
            set {
                reinventory_tipboxes_tooltip_ = value;
                OnPropertyChanged( "ReinventoryTipboxesToolTip");
            }
        }

        private string _reinventory_all_storage_tooltip;
        public string ReinventoryAllStorageToolTip
        {
            get { return _reinventory_all_storage_tooltip; }
            set {
                _reinventory_all_storage_tooltip = value;
                OnPropertyChanged( "ReinventoryAllStorageToolTip");
            }
        }

        private string reinventory_plates_tooltip_;
        public string ReinventoryPlatesToolTip
        {
            get { return reinventory_plates_tooltip_; }
            set {
                reinventory_plates_tooltip_ = value;
                OnPropertyChanged( "ReinventoryPlatesToolTip");
            }
        }

        public string DisplayTipboxesToolTip
        {
            get { return _model.DisplayTipboxesToolTip; }
            set {
                _model.DisplayTipboxesToolTip = value;
                OnPropertyChanged( "DisplayTipboxesToolTip");
            }
        }

        public string DisplayPlatesToolTip
        {
            get { return _model.DisplayPlatesToolTip; }
            set {
                _model.DisplayPlatesToolTip = value;
                OnPropertyChanged( "DisplayPlatesToolTip");
            }
        }

        private string home_all_devices_tooltip_;
        public string HomeAllDevicesToolTip
        {
            get { return home_all_devices_tooltip_; }
            set {
                home_all_devices_tooltip_ = value;
                OnPropertyChanged( "HomeAllDevicesToolTip");
            }
        }


        public List<string> AvailableLabware
        {
            get {
                try {
                    return _model.LabwareDatabase.GetLabwareNames();
                } catch( Exception ex) {
                    _log.Error( ex.Message);
                }
                return new List<string>();
            }
        }

        public List<string> AvailableLabMethods
        {
            // DKM 2010-10-19 I am putting this in the ViewModel because it's a customer-specific detail.
            //                All of this ViewModel vs. Model code needs to be addressed in the post-mortem.
            get { return GetLabMethods(); }
        }

        private string _selected_source_labware;
        /// <summary>
        /// This property is set when the user selects a piece of labware from
        /// the GUI.  After updating the property, it will set the
        /// SourceLabwareSelected property, which will the automatically
        /// change the checkbox state in the GUI.
        /// </summary>
        public string SelectedSourceLabware
        {
            get { return _selected_source_labware; }
            set {
                _selected_source_labware = value;
                SourceLabwareSelected = true;
                OnPropertyChanged( "SelectedSourceLabware");
            }
        }

        private bool _source_labware_selected;
        public bool SourceLabwareSelected
        {
            get { return _source_labware_selected; }
            set {
                _source_labware_selected = value;
                OnPropertyChanged( "SourceLabwareSelected");
            }
        }

        private string selected_lab_method_;
        public string SelectedLabMethod
        {
            get { return selected_lab_method_; }
            set {
                selected_lab_method_ = value;
                // grab the lab method data and change the droplist values
                LabMethod method = GUIConfig.LabMethods.First( x => x.Name == selected_lab_method_);
                SelectedSourceLabware = method.SourceLabware;
                SelectedDestinationLabware = method.DestinationLabware;
                TransferVolume = method.VolumeUl;
                SelectedLiquidProfile = method.LiquidProfile;
                AspirateDistanceFromWellBottom = method.AspirateDistanceFromBottomMm;
                DispenseDistanceFromWellBottom = method.DispenseDistanceFromBottomMm;
            }
        }

        private string _selected_destination_labware;
        /// <summary>
        /// this works the same way as SelectedSourceLabware
        /// </summary>
        public string SelectedDestinationLabware
        {
            get { return _selected_destination_labware; }
            set {
                _selected_destination_labware = value;
                DestinationLabwareSelected = true;
                OnPropertyChanged( "SelectedDestinationLabware");
            }
        }

        private bool _destination_labware_selected;
        public bool DestinationLabwareSelected
        {
            get { return _destination_labware_selected; }
            set {
                _destination_labware_selected = value;
                OnPropertyChanged( "DestinationLabwareSelected");
            }
        }

        private string _selected_liquid_profile;
        /// <summary>
        /// this works the same way as SelectedSourceLabware
        /// </summary>
        public string SelectedLiquidProfile
        {
            get { return _selected_liquid_profile; }
            set {
                _selected_liquid_profile = value;
                LiquidProfileSelected = true;
                OnPropertyChanged( "SelectedLiquidProfile");
            }
        }

        private bool _liquid_profile_selected;
        public bool LiquidProfileSelected
        {
            get { return _liquid_profile_selected; }
            set {
                _liquid_profile_selected = value;
                OnPropertyChanged( "LiquidProfileSelected");
            }
        }

        /*
        private bool tipboxes_reinventoried_;
        public bool TipboxesReinventoried
        {
            get { return tipboxes_reinventoried_; }
            set {
                tipboxes_reinventoried_ = value;
                OnPropertyChanged( "TipboxesReinventoried");
            }
        }
         */
        public bool TipboxesReinventoried { get { return _model.TipboxesReinventoried; } }

        /*
        private bool plates_reinventoried_;
        public bool PlatesReinventoried
        {
            get { return plates_reinventoried_; }
            set {
                plates_reinventoried_ = value;
                OnPropertyChanged( "PlatesReinventoried");
            }
        }
         */
        public bool PlatesReinventoried { get { return _model.PlatesReinventoried; } }

        private double _transfer_volume;
        public double TransferVolume
        {
            get { return _transfer_volume; }
            set {
                _transfer_volume = value;
                TransferVolumeEntered = true;
                OnPropertyChanged( "TransferVolume");
            }
        }

        private bool _transfer_volume_entered;
        public bool TransferVolumeEntered
        {
            get { return _transfer_volume_entered; }
            set {
                _transfer_volume_entered = true;
                OnPropertyChanged( "TransferVolumeEntered");
            }
        }

        private double _aspirate_distance_from_well_bottom;
        public double AspirateDistanceFromWellBottom
        {
            get { return _aspirate_distance_from_well_bottom; }
            set {
                _aspirate_distance_from_well_bottom = value;
                AspirateDistanceFromWellBottomEntered = true;
                OnPropertyChanged( "AspirateDistanceFromWellBottom");
            }
        }

        private bool _aspirate_distance_from_well_bottom_entered;
        public bool AspirateDistanceFromWellBottomEntered
        {
            get { return _aspirate_distance_from_well_bottom_entered; }
            set {
                _aspirate_distance_from_well_bottom_entered = value;
                OnPropertyChanged( "AspirateDistanceFromWellBottomEntered");
            }
        }

        private double _dispense_distance_from_well_bottom;
        public double DispenseDistanceFromWellBottom
        {
            get { return _dispense_distance_from_well_bottom; }
            set {
                _dispense_distance_from_well_bottom = value;
                DispenseDistanceFromWellBottomEntered = true;
                OnPropertyChanged( "DispenseDistanceFromWellBottom");
            }
        }

        private bool _dispense_distance_from_well_bottom_entered;
        public bool DispenseDistanceFromWellBottomEntered
        {
            get { return _dispense_distance_from_well_bottom_entered; }
            set {
                _dispense_distance_from_well_bottom_entered = value;
                OnPropertyChanged( "DispenseDistanceFromWellBottomEntered");
            }
        }

        public List<string> AvailableLiquids
        {
            get { return _model.LiquidProfileLibrary.EnumerateLiquidProfileNames(); }
        }
 
        /// <summary>
        /// this is actually now not the best name -- it really means, user is using a plugin AND a protocol
        /// isn't running.  I used to set this when the user selects a user plugin, but now I'm using
        /// _user_plugin_selected for that.
        /// </summary>
        public bool LabMethodsEnabled
        { 
            get { return !ProtocolRunning && UserPluginSelected; }
        }

        /// <summary>
        /// #329, 330: prevent user from changing selected lab method and parameters after starting protocol
        /// </summary>
        bool _user_plugin_selected;
        public bool UserPluginSelected
        {
            get { return _user_plugin_selected; }
            set {
                _user_plugin_selected = value;
                OnPropertyChanged( "LabMethodsEnabled");
            }
        }
        /// <summary>
        /// whether or not the protocol is running
        /// </summary>
        private bool _protocol_running;
        public bool ProtocolRunning
        {
            get { return _protocol_running; }
            set {
                _protocol_running = value;
                OnPropertyChanged( "LabMethodsEnabled");
            }
        }

        private PioneerGUIConfiguration GUIConfig { get; set; }

        private List<string> GetLabMethods()
        {
            try {
                XmlSerializer serializer = new XmlSerializer(typeof(PioneerGUIConfiguration));
                string path = BioNex.Shared.Utils.FileSystem.GetAppPath() + "\\pioneer_config.xml";

                // this was just used to write out XML so I could figure out what it looks like
                /*
                FileStream writer = new FileStream( path, FileMode.Create);
                PioneerGUIConfiguration temp = new PioneerGUIConfiguration();
                temp.LabMethods.Add( new LabMethod { Name = "test" } );
                serializer.Serialize( writer, temp);
                 */

                FileStream reader = new FileStream( path, FileMode.Open);
                GUIConfig = (PioneerGUIConfiguration)serializer.Deserialize( reader);
                var names = from x in GUIConfig.LabMethods select x.Name;
                return names.ToList();
            } catch( Exception ex) {
                _log.Info( "Did not load any lab methods: " + ex.Message);
                return new List<string>();
            }
        }

        public void SelectHitpickFile()
        {
            // DKM 2012-01-12 refactoring SelectHitpickFile ended up being lame because the arguments to
            //                it have to be properties, which you cannot pass as outparams
            string selected_hitpick_file;
            ILimsTextConverter selected_lims_text_converter;
            bool user_plugin_selected;
            PluginUtilities.SelectHitpickFile( _model.LimsTextConverters, out selected_hitpick_file, out selected_lims_text_converter, out user_plugin_selected);
            _log.Info( "user selected hitpick file '" + selected_hitpick_file + "'");
            SelectedHitpickFile = selected_hitpick_file;
            SelectedLimsTextConverter = selected_lims_text_converter;
            UserPluginSelected = user_plugin_selected;
        }

        private void VerifyFileExtensionForLimsConverter( string filename, string file_extension)
        {
            int dotpos = filename.LastIndexOf( '.');
            string extension = filename.Substring( dotpos + 1);
            if( extension != file_extension) {
                // if there's a mismatch and the user selects XML, then assume a native bionex hitpick file
                // and therefore clear out the LIMS plugin reference
                if( extension == "xml")
                    SelectedLimsTextConverter = null;
                else
                    MessageBox.Show( String.Format( "Selected file does not match the extension expected for the LIMS converter plugin: {0}", file_extension));
            }
        }

        private bool ExecuteHitpick( string hitpick_filepath, ILimsTextConverter converter, string tip_handling_method,
                                     string source_labware, string dest_labware, string liquid_profile,
                                     double transfer_volume, double aspirate_distance_from_well_bottom,
                                     double dispense_distance_from_well_bottom)
        {
            string pre_protocol_message_path = FileSystem.GetAppPath() + "\\" + GUIConfig.PreProtocolMessageFilename;
            if( File.Exists(  pre_protocol_message_path)){
                StreamReader sr = new StreamReader( pre_protocol_message_path);
                string message = sr.ReadToEnd();
                if( MessageBox.Show( message, "'OK' to Continue or 'Cancel' to Abort", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel){
                    return false;
                }
                sr.Close();
            }

            // if we are using a customer's hitpick file format, then we need to use the selected
            // converter to get a Synapsis-compatible hitpick file.
            if( converter != null) {
                // set the default values first
                converter.DefaultSourceLabware = _model.LabwareDatabase.GetLabware( source_labware);
                converter.DefaultDestinationLabware = _model.LabwareDatabase.GetLabware( dest_labware);
                converter.DefaultLiquidProfile = liquid_profile;
                converter.DefaultTransferVolume = transfer_volume;
                converter.DefaultAspirateDistanceFromWellBottom = aspirate_distance_from_well_bottom;
                converter.DefaultDispenseDistanceFromWellBottom = dispense_distance_from_well_bottom;
                // now write the file
                try {
                    hitpick_filepath = converter.GetConvertedHitpickFile( hitpick_filepath);
                } catch( Exception ex) {
                    MessageBox.Show( String.Format( "Could not load the input file '{0}': please check formatting", hitpick_filepath, ex.Message));
                    return false;
                }
            }

            try {
                // wrap the ExecuteHitpick call in the HourglassWindow so that the user knows the system
                // isn't hung.  Wrapper function is in the ViewModel because the parameters are BB-specific,
                // so the details couldn't be in the shared class.
                return ExecuteHitpickWithHourglassWindow( _model.ExecuteHitpick, hitpick_filepath, tip_handling_method);
            } catch( System.Xml.Schema.XmlSchemaValidationException ex) {
                MessageBox.Show( String.Format( "Could not execute the hitpick file '{0}' because its schema is invalid: {1}", hitpick_filepath, ex.Message));
            }

            return true;
        }

        private bool ExecuteHitpickWithHourglassWindow( Func<string,string,bool> action, string param1, string param2)
        {
            action.BeginInvoke( param1, param2, OnActionComplete, null);
            var hg = new BioNex.Shared.Utils.HourglassWindow()
            {
                Title = "Enabling motors",
                Owner = Application.Current.MainWindow
            };
            hg.Show();
            //! \todo figure out another way to accomplish this non-blocking UI behavior without DoEvents
            while( !CloseHourglassWindowEvent.WaitOne( 10))
                System.Windows.Forms.Application.DoEvents();
            hg.Close();

            //! \todo get the real result from action!!!
            return _execute_hitpick_result;
        }

        private void OnActionComplete( IAsyncResult iar)
        {
            AsyncResult ar = (AsyncResult)iar;
            Func<string,string,bool> caller = (Func<string,string,bool>)ar.AsyncDelegate;
            _execute_hitpick_result = caller.EndInvoke( iar);
            CloseHourglassWindowEvent.Set();
        }

        private bool CanExecuteReinventoryTipboxesCommand()
        {
            StringBuilder sb = new StringBuilder();
            string reason;
            if( !SynapsisQuery.Value.SystemCheckOK( out reason)) { sb.AppendLine( reason); }
            if( SynapsisQuery.Value.Running || SynapsisQuery.Value.Paused) { sb.AppendLine( "Protocol is running"); }
            if( !SynapsisQuery.Value.AllDevicesHomed) { sb.AppendLine( "Not all devices are homed"); }
            if( _model.Reinventorying) { sb.AppendLine( "Currently reinventorying storage"); }

            if( sb.Length != 0) {
                ReinventoryTipboxesToolTip = sb.ToString();
                return false;
            } else {
                ReinventoryTipboxesToolTip = "Reinventory tipboxes";
                return true;
            }
        }

        private bool CanExecuteReinventoryPlatesCommand()
        {
            StringBuilder sb = new StringBuilder();
            string reason;
            if( !SynapsisQuery.Value.SystemCheckOK( out reason)) { sb.AppendLine( reason); }
            if( SynapsisQuery.Value.Running || SynapsisQuery.Value.Paused) { sb.AppendLine( "Protocol is running"); }
            if( !SynapsisQuery.Value.AllDevicesHomed) { sb.AppendLine( "Not all devices are homed"); }
            if( _model.Reinventorying) { sb.AppendLine( "Currently reinventorying storage"); }

            if( sb.Length != 0) {
                ReinventoryPlatesToolTip = sb.ToString();
                return false;
            } else {
                ReinventoryPlatesToolTip = "Reinventory plates";
                return true;
            }
        }

        private bool CanExecuteReinventoryAllStorageCommand()
        {
            StringBuilder sb = new StringBuilder();
            string reason;
            if( !SynapsisQuery.Value.SystemCheckOK( out reason)) { sb.AppendLine( reason); }
            if( SynapsisQuery.Value.Running || SynapsisQuery.Value.Paused) { sb.AppendLine( "Protocol is running"); }
            if( !SynapsisQuery.Value.AllDevicesHomed) { sb.AppendLine( "Not all devices are homed"); }
            if( _model.Reinventorying) { sb.AppendLine( "Currently reinventorying storage"); }

            if( sb.Length != 0) {
                ReinventoryAllStorageToolTip = sb.ToString();
                return false;
            } else {
                ReinventoryAllStorageToolTip = "Reinventory all storage devices";
                return true;
            }
        }
 
        private bool CanExecuteSelectHitpickCommand()
        {
            return !SynapsisQuery.Value.Running && !SynapsisQuery.Value.Paused;
        }

        private void ReinventoryTipboxes()
        {
            _model.ReinventoryTipboxes( true);
        }

        private void ReinventoryPlates()
        {
            _model.ReinventoryPlates();
        }

        private void ReinventoryAllStorage()
        {
            _model.ReinventoryAllStorage();
        }

        private void HomeAllDevices()
        {
            if( !SynapsisQuery.Value.HomeAllDevices())
                return;
                       
            _already_homing = true;
            // kick off thread that will only poll the AllDevicesHomed property until homing is complete
            new Thread( () => {
                ShowHomingHourglassWindow();
                // in case we were already homed, we need to wait until the axes start homing so that their homing flags get reset
                while( SynapsisQuery.Value.AllDevicesHomed)
                    Thread.Sleep( 100);
                while( !SynapsisQuery.Value.AllDevicesHomed)
                    Thread.Sleep( 100);
                _already_homing = false;
                CloseHomingHourglassWindow();
            }).Start();
        }

        private void ShowHomingHourglassWindow()
        {
            _dispatcher.Invoke( new Action( () => {
                _hg = new HourglassWindow();
                _hg.Title = "Homing";
                _hg.ShowInTaskbar = false;
                _hg.Owner = Application.Current.MainWindow;
                _hg.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                _hg.Show();
            } ));
        }

        private void CloseHomingHourglassWindow()
        {
            _dispatcher.Invoke(new Action(() => { _hg.Close(); }));
        }

        private bool CanExecuteHomeAllDevicesCommand()
        {
            string reason;
            StringBuilder sb = new StringBuilder();
            bool ok = SynapsisQuery.Value.ClearToHome( out reason);
            if( !ok)
                sb.AppendLine( reason);
            if( _already_homing)
                sb.AppendLine( "Already homing");
            
            if( sb.Length == 0)
                HomeAllDevicesToolTip = "Home all devices";
            else
                HomeAllDevicesToolTip = sb.ToString();

            return sb.Length == 0;
        }

        #region ICustomerGUI Members

        public bool CanExecuteStart(out IEnumerable<string> failure_reasons)
        {
            List<string> reasons = new List<string>();
            if( !HitpickFileSelected) reasons.Add( "Hitpick file was not selected");
            if( _model.Reinventorying) reasons.Add( "Currently reinventorying storage");
            if( !TipboxesReinventoried) reasons.Add( "Tipboxes not reinventoried");
            if( !PlatesReinventoried) reasons.Add( "Plates not reinventoried");
            if( LabMethodsEnabled) {
                if( !SourceLabwareSelected) reasons.Add( "Source labware was not selected");
                if( !DestinationLabwareSelected) reasons.Add( "Destination labware was not selected");
                if( !LiquidProfileSelected) reasons.Add( "Liquid profile was not selected");
                if( !TransferVolumeEntered) reasons.Add( "Transfer volume was not entered");
                if( !AspirateDistanceFromWellBottomEntered) reasons.Add( "Aspirate distance from well bottom was not entered");
                if( !DispenseDistanceFromWellBottomEntered) reasons.Add( "Dispense distance from well bottom was not entered");
            }

            failure_reasons = reasons;
            return reasons.Count() == 0;
        }

        public bool ExecuteStart()
        {
            // REED change "" to "Change tip" to enable tip changing
            bool successful_start = ExecuteHitpick( SelectedHitpickFile, UserPluginSelected ? SelectedLimsTextConverter : null, "Change tip", SelectedSourceLabware,
                                                    SelectedDestinationLabware, SelectedLiquidProfile, TransferVolume, AspirateDistanceFromWellBottom,
                                                    DispenseDistanceFromWellBottom);
            ProtocolRunning = successful_start;
            // only register event handler if we were successful, or we'll fire ProtocolComplete twice
            // if we first fail a tipbox or labware check, and then run again.
            if( successful_start)
                _model.ProtocolComplete += new EventHandler(_model_ProtocolComplete);
            return successful_start;
        }

        public bool ShowProtocolExecuteButtons()
        {
            return true;
        }

        void _model_ProtocolComplete(object sender, EventArgs e)
        {
            ProtocolRunning = false;
            if( ProtocolComplete != null)
                ProtocolComplete( this, new EventArgs());
        }

        public string GUIName
        {
            get { return "Pioneer GUI"; }
        }

        public bool Busy
        {
            get { return _model.Reinventorying || _already_homing; }
        }

        public string BusyReason
        {
            get {
                if( _model.Reinventorying)
                    return "Reinventorying storage";
                else if( _already_homing)
                    return "System is homing";
                else
                    return "Not busy";
            }
        }

        public void Close()
        {
        }

        public bool CanClose() { return true; }
        public bool CanPause()
		{
			// pause/resume button available as long as protocol is running.
			return ProtocolRunning;
		}

        public bool AllowDiagnostics() { return true; }

        public void CompositionComplete() { }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion

        private bool _execute_hitpick_result { get; set; }
    }
}
