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
using System.Windows.Shapes;
using BioNex.Shared.Utils;

namespace HiGIntegrationTestApp
{
    /// <summary>
    /// Interaction logic for EepromSettings.xaml
    /// </summary>
    public partial class EepromSettings : Window
    {
        private BioNex.Hig.IHigModel _model;
        // save the initial values when we open the dialog so we can detect if the user changed anything
        private int InitialDoorOpenPos;
        private int InitialDoorClosedPos;
        private int InitialBucket1Position;
        private int InitialBucket2Offset;
        private int InitialImbalanceThreshold;

        public int DoorOpenPosition { get; set; }
        public int DoorClosedPosition { get; set; }
        public int Bucket1Position { get; set; }
        public short Bucket2Offset { get; set; }
        public int ImbalanceThreshold { get; set; }
        public bool ImbalanceEnabled { get { return _model != null ? _model.SupportsImbalance : false; } }
        public string ImbalanceToolTip { get; private set; }
        public string CalibrateToolTip { get; private set; }

        public SimpleRelayCommand OkCommand { get; set; }
        public SimpleRelayCommand CancelCommand { get; set; }
        public SimpleRelayCommand CalibrateImbalanceCommand { get; set; }

        public bool Calibrating { get; internal set; }

        public EepromSettings( BioNex.Hig.IHigModel model)
        {
            _model = model;

            InitializeComponent();
            InitializeCommands();
            this.DataContext = this;

            InitialDoorOpenPos = DoorOpenPosition = _model.ShieldOpenPosition;
            InitialDoorClosedPos = DoorClosedPosition = _model.ShieldClosedPosition;
            InitialBucket1Position = Bucket1Position = _model.Bucket1Position;
            InitialBucket2Offset = Bucket2Offset = _model.Bucket2Offset;
            InitialImbalanceThreshold = ImbalanceThreshold = _model.ImbalanceThreshold;

            ImbalanceToolTip = _model.SupportsImbalance ? "Imbalance threshold value" : "Imbalance detection is only available on spindle firmware v1.5 and later";
            CalibrateToolTip = _model.SupportsImbalance ? "Run imbalance threshold calibration" : "Imbalance calibration is only available on spindle firmware v1.5 and later";
        }

        private void InitializeCommands()
        {
            OkCommand = new SimpleRelayCommand( () => {
                // if settings have changed, prompt the user.  otherwise, exit
                if( !SettingsChanged()) {
                    Close();
                    return;
                }
            
                MessageBoxResult answer = MessageBox.Show( "Settings have changed.  Would you like to save your changes and reinitialize the device?", "Save changes?", MessageBoxButton.YesNo);
                if( answer == MessageBoxResult.No)
                    MessageBox.Show( "No changes have been saved.");
                SaveSettingsAndHome();
                Close();
            }, () => { return !Calibrating; });

            CancelCommand = new SimpleRelayCommand( Close, () => { return !Calibrating; });

            // CalibrateImbalanceCommand needs a HiG object because we need to write to the
            // imbalance value in RAM (imb_ampl_max), and not the EEPROM value which is
            // what the ImbalanceThreshold property changes.
            CalibrateImbalanceCommand = new SimpleRelayCommand( () => {
                System.Threading.Tasks.Task task = System.Threading.Tasks.Task.Factory.StartNew( () => {
                    Calibrating = true;
                    HourglassWindow hg = HourglassWindow.ShowHourglassWindow( Dispatcher, this , "Imbalance Calibration in Progress", true, 12);
                    (_model as BioNex.HiGIntegration.HiG).ExecuteCalibrateImbalance();
                    HourglassWindow.CloseHourglassWindow( Dispatcher, hg);       
                    Calibrating = false;
                });
            }, () => { return !Calibrating; });
        }

        public bool SettingsChanged()
        {
            return InitialDoorClosedPos != DoorClosedPosition || InitialDoorOpenPos != DoorOpenPosition ||
                   InitialBucket1Position != Bucket1Position || InitialBucket2Offset != Bucket2Offset ||
                   InitialImbalanceThreshold != ImbalanceThreshold;
        }

        public void SaveSettingsAndHome()
        {
            if( InitialDoorClosedPos != DoorClosedPosition)
                _model.ShieldClosedPosition = DoorClosedPosition;

            if( InitialDoorOpenPos != DoorOpenPosition)
                _model.ShieldOpenPosition = DoorOpenPosition;

            if( InitialBucket1Position != Bucket1Position)
                _model.Bucket1Position = Bucket1Position;

            if( InitialBucket2Offset != Bucket2Offset)
                _model.Bucket2Offset = Bucket2Offset;

            if( _model.SupportsImbalance) {
                if( InitialImbalanceThreshold != ImbalanceThreshold)
                    _model.ImbalanceThreshold = ImbalanceThreshold;
            }

            _model.ShieldAxis.Home( true);
            _model.SpindleAxis.Home( true);
        }
    }
}
