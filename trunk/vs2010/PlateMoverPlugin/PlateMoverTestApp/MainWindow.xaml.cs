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
using GalaSoft.MvvmLight.Command;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.IError;

namespace PlateMoverTestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [Export(typeof(IError))]
    public partial class MainWindow : Window, IError
    {
        public RelayCommand ShowDiagnosticsCommand { get; set; }
        [Import]
        public DeviceInterface Plugin { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            InitializeCommands();

            var catalog = new DirectoryCatalog( ".");
            var container = new CompositionContainer( catalog);
            try {
                container.ComposeParts( this);
            } catch( CompositionException ex) {
                foreach( CompositionError e in ex.Errors) {
                    string description = e.Description;
                    string details = e.Exception.Message;
                    MessageBox.Show( description + ": " + details);
                }
                throw;
            }
        }

        private void InitializeCommands()
        {
            ShowDiagnosticsCommand = new RelayCommand( ShowDiagnostics);
        }

        private void ShowDiagnostics()
        {
            // send the device properties
            Dictionary<string,string> props = new Dictionary<string,string>();
            props["simulate"] = "1";
            props["port"] = "1";
            props["configuration folder"] = "\\config";
            props["host device name"] = "self";

            DeviceManagerDatabase.DeviceInfo device_info = new DeviceManagerDatabase.DeviceInfo( "BioNex", "PlateMover", "PlateMover", false, props);
            Plugin.SetProperties( device_info);

            Plugin.ShowDiagnostics();
        }

        #region IError Members

        public void AddError(ErrorData error)
        {
        }

        public event ErrorEventHandler ErrorEvent { add {} remove {} }

        public IEnumerable< ErrorData> PendingErrors { get { return new List< ErrorData>(); } }

        public void Clear() {}

        public bool WaitForUserToHandleError { get { return true; } }

        #endregion
    }
}
