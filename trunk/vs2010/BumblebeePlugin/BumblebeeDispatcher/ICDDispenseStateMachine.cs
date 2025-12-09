using System;
using BioNex.BumblebeePlugin.Hardware;
using BioNex.Shared.DeviceInterfaces;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.Utils;
using log4net;

namespace BioNex.BumblebeePlugin.Dispatcher
{
    public class ICDDispenseStateMachine2 : ICDLiquidTransferStateMachine2
    {
        private static readonly ILog pipette_log_ = LogManager.GetLogger("PipetteLogger");
        public ILimsOutputTransferLog OutputPlugin { get; set; }

        public ICDDispenseStateMachine2(ICDParameterBundle parameter_bundle, ICDEventBundle event_bundle, DispatcherJob job, Channel channel, Transfer transfer, ILabware labware_type, ILiquidProfile liquid_profile, ILiquidProfile mix_liquid_profile, double x_coordinate, double z_origin, double z_above_plates, double distance_from_well_bottom, double volume, ILimsOutputTransferLog output_plugin)
            : base(parameter_bundle, event_bundle, job, channel, transfer, labware_type, liquid_profile, mix_liquid_profile, x_coordinate, z_origin, z_above_plates, distance_from_well_bottom, volume)
        {
            bool is_interpolated;
            double z_well_bottom = LabwareType[ LabwarePropertyNames.Thickness].ToDouble() - LabwareType[ LabwarePropertyNames.WellDepth].ToDouble();
            ZStartLiquidTransfer = z_well_bottom + distance_from_well_bottom;
            ZEndLiquidTransfer = ZStartLiquidTransfer + LiquidProfile.ZMoveDuringDispensing;
            WStartLiquidTransfer = LiquidProfile.PreAspirateVolume + LiquidProfile.GetAdjustedVolume( volume, out is_interpolated);
            WEndLiquidTransfer = LiquidProfile.PreAspirateVolume - LiquidProfile.PostDispenseVolume;

            if( MixLiquidProfile != null){
                MixCycles = LiquidProfile.PostDispenseMixCycles;
                ZMix = ZStartLiquidTransfer + MixLiquidProfile.ZMoveDuringDispensing;
                WMix = LiquidProfile.PreAspirateVolume + MixLiquidProfile.GetAdjustedVolume( LiquidProfile.PostDispenseMixVolume, out is_interpolated);
            }

            OutputPlugin = output_plugin;
        }

        // methods.
        protected override double GetZUnpierceSrc()
        {
            return (( MixLiquidProfile != null) && ( MixCycles > 0)) ? ZMix : ZEndLiquidTransfer;
        }

        // state functions.
        protected override void TransferLiquid()
        {
            try{
                double w_end_dispense = (( MixLiquidProfile != null) && ( MixCycles > 0)) ? LiquidProfile.PreAspirateVolume : WEndLiquidTransfer;
                AspirateOrDispense( LiquidProfile, false, w_end_dispense, ZEndLiquidTransfer, WStartLiquidTransfer, ZStartLiquidTransfer);

                // #370
                log4net.ThreadContext.Properties["ChannelID"] = Channel.ID.ToString();
                log4net.ThreadContext.Properties["DeviceName"] = BioNexDeviceNames.Bumblebee;
                // DKM 2011-03-22 I think we should be passing LiquidTransferJob instead of DispatcherJob, since this
                //                is specifically a liquid transfer state machine, but I will have Felix review this.
                LiquidTransferJob job = Job as LiquidTransferJob;
                Transfer t = job.TransfersToPerform[Channel];
                log4net.ThreadContext.Properties["SourceBarcode"] = t.SrcPlate.Barcode.Value;
                log4net.ThreadContext.Properties["SourceWell"] = t.SrcWell.WellName;
                log4net.ThreadContext.Properties["DestinationBarcode"] = t.DstPlate.Barcode.Value;
                log4net.ThreadContext.Properties["DestinationWell"] = t.DstWell.WellName;
                double transfer_volume = t.TransferUnits == VolumeUnits.ul ? t.TransferVolume : t.TransferVolume * 1000;
                log4net.ThreadContext.Properties["Volume"] = transfer_volume.ToString();
                pipette_log_.Info( "transfer complete");
                // check for null because a plugin for output might not be available
                if( OutputPlugin != null){
                    OutputPlugin.LogTransfer( t.SrcPlate.Barcode, t.SrcWell.WellName, t.DstPlate.Barcode, t.DstWell.WellName, transfer_volume, DateTime.Now); 
                }

                if(( MixLiquidProfile != null) && ( MixCycles > 0)){
                    double velocity = Math.Abs( ZEndLiquidTransfer - ZMix) / LiquidProfile.TimeToEnterLiquid;
                    Channel.MoveAbsoluteZOffset( ZOrigin, ZMix, velocity, true, true, false);
                    for( long cycle = 1; cycle <= MixCycles; ++cycle){
                        AspirateOrDispense( MixLiquidProfile, true, WMix, ZStartLiquidTransfer, w_end_dispense, ZMix);
                        AspirateOrDispense( MixLiquidProfile, false, w_end_dispense, ZMix, WMix, ZStartLiquidTransfer);
                    }
                    Channel.MoveWToAbsoluteUl( WEndLiquidTransfer, true);
                }

                Fire( Trigger.Success);
            } catch( Exception ex){
                StandardRetry( ex);
            }
        }

        protected override void EndStateFunction()
        {
            ParameterBundle.Messenger.Send( new TransferCompleteMessage( Channel, Transfer));
            ParameterBundle.Messenger.Send( new NumberOfTransferCompleteMessage( 1));
            base.EndStateFunction();
        }

        protected override void AbortedStateFunction()
        {
            ParameterBundle.Messenger.Send( new TransferAbortedMessage( Channel, Transfer));
            base.AbortedStateFunction();
        }
    }
}
