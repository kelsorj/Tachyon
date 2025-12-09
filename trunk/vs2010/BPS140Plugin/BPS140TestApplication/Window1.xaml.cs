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
using System.ComponentModel;
using GalaSoft.MvvmLight.Command;
using BioNex.BPS140Plugin;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using BioNex.Shared.DeviceInterfaces;

namespace BPS140TestApplication
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window, INotifyPropertyChanged
    {
        private string port_name_;
        public string PortName
        {
            get { return port_name_; }
            set {
                port_name_ = value;
                OnPropertyChanged( "PortName");
            }
        }

        private string rack1_config_;
        public string Rack1Configuration
        {
            get { return rack1_config_; }
            set {
                rack1_config_ = value;
                OnPropertyChanged( "Rack1Configuration");
            }
        }

        private string rack2_config_;
        public string Rack2Configuration
        {
            get { return rack2_config_; }
            set {
                rack2_config_ = value;
                OnPropertyChanged( "Rack2Configuration");
            }
        }

        public RelayCommand ConnectCommand { get; set; }
        public RelayCommand ShowDiagnosticsCommand { get; set; }
        public RelayCommand UpdateConfigurationCommand { get; set; }

        [Import(typeof(DeviceInterface))]
        private BPS140 Device { get; set; }

        public Window1()
        {
            LoadComponents();

            // set the device properties
            Device.SetProperties( CreateDeviceProperties());

            InitializeComponent();

            ConnectCommand = new RelayCommand( ExecuteConnect);
            ShowDiagnosticsCommand = new RelayCommand ( ExecuteShowDiagnostics);
            UpdateConfigurationCommand = new RelayCommand( ExecuteUpdateConfiguration);

            this.DataContext = this;
        }

        private void LoadComponents()
        {
            try {
                AggregateCatalog catalog = new AggregateCatalog();
                catalog.Catalogs.Add( new DirectoryCatalog( "."));
                // need to also add this assembly to the catalog, or we won't be able to import the ViewModel
                catalog.Catalogs.Add( new AssemblyCatalog( typeof(App).Assembly));
                CompositionContainer container = new CompositionContainer( catalog);
                try {
                    container.ComposeParts( this);
                } catch( CompositionException ex) {
                    foreach( CompositionError e in ex.Errors) {
                        string description = e.Description;
                        string details = e.Exception.Message;
                    }
                    throw;            
                } catch( System.Reflection.ReflectionTypeLoadException ex) {
                    foreach( Exception e in ex.LoaderExceptions) {
                        MessageBox.Show( e.Message);
                    }
                } catch( Exception ex) {
                    MessageBox.Show( ex.Message);
                }
            } catch( System.IO.DirectoryNotFoundException) {
                // couldn't find a plugins folder, so nothing else to do in this method
            } catch( Exception ex) {
                MessageBox.Show( ex.Message);
            }
        }

        private DeviceManagerDatabase.DeviceInfo CreateDeviceProperties()
        {
            Dictionary<string,string> properties = new Dictionary<string,string>();
            properties.Add( "configuration folder", "");
            properties.Add( "simulate", "1");
            properties.Add( "port", "2");
            properties.Add( "rack configuration, side 1", "14, 14, 14, 14, 14");
            properties.Add( "rack configuration, side 2", "14, 14, 14, 14, 14");
            
            return new DeviceManagerDatabase.DeviceInfo( Device.Manufacturer, Device.ProductName, "BPS140", false, properties);
        }

        public void ExecuteConnect()
        {
            Device.Connect();
        }

        public void ExecuteShowDiagnostics()
        {
            Device.ShowDiagnostics();
        }

        public void ExecuteUpdateConfiguration()
        {
            
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for
        /// a given property.
        /// </summary>
        /// <param name="propertyName">The name of the changed property.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
