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
using System.Collections.ObjectModel;
using System.Windows.Threading;
using BioNex.Shared;
using BioNex.Shared.DeviceInterfaces;
using System.ComponentModel.Composition;
using BioNex.Shared.IError;
using System.Diagnostics;
using System.ComponentModel.Composition.Hosting;

namespace VStackPluginTestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [Export(typeof(IError))]
    public partial class MainWindow : Window, IError
    {
        [Import(typeof(DeviceInterface))]
        DeviceInterface _plugin;

        [Export("DeviceManager.filename")]
        public string DeviceManagerFilename { get; set; }
        [Export("LabwareDatabase.filename")]
        public string LabwareDatabaseFilename { get; set; }
        [Export("MEFContainer")]
        public CompositionContainer Container=null;

        [Export("MainDispatcher")]
        public Dispatcher MainDispatcher
        {
            get { return this.Dispatcher; }
        }

        public MainWindow()
        {
            DeviceManagerFilename = "config\\devices.s3db";
            LabwareDatabaseFilename = "config\\labware.s3db";

            // MEF
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //_plugin = new VStackPlugin.VStackPlugin();
            Dictionary<string,string> properties = new Dictionary<string,string>();
            properties["Profile"] = "Stacker1 profile";
            DeviceManagerDatabase.DeviceInfo device_info = new DeviceManagerDatabase.DeviceInfo( "Velocity11", "VStack", "VStack instance", false, properties);
            uc_wrapper.SetPlugin( _plugin, device_info);
        }

        private void Upstack_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string,object> parameters = new Dictionary<string,object> { { VStackPlugin.VStackPlugin.CommandParameterNames.LabwareName, "todo" },
                                                                                   { VStackPlugin.VStackPlugin.CommandParameterNames.PlateFlags, IWorksDriverLib.PlateFlagsType.STACK_NORMAL_PLATES } };

            bool ret = _plugin.ExecuteCommand( VStackPlugin.VStackPlugin.Commands.Upstack.ToString(), parameters);
                                                                    
            if( !ret) {
                MessageBox.Show( "error");
            }
        }

        private void Downstack_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string,object> parameters = new Dictionary<string,object> { { VStackPlugin.VStackPlugin.CommandParameterNames.LabwareName, "todo" },
                                                                                   { VStackPlugin.VStackPlugin.CommandParameterNames.PlateFlags, IWorksDriverLib.PlateFlagsType.STACK_NORMAL_PLATES } };

            bool ret = (_plugin as DeviceInterface).ExecuteCommand( VStackPlugin.VStackPlugin.Commands.Downstack.ToString(), parameters);
                                                                    
            if( !ret) {
                MessageBox.Show( "error");
            }
        }
        #region IError Members

        public void AddError(ErrorData error)
        {
            Debug.WriteLine( error.ErrorMessage);
        }


        public event ErrorEventHandler ErrorEvent;

        #endregion

        private void ReleaseStack_Click(object sender, RoutedEventArgs e)
        {



        }


        private void LoadStack_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

