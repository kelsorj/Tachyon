using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BioNex.Shared.DeviceInterfaces;
using log4net;

namespace BioNex.LiquidLevelDevice
{
    /// <summary>
    /// Interaction logic for DiagnosticsPanel.xaml
    /// </summary>
    public partial class DiagnosticsPanel : UserControl, INotifyPropertyChanged
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(DiagnosticsPanel));

        public DiagnosticsPanel()
        {
            InitializeComponent();
            EngineeringTabVisibility = Visibility.Collapsed;
            DataContext = this;
        }
        public DiagnosticsPanel(DeviceInterface plugin)
            : this()
        {
            Plugin = plugin;
            Tabs.Focus();
        }

        ILLSensorPlugin _plugin;
        public DeviceInterface Plugin
        {
            set { _plugin = (ILLSensorPlugin)value; Status.Plugin = value; Engineering.Plugin = _plugin; SetupAndTesting.Plugin = value; ScanProgress.Plugin = _plugin; VolumeMap.Plugin = value; OutputConfig.Plugin = _plugin; }
        }

        Visibility _engineering_tab_visibility;
        public Visibility EngineeringTabVisibility { get { return _engineering_tab_visibility; } set { _engineering_tab_visibility = value; OnPropertyChanged("EngineeringTabVisibility"); } }

        void UserControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.B)
            {
                const ModifierKeys test = (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift);
                if ((Keyboard.Modifiers & test) == test)
                {
                    EngineeringTabVisibility = EngineeringTabVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                    var owner = Window.GetWindow(this);
                    if (EngineeringTabVisibility == Visibility.Visible)
                    {
                        Tabs.SelectedIndex = 4;
                        owner.Width = System.Math.Max(owner.Width, 765);
                        owner.Height = System.Math.Max(owner.Height, (_plugin.Model as LLSensorModel).HasRAxis ? 675 : 540);
                    }
                    else
                    {
                        if (Tabs.SelectedIndex > 3)
                            Tabs.SelectedIndex = 0;
                        owner.Width = System.Math.Min(owner.Width, 640);
                        owner.Height = System.Math.Min(owner.Height, 425);
                    }
                }
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

    }
}