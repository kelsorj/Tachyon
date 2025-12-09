using System;
using System.Collections.Generic;
using System.Text;
using IWorksDriver;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Xml.Linq;
using System.Linq;
using System.Threading;
using BioNex.IWorksCommandServer;
using CookComputing.XmlRpc;
using System.Net;
using System.Windows.Threading;


namespace BioNex.IWorksPlugins
{
    public class HiveUpstacker : IWorksDriver.CControllerClientClass, IWorksDriver.IWorksDriver, IStackerDriver
    {
        private const int CONNECTION_RECOVERY_SLEEP_DURATION = 1000;
        private IWorksStackerDeviceProxy _remote_stacker;
        private PingBackServer _pingback_server;
        private Dispatcher _dispatcher;

        public HiveUpstacker()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        #region unimplemented_IWorks_methods
  
        public ReturnCode LoadStack(string Labware, PlateFlagsType PlateFlags, string Location)
        {
            return ReturnCode.RETURN_SUCCESS;
        }

        public ReturnCode ScanStack(string Location)
        {
            return ReturnCode.RETURN_SUCCESS;
        }

        public ReturnCode UnloadStack(string Labware, PlateFlagsType PlateFlags, string Location)
        {
            return ReturnCode.RETURN_SUCCESS;
        }

        public ReturnCode Ignore()
        {
            return ReturnCode.RETURN_SUCCESS;
        }

        public ReturnCode Command(string CommandXML)
        {
            return ReturnCode.RETURN_SUCCESS;
        }

        public string Compile(CompileType iCompileType, string MetaDataXML)
        {
            return "";
        }

        public string ControllerQuery(string Query)
        {
            return "";
        }
        
        public stdole.IPictureDisp GetLayoutBitmap(string LayoutInfoXML)
        {
            return null;
        }

        public ReturnCode Retry()
        {
            return ReturnCode.RETURN_SUCCESS;
        }

        #endregion

        #region remotely_implemented_methods
       
        public void Ping()
        {
            _remote_stacker.Ping();
        }
        
        public short IsStackEmpty(string Location)
        {
            while(true)
                try
                {
                    int is_stack_empty = _remote_stacker.IsStackEmpty(Location);
                    return (short)is_stack_empty;
                }
                catch (WebException)
                { 
                    // web exception is generally due to a timeout or connection loss.
                    // VWorks doesn't let IsStackEmpty report errors, so we need to keep re-trying until the connection is restored.
                    Thread.Sleep(CONNECTION_RECOVERY_SLEEP_DURATION);
                }
        }

        public short IsStackFull(string Location)
        {
            while(true)
                try
                {
                    int is_stack_full = _remote_stacker.IsStackFull(Location);
                    return (short)is_stack_full;
                }
                catch (WebException)
                {
                    Thread.Sleep(CONNECTION_RECOVERY_SLEEP_DURATION);
                }
        }

        public ReturnCode SinkPlate(string labware, PlateFlagsType plateFlags, string sinkToLocation)
        {
            bool ok = false;
            while(!ok)
                try
                {
                    _remote_stacker.SinkPlate(labware, (int)plateFlags, sinkToLocation);
                    ok = true;
                }
                catch (WebException)
                {
                    Thread.Sleep(CONNECTION_RECOVERY_SLEEP_DURATION);
                }
            while(true)
                try
                {
                    while (!_remote_stacker.IsSinkPlateComplete())
                        Thread.Sleep(0);
                    return ReturnCode.RETURN_SUCCESS;
                }
                catch (WebException)
                {
                    Thread.Sleep(CONNECTION_RECOVERY_SLEEP_DURATION);
                }
        }

        public ReturnCode SourcePlate(string labware, PlateFlagsType plateFlags, string sourceFromLocation)
        {
            bool ok = false;
            while (!ok)
                try
                {
                    _remote_stacker.SourcePlate(labware, (int)plateFlags, sourceFromLocation);
                    ok = true;
                }
                catch (WebException)
                {
                    Thread.Sleep(CONNECTION_RECOVERY_SLEEP_DURATION);
                }
            while(true)
                try
                {
                    while (!_remote_stacker.IsSourcePlateComplete())
                        Thread.Sleep(0);
                    return ReturnCode.RETURN_SUCCESS;
                }
                catch (WebException)
                {
                    Thread.Sleep(CONNECTION_RECOVERY_SLEEP_DURATION);
                }
        }
     
        public bool IsLocationAvailable(string LocationAvailableXML)
        {
            while(true)
                try
                {
                    return _remote_stacker.IsLocationAvailable(LocationAvailableXML);
                }
                catch (WebException)
                {
                    Thread.Sleep(CONNECTION_RECOVERY_SLEEP_DURATION);
                }
        }

        public ReturnCode MakeLocationAvailable(string LocationAvailableXML)
        {
            bool ok = false;
            while (!ok)
            {
                try
                {
                    _remote_stacker.MakeLocationAvailable(LocationAvailableXML);
                    ok = true;
                }
                catch (WebException)
                {
                    Thread.Sleep(CONNECTION_RECOVERY_SLEEP_DURATION);
                }
            }
            while (true)
            {
                try
                {
                    while (!_remote_stacker.IsMLAComplete())
                        Thread.Sleep(0);
                    return ReturnCode.RETURN_SUCCESS;
                }
                catch (WebException)
                {
                    Thread.Sleep(CONNECTION_RECOVERY_SLEEP_DURATION);
                }
            }
        }

        public ReturnCode PrepareForRun(string LocationInfoXML)
        {
            _remote_stacker.PrepareForRun();
            return ReturnCode.RETURN_SUCCESS;
        }

        public void Abort()
        {
            _remote_stacker.Abort();
        }
        #endregion

        #region locally_implemented_methods
     
        public ReturnCode Initialize(string CommandXML)
        {
            Close();

            XDocument doc = XDocument.Parse(CommandXML);     
            string ip_address = doc.Descendants("Parameter").FirstOrDefault(x => x.Attribute("Name").Value == "IP Address" ).Attribute("Value").Value;
            string port = doc.Descendants("Parameter").FirstOrDefault(x => x.Attribute("Name").Value == "Port").Attribute("Value").Value;

            _remote_stacker = (IWorksStackerDeviceProxy)XmlRpcProxyGen.Create(typeof(IWorksStackerDeviceProxy));
            _remote_stacker.Url = String.Format("http://{0}:{1}/stackerdata", ip_address, int.Parse(port));
            _remote_stacker.Timeout = 30000;

            _pingback_server = new PingBackServer(int.Parse(port), this); // fields ping calls from Synapsis to HiveUpstackerDriver
            return ReturnCode.RETURN_SUCCESS;
        }

        public void Close()
        {
            if(_pingback_server != null)
                _pingback_server.Stop();
        }

        public void ShowDiagsDialog(SecurityLevel iSecurity)
        {
            Diagnostics dlg = new Diagnostics(this);
            dlg.ShowDialog();
            dlg.Close();
        }
    
        public string GetDescription(string CommandXML, bool Verbose)
        {
            return "BioNex Hive Stacker";
        }
        
        public string GetErrorInfo()
        {
            return "unknown error";
        }
        
        public string GetMetaData(MetaDataType iDataType, string current_metadata)
        {
            return @"   <?xml version='1.0' encoding='ASCII' ?>
                        <Velocity11 file='MetaData' md5sum='0' version='1.0' >
                            <MetaData >
                                <Device Description='HiveUpstacker' DynamicLocations='0' HardwareManufacturer='BioNex Solutions' HasBarcodeReader='1' Ignore='0' MiscAttributes='16' Name='HiveUpstacker' PreferredTab='Plate Storage' RegistryName='HiveUpstacker\Profiles' >
                                    <Parameters >
                                        <Parameter Name='IP Address' Type='1' Value='192.168.2.160'/>
                                        <Parameter Name='Port' Type='4' Value='7890'/>
                                    </Parameters>
                                    <Locations >
                                        <Location Group='0' MaxStackHeight='460' Name='Stage' Offset='0' Type='3' />
                                    </Locations>
                                    <StorageDimensions DirectStorageAccess='0' />
                                    <RobotMetaData ReachesExternalLocations='1' />
                                </Device>
                                <Versions >
                                    <Version Author='BioNex Solutions' Company='BioNex Solutions' Date='February 16, 2011' Name='HiveUpstacker' Version='1.0.0' />
                                </Versions>
                                <Commands >
                                <!-- Command Compiler='0' Description='Check First Plate Orientation' Editor='8' Name='Check First Plate Orientation' NextTaskToExecute='1' RequiresRefresh='0' TaskRequiresLocation='1' VisibleAvailability='1' /-->
                                </Commands> 
                            </MetaData>
                        </Velocity11>";
        }

        #region IPictureDisp
        /// Converts an image into a IPictureDisp
        private static Guid iPictureDispGuid = typeof(stdole.IPictureDisp).GUID;
        [DllImport("OleAut32.dll", EntryPoint = "OleCreatePictureIndirect", ExactSpelling = true, PreserveSig = false)]
        private static extern stdole.IPictureDisp OleCreatePictureIndirect([MarshalAs(UnmanagedType.AsAny)] object picdesc, ref Guid iid, bool fOwn);
        private static stdole.IPictureDisp ToIPictureDisp(Image image)
        {
            Bitmap bitmap = (image is Bitmap) ? (Bitmap)image : new Bitmap(image);
            PICTDESC.Bitmap pictBit = new PICTDESC.Bitmap(bitmap);
            return OleCreatePictureIndirect(pictBit, ref iPictureDispGuid, true);
        }
        private static class PICTDESC
        {
            //Picture Types
            public const short PICTYPE_UNINITIALIZED = -1;
            public const short PICTYPE_NONE = 0;
            public const short PICTYPE_BITMAP = 1;
            public const short PICTYPE_METAFILE = 2;
            public const short PICTYPE_ICON = 3;
            public const short PICTYPE_ENHMETAFILE = 4;

            [StructLayout(LayoutKind.Sequential)]
            public class Icon
            {
                internal int cbSizeOfStruct = Marshal.SizeOf(typeof(PICTDESC.Icon));
                internal int picType = PICTDESC.PICTYPE_ICON;
                internal IntPtr hicon = IntPtr.Zero;
                internal int unused1 = 0;
                internal int unused2 = 0;

                internal Icon(System.Drawing.Icon icon)
                {
                    this.hicon = icon.ToBitmap().GetHicon();
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            public class Bitmap
            {
                internal int cbSizeOfStruct = Marshal.SizeOf(typeof(PICTDESC.Bitmap));
                internal int picType = PICTDESC.PICTYPE_BITMAP;
                internal IntPtr hbitmap = IntPtr.Zero;
                internal IntPtr hpal = IntPtr.Zero;
                internal int unused = 0;

                internal Bitmap(System.Drawing.Bitmap bitmap)
                {
                    this.hbitmap = bitmap.GetHbitmap();
                }
            }
        }
        #endregion
        public stdole.IPictureDisp Get32x32Bitmap(string CommandName)
        {
            // this allows us to see wtf the stream wants to load
            //foreach (string x in this.GetType().Assembly.GetManifestResourceNames())
            //    MessageBox.Show(x);
            Stream s = this.GetType().Assembly.GetManifestResourceStream("BioNex.IWorksPlugins.Images.icon.bmp");
            Bitmap bmp = new Bitmap(s);
            return ToIPictureDisp(bmp);
        }

        public void PlateTransferAborted(string PlateInfoXML)
        {
        }
  
        public ReturnCode PlateDroppedOff(string PlateInfoXML)
        {
            return ReturnCode.RETURN_SUCCESS;
        }
        
        public ReturnCode PlatePickedUp(string PlateInfoXML)
        {
            return ReturnCode.RETURN_SUCCESS;
        }

        #endregion

        #region IWorksDriver.IControllerClient

        // if _controller is IWorksController, freezes on Update
        // if _controller is CWorksController, get error: Unable to cast COM object of type 'IWorksDriver.CWorksControllerClass' to interface type 'IWorksDriver.IWorksController'. This operation failed because the QueryInterface call on the COM component for the interface with IID '{E2046A47-15EA-40D9-B85A-88C3FC1AC686}' failed due to the following error: No such interface supported (Exception from HRESULT: 0x80004002 (E_NOINTERFACE)).
        CWorksController _controller;
        public override void SetController(CWorksController Controller)
        {
            _controller = Controller;
        }

        public void ResetStackHeight()
        {
            if (_controller != null)
            {
                try {
                    _dispatcher.Invoke(new Action( () =>
                    {
                        try {
                            string reset_stack_height_update = @"
                                <?xml version='1.0' encoding='ASCII' ?>
                                    <Velocity11 file='Update' version='1.0' >
                                        <Update Category='StackHeight' >
                                            <Parameters >
                                                <Parameter Name='PlateStackHeight' Value='0.0' />
                                            </Parameters>
                                        </Update>
                                    </Velocity11>";
                            var bullshit = (IWorksController)_controller;
                            _controller.Update(this, reset_stack_height_update);
                        } catch( Exception ex) {
                            int i = 0;
                        }
                    }));
                } catch( Exception ex) {
                    int i = 0;
                }
            }
        }

        #endregion
    }
}
