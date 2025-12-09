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
using System.Windows.Threading;
using System.IO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using BioNex.BumblebeeAlphaGUI.SchedulerInterface;
using BioNex.BumblebeeAlphaGUI.ViewModel;
using BioNex.Shared.HitpickXMLReader;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.Utils;
using BioNex.Shared.ReportingInterface;
using System.ComponentModel.Composition;
using BioNex.Shared.LabwareDatabase;
using System.ComponentModel.Composition.Hosting;
using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;
using log4net.Appender;

namespace BioNex.BumblebeeAlphaGUI
{
    /// <summary>
    /// Interaction logic for GUI.xaml
    /// </summary>
    public partial class GUI : Window//, ErrorDialog.ErrorPanelConsumer
    {
        private GanttDialog _gantt;
        private Diagnostics _diags;
        private Reporter _reporter = new Reporter();     
        private HardwareVisualizer _visualizer;

        [Import("ViewModel")]
        public MainViewModel _vm { get; set; }
        [Export("Preferences.rootnode")]
        public string PreferencesRootnode { get; set; }
        [Export("Preferences.path")]
        public string PreferencesPath { get; set; }
        [Export("LabwareDatabase.filename")]
        public string LabwareDatabaseFilename { get; set; }

        private static readonly ILog _log = LogManager.GetLogger(typeof(GUI));

        public GUI()
        {
            try {
                InitializeComponent();
            } 
            catch (Exception ex) {
                Debug.WriteLine("Could not Initialize main Bumblebee GUI Component.");
                throw (ex);
            }

            // icon
            Uri iconUri = new Uri("pack://application:,,,/Images/BioNex.ico", UriKind.RelativeOrAbsolute);
            this.Icon = BitmapFrame.Create(iconUri);

            // setup logging
            //BasicConfigurator.Configure();
            string logging_config_path = BioNex.Shared.Utils.FileSystem.GetAppPath() + "\\logging.xml";
            XmlConfigurator.Configure( new FileInfo( logging_config_path));
            log4net.Repository.ILoggerRepository repo = LogManager.GetRepository();
            foreach( log4net.Appender.IAppender appender in repo.GetAppenders()) {
                FileAppender fa = appender as FileAppender;
                if( fa == null)
                    continue;
                string datestamp = DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss");
                fa.File = String.Format("C:\\Engineering\\Logs\\{0}-debugLog.txt", datestamp);
                fa.ActivateOptions();
            }

            _log.Info( "Application started");

            PreferencesRootnode = "Preferences";
            PreferencesPath = BioNex.Shared.Utils.FileSystem.GetAppPath() + "\\preferences.xml";
            LabwareDatabaseFilename = "labware.s3db";

            // MEF
            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add( new DirectoryCatalog( "."));
            try {
                catalog.Catalogs.Add( new DirectoryCatalog( ".\\plugins"));
            } catch( System.IO.DirectoryNotFoundException) {
                // it's okay if there aren't any plugins (although you won't be able to do much without them)
            } catch( Exception ex) {
                MessageBox.Show( "Could not load modules in the plugins folder: " + ex.Message);
            }
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

            this.DataContext = _vm;
            _vm.Initialize();
        }

        /// <summary>
        /// Opens the diagnostics window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menu_diagnostics_Click(object sender, RoutedEventArgs e)
        {
            _diags = new Diagnostics( _vm);
            _diags.Owner = this;
            _diags.Show();
        }

        /// <summary>
        /// Displays the Gantt chart.  If you close it, you won't be able to
        /// reopen it (for now)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menu_ganttchart_Click( object sender, RoutedEventArgs e)
        {
            //! \todo maybe I should add a Gantt Dialog to the user control so the user doesn't
            //!       have to make a dialog for it each time.
            try {
                _gantt.Show();
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private void slider_DragCompleted( object sender, RoutedEventArgs e)
        {
            _vm.SetSystemSpeed( (int)((Slider)sender).Value);
        }

        /// <summary>
        /// Displays the open file dialog so the user can select a hitpick file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_select_file_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = "xml";
            dlg.Filter = "XML files (*.xml)|*.xml";
            if( dlg.ShowDialog() == true) {
                // got a filename, so set it in the GUI
                _vm.HitpickFilepath = dlg.FileName;
                _vm.SaveLastHitpickFile();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _gantt = new GanttDialog();
            _visualizer = new HardwareVisualizer();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            _gantt.Close();
            _visualizer.Close();
            _vm.SavePreferences();
            _vm.Close();
            _log.Info( "Application closed");
        }

        private void visualizer_Click(object sender, RoutedEventArgs e)
        {
            _visualizer.Show();
        }

        private void Diagnostics_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = e.OriginalSource as MenuItem;
            if( mi == null)
                return;
            string plugin_name = mi.Header.ToString();
            UserControl uc = _vm.GetDeviceDiagnosticsPanel( plugin_name);
            if( uc == null)
                return;
            DeviceDiagnosticsPanelHost host = new DeviceDiagnosticsPanelHost();
            host.Content = uc;
            host.Show();
        }

        private void menu_setup_Click(object sender, RoutedEventArgs e)
        {
            Setup setup_dialog = new Setup( _vm);
            bool? save_settings = setup_dialog.ShowDialog();
            if( save_settings == true)
                _vm.SaveSettingsFiles();
            setup_dialog.Close();
        }
    }
}

