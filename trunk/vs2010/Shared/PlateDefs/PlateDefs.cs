using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Utils;
using BioNex.Shared.Utils.WellMathUtil;

namespace BioNex.Shared.PlateDefs
{
    //-------------------------------------------------------------------------
    //-------------------------------------------------------------------------
    // Plate
    //-------------------------------------------------------------------------
    //-------------------------------------------------------------------------
    public class Plate
    {
        public ILabware Labware { get; private set; }
        // DKM 2011-10-05 changed to MutableString so that when a barcoded plate is downstacked, but its
        //                barcode isn't known at the time of hitpick data generation, we can change
        //                it after the robot picks the plate up.
        public MutableString Barcode { get; private set; }

        public LabwareFormat LabwareFormat { get; private set; }
        public string LabwareName { get { return Labware.Name; }}
        public int NumberOfWells { get { return Labware[ LabwarePropertyNames.NumberOfWells].ToInt(); }}
        /// <summary>
        /// Whether or not this plate has a lid at runtime.  So if the robot delids this plate, this
        /// property should get set to false.  Likewise, when the lid gets replaced, this property
        /// should get set to true.
        /// </summary>
        public bool CurrentlyLidded { get; set; }
        
        public Plate( ILabware labware, string barcode)
        {
            Labware = labware;
            Barcode = new MutableString(barcode);
            switch( NumberOfWells){
                case 48: 
                    LabwareFormat = LabwareFormat.LF_STANDARD_48;
                    break;
                case 96:
                    LabwareFormat = LabwareFormat.LF_STANDARD_96;
                    break;
                case 384:
                    LabwareFormat = LabwareFormat.LF_STANDARD_384;
                    break;
                case 1536:
                    LabwareFormat = LabwareFormat.LF_STANDARD_1536;
                    break;
                default:
                    throw new Exception( "unexpected number of wells");
            }
            // unused:
            // NumberOfRows = (int)Math.Sqrt( NumberOfWells / 1.5);
            // NumberOfColumns = (int)(NumberOfRows * 1.5);
            // Wells = ( from row_index in Enumerable.Range( 0, NumberOfRows)
            //           from col_index in Enumerable.Range( 0, NumberOfColumns)
            //           select new Well( row_index, col_index)).ToList();
            // PreHitpickTasks = prehitpick_tasks;
            // PostHitpickTasks = posthitpick_tasks;
            // Variables = variables;
        }

        public Plate( LabwareFormat labware_format)
        {
            LabwareFormat = labware_format;
        }

        public override string ToString()
        {
            return string.Format( "{0}[{1},{2}]", GetType().Name, LabwareName, Barcode.Value);
        }

        /* unused:
        // public int NumberOfRows { get; protected set; }
        // public int NumberOfColumns { get; protected set; }
        // public List< Well> Wells { get; protected set; }
        // public IList<TaskListXMLParser.PlateTask> PreHitpickTasks { get; private set; }
        // public IList<TaskListXMLParser.PlateTask> PostHitpickTasks { get; private set; }
        /// <summary>
        /// string #1: variable name
        /// string #2: variable value
        /// </summary>
        // public IList<KeyValuePair<string,string>> Variables { get; private set; }
        public Well this[ string well_name]
        {
            get {
                int row = -1;
                int column = -1;
                Wells.WellNameToRowColumn( well_name, out row, out column);
                int well_index = row * NumberOfColumns + column;
                return Wells[well_index];
            }

            set {
                int row = -1;
                int column = -1;
                Wells.WellNameToRowColumn( well_name, out row, out column);
                int well_index = row * NumberOfColumns + column;
                Debug.WriteLine( String.Format( "well_name: {0}, well_index: {1}", well_name, well_index));
                Wells[well_index] = value;
            }
        }
        public void SetLabwareXML( string xml)
        {
        }
        public override string ToString()
        {
            return String.Format( "Barcode: {0}", Barcode);
        }
        */
    }
    //-------------------------------------------------------------------------
    //-------------------------------------------------------------------------
    // SourcePlate
    //-------------------------------------------------------------------------
    //-------------------------------------------------------------------------
    public class SourcePlate : Plate
    {
        /// <summary>
        /// Added this constructor for unit tests, which won't have access to a real labware database
        /// </summary>
        /// <param name="labware"></param>
        /// <param name="barcode"></param>
        public SourcePlate( ILabware labware, string barcode)
            : base( labware, barcode)
        {
            if( labware.LidId != 0)
                CurrentlyLidded = true;
        }

        public SourcePlate( LabwareFormat labware_format)
            : base( labware_format)
        {
        }
    }
    //-------------------------------------------------------------------------
    //-------------------------------------------------------------------------
    // DestinationPlate
    //-------------------------------------------------------------------------
    //-------------------------------------------------------------------------
    public class DestinationPlate : Plate
    {
        // in order to support the notion of "any" dest well in the hitpick file,
        // we need to keep track of which wells have already been used by previous
        // transfers.
        //! \todo run this to make sure that when a plate comes back into the 
        //        BB multiple times, it has the same plate reference, i.e. the
        //        wells that were used already have been preserved
        //public List< Wells.WellUsageStates> WellUsageMap { get; private set; }
        // I moved to a dictionary instead of a list because it was a PITA to get
        // the well index value when I don't have the labware database handy
        public Dictionary< Well, WellMathUtil.WellUsageStates> WellUsageMap { get; private set; }

        //List<string> _usable_wells; //! \todo use List<Well> instead?
        // I am storing the string instead now for serialization reasons.  If
        // we want the List<string> itself, then it will get derived in the accessor
        readonly string _usable_wells;

        public DestinationPlate( ILabware labware, string barcode, string usable_wells_range)
            : base( labware, barcode)
        {
            // initialize the well usage List, so we need to get labware info
            WellUsageMap = new Dictionary< Well, WellMathUtil.WellUsageStates>();

            // check for valid well first!
            IList< Well> wells = WellMathUtil.ExtractWellNamesFromDestinationValue( usable_wells_range);
            foreach( Well well in wells){
                if( !well.FitsFormat( LabwareFormat))
                    throw new Well.InvalidWellNameException( well.WellName);
            }
            _usable_wells = usable_wells_range; //Wells.ExtractWellNamesFromDestinationValue( usable_wells_range);
            if( labware.LidId != 0)
                CurrentlyLidded = true;
        }

        public DestinationPlate( LabwareFormat labware_format)
            : base( labware_format)
        {
        }

        #region Usable Wells
        /// <summary>
        /// Although easily-confused with the well usage map, the UsableWells are pre-defined
        /// regions of usable wells, as opposed to wells that are dispensed to and thus
        /// have runtime state changes
        /// </summary>
        public IList< Well> UsableWells
        {
            get { return WellMathUtil.ExtractWellNamesFromDestinationValue( _usable_wells); }
        }

        public string UsableWellsString
        {
            get { return _usable_wells; }
        }

        public bool AreWellsUsable( Well well)
        {
            //! \todo handle the "any" case.  For now, let's just allow it, but we have to figure out
            //!       what to do with it later on.
            if( well.IsAny() || _usable_wells.ToUpper() == Well.ANY_WELL_NAME.ToUpper())
                return true;
            return UsableWells.Contains( well, WellComparer.TheWellComparer);
        }

        public bool AreWellsUsable( IList< Well> wells)
        {
            foreach( Well well in wells){
                if( well.IsAny())
                    continue;
                if( !AreWellsUsable( well))
                    return false;
            }
            return true;
        }
        #endregion Usable Wells

        public WellMathUtil.WellUsageStates GetWellUsageState( Well well)
        {
            // assume the well is available if we didn't specifically add a reservation or usage for it upon
            // initialization or parsing of the hitpick XML file
            if( !WellUsageMap.ContainsKey( well))
                return WellMathUtil.WellUsageStates.Available;
            else
                return WellUsageMap[ well];
        }

        public void SetWellUsageState( Well well, WellMathUtil.WellUsageStates state)
        {
            if( !WellUsageMap.ContainsKey( well))
                WellUsageMap.Add( well, state);
            else
                WellUsageMap[ well] = state;
        }

        public Well GetFirstAvailableWell()
        {
            Well available_well = null;
            try {
                available_well = WellUsageMap.First( kvp => kvp.Value == WellMathUtil.WellUsageStates.Available).Key;
            } catch( InvalidOperationException) {
                // do nothing, just need to make sure we can catch the case where nothing is available
            } 
            if( available_well == null) {
                for( int i=0; i<NumberOfWells; i++) {
                    Well temp_wellname = new Well( LabwareFormat, i);
                    if( !WellUsageMap.ContainsKey( temp_wellname))
                        return temp_wellname;
                }
                Debug.Fail( "Couldn't find any available wells in this dest plate -- check the hitpick file for errors!!!");
                return null;
            } else
                return available_well;
        }
    }
    //-------------------------------------------------------------------------
    //-------------------------------------------------------------------------
    // PlateCatalog
    //-------------------------------------------------------------------------
    //-------------------------------------------------------------------------
    //! used to keep track of all source and destination plates that
    //! have been read in from the hitpick XML file.  allows easy
    //! retrieval of plate information via indexers
    public class PlateCatalog
    {
        private readonly Dictionary<string, Plate> _plates = new Dictionary<string, Plate>();

        public Plate this[string barcode]
        {
            get {
                if( !_plates.ContainsKey( barcode))
                    throw new PlateDoesNotExistException( String.Format( "Plate with barcode {0} does not exist", barcode));
                else
                    return _plates[barcode];
            }
        }

        /// <summary>
        /// Adds a plate barcode to inventory.  If the plate already exists, ignore the add request.
        /// </summary>
        /// <param name="plate"></param>
        public void Add( Plate plate)
        {
            if( _plates.ContainsKey( plate.Barcode))
                // don't throw an exception anymore, just ignore the add
                //throw new PlateExistsException( String.Format( "Plate with barcode {0} already exists", plate.Barcode));
                return;
            _plates.Add( plate.Barcode, plate);
        }

        public int Count
        {
            get { return _plates.Count; }
        }

        public Dictionary<string,Plate>.Enumerator GetEnumerator()
        {
            return _plates.GetEnumerator();
        }

        public class PlateExistsException : ApplicationException
        {
            public PlateExistsException( string message)
                : base( message)
            {
            }
        }

        public class PlateDoesNotExistException : ApplicationException
        {
            public PlateDoesNotExistException( string message)
                : base( message)
            {
            }
        }
    }
}
