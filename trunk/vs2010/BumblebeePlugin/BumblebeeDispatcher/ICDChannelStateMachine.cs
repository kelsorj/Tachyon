using System;
using System.Collections.Generic;
using BioNex.BumblebeePlugin.Hardware;
using Stateless;

namespace BioNex.BumblebeePlugin.Dispatcher
{
    public abstract class ICDChannelStateMachine< TState, TTrigger> : ICDStateMachine< TState, TTrigger>
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        protected TState DisableChannelState { get; private set; }
        protected TTrigger DisableChannelTrigger { get; private set; }
        protected Channel Channel { get; private set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        protected ICDChannelStateMachine(ICDParameterBundle parameter_bundle, ICDEventBundle event_bundle, DispatcherJob job, Channel channel)
            : base(parameter_bundle, event_bundle, job)
        {
            DisableChannelState = ( TState)( Enum.Parse( typeof( TState), "DisableChannel"));
            DisableChannelTrigger = ( TTrigger)( Enum.Parse( typeof( TTrigger), "DisableChannel"));
            Channel = channel;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        protected override StateMachine< TState, TTrigger>.StateConfiguration ConfigureState(TState state, Action action, TState next_state, TState error_state, bool configure_ignore = false)
        {
            StateMachine< TState, TTrigger>.StateConfiguration state_config = SM.Configure( state)
                .Permit( SuccessTrigger, next_state)
                .Permit( FailureTrigger, error_state)
                .Permit( AbortTrigger, AbortState)
                .OnEntry( action);
            StateMachine< TState, TTrigger>.StateConfiguration error_state_config = SM.Configure( error_state)
                .Permit( RetryTrigger, state)
                .Permit( DisableChannelTrigger, DisableChannelState)
                .Permit( AbortTrigger, AbortState);
            if( !configure_ignore){
                error_state_config.OnEntry( HandleErrorWithRetryAndDisableChannel);
            } else{
                error_state_config.Permit( IgnoreTrigger, next_state);
                error_state_config.OnEntry( HandleErrorWithRetryIgnoreAndDisableChannel);
            }
            return state_config;
        }
        // ----------------------------------------------------------------------
        protected void HandleErrorWithRetryAndDisableChannel()
        {
            IDictionary< string, TTrigger> label_to_trigger = new Dictionary< string, TTrigger>();
            label_to_trigger[ "Try move again"] = RetryTrigger;
            label_to_trigger[ "Disable channel for remainder of protocol"] = DisableChannelTrigger;
            HandleLabels( label_to_trigger);
        }
        // ----------------------------------------------------------------------
        protected void HandleErrorWithRetryIgnoreAndDisableChannel()
        {
            throw new NotImplementedException();
        }

        // ----------------------------------------------------------------------
        // state functions.
        // ----------------------------------------------------------------------
        protected override void AbortedStateFunction()
        {
            try{
                Channel.ReturnHome();
            } catch( Exception){
            }
            base.AbortedStateFunction();
        }
        // ----------------------------------------------------------------------
        protected virtual void DisableChannelStateFunction()
        {
            try{
                Channel.ReturnHome();
                ParameterBundle.Messenger.Send( new DisableChannelMessage( Channel));
                Fire( SuccessTrigger);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }
    }
}
