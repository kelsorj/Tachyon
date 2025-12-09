using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Shared.Utils.WellMathUtil
{
#if !HIG_INTEGRATION
    public class LabwareFormat
    {
        // ----------------------------------------------------------------------
        // inner classes.
        // ----------------------------------------------------------------------
        public class InvalidWellCountException : Exception
        {
        }

        // ----------------------------------------------------------------------
        // constants.
        // ----------------------------------------------------------------------
        public static readonly LabwareFormat LF_STANDARD_48 = new LabwareFormat( 6, 8, 13.0, 13.0);
        public static readonly LabwareFormat LF_STANDARD_96 = new LabwareFormat( 8, 12, 9.0, 9.0);
        public static readonly LabwareFormat LF_STANDARD_384 = new LabwareFormat( 16, 24, 4.5, 4.5);
        public static readonly LabwareFormat LF_STANDARD_1536 = new LabwareFormat( 32, 48, 2.25, 2.25);

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public int NumRows { get; private set; }
        public int NumCols { get; private set; }
        public double RowToRowSpacing { get; private set; }
        public double ColToColSpacing { get; private set; }
        public double CenterRowIndex { get; private set; }
        public double CenterColIndex { get; private set; }
        public int NumWells { get{ return NumRows * NumCols; }}

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public LabwareFormat( int num_rows, int num_cols, double row_to_row_spacing, double col_to_col_spacing)
        {
            NumRows = num_rows;
            NumCols = num_cols;
            RowToRowSpacing = row_to_row_spacing;
            ColToColSpacing = col_to_col_spacing;
            CenterRowIndex = (( double)num_rows - 1.0) / 2.0;
            CenterColIndex = (( double)num_cols - 1.0) / 2.0;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        /// <summary>
        /// Calculate XY coordinates of subject well after rotation of well's plate by a given angle.
        /// </summary>
        /// <param name="well">The subject well.</param>
        /// <param name="angle">The applied angle of rotation.</param>
        /// <returns>A tuple containing XY coordinates of well after rotation of plate by angle.</returns>
        public Tuple< double, double> CalculatePostRotationXYCoordinates( Well well, double angle)
        {
            // pre-calculate cosine and sine of angle.
            double cos = Math.Cos( angle);
            double sin = Math.Sin( angle);
            // calculate pre-rotation XY coordinates of the subject well.
            double x_before_rotation = ( ( double)well.ColIndex - CenterColIndex) * ColToColSpacing;
            double y_before_rotation = ( CenterRowIndex - ( double)well.RowIndex) * RowToRowSpacing;
            // calculate post-rotation XY coordinates of the subject well.
            double x_after_rotation = x_before_rotation * cos - y_before_rotation * sin;
            double y_after_rotation = x_before_rotation * sin + y_before_rotation * cos;
            // return tuple containing XY coordinates of well after rotation of plate by angle.
            return Tuple.Create( Math.Round( x_after_rotation, 6),
                                 Math.Round( y_after_rotation, 6));
        }
        // ----------------------------------------------------------------------
        public override string ToString()
        {
            return string.Format( "{0}[{1},{2},{3},{4}]", GetType().Name, NumRows, NumCols, RowToRowSpacing, ColToColSpacing);
        }

        // ----------------------------------------------------------------------
        // class methods.
        // ----------------------------------------------------------------------
        public static LabwareFormat GetLabwareFormat( int num_wells)
        {
            switch( num_wells){
                case   48: return LF_STANDARD_48;
                case   96: return LF_STANDARD_96;
                case  384: return LF_STANDARD_384;
                case 1536: return LF_STANDARD_1536;
                default  : throw new InvalidWellCountException();
            }
        }
        // ----------------------------------------------------------------------
        /* not used -- SAVE FOR FUTURE MULTI DISPATCHER.
        internal class PostRotationXYCoordinatesExtResults
        {
            public Well Well { get; set; }
            public double PostRotationXCoordinate { get; set; }
            public double PostRotationYCoordinate { get; set; }
            public double PostRotationChannelYOffset { get; set; }
            public int PostRotationChannelGroup { get; set; }
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Calculate XY coordinates of subject wells after rotation of wells' plate by a given angle.
        /// </summary>
        /// <param name="wells">An enumeration of the subject wells.</param>
        /// <param name="angle">The applied angle of rotation.</param>
        /// <param name="channel_spacing">The channel-to-channel Y distance.</param>
        /// <returns>An enumeration of PostRotationXYCoordinatesExtResults containing the post-rotation XY coodinates + extended results.</returns>
        internal IEnumerable< PostRotationXYCoordinatesExtResults> CalculatePostRotationXYCoordinatesExt( IEnumerable< Well> wells, double angle, double channel_spacing)
        {
            // pre-calculate cosine and sine of angle.
            double cos = Math.Cos( angle);
            double sin = Math.Sin( angle);

            // declare and create List to contain post-rotation XY coodinates + extended results.
            List< PostRotationXYCoordinatesExtResults> results = new List< PostRotationXYCoordinatesExtResults>();

            // for each of the subject wells:
            foreach( Well well in wells){
                // calculate pre-rotation XY coodinates.
                double x_before_rotation = ( ( double)well.ColIndex - CenterColIndex) * ColToColSpacing;
                double y_before_rotation = ( CenterRowIndex - ( double)well.RowIndex) * RowToRowSpacing;
                // calculate post-rotation XY coodinates.
                double x_after_rotation = x_before_rotation * cos - y_before_rotation * sin;
                double y_after_rotation = x_before_rotation * sin + y_before_rotation * cos;
                // add post-rotation XY coordinates + extended results to List created for the purpose of aggregating the return value.
                results.Add( new PostRotationXYCoordinatesExtResults{ Well = well,
                                                                      PostRotationXCoordinate = Math.Round( x_after_rotation, 6),
                                                                      PostRotationYCoordinate = Math.Round( y_after_rotation, 6),
                                                                      PostRotationChannelYOffset = Math.Round( Math.IEEERemainder( y_after_rotation, channel_spacing), 6),
                                                                      PostRotationChannelGroup = ( int)( Math.Round( y_after_rotation / channel_spacing))});
            }

            // return post-rotation XY coordinates + extended results.
            return results;
        }
        */
    }

    public class Well
    {
        // ----------------------------------------------------------------------
        // exceptions.
        // ----------------------------------------------------------------------
        public class InvalidWellNameException : Exception
        {
            public string WellName { get; private set; }
            public InvalidWellNameException( string well_name)
            {
                WellName = well_name;
            }
        }

        // ----------------------------------------------------------------------
        // constants.
        // ----------------------------------------------------------------------
        public const string ANY_WELL_NAME = "ANY";
        public static readonly Well ANY_WELL = new Well( ANY_WELL_NAME);
        private static readonly char[] NUMERIC_CHARS = "0123456789".ToCharArray();
        private static readonly char[] ALPHABETIC_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public int RowIndex { get; private set; }
        public int ColIndex { get; private set; }
        public string WellName
        {
            get{
                if(( RowIndex == -1) && ( ColIndex == -1)){
                    return ANY_WELL_NAME;
                } else{
                    StringBuilder retval_sb = new StringBuilder();
                    retval_sb.Insert( 0, ( char)( 'A' + RowIndex % 26));
                    for( int temp = RowIndex / 26; temp > 0; temp = ( temp - 1) / 26){
                        retval_sb.Insert( 0, ( char)( 'A' + (( temp - 1) % 26)));
                    }
                    retval_sb.Append( ColIndex + 1);
                    return retval_sb.ToString();
                }
            }
        }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public Well( int row_index, int col_index)
        {
            RowIndex = row_index;
            ColIndex = col_index;
        }
        // ----------------------------------------------------------------------
        public Well( string well_name)
        {
            well_name = well_name.Trim().ToUpper();
            if( well_name == ANY_WELL_NAME.ToUpper()){
                RowIndex = -1;
                ColIndex = -1;
                return;
            }
            int alphabetic_start = well_name.IndexOfAny( ALPHABETIC_CHARS);
            if( alphabetic_start != 0){
                throw new InvalidWellNameException( well_name);
            }
            int alphabetic_end = StringUtil.FindFirstNotOf( well_name, ALPHABETIC_CHARS, alphabetic_start);
            if( alphabetic_end == -1){
                throw new InvalidWellNameException( well_name);
            }
            int numeric_start = well_name.IndexOfAny( NUMERIC_CHARS);
            if( numeric_start != alphabetic_end){
                throw new InvalidWellNameException( well_name);
            }
            int numeric_end = StringUtil.FindFirstNotOf( well_name, NUMERIC_CHARS, numeric_start);
            if( numeric_end != -1){
                throw new InvalidWellNameException( well_name);
            }

            string alphabetic_substring = well_name.Substring( alphabetic_start, alphabetic_end - alphabetic_start);
            string numeric_substring = well_name.Substring( numeric_start);
            int row_number = 0;
            for( int loop = 0; loop < alphabetic_substring.Length; loop++){
                row_number *= 26;
                row_number += ( alphabetic_substring[ loop] - 'A' + 1);
            }
            RowIndex = row_number - 1;
            ColIndex = int.Parse( numeric_substring) - 1;
        }
        // ----------------------------------------------------------------------
        public Well( string well_name, LabwareFormat labware_format)
            : this( well_name)
        {
            if( !FitsFormat( labware_format)){
                throw new InvalidWellNameException( well_name);
            }
        }
        // ----------------------------------------------------------------------
        public Well( LabwareFormat labware_format, int well_index)
        {
            RowIndex = well_index / labware_format.NumCols;
            ColIndex = well_index % labware_format.NumCols;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public bool IsAny()
        {
            return RowIndex == -1 && ColIndex == -1;
        }
        // ----------------------------------------------------------------------
        public bool FitsFormat( LabwareFormat labware_format)
        {
            return RowIndex < labware_format.NumRows && ColIndex < labware_format.NumCols;
        }
        // ----------------------------------------------------------------------
        public int GetWellIndex( LabwareFormat labware_format)
        {
            if(( RowIndex == -1) && ( ColIndex == -1)){
                return -1;
            } else{
                return ( RowIndex * labware_format.NumCols) + ColIndex;
            }
        }
        // ----------------------------------------------------------------------
        public override bool Equals( object obj)
        {
            Well other = obj as Well;
            if( other == null){
                return false;
            }
            return RowIndex == other.RowIndex && ColIndex == other.ColIndex;
        }
        // ----------------------------------------------------------------------
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        // ----------------------------------------------------------------------
        public override string ToString()
        {
            return string.Format( "{0}[{1,2},{2,2}]", GetType().Name, RowIndex, ColIndex);
        }
    }

    public class WellComparer : Comparer< Well>, IEqualityComparer< Well>
    {
        // ----------------------------------------------------------------------
        // constants.
        // ----------------------------------------------------------------------
        public static readonly WellComparer TheWellComparer = new WellComparer();

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        private WellComparer() {}

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public override int Compare( Well x, Well y)
        {
            if( x.IsAny() || y.IsAny()){
                throw new Exception( "can't compare Well.Any");
            }
            int row_index_comp = x.RowIndex.CompareTo( y.RowIndex);
            return ( row_index_comp != 0)
                ? row_index_comp
                : x.ColIndex.CompareTo( y.ColIndex);
        }
        // ----------------------------------------------------------------------
        #region IEqualityComparer< Well> Members
        // ----------------------------------------------------------------------
        public bool Equals( Well x, Well y)
        {
            if( x.IsAny() || y.IsAny()){
                throw new Exception( "can't compare Well.Any");
            }
            return x.RowIndex == y.RowIndex && x.ColIndex == y.ColIndex;
        }
        // ----------------------------------------------------------------------
        public int GetHashCode( Well obj)
        {
            return obj.GetHashCode();
        }
        // ----------------------------------------------------------------------
        #endregion
    }

    public static class WellMathUtil
    {
        // ----------------------------------------------------------------------
        // exceptions.
        // ----------------------------------------------------------------------
        public class WellsCloserThanChannelSpacingException : Exception
        {
        }

        // ----------------------------------------------------------------------
        // constants.
        // ----------------------------------------------------------------------
        private const double PPB = 0.000000001;
        private static readonly MathUtil.ApproximatelyEqualComparer PPB_COMPARER = new MathUtil.ApproximatelyEqualComparer( PPB);

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        #region CALCULATE_ANGLES_TO_PROPERLY_SPACE_WELLS_PLUS_HELPERS
        // ----------------------------------------------------------------------
        /// <summary>
        /// Calculate XY offset from well_a to well_b, where well_a and well_b are wells in the same plate with the given LabwareFormat.
        /// For the purpose of this function, the X axis runs along the columns with the positive direction heading towards greater column indices.
        /// For the purpose of this function, the Y axis runs along the rows with the positive drection heading towards lesser row indices.
        /// </summary>
        /// <param name="labware_format">The LabwareFormat of the plate of the subject wells.</param>
        /// <param name="well_a">One of the subject wells -- the initial point of the offset vector.</param>
        /// <param name="well_b">One of the subject wells -- the terminal point of the offset vector.</param>
        /// <returns>A tuple containing X and Y components of vector representing XY offset from well_a to well_b.</returns>
        private static Tuple< double, double> CalculateXYOffset( LabwareFormat labware_format, Well well_a, Well well_b)
        {
            double x_delta = (( double)( well_b.ColIndex - well_a.ColIndex)) * labware_format.ColToColSpacing;
            double y_delta = (( double)( well_a.RowIndex - well_b.RowIndex)) * labware_format.RowToRowSpacing;
            return Tuple.Create( x_delta, y_delta);
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Calculate length of given vector.
        /// </summary>
        /// <param name="vector">The subject vector.</param>
        /// <returns>The length of vector.</returns>
        private static double CalculateLength( Tuple< double, double> vector)
        {
            return Math.Sqrt(( vector.Item1 * vector.Item1) + ( vector.Item2 * vector.Item2));
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Calculate distance between well_a and well_b, where well_a and well_b are wells in the same plate with the given LabwareFormat.
        /// </summary>
        /// <param name="labware_format">The LabwareFormat of the plate of the subject wells.</param>
        /// <param name="well_a">One of the subject wells.</param>
        /// <param name="well_b">One of the subject wells.</param>
        /// <returns>The distance between well_a and well_b.</returns>
        public static double CalculateWellSpacing( LabwareFormat labware_format, Well well_a, Well well_b)
        {
            return CalculateLength( CalculateXYOffset( labware_format, well_a, well_b));
        }
        // ----------------------------------------------------------------------
        // (for a given XY offset -- helper; well_spacing passed in to prevent redundant calculations as it is usually calculated prior to calling this function anyway.)
        /// <summary>
        /// Calculate angles to properly space well_a from well_b, where well_a and well_b are wells in the same plate with the given LabwareFormat.
        /// Rotating the plate by the calculated angles would place the wells so that the Y displacement from well_a to well_b is -channel_spacing.
        /// Rotations by calculated angles result in negative (rather than positive) Y displacements so as to pair well_a with a channel of lower ordinality and pair well_b with the next higher ordinal channel.
        /// </summary>
        /// <param name="xy_offset">The XY offset from well_a to well_b.</param>
        /// <param name="well_spacing">The well-to-well spacing between well_a and well_b.</param>
        /// <param name="channel_spacing">The channel-to-channel Y distance.</param>
        /// <returns>A tuple containing two (possibly identical) angles by which if the subject plate were rotated would properly space well_a from well_b.</returns>
        private static Tuple< double, double> CalculateAnglesToProperlySpaceWellsHelper( Tuple< double, double> xy_offset, double well_spacing, double channel_spacing)
        {
            double beta = Math.Atan2( xy_offset.Item2, xy_offset.Item1);
            double omega = Math.Acos( channel_spacing / well_spacing);
            // note: angles are normalized then rounded to the nearest billionth radian.
            double angle_1 = Math.Round( MathUtil.NormalizeAngle(( -Math.PI / 2) - beta - omega), 9);
            double angle_2 = Math.Round( MathUtil.NormalizeAngle(( -Math.PI / 2) - beta + omega), 9);
            return Tuple.Create( angle_1, angle_2);
        }
        // ----------------------------------------------------------------------
        // (among all given XY offsets -- helper)
        /// <summary>
        /// Given an enumeration of wells in a plate with a given LabwareFormat,
        /// generate the enumeration of all angles that would "properly space" at least two wells in the well enumeration.
        /// In this context, properly space means to place wells so that the y distance between wells is channel_spacing.
        /// </summary>
        /// <param name="xy_offsets">An enumeration of all XY offsets among all the subject wells.</param>
        /// <param name="channel_spacing">The channel-to-channel Y distance.</param>
        /// <returns>An enumeration of all angles that would properly space at least two of the subject wells.</returns>
        private static IEnumerable< double> CalculateAnglesToProperlySpaceWellsHelper( IEnumerable< Tuple< double, double>> xy_offsets, double channel_spacing)
        {
            // declare and create SortedSet to contain all angles that would properly space well pairs.
            SortedSet< double> angles = new SortedSet< double>( PPB_COMPARER);

            // for each XY offset...
            foreach( Tuple< double, double> xy_offset in xy_offsets){
                // determine if channel spacing can hit well spacing...
                double well_spacing = CalculateLength( xy_offset);
                if( well_spacing < channel_spacing){
                    continue;
                }
                // if channel spacing can hit well spacing, then calculate angles to properly space the wells...
                Tuple< double, double> angle_pair = CalculateAnglesToProperlySpaceWellsHelper( xy_offset, well_spacing, channel_spacing);
                // add results to SortedSet containing all angles that would properly space well pairs.
                angles.Add( angle_pair.Item1);
                angles.Add( angle_pair.Item2);
            }

            // return all angles that would properly space well pairs.
            return angles;
        }
        // ----------------------------------------------------------------------
        // (between two given wells within a plate of labware_format)
        /// <summary>
        /// Calculate angles to properly space well_a from well_b, where well_a and well_b are wells in the same plate with the given LabwareFormat.
        /// Rotating the plate by the calculated angles would place the wells so that the Y displacement from well_a to well_b is -channel_spacing.
        /// Rotations by calculated angles result in negative (rather than positive) Y displacements so as to pair well_a with a channel of lower ordinality and pair well_b with the next higher ordinal channel.
        /// </summary>
        /// <param name="labware_format">The LabwareFormat of the plate of the subject wells.</param>
        /// <param name="well_a">One of the subject wells.</param>
        /// <param name="well_b">One of the subject wells.</param>
        /// <param name="channel_spacing">The channel-to-channel Y distance.</param>
        /// <returns>A tuple containing two (possibly identical) angles by which if the subject plate were rotated would properly space well_a from well_b.</returns>
        public static Tuple< double, double> CalculateAnglesToProperlySpaceWells( LabwareFormat labware_format, Well well_a, Well well_b, double channel_spacing)
        {
            Tuple< double, double> xy_offset = CalculateXYOffset( labware_format, well_a, well_b);
            double well_spacing = CalculateLength( xy_offset);
            if( well_spacing < channel_spacing){
                throw new WellsCloserThanChannelSpacingException();
            }
            return CalculateAnglesToProperlySpaceWellsHelper( xy_offset, well_spacing, channel_spacing);
        }
        // ----------------------------------------------------------------------
        // (among all given wells within a plate of labware_format)
        /// <summary>
        /// Given an enumeration of wells in a plate with a given LabwareFormat,
        /// generate the enumeration of all angles that would "properly space" at least two wells in the well enumeration.
        /// In this context, properly space means to place wells so that the Y distance between wells is channel_spacing.
        /// </summary>
        /// <param name="labware_format">The LabwareFormat of the plate of the subject wells.</param>
        /// <param name="wells">An enumeration of the subject wells.</param>
        /// <param name="channel_spacing">The channel-to-channel Y distance.</param>
        /// <returns>An enumeration of all angles that would properly space at least two of the subject wells.</returns>
        public static IEnumerable< double> CalculateAnglesToProperlySpaceWells( LabwareFormat labware_format, IEnumerable< Well> wells, double channel_spacing)
        {
            /* Parallel.ForEach does not help improve speed -- probably because of necessary locking.
            HashSet< Tuple< int, int>> offsets = new HashSet< Tuple< int, int>>();
            Parallel.ForEach( wells, well_a => {
            // foreach( Well well_a in wells){
                foreach( Well well_b in wells){
                    lock( offsets){
                        offsets.Add( Tuple.Create( well_a.RowIndex - well_b.RowIndex, well_b.ColIndex - well_a.ColIndex));
                    }
                }
            // }
            }
            );
            */
            // *** this query can probably be optimized to generate the well combinations more efficiently ***
            // the inner query cross joins all the subject wells with each other; from it, it generates a distinct list of row-column offsets.
            // the outer query transforms the row-column offsets into XY offset in tuple form.
            var xy_offsets = from rc_offset in( from well_a in wells from well_b in wells
                                                select new{ RowOffset = well_b.RowIndex - well_a.RowIndex, ColOffset = well_b.ColIndex - well_a.ColIndex}).Distinct()
                             select Tuple.Create( rc_offset.ColOffset * labware_format.ColToColSpacing, -rc_offset.RowOffset * labware_format.RowToRowSpacing);

            return CalculateAnglesToProperlySpaceWellsHelper( xy_offsets, channel_spacing);
        }
        // ----------------------------------------------------------------------
        // (all wells within a plate of labware_format)
        /// <summary>
        /// Given a plate of a given LabwareFormat,
        /// generate the enumeration of all angles that would "properly space" at least two wells in that plate.
        /// In this context, properly space means to place wells so that the Y distance between wells is channel_spacing.
        /// </summary>
        /// <param name="labware_format">The LabwareFormat of the subject plate.</param>
        /// <param name="channel_spacing">The channel-to-channel Y distance.</param>
        /// <returns>An enumeration of all angles that would properly space at least two wells in the subject plate.</returns>
        public static IEnumerable< double> CalculateAnglesToProperlySpaceWells( LabwareFormat labware_format, double channel_spacing)
        {
            // this query cross joins all possible row offsets with all possible column offsets and generates XY offsets (in tuple form) from them.
            var xy_offsets = from row_delta in Enumerable.Range( 1 - labware_format.NumRows, ( 2 * labware_format.NumRows) - 1)
                             from col_delta in Enumerable.Range( 1 - labware_format.NumCols, ( 2 * labware_format.NumCols) - 1)
                             select Tuple.Create( col_delta * labware_format.ColToColSpacing, row_delta * labware_format.RowToRowSpacing);

            return CalculateAnglesToProperlySpaceWellsHelper( xy_offsets, channel_spacing);
        }
        // ----------------------------------------------------------------------
        /* deprecated (slow)
        public static IEnumerable< double> CalculateAnglesToProperlySpaceWellsSlow( LabwareFormat2 labware_format, IEnumerable< Well> wells, double channel_spacing)
        {
            SortedSet< double> angles = new SortedSet< double>( PPB_COMPARER);
            foreach( Well well_a in wells){
                foreach( Well well_b in wells){
                    if( CalculateWellSpacing( labware_format, well_a, well_b) < channel_spacing){
                        continue;
                    }
                    Tuple< double, double> angle_pair = CalculateAnglesToProperlySpaceWells( labware_format, well_a, well_b, channel_spacing);
                    angles.Add( angle_pair.Item1);
                    angles.Add( angle_pair.Item2);
                }
            }
            return angles;
        }
        */
        // ----------------------------------------------------------------------
        #endregion
        // ----------------------------------------------------------------------
        // I think this is a horrible way to deal with the dest well usage, but I couldn't come up with
        // anything better to get around the circular dependency where WellUsageStates is defined in
        // DestinationPlate, but is needed by Utils, but DestinationPlate also uses Utils internally.
        // ----------------------------------------------------------------------
        public enum WellUsageStates { Available, Reserved, Used }
        // ----------------------------------------------------------------------
        public static IList< Well> ExtractWellNamesFromDestinationValue( string dest_well_names)
        {
            IList< Well> result = new List< Well>();
            // if we're here, look for comma-separated well names
            if( dest_well_names.Contains( ',')){
                // split the string up
                string[] wells = dest_well_names.Split( ',');
                for( int i = 0; i < wells.Length; ++i){
                    // for each of these strings, need to look for a ':', which indicates a range
                    string well_string = wells[ i].Trim();
                    // protect against stuff like "A1:C5,", or ",A1,B2"
                    if( well_string.Length == 0)
                        continue;
                    if( !well_string.Contains( ':'))
                        result.Add( new Well( well_string));
                    else
                        AddRange( result, well_string);
                }
            } else if( dest_well_names.Contains( ':')){
                AddRange( result, dest_well_names);
            } else {
                result.Add( new Well( dest_well_names.Trim()));
            }
            return result;
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// This figures out the well's X and Y coordinates in the plate frame, 
        /// given an angle of rotation about the center of the plate
        /// </summary>
        public static Tuple< double, double> GetXYAfterRotation( Tuple< double, double> initial_xy, double rotation, bool clockwise)
        {
            double cos_theta = Math.Cos( MathUtil.DegreesToRadians( Math.Abs(rotation)));
            double sin_theta = Math.Sin( MathUtil.DegreesToRadians( Math.Abs(rotation)));
            if( clockwise) {
                return Tuple.Create( cos_theta * initial_xy.Item1 + sin_theta * initial_xy.Item2,
                                    -sin_theta * initial_xy.Item1 + cos_theta * initial_xy.Item2);
            } else {
                return Tuple.Create( cos_theta * initial_xy.Item1 - sin_theta * initial_xy.Item2,
                                     sin_theta * initial_xy.Item1 + cos_theta * initial_xy.Item2);
            }
        }
        // ----------------------------------------------------------------------
        public static Tuple< double, double> GetA1DistanceFromCenterOfPlate( LabwareFormat labware_format)
        {
            const double offset_384 = 2.25;
            const double offset_1536_from_384 = 1.125;

            if( labware_format == LabwareFormat.LF_STANDARD_48){
                // for plate info, see Reed's post on CD: https://bionexsolutions.centraldesktop.com/p/aQAAAAAAqN4x 
                return Tuple.Create( -45.0, 32.2);
            }
            if( labware_format == LabwareFormat.LF_STANDARD_96){
                return Tuple.Create( -49.5, 31.5);
            }
            if( labware_format == LabwareFormat.LF_STANDARD_384){
                return Tuple.Create( -49.5 - offset_384, 31.5 + offset_384);
            }
            if( labware_format == LabwareFormat.LF_STANDARD_1536){
                return Tuple.Create( -49.5 - offset_384 - offset_1536_from_384, 31.5 + offset_384 + offset_1536_from_384);
            }
            throw new Exception( "unknown labware format");
        }
        // ----------------------------------------------------------------------
        private static void AddRange( ICollection< Well> wells, string well_range)
        {
            int num_colons = well_range.Count( c => c == ':');
            // check for more than one ':' -- not allowed
            if( num_colons != 1){
                throw new Exception( "invalid well range");
            }
            string[] well_names = well_range.Split( ':');
            Well start_well = new Well( well_names[ 0]);
            Well end_well = new Well( well_names[ 1]);
            // iterate from starting well to ending well.
            for( int row = start_well.RowIndex; row <= end_well.RowIndex; ++row){
                for( int col = start_well.ColIndex; col <= end_well.ColIndex; ++col){
                    wells.Add( new Well( row, col));
                }
            }
        }
        // ----------------------------------------------------------------------
        /* not used -- SAVE FOR FUTURE MULTI DISPATCHER.
        public class Hit2
        {
            // ------------------------------------------------------------------
            // properties.
            // ------------------------------------------------------------------
            public Plate SrcPlate { get; private set; }
            public Well SrcWell { get; private set; }

            // ------------------------------------------------------------------
            // constructors.
            // ------------------------------------------------------------------
            public Hit2( Plate src_plate, Well src_well)
            {
                SrcPlate = src_plate;
                SrcWell = src_well;
            }

            // ------------------------------------------------------------------
            // methods.
            // ------------------------------------------------------------------
            public override string ToString()
            {
                return string.Format( "{0}[{1},{2}]", GetType().Name, SrcPlate, SrcWell);
            }
        }
        // ----------------------------------------------------------------------
        // PART OF WELLMATHUTIL CLASS:
        public static IEnumerable< Tuple< double, int>> CalculateHitRatePotentialForAllAngles( LabwareFormat labware_format, double channel_spacing)
        {
            // get enumeration containing all the possible hits in a plate of labware_format.
            var wells = from hit in GenerateAllHitsInAPlate( labware_format)
                        select hit.SrcWell;

            // for each angle that properly spaces at least two wells within a plate of labware_format,
            // calculate post-rotation XY coordinates + extended results for every well in the plate.
            // call this a "rotation".
            // for each rotation, group results by Y-channel offset -->
            // by doing so, members of each group with different channel groups can be simultaneously addressed by the channels
            // (although members of the same group and the same channel group cannot be addressed simultaneously).
            // find the Y-channel-offset group with the greatest number of members
            // as this indicates that there could be up to "max" members in a Y-channel-offset group at the given rotation.
            // finally, generate list of all angles to properly space wells and their respective "hit-rate potential" ordered by hit-rate potential.
            return from angle in CalculateAnglesToProperlySpaceWells( labware_format, channel_spacing)
                   let max = ( from post_rotation_xy in labware_format.CalculatePostRotationXYCoordinatesExt( wells, angle, channel_spacing)
                               group post_rotation_xy by post_rotation_xy.PostRotationChannelYOffset into g
                               select Tuple.Create( g.Key, g.Count())).Max( x => x.Item2)
                   orderby max descending
                   select Tuple.Create( angle, max);
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="hits">in/out param, should all be from the same plate</param>
        /// <param name="channel_spacing"></param>
        public static void DestinationUnconstrained( IEnumerable<Hit2> hits, double channel_spacing)
        {
            DateTime start = DateTime.Now;

            // ensure that all hits from same plate.
            var plates = ( from hit in hits
                           group hit by hit.SrcPlate into g
                           select g.Key);

            double total_time = ( DateTime.Now - start).TotalSeconds;
            Console.WriteLine( "stage 0 took {0}", total_time);
            start = DateTime.Now;

            if( plates.Count() != 1){
                throw new Exception();
            }

            Plate plate = plates.First();

            SortedSet< double> angles = new SortedSet< double>( PPB_COMPARER);
            foreach( Hit2 h1 in hits){
                foreach( Hit2 h2 in hits){
                    try{
                        if( CalculateWellSpacing( plate.LabwareFormat, h1.SrcWell, h2.SrcWell) < channel_spacing){
                            continue;
                        }

                        Tuple< double, double> angle_pair = CalculateAnglesToProperlySpaceWells( plate.LabwareFormat, h1.SrcWell, h2.SrcWell, channel_spacing);
                        if( angle_pair != null){
                            angles.Add( angle_pair.Item1);
                            angles.Add( angle_pair.Item2);
                        }
                    } catch( WellsCloserThanChannelSpacingException){
                    }
                }
            }

            total_time = ( DateTime.Now - start).TotalSeconds;
            Console.WriteLine( "stage 1 took {0}", total_time);
            start = DateTime.Now;

            var wells = from hit in hits
                        select hit.SrcWell;

            foreach( double angle in angles){
                var ys = plate.LabwareFormat.CalculatePostRotationXYCoordinatesExt( wells, angle, channel_spacing);
                var ys3 = from whatever in ys
                          group whatever by whatever.PostRotationChannelYOffset into g
                          where g.Count() > 2
                          select Tuple.Create( g.Key, g.Count());
            }

            total_time = ( DateTime.Now - start).TotalSeconds;
            Console.WriteLine( "stage 2 took {0}", total_time);
            start = DateTime.Now;
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// <param name="transfers"> in/out param, should all be from the same plate</param>
        /// <param name="channel_spacing"></param>
        public static void DestinationConstrained( IEnumerable< Transfer> transfers, double channel_spacing)
        {
        }
        // ----------------------------------------------------------------------
        public static void TestG( out SortedSet< double> angles1, out SortedSet< double> angles2, LabwareFormat labware_format, double channel_spacing)
        {
            angles1 = new SortedSet< double>( PPB_COMPARER);
            angles2 = new SortedSet< double>( PPB_COMPARER);
            Well well_a1 = new Well( "a1");
            for( int row_index = 0; row_index < labware_format.NumRows; ++row_index){
                for( int col_index = 0; col_index < labware_format.NumCols; ++col_index){
                    try{
                        Tuple< double, double> angle_pair = CalculateAnglesToProperlySpaceWells( labware_format, well_a1, new Well( row_index, col_index), channel_spacing);
                        angles1.Add( angle_pair.Item1);
                        angles2.Add( angle_pair.Item2);
                    } catch( WellsCloserThanChannelSpacingException){
                    }
                }
            }
        }
        // ----------------------------------------------------------------------
        public static void TestG2( LabwareFormat labware_format, double channel_spacing)
        {
            foreach( int row_index_a in new int[]{ 0, labware_format.NumRows - 1}){
                foreach( int col_index_a in new int[]{ 0, labware_format.NumCols - 1}){
                    foreach( int row_index_b in new int[]{ 0, labware_format.NumRows - 1}){
                        foreach( int col_index_b in new int[]{ 0, labware_format.NumCols - 1}){
                            try{
                                Tuple< double, double> angle_pair = CalculateAnglesToProperlySpaceWells( labware_format, new Well( row_index_a, col_index_a), new Well( row_index_b, col_index_b), channel_spacing);
                                Console.WriteLine( "A({0,2},{1,2}) -> B({2,2},{3,2}): theta1 = {4,9:0.0000}, theta2 = {5,9:0.0000}", row_index_a, col_index_a, row_index_b, col_index_b, angle_pair.Item1, angle_pair.Item2);
                            } catch( WellsCloserThanChannelSpacingException){
                            }
                        }
                    }
                }
            }
        }
        // ----------------------------------------------------------------------
        #region GENERATE_AND_PRINT_ENUMERATIONS_OF_HITS_AND_TRANSFERS
        // ----------------------------------------------------------------------
        /// <summary>
        /// Generate all the possible hits from one source plate (created by this function).
        /// </summary>
        /// <param name="src_labware_format">The LabwareFormat of the source plate to be created by this function.</param>
        /// <returns>An enumeration containing all the possible hits of a source plate.</returns>
        public static IEnumerable< Hit2> GenerateAllHitsInAPlate( LabwareFormat src_labware_format)
        {
            // create source plate.
            Plate src_plate = new Plate( src_labware_format);

            // return an enumeration containing all the possible hits in the newly created source plate.
            return from row_index in Enumerable.Range( 0, src_labware_format.NumRows)
                   from col_index in Enumerable.Range( 0, src_labware_format.NumCols)
                   select new Hit2( src_plate, new Well( row_index, col_index));
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Generate a specified number of random hits from one source plate (created by this function).
        /// </summary>
        /// <param name="src_labware_format">The LabwareFormat of the source plate to be created by this function.</param>
        /// <param name="num_hits">The number of random hits to generate.</param>
        /// <returns>An enumeration of the randomly generated hits.</returns>
        public static IEnumerable< Hit2> GenerateRandomHits( LabwareFormat src_labware_format, int num_hits)
        {
            // validate input parameters.
            if( num_hits < 0){
                throw new Exception( "Number of random hits may not be less than zero.");
            }
            if( num_hits > src_labware_format.NumWells){
                throw new Exception( "Number of random hits to generate exceeds well count of source labware format.");
            }

            // initialize list of source wells not involved in a transfer to all wells in source plate.
            List< Well> src_wells_not_involved_in_hit = ( from row_index in Enumerable.Range( 0, src_labware_format.NumRows)
                                                           from col_index in Enumerable.Range( 0, src_labware_format.NumCols)
                                                           select new Well( row_index, col_index)).ToList();

            // create random generator and source plate.
            Random random_generator = new Random();
            Plate src_plate = new Plate( src_labware_format);

            // declare and create HashSet to contain randomly generated hits.
            HashSet< Hit2> hits = new HashSet< Hit2>();

            // do num_hits times:
            for( int loop = 0; loop < num_hits; ++loop){
                // generate a random index into the list of source wells not involved in a hit; remember the well; remove the well from the list.
                int src_well_index = random_generator.Next( src_wells_not_involved_in_hit.Count);
                Well src_well = src_wells_not_involved_in_hit[ src_well_index];
                src_wells_not_involved_in_hit.RemoveAt( src_well_index);
                // create a hit from source plate's source well and add hit to HashSet of randomly generated hits.
                hits.Add( new Hit2( src_plate, src_well));
            }

            // return randomly generated hits.
            return hits;
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Print each hit within an enumeration of hits to the console.
        /// </summary>
        /// <param name="hits">The enumeration of hits to print.</param>
        public static void PrintHits( IEnumerable< Hit2> hits)
        {
            foreach( Hit2 hit in hits){
                Console.WriteLine( hit);
            }
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Generate a specified number of random transfers from one source plate (created by this function) to one destination plate (created by this function).
        /// </summary>
        /// <param name="src_labware_format">The LabwareFormat of the source plate to be created by this function.</param>
        /// <param name="dst_labware_format">The LabwareFormat of the destination plate to be created by this function.</param>
        /// <param name="num_transfers">The number of random transfers to generate.</param>
        /// <returns>An enumeration of the randomly generated transfers.</returns>
        public static IEnumerable< Transfer> GenerateRandomTransfers( LabwareFormat src_labware_format, LabwareFormat dst_labware_format, int num_transfers)
        {
            // validate input parameters.
            if( num_transfers < 0){
                throw new Exception( "Number of random transfers may not be less than zero.");
            }
            if( num_transfers > src_labware_format.NumWells){
                throw new Exception( "Number of random transfers to generate exceeds well count of source labware format.");
            }
            if( num_transfers > dst_labware_format.NumWells){
                throw new Exception( "Number of random transfers to generate exceeds well count of destination labware format.");
            }

            // initialize list of source wells not involved in a transfer to all wells in source plate.
            List< Well> src_wells_not_involved_in_transfer = ( from row_index in Enumerable.Range( 0, src_labware_format.NumRows)
                                                                from col_index in Enumerable.Range( 0, src_labware_format.NumCols)
                                                                select new Well( row_index, col_index)).ToList();

            // initialize list of destination wells not involved in a transfer to all wells in destination plate.
            List< Well> dst_wells_not_involved_in_transfer = ( from row_index in Enumerable.Range( 0, dst_labware_format.NumRows)
                                                                from col_index in Enumerable.Range( 0, dst_labware_format.NumCols)
                                                                select new Well( row_index, col_index)).ToList();

            // create random generator, source plate, and destination plate.
            Random random_generator = new Random();
            SourcePlate src_plate = new SourcePlate( src_labware_format);
            DestinationPlate dst_plate = new DestinationPlate( dst_labware_format);

            // declare and create HashSet to contain randomly generated transfers.
            HashSet< Transfer> transfers = new HashSet< Transfer>();

            // do num_transfer times:
            for( int loop = 0; loop < num_transfers; ++loop){
                // generate a random index into the list of source wells not involved in a transfer; remember the well; remove the well from the list.
                int src_well_index = random_generator.Next( src_wells_not_involved_in_transfer.Count);
                Well src_well = src_wells_not_involved_in_transfer[ src_well_index];
                src_wells_not_involved_in_transfer.RemoveAt( src_well_index);
                // generate a random index into the list of destination wells not involved in a transfer; remember the well; remove the well from the list.
                int dst_well_index = random_generator.Next( dst_wells_not_involved_in_transfer.Count);
                Well dst_well = dst_wells_not_involved_in_transfer[ dst_well_index];
                dst_wells_not_involved_in_transfer.RemoveAt( dst_well_index);
                // create a transfer from source plate's source well to destination plate's destination well and add transfer to HashSet of randomly generated transfers.
                transfers.Add( new Transfer( src_plate, src_well, 0.0, VolumeUnits.ul, 0, VolumeUnits.ul, null, dst_plate, new List< Well>{ dst_well}, null, null));
            }

            // return randomly generated transfers.
            return transfers;
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// Print each transfer within an enumeration of transfers to the console.
        /// </summary>
        /// <param name="transfers">The enumeration of transfers to print.</param>
        public static void PrintTransfers( IEnumerable< Transfer> transfers)
        {
            foreach( Transfer transfer in transfers){
                Console.WriteLine( transfer);
            }
        }
        // ----------------------------------------------------------------------
        #endregion
        */
        // ----------------------------------------------------------------------
        /* not used -- CAN PROBABLY DELETE THIS OLD CODE.
        /// <summary>
        /// Figures out what angle the plate needs to be at if tip1 is in well1 and
        /// tip2 is in well2, and they are tip_spacing mm apart.
        /// </summary>
        /// <param name="labware_format"></param>
        /// <param name="well1">The wellname that tip1 is in</param>
        /// <param name="well2">The wellname that tip2 is in</param>
        /// <param name="channel1_id"></param>
        /// <param name="channel2_id"></param>
        /// <param name="channel_spacing">Channel spacing, in mm</param>
        /// <param name="angle">The angle the plate needs to be at</param>
        /// <returns>False if there is no possible solution</returns>
        private static bool GetAngleForTwoTips( LabwareFormat labware_format, Well well1, Well well2, byte channel1_id, byte channel2_id, double channel_spacing, out double angle)
        {
            Debug.Assert( !well1.IsAny());
            Debug.Assert( !well2.IsAny());
            // automatically disqualify any wells that are too close together
            // double distance_between_centers = GetDistanceBetweenWells( number_of_wells, well1, well2);
            angle = 0;

            // in case we change tip_spacing_tolerance to 0, we need to handle that case differently
            // than subtracting the tip spacing from the well spacing -- the tip spacing MUST be
            // less than the well spacing!
            // if( tip_spacing_tolerance == 0) {
            //     if( tip_spacing > distance_between_centers)
            //         return false;
            // } else {
            //     if( tip_spacing > distance_between_centers &&
            //         tip_spacing - distance_between_centers > tip_spacing_tolerance)
            //     {                    
            //         return false;
            //     }
            // }

            // need to figure out which way to rotate to get the desired Y spacing between wells
            // it really just depends on which quadrant well2 is in initially, relative to well1 as the center.
            // NOTE: this is basically going to give the same information as the call to
            // GetXYBetweenWellsAfterRotation(), but this allows us to figure out which quadrant
            // well2 is in, relative to well1.
            double x1, y1, x2, y2;
            GetWellDistanceFromCenterOfPlate( labware_format, well1, out x1, out y1);
            GetWellDistanceFromCenterOfPlate( labware_format, well2, out x2, out y2);
            // now we know which direction we need to rotate the plate, but we don't know how much to rotate
            // to get the desired change in Y distance between the two wells.
            // Ben solved this in Mathcad for me.  See Central Desktop for the MathCad and Octave files
            // link to Octave file: http://www.centraldesktop.com/p/aQAAAAAAPvfD
            // link to Mathcad file: 
            // there will be two solutions:
            double a = x2;
            double b = y2;
            double c = x1;
            double d = y1;
            // critical calculation is the tip spacing.  The tip spacing is either positive
            // or negative, depending on which tip goes into which well, and where that well
            // is relative to the other.  For example, if tip 2 -> A1 and tip 1 -> H12, then
            // the tip spacing should be negative.
            // see Octave script "rotations.m" at http://www.centraldesktop.com/p/aQAAAAAAPvfD
            double e;
            if( channel1_id < channel2_id)
                e = -channel_spacing;
            else
                e = channel_spacing;
            // solution 1 is 2 * atan( upper1 / lower1)
            double upper1_sqrt_term = a*a - 2*a*c + b*b - 2*b*d + c*c + d*d - e*e;
            if( upper1_sqrt_term < 0 && upper1_sqrt_term > -0.00001)
                upper1_sqrt_term = 0;
            double upper1 = a - c + Math.Sqrt( upper1_sqrt_term);
            double lower1 = d - b - e;
            double solution1 = MathUtil.RadiansToDegrees( 2 * Math.Atan(upper1 / lower1));
            // solution 2 is -2 * atan( upper2 / lower2)
            double upper2_sqrt_term = a*a - 2*a*c + b*b - 2*b*d + c*c + d*d - e*e;
            if( upper2_sqrt_term < 0 && upper2_sqrt_term > -0.00001)
                upper2_sqrt_term = 0;
            double upper2 = c - a + Math.Sqrt( upper2_sqrt_term);
            double lower2 = d - b - e;
            double solution2 = MathUtil.RadiansToDegrees( -2 * Math.Atan(upper2 / lower2));
            // there are two solutions, but either one is fine for us
            // NOTE: this is really important!  In Octave, I solved the problem by always
            //       assuming that we want to rotate CW.  So because of this, a negative
            //       answer is actually CCW, and positive is CW.  Therefore, here we
            //       need to reverse the sign of the answer.
            if (Double.IsNaN(solution1) && Double.IsNaN(solution2))
                return false;
            if( Double.IsNaN(solution1))
                angle = -solution2;
            else
                angle = -solution1;
            double old_answer = angle;

            // Felix's solution -- I ran this code on 2010-10-14 and the tip pressing was still off-center
            // double wells_to_deck_angle = Wells.RadiansToDegrees( Math.Acos( channel_spacing / distance_between_centers));
            // int row1, col1, row2, col2;
            // Wells.WellNameToRowColumn( well1, out row1, out col1);
            // Wells.WellNameToRowColumn( well2, out row2, out col2);
            // double row_delta = row2 - row1;
            // double col_delta = col2 - col1;
            // double wells_to_plate_angle = ( col_delta == 0) ? 90 : ( Wells.RadiansToDegrees( Math.Atan( row_delta / col_delta)));
            // angle = ( 90 - wells_to_deck_angle) + wells_to_plate_angle;
            // angle = angle - 180;

            return true;
        }
        // ----------------------------------------------------------------------
        /// <summary>
        /// This function assumes that the plate is currently at 0 degrees rotation
        /// </summary>
        /// <param name="labware_format"></param>
        /// <param name="well"></param>
        /// <param name="x_from_center"></param>
        /// <param name="y_from_center"></param>
        private static void GetWellDistanceFromCenterOfPlate( LabwareFormat labware_format, Well well, out double x_from_center, out double y_from_center)
        {
            double a1_x_from_center, a1_y_from_center;
            GetA1DistanceFromCenterOfPlate( labware_format, out a1_x_from_center, out a1_y_from_center);
            // now that we have the distance from A1 to the center of the plate, we can take the well name
            // and figure out how far it is from the center by adding its row and column distances from A1
            x_from_center = a1_x_from_center + well.ColIndex * labware_format.ColToColSpacing;
            y_from_center = a1_y_from_center - well.RowIndex * labware_format.RowToRowSpacing;
        }
        */
    }

    /* SAVE FOR FUTURE MULTI DISPATCHER (TESTING CODE).
    public static class NWWellTest
    {
        static void PrintResult( SortedSet< double> angles)
        {
            Console.WriteLine( "Count={0}", angles.Count);
            double previous_a = double.NaN;
            foreach( double a in angles){
                Console.WriteLine( "angles has {0}", a * 180 / Math.PI);
                if( MathUtil.ApproximatelyEqual( a, previous_a, 0.000000001)){
                    Console.WriteLine( "found approximately equal {0} and {1}", previous_a, a);
                }
                previous_a = a;
            }
        }

        delegate void TimedDelegate();

        static void TimeDelegate( TimedDelegate timed_delegate, string comment)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            timed_delegate();
            stopwatch.Stop();
            Console.WriteLine( "{0} took {1}ms", comment, stopwatch.ElapsedMilliseconds);
        }

        public static void Test()
        {
            for( int loop = 0; loop < 96; loop++){
                Well w = new Well( LabwareFormat2.LF_STANDARD_96, loop);
                int i = w.GetWellIndex( LabwareFormat2.LF_STANDARD_96);
                Console.WriteLine( "{0}: {1} -> {2}", loop, i, w);
            }

            const double channel_spacing = 18.0;
            // WellMathUtil.CalculateAnglesToProperlySpaceWells( LabwareFormat2.LF_STANDARD_96, channel_spacing);
            // WellMathUtil.CalculateAnglesToProperlySpaceWells( LabwareFormat2.LF_STANDARD_384, channel_spacing);
            // WellMathUtil.CalculateAnglesToProperlySpaceWells( LabwareFormat2.LF_STANDARD_1536, channel_spacing);

            IEnumerable< Hit2> hs = WellMathUtil.GenerateAllHitsInAPlate( LabwareFormat2.LF_STANDARD_1536);
            var ws = ( from h in hs
                     select h.SrcWell).ToList();

            TimeDelegate( () => WellMathUtil.CalculateAnglesToProperlySpaceWells( LabwareFormat2.LF_STANDARD_1536, ws, channel_spacing), "passing wells");
            TimeDelegate( () => WellMathUtil.CalculateAnglesToProperlySpaceWells( LabwareFormat2.LF_STANDARD_1536, ws, channel_spacing), "passing wells");
            TimeDelegate( () => WellMathUtil.CalculateAnglesToProperlySpaceWells( LabwareFormat2.LF_STANDARD_1536, ws, channel_spacing), "passing wells");
            TimeDelegate( () => WellMathUtil.CalculateAnglesToProperlySpaceWells( LabwareFormat2.LF_STANDARD_1536, ws, channel_spacing), "passing wells");
            TimeDelegate( () => WellMathUtil.CalculateAnglesToProperlySpaceWells( LabwareFormat2.LF_STANDARD_1536, ws, channel_spacing), "passing wells");
            TimeDelegate( () => WellMathUtil.CalculateAnglesToProperlySpaceWells( LabwareFormat2.LF_STANDARD_1536, channel_spacing), "not passing wells");
            TimeDelegate( () => WellMathUtil.CalculateAnglesToProperlySpaceWells( LabwareFormat2.LF_STANDARD_1536, channel_spacing), "not passing wells");
            TimeDelegate( () => WellMathUtil.CalculateAnglesToProperlySpaceWells( LabwareFormat2.LF_STANDARD_1536, channel_spacing), "not passing wells");
            TimeDelegate( () => WellMathUtil.CalculateAnglesToProperlySpaceWells( LabwareFormat2.LF_STANDARD_1536, channel_spacing), "not passing wells");
            TimeDelegate( () => WellMathUtil.CalculateAnglesToProperlySpaceWells( LabwareFormat2.LF_STANDARD_1536, channel_spacing), "not passing wells");

            var result1 = WellMathUtil.CalculateAnglesToProperlySpaceWells( LabwareFormat2.LF_STANDARD_1536, ws, channel_spacing);
            var result3 = WellMathUtil.CalculateAnglesToProperlySpaceWells( LabwareFormat2.LF_STANDARD_1536, channel_spacing);
            bool same_results13 = result1.SequenceEqual( result3);

            IEnumerable< Tuple< double, int>> special_angles_96;
            IEnumerable< Tuple< double, int>> special_angles_384;
            IEnumerable< Tuple< double, int>> special_angles_1536;

            TimeDelegate( () => special_angles_96 = WellMathUtil.CalculateHitRatePotentialForAllAngles( LabwareFormat2.LF_STANDARD_96, channel_spacing), "calculate special angles 96");
            TimeDelegate( () => special_angles_384 = WellMathUtil.CalculateHitRatePotentialForAllAngles( LabwareFormat2.LF_STANDARD_384, channel_spacing), "calculate special angles 384");
            TimeDelegate( () => special_angles_1536 = WellMathUtil.CalculateHitRatePotentialForAllAngles( LabwareFormat2.LF_STANDARD_1536, channel_spacing), "calculate special angles 1536");

            Well well_a = new Well( 0, 0);
            Well well_b = new Well( 5, 10);
            Tuple< double, double> angles_5_10 = WellMathUtil.CalculateAnglesToProperlySpaceWells( LabwareFormat2.LF_STANDARD_96, well_a, well_b, channel_spacing);
            LabwareFormat2.LF_STANDARD_96.CalculatePostRotationXYCoordinates( well_a, angles_5_10.Item1);
            LabwareFormat2.LF_STANDARD_96.CalculatePostRotationXYCoordinates( well_a, angles_5_10.Item2);
            LabwareFormat2.LF_STANDARD_96.CalculatePostRotationXYCoordinates( well_b, angles_5_10.Item1);
            LabwareFormat2.LF_STANDARD_96.CalculatePostRotationXYCoordinates( well_b, angles_5_10.Item2);

            // TimeDelegate( () => WellMathUtil.TestG2_2( LabwareFormat2.LF_STANDARD_96), "generate all angles on 96-well plate");
            // TimeDelegate( () => WellMathUtil.TestG2_2( LabwareFormat2.LF_STANDARD_384), "generate all angles on 384-well plate");
            // TimeDelegate( () => WellMathUtil.TestG2_2( LabwareFormat2.LF_STANDARD_1536), "generate all angles on 1536-well plate");

            Console.WriteLine( "generate -1 transfers in 384->384 transfer");
            try{
                WellMathUtil.PrintTransfers( WellMathUtil.GenerateRandomTransfers( LabwareFormat2.LF_STANDARD_384, LabwareFormat2.LF_STANDARD_384, -1));
            } catch( Exception){
                Console.WriteLine( "successfully catching exception");
            }
            Console.WriteLine( "generate 0 transfers in 384->384 transfer");
            WellMathUtil.PrintTransfers( WellMathUtil.GenerateRandomTransfers( LabwareFormat2.LF_STANDARD_384, LabwareFormat2.LF_STANDARD_384, 0));
            Console.WriteLine( "generate 16 transfers in 384->384 transfer");
            WellMathUtil.PrintTransfers( WellMathUtil.GenerateRandomTransfers( LabwareFormat2.LF_STANDARD_384, LabwareFormat2.LF_STANDARD_384, 16));
            Console.WriteLine( "generate 384 transfers in 384->384 transfer");
            WellMathUtil.PrintTransfers( WellMathUtil.GenerateRandomTransfers( LabwareFormat2.LF_STANDARD_384, LabwareFormat2.LF_STANDARD_384, 384));
            Console.WriteLine( "generate 385 transfers in 384->384 transfer");
            try{
                WellMathUtil.PrintTransfers( WellMathUtil.GenerateRandomTransfers( LabwareFormat2.LF_STANDARD_384, LabwareFormat2.LF_STANDARD_384, 385));
            } catch( Exception){
                Console.WriteLine( "successfully catching exception");
            }

            Console.WriteLine( "generate 32 hits in 96-well plate");
            WellMathUtil.PrintHits( WellMathUtil.GenerateRandomHits( LabwareFormat2.LF_STANDARD_96, 32));

            HashSet< Hit2> hits1 = new HashSet< Hit2>();
            Plate2 the_plate = new Plate2( new Labware2( LabwareFormat2.LF_STANDARD_96));
            hits1.Add( new Hit2( the_plate, new Well( 2, 7)));
            hits1.Add( new Hit2( the_plate, new Well( 5, 3)));
            hits1.Add( new Hit2( the_plate, new Well( 1, 4)));
            hits1.Add( new Hit2( the_plate, new Well( 6, 7)));
            hits1.Add( new Hit2( the_plate, new Well( 4, 3)));
            hits1.Add( new Hit2( the_plate, new Well( 2, 2)));
            hits1.Add( new Hit2( the_plate, new Well( 4, 9)));
            hits1.Add( new Hit2( the_plate, new Well( 0, 1)));
            hits1.Add( new Hit2( the_plate, new Well( 1,11)));
            hits1.Add( new Hit2( the_plate, new Well( 1, 7)));
            hits1.Add( new Hit2( the_plate, new Well( 4, 8)));
            hits1.Add( new Hit2( the_plate, new Well( 0,10)));
            hits1.Add( new Hit2( the_plate, new Well( 3, 7)));
            hits1.Add( new Hit2( the_plate, new Well( 2, 0)));
            hits1.Add( new Hit2( the_plate, new Well( 3, 0)));
            hits1.Add( new Hit2( the_plate, new Well( 3, 6)));

            Console.WriteLine( "16 hits to test with");
            WellMathUtil.PrintHits( hits1);
            WellMathUtil.DestinationUnconstrained( hits1, channel_spacing);

            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits1, channel_spacing), "Run 16 hits to test with");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits1, channel_spacing), "Run 16 hits to test with");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits1, channel_spacing), "Run 16 hits to test with");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits1, channel_spacing), "Run 16 hits to test with");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits1, channel_spacing), "Run 16 hits to test with");

            IEnumerable< Hit2> hits;

            hits = WellMathUtil.GenerateRandomHits( LabwareFormat2.LF_STANDARD_96, 16);
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 16 hits, 96-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 16 hits, 96-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 16 hits, 96-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 16 hits, 96-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 16 hits, 96-well plate");

            hits = WellMathUtil.GenerateRandomHits( LabwareFormat2.LF_STANDARD_96, 32);
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 32 hits, 96-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 32 hits, 96-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 32 hits, 96-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 32 hits, 96-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 32 hits, 96-well plate");

            hits = WellMathUtil.GenerateRandomHits( LabwareFormat2.LF_STANDARD_96, 64);
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 64 hits, 96-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 64 hits, 96-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 64 hits, 96-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 64 hits, 96-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 64 hits, 96-well plate");

            hits = WellMathUtil.GenerateRandomHits( LabwareFormat2.LF_STANDARD_96, 96);
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 96 hits, 96-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 96 hits, 96-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 96 hits, 96-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 96 hits, 96-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 96 hits, 96-well plate");

            hits = WellMathUtil.GenerateRandomHits( LabwareFormat2.LF_STANDARD_384, 384);
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 384 hits, 384-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 384 hits, 384-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 384 hits, 384-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 384 hits, 384-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 384 hits, 384-well plate");

            hits = WellMathUtil.GenerateRandomHits( LabwareFormat2.LF_STANDARD_1536, 1536);
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 1536 hits, 1536-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 1536 hits, 1536-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 1536 hits, 1536-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 1536 hits, 1536-well plate");
            TimeDelegate( () => WellMathUtil.DestinationUnconstrained( hits, channel_spacing), "Run DestinationUnconstrained 1536 hits, 1536-well plate");

            // Dictionary< HashSet< Well>, HashSet< double>> cache_96 = new Dictionary< HashSet< Well>, HashSet< double>>();
            // Dictionary< HashSet< Well>, HashSet< double>> cache_384 = new Dictionary< HashSet< Well>, HashSet< double>>();
            // Dictionary< HashSet< Well>, HashSet< double>> cache_1536 = new Dictionary< HashSet< Well>, HashSet< double>>();

            // TimeDelegate( () => WellMathUtil.TestG3( out cache_96, LabwareFormat2.LF_STANDARD_96, "Calculate all angles for all well pairs in 1536-well plate");
            // TimeDelegate( () => WellMathUtil.TestG3( out cache_384, LabwareFormat2.LF_STANDARD_384), "Calculate all angles for all well pairs in 1536-well plate");
            // TimeDelegate( () => WellMathUtil.TestG3( out cache_1536, LabwareFormat2.LF_STANDARD_1536), "Calculate all angles for all well pairs in 1536-well plate");

            TimeDelegate( () => WellMathUtil.TestG2( LabwareFormat2.LF_STANDARD_96, channel_spacing), "Calculate angle extremes 96-well plate");
            TimeDelegate( () => WellMathUtil.TestG2( LabwareFormat2.LF_STANDARD_384, channel_spacing), "Calculate angle extremes 384-well plate");
            TimeDelegate( () => WellMathUtil.TestG2( LabwareFormat2.LF_STANDARD_1536, channel_spacing), "Calculate angle extremes 1536-well plate");

            SortedSet< double> angles1 = new SortedSet< double>();
            SortedSet< double> angles2 = new SortedSet< double>();
            SortedSet< double> angles3 = new SortedSet< double>();
            SortedSet< double> angles4 = new SortedSet< double>();
            SortedSet< double> angles5 = new SortedSet< double>();
            SortedSet< double> angles6 = new SortedSet< double>();
            SortedSet< double> angles7 = new SortedSet< double>();
            SortedSet< double> angles8 = new SortedSet< double>();

            TimeDelegate( () => WellMathUtil.TestG( out angles1, out angles2, LabwareFormat2.LF_STANDARD_96, channel_spacing), "Calculate all angles from origin 96-well plate");
            TimeDelegate( () => WellMathUtil.TestG( out angles3, out angles4, LabwareFormat2.LF_STANDARD_384, channel_spacing), "Calculate all angles from origin 384-well plate");
            TimeDelegate( () => WellMathUtil.TestG( out angles5, out angles6, LabwareFormat2.LF_STANDARD_1536, channel_spacing), "Calculate all angles from origin 1536-well plate");
            TimeDelegate( () => WellMathUtil.TestG( out angles7, out angles8, new LabwareFormat2( 320, 480, 2.25, 2.25), channel_spacing), "Calculate all angles from origin \"153600\"-well plate");

            return;
            // Console.WriteLine( "----- angles1 -----");
            // PrintResult( angles1);
            // Console.WriteLine( "----- angles2 -----");
            // PrintResult( angles2);

            // Console.WriteLine( "----- angles3 -----");
            // PrintResult( angles3);
            // Console.WriteLine( "----- angles4 -----");
            // PrintResult( angles4);

            // Console.WriteLine( "----- angles5 -----");
            // PrintResult( angles5);
            // Console.WriteLine( "----- angles6 -----");
            // PrintResult( angles6);

            // Console.WriteLine( "----- angles7 -----");
            // PrintResult( angles7);
            // Console.WriteLine( "----- angles8 -----");
            // PrintResult( angles8);

            // bool b1 = angles1.IsSubsetOf( angles3);
            // bool b2 = angles2.IsSubsetOf( angles4);
            // bool b3 = angles3.IsSubsetOf( angles5);
            // bool b4 = angles4.IsSubsetOf( angles6);
        }
    }
    */
#endif
}
