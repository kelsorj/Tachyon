using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GreenMachineUnitTests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class TTRobotCommandTests
    {
        public TTRobotCommandTests()
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
        public void TestParseAxisQuery()
        {
            BioNex.GreenMachine.HardwareInterfaces.TTSyringeRobot robot = new BioNex.GreenMachine.HardwareInterfaces.TTSyringeRobot( 1);
            double position;
            bool enabled;
            bool homed;
            // at 10mm, servo on
            robot.ParseAxisResponseHelper( "#99212021C00000000002715AF\r\n", out position, out enabled, out homed);
            Assert.IsTrue( Math.Abs( 10.005 - position) < 0.01);
            Assert.IsTrue( enabled);
            // at 10mm, servo off
            robot.ParseAxisResponseHelper( "#9921202040000000000270EAF\r\n", out position, out enabled, out homed);
            Assert.IsTrue( Math.Abs( 9.998 - position) < 0.01);
            Assert.IsFalse( enabled);
        }
    }
}
