using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

namespace BioNex.Shared.Microscan
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class MicroscanTests
    {
        public MicroscanTests()
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
        public void TestSaveConfigurationDatabaseXML()
        {

        }

        [TestMethod]
        public void TestLoadConfigurationDatabaseXML()
        {

        }

        [TestMethod]
        public void TestLoadConfigurationDatabase()
        {
            /*
            string response = @"<K255,1,60,20,400,0,0,0,1536,2048,0,128,3,1,0,0><K255,2,60,20,400,0,0,0,1536,2048,0,128,3,1,0,0><K255,3,60,20,400,0,0,0,1536,2048,0,128,3,1,0,0><K255,4,60,20,400,0,0,0,1536,2048,0,128,3,1,0,0><K255,5,60,20,400,0,0,0,1536,2048,0,128,3,1,0,0><K255,6,60,20,400,0,0,0,1536,2048,0,128,3,1,0,0><K255,7,60,20,400,0,0,0,1536,2048,0,128,3,1,0,0><K255,8,60,20,400,0,0,0,1536,2048,0,128,3,1,0,0><K255,9,60,20,400,0,0,0,1536,2048,0,128,3,1,0,0><K255,10,50,10,200,1,2,3,480,640,2,3,4,5,6,7>";
            Microscan.MicroscanReader.ConfigurationDatabase db = new MicroscanReader.ConfigurationDatabase();
            db.LoadFromReaderResponseString( response);
            Assert.AreEqual( 10, db.Configurations.Count());
            // test the individual capture indexes
            // capture index #1
            Microscan.MicroscanReader.ConfigurationDatabase.ConfigurationDatabaseIndex capture_index = db.Configurations[0];
            Assert.AreEqual( 1, capture_index.Index);
            Assert.AreEqual( 60, capture_index.ShutterSpeed);
            Assert.AreEqual( 20, capture_index.Gain);
            Assert.AreEqual( 4.00, capture_index.FocalDistanceInches);
            Assert.AreEqual( 0, capture_index.SubSampling);
            Assert.AreEqual( 0, capture_index.WOI.RowPointer);
            Assert.AreEqual( 0, capture_index.WOI.ColumnPointer);
            Assert.AreEqual( 1536, capture_index.WOI.RowDepth);
            Assert.AreEqual( 2048, capture_index.WOI.ColumnWidth);
            Assert.AreEqual( 0, capture_index.ThresholdMode);
            Assert.AreEqual( 128, capture_index.FixedThresholdValue);
            Assert.AreEqual( 3, capture_index.ProcessingMode);
            Assert.AreEqual( 1, capture_index.NarrowMargins);
            Assert.AreEqual( 0, capture_index.BackgroundColor);
            Assert.AreEqual( 0, capture_index.Symbologies);
            // capture index #10
            capture_index = db.Configurations[9];
            Assert.AreEqual( 10, capture_index.Index);
            Assert.AreEqual( 50, capture_index.ShutterSpeed);
            Assert.AreEqual( 10, capture_index.Gain);
            Assert.AreEqual( 2.00, capture_index.FocalDistanceInches);
            Assert.AreEqual( 1, capture_index.SubSampling);
            Assert.AreEqual( 2, capture_index.WOI.RowPointer);
            Assert.AreEqual( 3, capture_index.WOI.ColumnPointer);
            Assert.AreEqual( 480, capture_index.WOI.RowDepth);
            Assert.AreEqual( 640, capture_index.WOI.ColumnWidth);
            Assert.AreEqual( 2, capture_index.ThresholdMode);
            Assert.AreEqual( 3, capture_index.FixedThresholdValue);
            Assert.AreEqual( 4, capture_index.ProcessingMode);
            Assert.AreEqual( 5, capture_index.NarrowMargins);
            Assert.AreEqual( 6, capture_index.BackgroundColor);
            Assert.AreEqual( 7, capture_index.Symbologies);
             */
        }

        [TestMethod]
        public void TestParseRegEx()
        {
            string sample_config = @"<K?328><K100,8,0,0,1><K101,0,8,0,0,1,0,1/><K102,1><K140,0,1><K141,0,^M><K142,1,^M^J><K143,12><K145,0><K146,0><K147,^@,^@,^@,^@,^F,^U><K148,^D,^E,^B,^C,^F,^U><K200,5,92,92><K201, ><K202,1><K220,2,35><K221,1><K222,1,,><K223,0,0,0,1,*,1,0><K224,1><K225,0><K229,00><K230,00><K231,1,><K241,0,1,1><K242,0,0,0,0,0,0,0,0><K244,1,0><K245,250><K252,0,0><K255,1,60,20,400,0,0,0,1536,2048,0,128,4,1,0,0><K255,2,60,20,400,0,0,0,1536,2048,0,128,4,1,0,0><K255,3,60,20,400,0,0,0,1536,2048,0,128,4,1,0,0><K255,4,60,20,400,0,0,0,1536,2048,0,128,4,1,0,0><K255,5,60,20,400,0,0,0,1536,2048,0,128,4,1,0,0><K255,6,60,20,400,0,0,0,1536,2048,0,128,4,1,0,0><K255,7,60,20,400,0,0,0,1536,2048,0,128,4,1,0,0><K255,8,60,20,400,0,0,0,1536,2048,0,128,4,1,0,0><K255,9,60,20,400,0,0,0,1536,2048,0,128,4,1,0,0><K255,10,60,20,400,0,0,0,1536,2048,0,128,4,1,0,0><K256,1,1,0,0><K406,2,0,14,1><K407,23,24><K409,0,SERVICE,300,0><K450,1,0><K451,0><K453,0,0,,><K458,0><K459,0><K460,0,1,1,1,0,0><K470,0,0,0,0,0,10,0><K471,0,1,1,0,0,10,0,0><K472,0,0,0,16,6,0,1><K473,0,1,0,0,,,0,0><K474,1,0,10,0,0,0,,,0,1><K475,0,0,10><K476,0,0,0,1,0,0><K477,0,0,5,4,0,0,10><K479,0,0,0,0,0,0,0,0><K480,0><K481,0,0,0,10><K482,0><K483,0><K484,0,0,14><K485,0,0,0,10><K514,0><K516,347,869,1056,602><K525,535><K529,1,2,1,0,0,75,64,2><K536,3><K537,0,0><K541,125,42><K542,0><K543,0><K550,0><K551,0,0,3><K701,0,0,0><K702,1><K704,,,0><K705,3,0><K706,0, ><K708, ,0><K709,0,0,0,0><K710,0,0,0,0,0,0,0,0,0,0><K714,1,NOREAD><K734,0,0><K735,0,MATCH><K736,0,MISMATCH><K737,0,0><K739,0,0,1,90><K740,1,0,0><K740,2,0,0><K740,3,0,0><K740,4,0,0><K740,5,0,0><K740,6,0,0><K740,7,0,0><K740,8,0,0><K740,9,0,0><K740,10,0,0><K740,11,0,0><K740,12,0,0><K740,13,0,0><K740,14,0,0><K740,15,0,0><K740,16,0,0><K740,17,0,0><K740,18,0,0><K740,19,0,0><K740,20,0,0><K740,21,0,0><K740,22,0,0><K740,23,0,0><K740,24,0,0><K740,25,0,0><K740,26,0,0><K740,27,0,0><K740,28,0,0><K740,29,0,0><K740,30,0,0><K740,31,0,0><K740,32,0,0><K740,33,0,0><K740,34,0,0><K740,35,0,0><K740,36,0,0><K740,37,0,0><K740,38,0,0><K740,39,0,0><K740,40,0,0><K740,41,0,0><K740,42,0,0><K740,43,0,0><K740,44,0,0><K740,45,0,0><K740,46,0,0><K740,47,0,0><K740,48,0,0><K740,49,0,0><K740,50,0,0><K740,51,0,0><K740,52,0,0><K740,53,0,0><K740,54,0,0><K740,55,0,0><K740,56,0,0><K740,57,0,0><K740,58,0,0><K740,59,0,0><K740,60,0,0><K740,61,0,0><K740,62,0,0><K740,63,0,0><K740,64,0,0><K740,65,0,0><K740,66,0,0><K740,67,0,0><K740,68,0,0><K740,69,0,0><K740,70,0,0><K740,71,0,0><K740,72,0,0><K740,73,0,0><K740,74,0,0><K740,75,0,0><K740,76,0,0><K740,77,0,0><K740,78,0,0><K740,79,0,0><K740,80,0,0><K740,81,0,0><K740,82,0,0><K740,83,0,0><K740,84,0,0><K740,85,0,0><K740,86,0,0><K740,87,0,0><K740,88,0,0><K740,89,0,0><K740,90,0,0><K740,91,0,0><K740,92,0,0><K740,93,0,0><K740,94,0,0><K740,95,0,0><K740,96,0,0><K740,97,0,0><K740,98,0,0><K740,99,0,0><K740,100,0,0><K741,1,0,00><K741,2,0,00><K741,3,0,00><K741,4,0,00><K741,5,0,00><K741,6,0,00><K741,7,0,00><K741,8,0,00><K741,9,0,00><K741,10,0,00><K741,11,0,00><K741,12,0,00><K741,13,0,00><K741,14,0,00><K741,15,0,00><K741,16,0,00><K741,17,0,00><K741,18,0,00><K741,19,0,00><K741,20,0,00><K741,21,0,00><K741,22,0,00><K741,23,0,00><K741,24,0,00><K741,25,0,00><K741,26,0,00><K741,27,0,00><K741,28,0,00><K741,29,0,00><K741,30,0,00><K741,31,0,00><K741,32,0,00><K741,33,0,00><K741,34,0,00><K741,35,0,00><K741,36,0,00><K741,37,0,00><K741,38,0,00><K741,39,0,00><K741,40,0,00><K741,41,0,00><K741,42,0,00><K741,43,0,00><K741,44,0,00><K741,45,0,00><K741,46,0,00><K741,47,0,00><K741,48,0,00><K741,49,0,00><K741,50,0,00><K741,51,0,00><K741,52,0,00><K741,53,0,00><K741,54,0,00><K741,55,0,00><K741,56,0,00><K741,57,0,00><K741,58,0,00><K741,59,0,00><K741,60,0,00><K741,61,0,00><K741,62,0,00><K741,63,0,00><K741,64,0,00><K741,65,0,00><K741,66,0,00><K741,67,0,00><K741,68,0,00><K741,69,0,00><K741,70,0,00><K741,71,0,00><K741,72,0,00><K741,73,0,00><K741,74,0,00><K741,75,0,00><K741,76,0,00><K741,77,0,00><K741,78,0,00><K741,79,0,00><K741,80,0,00><K741,81,0,00><K741,82,0,00><K741,83,0,00><K741,84,0,00><K741,85,0,00><K741,86,0,00><K741,87,0,00><K741,88,0,00><K741,89,0,00><K741,90,0,00><K741,91,0,00><K741,92,0,00><K741,93,0,00><K741,94,0,00><K741,95,0,00><K741,96,0,00><K741,97,0,00><K741,98,0,00><K741,99,0,00><K741,100,0,00><K742,1,0><K742,2,0><K742,3,0><K742,4,0><K742,5,0><K742,6,0><K742,7,0><K742,8,0><K742,9,0><K742,10,0><K743,0><K744,1,0,0,2A,3F,00,0,0><K744,2,0,0,2A,3F,00,0,0><K744,3,0,0,2A,3F,00,0,0><K744,4,0,0,2A,3F,00,0,0><K744,5,0,0,2A,3F,00,0,0><K744,6,0,0,2A,3F,00,0,0><K744,7,0,0,2A,3F,00,0,0><K744,8,0,0,2A,3F,00,0,0><K744,9,0,0,2A,3F,00,0,0><K744,10,0,0,2A,3F,00,0,0><K745,0><K750,1,3,20><K757,0,0,0,1,90,0,0><K759,0, ><K770,1,1,1,0><K771,9,2,1,3><K780,1,0,0,0><K781,1,0,0,0><K782,1,0,0,0><K790,0,0><K791,0,0><K792,0,0><K800,0,2,0,2,0,2,0,2><K801,0,2,0,2,0,2,0,2><K802,0,2,0,2,0,2,0,2><K810,0,0,5,0><K811,0,0,5,0><K812,0,0,5,0>";
            int value = int.Parse( MicroscanReader.ParseConfigurationSetting( sample_config, "K102", 1)[0]);
            Assert.AreEqual( 1, value);
            string[] values = MicroscanReader.ParseConfigurationSetting( sample_config, "K100", 4);
            Assert.AreEqual( 8, int.Parse( values[0]));
            Assert.AreEqual( 0, int.Parse( values[1]));
            Assert.AreEqual( 0, int.Parse( values[2]));
            Assert.AreEqual( 1, int.Parse( values[3]));
        }

        [TestMethod]
        public void TestParseFilenames()
        {
            // ---------------------
            // test #1 -- many files
            // should be ignored --------------vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv
            string filelisting1 = @"<op,009,0,/noread/0/640x480_gs.bmp 00018 00037,/saved/noread/0/640x480_gs.bmp 00015 00037,/saved/noread/1/320x240_gs.bmp 00016 00038,/saved/noread/2/160x120_gs.bmp 00017 00039,/saved/noread/3/80x60_gs.bmp 00018 00040>";
            List<MicroscanFilename> filenames = MicroscanReader.ParseImageFilenames( filelisting1);
            // should be 4 files in the saved folder
            Assert.AreEqual( 4, filenames.Count());
            // file #1
            MicroscanFilename f = filenames[0];
            Assert.AreEqual( "/saved/noread/0/", f.Folder);
            Assert.AreEqual( "640x480_gs.bmp", f.Filename);
            Assert.AreEqual( 640, f.Width);
            Assert.AreEqual( 480, f.Height);
            Assert.AreEqual( 15, f.ImageId);
            Assert.AreEqual( 37, f.Age);
            // file #2


            // ---------------------
            // test #2 -- no files
            string filelisting2 = @"<op,009,0,/noread/0/640x480_gs.bmp 00018 00037>";
            filenames = MicroscanReader.ParseImageFilenames( filelisting2);
            // should be 0 files in the saved folder
            Assert.AreEqual( filenames.Count(), 0);
        }

        [TestMethod]
        public void TestDecodeSettingsEqual()
        {
            MicroscanReader.DecodeSettings lhs = new MicroscanReader.DecodeSettings { 
                BackgroundColor = 0, FixedThresholdValue = 1, FocalDistanceInches = 2, Gain = 3, LineSpeed = 4,
                NarrowMargins = 5, ProcessingMode = 6, ShutterSpeed = 7, SubSampling = 8, Symbologies = 9,
                ThresholdMode = 10, WOI = new MicroscanReader.WindowOfInterest( 11, 12, 13, 14) };
            MicroscanReader.DecodeSettings rhs = new MicroscanReader.DecodeSettings {
                BackgroundColor = 0, FixedThresholdValue = 1, FocalDistanceInches = 2, Gain = 3, LineSpeed = 4,
                NarrowMargins = 5, ProcessingMode = 6, ShutterSpeed = 7, SubSampling = 8, Symbologies = 9,
                ThresholdMode = 10, WOI = new MicroscanReader.WindowOfInterest( 11, 12, 13, 14) };

            Assert.AreEqual( lhs, rhs);
        }
    }
}
