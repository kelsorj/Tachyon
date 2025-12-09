using System;
using BioNex.Shared.TechnosoftLibrary;

namespace BioNex.Hig.StateMachines
{
    public class OpenShieldStateMachine : HiGStateMachineCommon<OpenShieldStateMachine.State>
    {
        private int _bucket_index;

        public enum State
        {
            Idle,
            TestAngle,
            TestAngleError,
            CloseShield,
            CloseShieldError,
            SetAngle,
            SetAngleError,
            GotoAngle,
            GotoAngleError,
            OpenShield,
            OpenShieldError,
            Done,
            FailedDone
        }

        public OpenShieldStateMachine(IHigModel model, bool show_abort_label)
            : base(model, State.Idle, show_abort_label)
        {
            InitializeStates();
        }

        protected virtual void InitializeStates()
        {
            SM.Configure(State.Idle)
                .Permit(Trigger.Execute, State.TestAngle);
            SM.Configure(State.TestAngle)
                .Permit(Trigger.Fail, State.TestAngleError)
                .Permit(Trigger.Success, State.CloseShield)
                .Permit(Trigger.Continue, State.OpenShield)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(TestAngle);
            SM.Configure(State.CloseShield)
                .Permit(Trigger.Success, State.SetAngle)
                .Permit(Trigger.Fail, State.CloseShieldError)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(CloseShield);
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
                .Permit( Trigger.Success, State.Done)
                .Permit( Trigger.Fail, State.OpenShieldError)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry( OpenShield);
            SM.Configure(State.TestAngleError)
                .Permit(Trigger.Retry, State.TestAngle)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.CloseShieldError)
                .Permit(Trigger.Retry, State.CloseShield)
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

        public void ExecuteOpenShield( int bucket_index)
        {
            _bucket_index = bucket_index;
            Start();
        }

        double GetCurrentAngle()
        {
            // -- we tested wrap around, and found that the TML controller handles it correctly at a low level.
            // mark did more testing of wraparound on 29-aug-2011 with horrible results. now we handle all of this on the spindle controller
            // -- the bucket ends up 
            var spindle = Model.SpindleAxis;
            int position = spindle.GetPositionCounts();
            return HigUtils.ConvertIUToDegrees(position, spindle.Settings.EncoderLines);
        }

        internal void TestAngle()
        {
            // skip Closing the door and moving to position if we're already at position
            
            try{
                // DKM 2011-10-17 need to use EEPROM value for bucket 2 offset instead of assuming 180 degrees
                //var bucket_angle = _bucket_index * 180.0; // this assumes bucket #2 is 180 degrees from bucket1. bad assumption, but probably close enough given epsilon
                var bucket_angle = HigUtils.ConvertIUToDegrees(_bucket_index == 0 ? 0 : Model.Bucket2Offset, Model.SpindleAxis.Settings.EncoderLines);
                var angle = GetCurrentAngle();
                var delta = Math.Abs(bucket_angle - angle);
                var epsilon = 2 * Model.SpindleAxis.Settings.MoveDoneWindow;

                if( delta <= epsilon)
                    Fire(Trigger.Continue); // continue skips us to OpenShield
                else 
                    Fire(Trigger.Success);  // success moves us to the next state in order to be consistent with everything else
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

        internal override void SetAngle()
        {
            try {
                var spindle = Model.SpindleAxis;
                //spindle.SetIntVariable("angle_to_goto", (short)(_bucket_index * 180));
                spindle.SetIntVariable("bucket_to_goto", (short)(_bucket_index + 1)); // bucket numbers are assumed to be 1 or 2
                Fire(Trigger.Success);
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
