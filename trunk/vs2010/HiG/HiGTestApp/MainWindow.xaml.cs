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
using System.Windows.Threading;
//using BioNex.Shared.LibraryInterfaces;

namespace HiGTestApp
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
        [Export("MainDispatcher")]
        public Dispatcher MainDispatcher
        {
            get { return this.Dispatcher; }
        }

        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();

            _timer = new DispatcherTimer();
            _timer.Tick += new EventHandler(_timer_Tick);
            _timer.Interval = new TimeSpan( 0, 0, 0, 0, 100);
            _timer.Start();

            this.DataContext = this;
            InitializeCommands();
            var catalog = new DirectoryCatalog(".");
            var container = new CompositionContainer(catalog);
            try
            {
                container.ComposeParts(this);
            }
            catch (CompositionException ex)
            {
                foreach (CompositionError e in ex.Errors)
                {
                    string description = e.Description;
                    string details = e.Exception.Message;
                    MessageBox.Show(description + ": " + details);
                }
                throw;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            base.OnClosed(e);
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        private void InitializeCommands()
        {
            ShowDiagnosticsCommand = new RelayCommand(ShowDiagnostics);
        }

        private void ShowDiagnostics()
        {
            Dictionary<string, string> props = new Dictionary<string, string>();
            props["simulate"] = "0";
            props["port"] = "5";
            props["configuration folder"] = "..\\..\\..\\HigIntegration\\config";
            props["host device name"] = "self";

            DeviceManagerDatabase.DeviceInfo device_info = new DeviceManagerDatabase.DeviceInfo("BioNex", "HiG", "HiG", false, props);
            Plugin.SetProperties(device_info);

            Plugin.ShowDiagnostics();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Plugin.Close();
            base.OnClosing(e);
        }

        public void AddError(ErrorData error)
        {
            Console.WriteLine( error.ErrorMessage);
        }

        public event ErrorEventHandler ErrorEvent;


        public IEnumerable<ErrorData> PendingErrors
        {
            get { return new List<ErrorData>(); }
        }

        public bool WaitForUserToHandleError
        {
            get { return false; }
        }

        public void Clear()
        {
        }
    }
}
