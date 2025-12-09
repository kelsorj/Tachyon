using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using BioNex.Hive.Executor;
using BioNex.Hive.Hardware;
using BioNex.Shared.BioNexGuiControls;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.Location;

namespace BioNex.HivePrototypePlugin
{
    public class PlateLocationNotFoundException : ApplicationException
    {
        string LocationName { get; set; }

        public PlateLocationNotFoundException( string location_name)
        {
            LocationName = location_name;
        }
    }

    /// <summary>
    /// Keeps track of where plates are based off of reinventory and runtime transfers
    /// </summary>
    public class PlateLocationManager
    {
        // this is temporarily broken up so I can debug it more easily
        private ObservableCollection<RackView> _racks;
        public ObservableCollection<RackView> Racks
        { 
            get { return _racks; }
            set { _racks = value; }
        }
        private IDictionary< string, PlateLocation> Locations { get; set; }
        private IInventoryManagement Inventory { get; set; }
        private Dictionary<int,List<AutoShelfConfiguration>> _rack_configuration { get; set; }
        private Configuration _config_xml { get; set; }

        /// <summary>
        /// Need to tell the PlateLocationManager what the rack configuration looks like, i.e. the number
        /// of slots per shelf
        /// </summary>
        /// <param name="inventory"></param>
        /// <param name="config"></param>
        /// <param name="rack_configuration"></param>
        public PlateLocationManager( IInventoryManagement inventory, Configuration config, Dictionary<int,List<AutoShelfConfiguration>> rack_configuration)
        {
            Inventory = inventory;
            _config_xml = config;
            _rack_configuration = rack_configuration;
            try {
                RecreateRacks( _rack_configuration);
            } catch( ArgumentOutOfRangeException) {
                string message = String.Format( "Could not configure racks because the number of racks configured in config.xml does not match the number of racks defined in the device manager database.");
                MessageBox.Show( message);
            } catch( Exception ex) {
                MessageBox.Show( "Could not configure racks: " + ex.Message);
            }
        }

        private void RecreateRacks(Dictionary<int,List<AutoShelfConfiguration>> rack_configuration)
        {
            // use information from the device manager database to populate the racks
            Racks = new ObservableCollection<RackView>();
            Locations = new Dictionary< string, PlateLocation>();
            foreach( KeyValuePair<int,List<AutoShelfConfiguration>> kvp in rack_configuration) {
                int rack_index = kvp.Key;
                // it's optional to have a rack definition in the config file -- if the rack doesn't exist, just assume it is a barcode rack
                BioNex.Hive.Hardware.Configuration.RackConfig rack_config = (from racks in _config_xml.RackConfigurations where racks.RackNumber == (rack_index + 1) select racks).FirstOrDefault();
                RackView.PlateTypeT plate_type = rack_config != null ? rack_config.DefaultPlateType : RackView.PlateTypeT.Barcode;
                Racks.Add(new RackView(rack_index, (from x in kvp.Value select x.shelf_number), plate_type));
            }
        }

        public void Clear()
        {
            RecreateRacks( _rack_configuration);
        }

        public IList<HivePlateLocation> GetPlateLocations()
        {
            List<HivePlateLocation> plate_locations = new List<HivePlateLocation>();
            foreach( RackView rack in Racks) {
                // DKM 2011-04-25 here we need to be aware of possible gaps in the slots.  On the Monsanto Phase 1 system,
                //                one of the panels has a gap in it for the PlateMover.  The shelf above the PM is going to
                //                be "Shelf 3", but the one below it is going to be "Shelf 9".  We need to account for this
                //                in the teachpoint names that we report back to the robot.
                // use the slot spacing to figure out how to number the shelves in each rack
                foreach( int slot in rack.SlotIndexes) {
                    plate_locations.Add( new HivePlateLocation( rack.RackNumber, slot + 1));
                }
            }
            return plate_locations;
        }

        public IList<PlateLocation> GetPlateLocationNames()
        {
            return GetPlateLocations().Select( location => new PlateLocation( location.ToString())).ToArray();
        }

        private HivePlateLocation GetPlateLocation( int rack, int slot)
        {
            IEnumerable<HivePlateLocation> locations = GetPlateLocations().Where( location => (location.RackNumber == rack && location.SlotNumber == slot));
            Debug.Assert( locations.Count() == 1);
            // in release mode, return null if there isn't a PlateLocation (but there should always be one!)
            return ( locations.Count() == 0) ? null : locations.First();
        }

        private string GetTeachpointName( int rack, int slot)
        {
            HivePlateLocation plate_location = GetPlateLocation( rack, slot);
            return plate_location.ToString();
        }

        public string GetTeachpoint( string barcode)
        {
            Dictionary<string,string> location = Inventory.GetLocation( barcode);
            if( !bool.Parse( location["loaded"]))
                return "";
            int rack = int.Parse( location["rack"]);
            int slot = int.Parse( location["slot"]);
            return GetTeachpointName( rack, slot);
        }
    }

    public class HivePlateLocation
    {
        public int RackNumber { get; private set; }
        public int SlotNumber { get; private set; }

        public HivePlateLocation( int rack_number, int slot_number)
        {
            RackNumber = rack_number;
            SlotNumber = slot_number;
        }

        public override bool Equals(object obj)
        {
            HivePlateLocation other = obj as HivePlateLocation;
            if( other == null)
                return false;
            return RackNumber == other.RackNumber && SlotNumber == other.SlotNumber;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return String.Format( "Rack {0}, Slot {1}", RackNumber, SlotNumber);
        }

        /// <summary>
        /// Given a plate location's name, figures out what rack and slot is referred to.
        /// </summary>
        /// <remarks>
        /// I actually think I don't need this, after modeling everything last night.
        /// </remarks>
        /// <param name="location_name">e.g. "Rack 1, Slot 13"</param>
        /// <returns></returns>
        public static HivePlateLocation FromString( string location_name)
        {
            Regex regex = new Regex( @"[r|R]ack\s+(\d+),\s+[s|S]lot\s+(\d+)");
            MatchCollection matches = regex.Matches( location_name);
            if( matches.Count == 0)
                return null;
            Debug.Assert( matches.Count == 1);
            // whole string is group #1, rack and slot are groups 2 and 3
            Debug.Assert( matches[0].Groups.Count == 3);
            int rack = int.Parse( matches[0].Groups[1].ToString());
            int slot = int.Parse( matches[0].Groups[2].ToString());
            return new HivePlateLocation( rack, slot);
        }

        public static bool IsValidPlateLocationName( string location_name)
        {
            return FromString( location_name) != null;
        }
    }
}
