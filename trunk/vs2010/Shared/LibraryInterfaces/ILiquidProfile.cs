using System.Collections.Generic;
using System.Text;

namespace BioNex.Shared.LibraryInterfaces
{
    public static class LiquidProfileExtensions
    {
        public static string DumpInfo( this ILiquidProfile profile)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine( "***** " + profile.Name + " *****");
            sb.AppendLine( "Is mixing profile: " + profile.IsMixingProfile.ToString());
            sb.AppendLine( "Pre aspirate volume: " + profile.PreAspirateVolume.ToString());
            sb.AppendLine( "Rate to aspirate: " + profile.RateToAspirate.ToString());
            sb.AppendLine( "Max accel during aspirate: " + profile.MaxAccelDuringAspirate.ToString());
            sb.AppendLine( "Post aspirate delay: " + profile.PostAspirateDelay.ToString());
            sb.AppendLine( "Post dispense volume: " + profile.PostDispenseVolume.ToString());
            sb.AppendLine( "RateToDispense: " + profile.RateToDispense.ToString());
            sb.AppendLine( "Max accel during dispense: " + profile.MaxAccelDuringDispense.ToString());
            sb.AppendLine( "Post dispense delay: " + profile.PostDispenseDelay.ToString());
            sb.AppendLine( "Z move during aspirate: " + profile.ZMoveDuringAspirating.ToString());
            sb.AppendLine( "Z move during dispense: " + profile.ZMoveDuringDispensing.ToString());
            sb.AppendLine( "Time to enter liquid: " + profile.TimeToEnterLiquid.ToString());
            sb.AppendLine( "Time to exit liquid: " + profile.TimeToExitLiquid.ToString());
            sb.AppendLine( "Pre aspirate mix liquid profile: " + profile.PreAspirateMixLiquidProfile);
            sb.AppendLine( "Pre aspirate mix cycles: " + profile.PreAspirateMixCycles.ToString());
            sb.AppendLine( "Pre aspirate mix volume: " + profile.PreAspirateMixVolume.ToString());
            sb.AppendLine( "Post dispense mix liquid profile: " + profile.PostDispenseMixLiquidProfile);
            sb.AppendLine( "Post dispense mix cycles: " + profile.PostDispenseMixCycles.ToString());
            sb.AppendLine( "Post dispense mix volume: " + profile.PostDispenseMixVolume.ToString());

            return sb.ToString();
        }
    }

    public interface ILiquidProfile
    {
        long                    Id { get; set; }
        string                  Name { get; set; }
        bool                    IsFactoryProfile { get; set; }
        bool                    IsMixingProfile { get; set; }
        long                    BaseId { get; set; }
        long                    SyringeTypeId { get; set; }
        string                  SyringeSerialNumber { get; set; }
        long                    TipTypeId { get; set; }
        double                  PreAspirateVolume { get; set; }
        double                  RateToAspirate { get; set; }
        double                  MaxAccelDuringAspirate { get; set; }
        double                  PostAspirateDelay { get; set; }
        double                  PostDispenseVolume { get; set; }
        double                  RateToDispense { get; set; }
        double                  MaxAccelDuringDispense { get; set; }
        double                  PostDispenseDelay { get; set; }
        bool                    TrackFluidHeight { get; set; }
        double                  ZMoveDuringAspirating { get; set; }
        double                  ZMoveDuringDispensing { get; set; }
        /// <summary>
        /// time, in seconds, to move from the labware clearance position to the point where we will start aspirating
        /// </summary>
        double                  TimeToEnterLiquid { get; set; }
        double                  TimeToExitLiquid { get; set; }

        string                  PreAspirateMixLiquidProfile { get; set; }
        long                    PreAspirateMixCycles { get; set; }
        double                  PreAspirateMixVolume { get; set; }
        string                  PostDispenseMixLiquidProfile { get; set; }
        long                    PostDispenseMixCycles { get; set; }
        double                  PostDispenseMixVolume { get; set; }

        SortedList< double, double> GetCalibrationData();
        void                    SetCalibrationData( SortedList< double, double> calibration_data);
        double                  GetAdjustedVolume( double requested_volume, out bool is_interpolated);
    }

    public interface ILiquidCalibrationDatum
    {
        // long                    Id { get; set; }
        long                    LiquidProfileId { get; set; }
        double                  RequestedVolume { get; set; }
        double                  VolumeOffset { get; set; }
    }

    public interface ILiquidProfileLibrary
    {
        List< string>           EnumerateLiquidProfileNames();
        ILiquidProfile          LoadLiquidProfileByName( string name);
        void                    SaveLiquidProfileByName( ILiquidProfile liquid_profile);
        void                    DeleteLiquidProfileByName( string name);
    }
}
