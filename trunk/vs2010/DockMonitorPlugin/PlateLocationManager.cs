using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.ComponentModel.Composition;
using BioNex.Shared.LibraryInterfaces;
using System.Collections.ObjectModel;
using System.Windows;
using BioNex.Shared.DeviceInterfaces;

namespace BioNex.Plugins.Dock
{
    public class PlateLocationNotFoundException : ApplicationException
    {
        string LocationName { get; set; }

        public PlateLocationNotFoundException( string location_name)
        {
            LocationName = location_name;
        }
    }

    public class DockMonitorPlateLocation
    {
        public int RackNumber { get; private set; }
        public int SlotNumber { get; private set; }

        public DockMonitorPlateLocation( int rack_number, int slot_number)
        {
            RackNumber = rack_number;
            SlotNumber = slot_number;
        }

        public override bool Equals(object obj)
        {
            DockMonitorPlateLocation other = obj as DockMonitorPlateLocation;
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
            return ToString( this);
        }

        /// <summary>
        /// Given a plate location's name, figures out what rack and slot is referred to.
        /// </summary>
        /// <param name="location_name">e.g. "Rack 1, Slot 13"</param>
        /// <returns></returns>
        public static DockMonitorPlateLocation FromString( string location_name)
        {
            Regex regex = new Regex( @"[r|R]ack\s+(\d+),\s+[s|S]lot\s+(\d+)");
            MatchCollection matches = regex.Matches( location_name);
            if( matches.Count == 0)
                throw new PlateLocationNotFoundException( location_name);
            Debug.Assert( matches.Count == 1, "Regex error during parsing of location name (1)");
            // whole string is group #1, side, rack and slot are groups 1, 2 and 3
            Debug.Assert( matches[0].Groups.Count == 3, "Regex error during parsing of location name (2)");
            int rack = int.Parse( matches[0].Groups[1].ToString());
            int slot = int.Parse( matches[0].Groups[2].ToString());
            return new DockMonitorPlateLocation( rack, slot);
        }

        public static string ToString( DockMonitorPlateLocation location)
        {
            return String.Format( "Rack {0}, Slot {1}", location.RackNumber, location.SlotNumber);
        }

        public static bool IsValidPlateLocationName( string location_name)
        {
            try {
                // if FromString doesn't throw an exception, this must be a valid location name
                FromString( location_name);
                return true;
            } catch( Exception) {
                return false;
            }
        }

    }
}
