using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using BioNex.Shared.LibraryInterfaces;

namespace BioNex.Shared.PlateDefs
{
    public class MockLabwareDatabase : ILabwareDatabase
    {
        #region ILabwareDatabase Members

        public List<string> GetLabwareNames()
        {
            throw new NotImplementedException();
        }

        public List<string> GetTipBoxNames()
        {
            throw new NotImplementedException();
        }

        public ILabware GetLabware(string labware_name)
        {
            if( labware_name == "384")
                return new ILabware( "384", 384, 14, 11); // i.e. well bottom 2mm off of teachpoint
            else if( labware_name == "1536")
                return new ILabware( "1536", 1536, 14, 11); // i.e. well bottom 2mm off of teachpoint
            else
                return new ILabware( "96", 96, 14, 11); // i.e. well bottom 2mm off of teachpoint
        }

        public List<ILabware> GetCompatibleTips(ILabware labware)
        {
            throw new NotImplementedException();
        }

        public void ShowEditor()
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class PlateDefsTest
    {
        private MockLabwareDatabase db = new MockLabwareDatabase();

        [Test]
        public void TestAnyWell()
        {
            Well well = new Well();
            Assert.AreEqual( well.IsAnyWell(), true);
        }

        [Test]
        public void TestAnyWellStatic()
        {
            Assert.AreEqual( Well.IsAnyWell( "any"), true);
        }

        [Test]
        public void Test96WellNamesToRowColumn()
        {
            ILabware source = db.GetLabware( "96");
            Plate plate = new SourcePlate( source, "S00001");
            Well well = new Well( plate, "A5");
            Assert.AreEqual( well.Row, 0);
            Assert.AreEqual( well.Column, 4);
        }

        [Test]
        public void Test384WellNamesToRowColumn()
        {
            ILabware source = db.GetLabware( "384");
            Plate plate = new SourcePlate( source, "S00001");
            Well well = new Well( plate, "O12");
            Assert.AreEqual( well.Row, 14);
            Assert.AreEqual( well.Column, 11);
        }

        [Test]
        public void Test1536WellNamesToRowColumn()
        {
            ILabware source = db.GetLabware( "1536");
            Plate plate = new SourcePlate( source, "S00001");
            Well well = new Well( plate, "BB46");
            Assert.AreEqual( well.Row, 27);
            Assert.AreEqual( well.Column, 45);
        }

        [Test]
        public void TestDestinationPlateOneUsable()
        {
            ILabware dest = db.GetLabware( "96");
            DestinationPlate plate = new DestinationPlate( dest, "D00001", "A1");
            Assert.IsTrue( plate.AreWellsUsable( "a1"));
            Assert.IsFalse( plate.AreWellsUsable( "B2"));
        }

        [Test]
        public void TestDestinationPlateMultipleUsable()
        {
            ILabware dest = db.GetLabware( "96");
            DestinationPlate plate = new DestinationPlate( dest, "D00001", "A1, b2, C3, d4");
            Assert.IsTrue( plate.AreWellsUsable( "a1"));
            Assert.IsTrue( plate.AreWellsUsable( "B2"));
            Assert.IsTrue( plate.AreWellsUsable( "c3"));
            Assert.IsTrue( plate.AreWellsUsable( "D4"));
            Assert.IsTrue( plate.AreWellsUsable( new List<string>( new string[] { "a1", "B2", "c3" })));
            Assert.IsFalse( plate.AreWellsUsable( new List<string>( new string[] { "A1", "f6" })));
        }

        [Test]
        public void TestDestinationPlateRangeUsable()
        {
            ILabware dest = db.GetLabware( "96");
            DestinationPlate plate = new DestinationPlate( dest, "D00001", "C5:h9");
            Assert.IsTrue( plate.AreWellsUsable( "c5"));
            Assert.IsTrue( plate.AreWellsUsable( "H9"));
            Assert.IsTrue( plate.AreWellsUsable( new List<string>( new string[] { "C5", "h9" })));
            Assert.IsTrue( plate.AreWellsUsable( "f7"));
            Assert.IsFalse( plate.AreWellsUsable( "c4"));
            Assert.IsFalse( plate.AreWellsUsable( "h10"));
            Assert.IsFalse( plate.AreWellsUsable( "b7"));
            Assert.IsFalse( plate.AreWellsUsable( "i8"));
        }

        [Test]
        public void TestWellIndexer()
        {
            ILabware source = db.GetLabware( "96");
            Plate plate = new SourcePlate( source, "barcode");
            Well well = new Well( plate, "B6");
            Console.WriteLine( "B6: row = {0}, column = {1}", well.Row, well.Column);
            plate["A5"] = well;
            Well well_check = plate["A5"];
            Console.WriteLine( "B6 returned: row = {0}, column = {1}", well_check.Row, well_check.Column);
            Assert.AreEqual( well_check.Row, 1);
            Assert.AreEqual( well_check.Column, 5);
        }

        [Test]
        public void TestWellContents()
        {
            ILabware source = db.GetLabware( "96");
            Plate plate = new SourcePlate( source, "S1234");
            Well well = new Well( plate, "F5");
            WellContent wc = new WellContent( well, 20);
            plate["G6"].Contents.Add( wc);
            WellContent test_wc = plate["G6"].Contents[0];
            // this is comparing a reference, not the values.
            Assert.AreEqual( test_wc, wc);
        }

        [Test]
        public void TestDuplicatePlateEntries()
        {
            PlateCatalog cat = new PlateCatalog();
            ILabware source = db.GetLabware( "96");
            Plate plate = new SourcePlate( source, "S00001");
            cat.Add( plate);
            try {
                cat.Add( plate);
            } catch( PlateCatalog.PlateExistsException) {
                Assert.Fail( "Should not fail if we call Add() twice");
            }
        }

        [Test]
        public void TestAddRetrievePlateFromPlateCatalog()
        {
            PlateCatalog cat = new PlateCatalog();
            ILabware source = db.GetLabware( "96");
            Plate source_plate = new SourcePlate( source, "S00001");
            cat.Add( source_plate);
            Plate retrieved_plate = cat["S00001"];
            Assert.AreEqual( source_plate, retrieved_plate);
        }

        [Test]
        public void TestAddRetrieveWellFromPlateCatalog()
        {
            PlateCatalog cat = new PlateCatalog();
            ILabware source = db.GetLabware( "96");
            Plate source_plate = new SourcePlate( source, "S00001");
            Well source_well = new Well( source_plate, "D5");
            cat.Add( source_plate);
            Plate retrieved_plate = cat["S00001"];
            // comparing references, not values
            Assert.AreEqual( source_well, retrieved_plate["D5"]);
        }

        [Test]
        public void TestPlateCatalogIndexer()
        {
            PlateCatalog cat = new PlateCatalog();
            ILabware source = db.GetLabware( "96");
            Plate plate = new SourcePlate( source, "S00001");
            cat.Add( plate);
            Plate plate_ref = cat["S00001"];
            Assert.AreEqual( plate, plate_ref);
        }
    }
}
