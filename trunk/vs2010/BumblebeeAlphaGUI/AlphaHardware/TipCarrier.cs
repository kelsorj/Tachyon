using System;
using System.Collections.Generic;
using BioNex.Shared.Utils.WellMathUtil;

namespace BioNex.BumblebeePlugin.Hardware
{
    public enum TipWellState
    {
        Empty,
        Clean,
        InUse,
        Dirty,
    }

    public class TipWell : Well
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public TipCarrier Carrier { get; private set; }
        public TipWellState State { get; private set; }

        private static double[] ColumnPositions = new double[ 4]{ -15 - 10.5, -10.5, 10.5, 10.5 + 15};

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public TipWell( TipCarrier parent, int row_index, int col_index)
            : base( row_index, col_index)
        {
            Carrier = parent;
            State = TipWellState.Empty;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public void SetState( TipWellState state)
        {
            State = state;
        }
        // ----------------------------------------------------------------------
        public Tuple< double, double> GetXYPosition( byte channel_id)
        {
            Tuple< double, double> center_xy = Carrier.Stage.GetCenterPosition( channel_id);
            return Tuple.Create( center_xy.Item1 + ColumnPositions[ ColIndex],
                                 center_xy.Item2 - ( 7.5 - (( double)RowIndex)) * 9.0);
        }
        // ----------------------------------------------------------------------
        public override string ToString()
        {
            return string.Format( "{0}[{1},{2}]", GetType().Name, RowIndex, ColIndex);
        }
    }

    public class TipWellComparer : Comparer< TipWell>, IEqualityComparer< TipWell>
    {
        // ----------------------------------------------------------------------
        // constants.
        // ----------------------------------------------------------------------
        public static readonly TipWellComparer TheTipWellComparer = new TipWellComparer();

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        private TipWellComparer() {}

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public override int Compare( TipWell x, TipWell y)
        {
            if( x.Carrier != y.Carrier){
                throw new Exception( "can't compare TipWells from different carriers");
            }
            return WellComparer.TheWellComparer.Compare( x, y);
        }
        // ----------------------------------------------------------------------
        #region IEqualityComparer< TipWell> Members
        // ----------------------------------------------------------------------
        public bool Equals( TipWell x, TipWell y)
        {
            if( x.Carrier != y.Carrier){
                throw new Exception( "can't compare TipWells from different carriers");
            }
            return WellComparer.TheWellComparer.Equals( x, y);
        }
        // ----------------------------------------------------------------------
        public int GetHashCode( TipWell obj)
        {
            return obj.GetHashCode();
        }
        // ----------------------------------------------------------------------
        #endregion
    }

    public class TipCarrierFormat : LabwareFormat
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public double ColToColSpacing { get { throw new Exception( "property not supported"); } set { throw new Exception( "property not supported"); }}

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public TipCarrierFormat()
            : base( 16, 4, 9.0, 0.0)
        {
        }

    }

    public class TipCarrier
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public Stage Stage { get; private set; }
        public LabwareFormat LabwareFormat { get; private set; }
        public int NumRows { get { return LabwareFormat.NumRows; }}
        public int NumCols { get { return LabwareFormat.NumCols; }}
        protected double RowToRowSpacing { get { return LabwareFormat.RowToRowSpacing; }}
        protected double ColToColSpacing { get { return LabwareFormat.ColToColSpacing; }}
        protected TipWell[,] TipWells { get; set; }
        protected static readonly Object TipStateLock = new Object();

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public TipCarrier( Stage stage, LabwareFormat labware_format)
        {
            Stage = stage;
            LabwareFormat = labware_format;
            TipWells = new TipWell[ NumRows, NumCols];
            for( int row_index = 0; row_index < NumRows; ++row_index){
                for( int col_index = 0; col_index < NumCols; ++col_index){
                    TipWells[ row_index, col_index] = new TipWell( this, row_index, col_index);
                }
            }
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public TipWell GetTipWell( int row_index, int col_index)
        {
            return TipWells[ row_index, col_index];
        }
        // ----------------------------------------------------------------------
        public bool IsAllDirty()
        {
            foreach( TipWell tip_well in TipWells){
                if( tip_well.State != TipWellState.Dirty){
                    return false;
                }
            }
            return true;
        }
        // ----------------------------------------------------------------------
        public int CountTipsOfState( TipWellState state)
        {
            int retval = 0;
            foreach( TipWell tip_well in TipWells){
                if( tip_well.State == state){
                    retval++;
                }
            }
            return retval;
        }
        // ----------------------------------------------------------------------
        public int CountTipsOfStateInRow( TipWellState state, int row_index)
        {
            if( row_index < 0 || row_index >= NumRows){
                return 0;
            }
            int retval = 0;
            for( int col_index = 0; col_index < NumCols; ++col_index){
                if( TipWells[ row_index, col_index].State == state){
                    retval++;
                }
            }
            return retval;
        }
        // ----------------------------------------------------------------------
        public void SetAll( TipWellState state)
        {
            foreach( TipWell tip_well in TipWells){
                tip_well.SetState( state);
            }
        }
        // ----------------------------------------------------------------------
        public TipWell ReserveTip( int row_index)
        {
            lock( TipStateLock){
                for( int col_index = 0; col_index < NumCols; ++col_index){
                    TipWell potential_reservation = TipWells[ row_index, col_index];
                    if( potential_reservation.State == TipWellState.Clean){
                        potential_reservation.SetState( TipWellState.InUse);
                        return potential_reservation;
                    }
                }
                return null;
            }
        }
    }
}
