using BioNex.BumblebeePlugin.Dispatcher;
using BioNex.BumblebeePlugin.Hardware;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.ThreadsafeMessenger;

namespace BioNex.BumblebeePlugin.Scheduler
{
    public interface IScheduler
    {
        /// <summary>
        /// Used to present the user with a friendly name for the scheduler,
        /// for selection purposes.
        /// </summary>
        /// <returns></returns>
        string SchedulerName { get; }
        bool IsRunning { get; }

        /// <summary>
        /// The scheduler interface needs to allow the various implementations
        /// to clear their data from previously-run protocols
        /// </summary>
        void Reset();

        void Pause();
        void Resume();
        void Abort();

        void SetHardware( BBHardware hw);
        void SetMessenger( ThreadsafeMessenger messenger);
        void SetDispatcher( BumblebeeDispatcher protocol_dispatcher);
        void SetSharedMemory( object shared_memory);
        void SetTipBoxManager( object tip_box_manager);
        void SetRobotScheduler( object robot_scheduler);

        void StartScheduler();
        void StopScheduler();

        void PauseScheduler();
        void ResumeScheduler();

        void AddTransfers( TransferOverview to);
    }
}
