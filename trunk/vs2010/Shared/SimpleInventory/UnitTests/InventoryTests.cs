using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using BioNex.Shared.SimpleInventory;
using NUnit.Framework;

namespace BioNex.Shared.SimpleInventory.UnitTests
{
    public class UnitTests
    {
        [Test, RequiresSTA]
        public void TestLoadBadInventoryXML()
        {
            bool caught_error = false;
            try {
                InventoryXML inv = new InventoryXML( "this_file_doesn't_exist.xml");
            } catch( InventoryXMLFileException) {
                caught_error = true;
            }
            Assert.AreEqual( true, caught_error, "Did not detect non-existant inventory file");
        }

        [Test, RequiresSTA]
        public void TestLoadInventoryXML()
        {
            InventoryXML inv = new InventoryXML( @"..\..\unittests\test_inventory.xml");
            Assert.AreEqual( 2, inv.GetNumberOfPlates());
            Assert.AreEqual( "hotel 1", inv.GetTeachpoint( "S001"));
            Assert.AreEqual( "hotel 2", inv.GetTeachpoint( "D001"));
        }

        [Test, RequiresSTA]
        public void TestReloadInventoryXML()
        {
            InventoryXML inv = new InventoryXML( @"..\..\unittests\test_inventory.xml");
            inv.Reload();
            Assert.AreEqual( 2, inv.GetNumberOfPlates());
            Assert.AreEqual( "hotel 1", inv.GetTeachpoint( "S001"));
            Assert.AreEqual( "hotel 2", inv.GetTeachpoint( "D001"));

        }

        [Test, RequiresSTA]
        public void TestNewInventoryXML()
        {
            InventoryXML inv = new InventoryXML();
            Assert.AreEqual( 0, inv.GetNumberOfPlates());
            inv.AddPlate( "S001", "test teachpoint");
            Assert.AreEqual( "test teachpoint", inv.GetTeachpoint( "S001"));
            Assert.AreEqual( 1, inv.GetNumberOfPlates());
            inv.DeletePlate( "S001");
            Assert.AreEqual( 0, inv.GetNumberOfPlates());
            bool caught_error = false;
            try {
                string teachpoint = inv.GetTeachpoint( "S001");
            } catch( InventoryBarcodeNotFoundException) {
                caught_error = true;
            }
            Assert.AreEqual( true, caught_error, "Did not detect non-existant barcode");
        }

        [Test, RequiresSTA]
        public void TestDuplicateInventoryEntriesXML()
        {
            bool caught_error = false;
            InventoryXML inv = new InventoryXML();
            inv.AddPlate( "S001", "test teachpoint");
            try {
                inv.AddPlate( "S001", "test teachpoint");
            } catch( InventoryDuplicateBarcodeException) {
                caught_error = true;
            }
            Assert.AreEqual( true, caught_error, "Did not detect duplicate barcode");
        }

        [Test, RequiresSTA]
        public void TestGetInventory()
        {
            InventoryXML inv = new InventoryXML( @"..\..\unittests\test_inventory.xml");
            List<InventoryItem> plates = inv.GetInventory();
            Assert.AreEqual( 2, plates.Count);
            int matches = 0;
            foreach( InventoryItem ii in plates) {
                if( ii.Barcode == "S001") {
                    Assert.AreEqual( "hotel 1", ii.Teachpoint);
                    matches++;
                }
                if( ii.Barcode == "D001") {
                    Assert.AreEqual( "hotel 2", ii.Teachpoint);
                    matches++;
                }
            }
            Assert.AreEqual( 2, matches);
        }

        [Test, RequiresSTA]
        public void TestMovePlate()
        {
            InventoryXML inv = new InventoryXML( @"..\..\unittests\test_inventory.xml");
            List<InventoryItem> plates = inv.GetInventory();
            inv.ChangePlateLocation( "S001", "new teachpoint");
            Assert.AreEqual( "new teachpoint", inv.GetTeachpoint( "S001"));
        }

        [Test, RequiresSTA]
        public void TestSaveCopy()
        {
            InventoryXML inv = new InventoryXML( @"..\..\unittests\test_inventory.xml");
            inv.CommitAs( @"test_inventory_copy.xml");
            inv = new InventoryXML( @"test_inventory_copy.xml");
            Assert.AreEqual( 2, inv.GetNumberOfPlates());
            Assert.AreEqual( "hotel 1", inv.GetTeachpoint( "S001"));
            Assert.AreEqual( "hotel 2", inv.GetTeachpoint( "D001"));
        }

        [Test, RequiresSTA]
        public void TestCommit()
        {
            InventoryXML inv = new InventoryXML( @"..\..\unittests\test_inventory.xml");
            inv.CommitAs( @"test_inventory_copy.xml");
            inv = new InventoryXML( @"test_inventory_copy.xml");
            inv.AddPlate( "S002", "hotel 3");
            inv.Commit();
            inv = new InventoryXML( @"test_inventory_copy.xml");
            Assert.AreEqual( 3, inv.GetNumberOfPlates());
            Assert.AreEqual( "hotel 1", inv.GetTeachpoint( "S001"));
            Assert.AreEqual( "hotel 2", inv.GetTeachpoint( "D001"));
            Assert.AreEqual( "hotel 3", inv.GetTeachpoint( "S002"));
        }

        [Test, RequiresSTA]
        public void TestAddNewPlate()
        {
            InventoryXML inv = new InventoryXML();
            inv.AddPlate();
            Assert.AreEqual( 1, inv.GetNumberOfPlates());
            Assert.AreEqual( "", inv.GetTeachpoint( InventoryBackend.NewBarcodeString));
            inv.AddPlate();
            Assert.AreEqual( 2, inv.GetNumberOfPlates());
            List<InventoryItem> plates = inv.GetInventory();
            int matched = 0;
            foreach( InventoryItem ii in plates) {
                if( ii.Barcode == InventoryBackend.NewBarcodeString)
                    matched++;
                if( ii.Barcode == String.Format( "{0}1", InventoryBackend.NewBarcodeString))
                    matched++;
            }
            Assert.AreEqual( 2, matched, "Didn't match all newly created plates");
        }
    }
}
