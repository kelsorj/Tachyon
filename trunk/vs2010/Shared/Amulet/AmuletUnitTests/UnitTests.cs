using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BioNex.Shared.Amulet;

namespace AmuletUnitTests
{
    /// <summary>
    /// Summary description for UnitTests
    /// </summary>
    [TestClass]
    public class UnitTests
    {
        public UnitTests()
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
        public void TestByteToHexString()
        {
            Assert.AreEqual( Utils.ByteToHexString( 0), "00");
            Assert.AreEqual( Utils.ByteToHexString( 1), "01");
            Assert.AreEqual( Utils.ByteToHexString( 26), "1A");
            Assert.AreEqual( Utils.ByteToHexString( 161), "A1");
            Assert.AreEqual( Utils.ByteToHexString( 170), "AA");
            Assert.AreEqual( Utils.ByteToHexString( 255), "FF");
        }

        [TestMethod]
        public void TestWordToHexString()
        {
            Assert.AreEqual( Utils.WordToHexString( 0), "0000");
            Assert.AreEqual( Utils.WordToHexString( 1), "0001");
            Assert.AreEqual( Utils.WordToHexString( 26), "001A");
            Assert.AreEqual( Utils.WordToHexString( 161), "00A1");
            Assert.AreEqual( Utils.WordToHexString( 170), "00AA");
            Assert.AreEqual( Utils.WordToHexString( 255), "00FF");
            Assert.AreEqual( Utils.WordToHexString( 291), "0123");
            Assert.AreEqual( Utils.WordToHexString( 801), "0321");
            Assert.AreEqual( Utils.WordToHexString( 4095), "0FFF");
            Assert.AreEqual( Utils.WordToHexString( 4660), "1234");
            Assert.AreEqual( Utils.WordToHexString( 39030), "9876");
            Assert.AreEqual( Utils.WordToHexString( 65535), "FFFF");
        }

        [TestMethod]
        public void TestByteArrayToByte()
        {
            byte[] test = { (byte)'D', (byte)'8' };
            Assert.AreEqual( 0xD8, test.ByteArrayToByte());
        }

        [TestMethod]
        public void TestByteArrayToWord()
        {
            byte[] test = { (byte)'E', (byte)'5', (byte)'B', (byte)'0' };
            Assert.AreEqual( 0xE5B0, test.ByteArrayToWord());
        }

        [TestMethod]
        public void TestByteArrayToString()
        {
            byte[] test = { (byte)'T', (byte)'e', (byte)'s', (byte)'t' };
            Assert.AreEqual( "Test", test.ByteArrayToString());
        }

        [TestMethod]
        public void TestByteToByteArray()
        {
            byte test = 0xD8;
            byte[] result = test.ByteToByteArray();
            Assert.AreEqual( (byte)'D', result[0]);
            Assert.AreEqual( (byte)'8', result[1]);
        }

        [TestMethod]
        public void TestWordToByteArray()
        {
            ushort test = 0xD81E;
            byte[] result = test.WordToByteArray();
            Assert.AreEqual( (byte)'D', result[0]);
            Assert.AreEqual( (byte)'8', result[1]);
            Assert.AreEqual( (byte)'1', result[2]);
            Assert.AreEqual( (byte)'E', result[3]);
        }
    }
}
