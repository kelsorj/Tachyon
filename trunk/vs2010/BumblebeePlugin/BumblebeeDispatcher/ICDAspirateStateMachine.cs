using System;
using BioNex.BumblebeePlugin.Hardware;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.Utils;

namespace BioNex.BumblebeePlugin.Dispatcher
{
    public class ICDAspirateStateMachine2 : ICDLiquidTransferStateMachine2
    {
        public ICDAspirateStateMachine2(ICDParameterBundle parameter_bundle, ICDEventBundle event_bundle, DispatcherJob job, Channel channel, Transfer transfer, ILabware labware_type, ILiquidProfile liquid_profile, ILiquidProfile mix_liquid_profile, double x_coordinate, double z_origin, double z_above_plates, double distance_from_well_bottom, double volume)
            : base(parameter_bundle, event_bundle, job, channel, transfer, labware_type, liquid_profile, mix_liquid_profile, x_coordinate, z_origin, z_above_plates, distance_from_well_bottom, volume)
        {
            bool is_interpolated;
            double z_well_bottom = LabwareType[ LabwarePropertyNames.Thickness].ToDouble() - LabwareType[ LabwarePropertyNames.WellDepth].ToDouble();
            ZEndLiquidTransfer = z_well_bottom + distance_from_well_bottom;
            ZStartLiquidTransfer = ZEndLiquidTransfer + LiquidProfile.ZMoveDuringAspirating;
            WStartLiquidTransfer = LiquidProfile.PreAspirateVolume;
            WEndLiquidTransfer = LiquidProfile.PreAspirateVolume + LiquidProfile.GetAdjustedVolume( volume, out is_interpolated);

            if( MixLiquidProfile != null){
                MixCycles = LiquidProfile.PreAspirateMixCycles;
                ZMix = ZEndLiquidTransfer + MixLiquidProfile.ZMoveDuringAspirating;
                WMix = LiquidProfile.PreAspirateVolume + MixLiquidProfile.GetAdjustedVolume( LiquidProfile.PreAspirateMixVolume, out is_interpolated);
            }
        }

        // methods.
        protected override void PreApproachWell()
        {
            Channel.MoveWToAbsoluteUl( WStartLiquidTransfer, false);
        }

        protected override void PostApproachWell()
        {
            Channel.MoveWToAbsoluteUl( WStartLiquidTransfer, true);
        }

        protected override double GetZPierceDst()
        {
            return (( MixLiquidProfile != null) && ( MixCycles > 0)) ? ZMix : ZStartLiquidTransfer;
        }

        // state functions.
        protected override void TransferLiquid()
        {
            try{
                if(( MixLiquidProfile != null) && ( MixCycles > 0)){
                    for( long cycle = 1; cycle <= MixCycles; ++cycle){
                        AspirateOrDispense( MixLiquidProfile, true, WMix, ZEndLiquidTransfer, WStartLiquidTransfer, ZMix);
                        AspirateOrDispense( MixLiquidProfile, false, WStartLiquidTransfer, ZMix, WMix, ZEndLiquidTransfer);
                    }
                    double velocity = Math.Abs( ZMix - ZStartLiquidTransfer) / LiquidProfile.TimeToEnterLiquid;
                    Channel.MoveAbsoluteZOffset( ZOrigin, ZStartLiquidTransfer, velocity, true, true, false);
                }
                AspirateOrDispense( LiquidProfile, true, WEndLiquidTransfer, ZEndLiquidTransfer, WStartLiquidTransfer, ZStartLiquidTransfer);
                Fire( Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }

        protected override void EndStateFunction()
        {
            ParameterBundle.Messenger.Send( new AspirateCompleteMessage( Channel, Transfer));
            base.EndStateFunction();
        }
    }
}
/* old scripting code.
Dictionary< ScriptRunnerDelegate, IAsyncResult> script_threads = new Dictionary< ScriptRunnerDelegate, IAsyncResult>();
List< WaitHandle> events_to_wait_on = new List< WaitHandle>();
List< Exception> script_exceptions = new List< Exception>();

// DKM 2010-11-04 execute the python script here.  We'll have one per channel... how is that
//                going to work?  Spawn threads for each one?
// create an IEnumerable of stuff that we need to work on for scripts, i.e. transfers & channels
if( Transfer.AspirateScript != ""){
    ScriptRunnerDelegate thread = new ScriptRunnerDelegate( ScriptRunnerThread);
    IAsyncResult iar = thread.BeginInvoke( Transfer.AspirateScript, ScriptComplete, script_exceptions);
    events_to_wait_on.Add( iar.AsyncWaitHandle);
}

// wait for the spawned script threads to complete
foreach( WaitHandle wait_event in events_to_wait_on)
    wait_event.WaitOne();

//! \todo handle errors!!!
Debug.Assert( script_exceptions.Count == 0, "Error(s) occurred in pre-aspirate script(s)");
*/
