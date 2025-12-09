using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using log4net;

namespace BioNex.Shared.IWorksDriverExecutionStateMachine
{
    public class V11TaskAbortedException : Exception
    {
    }

    /// <summary>
    /// Manages creation of IWorksDrivers on a separate thread, as well as invokes all methods
    /// on them via the IWorksDriverExecutionStateMachine.
    /// </summary>
    [Export]
    public class IWorksDriverExecutor
    {
        [Import]
        private IError.IError _error_interface { get; set; }

        public class DriverAlreadyCreatedException : Exception
        {
            public DriverAlreadyCreatedException( string message)
                : base( message)
            {
            }
        }

        private abstract class ITask
        {
            public ManualResetEvent TaskComplete { get; set; }
            public ITask() { TaskComplete = new ManualResetEvent( false); }
            public abstract void ExecuteTask();
        }

        private abstract class BooleanTask : ITask
        {
            public bool Result { get; set; }
        }

        private abstract class StringTask : ITask
        {
            public string Result { get; set; }
        }

        private class CreatePlateLocTask : ITask
        {
            public Dictionary<string,IWorksDriverLib.IWorksDriver> DriverMap { get; set; }
            public string ProfileName { get; set; }

            private static readonly ILog _log = LogManager.GetLogger(typeof(CreatePlateLocTask));

            public override void ExecuteTask()
            {
                try
                {
                    lock( DriverMap) {
                        DriverMap[ProfileName] = (IWorksDriverLib.IWorksDriver)new PlateLocLib.PlateLocCoClass();
                    }
                }
                catch (System.Runtime.InteropServices.COMException e)
                {
                    _log.Fatal(e);
                }
            }
        }

        private class SetControllerTask : ITask
        {
            public Dictionary<string,IWorksDriverLib.IWorksDriver> DriverMap { get; set; }
            public string ProfileName { get; set; }
            public IWorksDriverLib.CWorksController Controller { get; set;}

            private static readonly ILog _log = LogManager.GetLogger(typeof(SetControllerTask));

            public override void ExecuteTask()
            {
                try
                {
                    lock( DriverMap) {
                        ((IWorksDriverLib.IControllerClient)DriverMap[ProfileName]).SetController( Controller);
                    }
                }
                catch (KeyNotFoundException e)
                {
                    _log.Fatal(e);
                }
            }
        }

        private class CreatePlatePierceTask : ITask
        {
            public Dictionary<string,IWorksDriverLib.IWorksDriver> DriverMap { get; set; }
            public string ProfileName { get; set; }

            private static readonly ILog _log = LogManager.GetLogger(typeof(CreatePlatePierceTask));

            public override void ExecuteTask()
            {
                try
                {
                    lock (DriverMap)
                    {
                        DriverMap[ProfileName] = (IWorksDriverLib.IWorksDriver)new PlatePierceLib.PlatePierceIWorksDriver();
                    }
                }
                catch (System.IO.FileNotFoundException e)
                {
                    _log.Fatal(e);
                }
            }
        }

        private class CreateVStackTask : ITask
        {
            public Dictionary<string,IWorksDriverLib.IWorksDriver> DriverMap { get; set; }
            public Dictionary<string,IWorksDriverLib.IStackerDriver> StackerMap { get; set; }
            public string ProfileName { get; set; }

            private static readonly ILog _log = LogManager.GetLogger(typeof(CreateVStackTask));

            public override void ExecuteTask()
            {
                try
                {
                    lock( DriverMap) {
                        DriverMap[ProfileName] = (IWorksDriverLib.IWorksDriver)new VSTACKBIONETLib.VStackBioNetPlugin();
                    }
                    lock( StackerMap) {
                        StackerMap[ProfileName] = (IWorksDriverLib.IStackerDriver)DriverMap[ProfileName];
                    }
                }
                catch (System.IO.FileNotFoundException e)
                {
                    _log.Fatal(e);
                }
            }
        }

        private class InitializeTask : BooleanTask
        {
            public IWorksDriverLib.IWorksDriver Driver { get; set; }
            public string CommandXml { get; set; }

            public override void ExecuteTask()
            {
                Result = Driver.Initialize( CommandXml) == IWorksDriverLib.ReturnCode.RETURN_SUCCESS;
            }
        }

        private class GetErrorInfoTask : StringTask
        {
            public IWorksDriverLib.IWorksDriver Driver { get; set; }

            public override void ExecuteTask()
            {
                Result = Driver.GetErrorInfo();
            }
        }

        private class CloseTask : ITask
        {
            public IWorksDriverLib.IWorksDriver Driver { get; set; }

            public override void ExecuteTask()
            {
                Driver.Close();
            }
        }

        private class ExecuteCommandTask : ITask
        {
            public string DeviceName { get; set; }
            public IWorksDriverLib.IWorksDriver Driver { get; set; }
            public string CommandXml { get; set; }
            public IError.IError ErrorInterface { get; set; }

            public override void ExecuteTask()
            {
                IWorksDriverExecutionStateMachine sm = new IWorksDriverExecutionStateMachine( DeviceName, Driver, new Func<int>( () => { return (int)Driver.Command( CommandXml); }), ErrorInterface);
                try {
                    sm.Start();
                } catch( V11TaskAbortedException) {
                }
            }
        }

        private class ShowDiagsDialogTask : ITask
        {
            public IWorksDriverLib.IWorksDriver Driver { get; set; }
            public IWorksDriverLib.SecurityLevel SecurityLevel { get; set; }

            public override void ExecuteTask()
            {
                Driver.ShowDiagsDialog( SecurityLevel);
            }
        }

        private class LoadStackTask : ITask
        {
            public string DeviceName { get; set; }
            public IWorksDriverLib.IStackerDriver Stacker { get; set; }
            public string Labware { get; set; }
            public IWorksDriverLib.PlateFlagsType PlateFlags { get; set; }
            public string LocationName { get; set; }
            public IError.IError ErrorInterface { get; set; }

            public override void ExecuteTask()
            {
                IWorksStackerExecutionStateMachine sm = new IWorksStackerExecutionStateMachine( DeviceName, Stacker, new Func<int>( () => { return (int)Stacker.LoadStack( Labware, PlateFlags, LocationName); }), ErrorInterface);
                try {
                    sm.Start();
                } catch( V11TaskAbortedException) {
                }
            }
        }

        private class UnloadStackTask : ITask
        {
            public string DeviceName { get; set; }
            public IWorksDriverLib.IStackerDriver Stacker { get; set; }
            public string Labware { get; set; }
            public IWorksDriverLib.PlateFlagsType PlateFlags { get; set; }
            public string LocationName { get; set; }
            public IError.IError ErrorInterface { get; set; }

            public override void ExecuteTask()
            {
                IWorksStackerExecutionStateMachine sm = new IWorksStackerExecutionStateMachine( DeviceName, Stacker, new Func<int>( () => { return (int)Stacker.UnloadStack( Labware, PlateFlags, LocationName); }), ErrorInterface);
                try {
                    sm.Start();
                } catch( V11TaskAbortedException) {
                }
            }
        }

        private class SinkPlateTask : ITask
        {
            public string DeviceName { get; set; }
            public IWorksDriverLib.IStackerDriver Stacker { get; set; }
            public string Labware { get; set; }
            public IWorksDriverLib.PlateFlagsType PlateFlags { get; set; }
            public string LocationName { get; set; }
            public IError.IError ErrorInterface { get; set; }

            public override void ExecuteTask()
            {
                IWorksStackerExecutionStateMachine sm = new IWorksStackerExecutionStateMachine( DeviceName, Stacker, new Func<int>( () => { return (int)Stacker.SinkPlate( Labware, PlateFlags, LocationName); }), ErrorInterface);
                try {
                    sm.Start();
                } catch( V11TaskAbortedException) {
                }
            }
        }

        private class SourcePlateTask : ITask
        {
            public string DeviceName { get; set; }
            public IWorksDriverLib.IStackerDriver Stacker { get; set; }
            public string Labware { get; set; }
            public IWorksDriverLib.PlateFlagsType PlateFlags { get; set; }
            public string LocationName { get; set; }
            public IError.IError ErrorInterface { get; set; }

            public override void ExecuteTask()
            {
 	            IWorksStackerExecutionStateMachine sm = new IWorksStackerExecutionStateMachine( DeviceName, Stacker, new Func<int>( () => { return (int)Stacker.SourcePlate( Labware, PlateFlags, LocationName); }), ErrorInterface);
                try {
                    sm.Start();
                } catch( V11TaskAbortedException) {
                }
            }
        }

        private class IsStackEmptyTask : BooleanTask
        {
            public IWorksDriverLib.IStackerDriver Stacker { get; set; }
            public string LocationName { get; set; }

            public override void ExecuteTask()
            {
                Result = Stacker.IsStackEmpty( LocationName) != 0;
            }
        }

        /// <summary>
        /// get an IWorksDriver reference by passing its profile name
        /// </summary>
        private readonly Dictionary<string,IWorksDriverLib.IWorksDriver> _driver_map = new Dictionary<string,IWorksDriverLib.IWorksDriver>();
        private readonly Dictionary<string,IWorksDriverLib.IStackerDriver> _stacker_map = new Dictionary<string,IWorksDriverLib.IStackerDriver>();
        /// <summary>
        /// Used to store metadata for a device when it is initialized, so that we can reinitialize if necessary, without having to pass metadata around
        /// </summary>
        private readonly Dictionary<object,string> _metadata_map = new Dictionary<object,string>();
        private readonly Thread _thread;
        private readonly Queue<ITask> _work_queue = new Queue<ITask>();
        private readonly Object _work_queue_lock = new Object();
        private readonly ManualResetEvent _exit_event = new ManualResetEvent( false);
        private readonly ManualResetEvent _shutting_down_event = new ManualResetEvent( false);
        private const int WORK_QUEUE_WAIT_INTERVAL = 1; //ms to sleep between checks while work queue is empty
        private const int WORK_QUEUE_QUIET_INTERVAL = 10; //ms once this time has elapsed between queue commands, queue will start sleeping
        private DateTime _last_queue_time;

        public IWorksDriverExecutor()
        {
            _thread = new Thread( ExecuteIWorksCommandThread);
            _thread.SetApartmentState( ApartmentState.STA);
            _thread.Name = "IWorksDriver proxy thread";
            _thread.IsBackground = true;
            _thread.Start();
        }

        ~IWorksDriverExecutor()
        {
            _exit_event.Set();
        }

        void ExecuteIWorksCommandThread()
        {
            _last_queue_time = DateTime.Now;

            while (!_exit_event.WaitOne(0))
            {
                var now = DateTime.Now;
                bool empty = true;
                lock (_work_queue_lock) {
                    empty = _work_queue.Count == 0;
                }
                if (empty) {
                    // when cycling over an empty queue, go to sleep if it's been a while since we last received a message
                    // I do this rather than sleep every cycle to reduce overall latency.  But when we're Idle I don't want to be busy-waiting the CPU
                    var delta = now - _last_queue_time;
                    if (delta.TotalMilliseconds > WORK_QUEUE_QUIET_INTERVAL)
                        Thread.Sleep(WORK_QUEUE_WAIT_INTERVAL);
                    continue;
                }
                ITask next_work;
                lock (_work_queue_lock) {
                    next_work = _work_queue.Dequeue();
                }
                try {
                    next_work.ExecuteTask();
                } finally {
                    _last_queue_time = DateTime.Now;
                    next_work.TaskComplete.Set();
                }
            }
        }

        private void WaitForTaskComplete( ITask task)
        {
            if( _shutting_down_event.WaitOne(0))
                return;

            if( _thread == null)
                return;

            lock(_work_queue_lock) {
                _work_queue.Enqueue( task);
            }
            task.TaskComplete.WaitOne();
        }

        private bool WaitForBoolTaskComplete( BooleanTask task)
        {
            WaitForTaskComplete( task);
            return task.Result;
        }

        private string WaitForStringTaskComplete( StringTask task)
        {
            WaitForTaskComplete( task);
            return task.Result;
        }

        // ----------------------------------------------------------------------------------------
        // public methods called by synapsis
        // ----------------------------------------------------------------------------------------
        public void CreatePlateLocDriver( string profile_name, IWorksDriverLib.CWorksController controller=null)
        {
            lock( _driver_map) {
                if( _driver_map.ContainsKey( profile_name))
                    throw new DriverAlreadyCreatedException( String.Format( "The profile '{0}' already has an IWorksDriver instance created for it", profile_name));
            }
            WaitForTaskComplete( new CreatePlateLocTask() { DriverMap = _driver_map, ProfileName = profile_name });
            if (controller != null)
                WaitForTaskComplete( new SetControllerTask() { DriverMap = _driver_map, ProfileName = profile_name, Controller = controller });
        }

        public void CreateVStackDriver( string profile_name, IWorksDriverLib.CWorksController controller=null)
        {
            lock( _driver_map) {
                if( _driver_map.ContainsKey( profile_name))
                    throw new DriverAlreadyCreatedException( String.Format( "The profile '{0}' already has an IWorksDriver instance created for it", profile_name));
            }
            lock( _stacker_map) {
                if( _stacker_map.ContainsKey( profile_name))
                    throw new DriverAlreadyCreatedException( String.Format( "The profile '{0}' already has an IStackerDriver instance created for it", profile_name));
            }
            WaitForTaskComplete( new CreateVStackTask() { DriverMap = _driver_map, StackerMap = _stacker_map, ProfileName = profile_name });
            if (controller != null)
                WaitForTaskComplete( new SetControllerTask() { DriverMap = _driver_map, ProfileName = profile_name, Controller = controller });
        }

        public void CreatePlatePierceDriver( string profile_name, IWorksDriverLib.CWorksController controller=null)
        {
            lock( _driver_map) {
                if( _driver_map.ContainsKey( profile_name))
                    throw new DriverAlreadyCreatedException( String.Format( "The profile '{0}' already has an IWorksDriver instance created for it", profile_name));
            }
            WaitForTaskComplete( new CreatePlatePierceTask() { DriverMap = _driver_map, ProfileName = profile_name });
            if (controller != null)
                WaitForTaskComplete( new SetControllerTask() { DriverMap = _driver_map, ProfileName = profile_name, Controller = controller });
        }

        /// <summary>
        /// Initializes a V11 device driver
        /// </summary>
        /// <param name="profile_name"></param>
        /// <param name="xml">Metadata</param>
        public bool Initialize( string profile_name, string xml)
        {
            IWorksDriverLib.IWorksDriver driver = null;
            lock( _driver_map) {
                driver = _driver_map[profile_name];
                // save the metadata in case we want to call reinitialize
                _metadata_map[driver] = xml;
            }
            return WaitForBoolTaskComplete( new InitializeTask() { Driver = driver, CommandXml = xml });
        }

        public string GetErrorInfo( string profile_name)
        {
            IWorksDriverLib.IWorksDriver driver = null;
            lock( _driver_map) {
                driver = _driver_map[profile_name];
            }
            return WaitForStringTaskComplete( new GetErrorInfoTask() { Driver = driver });
        }

        public void Close( string profile_name)
        {
            IWorksDriverLib.IWorksDriver driver = null;
            lock( _driver_map) {
                driver = _driver_map[profile_name];
            }
            WaitForTaskComplete( new CloseTask() { Driver = driver });
        }

        /// <summary>
        /// ExecuteCommand doesn't return anything, because the idea is that the state machine is expected to
        /// spin until the user has either made the device successfully complete its task, or the user has
        /// clicked Abort to abort running this task.
        /// </summary>
        /// <param name="profile_name"></param>
        /// <param name="command_xml"></param>
        public void ExecuteCommand( string profile_name, string command_xml)
        {
            IWorksDriverLib.IWorksDriver driver = null;
            lock( _driver_map) {
                driver = _driver_map[profile_name];
            }
            WaitForTaskComplete( new ExecuteCommandTask() { DeviceName = profile_name, Driver = driver, CommandXml = command_xml, ErrorInterface = _error_interface });
        }

        public void ShowDiagsDialog( string profile_name, IWorksDriverLib.SecurityLevel security_level)
        {
            IWorksDriverLib.IWorksDriver driver = null;
            lock( _driver_map) {
                driver = _driver_map[profile_name];
            }
            WaitForTaskComplete( new ShowDiagsDialogTask() { Driver = driver, SecurityLevel = security_level });
        }

        public void LoadStack( string profile_name, string labware, IWorksDriverLib.PlateFlagsType plate_flags, string location_name)
        {
            IWorksDriverLib.IStackerDriver stacker = null;
            lock( _stacker_map) {
                stacker = _stacker_map[profile_name];
            }
            WaitForTaskComplete( new LoadStackTask() { DeviceName = profile_name, Stacker = stacker, Labware = labware, PlateFlags = plate_flags, LocationName = location_name });
        }
        
        public void UnloadStack( string profile_name, string labware, IWorksDriverLib.PlateFlagsType plate_flags, string location_name)
        {
            IWorksDriverLib.IStackerDriver stacker = null;
            lock( _stacker_map) {
                stacker = _stacker_map[profile_name];
            }
            WaitForTaskComplete( new UnloadStackTask() { DeviceName = profile_name, Stacker = stacker, Labware = labware, PlateFlags = plate_flags, LocationName = location_name });
        }

        public void SinkPlate( string profile_name, string labware, IWorksDriverLib.PlateFlagsType plate_flags, string location_name)
        {
            IWorksDriverLib.IStackerDriver stacker = null;
            lock( _stacker_map) {
                stacker = _stacker_map[profile_name];
            }
            WaitForTaskComplete( new SinkPlateTask() { DeviceName = profile_name, Stacker = stacker, Labware = labware, PlateFlags = plate_flags, LocationName = location_name, ErrorInterface = _error_interface });
        }

        public void SourcePlate( string profile_name, string labware, IWorksDriverLib.PlateFlagsType plate_flags, string location_name)
        {
            IWorksDriverLib.IStackerDriver stacker = null;
            lock( _stacker_map) {
                stacker = _stacker_map[profile_name];
            }
            WaitForTaskComplete( new SourcePlateTask() { DeviceName = profile_name, Stacker = stacker, Labware = labware, PlateFlags = plate_flags, LocationName = location_name, ErrorInterface = _error_interface });
        }

        public bool IsStackEmpty( string profile_name, string location_name)
        {
            IWorksDriverLib.IStackerDriver stacker = null;
            lock( _stacker_map) {
                stacker = _stacker_map[profile_name];
            }
            return WaitForBoolTaskComplete( new IsStackEmptyTask() { Stacker = stacker, LocationName = location_name });
        }

        public bool Reinitialize( string profile_name)
        {
            string metadata;
            lock( _driver_map)
            {
                IWorksDriverLib.IWorksDriver driver = _driver_map[profile_name];
                // save the metadata in case we want to call reinitialize
                metadata = _metadata_map[driver];
            }
            return Initialize( profile_name, metadata);
        }
    }
}
