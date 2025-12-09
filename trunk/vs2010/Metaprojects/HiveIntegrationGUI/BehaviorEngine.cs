using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BioNex.HivePrototypePlugin;
using BioNex.Plugins.Dock;
using Stateless;

namespace BioNex.HiveIntegration
{
    public class BehaviorEngine : StateMachine<BehaviorEngine.State, BehaviorEngine.Trigger>
    {
        private Dictionary<string,DockMonitorPlugin> _docks { get; set; }
        private IntegrationGui _plugin { get; set; }
        /// <summary>
        /// flag to indicate that plates are being moved, but the process was interrupted by
        /// a request to load a plate from the external system.
        /// </summary>
        private bool _move_interrupted { get; set; }

        /// <summary>
        /// flag to allow FireResume to kick FirePause out of its Idle checking loop
        /// </summary>
        private bool _resumed;

        public bool WaitingForGo { get; private set; }

        /// <summary>
        /// Set this to abort a queued upstack / downstack operation (i.e. stacker is empty, and then VWorks shuts down)
        /// </summary>
        private AutoResetEvent _abort_upstack_downstack_event;

        // using events for each state transition, so that the GUI can update automatically
        public event EventHandler InIdle;
        public event EventHandler InNotHomed;
        public event EventHandler InHoming;
        public event EventHandler InLoadingPlate;
        public event EventHandler InUnloadingPlate;
        public event EventHandler InMovingPlates;
        public event EventHandler InReinventorying;
        public event EventHandler InDockingCart;
        public event EventHandler InUndockingCart;
        public event EventHandler InPaused;
        public event EventHandler InShutdown;

        public bool UserRequestedReinventory { get; private set; }
        public void RequestReinventory() { UserRequestedReinventory = true; }

        new public enum State
        {
            NotHomed,
            Homing,
            Idle,
            LoadingPlate,
            UnLoadingPlate,
            Reinventorying,
            MovingPlatesToCart,
            DockingCart,
            UndockingCart,
            Paused,
            Done,
            Shutdown,
        }

        public enum Trigger
        {
            Home,
            HomeComplete,
            HomeFailed,
            HomedAtInit,
            LoadPlate,
            UnLoadPlate,
            ReinventoryStorage,
            MovePlate,
            DoneLoading,
            DoneUnLoading,
            DoneMoving,
            DoneReinventorying,
            Abort,
            Retry,
            Ignore,
            Error,
            DockCartRequested,
            UndockCartRequested,
            DoneDocking,
            DoneUndocking,
            Pause,
            Resume,
            Shutdown,
        }

        public BehaviorEngine( IntegrationGui plugin)
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
            }

            // DKM 2011-06-09 Paused is now the first place we'll be when we either start homed or rehome
            WaitingForGo = true;
            Configure(State.NotHomed)
                .Ignore(Trigger.Resume)
                .Permit(Trigger.HomedAtInit, State.Paused)
                .Permit(Trigger.Home, State.Homing)
                .Permit(Trigger.Shutdown, State.Shutdown)
                .OnEntry(() => {
                    if (InNotHomed != null)
                        InNotHomed(this, null);
                });
            Configure(State.Homing)
                .Ignore(Trigger.Resume)
                .Permit(Trigger.HomeComplete, State.Idle)
                .Permit(Trigger.HomeFailed, State.NotHomed)
                .Permit(Trigger.Shutdown, State.Shutdown)
                .OnEntry( () => {
                    if( InHoming != null)
                        InHoming( this, null);
                });
            Configure(State.Idle)
                .Permit(Trigger.Home, State.Homing)
                .Permit(Trigger.LoadPlate, State.LoadingPlate)
                .Permit(Trigger.UnLoadPlate, State.UnLoadingPlate)
                .Permit(Trigger.MovePlate, State.MovingPlatesToCart)
                .Permit(Trigger.ReinventoryStorage, State.Reinventorying)
                .Permit(Trigger.DockCartRequested, State.DockingCart)
                .Permit(Trigger.UndockCartRequested, State.UndockingCart)
                .Permit(Trigger.Pause, State.Paused)
                .Permit(Trigger.Shutdown, State.Shutdown)
                // DKM 2011-06-15 added these two ignores because it's possible that a user doesn't do what he's supposed
                //                to do when docking / undocking, and we might not have actually transitioned yet from
                //                Idle -> Docking/Undocking, and when a timeout occurs, we'll fire DoneDocking / DoneUndocking
                .Ignore(Trigger.DoneDocking)
                .Ignore(Trigger.DoneUndocking)
                .Ignore(Trigger.Resume)
                .OnEntry(() => {
                    if( InIdle != null)
                        InIdle( this, null);
                    Debug.WriteLine("BehaviorEngine Idle");
                });
            Configure( State.LoadingPlate)
                .Ignore( Trigger.Resume)
                .Permit(Trigger.Shutdown, State.Shutdown)
                .Permit(Trigger.DoneLoading, State.Idle)
                .OnEntry( () => {
                    if( InLoadingPlate != null)
                        InLoadingPlate( this, null);
                });
            Configure( State.UnLoadingPlate)
                .Ignore(Trigger.Resume)
                .Permit(Trigger.Shutdown, State.Shutdown)
                .Permit(Trigger.DoneUnLoading, State.Idle)
                .OnEntry( () => {
                    if( InUnloadingPlate != null)
                        InUnloadingPlate( this, null);
                });
            Configure( State.Reinventorying)
                .Ignore(Trigger.Resume)
                .Permit(Trigger.Shutdown, State.Shutdown)
                .Permit(Trigger.DoneReinventorying, State.Idle)
                .OnEntry( () => {
                    if( InReinventorying != null)
                        InReinventorying( this, null);
                });
            Configure( State.MovingPlatesToCart)
                .Ignore(Trigger.Resume)
                .Permit(Trigger.Shutdown, State.Shutdown)
                .Permit(Trigger.DoneMoving, State.Idle)
                .OnEntry( () => {
                    if( InMovingPlates != null)
                        InMovingPlates( this, null);
                });
            Configure( State.DockingCart)
                .Ignore(Trigger.Resume)
                .Permit(Trigger.Shutdown, State.Shutdown)
                .Permit(Trigger.DoneDocking, State.Idle)
                .OnEntry( () => {
                    if( InDockingCart != null)
                        InDockingCart( this, null);
                });
            Configure( State.UndockingCart)
                .Ignore(Trigger.Resume)
                .Permit(Trigger.Shutdown, State.Shutdown)
                .Permit(Trigger.DoneUndocking, State.Idle)
                .OnEntry( () => {
                    if( InUndockingCart != null)
                        InUndockingCart( this, null);
                });
            Configure(State.Paused)
                .Permit(Trigger.Shutdown, State.Shutdown)
                .Permit(Trigger.Resume, State.Idle)
                .Permit(Trigger.Abort, State.Idle)
                .OnEntry(() => {
                    if (InPaused != null)
                        InPaused(this, null);
                    Debug.WriteLine("BehaviorEngine Paused");
                })
                .OnExit(() => {
                    // DKM 2011-06-09 WaitingForGo is a one shot bool, so when we get in here, just set it to false
                    WaitingForGo = false;
                });

            Configure(State.Shutdown)
                .Ignore(Trigger.Home)
                .Ignore(Trigger.HomeComplete)
                .Ignore(Trigger.LoadPlate)
                .Ignore(Trigger.UnLoadPlate)
                .Ignore(Trigger.ReinventoryStorage)
                .Ignore(Trigger.MovePlate)
                .Ignore(Trigger.DoneLoading)
                .Ignore(Trigger.DoneUnLoading)
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
                .Ignore(Trigger.Resume)
                .OnEntry(() => {
                    if( InShutdown != null)
                        InShutdown( this, null);
                    Debug.WriteLine("In Shutdown State"); 
                });
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
                //! \todo DKM 2012-05-16 here is where we can get into trouble via RPC.  Homing twice will freeze up here
                //!       because we'll be in the Paused state after homing the first time through.
                while (!IsInState(State.NotHomed) && !IsInState(State.Idle) && !IsInState(State.Shutdown))
                    Thread.Sleep(0);
                Fire(Trigger.Home);
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

        // carts & static storage start their inventory process the same way
        public bool FireReinventoryBegin()
        {
            lock (this)
            {
                while(!IsInState( State.Idle) && !IsInState(State.Shutdown))
                    Thread.Sleep(0);
                Fire( Trigger.ReinventoryStorage);
                return IsInState(State.Reinventorying);
            }
        }


        //private void HandleMovePlateMessage( MovePlateMessage message)
        public bool FireMovePlate()
        {
            lock (this)
            {
                while(!IsInState( State.Idle) && !IsInState(State.Shutdown))
                    Thread.Sleep(0);
                Fire( Trigger.MovePlate);
                return IsInState(State.MovingPlatesToCart);
            }
        }

        public void FireMovePlateComplete()
        {
            // currently this is called for Move complete Success or Fail, so don't go crazy in here
            Fire( Trigger.DoneMoving);
        }

        public bool FireLoadPlate()
        {
            bool space_available = false;
            try
            {
                while (!space_available && !IsInState(State.Shutdown))
                {
                    if (Monitor.TryEnter(this))
                    {
                        // check for abort event and if set, bail out of here
                        if( _abort_upstack_downstack_event.WaitOne(0))
                            return false;

                        space_available = _plugin.GetAvailableStaticLocations().Count() != 0;
                        if (!space_available)
                        {
                            Monitor.Exit(this);
                            Thread.Sleep( 50);
                            continue;
                        }

                        while (!IsInState(State.Idle) && !IsInState(State.Shutdown))
                            Thread.Sleep( 50);
                        
                        Fire(Trigger.LoadPlate);
                        return IsInState(State.LoadingPlate);
                    }
                }
                return false;
            }
            finally
            {
                Monitor.Exit(this);
            }
        }

        public void FireDoneLoadingPlate()
        {
            Fire(Trigger.DoneLoading);
        }

        public bool FireUnloadPlate()
        {
            bool storage_is_empty = true;
            try {
                while( storage_is_empty && !IsInState(State.Shutdown)) {
                    if( Monitor.TryEnter( this)) {
                        // check for abort event and if set, bail out of here
                        if( _abort_upstack_downstack_event.WaitOne(0))
                            return false;

                        storage_is_empty = _plugin.GetAvailableStaticLocations().Count() == _plugin.GetStaticStorageLocationNames().Count();
                        if( storage_is_empty) {
                            Monitor.Exit( this);
                            Thread.Sleep( 50);
                            continue;
                        }

                        while(!IsInState( State.Idle) && !IsInState(State.Shutdown))
                            Thread.Sleep( 50);
                        Fire( Trigger.UnLoadPlate);
                        return IsInState(State.UnLoadingPlate);
                    }
                }
                return false;
            } finally {
                Monitor.Exit( this);
            }
        }

        public void FireDoneUnloadingPlate()
        {
            Fire(Trigger.DoneUnLoading);
        }

        public bool FireDockCartRequested()
        {
            lock (this)
            {
                while(!IsInState( State.Idle) && !IsInState(State.Shutdown))
                    Thread.Sleep(0);
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
                    Thread.Sleep(0);
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
                while (!IsInState(State.Idle) && !_resumed && !IsInState(State.Shutdown))
                    Thread.Sleep( 0);
                if( !_resumed)
                    Fire( Trigger.Pause);
            }
        }

        public void FireResume()
        {
            _resumed = true;
            Fire( Trigger.Resume);
        }

        void BehaviorEngine_ReinventoryComplete(object sender, EventArgs e)
        {
            /*
            DockMonitorPlugin dock = sender as DockMonitorPlugin;
            Debug.Assert( dock != null);
            // message is not used, UI just responds directly to reinventory complete event
            //_messenger.Send<-ReinventoryCompleteMessage>( new ReinventoryCompleteMessage( "system name", dock.Name, "cart name goes here", dock.GetInventory( "robot name")));
             */
            // DoneReinventorying sends us back to Idle
            Fire(Trigger.DoneReinventorying);
        }
        
        void BehaviorEngine_ReinventoryError(object sender, EventArgs e)
        {
            /*
            DockMonitorPlugin dock = sender as DockMonitorPlugin;
            Debug.Assert(dock != null);
             */
            // DoneReinventorying sends us back to Idle
            Fire(Trigger.DoneReinventorying);
        }

        void hive_ReinventoryComplete(object sender, EventArgs e)
        {
            UserRequestedReinventory = false;
            Fire(Trigger.DoneReinventorying);
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
    }
}
