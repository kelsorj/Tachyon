using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BumblebeeRandomHitpickGenerator;

namespace BioNex.BumblebeeRandomHitpickGenerator.RandomHitpickTests
{
    /// <summary>
    /// Summary description for RandomHitpickTests
    /// </summary>
    [TestClass]
    public class RandomHitpickTests
    {
        public RandomHitpickTests()
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
        public void TestGenerateRandomPlateTypes()
        {
            List<PlateType> random_plates = RandomFunctions.GetRandomPlateTypes( 5);
            Assert.AreEqual( 5, random_plates.Count);
        }

        [TestMethod]
        public void TestGenerateRandomWells()
        {
            string labware_name;
            // 96
            List<string> wells = RandomFunctions.GetRandomWells( PlateType.Plate96, out labware_name, 1, 1, 5, 10);
            Assert.AreEqual( 1, wells.Count);
            wells = RandomFunctions.GetRandomWells( PlateType.Plate96, out labware_name, 5, 10, 20, 30);
            Assert.IsTrue( wells.Count >= 5 && wells.Count <= 10);
            wells = RandomFunctions.GetRandomWells( PlateType.Plate96, out labware_name, 20, 30, 5, 10);
            Assert.IsTrue( wells.Count >= 20 && wells.Count <= 30);
            // 384
            wells = RandomFunctions.GetRandomWells( PlateType.Plate384, out labware_name, 5, 10, 1, 1);
            Assert.AreEqual( 1, wells.Count);
            wells = RandomFunctions.GetRandomWells( PlateType.Plate384, out labware_name, 1, 1, 5, 10);
            Assert.IsTrue( wells.Count >= 5 && wells.Count <= 10);
            wells = RandomFunctions.GetRandomWells( PlateType.Plate384, out labware_name, 5, 10, 20, 30);
            Assert.IsTrue( wells.Count >= 20 && wells.Count <= 30);
        }

        [TestMethod]
        public void TestGetRandomPlateTypes()
        {
            int num_plates = 50;
            List<PlateType> plates = RandomFunctions.GetRandomPlateTypes( num_plates);
            int ctr = 0;
            foreach( PlateType plate in plates) {
                if( plate == PlateType.Plate96)
                    ctr++;
                else if( plate == PlateType.Plate384)
                    ctr--;
                else
                    Assert.Fail( "GetRandomPlateTypes should not have returned PlateType.Random");
            }
            // this metric for randomness is not based on any theory, it's just a good number IM.O
            Assert.IsTrue( (double)Math.Abs( ctr) / num_plates < 0.6);
        }

        [TestMethod]
        public void TestNumberOfWellsFromPlateType()
        {
            Assert.AreEqual( 96, RandomFunctions.GetNumberOfWellsFromPlateType( PlateType.Plate96));
            Assert.AreEqual( 384, RandomFunctions.GetNumberOfWellsFromPlateType( PlateType.Plate384));
        }

        [TestMethod]
        public void TestGetLabwareNameFromPlateType()
        {
            Assert.AreEqual( "NUNC 96 clear round well flat bottom", RandomFunctions.GetLabwareNameFromPlateType( PlateType.Plate96));
            Assert.AreEqual( "NUNC 384 clear drafted square well", RandomFunctions.GetLabwareNameFromPlateType( PlateType.Plate384));
        }
    }
}
