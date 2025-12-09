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
using BioNex.SynapsisPrototype;
using BioNex.Shared.DeviceInterfaces;

namespace BioNex.IgenicaGUI
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    [Export(typeof(ICustomerGUI))]
    public partial class Checklist : UserControl, ICustomerGUI, INotifyPropertyChanged
    {
        [Import]
        private Lazy<ICustomSynapsisQuery> SynapsisQuery { get; set; }
        [Import]
        private DeviceManager _device_manager { get; set; }
        [Import]
        private ILabwareDatabase _labware_database { get; set; }
        public Model _model { get; private set; }
        public event EventHandler ProtocolComplete;
        public event EventHandler AbortableTaskStarted;
        public event EventHandler AbortableTaskComplete;

        // logging to _log goes into all of the main log's appenders
        private static readonly ILog _log = LogManager.GetLogger( "PioneerGUI");

        private AutoResetEvent CloseHourglassWindowEvent = new AutoResetEvent( false);

        private IgenicaGuiConfig _config { get; set; }

        [ImportingConstructor]
        public Checklist( [Import("IgenicaModel")] Model model)
        {
            InitializeComponent();
            _model = model;
            this.DataContext =  this;
            InitializeCommands();
            _model.ModelAbortableProcessStarted += AbortableTaskStarted;
            _model.ModelAbortableProcessComplete += AbortableTaskComplete;

            LoadDllConfiguration();
        }

        private void LoadDllConfiguration()
        {
            string config_path = BioNex.Shared.Utils.FileSystem.GetAppPath() + "\\config\\IgenicaGuiConfig.xml";

            // testing only, save config here
            /*
            IgenicaGuiConfig config = new IgenicaGuiConfig();
            config.source_barcode_ranges.Add( new IgenicaGuiConfig.range { labware_name = "384 source", start_barcode = 3701000000, end_barcode = 3701000999 } );
            config.destination_barcode_ranges.Add( new IgenicaGuiConfig.range { labware_name = "48 dest", start_barcode = 3548000000, end_barcode = 3548000999 } );
            config.destination_barcode_ranges.Add( new IgenicaGuiConfig.range { labware_name = "96 dest", start_barcode = 3585000000, end_barcode = 3585000999 } );
            FileSystem.SaveXmlConfiguration<IgenicaGuiConfig>( config, config_path);
             */
            
            // load the config here
            if( !File.Exists( config_path)) {
                _log.Info( String.Format( "The file '{0}' was not found, so source / destination plate barcode detection will be unavailable.", config_path));
                return;
            }
            _config = FileSystem.LoadXmlConfiguration<IgenicaGuiConfig>( config_path);
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
            DisplayTipboxesCommand = new RelayCommand( _model.DisplayTipboxes, _model.CanExecuteDisplayTipboxes);
        }

        public RelayCommand SelectHitpickCommand { get; set; }
        public RelayCommand HomeAllDevicesCommand { get; set; }
        public RelayCommand ReinventoryTipboxesCommand { get; set; }
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

        public string DisplayTipboxesToolTip
        {
            get { return _model.DisplayTipboxesToolTip; }
            set {
                _model.DisplayTipboxesToolTip = value;
                OnPropertyChanged( "DisplayTipboxesToolTip");
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
                //SelectedDestinationLabware = method.DestinationLabware;
                TransferVolume = method.VolumeUl;
                SelectedLiquidProfile = method.LiquidProfile;
                AspirateDistanceFromWellBottom = method.AspirateDistanceFromBottomMm;
                DispenseDistanceFromWellBottom = method.DispenseDistanceFromBottomMm;
            }
        }

        /*
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
         */

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

        private LabMethodConfiguration GUIConfig { get; set; }

        private List<string> GetLabMethods()
        {
            try {
                XmlSerializer serializer = new XmlSerializer(typeof(LabMethodConfiguration));
                string path = BioNex.Shared.Utils.FileSystem.GetAppPath() + "\\config\\lab_methods.xml";

                // this was just used to write out XML so I could figure out what it looks like
                /*
                FileStream writer = new FileStream( path, FileMode.Create);
                LabMethodConfiguration temp = new LabMethodConfiguration();
                temp.LabMethods.Add( new LabMethod { Name = "test" } );
                serializer.Serialize( writer, temp);
                 */

                FileStream reader = new FileStream( path, FileMode.Open);
                GUIConfig = (LabMethodConfiguration)serializer.Deserialize( reader);
                var names = from x in GUIConfig.LabMethods select x.Name;
                return names.ToList();
            } catch( Exception ex) {
                _log.Info( "Did not load any lab methods: " + ex.Message);
                return new List<string>();
            }
        }

        public void SelectHitpickFile()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            // get all filters from available hitpick conversion plugins
            IEnumerable<ILimsTextConverter> converters = _model.LimsTextConverters;
            dlg.Filter = "XML files (*.xml)|*.xml";
            foreach( ILimsTextConverter converter in converters)
                dlg.Filter += "|" + converter.Filter;
            // probably not the most efficient way to do this, but it works
            List<ILimsTextConverter> converter_list = new List<ILimsTextConverter>();

            converter_list.AddRange( converters);

            if( dlg.ShowDialog() == true) {
                // got a filename, so set it in the GUI
                SelectedHitpickFile = dlg.FileName;
                _log.Info( "user selected hitpick file '" + SelectedHitpickFile + "'");
                // get the filter index so we can decide what to do with the file
                // need to make sure that the file extension of the selected file matches that of the plugin
                // why did MS make FilterIndex 1-based?
                if( dlg.FilterIndex != 1) {
                    SelectedLimsTextConverter = converter_list[dlg.FilterIndex - 2]; // -2 because FilterIndex is 1-based!
                    // verify that the filename selected matches the expected extension
                    VerifyFileExtensionForLimsConverter( dlg.FileName, SelectedLimsTextConverter.FileExtension);
                    UserPluginSelected = true;
                } else {
                    UserPluginSelected = false;
                }
            }
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
                                     string source_labware, string liquid_profile,
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
                //converter.DefaultDestinationLabware = _model.LabwareDatabase.GetLabware( dest_labware);
                converter.DefaultLiquidProfile = liquid_profile;
                converter.DefaultTransferVolume = transfer_volume;
                converter.DefaultAspirateDistanceFromWellBottom = aspirate_distance_from_well_bottom;
                converter.DefaultDispenseDistanceFromWellBottom = dispense_distance_from_well_bottom;
                // now write the file
                List<string> available_destination_barcodes = new List<string>();
                try {
                    available_destination_barcodes = GetDestinationBarcodes(); // comes from reinventory process
                } catch( Exception ex) {
                    MessageBox.Show( "Could not determine which destination plates to use in this protocol: " + ex.Message);
                    return false;
                }

                try {
                    // fail if we don't have any dest barcodes
                    if( available_destination_barcodes.Count() == 0) {
                        MessageBox.Show( "There are not any destination plates in the system.  Please check the plate inventory, as well as the ranges of acceptable destination plate barcodes.");
                        return false;
                    }
                    string dest_labware_name = _config.GetLabwareNameForBarcode( available_destination_barcodes[0]);
                    ILabware dest_labware = _labware_database.GetLabware( dest_labware_name); // comes from config file once we have destination barcodes
                    hitpick_filepath = converter.GetConvertedHitpickFile( hitpick_filepath, available_destination_barcodes, dest_labware);
                } catch( LabwareNotFoundException ex) {
                    MessageBox.Show( String.Format( "The labware named '{0}' was not found in the labware database.  Please check the labware name present in IgenicaGuiConfig.xml.", ex.LabwareName));
                    return false;
                } catch( Exception ex) {
                    MessageBox.Show( String.Format( "Could not load the input file '{0}': {1}", hitpick_filepath, ex.Message));
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

        /// <summary>
        /// Gets all of the destination barcodes in all plate storage devices
        /// </summary>
        /// <returns></returns>
        private List<string> GetDestinationBarcodes()
        {
            // first combine the inventory from all of the plate storage devices
            IEnumerable<IEnumerable<string>> barcodes1 = from robot in _device_manager.GetRobotInterfaces()
                                                         from storage in _device_manager.GetPlateStorageInterfaces()
                                                         select (from x in storage.GetInventory( (robot as DeviceInterface).Name) select x.Key);
            List<string> barcodes = new List<string>();
            foreach( var x in barcodes1) {
                barcodes.AddRange( from barcode in x where _config.IsDestinationBarcode( barcode) select barcode);
            }
            return barcodes;
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

        private bool CanExecuteSelectHitpickCommand()
        {
            return !SynapsisQuery.Value.Running && !SynapsisQuery.Value.Paused;
        }

        private void ReinventoryTipboxes()
        {
            _model.ReinventoryTipboxes( true);
        }

        private void HomeAllDevices()
        {
            SynapsisQuery.Value.HomeAllDevices();
        }

        private bool CanExecuteHomeAllDevicesCommand()
        {
            string reason;
            bool ok = SynapsisQuery.Value.ClearToHome( out reason);
            if( !ok)
                HomeAllDevicesToolTip = reason;
            else
                HomeAllDevicesToolTip = "Home all devices";

            return ok;
        }

        #region ICustomerGUI Members

        public bool CanExecuteStart(out IEnumerable<string> failure_reasons)
        {
            List<string> reasons = new List<string>();
            if( !HitpickFileSelected) reasons.Add( "Hitpick file was not selected");
            if( _model.Reinventorying) reasons.Add( "Currently reinventorying storage");
            if( !PlatesReinventoried) reasons.Add( "Plates not reinventoried");
            if( LabMethodsEnabled) {
                if( !SourceLabwareSelected) reasons.Add( "Source labware was not selected");
                //if( !DestinationLabwareSelected) reasons.Add( "Destination labware was not selected");
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
            bool successful_start = ExecuteHitpick( SelectedHitpickFile, SelectedLimsTextConverter, "Change tip", SelectedSourceLabware,
                                                    SelectedLiquidProfile, TransferVolume, AspirateDistanceFromWellBottom,
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
            get { return "Igenica GUI"; }
        }

        public bool Busy
        {
            get { return _model.Reinventorying; }
        }

        public string BusyReason
        {
            get {
                if( _model.Reinventorying)
                    return "Reinventorying storage";
                else
                    return "Not busy";
            }
        }

        public bool CanClose()
        {
            return true;
        }

        public void Close()
        {
        }

        public bool AllowDiagnostics() { return true; }

        public bool CanPause() { return true; }

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
