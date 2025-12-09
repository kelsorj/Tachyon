using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BioNex.Shared.Utils
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class UtilsTests
    {
        public UtilsTests()
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

        /* move these unit tests into platedefs:
        Dictionary<string,Wells.WellUsageStates> _well_usage_map_96;
        Dictionary<string,Wells.WellUsageStates> _well_usage_map_384;

        [TestInitialize]
        public void Init()
        {
            _well_usage_map_96 = new Dictionary<string,Wells.WellUsageStates>();
            _well_usage_map_384 = new Dictionary<string,Wells.WellUsageStates>();
        }

        [TestMethod]
        public void TestRowColumnToWellName()
        {
            // 48 well plate labeling is just like 96 well plate
            Assert.AreEqual( Wells.RowColumnToWellName( 0, 0), "A1");
            Assert.AreEqual( Wells.RowColumnToWellName( 16, 15), "Q16");
            Assert.AreEqual( Wells.RowColumnToWellName( 28, 44), "CC45");
        }

        [TestMethod]
        public void TestWellNameToRowColumn()
        {
            int row, col;
            // 48 well plate labeling is just like 96 well plate
            // 96
            Wells.WellNameToRowColumn( "A1", out row, out col);
            Assert.AreEqual( 0, row);
            Assert.AreEqual( 0, col);
            Wells.WellNameToRowColumn( "a1", out row, out col);
            Assert.AreEqual( 0, row);
            Assert.AreEqual( 0, col);
            Wells.WellNameToRowColumn( "H12", out row, out col);
            Assert.AreEqual( 7, row);
            Assert.AreEqual( 11, col);
            Wells.WellNameToRowColumn( "h12", out row, out col);
            Assert.AreEqual( 7, row);
            Assert.AreEqual( 11, col);
            // 384
            Wells.WellNameToRowColumn( "A14", out row, out col);
            Assert.AreEqual( 0, row);
            Assert.AreEqual( 13, col);
            Wells.WellNameToRowColumn( "a14", out row, out col);
            Assert.AreEqual( 0, row);
            Assert.AreEqual( 13, col);
            Wells.WellNameToRowColumn( "P24", out row, out col);
            Assert.AreEqual( 15, row);
            Assert.AreEqual( 23, col);
            Wells.WellNameToRowColumn( "p24", out row, out col);
            Assert.AreEqual( 15, row);
            Assert.AreEqual( 23, col);
            // 1536
            Wells.WellNameToRowColumn( "AA36", out row, out col);
            Assert.AreEqual( 26, row);
            Assert.AreEqual( 35, col);
            Wells.WellNameToRowColumn( "aa36", out row, out col);
            Assert.AreEqual( 26, row);
            Assert.AreEqual( 35, col);
        }

        [TestMethod]
        public void TestExtractWellNamesFromDestinationValue()
        {
            List<string> result = Wells.ExtractWellNamesFromDestinationValue( "A1,B2,C3");
            Assert.AreEqual( result.Count, 3);
            Assert.AreEqual( result[0], "A1");
            Assert.AreEqual( result[1], "B2");
            Assert.AreEqual( result[2], "C3");
            // test spacing sensitivity
            result = Wells.ExtractWellNamesFromDestinationValue( "  D4, E5 , F6");
            Assert.AreEqual( result.Count, 3);
            Assert.AreEqual( result[0], "D4");
            Assert.AreEqual( result[1], "E5");
            Assert.AreEqual( result[2], "F6");
        }

        [TestMethod]
        public void TestExtractRanges()
        {
            List<string> result = Wells.ExtractWellNamesFromDestinationValue( "C1:E5");
            Assert.AreEqual( result.Count, 15);
            CollectionAssert.Contains( result, "C1");
            CollectionAssert.Contains( result, "C2");
            CollectionAssert.Contains( result, "C3");
            CollectionAssert.Contains( result, "C4");
            CollectionAssert.Contains( result, "C5");
            CollectionAssert.Contains( result, "D1");
            CollectionAssert.Contains( result, "D2");
            CollectionAssert.Contains( result, "D3");
            CollectionAssert.Contains( result, "D4");
            CollectionAssert.Contains( result, "D5");
            CollectionAssert.Contains( result, "E1");
            CollectionAssert.Contains( result, "E2");
            CollectionAssert.Contains( result, "E3");
            CollectionAssert.Contains( result, "E4");
            CollectionAssert.Contains( result, "E5");
        }

        [TestMethod]
        public void TestAddRange()
        {
            List<string> wells = new List<string>();
            //! \todo make this a friend class of Utils
            Wells.AddRange( wells, "A1:c5");
            Assert.AreEqual( wells.Count, 15);
            Assert.IsTrue( wells.Contains( "a1", new Wells.WellNameComparer()));
            Assert.IsTrue( wells.Contains( "a2", new Wells.WellNameComparer()));
            Assert.IsTrue( wells.Contains( "a3", new Wells.WellNameComparer()));
            Assert.IsTrue( wells.Contains( "a4", new Wells.WellNameComparer()));
            Assert.IsTrue( wells.Contains( "a5", new Wells.WellNameComparer()));
            CollectionAssert.Contains( wells, "B1");
            CollectionAssert.Contains( wells, "B2");
            CollectionAssert.Contains( wells, "B3");
            CollectionAssert.Contains( wells, "B4");
            CollectionAssert.Contains( wells, "B5");
            CollectionAssert.Contains( wells, "C1");
            CollectionAssert.Contains( wells, "C2");
            CollectionAssert.Contains( wells, "C3");
            CollectionAssert.Contains( wells, "C4");
            CollectionAssert.Contains( wells, "C5");
            Assert.IsFalse( wells.Contains( "d5"));
            Assert.IsFalse( wells.Contains( "d3"));
        }

        [TestMethod]
        public void TestWellNameComparer()
        {
            List<string> names = new List<string>( new string[] { "A", "b" } );
            Assert.IsTrue( names.Contains( "a", new Wells.WellNameComparer()));
            Assert.IsTrue( names.Contains( "B", new Wells.WellNameComparer()));
        }

        [TestMethod]
        public void TestInvalidWellNames()
        {
            // 48
            Assert.IsTrue( Wells.IsWellNameValid( "A1", 48));
            Assert.IsTrue( Wells.IsWellNameValid( "A8", 48));
            Assert.IsTrue( Wells.IsWellNameValid( "F1", 48));
            Assert.IsTrue( Wells.IsWellNameValid( "F8", 48));
            Assert.IsFalse( Wells.IsWellNameValid( "A9", 48));
            Assert.IsFalse( Wells.IsWellNameValid( "B9", 48));
            Assert.IsFalse( Wells.IsWellNameValid( "B9", 48));
            Assert.IsFalse( Wells.IsWellNameValid( "G1", 48));
            Assert.IsFalse( Wells.IsWellNameValid( "G9", 48));
            // 96
            Assert.IsTrue( Wells.IsWellNameValid( "A1", 96));
            Assert.IsTrue( Wells.IsWellNameValid( "H1", 96));
            Assert.IsTrue( Wells.IsWellNameValid( "A12", 96));
            Assert.IsTrue( Wells.IsWellNameValid( "H12", 96));
            Assert.IsFalse( Wells.IsWellNameValid( "A14", 96));
            Assert.IsFalse( Wells.IsWellNameValid( "I1", 96));
            Assert.IsFalse( Wells.IsWellNameValid( "I14", 96));
            // 384
            Assert.IsTrue( Wells.IsWellNameValid( "A1", 384));
            Assert.IsTrue( Wells.IsWellNameValid( "A24", 384));
            Assert.IsTrue( Wells.IsWellNameValid( "P1", 384));
            Assert.IsTrue( Wells.IsWellNameValid( "P24", 384));
            Assert.IsFalse( Wells.IsWellNameValid( "A25", 384));
            Assert.IsFalse( Wells.IsWellNameValid( "Q1", 384));
            Assert.IsFalse( Wells.IsWellNameValid( "Q25", 384));
            // 1536
            Assert.IsTrue( Wells.IsWellNameValid( "A1", 1536));
            Assert.IsTrue( Wells.IsWellNameValid( "A48", 1536));
            Assert.IsTrue( Wells.IsWellNameValid( "FF1", 1536));
            Assert.IsTrue( Wells.IsWellNameValid( "FF48", 1536));
            Assert.IsFalse( Wells.IsWellNameValid( "A49", 1536));
            Assert.IsFalse( Wells.IsWellNameValid( "GG1", 1536));
            Assert.IsFalse( Wells.IsWellNameValid( "GG49", 1536));
        }

        [TestMethod]
        public void TestA1Rotations96WellPlateSimple()
        {
            double a1_x = -49.5;
            double a1_y = 31.5;
            double new_x, new_y;
            // 90 CCW from 0
            Wells.GetXYAfterRotation( a1_x, a1_y, 90, false, out new_x, out new_y);
            Assert.IsTrue( Math.Abs( new_x + 31.5) <= 0.01);
            Assert.IsTrue( Math.Abs( new_y + 49.5) <= 0.01);
            // 180 CCW from 0
            Wells.GetXYAfterRotation( a1_x, a1_y, 180, false, out new_x, out new_y);
            Assert.IsTrue( Math.Abs( new_x - 49.5) <= 0.01);
            Assert.IsTrue( Math.Abs( new_y + 31.5) <= 0.01);
            // 270 CCW from 0
            Wells.GetXYAfterRotation( a1_x, a1_y, 270, false, out new_x, out new_y);
            Assert.IsTrue( Math.Abs( new_x - 31.5) <= 0.01);
            Assert.IsTrue( Math.Abs( new_y - 49.5) <= 0.01);
            // 90 CW from 0 -- should be same as 270 CCW
            Wells.GetXYAfterRotation( a1_x, a1_y, 90, true, out new_x, out new_y);
            Assert.IsTrue( Math.Abs( new_x - 31.5) <= 0.01);
            Assert.IsTrue( Math.Abs( new_y - 49.5) <= 0.01);
            // 180 CW from 0 -- should be same as 180 CCW
            Wells.GetXYAfterRotation( a1_x, a1_y, 180, true, out new_x, out new_y);
            Assert.IsTrue( Math.Abs( new_x - 49.5) <= 0.01);
            Assert.IsTrue( Math.Abs( new_y + 31.5) <= 0.01);
            // 270 CW from 0 -- should be same as 90 CCW
            Wells.GetXYAfterRotation( a1_x, a1_y, 270, true, out new_x, out new_y);
            Assert.IsTrue( Math.Abs( new_x + 31.5) <= 0.01);
            Assert.IsTrue( Math.Abs( new_y + 49.5) <= 0.01);
        }

        [TestMethod]
        public void TestA1Rotations96WellPlateHarder()
        {
            double a1_x = -49.5;
            double a1_y = 31.5;
            double new_x, new_y;
            // 30.06 CCW from 0 -- chose this angle because this case failed during hardware testing so added unit test for it just in case
            Wells.GetXYAfterRotation( a1_x, a1_y, 30.06, false, out new_x, out new_y);
            Assert.IsTrue( Math.Abs( new_x + 58.6209) <= 0.01);
            Assert.IsTrue( Math.Abs( new_y - 2.4684) <= 0.01);
        }

        [TestMethod]
        public void TestA1DistanceFromCenterOfPlate()
        {
            double x, y;
            // 96
            Wells.GetA1DistanceFromCenterOfPlate( 96, out x, out y);
            Assert.AreEqual( -49.5, x);
            Assert.AreEqual( 31.5, y);
            // 384
            Wells.GetA1DistanceFromCenterOfPlate( 384, out x, out y);
            Assert.IsTrue( Math.Abs( -51.75 - x) <= 0.01);
            Assert.IsTrue( Math.Abs( 33.75 - y) <= 0.01);
            // 1536
            Wells.GetA1DistanceFromCenterOfPlate( 1536, out x, out y);
            Assert.IsTrue( Math.Abs( -52.875 - x) <= 0.01);
            Assert.IsTrue( Math.Abs( 34.875 - y) <= 0.01);
        }

        [TestMethod]
        public void TestGetWellDistanceFromCenterOfPlate()
        {
            double x = 0;
            double y = 0;
            // 96
            Wells.GetWellDistanceFromCenterOfPlate( "D6", 96, out x, out y);
            Assert.AreEqual( -4.5, x);
            Assert.AreEqual( 4.5, y);
            // this one failed during a hardware test so added unit test for it just in case
            Wells.GetWellDistanceFromCenterOfPlate( "A12", 96, out x, out y);
            Assert.AreEqual( 5.5 * 9, x);
            Assert.AreEqual( 3.5 * 9, y);
        }

        #region tip1 = 1, tip2 = 2
        
        [TestMethod]
        public void TestDualTipAngleA1D4()
        {
            byte tip1_id = 1;
            byte tip2_id = 2;
            double tip_spacing = 18;
            string well1 = "A1";
            string well2 = "D4";
            int number_of_wells = 96;
            double angle;
            bool result = Wells.GetAngleForTwoTips( well1, well2, tip1_id, tip2_id, tip_spacing, number_of_wells, out angle);
            Assert.IsTrue( result);
            // from Octave, the answer should be either -41.295 or 156.35
            bool matches1 = Math.Abs( angle - (-106.87)) < 0.01;
            bool matches2 = Math.Abs( angle - 16.874) < 0.01;
            Assert.IsTrue( matches1 || matches2);
        }

        [TestMethod]
        public void TestDualTipAngleH1E6()
        {
            byte tip1_id = 1;
            byte tip2_id = 2;
            double tip_spacing = 18;
            string well1 = "H1";
            string well2 = "E6";
            int number_of_wells = 96;
            double angle;
            bool result = Wells.GetAngleForTwoTips( well1, well2, tip1_id, tip2_id, tip_spacing, number_of_wells, out angle);
            Assert.IsTrue( result);
            // from Octave, the answer should be either -41.295 or 156.35
            bool matches1 = Math.Abs( angle - 169.10) < 0.01;
            bool matches2 = Math.Abs( angle - (-51.023)) < 0.01;
            Assert.IsTrue( matches1 || matches2);
        }
    
        [TestMethod]
        public void TestDualTipAngleH12E7()
        {
            byte tip1_id = 1;
            byte tip2_id = 2;
            double tip_spacing = 18;
            string well1 = "H12";
            string well2 = "E7";
            int number_of_wells = 96;
            double angle;
            bool result = Wells.GetAngleForTwoTips( well1, well2, tip1_id, tip2_id, tip_spacing, number_of_wells, out angle);
            Assert.IsTrue( result);
            // from Octave, the answer should be either -41.295 or 156.35
            bool matches1 = Math.Abs( angle - 51.023) < 0.01;
            bool matches2 = Math.Abs( angle - (-169.10)) < 0.01;
            Assert.IsTrue( matches1 || matches2);
        }

        [TestMethod]
        public void TestDualTipAngleA12D7()
        {
            byte tip1_id = 1;
            byte tip2_id = 2;
            double tip_spacing = 18;
            string well1 = "A12";
            string well2 = "D7";
            int number_of_wells = 96;
            double angle;
            bool result = Wells.GetAngleForTwoTips( well1, well2, tip1_id, tip2_id, tip_spacing, number_of_wells, out angle);
            Assert.IsTrue( result);
            // from Octave, the answer should be either -41.295 or 156.35
            bool matches1 = Math.Abs( angle - (-10.904)) < 0.01;
            bool matches2 = Math.Abs( angle - 128.98) < 0.01;
            Assert.IsTrue( matches1 || matches2);
        }

        [TestMethod]
        public void TestDualTipAngleD4A1()
        {
            byte tip1_id = 1;
            byte tip2_id = 2;
            double tip_spacing = 18;
            string well1 = "D4";
            string well2 = "A1";
            int number_of_wells = 96;
            double angle;
            bool result = Wells.GetAngleForTwoTips( well1, well2, tip1_id, tip2_id, tip_spacing, number_of_wells, out angle);
            Assert.IsTrue( result);
            // from Octave, the answer should be either -41.295 or 156.35
            bool matches1 = Math.Abs( angle - 73.126) < 0.01;
            bool matches2 = Math.Abs( angle - (-163.13)) < 0.01;
            Assert.IsTrue( matches1 || matches2);
        }

        [TestMethod]
        public void TestDualTipAngleE6H1()
        {
            byte tip1_id = 1;
            byte tip2_id = 2;
            double tip_spacing = 18;
            string well1 = "E6";
            string well2 = "H1";
            int number_of_wells = 96;
            double angle;
            bool result = Wells.GetAngleForTwoTips( well1, well2, tip1_id, tip2_id, tip_spacing, number_of_wells, out angle);
            Assert.IsTrue( result);
            // from Octave, the answer should be either -41.295 or 156.35
            bool matches1 = Math.Abs( angle - (-10.904)) < 0.01;
            bool matches2 = Math.Abs( angle - 128.98) < 0.01;
            Assert.IsTrue( matches1 || matches2);
        }
    
        [TestMethod]
        public void TestDualTipAngleE7H12()
        {
            byte tip1_id = 1;
            byte tip2_id = 2;
            double tip_spacing = 18;
            string well1 = "E7";
            string well2 = "H12";
            int number_of_wells = 96;
            double angle;
            bool result = Wells.GetAngleForTwoTips( well1, well2, tip1_id, tip2_id, tip_spacing, number_of_wells, out angle);
            Assert.IsTrue( result);
            // from Octave, the answer should be either -41.295 or 156.35
            bool matches1 = Math.Abs( angle - (-128.98)) < 0.01;
            bool matches2 = Math.Abs( angle - 10.904) < 0.01;
            Assert.IsTrue( matches1 || matches2);
        }

        [TestMethod]
        public void TestDualTipAngleD7A12()
        {
            byte tip1_id = 1;
            byte tip2_id = 2;
            double tip_spacing = 18;
            string well1 = "D7";
            string well2 = "A12";
            int number_of_wells = 96;
            double angle;
            bool result = Wells.GetAngleForTwoTips( well1, well2, tip1_id, tip2_id, tip_spacing, number_of_wells, out angle);
            Assert.IsTrue( result);
            // from Octave, the answer should be either -41.295 or 156.35
            bool matches1 = Math.Abs( angle - 169.10) < 0.01;
            bool matches2 = Math.Abs( angle - (-51.023)) < 0.01;
            Assert.IsTrue( matches1 || matches2);
        }    
        #endregion

        #region tip1 = 2, tip2 = 1
        
        [TestMethod]
        public void TestDualTipAngleA1D4Reverse()
        {
            byte tip1_id = 2;
            byte tip2_id = 1;
            double tip_spacing = 18;
            string well1 = "A1";
            string well2 = "D4";
            int number_of_wells = 96;
            double angle;
            bool result = Wells.GetAngleForTwoTips( well1, well2, tip1_id, tip2_id, tip_spacing, number_of_wells, out angle);
            Assert.IsTrue( result);
            // from Octave, the answer should be either -41.295 or 156.35
            bool matches1 = Math.Abs( angle - (-163.13)) < 0.01;
            bool matches2 = Math.Abs( angle - 73.126) < 0.01;
            Assert.IsTrue( matches1 || matches2);
        }

        [TestMethod]
        public void TestDualTipAngleH1E6Reverse()
        {
            byte tip1_id = 2;
            byte tip2_id = 1;
            double tip_spacing = 18;
            string well1 = "H1";
            string well2 = "E6";
            int number_of_wells = 96;
            double angle;
            bool result = Wells.GetAngleForTwoTips( well1, well2, tip1_id, tip2_id, tip_spacing, number_of_wells, out angle);
            Assert.IsTrue( result);
            // from Octave, the answer should be either -41.295 or 156.35
            bool matches1 = Math.Abs( angle - 128.98) < 0.01;
            bool matches2 = Math.Abs( angle - (-10.904)) < 0.01;
            Assert.IsTrue( matches1 || matches2);
        }
    
        [TestMethod]
        public void TestDualTipAngleH12E7Reverse()
        {
            byte tip1_id = 2;
            byte tip2_id = 1;
            double tip_spacing = 18;
            string well1 = "H12";
            string well2 = "E7";
            int number_of_wells = 96;
            double angle;
            bool result = Wells.GetAngleForTwoTips( well1, well2, tip1_id, tip2_id, tip_spacing, number_of_wells, out angle);
            Assert.IsTrue( result);
            // from Octave, the answer should be either -41.295 or 156.35
            bool matches1 = Math.Abs( angle - 10.904) < 0.01;
            bool matches2 = Math.Abs( angle - (-128.98)) < 0.01;
            Assert.IsTrue( matches1 || matches2);
        }

        [TestMethod]
        public void TestDualTipAngleA12D7Reverse()
        {
            byte tip1_id = 2;
            byte tip2_id = 1;
            double tip_spacing = 18;
            string well1 = "A12";
            string well2 = "D7";
            int number_of_wells = 96;
            double angle;
            bool result = Wells.GetAngleForTwoTips( well1, well2, tip1_id, tip2_id, tip_spacing, number_of_wells, out angle);
            Assert.IsTrue( result);
            // from Octave, the answer should be either -41.295 or 156.35
            bool matches1 = Math.Abs( angle - (-51.023)) < 0.01;
            bool matches2 = Math.Abs( angle - 169.10) < 0.01;
            Assert.IsTrue( matches1 || matches2);
        }

        [TestMethod]
        public void TestDualTipAngleD4A1Reverse()
        {
            byte tip1_id = 2;
            byte tip2_id = 1;
            double tip_spacing = 18;
            string well1 = "D4";
            string well2 = "A1";
            int number_of_wells = 96;
            double angle;
            bool result = Wells.GetAngleForTwoTips( well1, well2, tip1_id, tip2_id, tip_spacing, number_of_wells, out angle);
            Assert.IsTrue( result);
            // from Octave, the answer should be either -41.295 or 156.35
            bool matches1 = Math.Abs( angle - 16.874) < 0.01;
            bool matches2 = Math.Abs( angle - (-106.87)) < 0.01;
            Assert.IsTrue( matches1 || matches2);
        }

        [TestMethod]
        public void TestDualTipAngleE6H1Reverse()
        {
            byte tip1_id = 2;
            byte tip2_id = 1;
            double tip_spacing = 18;
            string well1 = "E6";
            string well2 = "H1";
            int number_of_wells = 96;
            double angle;
            bool result = Wells.GetAngleForTwoTips( well1, well2, tip1_id, tip2_id, tip_spacing, number_of_wells, out angle);
            Assert.IsTrue( result);
            // from Octave, the answer should be either -41.295 or 156.35
            bool matches1 = Math.Abs( angle - (-51.023)) < 0.01;
            bool matches2 = Math.Abs( angle - 169.10) < 0.01;
            Assert.IsTrue( matches1 || matches2);
        }
    
        [TestMethod]
        public void TestDualTipAngleE7H12Reverse()
        {
            byte tip1_id = 2;
            byte tip2_id = 1;
            double tip_spacing = 18;
            string well1 = "E7";
            string well2 = "H12";
            int number_of_wells = 96;
            double angle;
            bool result = Wells.GetAngleForTwoTips( well1, well2, tip1_id, tip2_id, tip_spacing, number_of_wells, out angle);
            Assert.IsTrue( result);
            // from Octave, the answer should be either -41.295 or 156.35
            bool matches1 = Math.Abs( angle - (-169.10)) < 0.01;
            bool matches2 = Math.Abs( angle - 51.023) < 0.01;
            Assert.IsTrue( matches1 || matches2);
        }

        [TestMethod]
        public void TestDualTipAngleD7A12Reverse()
        {
            byte tip1_id = 2;
            byte tip2_id = 1;
            double tip_spacing = 18;
            string well1 = "D7";
            string well2 = "A12";
            int number_of_wells = 96;
            double angle;
            bool result = Wells.GetAngleForTwoTips( well1, well2, tip1_id, tip2_id, tip_spacing, number_of_wells, out angle);
            Assert.IsTrue( result);
            // from Octave, the answer should be either -41.295 or 156.35
            bool matches1 = Math.Abs( angle - 128.98) < 0.01;
            bool matches2 = Math.Abs( angle - (-10.904)) < 0.01;
            Assert.IsTrue( matches1 || matches2);
        }    
        #endregion
        */

        [TestMethod]
        public void TestReplaceInvalideFilenameCharacters()
        {
            Assert.AreEqual( "t-e-s-t-t-e-s-t-t-e", FileSystem.ReplaceInvalidFilenameCharacters( "t\\e/s:t*t?e\"s<t>t|e"));
        }

#if !HIG_INTEGRATION
        private class SampleReportClass
        {
            private readonly string _description;
            public string Description { get { return _description; } }

            public SampleReportClass( string desc)
            {
                _description = desc;
            }
        }

        [TestMethod]
        public void TestReflection()
        {
            object test = new SampleReportClass( "this is a test");
            Dictionary<string,string> props = BioNex.Shared.Utils.Reflection.GetPropertiesAndValues( test);
            Assert.AreEqual( 1, props.Count);
            Assert.AreEqual( "this is a test", props["Description"]);
        }

        [TestMethod]
        public void TestPiecewiseLinearFunction()
        {
            // declare out variables and return values.
            double min = 0.0;
            double max = 0.0;
            bool is_interpolated = false;
            double a = 0.0, b = 0.0, c = 0.0, d = 0.0;
            double ans = 0.0;

            // instantiate piecewise linear function object to be tested.
            PiecewiseLinearFunction plf = new PiecewiseLinearFunction();

            // test GetDomain with 0, 1, 2+ values.
            plf.GetDomain( out min, out max);
            Assert.AreEqual( double.MaxValue, min);
            Assert.AreEqual( double.MinValue, max);
            plf.Add( 5, 50);
            plf.GetDomain( out min, out max);
            Assert.AreEqual( 5, min);
            Assert.AreEqual( 5, max);
            plf.Add( 50, 500);
            plf.GetDomain( out min, out max);
            Assert.AreEqual( 5, min);
            Assert.AreEqual( 50, max);
            plf.Add( 10, 100);
            plf.GetDomain( out min, out max);
            Assert.AreEqual( 5, min);
            Assert.AreEqual( 50, max);

            // make sure adding the same input causes an exception to be thrown.
            try {
                plf.Add( 5, 50);
                plf.Add( 5, 20);
                Assert.Fail( "Adding the same input did not cause an exception to be thrown");
            } catch( ArgumentException) {
            }

            // to this point, we have the following data (5, 50), (10, 100), (50, 500), so the function is y = 10x with x from 5 to 50 inclusive.
            // test from 5 to 50 inclusive by increments of 1.
            for( double loop = 5.0; loop <= 50.0; loop += 1.0){
                // make sure output is correct.
                Assert.AreEqual( loop * 10.0, ans = plf.GetOutput( loop, out is_interpolated, out a, out b, out c, out d));
                // if the input is a param_name, then the param_value is not interpolated.
                Assert.AreEqual( plf.ContainsKey( loop), !is_interpolated);
                Assert.IsTrue( a <= loop);
                Assert.IsTrue( b >= loop);
                Assert.IsTrue( c <= ans);
                Assert.IsTrue( d >= ans);
            }

            // make sure inserting using the [] operator does not throw an exception.
            try {
                plf[ 5] = 50;
                plf[ 5] = 20;
            } catch( Exception) {
                Assert.Fail( "Using the [] operator threw an exception, but should not have");
            }

            // data has been modified to (5, 20), (10, 100), (50, 500), so the function is y = 16x - 60 with x from 5 to 10 and y = 10x from 10 to 50.
            // test again from 5 to 50 inclusive by increments of 1.
            for( double loop = 5.0; loop < 10.0; loop += 1.0){
                Assert.AreEqual( loop * 16.0 - 60.0, ans = plf.GetOutput( loop, out is_interpolated, out a, out b, out c, out d));
                Assert.AreEqual( plf.ContainsKey( loop), !is_interpolated);
                Assert.IsTrue( a <= loop);
                Assert.IsTrue( b >= loop);
                Assert.IsTrue( c <= ans);
                Assert.IsTrue( d >= ans);
            }
            for( double loop = 10.0; loop <= 50.0; loop += 1.0){
                Assert.AreEqual( loop * 10.0, ans = plf.GetOutput( loop, out is_interpolated, out a, out b, out c, out d));
                Assert.AreEqual( plf.ContainsKey( loop), !is_interpolated);
                Assert.IsTrue( a <= loop);
                Assert.IsTrue( b >= loop);
                Assert.IsTrue( c <= ans);
                Assert.IsTrue( d >= ans);
            }

            // test Clear.
            plf.Clear();
            plf.GetDomain( out min, out max);
            Assert.AreEqual( double.MaxValue, min);
            Assert.AreEqual( double.MinValue, max);

            // randomly populate with 10,000 elements on y = 1000x with x from 0 to 10,000.
            Random rnd = new Random();
            for( int loop = 0; loop < 10000; loop++){
                ans = rnd.NextDouble();
                plf[ loop * ans] = loop * ans * 1000;
            }

            // unless we're extremely unlucky (or lucky), these tests will pass...
            Assert.IsTrue( plf.Count >= 9990);
            Assert.IsTrue( plf.Count <= 10000);

            // insert (0, 0) and (10,000, 10,000,000).
            plf[ 0] = 0;
            plf[ 10000] = 10000000;

            // check the values are stored properly.
            double key_sum = 0.0;
            double value_sum = 0.0;
            foreach( KeyValuePair< double, double> kvp in plf){
                key_sum += kvp.Key;
                value_sum += kvp.Value;
            }
            Console.WriteLine( "Key sum: " + key_sum);
            Console.WriteLine( "Value sum: " + value_sum);
            Console.WriteLine( "Should be close to 1000: " + value_sum / key_sum);
            Assert.IsTrue( value_sum / key_sum >= 999);
            Assert.IsTrue( value_sum / key_sum <= 1001);

            // check the outputs are generated properly.
            double input_sum = 0.0;
            double output_sum = 0.0;
            for( int loop = 0; loop <= 10000; loop++){
                input_sum += loop;
                output_sum += plf.GetOutput( loop);
            }
            Console.WriteLine( "Input sum: " + input_sum);
            Console.WriteLine( "Output sum: " + output_sum);
            Console.WriteLine( "Should be close to 1000: " + output_sum / input_sum);
            Assert.IsTrue( output_sum / input_sum >= 999);
            Assert.IsTrue( output_sum / input_sum <= 1001);
        }

        [ DatabaseTableAttribute( "test")]
        public class TestType
        {
            [ DatabaseColumnAttribute( "test_bool", DatabaseColumnType.BOOLEAN)]
            public bool TestBool { get; set; }
            [ DatabaseColumnAttribute( "test_long", DatabaseColumnType.INTEGER, DatabaseColumnFlags.AUTOINCREMENT | DatabaseColumnFlags.PRIMARY_KEY)]
            public long TestLong { get; set; }
            [ DatabaseColumnAttribute( "test_double", DatabaseColumnType.FLOAT)]
            public double TestDouble { get; set; }
            [ DatabaseColumnAttribute( "test_text", DatabaseColumnType.TEXT)]
            public string TestText { get; set; }
        }

        [TestMethod]
        public void TestDatabaseIntegration()
        {
            const double e_f = 2.7182818284590452353602874713527;
            DatabaseIntegration db_int = new DatabaseIntegration( "Data Source=test_db_int.s3db");
            db_int.DropTable( typeof( TestType));
            db_int.CreateTable( typeof( TestType));
            TestType pi = new TestType();
            pi.TestBool = false;
            pi.TestDouble = Math.PI;
            pi.TestText = "pi";
            db_int.InsertEntity( pi);
            List< TestType> ret1 = db_int.SelectEntities< TestType>( "");
            Assert.AreEqual( 1, ret1.Count);
            TestType e = new TestType();
            e.TestBool = true;
            e.TestDouble = e_f;
            e.TestText = "e";
            db_int.InsertEntity( e);
            List< TestType> ret2 = db_int.SelectEntities< TestType>( "");
            Assert.AreEqual( 2, ret2.Count);
            List< TestType> ret3 = db_int.SelectEntities< TestType>( "where test_double < 3");
            Assert.AreEqual( 1, ret3.Count);
            Assert.AreEqual( Math.PI, ret1[ 0].TestDouble);
            Assert.AreEqual( Math.PI, ret2[ 0].TestDouble);
            Assert.AreEqual( e_f, ret2[ 1].TestDouble);
            Assert.AreEqual( e_f, ret3[ 0].TestDouble);
            db_int.UpdateEntity( pi, "where test_long = 2");
            List< TestType> ret4 = db_int.SelectEntities< TestType>( "");
            Assert.AreEqual( 2, ret4.Count);
            Assert.AreEqual( Math.PI, ret4[ 0].TestDouble);
            Assert.AreEqual( Math.PI, ret4[ 1].TestDouble);
            db_int.InsertEntity( e);
            TestType ten = new TestType();
            ten.TestBool = true;
            ten.TestDouble = 10.0;
            ten.TestText = "ten";
            db_int.InsertEntity( ten);
            List< TestType> ret5 = db_int.SelectEntities< TestType>( "");
            Assert.AreEqual( 4, ret5.Count);
            db_int.DeleteEntities( typeof( TestType), "where test_bool = 0");
            List< TestType> ret6 = db_int.SelectEntities< TestType>( "");
            Assert.AreEqual( 2, ret6.Count);
            Assert.AreEqual( true, ret6[ 0].TestBool);
            Assert.AreEqual( true, ret6[ 1].TestBool);
        }

        [TestMethod]
        public void TestRemoveFromByteArrayUntil()
        {
            byte[] buffer = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x42, 0x4D, 0x06, 0x07, 0x08, 0x09, 0x0A };
            buffer = buffer.RemoveFromByteArrayUntil( new byte[] { 0x42, 0x4D });
            Assert.AreEqual( 0x42, buffer[0]);
            Assert.AreEqual( 0x4D, buffer[1]);
            Assert.AreEqual( 0x06, buffer[2]);
            Assert.AreEqual( 0x07, buffer[3]);
            Assert.AreEqual( 0x08, buffer[4]);
            Assert.AreEqual( 0x09, buffer[5]);
            Assert.AreEqual( 0x0A, buffer[6]);
        }

        [TestMethod]
        public void TestHeaderNotFound()
        {
            byte[] buffer = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x42, 0x4D, 0x06, 0x07, 0x08, 0x09, 0x0A };
            try {
                buffer.RemoveFromByteArrayUntil( new byte[] { 0xFF, 0xD8 });
            } catch( BioNex.Shared.Utils.ExtensionMethods.HeaderNotInByteArrayException) {
                return;
            }
            Assert.Fail();
        }

        [TestMethod]
        public void TestStripBytesAfter()
        {
            byte[] buffer = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x42, 0x4D, 0x06, 0x07, 0x08, 0x09, 0x0A };
            buffer = buffer.StripBytesAfter( 8);
            Assert.AreEqual( 0x4D, buffer[7]);
        }
        
        [TestMethod]
        public void TestCreateInBetweenTeachpointNamesForHive()
        {
            // success case
            const string top = "Rack 1, Slot 1";
            const string bottom = "Rack 1, Slot 13";
            List<string> teachpoint_names = Utils.Strings.GenerateIntermediateTeachpointNames( top, bottom);
            Assert.AreEqual( 13, teachpoint_names.Count());
            for( int i=1; i<=13; i++)
                Assert.AreEqual( String.Format( "Rack 1, Slot {0}", i), teachpoint_names[i-1]);

            // failure case
            const string bad_bottom = "Rack 2, Slot 13";
            teachpoint_names = Utils.Strings.GenerateIntermediateTeachpointNames( top, bad_bottom);
            Assert.AreEqual( 0, teachpoint_names.Count());
        }

        [TestMethod]
        public void TestCreateInBetweenTeachpointNamesForBPS140()
        {
            // success case
            const string top = "Side 1: Rack 1, Slot 1";
            const string bottom = "Side 1: Rack 1, Slot 13";
            List<string> teachpoint_names = Utils.Strings.GenerateIntermediateTeachpointNames( top, bottom);
            Assert.AreEqual( 13, teachpoint_names.Count());
            for( int i=1; i<=13; i++)
                Assert.AreEqual( String.Format( "Side 1: Rack 1, Slot {0}", i), teachpoint_names[i-1]);

            // failure case
            const string bad_bottom = "Side 1: Rack 2, Slot 13";
            teachpoint_names = Utils.Strings.GenerateIntermediateTeachpointNames( top, bad_bottom);
            Assert.AreEqual( 0, teachpoint_names.Count());
        }

        [TestMethod]
        public void TestStandardDeviationOfDoubles()
        {
            List<double> samples = new List<double> { 1.1, 1.4, 1.3, 1.2, 1, 1.5, 1.2, 1.3, 1.5, 1.5, 1.5, 1.4, 1.5 };
            Assert.IsTrue( Math.Abs( 0.17 - samples.StandardDeviation()) < 0.01);
        }

        [TestMethod]
        public void TestStandardDeviationOfLongs()
        {
            List<long> samples = new List<long> { 4, 5, 5, 5, 7, 5, 6, 4, 3, 5, 4, 3, 7 };
            double stddev = samples.StandardDeviation();
            Assert.IsTrue( Math.Abs( 1.28 - stddev) < 0.01);
        }

        [TestMethod]
        public void TestStandardDeviationEmptyDoubles()
        {
            List<double> samples = new List<double>();
            Assert.AreEqual( 0, samples.StandardDeviation());
        }

        [TestMethod]
        public void TestStandardDeviationEmptyLongs()
        {
            List<long> samples = new List<long>();
            Assert.AreEqual( 0, samples.StandardDeviation());
        }
#endif
    }
}
