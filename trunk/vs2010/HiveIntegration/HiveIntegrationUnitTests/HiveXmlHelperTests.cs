using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HiveIntegrationUnitTests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class HiveXmlHelperTests
    {
        public HiveXmlHelperTests()
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
        public void TestInitializeParamsSerialization()
        {
            BioNex.HiveIntegration.HiveIntegrationInterface _hive = new BioNex.HiveIntegration.Hive();
            string init_xml = BioNex.HiveIntegration.HiveXmlHelper.InitializeParamsToXml( "localhost", 7777);
            BioNex.HiveIntegration.HiveXmlHelper.InitializeParams init = BioNex.HiveIntegration.HiveXmlHelper.XmlToInitializeParams( init_xml);            
            Assert.AreEqual( "localhost", init.IpAddress);
            Assert.AreEqual( 7777, init.Port);
        }
    }
}
