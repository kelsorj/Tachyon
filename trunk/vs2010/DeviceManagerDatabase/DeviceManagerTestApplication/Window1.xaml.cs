using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using BioNex;
using BioNex.SynapsisPrototype;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using BioNex.Shared.DeviceInterfaces;

namespace DeviceManagerTestApplication
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        [Import]
        DeviceManager _dm { get; set; }
        [Export("DeviceManager.filename")]
        public string DeviceManagerFilename { get; set; }
        [Export("MEFContainer")]
        public CompositionContainer _container;

        public ObservableCollection<DeviceInterface> Devices { get; set; }

        public Window1()
        {
            InitializeComponent();

            this.DataContext = this;
            string exe_path = BioNex.Shared.Utils.FileSystem.GetAppPath();
            DeviceManagerFilename = exe_path + "\\testapp_devices.s3db";

            Devices = new ObservableCollection<DeviceInterface>();
            
            ShowDeviceManagerGUI();
        }

        public void ShowDeviceManagerGUI()
        {
            // MEF
            try {
                AggregateCatalog catalog = new AggregateCatalog();
                catalog.Catalogs.Add( new DirectoryCatalog( "."));
                catalog.Catalogs.Add( new AssemblyCatalog( typeof(App).Assembly));
                _container = new CompositionContainer( catalog);
                try {
                    _container.ComposeParts( this);
                } catch( CompositionException ex) {
                    foreach( CompositionError e in ex.Errors) {
                        string description = e.Description;
                        string details = e.Exception.Message;
                        MessageBox.Show( description + ": " + details);
                    }
                    throw;            
                } catch( System.Reflection.ReflectionTypeLoadException ex) {
                    foreach( Exception e in ex.LoaderExceptions)
                        MessageBox.Show( e.Message);
                }
            } catch( System.IO.DirectoryNotFoundException) {
                // couldn't find a plugins folder, so nothing else to do in this method
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }

            _dm.LoadDeviceFile();
            IEnumerable<DeviceInterface> devices = _dm.GetAllDevices();
            foreach( DeviceInterface di in devices) {
                Devices.Add( di);
            }
        }
    }
}
