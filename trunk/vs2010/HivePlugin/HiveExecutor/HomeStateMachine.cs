using System;
using System.Threading;
using BioNex.Shared.TechnosoftLibrary;

namespace BioNex.Hive.Executor
{
    internal class HomeStateMachine : HiveStateMachine< HomeStateMachine.State, HomeStateMachine.Trigger>
    {
        // ----------------------------------------------------------------------
        // inner classes.
        // ----------------------------------------------------------------------
        private class HomeStateMachineException : ApplicationException
        {
            public HomeStateMachineException( string message)
                : base( message)
            {
            }
        }

        // ----------------------------------------------------------------------
        // enumerations.
        // ----------------------------------------------------------------------
        internal enum State
        {
            Start,
            HomeTheta, HomeThetaError,
            TuckTheta, TuckThetaError,
            HomeGXZ, HomeGXZError,
            WaitForHomeGXYComplete, WaitForHomeGXYCompleteError,
            Park, ParkError,
            End, Abort,
        }
        // ----------------------------------------------------------------------
        internal enum Trigger
        {
            Success,
            Failure,
            Retry,
            Ignore,
            Abort,
        }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        internal HomeStateMachine( HiveExecutor executor, ManualResetEvent ended_aborted_event)
            : base( executor, ended_aborted_event, typeof( HomeStateMachine), State.Start, State.End, State.Abort, Trigger.Success, Trigger.Failure, Trigger.Retry, Trigger.Ignore, Trigger.Abort, executor.HandleError)
        {
            InitializeStates();
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        private void InitializeStates()
        {
            ConfigureState( State.Start, NullStateFunction, State.HomeTheta);
            ConfigureState( State.HomeTheta, HomeTheta, State.TuckTheta, State.HomeThetaError);
            ConfigureState( State.TuckTheta, TuckTheta, State.HomeGXZ, State.TuckThetaError);
            ConfigureState( State.HomeGXZ, HomeGXZ, State.WaitForHomeGXYComplete, State.HomeGXZError);
            SM.Configure( State.WaitForHomeGXYComplete)
                .Permit( Trigger.Abort, State.End)
                .Permit( Trigger.Success, State.Park)
                .Permit( Trigger.Failure, State.WaitForHomeGXYCompleteError)
                .OnEntry( WaitForHomeGXYComplete);
            SM.Configure( State.WaitForHomeGXYCompleteError)
                .Permit( Trigger.Abort, State.End)
                .Permit( Trigger.Retry, State.HomeTheta)
                .OnEntry( f => HandleErrorWithRetryOnly( LastError));
            ConfigureState( State.Park, Park, State.End, State.ParkError);
            ConfigureState( State.End, EndStateFunction);
            ConfigureState( State.Abort, AbortedStateFunction);
        }
        // ----------------------------------------------------------------------
        private void CheckSafeToHome()
        {
            // throw new Exception( "FYC -- reorg hive");
            /* was:
            if( !Executor.DataRequestInterface.Value.SafeToMove( Executor))
                throw new HomeStateMachineException( "It is not safe to home.  Please clear any obstructions and try again.");
            */
        }
        // ----------------------------------------------------------------------
        private void HomeTheta()
        {
            try {
                CheckSafeToHome();
                IAxis theta = Executor.Hardware.TAxis;
                theta.Home( true);
                Thread.Sleep( 50);
                Fire(Trigger.Success);
            } catch( Exception ex) {
                LastError = ex.Message;
                Fire(Trigger.Failure);
            }
        }
        // ----------------------------------------------------------------------
        private void TuckTheta()
        {
            try {
                CheckSafeToHome();
                Executor.Hardware.TuckTheta();
                Fire(Trigger.Success);
            } catch( Exception ex) {
                LastError = ex.Message;
                Fire(Trigger.Failure);
            }
        }
        // ----------------------------------------------------------------------
        private void HomeGXZ()
        {
            try {
                CheckSafeToHome();
                Executor.Hardware.GAxis.Home( false);
                Executor.Hardware.XAxis.Home( false);
                Executor.Hardware.ZAxis.Home( false);
                Thread.Sleep( 1000);
                Fire(Trigger.Success);
            } catch( Exception ex) {
                LastError = ex.Message;
                Fire(Trigger.Failure);
            }
        }
        // ----------------------------------------------------------------------
        private void WaitForHomeGXYComplete()
        {
            try {
                CheckSafeToHome();
                IAxis g = Executor.Hardware.GAxis;
                IAxis x = Executor.Hardware.XAxis;
                IAxis z = Executor.Hardware.ZAxis;

                while( g.IsHoming() || x.IsHoming() || z.IsHoming())
                    Thread.Sleep( 1000);
                // check for an error at this point -- could have hit ESTOP
                if( g.GetError() != "")
                    throw new Exception( string.Format( "G axis failed to home: {0}", g.GetError()));
                if( x.GetError() != "")
                    throw new Exception( string.Format( "X axis failed to home: {0}", x.GetError()));
                if( z.GetError() != "")
                    throw new Exception( string.Format( "Z axis failed to home: {0}", z.GetError()));
                Fire(Trigger.Success);
            } catch( Exception ex) {
                LastError = ex.Message;
                Fire(Trigger.Failure);
            }
        }
        // ----------------------------------------------------------------------
        private void Park()
        {
            try {
                Executor.Hardware.Park();
                Fire(Trigger.Success);
            } catch( Exception ex) {
                LastError = String.Format( "Could not park robot: {0}", ex.Message);
                Fire( Trigger.Failure);
            }
        }
    }
}
