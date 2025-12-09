using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace BioNex.Shared.Location
{
    public class PlatePlace
    {
        public string Name { get; protected set; }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public PlatePlace( string name)
        {
            Name = name;
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Used to describe plate locations to robots.  The LocationName is used to
    /// allow robots to create teachpoints, and the Used event is used to prevent
    /// robots from dropping plates off at occupied locations
    /// </summary>
    public class PlateLocation
    {
        // ----------------------------------------------------------------------
        // properties.
        // ----------------------------------------------------------------------
        public string Name { get; protected set; }
        public IEnumerable< PlatePlace> Places { get; protected set; }
        public ManualResetEvent Reserved { get; set; }
        public ManualResetEvent Occupied { get; set; }
        public bool Available{
            get{
                return !Reserved.WaitOne( 0) && !Occupied.WaitOne( 0);
            }
        }

        // ----------------------------------------------------------------------
        // constructors.
        // ----------------------------------------------------------------------
        public PlateLocation( string name)
            : this( name, new List< PlatePlace>{ new PlatePlace( name)})
        {
        }

        public PlateLocation( string name, IEnumerable< PlatePlace> places)
        {
            Name = name;
            Places = places;
            Reserved = new ManualResetEvent( false);
            Occupied = new ManualResetEvent( false);
        }

        // ----------------------------------------------------------------------
        // methods.
        // ----------------------------------------------------------------------
        public override string ToString()
        {
            return Name;
        }
    }
}
