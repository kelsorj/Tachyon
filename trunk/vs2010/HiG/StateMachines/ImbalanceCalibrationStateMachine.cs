using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BioNex.Shared.Utils;

namespace BioNex.Hig.StateMachines
{
    public class ImbalanceCalibrationStateMachine : HiGStateMachineCommon<ImbalanceCalibrationStateMachine.State>
    {
        private const int MAX_ALLOWABLE_IMBALANCE_AD = 11000;
        private const int NUMBER_OF_IMBALANCE_SAMPLES = 20;

        private double _accel;
        private double _decel;
        private double _rpm;
        private DateTime _startSpinTime;
        private DateTime last_time_test_imb;
        private List<int> _imbalance_readings;

        // _hig and _model point to the same object reference
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
            SetupImbalanceCalibration,
            CalculateImbalance,
            Accelerate,
            AccelerateError,
            SetupImbalanceCalibrationError,
            CalculateImbalanceError,
            AccelerateDoorError,
            AbortAcceleration,
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

        public ImbalanceCalibrationStateMachine(IHigModel model, bool show_abort_label)
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
            _imbalance_readings = new List<int>();
        }

        protected virtual void InitializeStates()
        {
            SM.Configure(State.Idle)
                .Permit(Trigger.Execute, State.CloseShield);
            SM.Configure(State.CloseShield)
                .Permit(Trigger.Success, State.SetupImbalanceCalibration)
                .Permit(Trigger.Fail, State.CloseShieldError)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(() =>  {
                    Thread.Sleep(0);
                    CloseShield();
                });
            SM.Configure( State.SetupImbalanceCalibration)
                .Permit( Trigger.Success, State.Accelerate)
                .Permit( Trigger.Fail, State.SetupImbalanceCalibrationError)
                .OnEntry( SetupImbalanceCalibration);
            SM.Configure(State.Accelerate)
                .Permit(Trigger.Success, State.Decelerate)
                .Permit(Trigger.AbortAndProceed, State.Decelerate)
                .Permit(Trigger.Fail, State.AccelerateError)
                .Permit(Trigger.FailDoor, State.AccelerateDoorError)
                .Permit(Trigger.Abort, State.Decelerate)
                .Permit(Trigger.Invalid, State.Done)
                .OnEntry(Accelerate);
            SM.Configure(State.Decelerate)
                .Permit( Trigger.Continue, State.Accelerate)
                .Permit( Trigger.Success, State.CalculateImbalance)
                .Permit( Trigger.Fail, State.DecelerateError)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry( Decelerate);
            SM.Configure( State.CalculateImbalance)
                .Permit( Trigger.Success, State.SetAngle)
                .Permit( Trigger.Fail, State.CalculateImbalanceError)
                .OnEntry( CalculateImbalance);
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
            SM.Configure( State.SetupImbalanceCalibrationError)
                .Permit( Trigger.Retry, State.SetupImbalanceCalibration)
                .Permit( Trigger.Abort, State.Done);
            SM.Configure(State.CloseShieldError)
                .Permit(Trigger.Retry, State.CloseShield)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.AccelerateError)
                .Permit(Trigger.Retry, State.Accelerate)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.AccelerateDoorError)
                .Permit(Trigger.Retry, State.CloseShield)
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

        public void ExecuteCalibration()
        {
            _accel = 100;
            _decel = 100;
            _rpm = 120;

            Start();
        }

        public void AbortImbalanceCalibration()
        {
            Log.InfoFormat( "{0} Aborting imbalance calibration", Model.Name);
            // DKM 2011-08-16 use AbortAndProceed instead of Abort, because until now Abort has been used with the assumption that
            //                we want to bail out and not process any more states.  But in our case, we want to still open the door
            //                afterward.
            if( !_abort_requested)
                _abort_requested = true;
        }

        /// <summary>
        /// Sets the highest imbalance threshold reasonable for protecting the hardware before starting the spin.  If the imbalance
        /// value is above this, then we assume that the user is using an invalid imbalance and we default to the highest value.
        /// </summary>
        protected void SetupImbalanceCalibration()
        {
            _imbalance_readings.Clear();
            // the value could change, but the idea here is to set something that is likely to be above the maximum-allowable
            // imbalance for all units shipped
            if( !_model.SpindleAxis.SetIntVariable( "imb_ampl_max", MAX_ALLOWABLE_IMBALANCE_AD)) {
                LastErrorMessage = "Imbalance calibration plate is too heavy.  Please replace with a plate that is <100g.";
                Log.Error( LastErrorMessage);
                Fire(Trigger.Fail);
            } else {
                Fire( Trigger.Success);
            }
        }

        protected void CalculateImbalance()
        {
            try {
                // calculate the average and stddev, then set the max-allowable value in EEPROM
                double average = _imbalance_readings.Average();
                double stddev = _imbalance_readings.StandardDeviation();
                _model.ImbalanceThreshold = (int)average + (int)(2 * stddev);
                Fire( Trigger.Success); 
            } catch( Exception ex) {
                LastErrorMessage = String.Format( "Could not save imbalance threshold: {0}", ex.Message);
                Log.Error( LastErrorMessage);
                Fire( Trigger.Fail);
            }
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
                    _startSpinTime = DateTime.Now;
                    _imbalance_readings.Add( MAX_ALLOWABLE_IMBALANCE_AD);
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
                        // DKM 2012-02-29 record the peak-to-peak imbalance values
                        int min;
                        bool min_ok = _model.SpindleAxis.GetLongVariable( "min_ad5", out min);
                        int max;
                        bool max_ok = _model.SpindleAxis.GetLongVariable( "max_ad5", out max);
                        if( min_ok && max_ok) {
                            _imbalance_readings.Add( (int)(max - min));
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
                            LastErrorMessage = "Spindle did not accelerate before timeout. Please try again.";
                            Log.Error( LastErrorMessage);
                            Log.DebugFormat("{0} spindle controller only had {1:0.0} seconds to get to speed in spin_up function", Model.Name, theor_spin_up_time);
                            Fire(Trigger.Fail);
                        }
                        else
                        {
                            _startSpinTime = DateTime.Now;
                            Fire(Trigger.Success);
                        }
                        break;
                    case 0: // Timeout, but this case is handled in CallFunctionAndWaitForDone
                        LastErrorMessage = "Timed out. Please try again";
                        Log.Error( LastErrorMessage);
                        Log.Info(String.Format("{0} spindle controller timed out in spin_up function", Model.Name));
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
                        Fire(Trigger.Fail);
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
                        Fire(Trigger.Fail);
                        break;
                    default:
                        LastErrorMessage = "Unknown error during spin_up call";
                        Log.Error( LastErrorMessage);
                        Log.Info(String.Format("{0} spindle controller reports func_done=={1} in spin_up function", Model.Name, func_done));
                        Fire(Trigger.Fail);
                        break;
                }

            } catch(NullReferenceException) {
                LastErrorMessage = "Please initialize the device first";
                Log.Error( LastErrorMessage);
                Fire( Trigger.Fail);
            } catch (Exception e) {
                LastErrorMessage = e.Message;
                Log.Error( LastErrorMessage);
                Fire(Trigger.Fail);
            }
        }

        protected void Decelerate()
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
                    // DKM 2012-02-29 make sure we get enough data points for imbalance calibration
                    if( _imbalance_readings.Count < NUMBER_OF_IMBALANCE_SAMPLES) {
                        Fire( Trigger.Continue);
                    } else {
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

                // DKM 2012-02-29 make sure we get enough data points for imbalance calibration
                if( _imbalance_readings.Count < NUMBER_OF_IMBALANCE_SAMPLES) {
                    Thread.Sleep( 5000); // let the tub settle
                    Fire( Trigger.Continue);
                } else {
                    Fire(Trigger.Success);
                }
            } catch(NullReferenceException) {
                LastErrorMessage = "Please initialize the device first";
                Log.Error( LastErrorMessage);
                Fire( Trigger.Fail);
            } catch (Exception e) {
                LastErrorMessage = e.Message;
                Log.Error( LastErrorMessage);
                Fire(Trigger.Fail);
            }
        }
    }
}
