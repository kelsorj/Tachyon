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

namespace BioNex.LabAutoGUI
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    [Export(typeof(ICustomerGUI))]
    public partial class Checklist : UserControl, ICustomerGUI, INotifyPropertyChanged
    {
        [Import]
        private Lazy<ICustomSynapsisQuery> SynapsisQuery { get; set; }
        public Model _model { get; private set; }
        public event EventHandler ProtocolComplete;
        public event EventHandler AbortableTaskStarted;
        public event EventHandler AbortableTaskComplete;

        // logging to _log goes into all of the main log's appenders
        private static readonly ILog _log = LogManager.GetLogger( "LabAutoGUI");

        private AutoResetEvent CloseHourglassWindowEvent = new AutoResetEvent( false);

        [ImportingConstructor]
        public Checklist( [Import("LabAutoModel")] Model model)
        {
            InitializeComponent();
            _model = model;
            this.DataContext =  this;
            InitializeCommands();
        }

        ~Checklist()
        {
            _model.ProtocolComplete -= this.ProtocolComplete;
        }

        private void InitializeCommands()
        {
            SelectHitpickCommand = new RelayCommand( SelectHitpickFile, CanExecuteSelectHitpickCommand);
            HomeAllDevicesCommand = new RelayCommand( HomeAllDevices, CanExecuteHomeAllDevicesCommand );
            ReinventoryTipboxesCommand = new RelayCommand( ReinventoryTipboxes, CanExecuteReinventoryTipboxesCommand);
            ReinventoryPlatesCommand = new RelayCommand( ReinventoryPlates, CanExecuteReinventoryPlatesCommand);
            DisplayTipboxesCommand = new RelayCommand( _model.DisplayTipboxes);
            DisplayPlatesCommand = new RelayCommand( _model.DisplayPlates);
            DisplayTipboxesToolTip = DisplayPlatesToolTip = "This feature is not yet available";            
        }

        public RelayCommand SelectHitpickCommand { get; set; }
        public RelayCommand HomeAllDevicesCommand { get; set; }
        public RelayCommand ReinventoryTipboxesCommand { get; set; }
        public RelayCommand ReinventoryPlatesCommand { get; set; }
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

        private string reinventory_plates_tooltip_;
        public string ReinventoryPlatesToolTip
        {
            get { return reinventory_plates_tooltip_; }
            set {
                reinventory_plates_tooltip_ = value;
                OnPropertyChanged( "ReinventoryPlatesToolTip");
            }
        }

        private string display_tipboxes_tooltip_;
        public string DisplayTipboxesToolTip
        {
            get { return display_tipboxes_tooltip_; }
            set {
                display_tipboxes_tooltip_ = value;
                OnPropertyChanged( "DisplayTipboxesToolTip");
            }
        }

        private string display_plates_tooltip_;
        public string DisplayPlatesToolTip
        {
            get { return display_plates_tooltip_; }
            set {
                display_plates_tooltip_ = value;
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
                // check for null since we might not have loaded any GUI methods
                if( GUIConfig == null)
                    return;
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
 
        private bool _is_using_user_plugin;
        public bool IsUsingUserPlugin
        { 
            get { return _is_using_user_plugin; }
            set {
                _is_using_user_plugin = value;
                OnPropertyChanged( "IsUsingUserPlugin");
            }
        }

        private LabMethodConfiguration GUIConfig { get; set; }

        private List<string> GetLabMethods()
        {
            try {
                XmlSerializer serializer = new XmlSerializer(typeof(LabMethodConfiguration));
                string path = BioNex.Shared.Utils.FileSystem.GetAppPath() + "\\lab_methods.xml";

                /*
                FileStream writer = new FileStream( path, FileMode.Create);
                PioneerGUIConfiguration temp = new PioneerGUIConfiguration();
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
                    IsUsingUserPlugin = true;
                } else {
                    IsUsingUserPlugin = false;
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
                hitpick_filepath = converter.GetConvertedHitpickFile( hitpick_filepath);
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
            return _hitpick_ok_to_start;
        }

        private bool _hitpick_ok_to_start { get; set; }

        private void OnActionComplete( IAsyncResult iar)
        {
            try {
                AsyncResult ar = (AsyncResult)iar;
                Func<string,string,bool> caller = (Func<string,string,bool>)ar.AsyncDelegate;
                _hitpick_ok_to_start = caller.EndInvoke( iar);
            } catch( Exception ex) {
                _log.Error( ex.Message);
            } finally {
                CloseHourglassWindowEvent.Set();
            }
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
 
        private bool CanExecuteSelectHitpickCommand()
        {
            return !SynapsisQuery.Value.Running && !SynapsisQuery.Value.Paused;
        }

        private void ReinventoryTipboxes()
        {
            _model.ReinventoryTipboxes();
        }

        private void ReinventoryPlates()
        {
            _model.ReinventoryPlates();
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
            if( !TipboxesReinventoried) reasons.Add( "Tipboxes not reinventoried");
            if( !PlatesReinventoried) reasons.Add( "Plates not reinventoried");
            if( IsUsingUserPlugin) {
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
            _model.ProtocolComplete += this.ProtocolComplete;
            // REED change "" to "Change tip" to enable tip changing
            bool result =  ExecuteHitpick( SelectedHitpickFile, SelectedLimsTextConverter, "Change tip", SelectedSourceLabware,
                                           SelectedDestinationLabware, SelectedLiquidProfile, TransferVolume, AspirateDistanceFromWellBottom,
                                           DispenseDistanceFromWellBottom);
            return result;
        }

        public bool ShowProtocolExecuteButtons()
        {
            return true;
        }

        public string GUIName
        {
            get { return "Customer GUI"; }
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

        public void Close()
        {
            
        }

        public bool CanPause()
        {
            return true;
        }

        public bool CanClose()
        {
            return true;
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
    }
}
