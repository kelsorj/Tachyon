using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Windows.Controls;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace BioNex.Shared.SimpleInventory
{
    //-------------------------------------------------------------------------
    // INVENTORY ITEM
    //-------------------------------------------------------------------------
    public class InventoryItem
    {
        private string _barcode;
        public string Barcode
        {
            get { return _barcode; }
            set {
                _barcode = value;

            }
        }
        private string _teachpoint;
        public string Teachpoint
        {
            get { return _teachpoint; }
            set {
                _teachpoint = value;

            }
        }
    }
    //-------------------------------------------------------------------------
    // BASE INVENTORY BACKEND CLASS
    //-------------------------------------------------------------------------
    public abstract partial class InventoryBackend
    {
        public const string NewBarcodeString = "NewBarcode";
        protected InventoryView _view = new InventoryView();
        public ObservableCollection<string> TeachpointNames { get; set; }
        public InventoryItem SelectedPlate { get; set; }
        public ObservableCollection<InventoryItem> Inventory { get; set; }

        public abstract string GetTeachpoint( string barcode);
        /// <summary>
        /// Adds a new plate to the inventory system, but does not specify a teachpoint.  Barcode
        /// is &lt;new barcode&gt; by default.
        /// </summary>
        /// <remarks>
        /// This is intended for use in the GUI where a user will want to add more plates to the system
        /// </remarks>
        public abstract void AddPlate();
        /// <summary>
        /// Adds a new plate to the inventory system and specifies a teachpoint
        /// </summary>
        /// <param name="barcode"></param>
        /// <param name="teachpoint"></param>
        public abstract void AddPlate( string barcode, string teachpoint);
        /// <summary>
        /// Moves a plate in inventory from one location to another
        /// </summary>
        /// <remarks>
        /// Should not be called by robots, only for manual plate reordering
        /// </remarks>
        /// <param name="barcode"></param>
        /// <param name="teachpoint"></param>
        public abstract void ChangePlateLocation( string barcode, string teachpoint);
        /// <summary>
        /// Removes a plate from inventory
        /// </summary>
        /// <param name="barcode"></param>
        public abstract void DeletePlate( string barcode);
        public abstract int GetNumberOfPlates();
        public abstract List<InventoryItem> GetInventory();
        /// <summary>
        /// Saves all inventory changes
        /// </summary>
        /// <remarks>
        /// During runtime, none of the plate moves are saved unless explicitly committed!
        /// </remarks>
        public abstract void Commit();
        /// <summary>
        /// Populates the data for the serialization object from the data in the GUI
        /// </summary>
        public abstract void UpdateFromGUI();
        public abstract void Reload();
        public abstract void DeleteSelectedPlate();

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for
        /// a given property.
        /// </summary>
        /// <param name="propertyName">The name of the changed property.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
    //-------------------------------------------------------------------------
    // XML IMPLEMENTATION
    //-------------------------------------------------------------------------
    public class InventoryXML : InventoryBackend
    {
        XDocument _xml;
        string _filepath;
        private const string ROOT = "Inventory";
        private const string PLATE = "Plate";
        private const string BARCODE = "Barcode";
        private const string LOCATION = "Location";

        public InventoryXML()
        {
            InitializeCommands();
            Inventory = new ObservableCollection<InventoryItem>();
            _xml = new XDocument();
            _xml.Add( new XElement( ROOT));
        }

        public InventoryXML( string filepath)
        {
            InitializeCommands();
            Inventory = new ObservableCollection<InventoryItem>();
            LoadFile( filepath);
        }

        public void LoadFile( string filepath)
        {
            try {
                _xml = XDocument.Load( filepath);
                _filepath = filepath;
                var results = from plate in _xml.Elements(ROOT).Elements(PLATE)
                              select new {
                                Barcode = plate.Element( BARCODE).Value,
                                Teachpoint = plate.Element( LOCATION).Value
                              };
                Inventory.Clear();
                foreach( var result in results)
                    Inventory.Add( new InventoryItem { Barcode=result.Barcode.ToString(), Teachpoint=result.Teachpoint.ToString() });
            } catch( Exception ex) {
                throw new InventoryXMLFileException( ex.Message);
            }
        }

        public override string GetTeachpoint( string barcode)
        {
            try {
                // use LINQ to query the List<InventoryItem>
                var results = from plate in Inventory
                              where plate.Barcode == barcode
                              select plate.Teachpoint;
                return results.First().ToString();
            } catch( InvalidOperationException) {
                throw new InventoryBarcodeNotFoundException( barcode);
            }
        }

        private bool BarcodeExists( string barcode)
        {
            var results = from plate in Inventory
                          where plate.Barcode == barcode
                          select plate;
            return results.Count() > 0;
        }

        public override void AddPlate()
        {
            // see if NewBarcodeString exists already
            string temp_barcode = InventoryBackend.NewBarcodeString;
            int counter = 1;
            while( BarcodeExists( temp_barcode)) {
                // loop and increment a counter until we find a barcode that doesn't already exist
                temp_barcode = String.Format( "{0}{1}", InventoryBackend.NewBarcodeString, counter++);
            }
            // now add the barcode
            AddPlate( temp_barcode, "");
        }

        public override void AddPlate( string barcode, string teachpoint)
        {
            // need to make sure the plate isn't already there
            if( BarcodeExists( barcode))
                throw new InventoryDuplicateBarcodeException( "The barcode '{0}' already exists in inventory");
            Inventory.Add( new InventoryItem { Barcode=barcode, Teachpoint=teachpoint } );
        }

        public override void ChangePlateLocation( string barcode, string teachpoint)
        {
            // the intent here is that the plate is getting moved, so it should already be in
            // inventory somewhere.  Currently, the use case is to change locations from within
            // the GUI, and not at runtime.  Runtime use case is to delete on unload, then add
            // upon loading the plate.
            if( !BarcodeExists( barcode))
                throw new InventoryBarcodeNotFoundException( barcode);
            var results = from plate in Inventory
                          where plate.Barcode == barcode
                          select plate;
            results.First().Teachpoint = teachpoint;
        }

        public override void DeletePlate( string barcode)
        {
            var results = from plate in Inventory
                          where plate.Barcode == barcode
                          select plate;

            foreach( InventoryItem ii in Inventory) {
                if( ii.Barcode == results.First().Barcode) {
                    Inventory.Remove( ii);
                    return;
                }
            }
        }

        public override int GetNumberOfPlates()
        {
            return Inventory.Count;
        }

        public override void Commit()
        {
            UpdateFromGUI();
            _xml.Save( _filepath);
        }

        public void CommitAs( string new_filepath)
        {
            UpdateFromGUI();
            _xml.Save( new_filepath);
        }

        public override List<InventoryItem> GetInventory()
        {
            return Inventory.ToList();
        }

        public override void UpdateFromGUI()
        {
            // remove all of the elements
            _xml.Element(ROOT).RemoveAll();
            // iterate over all of the inventory items and rewrite the elements
            foreach( InventoryItem ii in Inventory)
                _xml.Element(ROOT).Add( new XElement( PLATE, new XElement( BARCODE, ii.Barcode), new XElement( LOCATION, ii.Teachpoint)));
        }

        public override void Reload()
        {
            LoadFile( _filepath);
        }

        public override void DeleteSelectedPlate()
        {
            Inventory.Remove( SelectedPlate);
        }
    }
    //-------------------------------------------------------------------------
    // DATABASE IMPLEMENTATION
    //-------------------------------------------------------------------------
    public class InventoryDB : InventoryBackend
    {
        public override string GetTeachpoint(string barcode)
        {
            throw new NotImplementedException();
        }

        public override void AddPlate()
        {
            throw new NotImplementedException();
        }

        public override void AddPlate(string barcode, string teachpoint)
        {
            throw new NotImplementedException();
        }

        public override void ChangePlateLocation( string barcode, string teachpoint)
        {
            throw new NotImplementedException();
        }

        public override void DeletePlate(string barcode)
        {
            throw new NotImplementedException();
        }

        public override int GetNumberOfPlates()
        {
            throw new NotImplementedException();
        }

        public override List<InventoryItem> GetInventory()
        {
            throw new NotImplementedException();
        }
        
        public override void Commit()
        {
            throw new NotImplementedException();
        }

        public override void UpdateFromGUI()
        {
            throw new NotImplementedException();
        }

        public override void Reload()
        {
            throw new NotImplementedException();
        }

        public override void DeleteSelectedPlate()
        {
            throw new NotImplementedException();
        }
    }
}
