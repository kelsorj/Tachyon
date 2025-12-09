using System.Windows.Controls;
using GalaSoft.MvvmLight.Command;
using Microsoft.WindowsAPICodePack.Dialogs;
using BioNex.Shared.Utils;
using System.Windows.Input;
using System.ComponentModel;

namespace BioNex.LiquidLevelDevice
{
    /// <summary>
    /// Interaction logic for OutputConfigPanel.xaml
    /// </summary>
    public partial class OutputConfigPanel : UserControl, INotifyPropertyChanged
    {
        public OutputConfigPanel()
        {
            InitializeComponent();
            DataContext = this;

            BrowseCommand = new RelayCommand(ExecuteBrowseCommand);
        }

        ILLSensorPlugin _plugin;
        public ILLSensorPlugin Plugin
        {
            set { _plugin = (ILLSensorPlugin)value; SetModel(); }
        }

        ILLSensorModel _model;
        private void SetModel()
        {
            _model = _plugin.Model;
        }

        public RelayCommand BrowseCommand { get; set; }
        public string OutputFilePath { get { return _model.Properties.GetString(LLProperties.OutputFilePath); } set { _model.Properties[LLProperties.OutputFilePath] = value.ToString(); _model.FireSavePropertiesEvent(); OnPropertyChanged("OutputFilePath"); } }
        public string Delimeter { get { return _model.Properties.GetString(LLProperties.Delimeter); } set { _model.Properties[LLProperties.Delimeter] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public bool NewFilePerCap { get { return _model.Properties.GetBool(LLProperties.NewFilePerCapture); } set { _model.Properties[LLProperties.NewFilePerCapture] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public bool LogHeader { get { return _model.Properties.GetBool(LLProperties.LogHeader); } set { _model.Properties[LLProperties.LogHeader] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public bool LogTimestamp { get { return _model.Properties.GetBool(LLProperties.LogTimestamp); } set { _model.Properties[LLProperties.LogTimestamp] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public bool LogRunCounter { get { return _model.Properties.GetBool(LLProperties.LogRunCounter); } set { _model.Properties[LLProperties.LogRunCounter] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public bool LogChannel { get { return _model.Properties.GetBool(LLProperties.LogChannel); } set { _model.Properties[LLProperties.LogChannel] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public bool LogColumn { get { return _model.Properties.GetBool(LLProperties.LogColumn); } set { _model.Properties[LLProperties.LogColumn] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public bool LogRow { get { return _model.Properties.GetBool(LLProperties.LogRow); } set { _model.Properties[LLProperties.LogRow] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public bool LogX { get { return _model.Properties.GetBool(LLProperties.LogX); } set { _model.Properties[LLProperties.LogX] = value.ToString(); _model.FireSavePropertiesEvent(); } }
        public bool LogY { get { return _model.Properties.GetBool(LLProperties.LogY); } set { _model.Properties[LLProperties.LogY] = value.ToString(); _model.FireSavePropertiesEvent(); } }

        private void ExecuteBrowseCommand()
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.InitialDirectory = OutputFilePath.IsAbsolutePath() ? OutputFilePath : OutputFilePath.ToAbsoluteAppPath();
            var result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Cancel)
                return;

            OutputFilePath = dialog.FileName;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string property_name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property_name));
        }

        #endregion
    }
}
