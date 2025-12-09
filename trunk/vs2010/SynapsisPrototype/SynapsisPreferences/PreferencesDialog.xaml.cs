using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using BioNex.Shared.Utils;
using GalaSoft.MvvmLight.Command;

namespace BioNex.SynapsisPrototype
{
    /// <summary>
    /// Interaction logic for PreferencesDialog.xaml
    /// </summary>
    [Export]
    [PartCreationPolicy(System.ComponentModel.Composition.CreationPolicy.Shared)]
    public partial class PreferencesDialog : Window, INotifyPropertyChanged
    {
        private Preferences _preferences;
        private string _preferences_path;

        // these properties are for GUI interaction
        public int LowBatteryThresholdPercentage { get; set; }
        public string LightIODevice { get; set; }
        public bool PasswordProtectDiagnostics { get; set; }
        public string PreProtocolMessageFilename { get; set; }
        public bool ReturnTipBoxToOriginalLocation { get; set; }
        public int RobotDisableResetBit { get; set; }
        public bool RunForever { get; set; }
        public string SelectedCustomerGui { get; set; }

        public RelayCommand OkCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }

        public PreferencesDialog()
        {
            InitializeComponent();
            InitializeCommands();
            this.DataContext = this;
            this.Width = 400;
            this.Height = 400;

            LoadPreferencesFromFile();
        }

        private void LoadPreferencesFromFile()
        {
            string exe_path = BioNex.Shared.Utils.FileSystem.GetAppPath();
            _preferences_path = exe_path + "\\preferences.xml";
            _preferences = BioNex.Shared.Utils.FileSystem.LoadXmlConfiguration<Preferences>(_preferences_path, CreateDefaultPreferences());
            // now load all of the properties, which will be accessed by the GUI and other objects in Synapsis
            var temp = _preferences.PreferenceProperties.Where(x => x.Name == PreferenceStrings.LowBatteryThresholdPercentage);
            LowBatteryThresholdPercentage = (temp == null) ? 20 : temp.First().Value.ToInt();
            temp = _preferences.PreferenceProperties.Where(x => x.Name == PreferenceStrings.LightIODevice);
            LightIODevice = (temp == null) ? "IODevice" : temp.First().Value;
            temp = _preferences.PreferenceProperties.Where(x => x.Name == PreferenceStrings.PasswordProtectDiagnostics);
            PasswordProtectDiagnostics = (temp == null) ? false : temp.First().Value.ToBool();
            temp = _preferences.PreferenceProperties.Where(x => x.Name == PreferenceStrings.PreProtocolMessageFilename);
            PreProtocolMessageFilename = (temp == null) ? "" : temp.First().Value;
            temp = _preferences.PreferenceProperties.Where(x => x.Name == PreferenceStrings.ReturnTipBoxToOriginalLocation);
            ReturnTipBoxToOriginalLocation = (temp == null) ? true : temp.First().Value.ToBool();
            temp = _preferences.PreferenceProperties.Where(x => x.Name == PreferenceStrings.RobotDisableResetBit);
            RobotDisableResetBit = (temp == null) ? 3 : temp.First().Value.ToInt();
            temp = _preferences.PreferenceProperties.Where(x => x.Name == PreferenceStrings.RunForever);
            RunForever = (temp == null) ? false : temp.First().Value.ToBool();
            temp = _preferences.PreferenceProperties.Where(x => x.Name == PreferenceStrings.SelectedCustomerGui);
            SelectedCustomerGui = (temp == null) ? "" : temp.First().Value;
        }

        private void InitializeCommands()
        {
            OkCommand = new RelayCommand( () => { SavePreferences(); this.Hide(); } );
            CancelCommand = new RelayCommand( () => { this.Hide(); LoadPreferencesFromFile(); } );
        }

        private static Preferences CreateDefaultPreferences()
        {
            Preferences prefs = new Preferences();
            prefs.PreferenceProperties.Add( new Preferences.PreferenceProperty { Name=PreferenceStrings.LowBatteryThresholdPercentage, Type="integer", Value="10" } );
            prefs.PreferenceProperties.Add( new Preferences.PreferenceProperty { Name=PreferenceStrings.LightIODevice, Type="string", Value="IODevice" } );
            prefs.PreferenceProperties.Add( new Preferences.PreferenceProperty { Name=PreferenceStrings.PasswordProtectDiagnostics, Type="boolean", Value="false" } );
            prefs.PreferenceProperties.Add( new Preferences.PreferenceProperty { Name=PreferenceStrings.PreProtocolMessageFilename, Type="string", Value="pre-protocol-message.txt" } );
            prefs.PreferenceProperties.Add( new Preferences.PreferenceProperty { Name=PreferenceStrings.ReturnTipBoxToOriginalLocation, Type="boolean", Value="true" } );
            prefs.PreferenceProperties.Add( new Preferences.PreferenceProperty { Name=PreferenceStrings.RobotDisableResetBit, Type="integer", Value="3" } );
            prefs.PreferenceProperties.Add( new Preferences.PreferenceProperty { Name=PreferenceStrings.RunForever, Type="boolean", Value="false" } );
            prefs.PreferenceProperties.Add( new Preferences.PreferenceProperty { Name=PreferenceStrings.SelectedCustomerGui, Type="string", Value="" } );
            return prefs;
        }

        /// <summary>
        /// Saves application preferences in preferences.xml
        /// </summary>
        public void SavePreferences()
        {
            Preferences prefs = new Preferences();
            prefs.PreferenceProperties.Add( new Preferences.PreferenceProperty { Name=PreferenceStrings.LowBatteryThresholdPercentage, Type="integer", Value=LowBatteryThresholdPercentage.ToString() } );
            prefs.PreferenceProperties.Add( new Preferences.PreferenceProperty { Name=PreferenceStrings.LightIODevice, Type="string", Value=LightIODevice } );
            prefs.PreferenceProperties.Add( new Preferences.PreferenceProperty { Name=PreferenceStrings.PasswordProtectDiagnostics, Type="boolean", Value="false" } );
            prefs.PreferenceProperties.Add( new Preferences.PreferenceProperty { Name=PreferenceStrings.PreProtocolMessageFilename, Type="string", Value=PreProtocolMessageFilename } );
            prefs.PreferenceProperties.Add( new Preferences.PreferenceProperty { Name=PreferenceStrings.ReturnTipBoxToOriginalLocation, Type="boolean", Value=(ReturnTipBoxToOriginalLocation ? "true" : "false") } );
            prefs.PreferenceProperties.Add( new Preferences.PreferenceProperty { Name=PreferenceStrings.RobotDisableResetBit, Type="integer", Value=RobotDisableResetBit.ToString() } );
            prefs.PreferenceProperties.Add( new Preferences.PreferenceProperty { Name=PreferenceStrings.RunForever, Type="boolean", Value=(RunForever ? "true" : "false") } );
            prefs.PreferenceProperties.Add( new Preferences.PreferenceProperty { Name=PreferenceStrings.SelectedCustomerGui, Type="string", Value=SelectedCustomerGui } );
            BioNex.Shared.Utils.FileSystem.SaveXmlConfiguration<Preferences>( prefs, _preferences_path);
        }


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
