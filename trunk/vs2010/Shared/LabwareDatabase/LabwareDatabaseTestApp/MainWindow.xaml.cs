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
using BioNex.Shared.LabwareDatabase;
using BioNex.Shared.Utils;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.DeviceInterfaces;

namespace LabwareDatabaseTestApp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	[Export( typeof( ExternalDataRequesterInterface))]
	public partial class MainWindow : Window, ExternalDataRequesterInterface
	{
		[Import]
		private ILabwareDatabase _labware_database { get; set; }
		[Export("LabwareDatabase.filename")]
		public string LabwareDatabaseFilename { get; set; }

		public MainWindow()
		{
			InitializeComponent();

			LabwareDatabaseFilename = (@"config\labware.s3db").ToAbsoluteAppPath();

			var catalog = new AggregateCatalog();
            catalog.Catalogs.Add( new DirectoryCatalog( "."));
            catalog.Catalogs.Add( new AssemblyCatalog( typeof(App).Assembly));
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

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			_labware_database.ShowEditor();
		}
	
		#region ExternalDataRequesterInterface Members

		public SystemStartupCheckInterface.SafeToMoveDelegate  SafeToMove
		{
			get 
			{ 
				throw new NotImplementedException(); 
			}
			set 
			{ 
				throw new NotImplementedException(); 
			}
		}

		public int  GetDeviceTypeId(string company_name, string product_name)
		{
			if( company_name == "BioNex" && product_name == "MockDevice1")
                return 1;
            else if( company_name == "BioNex" && product_name == "MockDevice2")
                return 2;
            else
                return 0;
		}

		public IEnumerable<DeviceInterface>  GetDeviceInterfaces()
		{
			return new List<DeviceInterface> { new MockDeviceInterface1(), new MockDeviceInterface2() };
		}

		public IEnumerable<RobotInterface>  GetRobotInterfaces()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<PlateStorageInterface>  GetPlateStorageInterfaces()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<IOInterface>  GetIOInterfaces()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<SystemStartupCheckInterface>  GetSystemStartupCheckInterfaces()
		{
			throw new NotImplementedException();
		}

        public IEnumerable<AccessibleDeviceInterface> GetAccessibleDeviceInterfaces()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<SafetyInterface> GetSafetyInterfaces()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<StackerInterface> GetStackerInterfaces()
        {
            throw new NotImplementedException();
        }

        public Func<bool> ReadSensorCallback(string device_name, string location_name)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<DockablePlateStorageInterface> GetDockablePlateStorageInterfaces()
        {
            throw new NotImplementedException();
        }

        #endregion
    }

	internal class MockDeviceInterface1 : DeviceInterface
	{
		#region DeviceInterface Members

		public UserControl  GetDiagnosticsPanel()
		{
			throw new NotImplementedException();
		}

		public void  SetProperties(DeviceManagerDatabase.DeviceInfo device_info)
		{
			throw new NotImplementedException();
		}

		public void  ShowDiagnostics()
		{
			throw new NotImplementedException();
		}

		public void  Connect()
		{
			throw new NotImplementedException();
		}

		public bool  Connected
		{
			get { throw new NotImplementedException(); }
		}

		public void  Home()
		{
			throw new NotImplementedException();
		}

		public bool  IsHomed 
        {
            get
            {
                throw new NotImplementedException();
            }
		}

		public void  Close()
		{
			throw new NotImplementedException();
		}

		public bool  ExecuteCommand(string command, Dictionary<string,object> parameters)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<string>  GetCommands()
		{
			throw new NotImplementedException();
		}

		public void  Abort()
		{
			throw new NotImplementedException();
		}

		public void  Pause()
		{
			throw new NotImplementedException();
		}

		public void  Resume()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<string>  GetPlateLocationNames()
		{
			throw new NotImplementedException();
		}

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public bool ExecuteCommand(string command, IDictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IPluginIdentity Members

        public string Name
        {
            get { throw new NotImplementedException(); }
        }

        public string ProductName
        {
            get { return "Mock Device 1"; }
        }

        public string Manufacturer
        {
            get { return "BioNex"; }
        }

        public string Description
        {
            get { throw new NotImplementedException(); }
        }

        #endregion
    }

	internal class MockDeviceInterface2 : DeviceInterface
	{
		#region DeviceInterface Members

		public UserControl  GetDiagnosticsPanel()
		{
			throw new NotImplementedException();
		}

		public void  SetProperties(DeviceManagerDatabase.DeviceInfo device_info)
		{
			throw new NotImplementedException();
		}

		public void  ShowDiagnostics()
		{
			throw new NotImplementedException();
		}

		public void  Connect()
		{
			throw new NotImplementedException();
		}

		public bool  Connected
		{
			get { throw new NotImplementedException(); }
		}

		public void  Home()
		{
			throw new NotImplementedException();
		}

		public bool  IsHomed
		{
            get
            {
                throw new NotImplementedException();
            }
		}

		public void  Close()
		{
			throw new NotImplementedException();
		}

		public bool  ExecuteCommand(string command, Dictionary<string,object> parameters)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<string>  GetCommands()
		{
			throw new NotImplementedException();
		}

		public void  Abort()
		{
			throw new NotImplementedException();
		}

		public void  Pause()
		{
			throw new NotImplementedException();
		}

		public void  Resume()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<string>  GetPlateLocationNames()
		{
			throw new NotImplementedException();
		}

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public bool ExecuteCommand(string command, IDictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region IPluginIdentity Members

        public string Name
        {
            get { throw new NotImplementedException(); }
        }

        public string ProductName
        {
            get { return "Mock Device 2"; }
        }

        public string Manufacturer
        {
            get { return "BioNex"; }
        }

        public string Description
        {
            get { throw new NotImplementedException(); }
        }

        #endregion
    }
}

