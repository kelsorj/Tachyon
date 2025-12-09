using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using BioNex.Shared.BioNexGuiControls;
using BioNex.Shared.LibraryInterfaces;

namespace BioNex.BPS140Plugin
{
    public class PlateLocationNotFoundException : ApplicationException
    {
        string LocationName { get; set; }

        public PlateLocationNotFoundException( string location_name)
        {
            LocationName = location_name;
        }
    }

    public class PlateLocationManager
    {
        // this is temporarily broken up so I can debug it more easily
        private ObservableCollection<SideRackView> racks1_;
        public ObservableCollection<SideRackView> Side1Racks
        { 
            get { return racks1_; }
            set { racks1_ = value; }
        }
        private ObservableCollection<SideRackView> racks2_;
        public ObservableCollection<SideRackView> Side2Racks
        { 
            get { return racks2_; }
            set { racks2_ = value; }
        }

        private IInventoryManagement Inventory { get; set; }
        private List<int> _rack1_configuration { get; set; }
        private List<int> _rack2_configuration { get; set; }
        private BPS140Configuration _config_xml { get; set; }

        public PlateLocationManager( IInventoryManagement inventory, BPS140Configuration config, List<int> rack1_configuration, List<int> rack2_configuration)
        {
            Inventory = inventory;
            _rack1_configuration = rack1_configuration;
            _rack2_configuration = rack2_configuration;
            _config_xml = config;
            Side1Racks = new ObservableCollection<SideRackView>();
            Side2Racks = new ObservableCollection<SideRackView>();
            // use information from the device manager database to populate the racks
            RecreateRacksOnSide( 1);
            RecreateRacksOnSide( 2);
        }

        private void RecreateRacksOnSide( int side_number)
        {
            if( side_number == 1) {
                Side1Racks.Clear();
                for (int i = 0; i < _rack1_configuration.Count; i++) {
                    int rack_number = i + 1;
                    int number_of_slots = _rack1_configuration[i];
                    // DKM 2012-01-16 support one-sided BPS140
                    if( number_of_slots == 0)
                        continue;
                    RackView.PlateTypeT plate_type = _config_xml.GetDefaultPlateTypeForRack( side_number, rack_number);
                    Side1Racks.Add(new SideRackView( side_number, i, Enumerable.Range( 0, number_of_slots), plate_type));
                }
            } else if( side_number == 2) {
                // side 2
                Side2Racks.Clear();
                for (int i = 0; i < _rack2_configuration.Count; i++) {
                    int rack_number = i + 1;
                    int number_of_slots = _rack2_configuration[i];
                    // DKM 2012-01-16 support one-sided BPS140
                    if( number_of_slots == 0)
                        continue;
                    RackView.PlateTypeT plate_type = _config_xml.GetDefaultPlateTypeForRack( side_number, rack_number);
                    Side2Racks.Add(new SideRackView( side_number, i, Enumerable.Range( 0, number_of_slots), plate_type));
                }
            }
        }

        public void Clear( int side_number)
        {
            RecreateRacksOnSide( side_number);
        }

        public IList<BPS140PlateLocation> GetPlateLocations( int side)
        {
            List<BPS140PlateLocation> plate_locations = new List<BPS140PlateLocation>();
            var Racks = side == 1 ? Side1Racks : Side2Racks;
            foreach( RackView rack in Racks) {
                for( int slot=rack.SlotIndexes.Min()+1; slot<=rack.SlotIndexes.Max() + 1; slot++) {
                    plate_locations.Add( new BPS140PlateLocation( side, rack.RackNumber, slot));
                }
            }
            return plate_locations;
        }

        private BPS140PlateLocation GetPlateLocation( int side, int rack, int slot)
        {
            IEnumerable<BPS140PlateLocation> locations = GetPlateLocations( side).Where( location => (location.RackNumber == rack && location.SlotNumber == slot));
            Debug.Assert( locations.Count() == 1, String.Format( "There should only be one location defined for side {0}, rack {1}, slot {2}", side, rack, slot) );
            // in release mode, return null if there isn't a PlateLocation (but there should always be one!)
            if( locations.Count() == 0)
                return null;
            return locations.First();
        }

        private string GetTeachpointName( int side, int rack, int slot)
        {
            BPS140PlateLocation plate_location = GetPlateLocation( side, rack, slot);
            return plate_location.ToString();
        }

        public string GetTeachpoint( string barcode)
        {
            Dictionary<string,string> location = Inventory.GetLocation( barcode);
            int side = int.Parse( location["side"]);
            int rack = int.Parse( location["rack"]);
            int slot = int.Parse( location["slot"]);
            return GetTeachpointName( side, rack, slot);
        }
    }

    public class BPS140PlateLocation
    {
        public int SideNumber { get; private set; }
        public int RackNumber { get; private set; }
        public int SlotNumber { get; private set; }

        public BPS140PlateLocation( int side, int rack_number, int slot_number)
        {
            SideNumber = side;
            RackNumber = rack_number;
            SlotNumber = slot_number;
        }

        public override bool Equals(object obj)
        {
            BPS140PlateLocation other = obj as BPS140PlateLocation;
            if( other == null)
                return false;
            return SideNumber == other.SideNumber && RackNumber == other.RackNumber && SlotNumber == other.SlotNumber;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return ToString( this);
        }

        /// <summary>
        /// Given a plate location's name, figures out what rack and slot is referred to.
        /// </summary>
        /// <remarks>
        /// I actually think I don't need this, after modeling everything last night.
        /// </remarks>
        /// <param name="location_name">e.g. "Rack 1, Slot 13"</param>
        /// <returns></returns>
        public static BPS140PlateLocation FromString( string location_name)
        {
            Regex regex = new Regex( @"[s|S]ide\s+(\d+):\s+[r|R]ack\s+(\d+),\s+[s|S]lot\s+(\d+)");
            MatchCollection matches = regex.Matches( location_name);
            if( matches.Count == 0)
                throw new PlateLocationNotFoundException( location_name);
            Debug.Assert( matches.Count == 1, "Regex error during parsing of location name (1)");
            // whole string is group #1, side, rack and slot are groups 1, 2 and 3
            Debug.Assert( matches[0].Groups.Count == 4, "Regex error during parsing of location name (2)");
            int side = int.Parse( matches[0].Groups[1].ToString());
            int rack = int.Parse( matches[0].Groups[2].ToString());
            int slot = int.Parse( matches[0].Groups[3].ToString());
            return new BPS140PlateLocation( side, rack, slot);
        }

        public static string ToString( BPS140PlateLocation location)
        {
            return String.Format( "Side {0}: Rack {1}, Slot {2}", location.SideNumber, location.RackNumber, location.SlotNumber);
        }
    }
}
