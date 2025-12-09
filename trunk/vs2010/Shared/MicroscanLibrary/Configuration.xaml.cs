using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BioNex.Shared.Utils;
using GalaSoft.MvvmLight.Command;

namespace BioNex.Shared.Microscan
{
    /// <summary>
    /// Interaction logic for Configuration.xaml
    /// </summary>
    public partial class Configuration : UserControl, INotifyPropertyChanged
    {
        internal MicroscanReader _reader;

        public class BcrPropertyValue
        {
            public string PropertyName { get; set; }
            public string PropertyValue { get; set; }
        }

        public RelayCommand DownloadSettingsCommand { get; set; }
        public RelayCommand UploadSettingsCommand { get; set; }
        public RelayCommand SendAndSaveCommand { get; set; }
        public RelayCommand SendAndSaveAsDefaultsCommand { get; set; }
        public RelayCommand ResetSymbologiesCommand { get; set; }

        /// <summary>
        /// takes the values from the GUI and saves them in the reader at the currently-
        /// selected configuration database index
        /// </summary>
        public RelayCommand SaveIndexValuesCommand { get; set; }
        /// <summary>
        /// loads a configuration database index's settings into the reader
        /// </summary>
        public RelayCommand LoadIndexValuesCommand { get; set; }
        /// <summary>
        /// Reloads barcode_configuration_database.xml
        /// </summary>
        public RelayCommand ReloadConfigurationDatabaseCommand { get; set; }

        MicroscanReader.DecodeSettings _gui_settings = new MicroscanReader.DecodeSettings();

        public ObservableCollection<BcrPropertyValue> BcrProperties { get; set; }

        private int _selectedconfigindex;
        public int SelectedConfigIndex
        {
            get { return _selectedconfigindex; }
            set {
                _selectedconfigindex = value;
                OnPropertyChanged( "SelectedConfigIndex");
            }
        }

        public List<SelectableString> AvailableSymbologies { get; set; }

        #region stuff for databinding to GUI

        // reader properties in the configuration database
        // these are used for binding with the GUI
        private int _shutterspeed;
        public int ShutterSpeed
        {
            get { return _shutterspeed; }
            set {
                _shutterspeed = value;
                OnPropertyChanged( "ShutterSpeed");
            }
        }
        private int _gain;
        public int Gain
        {
            get { return _gain; }
            set {
                _gain = value;
                OnPropertyChanged( "Gain");
            }
        }
        private double _focaldistanceinches;
        public double FocalDistanceInches 
        {
            get { return _focaldistanceinches; }
            set {
                _focaldistanceinches = value;
                OnPropertyChanged( "FocalDistanceInches");
            }
        }
        private int _pixelsubsamplingindex;
        public int PixelSubSamplingIndex
        {
            get { return _pixelsubsamplingindex; }
            set {
                _pixelsubsamplingindex = value;
                OnPropertyChanged( "PixelSubSamplingIndex");
            }
        }
        private int _rowpointer;
        public int RowPointer
        {
            get { return _rowpointer; }
            set {
                _rowpointer = value;
                OnPropertyChanged( "RowPointer");
            }
        }
        private int _columnpointer;
        public int ColumnPointer
        {
            get { return _columnpointer; }
            set {
                _columnpointer = value;
                OnPropertyChanged( "ColumnPointer");
            }
        }
        private int _rowdepth;
        public int RowDepth
        {
            get { return _rowdepth; }
            set {
                _rowdepth = value;
                OnPropertyChanged( "RowDepth");
            }
        }
        private int _columnwidth;
        public int ColumnWidth
        {
            get { return _columnwidth; }
            set {
                _columnwidth = value;
                OnPropertyChanged( "ColumnWidth");
            }
        }
        private int _thresholdmodefixedindex;
        public int ThresholdModeFixedIndex
        {
            get { return _thresholdmodefixedindex; }
            set {
                _thresholdmodefixedindex = value;
                OnPropertyChanged( "ThresholdModeFixedIndex");
            }
        }
        private int _fixedthresholdvalue;
        public int FixedThresholdValue
        {
            get { return _fixedthresholdvalue; }
            set {
                _fixedthresholdvalue = value;
                OnPropertyChanged( "FixedThresholdValue");
            }
        }
        private int _processingmodeindex;
        public int ProcessingModeIndex
        {
            get { return _processingmodeindex; }
            set {
                _processingmodeindex = value;
                OnPropertyChanged( "ProcessingModeIndex");
            }
        }
        private int _narrowmarginsindex;
        public int NarrowMarginsIndex
        {
            get { return _narrowmarginsindex; }
            set {
                _narrowmarginsindex = value;
                OnPropertyChanged( "NarrowMarginsIndex");
            }
        }
        private int _backgroundcolorindex; 
        public int BackgroundColorIndex
        {
            get { return _backgroundcolorindex; }
            set {
                _backgroundcolorindex = value;
                OnPropertyChanged( "BackgroundColorIndex");
            }
        }
        private int _symbologymask;
        public int SymbologyMask
        {
            get { return _symbologymask; }
            set {
                _symbologymask = value;
                OnPropertyChanged( "SymbologyMask");
            }
        }

        // datagrid headers
        private string _propertynameheader;
        public string PropertyNameHeader
        {
            get { return _propertynameheader; }
            set {
                _propertynameheader = value;
                OnPropertyChanged( "PropertyNameHeader");
            }
        }

        private string _propertyvalueheader;
        public string PropertyValueHeader
        {
            get { return _propertynameheader; }
            set {
                _propertyvalueheader = value;
                OnPropertyChanged( "PropertyValueHeader");
            }
        }

        #endregion

        public Configuration()
        {
            AvailableSymbologies = new List<SelectableString>{ new SelectableString { Value = "All symbologies except Pharmacode" }, 
                                                               new SelectableString { Value = "Data Matrix" },
                                                               new SelectableString { Value = "QR Code" },
                                                               new SelectableString { Value = "Code 128" },
                                                               new SelectableString { Value = "Code 39" },
                                                               new SelectableString { Value = "Codabar" },
                                                               new SelectableString { Value = "Code 93" },
                                                               new SelectableString { Value = "Interleaved 2 of 5" },
                                                               new SelectableString { Value = "UPC/EAN" },
                                                               new SelectableString { Value = "PDF417" },
                                                               new SelectableString { Value = "MicroPDF417" },
                                                               new SelectableString { Value = "BC412" },
                                                               new SelectableString { Value = "Pharmacode" },
                                                               new SelectableString { Value = "DataBar-14" },
                                                               new SelectableString { Value = "DataBar Limited" },
                                                               new SelectableString { Value = "DataBar Expanded" },
                                                               new SelectableString { Value = "Micro QR Code" },
                                                               new SelectableString { Value = "Aztec Code" },
                                                               new SelectableString { Value = "Postal Symbologies" },
                                                               new SelectableString { Value = "OCR" } };

            InitializeComponent();
            DataContext = this;
            SelectedConfigIndex = 0;
            PropertyNameHeader = "Property Name";
            PropertyValueHeader = "Property Value";
            BcrProperties = new ObservableCollection<BcrPropertyValue>();

            // reader memory
            DownloadSettingsCommand = new RelayCommand( ExecuteDownloadSettingsCommand, () => { return _reader != null && _reader.Connected; } );
            UploadSettingsCommand = new RelayCommand( ExecuteUploadSettingsCommand, () => { return _reader != null && _reader.Connected; } );
            SendAndSaveCommand = new RelayCommand( ExecuteSendAndSaveCommand, () => { return _reader != null && _reader.Connected; } );
            SendAndSaveAsDefaultsCommand = new RelayCommand( ExecuteSendAndSaveAsDefaultsCommand, () => { return _reader != null && _reader.Connected; } );

            // -------------
            // configuration database
            LoadIndexValuesCommand = new RelayCommand( ExecuteLoadIndexValuesCommand, () => { return _reader != null && _reader.Connected; } );
            SaveIndexValuesCommand = new RelayCommand( ExecuteSaveIndexValuesCommand, () => { return _reader != null && _reader.Connected; } );
            ResetSymbologiesCommand = new RelayCommand( ExecuteResetSymbologiesCommand, () => { return _reader != null && _reader.Connected; } );
            ReloadConfigurationDatabaseCommand = new RelayCommand( ExecuteReloadConfigurationDatabaseCommand, () => { return _reader != null && _reader.Connected; });
            // ----------------------
        }

        private void ExecuteDownloadSettingsCommand()
        {
            try {
                // update the GUI
                _gui_settings = _reader.ReadCurrentSettings();
                //! \todo should I use reflection?
                BcrProperties.Clear();
                BcrProperties.Add( new BcrPropertyValue { PropertyName = MicroscanReader.PropertyNames.Gain, PropertyValue = _gui_settings.Gain.ToString() });
                BcrProperties.Add( new BcrPropertyValue { PropertyName = MicroscanReader.PropertyNames.ShutterSpeed, PropertyValue = _gui_settings.ShutterSpeed.ToString() });
                BcrProperties.Add( new BcrPropertyValue { PropertyName = MicroscanReader.PropertyNames.FocalDistance, PropertyValue = _gui_settings.FocalDistanceInches.ToString() });
                BcrProperties.Add( new BcrPropertyValue { PropertyName = MicroscanReader.PropertyNames.SubSampling, PropertyValue = _gui_settings.SubSampling.ToString() });
                BcrProperties.Add( new BcrPropertyValue { PropertyName = MicroscanReader.PropertyNames.RowPointer, PropertyValue = _gui_settings.WOI.RowPointer.ToString() });
                BcrProperties.Add( new BcrPropertyValue { PropertyName = MicroscanReader.PropertyNames.ColumnPointer, PropertyValue = _gui_settings.WOI.ColumnPointer.ToString() });
                BcrProperties.Add( new BcrPropertyValue { PropertyName = MicroscanReader.PropertyNames.RowDepth, PropertyValue = _gui_settings.WOI.RowDepth.ToString() });
                BcrProperties.Add( new BcrPropertyValue { PropertyName = MicroscanReader.PropertyNames.ColumnWidth, PropertyValue = _gui_settings.WOI.ColumnWidth.ToString() });
                BcrProperties.Add( new BcrPropertyValue { PropertyName = MicroscanReader.PropertyNames.NarrowMargins, PropertyValue = _gui_settings.NarrowMargins.ToString() });
                BcrProperties.Add( new BcrPropertyValue { PropertyName = MicroscanReader.PropertyNames.BackgroundColor, PropertyValue = _gui_settings.BackgroundColor.ToString() });
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private MicroscanReader.DecodeSettings GetGuiSettings()
        {
            MicroscanReader.DecodeSettings settings = new MicroscanReader.DecodeSettings();
            // all of the values are in BcrProperties
            settings.Gain = BcrProperties.Where( x => x.PropertyName == MicroscanReader.PropertyNames.Gain).First().PropertyValue.ToInt();
            settings.ShutterSpeed = BcrProperties.Where( x => x.PropertyName == MicroscanReader.PropertyNames.ShutterSpeed).First().PropertyValue.ToInt();
            settings.FocalDistanceInches = BcrProperties.Where( x => x.PropertyName == MicroscanReader.PropertyNames.FocalDistance).First().PropertyValue.ToDouble();
            settings.SubSampling = BcrProperties.Where( x => x.PropertyName == MicroscanReader.PropertyNames.SubSampling).First().PropertyValue.ToInt();
            settings.WOI.RowPointer = BcrProperties.Where( x => x.PropertyName == MicroscanReader.PropertyNames.RowPointer).First().PropertyValue.ToInt();
            settings.WOI.ColumnPointer = BcrProperties.Where( x => x.PropertyName == MicroscanReader.PropertyNames.ColumnPointer).First().PropertyValue.ToInt();
            settings.WOI.RowDepth = BcrProperties.Where( x => x.PropertyName == MicroscanReader.PropertyNames.RowDepth).First().PropertyValue.ToInt();
            settings.WOI.ColumnWidth = BcrProperties.Where( x => x.PropertyName == MicroscanReader.PropertyNames.ColumnWidth).First().PropertyValue.ToInt();
            settings.NarrowMargins = BcrProperties.Where( x => x.PropertyName == MicroscanReader.PropertyNames.NarrowMargins).First().PropertyValue.ToInt();
            settings.BackgroundColor = BcrProperties.Where( x => x.PropertyName == MicroscanReader.PropertyNames.BackgroundColor).First().PropertyValue.ToInt();
            return settings;
        }

        private void SendGuiSettingsToReader()
        {
            try {
                _gui_settings = GetGuiSettings();
                // send all of the settings in the reader settings datagrid to the reader
                _reader.SetAsCurrentSettings( _gui_settings);
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private void ExecuteUploadSettingsCommand()
        {
            try {
                SendGuiSettingsToReader();
                _reader.SendNoSave();
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private void ExecuteSendAndSaveCommand()
        {
            try {
                SendGuiSettingsToReader();
                _reader.SendAndSave();
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private void ExecuteSendAndSaveAsDefaultsCommand()
        {
            try {
                SendGuiSettingsToReader();
                _reader.SendAndSaveAsCustomerDefaults();
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private void UpdateGuiWithCurrentIndexSettings()
        {
            try {
                MicroscanReader.DecodeSettings settings = _reader.GetConfigurationIndexSettings( SelectedConfigIndex);
                ShutterSpeed = settings.ShutterSpeed;
                Gain = settings.Gain;
                FocalDistanceInches = settings.FocalDistanceInches;
                PixelSubSamplingIndex = settings.SubSampling;
                RowPointer = settings.WOI.RowPointer;
                ColumnPointer = settings.WOI.ColumnPointer;
                RowDepth = settings.WOI.RowDepth;
                ColumnWidth = settings.WOI.ColumnWidth;
                ThresholdModeFixedIndex = settings.ThresholdMode;
                FixedThresholdValue = settings.FixedThresholdValue;
                ProcessingModeIndex = settings.ProcessingMode;
                NarrowMarginsIndex = settings.NarrowMargins;
                BackgroundColorIndex = settings.BackgroundColor;
                SymbologyMask = settings.Symbologies;

                UpdateSymbologyMaskListBox();
            } catch( Exception ex) {
                MessageBox.Show( "Error during barcode reader configuration database query: " +  ex.Message);
            }
        }

        /// <summary>
        /// selects the items in AvailableSymbologies, based on the mask that gets passed in
        /// </summary>
        private void UpdateSymbologyMaskListBox()
        {
            foreach( var x in AvailableSymbologies)
                x.IsSelected = false;
            // if mask is 0, then we want to use the current symbology setting
            if( SymbologyMask == 0) {
                OnPropertyChanged( "AvailableSymbologies");
                return;
            }

            // -1 because we don't count the "all symbologies" entry
            for( int i=0; i<AvailableSymbologies.Count(); i++) {
                if( (SymbologyMask & (1 << i)) != 0)
                    AvailableSymbologies[i].IsSelected = true;
            }
            OnPropertyChanged( "AvailableSymbologies");
        }

        /// <summary>
        /// loads the values from the configuration database, updates the config database GUI, then
        /// writes the config values to the reader, and then updates the reader settings GUI
        /// </summary>
        private void ExecuteLoadIndexValuesCommand()
        {
            try {
                _reader.LoadConfigurationIndex( SelectedConfigIndex, true);
                // update reader GUI
                ExecuteDownloadSettingsCommand();
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private void ExecuteSaveIndexValuesCommand()
        {
            MessageBox.Show("This will only save the current index settings to the reader.  Upon restart, the values will be reloaded from barcode_reader_configuration.xml.  If you want these changes to persist, you must modify barcode_reader_configuration.xml.");

            // load configuration database settings into reader
            MicroscanReader.DecodeSettings settings = new MicroscanReader.DecodeSettings();
            settings.Gain = Gain;
            settings.ShutterSpeed = ShutterSpeed;
            settings.FocalDistanceInches = FocalDistanceInches;
            settings.SubSampling = PixelSubSamplingIndex;
            settings.WOI = new MicroscanReader.WindowOfInterest( RowPointer, ColumnPointer, RowDepth, ColumnWidth);
            settings.ThresholdMode = ThresholdModeFixedIndex;
            settings.FixedThresholdValue = FixedThresholdValue;
            settings.ProcessingMode = ProcessingModeIndex;
            settings.NarrowMargins = NarrowMarginsIndex;
            settings.BackgroundColor = BackgroundColorIndex;
            settings.Symbologies = GetSymbologiesFromListBox();
            _reader.SaveConfigurationIndexSettings( SelectedConfigIndex, settings);
        }

        private void ExecuteReloadConfigurationDatabaseCommand()
        {
            _reader.LoadConfigurationDatabase( _reader.ConfigurationPath, true);
            // calling this right afterward causes an operation timeout on the serial port
            //UpdateGuiWithCurrentIndexSettings();
        }

        private int GetSymbologiesFromListBox()
        {
            int symbology_mask = 0;
            for( int index=0; index<AvailableSymbologies.Count(); index++) {
                if( AvailableSymbologies[index].IsSelected)
                    symbology_mask |= (1 << index);
            }
            return symbology_mask;
        }

        private void ExecuteResetSymbologiesCommand()
        {
            SymbologyMask = 0;
            UpdateSymbologyMaskListBox();
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged( string property_name)
        {
            if( PropertyChanged != null)
                PropertyChanged( this, new PropertyChangedEventArgs( property_name));
        }

        #endregion

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if( _reader == null || !_reader.Connected)
                return;

            UpdateGuiWithCurrentIndexSettings();
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateGuiWithCurrentIndexSettings();
        }
    }
}
