using BioNex.Shared.TechnosoftLibrary;
using System;
using System.Threading;

namespace BioNex.Hig.StateMachines
{
    public class HomeStateMachine : HiGStateMachineCommon<HomeStateMachine.State>
    {
        protected bool _home_shield_only;
        protected bool _open_shield_after_homing;

        public enum State
        {
            Idle,
            HomeShield,
            HomeShieldError,
            HomeSpindle,
            HomeSpindleError,
            SetAngle,
            SetAngleError,
            GotoAngle,
            GotoAngleError,
            OpenShield,
            OpenShieldError,
            Done,
            FailedDone,
            CheckOpenShield,
        }

        public HomeStateMachine(IHigModel model, bool home_shield_only, bool open_shield_after_homing, bool show_abort_label)
            : base(model, State.Idle, show_abort_label)
        {
            InitializeStates();

            _home_shield_only = home_shield_only;
            _open_shield_after_homing = open_shield_after_homing;
        }

        protected virtual void InitializeStates()
        {
            SM.Configure(State.Idle)
                .Permit(Trigger.Execute, State.HomeShield);
            SM.Configure(State.HomeShield)
                .Permit(Trigger.Success, State.HomeSpindle)
                .Permit(Trigger.Fail, State.HomeShieldError)                
                .PermitReentry(Trigger.OvervoltageRecovery)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(HomeShield);
            SM.Configure(State.HomeSpindle)
                .Permit(Trigger.Success, State.SetAngle)
                .Permit(Trigger.Fail, State.HomeSpindleError)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(HomeSpindle);
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
            SM.Configure(State.HomeShieldError)
                .Permit(Trigger.Retry, State.HomeShield)
                .Permit(Trigger.Abort, State.Done)
                .OnEntry(f => HandleErrorWithRetryOnly(LastErrorMessage));
            SM.Configure(State.HomeSpindleError)
                .Permit(Trigger.Retry, State.HomeSpindle)
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
                .OnEntry(EndStateFunction);
        }

        protected void HomeShield()
        {
            // DKM 2012-04-17 if in NoShieldMode, just exit
            if( Model.NoShieldMode) {
                // DKM 2012-04-27 need to set homing_status = -1!
                if( !Model.ShieldAxis.SetIntVariable( "homing_status", 0)) {
                    Log.DebugFormat( "{0} Could not set homing_status = 0 on shield axis", Model.Name);
                    Fire(Trigger.Fail);
                    return;
                }

                Log.Debug( "HiG is in 'no shield' mode, leaving OpenShield()");
                Fire( Trigger.Success);
                return;
            }

            IAxis shield = null;
            try {
                // DKM 2011-01-09 I think ShieldAxis is null for Monsanto for some reason
                shield = Model.ShieldAxis;
                Log.DebugFormat( "{0} Entered HomeShield() {1}", Model.Name, shield == null ? "- shield is null" : "");
                Log.DebugFormat( "{0} HomeShield(): calling Home(true)", Model.Name);
                shield.Home(true);
                string appID = shield.ReadApplicationID();
                Log.DebugFormat("{0} Shield Door Axis has internal AppID of: {1}\n", Model.Name, appID);
                if (_home_shield_only)
                    Fire(Trigger.HomeShieldOnly);
                else
                    Fire(Trigger.Success);
            } catch(NullReferenceException) {
                LastErrorMessage = "Please initialize the device first";
                Log.Error( LastErrorMessage);
                Fire( Trigger.Fail);
            } catch (AxisException e) {
                // DKM 2012-01-30 if we get the imfamous overvoltage error (due to the homing flag being too short),
                //                attempt to recover from the situation by backing off of the sensor and trying again
                if( e.Message.ToLower().Contains( "over voltage") || e.Message.ToLower().Contains( "i2t")) {
                    shield.ResetFaults();
                    shield.Enable( true, true);
                    shield.SendTmlCommands( "SAP 0;");
                    shield.SendTmlCommands( "CPOS=45000; CPR; MODE PP;");
                    shield.SendTmlCommands( "POSOKLIM=1895; TONPOSOK=50; SRB UPGRADE, 0xFFFF, 0x0800; UPD; !MC; WAIT!;");
                    // DKM 2012-01-30 TODO figure out why the above WAIT! doesn't actually work.  That's why the Sleep is after this...
                    Thread.Sleep( 5000);
                    Fire( Trigger.OvervoltageRecovery);
                    return;
                }

                string details = e.Details;
                if( details == String.Empty)
                    details = GetHomingStatusString( shield);
                LastErrorMessage = String.Format( "{0} ({1})", e.Message, details);
                Log.Error( LastErrorMessage);
                Fire(Trigger.Fail);
            } catch( Exception e) {
                LastErrorMessage = e.Message;
                Log.Error( LastErrorMessage);
                Fire(Trigger.Fail);
            }
        }

        private string GetHomingStatusString( IAxis shield)
        {
            short homing_status;
            shield.GetIntVariable( "homing_status", out homing_status);
            return String.Format( "homing_status is {0}", homing_status);
        }

        protected void CheckOpenShield()
        {
            if (_open_shield_after_homing)
                Fire(Trigger.Success);
            else
                Fire(Trigger.LeaveShieldClosed);
        }

        protected void HomeSpindle()
        {
            Log.DebugFormat( "{0} Entered HomeSpindle()", Model.Name);
            IAxis spindle = null;
            try {
                spindle = Model.SpindleAxis;
                spindle.Home(true);
                Fire(Trigger.Success);
            } catch(NullReferenceException) {
                LastErrorMessage = "Please initialize the device first";
                Log.Error( LastErrorMessage);
                Fire( Trigger.Fail);
            } catch (AxisException e) {
                string details = e.Details;
                if( details == String.Empty)
                    details = GetHomingStatusString( spindle);
                LastErrorMessage = String.Format( "{0} ({1})", e.Message, details);
                Log.Error( LastErrorMessage);
                Fire(Trigger.Fail);
            } catch( Exception e) {
                LastErrorMessage = e.Message;
                Log.Error( LastErrorMessage);
                Fire(Trigger.Fail);
            }
        }
    }
}
