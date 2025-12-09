using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using BioNex.Shared.HitpickXMLReader;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.Utils;
using BioNex.Shared.Utils.WellMathUtil;

namespace BioNex.CustomerGUIPlugins
{
    public class MonsantoHitpick
    {
        public bool DestinationSpecified { get; private set; }
        public string SrcWellName { get; private set; }
        public int DstPlateNumber { get; private set; }
        public string DstWellName { get; private set; }
        public MonsantoHitpick( string src_well_name)
        {
            DestinationSpecified = false;
            SrcWellName = src_well_name;
            DstPlateNumber = 0;
            DstWellName = "";
        }
        public MonsantoHitpick( string src_well_name, int dst_plate_number, string dst_well_name)
        {
            DestinationSpecified = true;
            SrcWellName = src_well_name;
            DstPlateNumber = dst_plate_number;
            DstWellName = dst_well_name;
        }
    }

    public class MonsantoSource
    {
        public string Barcode { get; private set; }
        public string SrcLabwareCode { get; private set; }
        public IList< MonsantoHitpick> Hitpicks { get; private set; }
        public MonsantoSource( string barcode, string src_labware_code)
        {
            Barcode = barcode;
            SrcLabwareCode = src_labware_code;
            Hitpicks = new List< MonsantoHitpick>();
        }
    }

    public class MonsantoProject
    {
        public string Name { get; private set; }
        public string Method { get; private set; }
        public int Volume { get; private set; }
        public string DstLabwareCode { get; private set; }
        public IList< MonsantoSource> Sources { get; private set; }
        public MonsantoProject( string name, string method, int volume, string dst_labware_code)
        {
            Name = name;
            Method = method;
            Volume = volume;
            DstLabwareCode = dst_labware_code;
            Sources = new List< MonsantoSource>();
        }
        class SourceHitpick
        {
            public MonsantoSource Source;
            public MonsantoHitpick Hitpick;
            public Transfer TransferForHitpick( DestinationPlate dst_plate)
            {
                return null;
            }
        }
        public TransferOverview ToTransferOverview( ILabwareDatabase labware_database)
        {
            int NUM_WELLS_PER_DESTINATION_PLATE = 96;
            double ASPIRATE_DISTANCE_FROM_BOTTOM = 0;
            double DISPENSE_DISTANCE_FROM_BOTTOM = 0;

            // ------------------------------------------------------------------
            // pre-calculate some basic enumerations:
            // enumeration of all hitpicks coupled with their source information.
            var all_hitpicks = Sources.SelectMany( source => ( source.Hitpicks.Select( x => new SourceHitpick(){ Source = source, Hitpick = x})));
            // enumeration of all controls coupled with their source information.
            var all_control_hitpicks = all_hitpicks.Where( x => x.Hitpick.DestinationSpecified);
            // enumeration of all samples coupled with their source information.
            var all_sample_hitpicks = all_hitpicks.Where( x => !x.Hitpick.DestinationSpecified);
            int num_samples = all_sample_hitpicks.Count();
            if( num_samples == 0){
                throw new Exception( "no samples found");
            }
            // enumeration of all sources and the number of controls in each source.
            var controls_per_src = from source in Sources
                                   select new{ Source = source, NumControls = source.Hitpicks.Count( hitpick => hitpick.DestinationSpecified)};

            // ------------------------------------------------------------------
            // determine the number of controls per plate (with controls):
            // number of controls per plate (with controls).
            int controls_per_plate = 0;
            // distinctly from fewest to most, the number of controls found among the source plates.
            var distinct_ordered_src_control_count = controls_per_src.Select( item => item.NumControls).OrderBy( num_controls => num_controls).Distinct();
            if( distinct_ordered_src_control_count.Count() == 1){
                // all source plates have the same number of controls (possibly zero).
                controls_per_plate = distinct_ordered_src_control_count.First();
            } else if( distinct_ordered_src_control_count.Count() == 2){
                // some source plates have no controls others have some positive number of controls.
                int lower = distinct_ordered_src_control_count.First();
                if( lower != 0){
                    // the source plates with fewer controls must have no controls!
                    throw new Exception( "different number of controls specified in different source plates");
                }
                controls_per_plate = distinct_ordered_src_control_count.Last();
            } else{
                // some source plates might have no controls, but the remaining source plates must have the same number of controls!
                throw new Exception( "different number of controls specified in different source plates");
            }
            if( controls_per_plate == 0){
                throw new Exception( "no controls found");
            }

            // ------------------------------------------------------------------
            // if we've reached this point, then all sources that have controls also have the same number of controls.
            // ------------------------------------------------------------------

            // ------------------------------------------------------------------
            // make sure controls from one source are going to a single destination:
            // get an enumeration of control plates.
            var control_plates = from item in controls_per_src
                                 where item.NumControls == controls_per_plate
                                 select item.Source;
            // get an enumeration of sample only plates.
            var sample_only_plates = from item in controls_per_src
                                     where item.NumControls == 0
                                     select item.Source;
            // for each control plate, determine how many distinct destinations are targeted by the controls.
            // if there isn't a single distinct destination, then we're in trouble!
            foreach( MonsantoSource control_plate in control_plates){
                var distinct_control_dst_plates = ( from hitpick in control_plate.Hitpicks
                                                    where hitpick.DestinationSpecified
                                                    select hitpick.DstPlateNumber).Distinct();
                if( distinct_control_dst_plates.Count() != 1){
                    throw new Exception( "controls from the same source plate have different destinations");
                }
            }

            // ------------------------------------------------------------------
            // make sure controls from different sources are going to different destinations:
            // since we have ensured that controls from each source go to the same destination,
            // the following enumeration of source -> destination pairings will have distinct sources.
            var control_src_dst_pairings = all_control_hitpicks.Select( control_hitpick => new{ Source = control_hitpick.Source, DestinationPlateNumber = control_hitpick.Hitpick.DstPlateNumber}).Distinct();
            // determine how many distinct destinations are targeted with controls.
            var distinct_dsts_with_controls = control_src_dst_pairings.Select( item => item.DestinationPlateNumber).Distinct();
            // the number of sources with controls must equal the number of destinations targeted with controls.
            if( control_src_dst_pairings.Count() != distinct_dsts_with_controls.Count()){
                Debug.Assert( false); // change back to throw later.
                // throw new Exception( "controls from different source plates have the same destination");
            }

            // ------------------------------------------------------------------
            // if we've reached this point, then each source plate that has controls has controls that go to the same destination plate.
            // furthermore, for each source plate that has controls, its controls go to a unique destination plate among destination plates that receive controls.
            // ------------------------------------------------------------------

            // ------------------------------------------------------------------
            // determine number of destination plates needed.

            // DKM 2011-10-07 number of dests should be AT LEAST as many as the number of distinct destinations required for source controls
            //                the existing code tries to fit all of the hits into as few plates as possible, but violates the usage of
            //                distinct dest plates for controls.
            /*
            int num_samples_to_assign_dst = num_samples;
            int num_dst_plates_needed = 0;
            while( num_samples_to_assign_dst > 0){
                ++num_dst_plates_needed;
                int num_wells_for_samples = NUM_WELLS_PER_DESTINATION_PLATE;
                if( distinct_dsts_with_controls.Contains( num_dst_plates_needed)){
                    num_wells_for_samples -= controls_per_plate;
                }
                num_samples_to_assign_dst -= num_wells_for_samples;
            }
             */
            int num_dst_plates_needed = distinct_dsts_with_controls.Count();
            int remaining_wells = (NUM_WELLS_PER_DESTINATION_PLATE - controls_per_plate) * num_dst_plates_needed;
            int extra_wells_needed = remaining_wells - num_samples;
            while( extra_wells_needed < 0) {
                num_dst_plates_needed++;
                extra_wells_needed += NUM_WELLS_PER_DESTINATION_PLATE;
            }
            // ------------------------------------------------------------------
            // make sure control plates don't specify destination plates beyond the necessary number of destination plates.
            if( control_src_dst_pairings.Select( x => x.DestinationPlateNumber).Max() > num_dst_plates_needed){
                throw new Exception( "controls destined for a destination plate that won't be created");
            }

            // ------------------------------------------------------------------
            // create dictionary of source plate barcodes to source plates.
            // create dictionary of destination plate numbers to destination plates.

            // create all the destination plates and create a dictionary to facilitate lookup of the destination plates by destination plate number.
            IDictionary< int, DestinationPlate> dst_plates = Enumerable.Range( 1, num_dst_plates_needed).ToDictionary( x => x, x => new DestinationPlate( labware_database.GetLabware( MonsantoWorkset.MonsantoLabwareCodeToLabwareName( DstLabwareCode)), x.ToString(), "A1:H12"));

            // create a dictionary of hitpick assignments.
            IDictionary< SourceHitpick, DestinationPlate> hitpick_assignments = new Dictionary< SourceHitpick, DestinationPlate>();

            // assign entire contents of control plates to their respective destination plates.
            hitpick_assignments = all_hitpicks.Where( hitpick => control_plates.Contains( hitpick.Source)).ToDictionary( hitpick => hitpick, hitpick => dst_plates[ control_src_dst_pairings.Where( csdp => csdp.Source == hitpick.Source).First().DestinationPlateNumber]);

            Stack< SourceHitpick> sample_only_hitpicks = new Stack< SourceHitpick>( all_hitpicks.Where( hitpick => sample_only_plates.Contains( hitpick.Source)).OrderBy( hitpick => hitpick.Source.Barcode));
            foreach( DestinationPlate dst_plate in dst_plates.Values){
                int num_wells_to_assign = NUM_WELLS_PER_DESTINATION_PLATE - hitpick_assignments.Count( x => x.Value == dst_plate);
                for( int loop = 0; loop < num_wells_to_assign && sample_only_hitpicks.Count > 0; ++loop){
                    hitpick_assignments[ sample_only_hitpicks.Pop()] = dst_plate;
                }
            }

            var ordered_sources = hitpick_assignments.OrderBy( hitpick => int.Parse( hitpick.Value.Barcode)).Select( hitpick => hitpick.Key.Source).Distinct();
            IDictionary< string, SourcePlate> src_plates = ordered_sources.ToDictionary( x => x.Barcode, x => new SourcePlate( labware_database.GetLabware( MonsantoWorkset.MonsantoLabwareCodeToLabwareName( x.SrcLabwareCode)), x.Barcode));

            string file_path = FileSystem.GetAppPath() + "\\config\\methods\\" + Method + ".xml";
            Reader reader = new Reader( labware_database);
            TransferOverview transfer_overview = reader.Read( file_path, null, "method");

            foreach( SourcePlate src_plate in src_plates.Values){
                transfer_overview.SourcePlates.Add( src_plate);
            }
            foreach( DestinationPlate dst_plate in dst_plates.Values){
                transfer_overview.DestinationPlates.Add( dst_plate);
            }
            transfer_overview.Transfers.AddRange( hitpick_assignments.Select( hitpick => new Transfer( src_plates[ hitpick.Key.Source.Barcode],
                                                                                                       new Well( hitpick.Key.Hitpick.SrcWellName),
                                                                                                       Volume,
                                                                                                       VolumeUnits.ul,
                                                                                                       Volume,
                                                                                                       VolumeUnits.ul,
                                                                                                       transfer_overview.DefaultLiquidClass,
                                                                                                       hitpick.Value,
                                                                                                       new List< Well>{ new Well( hitpick.Key.Hitpick.DestinationSpecified ? hitpick.Key.Hitpick.DstWellName : Well.ANY_WELL_NAME)},
                                                                                                       "",
                                                                                                       "",
                                                                                                       ASPIRATE_DISTANCE_FROM_BOTTOM,
                                                                                                       DISPENSE_DISTANCE_FROM_BOTTOM)));
            foreach( var dst_plate in dst_plates.Values){
                var reserved_well_names = transfer_overview.Transfers.Where( transfer => transfer.DstPlate == dst_plate && !transfer.DstWell.IsAny()).Select( transfer => transfer.DstWell.WellName);
                var all_well_names = Enumerable.Range( 0, 96).Select( x => new Well( LabwareFormat.LF_STANDARD_96, x).WellName);
                var available_well_names = all_well_names.Except( reserved_well_names);
                Queue< string> available_well_names_stack = new Queue< string>( available_well_names);
                foreach( var transfer in transfer_overview.Transfers.Where( transfer => transfer.DstPlate == dst_plate && transfer.DstWell.IsAny())){
                    transfer.SetDestinationWell( new Well( available_well_names_stack.Dequeue()));
                }
            }
            return transfer_overview;
        }
    }

    public class MonsantoProjects
    {
        public IList< MonsantoProject> Projects { get; private set; }
        public MonsantoProjects()
        {
            Projects = new List< MonsantoProject>();
        }
    }

    public class MonsantoWorkset
    {
        public MonsantoWorkset()
        {
        }

        public MonsantoProjects ReadWorkset( string workset_xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml( workset_xml);
            Debug.Assert( doc.DocumentElement.Name == "projects");
            MonsantoProjects monsanto_projects = new MonsantoProjects();
            // parse each project node contained therein.
            foreach( XmlNode project_node in doc.DocumentElement.ChildNodes){
                monsanto_projects.Projects.Add( ParseProject( project_node));
            }
            return monsanto_projects;
        }

        protected MonsantoProject ParseProject( XmlNode project_node)
        {
            Debug.Assert( project_node.Name == "project");
            // parse attributes to create project.
            XmlAttributeCollection attributes = project_node.Attributes;
            string name = attributes[ "name"].Value;
            string method = attributes[ "method"].Value;
            string volume_string = attributes[ "volume"].Value;
            string volume_number = volume_string.Substring( 0, volume_string.LastIndexOfAny( "0123456789".ToCharArray()) + 1);
            int volume = int.Parse( volume_number);
            string destination_labware_code = attributes[ "destination"].Value;
            MonsantoProject monsanto_project = new MonsantoProject( name, method, volume, destination_labware_code);
            // parse each source node contained therein.
            foreach( XmlNode source_node in project_node){
                monsanto_project.Sources.Add( ParseSource( source_node));
            }
            return monsanto_project;
        }

        protected MonsantoSource ParseSource( XmlNode source_node)
        {
            Debug.Assert( source_node.Name == "source");
            // parse attributes to create source.
            XmlAttributeCollection attributes = source_node.Attributes;
            string barcode = attributes[ "bc"].Value;
            string source_labware_code = attributes[ "type"].Value;
            MonsantoSource monsanto_source = new MonsantoSource( barcode, source_labware_code);
            // parse each hitpick node contained therein.
            foreach( XmlNode hitpick_node in source_node){
                monsanto_source.Hitpicks.Add( ParseHitpick( hitpick_node));
            }
            return monsanto_source;
        }

        protected MonsantoHitpick ParseHitpick( XmlNode hitpick_node)
        {
            Debug.Assert( hitpick_node.Name == "hitpick");
            // parse attributes to create hitpick.
            XmlAttributeCollection attributes = hitpick_node.Attributes;
            // bool is_control = attributes[ "type"].Value == "control";
            // bool is_sample = attributes[ "type"].Value == "sample";
            string source_row = attributes[ "source_row"].Value;
            string source_column = attributes[ "source_column"].Value;
            string source_well_name = source_row + source_column;
            if( attributes[ "destination_plate"] != null){
                int destination_plate_number = int.Parse(attributes["destination_plate"].Value);
                string destination_row = attributes["destination_row"].Value;
                string destination_column = attributes["destination_column"].Value;
                string destination_well_name = destination_row + destination_column;
                return new MonsantoHitpick( source_well_name, destination_plate_number, destination_well_name);
            } else{
                return new MonsantoHitpick( source_well_name);
            }
        }

        public static string MonsantoLabwareCodeToLabwareName( string labware_code)
        {
            return labware_code;
        }
    }
}
