using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BioNex.Shared.Utils;
using BioNex.Shared.IError;
using BioNex.Shared.DeviceInterfaces;
using log4net;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using GalaSoft.MvvmLight.Messaging;

namespace BioNex.Plugins.HiGTestGui
{
    public class CycleStateMachine : StateMachineWrapper2<CycleStateMachine.State, CycleStateMachine.Trigger>
    {
        private int _num_cycles_requested;
        private int _cycle_count;
        private RobotInterface _robot;
        private AccessibleDeviceInterface _source_device;
        private string _labware_name;

        // used by the derived cycle state machine, which is only used to cycle test standalone HiGs
        protected ILog _log = LogManager.GetLogger(typeof(CycleStateMachine));
        public Hig.HigPlugin Hig { get; private set; }
        protected double _g;
        protected double _accel;
        protected double _decel;
        protected double _time_s;
        protected double _delay_s;
        private bool _home_after_each_spin;
        private AutoResetEvent _abort_signal_for_sleep_condition;

        public event EventHandler CycleComplete;
        public virtual void OnCycleComplete()
        {
            if( CycleComplete != null)
                CycleComplete( this, null);
        }

        public enum State
        {
            Idle,
            CheckCyclesRemaining,
            Done,
            PrepareSourceLocation, PrepareSourceLocationError,
            PickPlate, PickPlateError,
            PrepareDestinationLocation, PrepareDestinationLocationError,
            PlacePlate, PlacePlateError,
            ParkRobot, ParkRobotError,
            Spin, SpinError,
            Home, HomeError,
            PrepareDest2, PrepareDest2Error,
            PickPlate2, PickPlate2Error,
            PrepareSource2, PrepareSource2Error,
            PlacePlate2, PlacePlate2Error
        }

        public enum Trigger
        {
            Success,
            Failure,
            Retry,
            Ignore,
            Abort,
            NoMoreCycles
        }

        public CycleStateMachine( string labware_name, int num_cycles_requested, double g, double accel, double decel, double time_s, double delay_s,
                                  RobotInterface robot, Hig.HigPlugin hig, AccessibleDeviceInterface source_device, IError error_interface, bool home_after_each_spin)
            : base( typeof(CycleStateMachine), State.Idle, State.Done, State.Done,
                    Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, error_interface, false)
        {
            Messenger.Default.Register<AbortCommand>( this, HandleAbortCommand);
            _num_cycles_requested = num_cycles_requested;
            _robot = robot;
            Hig = hig;
            _source_device = source_device;
            _labware_name = labware_name;

            _g = g;
            _accel = accel;
            _decel = decel;
            _time_s = time_s;
            _delay_s = delay_s;
            _home_after_each_spin = home_after_each_spin;
            _abort_signal_for_sleep_condition = new AutoResetEvent( false);

            ConfigureStates();
        }

        protected virtual void HandleAbortCommand( AbortCommand cmd)
        {
            _abort_signal_for_sleep_condition.Set();
            Abort();
        }

        protected virtual void ConfigureStates()
        {
            const string source_device_location_name = "BB PM 4";
            const string hig_location_name = "Bucket 1";

            ConfigureState( State.Idle, NullStateFunction, State.CheckCyclesRemaining);
            SM.Configure( State.CheckCyclesRemaining)
                .Permit( Trigger.NoMoreCycles, State.Done)
                .Permit( Trigger.Success, State.PrepareSourceLocation)
                .OnEntry( CheckCyclesRemaining);
            ConfigureState( State.PrepareSourceLocation, PrepareSourceLocation, State.PickPlate, State.PrepareSourceLocationError);
            ConfigureState( State.PickPlate, () => PickFrom( _source_device.Name, source_device_location_name), State.PrepareDestinationLocation, State.PickPlateError);
            ConfigureState( State.PrepareDestinationLocation, PrepareDestinationLocation, State.PlacePlate, State.PrepareDestinationLocationError);
            ConfigureState( State.PlacePlate, () => PlaceAt( Hig.Name, hig_location_name), State.ParkRobot, State.PlacePlateError);
            ConfigureState( State.ParkRobot, Park, State.Spin, State.ParkRobotError);
            ConfigureState( State.Spin, () => Spin( _g, _accel, _decel, _time_s), State.Home, State.SpinError);
            ConfigureState( State.Home, Home, State.PrepareDest2, State.HomeError);
            ConfigureState( State.PrepareDest2, PrepareDestinationLocation, State.PickPlate2, State.PrepareDest2Error);
            ConfigureState( State.PickPlate2, () => PickFrom( Hig.Name, hig_location_name), State.PrepareSource2, State.PickPlate2Error);
            ConfigureState( State.PrepareSource2, PrepareSourceLocation, State.PlacePlate2, State.PrepareSource2Error);
            ConfigureState( State.PlacePlate2, () => PlaceAt( _source_device.Name, source_device_location_name), State.CheckCyclesRemaining, State.PlacePlate2Error);
            ConfigureState( State.Done, EndStateFunction);
        }

        protected void CheckCyclesRemaining()
        {
            if( ++_cycle_count > _num_cycles_requested) {
                _log.Info( "No more cycles left");
                Fire( Trigger.NoMoreCycles);
            } else {
                if (_cycle_count > 1)
                {
                    int sleep_time_ms = (int)(_delay_s * 1000); // sleep time to simulate robot pick-n-place for standalone testing
                    _log.Info(String.Format("Sleeping for {0:0} seconds before next cycle", sleep_time_ms / 1000.0));
                    // DKM 2012-04-27 refs #578 if user clicks abort, we should bail out of this sleep and exit
                    if( _abort_signal_for_sleep_condition.WaitOne( sleep_time_ms)) {
                        // DKM I cheated here -- I originally wanted to use NoMoreCycles, but Abort was getting
                        //     fired as well via the Abort handler.  I decided to use the same trigger here.
                        Fire( Trigger.Abort);
                        return;
                    }
                }
                _log.Info( String.Format( "Starting cycle {0}/{1}", _cycle_count, _num_cycles_requested));
                Fire( Trigger.Success);
            }
        }

        private void PrepareSourceLocation()
        {
            try {
                Fire( Trigger.Success);
            } catch( Exception ex) {
                LastError = ex.Message;
                Fire( Trigger.Failure);
            }
        }

        private void PickFrom( string device_name, string location_name)
        {
            try {
                _robot.Pick( device_name, location_name, _labware_name, new BioNex.Shared.Utils.MutableString(""));
                Fire( Trigger.Success);
            } catch( Exception ex) {
                LastError = ex.Message;
                Fire( Trigger.Failure);
            }
        }

        private void PrepareDestinationLocation()
        {
            try {
                Hig.ExecuteCommand( "OpenShield", new Dictionary<string,object> { { "bucket_number", 1 } } );
                Fire( Trigger.Success);
            } catch( Exception ex) {
                LastError = ex.Message;
                Fire( Trigger.Failure);
            }
        }

        private void PlaceAt( string device_name, string location_name)
        {
            try {
                _robot.Place( device_name, location_name, _labware_name, new BioNex.Shared.Utils.MutableString(""));
                Fire( Trigger.Success);
            } catch( Exception ex) {
                LastError = ex.Message;
                Fire( Trigger.Failure);
            }
        }

        private void Park()
        {
            try {
                _robot.Park();
                Fire( Trigger.Success);
            } catch( Exception ex) {
                LastError = ex.Message;
                Fire( Trigger.Failure);
            }
        }

        protected virtual void Spin( double rpm, double accel, double decel, double time_s)
        {
            try {
                Hig.ExecuteCommand( "Spin", new Dictionary<string,object> { { "accel", accel }, { "decel", decel }, { "rpm", rpm }, { "time_s", time_s } });
                Thread.Sleep( 500);
                Fire( Trigger.Success);
            } catch( Exception ex) {
                LastError = ex.Message;
                Fire( Trigger.Failure);
            }
        }

        protected void Home()
        {
            if( !_home_after_each_spin) {
                Fire( Trigger.Success);
                return;
            }

            try {
                Hig.Home( false, true);
                Fire( Trigger.Success);
            } catch( Exception ex) {
                LastError = ex.Message;
                Fire( Trigger.Failure);
            }
        }
    }

    public class CycleMultipleHigsOnlyStateMachine : CycleStateMachine
    {
        private AutoResetEvent _spin_event;

        public CycleMultipleHigsOnlyStateMachine( string labware_name, int num_cycles_requested, double rpm, double accel, double decel, double time_s,
                                                  double delay_s, Hig.HigPlugin hig, IError error_interface, bool home_after_each_spin)
            : base( labware_name, num_cycles_requested, rpm, accel, decel, time_s, delay_s, null, hig, null, error_interface, home_after_each_spin)
        {
            _spin_event = new AutoResetEvent( false);
        }

        protected override void ConfigureStates()
        {
            ConfigureState( State.Idle, NullStateFunction, State.CheckCyclesRemaining);
            SM.Configure( State.CheckCyclesRemaining)
                .Permit( Trigger.NoMoreCycles, State.Done)
                .Permit( Trigger.Abort, State.Done)
                .Permit( Trigger.Success, State.Spin)
                .OnEntry( CheckCyclesRemaining);
            ConfigureState( State.Spin, () => Spin( _g, _accel, _decel, _time_s), State.Home, State.SpinError);
            ConfigureState( State.Home, Home, State.CheckCyclesRemaining, State.HomeError);
            ConfigureState( State.Done, EndStateFunction);
        }

        /// <summary>
        /// Spins ALL of the higs available.  Kicks off threads for each and then waits for all 
        /// to complete or get an error.  If there's an error on *any* of the higs, then *all* respin.
        /// </summary>
        /// <param name="rpm"></param>
        /// <param name="accel"></param>
        /// <param name="decel"></param>
        /// <param name="time_s"></param>
        protected override void Spin( double rpm, double accel, double decel, double time_s)
        {
            try {
                // DKM 2011-11-16 after looking at this again, couldn't we just block?  This SM is now kicked off in its own thread.
                Action spin = new Action( () => SpinHiGThread( Hig));
                spin.BeginInvoke( SpinComplete, _spin_event);
                _spin_event.WaitOne();
                //spin.Invoke();
                Fire( Trigger.Success);
            } catch( Exception ex) {
                LastError = ex.Message;
                Fire( Trigger.Failure);
            }
        }

        private void SpinHiGThread( Hig.HigPlugin hig)
        {
            _log.Info( String.Format( "Spinning {0}", hig.Name));
            hig.ExecuteCommand( "Spin", new Dictionary<string,object> { { "accel", _accel }, { "decel", _decel }, { "g", _g }, { "time_s", _time_s } });
        }

        private void SpinComplete( IAsyncResult iar)
        {
            AsyncResult ar = (AsyncResult)iar;
            Action caller = (Action)ar.AsyncDelegate;
            AutoResetEvent spin_event = (AutoResetEvent)ar.AsyncState;

            try {
                caller.EndInvoke( iar);
            } catch( Exception ex) {
                _log.Error( "Spin error: " + ex.Message);
            } finally {
                spin_event.Set();
                OnCycleComplete();
            }
        }
    }

    public class CycleHomeStateMachine : CycleStateMachine
    {
        public CycleHomeStateMachine( int num_cycles_requested, double delay_s, Hig.HigPlugin hig, IError error_interface, bool home_after_each_spin)
            : base( "", num_cycles_requested, 0, 0, 0, 0, delay_s, null, hig, null, error_interface, false)
        {
        }

        protected override void ConfigureStates()
        {
            ConfigureState(State.Idle, NullStateFunction, State.CheckCyclesRemaining);
            SM.Configure(State.CheckCyclesRemaining)
                .Permit(Trigger.NoMoreCycles, State.Done)
                .Permit(Trigger.Success, State.Spin)
                .OnEntry(CheckCyclesRemaining);
            ConfigureState(State.Spin, Home, State.CheckCyclesRemaining, State.SpinError);
            ConfigureState(State.Done, EndStateFunction);
        }

        new private void Home()
        {
            try
            {
                Hig.Home();
                DateTime start = DateTime.Now;
                Thread.Sleep(1000);
                while (!Hig.IsHomed)
                    Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                Fire(Trigger.Failure);
                return;
            }
            OnCycleComplete();
            Fire(Trigger.Success);
        }
    }
}
