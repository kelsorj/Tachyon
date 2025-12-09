using System;
using BioNex.Hive.Hardware;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BioNex.Shared.Utils
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class CoordinateTransformTests
    {
        public CoordinateTransformTests()
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

        [TestMethod, Ignore]
        public void TestConvertZToolspaceToZWorldspace()
        {
            /*
            double z_tool = 10;
            double y_tool = 95;
            double arm_length = 250;
            double finger_offset = 28;
            double z_world = Hive.ConvertZToolToWorld( arm_length, finger_offset, z_tool, y_tool);
            Assert.IsTrue( Math.Abs(269.25 - z_world) < 0.01);
            // now make sure that we can convert back with Mark's existing function
            double z_tool_mark = Hive.ZRobPosFuncYRobZMot( arm_length, finger_offset, y_tool, z_world);
            Assert.AreEqual( z_tool, z_tool_mark);
            */
        }

        [TestMethod, Ignore]
        public void TestZCammingRange()
        {
            /*
            double y_start = 9;
            double y_end = 95;
            double arm_length = 250;
            double z_camming_range = Hive.GetZCammingRange( arm_length, y_start, y_end);
            Assert.IsTrue( Math.Abs( 18.591 - z_camming_range) < 0.01);
            */
        }

        [TestMethod]
        public void TestGetYFromTheta()
        {
            const double arm_length = 250;
            const double theta = 5.3;
            double y = HiveMath.GetYFromTheta( arm_length, theta);
            Assert.IsTrue( Math.Abs( 23.093 - y) < 0.01);
            // compare with Mark's function
            double y_mark = HiveMath.GetYFromTheta( arm_length, theta);
            Assert.IsTrue( Math.Abs( y - y_mark) < 0.01);
        }

        [TestMethod]
        public void TestTZWorldToYZTool()
        {
            const double arm_length = 250;
            const double finger_offset = 28;
            const double t_world = 25;
            const double z_world = 245;
            Tuple< double, double> yz_tool = HiveMath.ConvertTZWorldToYZTool( arm_length, finger_offset, t_world, z_world);
            Assert.IsTrue( Math.Abs( 105.65 - yz_tool.Item1) < 0.01);
            Assert.IsTrue( Math.Abs( -9.58 - yz_tool.Item2) < 0.01);
        }

    }
}
