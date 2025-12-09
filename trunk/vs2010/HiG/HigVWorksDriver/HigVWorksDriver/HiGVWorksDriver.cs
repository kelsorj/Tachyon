using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.IO;
using System.Xml.Linq;
using System.Windows;
using System.Threading;
using Interop.IWorksDriver;
using log4net;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace BioNex.IWorksPlugins
{
    public class HiGVWorksDriver : IWorksDriver, CControllerClient
    {
        //private HiGGlue _glue = new HiGGlue();
        private HiGIntegration.HiGInterface _hig;
        private ManualResetEvent _command_complete_event;
        private ReturnCode _last_command_result;
        private CWorksController _controller;
        private Dispatcher _main_thread;

        private readonly ILog _log = LogManager.GetLogger( "HiG VWorks3 Plugin");
        
        //private enum BucketState { Empty = 1, MLACalled = 2, Full = 4, Spun = 8 }
        //private BucketState[] _bucket_states;
        private int[] _bucket_plate_instance_map;

        /// <summary>
        /// This allows us to keep track of the last command executed, for error recovery reasons.
        /// If it is null, it means the last command executed successfully.
        /// </summary>
        private Action _last_command_executed; 
        /// <summary>
        /// Busy flag must be used in addition to _last_command_executed for now.  I think we could
        /// actually detect busy state differently, but let's try this for now.
        /// </summary>
        private bool _busy;
        private ReaderWriterLockSlim _lock;

        private readonly string Location1 = "Bucket 1";
        private readonly string Location2 = "Bucket 2";

        public HiGVWorksDriver()
        {
            _log.Debug( "HiG VWorks3 driver created");
            //_bucket_states = new BucketState[2] { BucketState.Empty, BucketState.Empty};
            // 0 means no plate.  anything > means the bucket is occupied by that plate instance
            _bucket_plate_instance_map = new int[2] { 0, 0 };
        }

        #region IWorksDriver

        #region stubs
        
        public string Compile(CompileType iCompileType, string MetaDataXML)
        {
            // DKM 2012-03-12 I was going to use this to check for an invalid parameter, but forgot that we don't
            //     get a number of protocol plates until later
            _log.DebugFormat( "Compile called with type {0}", iCompileType.ToString());
            return "";
        }

        public string ControllerQuery(string Query)
        {
            _log.DebugFormat( "ControllerQuery called: {0}", Query);
            return "";
        }

        public string GetDescription(string CommandXML, bool Verbose)
        {
            _log.DebugFormat( "GetDescription called: {0}", CommandXML.Replace( '\n', ' '));
            // assume the only command is Spin
            XDocument doc = XDocument.Parse(CommandXML);
            XElement command_element = doc.Descendants("Command").FirstOrDefault(x => x.Attribute("Name").Value == "Spin");
            if( command_element == null)
                return "Spin with HiG";
                
            double g = double.Parse( doc.Descendants("Parameter").FirstOrDefault(x => x.Attribute("Name").Value == "Gs").Attribute("Value").Value);
            double time_s = double.Parse( doc.Descendants("Parameter").FirstOrDefault(x => x.Attribute("Name").Value == "Spin time").Attribute("Value").Value);
            return String.Format( "Spin with HiG at {0}G for {1}s", g, time_s);
        }

        public stdole.IPictureDisp GetLayoutBitmap(string LayoutInfoXML)
        {
            return null;
        }

        public void PlateDroppedOff(string PlateInfoXML)
        {
            _log.DebugFormat( "PlateDroppedOff called: {0}", PlateInfoXML.Replace( '\n', ' '));

            #region sample XML from VW3
            // <?xml version='1.0' encoding='ASCII' ?>
            // <Plates file='PlateInfo' md5sum='06598e9fcfe2ef393512fb06fe90fdeb' version='1.0' >
	        //     <Plate Instance_Number='1' Labware='dummy' Location='Bucket 1' Name='unnamed - 1' Plate_Has_Lid='0' />
            // </Plates>
            #endregion

            string bucket = GetLocationFromPlateInfoXml( PlateInfoXML);
            int plate_instance = GetPlateInstanceFromPlateInfoXml( PlateInfoXML);
            SetBucketPlateInstance( bucket, plate_instance);
            _spin_complete = false;
        }

        private static string GetLocationFromPlateInfoXml(string PlateInfoXML)
        {
            XDocument doc = XDocument.Parse(PlateInfoXML);
            XElement location_available = doc.Descendants("Plate").First(); // there's only one
            XAttribute location = location_available.Attributes().First(x => x.Name == "Location");
            return location.Value;
        }

        private static int GetPlateInstanceFromPlateInfoXml(string PlateInfoXML)
        {
            XDocument doc = XDocument.Parse(PlateInfoXML);
            XElement location_available = doc.Descendants("Plate").First(); // there's only one
            XAttribute location = location_available.Attributes().First(x => x.Name == "Instance_Number");
            return int.Parse( location.Value);
        }

        private void SetBucketPlateInstance( string location, int plate_instance)
        {
            lock(this) {
                if( location == Location1) {
                    _bucket_plate_instance_map[0] = plate_instance;
                } else {
                    _bucket_plate_instance_map[1] = plate_instance;
                }
            }
        }

        private void ClearBucketPlateInstance( string location)
        {
            lock(this) {
                if( location == Location1) {
                    _bucket_plate_instance_map[0] = 0;
                } else {
                    _bucket_plate_instance_map[1] = 0;
                }
            }
        }

        private bool IsBucketFull( int bucket_index)
        {
            return IsBucketFull( bucket_index == 0 ? Location1 : Location2);
        }

        private bool IsBucketFull( string location)
        {
            return GetBucketPlateInstance( location) != 0;
        }

        private int GetBucketPlateInstance( string location)
        {
            return GetBucketPlateInstance( location == Location1 ? 0 : 1);
        }

        private int GetBucketPlateInstance( int bucket_index)
        {
            lock(this) {
                return _bucket_plate_instance_map[bucket_index];
            }
        }

        public void PlatePickedUp(string PlateInfoXML)
        {
            _log.DebugFormat( "PlatePickedUp called: {0}", PlateInfoXML.Replace( '\n', ' '));
            #region sample XML from VW3
            // <?xml version='1.0' encoding='ASCII' ?>
            // <Plates file='PlateInfo' md5sum='06598e9fcfe2ef393512fb06fe90fdeb' version='1.0' >
            //     <Plate Instance_Number='1' Labware='dummy' Location='Bucket 1' Name='unnamed - 1' Plate_Has_Lid='0' />
            // </Plates>
            #endregion

            string bucket = GetLocationFromPlateInfoXml( PlateInfoXML);
            ClearBucketPlateInstance( bucket);
        }

        public void PlateTransferAborted(string PlateInfoXML)
        {
            _log.DebugFormat( "PlateTransferAborted called: {0}", PlateInfoXML.Replace( '\n', ' '));
        }

        public ReturnCode PrepareForRun(string LocationInfoXML)
        {
            _log.DebugFormat( "PrepareForRun called: {0}", LocationInfoXML.Replace( '\n', ' '));
            _spin_complete = false;
            return ReturnCode.RETURN_SUCCESS;
        }

        #endregion

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
        public stdole.IPictureDisp Get32x32Bitmap(string CommandName)
        {
            // this allows us to see wtf the stream wants to load
            //foreach (string x in this.GetType().Assembly.GetManifestResourceNames())
            //    MessageBox.Show(x);
            Stream s = this.GetType().Assembly.GetManifestResourceStream("BioNex.IWorksPlugins.Images.icon.bmp");
            Bitmap bmp = new Bitmap(s);
            return ToIPictureDisp(bmp);
        }

        #endregion

        public string GetMetaData(MetaDataType iDataType, string current_metadata)
        {
            return @"  <?xml version='1.0' encoding='ASCII' ?>
                        <Velocity11 file='MetaData' version='1.0'>
	                        <MetaData>
                                <Device Description='HiG Centrifuge' 
                                        HardwareManufacturer='BioNex Solutions' 
                                        Name='HiG Centrifuge' 
                                        PreferredTab='Plate Handling' >
			                        <Parameters>
				                        <Parameter Name='adapter id' Type='8' Value='0' />
				                        <Parameter Name='simulate hardware' Type='8' Value='0' />
			                        </Parameters>
    			                    <Locations>
                                        <Location Group='0' MaxStackHeight='500' Name='Bucket 1' Offset='0' Type='1' />
                                        <Location Group='0' MaxStackHeight='500' Name='Bucket 2' Offset='0' Type='1' />
			                        </Locations>
                                    <!-- DKM 2011-12-20 don't need this -->
                                    <!-- StorageDimensions DirectStorageAccess='0' /-->
    		                    </Device>
	    	                    <Versions>
		    	                    <Version Author='BioNex Solutions' 
                                             Company='BioNex Solutions' 
                                             Name='HiG Centrifuge' 
                                             Version='1.0.0' />
		                        </Versions>
		                        <Commands>
                                    <!--
			                        <Command Compiler='0' 
                                             Description='Device information' 
                                             Editor='0' 
                                             NextTaskToExecute='1' 
                                             RequiresRefresh='0' 
                                             TaskRequiresLocation='0' 
                                             VisibleAvailability='1' />
                                    -->
                                    <Command Name='Spin'
                                             Description='Spin plate'
                                             VisibleAvailability='1'
                                             TaskRequiresLocation='1'
                                             RequiresRefresh='0'
                                             NextTaskToExecute='1'
                                             Editor='2'
                                             Compiler='0'>
                                        <Parameters>
                                            <Parameter Name='Gs' Type='8' Style='0' Scriptable='1' Value='1000' Units='G'>
                                                <Ranges>
                                                    <Range Value='500' />
                                                    <Range Value='5000' />
                                                </Ranges>
                                            </Parameter>
                                            <Parameter Name='Spin time' Type='12' Style='0' Scriptable='1' Value='10.0' Units='s'>
                                                <Ranges>
                                                    <Range Value='1' />
                                                    <Range Value='3600' />
                                                </Ranges>
                                            </Parameter>
                                            <Parameter Name='Acceleration' Type='8' Style='0' Scriptable='1' Value='100' Units='%'>
                                                <Ranges>
                                                    <Range Value='1' />
                                                    <Range Value='100' />
                                                </Ranges>
                                            </Parameter>
                                            <Parameter Name='Deceleration' Type='8' Style='0' Scriptable='1' Value='100' Units='%'>
                                                <Ranges>
                                                    <Range Value='1' />
                                                    <Range Value='100' />
                                                </Ranges>
                                            </Parameter>
                                            <!-- 12 = floating point, 8 = int, 4 = location list that works with HiG, 2 = droplist, 1 = textbox, 0 = bool droplist -->
                                            <Parameter Name='Spin two protocol plates' Type='0' Style='0' Scriptable='0' Value='0'>
                                                <Ranges>
                                                    <Range Value='0' />
                                                    <Range Value='1' />
                                                </Ranges>
                                            </Parameter>
                                        </Parameters>
                                    </Command>
		                        </Commands>
	                        </MetaData>
                        </Velocity11>";
        }

        string _last_error = "Unknown Error.";
        public ReturnCode Initialize(string CommandXML)
        {
            _log.DebugFormat( "Initialize called: {0}", CommandXML.Replace( '\n', ' '));
            try
            {
                XDocument doc = XDocument.Parse(CommandXML);
                string adapter_id = doc.Descendants("Parameter").FirstOrDefault(x => x.Attribute("Name").Value == "adapter id").Attribute("Value").Value;
                bool simulate = doc.Descendants("Parameter").FirstOrDefault(x => x.Attribute("Name").Value == "simulate hardware").Attribute("Value").Value != "0";

                // do all of the object creation and event subscription
                _lock = new ReaderWriterLockSlim( LockRecursionPolicy.NoRecursion);
                _hig = new HiGIntegration.HiG();
                _command_complete_event = new ManualResetEvent( false);
                _hig.InitializeComplete += new EventHandler(_hig_CommandComplete);
                _hig.InitializeError += new EventHandler(_hig_CommandError);
                _hig.HomeComplete += new EventHandler(_hig_CommandComplete);
                _hig.HomeError += new EventHandler(_hig_CommandError);
                _hig.OpenShieldComplete += new EventHandler(_hig_CommandComplete);
                _hig.OpenShieldError += new EventHandler(_hig_CommandError);
                _hig.SpinComplete += new EventHandler(_hig_SpinComplete);
                _hig.SpinError += new EventHandler(_hig_SpinError);
                _hig.DiagnosticsClosed += new EventHandler(_hig_DiagnosticsClosed);

                // now initialize -- need to lock busy flag every time because VWorks will call IsLocationAvailable a lot in a separate thread
                ThreadSafeSetBusy( true);
                string device_name = String.Format( "HiG on adapter {0}", adapter_id);
                // TODO fix path to config folder -- when debugging, uses source code folder instead of VWorks plugins folder
                _log.Debug( "calling Initialize()");
                _last_command_executed = new Action( () => { _hig.Initialize( device_name, adapter_id, simulate); } );
                _last_command_executed();
                _log.Debug( "waiting for Initialize command complete event");
                // Initialize kicks off a thread, so we need to block here since VW expects to handle all of the threading
                WaitForCommandCompleteOrError();
                _log.Debug( "Initialize signaled event");
                _command_complete_event.Reset();
                if( _last_command_result != ReturnCode.RETURN_SUCCESS) {
                    ThreadSafeSetBusy( false);
                    return _last_command_result;
                }

                // now home
                _log.Debug( "calling Home()");
                _last_command_executed = new Action( () => { _hig.Home(); } );
                _last_command_executed();
                _log.Debug( "waiting for Home command complete event");
                WaitForCommandCompleteOrError();
                _log.Debug( "Home signaled event");
                _command_complete_event.Reset();

                ThreadSafeSetBusy( false);
                return _last_command_result;
            }
            catch (Exception e)
            {
                _last_error = e.Message + "\n\nOnly Abort will work";
                return ReturnCode.RETURN_FAIL;
            }
        }

        void _hig_DiagnosticsClosed(object sender, EventArgs e)
        {
            // DKM 2011-10-17 the original idea here was to use DiagnosticsClosed so we could
            //                tell VW4 to decrement the ref count.  However, it doesn't work
            //                properly if we call OnCloseDiagsDialog from the event handler --
            //                it seems to need to be called right after we call ShowDiagnostics.
            //                Is this a threading issue? -- NO, I just checked and both are called
            //                from the main thread.
            if( _controller != null)
                _controller.OnCloseDiagsDialog( (CControllerClient)this);

        }

        private void WaitForCommandCompleteOrError()
        {
            _command_complete_event.WaitOne();
        }

        private void ThreadSafeSetBusy( bool state)
        {
            _lock.EnterWriteLock();
            _busy = state;
            _lock.ExitWriteLock();
        }

        private bool ThreadSafeGetBusy()
        {
            // IsLocationAvailable could get called before we've even initialized!  So do a null check here.
            if( _lock == null)
                return false;

            _lock.EnterReadLock();
            bool busy_copy = _busy;
            _lock.ExitReadLock();
            return busy_copy;
        }

        void _hig_CommandError(object sender, EventArgs e)
        {
            HiGIntegration.HiG.ErrorEventArgs err = (HiGIntegration.HiG.ErrorEventArgs)e;
            _last_error = err.Reason;
            _last_command_result = ReturnCode.RETURN_FAIL;
            _command_complete_event.Set();
        }

        void _hig_CommandComplete(object sender, EventArgs e)
        {
            _last_command_executed = null;
            _last_error = "No error";
            _last_command_result = ReturnCode.RETURN_SUCCESS;
            _command_complete_event.Set();
        }

        void _hig_SpinError(object sender, EventArgs e)
        {
            HiGIntegration.HiG.ErrorEventArgs err = (HiGIntegration.HiG.ErrorEventArgs)e;
            _last_error = err.Reason;
            _last_command_result = ReturnCode.RETURN_FAIL;
            _command_complete_event.Set();
        }

        void _hig_SpinComplete(object sender, EventArgs e)
        {
            _last_command_executed = null;
            _last_error = "No error";
            _last_command_result = ReturnCode.RETURN_SUCCESS;
            _command_complete_event.Set();
        }

        public void Close()
        {
            _hig.Close();
            _hig.InitializeComplete -= new EventHandler(_hig_CommandComplete);
            _hig.InitializeError -= new EventHandler(_hig_CommandError);
            _hig.HomeComplete -= new EventHandler(_hig_CommandComplete);
            _hig.HomeError -= new EventHandler(_hig_CommandError);
            _hig.OpenShieldComplete -= new EventHandler(_hig_CommandComplete);
            _hig.OpenShieldError -= new EventHandler(_hig_CommandError);
            _hig.SpinComplete -= new EventHandler(_hig_CommandComplete);
            _hig.SpinError -= new EventHandler(_hig_CommandError);
        }

        public ReturnCode Command(string CommandXML)
        {
            _log.DebugFormat( "Command called: {0}", CommandXML.Replace( '\n', ' '));

            #region sample command metadata coming from VW3:
            string vw3_metadata = 
            @"<?xml version='1.0' encoding='ASCII' ?>
            <Velocity11 file='MetaData' md5sum='6edbc10b6dc7bc6708b9ba1dc39146b1' version='1.0' >
	            <Command Compiler='0' Description='Spin plate' Editor='2' Name='Spin' RequiresRefresh='0' TaskRequiresLocation='1' >
		            <Parameters >
			            <Parameter Name='Gs' Style='0' Type='8' Units='G' Value='1000' >
				            <Ranges >
					            <Range Value='500' />
					            <Range Value='5000' />
				            </Ranges>
			            </Parameter>
			            <Parameter Name='Spin time' Style='0' Type='12' Units='s' Value='10.0' >
				            <Ranges >
					            <Range Value='1' />
					            <Range Value='3600' />
				            </Ranges>
			            </Parameter>
			            <Parameter Name='Acceleration' Style='0' Type='8' Units='%' Value='100' >
				            <Ranges >
					            <Range Value='1' />
					            <Range Value='100' />
				            </Ranges>
			            </Parameter>
			            <Parameter Name='Deceleration' Style='0' Type='8' Units='%' Value='100' >
				            <Ranges >
					            <Range Value='1' />
					            <Range Value='100' />
				            </Ranges>
			            </Parameter>
			            <Parameter Name='Used Location0' Style='0' Type='1' Value='Bucket 1' />
		            </Parameters>
	            </Command>
		        <Locations >
			        <Value Value='Bucket' />
		        </Locations>
            </Velocity11>";
            #endregion

            #region sample command metadata coming from VW4:
            string metadata = 
            @"<?xml version='1.0' encoding='ASCII' ?>
            <Velocity11 file='MetaData' md5sum='a540b23999f4a1dbc8eb040bf6cf1f23' version='1.0' >
	            <Command Compiler='0' Description='Spin plate' Editor='2' Name='Spin' NextTaskToExecute='1' ProtocolName='hig.pro' RequiresRefresh='0' TaskRequiresLocation='1' VisibleAvailability='1' >
                    <Parameters >
			            <Parameter Name='Gs' Scriptable='1' Style='0' Type='8' Units='G' Value='1000' >
				            <Ranges >
					            <Range Value='500' />
					            <Range Value='5000' />
				            </Ranges>
			            </Parameter>
                        <Parameter Name='Spin time' Scriptable='1' Style='0' Type='12' Units='s' Value='10.0' >
				            <Ranges >
					            <Range Value='1' />
					            <Range Value='3600' />
				            </Ranges>
			            </Parameter>
			            <Parameter Name='Acceleration' Scriptable='1' Style='0' Type='8' Units='%' Value='100' >
				            <Ranges >
					            <Range Value='1' />
					            <Range Value='100' />
				            </Ranges>
			            </Parameter>
			            <Parameter Name='Deceleration' Scriptable='1' Style='0' Type='8' Units='%' Value='100' >
				            <Ranges >
					            <Range Value='1' />
					            <Range Value='100' />
				            </Ranges>
			            </Parameter>
		            </Parameters>
		            <Locations >
    			        <Value Value='Bucket 1' />
		            </Locations>
	            </Command>
            </Velocity11>";
            #endregion

            // NOTE: the following code for handling VW commands assumes that there's only ONE command: Spin.

            try {
                XDocument doc = XDocument.Parse(CommandXML);
                XElement command_element = doc.Descendants("Command").FirstOrDefault(x => x.Attribute("Name").Value == "Spin");
                if( command_element == null)
                    return ReturnCode.RETURN_BAD_ARGS;
                
                double g = double.Parse( doc.Descendants("Parameter").FirstOrDefault(x => x.Attribute("Name").Value == "Gs").Attribute("Value").Value);
                double accel = double.Parse( doc.Descendants("Parameter").FirstOrDefault(x => x.Attribute("Name").Value == "Acceleration").Attribute("Value").Value);
                double decel = double.Parse( doc.Descendants("Parameter").FirstOrDefault(x => x.Attribute("Name").Value == "Deceleration").Attribute("Value").Value);
                double time = double.Parse( doc.Descendants("Parameter").FirstOrDefault(x => x.Attribute("Name").Value == "Spin time").Attribute("Value").Value);
                bool spin_two_protocol_plates = doc.Descendants("Parameter").FirstOrDefault(x => x.Attribute("Name").Value == "Spin two protocol plates").Attribute("Value").Value == "1";

                // DKM 2012-03-09 only spin if both buckets are in the Full state.  Actually, we also
                //     have to make the first plate in a pair sit until the spin is complete, so wait for
                //     an event or similar to get fired.
                bool bucket1_full = IsBucketFull( Location1);
                bool bucket2_full = IsBucketFull( Location2);
                // DKM 2012-03-09 we should only wait for a 2nd plate if the user selects "Spin two protocol plates" in the command parameters
                //     Otherwise, immediately spin.  I'm assuming that the counterweight is always in bucket 2, and I have taken the necessary
                //     precautions in ILA.  But in ILA, it looks like the metadata schema actually changes!  So we need to wait for the ILA
                //     call that includes the task metadata.  See ILA for more info, because I found 3 different types of metadata being passed in.
                // DKM 2012-03-10 technically, this could be a bad race condition if Command is called from two threads -- what if the 2nd
                //     incoming plate tries to Signal _spin_complete_event before it's been created?  I think this is very unlikely, if not
                //     impossible, since Command won't get called until a plate is physically placed in the HiG.
                if( spin_two_protocol_plates && !(bucket1_full && bucket2_full)) {                    
                    // wait for an event that says we finished spinning
                    //WaitForCommandCompleteOrError();
                    _log.Debug("spin command for first plate in pair acknowledged");
                    return ReturnCode.RETURN_SUCCESS;
                }

                // DKM 2012-03-09 if we get here, it just means that we are servicing the last plate in a pair,
                //     the one that actually will send the spin command.
                _log.Debug("spin command for second plate in pair acknowledged");
                ThreadSafeSetBusy( true);
                _log.Debug( "calling Spin()");
                _last_command_executed = new Action( () => {  _hig.Spin( g, accel, decel, time); } );
                _last_command_executed();
                _log.Debug( "waiting for Spin command complete event");
                WaitForCommandCompleteOrError();
                _log.Debug( "Spin signaled event");
                _command_complete_event.Reset();
                _main_thread.Invoke( new Action( () => { _controller.PrintToLog( this, "Spin complete"); } ));
                ThreadSafeSetBusy( false);
                _spin_complete = true;
                _log.DebugFormat("spin command for second plate in pair finished");
                return _last_command_result;
            } catch( Exception) {
                return ReturnCode.RETURN_FAIL;
            }
        }
        
        bool _spin_complete;
        public bool IsLocationAvailable(string LocationAvailableXML)
        {
            //_log.DebugFormat( "IsLocationAvailable called: {0}", LocationAvailableXML.Replace( '\n', ' '));
            
            #region sample XML from VW3 (idle call???)
            //<?xml version='1.0' encoding='ASCII' ?>
            //<Velocity11 file='MetaData' md5sum='f333a69f11a75b7d9ef822d4f7b38f28' version='1.0' >
            //    <LocationAvailable Device='hig' Location='Bucket 1' >
            //        <StorageLocation >
            //            <Location Group='0' MaxStackHeight='500' Offset='0' Type='1' />
            //        </StorageLocation>
            //        <Command Compiler='0' Editor='0' RequiresRefresh='0' TaskRequiresLocation='1' />
            //    </LocationAvailable>
            //</Velocity11>
            #endregion

            #region sample XML from VW3 (outgoing call???)
            //<?xml version='1.0' encoding='ASCII' ?>
            //<Velocity11 file='MetaData' md5sum='73e340e1e06692242b2fa7cd083627d6' version='1.0' >
            //    <LocationAvailable Device='hig' Labware='dummy' Location='Bucket 1' PlateInstance='1' PlateName='unnamed - 1' >
            //        <StorageLocation >
            //            <Location Group='0' MaxStackHeight='500' Offset='0' Type='1' />
            //        </StorageLocation>
            //        <Command Compiler='0' Editor='0' RequiresRefresh='0' TaskRequiresLocation='1' />
            //    </LocationAvailable>
            //</Velocity11>
            #endregion

            // check device state first
            bool device_busy = ThreadSafeGetBusy();
            if( device_busy)
                return false;
            #region sample XML from VW3
            //<?xml version='1.0' encoding='ASCII' ?>
            //<Velocity11 file='MetaData' md5sum='11264b7741816566d763b193e5d9c2c5' version='1.0' >
            //    <LocationAvailable Device='hig' Labware='dummy' Location='Bucket 1' PlateInstance='3' PlateName='unnamed - 1' >
            //        <StorageLocation >
            //            <Location Group='0' MaxStackHeight='500' Offset='0' Type='1' />
            //        </StorageLocation>
            //        <Command Compiler='0' Description='Spin plate' Editor='2' Name='Spin' RequiresRefresh='0' TaskRequiresLocation='1' >
            //            <Parameters >
            //                <Parameter Name='Gs' Style='0' Type='8' Units='G' Value='500' >
            //                    <Ranges >
            //                        <Range Value='500' />
            //                        <Range Value='5000' />
            //                    </Ranges>
            //                </Parameter>
            //                <Parameter Name='Spin time' Style='0' Type='12' Units='s' Value='1' >
            //                    <Ranges >
            //                        <Range Value='1' />
            //                        <Range Value='3600' />
            //                    </Ranges>
            //                </Parameter>
            //                <Parameter Name='Acceleration' Style='0' Type='8' Units='%' Value='100' >
            //                    <Ranges >
            //                        <Range Value='1' />
            //                        <Range Value='100' />
            //                    </Ranges>
            //                </Parameter>
            //                <Parameter Name='Deceleration' Style='0' Type='8' Units='%' Value='100' >
            //                    <Ranges >
            //                        <Range Value='1' />
            //                        <Range Value='100' />
            //                    </Ranges>
            //                </Parameter>
            //                <Parameter Name='Spin two protocol plates' Style='0' Type='0' Value='1' >
            //                    <Ranges >
            //                        <Range Value='0' />
            //                        <Range Value='1' />
            //                    </Ranges>
            //                </Parameter>
            //                <Parameter Name='Used Location0' Style='0' Type='1' Value='Bucket 1' />
            //                <Parameter Name='Used Location1' Style='0' Type='1' Value='Bucket 2' />
            //            </Parameters>
            //        </Command>
            //    </LocationAvailable>
            //</Velocity11>
            #endregion

            // DKM 2012-03-14 this approach doesn't seem to work -- VW3 doesn't care about ILA to prevent plate pickup?
            /*
            // DKM 2012-03-14 to try to work around what I think is a bug in VW3, need to allow Command to pass
            //     through for the first plate in a pair, and then return false from ILA so the plate can't be unloaded
            string check_bucket = GetLocationFromLocationXml( LocationAvailableXML);
            if( check_bucket == Location1) {
                bool result = _spin_complete_event == null || _spin_complete_event.WaitOne( 0);
                return result;
            }
             */
            /*
            no plates yet == true
            one plate only and asking for empty bucket bucket == true
            two plates && spin completed = true
            two plates && !spin completed = false
            */

            int? plate_instance = GetPlateInstanceFromLocationXml( LocationAvailableXML);
            if( plate_instance != null) {
                _log.DebugFormat( "IsLocationAvailable called for plate instance {0}", plate_instance.Value);
            }
            bool bucket_1_has_plate = GetBucketPlateInstance(0) != 0;
            bool bucket_2_has_plate = GetBucketPlateInstance(1) != 0;
            if( !bucket_1_has_plate && !bucket_2_has_plate) {
                _log.DebugFormat( "IsLocationAvailable returning true since both buckets are empty");
                return true;
            }
            if( !(bucket_1_has_plate && bucket_2_has_plate)) {
                int bucket_number = GetLocationFromLocationXml( LocationAvailableXML) == Location1 ? 1 : 2;
                bool bucket_requested_is_available = (bucket_1_has_plate && bucket_number == 2) || (bucket_2_has_plate && bucket_number == 1);
                _log.DebugFormat( "IsLocationAvailable returning {0}: spin complete = {1}, bucket requested is available = {2}", _spin_complete || bucket_requested_is_available, _spin_complete, bucket_requested_is_available);
                return _spin_complete || bucket_requested_is_available;
            }
            return _spin_complete;

            // DKM 2012-03-09 need to wait until we get the right metadata schema -- we need to have the task parameters
            //     so that we know if we're spinning with counterweights or not
            bool? spin_two_plates = GetSpinTwoPlatesFromLocationXml( LocationAvailableXML);
            // DKM 2012-03-09 I just found that if the task parameters metadata is missing, this is a call to IsLocationAvailable
            //     when the plate is asked to LEAVE the device.  This could work well for me!
            //if( spin_two_plates == null)
            //    return false;
            
            string bucket = GetLocationFromLocationXml( LocationAvailableXML);
            //int? plate_instance = GetPlateInstanceFromLocationXml( LocationAvailableXML);
            // DKM 2012-03-09 OMFG there are THREE kinds of metadata passed to ILA!  If we get spin_two_plates, then that
            //     means we have incoming plates.  If we don't, then we are either removing or idle.  Idle is when
            //     we also don't have a plate_instance, so in that case return true
            // DKM 2012-03-13 I am really having a problem with deadlocks and hanging.  From logging the calls to ILA,
            //     it seems that at some point VW3 only sends minimal metadata to ILA -- in which case, I can only assume
            //     that my interpretation of plate_instance == null && spin_two_plates == null is not an "idle" case.
            //     To circumvent this problem, I am additionally adding another check for the specified buckets being
            //     empty.  This is because I have a deadlock case where (for 5 sim. plates), plates #3 and #4 get
            //     transferred out of the HiG (i.e. PlatePickedUp gets called), but the only metadata I get in ILA is
            //     the minimal form.
            bool specified_bucket_available = (bucket == Location1 && !IsBucketFull( Location1)) || (bucket == Location2 && !IsBucketFull( Location2));
            if( plate_instance == null && spin_two_plates == null && specified_bucket_available) {
                _log.Debug( "IsLocationAvailable returned true because plate instance and spin two protocol plates were undefined.  Specified bucket is also available.");
                return true;
            }
            // if just no plate instance, then return false
            if( plate_instance == null) {
                _log.Debug( "IsLocationAvailable returned false because plate instance was undefined");
                return false;
            }

            // if we are NOT spinning two plates (i.e. using a counterweight), always return false for bucket 2
            // as mentioned earlier, if spin_two_plates is null, we're removing the plate
            if( spin_two_plates == null) {
                // only want to allow removing of a plate if the plate instance asked for matches the expected bucket
                // odd-numbered plates always go into bucket 1
                // even-numbered plates always go into bucket 2
                int temp_index = (plate_instance.Value - 1) % 2;
                bool available = GetBucketPlateInstance(temp_index) == plate_instance.Value;
                _log.DebugFormat( "IsLocationAvailable returned {0}.  Spin two protocol plates was undefined.", available);
                return available;
            } else if( !spin_two_plates.Value) {
                // means bucket 1 is not occupied ----------------------vvvvvvvv
                bool available = bucket != Location2 && !IsBucketFull( Location1);
                _log.DebugFormat( "IsLocationAvailable returned {0}.  Bucket was not #2 and was {1}", available, GetBucketPlateInstance(Location1) == 0 ? "empty" : "full");
                return available;
            }
            // if we are spinning two protocol plates, then return whether or not the requested bucket is full
            int bucket_index = (bucket == Location1) ? 0 : 1;
            // only allow odd numbered plates to go to bucket 1, and even numbered plates to go to bucket 2
            if( (plate_instance % 2 != 0 && bucket == Location2) || (plate_instance % 2 == 0 && bucket == Location1)) {
                _log.Debug( "IsLocationAvailable returned false because the plate instance did not match the location");
                return false;
            }
            if( plate_instance % 2 != 0) {
                //_log.DebugFormat( "IsLocationAvailable called: {0}", LocationAvailableXML.Replace( '\n', ' '));
            }
            // we can use the bucket if it's empty, or if it's full and the instance we're asking about is the one currently in the bucket
            bool bucket_empty = !IsBucketFull( bucket_index);
            bool bucket_has_matching_instance = GetBucketPlateInstance(bucket_index) == plate_instance;
            if( bucket_empty || bucket_has_matching_instance) {
                _log.Debug( "IsLocationAvailable returned true.  Bucket is empty or has a matching plate instance");
                return true;
            }

            _log.Debug( "IsLocationAvailable returned false");
            return false;
        }

        private static bool? GetSpinTwoPlatesFromLocationXml( string LocationAvailableXML)
        {
            XDocument doc = XDocument.Parse(LocationAvailableXML);
            IEnumerable<XElement> parameters = doc.Descendants().Where( x => x.Name == "Parameter");
            if( parameters.Count() == 0)
                return null;
            XElement task_parameter = parameters.Where( x => x.Attribute("Name").Value == "Spin two protocol plates").First();
            if( task_parameter == null)
                return null;
            return task_parameter.Attribute("Value").Value == "1";
        }

        private static string GetLocationFromLocationXml(string LocationAvailableXML)
        {
            XDocument doc = XDocument.Parse(LocationAvailableXML);
            return doc.Descendants("LocationAvailable").First().Attribute("Location").Value;
        }

        private static int? GetPlateInstanceFromLocationXml( string LocationAvailableXML)
        {
            XDocument doc = XDocument.Parse(LocationAvailableXML);
            // some location metadata doesn't have PlateInstance!
            var plate_instance = doc.Descendants("LocationAvailable").First().Attribute("PlateInstance");
            if( plate_instance == null)
                return null;
            return int.Parse( plate_instance.Value);
        }

        public ReturnCode MakeLocationAvailable(string LocationAvailableXML)
        {
            _log.DebugFormat( "MakeLocationAvailable called: {0}", LocationAvailableXML.Replace( '\n', ' '));

            #region sample XML from VW3
            //<?xml version='1.0' encoding='ASCII' ?>
            //<Velocity11 file='MetaData' md5sum='1d8711f79b2cff6341fbd00ab31f4c2e' version='1.0' >
            //    <LocationAvailable Device='hig' Location='Bucket 1' Robot='robot' >
            //        <StorageLocation >
            //            <Location Group='0' MaxStackHeight='500' Offset='0' Type='1' />
            //        </StorageLocation>
            //        <Command Compiler='0' Description='Spin plate' Editor='2' Name='Spin' RequiresRefresh='0' TaskRequiresLocation='1' >
            //            <Parameters >
            //                <Parameter Name='Gs' Style='0' Type='8' Units='G' Value='500' >
            //                    <Ranges >
            //                        <Range Value='500' />
            //                        <Range Value='5000' />
            //                    </Ranges>
            //                </Parameter>
            //                <Parameter Name='Spin time' Style='0' Type='12' Units='s' Value='1' >
            //                    <Ranges >
            //                        <Range Value='1' />
            //                        <Range Value='3600' />
            //                    </Ranges>
            //                </Parameter>
            //                <Parameter Name='Acceleration' Style='0' Type='8' Units='%' Value='100' >
            //                    <Ranges >
            //                        <Range Value='1' />
            //                        <Range Value='100' />
            //                    </Ranges>
            //                </Parameter>
            //                <Parameter Name='Deceleration' Style='0' Type='8' Units='%' Value='100' >
            //                    <Ranges >
            //                        <Range Value='1' />
            //                        <Range Value='100' />
            //                    </Ranges>
            //                </Parameter>
            //                <Parameter Name='Used Location0' Style='0' Type='1' Value='Bucket 1' />
            //            </Parameters>
            //        </Command>
            //    </LocationAvailable>
            //</Velocity11>
            #endregion

            try
            {
                string bucket = GetLocationFromLocationXml( LocationAvailableXML);

                /*
                if( bucket == Location1) {
                    while( (_bucket_plate_instance_map[0] != 0))
                        Thread.Sleep( 100);
                } else {
                    while( (_bucket_plate_instance_map[1] != 0))
                        Thread.Sleep( 100);
                }
                 */

                ThreadSafeSetBusy( true);                
                _main_thread.Invoke( new Action( () => { _controller.PrintToLog( this, String.Format( "Opening to bucket {0}", bucket == Location1 ? 1 : 2)); } ));
                _log.Debug( "calling OpenShield()");
                _last_command_executed = new Action( () => { _hig.OpenShield( bucket == Location1 ? 0 : 1); } );
                _last_command_executed();
                _log.Debug( "waiting for OpenShield command complete event");
                WaitForCommandCompleteOrError();
                _log.Debug( "OpenShield signaled event");
                _command_complete_event.Reset();
                ThreadSafeSetBusy( false);
                return _last_command_result;
            }
            catch (Exception e)
            {
                _last_error = e.Message + "\n\nOnly Abort will work";
                return ReturnCode.RETURN_FAIL;
            }
            return ReturnCode.RETURN_SUCCESS;
        }

        public void ShowDiagsDialog(SecurityLevel iSecurity)
        {
            // DKM 2012-03-20 added blocking to prevent VW3 from continuing if user spins from diags
            _hig.Blocking = true;
            _hig.ShowDiagnostics( true);
            _hig.Blocking = false;
        }
        
        public string GetErrorInfo()
        {
            return _last_error;
        }

        public void Abort()
        {
            // reset the bucket contents!
            ClearBucketPlateInstance( Location1);
            ClearBucketPlateInstance( Location2);

            if( _command_complete_event == null)
                return;

            _command_complete_event.Set();
        }

        public ReturnCode Ignore()
        {
            // Ignore doesn't need to do anything
            return ReturnCode.RETURN_SUCCESS;
        }

        public ReturnCode Retry()
        {
            ThreadSafeSetBusy( true);
            _last_command_executed();            
            _command_complete_event.Reset();
            WaitForCommandCompleteOrError();
            ThreadSafeSetBusy( false);
            return _last_command_result;
        }

        #endregion

        /*
        #region IWorksDiags Members

        public ReturnCode CloseDiagsDialog()
        {
            return ReturnCode.RETURN_SUCCESS;
        }

        public ReturnCode IsDiagsDialogOpen()
        {
            return ReturnCode.RETURN_SUCCESS;
        }

        void IWorksDriver.IWorksDiags.ShowDiagsDialog(SecurityLevel iSecurity, bool bModal)
        {
            if( _hig == null)
                return;
            _hig.ShowDiagnostics( bModal);
            //_controller.OnCloseDiagsDialog( (CControllerClient)this);
        }

        #endregion
         */


        #region IControllerClient Members

        public void SetController(CWorksController Controller)
        {
            _controller = Controller;
            // DKM 2012-03-26 need to cache the main thread, otherwise you get a weird COM error that seems to have nothing to do with threading.
            _main_thread = Dispatcher.CurrentDispatcher;
        }

        #endregion
    }
}