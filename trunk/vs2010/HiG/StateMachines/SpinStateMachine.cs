using System;
using System.Threading;

namespace BioNex.Hig.StateMachines
{
    /// <summary>
    /// Supports spinning for Synapsis and the integration API, but also imbalance calibration
    /// </summary>
    public class SpinStateMachine : HiGStateMachineCommon<SpinStateMachine.State>
    {
        private double _accel;
        private double _decel;
        private double _rpm;
        private double _timeSeconds;
        private DateTime _startSpinTime;

        private double _accel_time_s;
        private double _cruise_time_s;
        private double _decel_time_s;

        // temperature logging
        private double _temp_at_start;
        private double _temp_at_end;
        private double? _temp_at_middle;

        private DateTime last_time_test_imb;

        private readonly IHigModel _model;
        /// <summary>
        /// Needed this extra variable for aborting a spin during the InterruptableWait period.  Maybe it's a better
        /// idea to have a property in IAxis to check to see if an abort was requested?
        /// </summary>
        private bool _abort_requested;

        public enum State
        {
            Idle,
            CloseShield,
            CloseShieldError,
            Accelerate,
            AccelerateError,
            AccelerateDoorError,
            AccelerateErrorWaitForSpinDown,
            AbortAcceleration,
            InterruptableWait,
            InterruptableWaitError,
            Decelerate,
            DecelerateError,
            SetAngle,
            SetAngleError,
            GotoAngle,
            GotoAngleError,
            OpenShield,
            OpenShieldError,
            Done,
            FailedDone
        }

        public SpinStateMachine(IHigModel model, bool show_abort_label)
            : base(model, State.Idle, show_abort_label)
        {
            InitializeStates();
            // DKM 2011-07-26 I could have added Abort support this way with the default Messenger AbortCommand that comes
            //                from Synapsis, or I could have added a public method for the caller.  This seemed the cleanest
            //                at the time...
            // DKM 2011-07-26 I changed my mind -- adding this meant that I'd have to include the MVVM Light DLL, and I didn't
            //                want to have to add this dependency just to support Abort.
            //Messenger.Default.Register<AbortCommand>( this, new Action<AbortCommand>( (cmd) => { Fire( Trigger.Abort); } ));
            _model = model;
            last_time_test_imb = DateTime.Now;
            SkipLeavingStateLogging.Add( State.InterruptableWait);
        }

        protected virtual void InitializeStates()
        {
            SM.Configure(State.Idle)
                .Permit(Trigger.Execute, State.CloseShield);
            SM.Configure(State.CloseShield)
                .Permit(Trigger.Success, State.Accelerate)
                // DKM 2011-11-08 modding to do door open/close cycle testing
                // DKM 2011-12-09 need to at least accelerate to generate a "door not closed error", so moved to next state instead
                //.Permit(Trigger.CycleDoor, State.OpenShield)
                .Permit(Trigger.Fail, State.CloseShieldError)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(() =>  {
                    Thread.Sleep(0);
                    CloseShield();
                    /*
                    if (Model.CycleDoorOnly)
                    {
                        Fire(Trigger.CycleDoor);
                    }
                    else
                    {
                        Fire(Trigger.Success);
                    }
                     */
                });
            SM.Configure(State.Accelerate)
                .Permit(Trigger.Success, State.InterruptableWait)
                .Permit(Trigger.CycleDoor, State.Decelerate)
                .Permit(Trigger.AbortAndProceed, State.Decelerate)
                .Permit(Trigger.Fail, State.AccelerateError)
                .Permit(Trigger.FailDoor, State.AccelerateDoorError)
                .Permit(Trigger.FailWaitForSpinDown, State.AccelerateErrorWaitForSpinDown)
                .Permit(Trigger.Abort, State.Decelerate)
                .OnEntry(Accelerate);
            SM.Configure(State.InterruptableWait)
                .Permit(Trigger.Success, State.Decelerate)
                .PermitReentry(Trigger.ContinueWait)
                .Permit(Trigger.AbortAndProceed, State.Decelerate)
                .Permit(Trigger.Fail, State.InterruptableWaitError)
                .OnEntry(InterruptableWait);
            SM.Configure(State.Decelerate)
                .Permit( Trigger.Success, State.SetAngle)
                .Permit( Trigger.Fail, State.DecelerateError)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry( () => Decelerate());
            SM.Configure(State.SetAngle)
                .Permit(Trigger.Success, State.GotoAngle)
                .Permit(Trigger.Fail, State.SetAngleError)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(SetAngle);
            SM.Configure(State.GotoAngle)
                .Permit(Trigger.Success, State.OpenShield)
                .Permit(Trigger.Fail, State.GotoAngleError)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(GotoAngle);
            SM.Configure(State.OpenShield)
                .Permit(Trigger.Success, State.Done)
                .Permit(Trigger.Fail, State.OpenShieldError)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(OpenShield);
            SM.Configure(State.CloseShieldError)
                .Permit(Trigger.Retry, State.CloseShield)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.AccelerateError)
                .Permit(Trigger.Retry, State.Accelerate)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.AccelerateErrorWaitForSpinDown)
                .Permit(Trigger.Retry, State.Accelerate)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => {
                    // DKM 2012-04-19 refs #572: should wait until we decelerate before firing error from Accelerate
                    //                otherwise, the system could send a new command while the rotor is still spinning
                    try {
                        Decelerate( false);
                    } catch( Exception) {
                        // can't do much about a decelerate issue here...
                    }
                    HandleErrorWithRetryOnly(LastErrorMessage);
                });
            SM.Configure(State.AccelerateDoorError)
                .Permit(Trigger.Retry, State.CloseShield)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.InterruptableWaitError)
                .Permit(Trigger.Retry, State.Accelerate)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.DecelerateError)
                .Permit(Trigger.Retry, State.Decelerate)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.SetAngleError)
                .Permit(Trigger.Retry, State.SetAngle)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.GotoAngleError)
                .Permit(Trigger.Retry, State.GotoAngle)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.OpenShieldError)
                .Permit(Trigger.Retry, State.OpenShield)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.Done)
                .OnEntry( EndStateFunction);
        }

        public void ExecuteSpin( double accel, double decel, double rpm, double timeSeconds,
                                 ref double accel_time_s, ref double cruise_time_s, ref double decel_time_s)
        {
            _accel = accel;
            _decel = decel;
            _rpm = rpm;
            _timeSeconds = timeSeconds;

            Start();

            accel_time_s = _accel_time_s;
            cruise_time_s = _cruise_time_s;
            decel_time_s = _decel_time_s;
        }

        /// <summary>
        /// Not using this anymore, because Abort will prevent us from progressing through the rest of the desired states
        /// </summary>
        [Obsolete]
        new private void Abort()
        {
            Log.InfoFormat("{0} Aborting", Model.Name);
            Fire(Trigger.Abort);
        }

        public void AbortSpin()
        {
            Log.InfoFormat( "{0} Aborting spin cycle", Model.Name);
            // DKM 2011-08-16 use AbortAndProceed instead of Abort, because until now Abort has been used with the assumption that
            //                we want to bail out and not process any more states.  But in our case, we want to still open the door
            //                afterward.
            if( !_abort_requested)
                _abort_requested = true;
        }

        protected void Accelerate()
        {
            // allow user to bail out before slow accel to imbalance test starts
            if (_abort_requested)
            {
                Fire(Trigger.AbortAndProceed);
                return;
            }

            try {
                var spindle = Model.SpindleAxis;
                var degPerSecFromRpm = _rpm * 360 / 60.0;
                var degPerSec2FromAccel = spindle.Settings.Acceleration * _accel / 100.0;

                // DKM 2011-09-29 unfortunately, I had to add special simulation code here because it's not currently
                //                possible to support simulation of functions that run on the servo drives
                // DKM 2011-10-20 additionally, we are calling a different function in simulation.  Here we have to use
                //                the older MoveSpeed() method, whereas the TSM controllers now have a function we
                //                call, called "spin_up".
                if( _model.Simulating) {
                    DateTime sim_start = DateTime.Now;
                    spindle.MoveSpeed(degPerSecFromRpm, degPerSec2FromAccel, true);
                    _accel_time_s = (DateTime.Now - sim_start).TotalSeconds;
                    _startSpinTime = DateTime.Now;
                    Fire(Trigger.Success);
                    return;
                }

                const double spindle_current_limit = 29.0; // use very high current limit (30.9 is max)

                if (degPerSecFromRpm > spindle.Settings.Velocity)
                {
                    degPerSecFromRpm = spindle.Settings.Velocity;
                    Log.DebugFormat("{0} Accelerate() clipping velocity to {1:0.000} deg/s", Model.Name, degPerSecFromRpm);
                }

                if (degPerSec2FromAccel > spindle.Settings.Acceleration)
                {
                    degPerSec2FromAccel = spindle.Settings.Acceleration;
                    Log.DebugFormat("{0} Accelerate() clipping acceleration to {1:0.000} deg/s^2", Model.Name, degPerSec2FromAccel);
                }

                double cspd_iu = degPerSecFromRpm * spindle.GetCountsPerEngineeringUnit() * spindle.Settings.SlowLoopServoTimeS;
                double cacc_iu = degPerSec2FromAccel * spindle.GetCountsPerEngineeringUnit() * spindle.Settings.SlowLoopServoTimeS * spindle.Settings.SlowLoopServoTimeS;

                spindle.SetCurrentAmpsCmdLimit(spindle_current_limit); // set SATS and SATP with new current/torque limit
                spindle.SetFixedVariable("CSPD", cspd_iu); // set desired speed
                spindle.SetFixedVariable("CACC", cacc_iu); // set desired decel

#if false
                if (!spindle.MoveSpeed(degPerSecFromRpm, degPerSec2FromAccel, true))
                {
                    Fire(Trigger.AbortAndProceed);
                    return;
                }
#endif

                // tickle watchdog
                spindle.RefreshServoTimeout();

                double theor_spin_up_time = degPerSecFromRpm / degPerSec2FromAccel;
                theor_spin_up_time += 10.0; // add 10 seconds for imbalance detection
                theor_spin_up_time += 5.0; // add 5 seconds for current/power limiting
                DateTime start_accel = DateTime.Now;

                //int func_done = spindle.CallFunctionAndWaitForDone("spin_up", TimeSpan.FromSeconds(theor_spin_up_time), return_func_done: true);
                DateTime datetime_start_func = DateTime.Now;
                spindle.SetLongVariable("func_done", 0);
                spindle.CallFunction("spin_up");
                Int32 func_done = 0;
                while (DateTime.Now - datetime_start_func <= TimeSpan.FromSeconds(theor_spin_up_time))
                {
                    spindle.GetLongVariable("func_done", out func_done);
                    if (0 != func_done)
                    {
                        break;
                    }

                    // tickle watchdog
                    spindle.RefreshServoTimeout();

                    Thread.Sleep(50); // 50ms seems like a good amount of time to sleep for this kind of polling
                }
                switch (func_done)
                {
                    case 1: // Success! What we expected...
                        // DKM 2011-12-09 allows us to cycle shield faster by not spinning up to speed, but still requiring
                        //                us to pass all of the checks for acceleration
                        if (Model.CycleDoorOnly) {
                            Fire(Trigger.CycleDoor);
                            return;
                        }

                        // This success means we did the initial legs of acceleration and passed the imbalance test. It does not mean we are up to speed, yet
                        // now wait until we actually get up to speed
                        bool trajectory_complete = spindle.ReadTrajectoryCompleteFlag();

                        // standby while we are accelerating. Move on when speed is above below 99% of desired speed or time expires.
                        while (((spindle.GetActualSpeedDegPerSec() < (0.99 * degPerSecFromRpm)) || !trajectory_complete) && ((DateTime.Now - start_accel).TotalSeconds < theor_spin_up_time))
                        {
                            if (_abort_requested)
                            {
                                Fire(Trigger.AbortAndProceed);
                                return;
                            }

                            // check for faults?
                            // check to make sure we are accelerating?
                            // tickle watchdog
                            spindle.RefreshServoTimeout();
                            Thread.Sleep(100); // sleep while we are accelerating
                            trajectory_complete = spindle.ReadTrajectoryCompleteFlag();
                        }

                        Log.DebugFormat("{0} Spindle Accel Phase to motion complete took {1:0.000} out of {2:0.000} seconds. ASPD={3:0.0} rpm",
                            Model.Name, (DateTime.Now - start_accel).TotalSeconds, theor_spin_up_time, spindle.GetActualSpeedDegPerSec() * 60.0 / 360.0);

                        if (!trajectory_complete)
                        {
                            string mer_errors = spindle.GetError();
                            // DKM 2012-04-05 need to implement this for IAxis, but localize change for now for testing on HiG
                            short cer = 0;
                            // for now, ignore error from GetIntVariable
                            spindle.GetIntVariable( "CER", out cer);
                            LastErrorMessage = "Spindle did not accelerate before timeout. Please try again.";
                            Log.ErrorFormat( "{0} Errors reported: {1}. CER = {2}", LastErrorMessage, mer_errors, cer.ToString());
                            Log.DebugFormat("{0} spindle controller only had {1:0.0} seconds to get to speed in spin_up function", Model.Name, theor_spin_up_time);
                            Fire(Trigger.Fail);
                        }
                        else
                        {
                            _accel_time_s = (DateTime.Now - datetime_start_func).TotalSeconds;
                            Log.DebugFormat( "{0} Acceleration phase took {0:0.00}s", Model.Name, _accel_time_s);
                            _startSpinTime = DateTime.Now;
                            _temp_at_start = Model.SpindleTemperature;
                            Fire(Trigger.Success);
                        }
                        break;
                    case 0: // Timeout, but this case is handled in CallFunctionAndWaitForDone
                        LastErrorMessage = "Timed out. Please try again";
                        Log.Error( LastErrorMessage);
                        Log.Info(String.Format("{0} spindle controller timed out in spin_up function", Model.Name));
                        // DKM 2012-04-04 need to stop spin at this point.  imbalance routine should be called with CALLS instead of CALL,
                        //                so we can cancel that call by calling spin_down.
                        // DKM 2012-04-04 I changed spin_up in v1.6 to use CALLS for imbalance_test, so that we can abort the function here if necessary
                        spindle.AbortCancellableCall();
                        spindle.SetFixedVariable("CSPD", 0.0); // set desired speed to 0 rpm
                        spindle.CallFunctionAndWaitForDone("func_spin_down", TimeSpan.FromSeconds(1.0)); // wait up to 1 second for func_done == 1
                        Fire(Trigger.Fail);
                        break;
                    case -1: // not homed
                        LastErrorMessage = "Please rehome HiG and try again";
                        Log.Error( LastErrorMessage);
                        Log.Info(String.Format("{0} spindle controller reports not home in spin_up function", Model.Name));
                        Fire(Trigger.Fail);
                        break;
                    case -2: // Safety Interlocked/Disabled
                        LastErrorMessage = "Please reset safety interlock for HiG and try again";
                        Log.Error( LastErrorMessage);
                        Log.Info(String.Format("{0} spindle controller reports Safety Interlock Tripped (Amp Disabled) in spin_up function", Model.Name));
                        Fire(Trigger.Fail);
                        break;
                    case -3: // Was already spinning when this was called
                        LastErrorMessage = "Please allow HiG to come to a stop before commanding a new cycle. You may try again.";
                        Log.Error( LastErrorMessage);
                        Log.Info(String.Format("{0} spindle controller reports spindle was already spinning in spin_up function", Model.Name));
                        Fire(Trigger.FailWaitForSpinDown);
                        break;
                    case -4: // Door not closed
                        LastErrorMessage = "Shield door is apparently not closed. Check for obstructions before trying again.";
                        Log.Error( LastErrorMessage);
                        Log.Info(String.Format("{0} spindle controller reports shield door is not closed in spin_up function", Model.Name));
                        Fire(Trigger.FailDoor);
                        break;
                    case -5: // Imbalanced
                        LastErrorMessage = "Imbalanced load detected -- Please rebalance load in HiG and try again";
                        Log.Error( LastErrorMessage);
                        Log.Info(String.Format("{0} spindle controller reports Imbalanced Load in spin_up function", Model.Name));
                        Fire(Trigger.FailWaitForSpinDown);
                        break;
                    default:
                        LastErrorMessage = "Unknown error during spin_up call";
                        Log.Error( LastErrorMessage);
                        Log.Info(String.Format("{0} spindle controller reports func_done=={1} in spin_up function", Model.Name, func_done));
                        Fire(Trigger.FailWaitForSpinDown);
                        break;
                }

            } catch(NullReferenceException) {
                LastErrorMessage = "Please initialize the device first";
                Fire( Trigger.Fail);
            } catch (Exception e) {
                LastErrorMessage = e.Message;
                Fire(Trigger.Fail);
            }
        }

        protected void InterruptableWait()
        {
            const double maxSleepTime = 100.0;
            const double interval_test_imbalance = 0.2; // test imbalance every 200ms
            var elapsed = (DateTime.Now - _startSpinTime).TotalSeconds;
            _model.SpinSecRemaining = _timeSeconds - elapsed;
            if (_model.SpinSecRemaining > 0 && !_abort_requested)
            {
                // DKM 2011-09-29 special case for simulation
                if( _model.Simulating) {
                    Thread.Sleep((int)Math.Min( _model.SpinSecRemaining * 1000, maxSleepTime));
                    Fire(Trigger.ContinueWait);
                    return;
                }

                try
                {
                    // tickle watchdog
                    var spindle = Model.SpindleAxis;
                    spindle.RefreshServoTimeout();
                    if ((DateTime.Now - last_time_test_imb).TotalSeconds > interval_test_imbalance)
                    {
                        last_time_test_imb = DateTime.Now;

                        // DKM 2012-04-24 log imbalance sensor value -- ignore errors
                        short imbalance_ad_value;
                        if( !Model.SpindleAxis.GetIntVariable("AD5", out imbalance_ad_value))
                            Log.Warn( "Could not read imbalance sensor on AD5");
                        else
                            Log.DebugFormat( "Imbalance sensor reads: {0}", 65536 + imbalance_ad_value); // signed, so need to convert to uint in technosoft land

                        double ASPD_rpm = spindle.GetActualSpeedDegPerSec() / 360.0 * 60.0;

                        // we compare actual speed to desired speed here by subtracting desired from actual and then comparing the error to desired times a factor like 5%
                        const double max_speed_err_factor = 0.05;
                        if (Math.Abs(ASPD_rpm - _rpm) > max_speed_err_factor*_rpm)
                        {
                            Log.Error(String.Format("{0} spindle spinning at {1:0} rpm, but supposed to be spinning at {2:0} rpm with only {3:0} rpm of error.", 
                                                    Model.Name, ASPD_rpm, _rpm, max_speed_err_factor*_rpm));

                            var degPerSec2FromDecel = spindle.Settings.Acceleration * _decel / 100.0;
                            double CACC_IU = degPerSec2FromDecel * spindle.GetCountsPerEngineeringUnit() * spindle.Settings.SlowLoopServoTimeS * spindle.Settings.SlowLoopServoTimeS;
                            spindle.SetFixedVariable("CSPD", 0.0); // set desired speed to 0 rpm
                            spindle.SetFixedVariable("CACC", CACC_IU); // set desired decel
                            spindle.CallFunction("func_spin_down"); // fire off the command to spin down, but don't stick around for it to complete

                            LastErrorMessage = "Spindle not at desired Speed. Stopping...";
                            Log.Error( LastErrorMessage);
                            short cer = 0;
                            spindle.GetIntVariable( "CER", out cer);
                            Log.Debug( String.Format( "{0} Errors reported: {1}, CER = {2}", LastErrorMessage, spindle.GetError(), cer.ToString()));
                            Fire(Trigger.Fail);
                            return;

                        }
                    }
                }
                catch (NullReferenceException)
                {
                    Fire(Trigger.Fail);
                    return;
                }
                catch (Exception e)
                {
                    LastErrorMessage = e.Message;
                    Log.Error( LastErrorMessage);
                    Fire(Trigger.Fail);
                    return;
                }

                // log spindle temp at middle of spin
                if ( _model.SpinSecRemaining < _timeSeconds / 2 && _temp_at_middle == null)
                    _temp_at_middle = Model.SpindleTemperature;

                Thread.Sleep((int)Math.Min( _model.SpinSecRemaining * 1000, maxSleepTime));
                Fire(Trigger.ContinueWait);
                return;
            }

            if (_abort_requested)
            {
                Fire(Trigger.AbortAndProceed);
                return;
            }

            _temp_at_end = Model.SpindleTemperature;
            Log.InfoFormat("{0} spindle temperature after {1}s: start={2:0.00}, middle={3:0.00}, end={4:0.00}", Model.Name, _timeSeconds, _temp_at_start, _temp_at_middle, _temp_at_end);
            _cruise_time_s = (DateTime.Now - _startSpinTime).TotalSeconds;
            Log.DebugFormat("{0} Cruise velocity phase took {1:0.00}s", Model.Name, _cruise_time_s);
            Fire(Trigger.Success);
        }

        // DKM 2012-04-19 refs #572: conditionally fire triggers since this function now needs to be called from
        //                the AccelerateError error handler.
        protected void Decelerate( bool fire_triggers=true)
        {
            try {
                var spindle = Model.SpindleAxis;
                var degPerSec2FromDecel = spindle.Settings.Acceleration * _decel / 100.0;

                // DKM 2011-09-29 special case for simulation
                // DKM 2011-10-20 additionally, we are calling a different function in simulation.  Here we have to use
                //                the older MoveSpeed() method, whereas the TSM controllers now have a function we
                //                call, called "spin_down".
                if( _model.Simulating) {
                    DateTime sim_start = DateTime.Now;
                    spindle.MoveSpeed(0.0, degPerSec2FromDecel, true);
                    _decel_time_s = (DateTime.Now - sim_start).TotalSeconds;
                    if( fire_triggers) {
                        Fire( Trigger.Success);
                    }
                    return;
                }

                double CACC_IU = degPerSec2FromDecel * spindle.GetCountsPerEngineeringUnit() * spindle.Settings.SlowLoopServoTimeS * spindle.Settings.SlowLoopServoTimeS;
                double time_to_decel_secs = _rpm * 360.0 / 60.0 / degPerSec2FromDecel + 5.0; // give us an extra 5 seconds to decelerate since we limit decel near 0

                spindle.SetFixedVariable("CSPD", 0.0); // set desired speed to 0 rpm
                spindle.SetFixedVariable("CACC", CACC_IU); // set desired decel
                spindle.CallFunctionAndWaitForDone("func_spin_down", TimeSpan.FromSeconds(1.0)); // wait up to 1 second for func_done == 1
                //spindle.MoveSpeed(0.0, degPerSec2FromDecel, true); // use this function to wait for motion complete...
                DateTime start_decel = DateTime.Now;
                bool move_complete = spindle.ReadMotionCompleteFlag();

                // standby while we are decelerating. Move on when speed is below 5 rpm or time expires.
                while (((Math.Abs(spindle.GetActualSpeedDegPerSec()*60.0/360.0) > 5.0) || !move_complete) && ((DateTime.Now - start_decel).TotalSeconds < time_to_decel_secs))
                {
                    // check for faults?
                    // check to make sure we are decelerating?

                    // tickle watchdog
                    spindle.RefreshServoTimeout();

                    Thread.Sleep(100); // sleep while we are decelerating
                    move_complete = spindle.ReadMotionCompleteFlag();
                }

                Log.DebugFormat("{0} Spindle Decel Phase to motion complete took {1:0.000} seconds. ASPD={2:0.0} rpm", Model.Name, (DateTime.Now - start_decel).TotalSeconds, spindle.GetActualSpeedDegPerSec()*60.0/360.0);

                if (!move_complete)
                {
                    throw (new Exception(String.Format("{0} Spindle did not stop within {1:0.0} seconds", Model.Name, time_to_decel_secs)));
                }

                Thread.Sleep(250); // wait an extra 250 ms for spindle to really come to a complete stop
                _decel_time_s = (DateTime.Now - start_decel).TotalSeconds;
                Log.DebugFormat( "{0} Deceleration phase took {1:0.00}s", Model.Name, _decel_time_s);
                if( fire_triggers) {
                    Fire(Trigger.Success);
                }
            } catch(NullReferenceException) {
                LastErrorMessage = "Please initialize the device first";
                Log.Error( LastErrorMessage);
                if( fire_triggers) {
                    Fire( Trigger.Fail);
                }
            } catch (Exception e) {
                LastErrorMessage = e.Message;
                Log.Error( LastErrorMessage);
                if( fire_triggers) {
                    Fire(Trigger.Fail);
                }
            }
        }

    }
}
