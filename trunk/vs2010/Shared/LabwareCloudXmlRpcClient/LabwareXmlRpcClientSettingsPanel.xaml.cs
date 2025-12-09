using System.ComponentModel.Composition;
using System.Windows;
using BioNex.Shared.LibraryInterfaces;
using GalaSoft.MvvmLight.Command;

namespace BioNex.Shared.LabwareCloudXmlRpcClient
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class LabwareXmlRpcClientSettingsPanel : Window
    {
        readonly LabwareXmlRpcClient _client;

        public RelayCommand SyncCommand { get; private set; }
        public RelayCommand PublishCommand { get; private set; }

        public string UrlLabel { get { return _client == null ? "not configured" : _client.url; } }

        public LabwareXmlRpcClientSettingsPanel(LabwareXmlRpcClient client)
        {
            _client = client;
            SyncCommand = new RelayCommand(() => { _client.DoSync(); }, () => { return _client != null; });
            PublishCommand = new RelayCommand(() => { _client.DoPublish(); }, () => { return _client != null; });

            DataContext = this;
            InitializeComponent();
        }
    }

    /// <summary>
    /// ui proxy class used by Synapsis to present menu item and show ui.  Also added Configure method so Synapsis can pass the xmlrpc client object
    /// </summary>
    [Export(typeof(LabwareCloudSystemSetup))]
    [Export(typeof(ISystemSetupEditor))]
    public class LabwareCloudSystemSetup : ISystemSetupEditor
    {
        LabwareXmlRpcClient _client;

        public string Name
        {
            get
            {
                return "Labware-Cloud Settings";
            }
        }

        public void ShowTool()
        {
            var panel = new LabwareXmlRpcClientSettingsPanel(_client);
            panel.ShowDialog();
            panel.Close();
        }

        public void Configure(LabwareXmlRpcClient client)
        {
            _client = client;
        }
    }
}