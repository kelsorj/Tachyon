using System;
using System.Collections.Generic;
using System.Linq;
using BioNex.BumblebeePlugin.Hardware;
using BioNex.Shared.Utils.WellMathUtil;

namespace BioNex.BumblebeePlugin.Scheduler.DualChannelScheduler
{
    public class TipTracker : TipCarrier
    {
        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public TipTracker()
            : base( null, LabwareFormat.LF_STANDARD_96) //! \todo FYC get rid of hardcoded 96-well labware format.
        {
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public TipWell ReserveOneTip()
        {
            int[] consumption_pattern = { 17, 11, 16, 10, 15,  9, 14,  8, 13,  7, 12,  6,
                                          23,  5, 22,  4, 21,  3, 20,  2, 19,  1, 18,  0};
            for( int rowx2 = 4 - 1; rowx2 >= 0; --rowx2){
                for( int i = 0; i < 24; ++i){
                    int index = rowx2 * 24 + consumption_pattern[ i];
                    Well well = new Well( LabwareFormat.LF_STANDARD_96, index); //! \todo FYC get rid of hardcoded 96-well labware format.
                    TipWell tip_well = GetTipWell( well.RowIndex, well.ColIndex);
                    if( tip_well.State == TipWellState.Clean){
                        tip_well.SetState( TipWellState.InUse);
                        return tip_well;
                    }
                }
            }
            return null;
        }
        // ----------------------------------------------------------------------
        public Tuple< TipWell, TipWell> ReserveTwoCompatibleTips( double channel_spacing)
        {
            List< TipWell> clean_tip_wells = new List< TipWell>();
            foreach( TipWell tip_well in TipWells){
                if( tip_well.State == TipWellState.Clean){
                    clean_tip_wells.Add( tip_well);
                }
            }
            if( clean_tip_wells.Count() < 2){
                throw new Exception( "should not call this function if you already know there aren't enough clean tips");
            }
            var x = from c1 in clean_tip_wells
                    from c2 in clean_tip_wells
                    where TipWellComparer.TheTipWellComparer.Compare( c1, c2) < 0
                    let WellSpacing = WellMathUtil.CalculateWellSpacing( LabwareFormat.LF_STANDARD_96, c1, c2) //! \todo FYC get rid of hardcoded 96-well labware format.
                    where WellSpacing > channel_spacing
                    orderby WellSpacing
                    select new{ C1 = c1, C2 = c2, Spacing = WellSpacing};
            var y = x.FirstOrDefault();
            if( y != null){
                y.C1.SetState( TipWellState.InUse);
                y.C2.SetState( TipWellState.InUse);
                return Tuple.Create( y.C1, y.C2);
            } else{
                clean_tip_wells.First().SetState( TipWellState.InUse);
                clean_tip_wells.Last().SetState( TipWellState.InUse);
                return Tuple.Create( clean_tip_wells.First(), clean_tip_wells.Last());
            }
        }
    }
}
