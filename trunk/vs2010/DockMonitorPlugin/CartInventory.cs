using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;

namespace BioNex.Plugins.Dock
{
    public class CartInventory
    {
        private ILog _log = LogManager.GetLogger( typeof(CartInventory));

        public Dictionary<string,CartPlateLocation> Inventory { get; private set; }

        public CartInventory()
        {
            Inventory = new Dictionary<string,CartPlateLocation>();
        }

        public void ClearInventory()
        {
            Inventory.Clear();
        }

        public CartPlateLocation GetPlateLocation( string barcode)
        {
            if( !Inventory.ContainsKey( barcode)) {
                string message = String.Format( "The barcode '{0}' was not found", barcode);
                _log.Warn( message);
                return null;
            }
            return Inventory[barcode];
        }

        public void LoadPlate( string barcode, CartPlateLocation location)
        {
            // remove all plates that could have been at this location, although there should be at most ONE
            if( Inventory.ContainsValue( location)) {
                var previous_plate_info = Inventory.Where( x => x.Value == location);
                if( previous_plate_info.Count() != 0) {
                    _log.Info( String.Format( "Loading a plate into occupied location '{0}'.  Removing all previous plates at this location.", location));
                    foreach( KeyValuePair<string,CartPlateLocation> kvp in previous_plate_info) {
                        _log.Info( String.Format( "Removing barcode '{0}'", kvp.Key));
                        Inventory.Remove( kvp.Key);
                    }
                }
            }
            // load the plate
            if( Inventory.ContainsKey( barcode))
                Inventory[barcode] = location;
            else
                Inventory.Add( barcode, location);
            _log.Info( String.Format( "Loaded plate '{0}' into location '{1}'", barcode, location));
        }

        public void UnloadPlate( string barcode)
        {
            if( !Inventory.ContainsKey( barcode)) {
                string message = String.Format( "The barcode '{0}' was not found", barcode);
                _log.Warn( message);
                throw new CartInventoryException( message);
            }
            CartPlateLocation location = GetPlateLocation( barcode);
            Inventory.Remove( barcode);
            _log.Info( String.Format( "Unloaded plate '{0}' from location '{1}'", barcode, location));
        }
    }

    public class CartInventoryException : Exception
    {
        public CartInventoryException( string message) : base(message) {}
    }
}
