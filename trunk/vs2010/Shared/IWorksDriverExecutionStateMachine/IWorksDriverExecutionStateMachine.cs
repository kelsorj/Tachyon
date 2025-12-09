using System;
using System.Collections.Generic;
using System.Threading;
using BioNex.Shared.IError;
using BioNex.Shared.Utils;
using log4net;

namespace BioNex.Shared.IWorksDriverExecutionStateMachine
{
    public class IWorksDriverExecutionStateMachine : StateMachineWrapper2<IWorksDriverExecutionStateMachine.State,IWorksDriverExecutionStateMachine.Trigger>
    {
        /// <summary>
        /// The function that gets called when this state machine is executed
        /// </summary>
        /// <remarks>
        /// I wanted to use IWorksDriverLib.ReturnCode, but instead used an int because of the following error:
        /// Error	57	Member 'BioNex.Shared.IWorksDriverExecutionStateMachine.IWorksDriverExecutor.IWorksDriverExecutor()' from assembly 'c:\Code\MonsantoPhase2\vs2010\Shared\IWorksDriverExecutionStateMachine\bin\Debug\IWorksDriverExecutionStateMachine.dll' cannot be used across assembly boundaries because it contains a type which has a generic type parameter that is an embedded interop type.	C:\Code\MonsantoPhase2\vs2010\PlateLocPlugin\PlateLocPlugin\PlateLocPlugin.cs	68	44	PlateLocPlugin
        /// </remarks>
        protected Func<int> _func;
        protected IWorksDriverLib.IWorksDriver _iworksplugin;
        protected IError.IError _error_interface;
        private static readonly ILog _log = LogManager.GetLogger( typeof( IWorksDriverExecutionStateMachine));
        protected IWorksDriverExecutionSMConfiguration _config { get; set; }
        protected string _device_name;
                
        public enum State
        {
            Idle,
            Execute,
            ExecuteError,
            Abort,
            Retry,
            Ignore,
            FakeSuccess, // used so we can pretend that errors on V11 devices didn't occur, but gives device enough time to present plate to robot
            Done
        }

        public enum Trigger
        {
            Success,
            FakeSuccess,
            Failure,
            Abort,
            Retry,
            Ignore
        }

        public IWorksDriverExecutionStateMachine( string device_name, IWorksDriverLib.IWorksDriver iworksplugin, Func<int> func, IError.IError error_interface)
            : base(null, error_interface, State.Idle, State.Done, State.Done, Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, false)
        {
            IWorksDriverExecutionSMConfigurationParser parser = new IWorksDriverExecutionSMConfigurationParser();
            _device_name = device_name;
            _config = parser._configuration;
            _func = func;
            _iworksplugin = iworksplugin;
            _error_interface = error_interface;
            SM.Configure( State.Idle)
                .Permit( Trigger.Success, State.Execute)
                .OnEntry( NullStateFunction);
            SM.Configure( State.Execute)
                .Permit( Trigger.Success, State.Done)
                // REED CHANGE HERE -- change State.FakeSuccess BELOW to State.ExecuteError when you want to re-enable error handling!
                .Permit( Trigger.FakeSuccess, State.FakeSuccess)
                .Permit( Trigger.Failure, State.ExecuteError)
                .OnEntry( Execute);
            SM.Configure( State.FakeSuccess)
                .Permit( Trigger.Success, State.Done)
                .OnEntry( () => {
                    Thread.Sleep( 2000);
                    Fire( Trigger.Success);
                });
            SM.Configure( State.ExecuteError)
                .Permit( Trigger.Abort, State.Abort)
                .Permit( Trigger.Retry, State.Retry)
                .Permit( Trigger.Ignore, State.Ignore)
                .OnEntry( HandleErrorWithAbortRetryIgnore);
            SM.Configure( State.Abort)
                .Permit( Trigger.Success, State.Done)
                .OnEntry( AbortTask);
            SM.Configure( State.Retry)
                .Permit( Trigger.Success, State.Done)
                .Permit( Trigger.Failure, State.ExecuteError)
                .OnEntry( Retry);
            SM.Configure( State.Ignore)
                .Permit( Trigger.Success, State.Done)
                .Permit( Trigger.Failure, State.ExecuteError)
                .OnEntry( Ignore);
            SM.Configure( State.Done)
                .OnEntry( EndStateFunction);
        }

        protected virtual void Execute()
        {
            if( _func() != (int)IWorksDriverLib.ReturnCode.RETURN_SUCCESS) {
                LastError = _device_name + ": " + _iworksplugin.GetErrorInfo().Replace( "\r", " ").Replace( "\n", " ");
                if( _config.IgnoreErrors)
                    Fire( Trigger.FakeSuccess);
                else
                    Fire( Trigger.Failure);
            } else {
                Fire( Trigger.Success);
            }
        }

        private void AbortTask()
        {
            _iworksplugin.Abort();
            throw new V11TaskAbortedException();
        }

        private void Retry()
        {
            if( _iworksplugin.Retry() != IWorksDriverLib.ReturnCode.RETURN_SUCCESS) {
                LastError = _device_name + ": " + _iworksplugin.GetErrorInfo();
                Fire( Trigger.Failure);
            } else {
                Fire( Trigger.Success);
            }
        }

        private void Ignore()
        {
            if( _iworksplugin.Ignore() != IWorksDriverLib.ReturnCode.RETURN_SUCCESS) {
                LastError = _device_name + ": " + _iworksplugin.GetErrorInfo();
                Fire( Trigger.Failure);
            } else {
                Fire( Trigger.Success);
            }
        }

        private void HandleErrorWithAbortRetryIgnore()
        {
            const string abort_label = "Abort";
            const string retry_label = "Retry";
            const string ignore_label = "Ignore";
            List< string> error_strings = new List< string>{ abort_label, retry_label, ignore_label};
            ErrorData error_data = new ErrorData( LastError, error_strings);
            SMStopwatch.Stop();
            _error_interface.AddError( error_data);

            // allow error handler to bail out and report errors later via PendingErrors (used for device loading)
            if( !_error_interface.WaitForUserToHandleError)
                return;

            int event_index = WaitHandle.WaitAny( error_data.EventArray);
            SMStopwatch.Start();
            if( error_data.TriggeredEvent == abort_label) {
                Fire( AbortTrigger);
            } else if( error_data.TriggeredEvent == retry_label) {
                Fire( RetryTrigger);
            } else if( error_data.TriggeredEvent == ignore_label) {
                Fire( IgnoreTrigger);
            } else{
                _log.Error( UNEXPECTED_EVENT_STRING);
                Fire( AbortTrigger);
            }
        }
    }

    public class IWorksStackerExecutionStateMachine : IWorksDriverExecutionStateMachine
    {
        // private IWorksDriverLib.IStackerDriver _stacker;

        public IWorksStackerExecutionStateMachine( string device_name, IWorksDriverLib.IStackerDriver stacker, Func<int> func, IError.IError error_interface)
            : base( device_name, (IWorksDriverLib.IWorksDriver)stacker, func, error_interface)
        {
        }

        protected override void Execute()
        {
            if( _func() != (int)IWorksDriverLib.ReturnCode.RETURN_SUCCESS) {
                LastError = _device_name + ": " + _iworksplugin.GetErrorInfo().Replace( "\r", " ").Replace( "\n", " ");
                Fire( Trigger.Failure);
            } else {
                Fire( Trigger.Success);
            }
        }

    }
}
