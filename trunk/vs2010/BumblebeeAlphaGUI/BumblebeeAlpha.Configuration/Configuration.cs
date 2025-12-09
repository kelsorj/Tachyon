namespace BioNex.BumblebeePlugin
{
    public class BumblebeeConfiguration
    {
        public int TipsToUsePerTipbox { get; set; }
        public bool WarnForExtraPlates { get; set; }
        public int ExtraTipboxesRequired { get; set; }
        public bool EverlastingTipbox { get; set; }
        public double ZTipShuckAcceleration { get; set; }
        public bool UseWToShuck { get; set; }
        public double ZTipShuttleNestOffset { get; set; }
        public double TotalTipHeight { get; set; }
        public double TipCollarHeight { get; set; }
        public double NippleCollarOverlap { get; set; }
        public double TipPressRampDistance { get; set; }
        public double TipPressTipBoxAdditionalPush { get; set; }
        public double TipPressTipShuttleAdditionalPush { get; set; }
        public double TipBoxHeight { get; set; }
        public int BeforeMoveShuttleToWashPositionDelayMs { get; set; }
        public int BeforeMoveWasherToWashPositionDelayMs { get; set; }
        public int BeforeTurnOnBathWaterDelayMs { get; set; }
        public int BeforeTurnOnPlenumWaterDelayMs { get; set; }
        public int BeforeTurnOnOverflowExhaustDelayMs { get; set; }
        public int BeforeTurnOffOverflowExhaustDelayMs { get; set; }
        public int BeforeTurnOffPlenumWaterDelayMs { get; set; }
        public int BeforeTurnOffBathWaterDelayMs { get; set; }
        public int BeforeMoveBathToDryPositionDelayMs { get; set; }
        public int BeforeTurnVacuumOnDelayMs { get; set; }
        public int BeforeTurnAirOnDelayMs { get; set; }
        public int BeforeMovePlenumToDryPositionDelayMs { get; set; }
        public int BeforeMovePlenumToRetractedPositionDelayMs { get; set; }
        public int BeforeTurnOffAirDelayMs { get; set; }
        public int BeforeTurnOffVacuumDelayMs { get; set; }
        public int BeforeMoveBathToRetractedPositionDelayMs { get; set; }
        public double PlenumToRetractSpeedFactor { get; set; }

        public BumblebeeConfiguration()
        {
            ExtraTipboxesRequired = 1;
            ZTipShuckAcceleration = 1000.0;
            ZTipShuttleNestOffset = -10.0; // empirically measured by RL.
            TotalTipHeight = 57.47; // empirically measured by FYC.
            TipCollarHeight = 22.76; // empirically measured by FYC.
            NippleCollarOverlap = 8.0;
            TipPressRampDistance = 8.0;
            TipPressTipBoxAdditionalPush = 1.0;
            TipPressTipShuttleAdditionalPush = 0.0;
            TipBoxHeight = 46.2;
            BeforeMoveShuttleToWashPositionDelayMs = 1;
            BeforeMoveWasherToWashPositionDelayMs = 2;
            BeforeTurnOnBathWaterDelayMs = 3;
            BeforeTurnOnPlenumWaterDelayMs = 4;
            BeforeTurnOnOverflowExhaustDelayMs = 5;
            BeforeTurnOffOverflowExhaustDelayMs = 6;
            BeforeTurnOffPlenumWaterDelayMs = 7;
            BeforeTurnOffBathWaterDelayMs = 8;
            BeforeMoveBathToDryPositionDelayMs = 9;
            BeforeTurnVacuumOnDelayMs = 10;
            BeforeTurnAirOnDelayMs = 11;
            BeforeMovePlenumToDryPositionDelayMs = 12;
            BeforeMovePlenumToRetractedPositionDelayMs = 13;
            BeforeTurnOffAirDelayMs = 14;
            BeforeTurnOffVacuumDelayMs = 15;
            BeforeMoveBathToRetractedPositionDelayMs = 16;
            PlenumToRetractSpeedFactor = 1.0;
        }

        public double GetZChannelWithTipAboveTipShuttleWithTipsOffset()
        {
            return TipCollarHeight + ZTipShuttleNestOffset + 5.0 /* buffer space */;
        }

        public double GetZChannelWithTipInsertedInTipNestOffset( bool using_tip_box)
        {
            return -TotalTipHeight + TipCollarHeight + ( using_tip_box ? TipBoxHeight : ZTipShuttleNestOffset);
        }

        public double GetZChannelWithoutTipReadyForTipPress( bool using_tip_box)
        {
            return GetZChannelWithTipInsertedInTipNestOffset( using_tip_box) + NippleCollarOverlap + TipPressRampDistance;
        }
    }
}
