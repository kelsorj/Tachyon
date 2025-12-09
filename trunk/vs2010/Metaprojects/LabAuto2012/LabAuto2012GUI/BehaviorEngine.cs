using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using BioNex.HivePrototypePlugin;
using BioNex.Plugins.Dock;
// using log4net;
using Stateless;

namespace BioNex.CustomerGUIPlugins
{
    public class BehaviorEngine : StateMachine<BehaviorEngine.State, BehaviorEngine.Trigger>
    {
        // private ILog Log = LogManager.GetLogger( typeof( BehaviorEngine));

        private Dictionary<string,DockMonitorPlugin> _docks { get; set; }
        private LabAuto2012GUI _plugin { get; set; }
        /// <summary>
        /// flag to indicate that plates are being moved, but the process was interrupted by
        /// a request to load a plate from the external system.
        /// </summary>
        private bool _move_interrupted { get; set; }

        /// <summary>
        /// flag to allow FireResume to kick FirePause out of its Idle checking loop
        /// </summary>
        private bool _resumed;
        private bool _first_resume = true;

        public bool WaitingForGo { get; private set; }

        /// <summary>
        /// Set this to abort a queued upstack / downstack operation (i.e. stacker is empty, and then VWorks shuts down)
        /// </summary>
        private AutoResetEvent _abort_upstack_downstack_event;

        // using events for each state transition, so that the GUI can update automatically
        public event EventHandler InIdle;
        public event EventHandler InNotHomed;
        public event EventHandler InHoming;
        public event EventHandler InMovingPlates;
        public event EventHandler InProcessingWorklist;
        public event EventHandler InReinventorying;
        public event EventHandler InReinventoryingHive;
        public event EventHandler InDockingCart;
        public event EventHandler InUndockingCart;
        public event EventHandler InPaused;
        public event EventHandler InShutdown;
        public event EventHandler Resuming;

        public bool UserRequestedReinventory { get; private set; }
        public bool RequestReinventory()
        { 
            UserRequestedReinventory = true; 
            return FireReinventoryHive();
        }

        private readonly int STATE_CHECK_IDLE_TIME = 10;

        new public enum State
        {
            NotHomed,
            Homing,
            Idle,
            ProcessingWorklist,
            MovingPlates,
            Reinventorying,
            ReinventoryingHive,
            DockingCart,
            UndockingCart,
            Paused,
            Resuming,
            Done,
            Shutdown,
            PausedWhileMovingPlates,
            ResumedWhileMovingPlates,
        }

        public enum Trigger
        {
            Home,
            HomeComplete,
            HomeFailed,
            HomedAtInit,
            ReinventoryStorage,
            ReinventoryHive,
            MovePlate,
            DoneMoving,
            StartProcessing,
            DoneProcessing,
            DoneReinventorying,
            DoneReinventoryingHive,
            Abort,
            Retry,
            Ignore,
            Error,
            DockCartRequested,
            UndockCartRequested,
            DoneDocking,
            DoneUndocking,
            Pause,
            Resuming,
            Resumed,
            Shutdown,
        }

        public BehaviorEngine( LabAuto2012GUI plugin)
            : base( State.NotHomed)
        {
            _docks = plugin._docks;
            _plugin = plugin;
            _abort_upstack_downstack_event = new AutoResetEvent( false);
            
            // register ReinventoryComplete event handlers
            foreach (var kvp in _docks)
            {
                _docks[kvp.Key].ReinventoryComplete += new EventHandler(BehaviorEngine_ReinventoryComplete);
                _docks[kvp.Key].ReinventoryError += new EventHandler(BehaviorEngine_ReinventoryError);
            }

            // register Hive reinventory event handlers as well
            HivePlugin hive = _plugin._robot as HivePlugin;
            if( hive != null) {
                hive.ReinventoryComplete += new EventHandler(hive_ReinventoryComplete);
                hive.ReinventoryError += new EventHandler(hive_ReinventoryError);
            }

            // DKM 2011-06-09 Paused is now the first place we'll be when we either start homed or rehome
            WaitingForGo = true;
            Configure(State.NotHomed)
                .Ignore(Trigger.Resuming) // clicked Go Live
                .Permit(Trigger.HomedAtInit, State.Paused) // home state checked during call to CompositionComplete()
                .Permit(Trigger.Home, State.Homing)
                .Permit(Trigger.Shutdown, State.Shutdown)
                .OnEntry(() => {
                    if (InNotHomed != null)
                        InNotHomed(this, null);
                });
            Configure(State.Homing)
                .Ignore(Trigger.Resuming)
                .Permit(Trigger.HomeComplete, State.Paused)
                .Permit(Trigger.HomeFailed, State.NotHomed)
                .Permit(Trigger.Shutdown, State.Shutdown)
                .OnEntry( () => {
                    if( InHoming != null)
                        InHoming( this, null);
                });
            Configure(State.Idle)
                .Permit(Trigger.Home, State.Homing)
                .Permit(Trigger.MovePlate, State.MovingPlates)
                .Permit(Trigger.StartProcessing, State.ProcessingWorklist)
                .Permit(Trigger.ReinventoryStorage, State.Reinventorying)
                .Permit(Trigger.ReinventoryHive, State.ReinventoryingHive)
                .Permit(Trigger.DockCartRequested, State.DockingCart)
                .Permit(Trigger.UndockCartRequested, State.UndockingCart)
                .Permit(Trigger.Pause, State.Paused)
                .Permit(Trigger.Shutdown, State.Shutdown)
                // DKM 2011-06-15 added these two ignores because it's possible that a user doesn't do what he's supposed
                //                to do when docking / undocking, and we might not have actually transitioned yet from
                //                Idle -> Docking/Undocking, and when a timeout occurs, we'll fire DoneDocking / DoneUndocking
                .Ignore(Trigger.DoneDocking)
                .Ignore(Trigger.DoneUndocking)
                .OnEntry(() => {
                    // Log.Info( "On enter IDLE");
                    if( InIdle != null)
                        InIdle( this, null);
                    Debug.WriteLine("BehaviorEngine Idle");
                });
            Configure( State.ProcessingWorklist)
                .Ignore( Trigger.Resuming)
                .Permit(Trigger.Shutdown, State.Shutdown)
                .Permit(Trigger.DoneProcessing, State.Idle)
                .OnEntry( () => {
                    if( InProcessingWorklist != null)
                        InProcessingWorklist( this, null);
                });
            Configure( State.MovingPlates)
                .Ignore(Trigger.Resuming)
                .Permit(Trigger.Shutdown, State.Shutdown)
                .Permit(Trigger.DoneMoving, State.Idle)
                .Permit(Trigger.Pause, State.PausedWhileMovingPlates)
                .OnEntryFrom( Trigger.Resumed, NoOp)
                .OnEntry( () => {
                    // Log.Info( "On enter MOVING PLATES");
                    if( InMovingPlates != null)
                        InMovingPlates( this, null);
                });
            Configure( State.PausedWhileMovingPlates)
                .SubstateOf( State.MovingPlates)
                .Ignore(Trigger.Pause)
                .Permit(Trigger.DoneMoving, State.Paused)
                .Permit(Trigger.Resuming, State.ResumedWhileMovingPlates)
                .OnEntry( () => {
                    // Log.Info( "On enter PAUSED WHILE MOVING PLATES");
                    Pause();
                });
            Configure( State.ResumedWhileMovingPlates)
                .Ignore(Trigger.Pause)
                .Permit(Trigger.Shutdown, State.Shutdown)
                .Permit(Trigger.Resumed, State.MovingPlates)
                .OnEntry( () => {
                    // Log.Info( "On enter RESUMED WHILE MOVING PLATES");
                    Resume();
                });
            Configure( State.Reinventorying)
                .Ignore(Trigger.Resuming)
                .Permit(Trigger.Shutdown, State.Shutdown)
                .Permit(Trigger.DoneReinventorying, State.Idle)
                .OnEntry( () => {
                    if( InReinventorying != null)
                        InReinventorying( this, null);
                });
            Configure( State.ReinventoryingHive)
                .Ignore(Trigger.Resuming)
                .Permit(Trigger.Shutdown, State.Shutdown)
                .Permit(Trigger.DoneReinventoryingHive, State.Idle)
                .OnEntry( () => {
                    if( InReinventoryingHive != null)
                        InReinventoryingHive( this, null);
                });
            Configure( State.DockingCart)
                .Ignore(Trigger.Resuming)
                .Permit(Trigger.Shutdown, State.Shutdown)
                .Permit(Trigger.DoneDocking, State.Idle)
                .OnEntry( () => {
                    if( InDockingCart != null)
                        InDockingCart( this, null);
                });
            Configure( State.UndockingCart)
                .Ignore(Trigger.Resuming)
                .Permit(Trigger.Shutdown, State.Shutdown)
                .Permit(Trigger.DoneUndocking, State.Idle)
                .OnEntry( () => {
                    if( InUndockingCart != null)
                        InUndockingCart( this, null);
                });
            Configure(State.Paused)
                .Permit(Trigger.Shutdown, State.Shutdown)
                .Permit(Trigger.Resuming, State.Resuming)
                .Permit(Trigger.Abort, State.Idle)
                .OnEntry( () => {
                    // Log.Info( "On enter PAUSED");
                    Pause();
                })
                .OnExit(() => {
                    // DKM 2011-06-09 WaitingForGo is a one shot bool, so when we get in here, just set it to false
                    WaitingForGo = false;
                });
            Configure(State.Resuming)
                .Permit( Trigger.Shutdown, State.Shutdown)
                .Permit( Trigger.Abort, State.Idle)
                .Permit( Trigger.Resumed, State.Idle)
                .OnEntry( () => {
                    // Log.Info( "One enter RESUMING");
                    Resume();
                });
            Configure(State.Shutdown)
                .Ignore(Trigger.Home)
                .Ignore(Trigger.HomeComplete)
                .Ignore(Trigger.ReinventoryStorage)
                /*
                .Ignore(Trigger.LoadPlate)
                .Ignore(Trigger.UnLoadPlate)
                .Ignore(Trigger.DoneLoading)
                .Ignore(Trigger.DoneUnLoading)
                 */
                .Ignore(Trigger.MovePlate)
                .Ignore(Trigger.DoneMoving)
                .Ignore(Trigger.DoneReinventorying)
                .Ignore(Trigger.Abort)
                .Ignore(Trigger.Retry)
                .Ignore(Trigger.Ignore)
                .Ignore(Trigger.Error)
                .Ignore(Trigger.DockCartRequested)
                .Ignore(Trigger.UndockCartRequested)
                .Ignore(Trigger.DoneDocking)
                .Ignore(Trigger.DoneUndocking)
                .Ignore(Trigger.Pause)
                .Ignore(Trigger.Resuming)
                .OnEntry(() => {
                    if( InShutdown != null)
                        InShutdown( this, null);
                    Debug.WriteLine("In Shutdown State"); 
                });
        }

        private void NoOp()
        {
            // do nothing here -- this NoOp function is here because PausedWhileMovingPlates is now a substate of MovingPlates.  When we
            // transition from MovingPlates -> PausedWhileMovingPlates -> MovingPlates, we don't want the InMovingPlates event
            // to get fired again!
        }

        public void FireShutdown()
        {
            // no need to lock, shutdown is always allowed, and overrides all
            Fire(Trigger.Shutdown);
        }

        public bool FireHome()
        {
            lock (this)
            {
                // wait for pending action to complete?
                while (!IsInState(State.NotHomed) && !IsInState(State.Idle) && !IsInState(State.Shutdown))
                    Thread.Sleep(STATE_CHECK_IDLE_TIME);
                // transition to home state
                Fire(Trigger.Home);
                // why did we return this before, if we never evaluate the return value?
                return IsInState(State.NotHomed);
            }
        }

        public void FireHomeComplete()
        {
            Fire(Trigger.HomeComplete);
        }
        public void FireHomeFailed()
        {
            Fire(Trigger.HomeFailed);
        }
        public void FireHomedAtInit()
        {
            Fire(Trigger.HomedAtInit);
        }

        // called when carts get inventoried
        public bool FireReinventoryBegin()
        {
            lock (this)
            {
                while(!IsInState( State.Idle) && !IsInState(State.Shutdown))
                    Thread.Sleep(STATE_CHECK_IDLE_TIME);
                Fire( Trigger.ReinventoryStorage);
                return IsInState(State.Reinventorying);
            }
        }

        // called when Hive gets reinventoried
        public bool FireReinventoryHive()
        {
            while(!IsInState( State.Idle) && !IsInState(State.Shutdown))
                Thread.Sleep(STATE_CHECK_IDLE_TIME);
            Fire( Trigger.ReinventoryHive);
            return IsInState(State.ReinventoryingHive);
        }

        //private void HandleMovePlateMessage( MovePlateMessage message)
        public bool FireMovePlate()
        {
            bool move_ack = false;
            while (!move_ack) { 
                lock (this)
                {
                    if (!IsInState(State.Idle) && !IsInState(State.Shutdown)) { 
                        Thread.Sleep(STATE_CHECK_IDLE_TIME);
                        continue;
                    }
                    Fire( Trigger.MovePlate);
                    move_ack = true;
                }
            }
            return IsInState(State.MovingPlates);
        }

        public void FireMovePlateComplete()
        {
            // currently this is called for Move complete Success or Fail, so don't go crazy in here
            Fire( Trigger.DoneMoving);
        }

        public bool FireDockCartRequested()
        {
            lock (this)
            {
                while(!IsInState( State.Idle) && !IsInState(State.Shutdown))
                    Thread.Sleep(STATE_CHECK_IDLE_TIME);
                Fire( Trigger.DockCartRequested);
                return IsInState(State.DockingCart);
            }
        }

        public void FireDockCartComplete()
        {
            Fire( Trigger.DoneDocking);
        }

        public bool FireUndockCartRequested()
        {
            lock (this)
            {
                while (!IsInState(State.Idle) && !IsInState(State.Shutdown))
                    Thread.Sleep(STATE_CHECK_IDLE_TIME);
                Fire( Trigger.UndockCartRequested);
                return IsInState(State.UndockingCart);
            }
        }

        public void FireUndockCartComplete()
        {
            Fire( Trigger.DoneUndocking);
        }

        public void FirePause()
        {
            _resumed = false;
            lock( this) {
                while (!_resumed && !(IsInState(State.Shutdown) || IsInState(State.Idle) || IsInState(State.MovingPlates)))
                    Thread.Sleep( STATE_CHECK_IDLE_TIME);
                if( !_resumed)
                    Fire( Trigger.Pause);
            }
        }

        /// <summary>
        /// This gets called by the Go Live button command handler
        /// </summary>
        public void FireResume()
        {
            // DKM 2011-10-13 wait for the other executing state to finish, if any
            lock( this) {
                while( !(IsInState( State.Idle) || IsInState( State.Shutdown) || IsInState( State.Paused) || IsInState( State.PausedWhileMovingPlates)))
                    Thread.Sleep( STATE_CHECK_IDLE_TIME);
            }

            _resumed = true;
            if( _first_resume){
                _plugin.StartServices();
                _first_resume = false;
            }
            Fire( Trigger.Resuming);
        }

        void BehaviorEngine_ReinventoryComplete(object sender, EventArgs e)
        {
            // DoneReinventorying sends us back to Idle
            Fire(Trigger.DoneReinventorying);
        }
        
        void BehaviorEngine_ReinventoryError(object sender, EventArgs e)
        {
            // DoneReinventorying sends us back to Idle
            Fire(Trigger.DoneReinventorying);
        }

        void hive_ReinventoryComplete(object sender, EventArgs e)
        {
            UserRequestedReinventory = false;
            Fire(Trigger.DoneReinventoryingHive);
        }
        
        void hive_ReinventoryError(object sender, EventArgs e)
        {
            UserRequestedReinventory = false;
            Fire(Trigger.DoneReinventoryingHive);
        }

        public bool Idle 
        {
            get 
            { 
                if( !Monitor.TryEnter(this)) // we're only locked if someone else is waiting for us to become idle
                    return false;

                bool is_idle = false;
                try
                {                
                    is_idle = IsInState( State.Idle); 
                }
                finally
                {
                    Monitor.Exit(this);
                }
                return is_idle;
            }
        }

        public bool Paused { get { return IsInState(State.Paused); } }
        public bool NotHomed { get { return IsInState(State.NotHomed); } }
        public bool Homing { get { return IsInState(State.Homing); } }
        public bool Reinventorying { get { return IsInState(State.Reinventorying); } }
        public bool PausedWhileMovingPlates { get { return IsInState(State.PausedWhileMovingPlates); } }

        public void FireAbort()
        {
            Fire( Trigger.Abort);
        }

        public void ResetAbortUpstackOrDownstack()
        {
            _abort_upstack_downstack_event.Reset();
        }

        public void AbortUpstackOrDownstack()
        {
            _abort_upstack_downstack_event.Set();
        }

        /// <summary>
        /// Called when the BehaviorEngine enters the Paused state.  Notifies subscribers.
        /// </summary>
        private void Pause()
        {
            if (InPaused != null)
                InPaused(this, null);
            Debug.WriteLine("BehaviorEngine Paused");
        }

        private void Resume()
        {
            if( Resuming != null)
                Resuming( this, null);
            Debug.WriteLine( "BehaviorEngine Resuming");
            Fire( Trigger.Resumed);
        }
    }
}
