using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.TaskListXMLParser;
using BioNex.Shared.Utils.WellMathUtil;

namespace BioNex.Shared.HitpickXMLReader
{
    public class Reader
    {
        private ILabwareDatabase _labware_database { get; set; }

        public Reader( ILabwareDatabase labware_database)
        {
            _labware_database = labware_database;
        }

        public TransferOverview Read( string filepath, string schemapath, string root_element_name = "transfer_overview")
        {
            XmlDocument doc = new XmlDocument();
            // only validate against XML schema if a schema file is passed
            if( !String.IsNullOrEmpty( schemapath)) {
                XmlTextReader xtr = new XmlTextReader(filepath);
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.Schemas.Add(null, schemapath);
                settings.ValidationType = ValidationType.Schema;
                XmlReader reader = XmlReader.Create(xtr, settings);
                // need to do this so I can figure out why unit test is failing here
                try {
                    doc.Load(reader);
                } catch( Exception ex) {
                    throw ex;
                }
            } else {
                doc.Load( filepath);
            }
            Debug.Assert( doc.DocumentElement.Name == root_element_name);
            TransferOverview transfer_overview = new TransferOverview();
            //! \todo this is pretty weak -- figure out a better way to force parsing
            //!       of source / dest plates first.  Is this a good application for LINQ?
            foreach( XmlNode node in doc.DocumentElement.ChildNodes) {
                switch( node.Name) {
                    case "destinations":
                        ParseDestinations( node, transfer_overview);
                        break;
                    case "default_liquid_class":
                        transfer_overview.DefaultLiquidClass = node.InnerText;
                        break;
                    case "sources":
                        ParseSources( node, transfer_overview);
                        break;
                    case "aspirate_script":
                        transfer_overview.DefaultAspirateScript = node.InnerText;
                        break;
                    case "aspirate_distance_from_well_bottom_mm":
                        transfer_overview.DefaultAspirateDistanceFromWellBottomMm = double.Parse( node.InnerText);
                        break;
                    case "dispense_script":
                        transfer_overview.DefaultDispenseScript = node.InnerText;
                        break;
                    case "dispense_distance_from_well_bottom_mm":
                        transfer_overview.DefaultDispenseDistanceFromWellBottomMm = double.Parse( node.InnerText);
                        break;
                    case "tasks":
                        ParseDefaultTasks( node, transfer_overview);
                        break;
                }
            }
            // parse the source / transfer nodes
            foreach( XmlNode node in doc.DocumentElement.ChildNodes) {
                switch( node.Name) {
                    case "transfers":
                        ParseTransfers( node, transfer_overview);
                        break;
                }
            }

            return transfer_overview;   // temporary
        }

        public TransferOverview ReadString( string hitpick_xml, string schemapath)
        {
            XmlDocument doc = new XmlDocument();
            // only validate against XML schema if a schema file is passed
            if( !String.IsNullOrEmpty( schemapath)) {
                // XmlTextReader xtr = new XmlTextReader(filepath);
                XmlTextReader xtr = new XmlTextReader( new System.IO.StringReader( hitpick_xml));
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.Schemas.Add(null, schemapath);
                settings.ValidationType = ValidationType.Schema;
                XmlReader reader = XmlReader.Create(xtr, settings);
                // need to do this so I can figure out why unit test is failing here
                try {
                    doc.Load(reader);
                } catch( Exception ex) {
                    throw ex;
                }
            } else {
                doc.LoadXml( hitpick_xml);
            }
            Debug.Assert( doc.DocumentElement.Name == "transfer_overview");
            TransferOverview transfer_overview = new TransferOverview();
            //! \todo this is pretty weak -- figure out a better way to force parsing
            //!       of source / dest plates first.  Is this a good application for LINQ?
            foreach( XmlNode node in doc.DocumentElement.ChildNodes) {
                switch( node.Name) {
                    case "destinations":
                        ParseDestinations( node, transfer_overview);
                        break;
                    case "default_liquid_class":
                        transfer_overview.DefaultLiquidClass = node.InnerText;
                        break;
                    case "sources":
                        ParseSources( node, transfer_overview);
                        break;
                    case "aspirate_script":
                        transfer_overview.DefaultAspirateScript = node.InnerText;
                        break;
                    case "aspirate_distance_from_well_bottom_mm":
                        transfer_overview.DefaultAspirateDistanceFromWellBottomMm = double.Parse( node.InnerText);
                        break;
                    case "dispense_script":
                        transfer_overview.DefaultDispenseScript = node.InnerText;
                        break;
                    case "dispense_distance_from_well_bottom_mm":
                        transfer_overview.DefaultDispenseDistanceFromWellBottomMm = double.Parse( node.InnerText);
                        break;
                    case "tasks":
                        ParseDefaultTasks( node, transfer_overview);
                        break;
                }
            }
            // parse the source / transfer nodes
            foreach( XmlNode node in doc.DocumentElement.ChildNodes) {
                switch( node.Name) {
                    case "transfers":
                        ParseTransfers( node, transfer_overview);
                        break;
                }
            }

            return transfer_overview;   // temporary
        }

        private static void ParseDefaultTasks( XmlNode tasks_node, TransferOverview to)
        {
            to.Tasks = TaskListParser.ParseDefaultTaskLists( tasks_node);
        }

        private void ParseSources( XmlNode source_node, TransferOverview to)
        {
            // set the plate's tasklists to the default values first, and then when we parse the
            // task XML below, they will get overridden automatically.
            IList<PlateTask> source_prehitpick_tasks = to.Tasks.source_prehitpick_tasks;
            IList<PlateTask> source_posthitpick_tasks = to.Tasks.source_posthitpick_tasks;

            // loop over each source plate in the source_info node
            foreach( XmlNode plate_node in source_node) {
                if( plate_node.Name != "source")
                    continue;
                string labware_name = null;
                string barcode = null;
                List<KeyValuePair<string,string>> variables = new List<KeyValuePair<string,string>>();
                
                // I have a problem where the transfer information is in the source plate's XML, but
                // in order to create the transfer object, I need to have the source plate object
                // first!  So that means that I have to save a ref to the transfer node, then process
                // it after I've created the source plate object
                // note -- I tried this and it didn't work.. I couldn't cache the reference or save
                //         the index for later use.  Need to loop again for now.
                foreach( XmlNode plate_attributes_node in plate_node) {
                    switch( plate_attributes_node.Name) {
                        case "labware_id":
                            labware_name = plate_attributes_node.InnerText;
                            // #320 throw error if labware doesn't exist, BEFORE we start running the protocol!
                            ILabware lw = _labware_database.GetLabware( labware_name);
                            break;
                        case "barcode":
                            barcode = plate_attributes_node.InnerText;
                            break;
                        case "prehitpick_tasks":
                            ParseOverriddenTasks( plate_attributes_node, ref source_prehitpick_tasks);
                            break;
                        case "posthitpick_tasks":
                            ParseOverriddenTasks( plate_attributes_node, ref source_posthitpick_tasks);
                            break;
                        case "variables":
                            ParseVariables( plate_attributes_node, ref variables);
                            break;

                    }
                }
                ILabware labware = _labware_database.GetLabware( labware_name);
                SourcePlate plate = new SourcePlate( labware, barcode);
                to.SourcePlates.Add( plate);
            }
        }

        private static void ParseOverriddenTasks( XmlNode node, ref IList<PlateTask> tasks)
        {
            // could be lame, but for now to make my life easier, I am going to convert from XmlNode to XElement.
            // I will likely convert all of our XML code to XDocument/XElement later
            XDocument doc = new XDocument();
            using( XmlWriter writer = doc.CreateWriter())
                node.WriteTo( writer);
            XElement root = doc.Root;
            tasks = TaskListParser.ParseTaskList( root);
        }

        private void ParseTransfers( XmlNode transfers_node, TransferOverview to)
        {
            string source_barcode = null;
            string src_well_name = null;
            string destination_barcode = null;
            string dst_well_name = null;
            string aspirate_script = ""; // don't use null for scripts to prevent ambiguity downstream of null vs. ""
            string dispense_script = "";
            double transfer_volume = 0;
            VolumeUnits transfer_volume_units = VolumeUnits.ul;
            double current_volume;
            VolumeUnits current_volume_units = VolumeUnits.ul;

            // we should have loaded the default values for these by the time parsing
            // transfers comes around!
            Debug.Assert( to.DefaultAspirateDistanceFromWellBottomMm != null);
            Debug.Assert( to.DefaultDispenseDistanceFromWellBottomMm != null);

            double? aspirate_distance_from_well_bottom_mm = null;
            double? dispense_distance_from_well_bottom_mm = null;

            // loop over the nodes and set the attributes above
            foreach( XmlNode node in transfers_node) {
                if( node.Name != "transfer")
                    continue;
                // this needs to get set to null because each transfer needs to get its
                // liquid class value checked and set to the default if it wasn't specified
                string liquid_class = null;
                foreach( XmlNode transfer_attribute_node in node) {
                    switch( transfer_attribute_node.Name) {
                        case "liquid_class_id":
                            liquid_class = transfer_attribute_node.InnerText;
                            break;
                        case "source":
                            foreach( XmlNode source_node in transfer_attribute_node) {
                                switch( source_node.Name) {
                                    case "barcode":
                                        source_barcode = source_node.InnerText;
                                        break;
                                    case "well":
                                        src_well_name = source_node.InnerText;
                                        break;
                                    case "aspirate_distance_from_well_bottom_mm":
                                        aspirate_distance_from_well_bottom_mm = double.Parse( source_node.InnerText);
                                        break;
                                }
                            }
                            // if we don't have aspirate distance set by now, then use the default value
                            if( !aspirate_distance_from_well_bottom_mm.HasValue)
                                aspirate_distance_from_well_bottom_mm = to.DefaultAspirateDistanceFromWellBottomMm;
                            break;
                        case "destination":
                            // here we need to look at the node's child elements to get the dest barcode and well
                            foreach( XmlNode dest_node in transfer_attribute_node) {
                                switch( dest_node.Name) {
                                    case "barcode":
                                        destination_barcode = dest_node.InnerText;
                                        break;
                                    case "well":
                                        dst_well_name = dest_node.InnerText;
                                        break;
                                    case "dispense_distance_from_well_bottom_mm":
                                        dispense_distance_from_well_bottom_mm = double.Parse( dest_node.InnerText);
                                        break;
                                }
                            }
                            // if we don't have dispense distance set by now, then use the default value
                            if( !dispense_distance_from_well_bottom_mm.HasValue)
                                dispense_distance_from_well_bottom_mm = to.DefaultDispenseDistanceFromWellBottomMm;
                            break;
                        case "transfer_volume":
                            transfer_volume = double.Parse( transfer_attribute_node.InnerText);
                            transfer_volume_units = (transfer_attribute_node.Attributes["units"].Value == VolumeUnits.ul.ToString()) ? VolumeUnits.ul : VolumeUnits.ml;
                            break;
                        case "current_volume":
                            current_volume = double.Parse(transfer_attribute_node.InnerText);
                            current_volume_units = (transfer_attribute_node.Attributes["units"].Value == VolumeUnits.ul.ToString()) ? VolumeUnits.ul : VolumeUnits.ml;
                            break;
                        // DKM 2010-11-04 now parse out aspirate and dispense script information to override
                        //                the default values
                        case "aspirate_script":
                            aspirate_script = transfer_attribute_node.InnerText;
                            break;
                        case "dispense_script":
                            dispense_script = transfer_attribute_node.InnerText;
                            break;
                    }
                }

                SourcePlate source_plate = (SourcePlate)to.SourcePlates[source_barcode];
                // create the destination plate with the attributes
                DestinationPlate destination_plate = (DestinationPlate)to.DestinationPlates[destination_barcode];
                // validate the source and dest well names!
                Well src_well = new Well( src_well_name, source_plate.LabwareFormat);
                Well dst_well = new Well( dst_well_name, destination_plate.LabwareFormat);
                // make sure we check the liquid profile
                if( liquid_class == null)
                    liquid_class = to.DefaultLiquidClass;
                // now we can create the transfer object
                to.AddTransfer( source_plate, src_well, destination_plate, dst_well, transfer_volume,
                                transfer_volume_units, liquid_class, aspirate_distance_from_well_bottom_mm.Value,
                                dispense_distance_from_well_bottom_mm.Value, aspirate_script, dispense_script);
            }
        }

        private void ParseDestinations( XmlNode dest_node, TransferOverview to)
        {
            // set the default dest plate task lists
            IList<PlateTask> dest_prehitpick_tasks = to.Tasks.dest_prehitpick_tasks;
            IList<PlateTask> dest_posthitpick_tasks = to.Tasks.dest_posthitpick_tasks;

            // loop over each source plate in the source_info node
            foreach( XmlNode plate_node in dest_node) {
                if( plate_node.Name != "destination")
                    continue;
                string labware_name = null;
                string barcode = null;
                string usable_wells = null;
                List<KeyValuePair<string,string>> variables = new List<KeyValuePair<string,string>>();
                foreach( XmlNode plate_attributes_node in plate_node) {
                    switch( plate_attributes_node.Name) {
                        case "labware_id":
                            labware_name = plate_attributes_node.InnerText;
                            // #320 throw error if labware doesn't exist, BEFORE we start running the protocol!
                            ILabware lw = _labware_database.GetLabware( labware_name);
                            break;
                        case "barcode":
                            barcode = plate_attributes_node.InnerText;
                            break;
                        case "usable_wells":
                            usable_wells = plate_attributes_node.InnerText;
                            break;
                        case "prehitpick_tasks":
                            ParseOverriddenTasks( plate_attributes_node, ref dest_prehitpick_tasks);
                            break;
                        case "posthitpick_tasks":
                            ParseOverriddenTasks( plate_attributes_node, ref dest_posthitpick_tasks);
                            break;
                        case "variables":
                            ParseVariables( plate_attributes_node, ref variables);
                            break;
                    }
                }
                ILabware labware = _labware_database.GetLabware( labware_name);
                DestinationPlate plate = new DestinationPlate( labware, barcode, usable_wells);
                to.DestinationPlates.Add( plate);
                to.DestinationLabware = labware;
            }
        }

        private static void ParseVariables( XmlNode node, ref List<KeyValuePair<string,string>> variables)
        {
            // could be lame, but for now to make my life easier, I am going to convert from XmlNode to XElement.
            // I will likely convert all of our XML code to XDocument/XElement later
            XDocument doc = new XDocument();
            using( XmlWriter writer = doc.CreateWriter())
                node.WriteTo( writer);
            XElement root = doc.Root;
            Debug.Assert( root.Name == "variables");
            var elements = from x in root.Elements( "variable") select x;
            foreach( var x in elements) {
                variables.Add( new KeyValuePair<string,string>( x.Attribute( "name").Value, x.Attribute( "value").Value));
            }
        }
    }
}
 