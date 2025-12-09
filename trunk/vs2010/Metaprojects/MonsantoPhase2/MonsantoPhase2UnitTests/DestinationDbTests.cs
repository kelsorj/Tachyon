using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BioNex.CustomerGUIPlugins
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class DestinationDbTests
    {
        public DestinationDbTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestCreateDatabase()
        {
            DestinationProcessing db = new DestinationProcessing( "destination_processing_db.s3db");
        }

        [TestMethod]
        public void TestSaveWork()
        {
            DestinationProcessing db = new DestinationProcessing( "destination_processing_db.s3db");
            db.SaveWork( "dest001", new BioNex.GemsRpc.TransferMap { source_barcode="source001", source_row=0, source_column=1, destination_row=2, destination_column=3, transfer_volume=4, sensed_volume=5 });
            string dest_barcode;
            BioNex.GemsRpc.TransferMap mapping;
            Assert.IsTrue( db.GetNextWorkItem( out dest_barcode, out mapping));
            Assert.AreEqual( "dest001", dest_barcode);
            Assert.AreEqual( "source001", mapping.source_barcode);
            Assert.AreEqual( 0, mapping.source_row);
            Assert.AreEqual( 1, mapping.source_column);
            Assert.AreEqual( 2, mapping.destination_row);
            Assert.AreEqual( 3, mapping.destination_column);
            Assert.AreEqual( 4, mapping.transfer_volume);
            Assert.AreEqual( 5, mapping.sensed_volume);
        }

        [TestMethod]
        public void TestDeleteWork()
        {
            DestinationProcessing db = new DestinationProcessing( "destination_processing_db.s3db");
            db.SaveWork( "dest001", new BioNex.GemsRpc.TransferMap { source_barcode="source001", source_row=0, source_column=1, destination_row=2, destination_column=3, transfer_volume=4, sensed_volume=5 });
            db.DeleteWork( "dest001");
            string dest_barcode;
            BioNex.GemsRpc.TransferMap mapping;
            Assert.IsFalse( db.GetNextWorkItem( out dest_barcode, out mapping));
        }

        [TestMethod]
        public void TestFailureToSendData()
        {
            DestinationProcessing db = new DestinationProcessing( "destination_processing_db.s3db");
            db.SaveWork( "dest001", new BioNex.GemsRpc.TransferMap { source_barcode="source001", source_row=0, source_column=1, destination_row=2, destination_column=3, transfer_volume=4, sensed_volume=5 });
            string dest_barcode;
            BioNex.GemsRpc.TransferMap mapping;
            Assert.IsTrue( db.GetNextWorkItem( out dest_barcode, out mapping));
            Assert.AreEqual( "dest001", dest_barcode);
            Assert.AreEqual( "source001", mapping.source_barcode);
            Assert.AreEqual( 0, mapping.source_row);
            Assert.AreEqual( 1, mapping.source_column);
            Assert.AreEqual( 2, mapping.destination_row);
            Assert.AreEqual( 3, mapping.destination_column);
            Assert.AreEqual( 4, mapping.transfer_volume);
            Assert.AreEqual( 5, mapping.sensed_volume);
            // now we should be able to pull the same data again, which simulates a failure to talk to GEMS
            Assert.IsTrue( db.GetNextWorkItem( out dest_barcode, out mapping));
            Assert.AreEqual( "dest001", dest_barcode);
            Assert.AreEqual( "source001", mapping.source_barcode);
            Assert.AreEqual( 0, mapping.source_row);
            Assert.AreEqual( 1, mapping.source_column);
            Assert.AreEqual( 2, mapping.destination_row);
            Assert.AreEqual( 3, mapping.destination_column);
            Assert.AreEqual( 4, mapping.transfer_volume);
            Assert.AreEqual( 5, mapping.sensed_volume);
        }
    }
}
