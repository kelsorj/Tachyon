using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using log4net;

namespace BioNex.Shared.StateMachineExecutor
{
    public interface IStateMachineExecutorStateMachine
    {
        void Start();
        void Flush();
        bool IsRunning();
    }

    /*
    public abstract class StateMachineExecutorStateMachine< TState, TTrigger> : StateMachineWrapper2< TState, TTrigger>, IStateMachineExecutorStateMachine
    {
        protected StateMachineExecutorStateMachine( Type state_machine_type, TState start_state, TState end_state, TState abort_state, TTrigger success_trigger, TTrigger failure_trigger, TTrigger retry_trigger, TTrigger ignore_trigger, TTrigger abort_trigger, IError.IError error_interface, bool called_from_diags)

        {
        }

        #region INSStateMachine Members
        public void Flush()
        {
            Execute( AbortTrigger);
        }

        public bool IsRunning()
        {
            return SMStopwatch.IsRunning;
        }
        #endregion
    }
    */

    public class StateMachineExecutor : IDisposable
    {
        // ----------------------------------------------------------------------
        // inner classes.
        // ----------------------------------------------------------------------
        public class ExecutorInfo
        {
            public enum Status
            {
                Idle,
                Running, /* currently, includes paused executors */
                InError,
                Disabled,
            }

            public Status CurrentStatus { get; private set; }
            public double TimeToIdle { get; private set; }

            public ExecutorInfo( Status current_status, double time_to_idle)
            {
                CurrentStatus = current_status;
                TimeToIdle = time_to_idle;
            }
        }

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public bool Busy { get { return StateMachineInProgress() || GetQueueDepth() > 0; }}
        private Object Owner { get; set; }
        public string Name { get; private set; }
        private ConcurrentQueue< Tuple< IStateMachineExecutorStateMachine, ManualResetEvent>> StateMachineQueue { get; set; }
        private static readonly ILog Log = LogManager.GetLogger( typeof( StateMachineExecutor));

        private ManualResetEvent StopThreadEvent { get; set; }
        private Thread ExecutorThread { get; set; }

        // shared memory.
        private readonly Object status_lock_ = new Object();
        private Tuple< IStateMachineExecutorStateMachine, ManualResetEvent> current_state_machine_;
        private volatile bool disabled_;

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public StateMachineExecutor( Object owner, string name = null)
        {
            Owner = owner;
            Name = ( name ?? Owner.ToString());
            StateMachineQueue = new ConcurrentQueue< Tuple< IStateMachineExecutorStateMachine, ManualResetEvent>>();
            disabled_ = false;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public ExecutorInfo GetExecutorInfo()
        {
            lock( status_lock_){
                if( disabled_){
                    return new ExecutorInfo( ExecutorInfo.Status.Disabled, double.PositiveInfinity);
                }
                if( current_state_machine_ == null){
                    return new ExecutorInfo( ExecutorInfo.Status.Idle, 0);
                }
                if( !current_state_machine_.Item1.IsRunning()){
                    return new ExecutorInfo( ExecutorInfo.Status.InError, double.PositiveInfinity);
                }
                return new ExecutorInfo( ExecutorInfo.Status.Running, StateMachineQueue.Count);
            }
        }
        // ----------------------------------------------------------------------
        public void Start( string name = null)
        {
            StopThreadEvent = new ManualResetEvent( false);
            ExecutorThread = new Thread( new ThreadStart( ExecutorThreadRunner));
            if( name == null){
                ExecutorThread.Name = String.Format( "Executor: {0}", Name);
            } else{
                ExecutorThread.Name = name;
            }
            ExecutorThread.IsBackground = true;
            ExecutorThread.Start();
        }
        // ----------------------------------------------------------------------
        public void Stop()
        {
            StopThreadEvent.Set();
            ExecutorThread.Join();
        }
        // ----------------------------------------------------------------------
        public void Flush()
        {
            Tuple< IStateMachineExecutorStateMachine, ManualResetEvent> discard;
            while( StateMachineQueue.TryDequeue( out discard));
            if( current_state_machine_ != null){
                current_state_machine_.Item1.Flush();
            }
        }
        // ----------------------------------------------------------------------
        public void Disable()
        {
            disabled_ = true;
        }
        // ----------------------------------------------------------------------
        public void AddStateMachine( IStateMachineExecutorStateMachine state_machine, ManualResetEvent state_machine_ended_or_aborted_event = null)
        {
            StateMachineQueue.Enqueue( Tuple.Create( state_machine, state_machine_ended_or_aborted_event));
        }
        // ----------------------------------------------------------------------
        public bool StateMachineInProgress()
        {
            return current_state_machine_ != null;
        }
        // ----------------------------------------------------------------------
        public int GetQueueDepth()
        {
            return StateMachineQueue != null ? StateMachineQueue.Count : 0;
        }
        // ----------------------------------------------------------------------
        #region IDisposable Members
        public void Dispose()
        {
            StopThreadEvent.Set();
        }
        #endregion

        // ----------------------------------------------------------------------
        // private methods.
        // ----------------------------------------------------------------------
        private void ExecutorThreadRunner()
        {
            bool is_idle = true;
            Stopwatch stopwatch = Stopwatch.StartNew();
            int thisref = GetHashCode();
            while( !StopThreadEvent.WaitOne( 10)){
                lock( status_lock_){
                    StateMachineQueue.TryDequeue( out current_state_machine_);
                }
                if( current_state_machine_ != null){
                    if( is_idle){
                        is_idle = false;
                        stopwatch.Stop();
                        Log.DebugFormat( "{0} executor was idle for {1}", Name, stopwatch.Elapsed.ToString("G"));
                    }
                    if( disabled_){
                        Log.DebugFormat( "{0} executor starting to flush state machine", Name);
                        current_state_machine_.Item1.Flush();
                        Log.DebugFormat( "{0} executor finished flushing state machine", Name);
                    } else{
                        Log.DebugFormat( "{0} executor starting to run state machine", Name);
                        current_state_machine_.Item1.Start();
                        Log.DebugFormat( "{0} executor finished running state machine", Name);
                    }
                    if( current_state_machine_.Item2 != null){
                        current_state_machine_.Item2.Set();
                    }
                } else{
                    if( !is_idle){
                        is_idle = true;
                        stopwatch.Restart();
                    }
                }
            }
            Log.DebugFormat( "{0} executor stopping", Name);
        }
    }
}
