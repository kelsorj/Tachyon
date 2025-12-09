using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using BioNex.BumblebeePlugin.Dispatcher;
using BioNex.BumblebeePlugin.Hardware;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.PlateWork;
using BioNex.Shared.ThreadsafeMessenger;
using log4net;

namespace BioNex.BumblebeePlugin.Scheduler.DualChannelScheduler
{
    [ Export( typeof( IScheduler))]
    [ Export( typeof( IReportsStatus))]
    public class DualDisposableTipSplitScheduler : IScheduler, IReportsStatus, IDisposable
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        [ Import]
        private ILiquidProfileLibrary LiquidProfileLibrary { get; set; }
        [ Import( "MainDispatcher")]
        private System.Windows.Threading.Dispatcher WindowsDispatcher { get; set; }

        private BBHardware Hardware { get; set; }
        private ThreadsafeMessenger BumblebeeMessenger { get; set; }
        private BumblebeeDispatcher ProtocolDispatcher { get; set; }
        private ServiceSharedMemory SharedMemory { get; set; }
        private ITipBoxManager TipBoxManager { get; set; }
        private IRobotScheduler RobotScheduler { get; set; }

        private ChannelService ChannelService { get; set; }
        private CarrierBasedTipService TipService { get; set; }

        public string SchedulerName { get { return "Dual disposable tip scheduler"; }}
        public bool IsRunning { get; private set; }

        // ----------------------------------------------------------------------
        // class members.
        // ----------------------------------------------------------------------
        private static readonly ILog _log = LogManager.GetLogger( typeof( DualDisposableTipSplitScheduler));

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public DualDisposableTipSplitScheduler()
        {
            IsRunning = false;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public void Reset()
        {
        }
        // ----------------------------------------------------------------------
        public void Pause()
        {
            _log.Info( "Scheduler received Pause() call");
            BumblebeeMessenger.Send( new PauseCommand());
        }
        // ----------------------------------------------------------------------
        public void Resume()
        {
            _log.Info( "Scheduler received Resume() call");
            BumblebeeMessenger.Send( new ResumeCommand());
        }
        // ----------------------------------------------------------------------
        public void Abort()
        {
            _log.Info( "Scheduler received Abort() call");
            BumblebeeMessenger.Send( new AbortCommand());
            _log.Info( "Aborting... please wait until tasks currently in progress finish up.");
        }
        // ----------------------------------------------------------------------
        public void SetHardware( BBHardware hardware)
        {
            Hardware = hardware;
        }
        // ----------------------------------------------------------------------
        public void SetMessenger( ThreadsafeMessenger messenger)
        {
            BumblebeeMessenger = messenger;
        }
        // ----------------------------------------------------------------------
        public void SetDispatcher( BumblebeeDispatcher protocol_dispatcher)
        {
            ProtocolDispatcher = protocol_dispatcher;
        }
        // ----------------------------------------------------------------------
        public void SetSharedMemory( object shared_memory)
        {
            SharedMemory = shared_memory as ServiceSharedMemory;
        }
        // ----------------------------------------------------------------------
        public void SetTipBoxManager( object tip_box_manager)
        {
            TipBoxManager = tip_box_manager as ITipBoxManager;
        }
        // ----------------------------------------------------------------------
        public void SetRobotScheduler( object robot_scheduler)
        {
            RobotScheduler = robot_scheduler as IRobotScheduler;
        }
        // ----------------------------------------------------------------------
        public void StartScheduler()
        {
            // create services.
            ChannelService = new ChannelService( Hardware, LiquidProfileLibrary, BumblebeeMessenger, SharedMemory, ProtocolDispatcher);
            // FYC temporary tipshuttle work.
            // TipService = new TipService( Hardware, WindowsDispatcher, SharedMemory, ProtocolDispatcher, Hardware.Stages[ 3], LabwareDatabase.GetLabware( "tipbox") as TipBox, config_, TipBoxManager, RobotScheduler);
            TipService = new CarrierBasedTipService( Hardware, WindowsDispatcher, SharedMemory, ProtocolDispatcher, TipBoxManager, RobotScheduler);

            // start services.
            ChannelService.StartService();
            TipService.StartService();

            IsRunning = true;
        }
        // ----------------------------------------------------------------------
        public void StopScheduler()
        {
            IsRunning = false;

            // stop services.
            ChannelService.StopService();
            TipService.StopService();
            // dispose of services.
            ChannelService.Dispose();
        }
        // ----------------------------------------------------------------------
        public void PauseScheduler()
        {
            IsRunning = false;

            ChannelService.Pause();
            TipService.Pause();
        }
        // ----------------------------------------------------------------------
        public void ResumeScheduler()
        {
            ChannelService.Resume();
            TipService.Resume();

            IsRunning = true;
        }
        // ----------------------------------------------------------------------
        public void AddTransfers( TransferOverview to)
        {
            SharedMemory.AddTransfers( to.Transfers);
        }
        // ----------------------------------------------------------------------
        #region IDisposable Members
        public void Dispose()
        {
            BumblebeeMessenger.Unregister( this);
            GC.SuppressFinalize( this);
        }
        #endregion
        // ----------------------------------------------------------------------
        #region IReportsStatus Members
        /// <summary>
        /// This scheduler needs to report on SharedMemory contents
        /// </summary>
        /// <returns></returns>
        public string GetStatus()
        {
            if( SharedMemory == null)
                return "";

            StringBuilder sb = new StringBuilder();

            sb.AppendLine( SharedMemory.GetStatus());

            return sb.ToString();
        }
        #endregion
    }
}
