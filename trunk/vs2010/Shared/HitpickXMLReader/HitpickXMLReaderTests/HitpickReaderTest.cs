using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using BioNex.Shared.HitpickXMLReader;
using BioNex.Shared.PlateDefs;
using System.Xml;
using NUnit.Framework;
using System.IO;
using System.Reflection;
using BioNex.Shared.LabwareDatabase;
using BioNex.Shared.LibraryInterfaces;
using BioNex.Shared.TaskListXMLParser;

namespace HitpickXMLReaderTests
{
    public class HitpickXMLReaderTests
    {
        private Reader _reader;

        [SetUp]
        public void Init()
        {
            ILabwareDatabase labware_database = new MockLabwareDatabase();
            _reader = new Reader( labware_database);
        }

        [Test]
        public void TestTransferUsableWells()
        {
            ILabware source_labware = new Labware( "source labware", 96, 14, 11);
            SourcePlate source_plate = new SourcePlate( source_labware, "S00001", null, null, null);
            ILabware dest_labware = new Labware( "dest labware", 96, 14, 11);
            DestinationPlate dest_plate = new DestinationPlate( dest_labware, "D00001", "B2:E4", null, null, null);
            Transfer transfer = new Transfer( source_plate, "A1", 1.0, VolumeUnits.ul,
                                              10.0, VolumeUnits.ul, "liquid class", dest_plate,
                                              new List<string> {"B2"}, null, null, 0, 0);
        }

        [Test]
        public void TestTransferUnusableWells()
        {
            ILabware source_labware = new Labware( "source labware", 96, 14, 11);
            SourcePlate source_plate = new SourcePlate( source_labware, "S00001", null, null, null);
            ILabware dest_labware = new Labware( "dest labware", 96, 14, 11);
            DestinationPlate dest_plate = new DestinationPlate( dest_labware, "D00001", "A10", null, null, null);
            try {
                Transfer transfer = new Transfer( source_plate, "A1", 1.0, VolumeUnits.ul,
                                                  10.0, VolumeUnits.ul, "liquid class", dest_plate,
                                                  new List<string> {"B2"}, null, null, 0, 0);
                Assert.Fail( "did not catch unusable well");
            } catch( Transfer.TransferMismatchException)
            {
                return;
            }
            Assert.Fail( "did not catch unusable well");
        }

        [Test]
        public void TestTransferOverview()
        {
            TransferOverview transfers = new TransferOverview();
            ILabware source_labware = new Labware( "source labware", 384, 14, 11);
            SourcePlate source_plate = new SourcePlate( source_labware, "S00001", null, null, null);
            transfers.SourcePlates.Add( source_plate);
            ILabware destination_labware = new Labware( "dest labware", 384, 14, 11);
            DestinationPlate dest_plate = new DestinationPlate( destination_labware, "D00001", "A1:D5", null, null, null);
            transfers.DestinationPlates.Add( dest_plate);
            transfers.AddTransfer( source_plate, "C5", dest_plate, "C2", 1.0, VolumeUnits.ul, null, 0, 0, "", "");
        }

        [Test]
        public void TestTransferOverviewBadTransfer()
        {
            TransferOverview transfers = new TransferOverview();
            ILabware source_labware = new Labware( "source labware", 384, 14, 11);
            SourcePlate source_plate = new SourcePlate( source_labware, "S00001", null, null, null);
            transfers.SourcePlates.Add( source_plate);
            ILabware dest_labware = new Labware( "dest labware", 384, 14, 11);
            DestinationPlate dest_plate = new DestinationPlate( dest_labware, "D00001", "A1:D5", null, null, null);
            transfers.DestinationPlates.Add( dest_plate);
            try {
                transfers.AddTransfer( source_plate, "C5", dest_plate, "E2", 1.0, VolumeUnits.ul, null, 0, 0, "", "");
                Assert.Fail( "did not catch unusable well");
            } catch( Transfer.TransferMismatchException)
            {
                return;
            }
            Assert.Fail( "did not catch unusable well");
        }

        [Test]
        public void TestHitpickFileReader()
        {

        }

        [Test]
        public void TestXMLFileMissing()
        {
            try {
                TransferOverview list = _reader.Read( @"..\..\test_doesnotexist.xml", @"..\..\test_doesnotexist.xsd");
            } catch( System.IO.FileNotFoundException) {
            }
        }
        [Test]
        public void TestXMLFileInvalid()
        {
            try {
                TransferOverview list = _reader.Read( @"..\..\test_invalid.xml", @"..\..\test_invalid.xsd");
            } catch( Exception) {
            }
        }

        [Test]
        public void TestLoadXMLNoSchema()
        {
            TransferOverview list = _reader.Read( @"..\..\test_hitpick.xml", "");
        }

        [Test]
        public void TestLoadXMLWithSchema()
        {
            TransferOverview list = _reader.Read( @"..\..\test_hitpick.xml", @"..\..\test_hitpick.xsd");
        }

        [Test]
        public void TestCatalogSourcePlates()
        {
            TransferOverview list = _reader.Read( @"..\..\test_hitpick.xml", @"..\..\test_hitpick.xsd");
            Assert.AreEqual( list.SourcePlates.Count, 2);
            foreach( KeyValuePair<string,Plate> kvp in list.SourcePlates) {
                if( kvp.Value.LabwareName == "source labware")
                    Assert.AreEqual( kvp.Value.Barcode, "S00001");
                else if( kvp.Value.LabwareName == "1")
                    Assert.AreEqual( kvp.Value.Barcode, "S00002");
            }
        }

        [Test]
        public void TestCatalogDestinationPlates()
        {
            TransferOverview list = _reader.Read( @"..\..\test_hitpick.xml", @"..\..\test_hitpick.xsd");
            Assert.AreEqual( list.DestinationPlates.Count, 2);
            foreach( KeyValuePair<string,Plate> kvp in list.DestinationPlates) {
                DestinationPlate dp = kvp.Value as DestinationPlate;
                if( dp == null)
                    Assert.Fail( "Plate should have been a DestinationPlate!");
                if( dp.LabwareName == "dest labware") {
                    Assert.AreEqual( dp.Barcode, "D00001");
                    Assert.AreEqual( dp.UsableWellsString, "A1:P24");
                } else if( dp.LabwareName == "1") {
                    Assert.AreEqual( dp.Barcode, "D00002");
                    Assert.AreEqual( dp.UsableWellsString, "A2,B5,C3,D6,E9");
                }
            }
        }

        [Test]
        public void TestTransfersFromXMLFile()
        {
            TransferOverview list = _reader.Read( @"..\..\test_hitpick.xml", @"..\..\test_hitpick.xsd");
            List<Transfer> transfers = list.Transfers;
            // test transfer 1 of 3
            foreach( Transfer t in transfers) {
                if( t.SourceWellName == "A1") {
                    Assert.AreEqual( t.Source.Barcode, "S00001");
                    Assert.AreEqual( t.DestinationWellNames[0], "any");
                    Assert.AreEqual( t.Destination.Barcode, "D00001");
                } else if( t.SourceWellName == "B6") {
                    Assert.AreEqual( t.Source.Barcode, "S00001");
                    Assert.AreEqual( t.DestinationWellNames[0], "any");
                    Assert.AreEqual( t.Destination.Barcode, "D00001");
                } else if( t.SourceWellName == "B5") {
                    Assert.AreEqual( t.Source.Barcode, "S00002");
                    Assert.AreEqual( t.DestinationWellNames[0], "B5");
                    Assert.AreEqual( t.Destination.Barcode, "D00002");
                }
            }
        }

        [Test]
        public void TestTransfersForDestinationPlate()
        {
            TransferOverview to = _reader.Read( @"..\..\test_hitpick.xml", null);
            List<Transfer> transfers = to.Transfers;
            List<Transfer> dest_transfers = to.GetTransfersForDestination( "D00001");
            Assert.AreEqual( 2, dest_transfers.Count);
            foreach( Transfer t in dest_transfers) {
                if( t.SourceWellName == "A1") {
                    Assert.AreEqual( t.Source.Barcode, "S00001");
                    Assert.AreEqual( t.DestinationWellNames[0], "any");
                    Assert.AreEqual( t.Destination.Barcode, "D00001");
                } else if( t.SourceWellName == "B6") {
                    Assert.AreEqual( t.Source.Barcode, "S00001");
                    Assert.AreEqual( t.DestinationWellNames[0], "any");
                    Assert.AreEqual( t.Destination.Barcode, "D00001");
                }
            }
        }

        [Test]
        public void TestSourceBarcodeComparer()
        {
            ILabware source_plate = new Labware( "", 96, 14, 11);
            ILabware dest_plate = new Labware( "", 96, 14, 11);
            Transfer t1 = new Transfer( new SourcePlate( source_plate, "S0001", null, null, null), "A1", 0, VolumeUnits.ul, 0, VolumeUnits.ul, "",
                                        new DestinationPlate( dest_plate, "D001", "any", null, null, null), new List<string> { "B1" }, null, null, 0, 0);
            Transfer t2 = new Transfer( new SourcePlate( source_plate, "S0002", null, null, null), "A2", 0, VolumeUnits.ul, 0, VolumeUnits.ul, "",
                                        new DestinationPlate( dest_plate, "D002", "any", null, null, null), new List<string> { "B2" }, null, null, 0, 0);
            Transfer t3 = new Transfer( new SourcePlate( source_plate, "S0002", null, null, null), "A2", 0, VolumeUnits.ul, 0, VolumeUnits.ul, "",
                                        new DestinationPlate( dest_plate, "D002", "any", null, null, null), new List<string> { "B2" }, null, null, 0, 0);
            Transfer t4 = new Transfer( new SourcePlate( source_plate, "S0003", null, null, null), "A3", 0, VolumeUnits.ul, 0, VolumeUnits.ul, "",
                                        new DestinationPlate( dest_plate, "D003", "any", null, null, null), new List<string> { "B3" }, null, null, 0, 0);
            Transfer t5 = new Transfer( new SourcePlate( source_plate, "S0003", null, null, null), "A3", 0, VolumeUnits.ul, 0, VolumeUnits.ul, "",
                                        new DestinationPlate( dest_plate, "D003", "any", null, null, null), new List<string> { "B3" }, null, null, 0, 0);
            Transfer t6 = new Transfer( new SourcePlate( source_plate, "S0004", null, null, null), "A4", 0, VolumeUnits.ul, 0, VolumeUnits.ul, "",
                                        new DestinationPlate( dest_plate, "D004", "any", null, null, null), new List<string> { "B4" }, null, null, 0, 0);
            List<Transfer> transfers = new List<Transfer>() { t1, t2, t3, t4, t5, t6 };
            var unique = new HashSet<Transfer>(transfers, new SourceBarcodeComparer());
            Assert.AreEqual( 4, unique.Count);
        }

        [Test]
        public void TestForInvalidUsableWellName()
        {
            try {
                TransferOverview list = _reader.Read( @"..\..\test_invalid_well.xml", @"..\..\test_hitpick.xsd");
            } catch( InvalidWellNameException) {
                return;
            }
            Assert.Fail( "Should have caught an invalid well, but didn't");
        }

        [Test]
        public void TestForInvalidSourceWellName()
        {
            try {
                TransferOverview list = _reader.Read( @"..\..\test_invalid_source_well.xml", @"..\..\test_hitpick.xsd");
            } catch( InvalidWellNameException) {
                return;
            }
            Assert.Fail( "Should have caught an invalid well, but didn't");
        }    

        [Test]
        public void TestForInvalidDestWellName()
        {
            try {
                TransferOverview list = _reader.Read( @"..\..\test_invalid_dest_well.xml", @"..\..\test_hitpick.xsd");
            } catch( InvalidWellNameException) {
                return;
            }
            Assert.Fail( "Should have caught an invalid well, but didn't");
        }        

        [Test]
        public void TestDefaultAspirateDispenseDistances()
        {
            TransferOverview list = _reader.Read( @"..\..\test_default_distances.xml", @"..\..\test_hitpick.xsd");
            foreach( Transfer t in list.Transfers) {
                Assert.AreEqual( 1, t.AspirateDistanceFromWellBottomMm);
                Assert.AreEqual( 2, t.DispenseDistanceFromWellBottomMm);
            }
        }

        [Test]
        public void TestOverriddenAspirateDispenseDistances()
        {
            TransferOverview list = _reader.Read( @"..\..\test_overridden_distances.xml", @"..\..\test_hitpick.xsd");
            foreach( Transfer t in list.Transfers) {
                Assert.AreEqual( 3, t.AspirateDistanceFromWellBottomMm);
                Assert.AreEqual( 4, t.DispenseDistanceFromWellBottomMm);
            }
        }

        [Test]
        public void TestOverriddenTaskList()
        {
            TransferOverview list = _reader.Read( @"..\..\..\..\TaskListXMLParser\TaskListXMLParserTests\sample_hitpick.xml", @"..\..\test_hitpick.xsd");
            // check source plate override
            Plate source = list.SourcePlates["source1"];
            Assert.AreEqual( 1, source.PreHitpickTasks.Count());
            Assert.AreEqual( 0, source.PostHitpickTasks.Count());
            PlateTask task = source.PreHitpickTasks[0];
            Assert.AreEqual( "PlatePierce", task.DeviceInstance);
            Assert.AreEqual( "Pierce", task.Command);
            Assert.AreEqual( 1, task.ParametersAndVariables.Count());
            PlateTask.Parameter p = task.ParametersAndVariables[0];
            Assert.AreEqual( "pierce_time_seconds", p.Name);
            Assert.AreEqual( "5", p.Value);
            Assert.AreEqual( "", p.Variable);
            // check dest plate override
            Plate dest = list.DestinationPlates["dest10"];
            Assert.AreEqual( 0, dest.PreHitpickTasks.Count());
            Assert.AreEqual( 1, dest.PostHitpickTasks.Count());
            task = dest.PostHitpickTasks[0];
            Assert.AreEqual( "WellMate", task.DeviceInstance);
            Assert.AreEqual( "Fill", task.Command);
            Assert.AreEqual( 2, task.ParametersAndVariables.Count());
            PlateTask.Parameter p1 = task.ParametersAndVariables[0];
            Assert.AreEqual( "volume_ul", p1.Name);
            Assert.AreEqual( "10", p1.Value);
            Assert.AreEqual( "$volume_ul_other", p1.Variable);
            PlateTask.Parameter p2 = task.ParametersAndVariables[1];
            Assert.AreEqual( "columns_to_dispense", p2.Name);
            Assert.AreEqual( "", p2.Value);
            Assert.AreEqual( "", p2.Variable);
        }

        [Test]
        public void TestVariables()
        {
            TransferOverview list = _reader.Read( @"..\..\..\..\TaskListXMLParser\TaskListXMLParserTests\sample_hitpick.xml", @"..\..\test_hitpick.xsd");
            Plate plate = list.DestinationPlates["dest2"];
            Assert.AreEqual( 1, plate.Variables.Count());
            Assert.AreEqual( "$volume_ul", plate.Variables[0].Key);
            Assert.AreEqual( "20", plate.Variables[0].Value);
            plate = list.DestinationPlates["dest10"];
            Assert.AreEqual( 1, plate.Variables.Count());
            Assert.AreEqual( "$volume_ul_other", plate.Variables[0].Key);
            Assert.AreEqual( "30", plate.Variables[0].Value);
        }
    }

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
            return new Labware( "mock labware", 96, 14, 12);
        }

        public List<ILabware> GetCompatibleTips( ILabware labware)
        {
            return new List<ILabware>();
        }

        public void ShowEditor() {}

        public bool IsValidLabwareName(string labware_name)
        {
            throw new NotImplementedException();
        }

        public List<ILabwareProperty> GetLabwareProperties()
        {
            throw new NotImplementedException();
        }

        #endregion


        public void ReloadLabware()
        {
            throw new NotImplementedException();
        }

        public long UpdateLabware(ILabware labware)
        {
            throw new NotImplementedException();
        }

        public long AddLabware(ILabware labware)
        {
            throw new NotImplementedException();
        }

        public long CloneLabware(ILabware labware, string new_name)
        {
            throw new NotImplementedException();
        }
    }
}
