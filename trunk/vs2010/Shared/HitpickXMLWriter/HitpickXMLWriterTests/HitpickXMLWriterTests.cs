using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using BioNex.Shared.PlateDefs;
using BioNex.Shared.HitpickXMLWriter;
using BioNex.Shared.HitpickXMLReader;
using NUnit.Framework;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.LabwareDatabase;

namespace BioNex.Shared.HitpickXMLWriter
{
    internal class MockLabwareDatabase : ILabwareDatabase
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
            if( labware_name == "source labware")
                return new Labware( "source labware", 96, 14, 11);
            else if( labware_name == "dest labware")
                return new Labware( "dest labware", 96, 14, 11);
            return null;
        }

        public List<ILabware> GetCompatibleTips( ILabware labware)
        {
            return new List<ILabware>();
        }

        public void ShowEditor() {}
        #endregion
    }

    public class HitpickXMLWriterTest
    {
        [Test]
        public void TestOneOfEachSourceDestTransfer()
        {
            // create a TransferOverview to serialize
            TransferOverview to = new TransferOverview();
            Labware source = new Labware( "source labware", 96, 14, 11);
            SourcePlate source_plate = new SourcePlate( source, "S00001");
            to.SourcePlates.Add( source_plate);
            Labware dest = new Labware( "dest labware", 96, 14, 11);
            DestinationPlate dest_plate = new DestinationPlate( dest, "D00001", "B3:D8");
            to.DestinationPlates.Add( dest_plate);
            to.AddTransfer( source_plate, "A1", dest_plate, "B7", 1.0, VolumeUnits.ul, null, 0, 0);
            Writer.Write( to, "test.xml");
            // now read it back in
            ILabwareDatabase labware_database = new MockLabwareDatabase();
            Reader reader = new Reader( labware_database);
            TransferOverview comp = reader.Read( "test.xml", @"..\..\..\..\HitpickXMLReader\HitpickXMLReaderTests\test_hitpick.xsd");
            // compare the two TransferOverview objects
            Assert.IsTrue( TransferOverview.AreEqual( to, comp));
        }
    }
}
