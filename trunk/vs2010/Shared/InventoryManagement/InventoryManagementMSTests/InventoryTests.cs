using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BioNex.Shared.InventoryManagement;
using BioNex.Shared.LibraryInterfaces;

namespace InventoryManagementMSTests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class InventoryTests
    {
        public InventoryTests()
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

        Inventory _inventory { get; set; }

        [TestInitialize]
        public void Init()
        {
            _inventory = new Inventory();
        }

        [TestMethod]
        public void TestCreateDatabaseFile()
        {
            _inventory.CreateDatabase("test_create.s3db", new List<string> { "rack", "slot" });
        }

        [TestMethod]
        public void TestLoadDatabaseFile()
        {
            TestCreateDatabaseFile();
            try
            {
                _inventory.LoadDatabase("test_create.s3db", new List<string> { "rack", "slot" });
            }
            catch (Exception)
            {
                Assert.Fail("LoadDatabase should not have thrown an exception");
            }

            try
            {
                _inventory.LoadDatabase("test_create.s3db", new List<string> { "rack", "slot", "invalid_field_name" });
                Assert.Fail("LoadDatabase should have thrown an exception");
            }
            catch (InventorySchemaMismatchException)
            {
                // good, this is the type of exception that we wanted to catch
            }
            catch (Exception)
            {
                // bad, we threw a different kind of exception
                Assert.Fail("LoadDatabase threw an exception, but the wrong kind");
            }
        }

        [TestMethod]
        public void TestGetLocation()
        {
            TestCreateDatabaseFile();
            _inventory.Load("test barcode", new Dictionary<string, string>()
                                        {
                                            { "rack", "1" },
                                            { "slot", "2" }
                                        });
            Dictionary<string, string> location = _inventory.GetLocation("test barcode");
            Assert.AreEqual("1", location["rack"]);
            Assert.AreEqual("2", location["slot"]);
            Assert.AreEqual(true, bool.Parse(location["loaded"]));
        }

        [TestMethod]
        public void TestGetPlateId()
        {
            TestCreateDatabaseFile();
            _inventory.Load("test barcode", new Dictionary<string, string>()
                                        {
                                            { "rack", "1" },
                                            { "slot", "2" }
                                        });
            int plate_id = _inventory.GetPlateIdFromBarcode("test barcode");
            Assert.AreEqual(1, plate_id);
        }

        [TestMethod, Ignore]
        public void TestGetInventoryXML()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void TestGetInventoryData()
        {
            TestCreateDatabaseFile();
            _inventory.Load("test barcode", new Dictionary<string, string>()
                                        {
                                            { "rack", "1" },
                                            { "slot", "2" }
                                        });
            Dictionary<string, Dictionary<string, string>> inventory = _inventory.GetInventoryData();
            Assert.IsTrue(inventory.ContainsKey("test barcode"));
            Assert.AreEqual("1", inventory["test barcode"]["rack"]);
            Assert.AreEqual("2", inventory["test barcode"]["slot"]);
            _inventory.Load("test barcode 2", new Dictionary<string, string>()
                                        {
                                            { "rack", "2" },
                                            { "slot", "6" }
                                        });
            inventory = _inventory.GetInventoryData();
            Assert.AreEqual(2, inventory.Count);
            Assert.IsTrue(inventory.ContainsKey("test barcode 2"));
            Assert.AreEqual("2", inventory["test barcode 2"]["rack"]);
            Assert.AreEqual("6", inventory["test barcode 2"]["slot"]);
        }

        [TestMethod]
        public void TestLoad()
        {
            TestCreateDatabaseFile();
            _inventory.Load("test barcode", new Dictionary<string, string>()
                                        {
                                            { "rack", "1" },
                                            { "slot", "2" }
                                        });
        }

        [TestMethod]
        public void TestLoadDuplicateBarcode()
        {
            TestCreateDatabaseFile();
            Dictionary<string, string> location = new Dictionary<string, string>() {
                                                { "rack", "1" },
                                                { "slot", "2" }
                                                };
            _inventory.Load("test barcode", location);

            try
            {
                _inventory.Load("test barcode", location);
            }
            catch (Exception)
            {
                Assert.Fail("Load should not have thrown an exception");
            }
        }

        [TestMethod]
        public void TestUnload()
        {
            TestCreateDatabaseFile();
            _inventory.Load("test barcode", new Dictionary<string, string>()
                                        {
                                            { "rack", "1" },
                                            { "slot", "2" }
                                        });
            _inventory.Unload("test barcode");
            Dictionary<string, string> location_info = _inventory.GetLocation("test barcode");
            Assert.AreEqual(false, bool.Parse(location_info["loaded"]));
        }

        [TestMethod]
        public void TestDelete()
        {
            TestCreateDatabaseFile();
            _inventory.Load("test barcode", new Dictionary<string, string>()
                                        {
                                            { "rack", "1" },
                                            { "slot", "2" }
                                        });
            _inventory.Delete("test barcode");
            try
            {
                _inventory.GetLocation("test barcode");
                Assert.Fail("GetLocation should have thrown an exception");
            }
            catch (InventoryBarcodeNotFoundException)
            {
                // good, this is what we want to catch
            }
            catch (Exception)
            {
                // bad, we specifically wanted to catch an InventoryBarcodeNotFoundException
                Assert.Fail("GetLocation threw an exception, but not the right kind");
            }
        }

        [TestMethod]
        public void TestGetVolume()
        {
            TestCreateDatabaseFile();
            _inventory.Load("test barcode", new Dictionary<string, string>()
                                        {
                                            { "rack", "1" },
                                            { "slot", "2" }
                                        });
            _inventory.SetInitialVolume("test barcode", new List<string> { "A1", "A2" }, 10);
            double volume = _inventory.GetVolume("test barcode", "A1");
            Assert.AreEqual(10, volume);
            volume = _inventory.GetVolume("test barcode", "A2");
            Assert.AreEqual(10, volume);
        }

        [TestMethod]
        public void TestGetAllVolumes()
        {
            TestCreateDatabaseFile();
            _inventory.Load("test barcode", new Dictionary<string, string>()
                                        {
                                            { "rack", "1" },
                                            { "slot", "2" }
                                        });
            _inventory.SetInitialVolume("test barcode", new List<string> { "A1", "A2", "C3" }, 10);
            _inventory.AdjustVolume("test barcode", new List<string> { "A1" }, 10);
            _inventory.AdjustVolume("test barcode", new List<string> { "A2" }, -20);
            _inventory.AdjustVolume("test barcode", new List<string> { "C3" }, 30);
            Dictionary<string, double> volumes = _inventory.GetAllVolumes("test barcode");
            Assert.AreEqual(20, volumes["A1"]);
            Assert.AreEqual(-10, volumes["A2"]);
            Assert.AreEqual(40, volumes["C3"]);
        }

        [TestMethod]
        public void TestSetInitialVolume()
        {
            TestCreateDatabaseFile();
            _inventory.Load("test barcode", new Dictionary<string, string>()
                                        {
                                            { "rack", "1" },
                                            { "slot", "2" }
                                        });
            _inventory.SetInitialVolume("test barcode", new List<string> { "A1", "A2" }, 10);
        }

        [TestMethod]
        public void TestAdjustVolume()
        {
            TestCreateDatabaseFile();
            _inventory.Load("test barcode", new Dictionary<string, string>()
                                        {
                                            { "rack", "1" },
                                            { "slot", "2" }
                                        });
            _inventory.SetInitialVolume("test barcode", new List<string> { "A1", "A2" }, 10);
            _inventory.AdjustVolume("test barcode", new List<string> { "A1" }, 20);
            double volume = _inventory.GetVolume("test barcode", "A1");
            Assert.AreEqual(30, volume);
            _inventory.AdjustVolume("test barcode", new List<string> { "A1" }, -10);
            volume = _inventory.GetVolume("test barcode", "A1");
            Assert.AreEqual(20, volume);
        }
    }
}
