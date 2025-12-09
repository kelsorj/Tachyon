using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Windows;
using BioNex.Shared.CommandInterpreter;
using BioNex.Shared.IError;
using BioNex.Shared.Teachpoints;
using BioNex.Shared.TechnosoftLibrary;
using BioNex.Shared.Utils;
using GalaSoft.MvvmLight.Messaging;
using log4net;

namespace BioNex.PlateMover
{
    [Export(typeof(Model))]
    public class Model
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Model));

        /* remove ITechnosoftConnectionSharer
        [ImportMany(typeof(ITechnosoftConnectionSharer))]
        private IEnumerable<ITechnosoftConnectionSharer> AllSharedConnections { get; set; }
        private ITechnosoftConnectionSharer Connection { get; set; }
        */
        private TechnosoftConnection Connection { get; set; }
        private IList<IAxis> _axes { get; set; }
        private Dictionary<string,string> DeviceProperties { get; set; }
        public Stage Stage { get; private set; }
        internal string DeviceInstanceName { get; set; }
        [Export("PrivateTechnosoftPort")]
        public string PrivateTechnosoftPort { get; private set; }
        [Import]
        private IError _error_interface { get; set; }

        public bool Connected { get; private set; }
        public bool YHomed
        { 
            get { return Stage == null ? false : Stage.YHomed; }
        }
        public bool RHomed
        {
            get { return Stage == null ? false : Stage.RHomed; }
        }
        public bool Homed {
            get { return YHomed && RHomed; }
        }
        public double TrackLength { get; private set; }

        // device manager properties
        public class Properties
        {
            public static readonly string Simulate = "simulate";
            public static readonly string Port = "port";
            public static readonly string HostDeviceName = "host device name"; // either "self" to use own TechnosoftConnection, or device instance name to piggyback
            public static readonly string ConfigFolder = "configuration folder";
            public static readonly string Self = "self";
        }

        private ThreadedUpdates Updater { get; set; }
        public double R { get; private set; }
        public double Y { get; private set; }

        // command listener
        private IWorksStackerListener Listener { get; set; }

        // teachpoints
        private string TeachpointFilePath { get; set; }
        private GenericTeachpointCollection< PlateMoverTeachpoint> Teachpoints { get; set; }

        public Model()
        {
            Teachpoints = new GenericTeachpointCollection< PlateMoverTeachpoint>();
            Messenger.Default.Register<Command>( this, HandleStackerCommand);
            // DKM 2011-02-25 don't need this anymore since we're now using XML-RPC (for the time being, at least)
            /*
            Listener = new IWorksStackerListener();
            Listener.StartListening( System.Net.IPAddress.Parse( "192.168.2.150"), 12345);
             */
        }

        public void SetDeviceProperties( Dictionary<string,string> properties)
        {
            DeviceProperties = properties;
        }

        public void Connect( bool connect)
        {
            try {
                if (connect) {
                    ReloadMotorSettings();
                    Updater = new ThreadedUpdates( "PlateMover position caching", UpdatePositions);

                    Updater.Start();

                    // load teachpoint file
                    TeachpointFilePath = (DeviceProperties[Properties.ConfigFolder] + "\\teachpoints.xml").ToAbsoluteAppPath();
                    try{
                        Teachpoints = GenericTeachpointCollection< PlateMoverTeachpoint>.LoadTeachpointsFromFile( TeachpointFilePath);
                    } catch( Exception ex){
                        MessageBox.Show( ex.Message);
                    }
                    
                    Connected = true;
                } else { 
                    if( Updater != null)
                        Updater.Stop();
                    // DKM 2012-01-18 need to close technosoft connection instead of buddy connection
                    //Connection.CloseBuddyConnection( DeviceInstanceName);
                    Connection.Close();
                    Connected = false;
                }
            } catch( TechnosoftException ex) {
                _log.Error(ex);
                MessageBox.Show(ex.Message, "PlateMoverPlugin");
            }
        }

        internal void ResetPauseAbort()
        {
            Connection.ResetPauseAbort();
        }

        private void ReloadMotorSettings()
        {
            bool simulate = DeviceProperties[Properties.Simulate] != "0";
            PrivateTechnosoftPort = DeviceProperties[Properties.Port];
            string motor_settings_path = (DeviceProperties[Properties.ConfigFolder] + "\\motor_settings.xml").ToAbsoluteAppPath();
            string tsm_setup_folder = DeviceProperties[Properties.ConfigFolder].ToAbsoluteAppPath();
            // the name of the device instance whose TechnosoftConnection we're going to use
            string host_device_name = DeviceProperties[Properties.HostDeviceName];

            // try to use shared connection first
            /* remove ITechnosoftConnectionSharer
            if( host_device_name != Properties.Self) {
                // iterate over AllSharedConnections to figure out which one we want to use
                foreach( ITechnosoftConnectionSharer sharer in AllSharedConnections) {
                    if( sharer.GetConnectionSharerName() != host_device_name)
                        continue;
                    // otherwise, we've found our connection
                    Connection = sharer;
                    break;
                }
                // at this point, Connection could be null if we couldn't find the host device that shares its TS connection
                _axes = Connection.LoadBuddyConfiguration( DeviceInstanceName, simulate, motor_settings_path, tsm_setup_folder);
            } else if( host_device_name == Properties.Self || Connection == null || simulate) {
                // interface with TechnosoftConnection through ITechnosoftConnectionSharer
                Connection = new PrivateTechnosoftConnection();
                _axes = Connection.LoadBuddyConfiguration( PrivateTechnosoftPort, simulate, motor_settings_path, tsm_setup_folder);
            }
            */
            if( simulate){
                Connection = new TechnosoftConnection();
            } else{
                Connection = new TechnosoftConnection( PrivateTechnosoftPort, TMLLibConst.CHANNEL_SYS_TEC_USBCAN, 500000);
            }
            Connection.LoadConfiguration( motor_settings_path, tsm_setup_folder);
            _axes = Connection.GetAxes().Values.ToList();

            // after loading the configuration, we should be able to get at the axis names to determine what
            // hardware configuration we're using.  Could be Y/R/YR
            // Y = linear 
            // R = rotary
            // declare axes for y and r.  We'll leave them as null if we can't find their names
            // in the returned axes.  Then we'll construct the Stage object.
            IAxis y = null;
            IAxis r = null;
            foreach( IAxis axis in _axes) {
                string axis_name = axis.Settings.AxisName;
                switch( axis_name.ToLower()) {
                    case "y":
                        y = axis;
                        break;
                    case "r":
                        r = axis;
                        break;
                }
            }

            // now create the Stage object.  It's perfectly okay for an axis to be null, since
            // the plugin supports "platemovers" that are linear, rotary, or both.
            Stage = new Stage( y, r);

            // 82.5 is the width of the stage
            // 10 is the gap we want to leave at the end of travel
            TrackLength = y != null ? y.Settings.MaxLimit + 82.5 + 10 : 1;
        }

        /// <summary>
        /// Non-blocking
        /// </summary>
        internal void HomeAxes( bool called_from_diags)
        {
            // replaced with state machine
            //Stage.Home();
            HomeStateMachine sm = new HomeStateMachine(this, _error_interface, called_from_diags);
            new Action( sm.Start).BeginInvoke( HomeAxesCompleteCallback, null);
            
        }

        private void HomeAxesCompleteCallback(IAsyncResult iar)
        {
            try
            {
                AsyncResult ar = (AsyncResult)iar;
                Action callback = (Action)ar.AsyncDelegate;
                callback.EndInvoke(iar);
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Could not home {0}: {1}", DeviceInstanceName, ex.Message));
            }
        }

        /// <summary>
        /// Non blocking
        /// </summary>
        internal void HomeY()
        {
            Stage.HomeY();
        }

        /// <summary>
        /// Non blocking
        /// </summary>
        internal void HomeR()
        {
            Stage.HomeR();
        }

        private void UpdatePositions()
        {
            try {
                R = Stage.GetRPos();
                Y = Stage.GetYPos();
            } catch( Exception) {
                // do nothing
            }
        }

        public void SaveHiveTeachpoint( int orientation)
        {
            Teachpoints.SetTeachpoint( new PlateMoverTeachpoint( orientation == 0 ? PlateMoverTeachpointNames.HiveLandscapeTeachpoint : PlateMoverTeachpointNames.HivePortraitTeachpoint, Y, R));
            GenericTeachpointCollection< PlateMoverTeachpoint>.SaveTeachpointsToFile( TeachpointFilePath, Teachpoints);
        }

        public void SaveExternalTeachpoint()
        {
            Teachpoints.SetTeachpoint( new PlateMoverTeachpoint( PlateMoverTeachpointNames.ExternalTeachpoint, Y, R));
            GenericTeachpointCollection< PlateMoverTeachpoint>.SaveTeachpointsToFile( TeachpointFilePath, Teachpoints);
        }

        public void MoveToHiveTeachpoint( int orientation)
        {
            MoveToTeachpoint( orientation == 0 ? PlateMoverTeachpointNames.HiveLandscapeTeachpoint : PlateMoverTeachpointNames.HivePortraitTeachpoint);
        }

        public void MoveToExternalTeachpoint()
        {
            MoveToTeachpoint( PlateMoverTeachpointNames.ExternalTeachpoint);
        }

        private void MoveToTeachpoint( string name)
        {
            PlateMoverTeachpoint tp = Teachpoints.GetTeachpoint( name);
            Stage.Move( tp.Y, tp.R);
        }

        private void HandleStackerCommand( Command cmd)
        {
            Debug.WriteLine( String.Format( "Received command '{0}'", cmd.Name));
            switch( cmd.Name.ToLower())
            {
                case "home":
                    HomeAxes( false);
                    break;
                case "mla":
                    MoveToExternalTeachpoint();
                    break;
                case "pdo":
                    MoveToHiveTeachpoint( 0);
                    break;
            }
        }

        public void ServoOn()
        {
            Stage.ServoOn();
        }

        public void ServoOff()
        {
            Stage.ServoOff();
        }

        public void StopAll()
        {
            Stage.StopAll();
        }
    }
}
