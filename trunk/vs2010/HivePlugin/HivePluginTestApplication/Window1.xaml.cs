using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Windows;
using System.Windows.Threading;
using BioNex.Shared.ErrorHandling;
using BioNex.Shared.IError;

namespace HivePluginTestApplication
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    [Export(typeof(IError))]
    public partial class Window1 : Window, IError
    {
        /*
        [Import]
        public BioNex.Shared.LibraryInterfaces.IError ErrorSink { get; set; }
         */
        [Import]
        public BioNex.Shared.DeviceInterfaces.DeviceInterface Plugin { get; set; }
        /*
        [Import]
        public BioNex.Shared.LibraryInterfaces.IPreferences Preferences { get; set; }
        [Export("Preferences.rootnode")]
        public string PreferencesRootnode { get; set; }
        [Export("Preferences.path")]
        public string PreferencesPath { get; set; }
         */
        [Export("LabwareDatabase.filename")]
        public string LabwareDatabaseFilename { get; set; }

        public ObservableCollection<ErrorPanel> Errors { get; set; }
        private List<ErrorData> _errors;
        private DispatcherTimer _timer;

        public Window1()
        {
            InitializeComponent();
            string path = BioNex.Shared.Utils.FileSystem.GetAppPath() + "\\hive_testapp_preferences.xml";
            this.DataContext = this;
            _errors = new List<ErrorData>();
            Errors = new ObservableCollection<ErrorPanel>();

            /*
            Preferences = new BioNex.Shared.ApplicationPreferences.Preferences( "ApplicationPreferences", path);
            PreferencesRootnode = "ApplicationPreferences";
            PreferencesPath = path;
             */
            LabwareDatabaseFilename = "labware.s3db";
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

            _timer = new DispatcherTimer();
            _timer.Tick += new EventHandler(_timer_Tick);
            _timer.Interval = new TimeSpan( 0, 0, 0, 0, 250);
            _timer.Start();
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            for( int i=_errors.Count - 1; i>=0; i--) {
                Errors.Add( new ErrorPanel( _errors[i]));
                _errors.RemoveAt( i);
            }
            _timer.Start();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // window doesn't display properly for some reason!!!
            //_plugin.ShowDiagnostics();
            PanelHost host = new PanelHost();
            host.Content = Plugin.GetDiagnosticsPanel();
            host.Show();
        }

        public void AddError( ErrorData error)
        {
            _errors.Add( error);
        }

        public event ErrorEventHandler ErrorEvent;
        public IEnumerable<ErrorData> PendingErrors { get { return new List<ErrorData>(); } }
        public bool WaitForUserToHandleError { get { return true; } }
        public void Clear() {}
    }
}
