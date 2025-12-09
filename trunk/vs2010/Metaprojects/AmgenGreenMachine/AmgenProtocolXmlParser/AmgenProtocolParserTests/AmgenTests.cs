using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BioNex.AmgenProtocolXmlParser;

namespace AmgenProtocolParserTests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class AmgenTests
    {
        public AmgenTests()
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
        public void TestParseValidFile()
        {
            AmgenParser parser = new AmgenParser();
            parser.LoadProtocolFile( @"..\..\..\AmgenProtocolXmlParser\AmgenProtocolParserTests\sample_amgen_protocol.xml");
            // check high level stuff like plate name and number of tasks
            Assert.AreEqual( parser.LabwareName, "96_Costar3365");
            IList<Tuple<string,Dictionary<string,object>>> tasks = parser.Tasks;
            Assert.AreEqual( 19, tasks.Count());
            // now check for ordering of tasks
            const string stackplate = "stackplate";
            const string prime = "prime";
            const string aspirate = "aspirate";
            const string dispense = "dispense";
            const string wash = "wash";
            const string mix = "mix";
            Assert.AreEqual( stackplate, tasks[0].Item1);
            Assert.AreEqual( prime, tasks[1].Item1);
            Assert.AreEqual( aspirate, tasks[2].Item1);
            Assert.AreEqual( dispense, tasks[3].Item1);
            Assert.AreEqual( dispense, tasks[4].Item1);
            Assert.AreEqual( dispense, tasks[5].Item1);
            Assert.AreEqual( dispense, tasks[6].Item1);
            Assert.AreEqual( wash, tasks[7].Item1);
            Assert.AreEqual( mix, tasks[8].Item1);
            Assert.AreEqual( wash, tasks[9].Item1);
            Assert.AreEqual( prime, tasks[10].Item1);
            Assert.AreEqual( aspirate, tasks[11].Item1);
            Assert.AreEqual( dispense, tasks[12].Item1);
            Assert.AreEqual( dispense, tasks[13].Item1);
            Assert.AreEqual( dispense, tasks[14].Item1);
            Assert.AreEqual( dispense, tasks[15].Item1);
            Assert.AreEqual( dispense, tasks[16].Item1);
            Assert.AreEqual( stackplate, tasks[17].Item1);
            Assert.AreEqual( wash, tasks[18].Item1);
            // now check for the correct parameter values for each task TYPE
            // check stackplate
            Assert.AreEqual( "input", tasks[0].Item2["source"]);
            Assert.AreEqual( "1", tasks[0].Item2["dest"]);
            Assert.AreEqual( "false", tasks[0].Item2["barcode"]);
            // check prime
            Assert.AreEqual( "1", tasks[1].Item2["cycles"]);
            // check aspirate
            Assert.AreEqual( "A1", tasks[2].Item2["column"]);
            Assert.AreEqual( "200", tasks[2].Item2["speed"]);
            Assert.AreEqual( "50.3", tasks[2].Item2["volume"]);
            Assert.AreEqual( "0", tasks[2].Item2["airgap"]);
            Assert.AreEqual( "2.8", tasks[2].Item2["height"]);
            // check dispense
            Assert.AreEqual( "A2", tasks[3].Item2["column"]);
            Assert.AreEqual( "400", tasks[3].Item2["speed1"]);
            Assert.AreEqual( "33.6", tasks[3].Item2["volume1"]);
            Assert.AreEqual( "800", tasks[3].Item2["speed2"]);
            Assert.AreEqual( "66.4", tasks[3].Item2["volume2"]);
            Assert.AreEqual( "4.1", tasks[3].Item2["height"]);
            // check wash
            Assert.AreEqual( "1", tasks[7].Item2["cycles"]);
            // check mix
            Assert.AreEqual( "5", tasks[8].Item2["cycles"]);
            Assert.AreEqual( "A5", tasks[8].Item2["column"]);
            Assert.AreEqual( "75", tasks[8].Item2["volume"]);
            Assert.AreEqual( "800", tasks[8].Item2["speed"]);
            Assert.AreEqual( "1.8", tasks[8].Item2["height"]);
            Assert.AreEqual( "-1.8", tasks[8].Item2["deltaz"]);
        }
    }
}
