using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BioNex.Plugins.Dock
{
    public class CartPlateLocation
    {
        public int Rack { get; private set; }
        public int Slot { get; private set; }

        public CartPlateLocation( int rack, int slot)
        {
            Rack = rack;
            Slot = slot;
        }

        public override string ToString()
        {
            return String.Format( "Rack {0}, Slot {1}", Rack, Slot);
        }

        public override bool Equals(object obj)
        {
            CartPlateLocation other = obj as CartPlateLocation;
            if( other == null)
                return false;
            return Rack == other.Rack && Slot == other.Slot;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
