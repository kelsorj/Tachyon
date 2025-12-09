using System;
using System.Collections.Generic;
using System.Threading;
using BioNex.BumblebeePlugin.Hardware;
using BioNex.Shared.IError;
using Stateless;

namespace BioNex.BumblebeePlugin.Dispatcher
{
    public class WashTipsStateMachine : ICDStateMachine< WashTipsStateMachine.State, WashTipsStateMachine.Trigger>
    {
        // ----------------------------------------------------------------------
        // enumerations.
        // ----------------------------------------------------------------------
        public enum State
        {
            Start,
            OnStartCritical,
            RetractPlenumAndBath, RetractPlenumAndBathError, RetractPlenumAndBathDebug,
            MoveShuttleToWashPosition, MoveShuttleToWashPositionError, MoveShuttleToWashPositionDebug,
            MoveWasherToWashPosition, MoveWasherToWashPositionError, MoveWasherToWashPositionDebug,
            TurnOnBathWater, TurnOnBathWaterError, TurnOnBathWaterDebug,
            TurnOnPlenumWater, TurnOnPlenumWaterError, TurnOnPlenumWaterDebug,
            TurnOnOverflowExhaust, TurnOnOverflowExhaustError, TurnOnOverflowExhaustDebug,
            TurnOffOverflowExhaust, TurnOffOverflowExhaustError, TurnOffOverflowExhaustDebug,
            TurnOffPlenumWater, TurnOffPlenumWaterError, TurnOffPlenumWaterDebug,
            TurnOffBathWater, TurnOffBathWaterError, TurnOffBathWaterDebug,
            MoveBathToDryPosition, MoveBathToDryPositionError, MoveBathToDryPositionDebug,
            TurnVacuumOn, TurnVacuumOnError, TurnVacuumOnDebug,
            TurnAirOn, TurnAirOnError, TurnAirOnDebug,
            MovePlenumToDryPosition, MovePlenumToDryPositionError, MovePlenumToDryPositionDebug,
            MovePlenumToRetractedPosition, MovePlenumToRetractedPositionError, MovePlenumToRetractedPositionDebug,
            TurnOffAir, TurnOffAirError, TurnOffAirDebug,
            TurnOffVacuum, TurnOffVacuumError, TurnOffVacuumDebug,
            MoveBathToRetractedPosition, MoveBathtoRetractedPositionError, MoveBathtoRetractedPositionDebug,
            OnFinishCritical,
            End, Abort,
        }

        public enum Trigger
        {
            Success,
            Failure,
            Retry,
            Ignore,
            Abort,
            Debug,
        }

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        protected TipShuttle TipShuttle { get; private set; }
        private IDictionary< State, int> DelayLookup { get; set; }
        protected bool Debug { get; private set; }
        private IDictionary< State, State> FromDebugStateToNextStateLookup { get; set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public WashTipsStateMachine(ICDParameterBundle parameter_bundle, ICDEventBundle event_bundle, DispatcherJob job, TipShuttle tip_shuttle, bool debug)
            : base(parameter_bundle, event_bundle, job)
        {
            TipShuttle = tip_shuttle;
            DelayLookup = new Dictionary< State, int>();
            Debug = debug;
            FromDebugStateToNextStateLookup = new Dictionary< State, State>();

            DelayLookup[ State.RetractPlenumAndBath] = 0;
            DelayLookup[ State.MoveShuttleToWashPosition] = ParameterBundle.Configuration.BeforeMoveShuttleToWashPositionDelayMs;
            DelayLookup[ State.MoveWasherToWashPosition] = ParameterBundle.Configuration.BeforeMoveWasherToWashPositionDelayMs;
            DelayLookup[ State.TurnOnBathWater] = ParameterBundle.Configuration.BeforeTurnOnBathWaterDelayMs;
            DelayLookup[ State.TurnOnPlenumWater] = ParameterBundle.Configuration.BeforeTurnOnPlenumWaterDelayMs;
            DelayLookup[ State.TurnOnOverflowExhaust] = ParameterBundle.Configuration.BeforeTurnOnOverflowExhaustDelayMs;
            DelayLookup[ State.TurnOffOverflowExhaust] = ParameterBundle.Configuration.BeforeTurnOffOverflowExhaustDelayMs;
            DelayLookup[ State.TurnOffPlenumWater] = ParameterBundle.Configuration.BeforeTurnOffPlenumWaterDelayMs;
            DelayLookup[ State.TurnOffBathWater] = ParameterBundle.Configuration.BeforeTurnOffBathWaterDelayMs;
            DelayLookup[ State.MoveBathToDryPosition] = ParameterBundle.Configuration.BeforeMoveBathToDryPositionDelayMs;
            DelayLookup[ State.TurnVacuumOn] = ParameterBundle.Configuration.BeforeTurnVacuumOnDelayMs;
            DelayLookup[ State.TurnAirOn] = ParameterBundle.Configuration.BeforeTurnAirOnDelayMs;
            DelayLookup[ State.MovePlenumToDryPosition] = ParameterBundle.Configuration.BeforeMovePlenumToDryPositionDelayMs;
            DelayLookup[ State.MovePlenumToRetractedPosition] = ParameterBundle.Configuration.BeforeMovePlenumToRetractedPositionDelayMs;
            DelayLookup[ State.TurnOffAir] = ParameterBundle.Configuration.BeforeTurnOffAirDelayMs;
            DelayLookup[ State.TurnOffVacuum] = ParameterBundle.Configuration.BeforeTurnOffVacuumDelayMs;
            DelayLookup[ State.MoveBathToRetractedPosition] = ParameterBundle.Configuration.BeforeMoveBathToRetractedPositionDelayMs;

            InitializeStates();
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        protected virtual StateMachine< State, Trigger>.StateConfiguration ConfigureState( State state, Action action, State next_state, State error_state, State debug_state)
        {
            StateMachine< State, Trigger>.StateConfiguration state_config = ConfigureState( state, action, next_state, error_state);
            state_config.Permit( Trigger.Debug, debug_state);
            SM.Configure( debug_state)
                .Permit( SuccessTrigger, next_state)
                .OnEntry( PostDebug);
            FromDebugStateToNextStateLookup[ debug_state] = next_state;
            return state_config;
        }
        // ----------------------------------------------------------------------
        private void PostDebug()
        {
            List< string> error_strings = new List< string>{ "Continue"};
            string debug_state_name = SM.State.ToString();
            string state_name = debug_state_name.Remove( debug_state_name.Length - 5);
            ErrorData error_data = new ErrorData( String.Format( "Completed state {0}.  Next state will be {1}, which has a {2} millisecond delay.", state_name, FromDebugStateToNextStateLookup[ SM.State].ToString(), DelayLookup[ FromDebugStateToNextStateLookup[ SM.State]]), error_strings);
            SMStopwatch.Stop();
            ErrorInterface.AddError( error_data);
            List< ManualResetEvent> events = new List< ManualResetEvent>{ _main_gui_abort_event};
            events.AddRange( error_data.EventArray);
            int event_index = WaitHandle.WaitAny( events.ToArray());
            SMStopwatch.Start();
            Fire( Trigger.Success);
        }
        // ----------------------------------------------------------------------
        private void InitializeStates()
        {
            //              vvv CURRENT STATE vvv                   vvv STATE FUNCTION vvv          vvv NEXT STATE vvv                      vvv ERROR STATE vvv                         vvv DEBUG STATE vvv
            ConfigureState( State.Start,                            NullStateFunction,              State.OnStartCritical);
            ConfigureState( State.OnStartCritical,                  OnStartCritical,                State.RetractPlenumAndBath);
            ConfigureState( State.RetractPlenumAndBath,             RetractPlenumAndBath,           State.MoveShuttleToWashPosition,        State.RetractPlenumAndBathError,            State.RetractPlenumAndBathDebug);
            ConfigureState( State.MoveShuttleToWashPosition,        MoveShuttleToWashPosition,      State.MoveWasherToWashPosition,         State.MoveShuttleToWashPositionError,       State.MoveShuttleToWashPositionDebug);
            ConfigureState( State.MoveWasherToWashPosition,         MoveWasherToWashPosition,       State.TurnOnBathWater,                  State.MoveWasherToWashPositionError,        State.MoveWasherToWashPositionDebug);
            ConfigureState( State.TurnOnBathWater,                  TurnOnBathWater,                State.TurnOnPlenumWater,                State.TurnOnBathWaterError,                 State.TurnOnBathWaterDebug);
            ConfigureState( State.TurnOnPlenumWater,                TurnOnPlenumWater,              State.TurnOnOverflowExhaust,            State.TurnOnPlenumWaterError,               State.TurnOnPlenumWaterDebug);
            ConfigureState( State.TurnOnOverflowExhaust,            TurnOnOverflowExhaust,          State.TurnOffOverflowExhaust,           State.TurnOnOverflowExhaustError,           State.TurnOnOverflowExhaustDebug);
            ConfigureState( State.TurnOffOverflowExhaust,           TurnOffOverflowExhaust,         State.TurnOffPlenumWater,               State.TurnOffOverflowExhaustError,          State.TurnOffOverflowExhaustDebug);
            ConfigureState( State.TurnOffPlenumWater,               TurnOffPlenumWater,             State.TurnOffBathWater,                 State.TurnOffPlenumWaterError,              State.TurnOffPlenumWaterDebug);
            ConfigureState( State.TurnOffBathWater,                 TurnOffBathWater,               State.MoveBathToDryPosition,            State.TurnOffBathWaterError,                State.TurnOffBathWaterDebug);
            ConfigureState( State.MoveBathToDryPosition,            MoveBathToDryPosition,          State.TurnVacuumOn,                     State.MoveBathToDryPositionError,           State.MoveBathToDryPositionDebug);
            ConfigureState( State.TurnVacuumOn,                     TurnVacuumOn,                   State.TurnAirOn,                        State.TurnVacuumOnError,                    State.TurnVacuumOnDebug);
            ConfigureState( State.TurnAirOn,                        TurnAirOn,                      State.MovePlenumToDryPosition,          State.TurnAirOnError,                       State.TurnAirOnDebug);
            ConfigureState( State.MovePlenumToDryPosition,          MovePlenumToDryPosition,        State.MovePlenumToRetractedPosition,    State.MovePlenumToDryPositionError,         State.MovePlenumToDryPositionDebug);
            ConfigureState( State.MovePlenumToRetractedPosition,    MovePlenumToRetractedPosition,  State.TurnOffAir,                       State.MovePlenumToRetractedPositionError,   State.MovePlenumToRetractedPositionDebug);
            ConfigureState( State.TurnOffAir,                       TurnOffAir,                     State.TurnOffVacuum,                    State.TurnOffAirError,                      State.TurnOffAirDebug);
            ConfigureState( State.TurnOffVacuum,                    TurnOffVacuum,                  State.MoveBathToRetractedPosition,      State.TurnOffVacuumError,                   State.TurnOffVacuumDebug);
            ConfigureState( State.MoveBathToRetractedPosition,      MoveBathToRetractedPosition,    State.OnFinishCritical,                 State.MoveBathtoRetractedPositionError,     State.MoveBathtoRetractedPositionDebug);
            ConfigureState( State.OnFinishCritical,                 OnFinishCritical,               State.End);
            ConfigureState( State.End,                              EndStateFunction);
            ConfigureState( State.Abort,                            AbortedStateFunction);
        }

        // ----------------------------------------------------------------------
        // state functions.
        // ----------------------------------------------------------------------
        private void RetractPlenumAndBath()
        {
            try{
                Thread.Sleep( DelayLookup[ SM.State]);
                TipShuttle.MoveWasher( WasherPosition.Retracted);
                Fire( Debug ? Trigger.Debug : Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void MoveShuttleToWashPosition()
        {
            try{
                Thread.Sleep( DelayLookup[ SM.State]);
                TipShuttle.MoveToWasher();
                Fire( Debug ? Trigger.Debug : Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void MoveWasherToWashPosition()
        {
            try{
                Thread.Sleep( DelayLookup[ SM.State]);
                TipShuttle.MoveWasher( WasherPosition.Wash);
                Fire( Debug ? Trigger.Debug : Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void TurnOnBathWater()
        {
            try{
                Thread.Sleep( DelayLookup[ SM.State]);
                
                // ----------------------------------------------------------
                // PUT METHOD HERE   ||||||||||||||||||
                //                   VVVVVVVVVVVVVVVVVV
                // vacuum io bit is 13
                
                // Move plenum to wash position (the head is seal on top of the tips at this point
                TipShuttle.MoveWasher( WasherPosition.Wash, move_bath: false);
               
                // Turn on vacuum pump
                TipShuttle.IOInterface.SetOutputState( 13, true);

                // ----------------------------------------------------------- ADDED 5-15 ----------------------------------------------
                // THIS IS WHERE THE METHOD HAS TO CHANGE TO MAKE SURE THE RIGHT VALES ARE GETTING TURNED ON DEPENDING ON WHICH WASHER IS RUNNING
                // ---------------  ||||||||||||||||||
                //                  VVVVVVVVVVVVVVVVVV

                // Turn on overflow vacuum
                TipShuttle.IOInterface.SetOutputState( 3, true);
                
                // Turn on the water valve right now the PSI is ~2
                TipShuttle.IOInterface.SetOutputState( 6, true);
                // Let the water flow for 3 seconds to "prime the lines because they will be empty from the previous wash cycle which ends with a vacuum step
                Thread.Sleep( 3000);
                // Turn off the water valve
                TipShuttle.IOInterface.SetOutputState( 6, false);

                for (int i=0; i<3; ++i){
                    // Move the bath to the "wash" position which doesn't seal the bath against the tip carrier
                    TipShuttle.MoveWasher( WasherPosition.Wash);
                    // Turn on bath water
                    TipShuttle.IOInterface.SetOutputState( 7, true);
                    // turn on the plenum water
                    TipShuttle.IOInterface.SetOutputState( 6, true);
                    // Leave on for 5 seconds
                    Thread.Sleep( 5000);
                    // Turn off the plenum water 
                    TipShuttle.IOInterface.SetOutputState( 6, false);
                    // Turn off bath water
                    TipShuttle.IOInterface.SetOutputState( 7, false);

                    // Cycle again but do a quick drying of the tips
                    // Turn on the valve that pulls the water out of the bath let all the water empty out before moving the bath up
                    TipShuttle.IOInterface.SetOutputState( 8, true);
                    Thread.Sleep( 2000);
                    // Move the bath to the "dry" position
                    // Move the plenum to the "dry" position
                    TipShuttle.MoveWasher( WasherPosition.Dry);
                    // Let the bath and the tips empty out
                    Thread.Sleep( 2000);
                    TipShuttle.IOInterface.SetOutputState( 8, false);
                }

                // Repeat the  wash cycle
                // Move the bath to the "wash" position which doesn't seal the bath against the tip carrier
                TipShuttle.MoveWasher( WasherPosition.Wash);
                // Turn on bath water
                TipShuttle.IOInterface.SetOutputState( 7, true);
                // turn on the plenum water
                TipShuttle.IOInterface.SetOutputState( 6, true);
                // Leave on for 10 seconds
                Thread.Sleep( 10000);
                // Turn off the plenum water 
                TipShuttle.IOInterface.SetOutputState( 6, false);
                // Turn off bath water
                TipShuttle.IOInterface.SetOutputState( 7, false);

                // WATER IS DONE TIME TO DRY THE TIPS
                // Slowly move the bath down while it is full of water -- ADDED 5-15
                TipShuttle.MoveWasher( WasherPosition.Retracted, move_plenum:false, speed_factor:0.1 );
                // DONE ADDED 5-15
                // Leave overflow vacuum on for 10 seconds
                Thread.Sleep( 10000);
                // Turn off overflow vacuum
                TipShuttle.IOInterface.SetOutputState( 3, false);

                // Turn on the valve that pulls the water out of the bath let all the water empty out before moving the bath up
                TipShuttle.IOInterface.SetOutputState( 8, true);
                Thread.Sleep( 9000);
                // Move the bath to the "dry" position
                // Move the plenum to the "dry" position
                TipShuttle.MoveWasher( WasherPosition.Dry);
                // Let the bath and the tips empty out
                Thread.Sleep( 10000);
                // Turn off the vacuum to the bath
                TipShuttle.IOInterface.SetOutputState( 8, false);
                // empty out the plenum
                // Move the bath down
                TipShuttle.MoveWasher( WasherPosition.Retracted, move_plenum:false );
                // Turn on the valve to pull vacuum in the upper plenum
                TipShuttle.IOInterface.SetOutputState( 9, true);
                // wait for the plenum to clear
                Thread.Sleep( 5000);
                // Turn off the valve to the vacuum in the upper plenum
                TipShuttle.IOInterface.SetOutputState( 9, false);

                // hold onto the tips on the bottom
                // move the bath up to seal onto the tips
                TipShuttle.MoveWasher( WasherPosition.Dry, move_plenum:false );
                // turn on the bath vacuum
                TipShuttle.IOInterface.SetOutputState( 8, true);

                // pull away from the tips 
                TipShuttle.MoveWasher( WasherPosition.Retracted);
                // turn off the bath vacuum
                TipShuttle.IOInterface.SetOutputState( 8, false);

                // turn off vacuum pump
                TipShuttle.IOInterface.SetOutputState( 13, false);
                
                
                //                  ^^^^^^^^^^^^
                // PUT METHOD HERE  ||||||||||||
                // ---------------------------------------------------------------------------------
                
                                                
                //TipShuttle.SetBathWaterSwitch( true);
                Fire( Debug ? Trigger.Debug : Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void TurnOnPlenumWater()
        {
            try{
                //Thread.Sleep( DelayLookup[ SM.State]);
                //TipShuttle.SetPlenumWaterSwitch( true);
                Fire( Debug ? Trigger.Debug : Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void TurnOnOverflowExhaust()
        {
            try{
                //Thread.Sleep( DelayLookup[ SM.State]);
                //TipShuttle.SetOverflowExhaustSwitch( true);
                Fire( Debug ? Trigger.Debug : Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void TurnOffOverflowExhaust()
        {
            try{
                //Thread.Sleep( DelayLookup[ SM.State]);
                //TipShuttle.SetOverflowExhaustSwitch( false);
                Fire( Debug ? Trigger.Debug : Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void TurnOffPlenumWater()
        {
            try{
                //Thread.Sleep( DelayLookup[ SM.State]);
                //TipShuttle.SetPlenumWaterSwitch( false);
                Fire( Debug ? Trigger.Debug : Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void TurnOffBathWater()
        {
            try{
                //Thread.Sleep( DelayLookup[ SM.State]);
                //TipShuttle.SetBathWaterSwitch( false);
                Fire( Debug ? Trigger.Debug : Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void MoveBathToDryPosition()
        {
            try{
                //Thread.Sleep( DelayLookup[ SM.State]);
                //TipShuttle.MoveWasher( WasherPosition.Dry, move_plenum: false);
                Fire( Debug ? Trigger.Debug : Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void TurnVacuumOn()
        {
            try{
                //Thread.Sleep( DelayLookup[ SM.State]);
                //TipShuttle.SetVacuumSwitch( true);
                Fire( Debug ? Trigger.Debug : Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void TurnAirOn()
        {
            try{
                //Thread.Sleep( DelayLookup[ SM.State]);
                //TipShuttle.SetAirSwitch( true);
                Fire( Debug ? Trigger.Debug : Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void MovePlenumToDryPosition()
        {
            try{
                //Thread.Sleep( DelayLookup[ SM.State]);
                //TipShuttle.MoveWasher( WasherPosition.Dry, move_bath: false);
                Fire( Debug ? Trigger.Debug : Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void MovePlenumToRetractedPosition()
        {
            try{
                //Thread.Sleep( DelayLookup[ SM.State]);
                //TipShuttle.MoveWasher( WasherPosition.Retracted, move_bath: false, speed_factor: ParameterBundle.Configuration.PlenumToRetractSpeedFactor);
                Fire( Debug ? Trigger.Debug : Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void TurnOffAir()
        {
            try{
                //Thread.Sleep( DelayLookup[ SM.State]);
                //TipShuttle.SetAirSwitch( false);
                Fire( Debug ? Trigger.Debug : Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void TurnOffVacuum()
        {
            try{
                //Thread.Sleep( DelayLookup[ SM.State]);
                //TipShuttle.SetVacuumSwitch( false);
                Fire( Debug ? Trigger.Debug : Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        private void MoveBathToRetractedPosition()
        {
            try{
                //Thread.Sleep( DelayLookup[ SM.State]);
                //TipShuttle.MoveWasher( WasherPosition.Retracted, move_plenum: false);
                Fire( Debug ? Trigger.Debug : Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
        // ----------------------------------------------------------------------
        protected override void EndStateFunction()
        {
            ParameterBundle.Messenger.Send( new WashTipsCompleteMessage( TipShuttle));
            base.EndStateFunction();
        }
    }
}